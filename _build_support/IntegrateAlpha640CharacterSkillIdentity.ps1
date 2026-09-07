$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$modEntryPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$rendererPath = Join-Path $root 'src\TeamUp\UI\TraitIconRenderer.cs'
$equipmentRpgPath = Join-Path $root 'src\TeamUp\Core\EquipmentRpgPolishService.cs'
$catalogPath = Join-Path $root 'src\TeamUp\Core\CharacterSkillIdentityCatalog.cs'
$runtimePath = Join-Path $root 'src\TeamUp\Combat\CharacterSkillIdentityService.cs'
$progressionPath = Join-Path $root 'src\TeamUp\Core\ProgressionService.cs'
$projectPath = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$version = '0.2.0-alpha.6.4.0'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing Alpha 6.4.0 source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}

function WriteText([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom)
}

# Version is materialized directly on the branch, but keep the integrator idempotent.
$project = ReadText $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteText $projectPath $project

# Wire the new runtime service into ModEntry without changing the existing Alpha 6 prototype service.
$mod = ReadText $modEntryPath
$mod = $mod.Replace('0.2.0-alpha.6.3.2', $version)
$mod = $mod.Replace('Team Up! v0.2.0-alpha.6.4.0 signature icon art pass loaded.', 'Team Up! v0.2.0-alpha.6.4.0 character skill identity loaded.')

if ($mod -notmatch 'CharacterSkillIdentityService SkillIdentity') {
    $needle = "    private Alpha6CombatPolishService Alpha6Polish { get; set; } = null!;`n    private TeamUpDebugService DebugTools { get; set; } = null!;"
    $replacement = "    private Alpha6CombatPolishService Alpha6Polish { get; set; } = null!;`n    private CharacterSkillIdentityService SkillIdentity { get; set; } = null!;`n    private TeamUpDebugService DebugTools { get; set; } = null!;"
    if (-not $mod.Contains($needle)) { throw 'Could not locate Alpha6Polish field insertion point.' }
    $mod = $mod.Replace($needle, $replacement)
}

if ($mod -notmatch 'SkillIdentity = new CharacterSkillIdentityService') {
    $needle = "        Alpha6Polish = new Alpha6CombatPolishService(Monitor, Progression);`n        DebugTools = new TeamUpDebugService("
    $replacement = "        Alpha6Polish = new Alpha6CombatPolishService(Monitor, Progression);`n        SkillIdentity = new CharacterSkillIdentityService(Progression);`n        DebugTools = new TeamUpDebugService("
    if (-not $mod.Contains($needle)) { throw 'Could not locate SkillIdentity construction point.' }
    $mod = $mod.Replace($needle, $replacement)
}

if ($mod -notmatch 'SkillIdentity\.Clear\(\)') {
    $mod = $mod.Replace("        Alpha6Polish.Clear();`n", "        Alpha6Polish.Clear();`n        SkillIdentity.Clear();`n")
}

if ($mod -notmatch 'SkillIdentity\.Update\(Party\.Members') {
    $needle = "        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);`n        Alpha6Polish.Update(Party.Members, Game1.player.UniqueMultiplayerID);"
    $replacement = "        SkillIdentity.Update(Party.Members, Game1.player.UniqueMultiplayerID);`n        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);`n        Alpha6Polish.Update(Party.Members, Game1.player.UniqueMultiplayerID);"
    if (-not $mod.Contains($needle)) { throw 'Could not locate combat update insertion point.' }
    $mod = $mod.Replace($needle, $replacement)
}
WriteText $modEntryPath $mod

