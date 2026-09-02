$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if ($text.Contains($old)) { return $text.Replace($old, $new) }
    throw "Alpha.5.3.6 patch failed: expected source block not found: $label"
}

function Replace-RegexRequired([string]$text, [string]$pattern, [string]$replacement, [string]$alreadyPattern, [string]$label) {
    $options = [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::Singleline
    $already = [regex]::new($alreadyPattern, $options)
    if ($already.IsMatch($text)) { return $text }
    $regex = [regex]::new($pattern, $options)
    if (-not $regex.IsMatch($text)) {
        throw "Alpha.5.3.6 patch failed: expected source pattern not found: $label"
    }
    return $regex.Replace($text, $replacement, 1)
}

# -----------------------------------------------------------------------------
# ModEntry integration
# -----------------------------------------------------------------------------
$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw

$mod = Replace-Required $mod `
    'Team Up! v0.1.0-alpha.5.3.3 dialogue hint anchor hotfix loaded.' `
    'Team Up! v0.1.0-alpha.5.3.6 vault + member hint hotfix loaded.' `
    'version log'

$mod = Replace-Required $mod `
    'helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;' `
    "helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;`r`n        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;" `
    'rendering event subscription'

$mod = Replace-Required $mod `
    'if (!e.IsMultipleOf(4))' `
    'if (!e.IsMultipleOf(2))' `
    'follow update cadence'

$mod = Replace-RegexRequired $mod `
    'private void ShowMemberMenu\(NPC npc, PartyMemberData member\)\s*\{\s*RecruitHintNpcName = null;' `
    "private void ShowMemberMenu(NPC npc, PartyMemberData member)`r`n    {`r`n        RecruitHintNpcName = npc.Name;" `
    'private void ShowMemberMenu\(NPC npc, PartyMemberData member\)\s*\{\s*RecruitHintNpcName = npc\.Name;' `
    'member root hint pinning'

$mod = Replace-RegexRequired $mod `
    'private void ShowRoleMenu\(NPC npc, PartyMemberData member\)\s*\{' `
    "private void ShowRoleMenu(NPC npc, PartyMemberData member)`r`n    {`r`n        RecruitHintNpcName = null;" `
    'private void ShowRoleMenu\(NPC npc, PartyMemberData member\)\s*\{\s*RecruitHintNpcName = null;' `
    'role submenu hint cleanup'

$mod = Replace-RegexRequired $mod `
    'private void ShowEngagementMenu\(NPC npc, PartyMemberData member\)\s*\{' `
    "private void ShowEngagementMenu(NPC npc, PartyMemberData member)`r`n    {`r`n        RecruitHintNpcName = null;" `
    'private void ShowEngagementMenu\(NPC npc, PartyMemberData member\)\s*\{\s*RecruitHintNpcName = null;' `
    'engagement submenu hint cleanup'

$mod = Replace-RegexRequired $mod `
    'private void ShowLeaveQuestion\(NPC npc\)\s*\{\s*PartyActionConfirmationOpen = true;' `
    "private void ShowLeaveQuestion(NPC npc)`r`n    {`r`n        RecruitHintNpcName = npc.Name;`r`n        PartyActionConfirmationOpen = true;" `
    'private void ShowLeaveQuestion\(NPC npc\)\s*\{\s*RecruitHintNpcName = npc\.Name;\s*PartyActionConfirmationOpen = true;' `
    'leave confirmation pinning'

$leavePattern = 'long recruiterId = Game1\.player\.UniqueMultiplayerID;\s*Follow\.ReleaseToVanilla\(npc\);\s*if \(Party\.Remove\(npc\.Name, recruiterId\)\)\s*\{\s*SavePartyNow\(\);\s*ShowHud\(Helper\.Translation\.Get\("member\.left", new \{ name = npc\.displayName \}\)\);\s*\}'
$leaveReplacement = @'
long recruiterId = Game1.player.UniqueMultiplayerID;
            CompanionUnitData? linkedUnit = Party.GetLinkedCompanion(npc.Name, recruiterId);
            NPC? linkedNpc = linkedUnit is null
                ? null
                : Game1.getCharacterFromName(linkedUnit.CharacterName);

            // Remove roster ownership first so the next update tick cannot reacquire
            // Team Up movement control after we release the NPC.
            bool removed = Party.Remove(npc.Name, recruiterId);
            Follow.ReleaseToVanilla(npc);
            if (linkedNpc is not null)
                Follow.ReleaseToVanilla(linkedNpc);

            RecruitHintNpcName = null;
            if (removed)
            {
                SavePartyNow();
                ShowHud(Helper.Translation.Get("member.left", new { name = npc.displayName }));
            }
'@
$mod = Replace-RegexRequired $mod `
    $leavePattern `
    $leaveReplacement `
    'bool removed = Party\.Remove\(npc\.Name, recruiterId\);\s*Follow\.ReleaseToVanilla\(npc\);' `
    'leave release ordering'

$mod = Replace-RegexRequired $mod `
    'int dialogueLeft = dialogueBox\.x;\s*int dialogueTop = dialogueBox\.y;' `
    "int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);`r`n        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);" `
    'int dialogueLeft = Math\.Max\(8, \(Game1\.uiViewport\.Width - dialogueBox\.width\) / 2\);\s*int dialogueTop = Math\.Max\(8, Game1\.uiViewport\.Height - dialogueBox\.height - 64\);' `
    'compile-safe dialogue hint anchor'

$resolvePattern = 'private NPC\? ResolveDialogueSpeaker\(\)\s*\{\s*if \(Game1\.currentSpeaker is NPC currentSpeaker\)\s*return currentSpeaker;\s*if \(string\.IsNullOrWhiteSpace\(RecruitHintNpcName\)\)\s*return null;\s*return Game1\.getCharacterFromName\(RecruitHintNpcName\);\s*\}'
$resolveReplacement = @'
private NPC? ResolveDialogueSpeaker()
    {
        // Team Up root menus pin the NPC explicitly. Vanilla currentSpeaker can be
        // null/stale while createQuestionDialogue is active.
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
    $resolvePattern `
    $resolveReplacement `
    'Team Up root menus pin the NPC explicitly' `
    'dialogue speaker priority'

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
            // The vanilla question-mark icon adds no information to Team Up's
            // member command menu, so suppress it only while that root context is active.
            dialogueBox.dialogueIcon = null;
        }
    }

