using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.32 physical field-triangulation chapter.
///
/// The route begins only after the Old Mine Connection is confirmed. It requires a real field team:
/// at least three people physically present in the same location, with at least one active Team Up
/// NPC ally. Two different MineShaft locations must be sampled before Marlon can triangulate the
/// likely sealed-workings corridor. This chapter does not unlock story slot 4 and does not reveal
/// the historical miner's identity.
/// </summary>
internal sealed class FieldTriangulationStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/FieldTriangulationStage";
    public const string FirstBearingLocationKey = "Ronvotri.TeamUp/Story/FieldTriangulationFirstBearing";
    public const int CompleteStage = 4;
    public const int MinimumFieldPeople = 3;
    public const int MinimumActiveNpcAllies = 1;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getOldMineStage;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private int _stage;

    public FieldTriangulationStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getOldMineStage,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt)
    {
        _helper = helper;
        _monitor = monitor;
        _getOldMineStage = getOldMineStage;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded()
        => _stage = ReadStage(Game1.MasterPlayer);

    public void OnWarped(GameLocation location)
    {
        if (!CanAdvance())
            return;

        if (!HasRequiredFieldTeam(location))
        {
            if (IsRelevantLocationForCurrentStage(location))
                Hud("story.triangulation.need-team");
            return;
        }

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            Show("story.triangulation.briefing");
            SetStage(Game1.MasterPlayer, 1, "marlon-field-plan");
            Hud("story.triangulation.objective.first-bearing");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            Game1.MasterPlayer.modData[FirstBearingLocationKey] = location.NameOrUniqueName;
            Show("story.triangulation.bearing-one");
            SetStage(Game1.MasterPlayer, 2, $"first-bearing:{location.NameOrUniqueName}");
            Hud("story.triangulation.objective.second-bearing");
            return;
        }

        if (_stage == 2 && location is MineShaft)
        {
            if (!IsDifferentMineLocation(Game1.MasterPlayer, location))
            {
                Hud("story.triangulation.same-shaft");
                return;
            }

            Show("story.triangulation.bearing-two");
            SetStage(Game1.MasterPlayer, 3, $"second-bearing:{location.NameOrUniqueName}");
            Hud("story.triangulation.objective.return");
            return;
        }

        if (_stage == 3 && Is(location, "AdventureGuild"))
        {
            Show("story.triangulation.confirmed");
            SetStage(Game1.MasterPlayer, CompleteStage, "sealed-workings-triangulated");
            Hud("story.triangulation.complete");
        }
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(FirstBearingLocationKey);
        _stage = 0;
        _monitor.Log("[FieldTriangulation] stage reset; old-mine and roster progress were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        if (clamped <= 1)
            owner.modData.Remove(FirstBearingLocationKey);
        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string firstBearing = Game1.MasterPlayer.modData.TryGetValue(FirstBearingLocationKey, out string? stored)
            ? stored
            : "none";
        string objective = _stage switch
        {
            0 when _getOldMineStage() < OldMineConnectionStoryService.CompleteStage => "finish-old-mine-connection",
            0 => "assemble-field-team-at-guild",
            1 => "sample-first-mineshaft-bearing",
            2 => "sample-different-mineshaft-bearing",
            3 => "return-to-marlon-with-field-team",
            _ => "sealed-workings-corridor-triangulated"
        };

        return $"Field Triangulation: oldMineStage={_getOldMineStage()}/3 | stage={_stage}/{CompleteStage} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)} | npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)} | "
            + $"firstBearing={firstBearing} | objective={objective}";
    }

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getOldMineStage() >= OldMineConnectionStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasRequiredFieldTeam(GameLocation location)
        => _getFieldPeopleAt(location) >= MinimumFieldPeople
            && _getActiveNpcAlliesAt(location) >= MinimumActiveNpcAllies;

    private bool IsRelevantLocationForCurrentStage(GameLocation location)
        => (_stage is 0 or 3 && Is(location, "AdventureGuild"))
            || (_stage is 1 or 2 && location is MineShaft);

    private static bool IsDifferentMineLocation(Farmer owner, GameLocation location)
        => !owner.modData.TryGetValue(FirstBearingLocationKey, out string? first)
            || string.IsNullOrWhiteSpace(first)
            || !first.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[FieldTriangulation] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
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
