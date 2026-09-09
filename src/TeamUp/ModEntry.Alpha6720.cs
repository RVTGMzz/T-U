using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6720Events()
    {
        MonsterMutationFootprintPatch.Apply(Monitor, ModManifest.UniqueID);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6720SaveLoaded;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6720ReturnedToTitle;

        Helper.ConsoleCommands.Add(
            "teamup_mutation_detail",
            "Alpha 6.7.20 mutation detail: footprint patch coverage and same-type/fallback minion telemetry.",
            OnAlpha6720MutationDetailCommand);
    }

    private void OnAlpha6720SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
        MonsterMutationMinionFactory.ResetTelemetry();
    }

    private void OnAlpha6720ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        => MonsterMutationMinionFactory.ResetTelemetry();

    private void OnAlpha6720MutationDetailCommand(string command, string[] args)
    {
        string line =
            $"Mutation 6.7.20: footprintHooks={MonsterMutationFootprintPatch.PatchedMethodCount} | "
            + $"sameTypeMinions={MonsterMutationMinionFactory.SameTypeSpawned} | "
            + $"fallbackMinions={MonsterMutationMinionFactory.FallbackSpawned} | "
            + $"sameTypeFailures={MonsterMutationMinionFactory.SameTypeFailures}";

        Monitor.Log(line, LogLevel.Info);
        if (Context.IsWorldReady)
            Game1.showGlobalMessage(line);
    }
}
