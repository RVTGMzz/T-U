using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int Alpha674446AutoStepTimeoutTicks = 600;

    private static readonly string[] Alpha674446InteriorStateKeys =
    {
        LowerWorkingsInteriorSurveyStoryService.StageKey,
        LowerWorkingsInteriorSurveyStoryService.BreachTileKey,
        LowerWorkingsInteriorSurveyStoryService.InteriorEnteredFlagKey,
        LowerWorkingsInteriorSurveyStoryService.SurveyCompleteFlagKey,
        LowerWorkingsInteriorSurveyStoryService.SafeReturnUsedFlagKey,
        LowerWorkingsInteriorSurveyStoryService.SurveyReportedFlagKey
    };

    private LowerRouteAutoSessionAlpha674446? ActiveLowerRouteAutoAlpha674446;
    private string LowerRouteAutoLastAlpha674446 { get; set; } = "not-run";

    private void RegisterAlpha674446LowerRouteAutoTest()
    {
        Helper.ConsoleCommands.Add(
            "teamup_lower_route_auto",
            "One-command Lower Workings route runtime test. Usage: teamup_lower_route_auto [run|status|cancel].",
            OnAlpha674446LowerRouteAutoCommand);
        Helper.Events.GameLoop.UpdateTicked += OnAlpha674446LowerRouteAutoUpdateTicked;
    }

    private void OnAlpha674446LowerRouteAutoCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("[LowerRouteAuto] Load a save as host before running the automatic route test.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "run" : args[0].Trim().ToLowerInvariant();
        if (action == "status")
        {
            if (ActiveLowerRouteAutoAlpha674446 is null)
                Monitor.Log($"[LowerRouteAuto] idle | last={LowerRouteAutoLastAlpha674446}", LogLevel.Info);
            else
                Monitor.Log($"[LowerRouteAuto] running | step={ActiveLowerRouteAutoAlpha674446.Step} | ticks={ActiveLowerRouteAutoAlpha674446.StepTicks}", LogLevel.Info);
            return;
        }

        if (action == "cancel")
        {
            if (ActiveLowerRouteAutoAlpha674446 is null)
            {
                Monitor.Log("[LowerRouteAuto] nothing to cancel.", LogLevel.Info);
                return;
            }

            FinishAlpha674446LowerRouteAuto(false, "cancelled-by-user");
            return;
        }

        if (action != "run")
        {
            Monitor.Log("Usage: teamup_lower_route_auto [run|status|cancel]", LogLevel.Info);
            return;
        }

        if (ActiveLowerRouteAutoAlpha674446 is not null)
        {
            Monitor.Log("[LowerRouteAuto] A test is already running. Use teamup_lower_route_auto status.", LogLevel.Warn);
            return;
        }

        if (Game1.eventUp)
        {
            Monitor.Log("[LowerRouteAuto] AUTO TEST NOT STARTED: a scripted event is active. Finish the event, then run the command again.", LogLevel.Warn);
            return;
        }

        if (Game1.currentLocation.NameOrUniqueName.Equals(LowerWorkingsLocationNameAlpha6744, StringComparison.OrdinalIgnoreCase))
        {
            Monitor.Log("[LowerRouteAuto] AUTO TEST NOT STARTED: the player is already inside Lower Workings. Leave the room or reload the save, then run the command.", LogLevel.Warn);
            return;
        }

        bool firstDescent = LowerWorkingsDescentAlpha6742.FirstDescentComplete;
        bool protocolReady = EntryProtocolAlpha6740.ProtocolReady;
        bool high = SurgeHighAlpha6738.IsHigh;
        int slots = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        bool prerequisitesReady = firstDescent
            && protocolReady
            && high
            && slots >= TeamUpRosterProgressionService.MaxStoryNpcSlots;

        if (!prerequisitesReady)
        {
            Monitor.Log(
                $"[LowerRouteAuto] AUTO TEST NOT STARTED: prerequisites incomplete | firstDescent={firstDescent} | "
                + $"protocolReady={protocolReady} | high={high} | slots={slots}/4.",
                LogLevel.Warn);
            return;
        }

        if (!TryReadAlpha674446Breach(Game1.MasterPlayer, out string? breachLocation, out Point breachTile))
        {
            Monitor.Log("[LowerRouteAuto] AUTO TEST NOT STARTED: persisted breach location/tile is missing.", LogLevel.Warn);
            return;
        }

        if (Game1.getLocationFromName(LowerWorkingsLocationNameAlpha6744) is null)
        {
            Monitor.Log("[LowerRouteAuto] AUTO TEST NOT STARTED: Lower Workings location is not loaded.", LogLevel.Warn);
            return;
        }

        if (Game1.getLocationFromName(breachLocation!) is null)
        {
            Monitor.Log($"[LowerRouteAuto] AUTO TEST NOT STARTED: persisted breach location '{breachLocation}' is unavailable.", LogLevel.Warn);
            return;
        }

        Dictionary<string, string?> storyState = CaptureAlpha674446InteriorState(Game1.MasterPlayer);
        ActiveLowerRouteAutoAlpha674446 = new LowerRouteAutoSessionAlpha674446
        {
            OriginalLocation = Game1.currentLocation.NameOrUniqueName,
            OriginalTile = Game1.player.TilePoint,
            OriginalStoryState = storyState,
            OriginalBypass = LowerWorkingsInteriorSurveyAlpha6744.RouteTestFormationBypass,
            BreachLocation = breachLocation!,
            BreachTile = breachTile,
            Step = LowerRouteAutoStepAlpha674446.WaitAtBreach
        };

        LowerWorkingsRuntimeGateV2Alpha674438.ResetTelemetry();
        LowerWorkingsInteriorSurveyAlpha6744.SetRouteTestFormationBypass(true);
        LowerWorkingsInteriorSurveyAlpha6744.SetDebugStage(Game1.MasterPlayer, 1);

        CloseAlpha674446AutoDialogue();
        Game1.warpFarmer(breachLocation!, breachTile.X, breachTile.Y, false);

        Monitor.Log(
            $"[LowerRouteAuto] AUTO TEST STARTED | original={ActiveLowerRouteAutoAlpha674446.OriginalLocation}@{SerializeAlpha674446(ActiveLowerRouteAutoAlpha674446.OriginalTile)} "
            + $"| breach={breachLocation}@{SerializeAlpha674446(breachTile)}. Do not move the player until the final PASS/FAIL line appears.",
            LogLevel.Info);
    }

    private void OnAlpha674446LowerRouteAutoUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        LowerRouteAutoSessionAlpha674446? session = ActiveLowerRouteAutoAlpha674446;
        if (session is null)
            return;

        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            LowerRouteAutoLastAlpha674446 = "FAIL:world-became-unavailable";
            ActiveLowerRouteAutoAlpha674446 = null;
            return;
        }

        session.StepTicks++;
        if (session.StepTicks > Alpha674446AutoStepTimeoutTicks)
        {
            FinishAlpha674446LowerRouteAuto(false, $"timeout:{session.Step}");
            return;
        }

        CloseAlpha674446AutoDialogue();
        if (Game1.eventUp || Game1.activeClickableMenu is not null || !Context.IsPlayerFree)
            return;

        switch (session.Step)
        {
            case LowerRouteAutoStepAlpha674446.WaitAtBreach:
                if (!Game1.currentLocation.NameOrUniqueName.Equals(session.BreachLocation, StringComparison.OrdinalIgnoreCase))
                    return;
                if (Game1.player.TilePoint != session.BreachTile)
                {
                    FinishAlpha674446LowerRouteAuto(false, $"breach-tile-mismatch:{SerializeAlpha674446(Game1.player.TilePoint)}");
                    return;
                }

                if (!LowerWorkingsInteriorSurveyAlpha6744.TryHandleLocalAction())
                {
                    FinishAlpha674446LowerRouteAuto(false, "production-entry-handler-rejected");
                    return;
                }

                session.Step = LowerRouteAutoStepAlpha674446.WaitInsideLower;
                session.StepTicks = 0;
                return;

            case LowerRouteAutoStepAlpha674446.WaitInsideLower:
                if (!Game1.currentLocation.NameOrUniqueName.Equals(LowerWorkingsLocationNameAlpha6744, StringComparison.OrdinalIgnoreCase))
                    return;

                if (Game1.player.TilePoint != LowerWorkingsInteriorSurveyStoryService.ArrivalTile)
                {
                    FinishAlpha674446LowerRouteAuto(false, $"lower-arrival-mismatch:{SerializeAlpha674446(Game1.player.TilePoint)}");
                    return;
                }

                if (!LowerWorkingsInteriorSurveyAlpha6744.TryHandleLocalAction())
                {
                    FinishAlpha674446LowerRouteAuto(false, "production-return-handler-rejected");
                    return;
                }

                session.Step = LowerRouteAutoStepAlpha674446.WaitReturned;
                session.StepTicks = 0;
                return;

            case LowerRouteAutoStepAlpha674446.WaitReturned:
                if (!Game1.currentLocation.NameOrUniqueName.Equals(session.BreachLocation, StringComparison.OrdinalIgnoreCase))
                    return;

                Alpha674438LowerWorkingsRuntimeGateV2Service.PreflightSnapshot snapshot =
                    LowerWorkingsRuntimeGateV2Alpha674438.GetPreflightSnapshot();

                bool pass = snapshot.MapValid
                    && snapshot.RouteObserved
                    && snapshot.RoutePass
                    && snapshot.EntryObservations == 1
                    && snapshot.EntryPasses == 1
                    && snapshot.EntryMismatches == 0
                    && snapshot.ReturnObservations == 1
                    && snapshot.ReturnPasses == 1
                    && snapshot.ReturnMismatches == 0
                    && snapshot.MapFailures == 0
                    && snapshot.Errors == 0;

                string detail = BuildAlpha674446ResultLine(snapshot);
                FinishAlpha674446LowerRouteAuto(pass, pass ? "route-contract-pass" : detail, snapshot);
                return;

            case LowerRouteAutoStepAlpha674446.WaitRestored:
                if (!Game1.currentLocation.NameOrUniqueName.Equals(session.OriginalLocation, StringComparison.OrdinalIgnoreCase))
                    return;
                if (Game1.player.TilePoint != session.OriginalTile)
                    return;

                RestoreAlpha674446InteriorState(session);
                PrintAndClearAlpha674446LowerRouteAuto(session);
                return;
        }
    }

    private void FinishAlpha674446LowerRouteAuto(
        bool pass,
        string reason,
        Alpha674438LowerWorkingsRuntimeGateV2Service.PreflightSnapshot? snapshot = null)
    {
        LowerRouteAutoSessionAlpha674446? session = ActiveLowerRouteAutoAlpha674446;
        if (session is null)
            return;

        snapshot ??= LowerWorkingsRuntimeGateV2Alpha674438.GetPreflightSnapshot();
        session.Pass = pass;
        session.ResultReason = reason;
        session.TelemetryLine = BuildAlpha674446ResultLine(snapshot.Value);
        session.Step = LowerRouteAutoStepAlpha674446.WaitRestored;
        session.StepTicks = 0;

        CloseAlpha674446AutoDialogue();
        GameLocation? original = Game1.getLocationFromName(session.OriginalLocation);
        if (original is null)
        {
            RestoreAlpha674446InteriorState(session);
            session.ResultReason += "|original-location-missing";
            PrintAndClearAlpha674446LowerRouteAuto(session);
            return;
        }

        if (Game1.currentLocation.NameOrUniqueName.Equals(session.OriginalLocation, StringComparison.OrdinalIgnoreCase)
            && Game1.player.TilePoint == session.OriginalTile)
        {
            RestoreAlpha674446InteriorState(session);
            PrintAndClearAlpha674446LowerRouteAuto(session);
            return;
        }

        Game1.warpFarmer(session.OriginalLocation, session.OriginalTile.X, session.OriginalTile.Y, false);
    }

    private void RestoreAlpha674446InteriorState(LowerRouteAutoSessionAlpha674446 session)
    {
        Farmer owner = Game1.MasterPlayer;
        int originalStage = 0;
        if (session.OriginalStoryState.TryGetValue(LowerWorkingsInteriorSurveyStoryService.StageKey, out string? rawStage)
            && !string.IsNullOrWhiteSpace(rawStage)
            && int.TryParse(rawStage, out int parsedStage))
        {
            originalStage = Math.Clamp(parsedStage, 0, LowerWorkingsInteriorSurveyStoryService.CompleteStage);
        }

        LowerWorkingsInteriorSurveyAlpha6744.SetDebugStage(owner, originalStage);

        foreach (string key in Alpha674446InteriorStateKeys)
        {
            if (session.OriginalStoryState.TryGetValue(key, out string? value) && value is not null)
                owner.modData[key] = value;
            else
                owner.modData.Remove(key);
        }

        LowerWorkingsInteriorSurveyAlpha6744.SetRouteTestFormationBypass(session.OriginalBypass);
    }

    private void PrintAndClearAlpha674446LowerRouteAuto(LowerRouteAutoSessionAlpha674446 session)
    {
        string status = session.Pass ? "PASS" : "FAIL";
        string lowerRoute = session.Pass ? "PASS" : "FAIL";
        LowerRouteAutoLastAlpha674446 = $"{status}:{session.ResultReason}";

        Monitor.Log(
            $"[LowerRouteAuto] AUTO TEST {status} | {session.TelemetryLine} | lowerRoute={lowerRoute} | "
            + $"saveStateRestored=true | playerRestored={session.OriginalLocation}@{SerializeAlpha674446(session.OriginalTile)} | reason={session.ResultReason}",
            session.Pass ? LogLevel.Info : LogLevel.Warn);

        ActiveLowerRouteAutoAlpha674446 = null;
    }

    private static Dictionary<string, string?> CaptureAlpha674446InteriorState(Farmer owner)
    {
        Dictionary<string, string?> snapshot = new(StringComparer.Ordinal);
        foreach (string key in Alpha674446InteriorStateKeys)
            snapshot[key] = owner.modData.TryGetValue(key, out string? value) ? value : null;
        return snapshot;
    }

    private static bool TryReadAlpha674446Breach(Farmer owner, out string? locationName, out Point tile)
    {
        locationName = null;
        tile = Point.Zero;

        if (!owner.modData.TryGetValue(ControlledBreachFirstEntryStoryService.BreachLocationKey, out string? storedLocation)
            || string.IsNullOrWhiteSpace(storedLocation)
            || !owner.modData.TryGetValue(LowerWorkingsInteriorSurveyStoryService.BreachTileKey, out string? rawTile)
            || string.IsNullOrWhiteSpace(rawTile))
        {
            return false;
        }

        string[] parts = rawTile.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y))
            return false;

        locationName = storedLocation;
        tile = new Point(x, y);
        return true;
    }

    private static string BuildAlpha674446ResultLine(
        Alpha674438LowerWorkingsRuntimeGateV2Service.PreflightSnapshot snapshot)
        => $"entries={snapshot.EntryObservations} entryPass={snapshot.EntryPasses} entryMismatch={snapshot.EntryMismatches} "
            + $"returns={snapshot.ReturnObservations} returnPass={snapshot.ReturnPasses} returnMismatch={snapshot.ReturnMismatches} "
            + $"mapFail={snapshot.MapFailures} errors={snapshot.Errors}";

    private static string SerializeAlpha674446(Point point)
        => $"{point.X},{point.Y}";

    private static void CloseAlpha674446AutoDialogue()
    {
        if (!Game1.eventUp && Game1.activeClickableMenu is not null)
            Game1.exitActiveMenu();
    }

    private enum LowerRouteAutoStepAlpha674446
    {
        WaitAtBreach,
        WaitInsideLower,
        WaitReturned,
        WaitRestored
    }

    private sealed class LowerRouteAutoSessionAlpha674446
    {
        public string OriginalLocation { get; init; } = string.Empty;
        public Point OriginalTile { get; init; }
        public Dictionary<string, string?> OriginalStoryState { get; init; } = new(StringComparer.Ordinal);
        public bool OriginalBypass { get; init; }
        public string BreachLocation { get; init; } = string.Empty;
        public Point BreachTile { get; init; }
        public LowerRouteAutoStepAlpha674446 Step { get; set; }
        public int StepTicks { get; set; }
        public bool Pass { get; set; }
        public string ResultReason { get; set; } = "running";
        public string TelemetryLine { get; set; } = "not-observed";
    }
}
