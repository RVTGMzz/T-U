using Ronvotri.TeamUp.Core;
using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha679ChemistryVariantsRegistered;

    private void EnsureAlpha679ChemistryVariantsRegistered()
    {
        if (Alpha679ChemistryVariantsRegistered)
            return;

        Alpha679ChemistryVariantsRegistered = true;
        Helper.ConsoleCommands.Add(
            "teamup_chemistry_variant",
            "Show chemistry tone variant for a pair. Usage: teamup_chemistry_variant <NPC A> <NPC B>.",
            OnAlpha679ChemistryVariantCommand);
        Monitor.Log(
            $"Alpha 6.7.9 Chemistry Variants enabled: {ChemistryVariantCatalog.PairVariantCount} pair profiles, {ChemistryVariantCatalog.LineCount} bilingual line pairs.",
            LogLevel.Info);
    }

    private void OnAlpha679ChemistryVariantCommand(string command, string[] args)
    {
        if (args.Length < 2)
        {
            Monitor.Log("Usage: teamup_chemistry_variant <NPC A> <NPC B>", LogLevel.Info);
            return;
        }

        PartyChemistryType chemistry = PartyChemistryCatalog.Resolve(args[0], args[1]);
        ChemistryPairVariant profile = ChemistryVariantCatalog.ResolveProfile(args[0], args[1], chemistry);
        Monitor.Log(
            $"{args[0]} + {args[1]}: chemistry={chemistry}; variant={profile.Variant}; preferredLead={profile.PreferredLeadName ?? "random"}; linePool={ChemistryVariantCatalog.GetLines(profile.Variant).Count}",
            LogLevel.Info);
    }
}
