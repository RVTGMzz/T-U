using Ronvotri.TeamUp.Core;
using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha676ContextBanterRegistered;

    private void EnsureAlpha676ContextBanterRegistered()
    {
        if (Alpha676ContextBanterRegistered)
            return;

        Alpha676ContextBanterRegistered = true;
        Helper.ConsoleCommands.Add(
            "teamup_context_banter",
            "Context banter controls: audit | now.",
            OnAlpha676ContextBanterCommand);

        ContextBanterAuditReport report = ContextBanterCatalog.AuditKnownRoster();
        if (report.Passed)
            Monitor.Log($"Alpha 6.7.6 context banter PASS: {report.Scripts} context scripts.", LogLevel.Info);
        else
            Monitor.Log($"Alpha 6.7.6 context banter found {report.Issues.Count} issue(s): {string.Join(" | ", report.Issues)}", LogLevel.Warn);
    }

    private void OnAlpha676ContextBanterCommand(string command, string[] args)
    {
        string action = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "audit";
        switch (action)
        {
            case "audit":
                ContextBanterAuditReport report = ContextBanterCatalog.AuditKnownRoster();
                if (report.Passed)
                    Monitor.Log($"Context banter PASS: {report.Scripts} scripts, VI/EN lines and roster references valid.", LogLevel.Info);
                else
                {
                    Monitor.Log($"Context banter FAIL: {report.Issues.Count} issue(s).", LogLevel.Warn);
                    foreach (string issue in report.Issues)
                        Monitor.Log($" - {issue}", LogLevel.Warn);
                }
                break;

            case "now":
                Monitor.Log(
                    PartyBanterAlpha67?.ForceContext() == true
                        ? "Forced one eligible context banter line/exchange."
                        : "No eligible context banter is available at the current time/location/party.",
                    LogLevel.Info);
                break;

            default:
                Monitor.Log("Usage: teamup_context_banter <audit|now>", LogLevel.Info);
                break;
        }
    }
}
