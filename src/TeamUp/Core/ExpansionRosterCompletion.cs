namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.4.5 fills the remaining SVE/RSV Codex placeholders with Team Up-original
/// combat identities. These are balance interpretations for Team Up, not canonical
/// claims about the source mods. Existing Wave 1 profiles remain untouched.
/// </summary>
internal static class ExpansionRosterCompletion
{
    private sealed record RoleSpec(
        PartyRole Primary,
        PartyRole Secondary,
        EngagementStyle Engagement,
        int Tank,
        int Damage,
        int Support,
        int Healer,
        int Control);

    private static readonly Dictionary<string, RoleSpec> Specs = new(StringComparer.OrdinalIgnoreCase)
    {
        // Stardew Valley Expanded remaining roster.
        ["Apples"] = R(PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 1, 2, 5, 4, 3),
        ["Charlie"] = R(PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 5, 2, 4, 2, 2),
        ["Hank"] = R(PartyRole.Tank, PartyRole.Damage, EngagementStyle.Balanced, 5, 4, 2, 1, 3),
        ["Jolyne"] = R(PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 3, 5),
        ["Peaches"] = R(PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 5, 5, 2),
        ["Scarlett"] = R(PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 5, 4, 2, 2),
        ["Suki"] = R(PartyRole.Support, PartyRole.Control, EngagementStyle.Balanced, 2, 2, 5, 3, 4),
        ["Susan"] = R(PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 2, 1, 5, 5, 2),
        ["Treyvon"] = R(PartyRole.Damage, PartyRole.Tank, EngagementStyle.Aggressive, 4, 5, 2, 1, 3),

        // Ridgeside Village remaining roster.
        ["Acorn"] = R(PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 2, 2, 5, 3, 4),
        ["Alissa"] = R(PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 2, 5, 5, 2),
        ["Anton"] = R(PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 5, 3, 4, 1, 2),
        ["Ariah"] = R(PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 2, 5, 2, 1, 4),
        ["Belinda"] = R(PartyRole.Support, PartyRole.Control, EngagementStyle.Balanced, 2, 2, 5, 3, 4),
        ["Bert"] = R(PartyRole.Tank, PartyRole.Damage, EngagementStyle.Balanced, 5, 4, 2, 1, 2),
        ["Bliss"] = R(PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 5, 5, 3),
        ["Bryle"] = R(PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 3, 5, 2, 1, 4),
        ["Corine"] = R(PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 2, 2, 5, 4, 3),
        ["Ezekiel"] = R(PartyRole.Tank, PartyRole.Control, EngagementStyle.Balanced, 5, 3, 2, 1, 4),
        ["Faye"] = R(PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 1, 2, 5, 4, 3),
        ["Flor"] = R(PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 2, 1, 5, 5, 2),
        ["Freddie"] = R(PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 5, 3, 4, 1, 2),
        ["Helen"] = R(PartyRole.Support, PartyRole.Healer, EngagementStyle.Cautious, 2, 1, 5, 4, 3),
        ["Irene"] = R(PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 3, 5),
        ["Jeric"] = R(PartyRole.Damage, PartyRole.Tank, EngagementStyle.Aggressive, 4, 5, 2, 1, 3),
        ["Keahi"] = R(PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 3, 5, 2, 1, 4),
        ["Kimpoi"] = R(PartyRole.Support, PartyRole.Control, EngagementStyle.Balanced, 2, 3, 5, 2, 4),
        ["Kiwi"] = R(PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 5, 3, 1, 3),
        ["Lenny"] = R(PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 5, 3, 4, 1, 2),
        ["Lola"] = R(PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 1, 5, 5, 2),
        ["Lorenzo"] = R(PartyRole.Support, PartyRole.Damage, EngagementStyle.Balanced, 2, 4, 5, 2, 2),
        ["Louie"] = R(PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 5, 4, 1, 2),
        ["Maive"] = R(PartyRole.Control, PartyRole.Healer, EngagementStyle.Cautious, 2, 2, 4, 4, 5),
        ["Malaya"] = R(PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 3, 5, 2, 1, 4),
        ["Naomi"] = R(PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 2, 2, 5, 4, 2),
        ["Olga"] = R(PartyRole.Tank, PartyRole.Healer, EngagementStyle.Balanced, 5, 2, 3, 4, 2),
        ["Paula"] = R(PartyRole.Support, PartyRole.Control, EngagementStyle.Cautious, 2, 2, 5, 3, 4),
        ["Philip"] = R(PartyRole.Damage, PartyRole.Tank, EngagementStyle.Aggressive, 4, 5, 2, 1, 3),
        ["Pika"] = R(PartyRole.Support, PartyRole.Damage, EngagementStyle.Balanced, 2, 4, 5, 2, 3),
        ["Pipo"] = R(PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 2, 5),
        ["Raeriyala"] = R(PartyRole.Control, PartyRole.Healer, EngagementStyle.Cautious, 1, 2, 4, 4, 5),
        ["Richard"] = R(PartyRole.Tank, PartyRole.Support, EngagementStyle.Balanced, 5, 3, 4, 1, 2),
        ["Sari"] = R(PartyRole.Damage, PartyRole.Support, EngagementStyle.Aggressive, 2, 5, 4, 2, 2),
        ["Sean"] = R(PartyRole.Damage, PartyRole.Tank, EngagementStyle.Aggressive, 4, 5, 2, 1, 3),
        ["Shanice"] = R(PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 2, 2, 5, 4, 3),
        ["Sonny"] = R(PartyRole.Tank, PartyRole.Damage, EngagementStyle.Balanced, 5, 4, 2, 1, 3),
        ["Torts"] = R(PartyRole.Tank, PartyRole.Support, EngagementStyle.Cautious, 5, 2, 4, 2, 2),
        ["Trinnie"] = R(PartyRole.Control, PartyRole.Support, EngagementStyle.Balanced, 2, 3, 4, 2, 5),
        ["Undreya"] = R(PartyRole.Control, PartyRole.Damage, EngagementStyle.Aggressive, 2, 4, 3, 1, 5),
        ["Yuuma"] = R(PartyRole.Healer, PartyRole.Support, EngagementStyle.Cautious, 1, 2, 5, 5, 2),
        ["Zayne"] = R(PartyRole.Damage, PartyRole.Control, EngagementStyle.Aggressive, 3, 5, 2, 1, 4),
    };

    public static IReadOnlyCollection<string> CompletedNames => Specs.Keys;

    public static NpcCombatProfile Resolve(NpcCombatProfile profile)
    {
        if (profile.PrimaryRole != PartyRole.Unassigned || !Specs.TryGetValue(profile.CharacterName, out RoleSpec? spec))
            return profile;

        string key = profile.CharacterName.ToLowerInvariant();
        return new NpcCombatProfile
        {
            CharacterName = profile.CharacterName,
            SourceId = profile.SourceId,
            SourceLabel = profile.SourceLabel,
            PrimaryRole = spec.Primary,
            SecondaryRole = spec.Secondary,
            RecommendedEngagement = spec.Engagement,
            PassiveKey = $"codex.expansion.{key}.passive",
            AbilityKey = $"codex.expansion.{key}.ability",
            TankAffinity = spec.Tank,
            DamageAffinity = spec.Damage,
            SupportAffinity = spec.Support,
            HealerAffinity = spec.Healer,
            ControlAffinity = spec.Control
        };
    }

    private static RoleSpec R(
        PartyRole primary,
        PartyRole secondary,
        EngagementStyle engagement,
        int tank,
        int damage,
        int support,
        int healer,
        int control)
        => new(primary, secondary, engagement, tank, damage, support, healer, control);
}
