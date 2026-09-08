using Ronvotri.TeamUp.Combat;

namespace Ronvotri.TeamUp.Core;

public sealed record CombatRosterIntegrityReport(int ProfileCount, IReadOnlyList<string> Issues)
{
    public bool Passed => Issues.Count == 0;
}

/// <summary>
/// Provider-neutral roster integrity audit. It never mutates source NPCs or save data.
/// The same rules are mirrored by Alpha 6.7.4 CI so a missing profile/skill/balance row
/// is caught before a test ZIP is produced.
/// </summary>
public static class CombatRosterIntegrityService
{
    public static CombatRosterIntegrityReport AuditKnownCatalog()
    {
        List<string> issues = new();
        IReadOnlyList<NpcCombatProfile> profiles = NpcProfileCatalog.All;

        foreach (NpcCombatProfile profile in profiles)
        {
            if (profile.PrimaryRole == PartyRole.Unassigned || profile.SecondaryRole == PartyRole.Unassigned)
                issues.Add($"{profile.CharacterName}: unresolved combat role");
            if (profile.PrimaryRole == profile.SecondaryRole)
                issues.Add($"{profile.CharacterName}: primary and secondary role are identical");

            int[] affinities =
            {
                profile.TankAffinity,
                profile.DamageAffinity,
                profile.SupportAffinity,
                profile.HealerAffinity,
                profile.ControlAffinity
            };
            if (affinities.Any(value => value < 0 || value > 5))
                issues.Add($"{profile.CharacterName}: affinity outside 0..5");
            if (affinities.All(value => value == 0))
                issues.Add($"{profile.CharacterName}: all affinities are zero");

            int primary = profile.GetAffinity(profile.PrimaryRole);
            int secondary = profile.GetAffinity(profile.SecondaryRole);
            if (primary < 4)
                issues.Add($"{profile.CharacterName}: primary affinity below 4");
            if (secondary < 2)
                issues.Add($"{profile.CharacterName}: secondary affinity below 2");
            if (primary < secondary)
                issues.Add($"{profile.CharacterName}: secondary affinity exceeds primary affinity");
            if (affinities.Count(value => value == 5) > 2)
                issues.Add($"{profile.CharacterName}: more than two maxed affinities");

            if (!CombatKitCoverageService.HasCombatKit(profile.CharacterName))
                issues.Add($"{profile.CharacterName}: missing combat kit");

            _ = CombatRankCatalog.Get(profile.CharacterName, profile);
        }

        foreach (IGrouping<string, NpcCombatProfile> duplicate in profiles.GroupBy(
                     profile => profile.CharacterName,
                     StringComparer.OrdinalIgnoreCase)
                 .Where(group => group.Count() > 1))
        {
            issues.Add($"{duplicate.Key}: duplicate profile row");
        }

        return new CombatRosterIntegrityReport(profiles.Count, issues);
    }
}
