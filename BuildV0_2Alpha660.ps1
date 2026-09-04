$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$config = Join-Path $root 'src\TeamUp\ModConfig.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$strategy = Join-Path $root 'src\TeamUp\Core\PartyStrategy.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha660'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.0_PARTY_STRATEGY_FOUNDATION_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.0_PARTY_STRATEGY_FOUNDATION_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.0'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

try {
    foreach ($required in @($project, $manifest, $modEntry, $config, $combat, $strategy)) {
        if (-not (Test-Path $required)) { throw "Missing required Alpha 6.6.0 source: $required" }
    }

    $projectText = [System.IO.File]::ReadAllText($project, [System.Text.Encoding]::UTF8)
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    [System.IO.File]::WriteAllText($project, $projectText, $utf8NoBom)

    $configText = [System.IO.File]::ReadAllText($config, [System.Text.Encoding]::UTF8)
    if (-not $configText.Contains('using Ronvotri.TeamUp.Core;')) {
        $configText = $configText.Replace('using StardewModdingAPI.Utilities;', "using StardewModdingAPI.Utilities;`nusing Ronvotri.TeamUp.Core;")
    }
    if (-not $configText.Contains('public PartyStrategy PartyStrategy')) {
        $anchor = '    // Farmer-owned/special companions bypass Main Party recruitment entirely.'
        $insert = @'
    // Alpha 6.6.0: party-wide tactical posture. This is config-backed so changing strategy
    // never migrates or mutates PartySaveData.
    public PartyStrategy PartyStrategy { get; set; } = PartyStrategy.Balanced;

    // Farmer-owned/special companions bypass Main Party recruitment entirely.
'@
        $configText = $configText.Replace($anchor, $insert.TrimEnd())
    }
    [System.IO.File]::WriteAllText($config, $configText, $utf8NoBom)

    $combatText = [System.IO.File]::ReadAllText($combat, [System.Text.Encoding]::UTF8)

    if (-not $combatText.Contains('private readonly Func<PartyStrategy> _strategy;')) {
        $combatText = $combatText.Replace(
            '    private readonly ProgressionService _progression;',
            "    private readonly ProgressionService _progression;`n    private readonly Func<PartyStrategy> _strategy;")
    }

    $combatText = $combatText.Replace(
        'public CombatService(IMonitor monitor, FollowService follow, ProgressionService progression)',
        'public CombatService(IMonitor monitor, FollowService follow, ProgressionService progression, Func<PartyStrategy> strategy)')

    if (-not $combatText.Contains('_strategy = strategy;')) {
        $combatText = $combatText.Replace(
            '        _progression = progression;',
            "        _progression = progression;`n        _strategy = strategy;")
    }

    if (-not $combatText.Contains('public PartyStrategy CurrentStrategy')) {
        $anchor = '    public void Clear()'
        $insert = @'
    public PartyStrategy CurrentStrategy => _strategy();

    public string DescribeStrategy()
        => $"Party Strategy: {CurrentStrategy} | Radius x{GetStrategyEngagementRadiusMultiplier(CurrentStrategy):0.00} | AttackCD x{GetStrategyAttackCooldownMultiplier(CurrentStrategy):0.00}";

    public void Clear()
'@
        $combatText = $combatText.Replace($anchor, $insert.TrimEnd())
    }

    $combatText = $combatText.Replace(
        '            float radius = GetEngagementRadius(member.Engagement);',
        '            float radius = GetEngagementRadius(member.Engagement) * GetStrategyEngagementRadiusMultiplier(_strategy());')

    if (-not $combatText.Contains('PartyStrategy.HoldPosition)')) {
        throw 'PartyStrategy source was not materialized as expected.'
    }

    $passiveAnchor = @'
        if (member.Engagement == EngagementStyle.Passive)
            candidates = candidates.Where(monster => Vector2.Distance(monster.Tile, farmerTile) <= 2.75f).ToList();
        if (candidates.Count == 0)
'@
    if (-not $combatText.Contains('Vector2.Distance(monster.Tile, npc.Tile) <= 4.5f')) {
        $passiveReplace = @'
        if (member.Engagement == EngagementStyle.Passive)
            candidates = candidates.Where(monster => Vector2.Distance(monster.Tile, farmerTile) <= 2.75f).ToList();
        if (_strategy() == PartyStrategy.HoldPosition)
            candidates = candidates.Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 4.5f).ToList();
        if (candidates.Count == 0)
'@
        $combatText = $combatText.Replace($passiveAnchor, $passiveReplace)
    }

    $moveAnchor = @'
            if (distanceToTarget > attackRange)
            {
                MoveTowardTarget(npc, target, role);
                continue;
            }
