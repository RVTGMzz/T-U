using HarmonyLib;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.15: keep the intended 2-4 Mutation minions, but make fallback placement robust on
/// Pelipper/custom/Farm locations. The strict vanilla-style pass still runs first. If it rejects all
/// candidates, this layer anchors Pelipper waves on the visible Pokemon source, searches a wider ring,
/// allows harmless terrain such as grass, and still rejects off-map/impassable/object/occupied tiles.
/// </summary>
internal sealed class Alpha674413MutationMinionSpawnService
{
    private static readonly Point[] SpawnOffsets = BuildSpawnOffsets();

    private const float MinimumFarmerDistance = 80f;
    private const float MinimumCharacterDistance = 40f;

    private static Alpha674413MutationMinionSpawnService? Active;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private long _fallbackAttempts;
    private long _fallbackResolved;
    private long _fallbackRejected;
    private long _pelipperSourceAnchors;
    private long _tilesScanned;
    private string _last = "reset";

    public Alpha674413MutationMinionSpawnService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha674415MutationMinionSpawn");
        Active = this;

        var method = AccessTools.Method(typeof(MonsterMutationService), "TryFindSafeSpawnPosition");
        if (method is null)
        {
            _monitor.Log("6.7.44.15 minion spawn fallback unavailable: TryFindSafeSpawnPosition not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            method,
            postfix: new HarmonyMethod(typeof(Alpha674413MutationMinionSpawnService), nameof(SpawnPostfix))
            {
                priority = Priority.Last
            });
        _monitor.Log("Team Up 6.7.44.15 Mutation 2-4 minion wide-ring spawn fallback enabled.", LogLevel.Info);
    }

    public string Describe()
        => $"Mutation minion spawn fallback: minions=2-4 | attempts={_fallbackAttempts} | resolved={_fallbackResolved} | rejected={_fallbackRejected} | "
            + $"pelipperSourceAnchors={_pelipperSourceAnchors} | tilesScanned={_tilesScanned} | last={_last}";

    public void ResetTelemetry()
    {
        _fallbackAttempts = 0;
        _fallbackResolved = 0;
        _fallbackRejected = 0;
        _pelipperSourceAnchors = 0;
        _tilesScanned = 0;
        _last = "reset";
    }

    private static void SpawnPostfix(GameLocation __0, Monster __1, int __2, ref Vector2 __3, ref bool __result)
    {
        Alpha674413MutationMinionSpawnService? service = Active;
        if (service is null || __result || !Context.IsWorldReady)
            return;

        service._fallbackAttempts++;

        Vector2 anchorPosition = __1.Position;
        string anchorLabel = __1.Name;
        if (PelipperTownCompatibilityService.IsWildCombatActor(__1)
            && PelipperWildEncounterIdentityService.TryResolve(__1, out PelipperWildEncounterIdentity identity)
            && !ReferenceEquals(identity.SourceActor, __1)
            && ReferenceEquals(identity.SourceActor.currentLocation, __0))
        {
            anchorPosition = identity.SourceActor.Position;
            anchorLabel = identity.DisplayName;
            service._pelipperSourceAnchors++;
        }

        if (!TryFindRelaxed(__0, __1, anchorPosition, __2, service, out Vector2 position))
        {
            service._fallbackRejected++;
            service._last = $"rejected location={__0.NameOrUniqueName} anchor={anchorLabel} searched={SpawnOffsets.Length}";
            return;
        }

        __3 = position;
        __result = true;
        service._fallbackResolved++;
        service._last = $"resolved location={__0.NameOrUniqueName} anchor={anchorLabel} tile={(int)(position.X / 64f)},{(int)(position.Y / 64f)}";
    }

    private static bool TryFindRelaxed(
        GameLocation location,
        Monster anchor,
        Vector2 anchorPosition,
        int seed,
        Alpha674413MutationMinionSpawnService service,
        out Vector2 position)
    {
        int sourceTileX = (int)Math.Floor((anchorPosition.X + 32f) / 64f);
        int sourceTileY = (int)Math.Floor((anchorPosition.Y + 32f) / 64f);
        int start = Math.Abs(seed * 17 + sourceTileX * 5 + sourceTileY * 11) % SpawnOffsets.Length;

        for (int attempt = 0; attempt < SpawnOffsets.Length; attempt++)
        {
            Point offset = SpawnOffsets[(start + attempt) % SpawnOffsets.Length];
            int tileX = sourceTileX + offset.X;
            int tileY = sourceTileY + offset.Y;
            service._tilesScanned++;
            if (tileX < 0 || tileY < 0)
                continue;

            Vector2 tile = new(tileX, tileY);
            try
            {
                if (!location.isTileOnMap(tile) || !location.isTilePassable(tile))
                    continue;
                if (location.objects.ContainsKey(tile))
                    continue;

                // Grass and similar harmless TerrainFeatures are allowed here. Trees remain blocked.
                if (location.terrainFeatures.TryGetValue(tile, out var terrain)
                    && terrain is not null
                    && IsBlockingTerrainFeature(terrain.GetType().Name))
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            Vector2 candidate = new(tileX * 64f, tileY * 64f);
            Vector2 candidateCenter = candidate + new Vector2(32f, 32f);
            Vector2 farmerCenter = Game1.player.Position + new Vector2(32f, 32f);
            if (Vector2.DistanceSquared(candidateCenter, farmerCenter) < MinimumFarmerDistance * MinimumFarmerDistance)
                continue;

            bool occupied = location.characters
                .Where(character => !ReferenceEquals(character, anchor))
                .Any(character => Vector2.DistanceSquared(character.Position, candidate) < MinimumCharacterDistance * MinimumCharacterDistance);
            if (occupied)
                continue;

            position = candidate;
            return true;
        }

        position = Vector2.Zero;
        return false;
    }

    private static bool IsBlockingTerrainFeature(string typeName)
        => typeName.Contains("Tree", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("Bush", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("Stump", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("Log", StringComparison.OrdinalIgnoreCase);

    private static Point[] BuildSpawnOffsets()
    {
        List<Point> result = new();
        for (int radius = 2; radius <= 8; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                result.Add(new Point(x, -radius));
                result.Add(new Point(x, radius));
            }
            for (int y = -radius + 1; y <= radius - 1; y++)
            {
                result.Add(new Point(-radius, y));
                result.Add(new Point(radius, y));
            }
        }
        return result.Distinct().ToArray();
    }
}
