using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private LowerWorkingsEntryProtocolStoryService EntryProtocolAlpha6740 { get; set; } = null!;

    private void RegisterAlpha6740Events()
    {
        EntryProtocolAlpha6740 = new LowerWorkingsEntryProtocolStoryService(
            Helper,
            Monitor,
            () => SurgeHighAlpha6738.Stage,
            () => SurgeHighAlpha6738.IsHigh,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732,
            () => RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
            () => GetOnlineFarmerIds().Count,
            () => Config.MaxPartyMembers);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6740SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6740Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6740UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_entry_protocol",
            "Lower Workings entry protocol: status | reset | stage <0-4>.",
            OnAlpha6740Command);
    }

    private void OnAlpha6740SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            EntryProtocolAlpha6740.OnSaveLoaded();
    }

    private void OnAlpha6740Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            EntryProtocolAlpha6740.OnWarped(e.NewLocation);
    }

    private void OnAlpha6740UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            EntryProtocolAlpha6740.Update();
    }

    private void OnAlpha6740Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_entry_protocol.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            EntryProtocolAlpha6740.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 4)
            EntryProtocolAlpha6740.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_entry_protocol <status|reset|stage 0-4>", LogLevel.Info);
            return;
        }

        WriteAlpha6740Diagnostic();
    }

    private void WriteAlpha6740Diagnostic()
    {
        int unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        int active = GetActiveStoryNpcCountAlpha6727();
        int effective = GetEffectiveNpcSlotLimitAlpha6727();
        List<string> lines = new()
        {
            "TEAM UP 6.7.40 - HIGH RESPONSE PREPARATION / LOWER WORKINGS ENTRY PROTOCOL",
            Origin.Describe(),
            SurgeHighAlpha6738.Describe(Game1.currentLocation, unlocked),
            EntryProtocolAlpha6740.Describe(Game1.currentLocation),
            $"Story roster: unlocked={unlocked}/4 | effectiveNow={effective} | activeNpcAllies={active}",
            $"Full operational formation here requires people={EntryProtocolAlpha6740.GetRequiredFieldPeople()} and activeNpcAllies={EntryProtocolAlpha6740.GetRequiredNpcAllies()} under the configured five-person cap.",
            "Route: SURGE HIGH + slot4 -> Guild protocol briefing -> exact recorded breach face with full formation -> 240-tick readiness hold -> Guild report -> Entry Protocol READY.",
            "Abort criteria: broken formation, wrong shaft, warp, menu/dialogue ownership, or leaving player-free state resets the readiness hold.",
            "This checkpoint validates staging and withdrawal discipline only. It does not descend into a new lower-workings map and does not spawn a boss.",
            "Spoiler lock remains active: George is still observed Rank D / Non-Combatant / unrecruitable; historical worker identity remains unrevealed."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Lower_Workings_Entry_Protocol_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Entry Protocol diagnostic saved: {path}", LogLevel.Info);
    }
}