'@
    if (-not $combatText.Contains('HOLD POSITION')) {
        $moveReplace = @'
            if (distanceToTarget > attackRange)
            {
                if (_strategy() == PartyStrategy.HoldPosition)
                {
                    npc.controller = null;
                    npc.temporaryController = null;
                    npc.Halt();
                    if (!_lastTargetTiles.ContainsKey(member.CharacterName))
                        npc.showTextAboveHead("HOLD POSITION", new Color(150, 210, 255), 2, 850, 0);
                    _lastTargetTiles[member.CharacterName] = npc.Tile;
                    continue;
                }

                MoveTowardTarget(npc, target, role);
                continue;
            }
'@
        $combatText = $combatText.Replace($moveAnchor, $moveReplace)
    }

    $combatText = $combatText.Replace(
        '            cooldown = Math.Max(12, (int)Math.Round(cooldown * _progression.GetCooldownMultiplier(member, role)));',
        '            cooldown = Math.Max(12, (int)Math.Round(cooldown * _progression.GetCooldownMultiplier(member, role) * GetStrategyAttackCooldownMultiplier(_strategy())));')

    $combatText = $combatText.Replace(
        '        Monster? chosen = candidates.OrderBy(Score).FirstOrDefault();',
        '        Monster? chosen = _strategy() == PartyStrategy.BossFocus`n            ? candidates.OrderByDescending(monster => monster.MaxHealth).ThenBy(Score).FirstOrDefault()`n            : candidates.OrderBy(Score).FirstOrDefault();'.Replace('`n', [Environment]::NewLine))

    $healAnchor = @'
        if (farmerPressure >= 2)
            farmerThreshold = Math.Min(0.90f, farmerThreshold + 0.10f);
        farmerThreshold = Math.Clamp(
'@
    if (-not $combatText.Contains('_strategy() == PartyStrategy.Defensive')) {
        $healReplace = @'
        if (farmerPressure >= 2)
            farmerThreshold = Math.Min(0.90f, farmerThreshold + 0.10f);
        if (_strategy() == PartyStrategy.Defensive)
            farmerThreshold = Math.Min(0.95f, farmerThreshold + 0.10f);
        farmerThreshold = Math.Clamp(
'@
        $combatText = $combatText.Replace($healAnchor, $healReplace)
    }

    if (-not $combatText.Contains('GetStrategyEngagementRadiusMultiplier')) {
        throw 'Strategy radius hook missing after patch.'
    }
    if (-not $combatText.Contains('private static float GetStrategyAttackCooldownMultiplier')) {
        $anchor = '    private static float GetEngagementRadius(EngagementStyle style)'
        $helpers = @'
    private static float GetStrategyEngagementRadiusMultiplier(PartyStrategy strategy)
    {
        return strategy switch
        {
            PartyStrategy.Defensive => 0.78f,
            PartyStrategy.Aggressive => 1.18f,
            PartyStrategy.HoldPosition => 0.70f,
            PartyStrategy.BossFocus => 1.00f,
            _ => 1.00f
        };
    }

    private static float GetStrategyAttackCooldownMultiplier(PartyStrategy strategy)
    {
        return strategy switch
        {
            PartyStrategy.Defensive => 1.08f,
            PartyStrategy.Aggressive => 0.88f,
            PartyStrategy.BossFocus => 0.96f,
            _ => 1.00f
        };
    }

    private static float GetEngagementRadius(EngagementStyle style)
'@
        $combatText = $combatText.Replace($anchor, $helpers.TrimEnd())
    }

    [System.IO.File]::WriteAllText($combat, $combatText, $utf8NoBom)

    $modText = [System.IO.File]::ReadAllText($modEntry, [System.Text.Encoding]::UTF8)
    $modText = [regex]::Replace(
        $modText,
        'Team Up DEBUG HARNESS READY \| command: teamup_test \| build: v0\.2\.0-alpha\.6\.[0-9.]+',
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.0')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.[0-9.]+ [^\"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.0 Party Strategy foundation + Surge/Origin/MiMi/Sudoku integration loaded.')

    if (-not $modText.Contains('Enum.IsDefined(typeof(PartyStrategy)')) {
        $modText = $modText.Replace(
            '        Config.MonsterSurgeExtraCap = Math.Clamp(Config.MonsterSurgeExtraCap, 0, 30);',
            "        Config.MonsterSurgeExtraCap = Math.Clamp(Config.MonsterSurgeExtraCap, 0, 30);`n        if (!Enum.IsDefined(typeof(PartyStrategy), Config.PartyStrategy))`n            Config.PartyStrategy = PartyStrategy.Balanced;")
    }

    $modText = $modText.Replace(
        '        Combat = new CombatService(Monitor, Follow, Progression);',
        '        Combat = new CombatService(Monitor, Follow, Progression, () => Config.PartyStrategy);')

    if (-not $modText.Contains('teamup_strategy')) {
        $modText = $modText.Replace(
            '        DebugTools.RegisterCommands();',
            "        DebugTools.RegisterCommands();`n        helper.ConsoleCommands.Add(\"teamup_strategy\", \"Set Team Up party strategy: status|balanced|defensive|aggressive|hold|boss.\", OnStrategyCommand);".Replace('\"','"'))
    }

    if (-not $modText.Contains('private void OnStrategyCommand')) {
        $method = @'
    private void OnStrategyCommand(string command, string[] args)
    {
        string raw = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (raw == "status")
        {
            Monitor.Log(Combat.DescribeStrategy(), LogLevel.Info);
            if (Context.IsWorldReady)
                Game1.showGlobalMessage(Combat.DescribeStrategy());
            return;
        }

        PartyStrategy? next = raw switch
        {
            "balanced" or "balance" => PartyStrategy.Balanced,
            "defensive" or "defense" => PartyStrategy.Defensive,
            "aggressive" or "attack" => PartyStrategy.Aggressive,
            "hold" or "holdposition" or "hold-position" => PartyStrategy.HoldPosition,
            "boss" or "bossfocus" or "boss-focus" => PartyStrategy.BossFocus,
            _ => null
        };

        if (next is null)
        {
            Monitor.Log("Usage: teamup_strategy <status|balanced|defensive|aggressive|hold|boss>", LogLevel.Info);
            return;
        }

        Config.PartyStrategy = next.Value;
        Helper.WriteConfig(Config);
        Combat.Clear();
        string message = $"TEAM STRATEGY • {next.Value.ToString().ToUpperInvariant()}";
        Monitor.Log($"Party strategy changed to {next.Value}. Combat runtime locks cleared for clean retargeting.", LogLevel.Info);
        if (Context.IsWorldReady)
            Game1.showGlobalMessage(message);
    }

'@
        $modText = $modText.Replace(
            '    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)',
            $method + '    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)')
    }

    [System.IO.File]::WriteAllText($modEntry, $modText, $utf8NoBom)

    foreach ($token in @('PartyStrategy', 'Defensive', 'Aggressive', 'HoldPosition', 'BossFocus')) {
        if (-not ([System.IO.File]::ReadAllText($strategy).Contains($token))) { throw "Strategy enum token missing: $token" }
    }
    foreach ($token in @('GetStrategyEngagementRadiusMultiplier', 'GetStrategyAttackCooldownMultiplier', 'PartyStrategy.HoldPosition', 'PartyStrategy.BossFocus', 'PartyStrategy.Defensive', 'PartyStrategy.Aggressive', 'HOLD POSITION')) {
        if (-not $combatText.Contains($token)) { throw "Combat strategy hook missing: $token" }
    }
    foreach ($token in @('teamup_strategy', 'OnStrategyCommand', 'Combat.DescribeStrategy()', 'Config.PartyStrategy = next.Value', 'Helper.WriteConfig(Config)')) {
        if (-not $modText.Contains($token)) { throw "Strategy command hook missing: $token" }
    }

    Log 'Building Alpha 6.6.0 Party Strategy Foundation...'
    Log 'Strategies: Balanced / Defensive / Aggressive / Hold Position / Boss Focus.'
    Log 'Combat hooks: engagement radius, attack cadence, heal urgency, hold chase lock, boss HP priority.'
    Log 'Persistence: config-backed strategy only; PartySaveData schema unchanged.'
    Log 'Regression: Alpha 6.5.3 Surge harness + 6.5.2 safe placement + MiMi/Sudoku/Origin/prior systems retained.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.0 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($shaPath, "$hash  $(Split-Path $zip -Leaf)`r`n", $utf8NoBom)

    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_0_PARTY_STRATEGY_FOUNDATION_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }

    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.0'
    Log 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.6.0'
    Log 'PARTY STRATEGY: BALANCED / DEFENSIVE / AGGRESSIVE / HOLD POSITION / BOSS FOCUS'
    Log 'COMMAND: teamup_strategy status|balanced|defensive|aggressive|hold|boss'
    Log 'SAVE SAFETY: CONFIG-BACKED, NO PARTY SAVE SCHEMA CHANGE'
    Log 'REGRESSION: ALPHA 6.5.3 SURGE HARNESS + PRIOR LOCKS RETAINED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