# Add bespoke one-icon silhouettes for every newly completed vanilla kit.
$renderer = ReadText $rendererPath
if ($renderer -notmatch '\["Caroline"\] = P\(') {
$vanillaIcons = @'
            // Stardew Valley Alpha 6.4.0 completed character identities.
            ["Caroline"] = P(
                "........",
                "..####..",
                ".#....#.",
                ".######.",
                ".#....#.",
                "..####..",
                "...##...",
                "........"),
            ["Clint"] = P(
                "..##....",
                "..##....",
                "######..",
                "..##....",
                "..##....",
                "..###...",
                "...###..",
                "....##.."),
            ["Demetrius"] = P(
                "#......#",
                ".#....#.",
                "..####..",
                ".######.",
                "##.##.##",
                "...##...",
                "..#..#..",
                ".#....#."),
            ["Elliott"] = P(
                ".....##.",
                "....###.",
                "...###..",
                "..###...",
                "...##...",
                "..####..",
                ".##..##.",
                "##....##"),
            ["Evelyn"] = P(
                "...##...",
                "..####..",
                ".##..##.",
                "########",
                ".######.",
                "..####..",
                "...##...",
                "..#..#.."),
            ["George"] = P(
                ".######.",
                "##....##",
                "##.##.##",
                "##.##.##",
                "##.##.##",
                "##....##",
                ".######.",
                "...##..."),
            ["Gus"] = P(
                "..####..",
                ".######.",
                "##....##",
                "########",
                "..####..",
                "...##...",
                "...##...",
                "..####.."),
            ["Haley"] = P(
                ".######.",
                "##....##",
                "##.##.##",
                "##.##.##",
                "##....##",
                ".######.",
                "...##...",
                "..####.."),
            ["Jodi"] = P(
                "...##...",
                "..####..",
                ".######.",
                "##.##.##",
                "##....##",
                "########",
                "##....##",
                "##....##"),
            ["Kent"] = P(
                "##....##",
                "########",
                "..####..",
                "..####..",
                "########",
                "##....##",
                ".##..##.",
                "..####.."),
            ["Leah"] = P(
                "...##...",
                "..####..",
                ".##.##..",
                "##..##..",
                "..####..",
                "...##...",
                "..##....",
                ".##....."),
            ["Lewis"] = P(
                "..####..",
                ".######.",
                "##.##.##",
                "########",
                "..####..",
                "..####..",
                ".##..##.",
                "##....##"),
            ["Linus"] = P(
                "#......#",
                ".#....#.",
                "..#..#..",
                "...##...",
                "..####..",
                ".##..##.",
                "##....##",
                "#......#"),
            ["Marnie"] = P(
                "..#..#..",
                ".######.",
                "########",
                "########",
                ".######.",
                "..####..",
                "...##...",
                "..#..#.."),
            ["Pam"] = P(
                "##......",
                "####....",
                "######..",
                "########",
                "..######",
                "....####",
                "......##",
                "...##..."),
            ["Penny"] = P(
                "..####..",
                ".##..##.",
                "##....##",
                "##.##.##",
                "##.##.##",
                ".######.",
                "..####..",
                "...##..."),
            ["Pierre"] = P(
                "..####..",
                ".######.",
                "##.##.##",
                "##.##.##",
                ".######.",
                "...##...",
                "..####..",
                ".##..##."),
            ["Robin"] = P(
                "..##....",
                ".####...",
                "######..",
                "..####..",
                "...####.",
                "....####",
                "...##...",
                "..##...."),
            ["Sam"] = P(
                "...##...",
                "...###..",
                "...##.#.",
                "...##.##",
                "..###.##",
                ".##...##",
                ".##..##.",
                "..####.."),
            ["Sandy"] = P(
                "...##...",
                "..####..",
                ".######.",
                "########",
                "..####..",
                ".##..##.",
                "##....##",
                "..#..#.."),
            ["Sebastian"] = P(
                "......##",
                "....####",
                "..####..",
                ".####...",
                "####....",
                "..##....",
                "...##...",
                "....##.."),
            ["Shane"] = P(
                "..####..",
                ".######.",
                "########",
                "##.##.##",
                "##.##.##",
                ".######.",
                "..####..",
                "...##..."),
            ["Willy"] = P(
                ".....##.",
                "....###.",
                "...###..",
                "..###...",
                "..##....",
                ".##.....",
                "##..##..",
                ".####..."),
            ["Wizard"] = P(
                "#..##..#",
                ".######.",
                "..####..",
                "###..###",
                "..####..",
                ".######.",
                "#..##..#",
                "...##..."),

'@
    $marker = '            // Stardew Valley Expanded Wave 1.'
    if (-not $renderer.Contains($marker)) { throw 'Could not locate SVE icon marker.' }
    $renderer = $renderer.Replace($marker, $vanillaIcons + $marker)
}
$renderer = $renderer.Replace('Alpha 6.3.2 gives completed signature kits a hand-authored 8x8 silhouette,', 'Alpha 6.4.0 gives completed signature kits a hand-authored 8x8 silhouette,')
WriteText $rendererPath $renderer

