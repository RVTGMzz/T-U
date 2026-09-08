using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha677BanterMemoryRegistered;

    private void EnsureAlpha677BanterMemoryRegistered()
    {
        if (Alpha677BanterMemoryRegistered)
            return;

        Alpha677BanterMemoryRegistered = true;
        Helper.ConsoleCommands.Add(
            "teamup_banter_memory",
            "Banter recency memory controls: status | reset.",
            OnAlpha677BanterMemoryCommand);
        Monitor.Log("Alpha 6.7.7 Banter Memory enabled: soft recency scoring for exchange IDs, pairs and speakers.", LogLevel.Info);
    }

    private void OnAlpha677BanterMemoryCommand(string command, string[] args)
    {
        string action = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "status";
        if (PartyBanterAlpha67 is null)
        {
            Monitor.Log("Party Banter is not initialized.", LogLevel.Info);
            return;
        }

        switch (action)
        {
            case "status":
                Monitor.Log("Banter memory: " + PartyBanterAlpha67.DescribeMemory(), LogLevel.Info);
                break;
            case "reset":
                PartyBanterAlpha67.ResetMemory();
                Monitor.Log("Banter recency memory cleared. No save/progression state was changed.", LogLevel.Info);
                break;
            default:
                Monitor.Log("Usage: teamup_banter_memory <status|reset>", LogLevel.Info);
                break;
        }
    }
}
