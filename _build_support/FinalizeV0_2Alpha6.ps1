$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Replace-Method([string]$text, [string]$methodName, [string]$nextMethodName, [string]$replacement) {
    $method = [regex]::Escape($methodName)
    $next = [regex]::Escape($nextMethodName)
    $pattern = "(?ms)^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$method\([^\r\n]*\)\s*\{.*?(?=^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$next\()"
    if (-not [regex]::IsMatch($text, $pattern)) {
        throw "Finalizer could not locate method $methodName before $nextMethodName."
    }
    return [regex]::Replace($text, $pattern, $replacement + "`r`n`r`n", 1)
}

function Ensure-Replace([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Finalizer could not locate $label." }
    return $text.Replace($old, $new)
}

function Normalize-Crlf([string]$text) {
    return $text.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'
$mod = Normalize-Crlf ([System.IO.File]::ReadAllText($modPath))

if (-not $mod.Contains('using Ronvotri.TeamUp.Combat;')) {
    $mod = Ensure-Replace $mod 'using Ronvotri.TeamUp.Core;' "using Ronvotri.TeamUp.Combat;`r`nusing Ronvotri.TeamUp.Core;" 'combat using'
}

if (-not $mod.Contains('private CombatService Combat { get; set; }')) {
    $mod = Ensure-Replace $mod `
        '    private FollowService Follow { get; set; } = null!;' `
        "    private FollowService Follow { get; set; } = null!;`r`n    private ProgressionService Progression { get; set; } = null!;`r`n    private EquipmentService Equipment { get; set; } = null!;`r`n    private CombatService Combat { get; set; } = null!;" `
        'progression/combat fields'
}

if (-not $mod.Contains('Combat = new CombatService(Monitor, Follow, Progression);')) {
    $mod = Ensure-Replace $mod `
        '        Follow = new FollowService(Monitor);' `
        "        Follow = new FollowService(Monitor);`r`n        Progression = new ProgressionService();`r`n        Equipment = new EquipmentService();`r`n        Combat = new CombatService(Monitor, Follow, Progression);" `
        'service construction'
}

if (-not $mod.Contains('helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;')) {
    $mod = Ensure-Replace $mod `
        '        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;' `
        "        helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;`r`n        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;" `
        'rendering event subscription'
}

$mod = [regex]::Replace(
    $mod,
    'Monitor\.Log\("Team Up! v[^\"]+ loaded\."\s*,\s*LogLevel\.Info\);',
    'Monitor.Log("Team Up! v0.2.0-alpha.3.4 survival + progression + mastery + equipment loaded.", LogLevel.Info);',
    1)

$onSaveLoaded = @'
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PartySaveData? saveData = Helper.Data.ReadSaveData<PartySaveData>(SaveDataKey);
        Party.Load(saveData);
        Combat.Clear();
        Progression.NormalizeRoster(Party.Members);

        long recruiterId = Game1.player.UniqueMultiplayerID;
        int migratedSpecialMembers = MigrateSpecialMembersOutOfMainParty(recruiterId);
        Party.DeactivateForNewDay(recruiterId);
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);
        if (migratedSpecialMembers > 0)
            SavePartyNow();

        RecruitHintNpcName = null;
        PartyActionConfirmationOpen = false;

        Monitor.Log(
            $"Loaded {Party.Members.Count} Party Member(s) and {Party.CompanionUnits.Count} Companion Unit(s) as inactive roster entries.",
            LogLevel.Debug);
    }

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
            Monitor.Log($"Migrated special companion {member.CharacterName} out of Main Party recruitment.", LogLevel.Info);
        }

        return removed;
    }
'@
$mod = Replace-Method $mod 'OnSaveLoaded' 'OnSaving' $onSaveLoaded

$onDayEnding = @'
    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        long recruiterId = Game1.player.UniqueMultiplayerID;
        Combat.Clear();
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);
        Progression.ResetForNewDay(Party.Members);
        Party.DeactivateForNewDay(recruiterId);
        RecruitHintNpcName = null;
        PartyActionConfirmationOpen = false;
        SavePartyNow();
    }
'@
$mod = Replace-Method $mod 'OnDayEnding' 'OnReturnedToTitle' $onDayEnding

$onReturned = @'
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PendingUiAction = null;
        RecruitHintNpcName = null;
        PartyActionConfirmationOpen = false;
        SocialCodexButtonBounds = Rectangle.Empty;
        Combat.Clear();
        Party.Clear();
    }
'@
$mod = Replace-Method $mod 'OnReturnedToTitle' 'OnUpdateTicked' $onReturned

$onUpdate = @'
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        RunPendingUiAction();

        if (Game1.activeClickableMenu is null && !Game1.dialogueUp && PendingUiAction is null)
        {
            RecruitHintNpcName = null;
            PartyActionConfirmationOpen = false;
        }

        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);

        if (!e.IsMultipleOf(2))
            return;

        Follow.Update(
            Party.Members,
            Party.CompanionUnits,
            Game1.player.UniqueMultiplayerID);
    }
'@
$mod = Replace-Method $mod 'OnUpdateTicked' 'OnButtonPressed' $onUpdate

if (-not $mod.Contains('Progression.NormalizeMember(recruitedMember);')) {
    $mod = Ensure-Replace $mod `
        "                Follow.TakePartyControl(npc);`r`n                SavePartyNow();" `
        "                PartyMemberData? recruitedMember = Party.Get(npc.Name, recruiterId);`r`n                if (recruitedMember is not null)`r`n                    Progression.NormalizeMember(recruitedMember);`r`n`r`n                Follow.TakePartyControl(npc);`r`n                SavePartyNow();" `
        'recruit progression initialization'
}

$showLeave = @'
    private void ShowLeaveQuestion(NPC npc)
    {
        RecruitHintNpcName = npc.Name;
        PartyActionConfirmationOpen = true;

        Response[] responses =
        {
            new("Leave", Helper.Translation.Get("member.leave-confirm")),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = Helper.Translation.Get("member.leave-question", new { name = npc.displayName });
        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            PartyActionConfirmationOpen = false;
            if (answer != "Leave")
                return;

            long recruiterId = Game1.player.UniqueMultiplayerID;
            PartyMemberData? leavingMember = Party.Get(npc.Name, recruiterId);
            if (leavingMember is not null && !Equipment.TryReturnAll(leavingMember, out _))
            {
                ShowHud(Helper.Translation.Get("equipment.leave-blocked"), error: true);
                return;
            }

            CompanionUnitData? linkedUnit = Party.GetLinkedCompanion(npc.Name, recruiterId);
            NPC? linkedNpc = linkedUnit is null ? null : Game1.getCharacterFromName(linkedUnit.CharacterName);

            bool removed = Party.Remove(npc.Name, recruiterId);
            Follow.ReleaseToVanillaAndResumeSchedule(npc);
            if (linkedNpc is not null)
                Follow.ReleaseToVanilla(linkedNpc);

            RecruitHintNpcName = null;
            if (removed)
            {
                SavePartyNow();
                ShowHud(Helper.Translation.Get("member.left", new { name = npc.displayName }));
            }
        });
    }
'@
$mod = Replace-Method $mod 'ShowLeaveQuestion' 'ShowMemberMenu' $showLeave

$showMember = @'
    private void ShowMemberMenu(NPC npc, PartyMemberData member)
    {
        RecruitHintNpcName = npc.Name;
        PartyActionConfirmationOpen = false;

        string movementLabel = member.State == PartyMemberState.Following
            ? Helper.Translation.Get("member.stand")
            : Helper.Translation.Get("member.follow");

        Response[] responses =
        {
            new("Talk", Helper.Translation.Get("member.talk")),
            new("Movement", movementLabel),
            new("Role", Helper.Translation.Get("member.role", new { role = GetRoleLabel(member.Role) })),
            new("Engagement", Helper.Translation.Get("member.engagement", new { style = GetEngagementLabel(member.Engagement) })),
            new("Equipment", Helper.Translation.Get("member.equipment")),
            new("Vault", Helper.Translation.Get("member.vault")),
            new("Close", Helper.Translation.Get("common.close"))
        };

        string title = Helper.Translation.Get("member.title", new { name = npc.displayName, role = GetRoleLabel(member.Role) });
        Game1.currentLocation.createQuestionDialogue(title, responses, delegate(Farmer _, string answer)
        {
            switch (answer)
            {
                case "Talk": QueueUi(() => ShowVanillaDialogue(npc)); break;
                case "Movement": ToggleMovement(npc, member); break;
                case "Role": QueueUi(() => ShowRoleMenu(npc, member)); break;
                case "Engagement": QueueUi(() => ShowEngagementMenu(npc, member)); break;
                case "Equipment": QueueUi(() => ShowEquipmentMenu(npc, member)); break;
                case "Vault": QueueUi(OpenPartyVault); break;
            }
        });
    }
'@
$mod = Replace-Method $mod 'ShowMemberMenu' 'ShowRoleMenu' $showMember

$showRole = @'
    private void ShowRoleMenu(NPC npc, PartyMemberData member)
    {
        RecruitHintNpcName = null;
        NpcCombatProfile? profile = NpcProfileCatalog.Get(npc.Name);
        PartyRole[] roles = { PartyRole.Tank, PartyRole.Damage, PartyRole.Support, PartyRole.Healer, PartyRole.Control };
        List<Response> responses = roles.Select(role => new Response(role.ToString(), GetRoleOptionLabel(role, profile))).ToList();
        responses.Add(new Response("Cancel", Helper.Translation.Get("common.cancel")));

        string question = Helper.Translation.Get("role.question", new { name = npc.displayName, role = GetRoleLabel(member.Role) });
        Game1.currentLocation.createQuestionDialogue(question, responses.ToArray(), delegate(Farmer _, string answer)
        {
            if (!Enum.TryParse(answer, out PartyRole role) || role == PartyRole.Unassigned)
                return;

            if (Party.SetRole(npc.Name, Game1.player.UniqueMultiplayerID, role))
            {
                Progression.NormalizeMember(member);
                SavePartyNow();
                ShowHud(Helper.Translation.Get("role.changed", new { name = npc.displayName, role = GetRoleLabel(role) }));
            }
        });
    }
'@
$mod = Replace-Method $mod 'ShowRoleMenu' 'ToggleMovement' $showRole

$showEngagement = @'
    private void ShowEngagementMenu(NPC npc, PartyMemberData member)
    {
        RecruitHintNpcName = null;
        Response[] responses =
        {
            new(nameof(EngagementStyle.Passive), Helper.Translation.Get("engagement.passive")),
            new(nameof(EngagementStyle.Cautious), Helper.Translation.Get("engagement.cautious")),
            new(nameof(EngagementStyle.Balanced), Helper.Translation.Get("engagement.balanced")),
            new(nameof(EngagementStyle.Aggressive), Helper.Translation.Get("engagement.aggressive")),
            new(nameof(EngagementStyle.Reckless), Helper.Translation.Get("engagement.reckless")),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = Helper.Translation.Get("engagement.question", new { name = npc.displayName, style = GetEngagementLabel(member.Engagement) });
        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            if (!Enum.TryParse(answer, out EngagementStyle style))
                return;

            if (Party.SetEngagementStyle(npc.Name, Game1.player.UniqueMultiplayerID, style))
            {
                SavePartyNow();
                ShowHud(Helper.Translation.Get("engagement.changed", new { name = npc.displayName, style = GetEngagementLabel(style) }));
            }
        });
    }
