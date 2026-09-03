$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$version = '0.2.0-alpha.6.4.5'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$projectPath = Join-Path $root 'src/TeamUp/TeamUp.csproj'
$modEntryPath = Join-Path $root 'src/TeamUp/ModEntry.cs'
$profileCatalogPath = Join-Path $root 'src/TeamUp/Core/NpcProfileCatalog.cs'
$expansionSkillPath = Join-Path $root 'src/TeamUp/Combat/ExpansionSkillService.cs'
$equipmentRpgPath = Join-Path $root 'src/TeamUp/Core/EquipmentRpgPolishService.cs'
$equipmentMenuPath = Join-Path $root 'src/TeamUp/UI/EquipmentMenu.cs'
$vaultPath = Join-Path $root 'src/TeamUp/Storage/PartyVaultService.cs'
$combatPath = Join-Path $root 'src/TeamUp/Combat/CombatService.cs'
$characterSkillPath = Join-Path $root 'src/TeamUp/Combat/CharacterSkillIdentityService.cs'
$defaultI18nPath = Join-Path $root 'src/TeamUp/i18n/default.json'
$viI18nPath = Join-Path $root 'src/TeamUp/i18n/vi.json'
$completionPath = Join-Path $root 'src/TeamUp/Core/ExpansionRosterCompletion.cs'
$fullSkillsPath = Join-Path $root 'src/TeamUp/Combat/ExpansionSkillService.FullRoster.cs'

