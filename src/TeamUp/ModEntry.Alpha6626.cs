using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

/// <summary>
/// Alpha 6.6.26 collapses the historical Pelipper reconciliation stack into one periodic authority.
/// Old handlers remain compiled for UI/backwards compatibility, but they no longer mutate source
/// companion state autonomously on competing tick cadences.
/// </summary>
public sealed partial class ModEntry
{
    private const int CompanionAuthorityPulseTicksAlpha6626 = 10;

    private bool Alpha6626Registered;
    private bool Alpha6626Reconciling;
    private readonly HashSet<string> AuthorityReturnRequestsAlpha6626 = new(StringComparer.OrdinalIgnoreCase);
    private string LastAuthorityOverflowSignatureAlpha6626 = string.Empty;

    private void EnsureAlpha6626Registered()
    {
        if (Alpha6626Registered)
            return;

        Alpha6626Registered = true;

        // Historical state writers that previously fought each other:
        // Alpha663=30 ticks, Alpha6615=15, Alpha6617=10, Alpha6618=10.
        Helper.Events.GameLoop.UpdateTicked -= OnAlpha663UpdateTicked;
        Helper.Events.GameLoop.UpdateTicked -= OnAlpha6615UpdateTicked;
        Helper.Events.GameLoop.UpdateTicked -= OnAlpha6617UpdateTicked;
        Helper.Events.GameLoop.UpdateTicked -= OnAlpha6618UpdateTicked;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6626UpdateTicked;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6626ReturnedToTitle;

        Helper.ConsoleCommands.Add(
            "teamup_authority",
            "Show Team Up single companion-authority status and force one safe reconcile.",
            OnAlpha6626AuthorityCommand);

        Monitor.Log(
            "Team Up Alpha 6.6.26 single companion authority enabled: legacy Pelipper polling loops disabled.",
            LogLevel.Info);
    }

