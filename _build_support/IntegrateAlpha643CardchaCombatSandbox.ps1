$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$debugPath = Join-Path $root 'src\TeamUp\Debugging\TeamUpDebugService.cs'
$modEntryPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$projectPath = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$combatPath = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$identityPath = Join-Path $root 'src\TeamUp\Combat\CharacterSkillIdentityService.cs'
$alpha6Path = Join-Path $root 'src\TeamUp\Combat\Alpha6CombatPolishService.cs'
$relationshipPath = Join-Path $root 'src\TeamUp\Core\RelationshipBondService.cs'
$sandboxPath = Join-Path $root 'src\TeamUp\Debugging\CardchaCombatSandboxService.cs'
$compatPath = Join-Path $root 'src\TeamUp\Core\OptionalTestHostCompatibility.cs'
$version = '0.2.0-alpha.6.4.3'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing Alpha 6.4.3 source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}

function WriteText([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom)
}

# Version + startup marker.
$project = ReadText $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteText $projectPath $project

$mod = ReadText $modEntryPath
$mod = $mod.Replace('0.2.0-alpha.6.4.2', $version)
$mod = $mod.Replace('UI readability pass loaded.', 'Cardcha combat sandbox loaded.')

if ($mod -notmatch 'DebugTools\.Update\(\);') {
    $needle = "        RunPendingUiAction();`n`n        if (Game1.activeClickableMenu is null"
    $replacement = "        RunPendingUiAction();`n        DebugTools.Update();`n`n        if (Game1.activeClickableMenu is null"
    if (-not $mod.Contains($needle)) { throw 'Could not locate DebugTools.Update insertion point.' }
    $mod = $mod.Replace($needle, $replacement)
}

if ($mod -notmatch 'DebugTools\.ClearSandboxRuntime\(\);') {
    $mod = $mod.Replace("        Party.Load(saveData);`n        Combat.Clear();", "        Party.Load(saveData);`n        DebugTools.ClearSandboxRuntime();`n        Combat.Clear();")
    $mod = $mod.Replace("        long recruiterId = Game1.player.UniqueMultiplayerID;`n        Combat.Clear();", "        long recruiterId = Game1.player.UniqueMultiplayerID;`n        DebugTools.ClearSandboxRuntime();`n        Combat.Clear();")
    $mod = $mod.Replace("        SocialCodexButtonBounds = Rectangle.Empty;`n        Combat.Clear();", "        SocialCodexButtonBounds = Rectangle.Empty;`n        DebugTools.ClearSandboxRuntime();`n        Combat.Clear();")
}
WriteText $modEntryPath $mod

# TeamUpDebugService: replace the old Region I arena bridge with Cardcha's actual Card Test Lab lifecycle.
$debug = ReadText $debugPath
$debug = $debug.Replace('/// when loaded, Team Up can discover its controlled Region I hunting map through map metadata', '/// when loaded, Team Up can safely enter Cardcha''s own Card Test Arena and overlay disposable combat waves')
$debug = $debug.Replace('    private const string CardchaUniqueId = "Ronvotri.Cardcha";`n    private const string CardchaArenaRole = "region1-hunting";`n', '')

if ($debug -notmatch 'CardchaCombatSandboxService _sandbox') {
    $needle = "    private readonly Alpha6CombatPolishService _alpha6;`n    private readonly Action _saveNow;"
    $replacement = "    private readonly Alpha6CombatPolishService _alpha6;`n    private readonly CardchaCombatSandboxService _sandbox;`n    private readonly Action _saveNow;"
    if (-not $debug.Contains($needle)) { throw 'Could not locate sandbox field insertion point.' }
    $debug = $debug.Replace($needle, $replacement)
}

if ($debug -notmatch '_sandbox = new CardchaCombatSandboxService') {
    $needle = "        _alpha6 = alpha6;`n        _saveNow = saveNow;"
    $replacement = "        _alpha6 = alpha6;`n        _sandbox = new CardchaCombatSandboxService(helper, monitor);`n        _saveNow = saveNow;"
    if (-not $debug.Contains($needle)) { throw 'Could not locate sandbox construction point.' }
    $debug = $debug.Replace($needle, $replacement)
}

$oldArenaSwitch = @'
            case "arena":
                WarpToCardchaArena();
                break;