function ReadText([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing Alpha 6.4.5 source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}

function WriteText([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom)
}

function AddJsonProperty($obj, [string]$name, [string]$value) {
    if ($null -eq $obj.PSObject.Properties[$name]) {
        $obj | Add-Member -NotePropertyName $name -NotePropertyValue $value
    }
    else {
        $obj.PSObject.Properties[$name].Value = $value
    }
}

function RoleEn([string]$role) {
    if ($role -eq 'Damage') { return 'DPS' }
    return $role
}

function RoleVi([string]$role) {
    switch ($role) {
        'Tank' { return 'Đỡ đòn' }
        'Damage' { return 'DPS' }
        'Support' { return 'Hỗ trợ' }
        'Healer' { return 'Hồi phục' }
        'Control' { return 'Khống chế' }
        default { return $role }
    }
}

function EngagementVi([string]$style) {
    switch ($style) {
        'Cautious' { return 'thận trọng' }
        'Aggressive' { return 'hung hăng' }
        'Reckless' { return 'liều lĩnh' }
        'Passive' { return 'phòng thủ' }
        default { return 'cân bằng' }
    }
}

# -----------------------------------------------------------------------------
# Version + startup marker.
# -----------------------------------------------------------------------------
$project = ReadText $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteText $projectPath $project

$mod = ReadText $modEntryPath
$mod = $mod.Replace('0.2.0-alpha.6.4.4', $version)
$mod = $mod.Replace('test feedback + equipment hotfix loaded.', 'interaction + AI + full expansion roster loaded.')
WriteText $modEntryPath $mod

# -----------------------------------------------------------------------------
# Complete all remaining SVE/RSV profile placeholders and make ExpansionSkillService partial.
# -----------------------------------------------------------------------------
$catalog = ReadText $profileCatalogPath
$oldExpansionLoop = @'
        foreach (NpcCombatProfile profile in ExpansionNpcProfileCatalog.All)
            profiles[profile.CharacterName] = profile;
'@
$newExpansionLoop = @'
        foreach (NpcCombatProfile profile in ExpansionNpcProfileCatalog.All)
        {
            NpcCombatProfile resolved = ExpansionRosterCompletion.Resolve(profile);
            profiles[resolved.CharacterName] = resolved;
        }
'@
if ($catalog.Contains($oldExpansionLoop)) {
    $catalog = $catalog.Replace($oldExpansionLoop, $newExpansionLoop)
}
elseif ($catalog -notmatch 'ExpansionRosterCompletion\.Resolve\(profile\)') {
    throw 'Could not wire ExpansionRosterCompletion into NpcProfileCatalog.'
}
WriteText $profileCatalogPath $catalog

$expansion = ReadText $expansionSkillPath
$expansion = $expansion.Replace('internal sealed class ExpansionSkillService', 'internal sealed partial class ExpansionSkillService')
WriteText $expansionSkillPath $expansion

# Equipment cooldown preview now asks the expansion skill registry for newly completed kits.
$rpg = ReadText $equipmentRpgPath
if ($rpg -notmatch 'using Ronvotri\.TeamUp\.Combat;') {
    $rpg = $rpg.Replace('using Microsoft.Xna.Framework;', "using Microsoft.Xna.Framework;`nusing Ronvotri.TeamUp.Combat;")
}
$oldCooldownLookup = @'
        if (!SignatureCooldownTicks.TryGetValue(member.CharacterName, out int baseTicks))
            return null;
'@
$newCooldownLookup = @'
        if (!SignatureCooldownTicks.TryGetValue(member.CharacterName, out int baseTicks)
            && !ExpansionSkillService.TryGetBaseCooldownTicks(member.CharacterName, out baseTicks))
            return null;
'@
if ($rpg.Contains($oldCooldownLookup)) {
    $rpg = $rpg.Replace($oldCooldownLookup, $newCooldownLookup)
}
elseif ($rpg -notmatch 'ExpansionSkillService\.TryGetBaseCooldownTicks') {
    throw 'Could not wire full expansion cooldown preview.'
}
WriteText $equipmentRpgPath $rpg

# -----------------------------------------------------------------------------
# Equipment interaction: true double-click equip/unequip + controller action focus.
# -----------------------------------------------------------------------------
$equipment = ReadText $equipmentMenuPath
if ($equipment -notmatch 'DoubleClickWindowMs') {
    $equipment = $equipment.Replace(
        '    private const int SlotCardHeight = 96;',
        "    private const int SlotCardHeight = 96;`n    private const long DoubleClickWindowMs = 450;")
}
if ($equipment -notmatch '_loadoutFocusIndex') {
    $equipment = $equipment.Replace(
        '    private EquipmentSlot _selectedSlot = EquipmentSlot.Weapon;',
        "    private EquipmentSlot _selectedSlot = EquipmentSlot.Weapon;`n    private int _loadoutFocusIndex;`n    private int _lastMouseClickId = -1;`n    private long _lastMouseClickAtMs;")
}

$oldReceiveClick = @'
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < _slotBounds.Length; i++)
        {
            if (!_slotBounds[i].Contains(x, y))
                continue;

            _selectedSlot = (EquipmentSlot)i;
            _focusInventory = false;
            Game1.playSound("smallSelect");
            return;
        }

        for (int i = 0; i < _inventoryBounds.Length; i++)
        {
            if (!_inventoryBounds[i].Contains(x, y))
                continue;

            _focusInventory = true;
            _inventoryCursor = i;
            EquipInventoryIndex(i);
            return;
        }

        if (_autoEquipBounds.Contains(x, y))
        {
            AutoEquipBest();
            return;
        }

        if (_unequipBounds.Contains(x, y))
        {
            UnequipSelected();
            return;
        }

        if (_backBounds.Contains(x, y))
            ReturnToMemberMenu();
    }
'@
$newReceiveClick = @'
    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < _slotBounds.Length; i++)
        {
            if (!_slotBounds[i].Contains(x, y))
                continue;

            _selectedSlot = (EquipmentSlot)i;
            _loadoutFocusIndex = i;
            _focusInventory = false;
            if (IsDoubleClick(2000 + i) && _equipment.GetEquipped(_member, _selectedSlot) is not null)
                UnequipSelected();
            else
                Game1.playSound("smallSelect");
            return;
        }

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

            if (IsDoubleClick(1000 + i))
                EquipInventoryIndex(i);
            else
                Game1.playSound("smallSelect");
            return;
        }

        if (_autoEquipBounds.Contains(x, y))
        {
            _focusInventory = false;
            _loadoutFocusIndex = 3;
            AutoEquipBest();
            return;
        }

        if (_unequipBounds.Contains(x, y))
        {
            _focusInventory = false;
            _loadoutFocusIndex = 4;
            UnequipSelected();
            return;
        }

        if (_backBounds.Contains(x, y))
            ReturnToMemberMenu();
    }
'@
if ($equipment.Contains($oldReceiveClick)) {
    $equipment = $equipment.Replace($oldReceiveClick, $newReceiveClick)
}
elseif ($equipment -notmatch 'IsDoubleClick\(2000 \+ i\)') {
    throw 'Could not patch EquipmentMenu double-click interaction.'
}

