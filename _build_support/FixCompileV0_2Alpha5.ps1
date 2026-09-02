$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'

if (-not (Test-Path $modPath)) {
    throw "Compile fixer could not find ModEntry.cs: $modPath"
}

$text = [System.IO.File]::ReadAllText($modPath)

$replacements = @(
    @('Equipment.TryReturnAll(leavingMember, out _)', 'Equipment.TryReturnAll(leavingMember, out string _returnMessage)'),
    @('Equipment.TryUnequip(member, slot, out _)', 'Equipment.TryUnequip(member, slot, out string _unequipMessage)'),
    @('Equipment.TryEquip(member, slot, inventoryIndex, out _)', 'Equipment.TryEquip(member, slot, inventoryIndex, out string _equipMessage)'),
    @('string empty = Helper.Translation.Get("equipment.empty");', 'string empty = Helper.Translation.Get("equipment.empty").ToString();'),
    @('string current = equipped?.DisplayName ?? Helper.Translation.Get("equipment.empty");', 'string current = equipped?.DisplayName ?? Helper.Translation.Get("equipment.empty").ToString();')
)

foreach ($pair in $replacements) {
    $old = $pair[0]
    $new = $pair[1]
    if ($text.Contains($old)) {
        $text = $text.Replace($old, $new)
    }
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($modPath, $text, $utf8NoBom)
Write-Host 'Alpha 3+4 compile compatibility fixes applied.'
