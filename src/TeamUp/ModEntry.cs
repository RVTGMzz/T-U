using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace Ronvotri.TeamUp;

public sealed class ModEntry : Mod
{
    private const string SaveDataKey = "team-up-party";

    private ModConfig Config { get; set; } = new();

    private PartyManager Party { get; set; } = null!;

    private FollowService Follow { get; set; } = null!;

    private Action? PendingUiAction { get; set; }

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Config.MaxPartyMembers = Math.Clamp(Config.MaxPartyMembers, 1, 6);
        Config.MaxActiveLinkedCompanions = Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 6);
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

        Monitor.Log("Team Up! v0.1 alpha.4 Party Vault smoke test loaded.", LogLevel.Info);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        PartySaveData? saveData = Helper.Data.ReadSaveData<PartySaveData>(SaveDataKey);
        Party.Load(saveData);

        long recruiterId = Game1.player.UniqueMultiplayerID;
        Party.DeactivateForNewDay(recruiterId);
        Follow.ReleaseAll(Party.Members, Party.CompanionUnits, recruiterId);

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
        SavePartyNow();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PendingUiAction = null;
        Party.Clear();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        RunPendingUiAction();

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

        // Recruitment shortcut only exists while a normal NPC dialogue is already open.
        // E on keyboard / Right Shoulder on controller never doubles as the follower-management toggle.
        if (Game1.dialogueUp && Game1.currentSpeaker is NPC speaker)
        {
            if (Party.Get(speaker.Name, recruiterId) is null
                && IsRecruitableNpc(speaker)
                && Config.RecruitKey.JustPressed())
            {
                Helper.Input.Suppress(e.Button);
                RecruitNpc(speaker);
            }

            return;
        }

        if (!Context.IsPlayerFree || !e.Button.IsActionButton())
            return;

        NPC? npc = FindFacingNpc();
        if (npc is null)
            return;

        // Preserve vanilla gifting. A held gift should go to Stardew, not Team Up management.
        if (Game1.player.ActiveObject is not null)
            return;

        PartyMemberData? member = Party.Get(npc.Name, recruiterId);
        if (member is not null)
        {
            Helper.Input.Suppress(e.Button);
            ShowMemberMenu(npc, member);
            return;
        }

        // Once today's normal dialogue stack is exhausted, talking to the NPC again becomes
        // the recruitment conversation automatically.
        if (IsRecruitableNpc(npc) && npc.CurrentDialogue.Count == 0)
        {
            Helper.Input.Suppress(e.Button);
            ShowRecruitQuestion(npc);
        }
    }

    private void RecruitNpc(NPC npc)
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;
        PartyAddResult result = Party.TryAddMember(npc.Name, recruiterId);

        switch (result)
        {
            case PartyAddResult.Added:
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
        Response[] responses =
        {
            new("Invite", Helper.Translation.Get("recruit.invite")),
            new("Cancel", Helper.Translation.Get("common.cancel"))
        };

        string question = Helper.Translation.Get("recruit.question", new { name = npc.displayName });
        Game1.currentLocation.createQuestionDialogue(question, responses, delegate(Farmer _, string answer)
        {
            if (answer == "Invite")
                RecruitNpc(npc);
        });
    }

    private void ShowMemberMenu(NPC npc, PartyMemberData member)
    {
        string movementLabel = member.State == PartyMemberState.Following
            ? Helper.Translation.Get("member.stand")
            : Helper.Translation.Get("member.follow");

        Response[] responses =
        {
            new("Talk", Helper.Translation.Get("member.talk")),
            new("Movement", movementLabel),
            new("Engagement", Helper.Translation.Get("member.engagement", new { style = GetEngagementLabel(member.Engagement) })),
            new("Vault", Helper.Translation.Get("member.vault")),
            new("Leave", Helper.Translation.Get("member.leave")),
            new("Close", Helper.Translation.Get("common.close"))
        };

        string title = Helper.Translation.Get("member.title", new { name = npc.displayName });
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

                case "Engagement":
                    QueueUi(() => ShowEngagementMenu(npc, member));
                    break;

                case "Vault":
                    QueueUi(() => Ronvotri.TeamUp.Storage.PartyVaultService.Open(
                        Helper.Translation.Get("vault.title")));
                    break;

                case "Leave":
                    QueueUi(() => ShowLeaveQuestion(npc));
                    break;
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
            Game1.drawDialogue(npc);
            return;
        }

        ShowHud(Helper.Translation.Get("member.no-dialogue"));
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady
            || Game1.activeClickableMenu is not DialogueBox dialogueBox
            || Game1.currentSpeaker is not NPC speaker
            || !IsRecruitableNpc(speaker))
        {
            return;
        }

        long recruiterId = Game1.player.UniqueMultiplayerID;
        if (Party.Get(speaker.Name, recruiterId) is not null)
            return;

        string text = Helper.Translation.Get("hint.join");
        Vector2 size = Game1.smallFont.MeasureString(text);

        float x = dialogueBox.xPositionOnScreen + 28f;
        float y = dialogueBox.yPositionOnScreen - size.Y - 6f;
        x = Math.Clamp(x, 12f, Math.Max(12f, Game1.uiViewport.Width - size.X - 12f));
        y = Math.Max(8f, y);

        Vector2 position = new(x, y);
        e.SpriteBatch.DrawString(Game1.smallFont, text, position + new Vector2(2f, 2f), Color.Black * 0.7f);
        e.SpriteBatch.DrawString(Game1.smallFont, text, position, Color.White);
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

    private void SavePartyNow()
    {
        Helper.Data.WriteSaveData(SaveDataKey, Party.CreateSaveData());
    }

    private static bool IsRecruitableNpc(NPC npc)
    {
        return npc is not Pet
            && npc is not Child
            && npc.IsVillager
            && npc.canTalk();
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
