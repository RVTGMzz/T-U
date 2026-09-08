namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Prevents the five Alpha 6 prototype NPCs from firing both their legacy CombatService
/// signature and the upgraded Alpha6CombatPolishService signature. Generic attacks/heals
/// stay in CombatService; only signature ownership is exclusive.
/// </summary>
internal static class SignatureAuthorityService
{
    private static readonly HashSet<string> Alpha6PrototypeOwners = new(StringComparer.OrdinalIgnoreCase)
    {
        "Abigail",
        "Alex",
        "Harvey",
        "Maru",
        "Emily"
    };

    public static IReadOnlyCollection<string> Alpha6PrototypeSignatureOwners => Alpha6PrototypeOwners;

    public static bool IsAlpha6PrototypeSignatureOwner(string characterName)
        => Alpha6PrototypeOwners.Contains(characterName);
}
