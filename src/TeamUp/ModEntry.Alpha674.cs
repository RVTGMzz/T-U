using Ronvotri.TeamUp.Core;
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
