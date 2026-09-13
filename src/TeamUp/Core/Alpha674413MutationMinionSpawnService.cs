using HarmonyLib;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.13: the strict minion spawn probe can reject every nearby tile on custom maps and
/// farms because CollisionMask.All treats harmless map/runtime occupancy as blocking. Preserve the
/// strict pass first, then provide a conservative fallback that still requires map passability,
/// no placed object/terrain feature, and clear distance from the Farmer and all characters.
/// </summary>
internal sealed class Alpha674413MutationMinionSpawnService
{
    private static readonly Point[] SpawnOffsets =
    {
        new(2, 0), new(-2, 0), new(0, 2), new(0, -2),
        new(2, 2), new(-2, 2), new(2, -2), new(-2, -2),
        new(3, 0), new(-3, 0), new(0, 3), new(0, -3),
        new(3, 1), new(-3, 1), new(3, -1), new(-3, -1),
        new(1, 3), new(-1, 3), new(1, -3), new(-1, -3),
        new(4, 0), new(-4, 0), new(0, 4), new(0, -4)
    };

    private const float MinimumFarmerDistance = 96f;
    private const float MinimumCharacterDistance = 48f;

    private static Alpha674413MutationMinionSpawnService? Active;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private long _fallbackAttempts;
    private long _fallbackResolved;
    private long _fallbackRejected;
    private string _last = "reset";

    public Alpha674413MutationMinionSpawnService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha674413MutationMinionSpawn");
        Active = this;

        var method = AccessTools.Method(typeof(MonsterMutationService), "TryFindSafeSpawnPosition");
        if (method is null)
        {
            _monitor.Log("6.7.44.13 minion spawn fallback unavailable: TryFindSafeSpawnPosition not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            method,
            postfix: new HarmonyMethod(typeof(Alpha674413MutationMinionSpawnService), nameof(SpawnPostfix))
            {
                priority = Priority.Last
            });
        _monitor.Log("Team Up 6.7.44.13 Mutation minion relaxed spawn fallback enabled.", LogLevel.Info);
    }

    public string Describe()
        => $"Mutation minion spawn fallback: attempts={_fallbackAttempts} | resolved={_fallbackResolved} | rejected={_fallbackRejected} | last={_last}";

    public void ResetTelemetry()
    {
        _fallbackAttempts = 0;
        _fallbackResolved = 0;
        _fallbackRejected = 0;
        _last = "reset";
    }

    private static void SpawnPostfix(GameLocation __0, Monster __1, int __2, ref Vector2 __3, ref bool __result)
    {
        Alpha674413MutationMinionSpawnService? service = Active;
        if (service is null || __result || !Context.IsWorldReady)
            return;

        service._fallbackAttempts++;
        if (!TryFindRelaxed(__0, __1, __2, out Vector2 position))
        {
            service._fallbackRejected++;
            service._last = $"rejected location={__0.NameOrUniqueName} anchor={__1.Name}";
            return;
        }

        __3 = position;
        __result = true;
        service._fallbackResolved++;
        service._last = $"resolved location={__0.NameOrUniqueName} tile={(int)(position.X / 64f)},{(int)(position.Y / 64f)}";
    }

    private static bool TryFindRelaxed(GameLocation location, Monster anchor, int seed, out Vector2 position)
    {
        int sourceTileX = (int)Math.Floor((anchor.Position.X + 32f) / 64f);
        int sourceTileY = (int)Math.Floor((anchor.Position.Y + 32f) / 64f);
        int start = Math.Abs(seed * 11 + sourceTileX * 5 + sourceTileY * 7) % SpawnOffsets.Length;

        for (int attempt = 0; attempt < SpawnOffsets.Length; attempt++)
        {
            Point offset = SpawnOffsets[(start + attempt) % SpawnOffsets.Length];
            int tileX = sourceTileX + offset.X;
            int tileY = sourceTileY + offset.Y;
            if (tileX < 0 || tileY < 0)
                continue;

            Vector2 tile = new(tileX, tileY);
            try
            {
                if (!location.isTileOnMap(tile) || !location.isTilePassable(tile))
                    continue;
                if (location.objects.ContainsKey(tile) || location.terrainFeatures.ContainsKey(tile))
                    continue;
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
}
