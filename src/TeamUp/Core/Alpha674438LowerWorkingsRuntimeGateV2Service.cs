using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.38: read-only Lower Workings runtime validator.
///
/// Safety rules:
/// - no Harmony patches;
/// - no SaveLoaded or UpdateTicked subscription;
/// - no reflection/assembly scanning;
/// - no warp calls;
/// - no modData/story-state writes;
/// - observes only the existing local Warped event and explicit console status command.
/// </summary>
internal sealed class Alpha674438LowerWorkingsRuntimeGateV2Service
{
    private const int ExpectedMapWidth = 32;
    private const int ExpectedMapHeight = 24;

    private readonly IMonitor _monitor;
    private readonly string _locationName;
    private readonly Func<int> _getSurveyStage;
    private readonly Func<bool> _isFirstDescentComplete;
    private readonly Func<bool> _isEntryProtocolReady;
    private readonly Func<bool> _isSurgeHigh;
    private readonly Func<int> _getUnlockedNpcSlots;

    private int _entryObservations;
    private int _entryPasses;
    private int _entryMismatches;
    private int _returnObservations;
    private int _returnPasses;
    private int _returnMismatches;
    private int _farmhandObservations;
    private int _mapPasses;
    private int _mapFailures;
    private int _errors;
    private string _last = "not-observed";

    public Alpha674438LowerWorkingsRuntimeGateV2Service(
        IMonitor monitor,
        string locationName,
        Func<int> getSurveyStage,
        Func<bool> isFirstDescentComplete,
        Func<bool> isEntryProtocolReady,
        Func<bool> isSurgeHigh,
        Func<int> getUnlockedNpcSlots)
    {
        _monitor = monitor;
        _locationName = locationName;
        _getSurveyStage = getSurveyStage;
        _isFirstDescentComplete = isFirstDescentComplete;
        _isEntryProtocolReady = isEntryProtocolReady;
        _isSurgeHigh = isSurgeHigh;
        _getUnlockedNpcSlots = getUnlockedNpcSlots;

        _monitor.Log(
            "Team Up 6.7.44.38 Lower Workings Runtime Gate v2 ready: read-only/lazy validator; no SaveLoaded hook, no Harmony, no runtime state writes.",
            LogLevel.Info);
    }

    public void ObserveWarp(GameLocation oldLocation, GameLocation newLocation)
    {
        if (!Context.IsWorldReady)
            return;

        bool oldIsLower = IsLower(oldLocation);
        bool newIsLower = IsLower(newLocation);
        if (oldIsLower == newIsLower)
            return;

        try
        {
            if (!Context.IsMainPlayer)
                _farmhandObservations++;

            if (newIsLower)
            {
                _entryObservations++;
                Point actual = Game1.player.TilePoint;
                Point expected = LowerWorkingsInteriorSurveyStoryService.ArrivalTile;

                if (actual == expected)
                {
                    _entryPasses++;
                    _last = $"entry-pass:{newLocation.NameOrUniqueName}@{Serialize(actual)}";
                }
                else
                {
                    _entryMismatches++;
                    _last = $"entry-mismatch:expected={Serialize(expected)} actual={Serialize(actual)}";
                    _monitor.Log(
                        $"[LowerWorkingsGateV2] entry arrival mismatch: expected {Serialize(expected)}, got {Serialize(actual)}.",
                        LogLevel.Warn);
                }

                ObserveMap("entry");
                return;
            }

            _returnObservations++;
            if (!TryReadPersistedBreach(out string? expectedLocation, out Point expectedTile))
            {
                _returnMismatches++;
                _last = $"return-mismatch:no-persisted-breach actual={newLocation.NameOrUniqueName}@{Serialize(Game1.player.TilePoint)}";
                _monitor.Log("[LowerWorkingsGateV2] return observed without a persisted breach anchor.", LogLevel.Warn);
                return;
            }

            Point actualTile = Game1.player.TilePoint;
            bool locationMatch = newLocation.NameOrUniqueName.Equals(expectedLocation, StringComparison.OrdinalIgnoreCase);
            bool tileMatch = actualTile == expectedTile;
            if (locationMatch && tileMatch)
            {
                _returnPasses++;
                _last = $"return-pass:{expectedLocation}@{Serialize(expectedTile)}";
            }
            else
            {
                _returnMismatches++;
                _last = $"return-mismatch:expected={expectedLocation}@{Serialize(expectedTile)} actual={newLocation.NameOrUniqueName}@{Serialize(actualTile)}";
                _monitor.Log(
                    $"[LowerWorkingsGateV2] exact return mismatch: expected {expectedLocation}@{Serialize(expectedTile)}, got {newLocation.NameOrUniqueName}@{Serialize(actualTile)}.",
                    LogLevel.Warn);
            }
        }
        catch (Exception ex)
        {
            _errors++;
            _last = $"warp-observation-error:{ex.GetType().Name}";
            _monitor.Log($"[LowerWorkingsGateV2] read-only warp observation failed safely: {ex.GetType().Name}: {ex.Message}", LogLevel.Warn);
        }
    }

    public void ResetTelemetry()
    {
        _entryObservations = 0;
        _entryPasses = 0;
        _entryMismatches = 0;
        _returnObservations = 0;
        _returnPasses = 0;
        _returnMismatches = 0;
        _farmhandObservations = 0;
        _mapPasses = 0;
        _mapFailures = 0;
        _errors = 0;
        _last = "reset";
    }

