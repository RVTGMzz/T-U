using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// First real Team Up combat loop. Active Party Members acquire monsters around the
/// Farmer, temporarily take movement control from the formation system, attack, then
/// return to normal following when combat ends.
/// </summary>
public sealed class CombatService
{
    private const float HardLeashTiles = 12f;
    private const float RepathThresholdTiles = 0.9f;

    private readonly IMonitor _monitor;
    private readonly FollowService _follow;
    private readonly Dictionary<string, int> _attackCooldowns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _healCooldowns = new(StringComparer.OrdinalIgnoreCase);
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
        _lastTargetTiles.Clear();
    }

    public void Update(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return;

        TickCooldowns(_attackCooldowns);
        TickCooldowns(_healCooldowns);

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
        Game1.currentLocation.playSound("yoba");
        Game1.currentLocation.temporarySprites.Add(
            new TemporaryAnimatedSprite(10, Game1.player.Position + new Vector2(16f, -16f), Color.LightGreen, 8, false, 60f));

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

        Color effectColor = role switch
        {
            PartyRole.Tank => Color.Orange,
            PartyRole.Control => Color.Cyan,
            PartyRole.Support => Color.Gold,
            PartyRole.Healer => Color.LightGreen,
            _ => Color.White
        };

        Game1.currentLocation.temporarySprites.Add(
            new TemporaryAnimatedSprite(10, target.Position, effectColor, 6, false, 45f));
        Game1.currentLocation.playSound(role == PartyRole.Control ? "thunder_small" : "swordswipe");
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
