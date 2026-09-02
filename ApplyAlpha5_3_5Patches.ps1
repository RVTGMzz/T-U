$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if ($text.Contains($old)) { return $text.Replace($old, $new) }
    throw "Alpha.5.3.5 patch failed: expected source block not found: $label"
}

function Replace-RegexRequired([string]$text, [string]$pattern, [string]$replacement, [string]$alreadyPattern, [string]$label) {
    $already = [regex]::new($alreadyPattern, [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($already.IsMatch($text)) { return $text }
    $regex = [regex]::new($pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($regex.IsMatch($text)) { return $regex.Replace($text, $replacement, 1) }
    throw "Alpha.5.3.5 patch failed: expected source pattern not found: $label"
}

$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw

$mod = Replace-Required $mod `
    'Team Up! v0.1.0-alpha.5.3.4 polish hotfix loaded.' `
    'Team Up! v0.1.0-alpha.5.3.5 stability hotfix loaded.' `
    'version log'

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
        {
            // The vanilla question-mark icon is redundant on Team Up's member command menu.
            dialogueBox.dialogueIcon = null;
        }
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
        // Team Up contextual menus deliberately pin the NPC name because vanilla's
        // currentSpeaker can be null or stale while a question dialogue is active.
        if (!string.IsNullOrWhiteSpace(RecruitHintNpcName))
        {
            NPC? pinned = Game1.getCharacterFromName(RecruitHintNpcName);
            if (pinned is not null)
                return pinned;
        }

        return Game1.currentSpeaker as NPC;
    }
'@
$mod = Replace-RegexRequired $mod `
    $oldResolvePattern `
    $newResolve `
    'Team Up contextual menus deliberately pin the NPC name' `
    'dialogue speaker priority'

Set-Content -Path $modPath -Value $mod -Encoding UTF8
Write-Host 'Alpha.5.3.5 integration patches applied.'
