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
    if (-not $text.Contains($old)) { throw "Alpha 6.3 polish could not locate $label." }
    return $text.Replace($old, $new)
}

# Controller/keyboard focus should show the same stat comparison card as physical mouse hover.
$equipmentPath = Join-Path $repoRoot 'src\TeamUp\UI\EquipmentMenu.cs'
$equipment = Normalize-Crlf ([System.IO.File]::ReadAllText($equipmentPath, [System.Text.Encoding]::UTF8))
$oldDraw = @'
        if (_hoveredItem is not null)
            DrawHoverComparison(b, _hoveredItem);

        drawMouse(b);
'@
$newDraw = @'
        Item? comparisonItem = _hoveredItem;
        if (comparisonItem is null
            && _focusInventory
            && _inventoryCursor >= 0
            && _inventoryCursor < Game1.player.Items.Count)
        {
            comparisonItem = Game1.player.Items[_inventoryCursor];
        }

        if (comparisonItem is not null)
            DrawHoverComparison(b, comparisonItem);

        drawMouse(b);
'@
$equipment = Replace-Required $equipment $oldDraw $newDraw 'controller stat-comparison draw hook'
[System.IO.File]::WriteAllText($equipmentPath, $equipment, $utf8NoBom)

# Avoid nullable implicit Translation conversion in the dialogue action ternary.
$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'
$mod = Normalize-Crlf ([System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8))
$oldHint = @'
        string leftText = Helper.Translation.Get("hint.profile");
        string? rightText = member is not null
            ? Helper.Translation.Get("hint.leave")
            : IsRecruitableNpc(speaker)
                ? Helper.Translation.Get("hint.recruit")
                : null;
'@
$newHint = @'
        string leftText = Helper.Translation.Get("hint.profile").ToString();
        string? rightText = member is not null
            ? Helper.Translation.Get("hint.leave").ToString()
            : IsRecruitableNpc(speaker)
                ? Helper.Translation.Get("hint.recruit").ToString()
                : null;
'@
$mod = Replace-Required $mod $oldHint $newHint 'nullable dialogue translation conversion'
[System.IO.File]::WriteAllText($modPath, $mod, $utf8NoBom)

$equipmentVerify = [System.IO.File]::ReadAllText($equipmentPath, [System.Text.Encoding]::UTF8)
$modVerify = [System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8)
$controllerCompareOk = $equipmentVerify.Contains('Item? comparisonItem = _hoveredItem;') -and $equipmentVerify.Contains('Game1.player.Items[_inventoryCursor]')
$translationOk = $modVerify.Contains('Helper.Translation.Get("hint.leave").ToString()') -and $modVerify.Contains('Helper.Translation.Get("hint.recruit").ToString()')
if (-not $controllerCompareOk) { throw 'Alpha 6.3 polish verification failed: controller comparison hook missing.' }
if (-not $translationOk) { throw 'Alpha 6.3 polish verification failed: explicit translation conversion missing.' }

Write-Host 'Alpha 6.3 polish applied: controller stat comparison + nullable translation warning fix.'
