using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Optional-source adapter for Ronvotri NPCs. Team Up reads only live NPC state and
/// never rewrites Cardcha / Hey! You're Cursed! story save data.
/// </summary>
public static class CustomNpcCompatibilityService
{
    public const string CardchaModId = "Ronvotri.Cardcha";
    public const string MimiNpcId = "Ronvotri.Cardcha_MiMi";
    public const string MimiSourceId = "ronvotri-cardcha";

    public const string SudokuCanonicalNpcId = "ronvotri.HeyYoureCursed_Sudoku";
    public const string SudokuSourceId = "ronvotri-hey-youre-cursed";

    private static readonly HashSet<string> SudokuNpcAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        SudokuCanonicalNpcId,
        "Sudoku"
    };

    private static readonly string[] SudokuSourceModIds =
    {
        "ronvotri.HeyYoureCursed",
        "Ronvotri.HeyYoureCursed",
        "ronvotri.HeyYoureCursed_Sudoku",
        "ronvotri.chuyentamlinhkoduaduocdau"
    };

    public static bool IsExplicitCustomRecruit(NPC npc)
        => npc.Name.Equals(MimiNpcId, StringComparison.OrdinalIgnoreCase)
            || SudokuNpcAliases.Contains(npc.Name);

    public static bool CanRecruit(NPC npc, IModRegistry registry)
    {
        if (npc.Name.Equals(MimiNpcId, StringComparison.OrdinalIgnoreCase))
            return CanRecruitMimi(npc, registry);

        if (SudokuNpcAliases.Contains(npc.Name))
            return CanRecruitSudoku(npc, registry);

        return false;
    }

    public static bool IsMimiLoaded(IModRegistry registry)
        => registry.IsLoaded(CardchaModId);

    public static bool IsSudokuSourceLoaded(IModRegistry registry)
        => SudokuSourceModIds.Any(registry.IsLoaded);

    private static bool CanRecruitMimi(NPC npc, IModRegistry registry)
    {
        if (!IsMimiLoaded(registry)
            || npc.isInvisible.Value
            || npc.currentLocation is null
            || !npc.displayName.Equals("MiMi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Cardcha's mystery phase displays "???". Its story scenes can show MiMi on
        // the Farm/Wizard handoff, so Team Up deliberately refuses recruitment there.
        // The post-handoff merchant routine owns Town / WizardHouse and is the first
        // safe live state Team Up can identify without touching Cardcha save data.
        string location = npc.currentLocation.NameOrUniqueName;
        if (location.Equals("Farm", StringComparison.OrdinalIgnoreCase))
            return false;

        if (Game1.dialogueUp && location.Equals("WizardHouse", StringComparison.OrdinalIgnoreCase))
            return false;

        return location.Equals("Town", StringComparison.OrdinalIgnoreCase)
            || location.Equals("WizardHouse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool CanRecruitSudoku(NPC npc, IModRegistry registry)
    {
        if (!IsSudokuSourceLoaded(registry)
            || npc.isInvisible.Value
            || npc.currentLocation is null)
        {
            return false;
        }

        // Sudoku's source mod owns materialization / roommate / trust progression.
        // Team Up only accepts the live actor once the source actually exposes her.
        return npc.canTalk() || npc.IsVillager;
    }
}