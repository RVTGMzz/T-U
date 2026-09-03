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

function Patch-Translation([string]$path, [object]$map) {
    $data = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
    foreach ($property in $map.PSObject.Properties) {
        $data | Add-Member -NotePropertyName $property.Name -NotePropertyValue $property.Value -Force
    }
    [System.IO.File]::WriteAllText($path, (($data | ConvertTo-Json -Depth 8) + "`r`n"), $utf8NoBom)
}

$translations = [System.IO.File]::ReadAllText($translationsPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
Patch-Translation $defaultPath $translations.default
Patch-Translation $viPath $translations.vi

$combatVerify = [System.IO.File]::ReadAllText($combatPath, [System.Text.Encoding]::UTF8)
$modVerify = [System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8)
$catalogVerify = [System.IO.File]::ReadAllText($catalogPath, [System.Text.Encoding]::UTF8)
$skillVerify = [System.IO.File]::ReadAllText($skillPath, [System.Text.Encoding]::UTF8)
if (-not $combatVerify.Contains('_expansionSkills.Update(activeMembers, monsters);')) { throw 'Expansion skill runtime hook verification failed.' }
if (-not $modVerify.Contains('NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry)')) { throw 'Expansion Codex source-filter verification failed.' }
if (-not $modVerify.Contains('build: v0.2.0-alpha.6.2.0')) { throw 'Expansion debug marker verification failed.' }
if (-not $catalogVerify.Contains('GetAvailableProfiles(IModRegistry modRegistry)')) { throw 'Expansion profile catalog verification failed.' }
foreach ($skill in @('METEOR BREAK', 'SECOND TAKE', 'HIGHLAND BURST', 'RESONANT CHORD', 'SAFE HAVEN', 'GUARDIAN BREAK')) {
    if (-not $skillVerify.Contains($skill)) { throw "Expansion skill verification failed: $skill missing." }
}

Write-Host 'Alpha 6.2.0 expansion profiles + real signature skills wave 1 integrated.'
Write-Host 'Curated skill NPCs: 24 (12 SVE + 12 Ridgeside Village).'
