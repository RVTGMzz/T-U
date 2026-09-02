using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 6 additive combat layer. It upgrades the five locked signature prototypes from Alpha 5,
/// adds pre-faint Farmer rescue, and gives signature cooldowns simple situational awareness.
/// No save fields are required: signature tier is derived from Character Level + active Role Mastery.
/// </summary>
public sealed class Alpha6CombatPolishService
{
    private const int FarmerRescueCooldownTicks = 1800;
    private const int SanctuaryDurationTicks = 600;
    private const int SanctuaryPulseTicks = 120;

    private readonly IMonitor _monitor;
    private readonly ProgressionService _progression;
    private readonly Dictionary<string, int> _signatureCooldowns = new(StringComparer.OrdinalIgnoreCase);

    private int _farmerRescueCooldown;
    private int _sanctuaryTicks;
    private int _sanctuaryPulse;
    private int _sanctuaryHeal;
    private string? _sanctuaryOwner;

    public Alpha6CombatPolishService(IMonitor monitor, ProgressionService progression)
    {
        _monitor = monitor;
        _progression = progression;
    }

    public void Clear()
    {
        _signatureCooldowns.Clear();
        _farmerRescueCooldown = 0;
        _sanctuaryTicks = 0;
        _sanctuaryPulse = 0;
        _sanctuaryHeal = 0;
        _sanctuaryOwner = null;
    }

    public void Update(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        TickCooldowns(_signatureCooldowns);
        if (_farmerRescueCooldown > 0)
            _farmerRescueCooldown--;

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .ToList();

        List<PartyMemberData> activeMembers = members
            .Where(member => member.RecruiterId == recruiterId)
            .Where(member => member.State == PartyMemberState.Following)
            .ToList();

        UpdateSanctuary(activeMembers);
        AccelerateSituationalCooldowns(activeMembers, monsters);
        TryFarmerRescue(activeMembers, monsters);

        if (monsters.Count == 0)
            return;

        foreach (PartyMemberData member in activeMembers)
        {
            if (member.IsDowned || member.IsWithdrawn || member.CurrentHealth <= 0)
                continue;

            NPC? npc = GetActiveNpc(member);
            if (npc is null)
                continue;

            PartyRole role = ResolveRole(member);
            int tier = GetSignatureTier(member, role);
            if (tier < 2 || GetCooldown(_signatureCooldowns, member.CharacterName) > 0)
                continue;

            NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
            int affinity = Math.Max(2, profile?.GetAffinity(role) ?? 2);

            bool used = member.CharacterName switch
            {
                "Abigail" => TryAbigailUpgrade(npc, member, role, tier, affinity, monsters),
                "Alex" => TryAlexUpgrade(npc, member, role, tier, affinity, monsters),
                "Harvey" => TryHarveyUpgrade(npc, member, role, tier, affinity, activeMembers),
                "Maru" => TryMaruUpgrade(npc, member, role, tier, affinity, monsters),
                "Emily" => TryEmilyUpgrade(npc, member, role, tier, affinity, activeMembers),
                _ => false
            };

            if (used)
                AwardSkillProgress(member, role, npc, tier == 3 ? 5 : 3, tier == 3 ? 3 : 2);
        }
    }