$oldMoveHorizontal = @'
    private void MoveHorizontal(int delta)
    {
        if (!_focusInventory)
        {
            if (delta > 0)
            {
                _focusInventory = true;
                ClampInventoryCursor();
            }
            Game1.playSound("shiny4");
            return;
        }

        int col = _inventoryCursor % InventoryColumns;
        if (delta < 0 && col == 0)
        {
            _focusInventory = false;
            Game1.playSound("shiny4");
            return;
        }

        int next = _inventoryCursor + delta;
        if (next >= 0 && next < _inventoryBounds.Length && next / InventoryColumns == _inventoryCursor / InventoryColumns)
            _inventoryCursor = next;
        Game1.playSound("shiny4");
    }
'@
$newMoveHorizontal = @'
    private void MoveHorizontal(int delta)
    {
        if (!_focusInventory)
        {
            if (delta > 0 && _loadoutFocusIndex <= 2)
            {
                _focusInventory = true;
                ClampInventoryCursor();
            }
            Game1.playSound("shiny4");
            return;
        }

        int col = _inventoryCursor % InventoryColumns;
        if (delta < 0 && col == 0)
        {
            _focusInventory = false;
            _loadoutFocusIndex = (int)_selectedSlot;
            Game1.playSound("shiny4");
            return;
        }

        int next = _inventoryCursor + delta;
        if (next >= 0 && next < _inventoryBounds.Length && next / InventoryColumns == _inventoryCursor / InventoryColumns)
            _inventoryCursor = next;
        Game1.playSound("shiny4");
    }
'@
if ($equipment.Contains($oldMoveHorizontal)) {
    $equipment = $equipment.Replace($oldMoveHorizontal, $newMoveHorizontal)
}
elseif ($equipment -notmatch '_loadoutFocusIndex <= 2') {
    throw 'Could not patch equipment horizontal controller navigation.'
}

$oldMoveVertical = @'
    private void MoveVertical(int delta)
    {
        if (!_focusInventory)
        {
            int next = Math.Clamp((int)_selectedSlot + delta, 0, 2);
            _selectedSlot = (EquipmentSlot)next;
            Game1.playSound("shiny4");
            return;
        }

        int nextIndex = _inventoryCursor + delta * InventoryColumns;
        if (nextIndex >= 0 && nextIndex < _inventoryBounds.Length)
            _inventoryCursor = nextIndex;
        Game1.playSound("shiny4");
    }
'@
$newMoveVertical = @'
    private void MoveVertical(int delta)
    {
        if (!_focusInventory)
        {
            _loadoutFocusIndex = Math.Clamp(_loadoutFocusIndex + delta, 0, 4);
            if (_loadoutFocusIndex <= 2)
                _selectedSlot = (EquipmentSlot)_loadoutFocusIndex;
            Game1.playSound("shiny4");
            return;
        }

        int nextIndex = _inventoryCursor + delta * InventoryColumns;
        if (nextIndex >= 0 && nextIndex < _inventoryBounds.Length)
            _inventoryCursor = nextIndex;
        Game1.playSound("shiny4");
    }
'@
if ($equipment.Contains($oldMoveVertical)) {
    $equipment = $equipment.Replace($oldMoveVertical, $newMoveVertical)
}
elseif ($equipment -notmatch 'Math\.Clamp\(_loadoutFocusIndex \+ delta, 0, 4\)') {
    throw 'Could not patch equipment vertical controller navigation.'
}

$oldActivateFocused = @'
    private void ActivateFocused()
    {
        if (!_focusInventory)
        {
            _focusInventory = true;
            ClampInventoryCursor();
            Game1.playSound("smallSelect");
            return;
        }

        EquipInventoryIndex(_inventoryCursor);
    }
