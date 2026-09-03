$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Normalize-Crlf([string]$text) {
    return $text.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")
}

function Ensure-Replace([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Expansion skills integration could not locate $label." }
    return $text.Replace($old, $new)
}

function Convert-ToJsonAsciiString([string]$value) {
    $builder = New-Object System.Text.StringBuilder
    foreach ($character in $value.ToCharArray()) {
        $code = [int][char]$character
        if ($character -eq '"') { [void]$builder.Append('\"'); continue }
        if ($character -eq '\') { [void]$builder.Append('\\'); continue }
        if ($character -eq "`r") { [void]$builder.Append('\r'); continue }
        if ($character -eq "`n") { [void]$builder.Append('\n'); continue }
        if ($character -eq "`t") { [void]$builder.Append('\t'); continue }
        if ($code -lt 32 -or $code -gt 126) {
            [void]$builder.Append(('\u{0:x4}' -f $code))
            continue
        }
        [void]$builder.Append($character)
    }
    return $builder.ToString()
}

function Patch-Translation([string]$path, [object]$map) {
    $text = Normalize-Crlf ([System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8))
    $newLines = New-Object System.Collections.Generic.List[string]
    foreach ($property in $map.PSObject.Properties) {
        $marker = '"' + $property.Name + '"'
        if ($text.Contains($marker)) { continue }
        $escaped = Convert-ToJsonAsciiString ([string]$property.Value)
        $newLines.Add('  "' + $property.Name + '": "' + $escaped + '",')
    }

    if ($newLines.Count -eq 0) { return }
    $needle = '  "common.back":'
    if (-not $text.Contains($needle)) { throw "Expansion translations could not locate common.back in $path" }
    $block = [string]::Join("`r`n", $newLines)
    $text = $text.Replace($needle, $block + "`r`n`r`n" + $needle)
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

$combatPath = Join-Path $repoRoot 'src\TeamUp\Combat\CombatService.cs'
$skillPath = Join-Path $repoRoot 'src\TeamUp\Combat\ExpansionSkillService.cs'
$catalogPath = Join-Path $repoRoot 'src\TeamUp\Core\NpcProfileCatalog.cs'
$expansionCatalogPath = Join-Path $repoRoot 'src\TeamUp\Core\ExpansionNpcProfileCatalog.cs'
$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'
$projectPath = Join-Path $repoRoot 'src\TeamUp\TeamUp.csproj'
$defaultPath = Join-Path $repoRoot 'src\TeamUp\i18n\default.json'
$viPath = Join-Path $repoRoot 'src\TeamUp\i18n\vi.json'
$translationsPath = Join-Path $repoRoot '_build_support\ExpansionSkillsWave1Translations.json'

foreach ($path in @($combatPath, $skillPath, $catalogPath, $expansionCatalogPath, $modPath, $projectPath, $defaultPath, $viPath, $translationsPath)) {
    if (-not (Test-Path $path)) { throw "Expansion skills required source is missing: $path" }
}

$combat = Normalize-Crlf ([System.IO.File]::ReadAllText($combatPath, [System.Text.Encoding]::UTF8))
$combat = Ensure-Replace $combat `
    '    private readonly ThreatService _threat = new();' `
    "    private readonly ThreatService _threat = new();`r`n    private readonly ExpansionSkillService _expansionSkills;" `
    'ExpansionSkillService field'
$combat = Ensure-Replace $combat `
    '        _progression = progression;' `
    "        _progression = progression;`r`n        _expansionSkills = new ExpansionSkillService(progression, _threat);" `
    'ExpansionSkillService construction'
$combat = Ensure-Replace $combat `
    '        _threat.Clear();' `
    "        _threat.Clear();`r`n        _expansionSkills.Clear();" `
    'ExpansionSkillService clear hook'
$combat = Ensure-Replace $combat `
    '        UpdateSurvivalStates(activeMembers, monsters, validThreatActors);' `
    "        UpdateSurvivalStates(activeMembers, monsters, validThreatActors);`r`n        _expansionSkills.Update(activeMembers, monsters);" `
    'ExpansionSkillService update hook'
[System.IO.File]::WriteAllText($combatPath, $combat, $utf8NoBom)

$mod = Normalize-Crlf ([System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8))
$mod = Ensure-Replace $mod `
    '            NpcProfileCatalog.All,' `
    '            NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry),' `
    'source-aware Codex roster'
$oldRecruit = @'
                if (profile is not null)
                {
                    Party.SetRole(npc.Name, recruiterId, profile.PrimaryRole);
                    Party.SetEngagementStyle(npc.Name, recruiterId, profile.RecommendedEngagement);
                }
'@
$newRecruit = @'
                if (profile is not null && profile.PrimaryRole != PartyRole.Unassigned)
                {
                    Party.SetRole(npc.Name, recruiterId, profile.PrimaryRole);
                    Party.SetEngagementStyle(npc.Name, recruiterId, profile.RecommendedEngagement);
                }
'@
$mod = Ensure-Replace $mod (Normalize-Crlf ($oldRecruit.TrimEnd())) (Normalize-Crlf ($newRecruit.TrimEnd())) 'safe expansion recruit defaults'
$mod = [regex]::Replace($mod, 'Team Up! v[^\"]+ loaded\.', 'Team Up! v0.2.0-alpha.6.2.0 expansion skills wave 1 loaded.', 1)
$mod = $mod.Replace('Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.3', 'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.2.0')
[System.IO.File]::WriteAllText($modPath, $mod, $utf8NoBom)

$project = [System.IO.File]::ReadAllText($projectPath, [System.Text.Encoding]::UTF8)
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', '<Version>0.2.0-alpha.6.2.0</Version>', 1)
[System.IO.File]::WriteAllText($projectPath, $project, $utf8NoBom)

$translations = [System.IO.File]::ReadAllText($translationsPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
Patch-Translation $defaultPath $translations.default
Patch-Translation $viPath $translations.vi

$combatVerify = [System.IO.File]::ReadAllText($combatPath, [System.Text.Encoding]::UTF8)
$modVerify = [System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8)
$catalogVerify = [System.IO.File]::ReadAllText($catalogPath, [System.Text.Encoding]::UTF8)
$skillVerify = [System.IO.File]::ReadAllText($skillPath, [System.Text.Encoding]::UTF8)
$viVerify = [System.IO.File]::ReadAllText($viPath, [System.Text.Encoding]::UTF8)
if (-not $combatVerify.Contains('_expansionSkills.Update(activeMembers, monsters);')) { throw 'Expansion skill runtime hook verification failed.' }
if (-not $modVerify.Contains('NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry)')) { throw 'Expansion Codex source-filter verification failed.' }
if (-not $modVerify.Contains('build: v0.2.0-alpha.6.2.0')) { throw 'Expansion debug marker verification failed.' }
if (-not $catalogVerify.Contains('GetAvailableProfiles(IModRegistry modRegistry)')) { throw 'Expansion profile catalog verification failed.' }
if (-not $viVerify.Contains('"codex.expansion.claire.ability"')) { throw 'Expansion Vietnamese dossier verification failed.' }
foreach ($skill in @('METEOR BREAK', 'SECOND TAKE', 'HIGHLAND BURST', 'RESONANT CHORD', 'SAFE HAVEN', 'GUARDIAN BREAK')) {
    if (-not $skillVerify.Contains($skill)) { throw "Expansion skill verification failed: $skill missing." }
}

Write-Host 'Alpha 6.2.0 expansion profiles + real signature skills wave 1 integrated.'
Write-Host 'Curated skill NPCs: 24 (12 SVE + 12 Ridgeside Village).'
