using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6723Events()
    {
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6723CaptureCeasefireUpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_capture_ceasefire",
            "Report Pelipper <=10% capture ceasefire state. Writes diagnostics/TeamUp_Capture_Ceasefire_latest.txt.",
            OnAlpha6723CaptureCeasefireCommand);
    }

    private void OnAlpha6723CaptureCeasefireUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || Game1.currentLocation is null)
            return;

        foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>())
        {
            if (!PelipperCaptureSafetyService.IsProtected(monster))
                continue;

            // Remove only Team Up's transient attack opt-in. Keep WildCombatProxyKey so the
            // capture-floor identity/watchdog remains durable and source-owned Pelipper behavior
            // is never taken over.
            monster.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
        }
    }

    private void OnAlpha6723CaptureCeasefireCommand(string command, string[] args)
    {
        List<string> lines = new()
        {
            "TEAM UP 6.7.23 - PELIPPER CAPTURE CEASEFIRE",
            $"captureEnabled={PelipperCaptureSafetyService.CurrentEnabled} threshold={PelipperCaptureSafetyService.CurrentThreshold:P1}",
            $"location={Game1.currentLocation?.NameOrUniqueName ?? "<none>"}"
        };

        if (!Context.IsWorldReady || Game1.currentLocation is null)
        {
            lines.Add("worldReady=False");
        }
        else
        {
            int rows = 0;
            foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>())
            {
                if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
                    continue;

                rows++;
                bool hasBudget = PelipperCaptureSafetyService.TryGetDamageBudget(monster, out int budget);
                bool protectedNow = PelipperCaptureSafetyService.IsProtected(monster);
                bool eligible = TeamUpOffensiveTargetPolicy.IsEligible(monster);
                bool target = monster.modData.TryGetValue(PelipperTownCompatibilityService.CombatTargetOptInKey, out string? rawTarget)
                    && rawTarget.Equals("true", StringComparison.OrdinalIgnoreCase);
                bool proxy = monster.modData.TryGetValue(PelipperTownCompatibilityService.WildCombatProxyKey, out string? rawProxy)
                    && rawProxy.Equals("true", StringComparison.OrdinalIgnoreCase);
                int floor = 0;
                PelipperCaptureSafetyService.TryGetCaptureFloor(monster, out floor);
                lines.Add($"wild type={monster.GetType().FullName} hp={monster.Health}/{monster.MaxHealth} floor={floor} budget={(hasBudget ? budget : -1)} protected={protectedNow} eligible={eligible} target={target} proxy={proxy}");
            }
            if (rows == 0)
                lines.Add("wild none");
        }

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Capture_Ceasefire_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Capture ceasefire diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
