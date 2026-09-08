using Ronvotri.TeamUp.Core;
using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha678PartyChemistryRegistered;

    private void EnsureAlpha678PartyChemistryRegistered()
    {
        if (Alpha678PartyChemistryRegistered)
            return;

        Alpha678PartyChemistryRegistered = true;
        Helper.ConsoleCommands.Add(
            "teamup_chemistry",
            "Inspect cosmetic Party Chemistry: status | <NPC1> <NPC2>.",
            OnAlpha678PartyChemistryCommand);
        Monitor.Log(
            $"Alpha 6.7.8 Party Chemistry enabled: {PartyChemistryCatalog.PairCount} authored pair relationships; cosmetic-only.",
            LogLevel.Info);
    }

    private void OnAlpha678PartyChemistryCommand(string command, string[] args)
    {
        if (args.Length == 0 || args[0].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log(
                $"Party Chemistry: {PartyChemistryCatalog.PairCount} authored pairs. Types: Friends, Rivals, Family, Mentor, Awkward, Protective, Respectful, ShipperTarget, Neutral.",
                LogLevel.Info);
            return;
        }

        if (args.Length >= 2)
        {
            Monitor.Log(PartyChemistryCatalog.Describe(args[0], args[1]), LogLevel.Info);
            return;
        }

        Monitor.Log("Usage: teamup_chemistry status | <NPC1> <NPC2>", LogLevel.Info);
    }
}
