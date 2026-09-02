using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Team Up combat loop. Active Party Members acquire monsters around the Farmer,
/// temporarily take movement control from formation, attack, recover allies, and
/// return to normal following when combat ends.
///
/// Alpha 2 adds readable combat feedback: role-colored hit bursts, heal pulses,
/// floating combat text, real Control stuns, and the first signature ability VFX.
/// </summary>
public sealed class CombatService
{
    private const float HardLeashTiles = 12f;
    private const float RepathThresholdTiles = 0.9f;

    private readonly IMonitor _monitor;
    private readonly FollowService _follow;
    private readonly Dictionary<string, int> _attackCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _healCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _signatureCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Monster> _targets = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Vector2> _lastTargetTiles = new(StringComparer.OrdinalIgnoreCase);

    public CombatService(IMonitor monitor, FollowService follow)
    {
        _monitor = monitor;
        _follow = follow;
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
        _lastTargetTiles.Clear();
    }

    public void Update(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        TickCooldowns(_attackCooldowns);
        TickCooldowns(_healCooldowns);
        TickCooldowns(_signatureCooldowns);

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .ToList();

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

            PartyRole role = ResolveRole(member);
            NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
            int affinity = Math.Max(2, profile?.GetAffinity(role) ?? 2);

            if (TryPerformRecovery(npc, member, role, affinity, monsters.Count > 0))
                stillEngaged.Add(member.CharacterName);

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
            _attackCooldowns[member.CharacterName] = GetAttackCooldown(role, member.Engagement, affinity);
        }

