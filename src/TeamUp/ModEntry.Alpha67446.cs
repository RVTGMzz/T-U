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
    private Alpha674411PelipperDualHpProbeService PelipperDualHpProbeAlpha674411 { get; set; } = null!;
    private Alpha674412PelipperModDataHpBindingService PelipperModDataHpBindingAlpha674412 { get; set; } = null!;
    private Alpha674413PelipperVisibleMutationService PelipperVisibleMutationAlpha674413 { get; set; } = null!;
    private Alpha674413MutationMinionSpawnService MutationMinionSpawnAlpha674413 { get; set; } = null!;
    private Alpha674414PelipperMutantRewardService PelipperMutantRewardAlpha674414 { get; set; } = null!;

    private void RegisterAlpha67446RuntimeFixes()
    {
        // 6.7.44.10 reconnects a unique same-species visible PokemonNpc when Pelipper's hidden
        // combat proxy keeps the Pokemon display name but stable IDs/positions no longer line up.
        // 6.7.44.11 adds a proxy-instance cache inside this service so the location is not rescanned
        // every time another subsystem asks for the same encounter identity.
        PelipperSpeciesPairingAlpha674410 = new Alpha674410PelipperSpeciesPairingService(
            Monitor,
            ModManifest.UniqueID);

        // 6.7.44.11 cold-path dual probe inspects both visible source and hidden proxy only after a
        // forced transform still fails. It is diagnostic and cached by runtime type pair.
        PelipperDualHpProbeAlpha674411 = new Alpha674411PelipperDualHpProbeService(Monitor);

        // 6.7.44.8 owns the source-aware Mutation engine. 6.7.44.12 binds Pelipper 1.2.0's
        // authoritative WildCurrentHealth/WildMaxHealth proxy modData into that engine.
        PelipperSourceMutationAlpha67448 = new Alpha67448PelipperSourceMutationService(
            Monitor,
            ModManifest.UniqueID,
            () => Config.MutationHealthMultiplier);
        PelipperModDataHpBindingAlpha674412 = new Alpha674412PelipperModDataHpBindingService(
            Monitor,
            ModManifest.UniqueID);

        // 6.7.44.13 mirrors Mutation visual scale onto the visible PokemonNpc and restores it when
        // the wild encounter ends. 6.7.44.14 caps that visible multiplier at x2.
        PelipperVisibleMutationAlpha674413 = new Alpha674413PelipperVisibleMutationService(
            Monitor,
            ModManifest.UniqueID,
            () => Config.MutationVisualScaleMultiplier);

        // 6.7.44.13 fallback remains available for generic/non-Pelipper Mutation minions.
        MutationMinionSpawnAlpha674413 = new Alpha674413MutationMinionSpawnService(
            Monitor,
            ModManifest.UniqueID);

        // 6.7.44.14 temporarily suppresses Pelipper's unreliable 2-4 minion wave and compensates
        // the harder three-phase Mutant with x3 native loot passes when it is finally slain.
        PelipperMutantRewardAlpha674414 = new Alpha674414PelipperMutantRewardService(
            Monitor,
            ModManifest.UniqueID);

        PelipperRuntimeAlpha67446 = new Alpha67446PelipperRuntimeService(Monitor, ModManifest.UniqueID);

        // 6.7.44.9 legacy failure-only source probe remains available. With the 6.7.44.12 modData
        // binding resolving HP successfully this probe should stay cold.
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
            "6.7.44.14 Pelipper runtime diagnostics: status.",
            OnAlpha67446PelipperRuntimeCommand);

        Monitor.Log(
            $"Team Up 6.7.44.14 runtime fixes enabled: 20Hz encounter discovery, cached Pelipper identity/Shiny reflection, cached unique-species source pairing, Elite proxy guard, "
            + $"source-aware Mutation ({PelipperSourceMutationAlpha67448.PatchedDamageMethodCount} hooks), Pelipper modData HP binding, visible Mutation x2 cap, Pelipper minions temporarily off + loot x3, active-teammate gift guard.",
            LogLevel.Info);
    }

    private void OnAlpha67446EncounterUpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;
        if (Game1.ticks % EncounterDiscoveryPulseTicksAlpha67446 != 0)
            return;

        EncounterReactionsAlpha67442.Update(Party.Members);
        PelipperVisibleMutationAlpha674413.Update();
    }

    private void OnAlpha67447SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;

        PelipperRuntimeAlpha67446.ResetMutationBridgeTelemetry();
        PelipperSourceMutationAlpha67448.ResetTelemetry();
        PelipperModDataHpBindingAlpha674412.ResetTelemetry();
        PelipperVisibleMutationAlpha674413.ResetTelemetry();
        MutationMinionSpawnAlpha674413.ResetTelemetry();
        PelipperMutantRewardAlpha674414.ResetTelemetry();
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
        Monitor.Log(PelipperModDataHpBindingAlpha674412.Describe(), LogLevel.Info);
        Monitor.Log(PelipperVisibleMutationAlpha674413.Describe(), LogLevel.Info);
        Monitor.Log(PelipperMutantRewardAlpha674414.Describe(), LogLevel.Info);
        Monitor.Log(MutationMinionSpawnAlpha674413.Describe(), LogLevel.Info);
        Monitor.Log(PelipperSourceProbeAlpha67449.Describe(), LogLevel.Info);
        Monitor.Log(PelipperDualHpProbeAlpha674411.Describe(), LogLevel.Info);
        Monitor.Log(PelipperRuntimeAlpha67446.DescribeMutationBridge(), LogLevel.Info);
        Monitor.Log(PelipperCaptureSafetyService.DescribePolicy(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.Describe(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.LastMutationLine, LogLevel.Info);
    }
}
