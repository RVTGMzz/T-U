using Ronvotri.TeamUp.Core;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    /// <summary>
    /// Recruitment needs to know about a Pelipper villager partner even when the actor is dormant
    /// (sleeping / not materialized in the current location). Runtime combat truth continues to use
    /// FindLinkedCompanion; this helper is intentionally recruitment-only.
    /// </summary>
    private LiveCompanionDescriptor? FindRecruitCandidateCompanionAlpha671(NPC owner)
    {
        ConfigurePelipperApiBridgeAlpha6619();

        LiveCompanionDescriptor? live = CompanionIntegrationService.FindLinkedCompanion(owner);
        if (live is not null)
            return live;

        return PelipperTown119NativeBridge.TryGetConfiguredVillagerCompanionDescriptor(
            owner.Name,
            out LiveCompanionDescriptor? configured)
                ? configured
                : null;
    }

    /// <summary>
    /// A player explicitly selecting NPC + configured Pokemon reserves one of the shared 2/2 slots
    /// even while Pelipper keeps the Pokemon dormant. This closes the late-spawn 3/2 hole without
    /// pretending the dormant actor is physically source-live.
    /// </summary>
    private bool IsDormantConfiguredReservationAlpha671(CompanionUnitData unit, HashSet<long> online)
    {
        if (!unit.CountsTowardCombatCompanionLimit
            || !PelipperTownCompatibilityService.IsSourceControlled(unit)
            || unit.OwnerKind != CompanionOwnerKind.PartyMember
            || string.IsNullOrWhiteSpace(unit.OwnerCharacterName)
            || !IsSlotReservedAlpha6617(unit.State)
            || !online.Contains(unit.RecruiterId))
        {
            return false;
        }

        PartyMemberData? ownerMember = Party.Get(unit.OwnerCharacterName, unit.RecruiterId);
        if (ownerMember is null
            || ownerMember.State is not (PartyMemberState.Following or PartyMemberState.Waiting))
        {
            return false;
        }

        NPC? owner = Game1.getCharacterFromName(unit.OwnerCharacterName);
        if (owner is null || PelipperTownCompatibilityService.IsOwnerOptedOut(owner))
            return false;

        // If the actor is already live, normal source-live authority handles it instead.
        if (PelipperTownCompatibilityService.IsPelipperDescriptor(CompanionIntegrationService.FindLinkedCompanion(owner)))
            return false;

        if (!PelipperTown119NativeBridge.TryGetConfiguredVillagerCompanionDescriptor(
                owner.Name,
                out LiveCompanionDescriptor? configured)
            || configured is null)
        {
            return false;
        }

        // A synthetic configured row may later be replaced by the actor's stable runtime UnitId.
        // Owner identity is therefore the durable match, while provider/type prevents false claims.
        return configured.ProviderId.Equals(unit.ProviderId, StringComparison.OrdinalIgnoreCase);
    }
}
