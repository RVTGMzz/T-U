using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6743()
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

        if (ControlledBreachAlpha6736.Stage < ControlledBreachFirstEntryStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6737();

        if (SurgeHighAlpha6738.Stage < SurgeHighEscalationStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6739();

        if (EntryProtocolAlpha6740.Stage < LowerWorkingsEntryProtocolStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6741();

        return LowerWorkingsDescentAlpha6742.Stage switch
        {
            <= 0 => 30,
            1 => 31,
            2 => 32,
            3 => 33,
            4 => 34,
            _ => 35
        };
    }
}
