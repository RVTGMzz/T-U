using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.42 first real Lower Workings operation.
///
/// Alpha 6.7.40 proved that the HIGH-response formation can hold a safe staging and withdrawal line.
/// This checkpoint authorizes the team to cross the no-pursuit threshold through the already-braced
/// controlled breach, then inspect only the first interior threshold zone. Until a dedicated Lower
/// Workings map exists, that threshold zone is represented by persistent story state at the exact
/// recorded breach face rather than by pretending a new dungeon map already exists.
/// </summary>
internal sealed class LowerWorkingsDescentStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsDescentStage";
    public const string ThresholdCrossedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed";
    public const string FirstDescentCompleteFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete";
    public const int CompleteStage = 5;
    public const int ThresholdCrossingTicksRequired = 120;
    public const int ThresholdInspectionTicksRequired = 180;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getEntryProtocolStage;
    private readonly Func<bool> _isEntryProtocolReady;
    private readonly Func<bool> _isSurgeHigh;
    private readonly Func<int> _getUnlockedNpcSlots;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private readonly Func<int> _getRequiredFieldPeople;
    private readonly Func<int> _getRequiredNpcAllies;
    private int _stage;
    private int _crossingTicks;
    private int _inspectionTicks;

    public LowerWorkingsDescentStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getEntryProtocolStage,
        Func<bool> isEntryProtocolReady,
        Func<bool> isSurgeHigh,
        Func<int> getUnlockedNpcSlots,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt,
        Func<int> getRequiredFieldPeople,
        Func<int> getRequiredNpcAllies)
    {
        _helper = helper;
        _monitor = monitor;
        _getEntryProtocolStage = getEntryProtocolStage;
        _isEntryProtocolReady = isEntryProtocolReady;
        _isSurgeHigh = isSurgeHigh;
        _getUnlockedNpcSlots = getUnlockedNpcSlots;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
        _getRequiredFieldPeople = getRequiredFieldPeople;
        _getRequiredNpcAllies = getRequiredNpcAllies;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public bool ThresholdCrossed
        => Context.IsWorldReady
            && Game1.MasterPlayer.modData.TryGetValue(ThresholdCrossedFlagKey, out string? value)
            && value == "1";

    public bool FirstDescentComplete
        => Context.IsWorldReady
            && Game1.MasterPlayer.modData.TryGetValue(FirstDescentCompleteFlagKey, out string? value)
            && value == "1";

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        _crossingTicks = 0;
        _inspectionTicks = 0;
        if (_stage >= 3)
            Game1.MasterPlayer.modData[ThresholdCrossedFlagKey] = "1";
        if (_stage >= CompleteStage)
            Game1.MasterPlayer.modData[FirstDescentCompleteFlagKey] = "1";
    }

    public void OnWarped(GameLocation location)
    {
        _crossingTicks = 0;
        _inspectionTicks = 0;
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (!PrerequisitesReady())
        {
            if (Is(location, "AdventureGuild"))
                Hud("story.descent.need-protocol");
            return;
        }

        if (!CanPresent())
            return;

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            if (!TryGetBreachLocation(Game1.MasterPlayer, out _))
            {
                Hud("story.descent.missing-face");
                return;
            }

            Show("story.descent.briefing");
            SetStage(Game1.MasterPlayer, 1, "lower-workings-first-descent-authorized");
            Hud("story.descent.objective.reach");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
            {
                Hud("story.descent.wrong-shaft");
                return;
            }

            if (!HasFullOperationalFormation(location))
            {
                Hud("story.descent.need-full-team");
                return;
            }

            Show("story.descent.threshold-ready");
            SetStage(Game1.MasterPlayer, 2, $"threshold-crossing-line-ready:{location.NameOrUniqueName}");
            Hud("story.descent.objective.cross");
            return;
        }

        if ((_stage == 2 || _stage == 3) && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
                Hud("story.descent.wrong-shaft");
            else if (!HasFullOperationalFormation(location))
                Hud("story.descent.need-full-team");
            return;
        }

        if (_stage == 4 && Is(location, "AdventureGuild"))
        {
            Show("story.descent.report");
            Game1.MasterPlayer.modData[FirstDescentCompleteFlagKey] = "1";
            SetStage(Game1.MasterPlayer, CompleteStage, "first-lower-workings-descent-reported");
            Hud("story.descent.complete");
        }
    }

    public void Update()
    {
        if ((_stage != 2 && _stage != 3) || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (location is not MineShaft
            || !IsBreachLocation(Game1.MasterPlayer, location)
            || !HasFullOperationalFormation(location)
            || !Context.IsPlayerFree)
        {
            _crossingTicks = 0;
            _inspectionTicks = 0;
            return;
        }

        if (_stage == 2)
        {
            _crossingTicks++;
            if (_crossingTicks < ThresholdCrossingTicksRequired)
                return;

            _crossingTicks = 0;
            Game1.MasterPlayer.modData[ThresholdCrossedFlagKey] = "1";
            Game1.MasterPlayer.modData[LowerWorkingsInteriorSurveyStoryService.BreachTileKey] = $"{Game1.player.TilePoint.X},{Game1.player.TilePoint.Y}";
            Show("story.descent.crossed");
            SetStage(Game1.MasterPlayer, 3, $"lower-workings-threshold-crossed:{location.NameOrUniqueName}");
            Hud("story.descent.objective.inspect");
            return;
        }

        _inspectionTicks++;
        if (_inspectionTicks < ThresholdInspectionTicksRequired)
            return;

        _inspectionTicks = 0;
        Show("story.descent.evidence");
        SetStage(Game1.MasterPlayer, 4, $"first-threshold-zone-inspected:{location.NameOrUniqueName}");
        Hud("story.descent.objective.return");
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(ThresholdCrossedFlagKey);
        owner.modData.Remove(FirstDescentCompleteFlagKey);
        owner.modData.Remove(LowerWorkingsInteriorSurveyStoryService.BreachTileKey);
        _stage = 0;
        _crossingTicks = 0;
        _inspectionTicks = 0;
        _monitor.Log("[LowerWorkingsDescent] reset; Entry Protocol, SURGE HIGH, and roster progression were not changed.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        _crossingTicks = 0;
        _inspectionTicks = 0;

        if (clamped >= 3)
            owner.modData[ThresholdCrossedFlagKey] = "1";
        else
            owner.modData.Remove(ThresholdCrossedFlagKey);

        if (clamped >= CompleteStage)
            owner.modData[FirstDescentCompleteFlagKey] = "1";
        else
            owner.modData.Remove(FirstDescentCompleteFlagKey);

        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string breachLocation = TryGetBreachLocation(Game1.MasterPlayer, out string? stored) ? stored! : "none";
        string objective = _stage switch
        {
            0 when _getEntryProtocolStage() < LowerWorkingsEntryProtocolStoryService.CompleteStage || !_isEntryProtocolReady() => "finish-entry-protocol",
            0 when !_isSurgeHigh() => "restore-surge-high-state",
            0 when _getUnlockedNpcSlots() < TeamUpRosterProgressionService.MaxStoryNpcSlots => "restore-story-slot4-authorization",
            0 => "receive-first-descent-order-at-guild",
            1 => "assemble-full-formation-at-recorded-breach-face",
            2 => "hold-full-formation-and-cross-threshold",
            3 => "inspect-first-lower-threshold-zone",
            4 => "return-to-guild-and-report-first-descent",
            _ => "first-lower-workings-descent-complete"
        };

        return $"Lower Workings Descent: entryProtocol={_getEntryProtocolStage()}/{LowerWorkingsEntryProtocolStoryService.CompleteStage} | "
            + $"protocolReady={_isEntryProtocolReady()} | high={_isSurgeHigh()} | stage={_stage}/{CompleteStage} | "
            + $"thresholdCrossed={ThresholdCrossed} | firstDescentComplete={FirstDescentComplete} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)}/{_getRequiredFieldPeople()} | "
            + $"npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)}/{_getRequiredNpcAllies()} | breachLocation={breachLocation} | "
            + $"crossingTicks={_crossingTicks}/{ThresholdCrossingTicksRequired} | inspectionTicks={_inspectionTicks}/{ThresholdInspectionTicksRequired} | objective={objective}";
    }

    private bool PrerequisitesReady()
        => _getEntryProtocolStage() >= LowerWorkingsEntryProtocolStoryService.CompleteStage
            && _isEntryProtocolReady()
            && _isSurgeHigh()
            && _getUnlockedNpcSlots() >= TeamUpRosterProgressionService.MaxStoryNpcSlots;

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && PrerequisitesReady()
            && CanPresent();

    private bool CanPresent()
        => !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasFullOperationalFormation(GameLocation location)
        => _getFieldPeopleAt(location) >= _getRequiredFieldPeople()
            && _getActiveNpcAlliesAt(location) >= _getRequiredNpcAllies();

    private static bool TryGetBreachLocation(Farmer owner, out string? stored)
        => owner.modData.TryGetValue(ControlledBreachFirstEntryStoryService.BreachLocationKey, out stored)
            && !string.IsNullOrWhiteSpace(stored);

    private static bool IsBreachLocation(Farmer owner, GameLocation location)
        => TryGetBreachLocation(owner, out string? stored)
            && stored!.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[LowerWorkingsDescent] stage -> {_stage}/{CompleteStage} crossed={ThresholdCrossed} complete={FirstDescentComplete} source={source}.", LogLevel.Info);
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
