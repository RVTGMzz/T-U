$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$equipment = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha664'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.4_CONTROLLER_EQUIPMENT_TRANSACTION_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.4_CONTROLLER_EQUIPMENT_TRANSACTION_HOTFIX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.4'
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
    foreach ($required in @($project, $manifest, $modEntry, $equipment)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.4 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = $modText.Replace(
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.3',
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.4')
    $modText = $modText.Replace(
        'Team Up! v0.2.0-alpha.6.6.3 Live Test Hotfix loaded.',
        'Team Up! v0.2.0-alpha.6.6.4 Controller Equipment Transaction Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    $equipmentText = Read-Lf $equipment

    if (-not $equipmentText.Contains('ControllerActivationDebounceMs')) {
        $equipmentText = Replace-Required $equipmentText `
            '    private const long DoubleClickWindowMs = 450;' `
            "    private const long DoubleClickWindowMs = 450;`n    private const long ControllerActivationDebounceMs = 180;`n    private const long ControllerMouseEchoSuppressionMs = 260;" `
            'controller action constants'
    }

    if (-not $equipmentText.Contains('_suppressMouseClickUntilMs')) {
        $equipmentText = Replace-Required $equipmentText `
            '    private Point _lastHoverPoint = new(int.MinValue, int.MinValue);' `
            "    private Point _lastHoverPoint = new(int.MinValue, int.MinValue);`n    private long _lastControllerActivationAtMs = long.MinValue;`n    private long _suppressMouseClickUntilMs;" `
            'controller action fields'
    }

    if (-not $equipmentText.Contains('if (Environment.TickCount64 <= _suppressMouseClickUntilMs)')) {
        $equipmentText = Replace-Required $equipmentText `
            "    public override void receiveLeftClick(int x, int y, bool playSound = true)`n    {" `
            "    public override void receiveLeftClick(int x, int y, bool playSound = true)`n    {`n        // Stardew can echo gamepad A as a virtual left-click. Ignore that echo so one`n        // physical controller press cannot execute both the gamepad path and mouse path.`n        if (Environment.TickCount64 <= _suppressMouseClickUntilMs)`n            return;" `
            'suppress virtual gamepad mouse echo'
    }

    $oldSlotClick = @'
            _selectedSlot = (EquipmentSlot)i;
            _loadoutFocusIndex = i;
            _focusInventory = false;
            if (IsDoubleClick(2000 + i) && _equipment.GetEquipped(_member, _selectedSlot) is not null)
                UnequipSelected();
            else
                Game1.playSound("smallSelect");
            return;
'@
    $newSlotClick = @'
            _selectedSlot = (EquipmentSlot)i;
            _loadoutFocusIndex = i;
            _focusInventory = false;
            // Slot cards only select. Unequip is explicit through X / the Unequip button.
            // This prevents a controller A -> virtual-click echo from becoming a ghost double-click unequip.
            _lastMouseClickId = -1;
            _lastMouseClickAtMs = 0;
            Game1.playSound("smallSelect");
            return;
'@
    $equipmentText = Replace-Required $equipmentText $oldSlotClick $newSlotClick 'remove slot-card double-click unequip'

    $oldA = @'
        if (b == Buttons.A)
        {
            if (!_preferFocusedGamepadActivation && TryActivateControllerPointer())
                return;

            ActivateFocused();
            return;
        }
        if (b == Buttons.Y)
        {
            AutoEquipBest();
            return;
        }
        if (b == Buttons.X)
            UnequipSelected();
'@
    $newA = @'
        if (b == Buttons.A)
        {
            if (!TryBeginControllerActivation())
                return;

            if (!_preferFocusedGamepadActivation && TryActivateControllerPointer())
                return;

            ActivateFocused();
            return;
        }
        if (b == Buttons.Y)
        {
            if (!TryBeginControllerActivation())
                return;
            AutoEquipBest();
            return;
        }
        if (b == Buttons.X)
        {
            if (!TryBeginControllerActivation())
                return;
            UnequipSelected();
        }
'@
    $equipmentText = Replace-Required $equipmentText $oldA $newA 'debounced gamepad activation'

    if (-not $equipmentText.Contains('private bool TryBeginControllerActivation()')) {
        $helper = @'
    private bool TryBeginControllerActivation()
    {
        long now = Environment.TickCount64;
        if (_lastControllerActivationAtMs != long.MinValue)
        {
            long elapsed = now - _lastControllerActivationAtMs;
            if (elapsed >= 0 && elapsed < ControllerActivationDebounceMs)
                return false;
        }

        _lastControllerActivationAtMs = now;
        _suppressMouseClickUntilMs = now + ControllerMouseEchoSuppressionMs;
        _lastMouseClickId = -1;
        _lastMouseClickAtMs = 0;
        return true;
    }

'@
        $equipmentText = Replace-Required $equipmentText `
            '    private bool TryActivateControllerPointer()' `
            ($helper + '    private bool TryActivateControllerPointer()') `
            'controller action helper'
    }

    $oldEquip = @'
        _selectedSlot = naturalSlot.Value;
        if (_equipment.TryEquip(_member, _selectedSlot, inventoryIndex, out _))
        {
            _progression.NormalizeMember(_member);
            _saveNow();
            ShowHud(_translation.Get("equipment.equipped-name", new { item = _equipment.GetEquipped(_member, _selectedSlot)?.DisplayName ?? item.DisplayName }));
            Game1.playSound("coin");
            _hoveredItem = null;
            ClampInventoryCursor();
        }
        else
        {
            ShowHud(_translation.Get("equipment.inventory-full"), error: true);
            Game1.playSound("cancel");
        }
'@
    $newEquip = @'
        _selectedSlot = naturalSlot.Value;
        string requestedQualifiedId = item.QualifiedItemId;
        string requestedDisplayName = item.DisplayName;
        if (_equipment.TryEquip(_member, _selectedSlot, inventoryIndex, out string failureMessage))
        {
            EquippedItemData? committedData = _equipment.GetEquipped(_member, _selectedSlot);
            Item? committedItem = GetActualEquippedItem(_selectedSlot);
            bool committed = committedData is not null
                && committedItem is not null
                && committedData.QualifiedItemId.Equals(committedItem.QualifiedItemId, StringComparison.OrdinalIgnoreCase)
                && requestedQualifiedId.Equals(committedItem.QualifiedItemId, StringComparison.OrdinalIgnoreCase);

            if (!committed)
            {
                ShowHud($"Team Up could not verify {requestedDisplayName} in the NPC equipment slot.", error: true);
                Game1.playSound("cancel");
                _hoveredItem = null;
                ClampInventoryCursor();
                return;
            }

            _progression.NormalizeMember(_member);
            _saveNow();
            ShowHud(_translation.Get("equipment.equipped-name", new { item = committedData.DisplayName }));
            Game1.playSound("coin");
            _hoveredItem = null;
            ClampInventoryCursor();
        }
        else
        {
            ShowHud(string.IsNullOrWhiteSpace(failureMessage)
                ? _translation.Get("equipment.inventory-full").ToString()
                : failureMessage,
                error: true);
            Game1.playSound("cancel");
        }
'@
    $equipmentText = Replace-Required $equipmentText $oldEquip $newEquip 'transactional equip confirmation'

    $oldUnequip = @'
        string itemName = current.DisplayName;
        if (_equipment.TryUnequip(_member, _selectedSlot, out _))
        {
            _progression.NormalizeMember(_member);
            _saveNow();
            ShowHud(_translation.Get("equipment.unequipped-name", new { item = itemName }));
            Game1.playSound("dwop");
            _hoveredItem = null;
            _focusInventory = false;
            ClampInventoryCursor();
        }
        else
        {
            ShowHud(_translation.Get("equipment.inventory-full"), error: true);
            Game1.playSound("cancel");
        }
'@
    $newUnequip = @'
        string itemName = current.DisplayName;
        if (_equipment.TryUnequip(_member, _selectedSlot, out string failureMessage))
        {
            bool committed = _equipment.GetEquipped(_member, _selectedSlot) is null
                && GetActualEquippedItem(_selectedSlot) is null;
            if (!committed)
            {
                ShowHud($"Team Up could not verify that {itemName} was removed from the NPC equipment slot.", error: true);
                Game1.playSound("cancel");
                return;
            }

            _progression.NormalizeMember(_member);
            _saveNow();
            ShowHud(_translation.Get("equipment.unequipped-name", new { item = itemName }));
            Game1.playSound("dwop");
            _hoveredItem = null;
            _focusInventory = false;
            ClampInventoryCursor();
        }
        else
        {
            ShowHud(string.IsNullOrWhiteSpace(failureMessage)
                ? _translation.Get("equipment.inventory-full").ToString()
                : failureMessage,
                error: true);
            Game1.playSound("cancel");
        }
'@
    $equipmentText = Replace-Required $equipmentText $oldUnequip $newUnequip 'transactional unequip confirmation'

    Write-Utf8 $equipment $equipmentText

    $equipmentText = Read-Lf $equipment
    foreach ($token in @(
        'ControllerActivationDebounceMs = 180',
        'ControllerMouseEchoSuppressionMs = 260',
        'TryBeginControllerActivation()',
        '_suppressMouseClickUntilMs',
        'Slot cards only select',
        'string requestedQualifiedId = item.QualifiedItemId',
        'bool committed = committedData is not null',
        'GetActualEquippedItem(_selectedSlot) is null',
        'DoubleClickWindowMs = 450'
    )) {
        if (-not $equipmentText.Contains($token)) { throw "Alpha 6.6.4 equipment token missing: $token" }
    }
    if ($equipmentText.Contains('IsDoubleClick(2000 + i)')) { throw 'Slot-card double-click unequip still exists.' }
    if (-not $equipmentText.Contains('IsDoubleClick(1000 + i)')) { throw 'Inventory double-click equip regression.' }

    Log 'Building Alpha 6.6.4 Controller Equipment Transaction Hotfix...'
    Log 'FIX: gamepad A/Y/X debounce enabled.'
    Log 'FIX: gamepad A virtual mouse echo suppression enabled.'
    Log 'FIX: slot-card double-click unequip removed; explicit X/button remains.'
    Log 'FIX: equip/unequip HUD success requires committed backend + global inventory state.'
    Log 'REGRESSION: inventory double-click equip remains 450ms.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.4 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_4_CONTROLLER_EQUIPMENT_TRANSACTION_HOTFIX_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.4'
    Log 'CONTROLLER INPUT ECHO GUARD: ENABLED'
    Log 'TRANSACTIONAL EQUIP HUD CONFIRMATION: ENABLED'
    Log 'INVENTORY DOUBLE-CLICK 450MS: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
