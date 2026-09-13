using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int EncounterDiscoveryPulseTicksAlpha67446 = 3;
    private Alpha67446PelipperRuntimeService PelipperRuntimeAlpha67446 { get; set; } = null!;
    private Alpha67448PelipperSourceMutationService PelipperSourceMutationAlpha67448 { get; set; } = null!;
    private Alpha67449PelipperSourceProbeService PelipperSourceProbeAlpha67449 { get; set; } = null!;
    private Alpha674410PelipperSpeciesPairingService PelipperSpeciesPairingAlpha674410 { get; set; } = null!;

    private void RegisterAlpha67446RuntimeFixes()
    {
        // 6.7.44.10 must patch identity resolution before the source-aware Mutation layer starts.
        // Pelipper 1.2.0 can keep the correct Pokemon name on the hidden proxy while source/proxy
        // positions separate during combat. The late pairing fallback safely reconnects a unique
        // same-species visible wild actor without guessing across duplicate species.
        PelipperSpeciesPairingAlpha674410 = new Alpha674410PelipperSpeciesPairingService(
            Monitor,
            ModManifest.UniqueID);

        // 6.7.44.8 patches the source-aware damage path at highest Harmony priority. Keep this
        // registration before the 6.7.44.6 proxy telemetry layer so source HP can cancel a true
        // Pelipper lethal hit before the old sentinel-HP heuristic sees it.
        PelipperSourceMutationAlpha67448 = new Alpha67448PelipperSourceMutationService(
            Monitor,
            ModManifest.UniqueID,
            () => Config.MutationHealthMultiplier);
        PelipperRuntimeAlpha67446 = new Alpha67446PelipperRuntimeService(Monitor, ModManifest.UniqueID);

        // 6.7.44.9 probes only unresolved source-HP layouts. It is intentionally registered after
        // the 6.7.44.8 resolver so the postfix sees only genuine resolver failures and stays cold in
        // normal combat once Pelipper's HP layout is understood.
        PelipperSourceProbeAlpha67449 = new Alpha67449PelipperSourceProbeService(Monitor, ModManifest.UniqueID);

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

        // 6.7.44.8 draws Mutation feedback around the visible Pelipper Pokemon source rather than
        // the hidden Green Slime combat proxy.
        Helper.Events.Display.RenderedWorld += OnAlpha67448RenderedWorld;

        Helper.ConsoleCommands.Add(
            "teamup_pelipper_runtime",
            "6.7.44.10 Pelipper runtime diagnostics: status.",
            OnAlpha67446PelipperRuntimeCommand);

        Monitor.Log(
            $"Team Up 6.7.44.10 runtime fixes enabled: 20Hz encounter discovery, cached Pelipper identity/Shiny reflection, unique-species source pairing fallback, Elite proxy guard, "
            + $"legacy pre-lethal bridge ({PelipperRuntimeAlpha67446.PatchedDamageMethodCount} hooks), source-aware Mutation ({PelipperSourceMutationAlpha67448.PatchedDamageMethodCount} hooks), unresolved source-HP probe, active-teammate gift guard.",
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
        if (!Context.IsMainPlayer)
            return;

        PelipperRuntimeAlpha67446.ResetMutationBridgeTelemetry();
        PelipperSourceMutationAlpha67448.ResetTelemetry();
    }

    private void OnAlpha67448RenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp)
            return;
        if (Game1.activeClickableMenu is not null && !Game1.dialogueUp)
            return;

        PelipperSourceMutationAlpha67448.DrawSourceAuras(e.SpriteBatch);
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
        Monitor.Log(PelipperSpeciesPairingAlpha674410.Describe(), LogLevel.Info);
        Monitor.Log(PelipperSourceMutationAlpha67448.Describe(), LogLevel.Info);
        Monitor.Log(PelipperSourceProbeAlpha67449.Describe(), LogLevel.Info);
        Monitor.Log(PelipperRuntimeAlpha67446.DescribeMutationBridge(), LogLevel.Info);
        Monitor.Log(PelipperCaptureSafetyService.DescribePolicy(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.Describe(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.LastMutationLine, LogLevel.Info);
    }
}
