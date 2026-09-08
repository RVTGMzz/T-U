using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;

namespace Ronvotri.TeamUp.Following;

public sealed class FollowService
{
    public const string PartyControlledModDataKey = "Ronvotri.TeamUp/PartyControlled";
    public const string PartyControllerOwnerModDataKey = "Ronvotri.TeamUp/PartyControllerOwner";
    private const float StopDistanceTiles = 1.45f;
    private const float WarpDistanceTiles = 11f;
    private const float RepathDistanceTiles = 0.90f;
    private const int RepathCooldownUpdates = 6;
    private const int UnsafeTargetCooldownUpdates = 15;

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

    // Small deterministic fallback ring. The old radius-square search could perform dozens of
    // collision queries per follower every update around water, bridges, cliffs and narrow paths.
    private static readonly Point[] OpenSearchOffsets =
    {
        new(0, 0),
        new(0, 1), new(-1, 0), new(1, 0), new(0, -1),
        new(-1, 1), new(1, 1), new(-1, -1), new(1, -1),
        new(0, 2), new(-2, 0), new(2, 0), new(0, -2),
        new(-1, 2), new(1, 2), new(-2, 1), new(2, 1)
    };

    private readonly IMonitor _monitor;
    private readonly Dictionary<NPC, PathFindController> _ownedControllers = new();
    private readonly Dictionary<NPC, Vector2> _lastTargets = new();
    private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();
    private readonly Dictionary<NPC, bool> _baseFarmerPassesThrough = new();
    private readonly Dictionary<NPC, int> _repathCooldowns = new();
    private readonly HashSet<NPC> _unsafeTargetNpcs = new();
    private readonly HashSet<string> _releasedCharacters = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _combatControlled = new(StringComparer.OrdinalIgnoreCase);

