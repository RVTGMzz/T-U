using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

/// <summary>
/// Alpha 6.6.26 collapses the historical Pelipper reconciliation stack into one periodic authority.
/// Older handlers stay compiled for backwards-compatible UI/helpers, but they no longer mutate
/// companion state autonomously on separate tick cadences.
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

        // Four historical loops previously judged the same Pelipper slot state independently:
        // Alpha663 (30 ticks), Alpha6615 (15), Alpha6617 (10), and Alpha6618 (10). The first two
        // were the direct source of the 1/2 -> 2/2 -> 3/2 -> 4/2 oscillation seen in live tests.
        // Remove every autonomous judge, then install one ordered source-truth authority.
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

    /// <summary>
    /// One ordered pass owns periodic Team Up state for source-controlled companions:
    /// 1) register every live Pelipper actor in Team Up as Standby first;
    /// 2) clear stale records whose source is no longer live;
    /// 3) mirror at most two real source-live companions as Active;
    /// 4) ask the source mod to recall every physical overflow instead of hiding actors.
    /// </summary>
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

            // Pelipper exposes one active player partner per Farmer. Register source-live actors as
            // Standby first so PartyManager's state-only quota can never reject their roster record.
            Dictionary<long, LiveCompanionDescriptor> livePlayers = CompanionIntegrationService.FindPlayerSummons()
                .Where(PelipperTownCompatibilityService.IsPelipperDescriptor)
                .Where(descriptor => descriptor.OwnerFarmerId.HasValue && online.Contains(descriptor.OwnerFarmerId.Value))
                .GroupBy(descriptor => descriptor.OwnerFarmerId!.Value)
                .ToDictionary(group => group.Key, group => group.Last());

            foreach ((long farmerId, LiveCompanionDescriptor descriptor) in livePlayers)
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
                        requestActive: false);
                    changed |= added is CompanionAddResult.AddedActive
                        or CompanionAddResult.AddedStandby
                        or CompanionAddResult.AddedStandbyLimitReached;
                }
            }

            // Register each currently live NPC partner against its active Team Up owner. If the NPC
            // changed assigned Pokemon in Pelipper, replace the stale linked record rather than
            // leaving a ghost link that makes the real Pokemon disappear from Team Up UI.
            var liveNpcByOwner = new Dictionary<string, LiveCompanionDescriptor>(StringComparer.OrdinalIgnoreCase);
            foreach (PartyMemberData member in Party.Members.Where(member =>
                online.Contains(member.RecruiterId)
                && member.State is PartyMemberState.Following or PartyMemberState.Waiting))
            {
                NPC? owner = Game1.getCharacterFromName(member.CharacterName);
                if (owner is null)
                    continue;

                LiveCompanionDescriptor? descriptor = CompanionIntegrationService.FindLinkedCompanion(owner);
                if (!PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor))
                    continue;

                liveNpcByOwner[BuildAuthorityOwnerKeyAlpha6626(member.CharacterName, member.RecruiterId)] = descriptor!;
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
                    changed |= added is CompanionAddResult.AddedActive
                        or CompanionAddResult.AddedStandby
                        or CompanionAddResult.AddedStandbyLimitReached;
                }
            }

            // Source-live identity set. A Pelipper record that is not in this set must not reserve
            // a Team Up slot merely because an older save or reconciler left it Active.
            var liveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach ((long farmerId, LiveCompanionDescriptor descriptor) in livePlayers)
                liveKeys.Add(BuildAuthorityUnitKeyAlpha6626(descriptor.UnitId, farmerId));

            foreach (PartyMemberData member in Party.Members.Where(member =>
                online.Contains(member.RecruiterId)
                && member.State is PartyMemberState.Following or PartyMemberState.Waiting))
            {
                string ownerKey = BuildAuthorityOwnerKeyAlpha6626(member.CharacterName, member.RecruiterId);
                if (!liveNpcByOwner.TryGetValue(ownerKey, out LiveCompanionDescriptor? descriptor))
                    continue;
                liveKeys.Add(BuildAuthorityUnitKeyAlpha6626(descriptor.UnitId, member.RecruiterId));
            }

            foreach (CompanionUnitData unit in Party.CompanionUnits
                .Where(PelipperTownCompatibilityService.IsSourceControlled)
                .ToList())
            {
                string key = BuildAuthorityUnitKeyAlpha6626(unit.UnitId, unit.RecruiterId);
                if (liveKeys.Contains(key))
                {
                    AuthorityReturnRequestsAlpha6626.Remove(key);
                    continue;
                }

                if (IsSlotReservedAlpha6617(unit.State))
                    changed |= Party.SetCompanionState(unit.UnitId, unit.RecruiterId, CompanionDeploymentState.Standby);
                AuthorityReturnRequestsAlpha6626.Remove(key);
            }

            // NPC-only/manual Return is a durable intent. A source actor that is somehow still live
            // is never eligible for the two allowed slots and is recalled natively once.
            var forcedReturnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PartyMemberData member in Party.Members.Where(member =>
                online.Contains(member.RecruiterId)
                && member.State is PartyMemberState.Following or PartyMemberState.Waiting))
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
                changed |= Party.SetCompanionState(linked.UnitId, linked.RecruiterId, CompanionDeploymentState.Standby);
                RequestAuthorityReturnAlpha6626(linked, "NPC-only/manual Standby");
            }

            // State-authoritative non-Pelipper companions consume the pool first. Pelipper actors
            // then fill only the remaining real slots. Existing Active Pelipper records receive
            // priority so a harmless refresh does not reshuffle the player's party.
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

            int occupiedByNonPelipper = Math.Min(max, nonPelipper.Count);
            int availablePelipperSlots = Math.Max(0, max - occupiedByNonPelipper);

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

            // First release physical overflow and clear its Team Up reservation. Do this before
            // promoting allowed records so PartyManager's state-only activation guard cannot be
            // poisoned by stale Active rows from an older build.
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

            int effective = GetEffectiveCombatCompanionCountAlpha6618();
            string overflowSignature = effective > max
                ? string.Join(",", GetEffectiveCombatCompanionsAlpha6618()
                    .Select(unit => $"{unit.DisplayName}:{unit.RecruiterId}"))
                : string.Empty;

            if (!string.IsNullOrEmpty(overflowSignature)
                && !overflowSignature.Equals(LastAuthorityOverflowSignatureAlpha6626, StringComparison.Ordinal))
            {
                LastAuthorityOverflowSignatureAlpha6626 = overflowSignature;
                Monitor.Log(
                    $"Alpha 6.6.26 detected physical companion overflow {effective}/{max}; native Return requests were issued and source-live slots remain counted until they actually disappear.",
                    LogLevel.Warn);
            }
            else if (string.IsNullOrEmpty(overflowSignature))
            {
                LastAuthorityOverflowSignatureAlpha6626 = string.Empty;
            }
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

    private static string BuildAuthorityUnitKeyAlpha6626(string unitId, long recruiterId)
        => $"{recruiterId}|{unitId}";

    private static string BuildAuthorityOwnerKeyAlpha6626(string ownerName, long recruiterId)
        => $"{recruiterId}|{ownerName}";
}
