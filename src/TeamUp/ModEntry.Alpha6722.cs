using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6722Events()
    {
        Helper.ConsoleCommands.Add(
            "teamup_pelipper_density_probe",
            "Read-only Pelipper wild population surface probe. Writes diagnostics/Pelipper_Wild_Density_Probe_latest.txt.",
            OnAlpha6722PelipperDensityProbeCommand);
    }

    private void OnAlpha6722PelipperDensityProbeCommand(string command, string[] args)
    {
        object? modInfo = Helper.ModRegistry.Get(PelipperTownCompatibilityService.ProviderId);
        if (modInfo is null)
        {
            Monitor.Log("Pelipper Town is not installed or not visible to SMAPI.", LogLevel.Info);
            return;
        }

        if (!PelipperModRuntimeRootLocator.TryLocate(modInfo, out object? root, out string route) || root is null)
        {
            Monitor.Log("Pelipper runtime root could not be located. Probe made no changes.", LogLevel.Warn);
            return;
        }

        List<string> report = PelipperWildDensitySurfaceProbe.BuildReport(root, Context.IsWorldReady ? Game1.currentLocation : null).ToList();
        report.Insert(2, $"rootRoute={route}");

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "Pelipper_Wild_Density_Probe_latest.txt");
        File.WriteAllLines(outputPath, report);

        foreach (string line in report)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Pelipper wild-density probe saved: {outputPath}", LogLevel.Info);
    }
}
