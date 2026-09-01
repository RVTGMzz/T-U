using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;

namespace Ronvotri.TeamUp.Following;

public sealed class FollowService
{
    private const float StopDistanceTiles = 1.55f;
    private const float WarpDistanceTiles = 10f;
    private const float RepathDistanceTiles = 1.35f;

    private static readonly Point[] FormationOffsets =
    {
        new(-1, 1),
        new(1, 1),
        new(-2, 0),
        new(2, 0),
        new(-1, 2),
        new(1, 2)
    };

    private static readonly Point[] CompanionOffsets =
    {
        new(0, 1),
        new(-1, 0),
        new(1, 0),
        new(0, -1),
        new(-1, 1),
        new(1, 1)
    };

    private readonly IMonitor _monitor;
    private readonly Dictionary<NPC, PathFindController> _ownedControllers = new();
    private readonly Dictionary<NPC, Vector2> _lastTargets = new();
    private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();

    public FollowService(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void Update(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId)
    {
        if (!Context.IsWorldReady)
            return;

        UpdatePartyMembers(members, recruiterId);
        UpdateCompanionUnits(members, companionUnits, recruiterId);
    }

    public void PrepareForParty(NPC npc)
    {
        RememberBaseSpeed(npc);
        npc.followSchedule = false;
        npc.ignoreScheduleToday = true;
    }

    /// <summary>Take movement ownership from vanilla scheduling once, without destroying Team Up paths every tick.</summary>
    public void TakePartyControl(NPC npc)
    {
        PrepareForParty(npc);
        ClearPath(npc);
        npc.Halt();
    }

    public void HoldPosition(NPC npc)
    {
        PrepareForParty(npc);
        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: true);
        npc.Halt();
    }

