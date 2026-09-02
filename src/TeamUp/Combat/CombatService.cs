using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Team Up combat loop with persistent Level/Mastery progression and a non-permadeath
/// NPC survival model. NPCs can be injured, retreat, become Downed, revive, and finally
/// Withdraw after repeated knockouts without ever deleting or permanently harming a villager.
/// </summary>
public sealed class CombatService
{
    private const float HardLeashTiles = 12f;
    private const float RepathThresholdTiles = 0.9f;
    private const int AutoReviveTicks = 720;
    private const int ReviveGraceTicks = 600;

    private readonly IMonitor _monitor;
    private readonly FollowService _follow;
    private readonly ProgressionService _progression;
    private readonly Dictionary<string, int> _attackCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _healCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _signatureCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _incomingDamageCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _selfRecoveryCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Monster> _targets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Vector2> _lastTargetTiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _retreatNotified = new(StringComparer.OrdinalIgnoreCase);

    public CombatService(IMonitor monitor, FollowService follow, ProgressionService progression)
    {
        _monitor = monitor;
        _follow = follow;
        _progression = progression;
    }

    public void Clear()
    {
        foreach (string name in _targets.Keys.ToList())
        {
            NPC? npc = Game1.getCharacterFromName(name);
            if (npc is not null)
                _follow.SetCombatControl(npc, false);
        }

        _targets.Clear();
        _attackCooldowns.Clear();
        _healCooldowns.Clear();
        _signatureCooldowns.Clear();
        _incomingDamageCooldowns.Clear();
        _selfRecoveryCooldowns.Clear();
        _lastTargetTiles.Clear();
        _retreatNotified.Clear();
    }

    public void Update(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        TickCooldowns(_attackCooldowns);
        TickCooldowns(_healCooldowns);
        TickCooldowns(_signatureCooldowns);
        TickCooldowns(_incomingDamageCooldowns);
        TickCooldowns(_selfRecoveryCooldowns);

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .ToList();

        UpdateSurvivalStates(members, recruiterId, monsters);
        HashSet<string> stillEngaged = new(StringComparer.OrdinalIgnoreCase);

        foreach (PartyMemberData member in members.Where(member =>
                     member.RecruiterId == recruiterId
                     && member.State == PartyMemberState.Following))
        {
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is null || !ReferenceEquals(npc.currentLocation, Game1.currentLocation))
            {
                Disengage(member.CharacterName, npc);
                continue;
            }

            if (member.IsDowned)
            {
                _follow.SetCombatControl(npc, true);
                npc.controller = null;
                npc.temporaryController = null;
                npc.Halt();
                stillEngaged.Add(member.CharacterName);
                continue;
            }

            if (member.IsWithdrawn)
            {
                Disengage(member.CharacterName, npc);
                continue;
            }

            PartyRole role = ResolveRole(member);
            NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
            int affinity = Math.Max(2, profile?.GetAffinity(role) ?? 2);

            if (TryPerformRecovery(npc, member, role, affinity, members, recruiterId, monsters.Count > 0))
                stillEngaged.Add(member.CharacterName);

            if (ShouldRetreat(member))
            {
                Disengage(member.CharacterName, npc);
                if (_retreatNotified.Add(member.CharacterName))
                    npc.showTextAboveHead("RETREAT", new Color(255, 190, 95), 2, 900, 0);
                continue;
            }

            _retreatNotified.Remove(member.CharacterName);

            Monster? target = AcquireTarget(npc, member, role, monsters);
            if (target is null)
            {
                if (!stillEngaged.Contains(member.CharacterName))
                    Disengage(member.CharacterName, npc);
                continue;
            }

            stillEngaged.Add(member.CharacterName);
            _targets[member.CharacterName] = target;
            _follow.SetCombatControl(npc, true);

            float distanceToFarmer = Vector2.Distance(npc.Tile, Game1.player.Tile);
            if (distanceToFarmer > HardLeashTiles)
            {
                Disengage(member.CharacterName, npc);
                continue;
            }

            float attackRange = GetAttackRange(role);
            float distanceToTarget = Vector2.Distance(npc.Tile, target.Tile);

            if (distanceToTarget > attackRange)
            {
                MoveTowardTarget(npc, target, role);
                continue;
            }

            npc.controller = null;
            npc.temporaryController = null;
            npc.Halt();
            npc.faceDirection(GetFacingDirection(npc.Position, target.Position));

            if (GetCooldown(_attackCooldowns, member.CharacterName) > 0)
                continue;

            PerformAttack(npc, target, member, role, affinity);
            int cooldown = GetAttackCooldown(role, member.Engagement, affinity);
            cooldown = Math.Max(12, (int)Math.Round(cooldown * _progression.GetCooldownMultiplier(member, role)));
            _attackCooldowns[member.CharacterName] = cooldown;
        }

