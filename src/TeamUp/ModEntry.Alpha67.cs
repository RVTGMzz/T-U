using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha67BanterRegistered;
    private PartyBanterService? PartyBanterAlpha67;

    private void EnsureAlpha67BanterRegistered()
    {
        if (Alpha67BanterRegistered)
            return;

        Alpha67BanterRegistered = true;
        EnforceAlpha67PeopleCap();

        PartyBanterAlpha67 = new PartyBanterService(
            Monitor,
            Party,
            Progression,
            () => Helper.Translation.Locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase),
            () => Config.EnablePartyBanter,
            () => Config.EnableMimiShippingBanter);

        Helper.Events.GameLoop.UpdateTicked += OnAlpha67BanterUpdateTicked;
        Helper.Events.GameLoop.DayEnding += OnAlpha67BanterDayEnding;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha67BanterReturnedToTitle;

        // Replace the old Pokemon-only hint with the provider-neutral term used by Team Up.
        Helper.Events.Display.RenderedActiveMenu -= OnAlpha6615RenderedActiveMenu;
        Helper.Events.Display.RenderedActiveMenu += OnAlpha67RenderedActiveMenu;

        Helper.ConsoleCommands.Add(
            "teamup_banter",
            "Party banter controls: status | now | ship.",
            OnAlpha67BanterCommand);

        Monitor.Log(
            "Team Up Alpha 6.7.0 Party Banter enabled: 5-person formation, 2 companion slots, personality/context chatter, MiMi Shipper trait.",
            LogLevel.Info);
    }

    private void EnforceAlpha67PeopleCap()
    {
        // Final formation rule: five people TOTAL. Farmers consume people slots, therefore normal
        // single-player is exactly Farmer + at most four active NPC Party Members.
        if (Config.MaxPartyMembers != 5)
        {
            Config.MaxPartyMembers = 5;
            Helper.WriteConfig(Config);
        }

        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        IReadOnlyList<PartyMemberData> deactivated = Party.EnforceSharedPeopleCapacity(GetOnlineFarmerIds().ToArray());
        if (deactivated.Count == 0)
            return;

        foreach (PartyMemberData member in deactivated)
        {
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is not null)
                Follow.ReleaseToVanillaAndResumeSchedule(npc);
        }

        SavePartyNow();
        BroadcastPartySnapshot();
        Monitor.Log($"Alpha 6.7.0 enforced five-person formation; deactivated={deactivated.Count} excess/offline NPC member(s).", LogLevel.Debug);
    }

    private void OnAlpha67BanterUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || !e.IsMultipleOf(30))
            return;

        if (e.IsMultipleOf(60))
            EnforceAlpha67PeopleCap();

        PartyBanterAlpha67?.Update();
    }

    private void OnAlpha67BanterDayEnding(object? sender, DayEndingEventArgs e)
        => PartyBanterAlpha67?.Reset();

    private void OnAlpha67BanterReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        => PartyBanterAlpha67?.Reset();

    private void OnAlpha67BanterCommand(string command, string[] args)
    {
        string action = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "status";
        if (PartyBanterAlpha67 is null)
        {
            Monitor.Log("Party Banter is not initialized.", LogLevel.Info);
            return;
        }

        switch (action)
        {
            case "status":
                int people = Context.IsWorldReady ? Party.GetSharedPeopleCount(GetOnlineFarmerIds().ToArray()) : 0;
                Monitor.Log($"Team Up Party Banter: {PartyBanterAlpha67.DescribeStatus()}; people={people}/5, companionCap={Config.MaxActiveLinkedCompanions}/2.", LogLevel.Info);
                break;

            case "now":
                Monitor.Log(
                    PartyBanterAlpha67.ForceAmbient()
                        ? "Forced one ambient Party Banter exchange."
                        : "No eligible NPC pair is available for ambient banter.",
                    LogLevel.Info);
                break;

            case "ship":
                Monitor.Log(
                    PartyBanterAlpha67.ForceMimiShipping()
                        ? "Forced one MiMi Shipper exchange."
                        : "MiMi + two positively identified male party NPCs are required.",
                    LogLevel.Info);
                break;

            default:
                Monitor.Log("Usage: teamup_banter <status|now|ship>", LogLevel.Info);
                break;
        }
    }

    private void OnAlpha67RenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!Context.IsWorldReady
            || PartyActionConfirmationOpen
            || Game1.activeClickableMenu is not DialogueBox dialogueBox)
        {
            return;
        }

        NPC? speaker = ResolveDialogueSpeaker();
        if (speaker is null)
            return;

        PartyMemberData? member = Party.Get(speaker.Name, Game1.player.UniqueMultiplayerID);
        if (member is null || !CanManageLinkedCompanionAlpha6615(speaker, member))
            return;

        const string text = "P / L+R Companion";
        const int tagHeight = 44;
        int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);
        int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);
        int widthGuess = (int)Math.Ceiling(Game1.smallFont.MeasureString(text).X * DialogueHintScale) + 30;
        int x = dialogueLeft + (dialogueBox.width - widthGuess) / 2;
        int y = Math.Max(6, dialogueTop - tagHeight - 10);
        DrawDialogueTag(e, text, x, y, tagHeight);
    }
}
