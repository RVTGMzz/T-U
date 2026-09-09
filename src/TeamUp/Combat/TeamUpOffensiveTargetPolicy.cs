using Ronvotri.TeamUp.Core;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Canonical autonomous-offense gate. A Pelipper wild combat proxy at its capture floor is never
/// a legal Team Up offensive target. This is intentionally separate from incoming-threat context:
/// allies may still guard/heal while the wild target remains alive, but no Team Up attack/signature
/// layer may select or damage it.
/// </summary>
internal static class TeamUpOffensiveTargetPolicy
{
    public static bool IsEligible(Monster monster)
    {
        if (monster.Health <= 0)
            return false;
        if (OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            return false;
        if (PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            return false;
        if (PelipperCaptureSafetyService.IsProtected(monster))
            return false;
        return true;
    }
}
