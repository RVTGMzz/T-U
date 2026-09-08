from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.11"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, content: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


# Version only. Alpha 6.7.10 source is already materialized on main and remains the route-fix baseline.
project = read("TeamUp.csproj")
if f"<Version>{VERSION}</Version>" not in project:
    old = "<Version>0.2.0-alpha.6.7.10</Version>"
    if project.count(old) != 1:
        raise RuntimeError("Unexpected TeamUp.csproj version")
    write("TeamUp.csproj", project.replace(old, f"<Version>{VERSION}</Version>", 1))

combat = read("Combat/CombatService.cs")

# Alpha 6.7.10 introduced major-threat coordination, but the direct major-target override could
# bypass assignedCounts and pull every eligible attacker onto the same boss even when adds existed.
# Alpha 6.7.11 keeps boss pressure while restoring the anti-dogpile contract.
old_coordination = r'''    private Monster? AcquireCoordinatedTarget(
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        IReadOnlyList<Monster> monsters,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyCollection<string> validThreatActors,
        IReadOnlyDictionary<Monster, int> assignedCounts)
    {
        Monster? normal = AcquireTarget(npc, member, role, monsters, activeMembers, validThreatActors, assignedCounts);
        if (role is PartyRole.Tank or PartyRole.Healer || monsters.Count == 0)
            return normal;

        bool shouldCoordinate = _strategy() == PartyStrategy.BossFocus
            || role is PartyRole.Damage or PartyRole.Control
            || member.Engagement is EngagementStyle.Aggressive or EngagementStyle.Reckless;
        if (!shouldCoordinate)
            return normal;

        Monster? major = monsters
            .Where(monster => monster.Health > 0)
            .Where(monster => Vector2.Distance(monster.Tile, FarmerContext.Tile) <= 10f)
            .Where(monster => monster.MaxHealth >= 300)
            .OrderByDescending(monster => monster.MaxHealth)
            .ThenBy(monster => Vector2.DistanceSquared(monster.Tile, FarmerContext.Tile))
            .FirstOrDefault();

        return major ?? normal;
    }

    private static float GetDistanceToMonsterBoundsTiles(NPC npc, Monster target)
    {
        Rectangle a = npc.GetBoundingBox();
        Rectangle b = target.GetBoundingBox();
        int dx = a.Right < b.Left ? b.Left - a.Right : b.Right < a.Left ? a.Left - b.Right : 0;
        int dy = a.Bottom < b.Top ? b.Top - a.Bottom : b.Bottom < a.Top ? a.Top - b.Bottom : 0;
        return MathF.Sqrt(dx * dx + dy * dy) / 64f;
    }
'''
new_coordination = r'''    private Monster? AcquireCoordinatedTarget(
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        IReadOnlyList<Monster> monsters,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyCollection<string> validThreatActors,
        IReadOnlyDictionary<Monster, int> assignedCounts)
    {
        Monster? normal = AcquireTarget(npc, member, role, monsters, activeMembers, validThreatActors, assignedCounts);
        if (role is PartyRole.Tank or PartyRole.Healer || monsters.Count == 0)
            return normal;

        bool shouldCoordinate = _strategy() == PartyStrategy.BossFocus
            || role is PartyRole.Damage or PartyRole.Control
            || member.Engagement is EngagementStyle.Aggressive or EngagementStyle.Reckless;
        if (!shouldCoordinate)
            return normal;

        List<Monster> majors = monsters
            .Where(monster => monster.Health > 0)
            .Where(monster => ReferenceEquals(monster.currentLocation, FarmerContext.currentLocation))
            .Where(monster => Vector2.Distance(monster.Tile, FarmerContext.Tile) <= 10f)
            .Where(monster => monster.MaxHealth >= 300)
            .ToList();
        if (majors.Count == 0)
            return normal;

        // Outside Boss Focus, two coordinated attackers are enough to keep pressure on one major
        // threat while leaving room for add control. Boss Focus raises this soft cap to three.
        int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;
        Monster? major = majors
            .Where(monster => !assignedCounts.TryGetValue(monster, out int count) || count < coordinationCap)
            .OrderBy(monster => assignedCounts.TryGetValue(monster, out int count) ? count : 0)
            .ThenByDescending(monster => monster.MaxHealth)
            .ThenBy(monster => GetDistanceToMonsterBoundsTiles(npc, monster))
            .FirstOrDefault();
        if (major is not null)
            return major;

        // Every major is already covered. Prefer spreading to an add instead of bypassing the
        // anti-dogpile score. If the boss is the only living target, fall back to normal so nobody idles.
        List<Monster> nonMajors = monsters.Where(monster => !majors.Contains(monster)).ToList();
        if (nonMajors.Count > 0)
        {
            Monster? spread = AcquireTarget(npc, member, role, nonMajors, activeMembers, validThreatActors, assignedCounts);
            if (spread is not null)
                return spread;
        }

        return normal;
    }

    private static float GetDistanceToMonsterBoundsTiles(NPC npc, Monster target)
        => GetRectangleGapPixels(npc.GetBoundingBox(), target.GetBoundingBox()) / 64f;

    private static float GetRectangleGapPixels(Rectangle a, Rectangle b)
    {
        int dx = a.Right < b.Left ? b.Left - a.Right : b.Right < a.Left ? a.Left - b.Right : 0;
        int dy = a.Bottom < b.Top ? b.Top - a.Bottom : b.Bottom < a.Top ? a.Top - b.Bottom : 0;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
'''
if new_coordination not in combat:
    if combat.count(old_coordination) != 1:
        raise RuntimeError("Alpha 6.7.10 coordination block missing/ambiguous")
    combat = combat.replace(old_coordination, new_coordination, 1)

