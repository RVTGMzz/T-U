using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private LowerWorkingsDescentStoryService LowerWorkingsDescentAlpha6742 { get; set; } = null!;

    private void RegisterAlpha6742Events()
    {
        LowerWorkingsDescentAlpha6742 = new LowerWorkingsDescentStoryService(
            Helper,
            Monitor,
            () => EntryProtocolAlpha6740.Stage,
            () => EntryProtocolAlpha6740.ProtocolReady,
            () => SurgeHighAlpha6738.IsHigh,
            () => RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732,
            () => EntryProtocolAlpha6740.GetRequiredFieldPeople(),
            () => EntryProtocolAlpha6740.GetRequiredNpcAllies());

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6742SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6742Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6742UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_lower_descent",
            "Lower Workings first descent: status | reset | stage <0-5>.",
            OnAlpha6742Command);
    }

    private void OnAlpha6742SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            LowerWorkingsDescentAlpha6742.OnSaveLoaded();
    }

    private void OnAlpha6742Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            LowerWorkingsDescentAlpha6742.OnWarped(e.NewLocation);
    }

    private void OnAlpha6742UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            LowerWorkingsDescentAlpha6742.Update();
    }

    private void OnAlpha6742Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_lower_descent.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            LowerWorkingsDescentAlpha6742.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 5)
            LowerWorkingsDescentAlpha6742.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_lower_descent <status|reset|stage 0-5>", LogLevel.Info);
            return;
        }

        WriteAlpha6742Diagnostic();
    }

    private void WriteAlpha6742Diagnostic()
    {
        int unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        List<string> lines = new()
        {
            "TEAM UP 6.7.42 - LOWER WORKINGS DESCENT / THRESHOLD CROSSING",
            Origin.Describe(),
            EntryProtocolAlpha6740.Describe(Game1.currentLocation),
            LowerWorkingsDescentAlpha6742.Describe(Game1.currentLocation),
            $"Story roster: unlocked={unlocked}/4 | effectiveNow={GetEffectiveNpcSlotLimitAlpha6727()} | activeNpcAllies={GetActiveStoryNpcCountAlpha6727()}",
            $"Full operational formation requirement: people={EntryProtocolAlpha6740.GetRequiredFieldPeople()} | activeNpcAllies={EntryProtocolAlpha6740.GetRequiredNpcAllies()}.",
            "Route: Entry Protocol READY -> Guild descent order -> exact breach face with full formation -> 120-tick threshold crossing -> 180-tick first interior inspection -> Guild report.",
            "Threshold crossing and first-descent completion are persisted separately so a future dedicated Lower Workings map can inherit the state cleanly.",
            "Physical evidence now supports deliberate emergency containment: directed cribbing, shaped blast scoring, and a collapse pattern designed to close the passage. Historical worker identity remains unknown.",
            "No custom dungeon map, final boss, George reveal, or roster expansion is introduced by this checkpoint."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Lower_Workings_Descent_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Lower Workings descent diagnostic saved: {path}", LogLevel.Info);
    }
}
