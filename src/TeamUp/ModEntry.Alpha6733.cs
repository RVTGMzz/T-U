using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6733()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        if (MarlonInvestigationAlpha6729.Stage < MarlonInvestigationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6729();

        if (OldMineConnectionAlpha6730.Stage < OldMineConnectionStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6731();

        return FieldTriangulationAlpha6732.Stage switch
        {
            <= 0 => 9,
            1 => 10,
            2 => 11,
            3 => 12,
            _ => 13
        };
    }
}
