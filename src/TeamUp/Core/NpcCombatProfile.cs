namespace Ronvotri.TeamUp.Core;

public sealed class NpcCombatProfile
{
    public string CharacterName { get; init; } = string.Empty;

    // Data-provider identity used by Codex filters. Core stays expansion-agnostic;
    // adapters can provide their own source IDs/labels later.
    public string SourceId { get; init; } = "stardew-valley";

    public string SourceLabel { get; init; } = "Stardew Valley";

    public PartyRole PrimaryRole { get; init; } = PartyRole.Unassigned;

    public PartyRole SecondaryRole { get; init; } = PartyRole.Unassigned;

    public EngagementStyle RecommendedEngagement { get; init; } = EngagementStyle.Balanced;

    public string PassiveKey { get; init; } = string.Empty;

    public string AbilityKey { get; init; } = string.Empty;

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
