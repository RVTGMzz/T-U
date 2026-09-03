$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Normalize-Crlf([string]$text) {
    return $text.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")
}

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    $old = Normalize-Crlf $old
    $new = Normalize-Crlf $new
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Alpha 6.3.1 could not locate $label." }
    return $text.Replace($old, $new)
}

function Convert-ToJsonAsciiString([string]$value) {
    $builder = New-Object System.Text.StringBuilder
    foreach ($character in $value.ToCharArray()) {
        $code = [int][char]$character
        if ($character -eq '"') { [void]$builder.Append('\"'); continue }
        if ($character -eq '\') { [void]$builder.Append('\\'); continue }
        if ($character -eq "`r") { [void]$builder.Append('\r'); continue }
        if ($character -eq "`n") { [void]$builder.Append('\n'); continue }
        if ($character -eq "`t") { [void]$builder.Append('\t'); continue }
        if ($code -lt 32 -or $code -gt 126) { [void]$builder.Append(('\u{0:x4}' -f $code)); continue }
        [void]$builder.Append($character)
    }
    return $builder.ToString()
}

function Ensure-Translation([string]$path, [hashtable]$entries) {
    $text = Normalize-Crlf ([System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8))
    $newLines = New-Object System.Collections.Generic.List[string]
    foreach ($key in $entries.Keys) {
        if ($text.Contains('"' + $key + '"')) { continue }
        $escaped = Convert-ToJsonAsciiString ([string]$entries[$key])
        $newLines.Add('  "' + $key + '": "' + $escaped + '",')
    }
    if ($newLines.Count -eq 0) { return }
    $needle = '  "common.back":'
    if (-not $text.Contains($needle)) { throw "Alpha 6.3.1 i18n could not locate common.back in $path" }
    $block = [string]::Join("`r`n", $newLines)
    $text = $text.Replace($needle, $block + "`r`n`r`n" + $needle)
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

$projectPath = Join-Path $repoRoot 'src\TeamUp\TeamUp.csproj'
$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'
$equipmentPath = Join-Path $repoRoot 'src\TeamUp\UI\EquipmentMenu.cs'
$rpgPath = Join-Path $repoRoot 'src\TeamUp\Core\EquipmentRpgPolishService.cs'
$defaultPath = Join-Path $repoRoot 'src\TeamUp\i18n\default.json'
$viPath = Join-Path $repoRoot 'src\TeamUp\i18n\vi.json'
foreach ($path in @($projectPath, $modPath, $equipmentPath, $rpgPath, $defaultPath, $viPath)) {
    if (-not (Test-Path $path)) { throw "Alpha 6.3.1 required file missing: $path" }
}

# Version + runtime marker.
$project = [System.IO.File]::ReadAllText($projectPath, [System.Text.Encoding]::UTF8)
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', '<Version>0.2.0-alpha.6.3.1</Version>', 1)
[System.IO.File]::WriteAllText($projectPath, $project, $utf8NoBom)

$mod = Normalize-Crlf ([System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8))
$mod = $mod.Replace('build: v0.2.0-alpha.6.3.0', 'build: v0.2.0-alpha.6.3.1')
$mod = [regex]::Replace($mod, 'Team Up! v0\.2\.0-alpha\.6\.3\.0[^\"]*loaded\.', 'Team Up! v0.2.0-alpha.6.3.1 equipment RPG polish loaded.', 1)
[System.IO.File]::WriteAllText($modPath, $mod, $utf8NoBom)

Ensure-Translation $defaultPath @{
    'equipment.auto-equip' = 'AUTO EQUIP'
    'equipment.auto-equip-done' = 'Auto equipped {{count}} upgrade(s) for {{role}}.'
    'equipment.auto-equip-none' = 'No stronger role-fit gear was found.'
    'equipment.auto-hint' = 'Y: Auto Equip'
    'equipment.role-score' = 'ROLE SCORE'
    'equipment.skill-impact' = 'COMBAT IMPACT'
    'equipment.signature-cd' = 'Signature CD'
    'equipment.fit-label' = 'Role fit'
    'equipment.fit.excellent' = 'Excellent'
    'equipment.fit.good' = 'Good'
    'equipment.fit.neutral' = 'Neutral'
    'equipment.fit.poor' = 'Poor'
    'equipment.rarity.common' = 'Common'
    'equipment.rarity.uncommon' = 'Uncommon'
    'equipment.rarity.rare' = 'Rare'
    'equipment.rarity.epic' = 'Epic'
    'equipment.rarity.legendary' = 'Legendary'
}
Ensure-Translation $viPath @{
    'equipment.auto-equip' = 'TỰ ĐỘNG TRANG BỊ'
    'equipment.auto-equip-done' = 'Đã tự trang bị {{count}} món tốt hơn cho {{role}}.'
    'equipment.auto-equip-none' = 'Không tìm thấy trang bị phù hợp vai trò tốt hơn.'
    'equipment.auto-hint' = 'Y: Tự động trang bị'
    'equipment.role-score' = 'ĐIỂM VAI TRÒ'
    'equipment.skill-impact' = 'TÁC ĐỘNG CHIẾN ĐẤU'
    'equipment.signature-cd' = 'Hồi chiêu đặc trưng'
    'equipment.fit-label' = 'Độ hợp vai trò'
    'equipment.fit.excellent' = 'Rất phù hợp'
    'equipment.fit.good' = 'Phù hợp'
    'equipment.fit.neutral' = 'Trung tính'
    'equipment.fit.poor' = 'Kém phù hợp'
    'equipment.rarity.common' = 'Thường'
    'equipment.rarity.uncommon' = 'Không thường'
    'equipment.rarity.rare' = 'Hiếm'
    'equipment.rarity.epic' = 'Sử thi'
    'equipment.rarity.legendary' = 'Huyền thoại'
}

$equipment = Normalize-Crlf ([System.IO.File]::ReadAllText($equipmentPath, [System.Text.Encoding]::UTF8))

$equipment = Replace-Required $equipment @'
    private Rectangle _backBounds;
    private Rectangle _unequipBounds;
'@ @'
    private Rectangle _backBounds;
    private Rectangle _autoEquipBounds;
    private Rectangle _unequipBounds;
'@ 'auto-equip bounds field'

$equipment = Replace-Required $equipment @'
        _unequipBounds = new Rectangle(_loadoutPanel.X + 18, _loadoutPanel.Bottom - 64, _loadoutPanel.Width - 36, 44);
        _backBounds = new Rectangle(xPositionOnScreen + width - 214, yPositionOnScreen + height - 62, 180, 42);
'@ @'
        _autoEquipBounds = new Rectangle(_loadoutPanel.X + 18, _loadoutPanel.Bottom - 116, _loadoutPanel.Width - 36, 44);
        _unequipBounds = new Rectangle(_loadoutPanel.X + 18, _loadoutPanel.Bottom - 64, _loadoutPanel.Width - 36, 44);
        _backBounds = new Rectangle(xPositionOnScreen + width - 214, yPositionOnScreen + height - 62, 180, 42);
'@ 'auto-equip layout'

$equipment = Replace-Required $equipment @'
        if (_unequipBounds.Contains(x, y))
        {
            UnequipSelected();
            return;
        }
'@ @'
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
'@ 'auto-equip mouse action'

$equipment = Replace-Required $equipment @'
            case Keys.X:
                UnequipSelected();
                return;
'@ @'
            case Keys.Y:
                AutoEquipBest();
                return;
            case Keys.X:
                UnequipSelected();
                return;
'@ 'auto-equip keyboard action'

$equipment = Replace-Required $equipment @'
        if (b == Buttons.X)
            UnequipSelected();
'@ @'
        if (b == Buttons.Y)
        {
            AutoEquipBest();
            return;
        }
        if (b == Buttons.X)
            UnequipSelected();
'@ 'auto-equip controller action'

$equipment = Replace-Required $equipment @'
    private void UnequipSelected()
    {
'@ @'
    private void AutoEquipBest()
    {
        PartyRole role = EquipmentRpgPolishService.ResolveRole(_member);
        int changed = 0;

        foreach (EquipmentSlot slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.Armor, EquipmentSlot.Trinket })
        {
            float currentScore = EquipmentRpgPolishService.ScoreItem(role, _equipment.GetEquipped(_member, slot));
            EquipmentService.InventoryCandidate? best = null;
            float bestScore = currentScore;

            foreach (EquipmentService.InventoryCandidate candidate in _equipment.GetEligibleInventoryItems(slot))
            {
                EquippedItemData preview = EquipmentPreviewService.BuildPreview(slot, candidate.Item, _member.CharacterName);
                float score = EquipmentRpgPolishService.ScoreItem(role, preview);
                if (score <= bestScore + 0.01f)
                    continue;

                best = candidate;
                bestScore = score;
            }

            if (best is null)
                continue;

            if (!_equipment.TryEquip(_member, slot, best.InventoryIndex, out _))
                continue;

            changed++;
        }

        if (changed > 0)
        {
            _progression.NormalizeMember(_member);
            _saveNow();
            ShowHud(_translation.Get("equipment.auto-equip-done", new { count = changed, role = RoleLabel(role) }).ToString());
            Game1.playSound("reward");
            ClampInventoryCursor();
        }
        else
        {
            ShowHud(_translation.Get("equipment.auto-equip-none").ToString());
            Game1.playSound("cancel");
        }
    }

    private void UnequipSelected()
    {
'@ 'auto-equip method'

$equipment = Replace-Required $equipment @'
    private string SlotLabel(EquipmentSlot slot)
    {
'@ @'
    private static string RoleLabel(PartyRole role)
        => role == PartyRole.Damage ? "DPS" : role.ToString();

    private string SlotLabel(EquipmentSlot slot)
    {
'@ 'role label helper'

$oldHoverStart = @'
    private void DrawHoverComparison(SpriteBatch b, Item item)
    {
        bool compatible = CanEquip(_selectedSlot, item);
        int cardWidth = Math.Min(390, Math.Max(280, _inventoryPanel.Width - 36));
        int cardHeight = compatible ? 198 : 82;
        Rectangle card = new(
            _inventoryPanel.Right - cardWidth - 18,
            _inventoryPanel.Bottom - cardHeight - 16,
            cardWidth,
            cardHeight);

        DrawPanel(b, card, Color.White);
        b.DrawString(Game1.smallFont, item.DisplayName, new Vector2(card.X + 16, card.Y + 12), Game1.textColor);

        if (!compatible)
        {
            b.DrawString(
                Game1.smallFont,
                _translation.Get("equipment.incompatible"),
                new Vector2(card.X + 16, card.Y + 44),
                Color.DarkRed);
            return;
        }

        EquippedItemData? current = _equipment.GetEquipped(_member, _selectedSlot);
        EquippedItemData preview = EquipmentPreviewService.BuildPreview(_selectedSlot, item, _member.CharacterName);
        b.DrawString(
            Game1.smallFont,
            _translation.Get("equipment.compare-title"),
            new Vector2(card.X + 16, card.Y + 42),
            new Color(112, 73, 44));

        int y = card.Y + 72;
        DrawComparisonLine(b, card.X + 16, ref y, "ATK", current?.AttackBonus ?? 0, preview.AttackBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "DEF", current?.DefenseBonus ?? 0, preview.DefenseBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "HEAL", current?.HealPowerBonus ?? 0, preview.HealPowerBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "CTRL", current?.ControlPowerBonus ?? 0, preview.ControlPowerBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "CDR", current?.CooldownReductionPercent ?? 0, preview.CooldownReductionPercent, true);
    }
'@
$newHoverStart = @'
    private void DrawHoverComparison(SpriteBatch b, Item item)
    {
        bool compatible = CanEquip(_selectedSlot, item);
        int cardWidth = Math.Min(410, Math.Max(300, _inventoryPanel.Width - 32));
        int cardHeight = compatible ? 332 : 104;
        Rectangle card = new(
            _inventoryPanel.Right - cardWidth - 16,
            _inventoryPanel.Bottom - cardHeight - 14,
            cardWidth,
            cardHeight);

        DrawPanel(b, card, Color.White);
        EquipmentRarity rarity = EquipmentRpgPolishService.GetRarity(item);
        Color rarityColor = EquipmentRpgPolishService.GetRarityColor(rarity);
        b.DrawString(Game1.smallFont, item.DisplayName, new Vector2(card.X + 16, card.Y + 12), rarityColor);
        b.DrawString(
            Game1.smallFont,
            _translation.Get(EquipmentRpgPolishService.GetRarityTranslationKey(rarity)),
            new Vector2(card.X + 16, card.Y + 38),
            rarityColor * 0.90f);

        if (!compatible)
        {
            b.DrawString(
                Game1.smallFont,
                _translation.Get("equipment.incompatible"),
                new Vector2(card.X + 16, card.Y + 68),
                Color.DarkRed);
            return;
        }

        EquippedItemData? current = _equipment.GetEquipped(_member, _selectedSlot);
        EquippedItemData preview = EquipmentPreviewService.BuildPreview(_selectedSlot, item, _member.CharacterName);
        EquipmentImpactPreview impact = EquipmentRpgPolishService.BuildImpact(_progression, _member, _selectedSlot, preview);

        b.DrawString(
            Game1.smallFont,
            _translation.Get("equipment.compare-title"),
            new Vector2(card.X + 16, card.Y + 66),
            new Color(112, 73, 44));

        int y = card.Y + 94;
        DrawComparisonLine(b, card.X + 16, ref y, "ATK", current?.AttackBonus ?? 0, preview.AttackBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "DEF", current?.DefenseBonus ?? 0, preview.DefenseBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "HEAL", current?.HealPowerBonus ?? 0, preview.HealPowerBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "CTRL", current?.ControlPowerBonus ?? 0, preview.ControlPowerBonus, false);
        DrawComparisonLine(b, card.X + 16, ref y, "CDR", current?.CooldownReductionPercent ?? 0, preview.CooldownReductionPercent, true);

        y += 4;
        b.DrawString(Game1.smallFont, _translation.Get("equipment.skill-impact"), new Vector2(card.X + 16, y), new Color(112, 73, 44));
        y += 26;

        string fit = _translation.Get(impact.FitKey).ToString();
        b.DrawString(Game1.smallFont, $"{_translation.Get("equipment.fit-label")}: {fit} ({RoleLabel(impact.Role)})", new Vector2(card.X + 16, y), Game1.unselectedOptionColor);
        y += 24;
        DrawFloatComparisonLine(b, card.X + 16, ref y, _translation.Get("equipment.role-score").ToString(), impact.CurrentRoleScore, impact.NextRoleScore, "0.0");

        switch (impact.Role)
        {
            case PartyRole.Tank:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "DEF", impact.CurrentDefense, impact.NextDefense, "0");
                break;
            case PartyRole.Healer:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "HEAL x", impact.CurrentHealingMultiplier, impact.NextHealingMultiplier, "0.00");
                break;
            case PartyRole.Control:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "CTRL x", impact.CurrentControlMultiplier, impact.NextControlMultiplier, "0.00");
                break;
            case PartyRole.Support:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "CDR", (1f - impact.CurrentCooldownMultiplier) * 100f, (1f - impact.NextCooldownMultiplier) * 100f, "0", "%");
                break;
            default:
                DrawFloatComparisonLine(b, card.X + 16, ref y, "DMG x", impact.CurrentDamageMultiplier, impact.NextDamageMultiplier, "0.00");
                break;
        }

        if (impact.CurrentSignatureCooldownSeconds.HasValue && impact.NextSignatureCooldownSeconds.HasValue)
        {
            DrawFloatComparisonLine(
                b,
                card.X + 16,
                ref y,
                _translation.Get("equipment.signature-cd").ToString(),
                impact.CurrentSignatureCooldownSeconds.Value,
                impact.NextSignatureCooldownSeconds.Value,
                "0.0",
                "s",
                lowerIsBetter: true);
        }
    }
'@
$equipment = Replace-Required $equipment $oldHoverStart $newHoverStart 'RPG hover comparison card'

$equipment = Replace-Required $equipment @'
    public override void draw(SpriteBatch b)
'@ @'
    private static void DrawFloatComparisonLine(
        SpriteBatch b,
        int x,
        ref int y,
        string label,
        float current,
        float next,
        string format,
        string suffix = "",
        bool lowerIsBetter = false)
    {
        float delta = next - current;
        bool better = lowerIsBetter ? delta < -0.001f : delta > 0.001f;
        bool worse = lowerIsBetter ? delta > 0.001f : delta < -0.001f;
        string arrow = "→";
        string text = $"{label}  {current.ToString(format)}{suffix} {arrow} {next.ToString(format)}{suffix}";
        Color color = better
            ? new Color(72, 145, 76)
            : worse
                ? new Color(175, 72, 66)
                : Game1.unselectedOptionColor;
        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color);
        y += 24;
    }

    public override void draw(SpriteBatch b)
