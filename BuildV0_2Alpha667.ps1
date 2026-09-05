$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$pelipper = Join-Path $root 'src\TeamUp\Core\PelipperTownCompatibilityService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$equipment = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha667'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.7_WATER_COMBAT_PATHFINDING_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.7_WATER_COMBAT_PATHFINDING_HOTFIX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.7'
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
    foreach ($required in @($project,$manifest,$modEntry,$combat,$pelipper,$follow,$alpha663,$equipment,$codex)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.7 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = $modText.Replace('build: v0.2.0-alpha.6.6.6','build: v0.2.0-alpha.6.6.7')
    $modText = $modText.Replace('Team Up! v0.2.0-alpha.6.6.6 Switch Unequip Input Hotfix loaded.','Team Up! v0.2.0-alpha.6.6.7 Water Combat Pathfinding Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    $pelipperText = Read-Lf $pelipper
    if (-not $pelipperText.Contains('CombatTargetOptInKey')) {
        $oldConstants = '    public const string SuppressedOwnerKey = "Ronvotri.TeamUp/PelipperSuppressedOwner";'
        $newConstants = @'
    public const string SuppressedOwnerKey = "Ronvotri.TeamUp/PelipperSuppressedOwner";
    public const string CombatTargetOptInKey = "Ronvotri.TeamUp/CombatTarget";
'@
        $pelipperText = Replace-Required $pelipperText $oldConstants $newConstants 'Pelipper combat opt-in constant'
        $method = @'
    public static bool ShouldExcludeFromTeamUpCombat(NPC actor)
    {
        if (!LooksLikePelipperActor(actor))
            return false;

        return !actor.modData.TryGetValue(CombatTargetOptInKey, out string? raw)
            || !raw.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

'@
        $pelipperText = Replace-Required $pelipperText '    public static bool LooksLikePelipperActor(NPC actor)' ($method + '    public static bool LooksLikePelipperActor(NPC actor)') 'Pelipper combat exclusion helper'
    }
    Write-Utf8 $pelipper $pelipperText

    $combatText = Read-Lf $combat
    if (-not $combatText.Contains('CombatPathRetryCooldownTicks')) {
        $combatText = Replace-Required $combatText '    private const int FacingHoldDurationTicks = 10;' @'
    private const int FacingHoldDurationTicks = 10;
    private const int CombatPathRetryCooldownTicks = 24;
    private const int CombatMovementPulseTicks = 3;
'@ 'combat retry constants'
        $combatText = Replace-Required $combatText '    private readonly Dictionary<string, int> _facingHoldTicks = new(StringComparer.OrdinalIgnoreCase);' @'
    private readonly Dictionary<string, int> _facingHoldTicks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _combatPathRetryTicks = new(StringComparer.OrdinalIgnoreCase);
'@ 'combat retry dictionary'
        $combatText = Replace-Required $combatText '    private int _threatPulseTicks;' @'
    private int _threatPulseTicks;
    private int _combatMovementPulse;
'@ 'combat movement pulse field'
        $combatText = Replace-Required $combatText '        _facingHoldTicks.Clear();' @'
        _facingHoldTicks.Clear();
        _combatPathRetryTicks.Clear();
        _combatMovementPulse = 0;
'@ 'combat retry clear'
        $combatText = Replace-Required $combatText '        TickCooldowns(_facingHoldTicks);' @'
        TickCooldowns(_facingHoldTicks);
        TickCooldowns(_combatPathRetryTicks);
        _combatMovementPulse = (_combatMovementPulse + 1) % CombatMovementPulseTicks;
'@ 'combat retry tick'
        $combatText = Replace-Required $combatText '            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))' @'
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
'@ 'main Pelipper combat exclusion'
        $combatText = Replace-Required $combatText '                MoveTowardTarget(npc, target, role);' @'
                if (_combatMovementPulse == 0)
                    MoveTowardTarget(npc, target, role);
'@ 'combat movement pulse'

        $oldMove = @'
    private void MoveTowardTarget(NPC npc, Monster target, PartyRole role)
    {
        Vector2 targetTile = FindApproachTile(FarmerContext.currentLocation, npc.Tile, target.Tile, GetAttackRange(role));
        bool movedEnough = !_lastTargetTiles.TryGetValue(npc.Name, out Vector2 old)
            || Vector2.Distance(old, targetTile) >= RepathThresholdTiles;

        if (npc.controller is not null && !movedEnough)
            return;

        npc.controller = null;
        npc.temporaryController = null;
        try
        {
            npc.controller = new PathFindController(
                npc,
                FarmerContext.currentLocation,
                targetTile.ToPoint(),
                GetFacingDirection(npc.Position, target.Position));
            _lastTargetTiles[npc.Name] = targetTile;
        }
        catch (Exception ex)
        {
            _monitor.LogOnce($"Combat path failed for {npc.Name}: {ex.Message}", LogLevel.Trace);
        }
    }
'@
        $newMove = @'
    private void MoveTowardTarget(NPC npc, Monster target, PartyRole role)
    {
        if (GetCooldown(_combatPathRetryTicks, npc.Name) > 0 && npc.controller is null)
            return;

        if (!TryFindApproachTile(FarmerContext.currentLocation, npc.Tile, target.Tile, GetAttackRange(role), out Vector2 targetTile))
        {
            npc.controller = null;
            npc.temporaryController = null;
            npc.Halt();
            _combatPathRetryTicks[npc.Name] = CombatPathRetryCooldownTicks;
            return;
        }

        bool movedEnough = !_lastTargetTiles.TryGetValue(npc.Name, out Vector2 old)
            || Vector2.Distance(old, targetTile) >= RepathThresholdTiles;
        if (npc.controller is not null && !movedEnough)
            return;

        npc.controller = null;
        npc.temporaryController = null;
        try
        {
            var controller = new PathFindController(npc, FarmerContext.currentLocation, targetTile.ToPoint(), GetFacingDirection(npc.Position, target.Position));
            _lastTargetTiles[npc.Name] = targetTile;
            if (controller.pathToEndPoint is null || controller.pathToEndPoint.Count == 0)
            {
                _combatPathRetryTicks[npc.Name] = CombatPathRetryCooldownTicks;
                npc.Halt();
                return;
            }
            npc.controller = controller;
        }
        catch (Exception ex)
        {
            _combatPathRetryTicks[npc.Name] = CombatPathRetryCooldownTicks;
            _monitor.LogOnce($"Combat path failed for {npc.Name}: {ex.Message}", LogLevel.Trace);
        }
    }
'@
        $combatText = Replace-Required $combatText $oldMove $newMove 'combat path retry gate'

        $oldApproach = @'
    private static Vector2 FindApproachTile(GameLocation location, Vector2 from, Vector2 target, float range)
    {
        int radius = Math.Max(1, (int)Math.Floor(range));
        Vector2 best = target;
        float bestDistance = float.MaxValue;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                Vector2 candidate = target + new Vector2(x, y);
                if (!location.isTileLocationTotallyClearAndPlaceable((int)candidate.X, (int)candidate.Y))
                    continue;
                float distance = Vector2.DistanceSquared(candidate, from);
                if (distance >= bestDistance)
                    continue;
                best = candidate;
                bestDistance = distance;
            }
        }
        return best;
    }
'@
        $newApproach = @'
    private static bool TryFindApproachTile(GameLocation location, Vector2 from, Vector2 target, float range, out Vector2 best)
    {
        int radius = Math.Max(1, (int)Math.Floor(range));
        best = Vector2.Zero;
        float bestDistance = float.MaxValue;
        bool found = false;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;
                Vector2 candidate = target + new Vector2(x, y);
                if (!IsLightweightCombatTile(location, candidate))
                    continue;
                float distance = Vector2.DistanceSquared(candidate, from);
                if (distance >= bestDistance)
                    continue;
                best = candidate;
                bestDistance = distance;
                found = true;
            }
        }
        return found;
    }

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
        $combatText = Replace-Required $combatText $oldApproach $newApproach 'lightweight approach search'

        $oldLiving = @'
    private List<Monster> GetLivingMonstersNear(Vector2 centerTile, float radiusTiles)
    {
        return FarmerContext.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => Vector2.Distance(monster.Tile, centerTile) <= radiusTiles)
            .ToList();
    }