    private void TryFarmerRescue(IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)
    {
        if (_farmerRescueCooldown > 0 || monsters.Count == 0 || Game1.player.maxHealth <= 0)
            return;

        int rescueThreshold = Math.Max(8, (int)Math.Ceiling(Game1.player.maxHealth * 0.15f));
        if (Game1.player.health > rescueThreshold)
            return;

        PartyMemberData? rescuer = members
            .Where(IsAvailableRescuer)
            .Where(member =>
            {
                NPC? npc = GetActiveNpc(member);
                return npc is not null && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 7f;
            })
            .OrderByDescending(GetRescuePriority)
            .ThenByDescending(member => _progression.GetHealthRatio(member))
            .FirstOrDefault();

        if (rescuer is null)
            return;

        NPC? rescuerNpc = GetActiveNpc(rescuer);
        if (rescuerNpc is null)
            return;

        PartyRole role = ResolveRole(rescuer);
        int tier = GetSignatureTier(rescuer, role);
        float targetFraction;
        string label;
        Color color;

        if (rescuer.CharacterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase) && role == PartyRole.Healer)
        {
            targetFraction = tier >= 3 ? 0.40f : tier >= 2 ? 0.33f : 0.27f;
            label = tier >= 3 ? "FIELD RESCUE" : "EMERGENCY RESCUE";
            color = new Color(125, 255, 170);
        }
        else if (role == PartyRole.Healer)
        {
            targetFraction = tier >= 2 ? 0.27f : 0.23f;
            label = "EMERGENCY HEAL";
            color = new Color(125, 255, 170);
        }
        else if (role == PartyRole.Support)
        {
            targetFraction = tier >= 2 ? 0.23f : 0.19f;
            label = "RESCUE";
            color = new Color(255, 224, 120);
        }
        else
        {
            targetFraction = tier >= 3 ? 0.18f : 0.14f;
            label = tier >= 3 ? "IRON WALL" : "LAST GUARD";
            color = new Color(255, 165, 80);
        }

        int targetHealth = Math.Max(1, (int)Math.Round(Game1.player.maxHealth * targetFraction));
        int before = Game1.player.health;
        Game1.player.health = Math.Max(Game1.player.health, targetHealth);
        int restored = Math.Max(0, Game1.player.health - before);
        if (restored <= 0)
            return;

        if (role == PartyRole.Tank)
        {
            int guardCost = Math.Max(1, (int)Math.Round(_progression.GetMaxHealth(rescuer) * 0.08f));
            rescuer.CurrentHealth = Math.Max(1, rescuer.CurrentHealth - guardCost);
        }

