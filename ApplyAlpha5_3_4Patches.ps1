$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($old)) {
        return $text.Replace($old, $new)
    }
    if ($text.Contains($new)) {
        return $text
    }
    throw "Alpha.5.3.4 patch failed: expected source block not found: $label"
}

function Replace-RegexRequired([string]$text, [string]$pattern, [string]$replacement, [string]$alreadyPattern, [string]$label) {
    $regex = [regex]::new($pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if ($regex.IsMatch($text)) {
        return $regex.Replace($text, $replacement, 1)
    }
    $alreadyRegex = [regex]::new($alreadyPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    if ($alreadyRegex.IsMatch($text)) {
        return $text
    }
    throw "Alpha.5.3.4 patch failed: expected source pattern not found: $label"
}

# ModEntry: compile-safe dialogue anchor, smoother follow tick, and fixed member root hints.
$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw
$mod = Replace-Required $mod 'Team Up! v0.1.0-alpha.5.3.3 dialogue hint anchor hotfix loaded.' 'Team Up! v0.1.0-alpha.5.3.4 polish hotfix loaded.' 'version log'
$mod = Replace-Required $mod 'if (!e.IsMultipleOf(4))' 'if (!e.IsMultipleOf(2))' 'follow update cadence'
$mod = Replace-RegexRequired $mod 'private void ShowMemberMenu\(NPC npc, PartyMemberData member\)\r?\n    \{\r?\n        RecruitHintNpcName = null;' "private void ShowMemberMenu(NPC npc, PartyMemberData member)`n    {`n        // Keep profile/leave shortcuts attached to the party-member root dialogue.`n        RecruitHintNpcName = npc.Name;" 'private void ShowMemberMenu\(NPC npc, PartyMemberData member\)[\s\S]*?RecruitHintNpcName = npc\.Name;' 'party member hints'
$mod = Replace-RegexRequired $mod 'private void ShowRoleMenu\(NPC npc, PartyMemberData member\)\r?\n    \{' "private void ShowRoleMenu(NPC npc, PartyMemberData member)`n    {`n        RecruitHintNpcName = null;" 'private void ShowRoleMenu\(NPC npc, PartyMemberData member\)\r?\n    \{\r?\n        RecruitHintNpcName = null;' 'role submenu hint cleanup'
$mod = Replace-RegexRequired $mod 'private void ShowEngagementMenu\(NPC npc, PartyMemberData member\)\r?\n    \{' "private void ShowEngagementMenu(NPC npc, PartyMemberData member)`n    {`n        RecruitHintNpcName = null;" 'private void ShowEngagementMenu\(NPC npc, PartyMemberData member\)\r?\n    \{\r?\n        RecruitHintNpcName = null;' 'engagement submenu hint cleanup'
$mod = Replace-RegexRequired $mod 'int dialogueLeft = dialogueBox\.x;\r?\n        int dialogueTop = dialogueBox\.y;' "int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);`n        // Vanilla character dialogue sits 64 px above the viewport bottom.`n        // Avoid DialogueBox.x/y here because those members are not exposed by every`n        // reference assembly used by the one-click build.`n        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);" 'int dialogueLeft = Math\.Max\(8, \(Game1\.uiViewport\.Width - dialogueBox\.width\) / 2\);[\s\S]*?dialogueBox\.height - 64\);' 'compile-safe dialogue anchor'
Set-Content -Path $modPath -Value $mod -Encoding UTF8

# Codex: use native controller routing so A is never translated into an old mouse click.
$codexPath = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$codex = Get-Content $codexPath -Raw
$updatePattern = '(?s)    public override void update\(GameTime time\)\r?\n    \{\r?\n        base\.update\(time\);\r?\n\r?\n        bool confirmDown = IsControllerConfirmDown\(\);.*?        _controllerConfirmWasDown = confirmDown;\r?\n    \}'
$updateReplacement = @'
    public override void update(GameTime time)
    {
        base.update(time);
    }

    public override bool areGamePadControlsImplemented()
    {
        // Without this, Stardew can translate controller A into a synthetic mouse click
        // at the stale cursor position, which opens a character row instead of the filter.
        return true;
    }
'@
$codex = Replace-RegexRequired $codex $updatePattern $updateReplacement 'public override bool areGamePadControlsImplemented\(\)' 'native controller routing'
$codex = Replace-RegexRequired $codex 'public override void receiveGamePadButton\(Buttons b\)\r?\n    \{\r?\n        _showMouseCursor = false;' "public override void receiveGamePadButton(Buttons b)`n    {`n        _showMouseCursor = false;`n        _lastMousePosition = new Point(Game1.getMouseX(), Game1.getMouseY());" 'public override void receiveGamePadButton\(Buttons b\)[\s\S]*?_showMouseCursor = false;\r?\n        _lastMousePosition = new Point\(Game1\.getMouseX\(\), Game1\.getMouseY\(\)\);' 'controller cursor hide'
$codex = Replace-RegexRequired $codex 'else if \(b == Buttons\.A\)\r?\n                return;' "else if (b == Buttons.A)`n            {`n                ApplyDropdownSelection();`n                return;`n            }" 'else if \(b == Buttons\.A\)\r?\n            \{\r?\n                ApplyDropdownSelection\(\);' 'dropdown controller confirm'
$codex = Replace-RegexRequired $codex 'else if \(b == Buttons\.A\)\r?\n            return;' "else if (b == Buttons.A)`n        {`n            ActivateFocus();`n            return;`n        }" 'else if \(b == Buttons\.A\)\r?\n        \{\r?\n            ActivateFocus\(\);' 'filter controller confirm'
Set-Content -Path $codexPath -Value $codex -Encoding UTF8

# Follow polish: update formation targets sooner while retaining native PathFindController collision/path rules.
$followPath = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$follow = Get-Content $followPath -Raw
$follow = Replace-Required $follow 'private const float StopDistanceTiles = 1.55f;' 'private const float StopDistanceTiles = 1.45f;' 'follow stop distance'
$follow = Replace-Required $follow 'private const float WarpDistanceTiles = 10f;' 'private const float WarpDistanceTiles = 11f;' 'follow warp distance'
$follow = Replace-Required $follow 'private const float RepathDistanceTiles = 1.35f;' 'private const float RepathDistanceTiles = 0.90f;' 'follow repath distance'
Set-Content -Path $followPath -Value $follow -Encoding UTF8

Write-Host 'Alpha.5.3.4 source patches applied.'
