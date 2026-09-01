namespace Ronvotri.TeamUp.Core;

public static class NpcProfileCatalog
{
    private static readonly Dictionary<string, NpcCombatProfile> Profiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Abigail"] = new NpcCombatProfile
            {
                CharacterName = "Abigail",
                PrimaryRole = PartyRole.Damage,
                SecondaryRole = PartyRole.Control,
                RecommendedEngagement = EngagementStyle.Aggressive,
                PassiveKey = "codex.abigail.passive",
                AbilityKey = "codex.abigail.ability",
                TankAffinity = 2,
                DamageAffinity = 5,
                SupportAffinity = 1,
                HealerAffinity = 1,
                ControlAffinity = 4
            },
            ["Alex"] = new NpcCombatProfile
            {
                CharacterName = "Alex",
                PrimaryRole = PartyRole.Tank,
                SecondaryRole = PartyRole.Damage,
                RecommendedEngagement = EngagementStyle.Balanced,
                PassiveKey = "codex.alex.passive",
                AbilityKey = "codex.alex.ability",
                TankAffinity = 5,
                DamageAffinity = 4,
                SupportAffinity = 1,
                HealerAffinity = 1,
                ControlAffinity = 2
            },
            ["Emily"] = new NpcCombatProfile
            {
                CharacterName = "Emily",
                PrimaryRole = PartyRole.Support,
                SecondaryRole = PartyRole.Healer,
                RecommendedEngagement = EngagementStyle.Cautious,
                PassiveKey = "codex.emily.passive",
                AbilityKey = "codex.emily.ability",
                TankAffinity = 1,
                DamageAffinity = 1,
                SupportAffinity = 5,
                HealerAffinity = 4,
                ControlAffinity = 2
            },
            ["Harvey"] = new NpcCombatProfile
            {
                CharacterName = "Harvey",
                PrimaryRole = PartyRole.Healer,
                SecondaryRole = PartyRole.Support,
                RecommendedEngagement = EngagementStyle.Cautious,
                PassiveKey = "codex.harvey.passive",
                AbilityKey = "codex.harvey.ability",
                TankAffinity = 1,
                DamageAffinity = 1,
                SupportAffinity = 4,
                HealerAffinity = 5,
                ControlAffinity = 1
            },
            ["Maru"] = new NpcCombatProfile
            {
                CharacterName = "Maru",
                PrimaryRole = PartyRole.Control,
                SecondaryRole = PartyRole.Support,
                RecommendedEngagement = EngagementStyle.Balanced,
                PassiveKey = "codex.maru.passive",
                AbilityKey = "codex.maru.ability",
                TankAffinity = 1,
                DamageAffinity = 2,
                SupportAffinity = 4,
                HealerAffinity = 2,
                ControlAffinity = 5
            }
        };

    public static IReadOnlyList<NpcCombatProfile> All => Profiles.Values
        .OrderBy(profile => profile.CharacterName)
        .ToList();

    public static NpcCombatProfile? Get(string characterName)
    {
        return Profiles.TryGetValue(characterName, out NpcCombatProfile? profile)
            ? profile
            : null;
    }
}
