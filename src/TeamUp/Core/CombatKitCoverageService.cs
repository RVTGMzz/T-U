using Ronvotri.TeamUp.Combat;

namespace Ronvotri.TeamUp.Core;

public static class CombatKitCoverageService
{
    public static bool HasCombatKit(string characterName)
    {
        if (SpecialRecruitCombatService.HasSpecialCombatKit(characterName))
            return true;
        if (SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner(characterName))
            return true;
        if (CharacterSkillIdentityCatalog.Get(characterName) is not null)
            return true;
        return ExpansionSkillService.TryGetBaseCooldownTicks(characterName, out _);
    }
}
