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
    if (-not $text.Contains($old)) { throw "Alpha 6.1.2 UX hotfix could not locate $label." }
    return $text.Replace($old, $new)
}

function Replace-Method([string]$text, [string]$methodName, [string]$nextMethodName, [string]$replacement) {
    $method = [regex]::Escape($methodName)
    $next = [regex]::Escape($nextMethodName)
    $pattern = "(?ms)^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$method\([^\r\n]*\)\s*\{.*?(?=^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$next\()"
    if (-not [regex]::IsMatch($text, $pattern)) {
        throw "Alpha 6.1.2 UX hotfix could not locate method $methodName before $nextMethodName."
    }
    return [regex]::Replace($text, $pattern, $replacement + "`r`n`r`n", 1)
}

# 1) Recruit takeover: stop vanilla schedule/end-of-route animation before Team Up owns movement.
$followPath = Join-Path $repoRoot 'src\TeamUp\Following\FollowService.cs'
$follow = Read-Utf8 $followPath
if (-not $follow.Contains('private static void ResetToStandingPose(NPC npc)')) {
$takePartyControl = @'
    public void TakePartyControl(NPC npc)
    {
        _releasedCharacters.Remove(npc.Name);
        _combatControlled.Remove(npc.Name);
        PrepareForParty(npc);
        ClearPath(npc);
        ResetToStandingPose(npc);
    }

    private static void ResetToStandingPose(NPC npc)
    {
        int facing = Math.Clamp(npc.FacingDirection, 0, 3);
        npc.doingEndOfRouteAnimation.Value = false;
        npc.nextEndOfRouteMessage = null;
        npc.endOfRouteMessage.Value = null;
        npc.Halt();
        npc.Sprite.StopAnimation();
        npc.faceDirection(facing);
    }
'@
    $follow = Replace-Method $follow 'TakePartyControl' 'SetCombatControl' $takePartyControl
}
Write-Utf8 $followPath $follow

# 2) Codex: preserve the exact browser instance when returning from a profile.
$codexPath = Join-Path $repoRoot 'src\TeamUp\UI\CodexBrowserMenu.cs'
$codex = Read-Utf8 $codexPath
$codex = Replace-Required $codex `
    '    private readonly Action<string> _openProfile;' `
    '    private readonly Action<string, CodexBrowserMenu> _openProfile;' `
    'Codex profile callback field'
$codex = Replace-Required $codex `
    '        Action<string> openProfile,' `
    '        Action<string, CodexBrowserMenu> openProfile,' `
    'Codex profile callback constructor'
$codex = Replace-Required $codex `
    '        _openProfile(filtered[_selectedIndex].CharacterName);' `
    '        _openProfile(filtered[_selectedIndex].CharacterName, this);' `
    'Codex selected browser preservation'
Write-Utf8 $codexPath $codex

$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'
$mod = Read-Utf8 $modPath
$mod = Replace-Required $mod `
    '            characterName => OpenCharacterProfile(characterName, OpenCodexBrowser, OpenCodexBrowser),' `
    '            (characterName, browser) => OpenCharacterProfile(characterName, () => Game1.activeClickableMenu = browser, () => Game1.activeClickableMenu = browser),' `
    'Codex browser restore callback'

# Restore the dialogue hint pin by replacing the whole method rather than matching an exact source block.
$dialogueProfile = @'
    private void OpenProfileFromDialogue(NPC npc)
    {
        IClickableMenu? dialogueMenu = Game1.activeClickableMenu;
        OpenCharacterProfile(
            npc.Name,
            () =>
            {
                RecruitHintNpcName = npc.Name;
                RestoreMenu(dialogueMenu);
            },
            () => OpenCodexBrowser(() =>
            {
                RecruitHintNpcName = npc.Name;
                RestoreMenu(dialogueMenu);
            }));
    }
'@
$mod = Replace-Method $mod 'OpenProfileFromDialogue' 'OpenCharacterProfile' $dialogueProfile
Write-Utf8 $modPath $mod

# 3) Repair equipment Vietnamese with ASCII-only JSON unicode escapes for Windows PowerShell 5.1.
$viPath = Join-Path $repoRoot 'src\TeamUp\i18n\vi.json'
$vi = Read-Utf8 $viPath

function Set-JsonString([string]$text, [string]$key, [string]$asciiJsonValue) {
    $pattern = '(?m)("' + [regex]::Escape($key) + '"\s*:\s*)"(?:\\.|[^"\\])*"'
    if (-not [regex]::IsMatch($text, $pattern)) {
        throw "Alpha 6.1.2 UX hotfix could not locate i18n key $key."
    }
    $replacement = '$1"' + $asciiJsonValue + '"'
    return [regex]::Replace($text, $pattern, $replacement, 1)
}

$vi = Set-JsonString $vi 'member.equipment' 'Trang b\u1ecb'
$vi = Set-JsonString $vi 'equipment.question' 'TRANG B\u1eca \u00b7 {{name}}\n{{progression}}\nCh\u1ecdn \u00f4 trang b\u1ecb.'
$vi = Set-JsonString $vi 'equipment.weapon' 'V\u0169 kh\u00ed'
$vi = Set-JsonString $vi 'equipment.armor' 'Gi\u00e1p / Gi\u00e0y'
$vi = Set-JsonString $vi 'equipment.trinket' 'Nh\u1eabn / Trinket'
$vi = Set-JsonString $vi 'equipment.empty' 'Ch\u01b0a trang b\u1ecb'
$vi = Set-JsonString $vi 'equipment.choose' '{{slot}}\nHi\u1ec7n t\u1ea1i: {{current}}\nCh\u1ecdn \u0111\u1ed3 ph\u00f9 h\u1ee3p trong t\u00fai.'
$vi = Set-JsonString $vi 'equipment.unequip' 'Th\u00e1o trang b\u1ecb'
$vi = Set-JsonString $vi 'equipment.equipped' '\u0110\u00e3 trang b\u1ecb.'
$vi = Set-JsonString $vi 'equipment.unequipped' '\u0110\u00e3 th\u00e1o trang b\u1ecb.'
$vi = Set-JsonString $vi 'equipment.inventory-full' 'T\u00fai \u0111\u1ed3 kh\u00f4ng \u0111\u1ee7 ch\u1ed7 ho\u1eb7c m\u00f3n \u0111\u1ed3 kh\u00f4ng c\u00f2n h\u1ee3p l\u1ec7.'
$vi = Set-JsonString $vi 'equipment.leave-blocked' 'H\u00e3y ch\u1eeba ch\u1ed7 trong t\u00fai \u0111\u1ec3 nh\u1eadn l\u1ea1i trang b\u1ecb tr\u01b0\u1edbc khi NPC r\u1eddi \u0111\u1ed9i.'
Write-Utf8 $viPath $vi

Write-Host 'Alpha 6.1.2 UX hotfix applied: recruit pose reset + Codex focus memory + dialogue hint restore + Vietnamese equipment encoding.'
