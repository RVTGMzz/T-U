namespace Ronvotri.TeamUp.Core;

public sealed class NpcCombatProfile
{
    public required string CharacterName { get; init; }

    public required PartyRole PrimaryRole { get; init; }

    public required PartyRole SecondaryRole { get; init; }

    public required EngagementStyle RecommendedEngagement { get; init; }

    public required string PassiveKey { get; init; }

    public required string AbilityKey { get; init; }

    public int TankAffinity { get; init; }

    public int DamageAffinity { get; init; }

    public int SupportAffinity { get; init; }

    public int HealerAffinity { get; init; }

    public int ControlAffinity { get; init; }

    public int GetAffinity(PartyRole role)
    {
        return role switch
        {
            PartyRole.Tank => TankAffinity,
            PartyRole.Damage => DamageAffinity,
            PartyRole.Support => SupportAffinity,
            PartyRole.Healer => HealerAffinity,
            PartyRole.Control => ControlAffinity,
            _ => 0
        };
    }
}
