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

    private const int MimiMerchantStartTime = 1100;
    private const int MimiMerchantEndTime = 1700;

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
        if (!Context.IsWorldReady
            || !IsMimiLoaded(registry)
            || npc.isInvisible.Value
            || npc.currentLocation is null
            || !npc.displayName.Equals("MiMi", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Alpha 6.5.1: Cardcha promotes the canonical MiMi actor into Stardew's social
        // layer only after MimiMeetupCompleted, and that promotion creates the canonical
        // friendshipData entry. Treat that live entry as the fail-closed unlock contract.
        // This avoids reading or rewriting Cardcha save data while preventing Team Up from
        // recruiting the mystery actor or the Wizard/Farm handoff presentation.
        if (!Game1.player.friendshipData.ContainsKey(MimiNpcId))
            return false;

        // Never allow an invite while Cardcha still owns an event/dialogue/menu presentation.
        if (Game1.eventUp || Game1.dialogueUp || Game1.activeClickableMenu is not null)
            return false;

        // Cardcha's post-handoff merchant window is Mon-Fri, 11:00-17:00. Restricting Team Up
        // to the same observable window prevents a social MiMi at home/story staging from being
        // mistaken for the recruitable merchant actor.
        if (!IsMimiMerchantWeekday()
            || Game1.timeOfDay < MimiMerchantStartTime
            || Game1.timeOfDay >= MimiMerchantEndTime)
        {
            return false;
        }

        string location = npc.currentLocation.NameOrUniqueName;
        return location.Equals("Town", StringComparison.OrdinalIgnoreCase)
            || location.Equals("WizardHouse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMimiMerchantWeekday()
    {
        // Stardew seasons begin on Monday: day 1..5 = Mon..Fri, 6..7 = weekend.
        int dayIndex = (Math.Max(1, Game1.dayOfMonth) - 1) % 7;
        return dayIndex <= 4;
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
