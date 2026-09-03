using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 5 combat director. Adds per-monster threat tables, tank taunts/guard interception,
/// threat-aware NPC survival, role-based target scoring, anti-dogpile assignment penalties,
/// and balance passes while preserving alpha 3+4 progression/equipment/survival.
/// </summary>
public sealed class CombatService
{
    private const float HardLeashTiles = 12f;
    private const float RepathThresholdTiles = 1.35f;
    private const int AutoReviveTicks = 720;
    private const int ReviveGraceTicks = 600;
    private const int ThreatPulseInterval = 30;
    private const int TargetLockDurationTicks = 45;
    private const int FacingHoldDurationTicks = 10;

    private readonly IMonitor _monitor;
    private readonly FollowService _follow;
    private readonly ProgressionService _progression;
    private readonly ThreatService _threat = new();
    private readonly ExpansionSkillService _expansionSkills;
    private readonly Dictionary<string, int> _attackCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _healCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _signatureCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _tauntCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _incomingDamageCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _selfRecoveryCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Monster> _targets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Vector2> _lastTargetTiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _targetLockTicks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _facingHoldTicks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _lastFacingDirections = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _retreatNotified = new(StringComparer.OrdinalIgnoreCase);

    private int _threatPulseTicks;
    private int _lastFarmerHealth = -1;

    public CombatService(IMonitor monitor, FollowService follow, ProgressionService progression)
    {
        _monitor = monitor;
        _follow = follow;
        _progression = progression;
        _expansionSkills = new ExpansionSkillService(progression, _threat);
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
        _tauntCooldowns.Clear();
        _incomingDamageCooldowns.Clear();
        _selfRecoveryCooldowns.Clear();
        _lastTargetTiles.Clear();
        _targetLockTicks.Clear();
        _facingHoldTicks.Clear();
        _lastFacingDirections.Clear();
        _retreatNotified.Clear();
        _threat.Clear();
        _expansionSkills.Clear();
        _threatPulseTicks = 0;
        _lastFarmerHealth = -1;
    }

    public void Update(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        TickCooldowns(_attackCooldowns);
        TickCooldowns(_healCooldowns);
        TickCooldowns(_signatureCooldowns);
        TickCooldowns(_tauntCooldowns);
        TickCooldowns(_incomingDamageCooldowns);
        TickCooldowns(_selfRecoveryCooldowns);
        TickCooldowns(_targetLockTicks);
        TickCooldowns(_facingHoldTicks);

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();

        List<PartyMemberData> activeMembers = members
            .Where(member => member.RecruiterId == recruiterId && member.State == PartyMemberState.Following)
            .ToList();

        List<string> validThreatActors = activeMembers
            .Where(member => !member.IsDowned && !member.IsWithdrawn && member.CurrentHealth > 0)
            .Select(member => member.CharacterName)
            .ToList();

        _threat.BeginFrame(monsters, validThreatActors);
        PulseAmbientThreat(activeMembers, monsters);
        ApplyTankGuardToFarmerDamage(activeMembers, monsters, validThreatActors);
        UpdateSurvivalStates(activeMembers, monsters, validThreatActors);
        _expansionSkills.Update(activeMembers, monsters);

        var assignedCounts = new Dictionary<Monster, int>();
        HashSet<string> stillEngaged = new(StringComparer.OrdinalIgnoreCase);

        foreach (PartyMemberData member in activeMembers)
        {
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is null || !ReferenceEquals(npc.currentLocation, Game1.currentLocation))
            {
                Disengage(member.CharacterName, npc);
                continue;
            }

            if (member.IsDowned)
            {
                _threat.ScaleActor(member.CharacterName, 0.15f);
                _follow.SetCombatControl(npc, true);
                npc.controller = null;
                npc.temporaryController = null;
                npc.Halt();
                stillEngaged.Add(member.CharacterName);
                continue;
            }

            if (member.IsWithdrawn)
            {
                _threat.ScaleActor(member.CharacterName, 0f);
                Disengage(member.CharacterName, npc);
                continue;
            }

            PartyRole role = ResolveRole(member);
            NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
            int affinity = Math.Max(2, profile?.GetAffinity(role) ?? 2);

            if (TryPerformRecovery(npc, member, role, affinity, activeMembers, monsters, validThreatActors))
                stillEngaged.Add(member.CharacterName);

            if (ShouldRetreat(member))
            {
                _threat.ScaleActor(member.CharacterName, 0.35f);
                Disengage(member.CharacterName, npc);
                if (_retreatNotified.Add(member.CharacterName))
                    npc.showTextAboveHead("RETREAT", new Color(255, 190, 95), 2, 900, 0);
                continue;
            }

            _retreatNotified.Remove(member.CharacterName);

            Monster? target = AcquireTarget(npc, member, role, monsters, activeMembers, validThreatActors, assignedCounts);
            if (target is null)
            {
                if (!stillEngaged.Contains(member.CharacterName))
                    Disengage(member.CharacterName, npc);
                continue;
            }

            assignedCounts[target] = assignedCounts.TryGetValue(target, out int assigned) ? assigned + 1 : 1;
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

            if (role == PartyRole.Tank)
                TryTankTaunt(npc, member, monsters, validThreatActors);
            if (distanceToTarget > attackRange)
            {
                MoveTowardTarget(npc, target, role);
                continue;
            }

            npc.controller = null;
            npc.temporaryController = null;
            npc.Halt();
            bool attackReady = GetCooldown(_attackCooldowns, member.CharacterName) <= 0;
            FaceTargetStable(npc, target, attackReady);

            if (!attackReady)
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

        _lastFarmerHealth = Game1.player.health;
    }