'@ 'float impact comparison helper'

$equipment = Replace-Required $equipment @'
            Rectangle iconBox = new(bounds.X + 10, bounds.Y + 15, 62, 62);
            DrawInset(b, iconBox, false);
            if (actualItem is not null)
                DrawItem(b, actualItem, iconBox, 1f);

            b.DrawString(Game1.smallFont, SlotLabel(slot), new Vector2(bounds.X + 82, bounds.Y + 14), Game1.textColor);
            string itemName = data?.DisplayName ?? _translation.Get("equipment.empty");
            DrawFitText(b, itemName, new Rectangle(bounds.X + 82, bounds.Y + 40, bounds.Width - 94, 24), selected ? Color.DarkRed : Game1.unselectedOptionColor, 0.92f);
'@ @'
            Rectangle iconBox = new(bounds.X + 10, bounds.Y + 15, 62, 62);
            DrawInset(b, iconBox, false);
            Color itemNameColor = selected ? Color.DarkRed : Game1.unselectedOptionColor;
            if (actualItem is not null)
            {
                Color rarityColor = EquipmentRpgPolishService.GetRarityColor(EquipmentRpgPolishService.GetRarity(actualItem));
                DrawRarityFrame(b, iconBox, rarityColor, 1f);
                DrawItem(b, actualItem, iconBox, 1f);
                itemNameColor = rarityColor;
            }

            b.DrawString(Game1.smallFont, SlotLabel(slot), new Vector2(bounds.X + 82, bounds.Y + 14), Game1.textColor);
            string itemName = data?.DisplayName ?? _translation.Get("equipment.empty");
            DrawFitText(b, itemName, new Rectangle(bounds.X + 82, bounds.Y + 40, bounds.Width - 94, 24), itemNameColor, 0.92f);