'@
$newArenaSwitch = @'
            case "arena":
                CommandArena(args);
                break;
            case "waves":
                CommandWaves(args);
                break;
            case "spawn":
                CommandSpawn(args);
                break;
            case "sandbox":
                CommandSandbox(args);
                break;
'@
if ($debug.Contains($oldArenaSwitch))
    { $debug = $debug.Replace($oldArenaSwitch, $newArenaSwitch) }
elseif ($debug -notmatch 'case "waves":')
    { throw 'Could not locate old arena command switch.' }

$debug = $debug.Replace('                ResetCombatState();', '                _sandbox.StopWaves(clearMonsters: true);`n                ResetCombatState();')

$oldHelp = @'
        Info("  teamup_test arena");
        Info("  teamup_test add <NPC>");
'@
$newHelp = @'
        Info("  teamup_test arena [exit]");
        Info("  teamup_test waves <start [easy|normal|hard]|stop|clear|status>");
        Info("  teamup_test spawn boss");
        Info("  teamup_test sandbox [easy|normal|hard]");
        Info("  teamup_test add <NPC>");
'@
if ($debug.Contains($oldHelp)) { $debug = $debug.Replace($oldHelp, $newHelp) }
$debug = $debug.Replace('        Info("Cardcha is optional. ''arena'' only activates when Ronvotri.Cardcha and its Region I hunting map are loaded.");', '        Info("Cardcha is optional. Arena/sandbox uses Cardcha_CardTestArena via Cardcha''s own cardcha_card_test lifecycle; no Cardcha map asset is copied into Team Up.");')

$startMarker = '    private void WarpToCardchaArena()'
$endMarker = '    private void CommandAdd(string[] args)'
$startIndex = $debug.IndexOf($startMarker)
$endIndex = $debug.IndexOf($endMarker)
if ($startIndex -ge 0 -and $endIndex -gt $startIndex) {
$newArenaMethods = @'
    public void Update()
    {
        _sandbox.Update();
    }

    public void ClearSandboxRuntime()
    {
        _sandbox.ResetRuntime();
    }

    private void CommandArena(string[] args)
    {
        if (args.Length >= 2 && args[1].Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            _sandbox.ExitArena();
            Info("Cardcha combat sandbox exited; Team Up wave monsters cleared.");
            return;
        }

        _sandbox.EnterArena();
    }

    private void CommandWaves(string[] args)
    {
        if (args.Length < 2)
        {
            Info("Usage: teamup_test waves <start [easy|normal|hard]|stop|clear|status>");
            return;
        }

        switch (args[1].Trim().ToLowerInvariant())
        {
            case "start":
                if (!CardchaCombatSandboxService.TryParseDifficulty(args.Length >= 3 ? args[2] : null, out SandboxDifficulty difficulty))
                {
                    Info("Difficulty must be easy, normal, or hard.");
                    return;
                }
                _sandbox.StartWaves(difficulty);
                break;
            case "stop":
                _sandbox.StopWaves(clearMonsters: true);
                Info("Endless Team Up waves stopped and Team Up sandbox monsters cleared.");
                break;
            case "clear":
                _sandbox.ClearOwnedMonsters();
                Info("Cleared Team Up sandbox monsters only. Cardcha dummy/kill targets were preserved.");
                break;
            case "status":
                Info(_sandbox.Describe());
                break;
            default:
                Info("Usage: teamup_test waves <start [easy|normal|hard]|stop|clear|status>");
                break;
        }
    }

    private void CommandSpawn(string[] args)
    {
        if (args.Length < 2 || !args[1].Equals("boss", StringComparison.OrdinalIgnoreCase))
        {
            Info("Usage: teamup_test spawn boss");
            return;
        }
        _sandbox.SpawnBoss();
    }

    private void CommandSandbox(string[] args)
    {
        if (!CardchaCombatSandboxService.TryParseDifficulty(args.Length >= 2 ? args[1] : null, out SandboxDifficulty difficulty))
        {
            Info("Usage: teamup_test sandbox [easy|normal|hard]");
            return;
        }

        if (!_sandbox.EnterArena())
            return;

        ApplyTierPreset(20, 8);
        _sandbox.StartWaves(difficulty);
        Info($"Sandbox ready: party Tier 3 + full HP + cleared Team Up cooldowns + endless {difficulty} waves.");
    }

'@
    $debug = $debug.Substring(0, $startIndex) + $newArenaMethods + $debug.Substring($endIndex)
}
elseif ($debug -notmatch 'private void CommandSandbox\(string\[] args\)') {
    throw 'Could not locate old arena method block.'
}

