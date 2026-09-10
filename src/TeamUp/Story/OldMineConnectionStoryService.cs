using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Story;

internal sealed class OldMineConnectionStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/OldMineConnectionStage";
    public const int CompleteStage = 3;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getMarlonStage;
    private readonly Func<GameLocation, bool> _hasActiveAllyAt;
    private readonly Action _onComplete;
    private int _stage;

    public OldMineConnectionStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getMarlonStage,
        Func<GameLocation, bool> hasActiveAllyAt,
        Action onComplete)
    {
        _helper = helper;
        _monitor = monitor;
        _getMarlonStage = getMarlonStage;
        _hasActiveAllyAt = hasActiveAllyAt;
        _onComplete = onComplete;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded() => _stage = ReadStage(Game1.MasterPlayer);

    public void OnWarped(GameLocation location)
    {
        if (!CanAdvance(location) || !_hasActiveAllyAt(location))
            return;

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            Show("story.old-mine.archive-lead");
            SetStage(Game1.MasterPlayer, 1, "guild-archive-lead");
            Hud("story.old-mine.objective.records");
        }
        else if (_stage == 1 && Is(location, "ManorHouse"))
        {
            Show("story.old-mine.sealed-record");
            SetStage(Game1.MasterPlayer, 2, "municipal-safety-ledger");
            Hud("story.old-mine.objective.return");
        }
        else if (_stage == 2 && Is(location, "AdventureGuild"))
        {
            Show("story.old-mine.connection-confirmed");
            SetStage(Game1.MasterPlayer, CompleteStage, "old-mine-connection-confirmed");
            _onComplete();
            Hud("story.old-mine.complete");
        }
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        _stage = 0;
        _monitor.Log("[OldMineConnection] stage reset; roster unlocks were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
        => SetStage(owner, Math.Clamp(stage, 0, CompleteStage), "debug");

    public string Describe()
    {
        string objective = _stage switch
        {
            0 when _getMarlonStage() < MarlonInvestigationStoryService.CompleteStage => "finish-marlon-case",
            0 => "guild-archive-lead-with-ally",
            1 => "manorhouse-records-with-ally",
            2 => "return-to-marlon-with-ally",
            _ => "connection-confirmed-slot-3-unlocked"
        };
        return $"Old Mine Connection: marlonStage={_getMarlonStage()}/4 | stage={_stage}/{CompleteStage} | objective={objective}";
    }

    private bool CanAdvance(GameLocation location)
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getMarlonStage() >= MarlonInvestigationStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[OldMineConnection] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
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
        if (!string.IsNullOrWhiteSpace(text)) Game1.drawObjectDialogue(text);
    }

    private void Hud(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text) && !Game1.eventUp) Game1.showGlobalMessage(text);
    }
}
