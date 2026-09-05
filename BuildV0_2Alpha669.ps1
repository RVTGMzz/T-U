$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha661 = Join-Path $root 'src\TeamUp\ModEntry.Alpha661.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$alpha669 = Join-Path $root 'src\TeamUp\ModEntry.Alpha669.cs'
$deployment = Join-Path $root 'src\TeamUp\Core\PelipperDeploymentStateService.cs'
$pelipper = Join-Path $root 'src\TeamUp\Core\PelipperTownCompatibilityService.cs'
$healthOverlay = Join-Path $root 'src\TeamUp\UI\PartyHealthOverlayService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$tileSafety = Join-Path $root 'src\TeamUp\Core\PartyTileSafety.cs'
$equipment = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha669'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.9_COMPANION_FLICKER_HEALTH_BARS_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.9_COMPANION_FLICKER_HEALTH_BARS_HOTFIX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.9'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

try {
    foreach ($required in @($project,$manifest,$modEntry,$alpha661,$alpha663,$alpha669,$deployment,$pelipper,$healthOverlay,$follow,$combat,$tileSafety,$equipment,$codex)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.9 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    if (-not $modText.Contains('RegisterAlpha669HotfixEvents();')) {
        $modText = Replace-Required $modText `
            '        RegisterAlpha663HotfixEvents();' `
            "        RegisterAlpha663HotfixEvents();`n        RegisterAlpha669HotfixEvents();" `
            'register Alpha 6.6.9 hotfix events'
    }
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.9')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.9 Companion Flicker + Thin Health Bars Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    $alpha661Text = Read-Lf $alpha661
    $alpha661Text = Replace-Required $alpha661Text `
        '                        PelipperTownCompatibilityService.SetSuppressed(replacementActor, replacement.OwnerCharacterName ?? string.Empty, true);' `
        '                        PelipperDeploymentStateService.SetDesiredDeployment(replacementActor, replacement.OwnerCharacterName ?? string.Empty, false);' `
        'replacement Pelipper soft-standby handoff'
    Write-Utf8 $alpha661 $alpha661Text

    $modText = Read-Lf $modEntry
    $alpha661Text = Read-Lf $alpha661
    $alpha663Text = Read-Lf $alpha663
    $alpha669Text = Read-Lf $alpha669
    $deploymentText = Read-Lf $deployment
    $pelipperText = Read-Lf $pelipper
    $healthText = Read-Lf $healthOverlay
    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat
    $tileSafetyText = Read-Lf $tileSafety
    $equipmentText = Read-Lf $equipment
    $codexText = Read-Lf $codex

    foreach ($token in @('RegisterAlpha669HotfixEvents();','build: v0.2.0-alpha.6.6.9','Companion Flicker + Thin Health Bars Hotfix loaded.')) {
        if (-not $modText.Contains($token)) { throw "ModEntry 6.6.9 token missing: $token" }
    }

    foreach ($token in @('DeploymentStateKey = "Ronvotri.TeamUp/PelipperDeployment"','StandbyValue = "Standby"','SetDesiredDeployment','CleanupLegacySuppressionOnAllPelipperActors','ClearDesiredDeploymentOnAllPelipperActors')) {
        if (-not $deploymentText.Contains($token)) { throw "Pelipper deployment token missing: $token" }
    }
    if (-not $deploymentText.Contains('PelipperTownCompatibilityService.SetSuppressed(actor, owner, false);')) { throw 'Legacy suppression cleanup must only restore false/original state.' }
    if ($deploymentText.Contains('SetSuppressed(actor, owner, true)')) { throw 'Alpha 6.6.9 must never set Pelipper suppression=true.' }

    foreach ($token in @('PelipperDeploymentStateService.SetDesiredDeployment','IsPelipperUnitDeployedAlpha669','Team Up does not own Pelipper visibility/movement in 6.6.9')) {
        if (-not $alpha663Text.Contains($token)) { throw "Alpha663 soft deployment token missing: $token" }
    }
    if ($alpha663Text.Contains('PelipperTownCompatibilityService.SetSuppressed(')) { throw 'Alpha663 still mutates Pelipper visibility at runtime.' }
    if ($alpha661Text.Contains('PelipperTownCompatibilityService.SetSuppressed(')) { throw 'Alpha661 replacement flow still mutates Pelipper visibility.' }
    if (-not $alpha661Text.Contains('PelipperDeploymentStateService.SetDesiredDeployment(replacementActor')) { throw 'Alpha661 replacement flow is not soft deployment.' }

    foreach ($token in @('RenderedWorld','RenderedHud','BuildHealthSnapshotSignatureAlpha669','BroadcastPartySnapshot()','PartyHealthCombatRadiusAlpha669 = 10f')) {
        if (-not $alpha669Text.Contains($token)) { throw "Alpha669 health runtime token missing: $token" }
    }
    foreach ($token in @('HudBarHeight = 5','WorldBarHeight = 4','HudRowHeight = 17','ratio < 0.999f','DrawHud','DrawWorld')) {
        if (-not $healthText.Contains($token)) { throw "Thin health overlay token missing: $token" }
    }
    if ($healthText.Contains('100/100') -or $healthText.Contains('hpText')) { throw 'Overhead health UI regressed to verbose numeric text.' }

    foreach ($token in @('FindLandOpenNear','PartyTileSafety.IsWalkableLandOrBridge(owner.currentLocation, npc.Tile)')) {
        if (-not $followText.Contains($token)) { throw "6.6.8 follow regression missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService regressed to expensive placement query.' }
    foreach ($token in @('CombatPathRetryCooldownTicks = 24','CombatMovementPulseTicks = 3','ShouldExcludeFromTeamUpCombat(monster)','PartyTileSafety.IsWalkableLandOrBridge(location, tile)')) {
        if (-not $combatText.Contains($token)) { throw "6.6.7/6.6.8 combat regression missing: $token" }
    }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService regressed to expensive placement query.' }
    foreach ($token in @('isWaterTile(x, y)','GetLayer("Buildings")','isTilePassable(tile)')) {
        if (-not $tileSafetyText.Contains($token)) { throw "Land-safe tile regression missing: $token" }
    }
    foreach ($token in @('e.Button.IsActionButton()','e.Button.IsUseToolButton()','routedButton = Buttons.A','routedButton = Buttons.X')) {
        if (-not $alpha663Text.Contains($token)) { throw "Switch equip/unequip regression missing: $token" }
    }
    foreach ($token in @('ControllerActivationDebounceMs = 180','ControllerMouseEchoSuppressionMs = 260','DoubleClickWindowMs = 450')) {
        if (-not $equipmentText.Contains($token)) { throw "Equipment regression missing: $token" }
    }
    if ($codexText.Contains('MoveVertical(2)') -or $codexText.Contains('MoveVertical(-2)')) { throw 'Codex one-row navigation regressed.' }

    Log 'Building Alpha 6.6.9 Companion Flicker + Thin Health Bars Hotfix...'
    Log 'FIX: Pelipper Pokemon keep source render/movement authority; Team Up records soft Active/Standby only.'
    Log 'FIX: pre-6.6.9 legacy visibility suppression is restored once, never re-applied.'
    Log 'FEATURE: compact 5px party HUD health bars plus contextual 4px overhead bars.'
    Log 'MULTIPLAYER: host broadcasts party snapshot only when health/downed/state signature changes.'
    Log 'REGRESSION: 6.6.7 performance + 6.6.8 land-safe + Switch input + Codex one-row preserved.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.9 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_9_COMPANION_FLICKER_HEALTH_BARS_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.9'
    Log 'PELIPPER SOURCE RENDER/MOVEMENT AUTHORITY: ENABLED'
    Log 'PELIPPER SOFT DEPLOYMENT MARKERS: ENABLED'
    Log 'PARTY HUD HEALTH BAR HEIGHT: 5PX'
    Log 'OVERHEAD HEALTH BAR HEIGHT: 4PX'
    Log 'MULTIPLAYER HEALTH SNAPSHOT SYNC: ENABLED'
    Log '6.6.7 PERFORMANCE REGRESSION: PRESERVED'
    Log '6.6.8 LAND-SAFE REGRESSION: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
