$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$baseBuild = Join-Path $root 'BuildV0_2Alpha6.ps1'
$generatedBuild = Join-Path $root '_generated_BuildV0_2Alpha63.ps1'
$expansionIntegratorPath = Join-Path $root '_build_support\IntegrateExpansionSkillsWave1.ps1'
$uiIntegratorPath = Join-Path $root '_build_support\IntegrateNpcLoadoutTraitIcons.ps1'
$polishPath = Join-Path $root '_build_support\FixAlpha63LoadoutPolish.ps1'

if (-not (Test-Path $baseBuild)) { throw "Missing base Alpha 6 build script: $baseBuild" }
if (-not (Test-Path $expansionIntegratorPath)) { throw "Missing expansion integration helper: $expansionIntegratorPath" }
if (-not (Test-Path $uiIntegratorPath)) { throw "Missing NPC loadout integration helper: $uiIntegratorPath" }
if (-not (Test-Path $polishPath)) { throw "Missing Alpha 6.3 loadout polish helper: $polishPath" }

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
function Normalize-Crlf([string]$text) {
    return $text.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")
}

$source = Normalize-Crlf ([System.IO.File]::ReadAllText($baseBuild, [System.Text.Encoding]::UTF8))

# Preserve the stable 6.1.3 regression chain, then layer expansion skills and the
# visual NPC loadout/profile pass immediately before source verification + compile.
$source = $source.Replace(
    'TeamUp_v0.2.0-alpha.6.1.3_PARTY_UX_TANK_CHACHA_TEST.zip',
    'TeamUp_v0.2.0-alpha.6.3.0_NPC_LOADOUT_TRAIT_ICONS_TEST.zip')
$source = $source.Replace(
    'TeamUp_v0.2.0-alpha.6.1.3_PARTY_UX_TANK_CHACHA_TEST.sha256.txt',
    'TeamUp_v0.2.0-alpha.6.3.0_NPC_LOADOUT_TRAIT_ICONS_TEST.sha256.txt')

$oldVar = '$alpha613Fixer = Join-Path $root ''_build_support\FixAlpha613PartyUxCombat.ps1'''
$newVar = $oldVar + "`r`n" +
    '$expansionIntegrator = Join-Path $root ''_build_support\IntegrateExpansionSkillsWave1.ps1''' + "`r`n" +
    '$uiIntegrator = Join-Path $root ''_build_support\IntegrateNpcLoadoutTraitIcons.ps1''' + "`r`n" +
    '$alpha63Polish = Join-Path $root ''_build_support\FixAlpha63LoadoutPolish.ps1'''
if (-not $source.Contains($oldVar)) { throw 'Alpha 6.3 wrapper could not locate the Alpha 6.1.3 fixer variable.' }
$source = $source.Replace($oldVar, $newVar)
$source = $source.Replace(
    '@($finalizer, $compileFixer, $uxFixer, $followPerfFixer, $partyGhostFixer, $debugIntegrator, $alpha613Fixer)',
    '@($finalizer, $compileFixer, $uxFixer, $followPerfFixer, $partyGhostFixer, $debugIntegrator, $alpha613Fixer, $expansionIntegrator, $uiIntegrator, $alpha63Polish)')

$anchor = Normalize-Crlf @'
& $alpha613Fixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.1.3 consolidated hotfix failed.' }
'@
$insert = Normalize-Crlf @'
& $alpha613Fixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.1.3 consolidated hotfix failed.' }

"Integrating Alpha 6.2 expansion profiles + signature skills wave 1..." | Tee-Object -FilePath $log -Append
& $expansionIntegrator 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.2 expansion skills integration failed.' }

"Integrating Alpha 6.3 NPC loadout + trait icons..." | Tee-Object -FilePath $log -Append
& $uiIntegrator 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.3 NPC loadout integration failed.' }

"Polishing Alpha 6.3 controller stat preview + nullable warning..." | Tee-Object -FilePath $log -Append
& $alpha63Polish 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.3 loadout polish failed.' }
'@
$anchor = $anchor.TrimEnd()
$insert = $insert.TrimEnd()
if (-not $source.Contains($anchor)) {
    throw 'Alpha 6.3 wrapper could not locate the post-Alpha 6.1.3 insertion point.'
}
$source = $source.Replace($anchor, $insert)

# The base build's final verification/package metadata should expect the final layer.
$source = $source.Replace('0.2.0-alpha.6.1.3', '0.2.0-alpha.6.3.0')
$source = $source.Replace(
    'Checkpoint: Party UX + Tank Approach + ChaCha Exclusion',
    'Checkpoint: NPC Loadout + Trait Icons + Expansion Skills Wave 1')
$source = $source.Replace(
    'Vietnamese: Vault + Equipment strings are materialized through ASCII-only Unicode escapes.',
    "Vietnamese: Vault + Equipment strings are materialized through ASCII-only Unicode escapes.`r`nExpansion: SVE + Ridgeside source-aware Codex roster; 24 curated NPCs have real Tier 2/3 signature skills.`r`nLoadout: NPC portrait + 3 live equipment slots + 6x6 Farmer backpack grid with item icons + mouse/controller stat comparison.`r`nTraits: unique generated Passive + Signature icon per NPC.")
$source = $source.Replace(
    "Write-Host 'BUILD SUCCESS - ALPHA 6.1.3'",
    "Write-Host 'BUILD SUCCESS - ALPHA 6.3.0'")
$source = $source.Replace(
    "Write-Host 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.1.3'",
    "Write-Host 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.3.0'")

$smokeAnchor = Normalize-Crlf @'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_1_VI.txt'
if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir 'SMOKE_TEST_V0_2_ALPHA6_1_VI.txt') }
'@
$smokeInsert = Normalize-Crlf @'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_1_VI.txt'
if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir 'SMOKE_TEST_V0_2_ALPHA6_1_VI.txt') }
$smoke63 = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_3_NPC_LOADOUT_VI.txt'
if (Test-Path $smoke63) { Copy-Item $smoke63 (Join-Path $releaseDir 'SMOKE_TEST_V0_2_ALPHA6_3_NPC_LOADOUT_VI.txt') }
'@
$smokeAnchor = $smokeAnchor.TrimEnd()
$smokeInsert = $smokeInsert.TrimEnd()
if (-not $source.Contains($smokeAnchor)) {
    throw 'Alpha 6.3 wrapper could not locate smoke-test packaging block.'
}
$source = $source.Replace($smokeAnchor, $smokeInsert)

[System.IO.File]::WriteAllText($generatedBuild, $source, $utf8NoBom)
try {
    & $generatedBuild
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    if (Test-Path $generatedBuild) { Remove-Item $generatedBuild -Force -ErrorAction SilentlyContinue }
}