$debug = $debug.Replace('Cardcha loaded={_helper.ModRegistry.IsLoaded(CardchaUniqueId)}.', 'Cardcha loaded={_helper.ModRegistry.IsLoaded(OptionalTestHostCompatibility.CardchaUniqueId)}.')
if ($debug -notmatch 'Info\(_sandbox\.Describe\(\)\);') {
    $needle = '        Info($"Team Up test status: {members.Count} party member(s); Farmer HP {Game1.player.health}/{Game1.player.maxHealth}; Cardcha loaded={_helper.ModRegistry.IsLoaded(OptionalTestHostCompatibility.CardchaUniqueId)}.");'
    if (-not $debug.Contains($needle)) { throw 'Could not locate test status header.' }
    $debug = $debug.Replace($needle, $needle + "`n        Info(_sandbox.Describe());")
}
WriteText $debugPath $debug

# Keep Cardcha's fixed test dummy and four kill targets out of Team Up's target lists.
$monsterFilterNeedle = @'
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .ToList();
'@
$monsterFilterReplacement = @'
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();
'@
foreach ($path in @($combatPath, $identityPath, $alpha6Path, $relationshipPath)) {
    $text = ReadText $path
    if ($text.Contains($monsterFilterNeedle)) {
        $text = $text.Replace($monsterFilterNeedle, $monsterFilterReplacement)
        WriteText $path $text
    }
    elseif ($text -notmatch 'IsCardchaHarnessMonster') {
        throw "Could not insert Cardcha harness target filter into $path"
    }
}

# Acceptance checks before compile.
$project = ReadText $projectPath
$mod = ReadText $modEntryPath
$debug = ReadText $debugPath
$sandbox = ReadText $sandboxPath
$compat = ReadText $compatPath
$combat = ReadText $combatPath
$identity = ReadText $identityPath
$alpha6 = ReadText $alpha6Path
$relationship = ReadText $relationshipPath

if ($project -notmatch '<Version>0\.2\.0-alpha\.6\.4\.3</Version>') { throw 'Alpha 6.4.3 version was not materialized.' }
if ($mod -notmatch 'build: v0\.2\.0-alpha\.6\.4\.3') { throw 'Alpha 6.4.3 debug marker missing.' }
if ($mod -notmatch 'Cardcha combat sandbox loaded') { throw 'Alpha 6.4.3 load marker missing.' }
if ($mod -notmatch 'DebugTools\.Update\(\);') { throw 'Sandbox update loop missing.' }
if ($debug -match 'region1-hunting' -or $debug -match 'CardchaRegionRole') { throw 'Old Region I arena bridge must be removed.' }
if ($debug -notmatch 'teamup_test sandbox') { throw 'Sandbox command help missing.' }
if ($debug -notmatch 'CommandWaves') { throw 'Wave command missing.' }
if ($sandbox -notmatch 'Cardcha_CardTestArena') { throw 'Exact Cardcha Card Test Arena integration missing.' }
if ($sandbox -notmatch 'cardcha_card_test') { throw 'Cardcha safe Lab entry command missing.' }
if ($sandbox -notmatch 'receiveKeyPress\(Keys\.T\)') { throw 'Cardcha TEST ARENA lifecycle handoff missing.' }
if ($sandbox -notmatch 'BetweenWaveDelayMs = 2200L') { throw 'Wave pacing contract missing.' }
if ($sandbox -notmatch 'SandboxDifficulty\.Hard => 11') { throw 'Hard wave cap missing.' }
if ($compat -notmatch 'CardTestArenaDummy' -or $compat -notmatch 'CardTestArenaKillTarget') { throw 'Cardcha harness marker compatibility missing.' }
foreach ($text in @($combat, $identity, $alpha6, $relationship)) {
    if ($text -notmatch 'IsCardchaHarnessMonster') { throw 'A Team Up combat layer still targets Cardcha harness dummies.' }
}

Write-Host 'Alpha 6.4.3 Cardcha Combat Sandbox integrated.'
Write-Host 'Team Up now enters Cardcha_CardTestArena through Cardcha cardcha_card_test + TEST ARENA, then overlays bounded endless waves.'
Write-Host 'Cardcha owns the map/clock/dummies; Team Up owns only its tagged wave/boss monsters and ignores Cardcha harness targets.'