'@
$mod = Replace-Method $mod 'ShowEngagementMenu' 'OpenProfileFromDialogue' $showEngagement

if (-not $mod.Contains('private void ShowEquipmentMenu(NPC npc, PartyMemberData member)')) {
$equipmentMethods = @'
    private void ShowEquipmentMenu(NPC npc, PartyMemberData member)
    {
        RecruitHintNpcName = null;
        string empty = Helper.Translation.Get("equipment.empty");
        Response[] responses =
        {
            new(nameof(EquipmentSlot.Weapon), Helper.Translation.Get("equipment.slot-line", new { slot = Helper.Translation.Get("equipment.weapon"), item = member.Weapon?.DisplayName ?? empty })),
            new(nameof(EquipmentSlot.Armor), Helper.Translation.Get("equipment.slot-line", new { slot = Helper.Translation.Get("equipment.armor"), item = member.Armor?.DisplayName ?? empty })),
            new(nameof(EquipmentSlot.Trinket), Helper.Translation.Get("equipment.slot-line", new { slot = Helper.Translation.Get("equipment.trinket"), item = member.Trinket?.DisplayName ?? empty })),
            new("Close", Helper.Translation.Get("common.close"))
        };

        string question = Helper.Translation.Get("equipment.question", new { name = npc.displayName, progression = Progression.BuildCompactSummary(member) });
        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            if (Enum.TryParse(answer, out EquipmentSlot slot))
                QueueUi(() => ShowEquipmentSlotMenu(npc, member, slot));
        });
    }

    private void ShowEquipmentSlotMenu(NPC npc, PartyMemberData member, EquipmentSlot slot)
    {
        RecruitHintNpcName = null;
        List<Response> responses = new();
        IReadOnlyList<EquipmentService.InventoryCandidate> candidates = Equipment.GetEligibleInventoryItems(slot);
        foreach (EquipmentService.InventoryCandidate candidate in candidates.Take(10))
            responses.Add(new Response($"Equip_{candidate.InventoryIndex}", candidate.Item.DisplayName));

        if (Equipment.GetEquipped(member, slot) is not null)
            responses.Add(new Response("Unequip", Helper.Translation.Get("equipment.unequip")));
        responses.Add(new Response("Back", Helper.Translation.Get("common.back")));

        EquippedItemData? equipped = Equipment.GetEquipped(member, slot);
        string current = equipped?.DisplayName ?? Helper.Translation.Get("equipment.empty");
        string question = Helper.Translation.Get("equipment.choose", new { slot = GetEquipmentSlotLabel(slot), current });

        Game1.currentLocation.createQuestionDialogue(question, responses.ToArray(), delegate(Farmer _, string answer)
        {
            if (answer == "Back")
            {
                QueueUi(() => ShowEquipmentMenu(npc, member));
                return;
            }

            if (answer == "Unequip")
            {
                if (Equipment.TryUnequip(member, slot, out _))
                {
                    Progression.NormalizeMember(member);
                    SavePartyNow();
                    ShowHud(Helper.Translation.Get("equipment.unequipped"));
                }
                else
                {
                    ShowHud(Helper.Translation.Get("equipment.inventory-full"), error: true);
                }
                QueueUi(() => ShowEquipmentMenu(npc, member));
                return;
            }

            if (!answer.StartsWith("Equip_", StringComparison.Ordinal) || !int.TryParse(answer[6..], out int inventoryIndex))
                return;

            if (Equipment.TryEquip(member, slot, inventoryIndex, out _))
            {
                Progression.NormalizeMember(member);
                SavePartyNow();
                ShowHud(Helper.Translation.Get("equipment.equipped"));
            }
            else
            {
                ShowHud(Helper.Translation.Get("equipment.inventory-full"), error: true);
            }
            QueueUi(() => ShowEquipmentMenu(npc, member));
        });
    }

    private string GetEquipmentSlotLabel(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Weapon => Helper.Translation.Get("equipment.weapon"),
            EquipmentSlot.Armor => Helper.Translation.Get("equipment.armor"),
            EquipmentSlot.Trinket => Helper.Translation.Get("equipment.trinket"),
            _ => slot.ToString()
        };
    }

