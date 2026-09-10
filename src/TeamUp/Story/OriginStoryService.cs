using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.26 main-story opening bridge.
///
/// Narrative progression is no longer triggered by merely seeing a monster. The first story beat
/// becomes available only after Alpha 6.7.25 has successfully transformed lethal defeat #10 into
/// the first Mutant and committed The Surge activation. The player is then directed to Linus in
/// the Forest, and Linus directs them to Marlon at the Adventurer's Guild.
///
/// This checkpoint deliberately stops after Marlon's first investigation scene. The old prototype
/// auto-Awakening and instant Team Up completion are retired so later chapters can own those beats.
/// </summary>
public sealed class OriginStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/SurgeNarrativeStage";
    public const string LinusPromptShownKey = "Ronvotri.TeamUp/SurgeNarrative/LinusPromptShown";

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<bool> _enabled;
    private int _stage;
    private int _ticks;

    public OriginStoryService(IModHelper helper, IMonitor monitor, Func<bool> enabled)
    {
        _helper = helper;
        _monitor = monitor;
        _enabled = enabled;
    }

    /// <summary>0 = find Linus, 1 = find Marlon, 2 = Marlon intro complete.</summary>
    public int Stage => _stage;
    public bool Completed => _stage >= 2;

    public void OnSaveLoaded()
    {
        _ticks = 0;
        _stage = ReadInt(StageKey);
    }

    public void ResetRuntime()
    {
        _ticks = 0;
        _stage = Context.IsWorldReady ? ReadInt(StageKey) : 0;
    }

    public void OnWarped(GameLocation location)
    {
        if (!_enabled()
            || !Context.IsWorldReady
            || !Context.IsMainPlayer
            || !IsSurgeActivated()
            || Game1.eventUp
            || Game1.dialogueUp
            || Game1.activeClickableMenu is not null)
        {
            return;
        }

        string name = location.NameOrUniqueName;
        if (_stage == 0 && name.Equals("Forest", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.linus.first-surge");
            SetStage(1);
            Game1.showGlobalMessage(_helper.Translation.Get("origin.quest.marlon").ToString());
            return;
        }

        if (_stage == 1 && name.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.marlon.first-surge");
            SetStage(2);
            Game1.showGlobalMessage(_helper.Translation.Get("origin.first-surge-bridge.complete").ToString());
        }
    }

    public void Update()
    {
        if (!_enabled() || !Context.IsWorldReady || !Context.IsMainPlayer || !IsSurgeActivated())
            return;

        _ticks++;
        if (_ticks % 30 != 0
            || _stage != 0
            || HasFlag(LinusPromptShownKey)
            || Game1.eventUp
            || Game1.dialogueUp
            || Game1.activeClickableMenu is not null)
        {
            return;
        }

        Game1.player.modData[LinusPromptShownKey] = "1";
        Game1.showGlobalMessage(_helper.Translation.Get("origin.quest.linus").ToString());
        _monitor.Log("[Story] First Surge committed. Objective opened: find Linus in the Forest.", LogLevel.Info);
    }

    public string Describe()
    {
        if (!Context.IsWorldReady)
            return "First Surge narrative: world not loaded.";

        string objective = _stage switch
        {
            0 when !IsSurgeActivated() => "waiting-for-first-mutant",
            0 => "find-linus-forest",
            1 => "find-marlon-adventure-guild",
            _ => "marlon-investigation-open"
        };

        return $"First Surge Narrative: surgeActivated={IsSurgeActivated()} | stage={_stage}/2 | objective={objective} | linusPrompt={HasFlag(LinusPromptShownKey)}";
    }

    public void ResetStory(Farmer farmer)
    {
        farmer.modData.Remove(StageKey);
        farmer.modData.Remove(LinusPromptShownKey);
        _stage = 0;
        _ticks = 0;
        _monitor.Log("[Story] First Surge narrative bridge reset. Surge activation itself was not changed.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer farmer, int stage)
    {
        _stage = Math.Clamp(stage, 0, 2);
        farmer.modData[StageKey] = _stage.ToString();
        if (_stage == 0)
            farmer.modData.Remove(LinusPromptShownKey);
        _monitor.Log($"[Story] First Surge narrative debug stage set to {_stage}/2.", LogLevel.Info);
    }

    private bool IsSurgeActivated()
        => TheSurgeStoryService.ActiveInstance?.IsActivated == true;

    private void ShowLine(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text))
            Game1.drawObjectDialogue(text);
    }

    private int ReadInt(string key)
        => Game1.player.modData.TryGetValue(key, out string? raw) && int.TryParse(raw, out int value)
            ? Math.Clamp(value, 0, 2)
            : 0;

    private bool HasFlag(string key)
        => Game1.player.modData.TryGetValue(key, out string? value) && value == "1";

    private void SetStage(int stage)
    {
        _stage = Math.Clamp(stage, 0, 2);
        Game1.player.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[Story] First Surge narrative advanced to stage {_stage}/2.", LogLevel.Info);
    }
}
