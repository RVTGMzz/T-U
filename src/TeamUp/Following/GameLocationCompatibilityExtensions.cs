using Microsoft.Xna.Framework;
using StardewValley;
using xTile.Dimensions;

namespace Ronvotri.TeamUp.Following;

/// <summary>Small Stardew 1.6 compatibility helpers used only by Team Up path placement.</summary>
internal static class GameLocationCompatibilityExtensions
{
    public static bool isTileLocationTotallyClearAndPlaceable(this GameLocation location, int x, int y)
    {
        Vector2 tile = new(x, y);

        if (!location.isTileOnMap(tile))
            return false;

        if (location.IsTileOccupiedBy(
            tile,
            CollisionMask.Buildings
                | CollisionMask.Furniture
                | CollisionMask.Objects
                | CollisionMask.Characters
                | CollisionMask.TerrainFeatures))
        {
            return false;
        }

        return location.isTilePassable(new Location(x, y), Game1.viewport)
            && location.isTilePlaceable(tile);
    }
}
