from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.4"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, content: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def replace_once(rel: str, old: str, new: str, token: str) -> None:
    content = read(rel)
    if token in content:
        return
    count = content.count(old)
    if count != 1:
        raise RuntimeError(f"{rel}: expected one anchor for {token!r}, found {count}")
    write(rel, content.replace(old, new, 1))


# Version.
project = read("TeamUp.csproj")
if f"<Version>{VERSION}</Version>" not in project:
    if "<Version>0.2.0-alpha.6.7.3</Version>" not in project:
        raise RuntimeError("Unexpected TeamUp.csproj version")
    write("TeamUp.csproj", project.replace(
        "<Version>0.2.0-alpha.6.7.3</Version>",
        f"<Version>{VERSION}</Version>", 1))

# One authoritative owner table for the five Alpha 6 prototype signatures.
signature_authority = r'''namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Prevents the five Alpha 6 prototype NPCs from firing both their legacy CombatService
/// signature and the upgraded Alpha6CombatPolishService signature. Generic attacks/heals
/// stay in CombatService; only signature ownership is exclusive.
/// </summary>
internal static class SignatureAuthorityService
{
    private static readonly HashSet<string> Alpha6PrototypeOwners = new(StringComparer.OrdinalIgnoreCase)
    {
        "Abigail",
        "Alex",
        "Harvey",
        "Maru",
        "Emily"
    };

    public static IReadOnlyCollection<string> Alpha6PrototypeSignatureOwners => Alpha6PrototypeOwners;

    public static bool IsAlpha6PrototypeSignatureOwner(string characterName)
        => Alpha6PrototypeOwners.Contains(characterName);
}
'''
write("Combat/SignatureAuthorityService.cs", signature_authority)

coverage = r'''using Ronvotri.TeamUp.Combat;

namespace Ronvotri.TeamUp.Core;

public static class CombatKitCoverageService
{
    public static bool HasCombatKit(string characterName)
    {
        if (SpecialRecruitCombatService.HasSpecialCombatKit(characterName))
            return true;
        if (SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner(characterName))
            return true;
        if (CharacterSkillIdentityCatalog.Get(characterName) is not null)
            return true;
        return ExpansionSkillService.TryGetBaseCooldownTicks(characterName, out _);
    }
}
'''
write("Core/CombatKitCoverageService.cs", coverage)

# Gate the legacy signature methods, leaving normal CombatService attack/heal behavior intact.
replace_once(
    "Combat/CombatService.cs",
    "    {\n        if (GetCooldown(_signatureCooldowns, member.CharacterName) > 0)\n            return;\n\n        if (member.CharacterName.Equals(\"Harvey\", StringComparison.OrdinalIgnoreCase)",
    "    {\n        if (SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner(member.CharacterName))\n            return;\n        if (GetCooldown(_signatureCooldowns, member.CharacterName) > 0)\n            return;\n\n        if (member.CharacterName.Equals(\"Harvey\", StringComparison.OrdinalIgnoreCase)",
    "SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner(member.CharacterName))\n            return;\n        if (GetCooldown(_signatureCooldowns, member.CharacterName) > 0)\n            return;\n\n        if (member.CharacterName.Equals(\"Harvey\"",
)
replace_once(
    "Combat/CombatService.cs",
    "    private void TryTriggerAttackSignature(NPC npc, Monster target, PartyMemberData member, PartyRole role, int affinity)\n    {\n        if (target.Health <= 0 || GetCooldown(_signatureCooldowns, member.CharacterName) > 0)\n            return;\n",
    "    private void TryTriggerAttackSignature(NPC npc, Monster target, PartyMemberData member, PartyRole role, int affinity)\n    {\n        if (SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner(member.CharacterName))\n            return;\n        if (target.Health <= 0 || GetCooldown(_signatureCooldowns, member.CharacterName) > 0)\n            return;\n",
    "private void TryTriggerAttackSignature(NPC npc, Monster target, PartyMemberData member, PartyRole role, int affinity)\n    {\n        if (SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner",
)

integrity = r'''using Ronvotri.TeamUp.Combat;

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
'''
write("Core/CombatRosterIntegrityService.cs", integrity)

alpha674 = r'''using Ronvotri.TeamUp.Core;
using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha674RosterIntegrityRegistered;

    private void EnsureAlpha674RosterIntegrityRegistered()
    {
        if (Alpha674RosterIntegrityRegistered)
            return;

        Alpha674RosterIntegrityRegistered = true;
        Helper.ConsoleCommands.Add(
            "teamup_roster_static_audit",
            "Audit Team Up's full built-in profile/skill/rank/balance catalog without requiring a loaded save.",
            OnAlpha674RosterStaticAudit);

        CombatRosterIntegrityReport report = CombatRosterIntegrityService.AuditKnownCatalog();
        if (report.Passed)
        {
            Monitor.Log($"Alpha 6.7.4 roster integrity PASS: {report.ProfileCount} profile row(s), no known missing kit/balance issue.", LogLevel.Info);
        }
        else
        {
            Monitor.Log($"Alpha 6.7.4 roster integrity found {report.Issues.Count} issue(s): {string.Join(" | ", report.Issues)}", LogLevel.Warn);
        }
    }

    private void OnAlpha674RosterStaticAudit(string command, string[] args)
    {
        CombatRosterIntegrityReport report = CombatRosterIntegrityService.AuditKnownCatalog();
        if (report.Passed)
        {
            Monitor.Log($"Roster static audit PASS: {report.ProfileCount} profile row(s), all have roles, rank path and combat kit coverage.", LogLevel.Info);
            return;
        }

        Monitor.Log($"Roster static audit FAIL: {report.Issues.Count} issue(s).", LogLevel.Warn);
        foreach (string issue in report.Issues)
            Monitor.Log($" - {issue}", LogLevel.Warn);
    }
}
'''
write("ModEntry.Alpha674.cs", alpha674)

replace_once(
    "ModEntry.Alpha6625.cs",
    "        // Alpha 6.7.3: combat ranks, Special Recruit kits and S-rank identities.\n        EnsureAlpha673SpecialRecruitRegistered();\n",
    "        // Alpha 6.7.3: combat ranks, Special Recruit kits and S-rank identities.\n        EnsureAlpha673SpecialRecruitRegistered();\n\n        // Alpha 6.7.4: static roster coverage/balance audit + one signature authority.\n        EnsureAlpha674RosterIntegrityRegistered();\n",
    "EnsureAlpha674RosterIntegrityRegistered();",
)

print("Alpha 6.7.4 source materialized.")
