using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private TheSurgeStoryService SurgeStoryAlpha6725 { get; set; } = null!;

    private void RegisterAlpha6725Events()
    {
        SurgeStoryAlpha6725 = new TheSurgeStoryService(Monitor);
        Helper.ConsoleCommands.Add(
            "teamup_surge_story",
            "The Surge story gate: status | reset | setkills <0-9>. Status writes diagnostics/TeamUp_Surge_Story_latest.txt.",
            OnAlpha6725SurgeStoryCommand);
    }

    private void OnAlpha6725SurgeStoryCommand(string command, string[] args)
    {
        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_surge_story.", LogLevel.Info);
            return;
        }

        switch (action)
        {
            case "status":
                WriteAlpha6725SurgeStoryDiagnostic();
                return;

            case "reset":
                SurgeStoryAlpha6725.Reset(Game1.player);
                MutationAlpha6719.ResetRuntime();
                Surge.Reset();
                if (Game1.currentLocation is not null)
                    Surge.OnWarped(Game1.currentLocation);
                WriteAlpha6725SurgeStoryDiagnostic();
                Game1.showGlobalMessage("THE SURGE STORY • RESET TO 0/10");
                return;

            case "setkills":
                if (args.Length < 2 || !int.TryParse(args[1], out int count) || count < 0 || count >= TheSurgeStoryService.FirstMutationKillThreshold)
                {
                    Monitor.Log("Usage: teamup_surge_story setkills <0-9>", LogLevel.Info);
                    return;
                }
                SurgeStoryAlpha6725.SetPreActivationKillCount(Game1.player, count);
                MutationAlpha6719.ResetRuntime();
                Surge.Reset();
                if (Game1.currentLocation is not null)
                    Surge.OnWarped(Game1.currentLocation);
                WriteAlpha6725SurgeStoryDiagnostic();
                Game1.showGlobalMessage($"THE SURGE STORY • {count}/10");
                return;

            default:
                Monitor.Log("Usage: teamup_surge_story <status|reset|setkills 0-9>", LogLevel.Info);
                return;
        }
    }

    private void WriteAlpha6725SurgeStoryDiagnostic()
    {
        List<string> lines = new()
        {
            "TEAM UP 6.7.25 - FIRST SURGE TRIGGER",
            SurgeStoryAlpha6725.Describe(),
            MutationAlpha6719.Describe(),
            Surge.Describe(),
            "Expected pre-trigger: activated=False, kills<10, randomMutationUnlocked=False, densityUnlocked=False.",
            "Expected after lethal defeat #10 mutates: activated=True, kills=10/10, randomMutationUnlocked=True, densityUnlocked=True."
        };

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Surge_Story_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Surge story diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
