using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

internal sealed class Alpha674428LowerWorkingsRuntimeGateService
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

    private int _entriesObserved;
    private int _arrivalPasses;
    private int _arrivalMismatches;
    private int _returnsObserved;
    private int _exactReturnPasses;
    private int _returnMismatches;
    private int _farmhandObservations;
    private int _mapProbePasses;
    private int _mapProbeFailures;
    private int _errors;
    private string _last = "not-observed";

    public Alpha674428LowerWorkingsRuntimeGateService(
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
    }

    public void OnSaveLoaded()
    {
        ResetTelemetry();
        ObserveMapContract("save-loaded");
    }

    public void OnLocalWarped(GameLocation oldLocation, GameLocation newLocation)
    {
        try
        {
            bool oldIsLower = IsLowerWorkings(oldLocation);
            bool newIsLower = IsLowerWorkings(newLocation);
            if (oldIsLower == newIsLower)
                return;

            if (!Context.IsMainPlayer)
                _farmhandObservations++;

            if (newIsLower)
            {
                _entriesObserved++;
                Point actual = Game1.player.TilePoint;
                Point expected = LowerWorkingsInteriorSurveyStoryService.ArrivalTile;
                if (actual == expected)
                {
                    _arrivalPasses++;
                    _last = $"entered:{newLocation.NameOrUniqueName}@{Serialize(actual)}";
                }
                else
                {
                    _arrivalMismatches++;
                    _last = $"entry-mismatch:expected={Serialize(expected)} actual={Serialize(actual)}";
                    _monitor.Log($"[LowerWorkingsRuntimeGate] entry arrival mismatch: expected {Serialize(expected)}, got {Serialize(actual)}.", LogLevel.Warn);
                }

                ObserveMapContract("entered");
                return;
            }

            _returnsObserved++;
            if (!TryGetPersistedBreach(out string? breachLocation, out Point breachTile))
            {
                _returnMismatches++;
                _last = $"return-mismatch:no-persisted-breach actual={newLocation.NameOrUniqueName}@{Serialize(Game1.player.TilePoint)}";
                _monitor.Log("[LowerWorkingsRuntimeGate] return observed but persisted breach location/tile is unavailable.", LogLevel.Warn);
                return;
            }

            Point returnTile = Game1.player.TilePoint;
            bool locationMatches = newLocation.NameOrUniqueName.Equals(breachLocation, StringComparison.OrdinalIgnoreCase);
            bool tileMatches = returnTile == breachTile;
            if (locationMatches && tileMatches)
            {
                _exactReturnPasses++;
                _last = $"exact-return:{breachLocation}@{Serialize(breachTile)}";
            }
            else
            {
                _returnMismatches++;
                _last = $"return-mismatch:expected={breachLocation}@{Serialize(breachTile)} actual={newLocation.NameOrUniqueName}@{Serialize(returnTile)}";
                _monitor.Log($"[LowerWorkingsRuntimeGate] exact return mismatch: expected {breachLocation}@{Serialize(breachTile)}, got {newLocation.NameOrUniqueName}@{Serialize(returnTile)}.", LogLevel.Warn);
            }
        }
        catch (Exception ex)
        {
            _errors++;
            _last = $"warp-observation-error:{ex.GetType().Name}";
            _monitor.Log($"[LowerWorkingsRuntimeGate] warp observation failed: {ex}", LogLevel.Error);
        }
    }

    public void ResetTelemetry()
    {
        _entriesObserved = 0;
        _arrivalPasses = 0;
        _arrivalMismatches = 0;
        _returnsObserved = 0;
        _exactReturnPasses = 0;
        _returnMismatches = 0;
        _farmhandObservations = 0;
        _mapProbePasses = 0;
        _mapProbeFailures = 0;
        _errors = 0;
        _last = "reset";
    }

    public string Describe()
    {
        MapContractSnapshot map = ProbeMapContract();
        string state = ResolveState(map.Valid);
        string breach = TryGetPersistedBreach(out string? breachLocation, out Point breachTile)
            ? $"{breachLocation}@{Serialize(breachTile)}"
            : "none";

        return $"Lower Workings runtime gate: state={state} | locationLoaded={map.LocationLoaded} | type={map.RuntimeType} | "
            + $"map={map.Width}x{map.Height} | layers={map.Layers} | arrival={Serialize(LowerWorkingsInteriorSurveyStoryService.ArrivalTile)} | "
            + $"stage={_getSurveyStage()}/{LowerWorkingsInteriorSurveyStoryService.CompleteStage} | firstDescent={_isFirstDescentComplete()} | "
            + $"protocolReady={_isEntryProtocolReady()} | high={_isSurgeHigh()} | slots={_getUnlockedNpcSlots()}/4 | breach={breach} | "
            + $"entriesObserved={_entriesObserved} | arrivalPasses={_arrivalPasses} | arrivalMismatches={_arrivalMismatches} | "
            + $"returnsObserved={_returnsObserved} | exactReturnPasses={_exactReturnPasses} | returnMismatches={_returnMismatches} | "
            + $"farmhandObservations={_farmhandObservations} | mapProbePasses={_mapProbePasses} | mapProbeFailures={_mapProbeFailures} | errors={_errors} | last={_last}";
    }

    private void ObserveMapContract(string source)
    {
        MapContractSnapshot snapshot = ProbeMapContract();
        if (snapshot.Valid)
        {
            _mapProbePasses++;
            _last = $"map-pass:{source}:{snapshot.Width}x{snapshot.Height}:{snapshot.Layers}";
            return;
        }

        _mapProbeFailures++;
        _last = $"map-fail:{source}:{snapshot.Detail}";
        _monitor.Log($"[LowerWorkingsRuntimeGate] map contract failed ({source}): {snapshot.Detail}", LogLevel.Warn);
    }

    private MapContractSnapshot ProbeMapContract()
    {
        GameLocation? lower = Game1.getLocationFromName(_locationName);
        if (lower is null)
            return new(false, false, "none", 0, 0, "none", "location-missing");

        string runtimeType = lower.GetType().FullName ?? lower.GetType().Name;
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
        if (IsLowerWorkings(Game1.currentLocation) && _getSurveyStage() >= 5)
            return "SAFE_RETURN_READY";
        if (IsLowerWorkings(Game1.currentLocation))
            return "INSIDE";
        if (_getSurveyStage() <= 1)
            return "READY_TO_ENTER";
        return "RETURNED_PENDING_REPORT";
    }

    private static bool PointInBounds(Point point, int width, int height)
        => point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height;

    private bool IsLowerWorkings(GameLocation location)
        => location.NameOrUniqueName.Equals(_locationName, StringComparison.OrdinalIgnoreCase);

    private static bool TryGetPersistedBreach(out string? locationName, out Point tile)
    {
        Farmer owner = Game1.MasterPlayer;
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

    private static string Serialize(Point point) => $"{point.X},{point.Y}";

    private readonly record struct MapContractSnapshot(
        bool Valid,
        bool LocationLoaded,
        string RuntimeType,
        int Width,
        int Height,
        string Layers,
        string Detail);
}
