$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Patch-I18n([string]$path, [bool]$vi) {
    $text = Get-Content $path -Raw

    if ($text.Contains('"member.equipment"')) {
        Write-Host "i18n already patched: $path"
        return
    }

    if ($vi) {
        $oldMember = @'
  "member.vault": "Kho chung Party",
'@
        $newMember = @'
  "member.equipment": "Trang bị",
  "member.vault": "Kho chung Party",
'@
        $oldMember = $oldMember.TrimEnd()
        $newMember = $newMember.TrimEnd()
        $text = $text.Replace($oldMember, $newMember)

        $insert = @'
  "equipment.question": "TRANG BỊ · {{name}}\n{{progression}}\nChọn ô trang bị.",
  "equipment.slot-line": "{{slot}}: {{item}}",
  "equipment.weapon": "Vũ khí",
  "equipment.armor": "Giáp / Giày",
  "equipment.trinket": "Nhẫn / Trinket",
  "equipment.empty": "Chưa trang bị",
  "equipment.choose": "{{slot}}\nHiện tại: {{current}}\nChọn đồ phù hợp trong túi.",
  "equipment.unequip": "Tháo trang bị",
  "equipment.equipped": "Đã trang bị.",
  "equipment.unequipped": "Đã tháo trang bị.",
  "equipment.inventory-full": "Túi đồ không đủ chỗ hoặc món đồ không còn hợp lệ.",
  "equipment.leave-blocked": "Hãy chừa chỗ trong túi để nhận lại trang bị trước khi NPC rời đội.",

'@
    }
    else {
        $oldMember = @'
  "member.vault": "Party Vault",
'@
        $newMember = @'
  "member.equipment": "Equipment",
  "member.vault": "Party Vault",
'@
        $oldMember = $oldMember.TrimEnd()
        $newMember = $newMember.TrimEnd()
        $text = $text.Replace($oldMember, $newMember)

        $insert = @'
  "equipment.question": "EQUIPMENT · {{name}}\n{{progression}}\nChoose an equipment slot.",
  "equipment.slot-line": "{{slot}}: {{item}}",
  "equipment.weapon": "Weapon",
  "equipment.armor": "Armor / Boots",
  "equipment.trinket": "Ring / Trinket",
  "equipment.empty": "Empty",
  "equipment.choose": "{{slot}}\nCurrent: {{current}}\nChoose a compatible item from your inventory.",
  "equipment.unequip": "Unequip",
  "equipment.equipped": "Equipment updated.",
  "equipment.unequipped": "Equipment removed.",
  "equipment.inventory-full": "Inventory space is unavailable or the item is no longer valid.",
  "equipment.leave-blocked": "Make room in your inventory to receive this NPC's equipment before they leave the party.",

'@
    }

    $needle = '  "common.back":'
    if (-not $text.Contains($needle)) {
        throw "alpha 3+4 i18n patch failed: common.back not found in $path"
    }

    $text = $text.Replace($needle, $insert + $needle)
    Set-Content -Path $path -Value $text -Encoding UTF8
}

Patch-I18n (Join-Path $root 'src\TeamUp\i18n\vi.json') $true
Patch-I18n (Join-Path $root 'src\TeamUp\i18n\default.json') $false
Write-Host 'v0.2-alpha.3+4 i18n patch applied.'
