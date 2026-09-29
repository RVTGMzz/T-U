using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void OnAlpha674444LowerRouteTestCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_lower_route_test.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        bool firstDescent = LowerWorkingsDescentAlpha6742.FirstDescentComplete;
        bool protocolReady = EntryProtocolAlpha6740.ProtocolReady;
        bool high = SurgeHighAlpha6738.IsHigh;
        int slots = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        bool breachReady = Game1.MasterPlayer.modData.TryGetValue(
                ControlledBreachFirstEntryStoryService.BreachLocationKey,
                out string? breachLocation)
            && !string.IsNullOrWhiteSpace(breachLocation);
        bool prereq = firstDescent && protocolReady && high && slots >= TeamUpRosterProgressionService.MaxStoryNpcSlots && breachReady;

        switch (action)
        {
            case "status":
                Monitor.Log(
                    $"Lower route test harness: bypass={(LowerWorkingsInteriorSurveyAlpha6744.RouteTestFormationBypass ? "ARMED" : "OFF")} | "
                    + $"firstDescent={firstDescent} | protocolReady={protocolReady} | high={high} | slots={slots}/4 | "
                    + $"breach={(breachReady ? breachLocation : "none")} | here={Game1.currentLocation.NameOrUniqueName}",
                    LogLevel.Info);
                return;

            case "arm":
                if (!prereq)
                {
                    Monitor.Log(
                        $"Lower route test harness NOT ARMED: prerequisites incomplete | firstDescent={firstDescent} | "
                        + $"protocolReady={protocolReady} | high={high} | slots={slots}/4 | breach={(breachReady ? breachLocation : "none")}.",
                        LogLevel.Warn);
                    return;
                }

                LowerWorkingsInteriorSurveyAlpha6744.SetRouteTestFormationBypass(true);
                Monitor.Log(
                    "Lower route test harness ARMED. This bypasses only the Lower Workings Interior Survey formation count. "
                    + "Use the normal story route: Adventure Guild -> persisted breach -> Action -> Lower Workings -> Action at arrival to return. "
                    + "The bypass auto-clears after the successful return.",
                    LogLevel.Info);
                return;

            case "off":
                LowerWorkingsInteriorSurveyAlpha6744.SetRouteTestFormationBypass(false);
                return;

            default:
                Monitor.Log("Usage: teamup_lower_route_test <status|arm|off>", LogLevel.Info);
                return;
        }
    }
}