# Large monsters must be approached around their actual hitbox footprint, not around only target.Tile.
old_move = "        if (!TryFindApproachTile(FarmerContext.currentLocation, npc.Tile, target.Tile, GetAttackRange(role), out Vector2 targetTile))"
new_move = "        if (!TryFindApproachTile(FarmerContext.currentLocation, npc.Tile, target, GetAttackRange(role), out Vector2 targetTile))"
if new_move not in combat:
    if combat.count(old_move) != 1:
        raise RuntimeError("MoveTowardTarget approach anchor missing/ambiguous")
    combat = combat.replace(old_move, new_move, 1)

old_approach = r'''    private static bool TryFindApproachTile(GameLocation location, Vector2 from, Vector2 target, float range, out Vector2 best)
    {
        int radius = Math.Max(1, (int)Math.Floor(range));
        best = Vector2.Zero;
        float bestDistance = float.MaxValue;
        bool found = false;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                Vector2 candidate = target + new Vector2(x, y);
                if (!IsLightweightCombatTile(location, candidate))
                    continue;
                float distance = Vector2.DistanceSquared(candidate, from);
                if (distance >= bestDistance)
                    continue;
                best = candidate;
                bestDistance = distance;
                found = true;
            }
        }
        return found;
    }
'''
new_approach = r'''    private static bool TryFindApproachTile(GameLocation location, Vector2 from, Monster target, float range, out Vector2 best)
    {
        Rectangle bounds = target.GetBoundingBox();
        int left = (int)MathF.Floor(bounds.Left / 64f);
        int right = (int)MathF.Floor((bounds.Right - 1) / 64f);
        int top = (int)MathF.Floor(bounds.Top / 64f);
        int bottom = (int)MathF.Floor((bounds.Bottom - 1) / 64f);
        int radius = Math.Max(1, (int)Math.Ceiling(range));
        float maxGapPixels = Math.Max(64f, range * 64f);

        best = Vector2.Zero;
        float bestDistance = float.MaxValue;
        float bestGap = float.MaxValue;
        bool found = false;
        for (int x = left - radius; x <= right + radius; x++)
        {
            for (int y = top - radius; y <= bottom + radius; y++)
            {
                Vector2 candidate = new(x, y);
                if (!IsLightweightCombatTile(location, candidate))
                    continue;

                Rectangle tileBounds = new(x * 64, y * 64, 64, 64);
                if (tileBounds.Intersects(bounds))
                    continue;

                float gap = GetRectangleGapPixels(tileBounds, bounds);
                if (gap > maxGapPixels)
                    continue;

                float distance = Vector2.DistanceSquared(candidate, from);
                if (distance > bestDistance || (Math.Abs(distance - bestDistance) < 0.001f && gap >= bestGap))
                    continue;

                best = candidate;
                bestDistance = distance;
                bestGap = gap;
                found = true;
            }
        }
        return found;
    }
'''
if new_approach not in combat:
    if combat.count(old_approach) != 1:
        raise RuntimeError("TryFindApproachTile block missing/ambiguous")
    combat = combat.replace(old_approach, new_approach, 1)

write("Combat/CombatService.cs", combat)
print("Alpha 6.7.11 source materialized.")
