using Microsoft.Xna.Framework;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Cheap movement-target validation for Team Up-controlled humanoid NPCs.
/// Preserves the lightweight Alpha 6.6.7 pathing fix while rejecting bare water tiles.
/// </summary>
public static class PartyTileSafety
{
    public static bool IsWalkableLandOrBridge(GameLocation location, Vector2 tile)
    {
        if (tile.X < 0f || tile.Y < 0f || float.IsNaN(tile.X) || float.IsNaN(tile.Y))
            return false;

        int x = (int)Math.Floor(tile.X);
        int y = (int)Math.Floor(tile.Y);

        try
        {
            if (!location.isTileOnMap(tile) || !location.isTilePassable(tile))
                return false;

            if (!location.isWaterTile(x, y))
                return true;

            // Real bridges/walkways may sit over Back-layer water. A bridge normally has an
            // actual Buildings-layer tile at the same coordinate, while bare river/lake does not.
            var buildings = location.Map?.GetLayer("Buildings");
            return buildings is not null
                && x >= 0
                && y >= 0
                && x < buildings.LayerWidth
                && y < buildings.LayerHeight
                && buildings.Tiles[x, y] is not null;
        }
        catch
        {
            return false;
        }
    }
}