'@
$newActivateFocused = @'
    private void ActivateFocused()
    {
        if (!_focusInventory)
        {
            if (_loadoutFocusIndex == 3)
            {
                AutoEquipBest();
                return;
            }
            if (_loadoutFocusIndex == 4)
            {
                UnequipSelected();
                return;
            }

            _selectedSlot = (EquipmentSlot)Math.Clamp(_loadoutFocusIndex, 0, 2);
            _focusInventory = true;
            ClampInventoryCursor();
            Game1.playSound("smallSelect");
            return;
        }

        EquipInventoryIndex(_inventoryCursor);
    }

    private bool IsDoubleClick(int clickId)
    {
        long now = Environment.TickCount64;
        bool isDouble = _lastMouseClickId == clickId
            && now - _lastMouseClickAtMs >= 0
            && now - _lastMouseClickAtMs <= DoubleClickWindowMs;
        _lastMouseClickId = clickId;
        _lastMouseClickAtMs = now;
        return isDouble;
    }
'@
if ($equipment.Contains($oldActivateFocused)) {
    $equipment = $equipment.Replace($oldActivateFocused, $newActivateFocused)
}
elseif ($equipment -notmatch 'private bool IsDoubleClick') {
    throw 'Could not patch equipment focused activation.'
}

$equipment = $equipment.Replace(
    '            bool selected = slot == _selectedSlot;`n            DrawInset(b, bounds, selected && !_focusInventory);',
    '            bool selected = slot == _selectedSlot;`n            DrawInset(b, bounds, selected && !_focusInventory && _loadoutFocusIndex == i);')
$equipment = [regex]::Replace(
    $equipment,
    '            bool selected = slot == _selectedSlot;\n            DrawInset\(b, bounds, selected && !_focusInventory\);',
    "            bool selected = slot == _selectedSlot;`n            DrawInset(b, bounds, selected && !_focusInventory && _loadoutFocusIndex == i);",
    1)
$equipment = $equipment.Replace(
    '        DrawButton(b, _autoEquipBounds, _translation.Get("equipment.auto-equip"), false);`n        DrawButton(b, _unequipBounds, _translation.Get("equipment.unequip-button"), false);',
    '        DrawButton(b, _autoEquipBounds, _translation.Get("equipment.auto-equip"), !_focusInventory && _loadoutFocusIndex == 3);`n        DrawButton(b, _unequipBounds, _translation.Get("equipment.unequip-button"), !_focusInventory && _loadoutFocusIndex == 4);')
$equipment = [regex]::Replace(
    $equipment,
    '        DrawButton\(b, _autoEquipBounds, _translation\.Get\("equipment\.auto-equip"\), false\);\n        DrawButton\(b, _unequipBounds, _translation\.Get\("equipment\.unequip-button"\), false\);',
    "        DrawButton(b, _autoEquipBounds, _translation.Get(\"equipment.auto-equip\"), !_focusInventory && _loadoutFocusIndex == 3);`n        DrawButton(b, _unequipBounds, _translation.Get(\"equipment.unequip-button\"), !_focusInventory && _loadoutFocusIndex == 4);",
    1)
WriteText $equipmentMenuPath $equipment

# -----------------------------------------------------------------------------
# Party Vault: preserve click-click, add mouse press-drag-release between inventories.
# -----------------------------------------------------------------------------
$vault = ReadText $vaultPath
if ($vault -notmatch '_mouseDragCandidate') {
    $vault = $vault.Replace(
        '        private Point _lastPhysicalMouse;',
        "        private Point _lastPhysicalMouse;`n        private bool _mouseDragCandidate;`n        private Point _mouseDragStart;")
}

# Reset candidate at mouse-down, then arm it only for a normal inventory pickup.
if ($vault -notmatch '_mouseDragCandidate = false;\n            _lastPhysicalMouse') {
    $vault = $vault.Replace(
        '            _showMouseCursor = true;`n            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);',
        '            _showMouseCursor = true;`n            _mouseDragCandidate = false;`n            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);')
    $vault = [regex]::Replace(
        $vault,
        '            _showMouseCursor = true;\n            _lastPhysicalMouse = new Point\(Mouse\.GetState\(\)\.X, Mouse\.GetState\(\)\.Y\);',
        "            _showMouseCursor = true;`n            _mouseDragCandidate = false;`n            _lastPhysicalMouse = new Point(Mouse.GetState().X, Mouse.GetState().Y);",
        1)
}

$oldVaultNormal = @'
                else
                    HandleLeftClick(_vaultMenu, x, y, playSound);
'@
$newVaultNormal = @'
                else
                {
                    BeginMouseDragCandidate(_vaultMenu, x, y);
                    HandleLeftClick(_vaultMenu, x, y, playSound);
                }
