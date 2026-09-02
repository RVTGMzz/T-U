$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if ($text.Contains($old)) { return $text.Replace($old, $new) }
    throw "Alpha.5.3.7 patch failed: expected source block not found: $label"
}

function Replace-RegexRequired([string]$text, [string]$pattern, [string]$replacement, [string]$alreadyPattern, [string]$label) {
    $options = [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::Singleline
    if ([regex]::IsMatch($text, $alreadyPattern, $options)) { return $text }
    if (-not [regex]::IsMatch($text, $pattern, $options)) {
        throw "Alpha.5.3.7 patch failed: expected source pattern not found: $label"
    }
    return [regex]::Replace($text, $pattern, $replacement, $options, [timespan]::FromSeconds(2))
}

$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw

$mod = Replace-Required $mod `
    'Team Up! v0.1.0-alpha.5.3.6 vault + member hint hotfix loaded.' `
    'Team Up! v0.1.0-alpha.5.3.7 native vault + special lifecycle loaded.' `
    'version log'

$oldLoad = @'
        Party.Load(saveData);

        long recruiterId = Game1.player.UniqueMultiplayerID;
        Party.DeactivateForNewDay(recruiterId);
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);
'@
$newLoad = @'
        Party.Load(saveData);

        long recruiterId = Game1.player.UniqueMultiplayerID;
        int migratedSpecialMembers = MigrateSpecialMembersOutOfMainParty(recruiterId);
        Party.DeactivateForNewDay(recruiterId);
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);
        if (migratedSpecialMembers > 0)
            SavePartyNow();
'@
$mod = Replace-Required $mod $oldLoad $newLoad 'special member save migration'

$migrationMethod = @'
    private int MigrateSpecialMembersOutOfMainParty(long recruiterId)
    {
        List<PartyMemberData> invalidMembers = Party.Members
            .Where(member => member.RecruiterId == recruiterId)
            .Where(member => CompanionClassificationService.IsSpecialName(member.CharacterName, Config.SpecialCompanionNpcNames))
            .ToList();

        int removed = 0;
        foreach (PartyMemberData member in invalidMembers)
        {
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is not null)
                Follow.ReleaseToVanilla(npc);

            if (!Party.Remove(member.CharacterName, recruiterId))
                continue;

            removed++;
            Monitor.Log(
                $"Migrated special companion {member.CharacterName} out of Main Party recruitment.",
                LogLevel.Info);
        }

        return removed;
    }

'@
$mod = Replace-RegexRequired $mod `
    '    private void OnSaving\(object\? sender, SavingEventArgs e\)' `
    ($migrationMethod + '    private void OnSaving(object? sender, SavingEventArgs e)') `
    'private int MigrateSpecialMembersOutOfMainParty\(long recruiterId\)' `
    'special member migration helper'

$oldMemberOpen = @'
    private void ShowMemberMenu(NPC npc, PartyMemberData member)
    {
        RecruitHintNpcName = npc.Name;
'@
$newMemberOpen = @'
    private void ShowMemberMenu(NPC npc, PartyMemberData member)
    {
        // Pin both contextual actions before the question menu is created so the
        // left Profile and right Leave tags appear in the same render frame.
        RecruitHintNpcName = npc.Name;
        PartyActionConfirmationOpen = false;
'@
$mod = Replace-Required $mod $oldMemberOpen $newMemberOpen 'same-frame member hints'

$mod = Replace-Required $mod `
    'Follow.ReleaseToVanilla(npc);' `
    'Follow.ReleaseToVanillaAndResumeSchedule(npc);' `
    'leave resumes vanilla schedule'

Set-Content -Path $modPath -Value $mod -Encoding UTF8

$followPath = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$follow = Get-Content $followPath -Raw

$oldRelease = @'
    public void ReleaseToVanilla(NPC npc)
    {
        _releasedCharacters.Add(npc.Name);
        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: false);
        npc.Halt();
        npc.followSchedule = true;
        npc.ignoreScheduleToday = false;
    }
'@
$newRelease = @'
    public void ReleaseToVanilla(NPC npc)
    {
        _releasedCharacters.Add(npc.Name);
        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: false);
        npc.Halt();
        npc.followSchedule = true;
        npc.ignoreScheduleToday = false;
    }

    public void ReleaseToVanillaAndResumeSchedule(NPC npc)
    {
        ReleaseToVanilla(npc);
        ResumeVanillaSchedulePosition(npc);
    }

    private void ResumeVanillaSchedulePosition(NPC npc)
    {
        if (npc.Schedule is null || npc.Schedule.Count == 0)
            return;

        var currentEntry = npc.Schedule
            .Where(pair => pair.Key <= Game1.timeOfDay)
            .OrderByDescending(pair => pair.Key)
            .FirstOrDefault();

        SchedulePathDescription? destination = currentEntry.Value;
        if (destination is null || string.IsNullOrWhiteSpace(destination.targetLocationName))
            return;

        GameLocation? targetLocation = Game1.getLocationFromName(destination.targetLocationName);
        if (targetLocation is null)
            return;

        npc.queuedSchedulePaths.Clear();
        npc.lastAttemptedSchedule = Game1.timeOfDay;

        if (ReferenceEquals(npc.currentLocation, targetLocation))
        {
            try
            {
                var controller = new PathFindController(
                    npc,
                    targetLocation,
                    destination.targetTile,
                    destination.facingDirection);

                if (controller.pathToEndPoint is not null && controller.pathToEndPoint.Count > 0)
                {
                    npc.controller = controller;
                    return;
                }
            }
            catch (Exception ex)
            {
                _monitor.LogOnce($"Schedule resume path failed for {npc.Name}: {ex.Message}", LogLevel.Trace);
            }
        }

        Game1.warpCharacter(
            npc,
            targetLocation,
            new Vector2(destination.targetTile.X, destination.targetTile.Y));
        npc.faceDirection(destination.facingDirection);
        npc.Halt();
    }
'@
$follow = Replace-Required $follow $oldRelease $newRelease 'vanilla schedule resume helper'

Set-Content -Path $followPath -Value $follow -Encoding UTF8
Write-Host 'Alpha.5.3.7 integration patches applied.'
