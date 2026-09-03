using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 6.4.1 runtime for vanilla NPC signature identities with relationship affinity.
/// The five Alpha 6 signature prototypes (Abigail, Alex, Harvey, Maru, Emily)
/// remain owned by Alpha6CombatPolishService so this layer is additive and low-risk.
/// Temporary buffs are runtime-only and flow through ProgressionService, so generic
/// attacks, healing, expansion skills, and equipment previews share the same math.
/// </summary>
public sealed class CharacterSkillIdentityService
{
    private readonly ProgressionService _progression;
    private readonly Dictionary<string, int> _cooldowns = new(StringComparer.OrdinalIgnoreCase);

    public CharacterSkillIdentityService(ProgressionService progression)
    {
        _progression = progression;
    }

    public void Clear()
    {
        _cooldowns.Clear();
        _progression.ClearTemporaryModifiers();
    }

    public void Update(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        _progression.TickTemporaryModifiers();
        TickCooldowns();

        List<PartyMemberData> activeMembers = members
            .Where(member => member.RecruiterId == recruiterId)
            .Where(member => member.State == PartyMemberState.Following)
            .Where(member => !member.IsDowned && !member.IsWithdrawn && member.CurrentHealth > 0)
            .ToList();

        if (activeMembers.Count == 0)
            return;

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .ToList();

        foreach (PartyMemberData member in activeMembers)
        {
            CharacterSkillIdentity? identity = CharacterSkillIdentityCatalog.Get(member.CharacterName);
            if (identity is null || GetCooldown(member.CharacterName) > 0)
                continue;

            NPC? npc = GetActiveNpc(member);
            if (npc is null)
                continue;

            NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
            if (profile is null)
                continue;

            PartyRole role = ResolveRole(member);
            if (role != profile.PrimaryRole && role != profile.SecondaryRole)
                continue;

            int tier = GetSignatureTier(member, role);
            if (tier < 2)
                continue;

            int affinity = Math.Max(2, profile.GetAffinity(role));
            bool used = TryUse(identity, npc, member, role, tier, affinity, activeMembers, monsters);
            if (!used)
                continue;

            int baseCooldown = tier >= 3 ? identity.Tier3CooldownTicks : identity.Tier2CooldownTicks;
            _cooldowns[member.CharacterName] = ScaleCooldown(member, role, baseCooldown);
            AwardSkillProgress(member, role, npc, tier >= 3 ? 5 : 3, tier >= 3 ? 3 : 2);
        }
    }

    private bool TryUse(
        CharacterSkillIdentity identity,
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int tier,
        int affinity,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyList<Monster> monsters)
    {
        return identity.Archetype switch
        {
            CharacterSignatureArchetype.Recovery => TryRecovery(identity, npc, member, role, tier, affinity, activeMembers),
            CharacterSignatureArchetype.Guard => TryGuard(identity, npc, member, role, tier, affinity, activeMembers, monsters),
            CharacterSignatureArchetype.Control => TryControl(identity, npc, member, role, tier, affinity, activeMembers, monsters),
            CharacterSignatureArchetype.Rally => TryRally(identity, npc, member, role, tier, affinity, activeMembers, monsters),
            CharacterSignatureArchetype.Burst => TryBurst(identity, npc, member, role, tier, affinity, activeMembers, monsters),
            CharacterSignatureArchetype.Sweep => TrySweep(identity, npc, member, role, tier, affinity, activeMembers, monsters),
            CharacterSignatureArchetype.Hybrid => TryHybrid(identity, npc, member, role, tier, affinity, activeMembers, monsters),
            _ => false
        };
    }

    private bool TryRecovery(
        CharacterSkillIdentity identity,
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int tier,
        int affinity,
        IReadOnlyList<PartyMemberData> activeMembers)
    {
        float farmerRatio = Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);
        List<PartyMemberData> injured = activeMembers
            .Where(other => _progression.GetHealthRatio(other) < identity.TriggerHealthRatio)
            .OrderBy(other => _progression.GetHealthRatio(other))
            .ToList();

