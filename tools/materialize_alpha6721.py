from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if new in text:
        return text
    if old not in text:
        raise RuntimeError(f"{label}: anchor not found")
    return text.replace(old, new, 1)


def replace_between(text: str, start: str, end: str, replacement: str, label: str) -> str:
    a = text.find(start)
    if a < 0:
        raise RuntimeError(f"{label}: start anchor not found")
    b = text.find(end, a)
    if b < 0:
        raise RuntimeError(f"{label}: end anchor not found")
    return text[:a] + replacement + text[b:]


# Version.
csproj_path = SRC / "TeamUp.csproj"
csproj = csproj_path.read_text(encoding="utf-8")
csproj = replace_once(
    csproj,
    "<Version>0.2.0-alpha.6.7.20</Version>",
    "<Version>0.2.0-alpha.6.7.21</Version>",
    "TeamUp.csproj version",
)
csproj_path.write_text(csproj, encoding="utf-8", newline="\n")

# Density defaults: >2x by default and a larger bounded extra budget.
config_path = SRC / "ModConfig.cs"
config = config_path.read_text(encoding="utf-8")
config = replace_once(
    config,
    "public float MonsterDensityMultiplier { get; set; } = 2.0f;",
    "public float MonsterDensityMultiplier { get; set; } = 2.5f;",
    "density multiplier default",
)
config = replace_once(
    config,
    "public int MonsterSurgeExtraCap { get; set; } = 18;",
    "public int MonsterSurgeExtraCap { get; set; } = 36;",
    "density extra cap default",
)
config_path.write_text(config, encoding="utf-8", newline="\n")

