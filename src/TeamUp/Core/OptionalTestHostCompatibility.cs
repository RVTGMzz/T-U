using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Marker-only compatibility for optional developer test hosts.
/// Team Up never takes ownership of Cardcha's map or dummy actors; it only avoids
/// selecting Cardcha's instrumentation as real combat targets.
/// </summary>
public static class OptionalTestHostCompatibility
{
    public const string CardchaUniqueId = "Ronvotri.Cardcha";
    public const string CardchaArenaLocationName = "Cardcha_CardTestArena";
    public const string CardchaArenaRoleProperty = "CardchaTestArenaRole";
    public const string CardchaArenaRoleToken = "isolated-combat-test";
    public const string CardchaDummyMarker = "Ronvotri.Cardcha/CardTestArenaDummy";
    public const string CardchaKillTargetMarker = "Ronvotri.Cardcha/CardTestArenaKillTarget";

    public static bool IsCardchaHarnessMonster(Monster monster)
        => monster.modData.ContainsKey(CardchaDummyMarker)
            || monster.modData.ContainsKey(CardchaKillTargetMarker);
}
