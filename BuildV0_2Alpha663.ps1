$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha661 = Join-Path $root 'src\TeamUp\ModEntry.Alpha661.cs'
$alpha662 = Join-Path $root 'src\TeamUp\ModEntry.Alpha662.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$integration = Join-Path $root 'src\TeamUp\Core\CompanionIntegrationService.cs'
$pelipper = Join-Path $root 'src\TeamUp\Core\PelipperTownCompatibilityService.cs'
$party = Join-Path $root 'src\TeamUp\Core\PartyManager.cs'
$equipment = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$surge = Join-Path $root 'src\TeamUp\Combat\MonsterSurgeService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha663'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.3_LIVE_TEST_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.3_LIVE_TEST_HOTFIX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.3'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function Write-Utf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

try {
    foreach ($required in @($project, $manifest, $modEntry, $alpha661, $alpha662, $alpha663, $integration, $pelipper, $party, $equipment, $codex, $follow, $surge)) {
        if (-not (Test-Path $required)) { throw "Missing required Alpha 6.6.3 source: $required" }
    }

    # -------------------- version + activation --------------------
    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    if (-not $modText.Contains('RegisterAlpha663HotfixEvents();')) {
        $modText = Replace-Required $modText `
            '        RegisterAlpha662MultiplayerEvents();' `
            "        RegisterAlpha662MultiplayerEvents();`n        RegisterAlpha663HotfixEvents();" `
            'register Alpha 6.6.3 hotfix events'
    }
    $modText = $modText.Replace(
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.2',
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.3')
    $modText = $modText.Replace(
        'Team Up! v0.2.0-alpha.6.6.2 Party Tactics + Shared Capacity UI loaded.',
        'Team Up! v0.2.0-alpha.6.6.3 Live Test Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    # -------------------- Equipment controller: pointer A equips, focused A remains deterministic --------------------
    $equipmentText = Read-Lf $equipment
    if (-not $equipmentText.Contains('_preferFocusedGamepadActivation')) {
        $equipmentText = Replace-Required $equipmentText `
            "    private Item? _hoveredItem;" `
            "    private Item? _hoveredItem;`n    private bool _preferFocusedGamepadActivation = true;`n    private Point _lastHoverPoint = new(int.MinValue, int.MinValue);" `
            'equipment controller input-mode fields'
    }

    if (-not $equipmentText.Contains('Point hoverPoint = new(x, y);')) {
        $oldHover = @'
    public override void performHoverAction(int x, int y)
    {
        _hoveredItem = null;
'@
        $newHover = @'
    public override void performHoverAction(int x, int y)
    {
        Point hoverPoint = new(x, y);
        if (hoverPoint != _lastHoverPoint)
        {
            _lastHoverPoint = hoverPoint;
            _preferFocusedGamepadActivation = false;
        }

        _hoveredItem = null;
'@
        $equipmentText = Replace-Required $equipmentText $oldHover $newHover 'equipment pointer hover mode'
    }

    if (-not $equipmentText.Contains('Buttons.RightThumbstickUp')) {
        $anchor = @'
    public override void receiveGamePadButton(Buttons b)
    {
        if (b is Buttons.B or Buttons.Back)
'@
        $replacement = @'
    public override void receiveGamePadButton(Buttons b)
    {
        if (b is Buttons.RightThumbstickUp or Buttons.RightThumbstickDown or Buttons.RightThumbstickLeft or Buttons.RightThumbstickRight)
        {
            _preferFocusedGamepadActivation = false;
            return;
        }

        if (b is Buttons.B or Buttons.Back)
'@
        $equipmentText = Replace-Required $equipmentText $anchor $replacement 'equipment right-stick pointer mode'
    }

    foreach ($pair in @(
        @('            MoveHorizontal(-1);', '            _preferFocusedGamepadActivation = true;`n            MoveHorizontal(-1);'),
        @('            MoveHorizontal(1);', '            _preferFocusedGamepadActivation = true;`n            MoveHorizontal(1);'),
        @('            MoveVertical(-1);', '            _preferFocusedGamepadActivation = true;`n            MoveVertical(-1);'),
        @('            MoveVertical(1);', '            _preferFocusedGamepadActivation = true;`n            MoveVertical(1);')
    )) {
        $old = $pair[0]
        $new = $pair[1].Replace('`n', "`n")
        if ($equipmentText.Contains($old) -and -not $equipmentText.Contains($new)) {
            # Replace only the gamepad section by targeting the first matching branch after receiveGamePadButton.
            $gamepadIndex = $equipmentText.IndexOf('    public override void receiveGamePadButton(Buttons b)')
            $moveIndex = $equipmentText.IndexOf($old, $gamepadIndex)
            if ($moveIndex -ge 0) {
                $equipmentText = $equipmentText.Substring(0, $moveIndex) + $new + $equipmentText.Substring($moveIndex + $old.Length)
            }
        }
    }

    if (-not $equipmentText.Contains('TryActivateControllerPointer()')) {
        $oldA = @'
        if (b == Buttons.A)
        {
            ActivateFocused();
            return;
        }
'@
        $newA = @'
        if (b == Buttons.A)
        {
            if (!_preferFocusedGamepadActivation && TryActivateControllerPointer())
                return;

            ActivateFocused();
            return;
        }
'@
        $equipmentText = Replace-Required $equipmentText $oldA $newA 'equipment controller A activation bridge'

        $helper = @'
    private bool TryActivateControllerPointer()
    {
        int x = Game1.getMouseX();
        int y = Game1.getMouseY();

        for (int i = 0; i < _inventoryBounds.Length; i++)
        {
            if (!_inventoryBounds[i].Contains(x, y))
                continue;

            _focusInventory = true;
            _inventoryCursor = i;
            if (i < Game1.player.Items.Count && Game1.player.Items[i] is Item item)
            {
                EquipmentSlot? naturalSlot = GetNaturalSlot(item);
                if (naturalSlot.HasValue)
                    _selectedSlot = naturalSlot.Value;
            }

            EquipInventoryIndex(i);
            return true;
        }

        for (int i = 0; i < _slotBounds.Length; i++)
        {
            if (!_slotBounds[i].Contains(x, y))
                continue;

            _focusInventory = false;
            _loadoutFocusIndex = i;
            _selectedSlot = (EquipmentSlot)i;
            ActivateFocused();
            return true;
        }

        if (_autoEquipBounds.Contains(x, y))
        {
            _focusInventory = false;
            _loadoutFocusIndex = 3;
            AutoEquipBest();
            return true;
        }

        if (_unequipBounds.Contains(x, y))
        {
            _focusInventory = false;
            _loadoutFocusIndex = 4;
            UnequipSelected();
            return true;
        }

        if (_backBounds.Contains(x, y))
        {
            ReturnToMemberMenu();
            return true;
        }

        return false;
    }

'@
        $equipmentText = Replace-Required $equipmentText `
            '    private void MoveHorizontal(int delta)' `
            ($helper + '    private void MoveHorizontal(int delta)') `
            'equipment controller pointer helper'
    }
    Write-Utf8 $equipment $equipmentText

    # -------------------- Codex controller: viewport/selection sync + faster left-stick rows --------------------
    $codexText = Read-Lf $codex
    $oldScroll = @'
        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        int maxOffset = Math.Max(0, filtered.Count - _visibleRows);
        _scrollOffset = Math.Clamp(_scrollOffset + (direction < 0 ? 1 : -1), 0, maxOffset);
        _selectedIndex = Math.Clamp(_selectedIndex, 0, Math.Max(0, filtered.Count - 1));
        Game1.playSound("shiny4");
'@
    $newScroll = @'
        List<NpcCombatProfile> filtered = GetFilteredProfiles();
        int maxOffset = Math.Max(0, filtered.Count - _visibleRows);
        int scrollStep = direction < 0 ? 2 : -2;
        _scrollOffset = Math.Clamp(_scrollOffset + scrollStep, 0, maxOffset);
        if (filtered.Count > 0)
        {
            int firstVisible = _scrollOffset;
            int lastVisible = Math.Min(filtered.Count - 1, _scrollOffset + _visibleRows - 1);
            _selectedIndex = Math.Clamp(_selectedIndex, firstVisible, lastVisible);
        }
        else
        {
            _selectedIndex = 0;
        }
        Game1.playSound("shiny4");
'@
    $codexText = Replace-Required $codexText $oldScroll $newScroll 'Codex right-stick viewport selection sync'

    $oldPadVertical = @'
        else if (b is Buttons.DPadUp or Buttons.LeftThumbstickUp)
            MoveVertical(-1);
        else if (b is Buttons.DPadDown or Buttons.LeftThumbstickDown)
            MoveVertical(1);
'@
    $newPadVertical = @'
        else if (b == Buttons.DPadUp)
            MoveVertical(-1);
        else if (b == Buttons.LeftThumbstickUp)
            MoveVertical(-2);
        else if (b == Buttons.DPadDown)
            MoveVertical(1);
        else if (b == Buttons.LeftThumbstickDown)
            MoveVertical(2);
'@
    $codexText = Replace-Required $codexText $oldPadVertical $newPadVertical 'Codex faster left-stick list navigation'
    Write-Utf8 $codex $codexText

    # -------------------- Follow performance + provider authority --------------------
    $followText = Read-Lf $follow

    if (-not $followText.Contains('private static readonly Point[] OpenSearchOffsets')) {
        $anchor = @'
    private static readonly Point[] CompanionOffsets =
    {
        new(0, 1),
        new(-1, 0),
        new(1, 0),
        new(0, -1),
        new(-1, 1),
        new(1, 1)
    };
'@
        $replacement = @'
    private static readonly Point[] CompanionOffsets =
    {
        new(0, 1),
        new(-1, 0),
        new(1, 0),
        new(0, -1),
        new(-1, 1),
        new(1, 1)
    };

    // Small deterministic fallback ring. The old radius-square search could perform dozens of
    // collision queries per follower every update around water, bridges, cliffs and narrow paths.
    private static readonly Point[] OpenSearchOffsets =
    {
        new(0, 0),
        new(0, 1), new(-1, 0), new(1, 0), new(0, -1),
        new(-1, 1), new(1, 1), new(-1, -1), new(1, -1),
        new(0, 2), new(-2, 0), new(2, 0), new(0, -2),
        new(-1, 2), new(1, 2), new(-2, 1), new(2, 1)
    };
'@
        $followText = Replace-Required $followText $anchor $replacement 'Follow bounded open-tile fallback offsets'
    }

    $oldReleaseUnits = @'
        foreach (CompanionUnitData unit in companionUnits.Where(unit => unit.RecruiterId == recruiterId))
        {
            NPC? npc = ResolveCharacter(unit.CharacterName);
            if (npc is not null)
                ReleaseToVanilla(npc);
        }
'@
    $newReleaseUnits = @'
        foreach (CompanionUnitData unit in companionUnits.Where(unit => unit.RecruiterId == recruiterId))
        {
            if (PelipperTownCompatibilityService.IsSourceControlled(unit))
                continue;

            NPC? npc = ResolveCharacter(unit.CharacterName);
            if (npc is not null)
                ReleaseToVanilla(npc);
        }
'@
    $followText = Replace-Required $followText $oldReleaseUnits $newReleaseUnits 'Follow release source-owned Pelipper guard'

    if (-not $followText.Contains('if (PelipperTownCompatibilityService.IsSourceControlled(unit))`n                continue;'.Replace('`n', "`n"))) {
        $oldLoop = @'
        foreach (CompanionUnitData unit in activeUnits)
        {
            if (_releasedCharacters.Contains(unit.CharacterName))
                continue;
'@
        $newLoop = @'
        foreach (CompanionUnitData unit in activeUnits)
        {
            // Pelipper Town remains movement authority for its own live Pokémon. Team Up only
            // accounts for deployment/quota, avoiding two pathfinding controllers fighting.
            if (PelipperTownCompatibilityService.IsSourceControlled(unit))
                continue;
            if (_releasedCharacters.Contains(unit.CharacterName))
                continue;
'@
        $followText = Replace-Required $followText $oldLoop $newLoop 'Follow source-controlled companion skip'
    }

    $oldOpenSearch = @'
    private static Vector2 FindOpenNear(GameLocation location, Vector2 anchorTile, Point preferredOffset)
    {
        Vector2 preferred = anchorTile + new Vector2(preferredOffset.X, preferredOffset.Y);
        if (IsOpen(location, preferred))
            return preferred;

        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector2 candidate = anchorTile + new Vector2(x, y);
                    if (IsOpen(location, candidate))
                        return candidate;
                }
            }
        }

        return preferred;
    }

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
    $newOpenSearch = @'
    private static Vector2 FindOpenNear(GameLocation location, Vector2 anchorTile, Point preferredOffset)
    {
        Vector2 preferred = anchorTile + new Vector2(preferredOffset.X, preferredOffset.Y);
        if (IsOpen(location, preferred))
            return preferred;

        foreach (Point offset in OpenSearchOffsets)
        {
            if (offset == preferredOffset)
                continue;

            Vector2 candidate = anchorTile + new Vector2(offset.X, offset.Y);
            if (IsOpen(location, candidate))
                return candidate;
        }

        return preferred;
    }

    private static bool IsOpen(GameLocation location, Vector2 tile)
    {
        if (float.IsNaN(tile.X) || float.IsNaN(tile.Y) || tile.X < 0f || tile.Y < 0f)
            return false;

        try
        {
            // Follow targets only need a legal map/passable tile. Avoid the much heavier
            // totally-clear/placeable query, which also reacts badly to crowds on narrow bridges.
            return location.isTileOnMap(tile) && location.isTilePassable(tile);
        }
        catch
        {
            return false;
        }
    }
'@
    $followText = Replace-Required $followText $oldOpenSearch $newOpenSearch 'Follow low-cost water/narrow tile validation'
    Write-Utf8 $follow $followText

    # -------------------- Alpha 6.6.1 coordinator: Pelipper quota + source ownership --------------------
    $alpha661Text = Read-Lf $alpha661
    $alpha661Text = $alpha661Text.Replace('            if (e.IsMultipleOf(2))', '            if (e.IsMultipleOf(4))')

    $oldRuntimeAdd = @'
                NPC? actor = Game1.getCharacterFromName(descriptor.CharacterName);
                if (actor is not null && result == CompanionAddResult.AddedActive)
                    Follow.TakePartyControl(actor, recruiterId);
'@
    $newRuntimeAdd = @'
                NPC? actor = PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor)
                    ? PelipperTownCompatibilityService.ResolveActor(descriptor)
                    : Game1.getCharacterFromName(descriptor.CharacterName);
                if (PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor))
                {
                    CompanionUnitData? registered = Party.GetCompanionByUnitId(descriptor.UnitId, recruiterId);
                    ApplyPelipperLinkedDeploymentAlpha663(registered, descriptor);
                }
                else if (actor is not null && result == CompanionAddResult.AddedActive)
                {
                    Follow.TakePartyControl(actor, recruiterId);
                }
'@
    $alpha661Text = Replace-Required $alpha661Text $oldRuntimeAdd $newRuntimeAdd 'runtime player Pelipper movement authority'

    $oldDetect = @'
        LiveCompanionDescriptor? liveCompanion = includeCompanion
            ? CompanionIntegrationService.FindLinkedCompanion(npc)
            : null;
'@
    $newDetect = @'
        LiveCompanionDescriptor? detectedCompanion = CompanionIntegrationService.FindLinkedCompanion(npc);
        LiveCompanionDescriptor? liveCompanion = includeCompanion ? detectedCompanion : null;
'@
    $alpha661Text = Replace-Required $alpha661Text $oldDetect $newDetect 'recruit detects companion even for NPC-only choice'

    $oldReplacementRelease = @'
                Party.SetCompanionState(replacement.UnitId, replacement.RecruiterId, CompanionDeploymentState.Standby);
                NPC? replacementNpc = Game1.getCharacterFromName(replacement.CharacterName);
                if (replacementNpc is not null)
                    Follow.ReleaseToVanilla(replacementNpc);
'@
    $newReplacementRelease = @'
                Party.SetCompanionState(replacement.UnitId, replacement.RecruiterId, CompanionDeploymentState.Standby);
                if (PelipperTownCompatibilityService.IsSourceControlled(replacement))
                {
                    NPC? replacementActor = PelipperTownCompatibilityService.ResolveActor(replacement);
                    if (replacementActor is not null)
                        PelipperTownCompatibilityService.SetSuppressed(replacementActor, replacement.OwnerCharacterName ?? string.Empty, true);
                }
                else
                {
                    NPC? replacementNpc = Game1.getCharacterFromName(replacement.CharacterName);
                    if (replacementNpc is not null)
                        Follow.ReleaseToVanilla(replacementNpc);
                }
'@
    $alpha661Text = Replace-Required $alpha661Text $oldReplacementRelease $newReplacementRelease 'replacement source Pelipper standby suppression'

    if (-not $alpha661Text.Contains('ApplyPelipperRecruitChoiceAlpha663(npc, detectedCompanion, includeCompanion);')) {
        $alpha661Text = Replace-Required $alpha661Text `
            "        NpcCombatProfile? profile = NpcProfileCatalog.Get(npc.Name);" `
            "        ApplyPelipperRecruitChoiceAlpha663(npc, detectedCompanion, includeCompanion);`n`n        NpcCombatProfile? profile = NpcProfileCatalog.Get(npc.Name);" `
            'apply Pelipper NPC-only/together choice after successful recruit'
    }

    $oldLinkedControl = @'
            CompanionUnitData? linked = Party.GetLinkedCompanion(npc.Name, recruiterId);
            NPC? linkedNpc = linked is null ? null : Game1.getCharacterFromName(linked.CharacterName);
            if (linkedNpc is not null && linked?.State == CompanionDeploymentState.Active)
                Follow.TakePartyControl(linkedNpc, recruiterId);
            else if (linkedNpc is not null)
                Follow.ReleaseToVanilla(linkedNpc);
'@
    $newLinkedControl = @'
            CompanionUnitData? linked = Party.GetLinkedCompanion(npc.Name, recruiterId);
            if (linked is not null && PelipperTownCompatibilityService.IsSourceControlled(linked))
            {
                ApplyPelipperLinkedDeploymentAlpha663(linked, liveCompanion);
            }
            else
            {
                NPC? linkedNpc = linked is null ? null : Game1.getCharacterFromName(linked.CharacterName);
                if (linkedNpc is not null && linked?.State == CompanionDeploymentState.Active)
                    Follow.TakePartyControl(linkedNpc, recruiterId);
                else if (linkedNpc is not null)
                    Follow.ReleaseToVanilla(linkedNpc);
            }
'@
    $alpha661Text = Replace-Required $alpha661Text $oldLinkedControl $newLinkedControl 'linked Pelipper deployment source authority'

    if (-not $alpha661Text.Contains('ReleasePelipperOwnerAlpha663(npc, linkedUnit);')) {
        $alpha661Text = Replace-Required $alpha661Text `
            "        CompanionUnitData? linkedUnit = Party.GetLinkedCompanion(characterName, recruiterId);`n        NPC? linkedNpc = linkedUnit is null ? null : Game1.getCharacterFromName(linkedUnit.CharacterName);`n        bool removed = Party.Remove(characterName, recruiterId);" `
            "        CompanionUnitData? linkedUnit = Party.GetLinkedCompanion(characterName, recruiterId);`n        NPC? linkedNpc = linkedUnit is null || PelipperTownCompatibilityService.IsSourceControlled(linkedUnit)`n            ? null`n            : Game1.getCharacterFromName(linkedUnit.CharacterName);`n        if (npc is not null)`n            ReleasePelipperOwnerAlpha663(npc, linkedUnit);`n        bool removed = Party.Remove(characterName, recruiterId);" `
            'leave restores Pelipper source companion'
    }
    Write-Utf8 $alpha661 $alpha661Text

    # -------------------- acceptance locks before compile --------------------
    $modText = Read-Lf $modEntry
    $equipmentText = Read-Lf $equipment
    $codexText = Read-Lf $codex
    $followText = Read-Lf $follow
    $alpha661Text = Read-Lf $alpha661
    $alpha663Text = Read-Lf $alpha663
    $integrationText = Read-Lf $integration
    $pelipperText = Read-Lf $pelipper
    $partyText = Read-Lf $party
    $surgeText = Read-Lf $surge

    foreach ($token in @('RegisterAlpha661MultiplayerEvents();', 'RegisterAlpha662MultiplayerEvents();', 'RegisterAlpha663HotfixEvents();', 'v0.2.0-alpha.6.6.3')) {
        if (-not $modText.Contains($token)) { throw "ModEntry 6.6.3 token missing: $token" }
    }
    foreach ($token in @('DoubleClickWindowMs = 450', 'TryActivateControllerPointer', '_preferFocusedGamepadActivation', 'Buttons.RightThumbstickUp', 'EquipInventoryIndex(i)')) {
        if (-not $equipmentText.Contains($token)) { throw "Equipment controller token missing: $token" }
    }
    foreach ($token in @('int scrollStep = direction < 0 ? 2 : -2', 'firstVisible', 'lastVisible', 'Buttons.LeftThumbstickDown', 'MoveVertical(2)')) {
        if (-not $codexText.Contains($token)) { throw "Codex controller token missing: $token" }
    }
    foreach ($token in @('OpenSearchOffsets', 'location.isTileOnMap(tile) && location.isTilePassable(tile)', 'PelipperTownCompatibilityService.IsSourceControlled(unit)')) {
        if (-not $followText.Contains($token)) { throw "Follow performance token missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService still contains expensive totally-clear/placeable query.' }
    foreach ($token in @('detectedCompanion', 'ApplyPelipperRecruitChoiceAlpha663', 'ApplyPelipperLinkedDeploymentAlpha663', 'ReleasePelipperOwnerAlpha663', 'e.IsMultipleOf(4)')) {
        if (-not $alpha661Text.Contains($token)) { throw "Alpha661 hotfix integration token missing: $token" }
    }
    foreach ($token in @('ReconcilePelipperCompanionsAlpha663', 'PelipperReconcileIntervalTicksAlpha663 = 30', 'teamup_pelipper', 'GetActiveCombatCompanionCount')) {
        if (-not $alpha663Text.Contains($token)) { throw "Alpha663 coordinator token missing: $token" }
    }
    foreach ($token in @('PelipperTownCompatibilityService.FindVillagerPartner', 'PelipperTownCompatibilityService.FindPlayerCompanions')) {
        if (-not $integrationText.Contains($token)) { throw "Companion integration Pelipper token missing: $token" }
    }
    foreach ($token in @('ProviderId = "Griff.PelipperTown"', 'CompanionOptOutKey', 'SuppressedKey', 'FindVillagerPartner', 'SetSuppressed', 'ResolveActor')) {
        if (-not $pelipperText.Contains($token)) { throw "Pelipper adapter token missing: $token" }
    }
    foreach ($token in @('GetActiveCombatCompanionCount', 'Math.Clamp(_maxActiveLinkedCompanions(), 0, 2)', 'GetSharedPeopleCount')) {
        if (-not $partyText.Contains($token)) { throw "Party quota regression token missing: $token" }
    }
    foreach ($token in @('isTileOnMap', 'isTilePassable', 'IsTileBlockedBy')) {
        if (-not $surgeText.Contains($token)) { throw "Surge safety regression token missing: $token" }
    }

    Log 'Building Alpha 6.6.3 Live Test Hotfix...'
    Log 'FIX 1: controller pointer A equips inventory item; focused controller navigation preserved.'
    Log 'FIX 2: Codex right-stick scroll stays synchronized with selected row; left stick moves two rows.'
    Log 'FIX 3: Pelipper Town villager/player Pokemon enter the shared 2/2 combat-companion pool.'
    Log 'FIX 4: follower open-tile search is bounded and avoids expensive placeable queries on water/narrow maps.'
    Log 'PERF: Team Up follow path update cadence reduced from every 2 ticks to every 4 ticks.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.3 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_3_LIVE_TEST_HOTFIX_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.3'
    Log 'CONTROLLER EQUIPMENT HOTFIX: ENABLED'
    Log 'CODEX ANALOG SYNC/SPEED HOTFIX: ENABLED'
    Log 'PELIPPER SHARED 2/2 COMPANION QUOTA: ENABLED'
    Log 'WATER/NARROW FOLLOW PERFORMANCE HOTFIX: ENABLED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