'@
if ($vault.Contains($oldVaultNormal)) { $vault = $vault.Replace($oldVaultNormal, $newVaultNormal) }
$oldPlayerNormal = @'
                else
                    HandleLeftClick(_playerMenu, x, y, playSound);
'@
$newPlayerNormal = @'
                else
                {
                    BeginMouseDragCandidate(_playerMenu, x, y);
                    HandleLeftClick(_playerMenu, x, y, playSound);
                }
'@
if ($vault.Contains($oldPlayerNormal)) { $vault = $vault.Replace($oldPlayerNormal, $newPlayerNormal) }

if ($vault -notmatch 'public override void releaseLeftClick') {
    $needle = '        public override void receiveRightClick(int x, int y, bool playSound = true)'
    $releaseBlock = @'
        public override void releaseLeftClick(int x, int y)
        {
            if (!_mouseDragCandidate)
                return;

            int dx = x - _mouseDragStart.X;
            int dy = y - _mouseDragStart.Y;
            bool actualDrag = dx * dx + dy * dy >= 64;
            _mouseDragCandidate = false;
            if (!actualDrag || _heldItem is null)
                return;

            if (IsWithin(_vaultMenu, x, y))
            {
                HandleLeftClick(_vaultMenu, x, y, playSound: true);
                return;
            }

            if (IsWithin(_playerMenu, x, y))
            {
                HandleLeftClick(_playerMenu, x, y, playSound: true);
                return;
            }

            ReturnHeldItemSafely();
        }

        private void BeginMouseDragCandidate(InventoryMenu menu, int x, int y)
        {
            if (_heldItem is not null)
                return;

            int index = menu.getInventoryPositionOfClick(x, y);
            if (index < 0 || index >= menu.actualInventory.Count || menu.actualInventory[index] is null)
                return;

            _mouseDragCandidate = true;
            _mouseDragStart = new Point(x, y);
        }

'@
    $index = $vault.IndexOf($needle)
    if ($index -lt 0) { throw 'Could not add Party Vault drag release handler.' }
    $vault = $vault.Insert($index, $releaseBlock)
}
WriteText $vaultPath $vault

# -----------------------------------------------------------------------------
# Combat AI stability: target lock, wider sensible engagement, lower repath/facing thrash.
# -----------------------------------------------------------------------------
$combat = ReadText $combatPath
$combat = $combat.Replace('    private const float RepathThresholdTiles = 0.9f;', '    private const float RepathThresholdTiles = 1.35f;')
if ($combat -notmatch 'TargetLockDurationTicks') {
    $combat = $combat.Replace(
        '    private const int ThreatPulseInterval = 30;',
        "    private const int ThreatPulseInterval = 30;`n    private const int TargetLockDurationTicks = 45;`n    private const int FacingHoldDurationTicks = 10;")
}
if ($combat -notmatch '_targetLockTicks') {
    $combat = $combat.Replace(
        '    private readonly Dictionary<string, Vector2> _lastTargetTiles = new(StringComparer.OrdinalIgnoreCase);',
        "    private readonly Dictionary<string, Vector2> _lastTargetTiles = new(StringComparer.OrdinalIgnoreCase);`n    private readonly Dictionary<string, int> _targetLockTicks = new(StringComparer.OrdinalIgnoreCase);`n    private readonly Dictionary<string, int> _facingHoldTicks = new(StringComparer.OrdinalIgnoreCase);`n    private readonly Dictionary<string, int> _lastFacingDirections = new(StringComparer.OrdinalIgnoreCase);")
}
if ($combat -notmatch '_targetLockTicks\.Clear\(\)') {
    $combat = $combat.Replace(
        '        _lastTargetTiles.Clear();',
        "        _lastTargetTiles.Clear();`n        _targetLockTicks.Clear();`n        _facingHoldTicks.Clear();`n        _lastFacingDirections.Clear();")
}
if ($combat -notmatch 'TickCooldowns\(_targetLockTicks\)') {
    $combat = $combat.Replace(
        '        TickCooldowns(_selfRecoveryCooldowns);',
        "        TickCooldowns(_selfRecoveryCooldowns);`n        TickCooldowns(_targetLockTicks);`n        TickCooldowns(_facingHoldTicks);")
}

