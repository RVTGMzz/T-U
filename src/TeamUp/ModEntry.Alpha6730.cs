using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private OldMineConnectionStoryService OldMineConnectionAlpha6730 { get; set; } = null!;

    private void RegisterAlpha6730Events()
    {
        OldMineConnectionAlpha6730 = new OldMineConnectionStoryService(
            Helper,
            Monitor,
            () => MarlonInvestigationAlpha6729.Stage,
            HasActiveStoryAllyAtAlpha6729,
            OnOldMineConnectionCompleteAlpha6730);
        Helper.Events.GameLoop.SaveLoaded += OnAlpha6730SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6730Warped;
        Helper.ConsoleCommands.Add(
            "teamup_old_mine",
            "Old mine connection: status | reset | stage <0-3>.",
            OnAlpha6730Command);
    }

    private void OnAlpha6730SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            OldMineConnectionAlpha6730.OnSaveLoaded();
    }

    private void OnAlpha6730Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            OldMineConnectionAlpha6730.OnWarped(e.NewLocation);
    }

    private void OnOldMineConnectionCompleteAlpha6730()
    {
        bool unlocked = RosterProgressionAlpha6727.UnlockTo(Game1.MasterPlayer, 3, "old-mine-connection-confirmed");
        EnforceStoryRosterCapacityAlpha6727();
        SavePartyNow();
        BroadcastPartySnapshot();
        if (unlocked)
        {
            ShowHud(Helper.Translation.Get("story.roster.third-unlock", new
            {
                unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
                max = TeamUpRosterProgressionService.MaxStoryNpcSlots
            }));
        }
    }

    private void OnAlpha6730Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_old_mine.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            OldMineConnectionAlpha6730.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 3)
            OldMineConnectionAlpha6730.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_old_mine <status|reset|stage 0-3>", LogLevel.Info);
            return;
        }
        WriteAlpha6730Diagnostic();
    }

    private void WriteAlpha6730Diagnostic()
    {
        List<string> lines = new()
        {
            "TEAM UP 6.7.30 - OLD MINE CONNECTION",
            Origin.Describe(),
            MarlonInvestigationAlpha6729.Describe(),
            OldMineConnectionAlpha6730.Describe(),
            $"Active story ally here: {HasActiveStoryAllyAtAlpha6729(Game1.currentLocation)}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Route: Marlon case complete -> Guild archive lead -> ManorHouse sealed record -> Guild confirmation -> slot 3.",
            "Spoiler lock: the old miner remains unnamed; George stays Rank D / Non-Combatant and unrecruitable."
        };
        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Old_Mine_Connection_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines) Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Old mine connection diagnostic saved: {path}", LogLevel.Info);
    }
}
