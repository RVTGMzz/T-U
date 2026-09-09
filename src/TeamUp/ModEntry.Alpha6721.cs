using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6721Events()
    {
        Helper.ConsoleCommands.Add(
            "teamup_density",
            "Universal Monster Density: status | sources | reapply.",
            OnAlpha6721DensityCommand);
    }

    private void OnAlpha6721DensityCommand(string command, string[] args)
    {
        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "status":
                Monitor.Log(Surge.Describe(), LogLevel.Info);
                Monitor.Log(Surge.LastTelemetryLine, LogLevel.Info);
                return;

            case "sources":
                foreach (string line in Surge.DescribeCurrentSources())
                    Monitor.Log(line, LogLevel.Info);
                return;

            case "reapply":
                Surge.DebugReapplyCurrentLocation(out string result);
                Monitor.Log(result, LogLevel.Info);
                if (Context.IsWorldReady)
                    Game1.showGlobalMessage(result);
                return;

            default:
                Monitor.Log("Usage: teamup_density <status|sources|reapply>", LogLevel.Info);
                return;
        }
    }
}
