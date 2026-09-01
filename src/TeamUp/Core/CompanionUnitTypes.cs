namespace Ronvotri.TeamUp.Core;

public enum CompanionOwnerKind
{
    Player,
    PartyMember
}

public enum CompanionUnitKind
{
    VanillaPet,
    ExternalCreature
}

public enum CompanionDeploymentState
{
    Standby,
    Active,
    Waiting,
    ReturningHome,
    Inactive
}
