using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

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
    private Alpha674436EliteCaptureGuardService EliteCaptureGuardAlpha674436 { get; set; } = null!;
    private Alpha674416MutationLeaderMinionPolicyService MutationLeaderMinionPolicyAlpha674416 { get; set; } = null!;
    private Alpha674418NativeMutationMinionService NativeMutationMinionsAlpha674418 { get; set; } = null!;
    private Alpha674419PelipperSpawnCommandGateService PelipperSpawnCommandGateAlpha674419 { get; set; } = null!;
    private Alpha674420MutationAggroService MutationAggroAlpha674420 { get; set; } = null!;
    private Alpha674423PelipperMutantLeaderSmoothingService PelipperMutationLeaderSmoothingAlpha674423 { get; set; } = null!;

    private void RegisterAlpha67446RuntimeFixes()
    {
        PelipperSpeciesPairingAlpha674410 = new Alpha674410PelipperSpeciesPairingService(
            Monitor,
            ModManifest.UniqueID);

        PelipperDualHpProbeAlpha674411 = new Alpha674411PelipperDualHpProbeService(Monitor);

        PelipperSourceMutationAlpha67448 = new Alpha67448PelipperSourceMutationService(
            Monitor,
            ModManifest.UniqueID,
            () => Config.MutationHealthMultiplier);
        PelipperModDataHpBindingAlpha674412 = new Alpha674412PelipperModDataHpBindingService(
            Monitor,
            ModManifest.UniqueID);

        PelipperVisibleMutationAlpha674413 = new Alpha674413PelipperVisibleMutationService(
            Monitor,
            ModManifest.UniqueID,
            () => Config.MutationVisualScaleMultiplier);

        MutationMinionSpawnAlpha674413 = new Alpha674413MutationMinionSpawnService(
            Monitor,
            ModManifest.UniqueID);

        PelipperMutantRewardAlpha674414 = new Alpha674414PelipperMutantRewardService(
            Monitor,
            ModManifest.UniqueID);

        EliteCaptureGuardAlpha674436 = new Alpha674436EliteCaptureGuardService(
            Monitor,
            ModManifest.UniqueID);

        MutationLeaderMinionPolicyAlpha674416 = new Alpha674416MutationLeaderMinionPolicyService(
            Monitor,
            ModManifest.UniqueID);

        NativeMutationMinionsAlpha674418 = new Alpha674418NativeMutationMinionService(
            Monitor,
            Helper,
            ModManifest.UniqueID);

        PelipperSpawnCommandGateAlpha674419 = new Alpha674419PelipperSpawnCommandGateService(
            Monitor,
            Helper,
            ModManifest.UniqueID);

        // 6.7.44.20 remains useful for generic Monster hostility and telemetry, but Pelipper's visible
        // Pokemon NPCs don't consume those native Monster pursuit flags as active hostile AI.
        MutationAggroAlpha674420 = new Alpha674420MutationAggroService(
            Monitor,
            Helper);

        // 6.7.44.23 supersedes both the 6.7.44.21 tile-path experiment and 6.7.44.22 steering runtime.
        // Followers retain the proven pack steering, while the x2 Mutant leader no longer yields to
        // followers, clears Pelipper passive movement state before chase steps, uses short direction
        // hysteresis, holds a stable melee band and can damage Farmer from a visually appropriate x2 reach.
        PelipperMutationLeaderSmoothingAlpha674423 = new Alpha674423PelipperMutantLeaderSmoothingService(
            Monitor,
            Helper);

        PelipperRuntimeAlpha67446 = new Alpha67446PelipperRuntimeService(Monitor, ModManifest.UniqueID);
        PelipperSourceProbeAlpha67449 = new Alpha67449PelipperSourceProbeService(Monitor, ModManifest.UniqueID);

        Helper.Events.GameLoop.UpdateTicking -= OnAlpha67442UpdateTicking;
        Helper.Events.GameLoop.UpdateTicking += OnAlpha67446EncounterUpdateTicking;

        Helper.Events.Input.ButtonPressed += OnAlpha67447GiftGuardButtonPressed;
        Helper.Events.GameLoop.SaveLoaded += OnAlpha67447SaveLoaded;

        Helper.Events.Display.RenderingWorld += OnAlpha674434RenderingWorld;
        Helper.Events.Display.RenderedWorld += OnAlpha67448RenderedWorld;

        Helper.ConsoleCommands.Add(
            "teamup_pelipper_runtime",
            "6.7.44.37 Pelipper runtime diagnostics: status.",
            OnAlpha67446PelipperRuntimeCommand);

        Helper.ConsoleCommands.Add(
            "teamup_mutation_regression",
            "6.7.44.37 Mutation regression audit for the current location.",
            OnAlpha674437MutationRegressionCommand);

        Monitor.Log(
            $"Team Up 6.7.44.37 regression/stability enabled: exact source-equivalent minions only, GreenSlime fallback removed at factory level, unsupported custom sources fail closed, "
            + $"Pelipper native followers preserved, 6.7.44.36 elite contract retained, source-aware Mutation ({PelipperSourceMutationAlpha67448.PatchedDamageMethodCount} hooks).",
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
        PelipperModDataHpBindingAlpha674412.ResetTelemetry();
        PelipperVisibleMutationAlpha674413.ResetTelemetry();
        MutationMinionSpawnAlpha674413.ResetTelemetry();
        PelipperMutantRewardAlpha674414.ResetTelemetry();
        EliteCaptureGuardAlpha674436.ResetTelemetry();
        MutationLeaderMinionPolicyAlpha674416.ResetTelemetry();
        NativeMutationMinionsAlpha674418.ResetTelemetry();
        PelipperSpawnCommandGateAlpha674419.ResetTelemetry();
        MutationAggroAlpha674420.ResetTelemetry();
        PelipperMutationLeaderSmoothingAlpha674423.ResetTelemetry();
    }

    private void OnAlpha674434RenderingWorld(object? sender, RenderingWorldEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp)
            return;

        // Pelipper can refresh presentation fields during its own update cycle. Reassert the Mutant
        // scale immediately before world draw so the visible source doesn't alternate x1/x2 frames.
        PelipperVisibleMutationAlpha674413.Update();
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

    private void OnAlpha674437MutationRegressionCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
        {
            Monitor.Log("Load a save before using teamup_mutation_regression.", LogLevel.Info);
            return;
        }

        GameLocation location = Game1.currentLocation;
        List<Monster> minions = location.characters
            .OfType<Monster>()
            .Where(MonsterMutationService.IsMutationMinion)
            .ToList();

        int typeMismatch = 0;
        int recursiveMutants = 0;
        int missingExcluded = 0;
        int pelipperNative = 0;
        int pelipperIdentityMiss = 0;
        int pelipperDuplicateEncounter = 0;
        HashSet<string> pelipperEncounterIds = new(StringComparer.Ordinal);

        foreach (Monster minion in minions)
        {
            if (MonsterMutationService.IsMutant(minion))
                recursiveMutants++;

            if (!minion.modData.ContainsKey(MonsterMutationService.MutationExcludedMarker))
                missingExcluded++;

            minion.modData.TryGetValue(Alpha674418NativeMutationMinionService.NativeProviderMarker, out string? provider);
            minion.modData.TryGetValue(MonsterMutationService.MutationSourceMarker, out string? sourceType);

            if (string.Equals(provider, "pelipper-native", StringComparison.Ordinal))
            {
                pelipperNative++;
                if (!PelipperTownCompatibilityService.IsWildCombatActor(minion)
                    || !PelipperWildEncounterIdentityService.TryResolve(minion, out PelipperWildEncounterIdentity identity))
                {
                    pelipperIdentityMiss++;
                    continue;
                }

                if (!pelipperEncounterIds.Add(identity.EncounterId))
                    pelipperDuplicateEncounter++;
                continue;
            }

            string runtimeType = minion.GetType().FullName ?? minion.GetType().Name;
            if (!string.IsNullOrWhiteSpace(sourceType)
                && !runtimeType.Equals(sourceType, StringComparison.Ordinal))
            {
                typeMismatch++;
            }
        }

        int factoryFallback = MonsterMutationMinionFactory.FallbackSpawned;
        int failClosed = MonsterMutationMinionFactory.FailClosedRejected;
        bool pass = typeMismatch == 0
            && recursiveMutants == 0
            && missingExcluded == 0
            && factoryFallback == 0
            && pelipperIdentityMiss == 0
            && pelipperDuplicateEncounter == 0;

        Monitor.Log(
            $"Mutation regression audit: {(pass ? "PASS" : "FAIL")} | location={location.NameOrUniqueName} | minions={minions.Count} | "
            + $"typeMismatch={typeMismatch} | recursiveMutants={recursiveMutants} | missingExcluded={missingExcluded} | "
            + $"factoryFallback={factoryFallback} | failClosed={failClosed} | pelipperNative={pelipperNative} | "
            + $"pelipperIdentityMiss={pelipperIdentityMiss} | pelipperDuplicateEncounter={pelipperDuplicateEncounter}",
            pass ? LogLevel.Info : LogLevel.Warn);
        Monitor.Log(NativeMutationMinionsAlpha674418.Describe(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.Describe(), LogLevel.Info);
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
        Monitor.Log(EliteCaptureGuardAlpha674436.Describe(), LogLevel.Info);
        Monitor.Log(MutationLeaderMinionPolicyAlpha674416.Describe(), LogLevel.Info);
        Monitor.Log(NativeMutationMinionsAlpha674418.Describe(), LogLevel.Info);
        Monitor.Log(PelipperSpawnCommandGateAlpha674419.Describe(), LogLevel.Info);
        Monitor.Log(MutationAggroAlpha674420.Describe(), LogLevel.Info);
        Monitor.Log(PelipperMutationLeaderSmoothingAlpha674423.Describe(), LogLevel.Info);
        Monitor.Log(MutationMinionSpawnAlpha674413.Describe(), LogLevel.Info);
        Monitor.Log(PelipperSourceProbeAlpha67449.Describe(), LogLevel.Info);
        Monitor.Log(PelipperDualHpProbeAlpha674411.Describe(), LogLevel.Info);
        Monitor.Log(PelipperRuntimeAlpha67446.DescribeMutationBridge(), LogLevel.Info);
        Monitor.Log(PelipperCaptureSafetyService.DescribePolicy(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.Describe(), LogLevel.Info);
        Monitor.Log(MutationAlpha6719.LastMutationLine, LogLevel.Info);
    }
}