'@
        $newLiving = @'
    private List<Monster> GetLivingMonstersNear(Vector2 centerTile, float radiusTiles)
    {
        return FarmerContext.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            .Where(monster => Vector2.Distance(monster.Tile, centerTile) <= radiusTiles)
            .ToList();
    }
'@
        $combatText = Replace-Required $combatText $oldLiving $newLiving 'AoE Pelipper exclusion'
    }
    Write-Utf8 $combat $combatText

    $combatText = Read-Lf $combat
    $pelipperText = Read-Lf $pelipper
    $followText = Read-Lf $follow
    $alphaText = Read-Lf $alpha663
    $equipmentText = Read-Lf $equipment
    $codexText = Read-Lf $codex

    foreach ($token in @('CombatPathRetryCooldownTicks = 24','CombatMovementPulseTicks = 3','_combatPathRetryTicks','ShouldExcludeFromTeamUpCombat(monster)','TryFindApproachTile','IsLightweightCombatTile','location.isTileOnMap(tile) && location.isTilePassable(tile)','_combatMovementPulse == 0')) {
        if (-not $combatText.Contains($token)) { throw "Combat 6.6.7 token missing: $token" }
    }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService still contains expensive isTileLocationTotallyClearAndPlaceable.' }
    foreach ($token in @('CombatTargetOptInKey','ShouldExcludeFromTeamUpCombat')) { if (-not $pelipperText.Contains($token)) { throw "Pelipper token missing: $token" } }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService regressed to expensive placement query.' }
    foreach ($token in @('e.Button.IsActionButton()','e.Button.IsUseToolButton()','routedButton = Buttons.A','routedButton = Buttons.X')) { if (-not $alphaText.Contains($token)) { throw "Switch input regression missing: $token" } }
    foreach ($token in @('ControllerActivationDebounceMs = 180','ControllerMouseEchoSuppressionMs = 260','DoubleClickWindowMs = 450')) { if (-not $equipmentText.Contains($token)) { throw "Equipment regression missing: $token" } }
    if ($codexText.Contains('MoveVertical(2)') -or $codexText.Contains('MoveVertical(-2)')) { throw 'Codex one-row navigation regressed.' }

    Log 'Building Alpha 6.6.7 Water Combat Pathfinding Hotfix...'
    Log 'FIX: source-owned Pelipper water/decorative actors excluded from Team Up combat unless opted in.'
    Log 'FIX: lightweight combat approach tile validation.'
    Log 'FIX: unreachable path retry cooldown = 24 ticks.'
    Log 'FIX: combat movement path pulse = every 3 ticks.'
    Log 'REGRESSION: Switch equip/unequip + Codex one-row navigation preserved.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.7 ZIP was not created.' }
    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_7_WATER_COMBAT_PATHFINDING_HOTFIX_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.7'
    Log 'PELIPPER COMBAT TARGET FILTER: ENABLED'
    Log 'LIGHTWEIGHT COMBAT TILE SEARCH: ENABLED'
    Log 'UNREACHABLE PATH RETRY COOLDOWN: ENABLED'
    Log 'COMBAT PATH MOVEMENT PULSE: ENABLED'
    Log 'SWITCH EQUIP/UNEQUIP REGRESSION: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