'@
    $needle = '    private void OpenProfileFromDialogue(NPC npc)'
    if (-not $mod.Contains($needle)) { throw 'Finalizer could not locate equipment insertion point.' }
    $mod = $mod.Replace($needle, $equipmentMethods + $needle)
}

$openProfile = @'
    private void OpenCharacterProfile(string characterName, Action? onBack = null, Action? onOpenAll = null)
    {
        NpcCombatProfile? profile = NpcProfileCatalog.Get(characterName);
        NPC? npc = Game1.getCharacterFromName(characterName);
        string displayName = npc?.displayName ?? characterName;
        string status = GetProfileStatus(characterName);
        string source = profile?.SourceLabel ?? Helper.Translation.Get("profile.source-unknown");
        string engagement = profile is null ? string.Empty : GetEngagementLabel(profile.RecommendedEngagement);
        string passive = profile is null ? string.Empty : Helper.Translation.Get(profile.PassiveKey);
        string signature = profile is null ? string.Empty : Helper.Translation.Get(profile.AbilityKey);

        PartyMemberData? progressionMember = Party.Get(characterName, Game1.player.UniqueMultiplayerID);
        if (progressionMember is not null)
        {
            Progression.NormalizeMember(progressionMember);
            source = $"{source}\n{Progression.BuildCompactSummary(progressionMember)}\n{Progression.BuildEquipmentSummary(progressionMember)}";
        }

        Game1.activeClickableMenu = new CharacterProfileMenu(
            characterName, profile, displayName, status, source, engagement, passive, signature,
            GetRoleLabel, Helper.Translation, onBack ?? OpenCodexBrowser, onOpenAll ?? OpenCodexBrowser);
    }
