using Ronvotri.TeamUp.Core;
using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha675BanterCatalogRegistered;

    private void EnsureAlpha675BanterCatalogRegistered()
    {
        if (Alpha675BanterCatalogRegistered)
            return;

        Alpha675BanterCatalogRegistered = true;
        Helper.ConsoleCommands.Add(
            "teamup_banter_catalog_audit",
            "Audit authored Party Banter pair scripts and MiMi shipping scripts without requiring a loaded save.",
            OnAlpha675BanterCatalogAudit);

        BanterCatalogAuditReport report = BanterContentCatalog.AuditKnownRoster();
        if (report.Passed)
        {
            Monitor.Log(
                $"Alpha 6.7.5 banter catalog PASS: {report.PairScripts} pair scripts + {report.ShippingScripts} MiMi shipping scripts.",
                LogLevel.Info);
        }
        else
        {
            Monitor.Log(
                $"Alpha 6.7.5 banter catalog found {report.Issues.Count} issue(s): {string.Join(" | ", report.Issues)}",
                LogLevel.Warn);
        }
    }

    private void OnAlpha675BanterCatalogAudit(string command, string[] args)
    {
        BanterCatalogAuditReport report = BanterContentCatalog.AuditKnownRoster();
        if (report.Passed)
        {
            Monitor.Log(
                $"Banter catalog PASS: {report.PairScripts} authored pairs, {report.ShippingScripts} MiMi ship pairs, VI/EN lines and roster references valid.",
                LogLevel.Info);
            return;
        }

        Monitor.Log($"Banter catalog FAIL: {report.Issues.Count} issue(s).", LogLevel.Warn);
        foreach (string issue in report.Issues)
            Monitor.Log($" - {issue}", LogLevel.Warn);
    }
}
