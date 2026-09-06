$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$baseline = Join-Path $root 'BuildV0_2Alpha6616.ps1'
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha6616 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6616.cs'
$alpha6617 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6617.cs'
$partyManager = Join-Path $root 'src\TeamUp\Core\PartyManager.cs'
$integration = Join-Path $root 'src\TeamUp\Core\CompanionIntegrationService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6617'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_17_PELIPPER_SLOT_TRUTH_NO_GHOSTS_VI.txt'
$version = '0.2.0-alpha.6.6.17'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.17_PELIPPER_SLOT_TRUTH_NO_GHOSTS_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.17_PELIPPER_SLOT_TRUTH_NO_GHOSTS_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

try {
    foreach ($required in @($baseline,$project,$manifest,$modEntry,$alpha6616,$alpha6617,$partyManager,$integration,$follow,$combat,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.17 source: $required" }
    }

    # Keep every accepted 6.6.16 materialization/regression guard before applying the 6.6.17 delta.
    & $baseline
    if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.6.16 baseline builder failed.' }

    if (Test-Path $log) { Remove-Item $log -Force }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.17')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.17 Pelipper Slot Truth No-Ghosts Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $a16 = Read-Lf $alpha6616
    $a17 = Read-Lf $alpha6617
    $partyText = Read-Lf $partyManager
    $integrationText = Read-Lf $integration
    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.17</Version>')) { throw 'Version materialization failed.' }
    if (-not $modText.Contains('build: v0.2.0-alpha.6.6.17')) { throw 'ModEntry build string missing.' }
    if (-not $modText.Contains('Pelipper Slot Truth No-Ghosts Hotfix loaded.')) { throw 'ModEntry loaded string missing.' }
    if (-not $a16.Contains('EnsureAlpha6617EventsRegistered();')) { throw 'Alpha 6.6.17 lazy event registration missing.' }

    foreach ($token in @(
        'ReconcilePelipperPlayerSlotTruthAlpha6617',
        'FindPlayerSummons()',
        'OwnerKind == CompanionOwnerKind.Player',
        'CompanionDeploymentState.Standby',
        'Party.GetActiveCombatCompanionCount() < max',
        'PelipperRenderRestoreAlpha6613',
        'TrySetActorInvisibleAlpha6613(actor, false)',
        'PelipperRenderSuppressedAlpha6613.Clear()',
        'PelipperSourceDeploymentAlpha6613[actor]',
        'teamup_slots',
        'sourceLive='
    )) {
        if (-not $a17.Contains($token)) { throw "Alpha 6.6.17 token missing: $token" }
    }

    if (-not $partyText.Contains('CompanionDeploymentState.Waiting') -or -not $partyText.Contains('CompanionDeploymentState.ReturningHome')) {
        throw 'Shared companion reservation-state policy regressed.'
    }
    if (-not $integrationText.Contains('PelipperTownCompatibilityService.FindPlayerCompanions')) {
        throw 'Pelipper player source discovery missing.'
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }

    Log 'Building Alpha 6.6.17 Pelipper Slot Truth No-Ghosts Hotfix...'
    Log 'SLOT TRUTH: Pelipper live player summon is authoritative; recalled/swapped old player records become Standby and release shared slots.'
    Log 'OWNER LANE: at most one live Pelipper player partner per farmer remains slot-reserved after a source swap.'
    Log 'RECALL: existing Standby player Pokemon promotes from live source presence without guessed IsSummoned/IsDeployed reflection.'
    Log 'VISIBILITY: old render-only Standby suppression is neutralized before world draw; Team Up no longer makes source actors invisible as quota fallback.'
    Log 'QUOTA: Active/Waiting/ReturningHome remain reserved by design; Standby/Inactive remain free.'
    Log 'DIAGNOSTIC: teamup_slots prints reserved units and sourceLive truth.'
    Log 'REGRESSION: 6.6.7+ water performance, 6.6.10 Pelipper movement authority, 6.6.12 curfew, 6.6.14 capture safety and 6.6.16 P/L+R routing preserved.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.17 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $zipName`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

    Log 'PELIPPER SOURCE-TRUTH SLOT RECONCILIATION: ENABLED'
    Log 'STALE PLAYER COMPANION GHOST-SLOT RELEASE: ENABLED'
    Log 'PLAYER RECALL WITHOUT REFLECTION GATE: ENABLED'
    Log 'RENDER-ONLY INVISIBILITY FALLBACK: DISABLED'
    Log 'ACTIVE/WAITING/RETURNINGHOME RESERVATION POLICY: PRESERVED'
    Log 'P / L+R NPC POKEMON ROUTING: PRESERVED'
    Log 'BUILD SUCCESS - ALPHA 6.6.17'
    Log "ZIP: $zipName"
    Log "SHA256: $hash"
}
catch {
    if (-not (Test-Path $log)) { New-Item -ItemType File -Path $log | Out-Null }
    "BUILD FAILED - ALPHA 6.6.17`r`n$($_.Exception.Message)" | Tee-Object -FilePath $log -Append
    throw
}