$oldFacingAttack = @'
            npc.controller = null;
            npc.temporaryController = null;
            npc.Halt();
            npc.faceDirection(GetFacingDirection(npc.Position, target.Position));

            if (GetCooldown(_attackCooldowns, member.CharacterName) > 0)
                continue;

            PerformAttack(npc, target, member, role, affinity);
'@
$newFacingAttack = @'
            npc.controller = null;
            npc.temporaryController = null;
            npc.Halt();
            bool attackReady = GetCooldown(_attackCooldowns, member.CharacterName) <= 0;
            FaceTargetStable(npc, target, attackReady);

            if (!attackReady)
                continue;

            PerformAttack(npc, target, member, role, affinity);
'@
if ($combat.Contains($oldFacingAttack)) {
    $combat = $combat.Replace($oldFacingAttack, $newFacingAttack)
}
elseif ($combat -notmatch 'FaceTargetStable\(npc, target, attackReady\)') {
    throw 'Could not patch stable combat facing.'
}

$oldCandidates = @'
        List<Monster> candidates = monsters
            .Where(monster => ReferenceEquals(monster.currentLocation, Game1.currentLocation))
            .Where(monster => Vector2.Distance(monster.Tile, farmerTile) <= radius)
            .ToList();
'@
$newCandidates = @'
        List<Monster> candidates = monsters
            .Where(monster => ReferenceEquals(monster.currentLocation, Game1.currentLocation))
            .Where(monster => Vector2.Distance(monster.Tile, farmerTile) <= radius
                || Vector2.Distance(monster.Tile, npc.Tile) <= Math.Min(radius, 5.5f))
            .ToList();
'@
if ($combat.Contains($oldCandidates)) {
    $combat = $combat.Replace($oldCandidates, $newCandidates)
}
elseif ($combat -notmatch 'Math\.Min\(radius, 5\.5f\)') {
    throw 'Could not patch NPC-local combat awareness.'
}

$oldCurrent = @'
        Monster? current = _targets.TryGetValue(member.CharacterName, out Monster? tracked)
            && tracked.Health > 0
            && candidates.Contains(tracked)
                ? tracked
                : null;

        double Score(Monster monster)
'@
$newCurrent = @'
        Monster? current = _targets.TryGetValue(member.CharacterName, out Monster? tracked)
            && tracked.Health > 0
            && candidates.Contains(tracked)
                ? tracked
                : null;

        if (current is not null && GetCooldown(_targetLockTicks, member.CharacterName) > 0)
            return current;

        double Score(Monster monster)
'@
if ($combat.Contains($oldCurrent)) {
    $combat = $combat.Replace($oldCurrent, $newCurrent)
}
elseif ($combat -notmatch 'GetCooldown\(_targetLockTicks, member\.CharacterName\)') {
    throw 'Could not patch target lock read.'
}

$oldAcquireReturn = '        return candidates.OrderBy(Score).FirstOrDefault();'
$newAcquireReturn = @'
        Monster? chosen = candidates.OrderBy(Score).FirstOrDefault();
        if (chosen is not null && !ReferenceEquals(chosen, current))
            _targetLockTicks[member.CharacterName] = TargetLockDurationTicks;
        return chosen;
'@
if ($combat.Contains($oldAcquireReturn)) {
    $combat = $combat.Replace($oldAcquireReturn, $newAcquireReturn.TrimEnd("`r", "`n"))
}
elseif ($combat -notmatch '_targetLockTicks\[member\.CharacterName\] = TargetLockDurationTicks') {
    throw 'Could not patch target lock assignment.'
}

$combat = $combat.Replace('            EngagementStyle.Cautious => 4.5f,', '            EngagementStyle.Cautious => 5.5f,')
$combat = $combat.Replace('            EngagementStyle.Balanced => 6.5f,', '            EngagementStyle.Balanced => 7.0f,')
$combat = $combat.Replace('            EngagementStyle.Aggressive => 8.5f,', '            EngagementStyle.Aggressive => 9.0f,')

