$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Normalize-Crlf([string]$text) {
    return $text.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")
}

function Read-Utf8([string]$path) {
    return Normalize-Crlf ([System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8))
}

function Write-Utf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Alpha 6.1.3 fix could not locate $label." }
    return $text.Replace($old, $new)
}

function Replace-Method([string]$text, [string]$methodName, [string]$nextMethodName, [string]$replacement) {
    $method = [regex]::Escape($methodName)
    $next = [regex]::Escape($nextMethodName)
    $pattern = "(?ms)^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$method\([^\r\n]*\)\s*\{.*?(?=^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$next\()"
    if (-not [regex]::IsMatch($text, $pattern)) {
        throw "Alpha 6.1.3 fix could not locate method $methodName before $nextMethodName."
    }
    return [regex]::Replace($text, $pattern, $replacement + "`r`n`r`n", 1)
}

function Set-JsonString([string]$text, [string]$key, [string]$asciiJsonValue) {
    $pattern = '(?m)("' + [regex]::Escape($key) + '"\s*:\s*)"(?:\\.|[^"\\])*"'
    if (-not [regex]::IsMatch($text, $pattern)) {
        throw "Alpha 6.1.3 i18n fix could not locate key $key."
    }
    return [regex]::Replace($text, $pattern, ('$1"' + $asciiJsonValue + '"'), 1)
}

function Ensure-JsonString([string]$text, [string]$key, [string]$asciiJsonValue) {
    $pattern = '(?m)"' + [regex]::Escape($key) + '"\s*:'
    if ([regex]::IsMatch($text, $pattern)) {
        return Set-JsonString $text $key $asciiJsonValue
    }

    $needle = '  "common.back":'
    $index = $text.IndexOf($needle, [StringComparison]::Ordinal)
    if ($index -lt 0) { throw "Alpha 6.1.3 i18n fix could not find common.back insertion point for $key." }
    $entry = '  "' + $key + '": "' + $asciiJsonValue + '",' + "`r`n"
    return $text.Insert($index, $entry)
}

# -----------------------------------------------------------------------------
# 1) ChaCha hard gate at party-manager + classification layers.
# -----------------------------------------------------------------------------
$partyPath = Join-Path $repoRoot 'src\TeamUp\Core\PartyManager.cs'
$party = Read-Utf8 $partyPath
$party = Replace-Required $party `
    "        if (string.IsNullOrWhiteSpace(characterName))`r`n            return PartyAddResult.InvalidCharacter;" `
    "        if (string.IsNullOrWhiteSpace(characterName))`r`n            return PartyAddResult.InvalidCharacter;`r`n`r`n        if (CompanionClassificationService.IsSpecialName(characterName, null))`r`n            return PartyAddResult.InvalidCharacter;" `
    'PartyManager special-companion gate'
Write-Utf8 $partyPath $party

$classPath = Join-Path $repoRoot 'src\TeamUp\Core\CompanionClassificationService.cs'
$class = Read-Utf8 $classPath
if (-not $class.Contains('"Ronvotri.Cardcha_ChaCha"')) {
$oldSpecial = @'
        "ChaCha"
'@
$newSpecial = @'
        "ChaCha",
        "Ronvotri.Cardcha_ChaCha"
'@
    $class = Replace-Required $class $oldSpecial.TrimEnd() $newSpecial.TrimEnd() 'Cardcha ChaCha internal identity'
}
if (-not $class.Contains('IsSpecialName(npc.displayName, specialNpcNames)')) {
    $class = Replace-Required $class `
        '        if (IsSpecialName(npc.Name, specialNpcNames))' `
        "        if (IsSpecialName(npc.Name, specialNpcNames)`r`n            || IsSpecialName(npc.displayName, specialNpcNames))" `
        'ChaCha display-name fallback'
}
Write-Utf8 $classPath $class

