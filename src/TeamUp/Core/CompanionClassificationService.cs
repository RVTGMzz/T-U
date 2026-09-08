using StardewValley;
using StardewValley.Characters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Classifies characters before Team Up applies Main Party recruitment rules.
/// Farmer-owned/special companions and NPC-linked companions never enter the
/// normal villager recruitment path.
/// </summary>
public static class CompanionClassificationService
{
    public const string CompanionKindModDataKey = "Ronvotri.TeamUp/CompanionKind";

    public const string FarmerCompanionKind = "FarmerCompanion";

    public const string FarmerSummonKind = "FarmerSummon";

    public const string SpecialCompanionKind = "SpecialCompanion";

    public const string LinkedCompanionKind = "LinkedCompanion";

    // Product rule: ChaCha belongs to the Farmer/Special Companion subsystem and
    // never consumes a Main Party slot. Keep both the friendly/display identity
    // and Cardcha's native runtime NPC identity so UI, save migration, and direct
    // party calls agree even when Cardcha exposes the native actor by internal ID.
    private static readonly HashSet<string> BuiltInSpecialNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ChaCha",
        "Ronvotri.Cardcha_ChaCha"
    };

    // v0.2 Main Party baseline is adult human NPCs. These vanilla characters are
    // deliberately kept outside Main Party even if another mod makes them talkable.
    private static readonly HashSet<string> BuiltInNonMainPartyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Jas",
        "Vincent",
        "Leo",
        "Dwarf",
        "Krobus"
    };

    public static TeamUpCharacterKind Classify(NPC npc, IEnumerable<string>? specialNpcNames)
    {
        if (npc is Child)
            return TeamUpCharacterKind.Ineligible;

        if (npc is Pet)
            return TeamUpCharacterKind.FarmerOrSpecialCompanion;

        if (TryGetDeclaredKind(npc, out TeamUpCharacterKind declaredKind))
            return declaredKind;

        if (IsSpecialName(npc.Name, specialNpcNames)
            || IsSpecialName(npc.displayName, specialNpcNames))
        {
            return TeamUpCharacterKind.FarmerOrSpecialCompanion;
        }

        if (BuiltInNonMainPartyNames.Contains(npc.Name))
            return TeamUpCharacterKind.Ineligible;

        if (npc.Name.Equals("Henchman", StringComparison.OrdinalIgnoreCase)
            && !HasUsableDirectionalSprite(npc))
        {
            return TeamUpCharacterKind.Ineligible;
        }

        if (!npc.IsVillager || !npc.canTalk())
            return TeamUpCharacterKind.Ineligible;

        return TeamUpCharacterKind.MainPartyCandidate;
    }

    public static bool CanRecruitToMainParty(NPC npc, IEnumerable<string>? specialNpcNames)
    {
        return Classify(npc, specialNpcNames) == TeamUpCharacterKind.MainPartyCandidate;
    }

    public static bool IsSpecialName(string characterName, IEnumerable<string>? specialNpcNames)
    {
        if (string.IsNullOrWhiteSpace(characterName))
            return false;

        if (BuiltInSpecialNames.Contains(characterName))
            return true;

        return specialNpcNames?.Any(name =>
            !string.IsNullOrWhiteSpace(name)
            && name.Equals(characterName, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool HasUsableDirectionalSprite(NPC npc)
    {
        if (npc.Sprite?.Texture is null)
            return false;

        int frameWidth = Math.Max(1, npc.Sprite.SpriteWidth);
        int frameHeight = Math.Max(1, npc.Sprite.SpriteHeight);
        return npc.Sprite.Texture.Width >= frameWidth * 4
            && npc.Sprite.Texture.Height >= frameHeight * 4;
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
}

public enum TeamUpCharacterKind
{
    MainPartyCandidate,
    FarmerOrSpecialCompanion,
    NpcLinkedCompanion,
    Ineligible
}
