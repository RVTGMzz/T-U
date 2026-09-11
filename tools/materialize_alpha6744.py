from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.43"
NEW = "0.2.0-alpha.6.7.44"
NPCS = ["Abigail", "Alex", "Clint", "Demetrius", "Evelyn", "George", "Gus", "Lewis", "Linus", "Marlon", "Maru", "Pierre", "Robin", "Wizard"]
WINDOWS = {
    36: "surveyAuthorized",
    37: "lowerWorkingsEntered",
    38: "cribbingSurveyed",
    39: "mutationTraceSurveyed",
    40: "sealedDepthSurveyed",
    41: "interiorSurveyReported",
}


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


def write(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

# 6.7.42 already knows the exact player tile when the first real threshold crossing succeeds.
# Persist that tile now so new saves inherit a real ingress anchor; legacy 6.7.43 saves calibrate it once on first re-entry.
descent_path = SRC / "Story" / "LowerWorkingsDescentStoryService.cs"
descent = descent_path.read_text(encoding="utf-8")
old_cross = '            Game1.MasterPlayer.modData[ThresholdCrossedFlagKey] = "1";\n            Show("story.descent.crossed");'
new_cross = '            Game1.MasterPlayer.modData[ThresholdCrossedFlagKey] = "1";\n            Game1.MasterPlayer.modData[LowerWorkingsInteriorSurveyStoryService.BreachTileKey] = $"{Game1.player.TilePoint.X},{Game1.player.TilePoint.Y}";\n            Show("story.descent.crossed");'
if old_cross not in descent:
    raise RuntimeError("6.7.42 threshold-cross block missing")
descent = descent.replace(old_cross, new_cross, 1)
old_reset = '        owner.modData.Remove(FirstDescentCompleteFlagKey);\n        _stage = 0;'
new_reset = '        owner.modData.Remove(FirstDescentCompleteFlagKey);\n        owner.modData.Remove(LowerWorkingsInteriorSurveyStoryService.BreachTileKey);\n        _stage = 0;'
if old_reset not in descent:
    raise RuntimeError("6.7.42 reset block missing")
descent_path.write_text(descent.replace(old_reset, new_reset, 1), encoding="utf-8", newline="\n")

service = r'''using Microsoft.Xna.Framework;
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
        return parts.Length == 2
            && int.TryParse(parts[0], out point.X)
            && int.TryParse(parts[1], out point.Y);
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
'''
write(SRC / "Story" / "LowerWorkingsInteriorSurveyStoryService.cs", service)

mod_entry = r'''using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Locations;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const string LowerWorkingsLocationNameAlpha6744 = "Ronvotri.TeamUp_LowerWorkings";
    private LowerWorkingsInteriorSurveyStoryService LowerWorkingsInteriorSurveyAlpha6744 { get; set; } = null!;

    private void RegisterAlpha6744Events()
    {
        LowerWorkingsInteriorSurveyAlpha6744 = new LowerWorkingsInteriorSurveyStoryService(
            Helper,
            Monitor,
            LowerWorkingsLocationNameAlpha6744,
            () => LowerWorkingsDescentAlpha6742.FirstDescentComplete,
            () => EntryProtocolAlpha6740.ProtocolReady,
            () => SurgeHighAlpha6738.IsHigh,
            () => RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732,
            () => EntryProtocolAlpha6740.GetRequiredFieldPeople(),
            () => EntryProtocolAlpha6740.GetRequiredNpcAllies());

        Helper.Events.Content.AssetRequested += OnAlpha6744AssetRequested;
        Helper.Events.GameLoop.SaveLoaded += OnAlpha6744SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6744Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6744UpdateTicked;
        Helper.Events.Input.ButtonPressed += OnAlpha6744ButtonPressed;
        Helper.ConsoleCommands.Add(
            "teamup_lower_interior",
            "Lower Workings dedicated interior survey: status | reset | stage <0-6>.",
            OnAlpha6744Command);
    }

    private void OnAlpha6744AssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Locations"))
            return;

        e.Edit(asset =>
        {
            IDictionary<string, LocationData> data = asset.AsDictionary<string, LocationData>().Data;
            data[LowerWorkingsLocationNameAlpha6744] = new LocationData
            {
                DisplayName = "Lower Workings",
                DefaultArrivalTile = LowerWorkingsInteriorSurveyStoryService.ArrivalTile,
                CreateOnLoad = new CreateLocationData
                {
                    MapPath = Helper.ModContent.GetInternalAssetName("assets/LowerWorkings.tmx").Name,
                    Type = "StardewValley.GameLocation",
                    AlwaysActive = false
                },
                CanPlantHere = false,
                ExcludeFromNpcPathfinding = true,
                MinDailyWeeds = 0,
                MaxDailyWeeds = 0,
                MinDailyForageSpawn = 0,
                MaxDailyForageSpawn = 0,
                MaxSpawnedForageAtOnce = 0
            };
        });
    }

    private void OnAlpha6744SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsWorldReady)
            LowerWorkingsInteriorSurveyAlpha6744.OnSaveLoaded();
    }

    private void OnAlpha6744Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsWorldReady && e.IsLocalPlayer)
            LowerWorkingsInteriorSurveyAlpha6744.OnLocalWarped(e.NewLocation);
    }

    private void OnAlpha6744UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsWorldReady && Context.IsMainPlayer)
            LowerWorkingsInteriorSurveyAlpha6744.Update();
    }

    private void OnAlpha6744ButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree || !e.Button.IsActionButton())
            return;

        if (LowerWorkingsInteriorSurveyAlpha6744.TryHandleLocalAction())
            Helper.Input.Suppress(e.Button);
    }

    private void OnAlpha6744Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_lower_interior.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            LowerWorkingsInteriorSurveyAlpha6744.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 6)
            LowerWorkingsInteriorSurveyAlpha6744.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_lower_interior <status|reset|stage 0-6>", LogLevel.Info);
            return;
        }

        WriteAlpha6744Diagnostic();
    }

    private void WriteAlpha6744Diagnostic()
    {
        GameLocation? lower = Game1.getLocationFromName(LowerWorkingsLocationNameAlpha6744);
        List<string> lines = new()
        {
            "TEAM UP 6.7.44 - DEDICATED LOWER WORKINGS MAP / INTERIOR SURVEY",
            Origin.Describe(),
            LowerWorkingsDescentAlpha6742.Describe(Game1.currentLocation),
            LowerWorkingsInteriorSurveyAlpha6744.Describe(Game1.currentLocation),
            $"Dedicated location: name={LowerWorkingsLocationNameAlpha6744} loaded={lower is not null} map={(lower?.mapPath?.Value ?? "none")}",
            $"Formation requirement: people={EntryProtocolAlpha6740.GetRequiredFieldPeople()} | activeNpcAllies={EntryProtocolAlpha6740.GetRequiredNpcAllies()} | hard people cap remains 5.",
            "Route: Guild survey order -> persisted breach MineShaft + tile anchor -> dedicated Lower Workings map -> three 120-tick clue surveys -> safe return at entry -> Guild report.",
            "Clues: directed emergency cribbing -> fresh Mutation-linked residue over older blast scoring -> deeper sealed pressure edge. Historical worker identity remains unknown.",
            "No story monster, boss, George reveal, Evelyn postgame reveal, roster expansion, Mutation trigger rewrite, or Pelipper capture rewrite is introduced by 6.7.44."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Lower_Workings_Interior_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Lower Workings interior diagnostic saved: {path}", LogLevel.Info);
    }
}
'''
write(SRC / "ModEntry.Alpha6744.cs", mod_entry)

resolver = r'''using Ronvotri.TeamUp.Story;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private int GetStoryReactionWindowAlpha6744()
    {
        if (LowerWorkingsDescentAlpha6742.Stage < LowerWorkingsDescentStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6743();

        return LowerWorkingsInteriorSurveyAlpha6744.Stage switch
        {
            <= 0 => 35,
            1 => 36,
            2 => 37,
            3 => 38,
            4 => 39,
            5 => 40,
            _ => 41
        };
    }
}
'''
# resolver is part of the same Alpha file; append it by inserting the method before the final class brace.
alpha6744_path = SRC / "ModEntry.Alpha6744.cs"
alpha6744 = alpha6744_path.read_text(encoding="utf-8")
method_body = resolver.split("public sealed partial class ModEntry\n{\n", 1)[1].rsplit("\n}\n", 1)[0]
alpha6744 = alpha6744.rsplit("\n}\n", 1)[0] + "\n\n" + method_body + "\n}\n"
alpha6744_path.write_text(alpha6744, encoding="utf-8", newline="\n")

# Register the checkpoint and advance the startup marker.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
register_old = "        RegisterAlpha6742Events();\n        RegisterAlpha6720Events();"
register_new = "        RegisterAlpha6742Events();\n        RegisterAlpha6744Events();\n        RegisterAlpha6720Events();"
if register_old not in entry:
    raise RuntimeError("6.7.42 registration marker missing")
entry = entry.replace(register_old, register_new, 1)
old_start = "Lower Workings descent reaction layer active."
new_start = "Dedicated Lower Workings map / interior survey layer active."
if old_start not in entry:
    raise RuntimeError("6.7.43 startup marker missing")
entry_path.write_text(entry.replace(old_start, new_start, 1), encoding="utf-8", newline="\n")

# Reaction catalog: add six bundled interior-survey windows in this gameplay checkpoint.
reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
if "narrativeStage > 35" not in reactions:
    raise RuntimeError("6.7.43 reaction range guard missing")
reactions = reactions.replace("narrativeStage > 35", "narrativeStage > 41", 1)
marker = "        return new Dictionary<int, IReadOnlyDictionary<string, string>>\n        {"
if marker not in reactions:
    raise RuntimeError("reaction dictionary marker missing")
blocks: list[str] = []
for window, var_name in WINDOWS.items():
    rows = [f"        Dictionary<string, string> {var_name} = Stage("]
    for index, npc in enumerate(NPCS):
        comma = "," if index < len(NPCS) - 1 else ");"
        rows.append(f'            "{npc}", "story.react.{window}.{npc.lower()}"{comma}')
    blocks.append("\n".join(rows))
reactions = reactions.replace(marker, "\n\n".join(blocks) + "\n\n" + marker, 1)
old_tail = "            [35] = firstDescentReported\n        };"
new_tail = "            [35] = firstDescentReported,\n            [36] = surveyAuthorized,\n            [37] = lowerWorkingsEntered,\n            [38] = cribbingSurveyed,\n            [39] = mutationTraceSurveyed,\n            [40] = sealedDepthSurveyed,\n            [41] = interiorSurveyReported\n        };"
if old_tail not in reactions:
    raise RuntimeError("6.7.43 reaction dictionary tail missing")
reaction_path.write_text(reactions.replace(old_tail, new_tail, 1), encoding="utf-8", newline="\n")

# Route interaction and diagnostics through the new resolver.
a6728_path = SRC / "ModEntry.Alpha6728.cs"
a6728 = a6728_path.read_text(encoding="utf-8")
count = a6728.count("GetStoryReactionWindowAlpha6743()")
if count < 2:
    raise RuntimeError(f"Expected at least 2 Alpha6743 resolver calls, found {count}")
a6728 = a6728.replace("GetStoryReactionWindowAlpha6743()", "GetStoryReactionWindowAlpha6744()")
a6728 = a6728.replace(
    '"TEAM UP 6.7.43 - LOWER WORKINGS DESCENT REACTIONS",',
    '"TEAM UP 6.7.44 - LOWER WORKINGS INTERIOR SURVEY + REACTIONS",',
    1,
)
old_diag = '"Reaction windows: 0..30 previous story | 31=descent authorized | 32=threshold line ready | 33=threshold crossed | 34=first interior inspected | 35=first descent reported."'
new_diag = '"Reaction windows: 0..35 previous story | 36=interior survey authorized | 37=dedicated map entered | 38=directed cribbing surveyed | 39=Mutation trace surveyed | 40=sealed-depth edge surveyed | 41=interior survey reported."'
if old_diag not in a6728:
    raise RuntimeError("6.7.43 reaction diagnostic summary missing")
a6728_path.write_text(a6728.replace(old_diag, new_diag, 1), encoding="utf-8", newline="\n")

# Real TMX location. Only vanilla Mines/mine.png is referenced, so the package needs no copied game art.
width, height = 32, 24
back = [[151 for _ in range(width)] for _ in range(height)]
for y in range(2, height - 2):
    for x in range(2, width - 2):
        if (x * 3 + y * 5) % 17 == 0:
            back[y][x] = 166
        elif (x * 7 + y * 2) % 29 == 0:
            back[y][x] = 184

buildings = [[0 for _ in range(width)] for _ in range(height)]
for x in range(width):
    buildings[0][x] = 2
    buildings[height - 1][x] = 2
for y in range(height):
    buildings[y][0] = 2
    buildings[y][width - 1] = 2
for x in range(3, 14):
    if x not in (7, 8, 9):
        buildings[5][x] = 2
for y in range(2, 12):
    if y not in (7, 8, 9):
        buildings[y][16] = 2
for x in range(15, 30):
    if x not in (21, 22, 23, 24):
        buildings[13][x] = 2
for y in range(14, 22):
    if y not in (17, 18, 19):
        buildings[y][10] = 2

front = [[0 for _ in range(width)] for _ in range(height)]
for x, y, gid in [
    (7, 9, 234), (8, 9, 235), (9, 9, 236),
    (21, 8, 200), (22, 8, 201), (23, 8, 202),
    (22, 16, 218), (23, 16, 219), (24, 16, 220),
    (14, 21, 188), (16, 21, 188),
]:
    front[y][x] = gid


def csv_layer(rows: list[list[int]]) -> str:
    return "\n".join(",".join(str(value) for value in row) for row in rows)

map_tmx = f'''<?xml version="1.0" encoding="UTF-8"?>
<map version="1.10" tiledversion="1.10.2" orientation="orthogonal" renderorder="right-down" width="{width}" height="{height}" tilewidth="16" tileheight="16" infinite="0" nextlayerid="4" nextobjectid="1">
 <properties>
  <property name="AmbientLight" value="45 50 60"/>
 </properties>
 <tileset firstgid="1" name="cave" tilewidth="16" tileheight="16" tilecount="288" columns="16">
  <image source="Mines/mine.png" width="256" height="288"/>
  <tile id="0"><properties><property name="Type" value="Dirt"/></properties></tile>
  <tile id="1"><properties><property name="Type" value="Stone"/></properties></tile>
  <tile id="150"><properties><property name="Type" value="Dirt"/></properties></tile>
  <tile id="165"><properties><property name="Type" value="Dirt"/></properties></tile>
  <tile id="183"><properties><property name="Type" value="Dirt"/></properties></tile>
 </tileset>
 <layer id="1" name="Back" width="{width}" height="{height}">
  <data encoding="csv">\n{csv_layer(back)}\n  </data>
 </layer>
 <layer id="2" name="Buildings" width="{width}" height="{height}">
  <data encoding="csv">\n{csv_layer(buildings)}\n  </data>
 </layer>
 <layer id="3" name="Front" width="{width}" height="{height}">
  <data encoding="csv">\n{csv_layer(front)}\n  </data>
 </layer>
</map>
'''
write(SRC / "assets" / "LowerWorkings.tmx", map_tmx)

# Story copy and reactions.
EN_STORY = {
    "story.interior.need-first-descent": "The dedicated Lower Workings survey is not authorized yet. Complete the first descent, keep Entry Protocol READY, and maintain SURGE HIGH readiness.",
    "story.interior.missing-face": "The recorded breach face is missing from the operation log. Re-establish the controlled-breach state before continuing.",
    "story.interior.briefing": "Marlon spreads the first-descent notes across the table. The threshold is no longer the objective. A dedicated Lower Workings chamber is now mapped as its own operation: enter through the recorded breach, keep the full formation intact, survey three evidence zones, and leave by the same secured route. No pursuit. No chamber engagement.",
    "story.interior.objective.reach": "Objective: return the full operational formation to the recorded breach face.",
    "story.interior.objective.enter": "The old breach is here. Use the action button at the recorded anchor to enter the Lower Workings.",
    "story.interior.anchor-calibrated": "The old chalk and brace marks are matched. This tile is now the persistent Lower Workings ingress anchor.",
    "story.interior.need-full-team": "The dedicated survey requires the full operational formation here before crossing.",
    "story.interior.wait-host": "The host must open the dedicated Lower Workings route first.",
    "story.interior.crossing": "The formation passes the stabilized breach one by one. Beyond the old threshold, the passage finally opens into a chamber that can be surveyed as its own place.",
    "story.interior.map-missing": "The Lower Workings location did not load. Do not continue this save until the map registration is checked.",
    "story.interior.entered": "The Lower Workings settle into view: old cribbing, blast-scarred stone, and a deeper pressure line that the first threshold probe could not reach. The return opening remains visible behind the team.",
    "story.interior.objective.join": "Join the host at the Lower Workings entry and keep the formation together.",
    "story.interior.objective.cribbing": "Survey zone 1: hold the full formation near the directed cribbing marks.",
    "story.interior.clue.cribbing": "The cribbing is not merely old support work. Its braces were angled to steer a failure inward, creating a sacrificial choke point while preserving a narrow withdrawal lane. This strengthens the deliberate-containment conclusion without identifying who built it.",
    "story.interior.objective.trace": "Survey zone 2: move to the blast-scored wall and hold formation on the residue trace.",
    "story.interior.clue.trace": "Across the older blast scoring lies a much newer mineral-organic film with the same unstable signature seen around current Mutations. It did not cause the historical blast pattern; it accumulated over it later. The modern Surge is touching an older sealed system.",
    "story.interior.objective.seal": "Survey zone 3: approach the deeper sealed-pressure edge, but do not cross it.",
    "story.interior.clue.seal": "At the deeper edge, the Mutation-linked trace becomes denser and repeats along hairline fractures that breathe with the pressure cycle. Something beyond the old closure is still influencing the present mine, but the survey cannot yet identify the source. The team marks the boundary and stops.",
    "story.interior.objective.return": "Survey complete. Return to the entry tile and use the action button for the secured withdrawal route.",
    "story.interior.withdraw-early": "Emergency withdrawal accepted. The survey stage is preserved; regroup and re-enter through the recorded breach when ready.",
    "story.interior.return-failed": "The recorded return anchor is unavailable. Check the Lower Workings diagnostic before continuing.",
    "story.interior.report": "Back at the Guild, the three observations align: deliberate emergency cribbing, old blast work beneath newer Mutation-linked residue, and a deeper sealed pressure edge still affecting the mine. Marlon records the chamber as an active containment problem. The next operation will prepare for escalation, not rush the seal.",
    "story.interior.complete": "Lower Workings interior survey complete. Dedicated map and safe return route verified.",
}

VI_STORY = {
    "story.interior.need-first-descent": "Chưa thể mở đợt khảo sát chuyên sâu Hạ Tầng Hầm Mỏ. Hãy hoàn tất lần xuống hầm đầu tiên, giữ Entry Protocol ở trạng thái READY và duy trì mức cảnh giới SURGE HIGH.",
    "story.interior.missing-face": "Nhật ký chiến dịch không còn vị trí khe mở đã ghi nhận. Cần khôi phục trạng thái Controlled Breach trước khi tiếp tục.",
    "story.interior.briefing": "Marlon trải ghi chép của lần xuống hầm đầu tiên lên bàn. Từ giờ ngưỡng cửa không còn là mục tiêu nữa. Hạ Tầng Hầm Mỏ sẽ được khảo sát như một khu vực riêng: vào từ khe mở đã ghi nhận, giữ nguyên đội hình đầy đủ, kiểm tra ba vùng dấu vết rồi rút ra bằng đúng tuyến đã bảo đảm. Không truy đuổi. Không giao chiến trong buồng sâu.",
    "story.interior.objective.reach": "Mục tiêu: đưa đầy đủ đội hình tác chiến trở lại khe mở đã ghi nhận.",
    "story.interior.objective.enter": "Khe mở cũ ở đây. Dùng nút tương tác tại điểm neo đã ghi nhận để vào Hạ Tầng Hầm Mỏ.",
    "story.interior.anchor-calibrated": "Các vạch phấn và thanh chống cũ đã khớp. Ô này được lưu làm điểm vào Hạ Tầng Hầm Mỏ.",
    "story.interior.need-full-team": "Đợt khảo sát chuyên sâu cần đầy đủ đội hình tác chiến có mặt tại đây trước khi băng qua.",
    "story.interior.wait-host": "Host phải mở tuyến Hạ Tầng Hầm Mỏ trước.",
    "story.interior.crossing": "Cả đội lần lượt đi qua khe mở đã gia cố. Phía sau ngưỡng cũ, lối hầm cuối cùng cũng mở thành một buồng mỏ đủ rõ để khảo sát như một khu vực thật sự riêng biệt.",
    "story.interior.map-missing": "Khu vực Hạ Tầng Hầm Mỏ không được tải. Đừng tiếp tục save này cho đến khi kiểm tra xong phần đăng ký map.",
    "story.interior.entered": "Hạ Tầng Hầm Mỏ hiện ra rõ hơn: giàn chống cũ, đá mang vết nổ và một đường áp lực sâu hơn mà lần thăm dò ngưỡng trước đó chưa thể chạm tới. Lối rút lui vẫn nằm ngay phía sau đội.",
    "story.interior.objective.join": "Hãy hội quân với host tại cửa vào Hạ Tầng Hầm Mỏ và giữ đội hình liền mạch.",
    "story.interior.objective.cribbing": "Vùng khảo sát 1: giữ đầy đủ đội hình gần các dấu giàn chống có chủ đích.",
    "story.interior.clue.cribbing": "Giàn chống này không chỉ là kết cấu cũ. Các thanh được đặt lệch để hướng một vụ sập vào trong, tạo điểm nghẽn hy sinh nhưng vẫn chừa một lối rút hẹp. Dấu vết củng cố kết luận rằng nơi này từng bị phong tỏa có chủ ý, nhưng chưa cho biết ai đã làm việc đó.",
    "story.interior.objective.trace": "Vùng khảo sát 2: tới vách đá có vết nổ và giữ đội hình tại lớp cặn bất thường.",
    "story.interior.clue.trace": "Phủ lên các vết nổ cũ là một lớp khoáng-hữu cơ mới hơn rất nhiều, mang cùng dấu hiệu bất ổn từng thấy quanh các Mutation hiện tại. Nó không tạo ra dấu nổ lịch sử mà chỉ bồi lên sau này. SURGE hiện tại đang chạm vào một hệ thống phong kín cũ hơn.",
    "story.interior.objective.seal": "Vùng khảo sát 3: tiến tới mép áp lực bị phong kín ở sâu hơn, nhưng không vượt qua.",
    "story.interior.clue.seal": "Ở mép sâu, dấu vết liên quan Mutation dày hơn và lặp lại dọc những khe nứt nhỏ đang thay đổi theo chu kỳ áp lực. Thứ gì đó phía sau lớp phong kín cũ vẫn đang tác động tới khu mỏ hiện tại, nhưng đợt khảo sát này chưa thể xác định nguồn. Cả đội đánh dấu ranh giới rồi dừng lại.",
    "story.interior.objective.return": "Khảo sát hoàn tất. Quay lại ô cửa vào và dùng nút tương tác để rút theo tuyến an toàn.",
    "story.interior.withdraw-early": "Cho phép rút lui khẩn cấp. Tiến độ khảo sát được giữ nguyên; hãy tập hợp lại và vào lại từ khe mở khi sẵn sàng.",
    "story.interior.return-failed": "Không tìm được điểm neo rút lui đã ghi nhận. Hãy kiểm tra diagnostic của Hạ Tầng Hầm Mỏ trước khi tiếp tục.",
    "story.interior.report": "Trở lại Hội, ba nhóm dấu vết khớp với nhau: giàn chống khẩn cấp có chủ đích, dấu nổ cũ nằm dưới lớp cặn liên quan Mutation mới hơn, và một mép áp lực sâu hơn vẫn đang tác động tới khu mỏ. Marlon ghi nhận buồng sâu là một vấn đề phong tỏa còn hoạt động. Chiến dịch kế tiếp sẽ chuẩn bị cho tình huống leo thang, không vội phá lớp phong kín.",
    "story.interior.complete": "Hoàn tất khảo sát Hạ Tầng Hầm Mỏ. Map riêng và tuyến rút lui an toàn đã được xác nhận.",
}

EN_BASE = {
    36: "The Guild has authorized a dedicated Lower Workings survey. The team is going back through the recorded breach, but this time the chamber itself is the objective.",
    37: "The Lower Workings are finally a real place the team can stand inside, not just a line on the far side of a breach. The secured opening behind them matters as much as the chamber ahead.",
    38: "The first survey zone confirms that the old cribbing was arranged to steer a collapse and preserve a withdrawal lane. That is deliberate containment work, even though the worker remains unidentified.",
    39: "Newer Mutation-linked residue lies over much older blast scoring. The present Surge is interacting with the sealed workings, but the evidence still does not say what is behind the closure.",
    40: "The deeper pressure edge carries a denser version of the same unstable trace. The team marked the boundary and stopped instead of forcing the seal, which leaves the next operation with a clean problem to solve.",
    41: "The dedicated interior survey is reported complete: real chamber access, three evidence zones, and a verified return route. The Lower Workings are now an active containment problem rather than an old mining mystery.",
}
VI_BASE = {
    36: "Hội đã cho phép một đợt khảo sát Hạ Tầng Hầm Mỏ chuyên biệt. Cả đội sẽ quay lại qua khe mở đã ghi nhận, nhưng lần này chính buồng mỏ bên trong mới là mục tiêu.",
    37: "Hạ Tầng Hầm Mỏ cuối cùng đã là một nơi thật sự mà cả đội có thể đứng bên trong, không còn chỉ là một ranh giới phía sau khe mở. Lối rút an toàn phía sau quan trọng chẳng kém buồng mỏ phía trước.",
    38: "Vùng khảo sát đầu tiên xác nhận giàn chống cũ được bố trí để hướng vụ sập và chừa lại một lối rút. Đó là công việc phong tỏa có chủ đích, dù danh tính người thợ vẫn chưa được biết.",
    39: "Lớp cặn liên quan Mutation mới hơn nằm phủ lên những dấu nổ cũ rất nhiều. SURGE hiện tại đang tương tác với khu hầm bị phong kín, nhưng bằng chứng vẫn chưa cho biết thứ gì ở phía sau.",
    40: "Mép áp lực sâu hơn mang phiên bản đậm đặc hơn của cùng dấu vết bất ổn. Cả đội đã đánh dấu ranh giới rồi dừng lại thay vì cưỡng ép lớp phong kín, nhờ vậy chiến dịch kế tiếp có một vấn đề rõ ràng để xử lý.",
    41: "Đợt khảo sát nội thất chuyên biệt đã được báo cáo hoàn tất: có lối vào buồng mỏ thật, ba vùng bằng chứng và một tuyến rút lui đã kiểm chứng. Hạ Tầng Hầm Mỏ giờ là một vấn đề phong tỏa còn hoạt động chứ không chỉ là bí ẩn mỏ cũ.",
}
EN_VOICE = {
    "Abigail": "Keep the retreat route boring and the discoveries interesting.",
    "Alex": "A clean formation still matters more than how deep anyone gets.",
    "Clint": "Old timber and blast marks tell the truth if nobody disturbs them first.",
    "Demetrius": "Separate observation from conclusion; the pattern is strong, but the source is still unknown.",
    "Evelyn": "Please keep bringing everyone home together before asking the mine for another answer.",
    "George": "Old workings punish crews that stop reading the roof, the floor, and the air just because they found something worth staring at.",
    "Gus": "I remain a strong supporter of discoveries that end with everyone walking back through the same door.",
    "Lewis": "Keep the operation inside the authorized survey boundary until the Guild formally expands it.",
    "Linus": "A sealed place can still speak through dust, pressure, and the way stone settles around old choices.",
    "Marlon": "No pursuit and no improvising past the marked edge. Information is the win condition for this operation.",
    "Maru": "Preserve the site and the sequence of observations; later comparisons will matter more than dramatic guesses now.",
    "Pierre": "I am voting for the version of exploration where the exit remains extremely easy to find.",
    "Robin": "Load paths, brace angles, and failure lines can tell us what the structure was meant to do without telling us who built it.",
    "Wizard": "Pressure remembers even when names are lost. Do not confuse a stronger trace with a complete answer.",
}
VI_VOICE = {
    "Abigail": "Cứ để tuyến rút lui thật nhàm chán, còn những thứ phát hiện được thì thú vị là đủ.",
    "Alex": "Giữ đội hình sạch sẽ vẫn quan trọng hơn chuyện bất kỳ ai xuống được sâu tới đâu.",
    "Clint": "Gỗ chống cũ và vết nổ sẽ kể khá thật nếu chưa có ai phá hỏng chúng trước.",
    "Demetrius": "Hãy tách quan sát khỏi kết luận; mẫu dấu vết đã mạnh, nhưng nguồn gốc vẫn chưa xác định.",
    "Evelyn": "Xin hãy tiếp tục đưa mọi người trở về cùng nhau trước khi hỏi khu mỏ thêm một câu nữa.",
    "George": "Hầm mỏ cũ không tha cho đội nào ngừng nhìn trần, nền và luồng khí chỉ vì vừa thấy một thứ đáng chú ý.",
    "Gus": "Tôi vẫn hoàn toàn ủng hộ kiểu khám phá kết thúc bằng việc tất cả cùng đi bộ trở ra qua đúng cái cửa đã đi vào.",
    "Lewis": "Giữ chiến dịch trong ranh giới khảo sát đã được phê duyệt cho đến khi Hội chính thức mở rộng phạm vi.",
    "Linus": "Một nơi bị phong kín vẫn có thể lên tiếng qua bụi, áp lực và cách đá lắng quanh những quyết định cũ.",
    "Marlon": "Không truy đuổi, không tự ý vượt qua mép đã đánh dấu. Thu thập thông tin chính là điều kiện thắng của chiến dịch này.",
    "Maru": "Hãy giữ nguyên hiện trường và thứ tự quan sát; so sánh về sau sẽ có giá trị hơn những phỏng đoán kịch tính lúc này.",
    "Pierre": "Tôi bỏ phiếu cho kiểu thám hiểm mà đường ra lúc nào cũng cực kỳ dễ tìm.",
    "Robin": "Đường chịu lực, góc thanh chống và vệt sập có thể cho biết kết cấu được dùng để làm gì mà không cần cho biết ai đã dựng nó.",
    "Wizard": "Áp lực vẫn ghi nhớ ngay cả khi tên tuổi đã mất. Đừng nhầm một dấu vết mạnh hơn với một câu trả lời hoàn chỉnh.",
}

for locale, story, bases, voices in [
    ("default.json", EN_STORY, EN_BASE, EN_VOICE),
    ("vi.json", VI_STORY, VI_BASE, VI_VOICE),
]:
    path = SRC / "i18n" / locale
    data = json.loads(path.read_text(encoding="utf-8"))
    data.update(story)
    for window in WINDOWS:
        for npc in NPCS:
            data[f"story.react.{window}.{npc.lower()}"] = f"{bases[window]} {voices[npc]}"
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Materialized Team Up Alpha 6.7.44 dedicated Lower Workings map / interior survey source.")
