$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$pelipper = Join-Path $root 'src\TeamUp\Core\PelipperTownCompatibilityService.cs'
$version = '0.2.0-alpha.6.6.7'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) { [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

$projectText = Read-Lf $project
$projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
Write-Utf8 $project $projectText

$modText = Read-Lf $modEntry
$modText = $modText.Replace('build: v0.2.0-alpha.6.6.6', 'build: v0.2.0-alpha.6.6.7')
$modText = $modText.Replace('Team Up! v0.2.0-alpha.6.6.6 Switch Unequip Input Hotfix loaded.', 'Team Up! v0.2.0-alpha.6.6.7 Water Combat Pathfinding Hotfix loaded.')
Write-Utf8 $modEntry $modText

$pelipperText = Read-Lf $pelipper
if (-not $pelipperText.Contains('CombatTargetOptInKey')) {
    $pelipperText = Replace-Required $pelipperText `
        '    public const string SuppressedOwnerKey = "Ronvotri.TeamUp/PelipperSuppressedOwner";' `
        "    public const string SuppressedOwnerKey = \"Ronvotri.TeamUp/PelipperSuppressedOwner\";`n    public const string CombatTargetOptInKey = \"Ronvotri.TeamUp/CombatTarget\";" `
        'Pelipper explicit combat opt-in key'

    $method = @'
    public static bool ShouldExcludeFromTeamUpCombat(NPC actor)
    {
        if (!LooksLikePelipperActor(actor))
            return false;

        // Pelipper Town creatures are source-owned world actors/companions by default. Team Up
        // must never interpret water Pokemon or decorative partners as generic hostile monsters.
        // A provider can explicitly opt a specific actor into Team Up combat if desired.
        return !actor.modData.TryGetValue(CombatTargetOptInKey, out string? raw)
            || !raw.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

'@
    $pelipperText = Replace-Required $pelipperText `
        '    public static bool LooksLikePelipperActor(NPC actor)' `
        ($method + '    public static bool LooksLikePelipperActor(NPC actor)') `
        'Pelipper combat exclusion helper'
}
Write-Utf8 $pelipper $pelipperText

$combatText = Read-Lf $combat
if (-not $combatText.Contains('CombatPathRetryCooldownTicks')) {
    $combatText = Replace-Required $combatText `
        '    private const int FacingHoldDurationTicks = 10;' `
        "    private const int FacingHoldDurationTicks = 10;`n    private const int CombatPathRetryCooldownTicks = 24;`n    private const int CombatMovementPulseTicks = 3;" `
        'combat path retry constants'

    $combatText = Replace-Required $combatText `
        '    private readonly Dictionary<string, int> _facingHoldTicks = new(StringComparer.OrdinalIgnoreCase);' `
        "    private readonly Dictionary<string, int> _facingHoldTicks = new(StringComparer.OrdinalIgnoreCase);`n    private readonly Dictionary<string, int> _combatPathRetryTicks = new(StringComparer.OrdinalIgnoreCase);" `
        'combat retry dictionary'

    $combatText = Replace-Required $combatText `
        '    private int _threatPulseTicks;' `
        "    private int _threatPulseTicks;`n    private int _combatMovementPulse;" `
        'combat movement pulse field'

    $combatText = Replace-Required $combatText `
        '        _facingHoldTicks.Clear();' `
        "        _facingHoldTicks.Clear();`n        _combatPathRetryTicks.Clear();`n        _combatMovementPulse = 0;" `
        'combat retry clear'

    $combatText = Replace-Required $combatText `
        '        TickCooldowns(_facingHoldTicks);' `
        "        TickCooldowns(_facingHoldTicks);`n        TickCooldowns(_combatPathRetryTicks);`n        _combatMovementPulse = (_combatMovementPulse + 1) % CombatMovementPulseTicks;" `
        'combat retry tick'

    $combatText = Replace-Required $combatText `
        '            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))' `
        "            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))`n            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))" `
        'exclude source-owned Pelipper monsters from Team Up combat'

    $combatText = Replace-Required $combatText `
        '                MoveTowardTarget(npc, target, role);' `
        "                if (_combatMovementPulse == 0)`n                    MoveTowardTarget(npc, target, role);" `
        'throttle combat path movement pulse'

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

        Vector2 targetTile = FindApproachTile(FarmerContext.currentLocation, npc.Tile, target.Tile, GetAttackRange(role));
        bool movedEnough = !_lastTargetTiles.TryGetValue(npc.Name, out Vector2 old)
            || Vector2.Distance(old, targetTile) >= RepathThresholdTiles;

        if (npc.controller is not null && !movedEnough)
            return;

        npc.controller = null;
        npc.temporaryController = null;
        try
        {
            var controller = new PathFindController(
                npc,
                FarmerContext.currentLocation,
                targetTile.ToPoint(),
                GetFacingDirection(npc.Position, target.Position));

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
    $combatText = Replace-Required $combatText $oldMove $newMove 'combat path failure cooldown'

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
                if (!IsLightweightCombatTile(location, candidate))
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
    $combatText = Replace-Required $combatText $oldApproach $newApproach 'lightweight combat approach tile validation'

    $combatText = Replace-Required $combatText `
        '            .Where(monster => monster.Health > 0)' `
        "            .Where(monster => monster.Health > 0)`n            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))" `
        'signature AoE Pelipper exclusion'
}
Write-Utf8 $combat $combatText

$combatText = Read-Lf $combat
$pelipperText = Read-Lf $pelipper
foreach ($token in @('CombatPathRetryCooldownTicks = 24', 'CombatMovementPulseTicks = 3', '_combatPathRetryTicks', 'ShouldExcludeFromTeamUpCombat(monster)', 'IsLightweightCombatTile', 'location.isTileOnMap(tile) && location.isTilePassable(tile)')) {
    if (-not $combatText.Contains($token)) { throw "Combat hotfix token missing: $token" }
}
if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService still contains expensive isTileLocationTotallyClearAndPlaceable.' }
foreach ($token in @('CombatTargetOptInKey', 'ShouldExcludeFromTeamUpCombat')) {
    if (-not $pelipperText.Contains($token)) { throw "Pelipper combat exclusion token missing: $token" }
}

Write-Host 'Alpha 6.6.7 source materialization ready.'
Write-Host 'NOTE: final packaging/CI will be added after remaining live-test feedback is folded into this same hotfix.'
