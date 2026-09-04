$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$surge = Join-Path $root 'src\TeamUp\Combat\MonsterSurgeService.cs'
$debug = Join-Path $root 'src\TeamUp\Debugging\TeamUpDebugService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha653'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.5.3_SURGE_VALIDATION_HARNESS_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.5.3_SURGE_VALIDATION_HARNESS_TEST.sha256.txt'
$version = '0.2.0-alpha.6.5.3'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

try {
    foreach ($required in @($project, $manifest, $modEntry, $surge, $debug)) {
        if (-not (Test-Path $required)) { throw "Missing required Alpha 6.5.3 source: $required" }
    }

    $projectText = [System.IO.File]::ReadAllText($project, [System.Text.Encoding]::UTF8)
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    [System.IO.File]::WriteAllText($project, $projectText, $utf8NoBom)

    $modText = [System.IO.File]::ReadAllText($modEntry, [System.Text.Encoding]::UTF8)
    $modText = [regex]::Replace(
        $modText,
        'Team Up DEBUG HARNESS READY \| command: teamup_test \| build: v0\.2\.0-alpha\.6\.5\.\d+',
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.5.3')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.5\.\d+ [^\"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.5.3 Surge validation harness + 6.5.2 runtime polish + Origin/MiMi/Sudoku integration loaded.')
    [System.IO.File]::WriteAllText($modEntry, $modText, $utf8NoBom)

    $surgeText = [System.IO.File]::ReadAllText($surge, [System.Text.Encoding]::UTF8)
    $surgeText = $surgeText.Replace(
        '/// Alpha 6.5.2 Surge runtime overlay.',
        '/// Alpha 6.5.3 Surge validation harness over the Alpha 6.5.2 runtime overlay.')

    if (-not $surgeText.Contains('public static MonsterSurgeService? ActiveInstance')) {
        $anchor = '    public const string SurgeSourceMarker = "Ronvotri.TeamUp/SurgeSource";'
        $replacement = @'
    public const string SurgeSourceMarker = "Ronvotri.TeamUp/SurgeSource";

    // Alpha 6.5.3 exposes only the active Team Up Surge service to the developer harness.
    // This is runtime-only state and is never serialized into a save.
    public static MonsterSurgeService? ActiveInstance { get; private set; }
    public string LastTelemetryLine { get; private set; } = "[SurgeTelemetry] no-record";
'@
        $surgeText = $surgeText.Replace($anchor, $replacement.TrimEnd())
    }

    if (-not $surgeText.Contains('ActiveInstance = this;')) {
        $surgeText = $surgeText.Replace(
            '        _fullLoot = fullLoot;',
            "        _fullLoot = fullLoot;`n        ActiveInstance = this;")
    }

    if (-not $surgeText.Contains('public bool DebugReapplyCurrentLocation')) {
        $debugMethods = @'
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

'@
        $surgeText = $surgeText.Replace(
            '    public static bool IsSurgeMonster(Monster monster)',
            $debugMethods + '    public static bool IsSurgeMonster(Monster monster)')
    }

    if (-not $surgeText.Contains('private string BuildGuildThreatBrief()')) {
        $boardHelper = @'
    private string BuildGuildThreatBrief()
        => $"MARLON'S THREAT BOARD • {_recentThreatLevel} • {_recentEncounterLocation} • {_recentBaselineCount}->{_recentTotalCount}";

'@
        $surgeText = $surgeText.Replace(
            '    private void TryShowGuildThreatBrief()',
            $boardHelper + '    private void TryShowGuildThreatBrief()')
    }

    $oldBoard = @'
        _guildBriefShownSerial = _recentEncounterSerial;
        Game1.showGlobalMessage(
            $"MARLON'S THREAT BOARD • {_recentThreatLevel} • {_recentEncounterLocation} • {_recentBaselineCount}->{_recentTotalCount}");
'@
    $newBoard = @'
        _guildBriefShownSerial = _recentEncounterSerial;
        Game1.showGlobalMessage(BuildGuildThreatBrief());
'@
    if ($surgeText.Contains($oldBoard)) {
        $surgeText = $surgeText.Replace($oldBoard, $newBoard)
    }

    if (-not $surgeText.Contains('LastTelemetryLine = line;')) {
        $telemetryPattern = '    private void LogTelemetry\(GameLocation location, float multiplier\)\s*\{.*?\n    \}\r?\n\r?\n    private void ResetVisitTelemetry'
        $telemetryReplacement = @'
    private void LogTelemetry(GameLocation location, float multiplier)
    {
        string line =
            $"[SurgeTelemetry] location={location.NameOrUniqueName} baseline={_lastBaselineCount} wanted={_lastWantedCount} "
            + $"spawned={_lastSpawnedCount} unsafeRejected={_lastUnsafeRejected} total={_lastBaselineCount + _lastSpawnedCount} "
            + $"multiplier={multiplier:0.00} threat={_lastThreatLevel} suppression={_lastSuppressionReason}";
        LastTelemetryLine = line;
        _monitor.Log(line, LogLevel.Trace);
    }

    private void ResetVisitTelemetry
'@
        $updated = [regex]::Replace(
            $surgeText,
            $telemetryPattern,
            $telemetryReplacement,
            [System.Text.RegularExpressions.RegexOptions]::Singleline)
        if ($updated -eq $surgeText) { throw 'Could not patch Alpha 6.5.3 telemetry snapshot support.' }
        $surgeText = $updated
    }

    [System.IO.File]::WriteAllText($surge, $surgeText, $utf8NoBom)

    $debugText = [System.IO.File]::ReadAllText($debug, [System.Text.Encoding]::UTF8)

    if (-not $debugText.Contains('case "surge":')) {
        $switchAnchor = @'
        switch (action)
        {
            case "arena":
'@
        $switchReplacement = @'
        switch (action)
        {
            case "surge":
                CommandSurge(args);
                break;
            case "arena":
'@
        $debugText = $debugText.Replace($switchAnchor, $switchReplacement)
    }

    if (-not $debugText.Contains('teamup_test surge <status|reapply|clear|board>')) {
        $debugText = $debugText.Replace(
            '        Info("  teamup_test waves <start [easy|normal|hard]|stop|clear|status>");',
            "        Info(\"  teamup_test surge <status|reapply|clear|board>\");`n        Info(\"  teamup_test waves <start [easy|normal|hard]|stop|clear|status>\");")
    }

    if (-not $debugText.Contains('private void CommandSurge(string[] args)')) {
        $commandSurge = @'
    private void CommandSurge(string[] args)
    {
        MonsterSurgeService? surge = MonsterSurgeService.ActiveInstance;
        if (surge is null)
        {
            Info("Surge service is not initialized.");
            return;
        }

        string action = args.Length >= 2 ? args[1].Trim().ToLowerInvariant() : "status";
        switch (action)
        {
            case "status":
                Info(surge.Describe());
                Info(surge.LastTelemetryLine);
                Info($"Owned Surge monsters in current location: {surge.CountOwnedSurgeMonsters()}.");
                break;

            case "clear":
            {
                int cleared = surge.ClearOwnedSurgeMonsters();
                Info($"Cleared {cleared} Team Up Surge monster(s). Source/custom monsters were preserved.");
                break;
            }

            case "reapply":
                surge.DebugReapplyCurrentLocation(out string reapplyResult);
                Info(reapplyResult);
                break;

            case "board":
                surge.DebugShowThreatBoard(out string boardResult);
                Info(boardResult);
                break;

            default:
                Info("Usage: teamup_test surge <status|reapply|clear|board>");
                break;
        }
    }

'@
        $debugText = $debugText.Replace(
            '    private void CommandArena(string[] args)',
            $commandSurge + '    private void CommandArena(string[] args)')
    }

    if (-not $debugText.Contains('Surge status: service not initialized.')) {
        $debugText = $debugText.Replace(
            '        Info(_sandbox.Describe());',
            "        Info(_sandbox.Describe());`n        Info(MonsterSurgeService.ActiveInstance?.Describe() ?? \"Surge status: service not initialized.\");")
    }

    [System.IO.File]::WriteAllText($debug, $debugText, $utf8NoBom)

    foreach ($token in @(
        'ActiveInstance',
        'LastTelemetryLine',
        'CountOwnedSurgeMonsters',
        'ClearOwnedSurgeMonsters',
        'DebugReapplyCurrentLocation',
        'DebugShowThreatBoard',
        'BuildGuildThreatBrief',
        '.Where(IsSurgeMonster)',
        'ApplyOnce(location)',
        '[SurgeTelemetry]'
    )) {
        if (-not $surgeText.Contains($token)) { throw "Alpha 6.5.3 Surge harness token missing: $token" }
    }

    foreach ($token in @(
        'case "surge":',
        'CommandSurge(args)',
        'teamup_test surge <status|reapply|clear|board>',
        'MonsterSurgeService.ActiveInstance',
        'surge.ClearOwnedSurgeMonsters()',
        'surge.DebugReapplyCurrentLocation',
        'surge.DebugShowThreatBoard'
    )) {
        if (-not $debugText.Contains($token)) { throw "Alpha 6.5.3 debug command token missing: $token" }
    }

    Log 'Building Alpha 6.5.3 Surge Validation Harness...'
    Log 'Debug: teamup_test surge status/reapply/clear/board.'
    Log 'Safety: reapply clears Team Up-owned Surge extras before one fresh budget application.'
    Log 'Telemetry: last compact Surge telemetry line is retained for instant status inspection.'
    Log 'Regression: Alpha 6.5.2 safe placement/threat board plus MiMi/Sudoku/Origin/prior systems retained.'

    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    & dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    $dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' | Select-Object -First 1
    if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

    if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
    if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
    New-Item -ItemType Directory -Path $stageMod -Force | Out-Null

    Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force
    $manifestText = [System.IO.File]::ReadAllText($manifest, [System.Text.Encoding]::UTF8).Replace('%ProjectVersion%', $version)
    [System.IO.File]::WriteAllText((Join-Path $stageMod 'manifest.json'), $manifestText, $utf8NoBom)
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.5.3 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($shaPath, "$hash  $(Split-Path $zip -Leaf)`r`n", $utf8NoBom)

    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_5_3_SURGE_VALIDATION_HARNESS_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }

    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.5.3'
    Log 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.5.3'
    Log 'SURGE DEBUG: STATUS / REAPPLY / CLEAR / BOARD'
    Log 'REAPPLY SAFETY: CLEAR OWNED SURGE EXTRAS BEFORE FRESH APPLY'
    Log 'TELEMETRY SNAPSHOT: LAST [SurgeTelemetry] LINE AVAILABLE IN STATUS'
    Log 'REGRESSION: ALPHA 6.5.2 SAFE PLACEMENT/THREAT + 6.5.1 MIMI + 6.5.0 ORIGIN/SUDOKU RETAINED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