    private void OnAlpha6626UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer
            || !Context.IsWorldReady
            || !e.IsMultipleOf(CompanionAuthorityPulseTicksAlpha6626))
        {
            return;
        }

        ReconcileSingleCompanionAuthorityAlpha6626(repairOverflow: true);
    }

    private void OnAlpha6626ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        Alpha6626Reconciling = false;
        AuthorityReturnRequestsAlpha6626.Clear();
        LastAuthorityOverflowSignatureAlpha6626 = string.Empty;
    }

    private void ReconcileSingleCompanionAuthorityAlpha6626(bool repairOverflow)
    {
        if (Alpha6626Reconciling || !Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        Alpha6626Reconciling = true;
        try
        {
            ConfigurePelipperApiBridgeAlpha6619();

            HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
            int max = GetCompanionCapAlpha6618();
            bool changed = false;

            Dictionary<long, LiveCompanionDescriptor> livePlayers = CompanionIntegrationService.FindPlayerSummons()
                .Where(PelipperTownCompatibilityService.IsPelipperDescriptor)
                .Where(descriptor => descriptor.OwnerFarmerId.HasValue && online.Contains(descriptor.OwnerFarmerId.Value))
                .GroupBy(descriptor => descriptor.OwnerFarmerId!.Value)
                .ToDictionary(group => group.Key, group => group.Last());

            // Register live player companions as Standby first. Source truth, not PartyManager's
            // state-only count, decides whether they are promoted below.
            foreach ((long farmerId, LiveCompanionDescriptor descriptor) in livePlayers)
            {
                if (Party.GetCompanionByUnitId(descriptor.UnitId, farmerId) is not null)
                    continue;

                CompanionAddResult added = Party.TryAddPlayerCompanion(
                    descriptor.UnitId,
                    descriptor.CharacterName,
                    descriptor.DisplayName,
                    farmerId,
                    descriptor.ProviderId,
                    descriptor.ProviderUnitId,
                    requestActive: false);
                changed |= IsSuccessfulCompanionAddAlpha6626(added);
            }

            // Exact Pelipper 1.1.9 owner mapping is used through FindLinkedCompanion when bound.
            // Replace a stale linked Pokemon record if the NPC's configured/runtime partner changed.
            var liveNpcByOwner = new Dictionary<string, LiveCompanionDescriptor>(StringComparer.OrdinalIgnoreCase);
            foreach (PartyMemberData member in Party.Members.Where(member => IsActiveAuthorityOwnerAlpha6626(member, online)))
            {
                NPC? owner = Game1.getCharacterFromName(member.CharacterName);
                if (owner is null)
                    continue;

                LiveCompanionDescriptor? descriptor = CompanionIntegrationService.FindLinkedCompanion(owner);
                if (!PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor))
                    continue;

                string ownerKey = BuildAuthorityOwnerKeyAlpha6626(member.CharacterName, member.RecruiterId);
                liveNpcByOwner[ownerKey] = descriptor!;

                CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
                if (linked is not null
                    && PelipperTownCompatibilityService.IsSourceControlled(linked)
                    && !linked.UnitId.Equals(descriptor!.UnitId, StringComparison.OrdinalIgnoreCase))
                {
                    changed |= Party.RemoveCompanion(linked.UnitId, linked.RecruiterId);
                    linked = null;
                }

                if (linked is null)
                {
                    CompanionAddResult added = Party.TryLinkCompanion(
                        descriptor!.UnitId,
                        descriptor.CharacterName,
                        descriptor.DisplayName,
                        member.RecruiterId,
                        member.CharacterName,
                        CompanionUnitKind.ExternalCreature,
                        descriptor.ProviderId,
                        descriptor.ProviderUnitId,
                        requestActive: false);
                    changed |= IsSuccessfulCompanionAddAlpha6626(added);
                }
            }

            HashSet<string> liveKeys = new(StringComparer.OrdinalIgnoreCase);
            foreach ((long farmerId, LiveCompanionDescriptor descriptor) in livePlayers)
                liveKeys.Add(BuildAuthorityUnitKeyAlpha6626(descriptor.UnitId, farmerId));

            foreach (PartyMemberData member in Party.Members.Where(member => IsActiveAuthorityOwnerAlpha6626(member, online)))
            {
                string ownerKey = BuildAuthorityOwnerKeyAlpha6626(member.CharacterName, member.RecruiterId);
                if (liveNpcByOwner.TryGetValue(ownerKey, out LiveCompanionDescriptor? descriptor))
                    liveKeys.Add(BuildAuthorityUnitKeyAlpha6626(descriptor.UnitId, member.RecruiterId));
            }

            // A Pelipper record that is no longer source-live cannot reserve a Team Up slot.
            // Return-request latches are cleared only after the source actor actually disappears.
            foreach (CompanionUnitData unit in Party.CompanionUnits
                .Where(PelipperTownCompatibilityService.IsSourceControlled)
                .ToList())
            {
                string key = BuildAuthorityUnitKeyAlpha6626(unit.UnitId, unit.RecruiterId);
                if (liveKeys.Contains(key) || IsDormantConfiguredReservationAlpha671(unit, online))
                    continue;

                if (IsSlotReservedAlpha6617(unit.State))
                    changed |= Party.SetCompanionState(unit.UnitId, unit.RecruiterId, CompanionDeploymentState.Standby);
                AuthorityReturnRequestsAlpha6626.Remove(key);
            }

            // Manual NPC-only/Return intent always wins over capacity. If Pelipper still exposes the
            // actor, request native Return once and keep it physically counted until it disappears.
            HashSet<string> forcedReturnKeys = new(StringComparer.OrdinalIgnoreCase);
            foreach (PartyMemberData member in Party.Members.Where(member => IsActiveAuthorityOwnerAlpha6626(member, online)))
            {
                NPC? owner = Game1.getCharacterFromName(member.CharacterName);
                if (owner is null || !PelipperTownCompatibilityService.IsOwnerOptedOut(owner))
                    continue;

                CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
                if (linked is null || !PelipperTownCompatibilityService.IsSourceControlled(linked))
                    continue;

                string key = BuildAuthorityUnitKeyAlpha6626(linked.UnitId, linked.RecruiterId);
                if (!liveKeys.Contains(key))
                    continue;

                forcedReturnKeys.Add(key);
                if (IsSlotReservedAlpha6617(linked.State))
                    changed |= Party.SetCompanionState(linked.UnitId, linked.RecruiterId, CompanionDeploymentState.Standby);
                if (repairOverflow)
                    RequestAuthorityReturnAlpha6626(linked, "NPC-only/manual Standby");
            }

            // Non-Pelipper companions remain Team Up state-authoritative and consume the shared pool
            // first. Trim impossible stale state if an older save has more than max reserved.
            List<CompanionUnitData> nonPelipper = Party.CompanionUnits
                .Where(unit => unit.CountsTowardCombatCompanionLimit)
                .Where(unit => !PelipperTownCompatibilityService.IsSourceControlled(unit))
                .Where(unit => IsSlotReservedAlpha6617(unit.State))
                .ToList();

            for (int i = max; i < nonPelipper.Count; i++)
            {
                CompanionUnitData overflow = nonPelipper[i];
                changed |= Party.SetCompanionState(overflow.UnitId, overflow.RecruiterId, CompanionDeploymentState.Standby);
            }

            // Alpha 6.7.1: an explicitly selected NPC + Pokemon reserves capacity even while
            // Pelipper keeps that Pokemon asleep/dormant. Trim impossible old-save overflow first.
            int dormantBudget = Math.Max(0, max - Math.Min(max, nonPelipper.Count));
            List<CompanionUnitData> dormantPelipper = Party.CompanionUnits
                .Where(unit => IsDormantConfiguredReservationAlpha671(unit, online))
                .ToList();
            for (int i = dormantBudget; i < dormantPelipper.Count; i++)
            {
                CompanionUnitData overflow = dormantPelipper[i];
                changed |= Party.SetCompanionState(overflow.UnitId, overflow.RecruiterId, CompanionDeploymentState.Standby);
                if (repairOverflow)
                    RequestAuthorityReturnAlpha6626(overflow, "dormant configured 2/2 overflow");
            }

            int activeDormantReservations = dormantPelipper
                .Take(dormantBudget)
                .Count(unit => IsSlotReservedAlpha6617(unit.State));
            int availablePelipperSlots = Math.Max(0,
                max - Math.Min(max, nonPelipper.Count) - activeDormantReservations);

            // Only source-live Pelipper rows may be Active. Preserve already-active rows first, then
            // player companions, then NPC companions in stable roster order.
            List<CompanionUnitData> livePelipper = Party.CompanionUnits
                .Where(unit => unit.CountsTowardCombatCompanionLimit)
                .Where(PelipperTownCompatibilityService.IsSourceControlled)
                .Where(unit => liveKeys.Contains(BuildAuthorityUnitKeyAlpha6626(unit.UnitId, unit.RecruiterId)))
                .Where(unit => !forcedReturnKeys.Contains(BuildAuthorityUnitKeyAlpha6626(unit.UnitId, unit.RecruiterId)))
                .OrderByDescending(unit => IsSlotReservedAlpha6617(unit.State))
                .ThenBy(unit => unit.OwnerKind == CompanionOwnerKind.Player ? 0 : 1)
                .ToList();

            HashSet<string> allowed = livePelipper
                .Take(availablePelipperSlots)
                .Select(unit => BuildAuthorityUnitKeyAlpha6626(unit.UnitId, unit.RecruiterId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Release overflow first so stale Active rows cannot block the allowed rows from being
            // promoted by PartyManager's internal state-only guard.
            foreach (CompanionUnitData unit in livePelipper)
            {
                string key = BuildAuthorityUnitKeyAlpha6626(unit.UnitId, unit.RecruiterId);
                if (allowed.Contains(key))
                    continue;

                if (IsSlotReservedAlpha6617(unit.State))
                    changed |= Party.SetCompanionState(unit.UnitId, unit.RecruiterId, CompanionDeploymentState.Standby);
                if (repairOverflow)
                    RequestAuthorityReturnAlpha6626(unit, "hard shared 2/2 overflow");
            }

            foreach (CompanionUnitData unit in livePelipper)
            {
                string key = BuildAuthorityUnitKeyAlpha6626(unit.UnitId, unit.RecruiterId);
                if (!allowed.Contains(key) || unit.State == CompanionDeploymentState.Active)
                    continue;
                changed |= Party.SetCompanionState(unit.UnitId, unit.RecruiterId, CompanionDeploymentState.Active);
            }

            if (changed)
            {
                SavePartyNow();
                BroadcastPartySnapshot();
            }

            ReportAuthorityOverflowAlpha6626(max);
        }
        finally
        {
            Alpha6626Reconciling = false;
        }
    }

    private void RequestAuthorityReturnAlpha6626(CompanionUnitData unit, string reason)
    {
        string key = BuildAuthorityUnitKeyAlpha6626(unit.UnitId, unit.RecruiterId);
        if (!AuthorityReturnRequestsAlpha6626.Add(key))
            return;

        Party.SetCompanionState(unit.UnitId, unit.RecruiterId, CompanionDeploymentState.Standby);
        SetPelipperSourceDeploymentForUnitAlpha6615(unit, deployed: false);
        Monitor.Log($"Alpha 6.6.26 requested source-native Return for {unit.DisplayName} ({reason}).", LogLevel.Debug);
    }

    private void ReportAuthorityOverflowAlpha6626(int max)
    {
        int effective = GetEffectiveCombatCompanionCountAlpha6618();
        string signature = effective > max
            ? string.Join(",", GetEffectiveCombatCompanionsAlpha6618()
                .Select(unit => $"{unit.DisplayName}:{unit.RecruiterId}"))
            : string.Empty;

        if (!string.IsNullOrEmpty(signature)
            && !signature.Equals(LastAuthorityOverflowSignatureAlpha6626, StringComparison.Ordinal))
        {
            LastAuthorityOverflowSignatureAlpha6626 = signature;
            Monitor.Log(
                $"Alpha 6.6.26 detected physical companion overflow {effective}/{max}; native Return was requested and source-live slots stay counted until they actually disappear.",
                LogLevel.Warn);
        }
        else if (string.IsNullOrEmpty(signature))
        {
            LastAuthorityOverflowSignatureAlpha6626 = string.Empty;
        }
    }

    private void OnAlpha6626AuthorityCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_authority requires a loaded save.", LogLevel.Info);
            return;
        }

        if (Context.IsMainPlayer)
            ReconcileSingleCompanionAuthorityAlpha6626(repairOverflow: true);

        int max = GetCompanionCapAlpha6618();
        Monitor.Log(
            $"Team Up companion authority: single=True, reserved={Party.GetActiveCombatCompanionCount()}/{max}, effective={GetEffectiveCombatCompanionCountAlpha6618()}/{max}, legacyLoops=disabled, native119={PelipperTown119NativeBridge.Status}.",
            LogLevel.Info);
    }

    private static bool IsSuccessfulCompanionAddAlpha6626(CompanionAddResult result)
        => result is CompanionAddResult.AddedActive
            or CompanionAddResult.AddedStandby
            or CompanionAddResult.AddedStandbyLimitReached;

    private static bool IsActiveAuthorityOwnerAlpha6626(PartyMemberData member, HashSet<long> online)
        => online.Contains(member.RecruiterId)
            && member.State is PartyMemberState.Following or PartyMemberState.Waiting;

    private static string BuildAuthorityUnitKeyAlpha6626(string unitId, long recruiterId)
        => $"{recruiterId}|{unitId}";

    private static string BuildAuthorityOwnerKeyAlpha6626(string ownerName, long recruiterId)
        => $"{recruiterId}|{ownerName}";
}
