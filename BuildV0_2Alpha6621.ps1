$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha6617 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6617.cs'
$alpha6618 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6618.cs'
$alpha6619 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6619.cs'
$alpha6621 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6621.cs'
$lifecycle = Join-Path $root 'src\TeamUp\Core\PelipperVillagerLifecycleBridge.cs'
$locator = Join-Path $root 'src\TeamUp\Core\PelipperModRuntimeRootLocator.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6621'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_21_PELIPPER_VILLAGER_LIFECYCLE_RECALL_VI.txt'
$version = '0.2.0-alpha.6.6.21'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.21_PELIPPER_VILLAGER_LIFECYCLE_RECALL_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.21_PELIPPER_VILLAGER_LIFECYCLE_RECALL_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function Write-Utf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}
function Log([string]$text) {
    $text | Tee-Object -FilePath $log -Append
}

$projectText = Read-Lf $project
$projectText = $projectText.Replace('<Version>0.2.0-alpha.6.6.20</Version>', '<Version>0.2.0-alpha.6.6.21</Version>')
Write-Utf8 $project $projectText

$modText = Read-Lf $modEntry
$modText = $modText.Replace('build: v0.2.0-alpha.6.6.20', 'build: v0.2.0-alpha.6.6.21')
$modText = $modText.Replace(
    'Team Up! v0.2.0-alpha.6.6.20 Pelipper Runtime Root + Verified Recall Hotfix loaded.',
    'Team Up! v0.2.0-alpha.6.6.21 Pelipper Villager Lifecycle Recall + Probe Hotfix loaded.')
Write-Utf8 $modEntry $modText

$a17 = Read-Lf $alpha6617
$a17 = $a17.Replace(
    'runtimeRoot={PelipperApiRuntimeRootBridge.ApiTypeName}',
    'runtimeRoot={PelipperVillagerLifecycleBridge.RootTypeName}')
Write-Utf8 $alpha6617 $a17

if (Test-Path $log) { Remove-Item $log -Force }

$projectText = Read-Lf $project
$modText = Read-Lf $modEntry
$a17 = Read-Lf $alpha6617
$a18 = Read-Lf $alpha6618
$a19 = Read-Lf $alpha6619
$a21 = Read-Lf $alpha6621
$lifecycleText = Read-Lf $lifecycle
$locatorText = Read-Lf $locator
$followText = Read-Lf $follow
$combatText = Read-Lf $combat

if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.21</Version>')) { throw '6.6.21 version missing.' }
if (-not $modText.Contains('build: v0.2.0-alpha.6.6.21')) { throw '6.6.21 debug build string missing.' }
if (-not $modText.Contains('Pelipper Villager Lifecycle Recall + Probe Hotfix loaded.')) { throw '6.6.21 load string missing.' }
if (-not $a19.Contains('PelipperVillagerLifecycleBridge.Configure(runtimeRoot);')) { throw 'Lifecycle bridge runtime-root configuration missing.' }
if (-not $a19.Contains('TrySetPelipperNpcSourceEnabledAlpha6621')) { throw '6.6.19 source-control delegation to 6.6.21 missing.' }
if (-not $a21.Contains('teamup_pelipper_probe')) { throw 'Pelipper lifecycle probe command missing.' }
if (-not $a21.Contains('PelipperVillagerLifecycleBridge.TrySetEnabled')) { throw '6.6.21 lifecycle route missing.' }
if (-not $a21.Contains('source is still live=')) { throw '6.6.21 source-live verification diagnostic missing.' }
if (-not $a17.Contains('runtimeRoot={PelipperVillagerLifecycleBridge.RootTypeName}')) { throw '6.6.21 runtimeRoot slot diagnostic missing.' }

foreach ($token in @(
    'TrySetStringCollectionMembership',
    'TryInvokeLifecycleGraph',
    'TryInvokeStaticLifecycle',
    'TryInvokeReconcileGraph',
    'TryInvokeStaticReconcile',
    'recall',
    'remove',
    'despawn',
    'dismiss',
    'reconcile',
    'update'
)) {
    if (-not $lifecycleText.Contains($token)) { throw "Lifecycle bridge token missing: $token" }
}

foreach ($token in @(
    'ReconcilePelipperNpcSlotTruthAlpha6618',
    'GetEffectiveCombatCompanionCountAlpha6618',
    'PrepareNpcCompanionRecruitCapacityAlpha6618',
    'physically deployed and must block a third companion'
)) {
    if (-not $a18.Contains($token)) { throw "6.6.18 hard-cap regression token missing: $token" }
}

foreach ($forbidden in @(
    'TrySetActorInvisibleAlpha6613(',
    'PelipperTownCompatibilityService.SetSuppressed(',
    '.Halt();',
    '.controller =',
    '.temporaryController ='
)) {
    if ($lifecycleText.Contains($forbidden) -or $a21.Contains($forbidden)) {
        throw "6.6.21 Pelipper authority regression: $forbidden"
    }
}

if (-not $locatorText.Contains('IsPelipperRuntimeObject')) { throw '6.6.20 runtime-root locator regression.' }
if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService water/pathfinding performance regression.' }
if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService water/pathfinding performance regression.' }

Log 'Building Alpha 6.6.21 Pelipper Villager Lifecycle Recall + Probe Hotfix...'
Log 'RUNTIME ROOT: PelipperTown.ModEntry locator from 6.6.20 preserved.'
Log 'CONFIG SHAPES: bool dictionary plus enabled/disabled/hidden string collections supported.'
Log 'LIFECYCLE: source-native Recall/Remove/Despawn/Dismiss/Deactivate and Spawn/Deploy families enabled.'
Log 'RECONCILE: source-native Update/Refresh/Reconcile/Sync/Apply pass enabled after config changes.'
Log 'VERIFY: Standby does not free a slot until Pelipper sourceLive becomes false.'
Log 'PROBE: teamup_pelipper_probe <NPC> reports exact config/lifecycle candidates from installed Pelipper Town.'
Log 'AUTHORITY: no IsInvisible/Halt/controller fallback; Pelipper remains render/movement owner.'
Log 'HARD CAP: effective source-live 2/2 preflight preserved.'

& dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
& dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

$dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force

$manifestText = Read-Lf $manifest
$manifestText = $manifestText.Replace('%ProjectVersion%', $version)
Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
if (-not (Test-Path $zip)) { throw 'Alpha 6.6.21 ZIP was not created.' }

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Utf8 $shaPath ("$hash  $zipName`r`n")
Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

Log 'PELIPPER VILLAGER LIFECYCLE BRIDGE: ENABLED'
Log 'DISABLED/HIDDEN STRING COLLECTION SUPPORT: ENABLED'
Log 'REMOVE/DESPAWN/RECALL ACTION FAMILY: ENABLED'
Log 'UPDATE/RECONCILE SOURCE PASS: ENABLED'
Log 'PELIPPER PROBE COMMAND: ENABLED'
Log 'NPC RECALL SOURCE-LIVE VERIFICATION: PRESERVED'
Log 'SOURCE-LIVE 2/2 HARD CAP: PRESERVED'
Log 'RENDER/MOVEMENT AUTHORITY: PELIPPER'
Log 'BUILD SUCCESS - ALPHA 6.6.21'
Log "ZIP: $zipName"
Log "SHA256: $hash"
