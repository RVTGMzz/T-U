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

    private const int InitialDiscoveryDelayTicks = 90;
    private const int LateSpawnRetryTicks = 60;
    private const int MaxDiscoveryAttempts = 5;

    private string _locationKey = string.Empty;
    private int _pendingTicks;
    private bool _applied;
    private int _discoveryAttempts;

    private int _lastRawCount;
    private int _lastBossExcluded;
    private int _lastProtectedExcluded;
    private int _lastPelipperSignals;
    private int _lastFactoryRejected;

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
        _discoveryAttempts = 0;
        UniversalMonsterDensitySpawnFactory.ResetTelemetry();
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
        _pendingTicks = InitialDiscoveryDelayTicks;
        _applied = false;
        _discoveryAttempts = 0;
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

        bool terminal = ApplyOnce(location);
        if (terminal)
        {
            _applied = true;
            return;
        }

        _discoveryAttempts++;
        if (_discoveryAttempts >= MaxDiscoveryAttempts)
        {
            _applied = true;
            _lastSuppressionReason = _lastPelipperSignals > 0
                ? "pelipper-only-source-owned-by-pelipper"
                : "late-spawn-timeout";
            LogTelemetry(location, Math.Clamp(_multiplier(), 1f, 2.5f));
            return;
        }

        _pendingTicks = LateSpawnRetryTicks;
        _lastSuppressionReason = _lastPelipperSignals > 0
            ? "pelipper-source-wait"
            : "late-spawn-retry";
        LogTelemetry(location, Math.Clamp(_multiplier(), 1f, 2.5f));
    }

    public string Describe()
        => $"Density: Enabled={_enabled()} | Multiplier={Math.Clamp(_multiplier(), 1f, 2.5f):0.00} | "
            + $"Location={_locationKey} | Applied={_applied} | Attempt={_discoveryAttempts}/{MaxDiscoveryAttempts} | "
            + $"Raw={_lastRawCount} | Eligible={_lastBaselineCount} | BossExcluded={_lastBossExcluded} | "
            + $"ProtectedExcluded={_lastProtectedExcluded} | PelipperSignals={_lastPelipperSignals} | Wanted={_lastWantedCount} | "
            + $"Spawned={_lastSpawnedCount} | FactoryRejected={_lastFactoryRejected} | UnsafeRejected={_lastUnsafeRejected} | "
            + $"SameType={UniversalMonsterDensitySpawnFactory.SameTypeSpawned} | VanillaFallback={UniversalMonsterDensitySpawnFactory.VanillaFallbackSpawned} | "
            + $"CustomRejected={UniversalMonsterDensitySpawnFactory.CustomRejected} | Threat={_lastThreatLevel} | Suppress={_lastSuppressionReason}";

    public IReadOnlyList<string> DescribeCurrentSources()
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return new[] { "Density sources unavailable until a save is loaded." };

        List<string> lines = new();
        foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>().Where(monster => monster.Health > 0))
        {
            string classification = ClassifyDensitySource(monster);
            lines.Add($"DensitySource {classification} name={monster.Name} type={monster.GetType().FullName} HP={monster.Health}/{monster.MaxHealth}");
        }

        if (lines.Count == 0)
            lines.Add("No live Monster actors in the current location.");
        return lines;
    }

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
        _discoveryAttempts = 0;
        ResetVisitTelemetry("debug-reapply");
        bool terminal = ApplyOnce(location);
        _applied = terminal;
        if (!terminal)
            _pendingTicks = LateSpawnRetryTicks;

        result = $"Density reapply: cleared={cleared} | {Describe()}";
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

    private bool ApplyOnce(GameLocation location)
    {
        // Cardcha's explicit test sandbox is never density-amplified. Normal Cardcha maps are.
        if (location.NameOrUniqueName.Equals(OptionalTestHostCompatibility.CardchaArenaLocationName, StringComparison.OrdinalIgnoreCase))
        {
            SetSuppressed(location, "cardcha-sandbox");
            return true;
        }

        List<Monster> raw = location.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .ToList();

        _lastRawCount = raw.Count;
        _lastBossExcluded = 0;
        _lastProtectedExcluded = 0;
        _lastPelipperSignals = 0;
        _lastFactoryRejected = 0;

        List<Monster> baseline = new();
        foreach (Monster monster in raw)
        {
            string classification = ClassifyDensitySource(monster);
            switch (classification)
            {
                case "ELIGIBLE":
                    baseline.Add(monster);
                    break;
                case "BOSS":
                    _lastBossExcluded++;
                    break;
                case "PELIPPER":
                    _lastPelipperSignals++;
                    break;
                case "PROTECTED":
                    _lastProtectedExcluded++;
                    break;
            }
        }

        _lastBaselineCount = baseline.Count;
        _lastWantedCount = 0;
        _lastSpawnedCount = 0;
        _lastUnsafeRejected = 0;

        // No map-name heuristic anymore. A real eligible hostile Monster is the combat-zone signal.
        // Empty/late-spawn maps are retried by Update() before the visit is sealed.
        if (baseline.Count == 0)
        {
            _lastThreatLevel = "LOW";
            _lastSuppressionReason = _lastPelipperSignals > 0
                ? "pelipper-source-owned-by-pelipper"
                : "no-eligible-density-source";
            return false;
        }

        float multiplier = Math.Clamp(_multiplier(), 1f, 2.5f);
        int targetTotal = Math.Max(baseline.Count,
            (int)Math.Round(baseline.Count * multiplier, MidpointRounding.AwayFromZero));
        int wanted = Math.Max(0, targetTotal - baseline.Count);
        int toSpawn = Math.Clamp(wanted, 0, Math.Clamp(_extraCap(), 0, 60));
        _lastWantedCount = toSpawn;

        if (toSpawn <= 0)
        {
            _lastThreatLevel = ResolveThreatLevel(baseline.Count, 0);
            _lastSuppressionReason = "spawn-budget-zero";
            RecordEncounter(location, baseline.Count, 0, _lastThreatLevel);
            LogTelemetry(location, multiplier);
            return true;
        }

        int spawned = 0;
        int unsafeRejected = 0;
        int factoryRejected = 0;

        for (int i = 0; i < toSpawn; i++)
        {
            Monster source = baseline[i % baseline.Count];
            if (!TryFindSafeSpawnPosition(location, source, i, out Vector2 position))
            {
                unsafeRejected++;
                continue;
            }

            if (!UniversalMonsterDensitySpawnFactory.TryCreate(source, position, out Monster? extra, out string mode)
                || extra is null)
            {
                factoryRejected++;
                continue;
            }

            extra.modData[SurgeMarker] = "1";
            extra.modData[SurgeSourceMarker] = $"{source.GetType().FullName ?? source.GetType().Name}|{mode}";
            if (!_fullLoot())
                SuppressKnownLootCollections(extra);

            location.characters.Add(extra);
            spawned++;
        }

        _lastSpawnedCount = spawned;
        _lastUnsafeRejected = unsafeRejected;
        _lastFactoryRejected = factoryRejected;
        _lastThreatLevel = ResolveThreatLevel(baseline.Count, spawned);
        _lastSuppressionReason = spawned == 0
            ? factoryRejected > 0 ? "no-safe-custom-constructor" : "no-safe-spawn-tile"
            : factoryRejected > 0 || unsafeRejected > 0
                ? "partial-density-placement"
                : "none";

        RecordEncounter(location, baseline.Count, spawned, _lastThreatLevel);
        LogTelemetry(location, multiplier);

        if (spawned > 0)
            Game1.showGlobalMessage($"MONSTER DENSITY • {_lastThreatLevel} • +{spawned}");

        return true;
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
            $"[DensityTelemetry] location={location.NameOrUniqueName} raw={_lastRawCount} eligible={_lastBaselineCount} "
            + $"bossExcluded={_lastBossExcluded} protectedExcluded={_lastProtectedExcluded} pelipperSignals={_lastPelipperSignals} "
            + $"wanted={_lastWantedCount} spawned={_lastSpawnedCount} factoryRejected={_lastFactoryRejected} unsafeRejected={_lastUnsafeRejected} "
            + $"total={_lastBaselineCount + _lastSpawnedCount} multiplier={multiplier:0.00} attempt={_discoveryAttempts}/{MaxDiscoveryAttempts} "
            + $"sameType={UniversalMonsterDensitySpawnFactory.SameTypeSpawned} vanillaFallback={UniversalMonsterDensitySpawnFactory.VanillaFallbackSpawned} "
            + $"customRejected={UniversalMonsterDensitySpawnFactory.CustomRejected} threat={_lastThreatLevel} suppression={_lastSuppressionReason}";
        LastTelemetryLine = line;
        _monitor.Log(line, LogLevel.Trace);
    }

    private void ResetVisitTelemetry(string reason)
    {
        _lastRawCount = 0;
        _lastBaselineCount = 0;
        _lastWantedCount = 0;
        _lastSpawnedCount = 0;
        _lastUnsafeRejected = 0;
        _lastBossExcluded = 0;
        _lastProtectedExcluded = 0;
        _lastPelipperSignals = 0;
        _lastFactoryRejected = 0;
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

    private static string ClassifyDensitySource(Monster monster)
    {
        if (IsSurgeMonster(monster)
            || MonsterMutationService.IsMutant(monster)
            || MonsterMutationService.IsMutationMinion(monster)
            || OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
        {
            return "PROTECTED";
        }

        // Pelipper owns its wild/capture/companion lifecycle. Those actors are useful telemetry
        // signals that combat exists, but Team Up never fabricates duplicate Pokemon/proxies.
        if (PelipperTownCompatibilityService.IsWildCombatActor(monster)
            || PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
        {
            return "PELIPPER";
        }

        if (IsBossLike(monster))
            return "BOSS";

        if (HasTruthyPolicyTag(monster, "scripted", "questprotected", "densityexcluded", "surgeexcluded", "mutationexcluded"))
            return "PROTECTED";

        return "ELIGIBLE";
    }

    private static bool IsBossLike(Monster monster)
    {
        string typeName = monster.GetType().Name;
        string fullTypeName = monster.GetType().FullName ?? typeName;
        string monsterName = monster.Name ?? string.Empty;
        if (typeName.Contains("Boss", StringComparison.OrdinalIgnoreCase)
            || fullTypeName.Contains(".Boss", StringComparison.OrdinalIgnoreCase)
            || monsterName.Contains("Boss", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return HasTruthyPolicyTag(monster, "boss", "mutationboss");
    }

    private static bool HasTruthyPolicyTag(Monster monster, params string[] tokens)
    {
        foreach (string key in monster.modData.Keys)
        {
            string value = monster.modData.TryGetValue(key, out string? rawValue) ? rawValue ?? string.Empty : string.Empty;
            string normalizedKey = Normalize(key);
            if (!tokens.Any(token => normalizedKey.Contains(Normalize(token), StringComparison.Ordinal)))
                continue;
            if (IsTruthy(value))
                return true;
        }
        return false;
    }

    private static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        string normalized = value.Trim().ToLowerInvariant();
        return normalized is "1" or "true" or "yes" or "on" or "enabled";
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

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