# -----------------------------------------------------------------------------
# 2) Dedicated equipment panel instead of dialogue-question equipment flow.
# -----------------------------------------------------------------------------
$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'
$mod = Read-Utf8 $modPath
$equipmentMenuMethod = @'
    private void ShowEquipmentMenu(NPC npc, PartyMemberData member)
    {
        RecruitHintNpcName = null;
        Game1.activeClickableMenu = new EquipmentMenu(
            npc,
            member,
            Equipment,
            Progression,
            Helper.Translation,
            SavePartyNow,
            () => ShowMemberMenu(npc, member));
    }
'@
$mod = Replace-Method $mod 'ShowEquipmentMenu' 'ShowEquipmentSlotMenu' $equipmentMenuMethod
Write-Utf8 $modPath $mod

# -----------------------------------------------------------------------------
# 3) Tank must approach pressure before taunting. Taunt only fires around the Tank,
#    then normal combat movement continues toward the chosen threat target.
# -----------------------------------------------------------------------------
$combatPath = Join-Path $repoRoot 'src\TeamUp\Combat\CombatService.cs'
$combat = Read-Utf8 $combatPath
$oldPreTaunt = "            if (role == PartyRole.Tank)`r`n                TryTankTaunt(npc, member, monsters, validThreatActors);`r`n`r`n            Monster? target = AcquireTarget"
if ($combat.Contains($oldPreTaunt)) {
    $combat = $combat.Replace($oldPreTaunt, '            Monster? target = AcquireTarget')
}
elseif (-not $combat.Contains("            float distanceToTarget = Vector2.Distance(npc.Tile, target.Tile);`r`n`r`n            if (role == PartyRole.Tank)")) {
    throw 'Alpha 6.1.3 fix could not locate the old or new Tank taunt ordering.'
}

$combat = Replace-Required $combat `
    "            float attackRange = GetAttackRange(role);`r`n            float distanceToTarget = Vector2.Distance(npc.Tile, target.Tile);" `
    "            float attackRange = GetAttackRange(role);`r`n            float distanceToTarget = Vector2.Distance(npc.Tile, target.Tile);`r`n`r`n            if (role == PartyRole.Tank)`r`n                TryTankTaunt(npc, member, monsters, validThreatActors);" `
    'post-target tank taunt hook'
$combat = Replace-Required $combat `
    "            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 6f`r`n                || Vector2.Distance(monster.Tile, Game1.player.Tile) <= 5f)" `
    '            .Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 5f)' `
    'tank-local taunt radius'
Write-Utf8 $combatPath $combat