'@
$mod = Replace-RegexRequired $mod `
    '    private void OnRenderedActiveMenu\(object\? sender, RenderedActiveMenuEventArgs e\)' `
    ($renderHandler + '    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)') `
    'private void OnRenderingActiveMenu\(object\? sender, RenderingActiveMenuEventArgs e\)' `
    'member question icon suppression'

Set-Content -Path $modPath -Value $mod -Encoding UTF8

# -----------------------------------------------------------------------------
# Follow integration and ghost-follow guard
# -----------------------------------------------------------------------------
$followPath = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$follow = Get-Content $followPath -Raw

$follow = Replace-Required $follow 'private const float StopDistanceTiles = 1.55f;' 'private const float StopDistanceTiles = 1.45f;' 'follow stop distance'
$follow = Replace-Required $follow 'private const float WarpDistanceTiles = 10f;' 'private const float WarpDistanceTiles = 11f;' 'follow warp distance'
$follow = Replace-Required $follow 'private const float RepathDistanceTiles = 1.35f;' 'private const float RepathDistanceTiles = 0.90f;' 'follow repath distance'

$follow = Replace-Required $follow `
    'private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();' `
    "private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();`r`n    private readonly HashSet<string> _releasedCharacters = new(StringComparer.OrdinalIgnoreCase);" `
    'released character guard field'

$follow = Replace-RegexRequired $follow `
    'public void TakePartyControl\(NPC npc\)\s*\{\s*PrepareForParty\(npc\);' `
    "public void TakePartyControl(NPC npc)`r`n    {`r`n        _releasedCharacters.Remove(npc.Name);`r`n        PrepareForParty(npc);" `
    'public void TakePartyControl\(NPC npc\)\s*\{\s*_releasedCharacters\.Remove\(npc\.Name\);' `
    'recruit clears release guard'

$follow = Replace-RegexRequired $follow `
    'public void ReleaseToVanilla\(NPC npc\)\s*\{\s*ClearPath\(npc\);' `
    "public void ReleaseToVanilla(NPC npc)`r`n    {`r`n        _releasedCharacters.Add(npc.Name);`r`n        ClearPath(npc);" `
    'public void ReleaseToVanilla\(NPC npc\)\s*\{\s*_releasedCharacters\.Add\(npc\.Name\);' `
    'release guard activation'

$follow = Replace-RegexRequired $follow `
    'PartyMemberData member = ownedMembers\[index\];\s*NPC\? npc = ResolveCharacter\(member\.CharacterName\);' `
    "PartyMemberData member = ownedMembers[index];`r`n            if (_releasedCharacters.Contains(member.CharacterName))`r`n                continue;`r`n`r`n            NPC? npc = ResolveCharacter(member.CharacterName);" `
    'PartyMemberData member = ownedMembers\[index\];\s*if \(_releasedCharacters\.Contains\(member\.CharacterName\)\)' `
    'party member ghost-follow guard'

$follow = Replace-RegexRequired $follow `
    'foreach \(CompanionUnitData unit in activeUnits\)\s*\{\s*NPC\? npc = ResolveCharacter\(unit\.CharacterName\);' `
    "foreach (CompanionUnitData unit in activeUnits)`r`n        {`r`n            if (_releasedCharacters.Contains(unit.CharacterName))`r`n                continue;`r`n`r`n            NPC? npc = ResolveCharacter(unit.CharacterName);" `
    'foreach \(CompanionUnitData unit in activeUnits\)\s*\{\s*if \(_releasedCharacters\.Contains\(unit\.CharacterName\)\)' `
    'companion ghost-follow guard'

Set-Content -Path $followPath -Value $follow -Encoding UTF8
Write-Host 'Alpha.5.3.6 integration patches applied.'
