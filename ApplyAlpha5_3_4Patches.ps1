$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if (-not $text.Contains($old)) {
        throw "Alpha.5.3.4 patch failed: expected source block not found: $label"
    }
    return $text.Replace($old, $new)
}

# ModEntry: compile-safe dialogue anchor, smoother follow tick, and fixed member root hints.
$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw
$mod = Replace-Required $mod 'Team Up! v0.1.0-alpha.5.3.3 dialogue hint anchor hotfix loaded.' 'Team Up! v0.1.0-alpha.5.3.4 polish hotfix loaded.' 'version log'
$mod = Replace-Required $mod 'if (!e.IsMultipleOf(4))' 'if (!e.IsMultipleOf(2))' 'follow update cadence'
$mod = Replace-Required $mod "private void ShowMemberMenu(NPC npc, PartyMemberData member)`r`n    {`r`n        RecruitHintNpcName = null;" "private void ShowMemberMenu(NPC npc, PartyMemberData member)`r`n    {`r`n        // Keep the contextual profile/leave hints attached to the party-member root menu.`r`n        RecruitHintNpcName = npc.Name;" 'party member hints'
$mod = Replace-Required $mod "private void ShowRoleMenu(NPC npc, PartyMemberData member)`r`n    {" "private void ShowRoleMenu(NPC npc, PartyMemberData member)`r`n    {`r`n        RecruitHintNpcName = null;" 'role submenu hint cleanup'
$mod = Replace-Required $mod "private void ShowEngagementMenu(NPC npc, PartyMemberData member)`r`n    {" "private void ShowEngagementMenu(NPC npc, PartyMemberData member)`r`n    {`r`n        RecruitHintNpcName = null;" 'engagement submenu hint cleanup'
$mod = Replace-Required $mod "int dialogueLeft = dialogueBox.x;`r`n        int dialogueTop = dialogueBox.y;" "int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);`r`n        // Vanilla character DialogueBox is anchored 64 px above the bottom viewport edge.`r`n        // Use the public width/height values instead of DialogueBox.x/y so this compiles`r`n        // against the SMAPI/Stardew reference assemblies used by the one-click build.`r`n        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);" 'compile-safe dialogue anchor'
Set-Content -Path $modPath -Value $mod -Encoding UTF8

# Codex: make controller input native instead of letting Stardew emulate A as a mouse click.
$codexPath = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$codex = Get-Content $codexPath -Raw
$oldUpdate = @'
    public override void update(GameTime time)
    {
        base.update(time);

        bool confirmDown = IsControllerConfirmDown();
        if (confirmDown && !_controllerConfirmWasDown)
        {
            _showMouseCursor = false;
            if (_openDropdown != DropdownKind.None)
                ApplyDropdownSelection();
            else
                ActivateFocus();
        }

        _controllerConfirmWasDown = confirmDown;
    }
'@
$newUpdate = @'
    public override void update(GameTime time)
    {
        base.update(time);
    }

    public override bool areGamePadControlsImplemented()
    {
        // Prevent Stardew from translating controller A into a synthetic mouse click at
        // the old cursor position. Team Up handles the controller explicitly below.
        return true;
    }
'@
$codex = Replace-Required $codex $oldUpdate $newUpdate 'native controller routing'
$codex = Replace-Required $codex "    public override void receiveGamePadButton(Buttons b)`r`n    {`r`n        _showMouseCursor = false;" "    public override void receiveGamePadButton(Buttons b)`r`n    {`r`n        _showMouseCursor = false;`r`n        _lastMousePosition = new Point(Game1.getMouseX(), Game1.getMouseY());" 'controller cursor hide'
$codex = Replace-Required $codex "            else if (b == Buttons.A)`r`n                return;" "            else if (b == Buttons.A)`r`n            {`r`n                ApplyDropdownSelection();`r`n                return;`r`n            }" 'dropdown controller confirm'
$codex = Replace-Required $codex "        else if (b == Buttons.A)`r`n            return;" "        else if (b == Buttons.A)`r`n        {`r`n            ActivateFocus();`r`n            return;`r`n        }" 'filter controller confirm'
Set-Content -Path $codexPath -Value $codex -Encoding UTF8

# Follow polish: react sooner to formation target changes while keeping native pathfinding.
$followPath = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$follow = Get-Content $followPath -Raw
$follow = Replace-Required $follow 'private const float StopDistanceTiles = 1.55f;' 'private const float StopDistanceTiles = 1.45f;' 'follow stop distance'
$follow = Replace-Required $follow 'private const float WarpDistanceTiles = 10f;' 'private const float WarpDistanceTiles = 11f;' 'follow warp distance'
$follow = Replace-Required $follow 'private const float RepathDistanceTiles = 1.35f;' 'private const float RepathDistanceTiles = 0.90f;' 'follow repath distance'
Set-Content -Path $followPath -Value $follow -Encoding UTF8

Write-Host 'Alpha.5.3.4 source patches applied.'
