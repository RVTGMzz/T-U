using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 6.5.3 Surge validation harness over the Alpha 6.5.2 runtime overlay.
/// Adds bounded safe monsters around existing combat pressure without cloning unknown entities.
/// Placement fails closed when no clear tile exists, and every visit records compact telemetry.
/// </summary>
public sealed class MonsterSurgeService
{
    public const string SurgeMarker = "Ronvotri.TeamUp/SurgeSpawn";
    public const string SurgeSourceMarker = "Ronvotri.TeamUp/SurgeSource";

    // Alpha 6.5.3 exposes only the active Team Up Surge service to the developer harness.
    // This is runtime-only state and is never serialized into a save.
    public static MonsterSurgeService? ActiveInstance { get; private set; }
    public string LastTelemetryLine { get; private set; } = "[SurgeTelemetry] no-record";

    private static readonly Point[] SafeSpawnOffsets =
    {
        new(2, 0), new(-2, 0), new(0, 2), new(0, -2),
        new(2, 2), new(-2, 2), new(2, -2), new(-2, -2),
        new(3, 0), new(-3, 0), new(0, 3), new(0, -3),
        new(3, 1), new(-3, 1), new(3, -1), new(-3, -1),
        new(1, 3), new(-1, 3), new(1, -3), new(-1, -3)
    };

    private const float MinimumFarmerSpawnDistance = 128f;
    private const float MinimumMonsterSpawnDistance = 56f;

    private readonly IMonitor _monitor;
    private readonly Func<bool> _enabled;
    private readonly Func<float> _multiplier;
    private readonly Func<int> _extraCap;
    private readonly Func<bool> _fullLoot;

    private string _locationKey = string.Empty;
    private int _pendingTicks;
    private bool _applied;

    private int _lastBaselineCount;
    private int _lastWantedCount;
    private int _lastSpawnedCount;
    private int _lastUnsafeRejected;
    private string _lastSuppressionReason = "not-applied";
    private string _lastThreatLevel = "LOW";

    private int _recentEncounterSerial;
    private int _guildBriefShownSerial;
    private string _recentEncounterLocation = string.Empty;
    private int _recentBaselineCount;
    private int _recentSpawnedCount;
    private int _recentTotalCount;
    private string _recentThreatLevel = "LOW";

    public MonsterSurgeService(
        IMonitor monitor,
        Func<bool> enabled,
        Func<float> multiplier,
        Func<int> extraCap,
        Func<bool> fullLoot)
    {
        _monitor = monitor;
        _enabled = enabled;
        _multiplier = multiplier;
        _extraCap = extraCap;
        _fullLoot = fullLoot;
        ActiveInstance = this;
    }

    public void Reset()
    {
        _locationKey = string.Empty;
        _pendingTicks = 0;
        _applied = false;
        ResetVisitTelemetry("not-applied");

        _recentEncounterSerial = 0;
        _guildBriefShownSerial = 0;
        _recentEncounterLocation = string.Empty;
        _recentBaselineCount = 0;
        _recentSpawnedCount = 0;
        _recentTotalCount = 0;
        _recentThreatLevel = "LOW";
    }

    public void OnWarped(GameLocation location)
    {
        if (location.NameOrUniqueName.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase))
            TryShowGuildThreatBrief();

