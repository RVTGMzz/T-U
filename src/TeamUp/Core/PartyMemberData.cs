namespace Ronvotri.TeamUp.Core;

public sealed class PartyMemberData
{
    public string CharacterName { get; set; } = string.Empty;

    public long RecruiterId { get; set; }

    // Legacy alpha save field. New pets/creatures are stored as CompanionUnitData instead.
    public bool IsPet { get; set; }

    public string? LinkedCompanionUnitId { get; set; }

    public PartyRole Role { get; set; } = PartyRole.Unassigned;

    public EngagementStyle Engagement { get; set; } = EngagementStyle.Balanced;

    public PartyMemberState State { get; set; } = PartyMemberState.Following;
}
