using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int PelipperReconcileIntervalTicksAlpha663 = 30;

    private void RegisterAlpha663HotfixEvents()
    {
        Helper.Events.GameLoop.SaveLoaded += OnAlpha663SaveLoaded;
        Helper.Events.GameLoop.DayEnding += OnAlpha663DayEnding;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha663UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_pelipper",
            "Team Up Pelipper compatibility diagnostics: status|reconcile.",
            OnAlpha663PelipperCommand);
    }

    private void OnAlpha663SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        // Base SaveLoaded intentionally deactivates the roster. Restore any visual suppression
        // left by a previous session before live Team Up members are activated again.
        PelipperTownCompatibilityService.CleanupOrphanedSuppression(Array.Empty<string>());
    }

    private void OnAlpha663DayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        PelipperTownCompatibilityService.CleanupOrphanedSuppression(Array.Empty<string>());
    }

    private void OnAlpha663UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer
            || !Context.IsWorldReady
            || !e.IsMultipleOf(PelipperReconcileIntervalTicksAlpha663))
        {
            return;
        }

        ReconcilePelipperCompanionsAlpha663();
    }

    private void ReconcilePelipperCompanionsAlpha663()
    {
        HashSet<long> onlineFarmerIds = GetOnlineFarmerIds().ToHashSet();
        int maxCompanions = Config.AllowLinkedCompanions
            ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2)
            : 0;
        bool changed = false;

        List<PartyMemberData> activeMembers = Party.Members
            .Where(member => onlineFarmerIds.Contains(member.RecruiterId))
            .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .ToList();
        HashSet<string> activeOwnerNames = activeMembers
            .Select(member => member.CharacterName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (PartyMemberData member in Party.Members)
        {
            NPC? owner = Game1.getCharacterFromName(member.CharacterName);
            if (owner is null)
                continue;

            bool ownerActive = onlineFarmerIds.Contains(member.RecruiterId)
                && member.State is PartyMemberState.Following or PartyMemberState.Waiting;
            bool optedOut = PelipperTownCompatibilityService.IsOwnerOptedOut(owner);
            CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);

            if (linked is not null && PelipperTownCompatibilityService.IsSourceControlled(linked))
            {
                if (!ownerActive || optedOut || maxCompanions <= 0)
                {
                    if (linked.State is CompanionDeploymentState.Active
                        or CompanionDeploymentState.Waiting
                        or CompanionDeploymentState.ReturningHome)
                    {
                        changed |= Party.SetCompanionState(linked.UnitId, linked.RecruiterId, CompanionDeploymentState.Standby);
                    }
                }
                else if (linked.State == CompanionDeploymentState.Standby
                    && Party.GetActiveCombatCompanionCount() < maxCompanions)
                {
                    changed |= Party.SetCompanionState(linked.UnitId, linked.RecruiterId, CompanionDeploymentState.Active);
                }

                NPC? linkedActor = PelipperTownCompatibilityService.ResolveActor(linked);
                if (linkedActor is not null)
                {
                    bool deployed = ownerActive
                        && !optedOut
                        && linked.State is CompanionDeploymentState.Active
                            or CompanionDeploymentState.Waiting
                            or CompanionDeploymentState.ReturningHome;
                    PelipperTownCompatibilityService.SetSuppressed(linkedActor, owner.Name, !deployed);
                }
                continue;
            }

            if (!ownerActive)
                continue;

            LiveCompanionDescriptor? descriptor = CompanionIntegrationService.FindLinkedCompanion(owner);
            if (!PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor))
                continue;

            NPC? actor = PelipperTownCompatibilityService.ResolveActor(descriptor!);
            if (optedOut || maxCompanions <= 0)
            {
                if (actor is not null)
                    PelipperTownCompatibilityService.SetSuppressed(actor, owner.Name, true);
                continue;
            }

            CompanionAddResult result = Party.TryLinkCompanion(
                descriptor!.UnitId,
                descriptor.CharacterName,
                descriptor.DisplayName,
                member.RecruiterId,
                member.CharacterName,
                CompanionUnitKind.ExternalCreature,
                descriptor.ProviderId,
                descriptor.ProviderUnitId,
                requestActive: true);

            if (result is CompanionAddResult.AddedActive
                or CompanionAddResult.AddedStandby
                or CompanionAddResult.AddedStandbyLimitReached)
            {
                changed = true;
            }

            linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
            if (actor is not null && linked is not null)
            {
                bool deployed = linked.State is CompanionDeploymentState.Active
                    or CompanionDeploymentState.Waiting
                    or CompanionDeploymentState.ReturningHome;
                PelipperTownCompatibilityService.SetSuppressed(actor, owner.Name, !deployed);
            }
        }

        PelipperTownCompatibilityService.CleanupOrphanedSuppression(activeOwnerNames);

        if (!changed)
            return;

        SavePartyNow();
        BroadcastPartySnapshot();
        Monitor.Log(
            $"Alpha 6.6.3 reconciled Pelipper Town companions. Shared combat companion usage: {Party.GetActiveCombatCompanionCount()}/{maxCompanions}.",
            LogLevel.Debug);
    }

    private void ApplyPelipperRecruitChoiceAlpha663(
        NPC owner,
        LiveCompanionDescriptor? detectedCompanion,
        bool includeCompanion)
    {
        if (!PelipperTownCompatibilityService.IsPelipperDescriptor(detectedCompanion))
            return;

        PelipperTownCompatibilityService.SetOwnerOptOut(owner, !includeCompanion);
        NPC? actor = PelipperTownCompatibilityService.ResolveActor(detectedCompanion!);
        if (actor is not null)
            PelipperTownCompatibilityService.SetSuppressed(actor, owner.Name, !includeCompanion);
    }

    private void ApplyPelipperLinkedDeploymentAlpha663(
        CompanionUnitData? linked,
        LiveCompanionDescriptor? descriptor)
    {
        if (linked is null || !PelipperTownCompatibilityService.IsSourceControlled(linked))
            return;

        NPC? actor = PelipperTownCompatibilityService.ResolveActor(descriptor ?? new LiveCompanionDescriptor
        {
            UnitId = linked.UnitId,
            CharacterName = linked.CharacterName,
            DisplayName = linked.DisplayName,
            OwnerKind = linked.OwnerKind,
            OwnerCharacterName = linked.OwnerCharacterName,
            OwnerFarmerId = linked.OwnerKind == CompanionOwnerKind.Player ? linked.RecruiterId : null,
            ProviderId = linked.ProviderId,
            ProviderUnitId = linked.ProviderUnitId
        });
        if (actor is null)
            return;

        bool deployed = linked.State is CompanionDeploymentState.Active
            or CompanionDeploymentState.Waiting
            or CompanionDeploymentState.ReturningHome;
        PelipperTownCompatibilityService.SetSuppressed(actor, linked.OwnerCharacterName ?? string.Empty, !deployed);
    }

    private void ReleasePelipperOwnerAlpha663(NPC owner, CompanionUnitData? linked)
    {
        PelipperTownCompatibilityService.SetOwnerOptOut(owner, false);

        NPC? actor = null;
        if (linked is not null && PelipperTownCompatibilityService.IsSourceControlled(linked))
        {
            actor = PelipperTownCompatibilityService.ResolveActor(linked);
        }
        else
        {
            LiveCompanionDescriptor? detected = CompanionIntegrationService.FindLinkedCompanion(owner);
            if (PelipperTownCompatibilityService.IsPelipperDescriptor(detected))
                actor = PelipperTownCompatibilityService.ResolveActor(detected!);
        }

        if (actor is not null)
            PelipperTownCompatibilityService.SetSuppressed(actor, owner.Name, false);
    }

    private void OnAlpha663PelipperCommand(string command, string[] args)
    {
        string mode = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_pelipper requires a loaded save.", LogLevel.Info);
            return;
        }

        if (mode == "reconcile")
        {
            if (!Context.IsMainPlayer)
            {
                Monitor.Log("teamup_pelipper reconcile is host-only.", LogLevel.Info);
                return;
            }
            ReconcilePelipperCompanionsAlpha663();
        }

        int registered = Party.CompanionUnits.Count(PelipperTownCompatibilityService.IsSourceControlled);
        int active = Party.CompanionUnits.Count(unit =>
            PelipperTownCompatibilityService.IsSourceControlled(unit)
            && unit.State is CompanionDeploymentState.Active
                or CompanionDeploymentState.Waiting
                or CompanionDeploymentState.ReturningHome);
        int detected = 0;
        foreach (PartyMemberData member in Party.Members)
        {
            NPC? owner = Game1.getCharacterFromName(member.CharacterName);
            if (owner is not null
                && PelipperTownCompatibilityService.IsPelipperDescriptor(CompanionIntegrationService.FindLinkedCompanion(owner)))
            {
                detected++;
            }
        }

        Monitor.Log(
            $"Pelipper compatibility: detectedPartners={detected}, registered={registered}, activeSlots={active}/2, totalCombatCompanions={Party.GetActiveCombatCompanionCount()}/2.",
            LogLevel.Info);
    }
}
