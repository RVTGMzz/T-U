$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$equipmentPath = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$profilePath = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$combatPath = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$alpha6Path = Join-Path $root 'src\TeamUp\Combat\Alpha6CombatPolishService.cs'
$projectPath = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$modEntryPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$defaultI18nPath = Join-Path $root 'src\TeamUp\i18n\default.json'
$viI18nPath = Join-Path $root 'src\TeamUp\i18n\vi.json'
$version = '0.2.0-alpha.6.4.4'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing Alpha 6.4.4 source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}

function WriteText([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom)
}

# Version + startup marker.
$project = ReadText $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteText $projectPath $project

$mod = ReadText $modEntryPath
$mod = $mod.Replace('0.2.0-alpha.6.4.3', $version)
$mod = $mod.Replace('Cardcha combat sandbox loaded.', 'test feedback + equipment hotfix loaded.')
WriteText $modEntryPath $mod

# -----------------------------------------------------------------------------
# Equipment UX hotfixes
# -----------------------------------------------------------------------------
$equipment = ReadText $equipmentPath

# Clicking any equippable item now auto-selects its natural slot. This fixes the
# confusing "Scythe only works after reopening the menu" case when Trinket was selected.
$oldEquipGuard = @'
        Item? item = Game1.player.Items[inventoryIndex];
        if (item is null || !CanEquip(_selectedSlot, item))
        {
            Game1.playSound("cancel");
            return;
        }

        if (_equipment.TryEquip(_member, _selectedSlot, inventoryIndex, out _))
'@
$newEquipGuard = @'
        Item? item = Game1.player.Items[inventoryIndex];
        if (item is null)
        {
            Game1.playSound("cancel");
            return;
        }

        EquipmentSlot? naturalSlot = GetNaturalSlot(item);
        if (!naturalSlot.HasValue)
        {
            Game1.playSound("cancel");
            return;
        }

        _selectedSlot = naturalSlot.Value;
        if (_equipment.TryEquip(_member, _selectedSlot, inventoryIndex, out _))
'@
if ($equipment.Contains($oldEquipGuard)) {
    $equipment = $equipment.Replace($oldEquipGuard, $newEquipGuard)
}
elseif ($equipment -notmatch 'EquipmentSlot\? naturalSlot = GetNaturalSlot\(item\)') {
    throw 'Could not patch natural-slot auto selection in EquipmentMenu.'
}

# Clear stale hover state after an equipment move.
$equipment = $equipment.Replace(
    "            Game1.playSound(\"coin\");`n            ClampInventoryCursor();",
    "            Game1.playSound(\"coin\");`n            _hoveredItem = null;`n            ClampInventoryCursor();")

# Auto Equip should leave the UI focused on the last slot it actually changed so X
# immediately returns that exact item instead of silently targeting an empty old slot.
if ($equipment -notmatch 'EquipmentSlot\? lastChangedSlot = null;') {
    $needle = "        int changed = 0;`n`n        foreach (EquipmentSlot slot"
    $replacement = "        int changed = 0;`n        EquipmentSlot? lastChangedSlot = null;`n`n        foreach (EquipmentSlot slot"
    if (-not $equipment.Contains($needle)) { throw 'Could not insert Auto Equip lastChangedSlot.' }
    $equipment = $equipment.Replace($needle, $replacement)
}
$equipment = $equipment.Replace("            changed++;`n        }", "            changed++;`n            lastChangedSlot = slot;`n        }")
if ($equipment -notmatch '_selectedSlot = lastChangedSlot.Value;') {
    $needle = "        if (changed > 0)`n        {`n            _progression.NormalizeMember(_member);"
    $replacement = "        if (changed > 0)`n        {`n            if (lastChangedSlot.HasValue)`n                _selectedSlot = lastChangedSlot.Value;`n            _focusInventory = false;`n            _hoveredItem = null;`n            _progression.NormalizeMember(_member);"
    if (-not $equipment.Contains($needle)) { throw 'Could not patch Auto Equip focus handoff.' }
    $equipment = $equipment.Replace($needle, $replacement)
}

# Repeated X now walks through equipped slots if the currently selected slot is empty.
$oldUnequipStart = @'
        EquippedItemData? current = _equipment.GetEquipped(_member, _selectedSlot);
        if (current is null)
        {
            Game1.playSound("cancel");
            return;
        }

        string itemName = current.DisplayName;
