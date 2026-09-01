namespace Ronvotri.TeamUp.Core;

public sealed class CompanionUnitData
{
    public string UnitId { get; set; } = string.Empty;

    public string CharacterName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public long RecruiterId { get; set; }

    public CompanionOwnerKind OwnerKind { get; set; } = CompanionOwnerKind.Player;

    public string? OwnerCharacterName { get; set; }

    public CompanionUnitKind Kind { get; set; } = CompanionUnitKind.VanillaPet;

    public string ProviderId { get; set; } = "StardewValley";

    public string? ProviderUnitId { get; set; }

    public PartyRole Role { get; set; } = PartyRole.Unassigned;

    public CompanionDeploymentState State { get; set; } = CompanionDeploymentState.Active;

    public bool IsPlayerMainPet { get; set; }

    public bool CountsTowardPartyLimit => false;
}