# Clamp + command registration.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
entry = replace_once(
    entry,
    "Config.MonsterSurgeExtraCap = Math.Clamp(Config.MonsterSurgeExtraCap, 0, 30);",
    "Config.MonsterSurgeExtraCap = Math.Clamp(Config.MonsterSurgeExtraCap, 0, 60);",
    "density cap clamp",
)
entry = replace_once(
    entry,
    "        RegisterAlpha6719Events();\n",
    "        RegisterAlpha6719Events();\n        RegisterAlpha6721Events();\n",
    "Alpha 6.7.21 registration",
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

surge_path = SRC / "Combat" / "MonsterSurgeService.cs"
surge = surge_path.read_text(encoding="utf-8")

# New retry/telemetry fields.
surge = replace_once(
    surge,
    "    private string _locationKey = string.Empty;\n    private int _pendingTicks;\n    private bool _applied;\n",
    "    private const int InitialDiscoveryDelayTicks = 90;\n    private const int LateSpawnRetryTicks = 60;\n    private const int MaxDiscoveryAttempts = 5;\n\n    private string _locationKey = string.Empty;\n    private int _pendingTicks;\n    private bool _applied;\n    private int _discoveryAttempts;\n\n    private int _lastRawCount;\n    private int _lastBossExcluded;\n    private int _lastProtectedExcluded;\n    private int _lastPelipperSignals;\n    private int _lastFactoryRejected;\n",
    "density retry fields",
)

# Reset includes density-factory telemetry.
surge = replace_once(
    surge,
    "        _applied = false;\n        ResetVisitTelemetry(\"not-applied\");\n",
    "        _applied = false;\n        _discoveryAttempts = 0;\n        UniversalMonsterDensitySpawnFactory.ResetTelemetry();\n        ResetVisitTelemetry(\"not-applied\");\n",
    "density reset",
)

# Warps now wait a little longer and allow bounded late-spawn discovery.
surge = replace_once(
    surge,
    "        _locationKey = location.NameOrUniqueName;\n        _pendingTicks = 45;\n        _applied = false;\n        ResetVisitTelemetry(\"pending\");\n",
    "        _locationKey = location.NameOrUniqueName;\n        _pendingTicks = InitialDiscoveryDelayTicks;\n        _applied = false;\n        _discoveryAttempts = 0;\n        ResetVisitTelemetry(\"pending\");\n",
    "density warp discovery",
)

update_block = '''    public void Update()\n    {\n        if (!_enabled() || !Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)\n            return;\n\n        GameLocation location = Game1.currentLocation;\n        if (!_locationKey.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase))\n            OnWarped(location);\n\n        if (_applied || Game1.eventUp || Game1.activeClickableMenu is not null)\n            return;\n\n        if (_pendingTicks-- > 0)\n            return;\n\n        bool terminal = ApplyOnce(location);\n        if (terminal)\n        {\n            _applied = true;\n            return;\n        }\n\n        _discoveryAttempts++;\n        if (_discoveryAttempts >= MaxDiscoveryAttempts)\n        {\n            _applied = true;\n            _lastSuppressionReason = _lastPelipperSignals > 0\n                ? "pelipper-only-source-owned-by-pelipper"\n                : "late-spawn-timeout";\n            LogTelemetry(location, Math.Clamp(_multiplier(), 1f, 2.5f));\n            return;\n        }\n\n        _pendingTicks = LateSpawnRetryTicks;\n        _lastSuppressionReason = _lastPelipperSignals > 0\n            ? "pelipper-source-wait"\n            : "late-spawn-retry";\n        LogTelemetry(location, Math.Clamp(_multiplier(), 1f, 2.5f));\n    }\n\n'''
surge = replace_between(
    surge,
    "    public void Update()\n",
    "    public string Describe()\n",
    update_block,
    "density update retry",
)

describe_block = '''    public string Describe()\n        => $"Density: Enabled={_enabled()} | Multiplier={Math.Clamp(_multiplier(), 1f, 2.5f):0.00} | "\n            + $"Location={_locationKey} | Applied={_applied} | Attempt={_discoveryAttempts}/{MaxDiscoveryAttempts} | "\n            + $"Raw={_lastRawCount} | Eligible={_lastBaselineCount} | BossExcluded={_lastBossExcluded} | "\n            + $"ProtectedExcluded={_lastProtectedExcluded} | PelipperSignals={_lastPelipperSignals} | Wanted={_lastWantedCount} | "\n            + $"Spawned={_lastSpawnedCount} | FactoryRejected={_lastFactoryRejected} | UnsafeRejected={_lastUnsafeRejected} | "\n            + $"SameType={UniversalMonsterDensitySpawnFactory.SameTypeSpawned} | VanillaFallback={UniversalMonsterDensitySpawnFactory.VanillaFallbackSpawned} | "\n            + $"CustomRejected={UniversalMonsterDensitySpawnFactory.CustomRejected} | Threat={_lastThreatLevel} | Suppress={_lastSuppressionReason}";\n\n    public IReadOnlyList<string> DescribeCurrentSources()\n    {\n        if (!Context.IsWorldReady || Game1.currentLocation is null)\n            return new[] { "Density sources unavailable until a save is loaded." };\n\n        List<string> lines = new();\n        foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>().Where(monster => monster.Health > 0))\n        {\n            string classification = ClassifyDensitySource(monster);\n            lines.Add($"DensitySource {classification} name={monster.Name} type={monster.GetType().FullName} HP={monster.Health}/{monster.MaxHealth}");\n        }\n\n        if (lines.Count == 0)\n            lines.Add("No live Monster actors in the current location.");\n        return lines;\n    }\n\n'''
surge = replace_between(
    surge,
    "    public string Describe()\n",
    "    public int CountOwnedSurgeMonsters",
    describe_block,
    "density describe",
)

# Debug reapply respects late-spawn retry instead of permanently sealing an empty map.
surge = replace_once(
    surge,
    "        _pendingTicks = 0;\n        _applied = false;\n        ResetVisitTelemetry(\"debug-reapply\");\n        ApplyOnce(location);\n        _applied = true;\n\n        result = $\"Surge reapply complete: cleared={cleared} | {Describe()}\";\n",
    "        _pendingTicks = 0;\n        _applied = false;\n        _discoveryAttempts = 0;\n        ResetVisitTelemetry(\"debug-reapply\");\n        bool terminal = ApplyOnce(location);\n        _applied = terminal;\n        if (!terminal)\n            _pendingTicks = LateSpawnRetryTicks;\n\n        result = $\"Density reapply: cleared={cleared} | {Describe()}\";\n",
    "density debug reapply",
)

apply_block = '''    private bool ApplyOnce(GameLocation location)\n    {\n        // Cardcha's explicit test sandbox is never density-amplified. Normal Cardcha maps are.\n        if (location.NameOrUniqueName.Equals(OptionalTestHostCompatibility.CardchaArenaLocationName, StringComparison.OrdinalIgnoreCase))\n        {\n            SetSuppressed(location, "cardcha-sandbox");\n            return true;\n        }\n\n        List<Monster> raw = location.characters\n            .OfType<Monster>()\n            .Where(monster => monster.Health > 0)\n            .ToList();\n\n        _lastRawCount = raw.Count;\n        _lastBossExcluded = 0;\n        _lastProtectedExcluded = 0;\n        _lastPelipperSignals = 0;\n        _lastFactoryRejected = 0;\n\n        List<Monster> baseline = new();\n        foreach (Monster monster in raw)\n        {\n            string classification = ClassifyDensitySource(monster);\n            switch (classification)\n            {\n                case "ELIGIBLE":\n                    baseline.Add(monster);\n                    break;\n                case "BOSS":\n                    _lastBossExcluded++;\n                    break;\n                case "PELIPPER":\n                    _lastPelipperSignals++;\n                    break;\n                case "PROTECTED":\n                    _lastProtectedExcluded++;\n                    break;\n            }\n        }\n\n        _lastBaselineCount = baseline.Count;\n        _lastWantedCount = 0;\n        _lastSpawnedCount = 0;\n        _lastUnsafeRejected = 0;\n\n        // No map-name heuristic anymore. A real eligible hostile Monster is the combat-zone signal.\n        // Empty/late-spawn maps are retried by Update() before the visit is sealed.\n        if (baseline.Count == 0)\n        {\n            _lastThreatLevel = "LOW";\n            _lastSuppressionReason = _lastPelipperSignals > 0\n                ? "pelipper-source-owned-by-pelipper"\n                : "no-eligible-density-source";\n            return false;\n        }\n\n        float multiplier = Math.Clamp(_multiplier(), 1f, 2.5f);\n        int targetTotal = Math.Max(baseline.Count,\n            (int)Math.Round(baseline.Count * multiplier, MidpointRounding.AwayFromZero));\n        int wanted = Math.Max(0, targetTotal - baseline.Count);\n        int toSpawn = Math.Clamp(wanted, 0, Math.Clamp(_extraCap(), 0, 60));\n        _lastWantedCount = toSpawn;\n\n        if (toSpawn <= 0)\n        {\n            _lastThreatLevel = ResolveThreatLevel(baseline.Count, 0);\n            _lastSuppressionReason = "spawn-budget-zero";\n            RecordEncounter(location, baseline.Count, 0, _lastThreatLevel);\n            LogTelemetry(location, multiplier);\n            return true;\n        }\n\n        int spawned = 0;\n        int unsafeRejected = 0;\n        int factoryRejected = 0;\n\n        for (int i = 0; i < toSpawn; i++)\n        {\n            Monster source = baseline[i % baseline.Count];\n            if (!TryFindSafeSpawnPosition(location, source, i, out Vector2 position))\n            {\n                unsafeRejected++;\n                continue;\n            }\n\n            if (!UniversalMonsterDensitySpawnFactory.TryCreate(source, position, out Monster? extra, out string mode)\n                || extra is null)\n            {\n                factoryRejected++;\n                continue;\n            }\n\n            extra.modData[SurgeMarker] = "1";\n            extra.modData[SurgeSourceMarker] = $"{source.GetType().FullName ?? source.GetType().Name}|{mode}";\n            if (!_fullLoot())\n                SuppressKnownLootCollections(extra);\n\n            location.characters.Add(extra);\n            spawned++;\n        }\n\n        _lastSpawnedCount = spawned;\n        _lastUnsafeRejected = unsafeRejected;\n        _lastFactoryRejected = factoryRejected;\n        _lastThreatLevel = ResolveThreatLevel(baseline.Count, spawned);\n        _lastSuppressionReason = spawned == 0\n            ? factoryRejected > 0 ? "no-safe-custom-constructor" : "no-safe-spawn-tile"\n            : factoryRejected > 0 || unsafeRejected > 0\n                ? "partial-density-placement"\n                : "none";\n\n        RecordEncounter(location, baseline.Count, spawned, _lastThreatLevel);\n        LogTelemetry(location, multiplier);\n\n        if (spawned > 0)\n            Game1.showGlobalMessage($"MONSTER DENSITY • {_lastThreatLevel} • +{spawned}");\n\n        return true;\n    }\n\n'''
surge = replace_between(
    surge,
    "    private void ApplyOnce(GameLocation location)\n",
    "    private bool TryFindSafeSpawnPosition",
    apply_block,
    "universal density apply",
)

# Expanded telemetry.
surge = replace_between(
    surge,
    "    private void LogTelemetry(GameLocation location, float multiplier)\n",
    "    private void ResetVisitTelemetry",
    '''    private void LogTelemetry(GameLocation location, float multiplier)\n    {\n        string line =\n            $"[DensityTelemetry] location={location.NameOrUniqueName} raw={_lastRawCount} eligible={_lastBaselineCount} "\n            + $"bossExcluded={_lastBossExcluded} protectedExcluded={_lastProtectedExcluded} pelipperSignals={_lastPelipperSignals} "\n            + $"wanted={_lastWantedCount} spawned={_lastSpawnedCount} factoryRejected={_lastFactoryRejected} unsafeRejected={_lastUnsafeRejected} "\n            + $"total={_lastBaselineCount + _lastSpawnedCount} multiplier={multiplier:0.00} attempt={_discoveryAttempts}/{MaxDiscoveryAttempts} "\n            + $"sameType={UniversalMonsterDensitySpawnFactory.SameTypeSpawned} vanillaFallback={UniversalMonsterDensitySpawnFactory.VanillaFallbackSpawned} "\n            + $"customRejected={UniversalMonsterDensitySpawnFactory.CustomRejected} threat={_lastThreatLevel} suppression={_lastSuppressionReason}";\n        LastTelemetryLine = line;\n        _monitor.Log(line, LogLevel.Trace);\n    }\n\n''',
    "density telemetry",
)

surge = replace_between(
    surge,
    "    private void ResetVisitTelemetry(string reason)\n",
    "    private static string ResolveThreatLevel",
    '''    private void ResetVisitTelemetry(string reason)\n    {\n        _lastRawCount = 0;\n        _lastBaselineCount = 0;\n        _lastWantedCount = 0;\n        _lastSpawnedCount = 0;\n        _lastUnsafeRejected = 0;\n        _lastBossExcluded = 0;\n        _lastProtectedExcluded = 0;\n        _lastPelipperSignals = 0;\n        _lastFactoryRejected = 0;\n        _lastSuppressionReason = reason;\n        _lastThreatLevel = "LOW";\n    }\n\n''',
    "density telemetry reset",
)

# Replace legacy map-name heuristic with actor classification policy.
surge = replace_between(
    surge,
    "    private static bool LooksLikeCombatZone(GameLocation location)\n",
    "    private static void SuppressKnownLootCollections",
    '''    private static string ClassifyDensitySource(Monster monster)\n    {\n        if (IsSurgeMonster(monster)\n            || MonsterMutationService.IsMutant(monster)\n            || MonsterMutationService.IsMutationMinion(monster)\n            || OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))\n        {\n            return "PROTECTED";\n        }\n\n        // Pelipper owns its wild/capture/companion lifecycle. Those actors are useful telemetry\n        // signals that combat exists, but Team Up never fabricates duplicate Pokemon/proxies.\n        if (PelipperTownCompatibilityService.IsWildCombatActor(monster)\n            || PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))\n        {\n            return "PELIPPER";\n        }\n\n        if (IsBossLike(monster))\n            return "BOSS";\n\n        if (HasTruthyPolicyTag(monster, "scripted", "questprotected", "densityexcluded", "surgeexcluded", "mutationexcluded"))\n            return "PROTECTED";\n\n        return "ELIGIBLE";\n    }\n\n    private static bool IsBossLike(Monster monster)\n    {\n        string typeName = monster.GetType().Name;\n        string fullTypeName = monster.GetType().FullName ?? typeName;\n        string monsterName = monster.Name ?? string.Empty;\n        if (typeName.Contains("Boss", StringComparison.OrdinalIgnoreCase)\n            || fullTypeName.Contains(".Boss", StringComparison.OrdinalIgnoreCase)\n            || monsterName.Contains("Boss", StringComparison.OrdinalIgnoreCase))\n        {\n            return true;\n        }\n\n        return HasTruthyPolicyTag(monster, "boss", "mutationboss");\n    }\n\n    private static bool HasTruthyPolicyTag(Monster monster, params string[] tokens)\n    {\n        foreach (string key in monster.modData.Keys)\n        {\n            string value = monster.modData.TryGetValue(key, out string? rawValue) ? rawValue ?? string.Empty : string.Empty;\n            string normalizedKey = Normalize(key);\n            if (!tokens.Any(token => normalizedKey.Contains(Normalize(token), StringComparison.Ordinal)))\n                continue;\n            if (IsTruthy(value))\n                return true;\n        }\n        return false;\n    }\n\n    private static bool IsTruthy(string? value)\n    {\n        if (string.IsNullOrWhiteSpace(value))\n            return true;\n        string normalized = value.Trim().ToLowerInvariant();\n        return normalized is "1" or "true" or "yes" or "on" or "enabled";\n    }\n\n    private static string Normalize(string text)\n        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());\n\n''',
    "density classification policy",
)

surge_path.write_text(surge, encoding="utf-8", newline="\n")

print("Alpha 6.7.21 universal monster density source materialized.")
