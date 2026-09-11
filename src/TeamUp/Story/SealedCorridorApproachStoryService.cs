using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.34 sealed-corridor approach survey.
///
/// Field triangulation has already narrowed the old lower workings to a corridor beside the modern
/// mine network. This chapter does not breach the seal. Instead, a real three-person field team
/// approaches one MineShaft face, holds formation long enough to complete a pressure survey, and
/// reports the physical boundary back to Marlon. Story slot 4 remains reserved for a later Surge HIGH
/// milestone, and the historical miner remains unnamed.
/// </summary>
internal sealed class SealedCorridorApproachStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/SealedCorridorApproachStage";
    public const string SurveyLocationKey = "Ronvotri.TeamUp/Story/SealedCorridorSurveyLocation";
    public const int CompleteStage = 4;
    public const int MinimumFieldPeople = 3;
    public const int MinimumActiveNpcAllies = 1;
    public const int StableSurveyTicksRequired = 240;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getTriangulationStage;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private int _stage;
    private int _stableSurveyTicks;

    public SealedCorridorApproachStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getTriangulationStage,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt)
    {
        _helper = helper;
        _monitor = monitor;
        _getTriangulationStage = getTriangulationStage;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        _stableSurveyTicks = 0;
    }

    public void OnWarped(GameLocation location)
    {
        _stableSurveyTicks = 0;
        if (!CanAdvance())
            return;

        if (!HasRequiredFieldTeam(location))
        {
            if (IsRelevantLocationForCurrentStage(location))
                Hud("story.corridor.need-team");
            return;
        }

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            Show("story.corridor.briefing");
            SetStage(Game1.MasterPlayer, 1, "marlon-pressure-survey-plan");
            Hud("story.corridor.objective.reach");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            Game1.MasterPlayer.modData[SurveyLocationKey] = location.NameOrUniqueName;
            Show("story.corridor.approach-entry");
            SetStage(Game1.MasterPlayer, 2, $"approach-face:{location.NameOrUniqueName}");
            Hud("story.corridor.objective.hold");
            return;
        }

        if (_stage == 2 && location is MineShaft && !IsSurveyLocation(Game1.MasterPlayer, location))
            Hud("story.corridor.wrong-shaft");

        if (_stage == 3 && Is(location, "AdventureGuild"))
        {
            Show("story.corridor.confirmed");
            SetStage(Game1.MasterPlayer, CompleteStage, "sealed-access-face-confirmed");
            Hud("story.corridor.complete");
        }
    }

    public void Update()
    {
        if (_stage != 2 || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (location is not MineShaft
            || !IsSurveyLocation(Game1.MasterPlayer, location)
            || !HasRequiredFieldTeam(location)
            || !Context.IsPlayerFree)
        {
            _stableSurveyTicks = 0;
            return;
        }

        _stableSurveyTicks++;
        if (_stableSurveyTicks < StableSurveyTicksRequired)
            return;

        _stableSurveyTicks = 0;
        Show("story.corridor.pressure-survey");
        SetStage(Game1.MasterPlayer, 3, $"pressure-survey:{location.NameOrUniqueName}");
        Hud("story.corridor.objective.return");
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(SurveyLocationKey);
        _stage = 0;
        _stableSurveyTicks = 0;
        _monitor.Log("[SealedCorridorApproach] stage reset; triangulation and roster progress were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        if (clamped <= 1)
            owner.modData.Remove(SurveyLocationKey);
        else if (clamped == 2 && !owner.modData.ContainsKey(SurveyLocationKey) && Game1.currentLocation is MineShaft)
            owner.modData[SurveyLocationKey] = Game1.currentLocation.NameOrUniqueName;

        _stableSurveyTicks = 0;
        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string surveyLocation = Game1.MasterPlayer.modData.TryGetValue(SurveyLocationKey, out string? stored)
            ? stored
            : "none";
        string objective = _stage switch
        {
            0 when _getTriangulationStage() < FieldTriangulationStoryService.CompleteStage => "finish-field-triangulation",
            0 => "brief-with-marlon-at-guild",
            1 => "approach-triangulated-mine-face",
            2 => "hold-field-team-for-pressure-survey",
            3 => "return-to-marlon",
            _ => "sealed-access-face-confirmed"
        };

        return $"Sealed Corridor Approach: triangulationStage={_getTriangulationStage()}/4 | stage={_stage}/{CompleteStage} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)} | npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)} | "
            + $"surveyLocation={surveyLocation} | stableTicks={_stableSurveyTicks}/{StableSurveyTicksRequired} | objective={objective}";
    }

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getTriangulationStage() >= FieldTriangulationStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasRequiredFieldTeam(GameLocation location)
        => _getFieldPeopleAt(location) >= MinimumFieldPeople
            && _getActiveNpcAlliesAt(location) >= MinimumActiveNpcAllies;

    private bool IsRelevantLocationForCurrentStage(GameLocation location)
        => (_stage is 0 or 3 && Is(location, "AdventureGuild"))
            || (_stage is 1 or 2 && location is MineShaft);

    private static bool IsSurveyLocation(Farmer owner, GameLocation location)
        => owner.modData.TryGetValue(SurveyLocationKey, out string? stored)
            && !string.IsNullOrWhiteSpace(stored)
            && stored.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[SealedCorridorApproach] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
    }

    private static int ReadStage(Farmer owner)
        => owner.modData.TryGetValue(StageKey, out string? raw) && int.TryParse(raw, out int value)
            ? Math.Clamp(value, 0, CompleteStage)
            : 0;

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
