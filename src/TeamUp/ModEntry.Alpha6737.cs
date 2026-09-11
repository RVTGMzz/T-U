using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6737()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        if (MarlonInvestigationAlpha6729.Stage < MarlonInvestigationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6729();

        if (OldMineConnectionAlpha6730.Stage < OldMineConnectionStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6731();

        if (FieldTriangulationAlpha6732.Stage < FieldTriangulationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6733();

        if (CorridorApproachAlpha6734.Stage < SealedCorridorApproachStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6735();

        return ControlledBreachAlpha6736.Stage switch
        {
            <= 0 => 17,
            1 => 18,
            2 => 19,
            3 => 20,
            4 => 21,
            _ => 22
        };
    }
}
