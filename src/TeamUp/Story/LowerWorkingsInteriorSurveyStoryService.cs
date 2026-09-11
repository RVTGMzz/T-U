using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.44 dedicated Lower Workings chamber foundation and first interior survey.
///
/// The location is a real save-backed Stardew 1.6 GameLocation created through Data/Locations.
/// Entry is bound to the recorded controlled-breach MineShaft plus a persisted tile anchor. The
/// survey remains deliberately non-combat: it establishes structural clues and escalating Mutation
/// evidence while preserving the historical worker's anonymity and leaving the containment encounter
/// for the next checkpoint.
/// </summary>
internal sealed class LowerWorkingsInteriorSurveyStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyStage";
    public const string BreachTileKey = "Ronvotri.TeamUp/Story/LowerWorkingsBreachTile";
    public const string InteriorEnteredFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsInteriorEntered";
    public const string SurveyCompleteFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyComplete";
    public const string SafeReturnUsedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsSafeReturnUsed";
    public const string SurveyReportedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyReported";
    public const int CompleteStage = 6;
    public const int SurveyHoldTicksRequired = 120;
    public const int ClueRadius = 2;
    public static readonly Point ArrivalTile = new(15, 21);
    public static readonly Point CribbingClueTile = new(8, 9);
    public static readonly Point MutationTraceClueTile = new(22, 8);
    public static readonly Point SealedDepthClueTile = new(23, 16);

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly string _locationName;
    private readonly Func<bool> _isFirstDescentComplete;
    private readonly Func<bool> _isEntryProtocolReady;
    private readonly Func<bool> _isSurgeHigh;
    private readonly Func<int> _getUnlockedNpcSlots;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private readonly Func<int> _getRequiredFieldPeople;
    private readonly Func<int> _getRequiredNpcAllies;
    private int _stage;
    private int _surveyHoldTicks;

    public LowerWorkingsInteriorSurveyStoryService(
        IModHelper helper,
        IMonitor monitor,
        string locationName,
        Func<bool> isFirstDescentComplete,
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
        _locationName = locationName;
        _isFirstDescentComplete = isFirstDescentComplete;
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

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        _surveyHoldTicks = 0;
        if (!Context.IsMainPlayer)
            return;

        if (_stage >= 2)
            Game1.MasterPlayer.modData[InteriorEnteredFlagKey] = "1";
        if (_stage >= 5)
            Game1.MasterPlayer.modData[SurveyCompleteFlagKey] = "1";
        if (_stage >= CompleteStage)
        {
            Game1.MasterPlayer.modData[SafeReturnUsedFlagKey] = "1";
            Game1.MasterPlayer.modData[SurveyReportedFlagKey] = "1";
        }
    }

    public void OnLocalWarped(GameLocation location)
    {
        _surveyHoldTicks = 0;
        if (!Context.IsWorldReady)
            return;

        if (!Context.IsMainPlayer)
        {
            if (IsLowerWorkings(location) && ReadStage(Game1.MasterPlayer) >= 2)
                Hud("story.interior.objective.join");
            return;
        }

        _stage = ReadStage(Game1.MasterPlayer);
        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            if (!PrerequisitesReady())
            {
                Hud("story.interior.need-first-descent");
                return;
            }

            if (!TryGetBreachLocation(Game1.MasterPlayer, out _))
            {
                Hud("story.interior.missing-face");
                return;
            }

            Show("story.interior.briefing");
            SetStage(Game1.MasterPlayer, 1, "dedicated-interior-survey-authorized");
            Hud("story.interior.objective.reach");
            return;
        }

        if (_stage == 1 && IsBreachLocation(Game1.MasterPlayer, location))
        {
            Hud("story.interior.objective.enter");
            return;
        }

        if (_stage == 2 && IsLowerWorkings(location))
        {
            if (!Game1.MasterPlayer.modData.ContainsKey(InteriorEnteredFlagKey))
            {
                Game1.MasterPlayer.modData[InteriorEnteredFlagKey] = "1";
                Show("story.interior.entered");
            }
            Hud("story.interior.objective.cribbing");
            return;
        }

        if (_stage == 3 && IsLowerWorkings(location))
        {
            Hud("story.interior.objective.trace");
            return;
        }

        if (_stage == 4 && IsLowerWorkings(location))
        {
            Hud("story.interior.objective.seal");
            return;
        }

        if (_stage == 5 && IsLowerWorkings(location))
        {
            Hud("story.interior.objective.return");
            return;
        }

        if (_stage == 5
            && Is(location, "AdventureGuild")
            && HasFlag(Game1.MasterPlayer, SafeReturnUsedFlagKey))
        {
            Show("story.interior.report");
            Game1.MasterPlayer.modData[SurveyReportedFlagKey] = "1";
            SetStage(Game1.MasterPlayer, CompleteStage, "first-dedicated-interior-survey-reported");
            Hud("story.interior.complete");
        }
    }

    public void Update()
    {
        if (_stage is not (2 or 3 or 4) || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (!IsLowerWorkings(location)
            || !HasFullOperationalFormation(location)
            || !Context.IsPlayerFree)
        {
            _surveyHoldTicks = 0;
            return;
        }

        Point target = _stage switch
        {
            2 => CribbingClueTile,
            3 => MutationTraceClueTile,
            _ => SealedDepthClueTile
        };

        if (!Near(Game1.player.TilePoint, target, ClueRadius))
        {
            _surveyHoldTicks = 0;
            return;
        }

        _surveyHoldTicks++;
        if (_surveyHoldTicks < SurveyHoldTicksRequired)
            return;

        _surveyHoldTicks = 0;
        if (_stage == 2)
        {
            Show("story.interior.clue.cribbing");
            SetStage(Game1.MasterPlayer, 3, "directed-cribbing-surveyed");
            Hud("story.interior.objective.trace");
            return;
        }

        if (_stage == 3)
        {
            Show("story.interior.clue.trace");
            SetStage(Game1.MasterPlayer, 4, "mutation-trace-surveyed");
            Hud("story.interior.objective.seal");
            return;
        }

        Show("story.interior.clue.seal");
        Game1.MasterPlayer.modData[SurveyCompleteFlagKey] = "1";
        SetStage(Game1.MasterPlayer, 5, "sealed-depth-edge-surveyed");
        Hud("story.interior.objective.return");
    }

    public bool TryHandleLocalAction()
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return false;

        Farmer owner = Game1.MasterPlayer;
        int persistedStage = ReadStage(owner);
        GameLocation location = Game1.currentLocation;

        if (IsLowerWorkings(location) && persistedStage >= 2)
        {
            if (!Near(Game1.player.TilePoint, ArrivalTile, ClueRadius))
                return false;

            if (Context.IsMainPlayer && persistedStage >= 5)
                owner.modData[SafeReturnUsedFlagKey] = "1";
            else if (Context.IsMainPlayer)
                Hud("story.interior.withdraw-early");

            if (!TryWarpToBreach(owner))
                Hud("story.interior.return-failed");
            return true;
        }

        if (!IsBreachLocation(owner, location) || persistedStage < 1 || persistedStage >= CompleteStage)
            return false;

        if (!TryGetBreachAnchor(owner, out Point anchor))
        {
            if (!Context.IsMainPlayer || persistedStage != 1)
            {
                Hud("story.interior.wait-host");
                return true;
            }

            if (!HasFullOperationalFormation(location))
            {
                Hud("story.interior.need-full-team");
                return true;
            }

            anchor = Game1.player.TilePoint;
            owner.modData[BreachTileKey] = Serialize(anchor);
            Hud("story.interior.anchor-calibrated");
        }

        if (!Near(Game1.player.TilePoint, anchor, 1))
            return false;

        if (Context.IsMainPlayer && persistedStage == 1)
        {
            if (!PrerequisitesReady())
            {
                Hud("story.interior.need-first-descent");
                return true;
            }
            if (!HasFullOperationalFormation(location))
            {
                Hud("story.interior.need-full-team");
                return true;
            }

            Show("story.interior.crossing");
            SetStage(owner, 2, $"entered-dedicated-lower-workings:{location.NameOrUniqueName}@{Serialize(anchor)}");
        }
        else if (!Context.IsMainPlayer && persistedStage == 1)
        {
            Hud("story.interior.wait-host");
            return true;
        }

        if (!TryWarpIntoLowerWorkings())
            Hud("story.interior.map-missing");
        return true;
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(BreachTileKey);
        owner.modData.Remove(InteriorEnteredFlagKey);
        owner.modData.Remove(SurveyCompleteFlagKey);
        owner.modData.Remove(SafeReturnUsedFlagKey);
        owner.modData.Remove(SurveyReportedFlagKey);
        _stage = 0;
        _surveyHoldTicks = 0;
        _monitor.Log("[LowerWorkingsInterior] reset; first descent, Entry Protocol, SURGE HIGH, roster, Mutation, and capture state were not changed.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        _surveyHoldTicks = 0;
        SetFlag(owner, InteriorEnteredFlagKey, clamped >= 2);
        SetFlag(owner, SurveyCompleteFlagKey, clamped >= 5);
        SetFlag(owner, SafeReturnUsedFlagKey, clamped >= CompleteStage);
        SetFlag(owner, SurveyReportedFlagKey, clamped >= CompleteStage);
        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string breachLocation = TryGetBreachLocation(Game1.MasterPlayer, out string? stored) ? stored! : "none";
        string anchor = TryGetBreachAnchor(Game1.MasterPlayer, out Point point) ? Serialize(point) : "none";
        string objective = _stage switch
        {
            0 when !_isFirstDescentComplete() => "finish-first-descent",
            0 => "receive-dedicated-survey-order-at-guild",
            1 => "enter-dedicated-lower-workings-at-recorded-breach-anchor",
            2 => "survey-directed-cribbing",
            3 => "survey-mutation-trace",
            4 => "survey-sealed-depth-edge",
            5 when !HasFlag(Game1.MasterPlayer, SafeReturnUsedFlagKey) => "use-safe-return-path",
            5 => "report-at-adventure-guild",
            _ => "first-dedicated-interior-survey-complete"
        };

        return $"Lower Workings Interior Survey: stage={_stage}/{CompleteStage} | firstDescent={_isFirstDescentComplete()} | "
            + $"protocolReady={_isEntryProtocolReady()} | high={_isSurgeHigh()} | slots={_getUnlockedNpcSlots()}/4 | "
            + $"locationLoaded={Game1.getLocationFromName(_locationName) is not null} | here={currentLocation.NameOrUniqueName} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)}/{_getRequiredFieldPeople()} | npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)}/{_getRequiredNpcAllies()} | "
            + $"breachLocation={breachLocation} | breachTile={anchor} | entered={HasFlag(Game1.MasterPlayer, InteriorEnteredFlagKey)} | "
            + $"surveyComplete={HasFlag(Game1.MasterPlayer, SurveyCompleteFlagKey)} | safeReturn={HasFlag(Game1.MasterPlayer, SafeReturnUsedFlagKey)} | "
            + $"reported={HasFlag(Game1.MasterPlayer, SurveyReportedFlagKey)} | hold={_surveyHoldTicks}/{SurveyHoldTicksRequired} | objective={objective}";
    }

    private bool PrerequisitesReady()
        => _isFirstDescentComplete()
            && _isEntryProtocolReady()
            && _isSurgeHigh()
            && _getUnlockedNpcSlots() >= TeamUpRosterProgressionService.MaxStoryNpcSlots;

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && PrerequisitesReady()
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasFullOperationalFormation(GameLocation location)
        => _getFieldPeopleAt(location) >= _getRequiredFieldPeople()
            && _getActiveNpcAlliesAt(location) >= _getRequiredNpcAllies();

    private bool TryWarpIntoLowerWorkings()
    {
        if (Game1.getLocationFromName(_locationName) is null)
            return false;
        Game1.warpFarmer(_locationName, ArrivalTile.X, ArrivalTile.Y, false);
        return true;
    }

    private bool TryWarpToBreach(Farmer owner)
    {
        if (!TryGetBreachLocation(owner, out string? locationName) || !TryGetBreachAnchor(owner, out Point anchor))
            return false;
        Game1.warpFarmer(locationName!, anchor.X, anchor.Y, false);
        return true;
    }

    private bool IsLowerWorkings(GameLocation location)
        => location.NameOrUniqueName.Equals(_locationName, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetBreachLocation(Farmer owner, out string? stored)
        => owner.modData.TryGetValue(ControlledBreachFirstEntryStoryService.BreachLocationKey, out stored)
            && !string.IsNullOrWhiteSpace(stored);

    private static bool IsBreachLocation(Farmer owner, GameLocation location)
        => TryGetBreachLocation(owner, out string? stored)
            && stored!.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetBreachAnchor(Farmer owner, out Point point)
    {
        point = Point.Zero;
        if (!owner.modData.TryGetValue(BreachTileKey, out string? raw) || string.IsNullOrWhiteSpace(raw))
            return false;
        string[] parts = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !int.TryParse(parts[0], out int x)
            || !int.TryParse(parts[1], out int y))
        {
            return false;
        }

        point = new Point(x, y);
        return true;
    }

    private static string Serialize(Point point) => $"{point.X},{point.Y}";

    private static bool Near(Point a, Point b, int radius)
        => Math.Abs(a.X - b.X) <= radius && Math.Abs(a.Y - b.Y) <= radius;

    private static bool HasFlag(Farmer owner, string key)
        => owner.modData.TryGetValue(key, out string? value) && value == "1";

    private static void SetFlag(Farmer owner, string key, bool value)
    {
        if (value)
            owner.modData[key] = "1";
        else
            owner.modData.Remove(key);
    }

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[LowerWorkingsInterior] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
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
