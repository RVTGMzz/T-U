using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int EncounterDiscoveryPulseTicksAlpha67446 = 3;
    private Alpha67446PelipperRuntimeService PelipperRuntimeAlpha67446 { get; set; } = null!;

    private void RegisterAlpha67446RuntimeFixes()
    {
        PelipperRuntimeAlpha67446 = new Alpha67446PelipperRuntimeService(Monitor, ModManifest.UniqueID);

        // 6.7.44.4 ran the full source-aware identity + reflection classifier every simulation tick.
        // With many Pelipper wild actors this became O(monsters * actors) at 60Hz. Replace only that
        // discovery handler with a 20Hz pulse. Confirmed Shiny hold state itself remains persistent in
        // modData between pulses, so CombatService continues to see HOLD FIRE on every combat tick.
        Helper.Events.GameLoop.UpdateTicking -= OnAlpha67442UpdateTicking;
        Helper.Events.GameLoop.UpdateTicking += OnAlpha67446EncounterUpdateTicking;

        Helper.ConsoleCommands.Add(
            "teamup_pelipper_runtime",
            "6.7.44.6 Pelipper runtime diagnostics: status.",
            OnAlpha67446PelipperRuntimeCommand);

        Monitor.Log(
            $"Team Up 6.7.44.6 runtime fixes enabled: 20Hz encounter discovery, cached Pelipper identity/Shiny reflection, Elite proxy guard, pre-lethal Mutation ({PelipperRuntimeAlpha67446.PatchedDamageMethodCount} damage hooks).",
            LogLevel.Info);
    }

    private void OnAlpha67446EncounterUpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;
        if (Game1.ticks % EncounterDiscoveryPulseTicksAlpha67446 != 0)
            return;

        EncounterReactionsAlpha67442.Update(Party.Members);
    }

    private void OnAlpha67446PelipperRuntimeCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save before using teamup_pelipper_runtime.", LogLevel.Info);
            return;
        }

        Monitor.Log(PelipperRuntimeAlpha67446.Describe(), LogLevel.Info);
        Monitor.Log(PelipperCaptureSafetyService.DescribePolicy(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.Describe(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.LastMutationLine, LogLevel.Info);
    }
}