if ($combat -notmatch 'private void FaceTargetStable') {
    $needle = '    private static int GetFacingDirection(Vector2 from, Vector2 to)'
    $helper = @'
    private void FaceTargetStable(NPC npc, Monster target, bool force)
    {
        int desired = GetFacingDirection(npc.Position, target.Position);
        if (!force
            && _lastFacingDirections.TryGetValue(npc.Name, out int last)
            && last != desired
            && GetCooldown(_facingHoldTicks, npc.Name) > 0)
            return;

        if (!_lastFacingDirections.TryGetValue(npc.Name, out int previous) || previous != desired || force)
        {
            npc.faceDirection(desired);
            _lastFacingDirections[npc.Name] = desired;
            _facingHoldTicks[npc.Name] = FacingHoldDurationTicks;
        }
    }

'@
    $index = $combat.IndexOf($needle)
    if ($index -lt 0) { throw 'Could not insert stable facing helper.' }
    $combat = $combat.Insert($index, $helper)
}

# Remove target locks immediately when disengaging so a later combat starts cleanly.
if ($combat -notmatch '_targetLockTicks\.Remove\(characterName\)') {
    $combat = $combat.Replace(
        '        _lastTargetTiles.Remove(characterName);',
        "        _lastTargetTiles.Remove(characterName);`n        _targetLockTicks.Remove(characterName);`n        _facingHoldTicks.Remove(characterName);`n        _lastFacingDirections.Remove(characterName);")
}
WriteText $combatPath $combat

# -----------------------------------------------------------------------------
# Call-side sound guards. These cannot repair a third-party monster with invalid sound metadata,
# but they guarantee Team Up never passes a null/blank cue from variable-based skill helpers.
# -----------------------------------------------------------------------------
$expansion = ReadText $expansionSkillPath
$expansion = $expansion.Replace('        Game1.currentLocation.playSound(sound);', '        if (!string.IsNullOrWhiteSpace(sound))`n            Game1.currentLocation.playSound(sound);')
$expansion = $expansion.Replace('`n', "`n")
WriteText $expansionSkillPath $expansion

$characterSkill = ReadText $characterSkillPath
$characterSkill = $characterSkill.Replace('            Game1.currentLocation.playSound(sound);', '            if (!string.IsNullOrWhiteSpace(sound))`n                Game1.currentLocation.playSound(sound);')
$characterSkill = $characterSkill.Replace('`n', "`n")
WriteText $characterSkillPath $characterSkill

# -----------------------------------------------------------------------------
# Generate EN/VI Codex descriptions for all 51 newly completed expansion profiles.
# -----------------------------------------------------------------------------
$completionText = ReadText $completionPath
$fullSkillText = ReadText $fullSkillsPath
$specPattern = '\["(?<name>[^"]+)"\]\s*=\s*R\(PartyRole\.(?<primary>\w+),\s*PartyRole\.(?<secondary>\w+),\s*EngagementStyle\.(?<engagement>\w+)'
$skillPattern = 'Skills\["(?<name>[^"]+)"\]\s*=\s*S\("(?<signature>[^"]+)"'

$specs = @{}
foreach ($match in [regex]::Matches($completionText, $specPattern)) {
    $specs[$match.Groups['name'].Value] = @{
        Primary = $match.Groups['primary'].Value
        Secondary = $match.Groups['secondary'].Value
        Engagement = $match.Groups['engagement'].Value
    }
}
$signatures = @{}
foreach ($match in [regex]::Matches($fullSkillText, $skillPattern)) {
    $signatures[$match.Groups['name'].Value] = $match.Groups['signature'].Value
}
if ($specs.Count -ne 51 -or $signatures.Count -ne 51) {
    throw "Full expansion roster parser mismatch: specs=$($specs.Count), skills=$($signatures.Count), expected 51."
}