'@
$newUnequipStart = @'
        EquippedItemData? current = _equipment.GetEquipped(_member, _selectedSlot);
        if (current is null)
        {
            foreach (EquipmentSlot fallback in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Trinket })
            {
                current = _equipment.GetEquipped(_member, fallback);
                if (current is null)
                    continue;
                _selectedSlot = fallback;
                break;
            }
        }

        if (current is null)
        {
            Game1.playSound("cancel");
            return;
        }

        string itemName = current.DisplayName;
'@
if ($equipment.Contains($oldUnequipStart)) {
    $equipment = $equipment.Replace($oldUnequipStart, $newUnequipStart)
}
elseif ($equipment -notmatch 'foreach \(EquipmentSlot fallback') {
    throw 'Could not patch unequip fallback slot selection.'
}
$equipment = $equipment.Replace(
    "            Game1.playSound(\"dwop\");`n            ClampInventoryCursor();",
    "            Game1.playSound(\"dwop\");`n            _hoveredItem = null;`n            _focusInventory = false;`n            ClampInventoryCursor();")

# Natural-slot helper. All supported equipment can stay bright in the backpack even if
# another loadout slot is currently selected.
if ($equipment -notmatch 'private static EquipmentSlot\? GetNaturalSlot') {
    $needle = '    private static bool CanEquip(EquipmentSlot slot, Item item)'
    $helper = @'
    private static EquipmentSlot? GetNaturalSlot(Item item)
    {
        return item switch
        {
            Boots => EquipmentSlot.Armor,
            Ring => EquipmentSlot.Trinket,
            Trinket => EquipmentSlot.Trinket,
            MeleeWeapon => EquipmentSlot.Weapon,
            _ => null
        };
    }

'@
    $index = $equipment.IndexOf($needle)
    if ($index -lt 0) { throw 'Could not locate CanEquip for natural-slot helper.' }
    $equipment = $equipment.Insert($index, $helper)
}

