$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if ($text.Contains($old)) { return $text.Replace($old, $new) }
    throw "Alpha.5.3.5 patch failed: expected source block not found: $label"
}

function Replace-RegexRequired([string]$text, [string]$pattern, [string]$replacement, [string]$alreadyPattern, [string]$label) {
    $flags = [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::Singleline
    $already = [regex]::new($alreadyPattern, $flags)
    if ($already.IsMatch($text)) { return $text }
    $regex = [regex]::new($pattern, $flags)
    if ($regex.IsMatch($text)) { return $regex.Replace($text, $replacement, 1) }
    throw "Alpha.5.3.5 patch failed: expected source pattern not found: $label"
}

# ModEntry carries forward the 5.3.4 dialogue/follow fixes and adds 5.3.5 member-menu stability.
$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw

$mod = Replace-Required $mod `
    'Team Up! v0.1.0-alpha.5.3.3 dialogue hint anchor hotfix loaded.' `
    'Team Up! v0.1.0-alpha.5.3.5 stability hotfix loaded.' `
    'version log'

$mod = Replace-Required $mod 'if (!e.IsMultipleOf(4))' 'if (!e.IsMultipleOf(2))' 'follow update cadence'

$mod = Replace-RegexRequired $mod `
    'private void ShowMemberMenu\(NPC npc, PartyMemberData member\)\s*\{\s*RecruitHintNpcName = null;' `
    "private void ShowMemberMenu(NPC npc, PartyMemberData member)`n    {`n        RecruitHintNpcName = npc.Name;" `
    'private void ShowMemberMenu\(NPC npc, PartyMemberData member\)[\s\S]*?RecruitHintNpcName = npc\.Name;' `
    'party member contextual hints'

$mod = Replace-RegexRequired $mod `
    'private void ShowRoleMenu\(NPC npc, PartyMemberData member\)\s*\{' `
    "private void ShowRoleMenu(NPC npc, PartyMemberData member)`n    {`n        RecruitHintNpcName = null;" `
    'private void ShowRoleMenu\(NPC npc, PartyMemberData member\)\s*\{\s*RecruitHintNpcName = null;' `
    'role submenu hint cleanup'

$mod = Replace-RegexRequired $mod `
    'private void ShowEngagementMenu\(NPC npc, PartyMemberData member\)\s*\{' `
    "private void ShowEngagementMenu(NPC npc, PartyMemberData member)`n    {`n        RecruitHintNpcName = null;" `
    'private void ShowEngagementMenu\(NPC npc, PartyMemberData member\)\s*\{\s*RecruitHintNpcName = null;' `
    'engagement submenu hint cleanup'

$mod = Replace-RegexRequired $mod `
    'int dialogueLeft = dialogueBox\.x;\s*int dialogueTop = dialogueBox\.y;' `
    "int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);`n        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);" `
    'int dialogueLeft = Math\.Max\(8, \(Game1\.uiViewport\.Width - dialogueBox\.width\) / 2\);\s*int dialogueTop = Math\.Max\(8, Game1\.uiViewport\.Height - dialogueBox\.height - 64\);' `
    'compile-safe dialogue anchor'

$mod = Replace-Required $mod `
    'helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;' `
    "helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;`n        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;" `
    'rendering event subscription'

$renderHandler = @'
    private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady
            || PartyActionConfirmationOpen
            || Game1.activeClickableMenu is not DialogueBox dialogueBox
            || !dialogueBox.isQuestion
            || string.IsNullOrWhiteSpace(RecruitHintNpcName))
        {
            return;
        }

        long recruiterId = Game1.player.UniqueMultiplayerID;
        if (Party.Get(RecruitHintNpcName, recruiterId) is not null)
            dialogueBox.dialogueIcon = null;
    }

'@
$mod = Replace-RegexRequired $mod `
    '    private void OnRenderedActiveMenu\(object\? sender, RenderedActiveMenuEventArgs e\)' `
    ($renderHandler + '    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)') `
    'private void OnRenderingActiveMenu\(object\? sender, RenderingActiveMenuEventArgs e\)' `
    'member question icon suppression'

$oldResolvePattern = '    private NPC\? ResolveDialogueSpeaker\(\)\s*\{\s*if \(Game1\.currentSpeaker is NPC currentSpeaker\)\s*return currentSpeaker;\s*if \(string\.IsNullOrWhiteSpace\(RecruitHintNpcName\)\)\s*return null;\s*return Game1\.getCharacterFromName\(RecruitHintNpcName\);\s*\}'
$newResolve = @'
    private NPC? ResolveDialogueSpeaker()
    {
        if (!string.IsNullOrWhiteSpace(RecruitHintNpcName))
        {
            NPC? pinned = Game1.getCharacterFromName(RecruitHintNpcName);
            if (pinned is not null)
                return pinned;
        }

        return Game1.currentSpeaker as NPC;
    }
'@
$mod = Replace-RegexRequired $mod $oldResolvePattern $newResolve 'NPC\? pinned = Game1\.getCharacterFromName\(RecruitHintNpcName\);' 'dialogue speaker priority'

Set-Content -Path $modPath -Value $mod -Encoding UTF8

# Follow carries forward the smoother alpha.5.3.4 tuning.
$followPath = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$follow = Get-Content $followPath -Raw
$follow = Replace-Required $follow 'private const float StopDistanceTiles = 1.55f;' 'private const float StopDistanceTiles = 1.45f;' 'follow stop distance'
$follow = Replace-Required $follow 'private const float WarpDistanceTiles = 10f;' 'private const float WarpDistanceTiles = 11f;' 'follow warp distance'
$follow = Replace-Required $follow 'private const float RepathDistanceTiles = 1.35f;' 'private const float RepathDistanceTiles = 0.90f;' 'follow repath distance'
Set-Content -Path $followPath -Value $follow -Encoding UTF8

Write-Host 'Alpha.5.3.5 integration patches applied.'
