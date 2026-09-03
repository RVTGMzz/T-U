$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$baseBuild = Join-Path $root 'BuildV0_2Alpha6.ps1'
$generatedBuild = Join-Path $root '_generated_BuildV0_2Alpha62.ps1'
$integratorPath = Join-Path $root '_build_support\IntegrateExpansionSkillsWave1.ps1'

if (-not (Test-Path $baseBuild)) { throw "Missing base Alpha 6 build script: $baseBuild" }
if (-not (Test-Path $integratorPath)) { throw "Missing expansion integration helper: $integratorPath" }

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$source = [System.IO.File]::ReadAllText($baseBuild, [System.Text.Encoding]::UTF8)
$source = $source.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")

# Keep Alpha 6.1.3's known build chain intact. Generate the Alpha 6.2 build in memory,
# then add one post-6.1.3 integration pass for expansion profiles and real signature skills.
$source = $source.Replace(
    'TeamUp_v0.2.0-alpha.6.1.3_PARTY_UX_TANK_CHACHA_TEST.zip',
    'TeamUp_v0.2.0-alpha.6.2.0_EXPANSION_SKILLS_WAVE1_TEST.zip')
$source = $source.Replace(
    'TeamUp_v0.2.0-alpha.6.1.3_PARTY_UX_TANK_CHACHA_TEST.sha256.txt',
    'TeamUp_v0.2.0-alpha.6.2.0_EXPANSION_SKILLS_WAVE1_TEST.sha256.txt')
$source = $source.Replace(
    'SMOKE_TEST_V0_2_ALPHA6_1_VI.txt',
    'SMOKE_TEST_V0_2_ALPHA6_2_EXPANSION_SKILLS_VI.txt')

$oldVar = '$alpha613Fixer = Join-Path $root ''_build_support\FixAlpha613PartyUxCombat.ps1'''
$newVar = $oldVar + "`r`n" + '$expansionIntegrator = Join-Path $root ''_build_support\IntegrateExpansionSkillsWave1.ps1'''
if (-not $source.Contains($oldVar)) { throw 'Alpha 6.2 wrapper could not locate the Alpha 6.1.3 fixer variable.' }
$source = $source.Replace($oldVar, $newVar)
$source = $source.Replace(
    '@($finalizer, $compileFixer, $uxFixer, $followPerfFixer, $partyGhostFixer, $debugIntegrator, $alpha613Fixer)',
    '@($finalizer, $compileFixer, $uxFixer, $followPerfFixer, $partyGhostFixer, $debugIntegrator, $alpha613Fixer, $expansionIntegrator)')

$anchor = @'
& $alpha613Fixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.1.3 consolidated hotfix failed.' }
'@
$insert = @'
& $alpha613Fixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.1.3 consolidated hotfix failed.' }

"Integrating Alpha 6.2 expansion profiles + signature skills wave 1..." | Tee-Object -FilePath $log -Append
& $expansionIntegrator 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.2 expansion skills integration failed.' }
'@
if (-not $source.Contains($anchor.TrimEnd())) {
    throw 'Alpha 6.2 wrapper could not locate the post-Alpha 6.1.3 insertion point.'
}
$source = $source.Replace($anchor.TrimEnd(), $insert.TrimEnd())

# Once the new integration stage is inserted, advance every build/package/debug version marker.
$source = $source.Replace('0.2.0-alpha.6.1.3', '0.2.0-alpha.6.2.0')
$source = $source.Replace(
    'Checkpoint: Party UX + Tank Approach + ChaCha Exclusion',
    'Checkpoint: Expansion Skills Wave 1 + Alpha 6.1.3 regression fixes')
$source = $source.Replace(
    'Vietnamese: Vault + Equipment strings are materialized through ASCII-only Unicode escapes.',
    "Vietnamese: Vault + Equipment strings are materialized through ASCII-only Unicode escapes.`r`nExpansion: SVE + Ridgeside source-aware Codex roster; 24 curated NPCs have real Tier 2/3 signature skills.")
$source = $source.Replace('BUILD SUCCESS - ALPHA 6.1.3', 'BUILD SUCCESS - ALPHA 6.2.0')
$source = $source.Replace('SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.1.3', 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.2.0')
$source = $source.Replace('VAULT: VIETNAMESE UTF-8 REPAIR ENABLED', 'VAULT: CATEGORY LAYOUT + UTF-8 SAFETY RETAINED')
$source = $source.Replace(
    "Write-Host 'EQUIPMENT: DEDICATED PANEL ENABLED'",
    "Write-Host 'EQUIPMENT: DEDICATED PANEL ENABLED'`r`nWrite-Host 'EXPANSION: 24 REAL SVE/RSV SIGNATURE SKILLS ENABLED'")

if (-not $source.Contains('& $expansionIntegrator')) { throw 'Alpha 6.2 generated build is missing the expansion integration execution hook.' }
if (-not $source.Contains('SMOKE_TEST_V0_2_ALPHA6_2_EXPANSION_SKILLS_VI.txt')) { throw 'Alpha 6.2 generated build did not select the expansion smoke test.' }

[System.IO.File]::WriteAllText($generatedBuild, $source, $utf8NoBom)
try {
    & $generatedBuild
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    if (Test-Path $generatedBuild) { Remove-Item $generatedBuild -Force -ErrorAction SilentlyContinue }
}