'@
$mod = Replace-Method $mod 'OpenCharacterProfile' 'OpenCodexBrowser' $openProfile

if (-not $mod.Contains('private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)')) {
$renderingMethod = @'
    private void OnRenderingActiveMenu(object? sender, RenderingActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady
            || PartyActionConfirmationOpen
            || Game1.activeClickableMenu is not DialogueBox dialogueBox
            || !dialogueBox.isQuestion
            || string.IsNullOrWhiteSpace(RecruitHintNpcName))
            return;

        long recruiterId = Game1.player.UniqueMultiplayerID;
        if (Party.Get(RecruitHintNpcName, recruiterId) is not null)
            dialogueBox.dialogueIcon = null;
    }

'@
    $needle = '    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)'
    if (-not $mod.Contains($needle)) { throw 'Finalizer could not locate rendering insertion point.' }
    $mod = $mod.Replace($needle, $renderingMethod + $needle)
}

$mod = [regex]::Replace(
    $mod,
    'int dialogueLeft = dialogueBox\.x;\s*int dialogueTop = dialogueBox\.y;',
    "int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);`r`n        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);",
    1)

$resolveSpeaker = @'
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
$mod = Replace-Method $mod 'ResolveDialogueSpeaker' 'RestoreMenu' $resolveSpeaker
[System.IO.File]::WriteAllText($modPath, $mod, $utf8NoBom)