# Hover comparison follows the hovered item's own slot rather than calling a Scythe
# incompatible merely because Trinket was previously selected.
$oldHoverHeader = @'
    private void DrawHoverComparison(SpriteBatch b, Item item)
    {
        bool compatible = CanEquip(_selectedSlot, item);
        int cardWidth = Math.Min(410, Math.Max(300, _inventoryPanel.Width - 32));
'@
$newHoverHeader = @'
    private void DrawHoverComparison(SpriteBatch b, Item item)
    {
        EquipmentSlot? naturalSlot = GetNaturalSlot(item);
        bool compatible = naturalSlot.HasValue;
        EquipmentSlot previewSlot = naturalSlot ?? _selectedSlot;
        int cardWidth = Math.Min(410, Math.Max(300, _inventoryPanel.Width - 32));
'@
if ($equipment.Contains($oldHoverHeader)) {
    $equipment = $equipment.Replace($oldHoverHeader, $newHoverHeader)
}
elseif ($equipment -notmatch 'EquipmentSlot previewSlot = naturalSlot') {
    throw 'Could not patch hover natural-slot comparison.'
}
$equipment = $equipment.Replace(
    '        EquippedItemData? current = _equipment.GetEquipped(_member, _selectedSlot);`n        EquippedItemData preview = EquipmentPreviewService.BuildPreview(_selectedSlot, item, _member.CharacterName);`n        EquipmentImpactPreview impact = EquipmentRpgPolishService.BuildImpact(_progression, _member, _selectedSlot, preview);',
    '        EquippedItemData? current = _equipment.GetEquipped(_member, previewSlot);`n        EquippedItemData preview = EquipmentPreviewService.BuildPreview(previewSlot, item, _member.CharacterName);`n        EquipmentImpactPreview impact = EquipmentRpgPolishService.BuildImpact(_progression, _member, previewSlot, preview);')
# The string above may have LF normalized but PowerShell single quoted `n is literal; apply a regex fallback.
$equipment = [regex]::Replace(
    $equipment,
    '        EquippedItemData\? current = _equipment\.GetEquipped\(_member, _selectedSlot\);\n        EquippedItemData preview = EquipmentPreviewService\.BuildPreview\(_selectedSlot, item, _member\.CharacterName\);\n        EquipmentImpactPreview impact = EquipmentRpgPolishService\.BuildImpact\(_progression, _member, _selectedSlot, preview\);',
    "        EquippedItemData? current = _equipment.GetEquipped(_member, previewSlot);`n        EquippedItemData preview = EquipmentPreviewService.BuildPreview(previewSlot, item, _member.CharacterName);`n        EquipmentImpactPreview impact = EquipmentRpgPolishService.BuildImpact(_progression, _member, previewSlot, preview);",
    1)

# Move the comparison card upward so it cannot cover the controls footer.
$equipment = [regex]::Replace(
    $equipment,
    '        Rectangle card = new\(\n            _inventoryPanel\.Right - cardWidth - 16,\n            _inventoryPanel\.Bottom - cardHeight - 14,\n            cardWidth,\n            cardHeight\);',
    "        int cardY = Math.Max(_inventoryPanel.Y + 78, _inventoryPanel.Bottom - cardHeight - 92);`n        Rectangle card = new(`n            _inventoryPanel.Right - cardWidth - 16,`n            cardY,`n            cardWidth,`n            cardHeight);",
    1)

# Keep all supported gear readable/bright; click selects the correct slot automatically.
$equipment = $equipment.Replace('            bool compatible = CanEquip(_selectedSlot, item);', '            bool compatible = GetNaturalSlot(item).HasValue;')

# Replace the tiny one-line footer with a short, larger control legend.
$oldHint = @'
        string hint = $"{_translation.Get("equipment.hint")} · {_translation.Get("equipment.auto-hint")}";
        DrawFitText(b, hint, new Rectangle(_inventoryPanel.X + 18, _inventoryPanel.Bottom - 80, _inventoryPanel.Width - 36, 24), Game1.unselectedOptionColor, 0.82f);
'@
$newHint = @'
        string hint = _translation.Get("equipment.controls-short");
        DrawFitText(b, hint, new Rectangle(_inventoryPanel.X + 18, _inventoryPanel.Bottom - 54, _inventoryPanel.Width - 36, 30), Game1.unselectedOptionColor, 1.02f);
'@
if ($equipment.Contains($oldHint)) {
    $equipment = $equipment.Replace($oldHint, $newHint)
}
elseif ($equipment -notmatch 'equipment\.controls-short') {
    throw 'Could not patch compact equipment controls footer.'
}

WriteText $equipmentPath $equipment

# -----------------------------------------------------------------------------
# Character Profile overflow / placeholder readability
# -----------------------------------------------------------------------------
$profile = ReadText $profilePath
$profile = $profile.Replace('Alpha 6.4.2 doubles passive/signature description text, adds a clipped scroll region, and reserves the single character icon', 'Alpha 6.4.4 keeps real skill descriptions large, moves relationship data into the scroll region, keeps pending kits compact, and reserves the single character icon')

# Relationship no longer lives in the fixed-height left identity column.
$relationshipLeft = @'
        cursorY += (int)(Game1.smallFont.MeasureString(wrapped).Y * BodyScale) + 14;

        DrawScaledString(b, Game1.smallFont, _i18n.Get("profile.relationship"), new Vector2(x + SectionPadding, cursorY), new Color(112, 73, 44), CaptionScale);
        cursorY += (int)(Game1.smallFont.LineSpacing * CaptionScale) + 2;
        string relationship = WrapScaled(_relationshipText, panelWidth - SectionPadding * 2, 1.02f);
        DrawScaledString(b, Game1.smallFont, relationship, new Vector2(x + SectionPadding, cursorY), Game1.textColor, 1.02f);
'@
$relationshipLeftReplacement = @'
        cursorY += (int)(Game1.smallFont.MeasureString(wrapped).Y * BodyScale) + 14;
'@
if ($profile.Contains($relationshipLeft)) {
    $profile = $profile.Replace($relationshipLeft, $relationshipLeftReplacement)
}
elseif ($profile -match 'WrapScaled\(_relationshipText, panelWidth') {
    throw 'Relationship fixed-column block still exists but did not match expected Alpha 6.4.2 layout.'
}

# Real completed skills keep x2 text. Placeholder expansion kits use normal body scale so
# "signature pending" never becomes a giant paragraph.
$profile = $profile.Replace(
    '        int contentWidth = Math.Max(120, viewport.Width - 16);`n        int contentHeight = CalculateTraitContentHeight(contentWidth);',
    '        int contentWidth = Math.Max(120, viewport.Width - 16);`n        bool pendingKit = IsPendingCombatKit();`n        float passiveScale = pendingKit ? BodyScale : DescriptionScale;`n        float signatureScale = pendingKit ? BodyScale : DescriptionScale;`n        int contentHeight = CalculateTraitContentHeight(contentWidth, passiveScale, signatureScale);')
$profile = [regex]::Replace(
    $profile,
    '        int contentWidth = Math\.Max\(120, viewport\.Width - 16\);\n        int contentHeight = CalculateTraitContentHeight\(contentWidth\);',
    "        int contentWidth = Math.Max(120, viewport.Width - 16);`n        bool pendingKit = IsPendingCombatKit();`n        float passiveScale = pendingKit ? BodyScale : DescriptionScale;`n        float signatureScale = pendingKit ? BodyScale : DescriptionScale;`n        int contentHeight = CalculateTraitContentHeight(contentWidth, passiveScale, signatureScale);",
    1)
$profile = $profile.Replace('        string passiveWrapped = WrapScaled(_passiveText, contentWidth, DescriptionScale);', '        string passiveWrapped = WrapScaled(_passiveText, contentWidth, passiveScale);')
$profile = $profile.Replace('        contentY += DrawWrappedLinesInViewport(b, passiveWrapped, viewport.X, contentY, DescriptionScale, viewport);', '        contentY += DrawWrappedLinesInViewport(b, passiveWrapped, viewport.X, contentY, passiveScale, viewport);')
$profile = $profile.Replace('        string signatureWrapped = WrapScaled(_signatureText, contentWidth, DescriptionScale);`n        DrawWrappedLinesInViewport(b, signatureWrapped, viewport.X, contentY, DescriptionScale, viewport);', '        string signatureWrapped = WrapScaled(_signatureText, contentWidth, signatureScale);`n        contentY += DrawWrappedLinesInViewport(b, signatureWrapped, viewport.X, contentY, signatureScale, viewport);`n        contentY += 22;`n`n        DrawSectionTitleIfVisible(b, _i18n.Get("profile.relationship"), viewport.X, contentY, viewport);`n        contentY += 30;`n        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, BodyScale);`n        DrawWrappedLinesInViewport(b, relationshipWrapped, viewport.X, contentY, BodyScale, viewport);')
$profile = [regex]::Replace(
    $profile,
    '        string signatureWrapped = WrapScaled\(_signatureText, contentWidth, DescriptionScale\);\n        DrawWrappedLinesInViewport\(b, signatureWrapped, viewport\.X, contentY, DescriptionScale, viewport\);',
    "        string signatureWrapped = WrapScaled(_signatureText, contentWidth, signatureScale);`n        contentY += DrawWrappedLinesInViewport(b, signatureWrapped, viewport.X, contentY, signatureScale, viewport);`n        contentY += 22;`n`n        DrawSectionTitleIfVisible(b, _i18n.Get(\"profile.relationship\"), viewport.X, contentY, viewport);`n        contentY += 30;`n        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, BodyScale);`n        DrawWrappedLinesInViewport(b, relationshipWrapped, viewport.X, contentY, BodyScale, viewport);",
    1)

# Update content-height calculation for dynamic scales + relationship section.
$oldCalc = @'
    private int CalculateTraitContentHeight(int contentWidth)
    {
        string passiveWrapped = WrapScaled(_passiveText, contentWidth, DescriptionScale);
        string signatureWrapped = WrapScaled(_signatureText, contentWidth, DescriptionScale);
        return 30
            + MeasureWrappedHeight(passiveWrapped, DescriptionScale)
            + 18
            + SignatureHeaderHeight
            + MeasureWrappedHeight(signatureWrapped, DescriptionScale);
    }
'@
$newCalc = @'
    private int CalculateTraitContentHeight(int contentWidth, float passiveScale, float signatureScale)
    {
        string passiveWrapped = WrapScaled(_passiveText, contentWidth, passiveScale);
        string signatureWrapped = WrapScaled(_signatureText, contentWidth, signatureScale);
        string relationshipWrapped = WrapScaled(_relationshipText, contentWidth, BodyScale);
        return 30
            + MeasureWrappedHeight(passiveWrapped, passiveScale)
            + 18
            + SignatureHeaderHeight
            + MeasureWrappedHeight(signatureWrapped, signatureScale)
            + 22
            + 30
            + MeasureWrappedHeight(relationshipWrapped, BodyScale);
    }

    private bool IsPendingCombatKit()
    {
        if (_profile is null)
            return true;

        return _profile.PrimaryRole == PartyRole.Unassigned
            && _profile.SecondaryRole == PartyRole.Unassigned
            && _profile.TankAffinity == 0
            && _profile.DamageAffinity == 0
            && _profile.SupportAffinity == 0
            && _profile.HealerAffinity == 0
            && _profile.ControlAffinity == 0;
    }
'@
if ($profile.Contains($oldCalc)) {
    $profile = $profile.Replace($oldCalc, $newCalc)
}
elseif ($profile -notmatch 'private bool IsPendingCombatKit\(\)') {
    throw 'Could not patch CharacterProfile content-height calculation.'
}
WriteText $profilePath $profile

# -----------------------------------------------------------------------------
# Healing feedback / responsiveness
# -----------------------------------------------------------------------------
$combat = ReadText $combatPath
$combat = $combat.Replace('float farmerThreshold = role == PartyRole.Healer ? 0.78f : 0.52f;', 'float farmerThreshold = role == PartyRole.Healer ? 0.82f : 0.60f;')
$combat = $combat.Replace('_healCooldowns[member.CharacterName] = role == PartyRole.Healer ? 225 : 345;', '_healCooldowns[member.CharacterName] = role == PartyRole.Healer ? 210 : 315;')
$combat = $combat.Replace('            targetNpc.showTextAboveHead($"+{restored} HP", color, 2, 1000, 0);', '            targetNpc.showTextAboveHead($"+{restored} HP", color, 2, 1250, 0);')
$combat = $combat.Replace('            healer.showTextAboveHead($"+{restored} HP", color, 2, 1000, 0);', '            healer.showTextAboveHead($"HEAL +{restored}", color, 2, 1400, 0);')
WriteText $combatPath $combat

# Emily's dedicated Signature was overly conservative in live testing. Tier 2 can now fire
# for a clearly wounded Farmer even if no second ally is injured; Tier 3 reacts earlier.
$alpha6 = ReadText $alpha6Path
$oldEmilyCondition = '        if (urgent < 2 && !(tier >= 3 && urgent >= 1 && farmerRatio < 0.58f))`n            return false;'
$newEmilyCondition = '        bool signatureReady = urgent >= 2`n            || (tier >= 3 && urgent >= 1 && farmerRatio < 0.72f)`n            || (tier == 2 && urgent >= 1 && farmerRatio < 0.62f);`n        if (!signatureReady)`n            return false;'
$alpha6 = $alpha6.Replace($oldEmilyCondition, $newEmilyCondition)
$alpha6 = [regex]::Replace(
    $alpha6,
    '        if \(urgent < 2 && !\(tier >= 3 && urgent >= 1 && farmerRatio < 0\.58f\)\)\n            return false;',
    "        bool signatureReady = urgent >= 2`n            || (tier >= 3 && urgent >= 1 && farmerRatio < 0.72f)`n            || (tier == 2 && urgent >= 1 && farmerRatio < 0.62f);`n        if (!signatureReady)`n            return false;",
    1)
WriteText $alpha6Path $alpha6

# Compact localized control legend.
$defaultI18n = ReadText $defaultI18nPath
if ($defaultI18n -notmatch '"equipment.controls-short"') {
    $defaultI18n = $defaultI18n.Replace('  "equipment.auto-hint": "Y: Auto Equip",', '  "equipment.auto-hint": "Y: Auto Equip",`n  "equipment.controls-short": "A / Enter: Equip   ·   X: Unequip   ·   Y: Auto Equip",')
    $defaultI18n = $defaultI18n.Replace('`n', "`n")
}
$defaultI18n = $defaultI18n.Replace('"equipment.inventory-filter": "Selected slot: {{slot}} \u00b7 compatible gear stays bright"', '"equipment.inventory-filter": "Selected: {{slot}} · click any supported gear to auto-select its slot"')
WriteText $defaultI18nPath $defaultI18n

$viI18n = ReadText $viI18nPath
if ($viI18n -notmatch '"equipment.controls-short"') {
    $viI18n = $viI18n.Replace('  "equipment.auto-hint": "Y: T\u1ef1 \u0111\u1ed9ng trang b\u1ecb",', '  "equipment.auto-hint": "Y: T\u1ef1 \u0111\u1ed9ng trang b\u1ecb",`n  "equipment.controls-short": "A / Enter: Trang b\u1ecb   ·   X: Th\u00e1o   ·   Y: T\u1ef1 \u0111\u1ed9ng",')
    $viI18n = $viI18n.Replace('`n', "`n")
}
$viI18n = $viI18n.Replace('"equipment.inventory-filter": "\u0110ang ch\u1ecdn: {{slot}} \u00b7 \u0111\u1ed3 ph\u00f9 h\u1ee3p s\u1ebd s\u00e1ng r\u00f5"', '"equipment.inventory-filter": "\u0110ang ch\u1ecdn: {{slot}} · b\u1ea5m v\u00e0o \u0111\u1ed3 h\u1ee3p l\u1ec7 \u0111\u1ec3 t\u1ef1 ch\u1ecdn \u0111\u00fang \u00f4"')
WriteText $viI18nPath $viI18n

# Acceptance checks before compile.
$project = ReadText $projectPath
$mod = ReadText $modEntryPath
$equipment = ReadText $equipmentPath
$profile = ReadText $profilePath
$combat = ReadText $combatPath
$alpha6 = ReadText $alpha6Path
$defaultI18n = ReadText $defaultI18nPath
$viI18n = ReadText $viI18nPath

if ($project -notmatch '<Version>0\.2\.0-alpha\.6\.4\.4</Version>') { throw 'Alpha 6.4.4 version was not materialized.' }
if ($mod -notmatch 'build: v0\.2\.0-alpha\.6\.4\.4') { throw 'Alpha 6.4.4 debug marker missing.' }
if ($mod -notmatch 'test feedback \+ equipment hotfix loaded') { throw 'Alpha 6.4.4 load marker missing.' }
if ($equipment -notmatch 'EquipmentSlot\? naturalSlot = GetNaturalSlot\(item\)') { throw 'Natural-slot equipment click fix missing.' }
if ($equipment -notmatch 'EquipmentSlot\? lastChangedSlot = null') { throw 'Auto Equip focus fix missing.' }
if ($equipment -notmatch 'foreach \(EquipmentSlot fallback') { throw 'Unequip fallback fix missing.' }
if ($equipment -notmatch 'equipment\.controls-short') { throw 'Readable equipment controls footer missing.' }
if ($equipment -notmatch 'cardY = Math\.Max') { throw 'Hover card/footer separation missing.' }
if ($profile -match 'WrapScaled\(_relationshipText, panelWidth') { throw 'Relationship still renders in fixed left column.' }
if ($profile -notmatch 'DrawSectionTitleIfVisible\(b, _i18n\.Get\("profile\.relationship"\)') { throw 'Relationship scroll section missing.' }
if ($profile -notmatch 'IsPendingCombatKit\(\)') { throw 'Pending-kit compact scale missing.' }
if ($combat -notmatch '0\.82f : 0\.60f') { throw 'Recovery responsiveness adjustment missing.' }
if ($combat -notmatch 'HEAL \+\{restored\}') { throw 'Farmer heal feedback label missing.' }
if ($alpha6 -notmatch 'farmerRatio < 0\.72f') { throw 'Emily Signature responsiveness adjustment missing.' }
if ($defaultI18n -notmatch '"equipment.controls-short"' -or $viI18n -notmatch '"equipment.controls-short"') { throw 'Localized compact equipment controls missing.' }

Write-Host 'Alpha 6.4.4 Test Feedback + Equipment Hotfix integrated.'
Write-Host 'Fixes: natural-slot item clicks, reliable unequip after Auto Equip, readable backpack controls, hover-card separation, relationship overflow, pending-kit text scale, and clearer healer response.'
Write-Host 'No new save fields; Cardcha sandbox, Bond, skills, equipment backend, and one-Signature-icon rules remain intact.'
