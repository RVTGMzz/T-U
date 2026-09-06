using Microsoft.Xna.Framework.Input;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.UI;
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
        Helper.Events.Input.ButtonPressed += OnAlpha665EquipmentButtonPressed;
        Helper.ConsoleCommands.Add(
            "teamup_pelipper",
            "Team Up Pelipper compatibility diagnostics: status|reconcile.",
            OnAlpha663PelipperCommand);
        RegisterAlpha6615Events();
    }

    private void OnAlpha665EquipmentButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not EquipmentMenu menu)
            return;

        // Route semantic Stardew inputs rather than assuming Xbox face-button labels.
        // Action activates/equips; Use Tool explicitly unequips the selected NPC slot.
        Buttons? routedButton = null;
        if (e.Button.IsActionButton())
            routedButton = Buttons.A;
        else if (e.Button.IsUseToolButton())
            routedButton = Buttons.X;

        if (!routedButton.HasValue)
            return;

        Helper.Input.Suppress(e.Button);
        menu.receiveGamePadButton(routedButton.Value);
    }

    private void OnAlpha663SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        // Alpha 6.6.9 migration: restore any visibility state created by old Team Up builds,
        // then clear soft deployment markers because the base SaveLoaded path deactivates roster entries.
        PelipperDeploymentStateService.CleanupLegacySuppressionOnAllPelipperActors();
        PelipperDeploymentStateService.ClearDesiredDeploymentOnAllPelipperActors();
    }

    private void OnAlpha663DayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        PelipperDeploymentStateService.CleanupLegacySuppressionOnAllPelipperActors();
        PelipperDeploymentStateService.ClearDesiredDeploymentOnAllPelipperActors();
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
                        changed |= Party.SetCompanionState(
                            linked.UnitId,
                            linked.RecruiterId,
                            CompanionDeploymentState.Standby);
                    }
                }
                else if (linked.State == CompanionDeploymentState.Standby
                    && Party.GetActiveCombatCompanionCount() < maxCompanions)
                {
                    // Alpha 6.6.15: only auto-promote when the owner is explicitly NOT opted out.
                    // Manual Return/NPC-only recruitment sets the durable owner opt-out marker.
                    changed |= Party.SetCompanionState(
                        linked.UnitId,
                        linked.RecruiterId,
                        CompanionDeploymentState.Active);
                }

                NPC? linkedActor = PelipperTownCompatibilityService.ResolveActor(linked);
                if (linkedActor is not null)
                {
                    bool deployed = ownerActive
                        && !optedOut
                        && IsPelipperUnitDeployedAlpha669(linked);
                    PelipperDeploymentStateService.SetDesiredDeployment(linkedActor, owner.Name, deployed);
                }
                continue;
            }

            if (!ownerActive)
                continue;

            LiveCompanionDescriptor? descriptor = CompanionIntegrationService.FindLinkedCompanion(owner);
            if (!PelipperTownCompatibilityService.IsPelipperDescriptor(descriptor))
                continue;

            NPC? actor = PelipperTownCompatibilityService.ResolveActor(descriptor!);

            // Alpha 6.6.15: even an NPC-only choice gets a durable Standby link once Pelipper
            // exposes the actor. This preserves the user's choice while still enabling a later
            // explicit Call command from that NPC's dialogue.
            bool requestActive = !optedOut && maxCompanions > 0;
            CompanionAddResult result = Party.TryLinkCompanion(
                descriptor!.UnitId,
                descriptor.CharacterName,
                descriptor.DisplayName,
                member.RecruiterId,
                member.CharacterName,
                CompanionUnitKind.ExternalCreature,
                descriptor.ProviderId,
                descriptor.ProviderUnitId,
                requestActive);

            if (result is CompanionAddResult.AddedActive
                or CompanionAddResult.AddedStandby
                or CompanionAddResult.AddedStandbyLimitReached)
            {
                changed = true;
            }

            linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
            if (actor is not null && linked is not null)
            {
                bool deployed = !optedOut && IsPelipperUnitDeployedAlpha669(linked);
                PelipperDeploymentStateService.SetDesiredDeployment(actor, owner.Name, deployed);
            }
        }

        if (!changed)
            return;

        SavePartyNow();
        BroadcastPartySnapshot();
        Monitor.Log(
            $"Alpha 6.6.15 reconciled Pelipper companion state while preserving explicit NPC-only/Call intent. Shared combat companion usage: {Party.GetActiveCombatCompanionCount()}/{maxCompanions}.",
            LogLevel.Debug);
    }

    private void ApplyPelipperRecruitChoiceAlpha663(
        NPC owner,
        LiveCompanionDescriptor? detectedCompanion,
        bool includeCompanion)
    {
        // Alpha 6.6.15: persist the user's choice even when Pelipper hasn't spawned/detected the
        // companion actor yet. Previously a null descriptor meant NPC-only was forgotten, then
        // reconcile could auto-add the Pokemon a few ticks later and displace another slot.
        PelipperTownCompatibilityService.SetOwnerOptOut(owner, !includeCompanion);

        if (!PelipperTownCompatibilityService.IsPelipperDescriptor(detectedCompanion))
            return;

        NPC? actor = PelipperTownCompatibilityService.ResolveActor(detectedCompanion!);
        if (actor is not null)
            PelipperDeploymentStateService.SetDesiredDeployment(actor, owner.Name, includeCompanion);
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

        PelipperDeploymentStateService.SetDesiredDeployment(
            actor,
            linked.OwnerCharacterName ?? string.Empty,
            IsPelipperUnitDeployedAlpha669(linked));
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
            PelipperDeploymentStateService.ClearDesiredDeployment(actor);
    }

    private static bool IsPelipperUnitDeployedAlpha669(CompanionUnitData unit)
        => unit.State is CompanionDeploymentState.Active
            or CompanionDeploymentState.Waiting
            or CompanionDeploymentState.ReturningHome;

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
            && IsPelipperUnitDeployedAlpha669(unit));
        int detected = 0;
        int softStandby = 0;
        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC actor in location.characters.OfType<NPC>())
            {
                if (!PelipperTownCompatibilityService.LooksLikePelipperActor(actor))
                    continue;

                if (actor.modData.TryGetValue(PelipperDeploymentStateService.DeploymentStateKey, out string? state)
                    && state.Equals(PelipperDeploymentStateService.StandbyValue, StringComparison.OrdinalIgnoreCase))
                {
                    softStandby++;
                }
            }
        }

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
            $"Pelipper compatibility: detectedPartners={detected}, registered={registered}, activeSlots={active}/2, softStandby={softStandby}, totalCombatCompanions={Party.GetActiveCombatCompanionCount()}/2, captureSafety={PelipperCaptureSafetyService.CurrentEnabled}@{PelipperCaptureSafetyService.CurrentThreshold:P0}.",
            LogLevel.Info);
    }
}
