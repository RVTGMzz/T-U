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
$defaultPath = Join-Path $repoRoot 'src\TeamUp\i18n\default.json'
$viPath = Join-Path $repoRoot 'src\TeamUp\i18n\vi.json'

foreach ($path in @($projectPath, $modPath, $equipmentMenuPath, $profileMenuPath, $traitIconPath, $defaultPath, $viPath)) {
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
}

Ensure-Translation $viPath @{
    'equipment.loadout' = 'TRANG BỊ NPC'
    'equipment.inventory-title' = 'TÚI ĐỒ FARMER'
    'equipment.inventory-filter' = 'Đang chọn: {{slot}} · đồ phù hợp sẽ sáng rõ'
    'equipment.compatible' = 'Có thể trang bị'
    'equipment.incompatible' = 'Không phù hợp với slot này'
}

$equipment = [System.IO.File]::ReadAllText($equipmentMenuPath, [System.Text.Encoding]::UTF8)
$profile = [System.IO.File]::ReadAllText($profileMenuPath, [System.Text.Encoding]::UTF8)
$icons = [System.IO.File]::ReadAllText($traitIconPath, [System.Text.Encoding]::UTF8)
$modVerify = [System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8)

if (-not $equipment.Contains('private const int InventoryColumns = 6;')
    -or -not $equipment.Contains('item.drawInMenu(')
    -or -not $equipment.Contains('GetActualEquippedItem')) {
    throw 'NPC loadout verification failed: visual item-grid equipment menu markers are missing.'
}
if (-not $profile.Contains('TraitIconRenderer.Draw(')
    -or -not $profile.Contains('TraitIconRenderer.TraitIconKind.Passive')
    -or -not $profile.Contains('TraitIconRenderer.TraitIconKind.Signature')) {
    throw 'Trait icon profile verification failed.'
}
if (-not $icons.Contains('StableHash') -or -not $icons.Contains('BuildPattern')) {
    throw 'Trait icon renderer verification failed.'
}
if (-not $modVerify.Contains('build: v0.2.0-alpha.6.3.0')) {
    throw 'Alpha 6.3 debug marker verification failed.'
}

Write-Host 'Alpha 6.3.0 NPC loadout + unique trait icon integration complete.'
Write-Host 'Equipment UI: portrait + 3 live gear slots + 6x6 Farmer backpack grid.'
Write-Host 'Trait UI: deterministic unique Passive + Signature pixel icon per NPC.'
