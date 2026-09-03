using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

public static class NpcProfileCatalog
{
    public const string StardewValleySourceId = "stardew-valley";

    private static readonly Dictionary<string, NpcCombatProfile> Profiles = BuildProfiles();

    public static IReadOnlyList<NpcCombatProfile> All => Profiles.Values
        .OrderBy(profile => profile.SourceLabel)
        .ThenBy(profile => profile.CharacterName)
        .ToList();

    public static IReadOnlyList<NpcCombatProfile> GetAvailableProfiles(IModRegistry modRegistry)
    {
        bool sveLoaded = modRegistry.IsLoaded(ExpansionNpcProfileCatalog.SveModId)
            || modRegistry.IsLoaded(ExpansionNpcProfileCatalog.SveCodeModId);
        bool rsvLoaded = modRegistry.IsLoaded(ExpansionNpcProfileCatalog.RsvModId);

        return All.Where(profile => profile.SourceId switch
            {
                StardewValleySourceId => true,
                ExpansionNpcProfileCatalog.SveSourceId => sveLoaded
                    && Game1.getCharacterFromName(profile.CharacterName) is not null,
                ExpansionNpcProfileCatalog.RsvSourceId => rsvLoaded
                    && Game1.getCharacterFromName(profile.CharacterName) is not null,
                _ => Game1.getCharacterFromName(profile.CharacterName) is not null
            })
            .ToList();
    }

    public static NpcCombatProfile? Get(string characterName)
    {
        return Profiles.TryGetValue(characterName, out NpcCombatProfile? profile)
            ? profile
            : null;
    }

    private static Dictionary<string, NpcCombatProfile> BuildProfiles()
    {
        var profiles = new Dictionary<string, NpcCombatProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["Abigail"] = P("Abigail", PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 2, 5, 1, 1, 4),
            ["Alex"] = P("Alex", PartyRole.Tank, PartyRole.Damage, EngagementStyle.Balanced, 5, 4, 1, 1, 2),
            ["Caroline"] = P("Caroline", PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 2, 1, 5, 4, 2),
            ["Clint"] = P("Clint", PartyRole.Tank, PartyRole.Control, EngagementStyle.Balanced, 4, 3, 2, 1, 4),
            ["Demetrius"] = P("Demetrius", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 2, 5),
            ["Elliott"] = P("Elliott", PartyRole.Support, PartyRole.Damage, EngagementStyle.Balanced, 2, 3, 5, 2, 3),
            ["Emily"] = P("Emily", PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 1, 1, 5, 4, 2),
            ["Evelyn"] = P("Evelyn", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 4, 5, 2),
            ["George"] = P("George", PartyRole.Tank, PartyRole.Control, EngagementStyle.Cautious, 4, 2, 2, 1, 4),
            ["Gus"] = P("Gus", PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 3, 2, 5, 4, 2),
            ["Haley"] = P("Haley", PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 4, 3, 1, 2),
            ["Harvey"] = P("Harvey", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 4, 5, 1),
            ["Jodi"] = P("Jodi", PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 2, 2, 5, 4, 2),
            ["Kent"] = P("Kent", PartyRole.Tank, PartyRole.Damage, EngagementStyle.Aggressive, 5, 4, 2, 1, 3),
            ["Leah"] = P("Leah", PartyRole.Damage, PartyRole.Control, EngagementStyle.Balanced, 3, 4, 3, 2, 4),
            ["Lewis"] = P("Lewis", PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 4, 2, 4, 2, 3),
            ["Linus"] = P("Linus", PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 3, 2, 4, 3, 5),
            ["Marnie"] = P("Marnie", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 2, 1, 5, 5, 2),
            ["Maru"] = P("Maru", PartyRole.Control, PartyRole.Support, EngagementStyle.Balanced, 1, 2, 4, 2, 5),
            ["Pam"] = P("Pam", PartyRole.Tank, PartyRole.Damage, EngagementStyle.Reckless, 5, 4, 1, 1, 2),
            ["Penny"] = P("Penny", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 5, 5, 2),
            ["Pierre"] = P("Pierre", PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 3, 4, 3, 1, 2),
            ["Robin"] = P("Robin", PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 5, 3, 4, 2, 2),
            ["Sam"] = P("Sam", PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 5, 3, 1, 3),
            ["Sandy"] = P("Sandy", PartyRole.Support, PartyRole.Control, EngagementStyle.Balanced, 2, 3, 5, 2, 4),
            ["Sebastian"] = P("Sebastian", PartyRole.Control, PartyRole.Damage, EngagementStyle.Cautious, 2, 4, 2, 1, 5),
            ["Shane"] = P("Shane", PartyRole.Damage, PartyRole.Tank, EngagementStyle.Aggressive, 4, 5, 1, 1, 2),
            ["Willy"] = P("Willy", PartyRole.Damage, PartyRole.Control, EngagementStyle.Balanced, 3, 4, 2, 2, 4),
            ["Wizard"] = P("Wizard", PartyRole.Control, PartyRole.Damage, EngagementStyle.Cautious, 2, 4, 3, 2, 5),
        };

        foreach (NpcCombatProfile profile in ExpansionNpcProfileCatalog.All)
            profiles[profile.CharacterName] = profile;

        return profiles;
    }

    private static NpcCombatProfile P(
        string name,
        PartyRole primary,
        PartyRole secondary,
        EngagementStyle engagement,
        int tank,
        int damage,
        int support,
        int healer,
        int control)
    {
        string key = name.ToLowerInvariant();
        return new NpcCombatProfile
        {
            CharacterName = name,
            SourceId = StardewValleySourceId,
            SourceLabel = "Stardew Valley",
            PrimaryRole = primary,
            SecondaryRole = secondary,
            RecommendedEngagement = engagement,
            PassiveKey = $"codex.{key}.passive",
            AbilityKey = $"codex.{key}.ability",
            TankAffinity = tank,
            DamageAffinity = damage,
            SupportAffinity = support,
            HealerAffinity = healer,
            ControlAffinity = control
        };
    }
}