    public string Describe()
    {
        MapSnapshot map = ProbeMap();
        string breach = TryReadPersistedBreach(out string? breachLocation, out Point breachTile)
            ? $"{breachLocation}@{Serialize(breachTile)}"
            : "none";
        string state = ResolveState(map.Valid);

        return $"Lower Workings runtime gate v2: state={state} | readonly=true | locationLoaded={map.LocationLoaded} | "
            + $"runtimeType={map.RuntimeType} | map={map.Width}x{map.Height} | layers={map.Layers} | mapDetail={map.Detail} | "
            + $"arrival={Serialize(LowerWorkingsInteriorSurveyStoryService.ArrivalTile)} | stage={_getSurveyStage()}/{LowerWorkingsInteriorSurveyStoryService.CompleteStage} | "
            + $"firstDescent={_isFirstDescentComplete()} | protocolReady={_isEntryProtocolReady()} | high={_isSurgeHigh()} | slots={_getUnlockedNpcSlots()}/4 | "
            + $"breach={breach} | entries={_entryObservations} | entryPass={_entryPasses} | entryMismatch={_entryMismatches} | "
            + $"returns={_returnObservations} | returnPass={_returnPasses} | returnMismatch={_returnMismatches} | "
            + $"farmhandObservations={_farmhandObservations} | mapPass={_mapPasses} | mapFail={_mapFailures} | errors={_errors} | last={_last}";
    }

    private void ObserveMap(string source)
    {
        MapSnapshot snapshot = ProbeMap();
        if (snapshot.Valid)
        {
            _mapPasses++;
            _last = $"map-pass:{source}:{snapshot.Width}x{snapshot.Height}:{snapshot.Layers}";
        }
        else
        {
            _mapFailures++;
            _last = $"map-fail:{source}:{snapshot.Detail}";
            _monitor.Log($"[LowerWorkingsGateV2] map contract failed ({source}): {snapshot.Detail}", LogLevel.Warn);
        }
    }

    private MapSnapshot ProbeMap()
    {
        GameLocation? lower = Game1.getLocationFromName(_locationName);
        if (lower is null)
            return new(false, false, "none", 0, 0, "none", "location-missing");

        string runtimeType = lower.GetType().FullName ?? lower.GetType().Name;
        if (lower.GetType() != typeof(GameLocation))
            return new(false, true, runtimeType, 0, 0, "none", "runtime-type-not-vanilla-gamelocation");

        if (lower.Map is null)
            return new(false, true, runtimeType, 0, 0, "none", "map-null");

        var back = lower.Map.GetLayer("Back");
        var buildings = lower.Map.GetLayer("Buildings");
        var front = lower.Map.GetLayer("Front");

        string layers = $"Back={(back is not null)},Buildings={(buildings is not null)},Front={(front is not null)}";
        if (back is null || buildings is null || front is null)
            return new(false, true, runtimeType, 0, 0, layers, "required-layer-missing");

        int width = back.LayerWidth;
        int height = back.LayerHeight;
        bool dimensionsMatch = width == ExpectedMapWidth
            && height == ExpectedMapHeight
            && buildings.LayerWidth == ExpectedMapWidth
            && buildings.LayerHeight == ExpectedMapHeight
            && front.LayerWidth == ExpectedMapWidth
            && front.LayerHeight == ExpectedMapHeight;
        if (!dimensionsMatch)
            return new(false, true, runtimeType, width, height, layers, "map-dimensions-mismatch");

        if (!PointInBounds(LowerWorkingsInteriorSurveyStoryService.ArrivalTile, width, height)
            || !PointInBounds(LowerWorkingsInteriorSurveyStoryService.CribbingClueTile, width, height)
            || !PointInBounds(LowerWorkingsInteriorSurveyStoryService.MutationTraceClueTile, width, height)
            || !PointInBounds(LowerWorkingsInteriorSurveyStoryService.SealedDepthClueTile, width, height))
        {
            return new(false, true, runtimeType, width, height, layers, "story-point-out-of-bounds");
        }

        return new(true, true, runtimeType, width, height, "Back,Buildings,Front", "ok");
    }

    private string ResolveState(bool mapValid)
    {
        if (!mapValid)
            return "INVALID";
        if (!_isFirstDescentComplete() || !_isEntryProtocolReady() || !_isSurgeHigh() || _getUnlockedNpcSlots() < 4)
            return "NOT_READY";
        if (_getSurveyStage() >= LowerWorkingsInteriorSurveyStoryService.CompleteStage)
            return "COMPLETE";
        if (IsLower(Game1.currentLocation) && _getSurveyStage() >= 5)
            return "SAFE_RETURN_READY";
        if (IsLower(Game1.currentLocation))
            return "INSIDE";
        if (_getSurveyStage() <= 1)
            return "READY_TO_ENTER";
        return "RETURNED_PENDING_REPORT";
    }

    private bool IsLower(GameLocation location)
        => location.NameOrUniqueName.Equals(_locationName, StringComparison.OrdinalIgnoreCase);

    private static bool TryReadPersistedBreach(out string? locationName, out Point tile)
    {
        locationName = null;
        tile = Point.Zero;

        Farmer owner = Game1.MasterPlayer;
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

    private static bool PointInBounds(Point point, int width, int height)
        => point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height;

    private static string Serialize(Point point)
        => $"{point.X},{point.Y}";

    private readonly record struct MapSnapshot(
        bool Valid,
        bool LocationLoaded,
        string RuntimeType,
        int Width,
        int Height,
        string Layers,
        string Detail);
}
