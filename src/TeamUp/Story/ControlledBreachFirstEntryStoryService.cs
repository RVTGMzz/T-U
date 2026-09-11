using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.36 controlled breach / first-entry probe.
///
/// The sealed access face has already been triangulated and pressure-surveyed. This checkpoint
/// returns a real three-person field team to that exact MineShaft face, holds formation while a
/// narrow controlled opening is stabilized, then performs only a short threshold probe into the
/// old workings. No custom lower-workings map is claimed here, no boss is spawned, story slot 4
/// remains locked, and the historical miner remains unnamed.
/// </summary>
internal sealed class ControlledBreachFirstEntryStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/ControlledBreachFirstEntryStage";
    public const string BreachLocationKey = "Ronvotri.TeamUp/Story/ControlledBreachLocation";
    public const int CompleteStage = 5;
    public const int MinimumFieldPeople = 3;
    public const int MinimumActiveNpcAllies = 1;
    public const int BreachHoldTicksRequired = 180;
    public const int EntryProbeTicksRequired = 120;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getCorridorStage;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private int _stage;
    private int _breachHoldTicks;
    private int _entryProbeTicks;

    public ControlledBreachFirstEntryStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getCorridorStage,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt)
    {
        _helper = helper;
        _monitor = monitor;
        _getCorridorStage = getCorridorStage;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        ResetRuntimeHolds();
    }

    public void OnWarped(GameLocation location)
    {
        ResetRuntimeHolds();
        if (!CanAdvance())
            return;

        if (!HasRequiredFieldTeam(location))
        {
            if (IsRelevantLocationForCurrentStage(location))
                Hud("story.breach.need-team");
            return;
        }

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            string? surveyLocation = ResolveSurveyLocation(Game1.MasterPlayer);
            if (string.IsNullOrWhiteSpace(surveyLocation))
            {
                Hud("story.breach.missing-face");
                return;
            }

            Game1.MasterPlayer.modData[BreachLocationKey] = surveyLocation;
            Show("story.breach.briefing");
            SetStage(Game1.MasterPlayer, 1, $"controlled-breach-briefing:{surveyLocation}");
            Hud("story.breach.objective.reach");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
            {
                Hud("story.breach.wrong-shaft");
                return;
            }

            Show("story.breach.arrival");
            SetStage(Game1.MasterPlayer, 2, $"breach-face-ready:{location.NameOrUniqueName}");
            Hud("story.breach.objective.stabilize");
            return;
        }

        if (_stage is 2 or 3 && location is MineShaft && !IsBreachLocation(Game1.MasterPlayer, location))
            Hud("story.breach.wrong-shaft");

        if (_stage == 4 && Is(location, "AdventureGuild"))
        {
            Show("story.breach.report");
            SetStage(Game1.MasterPlayer, CompleteStage, "first-entry-probe-reported");
            Hud("story.breach.complete");
        }
    }

    public void Update()
    {
        if (_stage is not (2 or 3) || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (location is not MineShaft
            || !IsBreachLocation(Game1.MasterPlayer, location)
            || !HasRequiredFieldTeam(location)
            || !Context.IsPlayerFree)
        {
            ResetRuntimeHolds();
            return;
        }

        if (_stage == 2)
        {
            _entryProbeTicks = 0;
            _breachHoldTicks++;
            if (_breachHoldTicks < BreachHoldTicksRequired)
                return;

            _breachHoldTicks = 0;
            Show("story.breach.opened");
            SetStage(Game1.MasterPlayer, 3, $"controlled-opening:{location.NameOrUniqueName}");
            Hud("story.breach.objective.probe");
            return;
        }

        _breachHoldTicks = 0;
        _entryProbeTicks++;
        if (_entryProbeTicks < EntryProbeTicksRequired)
            return;

        _entryProbeTicks = 0;
        Show("story.breach.first-entry");
        SetStage(Game1.MasterPlayer, 4, $"first-entry-threshold-probe:{location.NameOrUniqueName}");
        Hud("story.breach.objective.return");
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(BreachLocationKey);
        _stage = 0;
        ResetRuntimeHolds();
        _monitor.Log("[ControlledBreach] stage reset; corridor, triangulation, reactions, and roster progress were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        if (clamped == 0)
            owner.modData.Remove(BreachLocationKey);
        else if (!owner.modData.ContainsKey(BreachLocationKey))
        {
            string? surveyLocation = ResolveSurveyLocation(owner);
            if (!string.IsNullOrWhiteSpace(surveyLocation))
                owner.modData[BreachLocationKey] = surveyLocation;
            else if (Game1.currentLocation is MineShaft)
                owner.modData[BreachLocationKey] = Game1.currentLocation.NameOrUniqueName;
        }

        ResetRuntimeHolds();
        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string breachLocation = Game1.MasterPlayer.modData.TryGetValue(BreachLocationKey, out string? stored)
            ? stored
            : "none";
        string objective = _stage switch
        {
            0 when _getCorridorStage() < SealedCorridorApproachStoryService.CompleteStage => "finish-sealed-corridor-approach",
            0 => "brief-with-marlon-at-guild",
            1 => "return-to-confirmed-survey-face",
            2 => "hold-field-team-for-controlled-opening",
            3 => "hold-field-team-for-threshold-probe",
            4 => "return-to-marlon",
            _ => "first-entry-probe-reported"
        };

        return $"Controlled Breach / First Entry: corridorStage={_getCorridorStage()}/4 | stage={_stage}/{CompleteStage} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)} | npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)} | "
            + $"breachLocation={breachLocation} | breachTicks={_breachHoldTicks}/{BreachHoldTicksRequired} | "
            + $"probeTicks={_entryProbeTicks}/{EntryProbeTicksRequired} | objective={objective}";
    }

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getCorridorStage() >= SealedCorridorApproachStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasRequiredFieldTeam(GameLocation location)
        => _getFieldPeopleAt(location) >= MinimumFieldPeople
            && _getActiveNpcAlliesAt(location) >= MinimumActiveNpcAllies;

    private bool IsRelevantLocationForCurrentStage(GameLocation location)
        => (_stage is 0 or 4 && Is(location, "AdventureGuild"))
            || (_stage is 1 or 2 or 3 && location is MineShaft);

    private static string? ResolveSurveyLocation(Farmer owner)
        => owner.modData.TryGetValue(SealedCorridorApproachStoryService.SurveyLocationKey, out string? stored)
            && !string.IsNullOrWhiteSpace(stored)
            ? stored
            : null;

    private static bool IsBreachLocation(Farmer owner, GameLocation location)
        => owner.modData.TryGetValue(BreachLocationKey, out string? stored)
            && !string.IsNullOrWhiteSpace(stored)
            && stored.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[ControlledBreach] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
    }

    private static int ReadStage(Farmer owner)
        => owner.modData.TryGetValue(StageKey, out string? raw) && int.TryParse(raw, out int value)
            ? Math.Clamp(value, 0, CompleteStage)
            : 0;

    private void ResetRuntimeHolds()
    {
        _breachHoldTicks = 0;
        _entryProbeTicks = 0;
    }

    private static bool Is(GameLocation location, string name)
        => location.NameOrUniqueName.Equals(name, StringComparison.OrdinalIgnoreCase);

    private void Show(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text))
            Game1.drawObjectDialogue(text);
    }

    private void Hud(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text) && !Game1.eventUp)
            Game1.showGlobalMessage(text);
    }
}
