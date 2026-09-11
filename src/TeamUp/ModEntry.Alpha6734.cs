using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private SealedCorridorApproachStoryService CorridorApproachAlpha6734 { get; set; } = null!;

    private void RegisterAlpha6734Events()
    {
        CorridorApproachAlpha6734 = new SealedCorridorApproachStoryService(
            Helper,
            Monitor,
            () => FieldTriangulationAlpha6732.Stage,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6734SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6734Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6734UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_corridor",
            "Sealed corridor approach: status | reset | stage <0-4>.",
            OnAlpha6734Command);
    }

    private void OnAlpha6734SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            CorridorApproachAlpha6734.OnSaveLoaded();
    }

    private void OnAlpha6734Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            CorridorApproachAlpha6734.OnWarped(e.NewLocation);
    }

    private void OnAlpha6734UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            CorridorApproachAlpha6734.Update();
    }

    private void OnAlpha6734Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_corridor.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            CorridorApproachAlpha6734.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 4)
            CorridorApproachAlpha6734.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_corridor <status|reset|stage 0-4>", LogLevel.Info);
            return;
        }

        WriteAlpha6734Diagnostic();
    }

    private void WriteAlpha6734Diagnostic()
    {
        int fieldPeople = CountFieldPeopleAtAlpha6732(Game1.currentLocation);
        int npcAllies = CountActiveStoryNpcAlliesAtAlpha6732(Game1.currentLocation);
        List<string> lines = new()
        {
            "TEAM UP 6.7.34 - SEALED CORRIDOR APPROACH",
            Origin.Describe(),
            FieldTriangulationAlpha6732.Describe(Game1.currentLocation),
            CorridorApproachAlpha6734.Describe(Game1.currentLocation),
            $"Field team here: people={fieldPeople} | active Team Up NPC allies={npcAllies}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Route: Guild pressure-survey briefing -> MineShaft approach face -> hold a valid field team for 240 ticks -> Guild report.",
            "This checkpoint confirms the sealed access face only. It does not breach the wall and does not unlock story slot 4.",
            "Spoiler lock: historical worker remains unnamed; George stays Rank D / Non-Combatant and unrecruitable."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Sealed_Corridor_Approach_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Sealed corridor approach diagnostic saved: {path}", LogLevel.Info);
    }
}
