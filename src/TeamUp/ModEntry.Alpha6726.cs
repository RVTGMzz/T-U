using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6726Events()
    {
        Helper.ConsoleCommands.Add(
            "teamup_story_intro",
            "First Surge narrative bridge: status | reset | stage <0-2>. Status writes diagnostics/TeamUp_Story_Intro_latest.txt.",
            OnAlpha6726StoryIntroCommand);
    }

    private void OnAlpha6726StoryIntroCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_story_intro.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "status":
                WriteAlpha6726StoryDiagnostic();
                return;

            case "reset":
                Origin.ResetStory(Game1.player);
                WriteAlpha6726StoryDiagnostic();
                Game1.showGlobalMessage("FIRST SURGE STORY • INTRO RESET");
                return;

            case "stage":
                if (args.Length < 2 || !int.TryParse(args[1], out int stage) || stage < 0 || stage > 2)
                {
                    Monitor.Log("Usage: teamup_story_intro stage <0-2>", LogLevel.Info);
                    return;
                }
                Origin.SetDebugStage(Game1.player, stage);
                WriteAlpha6726StoryDiagnostic();
                return;

            default:
                Monitor.Log("Usage: teamup_story_intro <status|reset|stage 0-2>", LogLevel.Info);
                return;
        }
    }

    private void WriteAlpha6726StoryDiagnostic()
    {
        List<string> lines = new()
        {
            "TEAM UP 6.7.26 - LINUS / MARLON FIRST SURGE BRIDGE",
            SurgeStoryAlpha6725.Describe(),
            Origin.Describe(),
            "Expected before first Mutant: stage=0/2 objective=waiting-for-first-mutant.",
            "After first Mutant: objective=find-linus-forest.",
            "After entering Forest: stage=1/2 objective=find-marlon-adventure-guild.",
            "After entering AdventureGuild: stage=2/2 objective=marlon-investigation-open."
        };

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Story_Intro_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Story intro diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
