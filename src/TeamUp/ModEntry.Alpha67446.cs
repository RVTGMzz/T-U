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

        // 6.7.44.7 quality-of-life safety: an active Team Up member must not consume the Farmer's
        // held object as a vanilla gift during combat. Inactive roster members retain normal gifting.
        Helper.Events.Input.ButtonPressed += OnAlpha67447GiftGuardButtonPressed;
        Helper.Events.GameLoop.SaveLoaded += OnAlpha67447SaveLoaded;

        Helper.ConsoleCommands.Add(
            "teamup_pelipper_runtime",
            "6.7.44.6+ Pelipper runtime diagnostics: status.",
            OnAlpha67446PelipperRuntimeCommand);

        Monitor.Log(
            $"Team Up 6.7.44.7 runtime fixes enabled: 20Hz encounter discovery, cached Pelipper identity/Shiny reflection, Elite proxy guard, pre-lethal Mutation ({PelipperRuntimeAlpha67446.PatchedDamageMethodCount} damage hooks), active-teammate gift guard.",
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

    private void OnAlpha67447SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer)
            PelipperRuntimeAlpha67446.ResetMutationBridgeTelemetry();
    }

    private void OnAlpha67447GiftGuardButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady
            || Game1.eventUp
            || Game1.dialogueUp
            || !Context.IsPlayerFree
            || !e.Button.IsActionButton()
            || Game1.player.ActiveObject is null)
        {
            return;
        }

        NPC? npc = FindFacingNpc();
        if (npc is null)
            return;

        PartyMemberData? member = Party.Get(npc.Name, Game1.player.UniqueMultiplayerID);
        if (member is null || member.State is not (PartyMemberState.Following or PartyMemberState.Waiting))
            return;

        // Suppress before vanilla NPC interaction can turn the held object into a gift.
        // Do not remove or mutate the item: the Farmer keeps exactly what they were holding.
        Helper.Input.Suppress(e.Button);
        RecruitHintNpcName = null;

        bool vi = Helper.Translation.Locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        string name = string.IsNullOrWhiteSpace(npc.displayName) ? npc.Name : npc.displayName;
        ShowHud(
            vi
                ? $"Không thể tặng quà khi {name} đang ở trong đội."
                : $"You can't give gifts while {name} is active in Team Up.",
            error: true);
        Monitor.Log($"[GiftGuard] Blocked held-item gift to active teammate {npc.Name} state={member.State}.", LogLevel.Debug);
    }

    private void OnAlpha67446PelipperRuntimeCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save before using teamup_pelipper_runtime.", LogLevel.Info);
            return;
        }

        Monitor.Log(PelipperRuntimeAlpha67446.Describe(), LogLevel.Info);
        Monitor.Log(PelipperRuntimeAlpha67446.DescribeMutationBridge(), LogLevel.Info);
        Monitor.Log(PelipperCaptureSafetyService.DescribePolicy(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.Describe(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.LastMutationLine, LogLevel.Info);
    }
}
