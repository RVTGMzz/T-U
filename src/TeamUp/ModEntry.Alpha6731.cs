using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6731()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        if (MarlonInvestigationAlpha6729.Stage < MarlonInvestigationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6729();

        return OldMineConnectionAlpha6730.Stage switch
        {
            <= 0 => 6,
            1 => 7,
            2 => 8,
            _ => 9
        };
    }
}
