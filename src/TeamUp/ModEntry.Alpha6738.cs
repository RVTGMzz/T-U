using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private SurgeHighEscalationStoryService SurgeHighAlpha6738 { get; set; } = null!;

    private void RegisterAlpha6738Events()
    {
        SurgeHighAlpha6738 = new SurgeHighEscalationStoryService(
            Helper,
            Monitor,
            () => ControlledBreachAlpha6736.Stage,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732,
            (requestedSlots, source) => RosterProgressionAlpha6727.UnlockTo(Game1.MasterPlayer, requestedSlots, source),
            () => EnforceStoryRosterCapacityAlpha6727());

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6738SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6738Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6738UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_surge_high",
            "Surge HIGH escalation: status | reset | stage <0-4>.",
            OnAlpha6738Command);
    }

    private void OnAlpha6738SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            SurgeHighAlpha6738.OnSaveLoaded();
    }

    private void OnAlpha6738Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            SurgeHighAlpha6738.OnWarped(e.NewLocation);
    }

    private void OnAlpha6738UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            SurgeHighAlpha6738.Update();
    }

    private void OnAlpha6738Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_surge_high.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            SurgeHighAlpha6738.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 4)
            SurgeHighAlpha6738.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_surge_high <status|reset|stage 0-4>", LogLevel.Info);
            return;
        }

        WriteAlpha6738Diagnostic();
    }

    private void WriteAlpha6738Diagnostic()
    {
        int unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        int active = GetActiveStoryNpcCountAlpha6727();
        int effective = GetEffectiveNpcSlotLimitAlpha6727();
        List<string> lines = new()
        {
            "TEAM UP 6.7.38 - SURGE HIGH ESCALATION / STORY SLOT 4",
            Origin.Describe(),
            SurgeStoryAlpha6725.Describe(),
            ControlledBreachAlpha6736.Describe(Game1.currentLocation),
            SurgeHighAlpha6738.Describe(Game1.currentLocation, unlocked),
            $"Story roster: unlocked={unlocked}/4 | effectiveNow={effective} | activeNpcAllies={active}",
            "Route: completed first-entry probe -> Guild HIGH check briefing -> exact recorded breach face -> 180-tick stable reading -> SURGE HIGH -> Guild report -> story slot 4.",
            "SURGE HIGH is a persistent chapter state. This checkpoint does not spawn the final boss or create a lower-workings dungeon map.",
            "Formation rule remains five PEOPLE total, so multiplayer Farmers can reduce effective NPC capacity below the story allowance.",
            "Spoiler lock remains active: George is still observed Rank D / Non-Combatant / unrecruitable; historical worker identity remains unrevealed."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Surge_HIGH_Escalation_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Surge HIGH diagnostic saved: {path}", LogLevel.Info);
    }
}
