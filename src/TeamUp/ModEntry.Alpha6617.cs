using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int PelipperSlotTruthPulseTicksAlpha6617 = 10;

    private bool Alpha6617EventsRegistered;
    private bool Alpha6617SlotCommandRegistered;

    /// <summary>
    /// Alpha 6.6.17 is registered lazily from the base UpdateTicked path so its handlers are
    /// appended after the older Pelipper quota/render handlers. This lets the new source-truth
    /// reconciliation repair legacy state and neutralize the old render-only Standby fallback.
    /// </summary>
    private void EnsureAlpha6617EventsRegistered()
    {
        if (Alpha6617EventsRegistered)
            return;

        Alpha6617EventsRegistered = true;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6617UpdateTicked;
        Helper.Events.Display.RenderingWorld += OnAlpha6617RenderingWorld;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6617ReturnedToTitle;

        if (!Alpha6617SlotCommandRegistered)
        {
            Alpha6617SlotCommandRegistered = true;
            Helper.ConsoleCommands.Add(
                "teamup_slots",
                "Show Team Up combat-companion slot truth and Pelipper live-source state.",
                OnAlpha6617SlotCommand);
        }
    }

    private void OnAlpha6617UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer
            || !Context.IsWorldReady
            || !e.IsMultipleOf(PelipperSlotTruthPulseTicksAlpha6617))
        {
            return;
        }

        ReconcilePelipperPlayerSlotTruthAlpha6617();
    }

    /// <summary>
    /// Alpha 6.6.13 used a render-only fallback when Pelipper didn't expose a deployment setter.
    /// That made Standby Pokemon invisible while their Pelipper AI continued following the owner.
    /// Undo that temporary render mutation before the world is actually drawn. Team Up no longer
    /// uses invisibility as a quota mechanism.
    /// </summary>
    private void OnAlpha6617RenderingWorld(object? sender, RenderingWorldEventArgs e)
    {
        foreach ((NPC actor, bool wasInvisible) in PelipperRenderRestoreAlpha6613.ToList())
        {
            if (!wasInvisible)
                TrySetActorInvisibleAlpha6613(actor, false);
        }

        PelipperRenderRestoreAlpha6613.Clear();
        PelipperRenderSuppressedAlpha6613.Clear();
    }

    private void OnAlpha6617ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PelipperRenderRestoreAlpha6613.Clear();
        PelipperRenderSuppressedAlpha6613.Clear();
    }

    /// <summary>
    /// Pelipper Town owns the Farmer's active-partner lifecycle. Team Up mirrors that live state.
    /// A previously summoned Pokemon must not keep a shared slot after Pelipper recalled/swapped it.
    /// Presence in FindPlayerSummons is therefore the source of truth for player-owned Pelipper units.
    /// </summary>
    private void ReconcilePelipperPlayerSlotTruthAlpha6617()
    {
        int max = Config.AllowLinkedCompanions
            ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2)
            : 0;

        List<LiveCompanionDescriptor> live = CompanionIntegrationService.FindPlayerSummons()
            .Where(PelipperTownCompatibilityService.IsPelipperDescriptor)
            .Where(descriptor => descriptor.OwnerFarmerId.HasValue)
            .ToList();

        // Pelipper exposes one active partner per farmer. If a swap creates a new live descriptor,
        // all older Team Up records for that same farmer become Standby before capacity is checked.
        Dictionary<long, LiveCompanionDescriptor> liveByFarmer = live
            .GroupBy(descriptor => descriptor.OwnerFarmerId!.Value)
            .ToDictionary(group => group.Key, group => group.Last());

        bool changed = false;

        foreach (CompanionUnitData unit in Party.CompanionUnits
            .Where(PelipperTownCompatibilityService.IsSourceControlled)
            .Where(unit => unit.OwnerKind == CompanionOwnerKind.Player)
            .ToList())
        {
            bool isCurrentLive = liveByFarmer.TryGetValue(unit.RecruiterId, out LiveCompanionDescriptor? current)
                && current.UnitId.Equals(unit.UnitId, StringComparison.OrdinalIgnoreCase);

            if (isCurrentLive)
                continue;

            if (IsSlotReservedAlpha6617(unit.State))
            {
                changed |= Party.SetCompanionState(
                    unit.UnitId,
                    unit.RecruiterId,
                    CompanionDeploymentState.Standby);
            }

            PendingPlayerCompanionRecallAlpha6615.Remove(unit.UnitId);
        }

        foreach ((long farmerId, LiveCompanionDescriptor descriptor) in liveByFarmer)
        {
            CompanionUnitData? existing = Party.GetCompanionByUnitId(descriptor.UnitId, farmerId);
            if (existing is null)
            {
                CompanionAddResult added = Party.TryAddPlayerCompanion(
                    descriptor.UnitId,
                    descriptor.CharacterName,
                    descriptor.DisplayName,
                    farmerId,
                    descriptor.ProviderId,
                    descriptor.ProviderUnitId,
                    requestActive: max > 0 && Party.GetActiveCombatCompanionCount() < max);

                if (added is CompanionAddResult.AddedActive
                    or CompanionAddResult.AddedStandby
                    or CompanionAddResult.AddedStandbyLimitReached)
                {
                    changed = true;
                }

                existing = Party.GetCompanionByUnitId(descriptor.UnitId, farmerId);
            }

            if (existing is null || IsSlotReservedAlpha6617(existing.State))
                continue;

            if (max > 0 && Party.GetActiveCombatCompanionCount() < max)
            {
                // The source already sent this Pokemon out. Mirror that fact directly instead of
                // requiring guessed reflection fields such as IsSummoned/IsDeployed.
                changed |= Party.SetCompanionState(
                    existing.UnitId,
                    existing.RecruiterId,
                    CompanionDeploymentState.Active);
                PendingPlayerCompanionRecallAlpha6615.Remove(existing.UnitId);
                continue;
            }

            // This is a genuinely full shared pool, not a stale/ghost slot. Keep the source-visible
            // Pokemon untouched and ask which Team Up slot to replace. Never hide it as fallback.
            if (PendingPlayerCompanionRecallAlpha6615.Add(existing.UnitId)
                && Game1.activeClickableMenu is null
                && !Game1.dialogueUp
                && PendingUiAction is null)
            {
                CompanionUnitData target = existing;
                QueueUi(() => ShowCompanionReplacementForTargetAlpha6615(target));
            }
        }

        // Pre-seed the legacy source-deployment cache with the Team Up state. This prevents the
        // old best-effort reflection writer from repeatedly poking Pelipper actors on later quota
        // pulses. Pelipper remains movement/render/deployment authority.
        foreach (CompanionUnitData unit in Party.CompanionUnits.Where(PelipperTownCompatibilityService.IsSourceControlled))
        {
            NPC? actor = PelipperTownCompatibilityService.ResolveActor(unit);
            if (actor is null)
                continue;

            PelipperSourceDeploymentAlpha6613[actor] = IsSlotReservedAlpha6617(unit.State);
        }

        if (!changed)
            return;

        SavePartyNow();
        BroadcastPartySnapshot();
        Monitor.Log(
            $"Alpha 6.6.17 reconciled Pelipper live slot truth. Shared combat companions: {Party.GetActiveCombatCompanionCount()}/{max}.",
            LogLevel.Debug);
    }

    private void OnAlpha6617SlotCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_slots requires a loaded save.", LogLevel.Info);
            return;
        }

        RefreshCompanionSlotTruthForDecisionAlpha6618();
        List<LiveCompanionDescriptor> livePlayers = CompanionIntegrationService.FindPlayerSummons()
            .Where(PelipperTownCompatibilityService.IsPelipperDescriptor)
            .ToList();
        List<CompanionUnitData> effective = GetEffectiveCombatCompanionsAlpha6618();
        int max = GetCompanionCapAlpha6618();

        Monitor.Log(
            $"Team Up slot truth: reserved={Party.GetActiveCombatCompanionCount()}/{max}, effective={effective.Count}/{max}, livePelipperPlayer={livePlayers.Count}, runtimeRoot={PelipperApiRuntimeRootBridge.ApiTypeName}.",
            LogLevel.Info);

        foreach (CompanionUnitData unit in Party.CompanionUnits.Where(unit => unit.CountsTowardCombatCompanionLimit))
        {
            bool sourceLive;
            if (unit.OwnerKind == CompanionOwnerKind.Player)
            {
                sourceLive = livePlayers.Any(descriptor => descriptor.OwnerFarmerId == unit.RecruiterId
                    && descriptor.UnitId.Equals(unit.UnitId, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                NPC? owner = string.IsNullOrWhiteSpace(unit.OwnerCharacterName)
                    ? null
                    : Game1.getCharacterFromName(unit.OwnerCharacterName);
                LiveCompanionDescriptor? linkedLive = owner is null ? null : CompanionIntegrationService.FindLinkedCompanion(owner);
                sourceLive = PelipperTownCompatibilityService.IsPelipperDescriptor(linkedLive)
                    && linkedLive!.UnitId.Equals(unit.UnitId, StringComparison.OrdinalIgnoreCase);
            }

            bool effectiveSlot = effective.Any(item => item.UnitId.Equals(unit.UnitId, StringComparison.OrdinalIgnoreCase)
                && item.RecruiterId == unit.RecruiterId);
            Monitor.Log(
                $"slot unit={unit.DisplayName} owner={unit.OwnerKind}:{unit.OwnerCharacterName ?? unit.RecruiterId.ToString()} state={unit.State} provider={unit.ProviderId} sourceLive={sourceLive} effectiveSlot={effectiveSlot}",
                LogLevel.Info);
        }
    }

    private static bool IsSlotReservedAlpha6617(CompanionDeploymentState state)
        => state is CompanionDeploymentState.Active
            or CompanionDeploymentState.Waiting
            or CompanionDeploymentState.ReturningHome;
}
