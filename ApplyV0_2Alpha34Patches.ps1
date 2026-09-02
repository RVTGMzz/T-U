$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if ($text.Contains($old)) { return $text.Replace($old, $new) }
    throw "v0.2-alpha.3+4 patch failed: expected source block not found: $label"
}

# -----------------------------------------------------------------------------
# ModEntry integration: progression, gear UI, save/load lifecycle.
# -----------------------------------------------------------------------------
$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw

$oldMarker = 'Team Up! v0.2.0-alpha.2 combat feedback + signature VFX loaded.'
$newMarker = 'Team Up! v0.2.0-alpha.3.4 survival + progression + mastery + equipment loaded.'
if ($mod.Contains($newMarker)) {
    Write-Host 'v0.2-alpha.3+4 ModEntry integration already applied.'
}
else {
    if (-not $mod.Contains($oldMarker)) {
        throw 'v0.2-alpha.3+4 patch failed: alpha.2 integration marker not found.'
    }

    $mod = Replace-Required $mod `
        'private CombatService Combat { get; set; } = null!;' `
        "private CombatService Combat { get; set; } = null!;`r`n    private ProgressionService Progression { get; set; } = null!;`r`n    private EquipmentService Equipment { get; set; } = null!;" `
        'progression fields'

    $mod = Replace-Required $mod `
        "Follow = new FollowService(Monitor);`r`n        Combat = new CombatService(Monitor, Follow);" `
        "Follow = new FollowService(Monitor);`r`n        Progression = new ProgressionService();`r`n        Equipment = new EquipmentService();`r`n        Combat = new CombatService(Monitor, Follow, Progression);" `
        'service construction'

    $mod = Replace-Required $mod `
        "Party.Load(saveData);`r`n        Combat.Clear();" `
        "Party.Load(saveData);`r`n        Combat.Clear();`r`n        Progression.NormalizeRoster(Party.Members);" `
        'normalize loaded roster'

    $mod = Replace-Required $mod `
        "Combat.Clear();`r`n        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);`r`n        Party.DeactivateForNewDay(recruiterId);" `
        "Combat.Clear();`r`n        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);`r`n        Progression.ResetForNewDay(Party.Members);`r`n        Party.DeactivateForNewDay(recruiterId);" `
        'overnight progression reset'

    $mod = Replace-Required $mod `
        "                Follow.TakePartyControl(npc);`r`n                SavePartyNow();" `
        "                PartyMemberData? recruitedMember = Party.Get(npc.Name, recruiterId);`r`n                if (recruitedMember is not null)`r`n                    Progression.NormalizeMember(recruitedMember);`r`n`r`n                Follow.TakePartyControl(npc);`r`n                SavePartyNow();" `
        'initialize fresh recruit progression'

    $leaveOld = @'
            long recruiterId = Game1.player.UniqueMultiplayerID;
            Follow.ReleaseToVanilla(npc);
            if (Party.Remove(npc.Name, recruiterId))
'@
    $leaveNew = @'
            long recruiterId = Game1.player.UniqueMultiplayerID;
            PartyMemberData? leavingMember = Party.Get(npc.Name, recruiterId);
            if (leavingMember is not null && !Equipment.TryReturnAll(leavingMember, out _))
            {
                ShowHud(Helper.Translation.Get("equipment.leave-blocked"), error: true);
                return;
            }

            Follow.ReleaseToVanilla(npc);
            if (Party.Remove(npc.Name, recruiterId))
'@
    $mod = Replace-Required $mod $leaveOld $leaveNew 'safe gear return on leave'

    $menuOld = @'
            new("Engagement", Helper.Translation.Get("member.engagement", new { style = GetEngagementLabel(member.Engagement) })),
            new("Vault", Helper.Translation.Get("member.vault")),
'@
    $menuNew = @'
            new("Engagement", Helper.Translation.Get("member.engagement", new { style = GetEngagementLabel(member.Engagement) })),
            new("Equipment", Helper.Translation.Get("member.equipment")),
            new("Vault", Helper.Translation.Get("member.vault")),
'@
    $mod = Replace-Required $mod $menuOld $menuNew 'member equipment option'

    $switchOld = @'
                case "Vault":
                    QueueUi(OpenPartyVault);
                    break;
'@
    $switchNew = @'
                case "Equipment":
                    QueueUi(() => ShowEquipmentMenu(npc, member));
                    break;
                case "Vault":
                    QueueUi(OpenPartyVault);
                    break;
'@
    $mod = Replace-Required $mod $switchOld $switchNew 'member equipment action'

    $roleOld = @'
            if (Party.SetRole(npc.Name, Game1.player.UniqueMultiplayerID, role))
            {
                SavePartyNow();
'@
    $roleNew = @'
            if (Party.SetRole(npc.Name, Game1.player.UniqueMultiplayerID, role))
            {
                Progression.NormalizeMember(member);
                SavePartyNow();
'@
    $mod = Replace-Required $mod $roleOld $roleNew 'normalize after role change'

    $equipmentMethods = @'
    private void ShowEquipmentMenu(NPC npc, PartyMemberData member)
    {
        string empty = Helper.Translation.Get("equipment.empty");
        Response[] responses =
        {
            new(nameof(EquipmentSlot.Weapon), Helper.Translation.Get("equipment.slot-line", new
            {
                slot = Helper.Translation.Get("equipment.weapon"),
                item = member.Weapon?.DisplayName ?? empty
            })),
            new(nameof(EquipmentSlot.Armor), Helper.Translation.Get("equipment.slot-line", new
            {
                slot = Helper.Translation.Get("equipment.armor"),
                item = member.Armor?.DisplayName ?? empty
            })),
            new(nameof(EquipmentSlot.Trinket), Helper.Translation.Get("equipment.slot-line", new
            {
                slot = Helper.Translation.Get("equipment.trinket"),
                item = member.Trinket?.DisplayName ?? empty
            })),
            new("Close", Helper.Translation.Get("common.close"))
        };

        string question = Helper.Translation.Get("equipment.question", new
        {
            name = npc.displayName,
            progression = Progression.BuildCompactSummary(member)
        });

        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            if (Enum.TryParse(answer, out EquipmentSlot slot))
                QueueUi(() => ShowEquipmentSlotMenu(npc, member, slot));
        });
    }

    private void ShowEquipmentSlotMenu(NPC npc, PartyMemberData member, EquipmentSlot slot)
    {
        List<Response> responses = new();
        IReadOnlyList<EquipmentService.InventoryCandidate> candidates = Equipment.GetEligibleInventoryItems(slot);
        foreach (EquipmentService.InventoryCandidate candidate in candidates.Take(10))
        {
            responses.Add(new Response($"Equip_{candidate.InventoryIndex}", candidate.Item.DisplayName));
        }

        if (Equipment.GetEquipped(member, slot) is not null)
            responses.Add(new Response("Unequip", Helper.Translation.Get("equipment.unequip")));

        responses.Add(new Response("Back", Helper.Translation.Get("common.back")));

        EquippedItemData? equipped = Equipment.GetEquipped(member, slot);
        string current = equipped?.DisplayName ?? Helper.Translation.Get("equipment.empty");
        string question = Helper.Translation.Get("equipment.choose", new
        {
            slot = GetEquipmentSlotLabel(slot),
            current
        });

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

            if (!answer.StartsWith("Equip_", StringComparison.Ordinal)
                || !int.TryParse(answer[6..], out int inventoryIndex))
            {
                return;
            }

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
    $mod = Replace-Required $mod `
        '    private void OpenProfileFromDialogue(NPC npc)' `
        ($equipmentMethods + '    private void OpenProfileFromDialogue(NPC npc)') `
        'equipment methods'

    $profileOld = @'
        string passive = profile is null ? string.Empty : Helper.Translation.Get(profile.PassiveKey);
        string signature = profile is null ? string.Empty : Helper.Translation.Get(profile.AbilityKey);

        Game1.activeClickableMenu = new CharacterProfileMenu(
'@
    $profileNew = @'
        string passive = profile is null ? string.Empty : Helper.Translation.Get(profile.PassiveKey);
        string signature = profile is null ? string.Empty : Helper.Translation.Get(profile.AbilityKey);

        PartyMemberData? progressionMember = Party.Get(characterName, Game1.player.UniqueMultiplayerID);
        if (progressionMember is not null)
        {
            Progression.NormalizeMember(progressionMember);
            source = $"{source}\n{Progression.BuildCompactSummary(progressionMember)}\n{Progression.BuildEquipmentSummary(progressionMember)}";
        }

        Game1.activeClickableMenu = new CharacterProfileMenu(
'@
    $mod = Replace-Required $mod $profileOld $profileNew 'profile progression summary'

    $mod = $mod.Replace($oldMarker, $newMarker)
    Set-Content -Path $modPath -Value $mod -Encoding UTF8
    Write-Host 'v0.2-alpha.3+4 ModEntry integration applied.'
}

# -----------------------------------------------------------------------------
# PartyManager persistence: carry all progression/equipment fields into save DTO.
# -----------------------------------------------------------------------------
$partyPath = Join-Path $root 'src\TeamUp\Core\PartyManager.cs'
$party = Get-Content $partyPath -Raw

$party = Replace-Required $party 'SchemaVersion = 3,' 'SchemaVersion = 4,' 'save schema'

$saveOld = @'
                    Role = member.Role,
                    Engagement = member.Engagement,
                    State = member.State
'@
$saveNew = @'
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
$party = Replace-Required $party $saveOld $saveNew 'persist progression fields'
Set-Content -Path $partyPath -Value $party -Encoding UTF8

Write-Host 'v0.2-alpha.3+4 persistence patch applied.'