        if (farmerRatio >= identity.TriggerHealthRatio && injured.Count == 0)
            return false;

        int heal = ScaleHeal(identity.BaseHeal, affinity, tier, member, role);
        int restored = HealFarmer(heal);
        int allyLimit = Math.Max(1, identity.MaxTargets - 1);
        foreach (PartyMemberData target in injured.Take(tier >= 3 ? allyLimit + 1 : allyLimit))
        {
            NPC? targetNpc = GetActiveNpc(target);
            if (targetNpc is null || Vector2.Distance(targetNpc.Tile, npc.Tile) > identity.Radius)
                continue;

            int before = target.CurrentHealth;
            target.CurrentHealth = Math.Min(_progression.GetMaxHealth(target), target.CurrentHealth + heal);
            restored += Math.Max(0, target.CurrentHealth - before);
            SpawnBurst(targetNpc.Position, RoleColor(role), 4, 22f);
        }

        if (restored <= 0)
            return false;

        ApplyConfiguredBuff(identity, member, activeMembers, tier);
        ShowUse(npc, identity.SignatureName, RoleColor(role), restored > 0 ? $" +{restored}" : string.Empty, "yoba");
        SpawnBurst(Game1.player.Position, RoleColor(role), tier >= 3 ? 9 : 6, 34f);
        return true;
    }

    private bool TryGuard(
        CharacterSkillIdentity identity,
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int tier,
        int affinity,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyList<Monster> monsters)
    {
        List<Monster> targets = monsters
            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= identity.Radius
                || Vector2.Distance(monster.Tile, Game1.player.Tile) <= identity.Radius)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, Game1.player.Tile))
            .Take(identity.MaxTargets)
            .ToList();

        if (targets.Count == 0)
            return false;

        int damage = ScaleDamage(identity.BaseDamage, affinity, tier, member, role);
        int stun = ScaleStun(tier >= 3 ? identity.Tier3StunMs : identity.Tier2StunMs, member, role);
        foreach (Monster monster in targets)
        {
            DamageMonster(monster, damage, identity.Knockback);
            if (stun > 0 && monster.Health > 0)
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, stun);
            SpawnBurst(monster.Position, RoleColor(role), 4, 26f);
        }

        ApplyConfiguredBuff(identity, member, activeMembers, tier);
        ShowUse(npc, identity.SignatureName, RoleColor(role), string.Empty, "clubSmash");
        return true;
    }

    private bool TryControl(
        CharacterSkillIdentity identity,
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int tier,
        int affinity,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyList<Monster> monsters)
    {
        if (monsters.Count == 0)
            return false;

        Monster? anchor = monsters
            .Where(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= Math.Max(6f, identity.Radius + 2f))
            .OrderByDescending(monster => monsters.Count(other => Vector2.Distance(other.Tile, monster.Tile) <= identity.Radius))
            .FirstOrDefault();
        if (anchor is null)
            return false;

        List<Monster> targets = monsters
            .Where(monster => Vector2.Distance(monster.Tile, anchor.Tile) <= identity.Radius)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, anchor.Tile))
            .Take(identity.MaxTargets)
            .ToList();
        if (targets.Count == 0)
            return false;

        int damage = ScaleDamage(identity.BaseDamage, affinity, tier, member, role);
        int stun = ScaleStun(tier >= 3 ? identity.Tier3StunMs : identity.Tier2StunMs, member, role);
        foreach (Monster monster in targets)
        {
            if (damage > 0)
                DamageMonster(monster, damage, identity.Knockback);
            if (stun > 0 && monster.Health > 0)
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, stun);
            SpawnBurst(monster.Position, RoleColor(role), tier >= 3 ? 6 : 4, 24f);
        }

        ApplyConfiguredBuff(identity, member, activeMembers, tier);
        ShowUse(npc, identity.SignatureName, RoleColor(role), string.Empty, "thunder_small");
        return true;
    }

    private bool TryRally(
        CharacterSkillIdentity identity,
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int tier,
        int affinity,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyList<Monster> monsters)
    {
        bool partyUnderPressure = activeMembers.Any(other => _progression.GetHealthRatio(other) < 0.82f)
            || Game1.player.health < Game1.player.maxHealth * 0.82f;
        if (monsters.Count == 0 && !partyUnderPressure)
            return false;

        int restored = 0;
        if (identity.BaseHeal > 0 && partyUnderPressure)
        {
            int heal = ScaleHeal(identity.BaseHeal, affinity, tier, member, role);
            restored += HealFarmer(heal);
            foreach (PartyMemberData target in activeMembers.Where(other => _progression.GetHealthRatio(other) < 0.90f))
            {
                int before = target.CurrentHealth;
                target.CurrentHealth = Math.Min(_progression.GetMaxHealth(target), target.CurrentHealth + Math.Max(1, heal / 2));
                restored += Math.Max(0, target.CurrentHealth - before);
            }
        }

        if (identity.BaseDamage > 0 && monsters.Count > 0)
        {
            int damage = ScaleDamage(identity.BaseDamage, affinity, tier, member, role);
            foreach (Monster monster in monsters
                .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= identity.Radius)
                .Take(identity.MaxTargets))
            {
                DamageMonster(monster, damage, identity.Knockback);
                SpawnBurst(monster.Position, RoleColor(role), 3, 20f);
            }
        }

        ApplyConfiguredBuff(identity, member, activeMembers, tier);
        ShowUse(npc, identity.SignatureName, RoleColor(role), restored > 0 ? $" +{restored}" : string.Empty, "reward");
        SpawnBurst(npc.Position, RoleColor(role), tier >= 3 ? 10 : 7, 38f);
        return true;
    }

    private bool TryBurst(
        CharacterSkillIdentity identity,
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int tier,
        int affinity,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyList<Monster> monsters)
    {
        Monster? target = monsters
            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= identity.Radius
                || Vector2.Distance(monster.Tile, Game1.player.Tile) <= identity.Radius)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, Game1.player.Tile))
            .ThenBy(monster => monster.Health)
            .FirstOrDefault();
        if (target is null)
            return false;

        int damage = ScaleDamage(identity.BaseDamage, affinity, tier, member, role);
        DamageMonster(target, damage, identity.Knockback);
        int stun = ScaleStun(tier >= 3 ? identity.Tier3StunMs : identity.Tier2StunMs, member, role);
        if (stun > 0 && target.Health > 0)
            target.stunTime.Value = Math.Max(target.stunTime.Value, stun);

        ApplyConfiguredBuff(identity, member, activeMembers, tier);
        ShowUse(npc, identity.SignatureName, RoleColor(role), string.Empty, "swordswipe");
        SpawnBurst(target.Position, RoleColor(role), tier >= 3 ? 7 : 5, 28f);
        return true;
    }

    private bool TrySweep(
        CharacterSkillIdentity identity,
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int tier,
        int affinity,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyList<Monster> monsters)
    {
        List<Monster> targets = monsters
            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= identity.Radius)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile))
            .Take(tier >= 3 ? Math.Min(6, identity.MaxTargets + 1) : identity.MaxTargets)
            .ToList();
        if (targets.Count == 0)
            return false;

        int damage = ScaleDamage(identity.BaseDamage, affinity, tier, member, role);
        int stun = ScaleStun(tier >= 3 ? identity.Tier3StunMs : identity.Tier2StunMs, member, role);
        foreach (Monster monster in targets)
        {
            DamageMonster(monster, damage, identity.Knockback);
            if (stun > 0 && monster.Health > 0)
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, stun);
            SpawnBurst(monster.Position, RoleColor(role), 4, 24f);
        }

        ApplyConfiguredBuff(identity, member, activeMembers, tier);
        ShowUse(npc, identity.SignatureName, RoleColor(role), string.Empty, "swordswipe");
        return true;
    }

    private bool TryHybrid(
        CharacterSkillIdentity identity,
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int tier,
        int affinity,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyList<Monster> monsters)
    {
        bool injured = Game1.player.health < Game1.player.maxHealth * identity.TriggerHealthRatio
            || activeMembers.Any(other => _progression.GetHealthRatio(other) < identity.TriggerHealthRatio);
        bool pressured = monsters.Any(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= identity.Radius);
        if (!injured && !pressured)
            return false;

        int restored = 0;
        if (injured && identity.BaseHeal > 0)
        {
            int heal = ScaleHeal(identity.BaseHeal, affinity, tier, member, role);
            restored += HealFarmer(heal);
            foreach (PartyMemberData target in activeMembers
                .Where(other => _progression.GetHealthRatio(other) < identity.TriggerHealthRatio)
                .OrderBy(other => _progression.GetHealthRatio(other))
                .Take(Math.Max(1, identity.MaxTargets - 1)))
            {
                int before = target.CurrentHealth;
                target.CurrentHealth = Math.Min(_progression.GetMaxHealth(target), target.CurrentHealth + heal);
                restored += Math.Max(0, target.CurrentHealth - before);
            }
        }

        if (pressured && identity.BaseDamage > 0)
        {
            int damage = ScaleDamage(identity.BaseDamage, affinity, tier, member, role);
            foreach (Monster monster in monsters
                .Where(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= identity.Radius)
                .Take(identity.MaxTargets))
            {
                DamageMonster(monster, damage, identity.Knockback);
                SpawnBurst(monster.Position, RoleColor(role), 3, 20f);
            }
        }

        ApplyConfiguredBuff(identity, member, activeMembers, tier);
        ShowUse(npc, identity.SignatureName, RoleColor(role), restored > 0 ? $" +{restored}" : string.Empty, "yoba");
        return true;
    }

    private void ApplyConfiguredBuff(
        CharacterSkillIdentity identity,
        PartyMemberData owner,
        IReadOnlyList<PartyMemberData> activeMembers,
        int tier)
    {
        if (identity.BuffDurationTicks <= 0)
            return;
        if (identity.DamageBuffPercent <= 0f
            && identity.DefenseBuff <= 0
            && identity.HealingBuffPercent <= 0f
            && identity.ControlBuffPercent <= 0f
            && identity.CooldownBuffPercent <= 0)
            return;

        float signatureAffinity = _progression.GetSignatureEffectMultiplier(owner, ResolveRole(owner));
        int duration = tier >= 3
            ? (int)Math.Round(identity.BuffDurationTicks * 1.25f * signatureAffinity)
            : (int)Math.Round(identity.BuffDurationTicks * signatureAffinity);
        IEnumerable<PartyMemberData> targets = identity.PartyWideBuff ? activeMembers : new[] { owner };
        foreach (PartyMemberData target in targets)
        {
            _progression.ApplyTemporaryModifier(
                target,
                $"signature:{identity.CharacterName}",
                duration,
                identity.DamageBuffPercent,
                identity.DefenseBuff,
                identity.HealingBuffPercent,
                identity.ControlBuffPercent,
                identity.CooldownBuffPercent);
        }
    }

    private int ScaleDamage(int baseDamage, int affinity, int tier, PartyMemberData member, PartyRole role)
    {
        if (baseDamage <= 0)
            return 0;
        float tierMultiplier = tier >= 3 ? 1.20f : 1f;
        return Math.Max(1, (int)Math.Round((baseDamage + affinity) * tierMultiplier * _progression.GetDamageMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)));
    }

    private int ScaleHeal(int baseHeal, int affinity, int tier, PartyMemberData member, PartyRole role)
    {
        if (baseHeal <= 0)
            return 0;
        float tierMultiplier = tier >= 3 ? 1.18f : 1f;
        return Math.Max(1, (int)Math.Round((baseHeal + affinity) * tierMultiplier * _progression.GetHealingMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)));
    }

    private int ScaleStun(int baseStunMs, PartyMemberData member, PartyRole role)
    {
        if (baseStunMs <= 0)
            return 0;
        return Math.Clamp((int)Math.Round(baseStunMs * _progression.GetControlMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)), 0, 1800);
    }

    private static int HealFarmer(int amount)
    {
        if (amount <= 0)
            return 0;
        int before = Game1.player.health;
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + amount);
        return Math.Max(0, Game1.player.health - before);
    }

    private static void DamageMonster(Monster monster, int damage, float knockback)
    {
        if (damage <= 0 || Game1.currentLocation is null)
            return;

        Game1.currentLocation.damageMonster(
            monster.GetBoundingBox(),
            damage,
            damage + 2,
            isBomb: false,
            knockback,
            100,
            0.01f,
            1.2f,
            triggerMonsterInvincibleTimer: false,
            Game1.player);
    }

    private static void ShowUse(NPC npc, string name, Color color, string suffix, string sound)
    {
        npc.showTextAboveHead(name + suffix, color, 2, 1450, 0);
        if (Game1.currentLocation is not null)
            Game1.currentLocation.playSound(sound);
    }

    private void AwardSkillProgress(PartyMemberData member, PartyRole role, NPC npc, int xp, int masteryXp)
    {
        bool leveled = _progression.AwardExperience(member, xp);
        bool masteryUp = _progression.AwardMastery(member, role, masteryXp);
        if (leveled)
        {
            npc.showTextAboveHead($"LEVEL {member.Level}!", new Color(255, 220, 95), 2, 1500, 0);
            Game1.currentLocation?.playSound("reward");
        }
        else if (masteryUp)
        {
            npc.showTextAboveHead($"{RoleShort(role)} M{_progression.GetMasteryLevel(member, role)}", new Color(155, 215, 255), 2, 1100, 0);
        }
    }

    private int ScaleCooldown(PartyMemberData member, PartyRole role, int baseTicks)
        => Math.Max(180, (int)Math.Round(baseTicks * _progression.GetCooldownMultiplier(member, role)));

    private int GetSignatureTier(PartyMemberData member, PartyRole role)
    {
        int mastery = _progression.GetMasteryLevel(member, role);
        if (member.Level >= 20 || mastery >= 8)
            return 3;
        if (member.Level >= 10 || mastery >= 4)
            return 2;
        return 1;
    }

    private NPC? GetActiveNpc(PartyMemberData member)
    {
        NPC? npc = Game1.getCharacterFromName(member.CharacterName);
        return npc is not null && ReferenceEquals(npc.currentLocation, Game1.currentLocation) ? npc : null;
    }

    private static PartyRole ResolveRole(PartyMemberData member)
    {
        if (member.Role != PartyRole.Unassigned)
            return member.Role;
        return NpcProfileCatalog.Get(member.CharacterName)?.PrimaryRole ?? PartyRole.Damage;
    }

    private static string RoleShort(PartyRole role)
        => role == PartyRole.Damage ? "DPS" : role.ToString();

    private void TickCooldowns()
    {
        foreach (string key in _cooldowns.Keys.ToList())
        {
            int next = _cooldowns[key] - 1;
            if (next <= 0)
                _cooldowns.Remove(key);
            else
                _cooldowns[key] = next;
        }
    }

    private int GetCooldown(string characterName)
        => _cooldowns.TryGetValue(characterName, out int value) ? value : 0;

    private static Color RoleColor(PartyRole role)
    {
        return role switch
        {
            PartyRole.Tank => new Color(95, 165, 235),
            PartyRole.Damage => new Color(235, 105, 90),
            PartyRole.Support => new Color(235, 195, 85),
            PartyRole.Healer => new Color(110, 215, 135),
            PartyRole.Control => new Color(175, 120, 235),
            _ => new Color(220, 190, 150)
        };
    }

    private static void SpawnBurst(Vector2 worldPosition, Color color, int count, float spreadPixels)
    {
        if (Game1.currentLocation is null)
            return;

        int safeCount = Math.Clamp(count, 1, 16);
        for (int i = 0; i < safeCount; i++)
        {
            double angle = Math.PI * 2d * i / safeCount;
            Vector2 offset = new((float)Math.Cos(angle) * spreadPixels, (float)Math.Sin(angle) * spreadPixels * 0.65f);
            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(10, worldPosition + offset, color, 6, false, 45f + i * 3f));
        }
    }
}
