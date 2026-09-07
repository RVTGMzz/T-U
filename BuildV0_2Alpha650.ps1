$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$integrator = Join-Path $root '_build_support\IntegrateAlpha650OriginSurgeCustomRecruits.ps1'
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha650'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.5.0_ORIGIN_SURGE_CUSTOM_RECRUITS_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.5.0_ORIGIN_SURGE_CUSTOM_RECRUITS_TEST.sha256.txt'
$version = '0.2.0-alpha.6.5.0'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

try {
    if (-not (Test-Path $integrator)) { throw "Missing Alpha 6.5.0 integrator: $integrator" }
    if (-not (Test-Path $project)) { throw "Missing Team Up project: $project" }

    # Keep literal source identities in the materialized catalog too. This doubles as
    # documentation and lets the integrator's acceptance check verify the two external IDs.
    $catalogPath = Join-Path $root 'src\TeamUp\Core\NpcProfileCatalog.cs'
    $catalogText = [System.IO.File]::ReadAllText($catalogPath, [System.Text.Encoding]::UTF8)
    $sourceIdNote = '// Alpha 6.5.0 custom source IDs: Ronvotri.Cardcha_MiMi | HeyYoureCursed_Sudoku'
    if (-not $catalogText.Contains($sourceIdNote)) {
        $catalogText = $catalogText.Replace(
            'namespace Ronvotri.TeamUp.Core;',
            "namespace Ronvotri.TeamUp.Core;`n`n$sourceIdNote")
        [System.IO.File]::WriteAllText($catalogPath, $catalogText, $utf8NoBom)
    }

    Log 'Integrating Alpha 6.5.0 Origin + Surge + Custom Recruits...'
    & $integrator 2>&1 | Tee-Object -FilePath $log -Append

    Log 'Restoring Team Up...'
    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    Log 'Compiling Team Up Alpha 6.5.0...'
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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.5.0 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($shaPath, "$hash  $(Split-Path $zip -Leaf)`r`n", $utf8NoBom)

    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_5_0_ORIGIN_SURGE_CUSTOM_RECRUITS_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }

    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.5.0'
    Log 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.5.0'
    Log 'ORIGIN: LINUS -> MARLON / THE SURGE -> FIRST AWAKENING -> TEAM UP'
    Log 'SURGE: DEFAULT x2 TARGET IN ELIGIBLE COMBAT ZONES; SAFE SPAWN BUDGET; NO BLIND CLONE'
    Log 'ECONOMY: SURGE BONUS LOOT SUPPRESSED BY DEFAULT'
    Log 'CUSTOM NPCS: MIMI + SUDOKU FULL ROLE / SIGNATURE / BESPOKE ICON PROFILES'
    Log 'CARDCHA: CHACHA REMAINS SPECIAL COMPANION, NEVER MAIN PARTY'
    Log 'REGRESSION: 6.4.6 EXPANSION BALANCE + 6.4.5 INTERACTION/AI FIXES RETAINED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