'@ 'equipped rarity frame'

$equipment = Replace-Required $equipment @'
        DrawButton(b, _unequipBounds, _translation.Get("equipment.unequip-button"), false);
'@ @'
        DrawButton(b, _autoEquipBounds, _translation.Get("equipment.auto-equip"), false);
        DrawButton(b, _unequipBounds, _translation.Get("equipment.unequip-button"), false);
'@ 'auto-equip draw button'

$equipment = Replace-Required $equipment @'
            bool compatible = CanEquip(_selectedSlot, item);
            DrawItem(b, item, bounds, compatible ? 1f : 0.28f);
'@ @'
            bool compatible = CanEquip(_selectedSlot, item);
            Color rarityColor = EquipmentRpgPolishService.GetRarityColor(EquipmentRpgPolishService.GetRarity(item));
            DrawRarityFrame(b, bounds, rarityColor, compatible ? 0.90f : 0.28f);
            DrawItem(b, item, bounds, compatible ? 1f : 0.28f);
'@ 'inventory rarity frame'

$equipment = Replace-Required $equipment @'
        string hint = _translation.Get("equipment.hint");
        DrawFitText(b, hint, new Rectangle(_inventoryPanel.X + 18, _inventoryPanel.Bottom - 80, _inventoryPanel.Width - 36, 24), Game1.unselectedOptionColor, 0.82f);
