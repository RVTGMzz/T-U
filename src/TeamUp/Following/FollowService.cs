using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Pathfinding;

namespace Ronvotri.TeamUp.Following;

public sealed class FollowService
{
    private const float StopDistanceTiles = 1.6f;
    private const float WarpDistanceTiles = 12f;

    private static readonly Point[] FormationOffsets =
    {
        new(-1, 1),
        new(1, 1),
        new(-2, 0),
        new(2, 0),
        new(-1, 2),
        new(1, 2)
    };

    private readonly IMonitor _monitor;

    public FollowService(IMonitor monitor)
    {
        _monitor = monitor;
    }

    public void Update(IReadOnlyList<PartyMemberData> members, long recruiterId)
    {
        if (!Context.IsWorldReady)
            return;

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
            FollowPlayer(npc, index);
        }
    }

    public void PrepareForParty(NPC npc)
    {
        npc.followSchedule = false;
        npc.ignoreScheduleToday = true;
    }

    public void HoldPosition(NPC npc)
    {
        PrepareForParty(npc);
        npc.controller = null;
        npc.temporaryController = null;
        npc.Halt();
    }

    public void ReleaseToVanilla(NPC npc)
    {
        npc.controller = null;
        npc.temporaryController = null;
        npc.Halt();
        npc.followSchedule = true;
        npc.ignoreScheduleToday = false;
    }

    public void ReleaseAll(IEnumerable<PartyMemberData> members, long recruiterId)
    {
        foreach (PartyMemberData member in members.Where(member => member.RecruiterId == recruiterId))
        {
            NPC? npc = ResolveCharacter(member.CharacterName);
            if (npc is not null)
                ReleaseToVanilla(npc);
        }
    }

    private void FollowPlayer(NPC npc, int slotIndex)
    {
        Vector2 targetTile = FindFollowTile(Game1.currentLocation, slotIndex);

        if (npc.currentLocation != Game1.currentLocation)
        {
            WarpNearPlayer(npc, targetTile);
            return;
        }

        float distance = Vector2.Distance(npc.Tile, targetTile);
        if (distance >= WarpDistanceTiles)
        {
            WarpNearPlayer(npc, targetTile);
            return;
        }

        if (distance <= StopDistanceTiles)
        {
            if (npc.controller is not null || npc.temporaryController is not null)
            {
                npc.controller = null;
                npc.temporaryController = null;
                npc.Halt();
            }
            return;
        }

        if (npc.controller is null)
        {
            try
            {
                npc.controller = new PathFindController(
                    npc,
                    npc.currentLocation,
                    targetTile.ToPoint(),
                    Game1.player.FacingDirection);
            }
            catch (Exception ex)
            {
                _monitor.LogOnce($"Pathfinding failed for {npc.Name}: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    private void WarpNearPlayer(NPC npc, Vector2 targetTile)
    {
        npc.controller = null;
        npc.temporaryController = null;

        Game1.warpCharacter(npc, Game1.currentLocation, targetTile);
        npc.Halt();
    }

    private static Vector2 FindFollowTile(GameLocation location, int slotIndex)
    {
        Point offset = FormationOffsets[Math.Clamp(slotIndex, 0, FormationOffsets.Length - 1)];
        Vector2 playerTile = Game1.player.Tile;
        Vector2 preferred = playerTile + new Vector2(offset.X, offset.Y);

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

                    Vector2 candidate = playerTile + new Vector2(x, y);
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
