$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha661 = Join-Path $root 'src\TeamUp\ModEntry.Alpha661.cs'
$config = Join-Path $root 'src\TeamUp\ModConfig.cs'
$party = Join-Path $root 'src\TeamUp\Core\PartyManager.cs'
$companion = Join-Path $root 'src\TeamUp\Core\CompanionUnitData.cs'
$integration = Join-Path $root 'src\TeamUp\Core\CompanionIntegrationService.cs'
$messages = Join-Path $root 'src\TeamUp\Core\MultiplayerMessages.cs'
$compat = Join-Path $root 'src\TeamUp\Core\CustomNpcCompatibilityService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha661'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.1_SHARED_PARTY_MULTIPLAYER_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.1_SHARED_PARTY_MULTIPLAYER_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}
function Replace-RegexRequired([string]$text, [string]$pattern, [string]$replacement, [string]$label) {
    $rx = New-Object System.Text.RegularExpressions.Regex($pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if (-not $rx.IsMatch($text)) { throw "Regex patch anchor missing: $label" }
    return $rx.Replace($text, $replacement, 1)
}

try {
    foreach ($required in @($project, $manifest, $modEntry, $alpha661, $config, $party, $companion, $integration, $messages, $compat, $follow, $combat)) {
        if (-not (Test-Path $required)) { throw "Missing required Alpha 6.6.1 source: $required" }
    }

    $projectText = [System.IO.File]::ReadAllText($project, [System.Text.Encoding]::UTF8)
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    [System.IO.File]::WriteAllText($project, $projectText, $utf8NoBom)

    # Shared people + companion capacity.
    $partyText = [System.IO.File]::ReadAllText($party, [System.Text.Encoding]::UTF8)
    if (-not $partyText.Contains('public PartyMemberData? GetAnyOwner')) {
        $oldContains = @'
    public bool Contains(string characterName, long recruiterId)
    {
        return Get(characterName, recruiterId) is not null;
    }
'@
        $newContains = @'
    public bool Contains(string characterName, long recruiterId)
    {
        return Get(characterName, recruiterId) is not null;
    }

    public PartyMemberData? GetAnyOwner(string characterName)
    {
        return _members.FirstOrDefault(member =>
            string.Equals(member.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));
    }
'@
        $partyText = Replace-Required $partyText $oldContains $newContains 'PartyManager global owner lookup'
    }

    if (-not $partyText.Contains('public int GetSharedPeopleCount')) {
        $pattern = '    public int GetActiveLinkedCompanionCount\(long recruiterId\).*?    public CompanionAddResult TryAddMainPet'
        $replacement = @'
    public int GetActiveLinkedCompanionCount(long recruiterId)
        => _companionUnits.Count(unit =>
            unit.RecruiterId == recruiterId
            && unit.CountsTowardCombatCompanionLimit
            && IsSlotOccupiedState(unit.State));

    public int GetActiveCombatCompanionCount()
        => _companionUnits.Count(unit => unit.CountsTowardCombatCompanionLimit && IsSlotOccupiedState(unit.State));

    public IReadOnlyList<CompanionUnitData> GetActiveCombatCompanions()
        => _companionUnits
            .Where(unit => unit.CountsTowardCombatCompanionLimit && IsSlotOccupiedState(unit.State))
            .ToList();

    public int GetSharedPeopleCount(IReadOnlyCollection<long> onlineFarmerIds)
    {
        HashSet<long> online = onlineFarmerIds.ToHashSet();
        int farmers = Math.Min(6, online.Count);
        int activeNpcs = _members.Count(member =>
            online.Contains(member.RecruiterId)
            && member.State is PartyMemberState.Following or PartyMemberState.Waiting);
        return farmers + activeNpcs;
    }

    public PartyAddResult TryAddMember(string characterName, long recruiterId)
        => TryAddMember(characterName, recruiterId, new[] { recruiterId });

    public PartyAddResult TryAddMember(string characterName, long recruiterId, IReadOnlyCollection<long> onlineFarmerIds)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return PartyAddResult.InvalidCharacter;

        if (CompanionClassificationService.IsSpecialName(characterName, null))
            return PartyAddResult.InvalidCharacter;

        if (GetAnyOwner(characterName) is not null)
            return PartyAddResult.AlreadyInParty;

        int max = Math.Clamp(_maxPartySize(), 1, 6);
        if (GetSharedPeopleCount(onlineFarmerIds) >= max)
            return PartyAddResult.PartyFull;

        _members.Add(new PartyMemberData
        {
            CharacterName = characterName,
            RecruiterId = recruiterId,
            IsPet = false,
            Role = PartyRole.Unassigned,
            Engagement = EngagementStyle.Balanced,
            State = PartyMemberState.Following
        });

        return PartyAddResult.Added;
    }

    public CompanionAddResult TryAddMainPet
'@
        $partyText = Replace-RegexRequired $partyText $pattern $replacement 'PartyManager shared people replacement'
    }

    if (-not $partyText.Contains('public CompanionAddResult TryAddPlayerCompanion')) {
        $playerCompanion = @'
    public CompanionAddResult TryAddPlayerCompanion(
        string unitId,
        string characterName,
        string displayName,
        long recruiterId,
        string providerId,
        string? providerUnitId,
        bool requestActive)
    {
        if (string.IsNullOrWhiteSpace(unitId) || string.IsNullOrWhiteSpace(characterName))
            return CompanionAddResult.InvalidCompanion;
        if (GetCompanionByUnitId(unitId, recruiterId) is not null)
            return CompanionAddResult.AlreadyRegistered;

        bool canActivate = !requestActive || CanActivateAnotherCombatCompanion();
        CompanionDeploymentState state = requestActive && canActivate
            ? CompanionDeploymentState.Active
            : CompanionDeploymentState.Standby;

        _companionUnits.Add(new CompanionUnitData
        {
            UnitId = unitId,
            CharacterName = characterName,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? characterName : displayName,
            RecruiterId = recruiterId,
            OwnerKind = CompanionOwnerKind.Player,
            Kind = CompanionUnitKind.ExternalCreature,
            ProviderId = string.IsNullOrWhiteSpace(providerId) ? "Unknown" : providerId,
            ProviderUnitId = providerUnitId,
            Role = PartyRole.Unassigned,
            State = state,
            IsPlayerMainPet = false
        });

        if (requestActive && !canActivate)
            return CompanionAddResult.AddedStandbyLimitReached;
        return state == CompanionDeploymentState.Active
            ? CompanionAddResult.AddedActive
            : CompanionAddResult.AddedStandby;
    }

'@
        $partyText = Replace-Required $partyText '    public CompanionAddResult TryLinkCompanion(' ($playerCompanion + '    public CompanionAddResult TryLinkCompanion(') 'player summon registration'
    }

    $partyText = $partyText.Replace('CanActivateAnotherLinkedCompanion(recruiterId)', 'CanActivateAnotherCombatCompanion()')

    if (-not $partyText.Contains('GetCompanionByUnitIdAnyOwner')) {
        $oldLookup = @'
    public CompanionUnitData? GetCompanionByUnitId(string unitId, long recruiterId)
    {
        return _companionUnits.FirstOrDefault(unit =>
            unit.RecruiterId == recruiterId
            && string.Equals(unit.UnitId, unitId, StringComparison.OrdinalIgnoreCase));
    }
'@
        $newLookup = @'
    public CompanionUnitData? GetCompanionByUnitId(string unitId, long recruiterId)
    {
        return _companionUnits.FirstOrDefault(unit =>
            unit.RecruiterId == recruiterId
            && string.Equals(unit.UnitId, unitId, StringComparison.OrdinalIgnoreCase));
    }

    public CompanionUnitData? GetCompanionByUnitIdAnyOwner(string unitId)
        => _companionUnits.FirstOrDefault(unit =>
            string.Equals(unit.UnitId, unitId, StringComparison.OrdinalIgnoreCase));
'@
        $partyText = Replace-Required $partyText $oldLookup $newLookup 'global companion lookup'
    }

    if (-not $partyText.Contains('EnforceSharedPeopleCapacity')) {
        $capacity = @'
    public IReadOnlyList<PartyMemberData> EnforceSharedPeopleCapacity(IReadOnlyCollection<long> onlineFarmerIds)
    {
        HashSet<long> online = onlineFarmerIds.ToHashSet();
        List<PartyMemberData> deactivated = new();

        foreach (PartyMemberData member in _members.Where(member =>
            !online.Contains(member.RecruiterId)
            && member.State is PartyMemberState.Following or PartyMemberState.Waiting))
        {
            member.State = PartyMemberState.Inactive;
            deactivated.Add(member);
        }

        int max = Math.Clamp(_maxPartySize(), 1, 6);
        int availableNpcSlots = Math.Max(0, max - Math.Min(6, online.Count));
        List<PartyMemberData> activeOnline = _members
            .Where(member => online.Contains(member.RecruiterId))
            .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .ToList();

        for (int index = availableNpcSlots; index < activeOnline.Count; index++)
        {
            activeOnline[index].State = PartyMemberState.Inactive;
            deactivated.Add(activeOnline[index]);
        }

        return deactivated;
    }

'@
        $partyText = Replace-Required $partyText '    public bool SetState(string characterName, long recruiterId, PartyMemberState state)' ($capacity + '    public bool SetState(string characterName, long recruiterId, PartyMemberState state)') 'shared people enforcement'
    }

    $oldStateGuard = @'
        if (state == CompanionDeploymentState.Active
            && unit.OwnerKind == CompanionOwnerKind.PartyMember
            && unit.State != CompanionDeploymentState.Active
            && !CanActivateAnotherCombatCompanion())
'@
    $newStateGuard = @'
        if (state == CompanionDeploymentState.Active
            && unit.CountsTowardCombatCompanionLimit
            && unit.State != CompanionDeploymentState.Active
            && !CanActivateAnotherCombatCompanion())
'@
    if ($partyText.Contains($oldStateGuard)) {
        $partyText = $partyText.Replace($oldStateGuard, $newStateGuard)
    }

    if (-not $partyText.Contains('private static bool IsSlotOccupiedState')) {
        $oldLimit = @'
    private bool CanActivateAnotherLinkedCompanion(long recruiterId)
    {
        int max = Math.Clamp(_maxActiveLinkedCompanions(), 0, 6);
        return GetActiveLinkedCompanionCount(recruiterId) < max;
    }
'@
        $newLimit = @'
    private bool CanActivateAnotherCombatCompanion()
    {
        int max = Math.Clamp(_maxActiveLinkedCompanions(), 0, 2);
        return GetActiveCombatCompanionCount() < max;
    }

    private static bool IsSlotOccupiedState(CompanionDeploymentState state)
        => state is CompanionDeploymentState.Active
            or CompanionDeploymentState.Waiting
            or CompanionDeploymentState.ReturningHome;
'@
        $partyText = Replace-Required $partyText $oldLimit $newLimit 'global two-companion limit'
    }
    [System.IO.File]::WriteAllText($party, $partyText, $utf8NoBom)

    # Follow ownership + Sudoku handshake + per-Farmer routing.
    $followText = [System.IO.File]::ReadAllText($follow, [System.Text.Encoding]::UTF8)
    if (-not $followText.Contains('PartyControlledModDataKey')) {
        $followText = Replace-Required $followText 'public sealed class FollowService`n{' "public sealed class FollowService`n{`n    public const string PartyControlledModDataKey = \"Ronvotri.TeamUp/PartyControlled\";`n    public const string PartyControllerOwnerModDataKey = \"Ronvotri.TeamUp/PartyControllerOwner\";" 'follow control marker constants'
    }

    if (-not $followText.Contains('Farmer owner)')) {
        $oldUpdate = @'
    public void Update(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId)
    {
        if (!Context.IsWorldReady)
            return;

        UpdatePartyMembers(members, recruiterId);
        UpdateCompanionUnits(members, companionUnits, recruiterId);
    }
'@
        $newUpdate = @'
    public void Update(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId)
        => Update(members, companionUnits, recruiterId, Game1.player);

    public void Update(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId,
        Farmer owner)
    {
        if (!Context.IsWorldReady || owner.currentLocation is null)
            return;

        UpdatePartyMembers(members, recruiterId, owner);
        UpdateCompanionUnits(members, companionUnits, recruiterId, owner);
    }
'@
        $followText = Replace-Required $followText $oldUpdate $newUpdate 'per-Farmer follow update'
    }

    if (-not $followText.Contains('long? recruiterId = null')) {
        $oldPrepare = @'
    public void PrepareForParty(NPC npc)
    {
        RememberBaseSpeed(npc);
        EnableFarmerPassThrough(npc);
        npc.followSchedule = false;
        npc.ignoreScheduleToday = true;
    }

    public void TakePartyControl(NPC npc)
    {
        _releasedCharacters.Remove(npc.Name);
        _combatControlled.Remove(npc.Name);
        PrepareForParty(npc);
'@
        $newPrepare = @'
    public void PrepareForParty(NPC npc, long? recruiterId = null)
    {
        RememberBaseSpeed(npc);
        EnableFarmerPassThrough(npc);
        npc.modData[PartyControlledModDataKey] = "true";
        if (recruiterId.HasValue)
            npc.modData[PartyControllerOwnerModDataKey] = recruiterId.Value.ToString();
        npc.followSchedule = false;
        npc.ignoreScheduleToday = true;
    }

    public void TakePartyControl(NPC npc, long? recruiterId = null)
    {
        _releasedCharacters.Remove(npc.Name);
        _combatControlled.Remove(npc.Name);
        PrepareForParty(npc, recruiterId);
'@
        $followText = Replace-Required $followText $oldPrepare $newPrepare 'party-controlled runtime marker'
    }

    if (-not $followText.Contains('npc.modData.Remove(PartyControlledModDataKey)')) {
        $followText = Replace-Required $followText '        RestoreFarmerPassThrough(npc);`n        npc.Halt();' "        RestoreFarmerPassThrough(npc);`n        npc.modData.Remove(PartyControlledModDataKey);`n        npc.modData.Remove(PartyControllerOwnerModDataKey);`n        npc.Halt();" 'release party marker'
    }

    if (-not $followText.Contains('UpdatePartyMembers(IReadOnlyList<PartyMemberData> members, long recruiterId, Farmer owner)')) {
        $pattern = '    private void UpdatePartyMembers\(IReadOnlyList<PartyMemberData> members, long recruiterId\).*?    private void UpdateCompanionUnits\('
        $replacement = @'
    private void UpdatePartyMembers(IReadOnlyList<PartyMemberData> members, long recruiterId, Farmer owner)
    {
        List<PartyMemberData> ownedMembers = members
            .Where(member => member.RecruiterId == recruiterId)
            .Take(FormationOffsets.Length)
            .ToList();

        for (int index = 0; index < ownedMembers.Count; index++)
        {
            PartyMemberData member = ownedMembers[index];
            if (_releasedCharacters.Contains(member.CharacterName)
                || _combatControlled.Contains(member.CharacterName))
                continue;
            NPC? npc = ResolveCharacter(member.CharacterName);
            if (npc is null)
                continue;

            if (member.State == PartyMemberState.Waiting)
            {
                HoldPosition(npc);
                continue;
            }
            if (member.State != PartyMemberState.Following)
                continue;

            PrepareForParty(npc, recruiterId);
            Vector2 targetTile = FindPlayerFollowTile(owner.currentLocation, owner.Tile, index);
            FollowTarget(npc, owner.currentLocation, targetTile, owner.FacingDirection);
        }
    }

    private void UpdateCompanionUnits(
'@
        $followText = Replace-RegexRequired $followText $pattern $replacement 'owner-routed party follow'
    }

    if (-not $followText.Contains('long recruiterId,`n        Farmer owner)')) {
        $pattern = '    private void UpdateCompanionUnits\(\s*IReadOnlyList<PartyMemberData> members,\s*IReadOnlyList<CompanionUnitData> companionUnits,\s*long recruiterId\)\s*\{.*?\n    \}\n\n    private void FollowTarget'
        $replacement = @'
    private void UpdateCompanionUnits(
        IReadOnlyList<PartyMemberData> members,
        IReadOnlyList<CompanionUnitData> companionUnits,
        long recruiterId,
        Farmer owner)
    {
        List<CompanionUnitData> activeUnits = companionUnits
            .Where(unit => unit.RecruiterId == recruiterId)
            .Where(unit => unit.State is CompanionDeploymentState.Active or CompanionDeploymentState.Waiting)
            .ToList();

        int playerPetIndex = 0;
        var ownerCompanionIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (CompanionUnitData unit in activeUnits)
        {
            if (_releasedCharacters.Contains(unit.CharacterName))
                continue;
            NPC? npc = ResolveCharacter(unit.CharacterName);
            if (npc is null)
                continue;

            if (unit.State == CompanionDeploymentState.Waiting)
            {
                HoldPosition(npc);
                continue;
            }

            if (unit.OwnerKind == CompanionOwnerKind.Player)
            {
                PrepareForParty(npc, recruiterId);
                Vector2 target = FindCompanionTile(owner.currentLocation, owner.Tile, playerPetIndex++);
                FollowTarget(npc, owner.currentLocation, target, owner.FacingDirection);
                continue;
            }

            if (unit.OwnerCharacterName is null)
                continue;
            PartyMemberData? ownerData = members.FirstOrDefault(member =>
                member.RecruiterId == recruiterId
                && string.Equals(member.CharacterName, unit.OwnerCharacterName, StringComparison.OrdinalIgnoreCase));
            NPC? ownerNpc = ResolveCharacter(unit.OwnerCharacterName);
            if (ownerData is null || ownerNpc is null || ownerData.State != PartyMemberState.Following)
                continue;

            PrepareForParty(npc, recruiterId);
            int index = ownerCompanionIndex.TryGetValue(ownerData.CharacterName, out int current) ? current : 0;
            ownerCompanionIndex[ownerData.CharacterName] = index + 1;
            Vector2 ownerTarget = FindCompanionTile(ownerNpc.currentLocation, ownerNpc.Tile, index);
            FollowTarget(npc, ownerNpc.currentLocation, ownerTarget, ownerNpc.FacingDirection);
        }
    }

    private void FollowTarget
'@
        $followText = Replace-RegexRequired $followText $pattern $replacement 'owner-routed companion follow'
    }

    $oldFindPlayer = @'
    private static Vector2 FindPlayerFollowTile(GameLocation location, int slotIndex)
    {
        Point offset = FormationOffsets[Math.Clamp(slotIndex, 0, FormationOffsets.Length - 1)];
        return FindOpenNear(location, Game1.player.Tile, offset);
    }
'@
    $newFindPlayer = @'
    private static Vector2 FindPlayerFollowTile(GameLocation location, Vector2 farmerTile, int slotIndex)
    {
        Point offset = FormationOffsets[Math.Clamp(slotIndex, 0, FormationOffsets.Length - 1)];
        return FindOpenNear(location, farmerTile, offset);
    }
'@
    if ($followText.Contains($oldFindPlayer)) { $followText = $followText.Replace($oldFindPlayer, $newFindPlayer) }
    [System.IO.File]::WriteAllText($follow, $followText, $utf8NoBom)

    # Per-owner combat context. Each online Farmer gets an independent CombatService runtime.
    $combatText = [System.IO.File]::ReadAllText($combat, [System.Text.Encoding]::UTF8)
    if (-not $combatText.Contains('public void SetFarmerContext')) {
        $combatText = $combatText.Replace('private static List<Monster> GetLivingMonstersNear', 'private List<Monster> GetLivingMonstersNear')
        $combatText = $combatText.Replace('Game1.currentLocation', 'FarmerContext.currentLocation')
        $combatText = $combatText.Replace('Game1.player', 'FarmerContext')
        $contextBlock = @'
    private int _threatPulseTicks;
    private int _lastFarmerHealth = -1;
    private Farmer? _farmerContext;

    private Farmer FarmerContext => _farmerContext ?? Game1.player;

    public void SetFarmerContext(Farmer farmer)
    {
        _farmerContext = farmer;
    }
'@
        $combatText = Replace-Required $combatText "    private int _threatPulseTicks;`n    private int _lastFarmerHealth = -1;" $contextBlock.TrimEnd() 'combat Farmer context'
    }
    [System.IO.File]::WriteAllText($combat, $combatText, $utf8NoBom)

    # Main entry wiring.
    $modText = [System.IO.File]::ReadAllText($modEntry, [System.Text.Encoding]::UTF8)
    $modText = $modText.Replace('public sealed class ModEntry : Mod', 'public sealed partial class ModEntry : Mod')
    $modText = $modText.Replace('Config.MaxActiveLinkedCompanions = Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 6);', 'Config.MaxActiveLinkedCompanions = Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2);')
    $modText = [regex]::Replace($modText, 'Team Up DEBUG HARNESS READY \| command: teamup_test \| build: v0\.2\.0-alpha\.6\.[0-9.]+', 'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.6.1')
    $modText = [regex]::Replace($modText, 'Team Up! v0\.2\.0-alpha\.6\.[0-9.]+ [^\"]+ loaded\.', 'Team Up! v0.2.0-alpha.6.6.1 Shared Party Capacity + Companion Choice + Multiplayer Foundation loaded.')

    if (-not $modText.Contains('RegisterAlpha661MultiplayerEvents();')) {
        $modText = Replace-Required $modText '        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;' "        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;`n        RegisterAlpha661MultiplayerEvents();" 'multiplayer event registration'
    }

    $oldSaving = @'
    private void OnSaving(object? sender, SavingEventArgs e)
    {
        SavePartyNow();
    }
'@
    $newSaving = @'
    private void OnSaving(object? sender, SavingEventArgs e)
    {
        if (Context.IsMainPlayer)
            SavePartyNow();
    }
'@
    if ($modText.Contains($oldSaving)) { $modText = $modText.Replace($oldSaving, $newSaving) }

    if (-not $modText.Contains('UpdateOwnedPartyRuntime(e);')) {
        $pattern = '        SkillIdentity\.Update\(Party\.Members, Game1\.player\.UniqueMultiplayerID\);.*?        Follow\.Update\(\s*Party\.Members,\s*Party\.CompanionUnits,\s*Game1\.player\.UniqueMultiplayerID\);'
        $modText = Replace-RegexRequired $modText $pattern '        UpdateOwnedPartyRuntime(e);' 'host runtime owner routing'
    }

    $modText = $modText.Replace('        if (!Context.IsWorldReady || !Context.IsMainPlayer)`n            return;`n`n        if (Game1.activeClickableMenu is GameMenu gameMenu', '        if (!Context.IsWorldReady)`n            return;`n`n        if (Game1.activeClickableMenu is GameMenu gameMenu')

    if (-not $modText.Contains('RequestOrRecruitAlpha661')) {
        $pattern = '    private void RecruitNpc\(NPC npc\).*?    private void ShowMemberMenu\(NPC npc, PartyMemberData member\)'
        $replacement = @'
    private void RecruitNpc(NPC npc)
        => RequestOrRecruitAlpha661(npc, includeCompanion: false, replacementCompanionUnitId: null);

    private void ShowRecruitQuestion(NPC npc)
        => ShowRecruitQuestionAlpha661(npc);

    private void ShowLeaveQuestion(NPC npc)
        => ShowLeaveQuestionAlpha661(npc);

    private void ShowMemberMenu(NPC npc, PartyMemberData member)
'@
        $modText = Replace-RegexRequired $modText $pattern $replacement 'recruit/leave alpha 6.6.1 delegation'
    }

    if (-not $modText.Contains('ToggleMovementAlpha661(npc, member);')) {
        $pattern = '    private void ToggleMovement\(NPC npc, PartyMemberData member\).*?    private void ShowEngagementMenu'
        $replacement = @'
    private void ToggleMovement(NPC npc, PartyMemberData member)
        => ToggleMovementAlpha661(npc, member);

    private void ShowEngagementMenu
'@
        $modText = Replace-RegexRequired $modText $pattern $replacement 'movement multiplayer delegation'
    }

    $modText = $modText.Replace('if (Party.SetRole(npc.Name, Game1.player.UniqueMultiplayerID, role))', 'if (TrySetRoleForCurrentPlayer(npc, role))')
    $modText = $modText.Replace('if (Party.SetEngagementStyle(npc.Name, Game1.player.UniqueMultiplayerID, style))', 'if (TrySetEngagementForCurrentPlayer(npc, style))')

    $oldEquipmentCase = '                case "Equipment": QueueUi(() => ShowEquipmentMenu(npc, member)); break;'
    $newEquipmentCase = @'
                case "Equipment":
                    if (Context.IsMainPlayer)
                        QueueUi(() => ShowEquipmentMenu(npc, member));
                    else
                        ShowHud("Equipment management is host-authoritative in Alpha 6.6.1 multiplayer.", error: true);
                    break;
'@
    if ($modText.Contains($oldEquipmentCase)) { $modText = $modText.Replace($oldEquipmentCase, $newEquipmentCase.TrimEnd()) }

    $oldSave = @'
    private void SavePartyNow()
    {
        Helper.Data.WriteSaveData(SaveDataKey, Party.CreateSaveData());
    }
'@
    $newSave = @'
    private void SavePartyNow()
    {
        if (!Context.IsMainPlayer)
            return;
        Helper.Data.WriteSaveData(SaveDataKey, Party.CreateSaveData());
    }
'@
    if ($modText.Contains($oldSave)) { $modText = $modText.Replace($oldSave, $newSave) }

    $oldRecruitable = @'
    private bool IsRecruitableNpc(NPC npc)
    {
        if (CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc))
            return CustomNpcCompatibilityService.CanRecruit(npc, Helper.ModRegistry);

        return CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames);
    }
'@
    $newRecruitable = @'
    private bool IsRecruitableNpc(NPC npc)
    {
        PartyMemberData? owned = Party.GetAnyOwner(npc.Name);
        if (owned is not null && owned.RecruiterId != Game1.player.UniqueMultiplayerID)
            return false;
        return IsRecruitableNpcFor(npc, Game1.player);
    }
'@
    if ($modText.Contains($oldRecruitable)) { $modText = $modText.Replace($oldRecruitable, $newRecruitable) }

    $strategyAnchor = "        Config.PartyStrategy = next.Value;`n        Helper.WriteConfig(Config);`n        Combat.Clear();"
    if ($modText.Contains($strategyAnchor) -and -not $modText.Contains("Config.PartyStrategy = next.Value;`n        Helper.WriteConfig(Config);`n        Combat.Clear();`n        ClearRemoteCombatServices();")) {
        $modText = $modText.Replace($strategyAnchor, $strategyAnchor + "`n        ClearRemoteCombatServices();")
    }

    if (-not $modText.Contains('ClearRemoteCombatServices();`n        Party.Clear();')) {
        $modText = $modText.Replace('        Surge.Reset();`n        Party.Clear();', '        Surge.Reset();`n        ClearRemoteCombatServices();`n        Party.Clear();')
    }
    [System.IO.File]::WriteAllText($modEntry, $modText, $utf8NoBom)

    # Acceptance tokens before compiling.
    foreach ($token in @('GetSharedPeopleCount', 'EnforceSharedPeopleCapacity', 'GetActiveCombatCompanionCount', 'TryAddPlayerCompanion', 'CanActivateAnotherCombatCompanion')) {
        if (-not $partyText.Contains($token)) { throw "Party capacity token missing: $token" }
    }
    foreach ($token in @('PartyControlledModDataKey', 'PartyControllerOwnerModDataKey', 'Farmer owner')) {
        if (-not $followText.Contains($token)) { throw "Follow multiplayer token missing: $token" }
    }
    foreach ($token in @('SetFarmerContext', 'FarmerContext.currentLocation', 'HardLeashTiles = 12f', 'TargetLockDurationTicks = 45')) {
        if (-not $combatText.Contains($token)) { throw "Combat owner-context token missing: $token" }
    }
    foreach ($token in @('RegisterAlpha661MultiplayerEvents', 'UpdateOwnedPartyRuntime(e)', 'RequestOrRecruitAlpha661', 'TrySetRoleForCurrentPlayer', 'TrySetEngagementForCurrentPlayer')) {
        if (-not $modText.Contains($token)) { throw "ModEntry Alpha 6.6.1 token missing: $token" }
    }

    Log 'Building Alpha 6.6.1 Shared Party Capacity + Companion Choice + Multiplayer Foundation...'
    Log 'People cap: 6 total online Farmers + active NPCs.'
    Log 'Companion cap: 2 shared external summon/Pokemon slots; vanilla pets and ChaCha free.'
    Log 'Recruit UX: NPC-only / NPC+companion / cancel, with replacement selection when pool is full.'
    Log 'Multiplayer: host-authoritative requests, shared snapshots, recruiter ownership, per-Farmer Follow/Combat context.'
    Log 'Sudoku compatibility: Ronvotri.TeamUp/PartyControlled runtime handshake.'

    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    & dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    $dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' | Select-Object -First 1
    if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

    if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
    if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
    New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
    Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force
    $manifestText = [System.IO.File]::ReadAllText($manifest, [System.Text.Encoding]::UTF8).Replace('%ProjectVersion%', $version)
    [System.IO.File]::WriteAllText((Join-Path $stageMod 'manifest.json'), $manifestText, $utf8NoBom)
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.1 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($shaPath, "$hash  $(Split-Path $zip -Leaf)`r`n", $utf8NoBom)
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_1_SHARED_PARTY_MULTIPLAYER_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.1'
    Log 'PEOPLE CAP: 6 TOTAL FARMERS + NPCS'
    Log 'COMPANION CAP: 2 SHARED EXTERNAL CREATURES; CHACHA/PETS FREE'
    Log 'MULTIPLAYER: HOST AUTHORITATIVE + PER-FARMER FOLLOW/COMBAT OWNER CONTEXT'
    Log 'SUDOKU HANDSHAKE: Ronvotri.TeamUp/PartyControlled=true'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
