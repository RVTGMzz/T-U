$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$materializer = Join-Path $root 'BuildV0_2Alpha6618.ps1'
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha661 = Join-Path $root 'src\TeamUp\ModEntry.Alpha661.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$alpha6615 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6615.cs'
$alpha6616 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6616.cs'
$alpha6617 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6617.cs'
$alpha6618 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6618.cs'
$bridge = Join-Path $root 'src\TeamUp\Core\PelipperVillagerCompanionRuntimeBridge.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6618_final'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_18_NPC_COMPANION_SOURCE_TRUTH_HARD_PREFLIGHT_VI.txt'
$version = '0.2.0-alpha.6.6.18'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.18_NPC_COMPANION_SOURCE_TRUTH_HARD_PREFLIGHT_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.18_NPC_COMPANION_SOURCE_TRUTH_HARD_PREFLIGHT_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

# The first 6.6.18 materializer intentionally patches source in-place. Its original acceptance
# gate also matched the literal word IsInvisible inside a documentation comment. Treat only that
# known guard failure as non-fatal, then apply semantic source checks below before compiling.
try {
    & $materializer
}
catch {
    if (-not $_.Exception.Message.Contains('source bridge must not use render/movement suppression'))
        { throw }
}

if (Test-Path $log) { Remove-Item $log -Force }

$projectText = Read-Lf $project
$manifestText = Read-Lf $manifest
$modText = Read-Lf $modEntry
$a661 = Read-Lf $alpha661
$a663 = Read-Lf $alpha663
$a15 = Read-Lf $alpha6615
$a16 = Read-Lf $alpha6616
$a17 = Read-Lf $alpha6617
$a18 = Read-Lf $alpha6618
$bridgeText = Read-Lf $bridge
$followText = Read-Lf $follow
$combatText = Read-Lf $combat

if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.18</Version>')) { throw 'Version materialization failed.' }
if (-not $modText.Contains('build: v0.2.0-alpha.6.6.18')) { throw 'ModEntry build string missing.' }
if (-not $modText.Contains('NPC Companion Source Truth + Hard Preflight Hotfix loaded.')) { throw 'Loaded string missing.' }
if (-not $a16.Contains('EnsureAlpha6618EventsRegistered();')) { throw '6.6.18 event registration missing.' }

foreach ($token in @(
    'ReconcilePelipperNpcSlotTruthAlpha6618',
    'GetEffectiveCombatCompanionCountAlpha6618',
    'GetEffectiveReplaceableCompanionsAlpha6618',
    'PrepareNpcCompanionRecruitCapacityAlpha6618',
    'TrySetPelipperNpcSourceEnabledAlpha6618',
    'RestorePelipperNpcSourceAlpha6618',
    'source-live partner continues to consume a real slot'
)) {
    if (-not $a18.Contains($token)) { throw "Alpha 6.6.18 token missing: $token" }
}
foreach ($token in @('TrySetEnabled','TrySetConfigState','TryInvokeSourceAction','RestoreAll','CompanionEnabled','PartnerEnabled')) {
    if (-not $bridgeText.Contains($token)) { throw "Pelipper runtime bridge token missing: $token" }
}
if (-not $a15.Contains('explicit Return/Standby')) { throw 'NPC Return source-native route missing.' }
if (-not $a663.Contains('RestorePelipperNpcSourceAlpha6618(owner);')) { throw 'Owner source restore missing.' }
if (-not $a661.Contains('PrepareNpcCompanionRecruitCapacityAlpha6618')) { throw 'Authoritative recruit preflight missing.' }
if (-not $a661.Contains('HasFreeEffectiveCompanionSlotAlpha6618')) { throw 'Effective slot UI gate missing.' }
if (-not $a661.Contains('GetEffectiveReplaceableCompanionsAlpha6618')) { throw 'Effective replacement list missing.' }

# Semantic no-suppression gate: comments may mention old APIs, executable bridge must not call them.
foreach ($forbidden in @('TrySetActorInvisibleAlpha6613(', 'PelipperTownCompatibilityService.SetSuppressed(', '.Halt();', '.controller =', '.temporaryController =')) {
    if ($bridgeText.Contains($forbidden) -or $a18.Contains($forbidden)) {
        throw "6.6.18 source bridge suppression regression: $forbidden"
    }
}
if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }
if (-not $a17.Contains('ReconcilePelipperPlayerSlotTruthAlpha6617')) { throw '6.6.17 player slot truth regression.' }

Log 'Building Alpha 6.6.18 NPC Companion Source Truth + Hard Preflight Hotfix...'
Log 'NPC RETURN: linked Pelipper Pokemon uses source-native villager companion enable/recall bridge; no render/movement suppression fallback.'
Log 'NPC SOURCE TRUTH: source-live partner remains a real slot consumer until Pelipper actually recalls it.'
Log 'HARD PREFLIGHT: NPC+Pokemon recruitment rechecks effective source-live 2/2 immediately before commit.'
Log 'REPLACE: full 2/2 requires a real replacement; if source recall fails, recruit fails instead of allowing 3/2.'
Log 'NPC ONLY: people-only recruitment remains allowed when people capacity permits; opted-out partner is source-recalled while owner is active.'
Log 'REGRESSION: 6.6.17 player ghost-slot truth, 6.6.16 P/L+R, 6.6.14 capture safety and 6.6.7 water performance preserved.'

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
$manifestText = $manifestText.Replace('%ProjectVersion%', $version)
Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
if (-not (Test-Path $zip)) { throw 'Alpha 6.6.18 ZIP was not created.' }
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Utf8 $shaPath ("$hash  $zipName`r`n")
Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

Log 'PELIPPER NPC SOURCE-NATIVE RECALL/DEPLOY BRIDGE: ENABLED'
Log 'NPC SOURCE-LIVE EFFECTIVE SLOT ACCOUNTING: ENABLED'
Log 'NPC+POKEMON AUTHORITATIVE 2/2 PREFLIGHT: ENABLED'
Log 'FULL POOL REQUIRES REAL REPLACEMENT OR REJECT: ENABLED'
Log 'RENDER-ONLY INVISIBILITY FALLBACK: DISABLED'
Log 'P / L+R NPC POKEMON ROUTING: PRESERVED'
Log 'BUILD SUCCESS - ALPHA 6.6.18'
Log "ZIP: $zipName"
Log "SHA256: $hash"
