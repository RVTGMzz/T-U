$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$followPath = Join-Path $repoRoot 'src\TeamUp\Following\FollowService.cs'
$follow = [System.IO.File]::ReadAllText($followPath, [System.Text.Encoding]::UTF8)
$follow = $follow.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Alpha 6.1.1 follow performance fix could not locate $label." }
    return $text.Replace($old, $new)
}

function Replace-Method([string]$text, [string]$methodName, [string]$nextMethodName, [string]$replacement) {
    $method = [regex]::Escape($methodName)
    $next = [regex]::Escape($nextMethodName)
    $pattern = "(?ms)^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$method\([^\r\n]*\)\s*\{.*?(?=^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$next\()"
    if (-not [regex]::IsMatch($text, $pattern)) {
        throw "Alpha 6.1.1 follow performance fix could not locate method $methodName before $nextMethodName."
    }
    return [regex]::Replace($text, $pattern, $replacement + "`r`n`r`n", 1)
}

if (-not $follow.Contains('RepathCooldownUpdates')) {
    $follow = Replace-Required $follow `
        '    private const float RepathDistanceTiles = 0.90f;' `
        "    private const float RepathDistanceTiles = 0.90f;`r`n    private const int RepathCooldownUpdates = 6;`r`n    private const int UnsafeTargetCooldownUpdates = 15;" `
        'follow throttle constants'
}

if (-not $follow.Contains('_repathCooldowns')) {
    $follow = Replace-Required $follow `
        '    private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();' `
        "    private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();`r`n    private readonly Dictionary<NPC, int> _repathCooldowns = new();`r`n    private readonly HashSet<NPC> _unsafeTargetNpcs = new();" `
        'follow throttle fields'
}

$followTarget = @'
    private void FollowTarget(NPC npc, GameLocation targetLocation, Vector2 targetTile, int finalFacingDirection)
    {
        int cooldown = 0;
        if (_repathCooldowns.TryGetValue(npc, out int existingCooldown) && existingCooldown > 0)
        {
            cooldown = existingCooldown;
            _repathCooldowns[npc] = existingCooldown - 1;
        }

        // Flying mounts and modded traversal can place the Farmer over water, cliffs, or other
        // tiles NPC pathfinding cannot legally reach. Never ask PathFindController to solve an
        // invalid destination every update; that creates severe CPU churn with a full party.
        if (!IsOpen(targetLocation, targetTile))
        {
            SuspendForUnsafeTarget(npc);
            return;
        }

        if (_unsafeTargetNpcs.Remove(npc))
        {
            _repathCooldowns.Remove(npc);
            cooldown = 0;
        }

        if (npc.currentLocation != targetLocation)
        {
            WarpNearTarget(npc, targetLocation, targetTile);
            _repathCooldowns[npc] = RepathCooldownUpdates;
            return;
        }

        float distance = Vector2.Distance(npc.Tile, targetTile);
        ApplyCatchUpSpeed(npc, distance);

        if (distance >= WarpDistanceTiles)
        {
            WarpNearTarget(npc, targetLocation, targetTile);
            _repathCooldowns[npc] = RepathCooldownUpdates;
            return;
        }

        if (distance <= StopDistanceTiles)
        {
            ClearPath(npc);
            RestoreBaseSpeed(npc, keepTracked: true);
            npc.Halt();
            return;
        }

        bool hasForeignController = npc.controller is not null
            && (!_ownedControllers.TryGetValue(npc, out PathFindController? owned)
                || !ReferenceEquals(npc.controller, owned));

        bool targetMovedEnough = _lastTargets.TryGetValue(npc, out Vector2 oldTarget)
            && Vector2.Distance(oldTarget, targetTile) >= RepathDistanceTiles;

        if (hasForeignController)
        {
            ClearPath(npc);
            cooldown = 0;
        }
        else if (targetMovedEnough && cooldown <= 0)
        {
            ClearPath(npc);
        }

        if (npc.temporaryController is not null)
        {
            npc.temporaryController = null;
            npc.Halt();
        }

        // When a mount or fast traversal moves the target every couple of game ticks, keep the
        // current route briefly instead of rebuilding it continuously. If the controller already
        // finished, a very short pause is preferable to a pathfinding storm.
        if (npc.controller is null && cooldown > 0)
            return;

        if (npc.controller is null)
        {
            try
            {
                var controller = new PathFindController(
                    npc,
                    npc.currentLocation,
                    targetTile.ToPoint(),
                    finalFacingDirection);

                npc.controller = controller;
                _ownedControllers[npc] = controller;
                _lastTargets[npc] = targetTile;
                _repathCooldowns[npc] = RepathCooldownUpdates;
            }
            catch (Exception ex)
            {
                _repathCooldowns[npc] = UnsafeTargetCooldownUpdates;
                _monitor.LogOnce($"Pathfinding failed for {npc.Name}: {ex.Message}", LogLevel.Warn);
            }
        }
    }

    private void SuspendForUnsafeTarget(NPC npc)
    {
        _repathCooldowns[npc] = UnsafeTargetCooldownUpdates;
        if (!_unsafeTargetNpcs.Add(npc))
            return;

        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: true);
        npc.Halt();
    }
'@
$follow = Replace-Method $follow 'FollowTarget' 'WarpNearTarget' $followTarget

$isOpen = @'
    private static bool IsOpen(GameLocation location, Vector2 tile)
    {
        if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || tile.X < 0f || tile.Y < 0f)
            return false;

        try
        {
            return location.isTileLocationTotallyClearAndPlaceable((int)tile.X, (int)tile.Y);
        }
        catch
        {
            return false;
        }
    }
'@
$follow = Replace-Method $follow 'IsOpen' 'ResolveCharacter' $isOpen

[System.IO.File]::WriteAllText($followPath, $follow, $utf8NoBom)
Write-Host 'Alpha 6.1.1 follow performance safeguard applied: invalid-target suspension + repath throttle.'
