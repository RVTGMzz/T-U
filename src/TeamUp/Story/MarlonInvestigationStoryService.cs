using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Story;

internal enum MarlonInvestigationTransition
{
    None,
    BriefingStarted,
    MineTrailFound,
    MutantEvidenceSecured,
    DebriefCompleted
}

/// <summary>
/// Alpha 6.7.29 first post-Marlon investigation chapter.
///
/// The chapter requires a real active Team Up NPC ally to accompany the host. Marlon briefs the
/// party at the Guild, the party traces the anomaly inside a MineShaft, a naturally occurring
/// Team Up Mutant must then be defeated in the mine, and returning to Marlon with the ally completes
/// the case. The payoff unlocks story NPC slot 2. No synthetic quest monster is spawned here.
/// </summary>
internal sealed class MarlonInvestigationStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/MarlonInvestigationStage";
    public const string EvidenceMonsterMarker = "Ronvotri.TeamUp/Story/MarlonEvidenceCounted";
    public const int CompleteStage = 4;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getOriginStage;
    private readonly Func<GameLocation, bool> _hasActiveAllyAt;
    private readonly Action _onDebriefComplete;
    private int _stage;

    public static MarlonInvestigationStoryService? ActiveInstance { get; private set; }

    public MarlonInvestigationStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getOriginStage,
        Func<GameLocation, bool> hasActiveAllyAt,
        Action onDebriefComplete)
    {
        _helper = helper;
        _monitor = monitor;
        _getOriginStage = getOriginStage;
        _hasActiveAllyAt = hasActiveAllyAt;
        _onDebriefComplete = onDebriefComplete;
        ActiveInstance = this;
    }

    /// <summary>
    /// 0 = waiting for first ally, 1 = Marlon briefing accepted, 2 = mine trail found,
    /// 3 = mutant evidence secured, 4 = debrief complete / second NPC slot unlocked.
    /// </summary>
    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded()
        => _stage = ReadStage(Game1.MasterPlayer);

    public MarlonInvestigationTransition OnWarped(GameLocation location)
    {
        if (!CanAdvance(location))
            return MarlonInvestigationTransition.None;

        if (_stage == 0
            && location.NameOrUniqueName.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase)
            && _hasActiveAllyAt(location))
        {
            ShowLine("story.marlon-case.briefing");
            SetStage(Game1.MasterPlayer, 1, "briefing-with-first-ally");
            ShowObjective("story.marlon-case.objective.mine");
            return MarlonInvestigationTransition.BriefingStarted;
        }

        if (_stage == 1 && location is MineShaft && _hasActiveAllyAt(location))
        {
            ShowLine("story.marlon-case.mine-trail");
            SetStage(Game1.MasterPlayer, 2, "mine-trail-found");
            ShowObjective("story.marlon-case.objective.mutant");
            return MarlonInvestigationTransition.MineTrailFound;
        }

        if (_stage == 3
            && location.NameOrUniqueName.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase)
            && _hasActiveAllyAt(location))
        {
            ShowLine("story.marlon-case.debrief");
            SetStage(Game1.MasterPlayer, CompleteStage, "marlon-debrief");
            _onDebriefComplete();
            ShowObjective("story.marlon-case.complete");
            return MarlonInvestigationTransition.DebriefCompleted;
        }

        return MarlonInvestigationTransition.None;
    }

    public bool ObserveMonsterDeath(Monster monster)
    {
        if (!Context.IsWorldReady
            || !Context.IsMainPlayer
            || _getOriginStage() < 2
            || _stage != 2
            || Game1.eventUp
            || !MonsterMutationService.IsMutant(monster)
            || monster.modData.ContainsKey(EvidenceMonsterMarker))
        {
            return false;
        }

        GameLocation? location = monster.currentLocation ?? Game1.currentLocation;
        if (location is not MineShaft || !_hasActiveAllyAt(location))
            return false;

        monster.modData[EvidenceMonsterMarker] = "1";
        SetStage(Game1.MasterPlayer, 3, $"mutant-evidence:{monster.GetType().FullName ?? monster.Name}");
        ShowLine("story.marlon-case.evidence");
        ShowObjective("story.marlon-case.objective.return");
        _monitor.Log(
            $"[MarlonCase] mutant evidence secured from {monster.Name} ({monster.GetType().FullName}) in {location.NameOrUniqueName}.",
            LogLevel.Info);
        return true;
    }

    public void Reset(Farmer storyOwner)
    {
        storyOwner.modData.Remove(StageKey);
        _stage = 0;
        _monitor.Log("[MarlonCase] investigation stage reset. Roster unlocks were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer storyOwner, int stage)
        => SetStage(storyOwner, Math.Clamp(stage, 0, CompleteStage), "debug");

    public string Describe()
    {
        if (!Context.IsWorldReady)
            return "Marlon Investigation: world not loaded.";

        string objective = _stage switch
        {
            0 when _getOriginStage() < 2 => "finish-marlon-origin-bridge",
            0 => "bring-first-ally-to-guild",
            1 => "enter-mineshaft-with-ally",
            2 => "defeat-a-natural-mutant-in-mineshaft-with-ally",
            3 => "return-evidence-to-marlon-with-ally",
            _ => "case-complete-second-slot-unlocked"
        };

        return $"Marlon Investigation: originStage={_getOriginStage()} | stage={_stage}/{CompleteStage} | objective={objective}";
    }

    private bool CanAdvance(GameLocation location)
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getOriginStage() >= 2
            && location is not null
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private void SetStage(Farmer storyOwner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        storyOwner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[MarlonCase] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
    }

    private static int ReadStage(Farmer storyOwner)
    {
        if (!storyOwner.modData.TryGetValue(StageKey, out string? raw) || !int.TryParse(raw, out int value))
            return 0;
        return Math.Clamp(value, 0, CompleteStage);
    }

    private void ShowLine(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text))
            Game1.drawObjectDialogue(text);
    }

    private void ShowObjective(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text) && !Game1.eventUp)
            Game1.showGlobalMessage(text);
    }
}
