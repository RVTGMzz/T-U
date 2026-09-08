using Ronvotri.TeamUp.Combat;

namespace Ronvotri.TeamUp.Core;

public static class CombatKitCoverageService
{
    private static readonly HashSet<string> Alpha6PrototypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Abigail", "Alex", "Harvey", "Maru", "Emily"
    };

    public static bool HasCombatKit(string characterName)
    {
        if (SpecialRecruitCombatService.HasSpecialCombatKit(characterName))
            return true;
        if (Alpha6PrototypeNames.Contains(characterName))
            return true;
        if (CharacterSkillIdentityCatalog.Get(characterName) is not null)
            return true;
        return ExpansionSkillService.TryGetBaseCooldownTicks(characterName, out _);
    }
}