        rescuerNpc.faceTowardFarmerForPeriod(650, 4, false, Game1.player);
        rescuerNpc.showTextAboveHead($"{label} +{restored}", color, 2, 1500, 0);
        SpawnBurst(Game1.currentLocation, Game1.player.Position, color, 10, 38f);
        Game1.currentLocation.playSound(role == PartyRole.Tank ? "clubSmash" : "yoba");
        _farmerRescueCooldown = FarmerRescueCooldownTicks;
        AwardSkillProgress(rescuer, role, rescuerNpc, 8, 5);
    }

    private bool TryAbigailUpgrade(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, IReadOnlyList<Monster> monsters)
    {
        if (role is not (PartyRole.Damage or PartyRole.Control))
            return false;

        List<Monster> inRange = monsters.Where(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 8f).ToList();
        if (inRange.Count == 0)
            return false;

        Monster? anchor = inRange
            .OrderByDescending(monster => inRange.Count(other => Vector2.Distance(other.Tile, monster.Tile) <= 2.8f))
            .ThenBy(monster => monster.Health)
            .FirstOrDefault();
        if (anchor is null)
            return false;

        int cluster = inRange.Count(other => Vector2.Distance(other.Tile, anchor.Tile) <= (tier >= 3 ? 3.25f : 2.8f));
        if (cluster < 2 && (tier < 3 || anchor.Health < 70))
            return false;

        float radius = tier >= 3 ? 3.25f : 2.8f;
        List<Monster> targets = inRange
            .Where(monster => Vector2.Distance(monster.Tile, anchor.Tile) <= radius)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, anchor.Tile))
            .Take(tier >= 3 ? 5 : 3)
            .ToList();
        if (targets.Count == 0)
            return false;

        int baseDamage = tier >= 3 ? 8 + affinity * 2 : 5 + affinity;
        int damage = Math.Max(1, (int)Math.Round(baseDamage * _progression.GetDamageMultiplier(member, role)));
        int dealtTotal = 0;
        Color purple = new(195, 95, 255);
        foreach (Monster monster in targets)
        {
            int before = monster.Health;
            Game1.currentLocation.damageMonster(monster.GetBoundingBox(), damage, damage + 2,
                isBomb: false, tier >= 3 ? 1.4f : 1.0f, 100, 0.02f, 1.5f,
                triggerMonsterInvincibleTimer: false, Game1.player);
            dealtTotal += Math.Max(0, before - Math.Max(0, monster.Health));
            if (tier >= 3 && monster.Health > 0)
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, 300);
            SpawnBurst(Game1.currentLocation, monster.Position, purple, tier >= 3 ? 6 : 4, 28f);
        }

        if (dealtTotal <= 0)
            return false;

        npc.showTextAboveHead(tier >= 3 ? "HAUNTED BLADE" : "SPIRIT WAVE", purple, 2, 1450, 0);
        Game1.currentLocation.playSound("swordswipe");
        _signatureCooldowns[member.CharacterName] = ScaleCooldown(member, role, tier >= 3 ? 480 : 600);
        return true;
    }

    private bool TryAlexUpgrade(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, IReadOnlyList<Monster> monsters)
    {
        if (role != PartyRole.Tank)
            return false;

        List<Monster> nearby = monsters.Where(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 3.75f).Take(6).ToList();
        float farmerRatio = Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);
        if (nearby.Count < 2 && farmerRatio > 0.45f)
            return false;
        if (nearby.Count == 0)
            return false;

        int damage = Math.Max(1, (int)Math.Round((2 + affinity) * _progression.GetDamageMultiplier(member, role)));
        Color orange = new(255, 165, 80);
        foreach (Monster monster in nearby)
        {
            Game1.currentLocation.damageMonster(monster.GetBoundingBox(), damage, damage + 1,
                isBomb: false, tier >= 3 ? 3.0f : 2.5f, 100, 0f, 1.25f,
                triggerMonsterInvincibleTimer: false, Game1.player);
            SpawnBurst(Game1.currentLocation, monster.Position, orange, 4, 28f);
        }

        if (tier >= 3)
        {
            int selfHeal = Math.Max(2, _progression.GetMaxHealth(member) / 12);
            member.CurrentHealth = Math.Min(_progression.GetMaxHealth(member), member.CurrentHealth + selfHeal);
        }

        npc.showTextAboveHead(tier >= 3 ? "IRON WALL" : "CHALLENGE", orange, 2, 1450, 0);
        SpawnBurst(Game1.currentLocation, Game1.player.Position, orange, tier >= 3 ? 10 : 7, 42f);
        Game1.currentLocation.playSound("clubSmash");
        _signatureCooldowns[member.CharacterName] = ScaleCooldown(member, role, tier >= 3 ? 600 : 720);
        return true;
    }

    private bool TryHarveyUpgrade(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, IReadOnlyList<PartyMemberData> members)
    {
        if (role != PartyRole.Healer)
            return false;

        List<PartyMemberData> injured = members
            .Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
            .Where(other => GetActiveNpc(other) is not null)
            .Where(other => _progression.GetHealthRatio(other) < (tier >= 3 ? 0.72f : 0.56f))
            .OrderBy(other => _progression.GetHealthRatio(other))
            .ToList();
        float farmerRatio = Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);
        if (injured.Count == 0 && farmerRatio >= (tier >= 3 ? 0.70f : 0.52f))
            return false;

        int amount = Math.Max(2, (int)Math.Round((6 + affinity * 2) * _progression.GetHealingMultiplier(member, role)));
        int restoredTotal = 0;
        int farmerBefore = Game1.player.health;
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + amount);
        restoredTotal += Math.Max(0, Game1.player.health - farmerBefore);

        foreach (PartyMemberData target in injured.Take(tier >= 3 ? 4 : 1))
        {
            NPC? targetNpc = GetActiveNpc(target);
            if (targetNpc is null || Vector2.Distance(targetNpc.Tile, npc.Tile) > 7f)
                continue;

            int before = target.CurrentHealth;
            int allyAmount = tier >= 3 ? Math.Max(2, (int)Math.Round(amount * 0.70f)) : amount;
            target.CurrentHealth = Math.Min(_progression.GetMaxHealth(target), target.CurrentHealth + allyAmount);
            restoredTotal += Math.Max(0, target.CurrentHealth - before);
            SpawnBurst(Game1.currentLocation, targetNpc.Position, new Color(125, 255, 170), 5, 22f);
        }

        if (restoredTotal <= 0)
            return false;

        string label = tier >= 3 ? "FIELD HOSPITAL" : "TRIAGE";
        npc.showTextAboveHead($"{label} +{restoredTotal}", new Color(125, 255, 170), 2, 1500, 0);
        SpawnBurst(Game1.currentLocation, Game1.player.Position, new Color(125, 255, 170), 9, 36f);
        Game1.currentLocation.playSound("yoba");
        _signatureCooldowns[member.CharacterName] = ScaleCooldown(member, role, tier >= 3 ? 720 : 900);
        return true;
    }

    private bool TryMaruUpgrade(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, IReadOnlyList<Monster> monsters)
    {
        if (role != PartyRole.Control)
            return false;

        List<Monster> candidates = monsters
            .Where(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 8f)
            .Where(monster => monster.stunTime.Value < 600)
            .ToList();
        if (candidates.Count < 2)
            return false;

        Monster? anchor = candidates.OrderByDescending(monster => candidates.Count(other => Vector2.Distance(other.Tile, monster.Tile) <= 3f)).FirstOrDefault();
        if (anchor is null)
            return false;

        List<Monster> targets = candidates
            .Where(monster => Vector2.Distance(monster.Tile, anchor.Tile) <= (tier >= 3 ? 3.4f : 3f))
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, anchor.Tile))
            .Take(tier >= 3 ? 5 : 3)
            .ToList();
        if (targets.Count < 2)
            return false;

        int stunMs = (int)Math.Round((tier >= 3 ? 1300 : 900) * _progression.GetControlMultiplier(member, role));
        int overloadDamage = tier >= 3 ? Math.Max(1, (int)Math.Round((3 + affinity) * _progression.GetDamageMultiplier(member, role))) : 0;
        Color cyan = new(80, 230, 255);
        foreach (Monster monster in targets)
        {
            monster.stunTime.Value = Math.Max(monster.stunTime.Value, stunMs);
            if (overloadDamage > 0)
            {
                Game1.currentLocation.damageMonster(monster.GetBoundingBox(), overloadDamage, overloadDamage + 1,
                    isBomb: false, 0.6f, 100, 0f, 1.2f,
                    triggerMonsterInvincibleTimer: false, Game1.player);
            }
            SpawnBurst(Game1.currentLocation, monster.Position, cyan, tier >= 3 ? 8 : 6, 30f);
        }

        npc.showTextAboveHead(tier >= 3 ? "OVERLOAD" : "CHAIN SHOCK", cyan, 2, 1450, 0);
        Game1.currentLocation.playSound("thunder_small");
        _signatureCooldowns[member.CharacterName] = ScaleCooldown(member, role, tier >= 3 ? 600 : 720);
        return true;
    }

    private bool TryEmilyUpgrade(NPC npc, PartyMemberData member, PartyRole role, int tier, int affinity, IReadOnlyList<PartyMemberData> members)
    {
        if (role is not (PartyRole.Support or PartyRole.Healer))
            return false;

        List<PartyMemberData> injured = members
            .Where(other => !other.IsDowned && !other.IsWithdrawn && other.CurrentHealth > 0)
            .Where(other => GetActiveNpc(other) is not null)
            .Where(other => _progression.GetHealthRatio(other) < 0.78f)
            .ToList();
        float farmerRatio = Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);
        int urgent = injured.Count + (farmerRatio < 0.75f ? 1 : 0);
        if (urgent < 2 && !(tier >= 3 && urgent >= 1 && farmerRatio < 0.58f))
            return false;

        int amount = Math.Max(2, (int)Math.Round((3 + affinity) * _progression.GetHealingMultiplier(member, role)));
        int restoredTotal = 0;
        int beforeFarmer = Game1.player.health;
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + amount);
        restoredTotal += Math.Max(0, Game1.player.health - beforeFarmer);

        foreach (PartyMemberData target in injured)
        {
            NPC? targetNpc = GetActiveNpc(target);
            if (targetNpc is null || Vector2.Distance(targetNpc.Tile, npc.Tile) > 6.5f)
                continue;

            int before = target.CurrentHealth;
            target.CurrentHealth = Math.Min(_progression.GetMaxHealth(target), target.CurrentHealth + amount);
            restoredTotal += Math.Max(0, target.CurrentHealth - before);
            SpawnBurst(Game1.currentLocation, targetNpc.Position, new Color(230, 160, 255), 4, 22f);
        }

        if (tier >= 3)
        {
            _sanctuaryOwner = member.CharacterName;
            _sanctuaryTicks = SanctuaryDurationTicks;
            _sanctuaryPulse = 1;
            _sanctuaryHeal = Math.Max(1, amount / 3);
        }

        if (restoredTotal <= 0 && tier < 3)
            return false;

        npc.showTextAboveHead(tier >= 3 ? "PRISMATIC SANCTUARY" : "RESONANCE", new Color(230, 160, 255), 2, 1500, 0);
        Color[] prism =
        {
            new Color(255, 110, 150), new Color(255, 190, 90), new Color(120, 255, 150),
            new Color(100, 220, 255), new Color(180, 120, 255)
        };
        foreach (Color color in prism)
            SpawnBurst(Game1.currentLocation, Game1.player.Position, color, 2, 24f);
        Game1.currentLocation.playSound("yoba");
        _signatureCooldowns[member.CharacterName] = ScaleCooldown(member, role, tier >= 3 ? 720 : 900);
        return true;
    }

    private void UpdateSanctuary(IReadOnlyList<PartyMemberData> members)
    {
        if (_sanctuaryTicks <= 0)
            return;

        _sanctuaryTicks--;
        _sanctuaryPulse--;
        if (_sanctuaryPulse > 0)
            return;
        _sanctuaryPulse = SanctuaryPulseTicks;

        PartyMemberData? owner = members.FirstOrDefault(member =>
            member.CharacterName.Equals(_sanctuaryOwner, StringComparison.OrdinalIgnoreCase)
            && !member.IsDowned && !member.IsWithdrawn);
        NPC? ownerNpc = owner is null ? null : GetActiveNpc(owner);
        if (ownerNpc is null)
        {
            _sanctuaryTicks = 0;
            return;
        }

        int beforeFarmer = Game1.player.health;
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + _sanctuaryHeal);
        int restored = Math.Max(0, Game1.player.health - beforeFarmer);
        foreach (PartyMemberData target in members.Where(target => !target.IsDowned && !target.IsWithdrawn && target.CurrentHealth > 0))
        {
            NPC? targetNpc = GetActiveNpc(target);
            if (targetNpc is null || Vector2.Distance(targetNpc.Tile, ownerNpc.Tile) > 7f)
                continue;

            int before = target.CurrentHealth;
            target.CurrentHealth = Math.Min(_progression.GetMaxHealth(target), target.CurrentHealth + _sanctuaryHeal);
            restored += Math.Max(0, target.CurrentHealth - before);
        }

        if (restored > 0)
            SpawnBurst(Game1.currentLocation, Game1.player.Position, new Color(230, 160, 255), 5, 28f);
    }

    private void AccelerateSituationalCooldowns(IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)
    {
        foreach (PartyMemberData member in members)
        {
            if (GetCooldown(_signatureCooldowns, member.CharacterName) <= 0 || member.IsDowned || member.IsWithdrawn)
                continue;

            NPC? npc = GetActiveNpc(member);
            if (npc is null)
                continue;

            int reduction = 0;
            if (member.CharacterName.Equals("Abigail", StringComparison.OrdinalIgnoreCase)
                && monsters.Count(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 3f) >= 2)
                reduction = 1;
            else if (member.CharacterName.Equals("Alex", StringComparison.OrdinalIgnoreCase)
                && monsters.Count(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 3.5f) >= 2)
                reduction = 1;
            else if (member.CharacterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase)
                && Game1.player.health < Game1.player.maxHealth / 2)
                reduction = 2;
            else if (member.CharacterName.Equals("Maru", StringComparison.OrdinalIgnoreCase)
                && monsters.Count(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 5f) >= 2)
                reduction = 1;
            else if (member.CharacterName.Equals("Emily", StringComparison.OrdinalIgnoreCase)
                && members.Any(other => !other.IsDowned && _progression.GetHealthRatio(other) < 0.60f))
                reduction = 1;

            if (reduction > 0)
                _signatureCooldowns[member.CharacterName] = Math.Max(0, _signatureCooldowns[member.CharacterName] - reduction);
        }
    }

    private bool IsAvailableRescuer(PartyMemberData member)
    {
        if (member.IsDowned || member.IsWithdrawn || member.CurrentHealth <= 0)
            return false;

        PartyRole role = ResolveRole(member);
        if (role is PartyRole.Healer or PartyRole.Support)
            return true;

        return member.CharacterName.Equals("Alex", StringComparison.OrdinalIgnoreCase)
            && role == PartyRole.Tank
            && GetSignatureTier(member, role) >= 2;
    }

    private int GetRescuePriority(PartyMemberData member)
    {
        PartyRole role = ResolveRole(member);
        if (member.CharacterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase) && role == PartyRole.Healer)
            return 100;
        return role switch
        {
            PartyRole.Healer => 80,
            PartyRole.Support => 60,
            PartyRole.Tank => 30,
            _ => 0
        };
    }

    private int GetSignatureTier(PartyMemberData member, PartyRole role)
    {
        int mastery = _progression.GetMasteryLevel(member, role);
        if (member.Level >= 20 || mastery >= 8)
            return 3;
        if (member.Level >= 10 || mastery >= 4)
            return 2;
        return 1;
    }

    private void AwardSkillProgress(PartyMemberData member, PartyRole role, NPC npc, int xp, int masteryXp)
    {
        bool leveled = _progression.AwardExperience(member, xp);
        bool masteryUp = _progression.AwardMastery(member, role, masteryXp);
        if (leveled)
        {
            npc.showTextAboveHead($"LEVEL {member.Level}!", new Color(255, 220, 95), 2, 1500, 0);
            Game1.currentLocation.playSound("reward");
        }
        else if (masteryUp)
        {
            npc.showTextAboveHead($"{RoleShort(role)} M{_progression.GetMasteryLevel(member, role)}", new Color(155, 215, 255), 2, 1100, 0);
        }
    }

    private int ScaleCooldown(PartyMemberData member, PartyRole role, int baseTicks)
    {
        return Math.Max(180, (int)Math.Round(baseTicks * _progression.GetCooldownMultiplier(member, role)));
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
    {
        return role == PartyRole.Damage ? "DPS" : role.ToString();
    }

    private static void TickCooldowns(Dictionary<string, int> cooldowns)
    {
        foreach (string key in cooldowns.Keys.ToList())
        {
            int next = cooldowns[key] - 1;
            if (next <= 0)
                cooldowns.Remove(key);
            else
                cooldowns[key] = next;
        }
    }

    private static int GetCooldown(Dictionary<string, int> cooldowns, string key)
    {
        return cooldowns.TryGetValue(key, out int value) ? value : 0;
    }

    private static void SpawnBurst(GameLocation location, Vector2 worldPosition, Color color, int count, float spreadPixels)
    {
        int safeCount = Math.Clamp(count, 1, 16);
        for (int i = 0; i < safeCount; i++)
        {
            double angle = Math.PI * 2d * i / safeCount;
            Vector2 offset = new((float)Math.Cos(angle) * spreadPixels, (float)Math.Sin(angle) * spreadPixels * 0.65f);
            location.temporarySprites.Add(new TemporaryAnimatedSprite(10, worldPosition + offset, color, 6, false, 45f + i * 3f));
        }
    }
}