$alpha6Path = Join-Path $repoRoot 'src\TeamUp\Combat\Alpha6CombatPolishService.cs'
$alpha6 = Read-Utf8 $alpha6Path
$alpha6 = Replace-Required $alpha6 `
    '        List<Monster> nearby = monsters.Where(monster => Vector2.Distance(monster.Tile, Game1.player.Tile) <= 3.75f).Take(6).ToList();' `
    '        List<Monster> nearby = monsters.Where(monster => Vector2.Distance(monster.Tile, npc.Tile) <= 4.5f).Take(6).ToList();' `
    'Alex signature local pressure radius'
Write-Utf8 $alpha6Path $alpha6

# -----------------------------------------------------------------------------
# 4) Repair all Vault + Equipment Vietnamese strings using ASCII-only JSON escapes,
#    so Windows PowerShell 5.1 cannot mojibake them while materializing the build.
# -----------------------------------------------------------------------------
$viPath = Join-Path $repoRoot 'src\TeamUp\i18n\vi.json'
$vi = Read-Utf8 $viPath
$vi = Set-JsonString $vi 'member.vault' 'Kho chung Party'
$vi = Set-JsonString $vi 'vault.title' 'KHO PARTY'
$vi = Set-JsonString $vi 'vault.subtitle' 'V\u1eadt t\u01b0 d\u00f9ng chung cho to\u00e0n \u0111\u1ed9i'
$vi = Set-JsonString $vi 'vault.slots' '\u00f4 \u0111\u00e3 d\u00f9ng'
$vi = Set-JsonString $vi 'vault.categories' 'Th\u1ee9c \u0103n \u00b7 H\u1ed3i ph\u1ee5c \u00b7 Ti\u1ec7n \u00edch \u00b7 Chi\u1ebfn l\u1ee3i ph\u1ea9m'
$vi = Set-JsonString $vi 'member.equipment' 'Trang b\u1ecb'
$vi = Set-JsonString $vi 'equipment.weapon' 'V\u0169 kh\u00ed'
$vi = Set-JsonString $vi 'equipment.armor' 'Gi\u00e1p / Gi\u00e0y'
$vi = Set-JsonString $vi 'equipment.trinket' 'Nh\u1eabn / Trinket'
$vi = Set-JsonString $vi 'equipment.empty' 'Ch\u01b0a trang b\u1ecb'
$vi = Set-JsonString $vi 'equipment.inventory-full' 'T\u00fai \u0111\u1ed3 kh\u00f4ng \u0111\u1ee7 ch\u1ed7 ho\u1eb7c m\u00f3n \u0111\u1ed3 kh\u00f4ng c\u00f2n h\u1ee3p l\u1ec7.'
$vi = Ensure-JsonString $vi 'equipment.panel-title' 'TRANG B\u1eca \u00b7 {{name}}'
$vi = Ensure-JsonString $vi 'equipment.panel-subtitle' 'Ch\u1ecdn slot b\u00ean tr\u00e1i, sau \u0111\u00f3 ch\u1ecdn \u0111\u1ed3 ph\u00f9 h\u1ee3p trong t\u00fai.'
$vi = Ensure-JsonString $vi 'equipment.current' '\u0110ANG TRANG B\u1eca'
$vi = Ensure-JsonString $vi 'equipment.bag' '\u0110\u1ed2 PH\u00d9 H\u1ee2P TRONG T\u00daI'
$vi = Ensure-JsonString $vi 'equipment.bag-empty' 'Kh\u00f4ng c\u00f3 m\u00f3n \u0111\u1ed3 ph\u00f9 h\u1ee3p cho slot n\u00e0y.'
$vi = Ensure-JsonString $vi 'equipment.unequip-button' 'X / Th\u00e1o trang b\u1ecb'
$vi = Ensure-JsonString $vi 'equipment.hint' 'D-pad tr\u00e1i/ph\u1ea3i: Slot / T\u00fai   \u00b7   A: Trang b\u1ecb   \u00b7   X: Th\u00e1o   \u00b7   B: Quay l\u1ea1i'
$vi = Ensure-JsonString $vi 'equipment.equipped-name' '\u0110\u00e3 trang b\u1ecb {{item}}.'
$vi = Ensure-JsonString $vi 'equipment.unequipped-name' '\u0110\u00e3 th\u00e1o {{item}} v\u1ec1 t\u00fai.'
Write-Utf8 $viPath $vi

$defaultPath = Join-Path $repoRoot 'src\TeamUp\i18n\default.json'
$en = Read-Utf8 $defaultPath
$en = Ensure-JsonString $en 'equipment.panel-title' 'EQUIPMENT \u00b7 {{name}}'
$en = Ensure-JsonString $en 'equipment.panel-subtitle' 'Choose a slot on the left, then choose eligible gear from your inventory.'
$en = Ensure-JsonString $en 'equipment.current' 'EQUIPPED'
$en = Ensure-JsonString $en 'equipment.bag' 'ELIGIBLE GEAR IN BACKPACK'
$en = Ensure-JsonString $en 'equipment.bag-empty' 'No eligible item is available for this slot.'
$en = Ensure-JsonString $en 'equipment.unequip-button' 'X / Unequip'
$en = Ensure-JsonString $en 'equipment.hint' 'D-pad left/right: Slot / Bag   \u00b7   A: Equip   \u00b7   X: Unequip   \u00b7   B: Back'
$en = Ensure-JsonString $en 'equipment.equipped-name' 'Equipped {{item}}.'
$en = Ensure-JsonString $en 'equipment.unequipped-name' 'Returned {{item}} to your backpack.'
Write-Utf8 $defaultPath $en

Write-Host 'Alpha 6.1.3 applied: ChaCha hard exclusion + dedicated equipment panel + Vault UTF-8 repair + Tank approach-before-taunt behavior.'
