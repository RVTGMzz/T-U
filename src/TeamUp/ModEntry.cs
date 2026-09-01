using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
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

    private ModConfig Config { get; set; } = new();

    private PartyManager Party { get; set; } = null!;

    private FollowService Follow { get; set; } = null!;

    private Action? PendingUiAction { get; set; }

    private PartyRole? CodexOverlayRole { get; set; }

    private string? RecruitHintNpcName { get; set; }

    private bool RecruitConfirmationOpen { get; set; }

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

        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.DayEnding += OnDayEnding;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;

        Monitor.Log("Team Up! v0.1.0-alpha.5.2.1 recruit + vault hotfix loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PartySaveData? saveData = Helper.Data.ReadSaveData<PartySaveData>(SaveDataKey);
        Party.Load(saveData);

        long recruiterId = Game1.player.UniqueMultiplayerID;
        Party.DeactivateForNewDay(recruiterId);
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);
        RecruitHintNpcName = null;
        RecruitConfirmationOpen = false;

        Monitor.Log(
            $"Loaded {Party.Members.Count} Party Member(s) and {Party.CompanionUnits.Count} Companion Unit(s) as inactive roster entries.",
            LogLevel.Debug);
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
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);
        Party.DeactivateForNewDay(recruiterId);
        RecruitHintNpcName = null;
        RecruitConfirmationOpen = false;
        SavePartyNow();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PendingUiAction = null;
        CodexOverlayRole = null;
        RecruitHintNpcName = null;
        RecruitConfirmationOpen = false;
        Party.Clear();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        RunPendingUiAction();

        if (Game1.activeClickableMenu is null && !Game1.dialogueUp && PendingUiAction is null)
        {
            CodexOverlayRole = null;
            RecruitHintNpcName = null;
            RecruitConfirmationOpen = false;
        }

        if (!e.IsMultipleOf(4))
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

        long recruiterId = Game1.player.UniqueMultiplayerID;

        // Recruitment shortcut exists only while normal NPC dialogue is already open.
        // Pressing the shortcut now opens a confirmation step; it never recruits instantly.
        if (Game1.dialogueUp)
        {
            NPC? speaker = ResolveRecruitmentSpeaker();
            if (!RecruitConfirmationOpen
                && speaker is not null
                && Party.Get(speaker.Name, recruiterId) is null
                && IsRecruitableNpc(speaker)
                && Config.RecruitKey.JustPressed())
            {
                Helper.Input.Suppress(e.Button);
                RecruitHintNpcName = null;
                ShowRecruitQuestion(speaker);
            }

            return;
        }

        // PC shortcut for the in-game Codex. Controller users can open Codex from any party member menu.
        if (Context.IsPlayerFree && Config.PartyMenuKey.JustPressed())
        {
            Helper.Input.Suppress(e.Button);
            RecruitHintNpcName = null;
            ShowCodexRoot();
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

        // Preserve vanilla gifting. A held gift belongs to Stardew's normal NPC interaction.
        if (Game1.player.ActiveObject is not null)
        {
            RecruitHintNpcName = null;
            return;
        }

        PartyMemberData? member = Party.Get(npc.Name, recruiterId);
        if (member is not null)
        {
            Helper.Input.Suppress(e.Button);
            RecruitHintNpcName = null;
            ShowMemberMenu(npc, member);
            return;
        }

        if (!IsRecruitableNpc(npc))
        {
            RecruitHintNpcName = null;
            return;
        }

        // Remember the manually-interacted NPC before vanilla creates its DialogueBox.
        RecruitHintNpcName = npc.Name;

        // If today's normal dialogue stack is exhausted, talking again becomes Team Up recruitment.
        if (npc.CurrentDialogue.Count == 0)
        {
            Helper.Input.Suppress(e.Button);
            RecruitHintNpcName = null;
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
                if (profile is not null)
                {
                    Party.SetRole(npc.Name, recruiterId, profile.PrimaryRole);
                    Party.SetEngagementStyle(npc.Name, recruiterId, profile.RecommendedEngagement);
                }

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
        RecruitHintNpcName = null;
        RecruitConfirmationOpen = true;

        Response[] responses =
        {
            new("Invite", Helper.Translation.Get("recruit.invite")),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = Helper.Translation.Get("recruit.question", new { name = npc.displayName });
        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            RecruitConfirmationOpen = false;
            if (answer == "Invite")
                RecruitNpc(npc);
        });
    }

    private void ShowMemberMenu(NPC npc, PartyMemberData member)
    {
        RecruitHintNpcName = null;

        string movementLabel = member.State == PartyMemberState.Following
            ? Helper.Translation.Get("member.stand")
            : Helper.Translation.Get("member.follow");

        Response[] responses =
        {
            new("Talk", Helper.Translation.Get("member.talk")),
            new("Movement", movementLabel),
            new("Role", Helper.Translation.Get("member.role", new { role = GetRoleLabel(member.Role) })),
            new("Engagement", Helper.Translation.Get("member.engagement", new { style = GetEngagementLabel(member.Engagement) })),
            new("Vault", Helper.Translation.Get("member.vault")),
            new("Codex", Helper.Translation.Get("member.codex")),
            new("Leave", Helper.Translation.Get("member.leave")),
            new("Close", Helper.Translation.Get("common.close"))
        };

        string title = Helper.Translation.Get("member.title", new
        {
            name = npc.displayName,
            role = GetRoleLabel(member.Role)
        });

        Game1.currentLocation.createQuestionDialogue(title, responses, delegate(Farmer _, string answer)
        {
            switch (answer)
            {
                case "Talk":
                    QueueUi(() => ShowVanillaDialogue(npc));
                    break;

                case "Movement":
                    ToggleMovement(npc, member);
                    break;

                case "Role":
                    QueueUi(() => ShowRoleMenu(npc, member));
                    break;

                case "Engagement":
                    QueueUi(() => ShowEngagementMenu(npc, member));
                    break;

                case "Vault":
                    QueueUi(OpenPartyVault);
                    break;

                case "Codex":
                    QueueUi(ShowCodexRoot);
                    break;

                case "Leave":
                    QueueUi(() => ShowLeaveQuestion(npc));
                    break;
            }
        });
    }

    private void ShowRoleMenu(NPC npc, PartyMemberData member)
    {
        NpcCombatProfile? profile = NpcProfileCatalog.Get(npc.Name);
        PartyRole[] roles =
        {
            PartyRole.Tank,
            PartyRole.Damage,
            PartyRole.Support,
            PartyRole.Healer,
            PartyRole.Control
        };

        List<Response> responses = roles
            .Select(role => new Response(role.ToString(), GetRoleOptionLabel(role, profile)))
            .ToList();
        responses.Add(new Response("Cancel", Helper.Translation.Get("common.cancel")));

        string question = Helper.Translation.Get("role.question", new
        {
            name = npc.displayName,
            role = GetRoleLabel(member.Role)
        });

        Game1.currentLocation.createQuestionDialogue(question, responses.ToArray(), delegate(Farmer _, string answer)
        {
            if (!Enum.TryParse(answer, out PartyRole role) || role == PartyRole.Unassigned)
                return;

            if (Party.SetRole(npc.Name, Game1.player.UniqueMultiplayerID, role))
            {
                SavePartyNow();
                ShowHud(Helper.Translation.Get("role.changed", new
                {
                    name = npc.displayName,
                    role = GetRoleLabel(role)
                }));
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
        Response[] responses =
        {
            new(nameof(EngagementStyle.Passive), Helper.Translation.Get("engagement.passive")),
            new(nameof(EngagementStyle.Cautious), Helper.Translation.Get("engagement.cautious")),
            new(nameof(EngagementStyle.Balanced), Helper.Translation.Get("engagement.balanced")),
            new(nameof(EngagementStyle.Aggressive), Helper.Translation.Get("engagement.aggressive")),
            new(nameof(EngagementStyle.Reckless), Helper.Translation.Get("engagement.reckless")),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = Helper.Translation.Get("engagement.question", new
        {
            name = npc.displayName,
            style = GetEngagementLabel(member.Engagement)
        });

        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            if (!Enum.TryParse(answer, out EngagementStyle style))
                return;

            if (Party.SetEngagementStyle(npc.Name, Game1.player.UniqueMultiplayerID, style))
            {
                SavePartyNow();
                ShowHud(Helper.Translation.Get("engagement.changed", new
                {
                    name = npc.displayName,
                    style = GetEngagementLabel(style)
                }));
            }
        });
    }

    private void ShowCodexRoot()
    {
        RecruitHintNpcName = null;
        CodexOverlayRole = null;

        Response[] responses =
        {
            new("Characters", Helper.Translation.Get("codex.characters")),
            new("Roles", Helper.Translation.Get("codex.roles")),
            new("Vault", Helper.Translation.Get("member.vault")),
            new("Close", Helper.Translation.Get("common.close"))
        };

        Game1.currentLocation.createQuestionDialogue(
            Helper.Translation.Get("codex.title"),
            responses,
            delegate(Farmer _, string answer)
            {
                switch (answer)
                {
                    case "Characters":
                        QueueUi(ShowCodexCharacters);
                        break;
                    case "Roles":
                        QueueUi(ShowCodexRoles);
                        break;
                    case "Vault":
                        QueueUi(OpenPartyVault);
                        break;
                }
            });
    }

    private void ShowCodexCharacters()
    {
        CodexOverlayRole = null;

        List<Response> responses = NpcProfileCatalog.All
            .Select(profile => new Response(profile.CharacterName, GetNpcDisplayName(profile.CharacterName)))
            .ToList();
        responses.Add(new Response("Back", Helper.Translation.Get("common.back")));

        Game1.currentLocation.createQuestionDialogue(
            Helper.Translation.Get("codex.characters-title"),
            responses.ToArray(),
            delegate(Farmer _, string answer)
            {
                if (answer == "Back")
                    QueueUi(ShowCodexRoot);
                else
                    QueueUi(() => ShowCodexProfile(answer));
            });
    }

    private void ShowCodexProfile(string characterName)
    {
        NpcCombatProfile? profile = NpcProfileCatalog.Get(characterName);
        if (profile is null)
        {
            QueueUi(ShowCodexCharacters);
            return;
        }

        CodexOverlayRole = null;

        Game1.activeClickableMenu = new CharacterProfileMenu(
            profile,
            GetNpcDisplayName(profile.CharacterName),
            GetEngagementLabel(profile.RecommendedEngagement),
            Helper.Translation.Get(profile.PassiveKey),
            Helper.Translation.Get(profile.AbilityKey),
            GetRoleLabel,
            Helper.Translation,
            ShowCodexCharacters);
    }

    private void ShowCodexRoles()
    {
        PartyRole[] roles =
        {
            PartyRole.Tank,
            PartyRole.Damage,
            PartyRole.Support,
            PartyRole.Healer,
            PartyRole.Control
        };

        List<Response> responses = roles
            .Select(role => new Response(role.ToString(), GetRoleLabel(role)))
            .ToList();
        responses.Add(new Response("Back", Helper.Translation.Get("common.back")));

        Game1.currentLocation.createQuestionDialogue(
            Helper.Translation.Get("codex.roles-title"),
            responses.ToArray(),
            delegate(Farmer _, string answer)
            {
                if (answer == "Back")
                {
                    QueueUi(ShowCodexRoot);
                    return;
                }

                if (Enum.TryParse(answer, out PartyRole role))
                    QueueUi(() => ShowCodexRole(role));
            });
    }

    private void ShowCodexRole(PartyRole role)
    {
        CodexOverlayRole = role;

        string text = Helper.Translation.Get("codex.role-profile", new
        {
            role = GetRoleLabel(role),
            description = Helper.Translation.Get(GetRoleDescriptionKey(role))
        });

        Response[] responses =
        {
            new("Back", Helper.Translation.Get("common.back"))
        };

        Game1.currentLocation.createQuestionDialogue(text, responses, delegate(Farmer _, string answer)
        {
            CodexOverlayRole = null;
            if (answer == "Back")
                QueueUi(ShowCodexRoles);
        });
    }

    private void ShowLeaveQuestion(NPC npc)
    {
        Response[] responses =
        {
            new("Leave", Helper.Translation.Get("member.leave-confirm")),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = Helper.Translation.Get("member.leave-question", new { name = npc.displayName });
        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            if (answer != "Leave")
                return;

            long recruiterId = Game1.player.UniqueMultiplayerID;
            Follow.ReleaseToVanilla(npc);
            if (Party.Remove(npc.Name, recruiterId))
            {
                SavePartyNow();
                ShowHud(Helper.Translation.Get("member.left", new { name = npc.displayName }));
            }
        });
    }

    private void ShowVanillaDialogue(NPC npc)
    {
        if (npc.CurrentDialogue.Count > 0)
        {
            RecruitHintNpcName = null;
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

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not DialogueBox dialogueBox)
            return;

        if (CodexOverlayRole is PartyRole codexRole)
        {
            DrawCodexRoleIcon(e, dialogueBox, codexRole);
            return;
        }

        if (RecruitConfirmationOpen)
            return;

        NPC? speaker = ResolveRecruitmentSpeaker();
        if (speaker is null || !IsRecruitableNpc(speaker))
            return;

        long recruiterId = Game1.player.UniqueMultiplayerID;
        if (Party.Get(speaker.Name, recruiterId) is not null)
            return;

        DrawRecruitHint(e, dialogueBox, speaker);
    }

    private void DrawRecruitHint(RenderedActiveMenuEventArgs e, DialogueBox dialogueBox, NPC speaker)
    {
        string hint = Helper.Translation.Get("hint.join");
        NpcCombatProfile? profile = NpcProfileCatalog.Get(speaker.Name);
        string? recommendation = profile is null
            ? null
            : Helper.Translation.Get("hint.recommended", new
            {
                primary = GetRoleLabel(profile.PrimaryRole),
                secondary = GetRoleLabel(profile.SecondaryRole)
            });

        Vector2 hintSize = Game1.smallFont.MeasureString(hint);
        Vector2 recommendationSize = recommendation is null
            ? Vector2.Zero
            : Game1.smallFont.MeasureString(recommendation);

        int iconSpace = profile is null ? 0 : 24;
        int boxWidth = (int)Math.Ceiling(Math.Max(hintSize.X, recommendationSize.X + iconSpace)) + 28;
        int boxHeight = profile is null ? 38 : 63;

        // DialogueBox x/y fields are unreliable for portrait dialogue in Stardew 1.6.
        // Anchor the hint from the actual viewport + menu dimensions so it sits on the chat frame.
        int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);
        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 24);
        int x = dialogueLeft + 18;
        int y = dialogueTop - boxHeight + 4;

        x = Math.Clamp(x, 8, Math.Max(8, Game1.uiViewport.Width - boxWidth - 8));
        y = Math.Clamp(y, 6, Math.Max(6, Game1.uiViewport.Height - boxHeight - 6));

        Color background = new Color(43, 29, 22) * 0.92f;
        Color border = new Color(219, 165, 91) * 0.95f;

        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(x, y, boxWidth, boxHeight), background);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(x, y, boxWidth, 2), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(x, y + boxHeight - 2, boxWidth, 2), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(x, y, 2, boxHeight), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(x + boxWidth - 2, y, 2, boxHeight), border);

        DrawShadowedText(e, hint, new Vector2(x + 14, y + 8));

        if (profile is null || recommendation is null)
            return;

        RoleIconRenderer.Draw(e.SpriteBatch, profile.PrimaryRole, new Vector2(x + 14, y + 35), pixelSize: 2);
        DrawShadowedText(e, recommendation, new Vector2(x + 38, y + 34));
    }

    private static void DrawCodexRoleIcon(RenderedActiveMenuEventArgs e, DialogueBox dialogueBox, PartyRole role)
    {
        float x = dialogueBox.xPositionOnScreen + dialogueBox.width - 70f;
        float y = dialogueBox.yPositionOnScreen + 22f;
        RoleIconRenderer.Draw(e.SpriteBatch, role, new Vector2(x, y), pixelSize: 4);
    }

    private static void DrawShadowedText(RenderedActiveMenuEventArgs e, string text, Vector2 position)
    {
        e.SpriteBatch.DrawString(Game1.smallFont, text, position + new Vector2(2f, 2f), Color.Black * 0.7f);
        e.SpriteBatch.DrawString(Game1.smallFont, text, position, Color.White);
    }

    private NPC? ResolveRecruitmentSpeaker()
    {
        if (Game1.currentSpeaker is NPC currentSpeaker)
            return currentSpeaker;

        if (string.IsNullOrWhiteSpace(RecruitHintNpcName))
            return null;

        return Game1.getCharacterFromName(RecruitHintNpcName);
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

    private static string GetRoleDescriptionKey(PartyRole role)
    {
        return role switch
        {
            PartyRole.Tank => "codex.role.tank",
            PartyRole.Damage => "codex.role.damage",
            PartyRole.Support => "codex.role.support",
            PartyRole.Healer => "codex.role.healer",
            PartyRole.Control => "codex.role.control",
            _ => "codex.role.unassigned"
        };
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