        foreach (string name in _targets.Keys.ToList())
        {
            if (stillEngaged.Contains(name))
                continue;

            NPC? npc = Game1.getCharacterFromName(name);
            Disengage(name, npc);
        }
    }

    private void UpdateSurvivalStates(
        IReadOnlyList<PartyMemberData> members,
        long recruiterId,
        IReadOnlyList<Monster> monsters)
    {
        foreach (PartyMemberData member in members.Where(member =>
                     member.RecruiterId == recruiterId
                     && member.State == PartyMemberState.Following))
        {
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is null || !ReferenceEquals(npc.currentLocation, Game1.currentLocation))
                continue;

            if (member.WoundedTicks > 0)
                member.WoundedTicks--;

            if (member.IsWithdrawn)
                continue;

            if (member.IsDowned)
            {
                member.DownedTicks++;
                if (member.DownedTicks >= AutoReviveTicks)
                    ReviveMember(member, npc, 0.25f, "RECOVERED");
                continue;
            }

            TryApplyIncomingMonsterDamage(member, npc, monsters);
            TryPassiveRecovery(member, npc, monsters);
        }
    }

    private void TryApplyIncomingMonsterDamage(
        PartyMemberData member,
        NPC npc,
        IReadOnlyList<Monster> monsters)
    {
        if (GetCooldown(_incomingDamageCooldowns, member.CharacterName) > 0)
            return;

        Monster? threat = monsters
            .Where(monster => monster.Health > 0)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile))
            .FirstOrDefault(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 1.35f);

        if (threat is null)
            return;

        int raw = Math.Max(2, threat.DamageToFarmer);
        int scaled = Math.Max(1, (int)Math.Round(raw * 0.45f));
        int damage = Math.Max(1, scaled - _progression.GetDefense(member));
        if (member.WoundedTicks > 0)
            damage = Math.Max(1, (int)Math.Ceiling(damage * 1.15f));

        member.CurrentHealth = Math.Max(0, member.CurrentHealth - damage);
        npc.showTextAboveHead($"-{damage} HP", new Color(245, 95, 95), 2, 700, 0);
        SpawnBurst(Game1.currentLocation, npc.Position + new Vector2(16f, 16f), new Color(235, 90, 90), 4, 18f);
        _incomingDamageCooldowns[member.CharacterName] = 50;

        if (member.CurrentHealth <= 0)
            DownMember(member, npc);
    }

    private void TryPassiveRecovery(
        PartyMemberData member,
        NPC npc,
        IReadOnlyList<Monster> monsters)
    {
        if (member.CurrentHealth >= _progression.GetMaxHealth(member)
            || GetCooldown(_selfRecoveryCooldowns, member.CharacterName) > 0)
        {
            return;
        }

        bool dangerNearby = monsters.Any(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 4f);
        if (dangerNearby)
            return;

        int amount = Math.Max(1, _progression.GetMaxHealth(member) / 100);
        member.CurrentHealth = Math.Min(_progression.GetMaxHealth(member), member.CurrentHealth + amount);
        _selfRecoveryCooldowns[member.CharacterName] = 180;
    }

    private void DownMember(PartyMemberData member, NPC npc)
    {
        member.DownCountToday++;
        member.DownedTicks = 0;
        member.CurrentHealth = 0;
        _targets.Remove(member.CharacterName);
        _lastTargetTiles.Remove(member.CharacterName);

        if (member.DownCountToday >= 3)
        {
            member.IsDowned = false;
            member.IsWithdrawn = true;
            member.CurrentHealth = 1;
            _follow.SetCombatControl(npc, false);
            npc.showTextAboveHead("WITHDRAWN", new Color(180, 180, 180), 2, 1600, 0);
            SpawnBurst(Game1.currentLocation, npc.Position, new Color(130, 130, 130), 7, 30f);
            return;
        }

        member.IsDowned = true;
        member.IsWithdrawn = false;
        _follow.SetCombatControl(npc, true);
        npc.controller = null;
        npc.temporaryController = null;
        npc.Halt();
        npc.showTextAboveHead("DOWNED", new Color(255, 95, 95), 2, 1500, 0);
        SpawnBurst(Game1.currentLocation, npc.Position, new Color(255, 95, 95), 8, 28f);
        Game1.currentLocation.playSound("cancel");
    }

    private void ReviveMember(PartyMemberData member, NPC npc, float healthFraction, string label)
    {
        if (!member.IsDowned)
            return;

        member.IsDowned = false;
        member.DownedTicks = 0;
        member.WoundedTicks = ReviveGraceTicks;
        member.CurrentHealth = Math.Max(1, (int)Math.Round(_progression.GetMaxHealth(member) * healthFraction));
        _follow.SetCombatControl(npc, false);
        npc.showTextAboveHead(label, new Color(130, 255, 175), 2, 1200, 0);
        SpawnBurst(Game1.currentLocation, npc.Position, new Color(130, 255, 175), 8, 30f);
        Game1.currentLocation.playSound("yoba");
    }

    private bool ShouldRetreat(PartyMemberData member)
    {
        return _progression.GetHealthRatio(member) <= _progression.GetRetreatThreshold(member);
    }

    private Monster? AcquireTarget(NPC npc, PartyMemberData member, PartyRole role, IReadOnlyList<Monster> monsters)
    {
        if (monsters.Count == 0)
            return null;

        float radius = GetEngagementRadius(member.Engagement);
        Vector2 farmerTile = Game1.player.Tile;

        IEnumerable<Monster> candidates = monsters.Where(monster =>
            ReferenceEquals(monster.currentLocation, Game1.currentLocation)
            && Vector2.Distance(monster.Tile, farmerTile) <= radius);

        if (member.Engagement == EngagementStyle.Passive)
            candidates = candidates.Where(monster => Vector2.Distance(monster.Tile, farmerTile) <= 2.75f);

        Monster? current = _targets.TryGetValue(member.CharacterName, out Monster? tracked)
            && tracked.Health > 0
            && candidates.Contains(tracked)
                ? tracked
                : null;

        if (current is not null)
            return current;

        return role == PartyRole.Tank
            ? candidates.OrderBy(monster => Vector2.DistanceSquared(monster.Tile, farmerTile)).FirstOrDefault()
            : candidates.OrderBy(monster => Vector2.DistanceSquared(monster.Tile, npc.Tile)).FirstOrDefault();
    }

    private bool TryPerformRecovery(
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int affinity,
        IReadOnlyList<PartyMemberData> members,
        long recruiterId,
        bool combatPresent)
    {
        if (!combatPresent || role is not (PartyRole.Healer or PartyRole.Support))
            return false;

        if (GetCooldown(_healCooldowns, member.CharacterName) > 0)
            return false;

        PartyMemberData? downed = members
            .Where(other => other.RecruiterId == recruiterId && other.IsDowned && !other.IsWithdrawn)
            .Where(other => !other.CharacterName.Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(other => other.DownedTicks)
            .FirstOrDefault(other =>
            {
                NPC? otherNpc = Game1.getCharacterFromName(other.CharacterName);
                return otherNpc is not null
                    && ReferenceEquals(otherNpc.currentLocation, Game1.currentLocation)
                    && Vector2.Distance(otherNpc.Tile, npc.Tile) <= 6f
                    && other.DownedTicks >= 120;
            });

        if (downed is not null)
        {
            NPC? downedNpc = Game1.getCharacterFromName(downed.CharacterName);
            if (downedNpc is not null)
            {
                ReviveMember(downed, downedNpc, role == PartyRole.Healer ? 0.35f : 0.30f, "REVIVED");
                AwardProgress(member, role, 8, 5, npc);
                _healCooldowns[member.CharacterName] = role == PartyRole.Healer ? 300 : 420;
                return true;
            }
        }

        PartyMemberData? injured = members
            .Where(other => other.RecruiterId == recruiterId && !other.IsDowned && !other.IsWithdrawn)
            .Where(other => other.CurrentHealth > 0 && other.CurrentHealth < _progression.GetMaxHealth(other))
            .OrderBy(other => _progression.GetHealthRatio(other))
            .FirstOrDefault(other =>
            {
                NPC? otherNpc = Game1.getCharacterFromName(other.CharacterName);
                return otherNpc is not null
                    && ReferenceEquals(otherNpc.currentLocation, Game1.currentLocation)
                    && Vector2.Distance(otherNpc.Tile, npc.Tile) <= 6f;
            });

        float farmerThreshold = role == PartyRole.Healer ? 0.78f : 0.52f;
        bool farmerNeedsHelp = Game1.player.health < (int)(Game1.player.maxHealth * farmerThreshold);
        bool allyMoreUrgent = injured is not null
            && _progression.GetHealthRatio(injured) < Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);

        int baseAmount = role == PartyRole.Healer ? 4 + affinity * 2 : 2 + affinity;
        int amount = Math.Max(1, (int)Math.Round(baseAmount * _progression.GetHealingMultiplier(member, role)));

        if (injured is not null && (allyMoreUrgent || !farmerNeedsHelp))
        {
            NPC? targetNpc = Game1.getCharacterFromName(injured.CharacterName);
            if (targetNpc is not null)
            {
                int before = injured.CurrentHealth;
                injured.CurrentHealth = Math.Min(_progression.GetMaxHealth(injured), injured.CurrentHealth + amount);
                int restored = injured.CurrentHealth - before;
                if (restored > 0)
                {
                    npc.faceDirection(GetFacingDirection(npc.Position, targetNpc.Position));
                    PlayHealFeedback(npc, targetNpc.Position, restored, role, targetNpc);
                    AwardProgress(member, role, 3, 2, npc);
                    _healCooldowns[member.CharacterName] = role == PartyRole.Healer ? 240 : 360;
                    return true;
                }
            }
        }

        if (!farmerNeedsHelp)
            return false;

        int farmerBefore = Game1.player.health;
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + amount);
        int farmerRestored = Game1.player.health - farmerBefore;
        if (farmerRestored <= 0)
            return false;

        npc.faceTowardFarmerForPeriod(500, 4, false, Game1.player);
        PlayHealFeedback(npc, Game1.player.Position, farmerRestored, role, null);
        TryTriggerRecoverySignature(npc, member, role, affinity, farmerBefore);
        AwardProgress(member, role, 3, 2, npc);
        _healCooldowns[member.CharacterName] = role == PartyRole.Healer ? 240 : 360;
        return true;
    }

    private void MoveTowardTarget(NPC npc, Monster target, PartyRole role)
    {
        Vector2 targetTile = FindApproachTile(Game1.currentLocation, npc.Tile, target.Tile, GetAttackRange(role));
        bool movedEnough = !_lastTargetTiles.TryGetValue(npc.Name, out Vector2 old)
            || Vector2.Distance(old, targetTile) >= RepathThresholdTiles;

        if (npc.controller is not null && !movedEnough)
            return;

        npc.controller = null;
        npc.temporaryController = null;

        try
        {
            npc.controller = new PathFindController(
                npc,
                Game1.currentLocation,
                targetTile.ToPoint(),
                GetFacingDirection(npc.Position, target.Position));
            _lastTargetTiles[npc.Name] = targetTile;
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Combat path failed for {npc.Name}: {ex.Message}", LogLevel.Trace);
        }
    }

    private void PerformAttack(NPC npc, Monster target, PartyMemberData member, PartyRole role, int affinity)
    {
        int damage = role switch
        {
            PartyRole.Damage => 8 + affinity * 2,
            PartyRole.Tank => 6 + affinity * 2,
            PartyRole.Control => 5 + affinity,
            PartyRole.Support => 4 + affinity,
            PartyRole.Healer => 3 + affinity,
            _ => 6 + affinity
        };

        float knockback = role switch
        {
            PartyRole.Tank => 1.6f,
            PartyRole.Control => 2.0f,
            PartyRole.Damage => 0.8f,
            PartyRole.Support => 0.7f,
            PartyRole.Healer => 0.5f,
            _ => 0.8f
        };

        damage = (int)Math.Round(damage * (0.85f + affinity * 0.05f));
        damage = Math.Max(1, (int)Math.Round(damage * _progression.GetDamageMultiplier(member, role)));

        int healthBefore = target.Health;
        Game1.currentLocation.damageMonster(
            target.GetBoundingBox(),
            damage,
            damage + 2,
            isBomb: false,
            knockback,
            100,
            0.02f,
            1.5f,
            triggerMonsterInvincibleTimer: false,
            Game1.player);

        int dealt = Math.Max(0, healthBefore - Math.Max(0, target.Health));
        ApplyRoleCombatEffect(npc, target, member, role, affinity, dealt);
        if (target.Health > 0)
            TryTriggerAttackSignature(npc, target, member, role, affinity);

        if (dealt > 0)
            AwardProgress(member, role, target.Health <= 0 ? 10 : 2, target.Health <= 0 ? 3 : 1, npc);
    }

    private void AwardProgress(PartyMemberData member, PartyRole role, int xp, int masteryXp, NPC npc)
    {
        bool leveled = _progression.AwardExperience(member, xp);
        bool masteryUp = _progression.AwardMastery(member, role, masteryXp);

        if (leveled)
        {
            npc.showTextAboveHead($"LEVEL {member.Level}!", new Color(255, 220, 95), 2, 1500, 0);
            SpawnBurst(Game1.currentLocation, npc.Position, new Color(255, 220, 95), 10, 34f);
            Game1.currentLocation.playSound("reward");
        }
        else if (masteryUp)
        {
            int mastery = _progression.GetMasteryLevel(member, role);
            npc.showTextAboveHead($"{RoleShort(role)} M{mastery}", new Color(155, 215, 255), 2, 1100, 0);
        }
    }

    private void PlayHealFeedback(NPC healer, Vector2 targetPosition, int restored, PartyRole role, NPC? targetNpc)
    {
        Color color = role == PartyRole.Healer
            ? new Color(120, 255, 160)
            : new Color(255, 224, 120);

        SpawnBurst(Game1.currentLocation, targetPosition + new Vector2(16f, -12f), color,
            role == PartyRole.Healer ? 6 : 4, 24f);

        if (targetNpc is not null)
            targetNpc.showTextAboveHead($"+{restored} HP", color, 2, 1000, 0);
        else
            healer.showTextAboveHead($"+{restored} HP", color, 2, 1000, 0);

        Game1.currentLocation.playSound("yoba");
    }

    private void ApplyRoleCombatEffect(NPC npc, Monster target, PartyMemberData member, PartyRole role, int affinity, int dealt)
    {
        Color color = role switch
        {
            PartyRole.Tank => new Color(255, 155, 70),
            PartyRole.Damage => new Color(225, 95, 120),
            PartyRole.Control => new Color(80, 225, 255),
            PartyRole.Support => new Color(255, 215, 90),
            PartyRole.Healer => new Color(120, 255, 160),
            _ => Color.White
        };

        SpawnBurst(Game1.currentLocation, target.Position + new Vector2(16f, 16f), color,
            role == PartyRole.Control ? 6 : 4, role == PartyRole.Tank ? 28f : 20f);

        if (dealt > 0)
            target.showTextAboveHead($"-{dealt}", color, 2, 700, 0);

        if (role == PartyRole.Control && target.Health > 0)
        {
            int stunMs = (int)Math.Round((350 + affinity * 100) * _progression.GetControlMultiplier(member, role));
            target.stunTime.Value = Math.Max(target.stunTime.Value, stunMs);
            target.showTextAboveHead("STUN", new Color(100, 235, 255), 2, 850, 0);
        }

        Game1.currentLocation.playSound(role == PartyRole.Control ? "thunder_small" : "swordswipe");
    }

    private void TryTriggerRecoverySignature(NPC npc, PartyMemberData member, PartyRole role, int affinity, int healthBeforeBaseHeal)
    {
        if (GetCooldown(_signatureCooldowns, member.CharacterName) > 0)
            return;

        if (member.CharacterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase)
            && role == PartyRole.Healer
            && healthBeforeBaseHeal <= (int)(Game1.player.maxHealth * 0.40f))
        {
            int before = Game1.player.health;
            int bonus = Math.Max(1, (int)Math.Round((6 + affinity * 2) * _progression.GetHealingMultiplier(member, role)));
            Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + bonus);
            int restored = Game1.player.health - before;
            if (restored > 0)
            {
                Color green = new(125, 255, 170);
                SpawnBurst(Game1.currentLocation, Game1.player.Position, Color.White, 8, 34f);
                SpawnBurst(Game1.currentLocation, Game1.player.Position, green, 8, 24f);
                npc.showTextAboveHead($"EMERGENCY +{restored}", green, 2, 1400, 0);
                Game1.currentLocation.playSound("yoba");
            }

            _signatureCooldowns[member.CharacterName] = Math.Max(300, (int)(600 * _progression.GetCooldownMultiplier(member, role)));
            return;
        }

        if (member.CharacterName.Equals("Emily", StringComparison.OrdinalIgnoreCase)
            && role is PartyRole.Support or PartyRole.Healer
            && healthBeforeBaseHeal <= (int)(Game1.player.maxHealth * 0.65f))
        {
            int before = Game1.player.health;
            int bonus = Math.Max(1, (int)Math.Round((2 + affinity) * _progression.GetHealingMultiplier(member, role)));
            Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + bonus);
            int restored = Game1.player.health - before;

            Color[] prism =
            {
                new Color(255, 110, 150), new Color(255, 190, 90), new Color(120, 255, 150),
                new Color(100, 220, 255), new Color(180, 120, 255)
            };
            for (int i = 0; i < prism.Length; i++)
                SpawnBurst(Game1.currentLocation, Game1.player.Position, prism[i], 2, 20f + i * 5f);

            npc.showTextAboveHead(restored > 0 ? $"PRISMATIC +{restored}" : "PRISMATIC AURA",
                new Color(230, 160, 255), 2, 1400, 0);
            Game1.currentLocation.playSound("yoba");
            _signatureCooldowns[member.CharacterName] = Math.Max(360, (int)(720 * _progression.GetCooldownMultiplier(member, role)));
        }
    }

    private void TryTriggerAttackSignature(NPC npc, Monster target, PartyMemberData member, PartyRole role, int affinity)
    {
        if (target.Health <= 0 || GetCooldown(_signatureCooldowns, member.CharacterName) > 0)
            return;

        if (member.CharacterName.Equals("Abigail", StringComparison.OrdinalIgnoreCase)
            && role is PartyRole.Damage or PartyRole.Control)
        {
            Color purple = new(195, 95, 255);
            int bonusDamage = Math.Max(1, (int)Math.Round((4 + affinity) * _progression.GetDamageMultiplier(member, role)));
            foreach (Monster monster in GetLivingMonstersNear(target.Tile, 2.25f))
            {
                Game1.currentLocation.damageMonster(monster.GetBoundingBox(), bonusDamage, bonusDamage + 2,
                    isBomb: false, 1.0f, 100, 0.02f, 1.5f, triggerMonsterInvincibleTimer: false, Game1.player);
                SpawnBurst(Game1.currentLocation, monster.Position, purple, 5, 28f);
            }

            npc.showTextAboveHead("SPIRIT SLASH", purple, 2, 1300, 0);
            Game1.currentLocation.playSound("swordswipe");
            _signatureCooldowns[member.CharacterName] = Math.Max(180, (int)(360 * _progression.GetCooldownMultiplier(member, role)));
            return;
        }

        if (member.CharacterName.Equals("Alex", StringComparison.OrdinalIgnoreCase)
            && role == PartyRole.Tank
            && Vector2.Distance(target.Tile, Game1.player.Tile) <= 4.0f)
        {
            Color orange = new(255, 155, 70);
            int guardDamage = Math.Max(1, (int)Math.Round((2 + affinity) * _progression.GetDamageMultiplier(member, role)));
            foreach (Monster monster in GetLivingMonstersNear(Game1.player.Tile, 2.75f))
            {
                Game1.currentLocation.damageMonster(monster.GetBoundingBox(), guardDamage, guardDamage + 1,
                    isBomb: false, 2.4f, 100, 0f, 1.25f, triggerMonsterInvincibleTimer: false, Game1.player);
                SpawnBurst(Game1.currentLocation, monster.Position, orange, 4, 30f);
            }

            SpawnBurst(Game1.currentLocation, Game1.player.Position, orange, 8, 42f);
            npc.showTextAboveHead("BODYGUARD", orange, 2, 1300, 0);
            Game1.currentLocation.playSound("clubSmash");
            _signatureCooldowns[member.CharacterName] = Math.Max(210, (int)(420 * _progression.GetCooldownMultiplier(member, role)));
            return;
        }

        if (member.CharacterName.Equals("Maru", StringComparison.OrdinalIgnoreCase) && role == PartyRole.Control)
        {
            Color cyan = new(80, 230, 255);
            int stunMs = (int)Math.Round((700 + affinity * 100) * _progression.GetControlMultiplier(member, role));
            foreach (Monster monster in GetLivingMonstersNear(target.Tile, 2.5f))
            {
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, stunMs);
                SpawnBurst(Game1.currentLocation, monster.Position, cyan, 7, 30f);
                monster.showTextAboveHead("SHOCK", cyan, 2, 1000, 0);
            }

            npc.showTextAboveHead("SHOCK DEVICE", cyan, 2, 1300, 0);
            Game1.currentLocation.playSound("thunder_small");
            _signatureCooldowns[member.CharacterName] = Math.Max(240, (int)(480 * _progression.GetCooldownMultiplier(member, role)));
        }
    }

    private static List<Monster> GetLivingMonstersNear(Vector2 centerTile, float radiusTiles)
    {
        return Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => Vector2.Distance(monster.Tile, centerTile) <= radiusTiles)
            .ToList();
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

    private void Disengage(string characterName, NPC? npc)
    {
        _targets.Remove(characterName);
        _lastTargetTiles.Remove(characterName);
        if (npc is not null)
            _follow.SetCombatControl(npc, false);
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

    private static float GetEngagementRadius(EngagementStyle style)
    {
        return style switch
        {
            EngagementStyle.Passive => 2.75f,
            EngagementStyle.Cautious => 4.5f,
            EngagementStyle.Balanced => 6.5f,
            EngagementStyle.Aggressive => 8.5f,
            EngagementStyle.Reckless => 10.5f,
            _ => 6.5f
        };
    }

    private static float GetAttackRange(PartyRole role)
    {
        return role switch
        {
            PartyRole.Healer => 4.25f,
            PartyRole.Support => 3.75f,
            PartyRole.Control => 3.5f,
            PartyRole.Damage => 1.8f,
            PartyRole.Tank => 1.65f,
            _ => 1.8f
        };
    }

    private static int GetAttackCooldown(PartyRole role, EngagementStyle style, int affinity)
    {
        int baseTicks = role switch
        {
            PartyRole.Damage => 32,
            PartyRole.Tank => 42,
            PartyRole.Control => 52,
            PartyRole.Support => 58,
            PartyRole.Healer => 64,
            _ => 42
        };
        baseTicks -= Math.Max(0, affinity - 2) * 2;
        return style switch
        {
            EngagementStyle.Cautious => baseTicks + 8,
            EngagementStyle.Aggressive => Math.Max(18, baseTicks - 5),
            EngagementStyle.Reckless => Math.Max(16, baseTicks - 9),
            _ => baseTicks
        };
    }

    private static Vector2 FindApproachTile(GameLocation location, Vector2 from, Vector2 target, float range)
    {
        int radius = Math.Max(1, (int)Math.Floor(range));
        Vector2 best = target;
        float bestDistance = float.MaxValue;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                Vector2 candidate = target + new Vector2(x, y);
                if (!location.isTileLocationTotallyClearAndPlaceable((int)candidate.X, (int)candidate.Y))
                    continue;
                float distance = Vector2.DistanceSquared(candidate, from);
                if (distance >= bestDistance)
                    continue;
                best = candidate;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static int GetFacingDirection(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        if (Math.Abs(delta.X) > Math.Abs(delta.Y))
            return delta.X >= 0f ? 1 : 3;
        return delta.Y >= 0f ? 2 : 0;
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
}
