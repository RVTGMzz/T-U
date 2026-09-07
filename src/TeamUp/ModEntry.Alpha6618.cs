using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int PelipperNpcSlotTruthPulseTicksAlpha6618 = 10;
    private bool Alpha6618EventsRegistered;
    private readonly HashSet<string> PelipperNpcNativeControlWarningsAlpha6618 = new(StringComparer.OrdinalIgnoreCase);

    private void EnsureAlpha6618EventsRegistered()
    {
        if (Alpha6618EventsRegistered)
            return;

        Alpha6618EventsRegistered = true;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6618UpdateTicked;
        Helper.Events.GameLoop.DayEnding += OnAlpha6618DayEnding;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6618ReturnedToTitle;
    }

    private void OnAlpha6618UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer
            || !Context.IsWorldReady
            || !e.IsMultipleOf(PelipperNpcSlotTruthPulseTicksAlpha6618))
        {
            return;
        }

        ReconcilePelipperNpcSlotTruthAlpha6618();
    }

    private void OnAlpha6618DayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        PelipperVillagerCompanionRuntimeBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperNpcNativeControlWarningsAlpha6618.Clear();
    }

    private void OnAlpha6618ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PelipperVillagerCompanionRuntimeBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperNpcNativeControlWarningsAlpha6618.Clear();
    }

    /// <summary>
    /// Mirrors live Pelipper villager-partner state for active Team Up NPC owners. Manual NPC-only
    /// or Return intent is authoritative: Team Up asks Pelipper's own runtime/config to disable
    /// that villager partner instead of hiding the actor. Inactive owners are released back to
    /// Pelipper's original setting and don't consume Team Up companion capacity.
    /// </summary>
    private void ReconcilePelipperNpcSlotTruthAlpha6618()
    {
        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        bool changed = false;

        foreach (PartyMemberData member in Party.Members.ToList())
        {
            NPC? owner = Game1.getCharacterFromName(member.CharacterName);
            if (owner is null)
                continue;

            bool ownerActive = online.Contains(member.RecruiterId)
                && member.State is PartyMemberState.Following or PartyMemberState.Waiting;
            CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);

            if (!ownerActive)
            {
                if (linked is not null
                    && PelipperTownCompatibilityService.IsSourceControlled(linked)
                    && IsSlotReservedAlpha6617(linked.State))
                {
                    changed |= Party.SetCompanionState(
                        linked.UnitId,
                        linked.RecruiterId,
                        CompanionDeploymentState.Standby);
                }

                // Alpha 6.6.25: release the exact native 1.1.9 override when the NPC leaves Team
                // Up. This restores Pelipper's original companion setting and lets Pelipper own the
                // NPC+Pokemon relationship again outside the party.
                RestorePelipperNpcSourceAlpha6618(owner);
                continue;
            }

            LiveCompanionDescriptor? detected = CompanionIntegrationService.FindLinkedCompanion(owner);
            bool sourceLive = PelipperTownCompatibilityService.IsPelipperDescriptor(detected);

            if (linked is null && sourceLive)
            {
                bool optedOutBeforeLink = PelipperTownCompatibilityService.IsOwnerOptedOut(owner);
                CompanionAddResult added = Party.TryLinkCompanion(
                    detected!.UnitId,
                    detected.CharacterName,
                    detected.DisplayName,
                    member.RecruiterId,
                    member.CharacterName,
                    CompanionUnitKind.ExternalCreature,
                    detected.ProviderId,
                    detected.ProviderUnitId,
                    requestActive: false);

                if (added is CompanionAddResult.AddedActive
                    or CompanionAddResult.AddedStandby
                    or CompanionAddResult.AddedStandbyLimitReached)
                {
                    changed = true;
                }

                linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
                if (optedOutBeforeLink && linked is not null)
                {
                    changed |= Party.SetCompanionState(
                        linked.UnitId,
                        linked.RecruiterId,
                        CompanionDeploymentState.Standby);
                }
            }

            if (linked is null || !PelipperTownCompatibilityService.IsSourceControlled(linked))
                continue;

            bool optedOut = PelipperTownCompatibilityService.IsOwnerOptedOut(owner);
            if (optedOut)
            {
                if (IsSlotReservedAlpha6617(linked.State))
                {
                    changed |= Party.SetCompanionState(
                        linked.UnitId,
                        linked.RecruiterId,
                        CompanionDeploymentState.Standby);
                }

                if (sourceLive)
                    TrySetPelipperNpcSourceEnabledAlpha6618(owner, enabled: false, reason: "manual/NPC-only Standby");
                continue;
            }

            if (!sourceLive)
            {
                if (IsSlotReservedAlpha6617(linked.State))
                {
                    changed |= Party.SetCompanionState(
                        linked.UnitId,
                        linked.RecruiterId,
                        CompanionDeploymentState.Standby);
                }
                continue;
            }

            // A source-live partner with an active owner is a real slot consumer even if an older
            // Team Up record says Standby. Mirror it only when doing so doesn't exceed the hard cap.
            int max = GetCompanionCapAlpha6618();
            int effective = GetEffectiveCombatCompanionCountAlpha6618();
            if (!IsSlotReservedAlpha6617(linked.State) && max > 0 && effective <= max)
            {
                changed |= Party.SetCompanionState(
                    linked.UnitId,
                    linked.RecruiterId,
                    CompanionDeploymentState.Active);
            }
            else if (max <= 0 || effective > max)
            {
                if (IsSlotReservedAlpha6617(linked.State))
                {
                    changed |= Party.SetCompanionState(
                        linked.UnitId,
                        linked.RecruiterId,
                        CompanionDeploymentState.Standby);
                }
                TrySetPelipperNpcSourceEnabledAlpha6618(owner, enabled: false, reason: "hard 2/2 overflow");
            }
        }

        if (!changed)
            return;

        SavePartyNow();
        BroadcastPartySnapshot();
        Monitor.Log(
            $"Alpha 6.6.18 reconciled NPC-linked Pelipper source truth. effectiveSlots={GetEffectiveCombatCompanionCountAlpha6618()}/{GetCompanionCapAlpha6618()}.",
            LogLevel.Debug);
    }

    private int GetCompanionCapAlpha6618()
        => Config.AllowLinkedCompanions
            ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2)
            : 0;

    /// <summary>
    /// Returns real shared slot usage. This deliberately counts a live Pelipper NPC partner even
    /// when Team Up says Standby, because until Pelipper actually recalls it the creature remains
    /// physically deployed and must block a third companion from joining.
    /// </summary>
    private int GetEffectiveCombatCompanionCountAlpha6618()
        => GetEffectiveCombatCompanionsAlpha6618().Count;

    private List<CompanionUnitData> GetEffectiveCombatCompanionsAlpha6618()
    {
        var result = new List<CompanionUnitData>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();

        // Non-Pelipper providers remain Team Up state-authoritative.
        foreach (CompanionUnitData unit in Party.CompanionUnits
            .Where(unit => unit.CountsTowardCombatCompanionLimit)
            .Where(unit => !PelipperTownCompatibilityService.IsSourceControlled(unit))
            .Where(unit => IsSlotReservedAlpha6617(unit.State)))
        {
            if (ids.Add(unit.UnitId))
                result.Add(unit);
        }

        // Player Pelipper partners are source-authoritative from Alpha 6.6.17.
        foreach (LiveCompanionDescriptor live in CompanionIntegrationService.FindPlayerSummons()
            .Where(PelipperTownCompatibilityService.IsPelipperDescriptor)
            .Where(descriptor => descriptor.OwnerFarmerId.HasValue && online.Contains(descriptor.OwnerFarmerId.Value)))
        {
            CompanionUnitData? registered = Party.GetCompanionByUnitId(live.UnitId, live.OwnerFarmerId!.Value);
            if (registered is not null && registered.CountsTowardCombatCompanionLimit && ids.Add(registered.UnitId))
                result.Add(registered);
        }

        // Active NPC owners: source-visible linked partner consumes a slot even if Team Up's saved
        // state is temporarily Standby. Reserved state is also counted conservatively until the
        // source-truth reconcile proves it was recalled.
        foreach (PartyMemberData member in Party.Members.Where(member =>
            online.Contains(member.RecruiterId)
            && member.State is PartyMemberState.Following or PartyMemberState.Waiting))
        {
            CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
            if (linked is null
                || !linked.CountsTowardCombatCompanionLimit
                || !PelipperTownCompatibilityService.IsSourceControlled(linked))
            {
                continue;
            }

            NPC? owner = Game1.getCharacterFromName(member.CharacterName);
            bool sourceLive = owner is not null
                && PelipperTownCompatibilityService.IsPelipperDescriptor(CompanionIntegrationService.FindLinkedCompanion(owner));
            if ((sourceLive || IsSlotReservedAlpha6617(linked.State)) && ids.Add(linked.UnitId))
                result.Add(linked);
        }

        return result;
    }

    private void RefreshCompanionSlotTruthForDecisionAlpha6618()
    {
        ReconcilePelipperPlayerSlotTruthAlpha6617();
        ReconcilePelipperNpcSlotTruthAlpha6618();
    }

    private bool HasFreeEffectiveCompanionSlotAlpha6618()
    {
        RefreshCompanionSlotTruthForDecisionAlpha6618();
        int max = GetCompanionCapAlpha6618();
        return max > 0 && GetEffectiveCombatCompanionCountAlpha6618() < max;
    }

    private IReadOnlyList<CompanionUnitData> GetEffectiveReplaceableCompanionsAlpha6618(long recruiterId, bool hostMayReplaceAll)
    {
        RefreshCompanionSlotTruthForDecisionAlpha6618();
        return GetEffectiveCombatCompanionsAlpha6618()
            .Where(unit => hostMayReplaceAll || unit.RecruiterId == recruiterId)
            .Take(2)
            .ToList();
    }

    /// <summary>
    /// Final authoritative gate immediately before an NPC+Pokemon recruit is committed. UI checks
    /// are advisory only. If the real source-live pool is full, a replacement is mandatory and
    /// must actually release a slot before the NPC is added.
    /// </summary>
    private bool PrepareNpcCompanionRecruitCapacityAlpha6618(
        LiveCompanionDescriptor? incoming,
        long recruiterId,
        string? replacementUnitId,
        out string failure)
    {
        failure = string.Empty;
        if (!PelipperTownCompatibilityService.IsPelipperDescriptor(incoming))
            return true;

        RefreshCompanionSlotTruthForDecisionAlpha6618();
        int max = GetCompanionCapAlpha6618();
        if (max <= 0)
        {
            failure = "Companions are disabled in Team Up config.";
            return false;
        }

        int effective = GetEffectiveCombatCompanionCountAlpha6618();
        if (effective < max)
            return true;

        if (string.IsNullOrWhiteSpace(replacementUnitId))
        {
            failure = "COMPANION LIMIT 2/2 - choose a Pokemon to replace, or invite the NPC alone.";
            return false;
        }

        CompanionUnitData? replacement = GetEffectiveCombatCompanionsAlpha6618()
            .FirstOrDefault(unit => unit.UnitId.Equals(replacementUnitId, StringComparison.OrdinalIgnoreCase));
        bool requesterMayReplace = replacement is not null
            && (replacement.RecruiterId == recruiterId || recruiterId == Game1.player.UniqueMultiplayerID);
        if (!requesterMayReplace || replacement is null)
        {
            failure = "That companion is no longer occupying a replaceable slot.";
            return false;
        }

        PutCompanionOnStandbyAlpha6615(replacement, manualHold: true);
        RefreshCompanionSlotTruthForDecisionAlpha6618();
        if (GetEffectiveCombatCompanionCountAlpha6618() >= max)
        {
            failure = "The selected Pokemon is still deployed by its source mod, so Team Up cannot add a third companion.";
            return false;
        }

        return true;
    }

    private bool TrySetPelipperNpcSourceEnabledAlpha6618(NPC owner, bool enabled, string reason)
        => TrySetPelipperNpcSourceEnabledAlpha6619(owner, enabled, reason);

    private void RestorePelipperNpcSourceAlpha6618(NPC owner)
        => RestorePelipperNpcSourceAlpha6619(owner);}
