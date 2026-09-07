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
        => CanRecruit(npc, registry, Game1.player);

    public static bool CanRecruit(NPC npc, IModRegistry registry, Farmer farmer)
    {
        if (npc.Name.Equals(MimiNpcId, StringComparison.OrdinalIgnoreCase))
            return CanRecruitMimi(npc, registry, farmer);

        if (SudokuNpcAliases.Contains(npc.Name))
            return CanRecruitSudoku(npc, registry);

        return false;
    }

    public static bool IsMimiLoaded(IModRegistry registry)
        => registry.IsLoaded(CardchaModId);

    public static bool IsSudokuSourceLoaded(IModRegistry registry)
        => SudokuSourceModIds.Any(registry.IsLoaded);

    private static bool CanRecruitMimi(NPC npc, IModRegistry registry, Farmer farmer)
    {
        if (!Context.IsWorldReady
            || !IsMimiLoaded(registry)
            || npc.IsInvisible
            || npc.currentLocation is null
            || !npc.displayName.Equals("MiMi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Cardcha promotes the canonical MiMi actor into Stardew's social layer only after
        // its Wizard meetup. In multiplayer validate the friendship entry of the Farmer who
        // actually sent the recruit request, not whichever machine happens to be the host.
        if (!farmer.friendshipData.ContainsKey(MimiNpcId))
            return false;

        // A promoted social MiMi may legitimately be at her merchant spot, at home, or at a
        // future source-controlled social location. Don't couple recruitment to Cardcha's work
        // schedule. We only refuse while Cardcha/Stardew owns an event/dialogue/menu presentation.
        if (Game1.eventUp || Game1.dialogueUp || Game1.activeClickableMenu is not null)
            return false;

        return npc.canTalk() || npc.IsVillager;
    }

    private static bool CanRecruitSudoku(NPC npc, IModRegistry registry)
    {
        if (!IsSudokuSourceLoaded(registry)
            || npc.IsInvisible
            || npc.currentLocation is null)
        {
            return false;
        }

        // Sudoku's source mod owns materialization / roommate / trust progression.
        // Team Up only accepts the live actor once the source actually exposes her.
        return npc.canTalk() || npc.IsVillager;
    }
}