# Equipment tooltip can now preview cooldowns for every completed vanilla identity.
$equipmentRpg = ReadText $equipmentRpgPath
if ($equipmentRpg -notmatch '\["Caroline"\] = 780') {
$newCooldowns = @'
            ["Caroline"] = 780,
            ["Clint"] = 720,
            ["Demetrius"] = 750,
            ["Elliott"] = 780,
            ["Evelyn"] = 810,
            ["George"] = 750,
            ["Gus"] = 780,
            ["Haley"] = 600,
            ["Jodi"] = 780,
            ["Kent"] = 660,
            ["Leah"] = 630,
            ["Lewis"] = 750,
            ["Linus"] = 720,
            ["Marnie"] = 780,
            ["Pam"] = 660,
            ["Penny"] = 780,
            ["Pierre"] = 600,
            ["Robin"] = 720,
            ["Sam"] = 600,
            ["Sandy"] = 720,
            ["Sebastian"] = 660,
            ["Shane"] = 630,
            ["Willy"] = 660,
            ["Wizard"] = 720,

'@
    $marker = '            ["Alesia"] = 620,'
    if (-not $equipmentRpg.Contains($marker)) { throw 'Could not locate expansion cooldown marker.' }
    $equipmentRpg = $equipmentRpg.Replace($marker, $newCooldowns + $marker)
}
WriteText $equipmentRpgPath $equipmentRpg

# Acceptance checks. These are intentionally source-level and fail before dotnet build if integration drifted.
$mod = ReadText $modEntryPath
$renderer = ReadText $rendererPath
$equipmentRpg = ReadText $equipmentRpgPath
$catalog = ReadText $catalogPath
$runtime = ReadText $runtimePath
$progression = ReadText $progressionPath
$project = ReadText $projectPath

if ($project -notmatch '<Version>0\.2\.0-alpha\.6\.4\.0</Version>') { throw 'Alpha 6.4.0 version was not materialized.' }
if ($mod -notmatch 'CharacterSkillIdentityService SkillIdentity') { throw 'SkillIdentity field missing from ModEntry.' }
if ($mod -notmatch 'SkillIdentity\.Update\(Party\.Members') { throw 'SkillIdentity update loop missing from ModEntry.' }
if ($mod -notmatch 'build: v0\.2\.0-alpha\.6\.4\.0') { throw 'Alpha 6.4.0 debug marker missing.' }
if ($catalog -notmatch '\["Wizard"\] = I\(' -or $catalog -notmatch '\["Caroline"\] = I\(') { throw 'Vanilla skill identity catalog is incomplete.' }
if ($runtime -notmatch 'CharacterSignatureArchetype\.Recovery' -or $runtime -notmatch 'ApplyConfiguredBuff') { throw 'Character skill runtime is incomplete.' }
if ($progression -notmatch 'ApplyTemporaryModifier' -or $progression -notmatch 'TickTemporaryModifiers') { throw 'Runtime buff hooks are missing from ProgressionService.' }
if ($renderer -notmatch '\["Wizard"\] = P\(' -or $renderer -notmatch '\["Caroline"\] = P\(') { throw 'New vanilla bespoke signature icons are incomplete.' }
if ($equipmentRpg -notmatch '\["Wizard"\] = 720' -or $equipmentRpg -notmatch '\["Caroline"\] = 780') { throw 'Equipment signature cooldown preview is incomplete.' }

Write-Host 'Alpha 6.4.0 character skill identity integrated.'
Write-Host '24 remaining vanilla NPCs now have data-driven Tier 2/3 signatures, bounded runtime buffs, and bespoke signature icons.'
Write-Host 'Role balance remains locked at 70% Primary / 30% Secondary; marriage/friendship bonuses are intentionally deferred to Alpha 6.4.1.'
