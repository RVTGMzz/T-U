using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private ControlledBreachFirstEntryStoryService ControlledBreachAlpha6736 { get; set; } = null!;

    private void RegisterAlpha6736Events()
    {
        ControlledBreachAlpha6736 = new ControlledBreachFirstEntryStoryService(
            Helper,
            Monitor,
            () => CorridorApproachAlpha6734.Stage,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6736SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6736Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6736UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_breach",
            "Controlled breach / first entry: status | reset | stage <0-5>.",
            OnAlpha6736Command);
    }

    private void OnAlpha6736SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            ControlledBreachAlpha6736.OnSaveLoaded();
    }

    private void OnAlpha6736Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            ControlledBreachAlpha6736.OnWarped(e.NewLocation);
    }

    private void OnAlpha6736UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            ControlledBreachAlpha6736.Update();
    }

    private void OnAlpha6736Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_breach.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            ControlledBreachAlpha6736.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 5)
            ControlledBreachAlpha6736.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_breach <status|reset|stage 0-5>", LogLevel.Info);
            return;
        }

        WriteAlpha6736Diagnostic();
    }

    private void WriteAlpha6736Diagnostic()
    {
        int fieldPeople = CountFieldPeopleAtAlpha6732(Game1.currentLocation);
        int npcAllies = CountActiveStoryNpcAlliesAtAlpha6732(Game1.currentLocation);
        List<string> lines = new()
        {
            "TEAM UP 6.7.36 - CONTROLLED BREACH / FIRST ENTRY",
            Origin.Describe(),
            CorridorApproachAlpha6734.Describe(Game1.currentLocation),
            ControlledBreachAlpha6736.Describe(Game1.currentLocation),
            $"Field team here: people={fieldPeople} | active Team Up NPC allies={npcAllies}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Route: Guild breach briefing -> exact saved survey face -> 180-tick controlled opening -> 120-tick threshold probe -> Guild report.",
            "First entry is deliberately a short threshold probe at the existing MineShaft face. No custom lower-workings map or boss is claimed yet.",
            "The pressure pulse after opening is a story escalation hook only. Story slot 4 remains locked.",
            "Spoiler lock: historical worker remains unnamed; George stays Rank D / Non-Combatant and unrecruitable."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Controlled_Breach_First_Entry_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Controlled breach diagnostic saved: {path}", LogLevel.Info);
    }
}