$followPath = Join-Path $repoRoot 'src\TeamUp\Following\FollowService.cs'
$follow = Normalize-Crlf ([System.IO.File]::ReadAllText($followPath))
$follow = $follow.Replace('private const float StopDistanceTiles = 1.55f;', 'private const float StopDistanceTiles = 1.45f;')
$follow = $follow.Replace('private const float WarpDistanceTiles = 10f;', 'private const float WarpDistanceTiles = 11f;')
$follow = $follow.Replace('private const float RepathDistanceTiles = 1.35f;', 'private const float RepathDistanceTiles = 0.90f;')

if (-not $follow.Contains('_releasedCharacters')) {
    $follow = Ensure-Replace $follow `
        '    private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();' `
        "    private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();`r`n    private readonly HashSet<string> _releasedCharacters = new(StringComparer.OrdinalIgnoreCase);`r`n    private readonly HashSet<string> _combatControlled = new(StringComparer.OrdinalIgnoreCase);" `
        'follow control fields'
}

$takeControl = @'
    public void TakePartyControl(NPC npc)
    {
        _releasedCharacters.Remove(npc.Name);
        _combatControlled.Remove(npc.Name);
        PrepareForParty(npc);
        ClearPath(npc);
        npc.Halt();
    }

    public void SetCombatControl(NPC npc, bool active)
    {
        if (active)
        {
            _releasedCharacters.Remove(npc.Name);
            if (!_combatControlled.Add(npc.Name))
                return;

            PrepareForParty(npc);
            ClearPath(npc);
            npc.Halt();
            return;
        }

        if (!_combatControlled.Remove(npc.Name))
            return;

        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: true);
        npc.Halt();
    }
'@
$follow = Replace-Method $follow 'TakePartyControl' 'HoldPosition' $takeControl

$release = @'
    public void ReleaseToVanilla(NPC npc)
    {
        _releasedCharacters.Add(npc.Name);
        _combatControlled.Remove(npc.Name);
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

        var currentEntry = npc.Schedule.Where(pair => pair.Key <= Game1.timeOfDay).OrderByDescending(pair => pair.Key).FirstOrDefault();
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
                var controller = new PathFindController(npc, targetLocation, destination.targetTile, destination.facingDirection);
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

        Game1.warpCharacter(npc, targetLocation, new Vector2(destination.targetTile.X, destination.targetTile.Y));
        npc.faceDirection(destination.facingDirection);
        npc.Halt();
    }
'@
$follow = Replace-Method $follow 'ReleaseToVanilla' 'ReleaseAll' $release

if (-not $follow.Contains('|| _combatControlled.Contains(member.CharacterName)')) {
    $follow = [regex]::Replace(
        $follow,
        'PartyMemberData member = ownedMembers\[index\];',
        "PartyMemberData member = ownedMembers[index];`r`n            if (_releasedCharacters.Contains(member.CharacterName)`r`n                || _combatControlled.Contains(member.CharacterName))`r`n                continue;",
        1)
}

if (-not $follow.Contains('_releasedCharacters.Contains(unit.CharacterName)')) {
    $follow = [regex]::Replace(
        $follow,
        'foreach \(CompanionUnitData unit in activeUnits\)\s*\{',
        "foreach (CompanionUnitData unit in activeUnits)`r`n        {`r`n            if (_releasedCharacters.Contains(unit.CharacterName))`r`n                continue;",
        1)
}
[System.IO.File]::WriteAllText($followPath, $follow, $utf8NoBom)

$partyPath = Join-Path $repoRoot 'src\TeamUp\Core\PartyManager.cs'
$party = Normalize-Crlf ([System.IO.File]::ReadAllText($partyPath))
$party = $party.Replace('SchemaVersion = 3,', 'SchemaVersion = 4,')
if (-not $party.Contains('TankMasteryExperience = member.TankMasteryExperience')) {
$oldSave = @'
                    Role = member.Role,
                    Engagement = member.Engagement,
                    State = member.State
'@
$newSave = @'
                    Role = member.Role,
                    Engagement = member.Engagement,
                    State = member.State,
                    Level = member.Level,
                    Experience = member.Experience,
                    CurrentHealth = member.CurrentHealth,
                    TankMasteryExperience = member.TankMasteryExperience,
                    DamageMasteryExperience = member.DamageMasteryExperience,
                    SupportMasteryExperience = member.SupportMasteryExperience,
                    HealerMasteryExperience = member.HealerMasteryExperience,
                    ControlMasteryExperience = member.ControlMasteryExperience,
                    Weapon = member.Weapon,
                    Armor = member.Armor,
                    Trinket = member.Trinket,
                    IsDowned = member.IsDowned,
                    IsWithdrawn = member.IsWithdrawn,
                    DownedTicks = member.DownedTicks,
                    DownCountToday = member.DownCountToday,
                    WoundedTicks = member.WoundedTicks
'@
    $party = Ensure-Replace $party $oldSave $newSave 'party progression persistence'
}
[System.IO.File]::WriteAllText($partyPath, $party, $utf8NoBom)

function Patch-I18n([string]$path, [bool]$vi) {
    $text = Normalize-Crlf ([System.IO.File]::ReadAllText($path))
    if ($text.Contains('"member.equipment"')) { return }

    if ($vi) {
$oldMember = @'
  "member.vault": "Kho chung Party",
'@
$newMember = @'
  "member.equipment": "Trang bị",
  "member.vault": "Kho chung Party",
'@
        $insert = @'
  "equipment.question": "TRANG BỊ · {{name}}\n{{progression}}\nChọn ô trang bị.",
  "equipment.slot-line": "{{slot}}: {{item}}",
  "equipment.weapon": "Vũ khí",
  "equipment.armor": "Giáp / Giày",
  "equipment.trinket": "Nhẫn / Trinket",
  "equipment.empty": "Chưa trang bị",
  "equipment.choose": "{{slot}}\nHiện tại: {{current}}\nChọn đồ phù hợp trong túi.",
  "equipment.unequip": "Tháo trang bị",
  "equipment.equipped": "Đã trang bị.",
  "equipment.unequipped": "Đã tháo trang bị.",
  "equipment.inventory-full": "Túi đồ không đủ chỗ hoặc món đồ không còn hợp lệ.",
  "equipment.leave-blocked": "Hãy chừa chỗ trong túi để nhận lại trang bị trước khi NPC rời đội.",

'@
    }
    else {
$oldMember = @'
  "member.vault": "Party Vault",
'@
$newMember = @'
  "member.equipment": "Equipment",
  "member.vault": "Party Vault",
'@
        $insert = @'
  "equipment.question": "EQUIPMENT · {{name}}\n{{progression}}\nChoose an equipment slot.",
  "equipment.slot-line": "{{slot}}: {{item}}",
  "equipment.weapon": "Weapon",
  "equipment.armor": "Armor / Boots",
  "equipment.trinket": "Ring / Trinket",
  "equipment.empty": "Empty",
  "equipment.choose": "{{slot}}\nCurrent: {{current}}\nChoose a compatible item from your inventory.",
  "equipment.unequip": "Unequip",
  "equipment.equipped": "Equipment updated.",
  "equipment.unequipped": "Equipment removed.",
  "equipment.inventory-full": "Inventory space is unavailable or the item is no longer valid.",
  "equipment.leave-blocked": "Make room in your inventory to receive this NPC's equipment before they leave the party.",

'@
    }

    if (-not $text.Contains($oldMember.TrimEnd())) { throw "Finalizer could not locate member.vault in $path" }
    $text = $text.Replace($oldMember.TrimEnd(), $newMember.TrimEnd())
    $needle = '  "common.back":'
    if (-not $text.Contains($needle)) { throw "Finalizer could not locate common.back in $path" }
    $text = $text.Replace($needle, $insert + $needle)
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

Patch-I18n (Join-Path $repoRoot 'src\TeamUp\i18n\default.json') $false
Patch-I18n (Join-Path $repoRoot 'src\TeamUp\i18n\vi.json') $true
Write-Host 'Team Up v0.2 alpha 3+4 consolidated source finalization complete.'