        _locationKey = location.NameOrUniqueName;
        _pendingTicks = 45;
        _applied = false;
        ResetVisitTelemetry("pending");
    }

    public void Update()
    {
        if (!_enabled() || !Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
            return;

        GameLocation location = Game1.currentLocation;
        if (!_locationKey.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase))
            OnWarped(location);

        if (_applied || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        if (_pendingTicks-- > 0)
            return;

        ApplyOnce(location);
        _applied = true;
    }

    public string Describe()
        => $"Surge: Enabled={_enabled()} | Multiplier={Math.Clamp(_multiplier(), 1f, 2.5f):0.00} | "
            + $"Location={_locationKey} | Applied={_applied} | Baseline={_lastBaselineCount} | Wanted={_lastWantedCount} | "
            + $"Spawned={_lastSpawnedCount} | UnsafeRejected={_lastUnsafeRejected} | Threat={_lastThreatLevel} | Suppress={_lastSuppressionReason}";

    public int CountOwnedSurgeMonsters(GameLocation? location = null)
    {
        location ??= Game1.currentLocation;
        return location?.characters.OfType<Monster>().Count(IsSurgeMonster) ?? 0;
    }

    public int ClearOwnedSurgeMonsters(GameLocation? location = null)
    {
        location ??= Game1.currentLocation;
        if (location is null)
            return 0;

        List<Monster> owned = location.characters
            .OfType<Monster>()
            .Where(IsSurgeMonster)
            .ToList();

        foreach (Monster monster in owned)
            location.characters.Remove(monster);

        return owned.Count;
    }

    public bool DebugReapplyCurrentLocation(out string result)
    {
        if (!_enabled())
        {
            result = "Surge is disabled in config.";
            return false;
        }

        if (!Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
        {
            result = "Load a save as the main player before reapplying The Surge.";
            return false;
        }

        if (Game1.eventUp || Game1.dialogueUp || Game1.activeClickableMenu is not null)
        {
            result = "Surge reapply blocked while an event, dialogue, or menu owns presentation.";
            return false;
        }

        GameLocation location = Game1.currentLocation;
        int cleared = ClearOwnedSurgeMonsters(location);

        _locationKey = location.NameOrUniqueName;
        _pendingTicks = 0;
        _applied = false;
        ResetVisitTelemetry("debug-reapply");
        ApplyOnce(location);
        _applied = true;

        result = $"Surge reapply complete: cleared={cleared} | {Describe()}";
        return true;
    }

    public bool DebugShowThreatBoard(out string result)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
        {
            result = "Load a save as the main player before testing Marlon's Threat Board.";
            return false;
        }

        if (!Game1.currentLocation.NameOrUniqueName.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase))
        {
            result = "Threat Board debug display is only available inside AdventureGuild.";
            return false;
        }

        if (Game1.eventUp || Game1.dialogueUp || Game1.activeClickableMenu is not null)
        {
            result = "Threat Board debug display blocked while an event, dialogue, or menu owns presentation.";
            return false;
        }

        if (_recentEncounterSerial <= 0)
        {
            result = "No Surge encounter has been recorded yet.";
            return false;
        }

        string message = BuildGuildThreatBrief();
        _guildBriefShownSerial = _recentEncounterSerial;
        Game1.showGlobalMessage(message);
        result = message;
        return true;
    }
    public static bool IsSurgeMonster(Monster monster)
        => monster.modData.ContainsKey(SurgeMarker);

    private void ApplyOnce(GameLocation location)
    {
        if (!LooksLikeCombatZone(location))
        {
            SetSuppressed(location, "not-combat-zone");
            return;
        }

        if (location.NameOrUniqueName.Equals(OptionalTestHostCompatibility.CardchaArenaLocationName, StringComparison.OrdinalIgnoreCase))
        {
            SetSuppressed(location, "cardcha-sandbox");
            return;
        }

        List<Monster> baseline = location.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !IsSurgeMonster(monster))
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();

        _lastBaselineCount = baseline.Count;
        if (baseline.Count == 0)
        {
            SetSuppressed(location, "no-baseline-monsters");
            return;
        }

        float multiplier = Math.Clamp(_multiplier(), 1f, 2.5f);
        int wanted = (int)Math.Round(baseline.Count * (multiplier - 1f), MidpointRounding.AwayFromZero);
        int toSpawn = Math.Clamp(wanted, 0, Math.Clamp(_extraCap(), 0, 30));
        _lastWantedCount = toSpawn;

        if (toSpawn <= 0)
        {
            _lastThreatLevel = ResolveThreatLevel(baseline.Count, 0);
            _lastSuppressionReason = "spawn-budget-zero";
            RecordEncounter(location, baseline.Count, 0, _lastThreatLevel);
            LogTelemetry(location, multiplier);
            return;
        }

        int averageHealth = (int)Math.Round(baseline.Average(monster => (double)Math.Max(1, monster.MaxHealth)));
        int surgeHealth = Math.Clamp((int)Math.Round(averageHealth * 0.82f), 36, 220);
        int spawned = 0;
        int unsafeRejected = 0;

        for (int i = 0; i < toSpawn; i++)
        {
            Monster source = baseline[i % baseline.Count];
            if (!TryFindSafeSpawnPosition(location, source, i, out Vector2 position))
            {
                unsafeRejected++;
                continue;
            }

            int mineLevel = Math.Clamp(20 + averageHealth / 3, 20, 100);
            GreenSlime extra = new(position, mineLevel)
            {
                MaxHealth = surgeHealth,
                Health = surgeHealth,
                Speed = Math.Clamp(source.Speed, 2, 5)
            };
            extra.modData[SurgeMarker] = "1";
            extra.modData[SurgeSourceMarker] = source.GetType().FullName ?? source.GetType().Name;
            if (!_fullLoot())
                SuppressKnownLootCollections(extra);

            location.characters.Add(extra);
            spawned++;
        }

        _lastSpawnedCount = spawned;
        _lastUnsafeRejected = unsafeRejected;
        _lastThreatLevel = ResolveThreatLevel(baseline.Count, spawned);
        _lastSuppressionReason = spawned == 0
            ? "no-safe-spawn-tile"
            : unsafeRejected > 0
                ? "partial-safe-placement"
                : "none";

        RecordEncounter(location, baseline.Count, spawned, _lastThreatLevel);
        LogTelemetry(location, multiplier);

        if (spawned > 0)
            Game1.showGlobalMessage($"THE SURGE • {_lastThreatLevel} • +{spawned} MONSTERS");
    }

    private bool TryFindSafeSpawnPosition(GameLocation location, Monster source, int seed, out Vector2 position)
    {
        int sourceTileX = (int)Math.Floor((source.Position.X + 32f) / 64f);
        int sourceTileY = (int)Math.Floor((source.Position.Y + 32f) / 64f);

        int start = Math.Abs(seed * 7 + sourceTileX * 3 + sourceTileY * 5) % SafeSpawnOffsets.Length;
        for (int attempt = 0; attempt < SafeSpawnOffsets.Length; attempt++)
        {
            Point offset = SafeSpawnOffsets[(start + attempt) % SafeSpawnOffsets.Length];
            int tileX = sourceTileX + offset.X;
            int tileY = sourceTileY + offset.Y;
            if (!IsSafeSpawnTile(location, tileX, tileY))
                continue;

            Vector2 candidate = new(tileX * 64f, tileY * 64f);
            Vector2 candidateCenter = candidate + new Vector2(32f, 32f);
            Vector2 farmerCenter = Game1.player.Position + new Vector2(32f, 32f);
            if (Vector2.DistanceSquared(candidateCenter, farmerCenter) < MinimumFarmerSpawnDistance * MinimumFarmerSpawnDistance)
                continue;

            bool overlapsMonsterPressure = location.characters
                .OfType<Monster>()
                .Where(monster => monster.Health > 0)
                .Any(monster => Vector2.DistanceSquared(monster.Position, candidate) < MinimumMonsterSpawnDistance * MinimumMonsterSpawnDistance);
            if (overlapsMonsterPressure)
                continue;

            position = candidate;
            return true;
        }

        position = Vector2.Zero;
        return false;
    }

    private static bool IsSafeSpawnTile(GameLocation location, int tileX, int tileY)
    {
        if (tileX < 0 || tileY < 0)
            return false;

        try
        {
            Vector2 tile = new(tileX, tileY);
            return location.isTileOnMap(tile)
                && location.isTilePassable(tile)
                && !location.IsTileBlockedBy(tile, ignorePassables: CollisionMask.All);
        }
        catch
        {
            // Unknown map implementations fail closed. A skipped Surge monster is safer
            // than creating an actor inside invalid collision geometry.
            return false;
        }
    }

    private void SetSuppressed(GameLocation location, string reason)
    {
        _lastSuppressionReason = reason;
        _lastThreatLevel = _lastBaselineCount > 0 ? ResolveThreatLevel(_lastBaselineCount, 0) : "LOW";
        LogTelemetry(location, Math.Clamp(_multiplier(), 1f, 2.5f));
    }

    private void RecordEncounter(GameLocation location, int baseline, int spawned, string threat)
    {
        _recentEncounterSerial++;
        _recentEncounterLocation = location.NameOrUniqueName;
        _recentBaselineCount = baseline;
        _recentSpawnedCount = spawned;
        _recentTotalCount = baseline + spawned;
        _recentThreatLevel = threat;
    }

    private string BuildGuildThreatBrief()
        => $"MARLON'S THREAT BOARD • {_recentThreatLevel} • {_recentEncounterLocation} • {_recentBaselineCount}->{_recentTotalCount}";
    private void TryShowGuildThreatBrief()
    {
        if (_recentEncounterSerial <= 0
            || _guildBriefShownSerial == _recentEncounterSerial
            || !Context.IsWorldReady
            || Game1.eventUp
            || Game1.dialogueUp
            || Game1.activeClickableMenu is not null)
        {
            return;
        }

        _guildBriefShownSerial = _recentEncounterSerial;
        Game1.showGlobalMessage(BuildGuildThreatBrief());
    }

    private void LogTelemetry(GameLocation location, float multiplier)
    {
        string line =
            $"[SurgeTelemetry] location={location.NameOrUniqueName} baseline={_lastBaselineCount} wanted={_lastWantedCount} "
            + $"spawned={_lastSpawnedCount} unsafeRejected={_lastUnsafeRejected} total={_lastBaselineCount + _lastSpawnedCount} "
            + $"multiplier={multiplier:0.00} threat={_lastThreatLevel} suppression={_lastSuppressionReason}";
        LastTelemetryLine = line;
        _monitor.Log(line, LogLevel.Trace);
    }

    private void ResetVisitTelemetry(string reason)
    {
        _lastBaselineCount = 0;
        _lastWantedCount = 0;
        _lastSpawnedCount = 0;
        _lastUnsafeRejected = 0;
        _lastSuppressionReason = reason;
        _lastThreatLevel = "LOW";
    }

    private static string ResolveThreatLevel(int baseline, int spawned)
    {
        int total = baseline + spawned;
        if (spawned >= 8 || total >= 14)
            return "SURGE";
        if (spawned >= 4 || total >= 9)
            return "HIGH";
        if (spawned > 0 || total >= 5)
            return "ELEVATED";
        return "LOW";
    }

    private static bool LooksLikeCombatZone(GameLocation location)
    {
        if (location is MineShaft)
            return true;

        string name = location.NameOrUniqueName.ToLowerInvariant();
        string[] combatTokens =
        {
            "mine", "cave", "cavern", "dungeon", "volcano", "skull", "quarry",
            "highland", "badland", "combat", "monster", "lair", "depth"
        };
        return combatTokens.Any(name.Contains);
    }

    private static void SuppressKnownLootCollections(Monster monster)
    {
        // Reflection keeps this point-release/mod compatible. If a field/property doesn't exist,
        // Team Up simply leaves it alone instead of depending on private monster internals.
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        foreach (string memberName in new[] { "objectsToDrop", "itemsToDrop", "ObjectsToDrop", "ItemsToDrop" })
        {
            try
            {
                object? value = monster.GetType().GetField(memberName, flags)?.GetValue(monster)
                    ?? monster.GetType().GetProperty(memberName, flags)?.GetValue(monster);
                if (value is IList list)
                    list.Clear();
            }
            catch
            {
                // Economy guard is best-effort. Never fail combat because a modded monster uses
                // a different reward representation.
            }
        }
    }
}
