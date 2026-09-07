namespace Ronvotri.TeamUp.Core;

internal static class ExpansionNpcProfileCatalog
{
    public const string SveSourceId = "stardew-valley-expanded";
    public const string RsvSourceId = "ridgeside-village";
    public const string SveModId = "FlashShifter.StardewValleyExpandedCP";
    public const string SveCodeModId = "FlashShifter.SVECode";
    public const string RsvModId = "Rafseazz.RSVCP";

    public static IReadOnlyList<NpcCombatProfile> All { get; } = new NpcCombatProfile[]
    {
        E("Alesia", SveSourceId, "Stardew Valley Expanded", PartyRole.Damage, PartyRole.Tank, EngagementStyle.Aggressive, 4, 5, 1, 1, 3),
        E("Andy", SveSourceId, "Stardew Valley Expanded", PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 5, 3, 3, 1, 2),
        X("Apples", SveSourceId, "Stardew Valley Expanded"),
        E("Camilla", SveSourceId, "Stardew Valley Expanded", PartyRole.Control, PartyRole.Damage, EngagementStyle.Aggressive, 2, 4, 3, 1, 5),
        X("Charlie", SveSourceId, "Stardew Valley Expanded"),
        E("Claire", SveSourceId, "Stardew Valley Expanded", PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 1, 2, 5, 4, 2),
        X("Hank", SveSourceId, "Stardew Valley Expanded"),
        E("Isaac", SveSourceId, "Stardew Valley Expanded", PartyRole.Damage, PartyRole.Tank, EngagementStyle.Reckless, 4, 5, 1, 1, 3),
        E("Jadu", SveSourceId, "Stardew Valley Expanded", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 3, 4, 2, 5),
        X("Jolyne", SveSourceId, "Stardew Valley Expanded"),
        E("Lance", SveSourceId, "Stardew Valley Expanded", PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 3, 5, 2, 1, 4),
        E("Martin", SveSourceId, "Stardew Valley Expanded", PartyRole.Support, PartyRole.Damage, EngagementStyle.Balanced, 2, 3, 4, 2, 2),
        E("Morgan", SveSourceId, "Stardew Valley Expanded", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 1, 3, 4, 3, 5),
        E("Olivia", SveSourceId, "Stardew Valley Expanded", PartyRole.Support, PartyRole.Control, EngagementStyle.Balanced, 2, 2, 5, 3, 4),
        X("Peaches", SveSourceId, "Stardew Valley Expanded"),
        X("Scarlett", SveSourceId, "Stardew Valley Expanded"),
        E("Sophia", SveSourceId, "Stardew Valley Expanded", PartyRole.Support, PartyRole.Damage, EngagementStyle.Cautious, 2, 4, 5, 2, 2),
        X("Suki", SveSourceId, "Stardew Valley Expanded"),
        X("Susan", SveSourceId, "Stardew Valley Expanded"),
        X("Treyvon", SveSourceId, "Stardew Valley Expanded"),
        E("Victor", SveSourceId, "Stardew Valley Expanded", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 2, 5),
        X("Acorn", RsvSourceId, "Ridgeside Village"),
        E("Aguar", RsvSourceId, "Ridgeside Village", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 3, 5),
        X("Alissa", RsvSourceId, "Ridgeside Village"),
        X("Anton", RsvSourceId, "Ridgeside Village"),
        X("Ariah", RsvSourceId, "Ridgeside Village"),
        X("Belinda", RsvSourceId, "Ridgeside Village"),
        X("Bert", RsvSourceId, "Ridgeside Village"),
        E("Blair", RsvSourceId, "Ridgeside Village", PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 5, 3, 1, 2),
        X("Bliss", RsvSourceId, "Ridgeside Village"),
        X("Bryle", RsvSourceId, "Ridgeside Village"),
        E("Carmen", RsvSourceId, "Ridgeside Village", PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 3, 2, 5, 4, 2),
        X("Corine", RsvSourceId, "Ridgeside Village"),
        E("Daia", RsvSourceId, "Ridgeside Village", PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 3, 5, 2, 1, 4),
        X("Ezekiel", RsvSourceId, "Ridgeside Village"),
        X("Faye", RsvSourceId, "Ridgeside Village"),
        X("Flor", RsvSourceId, "Ridgeside Village"),
        X("Freddie", RsvSourceId, "Ridgeside Village"),
        X("Helen", RsvSourceId, "Ridgeside Village"),
        E("Ian", RsvSourceId, "Ridgeside Village", PartyRole.Tank, PartyRole.Damage, EngagementStyle.Balanced, 4, 4, 2, 1, 3),
        X("Irene", RsvSourceId, "Ridgeside Village"),
        X("Jeric", RsvSourceId, "Ridgeside Village"),
        E("Jio", RsvSourceId, "Ridgeside Village", PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 3, 5, 2, 1, 5),
        E("June", RsvSourceId, "Ridgeside Village", PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 1, 3, 5, 3, 4),
        X("Keahi", RsvSourceId, "Ridgeside Village"),
        E("Kenneth", RsvSourceId, "Ridgeside Village", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 2, 5),
        E("Kiarra", RsvSourceId, "Ridgeside Village", PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 5, 3, 1, 3),
        X("Kimpoi", RsvSourceId, "Ridgeside Village"),
        X("Kiwi", RsvSourceId, "Ridgeside Village"),
        X("Lenny", RsvSourceId, "Ridgeside Village"),
        X("Lola", RsvSourceId, "Ridgeside Village"),
        X("Lorenzo", RsvSourceId, "Ridgeside Village"),
        X("Louie", RsvSourceId, "Ridgeside Village"),
        E("Maddie", RsvSourceId, "Ridgeside Village", PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 5, 5, 2),
        X("Maive", RsvSourceId, "Ridgeside Village"),
        X("Malaya", RsvSourceId, "Ridgeside Village"),
        X("Naomi", RsvSourceId, "Ridgeside Village"),
        X("Olga", RsvSourceId, "Ridgeside Village"),
        X("Paula", RsvSourceId, "Ridgeside Village"),
        X("Philip", RsvSourceId, "Ridgeside Village"),
        X("Pika", RsvSourceId, "Ridgeside Village"),
        X("Pipo", RsvSourceId, "Ridgeside Village"),
        X("Raeriyala", RsvSourceId, "Ridgeside Village"),
        X("Richard", RsvSourceId, "Ridgeside Village"),
        X("Sari", RsvSourceId, "Ridgeside Village"),
        X("Sean", RsvSourceId, "Ridgeside Village"),
        X("Shanice", RsvSourceId, "Ridgeside Village"),
        E("Shiro", RsvSourceId, "Ridgeside Village", PartyRole.Tank, PartyRole.Damage, EngagementStyle.Balanced, 5, 4, 2, 1, 3),
        X("Sonny", RsvSourceId, "Ridgeside Village"),
        X("Torts", RsvSourceId, "Ridgeside Village"),
        X("Trinnie", RsvSourceId, "Ridgeside Village"),
        X("Undreya", RsvSourceId, "Ridgeside Village"),
        E("Ysabelle", RsvSourceId, "Ridgeside Village", PartyRole.Support, PartyRole.Control, EngagementStyle.Balanced, 2, 3, 5, 2, 4),
        X("Yuuma", RsvSourceId, "Ridgeside Village"),
        X("Zayne", RsvSourceId, "Ridgeside Village"),
    };

    private static NpcCombatProfile E(
        string name,
        string sourceId,
        string sourceLabel,
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
            SourceId = sourceId,
            SourceLabel = sourceLabel,
            PrimaryRole = primary,
            SecondaryRole = secondary,
            RecommendedEngagement = engagement,
            PassiveKey = $"codex.expansion.{key}.passive",
            AbilityKey = $"codex.expansion.{key}.ability",
            TankAffinity = tank,
            DamageAffinity = damage,
            SupportAffinity = support,
            HealerAffinity = healer,
            ControlAffinity = control
        };
    }

    private static NpcCombatProfile X(string name, string sourceId, string sourceLabel)
    {
        return new NpcCombatProfile
        {
            CharacterName = name,
            SourceId = sourceId,
            SourceLabel = sourceLabel,
            PrimaryRole = PartyRole.Unassigned,
            SecondaryRole = PartyRole.Unassigned,
            RecommendedEngagement = EngagementStyle.Balanced,
            PassiveKey = "codex.expansion.passive",
            AbilityKey = "codex.expansion.ability",
            TankAffinity = 0,
            DamageAffinity = 0,
            SupportAffinity = 0,
            HealerAffinity = 0,
            ControlAffinity = 0
        };
    }
}
