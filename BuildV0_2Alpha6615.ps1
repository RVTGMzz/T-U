$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$alpha6615 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6615.cs'
$capturePatch = Join-Path $root 'src\TeamUp\Core\PelipperCaptureDamagePatch.cs'
$captureSafety = Join-Path $root 'src\TeamUp\Core\PelipperCaptureSafetyService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6615'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.15_COMPANION_INTENT_RECALL_CAPTURE_FLOOR_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.15_COMPANION_INTENT_RECALL_CAPTURE_FLOOR_TEST.sha256.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_15_COMPANION_INTENT_RECALL_CAPTURE_FLOOR_VI.txt'
$version = '0.2.0-alpha.6.6.15'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }

try {
    foreach ($required in @($project,$manifest,$modEntry,$alpha663,$alpha6615,$capturePatch,$captureSafety,$follow,$combat,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.15 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    $projectText = [regex]::Replace($projectText, '<EnableHarmony>[^<]+</EnableHarmony>', '<EnableHarmony>true</EnableHarmony>')
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.15')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.15 Companion Intent + Recall + Capture Floor loaded.')
    Write-Utf8 $modEntry $modText

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $a663 = Read-Lf $alpha663
    $a15 = Read-Lf $alpha6615
    $capturePatchText = Read-Lf $capturePatch
    $captureSafetyText = Read-Lf $captureSafety
    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.15</Version>')) { throw 'Version materialization failed.' }
    if (-not $projectText.Contains('<EnableHarmony>true</EnableHarmony>')) { throw 'Harmony reference is not enabled.' }
    foreach ($token in @('build: v0.2.0-alpha.6.6.15','Companion Intent + Recall + Capture Floor loaded.')) {
        if (-not $modText.Contains($token)) { throw "ModEntry token missing: $token" }
    }
    foreach ($token in @('RegisterAlpha6615Events();','SetOwnerOptOut(owner, !includeCompanion)','requestActive = !optedOut','NPC-only/Call intent')) {
        if (-not $a663.Contains($token)) { throw "NPC intent token missing: $token" }
    }
    foreach ($token in @('ShowLinkedCompanionControlAlpha6615','ShowCompanionReplacementForTargetAlpha6615','DetectPlayerCompanionRecallAttemptsAlpha6615','TryReadPelipperSourceDeploymentAlpha6615','PutCompanionOnStandbyAlpha6615','ActivateCompanionAlpha6615')) {
        if (-not $a15.Contains($token)) { throw "Recall token missing: $token" }
    }
    foreach ($token in @('Harmony','Monster.takeDamage','BeforeTakeDamage','ClampDamage')) {
        if (-not $capturePatchText.Contains($token)) { throw "Capture-floor token missing: $token" }
    }
    if (-not $captureSafetyText.Contains('FallbackThreshold = 0.10f')) { throw '10 percent capture fallback missing.' }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }

    Log 'Building Alpha 6.6.15 Companion Intent + Recall + Capture Floor...'
    Log 'INTENT: NPC-only is durable even if Pelipper actor is detected later.'
    Log 'RECALL: Standby linked companions can be called/returned from member dialogue; full 2/2 pool asks for replacement.'
    Log 'PLAYER SUMMON: re-summoning a registered Standby Pokemon can promote it or open replacement flow instead of silently retracting forever.'
    Log 'CAPTURE: Harmony clamps Monster.takeDamage at Pelipper capture floor so source-owned companions cannot finish the protected Pokemon.'
    Log 'REGRESSION: 6.6.7+ water performance, land safety, source movement authority, HP bars, Switch input, Codex and farewell preserved.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.15 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $zipName`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.15'
    Log 'NPC COMPANION INTENT LOCK: ENABLED'
    Log 'COMPANION CALL / RETURN / REPLACE: ENABLED'
    Log 'PLAYER STANDBY RECALL DETECTION: ENABLED'
    Log 'GLOBAL PELIPPER CAPTURE DAMAGE FLOOR: ENABLED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
