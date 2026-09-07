$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$tileSafety = Join-Path $root 'src\TeamUp\Core\PartyTileSafety.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$equipment = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha668'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.8_LAND_SAFE_FOLLOW_TARGETS_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.8_LAND_SAFE_FOLLOW_TARGETS_HOTFIX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.8'
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
    foreach ($required in @($project,$manifest,$modEntry,$follow,$combat,$tileSafety,$alpha663,$equipment,$codex)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.8 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = $modText.Replace('build: v0.2.0-alpha.6.6.7','build: v0.2.0-alpha.6.6.8')
    $modText = $modText.Replace('Team Up! v0.2.0-alpha.6.6.7 Water Combat Pathfinding Hotfix loaded.','Team Up! v0.2.0-alpha.6.6.8 Land-Safe Follow Targets Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    $followText = Read-Lf $follow
    if (-not $followText.Contains('FindLandOpenNear')) {
        $oldPartyFollow = @'
            PrepareForParty(npc, recruiterId);
            Vector2 targetTile = FindPlayerFollowTile(owner.currentLocation, owner.Tile, index);
            FollowTarget(npc, owner.currentLocation, targetTile, owner.FacingDirection);
'@
        $newPartyFollow = @'
            PrepareForParty(npc, recruiterId);
            Vector2? targetTile = FindPlayerFollowTile(owner.currentLocation, owner.Tile, index);
            if (!targetTile.HasValue)
            {
                SuspendForUnsafeTarget(npc);
                continue;
            }

            // Alpha 6.6.7 intentionally made target checks lightweight, but some water tiles are
            // passable to the map even though humanoid NPCs should never stand there. Repair any
            // already-stranded party member immediately to the nearest safe formation tile.
            if (ReferenceEquals(npc.currentLocation, owner.currentLocation)
                && !PartyTileSafety.IsWalkableLandOrBridge(owner.currentLocation, npc.Tile))
            {
                WarpNearTarget(npc, owner.currentLocation, targetTile.Value);
                _repathCooldowns[npc] = RepathCooldownUpdates;
                continue;
            }

            FollowTarget(npc, owner.currentLocation, targetTile.Value, owner.FacingDirection);
'@
        $followText = Replace-Required $followText $oldPartyFollow $newPartyFollow 'party member safe target + water rescue'

        $oldFindPlayer = @'
    private static Vector2 FindPlayerFollowTile(GameLocation location, Vector2 farmerTile, int slotIndex)
    {
        Point offset = FormationOffsets[Math.Clamp(slotIndex, 0, FormationOffsets.Length - 1)];
        return FindOpenNear(location, farmerTile, offset);
    }
'@
        $newFindPlayer = @'
    private static Vector2? FindPlayerFollowTile(GameLocation location, Vector2 farmerTile, int slotIndex)
    {
        Point offset = FormationOffsets[Math.Clamp(slotIndex, 0, FormationOffsets.Length - 1)];
        return FindLandOpenNear(location, farmerTile, offset);
    }

    private static Vector2? FindLandOpenNear(GameLocation location, Vector2 anchorTile, Point preferredOffset)
    {
        Vector2 preferred = anchorTile + new Vector2(preferredOffset.X, preferredOffset.Y);
        if (PartyTileSafety.IsWalkableLandOrBridge(location, preferred))
            return preferred;

        foreach (Point offset in OpenSearchOffsets)
        {
            if (offset == preferredOffset)
                continue;

            Vector2 candidate = anchorTile + new Vector2(offset.X, offset.Y);
            if (PartyTileSafety.IsWalkableLandOrBridge(location, candidate))
                return candidate;
        }

        // Never fall back to a known-invalid preferred tile. If the Farmer is on a mount or
        // traversal surface with no nearby humanoid-safe tile, hold followers until land returns.
        return PartyTileSafety.IsWalkableLandOrBridge(location, anchorTile) ? anchorTile : null;
    }
'@
        $followText = Replace-Required $followText $oldFindPlayer $newFindPlayer 'land-safe human formation finder'
    }
    Write-Utf8 $follow $followText

    $combatText = Read-Lf $combat
    $oldCombatTile = @'
    private static bool IsLightweightCombatTile(GameLocation location, Vector2 tile)
    {
        if (tile.X < 0f || tile.Y < 0f || float.IsNaN(tile.X) || float.IsNaN(tile.Y))
            return false;
        try
        {
            return location.isTileOnMap(tile) && location.isTilePassable(tile);
        }
        catch
        {
            return false;
        }
    }
'@
    $newCombatTile = @'
    private static bool IsLightweightCombatTile(GameLocation location, Vector2 tile)
        => PartyTileSafety.IsWalkableLandOrBridge(location, tile);
'@
    $combatText = Replace-Required $combatText $oldCombatTile $newCombatTile 'land-safe combat approach tile validation'
    Write-Utf8 $combat $combatText

    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat
    $tileSafetyText = Read-Lf $tileSafety
    $alphaText = Read-Lf $alpha663
    $equipmentText = Read-Lf $equipment
    $codexText = Read-Lf $codex

    foreach ($token in @(
        'Vector2? targetTile = FindPlayerFollowTile',
        'FindLandOpenNear',
        'PartyTileSafety.IsWalkableLandOrBridge(owner.currentLocation, npc.Tile)',
        'WarpNearTarget(npc, owner.currentLocation, targetTile.Value)',
        'return PartyTileSafety.IsWalkableLandOrBridge(location, anchorTile) ? anchorTile : null'
    )) {
        if (-not $followText.Contains($token)) { throw "Follow land-safety token missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService regressed to expensive placement query.' }

    foreach ($token in @('isWaterTile(x, y)','GetLayer("Buildings")','isTileOnMap(tile)','isTilePassable(tile)')) {
        if (-not $tileSafetyText.Contains($token)) { throw "PartyTileSafety token missing: $token" }
    }

    if (-not $combatText.Contains('=> PartyTileSafety.IsWalkableLandOrBridge(location, tile);')) { throw 'CombatService is not using land-safe approach validation.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService regressed to expensive placement query.' }
    foreach ($token in @('CombatPathRetryCooldownTicks = 24','CombatMovementPulseTicks = 3','ShouldExcludeFromTeamUpCombat(monster)')) {
        if (-not $combatText.Contains($token)) { throw "6.6.7 combat regression missing: $token" }
    }
    foreach ($token in @('e.Button.IsActionButton()','e.Button.IsUseToolButton()','routedButton = Buttons.A','routedButton = Buttons.X')) {
        if (-not $alphaText.Contains($token)) { throw "Switch input regression missing: $token" }
    }
    foreach ($token in @('ControllerActivationDebounceMs = 180','ControllerMouseEchoSuppressionMs = 260','DoubleClickWindowMs = 450')) {
        if (-not $equipmentText.Contains($token)) { throw "Equipment regression missing: $token" }
    }
    if ($codexText.Contains('MoveVertical(2)') -or $codexText.Contains('MoveVertical(-2)')) { throw 'Codex one-row navigation regressed.' }

    Log 'Building Alpha 6.6.8 Land-Safe Follow Targets Hotfix...'
    Log 'FIX: humanoid party formation rejects bare water while allowing bridge overlays.'
    Log 'FIX: party members already stranded on water are rescued to a safe formation tile.'
    Log 'FIX: no invalid preferred-tile fallback when no safe human formation tile exists.'
    Log 'FIX: combat approach uses the same lightweight land/bridge safety rule.'
    Log 'REGRESSION: 6.6.7 water performance, Pelipper filtering, Switch equip/unequip and Codex one-row navigation preserved.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.8 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_8_LAND_SAFE_FOLLOW_TARGETS_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.8'
    Log 'HUMANOID BARE-WATER REJECTION: ENABLED'
    Log 'BRIDGE OVERLAY ALLOWANCE: ENABLED'
    Log 'STRANDED NPC WATER RESCUE: ENABLED'
    Log 'COMBAT LAND-SAFE APPROACH: ENABLED'
    Log '6.6.7 PERFORMANCE REGRESSION: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
