using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Debugging;
using Ronvotri.TeamUp.Following;
using Ronvotri.TeamUp.Storage;
using Ronvotri.TeamUp.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp;

public sealed class ModEntry : Mod
{
    private const string SaveDataKey = "team-up-party";
    private const float DialogueHintScale = 1.5f;

    private ModConfig Config { get; set; } = new();
    private PartyManager Party { get; set; } = null!;
    private FollowService Follow { get; set; } = null!;
    private ProgressionService Progression { get; set; } = null!;
    private EquipmentService Equipment { get; set; } = null!;
    private CombatService Combat { get; set; } = null!;
    private Alpha6CombatPolishService Alpha6Polish { get; set; } = null!;
    private CharacterSkillIdentityService SkillIdentity { get; set; } = null!;
    private TeamUpDebugService DebugTools { get; set; } = null!;
    private Action? PendingUiAction { get; set; }
    private string? RecruitHintNpcName { get; set; }
    private bool PartyActionConfirmationOpen { get; set; }
    private Rectangle SocialCodexButtonBounds { get; set; }

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Config.MaxPartyMembers = Math.Clamp(Config.MaxPartyMembers, 1, 6);
        Config.MaxActiveLinkedCompanions = Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 6);
        Config.SpecialCompanionNpcNames ??= new List<string>();
        helper.WriteConfig(Config);

        Party = new PartyManager(
            () => Config.MaxPartyMembers,
            () => Config.AllowLinkedCompanions ? Config.MaxActiveLinkedCompanions : 0);
        Follow = new FollowService(Monitor);
        Progression = new ProgressionService();
        Equipment = new EquipmentService();
        Combat = new CombatService(Monitor, Follow, Progression);
        Alpha6Polish = new Alpha6CombatPolishService(Monitor, Progression);
        SkillIdentity = new CharacterSkillIdentityService(Progression);
        DebugTools = new TeamUpDebugService(
            Helper,
            Monitor,
            Party,
            Progression,
            Follow,
            Combat,
            Alpha6Polish,
            SavePartyNow);
        DebugTools.RegisterCommands();
        Monitor.Log("Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.4.0", LogLevel.Info);

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Display.RenderingActiveMenu += OnRenderingActiveMenu;
        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;

        Monitor.Log("Team Up! v0.2.0-alpha.6.4.0 character skill identity loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PartySaveData? saveData = Helper.Data.ReadSaveData<PartySaveData>(SaveDataKey);
        Party.Load(saveData);
        Combat.Clear();
        Alpha6Polish.Clear();
        SkillIdentity.Clear();
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

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        SavePartyNow();
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        long recruiterId = Game1.player.UniqueMultiplayerID;
        Combat.Clear();
        Alpha6Polish.Clear();
        SkillIdentity.Clear();
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);
        Progression.ResetForNewDay(Party.Members);
        Party.DeactivateForNewDay(recruiterId);
        RecruitHintNpcName = null;
        PartyActionConfirmationOpen = false;
        SavePartyNow();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PendingUiAction = null;
        RecruitHintNpcName = null;
        PartyActionConfirmationOpen = false;
        SocialCodexButtonBounds = Rectangle.Empty;
        Combat.Clear();
        Alpha6Polish.Clear();
        SkillIdentity.Clear();
        Party.Clear();
    }

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

        SkillIdentity.Update(Party.Members, Game1.player.UniqueMultiplayerID);
        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);
        Alpha6Polish.Update(Party.Members, Game1.player.UniqueMultiplayerID);

        if (!e.IsMultipleOf(2))
            return;

        Follow.Update(
            Party.Members,
            Party.CompanionUnits,
            Game1.player.UniqueMultiplayerID);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == GameMenu.socialTab)
        {
            bool clicked = e.Button == SButton.MouseLeft
                && SocialCodexButtonBounds.Contains(Game1.getMouseX(), Game1.getMouseY());
            bool controllerOpen = e.Button == Buttons.X.ToSButton();

            if (clicked || controllerOpen || Config.PartyMenuKey.JustPressed())
            {
                Helper.Input.Suppress(e.Button);
                Game1.activeClickableMenu = null;
                OpenCodexBrowser();
                return;
            }
        }

        if (Game1.activeClickableMenu is CharacterProfileMenu or CodexBrowserMenu)
            return;

        long recruiterId = Game1.player.UniqueMultiplayerID;

        if (Game1.dialogueUp)
        {
            NPC? speaker = ResolveDialogueSpeaker();
            if (speaker is null || PartyActionConfirmationOpen)
                return;

            if (Config.ProfileKey.JustPressed())
            {
                Helper.Input.Suppress(e.Button);
                OpenProfileFromDialogue(speaker);
                return;
            }

            if (Config.RecruitKey.JustPressed())
            {
                PartyMemberData? member = Party.Get(speaker.Name, recruiterId);

                if (member is not null)
                {
                    Helper.Input.Suppress(e.Button);
                    ShowLeaveQuestion(speaker);
                    return;
                }

                if (IsRecruitableNpc(speaker))
                {
                    Helper.Input.Suppress(e.Button);
                    ShowRecruitQuestion(speaker);
                    return;
                }
            }

            return;
        }

        if (Context.IsPlayerFree && Config.PartyMenuKey.JustPressed())
        {
            Helper.Input.Suppress(e.Button);
            RecruitHintNpcName = null;
            OpenCodexBrowser();
            return;
        }

        if (!Context.IsPlayerFree || !e.Button.IsActionButton())
            return;

        NPC? npc = FindFacingNpc();
        if (npc is null)
        {
            RecruitHintNpcName = null;
            return;
        }

        if (Game1.player.ActiveObject is not null)
        {
            RecruitHintNpcName = null;
            return;
        }

        PartyMemberData? memberData = Party.Get(npc.Name, recruiterId);
        if (memberData is not null)
        {
            Helper.Input.Suppress(e.Button);
            RecruitHintNpcName = null;
            ShowMemberMenu(npc, memberData);
            return;
        }

        if (!IsRecruitableNpc(npc))
        {
            RecruitHintNpcName = npc.Name;
            return;
        }

        RecruitHintNpcName = npc.Name;

        if (npc.CurrentDialogue.Count == 0)
        {
            Helper.Input.Suppress(e.Button);
            ShowRecruitQuestion(npc);
        }
    }

    private void RecruitNpc(NPC npc)
    {
        if (!IsRecruitableNpc(npc))
        {
            ShowHud(Helper.Translation.Get("party.invalid"), error: true);
            return;
        }

        long recruiterId = Game1.player.UniqueMultiplayerID;
        PartyAddResult result = Party.TryAddMember(npc.Name, recruiterId);

        switch (result)
        {
            case PartyAddResult.Added:
                NpcCombatProfile? profile = NpcProfileCatalog.Get(npc.Name);
                if (profile is not null && profile.PrimaryRole != PartyRole.Unassigned)
                {
                    Party.SetRole(npc.Name, recruiterId, profile.PrimaryRole);
                    Party.SetEngagementStyle(npc.Name, recruiterId, profile.RecommendedEngagement);
                }

                PartyMemberData? recruitedMember = Party.Get(npc.Name, recruiterId);
                if (recruitedMember is not null)
                    Progression.NormalizeMember(recruitedMember);

                Follow.TakePartyControl(npc);
                SavePartyNow();
                ShowHud(Helper.Translation.Get("party.added", new { name = npc.displayName }));
                Monitor.Log($"Added {npc.Name} to Team Up! Main Party.", LogLevel.Info);
                break;

            case PartyAddResult.PartyFull:
                ShowHud(Helper.Translation.Get("party.full", new { max = Config.MaxPartyMembers }), error: true);
                break;

            case PartyAddResult.AlreadyInParty:
                ShowHud(Helper.Translation.Get("party.already-member", new { name = npc.displayName }), error: true);
                break;

            default:
                ShowHud(Helper.Translation.Get("party.invalid"), error: true);
                break;
        }
    }

    private void ShowRecruitQuestion(NPC npc)
    {
        RecruitHintNpcName = npc.Name;
        PartyActionConfirmationOpen = true;

        Response[] responses =
        {
            new("Invite", Helper.Translation.Get("recruit.invite")),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = Helper.Translation.Get("recruit.question", new { name = npc.displayName });
        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            PartyActionConfirmationOpen = false;
            if (answer == "Invite")
                RecruitNpc(npc);
        });
    }

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
            if (leavingMember is not null && !Equipment.TryReturnAll(leavingMember, out string _returnMessage))
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

    private void ToggleMovement(NPC npc, PartyMemberData member)
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;

        if (member.State == PartyMemberState.Following)
        {
            Party.SetState(npc.Name, recruiterId, PartyMemberState.Waiting);
            Follow.HoldPosition(npc);
            ShowHud(Helper.Translation.Get("party.wait", new { name = npc.displayName }));
        }
        else
        {
            Party.SetState(npc.Name, recruiterId, PartyMemberState.Following);
            Follow.TakePartyControl(npc);
            ShowHud(Helper.Translation.Get("party.resume", new { name = npc.displayName }));
        }

        SavePartyNow();
    }

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

    private void ShowEquipmentMenu(NPC npc, PartyMemberData member)
    {
        RecruitHintNpcName = null;
        Game1.activeClickableMenu = new EquipmentMenu(
            npc,
            member,
            Equipment,
            Progression,
            Helper.Translation,
            SavePartyNow,
            () => ShowMemberMenu(npc, member));
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
        string current = equipped?.DisplayName ?? Helper.Translation.Get("equipment.empty").ToString();
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
                if (Equipment.TryUnequip(member, slot, out string _unequipMessage))
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

            if (Equipment.TryEquip(member, slot, inventoryIndex, out string _equipMessage))
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

    private void OpenCodexBrowser()
    {
        OpenCodexBrowser(null);
    }

    private void OpenCodexBrowser(Action? onClose)
    {
        RecruitHintNpcName = null;

        Game1.activeClickableMenu = new CodexBrowserMenu(
            NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry),
            GetNpcDisplayName,
            GetRoleLabel,
            IsCharacterInParty,
            CanRecruitCharacter,
            Helper.Translation,
            (characterName, browser) => OpenCharacterProfile(characterName, () => Game1.activeClickableMenu = browser, () => Game1.activeClickableMenu = browser),
            onClose ?? (() => { }));
    }

    private void ShowVanillaDialogue(NPC npc)
    {
        if (npc.CurrentDialogue.Count > 0)
        {
            RecruitHintNpcName = npc.Name;
            Game1.drawDialogue(npc);
            return;
        }

        ShowHud(Helper.Translation.Get("member.no-dialogue"));
    }

    private void OpenPartyVault()
    {
        PartyVaultService.Open(
            Helper.Translation.Get("vault.title"),
            Helper.Translation.Get("vault.subtitle"),
            Helper.Translation.Get("vault.slots"),
            Helper.Translation.Get("vault.categories"));
    }

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
    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.currentTab == GameMenu.socialTab)
        {
            DrawSocialCodexEntry(e, gameMenu);
            return;
        }

        if (Game1.activeClickableMenu is not DialogueBox dialogueBox || PartyActionConfirmationOpen)
            return;

        NPC? speaker = ResolveDialogueSpeaker();
        if (speaker is null)
            return;

        DrawDialogueActions(e, dialogueBox, speaker);
    }

    private void DrawDialogueActions(RenderedActiveMenuEventArgs e, DialogueBox dialogueBox, NPC speaker)
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;
        PartyMemberData? member = Party.Get(speaker.Name, recruiterId);

        string leftText = Helper.Translation.Get("hint.profile").ToString();
        string? rightText = member is not null
            ? Helper.Translation.Get("hint.leave").ToString()
            : IsRecruitableNpc(speaker)
                ? Helper.Translation.Get("hint.recruit").ToString()
                : null;

        const int tagHeight = 50;

        // DialogueBox exposes the actual x/y used by Stardew. Use those directly.
        // Alpha.5.3.2 estimated top with -24 while vanilla uses -64, which pulled
        // these tags about 40px down into the dialogue frame.
        int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);
        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);
        int y = Math.Max(6, dialogueTop - tagHeight - 10);

        DrawDialogueTag(e, leftText, dialogueLeft + 18, y, tagHeight);

        if (rightText is not null)
        {
            Vector2 size = Game1.smallFont.MeasureString(rightText) * DialogueHintScale;
            int tagWidth = (int)Math.Ceiling(size.X) + 30;
            int rightX = dialogueLeft + dialogueBox.width - tagWidth - 18;
            DrawDialogueTag(e, rightText, rightX, y, tagHeight);
        }
    }

    private static void DrawDialogueTag(RenderedActiveMenuEventArgs e, string text, int x, int y, int height)
    {
        Vector2 size = Game1.smallFont.MeasureString(text) * DialogueHintScale;
        int width = (int)Math.Ceiling(size.X) + 30;
        Rectangle bounds = new(x, y, width, height);

        Color background = new Color(43, 29, 22) * 0.94f;
        Color border = new Color(219, 165, 91) * 0.98f;

        e.SpriteBatch.Draw(Game1.staminaRect, bounds, background);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), border);

        Vector2 textSize = Game1.smallFont.MeasureString(text) * DialogueHintScale;
        Vector2 position = new(
            bounds.X + 15,
            bounds.Center.Y - textSize.Y / 2f);
        Vector2 shadowOffset = new(2.5f, 2.5f);

        e.SpriteBatch.DrawString(
            Game1.smallFont,
            text,
            position + shadowOffset,
            Color.Black * 0.7f,
            0f,
            Vector2.Zero,
            DialogueHintScale,
            SpriteEffects.None,
            1f);
        e.SpriteBatch.DrawString(
            Game1.smallFont,
            text,
            position,
            Color.White,
            0f,
            Vector2.Zero,
            DialogueHintScale,
            SpriteEffects.None,
            1f);
    }

    private void DrawSocialCodexEntry(RenderedActiveMenuEventArgs e, GameMenu gameMenu)
    {
        const int preferredWidth = 340;
        const int height = 50;
        int width = Math.Min(preferredWidth, Math.Max(220, gameMenu.width - 70));
        int x = gameMenu.xPositionOnScreen + gameMenu.width - width - 34;
        int y = gameMenu.yPositionOnScreen + gameMenu.height - height - 30;
        SocialCodexButtonBounds = new Rectangle(x, y, width, height);

        IClickableMenu.drawTextureBox(
            e.SpriteBatch,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            x,
            y,
            width,
            height,
            Color.White,
            0.8f,
            true);

        DrawFitText(
            e.SpriteBatch,
            Game1.smallFont,
            Helper.Translation.Get("social.codex-entry"),
            new Rectangle(x + 12, y + 6, width - 24, height - 12),
            Game1.textColor,
            1.08f);
    }

    private static void DrawFitText(SpriteBatch b, SpriteFont font, string text, Rectangle bounds, Color color, float preferredScale)
    {
        Vector2 measured = font.MeasureString(text);
        float scale = measured.X <= 0f
            ? preferredScale
            : Math.Min(preferredScale, bounds.Width / measured.X);
        scale = Math.Max(0.72f, scale);
        Vector2 position = new(
            bounds.Center.X - measured.X * scale / 2f,
            bounds.Center.Y - measured.Y * scale / 2f);
        b.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

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

    private static void RestoreMenu(IClickableMenu? menu)
    {
        if (menu is not null)
            Game1.activeClickableMenu = menu;
    }

    private void QueueUi(Action action)
    {
        PendingUiAction = action;
    }

    private void RunPendingUiAction()
    {
        if (PendingUiAction is null || Game1.activeClickableMenu is not null || Game1.dialogueUp)
            return;

        Action action = PendingUiAction;
        PendingUiAction = null;
        action();
    }

    private string GetProfileStatus(string characterName)
    {
        PartyMemberData? member = Party.Get(characterName, Game1.player.UniqueMultiplayerID);
        if (member is not null)
        {
            return member.State switch
            {
                PartyMemberState.Following => Helper.Translation.Get("profile.status-following"),
                PartyMemberState.Waiting => Helper.Translation.Get("profile.status-waiting"),
                _ => Helper.Translation.Get("profile.status-in-party")
            };
        }

        NPC? npc = Game1.getCharacterFromName(characterName);
        if (npc is not null && IsRecruitableNpc(npc))
            return Helper.Translation.Get("profile.status-recruitable");

        return Helper.Translation.Get("profile.status-special");
    }

    private bool IsCharacterInParty(string characterName)
    {
        return Party.Get(characterName, Game1.player.UniqueMultiplayerID) is not null;
    }

    private bool CanRecruitCharacter(string characterName)
    {
        NPC? npc = Game1.getCharacterFromName(characterName);
        return npc is not null && IsRecruitableNpc(npc);
    }

    private string GetRoleOptionLabel(PartyRole role, NpcCombatProfile? profile)
    {
        string label = GetRoleLabel(role);
        if (profile is null)
            return label;

        if (role == profile.PrimaryRole)
            return Helper.Translation.Get("role.option-recommended", new { role = label });

        if (role == profile.SecondaryRole)
            return Helper.Translation.Get("role.option-alternate", new { role = label });

        return label;
    }

    private string GetRoleLabel(PartyRole role)
    {
        string key = role switch
        {
            PartyRole.Tank => "role.tank",
            PartyRole.Damage => "role.damage",
            PartyRole.Support => "role.support",
            PartyRole.Healer => "role.healer",
            PartyRole.Control => "role.control",
            _ => "role.unassigned"
        };

        return Helper.Translation.Get(key);
    }

    private string GetEngagementLabel(EngagementStyle style)
    {
        string key = style switch
        {
            EngagementStyle.Passive => "engagement.passive",
            EngagementStyle.Cautious => "engagement.cautious",
            EngagementStyle.Aggressive => "engagement.aggressive",
            EngagementStyle.Reckless => "engagement.reckless",
            _ => "engagement.balanced"
        };

        return Helper.Translation.Get(key);
    }

    private static string GetNpcDisplayName(string characterName)
    {
        return Game1.getCharacterFromName(characterName)?.displayName ?? characterName;
    }

    private void SavePartyNow()
    {
        Helper.Data.WriteSaveData(SaveDataKey, Party.CreateSaveData());
    }

    private bool IsRecruitableNpc(NPC npc)
    {
        return CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames);
    }

    private static NPC? FindFacingNpc()
    {
        Vector2 targetTile = Game1.player.GetGrabTile();

        return Game1.currentLocation.characters
            .OfType<NPC>()
            .OrderBy(npc => Vector2.DistanceSquared(npc.Tile, targetTile))
            .FirstOrDefault(npc => Vector2.DistanceSquared(npc.Tile, targetTile) <= 1f);
    }

    private static void ShowHud(string message, bool error = false)
    {
        int type = error ? HUDMessage.error_type : HUDMessage.newQuest_type;
        Game1.addHUDMessage(new HUDMessage(message, type));
    }
}
