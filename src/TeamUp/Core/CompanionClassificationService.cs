using StardewValley;
using StardewValley.Characters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Classifies characters before Team Up applies Main Party recruitment rules.
/// This keeps Farmer-owned/special companions and NPC-linked companions out of the
/// normal villager recruitment path without hard-wiring every future summon into ModEntry.
/// </summary>
public static class CompanionClassificationService
{
    public const string CompanionKindModDataKey = "Ronvotri.TeamUp/CompanionKind";

    public const string FarmerCompanionKind = "FarmerCompanion";

    public const string FarmerSummonKind = "FarmerSummon";

    public const string SpecialCompanionKind = "SpecialCompanion";

    public const string LinkedCompanionKind = "LinkedCompanion";

    public static TeamUpCharacterKind Classify(NPC npc, IEnumerable<string>? specialNpcNames)
    {
        if (npc is Child)
            return TeamUpCharacterKind.Ineligible;

        if (npc is Pet)
            return TeamUpCharacterKind.FarmerOrSpecialCompanion;

        if (TryGetDeclaredKind(npc, out TeamUpCharacterKind declaredKind))
            return declaredKind;

        if (MatchesConfiguredSpecialName(npc.Name, specialNpcNames))
            return TeamUpCharacterKind.FarmerOrSpecialCompanion;

        if (!npc.IsVillager || !npc.canTalk())
            return TeamUpCharacterKind.Ineligible;

        return TeamUpCharacterKind.MainPartyCandidate;
    }

    public static bool CanRecruitToMainParty(NPC npc, IEnumerable<string>? specialNpcNames)
    {
        return Classify(npc, specialNpcNames) == TeamUpCharacterKind.MainPartyCandidate;
    }

    private static bool TryGetDeclaredKind(NPC npc, out TeamUpCharacterKind kind)
    {
        kind = TeamUpCharacterKind.Ineligible;

        if (!npc.modData.TryGetValue(CompanionKindModDataKey, out string? rawKind)
            || string.IsNullOrWhiteSpace(rawKind))
        {
            return false;
        }

        if (rawKind.Equals(FarmerCompanionKind, StringComparison.OrdinalIgnoreCase)
            || rawKind.Equals(FarmerSummonKind, StringComparison.OrdinalIgnoreCase)
            || rawKind.Equals(SpecialCompanionKind, StringComparison.OrdinalIgnoreCase))
        {
            kind = TeamUpCharacterKind.FarmerOrSpecialCompanion;
            return true;
        }

        if (rawKind.Equals(LinkedCompanionKind, StringComparison.OrdinalIgnoreCase))
        {
            kind = TeamUpCharacterKind.NpcLinkedCompanion;
            return true;
        }

        return false;
    }

    private static bool MatchesConfiguredSpecialName(string characterName, IEnumerable<string>? specialNpcNames)
    {
        if (specialNpcNames is null)
            return false;

        return specialNpcNames.Any(name =>
            !string.IsNullOrWhiteSpace(name)
            && name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
    }
}

public enum TeamUpCharacterKind
{
    MainPartyCandidate,
    FarmerOrSpecialCompanion,
    NpcLinkedCompanion,
    Ineligible
}