'@ @'
        string hint = $"{_translation.Get("equipment.hint")} · {_translation.Get("equipment.auto-hint")}";
        DrawFitText(b, hint, new Rectangle(_inventoryPanel.X + 18, _inventoryPanel.Bottom - 80, _inventoryPanel.Width - 36, 24), Game1.unselectedOptionColor, 0.82f);
'@ 'auto-equip hint'

$equipment = Replace-Required $equipment @'
    private static void DrawItem(SpriteBatch b, Item item, Rectangle bounds, float alpha)
'@ @'
    private static void DrawRarityFrame(SpriteBatch b, Rectangle bounds, Color color, float alpha)
    {
        Color drawColor = color * Math.Clamp(alpha, 0f, 1f);
        const int thickness = 3;
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), drawColor);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), drawColor);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), drawColor);
        b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), drawColor);
    }

    private static void DrawItem(SpriteBatch b, Item item, Rectangle bounds, float alpha)
'@ 'rarity frame renderer'

[System.IO.File]::WriteAllText($equipmentPath, $equipment, $utf8NoBom)

$verify = [System.IO.File]::ReadAllText($equipmentPath, [System.Text.Encoding]::UTF8)
$rpg = [System.IO.File]::ReadAllText($rpgPath, [System.Text.Encoding]::UTF8)
$modVerify = [System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8)
if (-not $verify.Contains('AutoEquipBest()') -or -not $verify.Contains('DrawRarityFrame') -or -not $verify.Contains('EquipmentImpactPreview')) {
    throw 'Alpha 6.3.1 UI verification failed.'
}
if (-not $rpg.Contains('ScoreItem') -or -not $rpg.Contains('SignatureCooldownTicks') -or -not $rpg.Contains('BuildImpact')) {
    throw 'Alpha 6.3.1 role-aware equipment service verification failed.'
}
if (-not $modVerify.Contains('build: v0.2.0-alpha.6.3.1')) {
    throw 'Alpha 6.3.1 runtime marker verification failed.'
}

Write-Host 'Alpha 6.3.1 equipment RPG polish integrated.'
Write-Host 'Rarity frames + role-aware scoring + combat impact + safe Auto Equip are enabled.'