$en = (ReadText $defaultI18nPath) | ConvertFrom-Json
$vi = (ReadText $viI18nPath) | ConvertFrom-Json
foreach ($name in $specs.Keys | Sort-Object) {
    $spec = $specs[$name]
    $signature = $signatures[$name]
    $key = $name.ToLowerInvariant()
    $primaryEn = RoleEn $spec.Primary
    $secondaryEn = RoleEn $spec.Secondary
    $primaryVi = RoleVi $spec.Primary
    $secondaryVi = RoleVi $spec.Secondary
    $engagementVi = EngagementVi $spec.Engagement

    AddJsonProperty $en "codex.expansion.$key.passive" "$name: a Team Up $primaryEn/$secondaryEn specialist using $($spec.Engagement.ToLowerInvariant()) positioning and class-safe utility."
    AddJsonProperty $en "codex.expansion.$key.ability" "$signature: a unique Team Up signature tuned around $primaryEn with $secondaryEn utility; secondary effects trade against raw power to preserve class balance."
    AddJsonProperty $vi "codex.expansion.$key.passive" "$name: chiến đấu theo hướng $primaryVi/$secondaryVi của Team Up, giữ vị trí $engagementVi và ưu tiên đúng nhiệm vụ class."
    AddJsonProperty $vi "codex.expansion.$key.ability" "$signature: kỹ năng đặc trưng riêng của Team Up thiên về $primaryVi, kèm tiện ích $secondaryVi; hiệu ứng phụ được đổi bằng sức mạnh thô để giữ cân bằng class."
}
WriteText $defaultI18nPath ($en | ConvertTo-Json -Depth 20)
WriteText $viI18nPath ($vi | ConvertTo-Json -Depth 20)

# -----------------------------------------------------------------------------
# Acceptance before dotnet compile.
# -----------------------------------------------------------------------------
$project = ReadText $projectPath
$mod = ReadText $modEntryPath
$catalog = ReadText $profileCatalogPath
$expansion = ReadText $expansionSkillPath
$rpg = ReadText $equipmentRpgPath
$equipment = ReadText $equipmentMenuPath
$vault = ReadText $vaultPath
$combat = ReadText $combatPath
$enText = ReadText $defaultI18nPath
$viText = ReadText $viI18nPath

if ($project -notmatch '<Version>0\.2\.0-alpha\.6\.4\.5</Version>') { throw 'Alpha 6.4.5 version missing.' }
if ($mod -notmatch 'build: v0\.2\.0-alpha\.6\.4\.5') { throw 'Alpha 6.4.5 debug marker missing.' }
if ($mod -notmatch 'interaction \+ AI \+ full expansion roster loaded') { throw 'Alpha 6.4.5 load marker missing.' }
if ($catalog -notmatch 'ExpansionRosterCompletion\.Resolve\(profile\)') { throw 'Full roster profile resolver missing.' }
if ($expansion -notmatch 'internal sealed partial class ExpansionSkillService') { throw 'ExpansionSkillService partial integration missing.' }
if ($rpg -notmatch 'ExpansionSkillService\.TryGetBaseCooldownTicks') { throw 'New expansion cooldown preview missing.' }
if ($equipment -notmatch 'IsDoubleClick\(1000 \+ i\)' -or $equipment -notmatch 'IsDoubleClick\(2000 \+ i\)') { throw 'Equipment double-click missing.' }
if ($equipment -notmatch '_loadoutFocusIndex == 3' -or $equipment -notmatch '_loadoutFocusIndex == 4') { throw 'Controller action focus missing.' }
if ($vault -notmatch 'public override void releaseLeftClick' -or $vault -notmatch 'BeginMouseDragCandidate') { throw 'Party Vault drag/drop missing.' }
if ($combat -notmatch 'TargetLockDurationTicks = 45' -or $combat -notmatch 'FaceTargetStable') { throw 'Combat anti-jitter lock missing.' }
if ($combat -notmatch 'Math\.Min\(radius, 5\.5f\)') { throw 'NPC-local monster awareness missing.' }
if ($enText -notmatch 'codex\.expansion\.ariah\.ability' -or $viText -notmatch 'codex\.expansion\.ariah\.ability') { throw 'Ariah completed translations missing.' }
if ($enText -notmatch 'codex\.expansion\.zayne\.ability' -or $viText -notmatch 'codex\.expansion\.zayne\.ability') { throw 'Zayne completed translations missing.' }

Write-Host 'Alpha 6.4.5 Interaction + AI + Expansion Completion integrated.'
Write-Host 'Equipment: double-click equip/unequip; controller reaches Auto Equip and Unequip.'
Write-Host 'Vault: mouse drag-and-drop added while click-click and controller quick transfer remain.'
Write-Host 'Combat: target locks, local monster awareness, stable facing, reduced repath thrash.'
Write-Host 'Expansion: all remaining 9 SVE + 42 RSV profiles now have Team Up roles, affinities, signatures, and Codex text.'
Write-Host 'Sound: variable Team Up skill cue calls now reject null/blank names; third-party sound metadata remains outside Team Up ownership.'
