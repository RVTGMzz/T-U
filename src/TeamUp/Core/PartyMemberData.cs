namespace Ronvotri.TeamUp.Core;

public sealed class PartyMemberData
{
    public string CharacterName { get; set; } = string.Empty;

    public long RecruiterId { get; set; }

    public bool IsPet { get; set; }

    public PartyRole Role { get; set; } = PartyRole.Unassigned;

    public PartyMemberState State { get; set; } = PartyMemberState.Following;
}
