$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Normalize-Crlf([string]$text) {
    return $text.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")
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
        if ($code -lt 32 -or $code -gt 126) {
            [void]$builder.Append(('\u{0:x4}' -f $code))
            continue
        }
        [void]$builder.Append($character)
    }
    return $builder.ToString()
}

function Ensure-Translation([string]$path, [hashtable]$entries) {
    $text = Normalize-Crlf ([System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8))
    $newLines = New-Object System.Collections.Generic.List[string]
    foreach ($key in $entries.Keys) {
        $marker = '"' + $key + '"'
        if ($text.Contains($marker)) { continue }
        $escaped = Convert-ToJsonAsciiString ([string]$entries[$key])
        $newLines.Add('  "' + $key + '": "' + $escaped + '",')
    }

    if ($newLines.Count -eq 0) { return }
    $needle = '  "common.back":'
    if (-not $text.Contains($needle)) { throw "NPC loadout i18n could not locate common.back in $path" }
    $block = [string]::Join("`r`n", $newLines)
    $text = $text.Replace($needle, $block + "`r`n`r`n" + $needle)
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

$projectPath = Join-Path $repoRoot 'src\TeamUp\TeamUp.csproj'
$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'
$equipmentMenuPath = Join-Path $repoRoot 'src\TeamUp\UI\EquipmentMenu.cs'
$profileMenuPath = Join-Path $repoRoot 'src\TeamUp\UI\CharacterProfileMenu.cs'
$traitIconPath = Join-Path $repoRoot 'src\TeamUp\UI\TraitIconRenderer.cs'
$previewPath = Join-Path $repoRoot 'src\TeamUp\Core\EquipmentPreviewService.cs'
$defaultPath = Join-Path $repoRoot 'src\TeamUp\i18n\default.json'
$viPath = Join-Path $repoRoot 'src\TeamUp\i18n\vi.json'

foreach ($path in @($projectPath, $modPath, $equipmentMenuPath, $profileMenuPath, $traitIconPath, $previewPath, $defaultPath, $viPath)) {
    if (-not (Test-Path $path)) { throw "NPC loadout required source is missing: $path" }
}

$project = [System.IO.File]::ReadAllText($projectPath, [System.Text.Encoding]::UTF8)
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', '<Version>0.2.0-alpha.6.3.0</Version>', 1)
[System.IO.File]::WriteAllText($projectPath, $project, $utf8NoBom)

$mod = Normalize-Crlf ([System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8))
$mod = [regex]::Replace($mod, 'Team Up! v[^\"]+ loaded\.', 'Team Up! v0.2.0-alpha.6.3.0 NPC loadout + trait icons loaded.', 1)
$mod = $mod.Replace('Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.2.0', 'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.3.0')
$mod = $mod.Replace('Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.3', 'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.3.0')
[System.IO.File]::WriteAllText($modPath, $mod, $utf8NoBom)

Ensure-Translation $defaultPath @{
    'equipment.loadout' = 'NPC LOADOUT'
    'equipment.inventory-title' = 'FARMER BACKPACK'
    'equipment.inventory-filter' = 'Selected slot: {{slot}} · compatible gear stays bright'
    'equipment.compatible' = 'Compatible'
    'equipment.incompatible' = 'Not compatible with this slot'
    'equipment.compare-title' = 'STAT COMPARISON'
}

Ensure-Translation $viPath @{
    'equipment.loadout' = 'TRANG BỊ NPC'
    'equipment.inventory-title' = 'TÚI ĐỒ FARMER'
    'equipment.inventory-filter' = 'Đang chọn: {{slot}} · đồ phù hợp sẽ sáng rõ'
    'equipment.compatible' = 'Có thể trang bị'
    'equipment.incompatible' = 'Không phù hợp với slot này'
    'equipment.compare-title' = 'SO SÁNH CHỈ SỐ'
}

$equipment = Normalize-Crlf ([System.IO.File]::ReadAllText($equipmentMenuPath, [System.Text.Encoding]::UTF8))
if (-not $equipment.Contains('private void DrawHoverComparison(SpriteBatch b, Item item)')) {
$methods = @'
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

    private static void DrawComparisonLine(
        SpriteBatch b,
        int x,
        ref int y,
        string label,
        int current,
        int next,
        bool percent)
    {
        if (current == 0 && next == 0)
            return;

        int delta = next - current;
        string suffix = percent ? "%" : string.Empty;
        string deltaText = delta == 0 ? string.Empty : $"  ({(delta > 0 ? "+" : string.Empty)}{delta}{suffix})";
        string text = $"{label}  {current}{suffix} \u2192 {next}{suffix}{deltaText}";
        Color color = delta > 0
            ? new Color(72, 145, 76)
            : delta < 0
                ? new Color(175, 72, 66)
                : Game1.unselectedOptionColor;

        b.DrawString(Game1.smallFont, text, new Vector2(x, y), color);
        y += 24;
    }

'@
    $needle = '    public override void draw(SpriteBatch b)'
    if (-not $equipment.Contains($needle)) { throw 'NPC loadout comparison could not locate draw method insertion point.' }
    $equipment = $equipment.Replace($needle, $methods + $needle)

$oldHover = @'
        if (_hoveredItem is not null)
        {
            string compatibility = CanEquip(_selectedSlot, _hoveredItem)
                ? _translation.Get("equipment.compatible")
                : _translation.Get("equipment.incompatible");
            string hover = $"{_hoveredItem.DisplayName}  ·  {compatibility}";
            Vector2 size = Game1.smallFont.MeasureString(hover);
            Rectangle strip = new(
                _inventoryPanel.X + 18,
                _inventoryPanel.Bottom - 48,
                _inventoryPanel.Width - 36,
                30);
            float scale = size.X <= strip.Width ? 1f : Math.Max(0.72f, strip.Width / size.X);
            b.DrawString(Game1.smallFont, hover, new Vector2(strip.X, strip.Y), Game1.textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
        }
'@
$newHover = @'
        if (_hoveredItem is not null)
            DrawHoverComparison(b, _hoveredItem);
'@
    $oldHoverNormalized = Normalize-Crlf ($oldHover.TrimEnd())
    $newHoverNormalized = Normalize-Crlf ($newHover.TrimEnd())
    if (-not $equipment.Contains($oldHoverNormalized)) {
        throw 'NPC loadout comparison could not locate the old hover strip.'
    }
    $equipment = $equipment.Replace($oldHoverNormalized, $newHoverNormalized)
    [System.IO.File]::WriteAllText($equipmentMenuPath, $equipment, $utf8NoBom)
}

$equipment = [System.IO.File]::ReadAllText($equipmentMenuPath, [System.Text.Encoding]::UTF8)
$profile = [System.IO.File]::ReadAllText($profileMenuPath, [System.Text.Encoding]::UTF8)
$icons = [System.IO.File]::ReadAllText($traitIconPath, [System.Text.Encoding]::UTF8)
$preview = [System.IO.File]::ReadAllText($previewPath, [System.Text.Encoding]::UTF8)
$defaultVerify = [System.IO.File]::ReadAllText($defaultPath, [System.Text.Encoding]::UTF8)
$modVerify = [System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8)

$loadoutOk = $equipment.Contains('private const int InventoryColumns = 6;') -and $equipment.Contains('item.drawInMenu(') -and $equipment.Contains('GetActualEquippedItem')
if (-not $loadoutOk) {
    throw 'NPC loadout verification failed: visual item-grid equipment menu markers are missing.'
}

$comparisonOk = $equipment.Contains('DrawHoverComparison') -and $equipment.Contains('EquipmentPreviewService.BuildPreview') -and $defaultVerify.Contains('equipment.compare-title')
if (-not $comparisonOk) {
    throw 'NPC loadout stat comparison verification failed.'
}

$previewOk = $preview.Contains('BuildPreview') -and $preview.Contains('CooldownReductionPercent')
if (-not $previewOk) {
    throw 'Equipment preview formula verification failed.'
}

$profileOk = $profile.Contains('TraitIconRenderer.Draw(') -and $profile.Contains('TraitIconRenderer.TraitIconKind.Passive') -and $profile.Contains('TraitIconRenderer.TraitIconKind.Signature')
if (-not $profileOk) {
    throw 'Trait icon profile verification failed.'
}

$iconsOk = $icons.Contains('StableHash') -and $icons.Contains('BuildPattern')
if (-not $iconsOk) {
    throw 'Trait icon renderer verification failed.'
}

if (-not $modVerify.Contains('build: v0.2.0-alpha.6.3.0')) {
    throw 'Alpha 6.3 debug marker verification failed.'
}

Write-Host 'Alpha 6.3.0 NPC loadout + unique trait icon integration complete.'
Write-Host 'Equipment UI: portrait + 3 live gear slots + 6x6 Farmer backpack grid + hover stat comparison.'
Write-Host 'Trait UI: deterministic unique Passive + Signature pixel icon per NPC.'