    public void ReleaseToVanilla(NPC npc)
    {
        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: false);
        npc.Halt();
        npc.followSchedule = true;
        npc.ignoreScheduleToday = false;
    }

    public void ReleaseAll(
        IEnumerable<PartyMemberData> members,
        IEnumerable<CompanionUnitData> companionUnits,
        long recruiterId)
    {
        foreach (PartyMemberData member in members.Where(member => member.RecruiterId == recruiterId))
        {
            NPC? npc = ResolveCharacter(member.CharacterName);
            if (npc is not null)
                ReleaseToVanilla(npc);
        }

        foreach (CompanionUnitData unit in companionUnits.Where(unit => unit.RecruiterId == recruiterId))
        {
            NPC? npc = ResolveCharacter(unit.CharacterName);
            if (npc is not null)
                ReleaseToVanilla(npc);
        }
    }

    private void UpdatePartyMembers(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        List<PartyMemberData> ownedMembers = members
            .Where(member => member.RecruiterId == recruiterId)
            .Take(FormationOffsets.Length)
            .ToList();

        for (int index = 0; index < ownedMembers.Count; index++)
        {
            PartyMemberData member = ownedMembers[index];
            NPC? npc = ResolveCharacter(member.CharacterName);
            if (npc is null)
                continue;

            if (member.State == PartyMemberState.Waiting)
            {
                HoldPosition(npc);
                continue;
            }

            if (member.State != PartyMemberState.Following)
                continue;

            PrepareForParty(npc);
            Vector2 targetTile = FindPlayerFollowTile(Game1.currentLocation, index);
            FollowTarget(npc, Game1.currentLocation, targetTile, Game1.player.FacingDirection);
        }
    }

    private void UpdateCompanionUnits(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId)
    {
        List<CompanionUnitData> activeUnits = companionUnits
            .Where(unit => unit.RecruiterId == recruiterId)
            .Where(unit => unit.State is CompanionDeploymentState.Active or CompanionDeploymentState.Waiting)
            .ToList();

        int playerPetIndex = 0;
        var ownerCompanionIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (CompanionUnitData unit in activeUnits)
        {
            NPC? npc = ResolveCharacter(unit.CharacterName);
            if (npc is null)
                continue;

            if (unit.State == CompanionDeploymentState.Waiting)
            {
                HoldPosition(npc);
                continue;
            }

            if (unit.OwnerKind == CompanionOwnerKind.Player)
            {
                PrepareForParty(npc);
                Vector2 target = FindCompanionTile(
                    Game1.currentLocation,
                    Game1.player.Tile,
                    playerPetIndex++);
                FollowTarget(npc, Game1.currentLocation, target, Game1.player.FacingDirection);
                continue;
            }

            if (unit.OwnerCharacterName is null)
                continue;

            PartyMemberData? ownerData = members.FirstOrDefault(member =>
                member.RecruiterId == recruiterId
                && string.Equals(member.CharacterName, unit.OwnerCharacterName, StringComparison.OrdinalIgnoreCase));

            NPC? ownerNpc = ResolveCharacter(unit.OwnerCharacterName);
            if (ownerData is null || ownerNpc is null || ownerData.State != PartyMemberState.Following)
                continue;

            PrepareForParty(npc);

            int index = ownerCompanionIndex.TryGetValue(ownerData.CharacterName, out int current)
                ? current
                : 0;
            ownerCompanionIndex[ownerData.CharacterName] = index + 1;

            Vector2 ownerTarget = FindCompanionTile(ownerNpc.currentLocation, ownerNpc.Tile, index);
            FollowTarget(npc, ownerNpc.currentLocation, ownerTarget, ownerNpc.FacingDirection);
        }
    }

    private void FollowTarget(NPC npc, GameLocation targetLocation, Vector2 targetTile, int finalFacingDirection)
    {
        if (npc.currentLocation != targetLocation)
        {
            WarpNearTarget(npc, targetLocation, targetTile);
            return;
        }

        float distance = Vector2.Distance(npc.Tile, targetTile);
        ApplyCatchUpSpeed(npc, distance);

        if (distance >= WarpDistanceTiles)
        {
            WarpNearTarget(npc, targetLocation, targetTile);
            return;
        }

        if (distance <= StopDistanceTiles)
        {
            ClearPath(npc);
            RestoreBaseSpeed(npc, keepTracked: true);
            npc.Halt();
            return;
        }

        bool hasForeignController = npc.controller is not null
            && (!_ownedControllers.TryGetValue(npc, out PathFindController? owned)
                || !ReferenceEquals(npc.controller, owned));

        bool targetMovedEnough = _lastTargets.TryGetValue(npc, out Vector2 oldTarget)
            && Vector2.Distance(oldTarget, targetTile) >= RepathDistanceTiles;

        if (hasForeignController || targetMovedEnough)
            ClearPath(npc);

        if (npc.temporaryController is not null)
        {
            npc.temporaryController = null;
            npc.Halt();
        }

        if (npc.controller is null)
        {
            try
            {
                var controller = new PathFindController(
                    npc,
                    npc.currentLocation,
                    targetTile.ToPoint(),
                    finalFacingDirection);

                npc.controller = controller;
                _ownedControllers[npc] = controller;
                _lastTargets[npc] = targetTile;
            }
            catch (Exception ex)
            {
                _monitor.LogOnce($"Pathfinding failed for {npc.Name}: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    private void WarpNearTarget(NPC npc, GameLocation location, Vector2 targetTile)
    {
        ClearPath(npc);
        Game1.warpCharacter(npc, location, targetTile);
        RestoreBaseSpeed(npc, keepTracked: true);
        npc.Halt();
    }

    private void ApplyCatchUpSpeed(NPC npc, float distanceTiles)
    {
        RememberBaseSpeed(npc);
        float baseSpeed = _baseAddedSpeeds[npc];

        float bonus = distanceTiles switch
        {
            >= 8f => 4f,
            >= 5f => 3f,
            >= 3f => 2f,
            _ => 1f
        };

        npc.addedSpeed = baseSpeed + bonus;
    }

    private void RememberBaseSpeed(NPC npc)
    {
        if (!_baseAddedSpeeds.ContainsKey(npc))
            _baseAddedSpeeds[npc] = npc.addedSpeed;
    }

    private void RestoreBaseSpeed(NPC npc, bool keepTracked)
    {
        if (_baseAddedSpeeds.TryGetValue(npc, out float baseSpeed))
            npc.addedSpeed = baseSpeed;

        if (!keepTracked)
            _baseAddedSpeeds.Remove(npc);
    }

    private void ClearPath(NPC npc)
    {
        _ownedControllers.Remove(npc);
        _lastTargets.Remove(npc);
        npc.controller = null;
        npc.temporaryController = null;
    }

    private static Vector2 FindPlayerFollowTile(GameLocation location, int slotIndex)
    {
        Point offset = FormationOffsets[Math.Clamp(slotIndex, 0, FormationOffsets.Length - 1)];
        return FindOpenNear(location, Game1.player.Tile, offset);
    }

    private static Vector2 FindCompanionTile(GameLocation location, Vector2 anchorTile, int companionIndex)
    {
        Point offset = CompanionOffsets[Math.Clamp(companionIndex, 0, CompanionOffsets.Length - 1)];
        return FindOpenNear(location, anchorTile, offset);
    }

    private static Vector2 FindOpenNear(GameLocation location, Vector2 anchorTile, Point preferredOffset)
    {
        Vector2 preferred = anchorTile + new Vector2(preferredOffset.X, preferredOffset.Y);
        if (IsOpen(location, preferred))
            return preferred;

        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector2 candidate = anchorTile + new Vector2(x, y);
                    if (IsOpen(location, candidate))
                        return candidate;
                }
            }
        }

        return preferred;
    }

    private static bool IsOpen(GameLocation location, Vector2 tile)
    {
        return location.isTileLocationTotallyClearAndPlaceable((int)tile.X, (int)tile.Y);
    }

    private static NPC? ResolveCharacter(string characterName)
    {
        NPC? direct = Game1.getCharacterFromName(characterName);
        if (direct is not null)
            return direct;

        foreach (GameLocation location in Game1.locations)
        {
            NPC? found = location.characters
                .OfType<NPC>()
                .FirstOrDefault(npc => string.Equals(npc.Name, characterName, StringComparison.OrdinalIgnoreCase));

            if (found is not null)
                return found;
        }

        return null;
    }
}