    private void PulseAmbientThreat(IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)
    {
        _threatPulseTicks--;
        if (_threatPulseTicks > 0)
            return;
        _threatPulseTicks = ThreatPulseInterval;

        foreach (PartyMemberData member in members)
        {
            if (member.IsDowned || member.IsWithdrawn || ShouldRetreat(member))
                continue;

            PartyRole role = ResolveRole(member);
            if (role != PartyRole.Tank)
                continue;

            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is null || !ReferenceEquals(npc.currentLocation, Game1.currentLocation))
                continue;

            int mastery = _progression.GetMasteryLevel(member, PartyRole.Tank);
            float auraThreat = (5f + mastery) * GetEngagementThreatMultiplier(member.Engagement);
            foreach (Monster monster in monsters.Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 6.25f))
                _threat.AddThreat(monster, member.CharacterName, auraThreat);
        }
    }

    private void ApplyTankGuardToFarmerDamage(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<Monster> monsters,
        IReadOnlyCollection<string> validThreatActors)
    {
        if (_lastFarmerHealth < 0)
        {
            _lastFarmerHealth = Game1.player.health;
            return;
        }

        int lost = _lastFarmerHealth - Game1.player.health;
        if (lost <= 0 || monsters.Count == 0)
            return;

        List<Monster> nearbyThreats = monsters
            .Where(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 3.25f)
            .ToList();
        if (nearbyThreats.Count == 0)
            return;

        PartyMemberData? guard = members
            .Where(member => !member.IsDowned && !member.IsWithdrawn && member.CurrentHealth > 0)
            .Where(member => ResolveRole(member) == PartyRole.Tank && !ShouldRetreat(member))
            .Where(member =>
            {
                NPC? npc = Game1.getCharacterFromName(member.CharacterName);
                return npc is not null
                    && ReferenceEquals(npc.currentLocation, Game1.currentLocation)
                    && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 4f;
            })
            .OrderByDescending(member => nearbyThreats.Count(monster =>
                _threat.GetAggroActor(monster, validThreatActors).Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(member => _threat.GetTotalThreat(member.CharacterName, nearbyThreats))
            .FirstOrDefault();

        if (guard is null)
            return;

        bool ownsPressure = nearbyThreats.Any(monster =>
            _threat.GetAggroActor(monster, validThreatActors).Equals(guard.CharacterName, StringComparison.OrdinalIgnoreCase)
            || _threat.GetThreat(monster, guard.CharacterName) >= _threat.GetThreat(monster, ThreatService.FarmerActorId) * 0.85f);
        if (!ownsPressure)
            return;

        NPC? guardNpc = Game1.getCharacterFromName(guard.CharacterName);
        if (guardNpc is null)
            return;

        int mastery = _progression.GetMasteryLevel(guard, PartyRole.Tank);
        float guardRatio = Math.Min(0.55f, 0.35f + mastery * 0.02f);
        int absorbed = Math.Clamp((int)Math.Round(lost * guardRatio), 1, lost);
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + absorbed);

        int redirected = Math.Max(1, absorbed - _progression.GetDefense(guard) / 3);
        guard.CurrentHealth = Math.Max(0, guard.CurrentHealth - redirected);
        guardNpc.showTextAboveHead($"GUARD -{redirected}", new Color(255, 165, 80), 2, 900, 0);
        SpawnBurst(Game1.currentLocation, guardNpc.Position, new Color(255, 165, 80), 5, 24f);
        foreach (Monster monster in nearbyThreats)
            _threat.AddThreat(monster, guard.CharacterName, 18f + absorbed * 3f);

        if (guard.CurrentHealth <= 0)
            DownMember(guard, guardNpc);
    }

    private void UpdateSurvivalStates(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<Monster> monsters,
        IReadOnlyCollection<string> validThreatActors)
    {
        foreach (PartyMemberData member in members)
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

            TryPassiveRecovery(member, npc, monsters);
        }

        foreach (Monster monster in monsters)
            TryApplyThreatDamage(monster, members, validThreatActors);
    }

    private void TryApplyThreatDamage(
        Monster monster,
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyCollection<string> validThreatActors)
    {
        string actor = _threat.GetAggroActor(monster, validThreatActors);
        if (actor == ThreatService.FarmerActorId)
            return;

        PartyMemberData? member = members.FirstOrDefault(candidate =>
            candidate.CharacterName.Equals(actor, StringComparison.OrdinalIgnoreCase)
            && !candidate.IsDowned
            && !candidate.IsWithdrawn
            && candidate.CurrentHealth > 0);
        if (member is null || GetCooldown(_incomingDamageCooldowns, member.CharacterName) > 0)
            return;

        NPC? npc = Game1.getCharacterFromName(member.CharacterName);
        if (npc is null || !ReferenceEquals(npc.currentLocation, Game1.currentLocation))
            return;

        PartyRole role = ResolveRole(member);
        float directDistance = Vector2.Distance(monster.Tile, npc.Tile);
        bool tankIntercept = role == PartyRole.Tank
            && Vector2.Distance(monster.Tile, Game1.player.Tile) <= 1.9f
            && Vector2.Distance(npc.Tile, Game1.player.Tile) <= 3.5f;
        if (directDistance > 2.0f && !tankIntercept)
            return;

        float roleScale = role switch
        {
            PartyRole.Tank => 0.34f,
            PartyRole.Support => 0.39f,
            PartyRole.Healer => 0.39f,
            PartyRole.Control => 0.41f,
            PartyRole.Damage => 0.43f,
            _ => 0.42f
        };

        int raw = Math.Max(2, monster.DamageToFarmer);
        int scaled = Math.Max(1, (int)Math.Round(raw * roleScale));
        int damage = Math.Max(1, scaled - _progression.GetDefense(member));
        if (member.WoundedTicks > 0)
            damage = Math.Max(1, (int)Math.Ceiling(damage * 1.15f));

        member.CurrentHealth = Math.Max(0, member.CurrentHealth - damage);
        npc.showTextAboveHead($"-{damage} HP", new Color(245, 95, 95), 2, 700, 0);
        SpawnBurst(Game1.currentLocation, npc.Position + new Vector2(16f, 16f), new Color(235, 90, 90), 4, 18f);
        _incomingDamageCooldowns[member.CharacterName] = role == PartyRole.Tank ? 44 : 50;

        if (member.CurrentHealth <= 0)
            DownMember(member, npc);
    }

    private void TryPassiveRecovery(PartyMemberData member, NPC npc, IReadOnlyList<Monster> monsters)
    {
        if (member.CurrentHealth >= _progression.GetMaxHealth(member)
            || GetCooldown(_selfRecoveryCooldowns, member.CharacterName) > 0)
            return;

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
        _threat.ScaleActor(member.CharacterName, 0.05f);

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
        _threat.ScaleActor(member.CharacterName, 0.15f);
        _follow.SetCombatControl(npc, false);
        npc.showTextAboveHead(label, new Color(130, 255, 175), 2, 1200, 0);
        SpawnBurst(Game1.currentLocation, npc.Position, new Color(130, 255, 175), 8, 30f);
        Game1.currentLocation.playSound("yoba");
    }

    private bool ShouldRetreat(PartyMemberData member)
    {
        return _progression.GetHealthRatio(member) <= _progression.GetRetreatThreshold(member);
    }

    private void TryTankTaunt(
        NPC npc,
        PartyMemberData member,
        IReadOnlyList<Monster> monsters,
        IReadOnlyCollection<string> validThreatActors)
    {
        if (GetCooldown(_tauntCooldowns, member.CharacterName) > 0)
            return;

        List<Monster> candidates = monsters
            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 5f)
            .Where(monster => !_threat.GetAggroActor(monster, validThreatActors)
                .Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count == 0)
            return;

        int mastery = _progression.GetMasteryLevel(member, PartyRole.Tank);
        float amount = (34f + mastery * 5f) * GetEngagementThreatMultiplier(member.Engagement);
        foreach (Monster monster in candidates)
            _threat.AddThreat(monster, member.CharacterName, amount);

        npc.showTextAboveHead("TAUNT", new Color(255, 165, 80), 2, 900, 0);
        SpawnBurst(Game1.currentLocation, npc.Position, new Color(255, 165, 80), 6, 32f);
        _tauntCooldowns[member.CharacterName] = Math.Max(150,
            (int)Math.Round(270 * _progression.GetCooldownMultiplier(member, PartyRole.Tank)));
    }

    private Monster? AcquireTarget(
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        IReadOnlyList<Monster> monsters,
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyCollection<string> validThreatActors,
        IReadOnlyDictionary<Monster, int> assignedCounts)
    {
        if (monsters.Count == 0)
            return null;

        float radius = GetEngagementRadius(member.Engagement);
        Vector2 farmerTile = Game1.player.Tile;
        List<Monster> candidates = monsters
            .Where(monster => ReferenceEquals(monster.currentLocation, Game1.currentLocation))
            .Where(monster => Vector2.Distance(monster.Tile, farmerTile) <= radius
                || Vector2.Distance(monster.Tile, npc.Tile) <= Math.Min(radius, 5.5f))
            .ToList();

        if (member.Engagement == EngagementStyle.Passive)
            candidates = candidates.Where(monster => Vector2.Distance(monster.Tile, farmerTile) <= 2.75f).ToList();
        if (candidates.Count == 0)
            return null;

        Monster? current = _targets.TryGetValue(member.CharacterName, out Monster? tracked)
            && tracked.Health > 0
            && candidates.Contains(tracked)
                ? tracked
                : null;

        if (current is not null && GetCooldown(_targetLockTicks, member.CharacterName) > 0)
            return current;

        double Score(Monster monster)
        {
            float distanceNpc = Vector2.Distance(monster.Tile, npc.Tile);
            float distanceFarmer = Vector2.Distance(monster.Tile, farmerTile);
            int assigned = assignedCounts.TryGetValue(monster, out int count) ? count : 0;
            string aggroActor = _threat.GetAggroActor(monster, validThreatActors);
            PartyMemberData? aggroMember = members.FirstOrDefault(other =>
                other.CharacterName.Equals(aggroActor, StringComparison.OrdinalIgnoreCase));
            PartyRole aggroRole = aggroMember is null ? PartyRole.Unassigned : ResolveRole(aggroMember);
            bool farmerAggro = aggroActor == ThreatService.FarmerActorId;
            bool vulnerableAggro = aggroRole is PartyRole.Healer or PartyRole.Support;
            int cluster = candidates.Count(other => Vector2.Distance(other.Tile, monster.Tile) <= 2.5f);
            double sticky = ReferenceEquals(monster, current) ? -4d : 0d;

            return role switch
            {
                PartyRole.Tank => distanceFarmer * 3.4d + distanceNpc * 1.4d + assigned * 7d
                    - (farmerAggro ? 30d : 0d) - (vulnerableAggro ? 22d : 0d) + sticky,
                PartyRole.Damage => monster.Health * 0.075d + distanceNpc * 2.1d + assigned * 14d
                    - (aggroRole == PartyRole.Tank ? 5d : 0d) + sticky,
                PartyRole.Control => distanceFarmer * 1.7d + distanceNpc * 1.2d + assigned * 10d
                    - cluster * 7d + (monster.stunTime.Value > 0 ? 16d : 0d) + sticky,
                PartyRole.Support => distanceFarmer * 2.2d + distanceNpc * 1.4d + assigned * 11d
                    - (farmerAggro ? 18d : 0d) - (vulnerableAggro ? 14d : 0d) + sticky,
                PartyRole.Healer => distanceNpc * 2.5d + distanceFarmer * 1.5d + assigned * 15d
                    - (farmerAggro ? 7d : 0d) + sticky,
                _ => distanceNpc * 2d + assigned * 10d + sticky
            };
        }

        Monster? chosen = candidates.OrderBy(Score).FirstOrDefault();
        if (chosen is not null && !ReferenceEquals(chosen, current))
            _targetLockTicks[member.CharacterName] = TargetLockDurationTicks;
        return chosen;
    }

    private bool TryPerformRecovery(
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int affinity,
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<Monster> monsters,
        IReadOnlyCollection<string> validThreatActors)
    {
        if (monsters.Count == 0 || role is not (PartyRole.Healer or PartyRole.Support))
            return false;
        if (GetCooldown(_healCooldowns, member.CharacterName) > 0)
            return false;

        PartyMemberData? downed = members
            .Where(other => other.IsDowned && !other.IsWithdrawn)
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
                AddHealingThreat(member, role, 10, monsters);
                _healCooldowns[member.CharacterName] = role == PartyRole.Healer ? 285 : 400;
                return true;
            }
        }

        PartyMemberData? injured = members
            .Where(other => !other.IsDowned && !other.IsWithdrawn)
            .Where(other => other.CurrentHealth > 0 && other.CurrentHealth < _progression.GetMaxHealth(other))
            .Where(other =>
            {
                NPC? otherNpc = Game1.getCharacterFromName(other.CharacterName);
                return otherNpc is not null
                    && ReferenceEquals(otherNpc.currentLocation, Game1.currentLocation)
                    && Vector2.Distance(otherNpc.Tile, npc.Tile) <= 6f;
            })
            .OrderBy(other =>
            {
                float ratio = _progression.GetHealthRatio(other);
                int pressure = _threat.CountMonstersTargeting(other.CharacterName, monsters, validThreatActors);
                return ratio - pressure * 0.06f;
            })
            .FirstOrDefault();

        int farmerPressure = monsters.Count(monster =>
            _threat.GetAggroActor(monster, validThreatActors) == ThreatService.FarmerActorId
            && Vector2.Distance(monster.Tile, Game1.player.Tile) <= 6f);
        float farmerThreshold = role == PartyRole.Healer ? 0.82f : 0.60f;
        if (farmerPressure >= 2)
            farmerThreshold = Math.Min(0.90f, farmerThreshold + 0.10f);
        farmerThreshold = Math.Clamp(
            farmerThreshold + _progression.GetRecoveryThresholdAdjustment(member, role),
            0.35f,
            0.95f);

        bool farmerNeedsHelp = Game1.player.health < (int)(Game1.player.maxHealth * farmerThreshold);
        float farmerRatio = Game1.player.health / (float)Math.Max(1, Game1.player.maxHealth);
        bool allyMoreUrgent = injured is not null && _progression.GetHealthRatio(injured) < farmerRatio;

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
                    AddHealingThreat(member, role, restored, monsters);
                    _healCooldowns[member.CharacterName] = role == PartyRole.Healer ? 210 : 315;
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
        TryTriggerRecoverySignature(npc, member, role, affinity, farmerBefore, monsters);
        AwardProgress(member, role, 3, 2, npc);
        AddHealingThreat(member, role, farmerRestored, monsters);
        _healCooldowns[member.CharacterName] = role == PartyRole.Healer ? 210 : 315;
        return true;
    }

    private void AddHealingThreat(PartyMemberData member, PartyRole role, int restored, IReadOnlyList<Monster> monsters)
    {
        float roleFactor = role == PartyRole.Healer ? 0.75f : 0.55f;
        float amount = Math.Max(1f, restored * roleFactor * GetEngagementThreatMultiplier(member.Engagement));
        IEnumerable<Monster> nearby = monsters.Where(monster =>
            Vector2.Distance(monster.Tile, Game1.player.Tile) <= 8f
            || Vector2.Distance(monster.Tile, Game1.getCharacterFromName(member.CharacterName)?.Tile ?? Game1.player.Tile) <= 8f);
        _threat.AddThreat(nearby, member.CharacterName, amount);
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
            target.GetBoundingBox(), damage, damage + 2, isBomb: false, knockback,
            100, 0.02f, 1.5f, triggerMonsterInvincibleTimer: false, Game1.player);

        int dealt = Math.Max(0, healthBefore - Math.Max(0, target.Health));
        if (dealt > 0)
            _threat.AddThreat(target, member.CharacterName, GetAttackThreat(member, role, dealt));

        ApplyRoleCombatEffect(npc, target, member, role, affinity, dealt);
        if (target.Health > 0)
            TryTriggerAttackSignature(npc, target, member, role, affinity);

        if (dealt > 0)
            AwardProgress(member, role, target.Health <= 0 ? 8 : 2, target.Health <= 0 ? 3 : 1, npc);
    }

    private float GetAttackThreat(PartyMemberData member, PartyRole role, int dealt)
    {
        float roleFactor = role switch
        {
            PartyRole.Tank => 2.45f,
            PartyRole.Control => 1.30f,
            PartyRole.Damage => 1.00f,
            PartyRole.Support => 0.72f,
            PartyRole.Healer => 0.55f,
            _ => 1f
        };
        float flat = role == PartyRole.Tank ? 8f : role == PartyRole.Control ? 3f : 0f;
        return (dealt * roleFactor + flat) * GetEngagementThreatMultiplier(member.Engagement);
    }

    private static float GetEngagementThreatMultiplier(EngagementStyle style)
    {
        return style switch
        {
            EngagementStyle.Passive => 0.70f,
            EngagementStyle.Cautious => 0.85f,
            EngagementStyle.Aggressive => 1.12f,
            EngagementStyle.Reckless => 1.22f,
            _ => 1f
        };
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
        Color color = role == PartyRole.Healer ? new Color(120, 255, 160) : new Color(255, 224, 120);
        SpawnBurst(Game1.currentLocation, targetPosition + new Vector2(16f, -12f), color,
            role == PartyRole.Healer ? 6 : 4, 24f);
        if (targetNpc is not null)
            targetNpc.showTextAboveHead($"+{restored} HP", color, 2, 1250, 0);
        else
            healer.showTextAboveHead($"HEAL +{restored}", color, 2, 1400, 0);
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
            _threat.AddThreat(target, member.CharacterName, 10f * GetEngagementThreatMultiplier(member.Engagement));
            target.showTextAboveHead("STUN", new Color(100, 235, 255), 2, 850, 0);
        }
        Game1.currentLocation.playSound(role == PartyRole.Control ? "thunder_small" : "swordswipe");
    }

    private void TryTriggerRecoverySignature(
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int affinity,
        int healthBeforeBaseHeal,
        IReadOnlyList<Monster> monsters)
    {
        if (GetCooldown(_signatureCooldowns, member.CharacterName) > 0)
            return;

        if (member.CharacterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase)
            && role == PartyRole.Healer
            && healthBeforeBaseHeal <= (int)(Game1.player.maxHealth * 0.40f))
        {
            int before = Game1.player.health;
            int bonus = Math.Max(1, (int)Math.Round((6 + affinity * 2) * _progression.GetHealingMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)));
            Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + bonus);
            int restored = Game1.player.health - before;
            if (restored > 0)
            {
                Color green = new(125, 255, 170);
                SpawnBurst(Game1.currentLocation, Game1.player.Position, Color.White, 8, 34f);
                SpawnBurst(Game1.currentLocation, Game1.player.Position, green, 8, 24f);
                npc.showTextAboveHead($"EMERGENCY +{restored}", green, 2, 1400, 0);
                AddHealingThreat(member, role, restored, monsters);
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
            int bonus = Math.Max(1, (int)Math.Round((2 + affinity) * _progression.GetHealingMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)));
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
            if (restored > 0)
                AddHealingThreat(member, role, restored, monsters);
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
            int bonusDamage = Math.Max(1, (int)Math.Round((4 + affinity) * _progression.GetDamageMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)));
            foreach (Monster monster in GetLivingMonstersNear(target.Tile, 2.25f))
            {
                Game1.currentLocation.damageMonster(monster.GetBoundingBox(), bonusDamage, bonusDamage + 2,
                    isBomb: false, 1.0f, 100, 0.02f, 1.5f, triggerMonsterInvincibleTimer: false, Game1.player);
                _threat.AddThreat(monster, member.CharacterName, bonusDamage * 1.15f);
                SpawnBurst(Game1.currentLocation, monster.Position, purple, 5, 28f);
            }
            npc.showTextAboveHead("SPIRIT SLASH", purple, 2, 1300, 0);
            Game1.currentLocation.playSound("swordswipe");
            _signatureCooldowns[member.CharacterName] = Math.Max(180, (int)(360 * _progression.GetCooldownMultiplier(member, role)));
            return;
        }

        if (member.CharacterName.Equals("Alex", StringComparison.OrdinalIgnoreCase)
            && role == PartyRole.Tank
            && Vector2.Distance(target.Tile, Game1.player.Tile) <= 4f)
        {
            Color orange = new(255, 155, 70);
            int guardDamage = Math.Max(1, (int)Math.Round((2 + affinity) * _progression.GetDamageMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)));
            foreach (Monster monster in GetLivingMonstersNear(Game1.player.Tile, 2.75f))
            {
                Game1.currentLocation.damageMonster(monster.GetBoundingBox(), guardDamage, guardDamage + 1,
                    isBomb: false, 2.4f, 100, 0f, 1.25f, triggerMonsterInvincibleTimer: false, Game1.player);
                _threat.AddThreat(monster, member.CharacterName, 55f + guardDamage * 3f);
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
            int stunMs = (int)Math.Round((700 + affinity * 100) * _progression.GetControlMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role));
            foreach (Monster monster in GetLivingMonstersNear(target.Tile, 2.5f))
            {
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, stunMs);
                _threat.AddThreat(monster, member.CharacterName, 18f);
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
        _targetLockTicks.Remove(characterName);
        _facingHoldTicks.Remove(characterName);
        _lastFacingDirections.Remove(characterName);
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
            EngagementStyle.Cautious => 5.5f,
            EngagementStyle.Balanced => 7.0f,
            EngagementStyle.Aggressive => 9.0f,
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

    private void FaceTargetStable(NPC npc, Monster target, bool force)
    {
        int desired = GetFacingDirection(npc.Position, target.Position);
        if (!force
            && _lastFacingDirections.TryGetValue(npc.Name, out int last)
            && last != desired
            && GetCooldown(_facingHoldTicks, npc.Name) > 0)
            return;

        if (!_lastFacingDirections.TryGetValue(npc.Name, out int previous) || previous != desired || force)
        {
            npc.faceDirection(desired);
            _lastFacingDirections[npc.Name] = desired;
            _facingHoldTicks[npc.Name] = FacingHoldDurationTicks;
        }
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