    public FollowService(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void Update(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId)
        => Update(members, companionUnits, recruiterId, Game1.player);

    public void Update(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId,
        Farmer owner)
    {
        if (!Context.IsWorldReady || owner.currentLocation is null)
            return;

        UpdatePartyMembers(members, recruiterId, owner);
        UpdateCompanionUnits(members, companionUnits, recruiterId, owner);
    }

    public void PrepareForParty(NPC npc, long? recruiterId = null)
    {
        RememberBaseSpeed(npc);
        EnableFarmerPassThrough(npc);
        UnlockVanillaMovementAnimation(npc);
        npc.modData[PartyControlledModDataKey] = "true";
        if (recruiterId.HasValue)
            npc.modData[PartyControllerOwnerModDataKey] = recruiterId.Value.ToString();
        npc.followSchedule = false;
        npc.ignoreScheduleToday = true;
    }

    public void TakePartyControl(NPC npc, long? recruiterId = null)
    {
        _releasedCharacters.Remove(npc.Name);
        _combatControlled.Remove(npc.Name);
        PrepareForParty(npc, recruiterId);
        ClearPath(npc);
        ResetToStandingPose(npc);
    }

    private static void ResetToStandingPose(NPC npc)
    {
        int facing = Math.Clamp(npc.FacingDirection, 0, 3);
        UnlockVanillaMovementAnimation(npc, force: true);
        npc.Halt();
        npc.Sprite.StopAnimation();
        npc.faceDirection(facing);
    }

    /// <summary>
    /// Alpha 6.7.2: some vanilla end-of-route jobs (notably Gus at the Saloon) leave
    /// AnimatedSprite.ignoreStopAnimation enabled. Stardew's StopAnimation() and faceDirection()
    /// both early-return while that flag is set, so Team Up pathfinding can move the NPC while the
    /// visible sprite remains frozen on one frame. Clear only the vanilla route-animation locks;
    /// walking/facing remains entirely Stardew's normal NPC animation system.
    /// </summary>
    private static void UnlockVanillaMovementAnimation(NPC npc, bool force = false)
    {
        bool routeLocked = npc.Sprite.ignoreStopAnimation
            || npc.Sprite.ignoreSourceRectUpdates
            || npc.doingEndOfRouteAnimation.Value
            || npc.goingToDoEndOfRouteAnimation.Value;
        if (!force && !routeLocked)
            return;

        int facing = Math.Clamp(npc.FacingDirection, 0, 3);
        npc.doingEndOfRouteAnimation.Value = false;
        npc.goingToDoEndOfRouteAnimation.Value = false;
        // Alpha 6.7.10: preserve endOfRouteBehaviorName/messages. Team Up suspends route
        // execution while it owns the NPC instead of corrupting the schedule metadata.

        npc.Sprite.ignoreStopAnimation = false;
        npc.Sprite.ignoreSourceRectUpdates = false;
        npc.Sprite.loop = true;
        npc.Sprite.ClearAnimation();
        npc.Sprite.StopAnimation();
        npc.faceDirection(facing);
    }

    public void SetCombatControl(NPC npc, bool active)
    {
        if (active)
        {
            _releasedCharacters.Remove(npc.Name);
            if (!_combatControlled.Add(npc.Name))
                return;

            PrepareForParty(npc);
            ClearPath(npc);
            npc.Halt();
            return;
        }

        if (!_combatControlled.Remove(npc.Name))
            return;

        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: true);
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
        _releasedCharacters.Add(npc.Name);
        _combatControlled.Remove(npc.Name);
        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: false);
        RestoreFarmerPassThrough(npc);
        npc.modData.Remove(PartyControlledModDataKey);
        npc.modData.Remove(PartyControllerOwnerModDataKey);
        npc.Halt();
        npc.followSchedule = true;
        npc.ignoreScheduleToday = false;
    }

    public void ReleaseToVanillaAndResumeSchedule(NPC npc)
    {
        ReleaseToVanilla(npc);
        ResumeVanillaSchedulePosition(npc);
    }

    private void ResumeVanillaSchedulePosition(NPC npc)
    {
        if (npc.Schedule is null || npc.Schedule.Count == 0)
            return;

        var currentEntry = npc.Schedule.Where(pair => pair.Key <= Game1.timeOfDay).OrderByDescending(pair => pair.Key).FirstOrDefault();
        SchedulePathDescription? destination = currentEntry.Value;
        if (destination is null || string.IsNullOrWhiteSpace(destination.targetLocationName))
            return;

        GameLocation? targetLocation = Game1.getLocationFromName(destination.targetLocationName);
        if (targetLocation is null)
            return;

        npc.queuedSchedulePaths.Clear();
        npc.lastAttemptedSchedule = Game1.timeOfDay;

        if (ReferenceEquals(npc.currentLocation, targetLocation))
        {
            try
            {
                var controller = new PathFindController(npc, targetLocation, destination.targetTile, destination.facingDirection);
                if (controller.pathToEndPoint is not null && controller.pathToEndPoint.Count > 0)
                {
                    npc.controller = controller;
                    return;
                }
            }
            catch (Exception ex)
            {
                _monitor.LogOnce($"Schedule resume path failed for {npc.Name}: {ex.Message}", LogLevel.Trace);
            }
        }

        Game1.warpCharacter(npc, targetLocation, new Vector2(destination.targetTile.X, destination.targetTile.Y));
        npc.faceDirection(destination.facingDirection);
        npc.Halt();
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
            if (PelipperTownCompatibilityService.IsSourceControlled(unit))
                continue;

            NPC? npc = ResolveCharacter(unit.CharacterName);
            if (npc is not null)
                ReleaseToVanilla(npc);
        }
    }

    private void UpdatePartyMembers(IReadOnlyList<PartyMemberData> members, long recruiterId, Farmer owner)
    {
        List<PartyMemberData> ownedMembers = members
            .Where(member => member.RecruiterId == recruiterId)
            .Take(FormationOffsets.Length)
            .ToList();
        for (int index = 0; index < ownedMembers.Count; index++)
        {
            PartyMemberData member = ownedMembers[index];
            if (_releasedCharacters.Contains(member.CharacterName) || _combatControlled.Contains(member.CharacterName))
                continue;
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

            PrepareForParty(npc, recruiterId);
            Vector2? targetTile = FindPlayerFollowTile(owner.currentLocation, owner.Tile, index);
            if (!targetTile.HasValue)
            {
                SuspendForUnsafeTarget(npc);
                continue;
            }

            // Alpha 6.6.7 intentionally made target checks lightweight, but some water tiles are
            // passable to the map even though humanoid NPCs should never stand there. Repair any
            // already-stranded party member immediately to the nearest safe formation tile.
            if (ReferenceEquals(npc.currentLocation, owner.currentLocation)
                && !PartyTileSafety.IsWalkableLandOrBridge(owner.currentLocation, npc.Tile))
            {
                WarpNearTarget(npc, owner.currentLocation, targetTile.Value);
                _repathCooldowns[npc] = RepathCooldownUpdates;
                continue;
            }

            FollowTarget(npc, owner.currentLocation, targetTile.Value, owner.FacingDirection);
        }
    }

    private void UpdateCompanionUnits(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId,
        Farmer owner)
    {
        List<CompanionUnitData> activeUnits = companionUnits
            .Where(unit => unit.RecruiterId == recruiterId)
            .Where(unit => unit.State is CompanionDeploymentState.Active or CompanionDeploymentState.Waiting)
            .ToList();
        int playerPetIndex = 0;
        var ownerCompanionIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (CompanionUnitData unit in activeUnits)
        {
            // Alpha 6.6.10: Pelipper Town is the sole movement/render authority for its Pokemon.
            // Skip before resolving the actor so Team Up cannot PrepareForParty, HoldPosition,
            // FollowTarget, warp, Halt, or replace controllers for source-owned companions.
            if (PelipperTownCompatibilityService.IsSourceControlled(unit))
                continue;

            if (_releasedCharacters.Contains(unit.CharacterName))
                continue;
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
                PrepareForParty(npc, recruiterId);
                Vector2 playerTarget = FindCompanionTile(owner.currentLocation, owner.Tile, playerPetIndex++);
                FollowTarget(npc, owner.currentLocation, playerTarget, owner.FacingDirection);
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

            PrepareForParty(npc, recruiterId);
            int index = ownerCompanionIndex.TryGetValue(ownerData.CharacterName, out int current) ? current : 0;
            ownerCompanionIndex[ownerData.CharacterName] = index + 1;
            Vector2 linkedTarget = FindCompanionTile(ownerNpc.currentLocation, ownerNpc.Tile, index);
            FollowTarget(npc, ownerNpc.currentLocation, linkedTarget, ownerNpc.FacingDirection);
        }
    }

    private void FollowTarget(NPC npc, GameLocation targetLocation, Vector2 targetTile, int finalFacingDirection)
    {
        int cooldown = 0;
        if (_repathCooldowns.TryGetValue(npc, out int existingCooldown) && existingCooldown > 0)
        {
            cooldown = existingCooldown;
            _repathCooldowns[npc] = existingCooldown - 1;
        }

        // Flying mounts and modded traversal can place the Farmer over water, cliffs, or other
        // tiles NPC pathfinding cannot legally reach. Never ask PathFindController to solve an
        // invalid destination every update; that creates severe CPU churn with a full party.
        if (!IsOpen(targetLocation, targetTile))
        {
            SuspendForUnsafeTarget(npc);
            return;
        }

        if (_unsafeTargetNpcs.Remove(npc))
        {
            _repathCooldowns.Remove(npc);
            cooldown = 0;
        }

        if (npc.currentLocation != targetLocation)
        {
            WarpNearTarget(npc, targetLocation, targetTile);
            _repathCooldowns[npc] = RepathCooldownUpdates;
            return;
        }

        float distance = Vector2.Distance(npc.Tile, targetTile);
        ApplyCatchUpSpeed(npc, distance);

        if (distance >= WarpDistanceTiles)
        {
            WarpNearTarget(npc, targetLocation, targetTile);
            _repathCooldowns[npc] = RepathCooldownUpdates;
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

        if (hasForeignController)
        {
            ClearPath(npc);
            cooldown = 0;
        }
        else if (targetMovedEnough && cooldown <= 0)
        {
            ClearPath(npc);
        }

        if (npc.temporaryController is not null)
        {
            npc.temporaryController = null;
            npc.Halt();
        }

        // When a mount or fast traversal moves the target every couple of game ticks, keep the
        // current route briefly instead of rebuilding it continuously. If the controller already
        // finished, a very short pause is preferable to a pathfinding storm.
        if (npc.controller is null && cooldown > 0)
            return;

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
                _repathCooldowns[npc] = RepathCooldownUpdates;
            }
            catch (Exception ex)
            {
                _repathCooldowns[npc] = UnsafeTargetCooldownUpdates;
                _monitor.LogOnce($"Pathfinding failed for {npc.Name}: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    private void SuspendForUnsafeTarget(NPC npc)
    {
        _repathCooldowns[npc] = UnsafeTargetCooldownUpdates;
        if (!_unsafeTargetNpcs.Add(npc))
            return;

        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: true);
        npc.Halt();
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

    private void EnableFarmerPassThrough(NPC npc)
    {
        if (!_baseFarmerPassesThrough.ContainsKey(npc))
            _baseFarmerPassesThrough[npc] = npc.farmerPassesThrough;

        npc.farmerPassesThrough = true;
    }

    private void RestoreFarmerPassThrough(NPC npc)
    {
        if (!_baseFarmerPassesThrough.TryGetValue(npc, out bool original))
            return;

        npc.farmerPassesThrough = original;
        _baseFarmerPassesThrough.Remove(npc);
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

    private static Vector2? FindPlayerFollowTile(GameLocation location, Vector2 farmerTile, int slotIndex)
    {
        Point offset = FormationOffsets[Math.Clamp(slotIndex, 0, FormationOffsets.Length - 1)];
        return FindLandOpenNear(location, farmerTile, offset);
    }

    private static Vector2? FindLandOpenNear(GameLocation location, Vector2 anchorTile, Point preferredOffset)
    {
        Vector2 preferred = anchorTile + new Vector2(preferredOffset.X, preferredOffset.Y);
        if (PartyTileSafety.IsWalkableLandOrBridge(location, preferred))
            return preferred;

        foreach (Point offset in OpenSearchOffsets)
        {
            if (offset == preferredOffset)
                continue;

            Vector2 candidate = anchorTile + new Vector2(offset.X, offset.Y);
            if (PartyTileSafety.IsWalkableLandOrBridge(location, candidate))
                return candidate;
        }

        // Never fall back to a known-invalid preferred tile. If the Farmer is on a mount or
        // traversal surface with no nearby humanoid-safe tile, hold followers until land returns.
        return PartyTileSafety.IsWalkableLandOrBridge(location, anchorTile) ? anchorTile : null;
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

        foreach (Point offset in OpenSearchOffsets)
        {
            if (offset == preferredOffset)
                continue;

            Vector2 candidate = anchorTile + new Vector2(offset.X, offset.Y);
            if (IsOpen(location, candidate))
                return candidate;
        }

        return preferred;
    }

    private static bool IsOpen(GameLocation location, Vector2 tile)
    {
        if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || tile.X < 0f || tile.Y < 0f)
            return false;

        try
        {
            // Follow targets only need a legal map/passable tile. Avoid the much heavier
            // totally-clear/placeable query, which also reacts badly to crowds on narrow bridges.
            return location.isTileOnMap(tile) && location.isTilePassable(tile);
        }
        catch
        {
            return false;
        }
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