        foreach (string name in _targets.Keys.ToList())
        {
            if (stillEngaged.Contains(name))
                continue;

            NPC? npc = Game1.getCharacterFromName(name);
            Disengage(name, npc);
        }
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
        {
            // Passive never hunts. It only protects the immediate Farmer bubble.
            candidates = candidates.Where(monster => Vector2.Distance(monster.Tile, farmerTile) <= 2.75f);
        }

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
        bool combatPresent)
    {
        if (!combatPresent || role is not (PartyRole.Healer or PartyRole.Support))
            return false;

        float threshold = role == PartyRole.Healer ? 0.78f : 0.52f;
        if (Game1.player.health >= (int)(Game1.player.maxHealth * threshold))
            return false;

        if (GetCooldown(_healCooldowns, member.CharacterName) > 0)
            return false;

        int amount = role == PartyRole.Healer
            ? 4 + affinity * 2
            : 2 + affinity;

        int before = Game1.player.health;
        Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + amount);
        int restored = Game1.player.health - before;
        if (restored <= 0)
            return false;

        npc.faceTowardFarmerForPeriod(500, 4, false, Game1.player);
        PlayHealFeedback(npc, restored, role);
        TryTriggerRecoverySignature(npc, member, role, affinity, before);

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

    private void PerformAttack(
        NPC npc,
        Monster target,
        PartyMemberData member,
        PartyRole role,
        int affinity)
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

        // Affinity is a tuning modifier, not an MMO-level power multiplier.
        damage = (int)Math.Round(damage * (0.85f + affinity * 0.05f));

        int healthBefore = target.Health;
        Rectangle hitbox = target.GetBoundingBox();
        Game1.currentLocation.damageMonster(
            hitbox,
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
        ApplyRoleCombatEffect(npc, target, role, affinity, dealt);
        TryTriggerAttackSignature(npc, target, member, role, affinity);
    }

    private void PlayHealFeedback(NPC npc, int restored, PartyRole role)
    {
        Color color = role == PartyRole.Healer
            ? new Color(120, 255, 160)
            : new Color(255, 224, 120);

        SpawnBurst(Game1.currentLocation, Game1.player.Position + new Vector2(16f, -12f), color,
            role == PartyRole.Healer ? 6 : 4, 24f);

        npc.showTextAboveHead($"+{restored} HP", color, 2, 1000, 0);
        Game1.currentLocation.playSound("yoba");
    }

    private void ApplyRoleCombatEffect(NPC npc, Monster target, PartyRole role, int affinity, int dealt)
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
            int stunMs = 350 + affinity * 100;
            target.stunTime.Value = Math.Max(target.stunTime.Value, stunMs);
            target.showTextAboveHead("STUN", new Color(100, 235, 255), 2, 850, 0);
        }

        Game1.currentLocation.playSound(role == PartyRole.Control ? "thunder_small" : "swordswipe");
    }

    private void TryTriggerRecoverySignature(
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        int affinity,
        int healthBeforeBaseHeal)
    {
        if (GetCooldown(_signatureCooldowns, member.CharacterName) > 0)
            return;

        if (member.CharacterName.Equals("Harvey", StringComparison.OrdinalIgnoreCase)
            && role == PartyRole.Healer
            && healthBeforeBaseHeal <= (int)(Game1.player.maxHealth * 0.40f))
        {
            int before = Game1.player.health;
            int bonus = 6 + affinity * 2;
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

            _signatureCooldowns[member.CharacterName] = 600;
            return;
        }

        if (member.CharacterName.Equals("Emily", StringComparison.OrdinalIgnoreCase)
            && role is PartyRole.Support or PartyRole.Healer
            && healthBeforeBaseHeal <= (int)(Game1.player.maxHealth * 0.65f))
        {
            int before = Game1.player.health;
            int bonus = 2 + affinity;
            Game1.player.health = Math.Min(Game1.player.maxHealth, Game1.player.health + bonus);
            int restored = Game1.player.health - before;

            Color[] prism =
            {
                new Color(255, 110, 150),
                new Color(255, 190, 90),
                new Color(120, 255, 150),
                new Color(100, 220, 255),
                new Color(180, 120, 255)
            };

            for (int i = 0; i < prism.Length; i++)
                SpawnBurst(Game1.currentLocation, Game1.player.Position, prism[i], 2, 20f + i * 5f);

            npc.showTextAboveHead(restored > 0 ? $"PRISMATIC +{restored}" : "PRISMATIC AURA",
                new Color(230, 160, 255), 2, 1400, 0);
            Game1.currentLocation.playSound("yoba");
            _signatureCooldowns[member.CharacterName] = 720;
        }
    }

    private void TryTriggerAttackSignature(
        NPC npc,
        Monster target,
        PartyMemberData member,
        PartyRole role,
        int affinity)
    {
        if (target.Health <= 0 || GetCooldown(_signatureCooldowns, member.CharacterName) > 0)
            return;

        if (member.CharacterName.Equals("Abigail", StringComparison.OrdinalIgnoreCase)
            && role is PartyRole.Damage or PartyRole.Control)
        {
            Color purple = new(195, 95, 255);
            int bonusDamage = 4 + affinity;
            List<Monster> nearby = GetLivingMonstersNear(target.Tile, 2.25f);

            foreach (Monster monster in nearby)
            {
                Game1.currentLocation.damageMonster(
                    monster.GetBoundingBox(),
                    bonusDamage,
                    bonusDamage + 2,
                    isBomb: false,
                    1.0f,
                    100,
                    0.02f,
                    1.5f,
                    triggerMonsterInvincibleTimer: false,
                    Game1.player);
                SpawnBurst(Game1.currentLocation, monster.Position, purple, 5, 28f);
            }

            npc.showTextAboveHead("SPIRIT SLASH", purple, 2, 1300, 0);
            Game1.currentLocation.playSound("swordswipe");
            _signatureCooldowns[member.CharacterName] = 360;
            return;
        }

        if (member.CharacterName.Equals("Alex", StringComparison.OrdinalIgnoreCase)
            && role == PartyRole.Tank
            && Vector2.Distance(target.Tile, Game1.player.Tile) <= 4.0f)
        {
            Color orange = new(255, 155, 70);
            int guardDamage = 2 + affinity;
            List<Monster> threats = GetLivingMonstersNear(Game1.player.Tile, 2.75f);

            foreach (Monster monster in threats)
            {
                Game1.currentLocation.damageMonster(
                    monster.GetBoundingBox(),
                    guardDamage,
                    guardDamage + 1,
                    isBomb: false,
                    2.4f,
                    100,
                    0f,
                    1.25f,
                    triggerMonsterInvincibleTimer: false,
                    Game1.player);
                SpawnBurst(Game1.currentLocation, monster.Position, orange, 4, 30f);
            }

            SpawnBurst(Game1.currentLocation, Game1.player.Position, orange, 8, 42f);
            npc.showTextAboveHead("BODYGUARD", orange, 2, 1300, 0);
            Game1.currentLocation.playSound("clubSmash");
            _signatureCooldowns[member.CharacterName] = 420;
            return;
        }

        if (member.CharacterName.Equals("Maru", StringComparison.OrdinalIgnoreCase)
            && role == PartyRole.Control)
        {
            Color cyan = new(80, 230, 255);
            int stunMs = 700 + affinity * 100;
            List<Monster> nearby = GetLivingMonstersNear(target.Tile, 2.5f);

            foreach (Monster monster in nearby)
            {
                monster.stunTime.Value = Math.Max(monster.stunTime.Value, stunMs);
                SpawnBurst(Game1.currentLocation, monster.Position, cyan, 7, 30f);
                monster.showTextAboveHead("SHOCK", cyan, 2, 1000, 0);
            }

            npc.showTextAboveHead("SHOCK DEVICE", cyan, 2, 1300, 0);
            Game1.currentLocation.playSound("thunder_small");
            _signatureCooldowns[member.CharacterName] = 480;
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

    private static void SpawnBurst(
        GameLocation location,
        Vector2 worldPosition,
        Color color,
        int count,
        float spreadPixels)
    {
        int safeCount = Math.Clamp(count, 1, 16);
        for (int i = 0; i < safeCount; i++)
        {
            double angle = Math.PI * 2d * i / safeCount;
            Vector2 offset = new(
                (float)Math.Cos(angle) * spreadPixels,
                (float)Math.Sin(angle) * spreadPixels * 0.65f);

            location.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    10,
                    worldPosition + offset,
                    color,
                    6,
                    false,
                    45f + i * 3f));
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
