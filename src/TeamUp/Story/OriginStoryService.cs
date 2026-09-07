using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Four-beat, low-intrusion origin story:
/// combat anomaly -> Linus observation -> Marlon names The Surge -> first Awakening -> Team Up.
/// Progress is stored in Farmer.modData so no PartySaveData migration is required.
/// </summary>
public sealed class OriginStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/OriginStage";
    public const string CombatSeenKey = "Ronvotri.TeamUp/OriginCombatSeen";

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<IReadOnlyList<PartyMemberData>> _getMembers;
    private readonly Func<bool> _enabled;
    private int _stage;
    private int _ticks;

    public OriginStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<IReadOnlyList<PartyMemberData>> getMembers,
        Func<bool> enabled)
    {
        _helper = helper;
        _monitor = monitor;
        _getMembers = getMembers;
        _enabled = enabled;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= 4;

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
        if (!_enabled() || !Context.IsWorldReady || Game1.eventUp || Game1.activeClickableMenu is not null)
            return;

        string name = location.NameOrUniqueName;
        if (_stage == 0 && HasFlag(CombatSeenKey) && name.Equals("Forest", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.linus");
            SetStage(1);
            return;
        }

        if (_stage == 1 && name.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.marlon.surge");
            SetStage(2);
            return;
        }

        if (_stage == 3 && name.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.marlon.teamup");
            SetStage(4);
            Game1.showGlobalMessage(_helper.Translation.Get("origin.complete").ToString());
        }
    }

    public void Update()
    {
        if (!_enabled() || !Context.IsWorldReady || Game1.currentLocation is null)
            return;

        _ticks++;
        if (_ticks % 30 != 0 || Game1.eventUp)
            return;

        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();

        if (_stage == 0 && monsters.Count > 0 && !HasFlag(CombatSeenKey))
            Game1.player.modData[CombatSeenKey] = "1";

        if (_stage != 2 || monsters.Count == 0 || Game1.activeClickableMenu is not null || Game1.dialogueUp)
            return;

        PartyMemberData? awakened = _getMembers()
            .FirstOrDefault(member =>
                member.RecruiterId == Game1.player.UniqueMultiplayerID
                && member.State == PartyMemberState.Following
                && !member.IsDowned
                && member.CurrentHealth > 0
                && Game1.getCharacterFromName(member.CharacterName)?.currentLocation == Game1.currentLocation);
        if (awakened is null)
            return;

        NPC? npc = Game1.getCharacterFromName(awakened.CharacterName);
        string display = npc?.displayName ?? awakened.CharacterName;
        npc?.showTextAboveHead("AWAKENING", Microsoft.Xna.Framework.Color.Gold, 2, 1600, 0);
        Game1.showGlobalMessage(_helper.Translation.Get("origin.awakening", new { name = display }).ToString());
        SetStage(3);
    }

    private void ShowLine(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (string.IsNullOrWhiteSpace(text))
            return;
        Game1.drawObjectDialogue(text);
    }

    private int ReadInt(string key)
        => Game1.player.modData.TryGetValue(key, out string? raw) && int.TryParse(raw, out int value)
            ? Math.Clamp(value, 0, 4)
            : 0;

    private bool HasFlag(string key)
        => Game1.player.modData.TryGetValue(key, out string? value) && value == "1";

    private void SetStage(int stage)
    {
        _stage = Math.Clamp(stage, 0, 4);
        Game1.player.modData[StageKey] = _stage.ToString();
        _monitor.Log($"Team Up origin advanced to stage {_stage}.", LogLevel.Debug);
    }
}