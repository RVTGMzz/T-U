$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha6612 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6612.cs'
$alpha6613 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6613.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6613'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.13_SINGLE_TARGET_PELIPPER_QUOTA_FAREWELL_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.13_SINGLE_TARGET_PELIPPER_QUOTA_FAREWELL_TEST.sha256.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_13_SINGLE_TARGET_PELIPPER_QUOTA_FAREWELL_VI.txt'
$version = '0.2.0-alpha.6.6.13'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }

try {
    foreach ($required in @($project,$manifest,$modEntry,$alpha6612,$alpha6613,$follow,$combat,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.13 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.13')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.13 Single Target + Pelipper Hard Quota + Farewell loaded.')
    Write-Utf8 $modEntry $modText

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $a12 = Read-Lf $alpha6612
    $a13 = Read-Lf $alpha6613
    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.13</Version>')) { throw 'Version materialization failed.' }
    foreach ($token in @('build: v0.2.0-alpha.6.6.13','Single Target + Pelipper Hard Quota + Farewell loaded.')) {
        if (-not $modText.Contains($token)) { throw "ModEntry 6.6.13 token missing: $token" }
    }
    foreach ($token in @('RegisterAlpha6613Events();','ShowCurfewFarewellAlpha6613','GetCurfewFarewellAlpha6613','npc.showTextAboveHead')) {
        if (-not $a12.Contains($token)) { throw "Farewell token missing: $token" }
    }
    foreach ($token in @(
        'RefreshPelipperCombatTargetsAlpha6613',
        'CombatTargetOptInKey',
        'PelipperRenderSuppressedAlpha6613',
        'EnforcePelipperHardQuotaAlpha6613',
        'TrySetPelipperSourceDeploymentAlpha6613',
        'RenderingWorld',
        'RenderedWorld',
        'CompanionDeploymentState.Standby'
    )) {
        if (-not $a13.Contains($token)) { throw "Alpha 6.6.13 runtime token missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }

    Log 'Building Alpha 6.6.13 Single Target + Pelipper Hard Quota + Farewell...'
    Log 'COMBAT: unowned Pelipper Monster combat proxies opt in as valid Team Up targets.'
    Log 'QUOTA: shared combat companion state remains hard capped at 2; standby Pelipper actors use source deployment hooks when available plus render-only fallback.'
    Log 'FAREWELL: curfew release shows one personality-specific bye before vanilla/mod schedule resumes.'
    Log 'REGRESSION: water/bridge performance, land safety, source movement authority, contextual HP, Switch input and Codex preserved.'

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
    $manifestText = Read-Lf $manifest
    $manifestText = $manifestText.Replace('%ProjectVersion%', $version)
    Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.13 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $zipName`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.13'
    Log 'SINGLE PELIPPER TARGET COMBAT: ENABLED'
    Log 'SHARED 2/2 PELIPPER QUOTA GATE: ENABLED'
    Log 'PERSONALITY CURFEW FAREWELL: ENABLED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
