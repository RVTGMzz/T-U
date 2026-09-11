using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.40 HIGH response preparation.
///
/// SURGE HIGH has been confirmed and story NPC slot 4 is already authorized by Alpha 6.7.38.
/// This checkpoint does not descend into a new dungeon. Instead, Marlon requires a full operational
/// formation to establish a staging line, withdrawal criteria, and a no-pursuit threshold at the
/// exact recorded breach face. A continuous readiness hold validates that the team can maintain the
/// protocol before a later checkpoint is allowed to cross into the lower workings.
/// </summary>
internal sealed class LowerWorkingsEntryProtocolStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolStage";
    public const string ProtocolReadyFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady";
    public const int CompleteStage = 4;
    public const int ReadinessHoldTicksRequired = 240;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getSurgeHighStage;
    private readonly Func<bool> _isSurgeHigh;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private readonly Func<int> _getUnlockedNpcSlots;
    private readonly Func<int> _getOnlineFarmerCount;
    private readonly Func<int> _getConfiguredPeopleCap;
    private int _stage;
    private int _readinessTicks;

    public LowerWorkingsEntryProtocolStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getSurgeHighStage,
        Func<bool> isSurgeHigh,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt,
        Func<int> getUnlockedNpcSlots,
        Func<int> getOnlineFarmerCount,
        Func<int> getConfiguredPeopleCap)
    {
        _helper = helper;
        _monitor = monitor;
        _getSurgeHighStage = getSurgeHighStage;
        _isSurgeHigh = isSurgeHigh;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
        _getUnlockedNpcSlots = getUnlockedNpcSlots;
        _getOnlineFarmerCount = getOnlineFarmerCount;
        _getConfiguredPeopleCap = getConfiguredPeopleCap;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public bool ProtocolReady
        => Context.IsWorldReady
            && Game1.MasterPlayer.modData.TryGetValue(ProtocolReadyFlagKey, out string? value)
            && value == "1";

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        _readinessTicks = 0;
        if (_stage >= CompleteStage)
            Game1.MasterPlayer.modData[ProtocolReadyFlagKey] = "1";
    }

    public void OnWarped(GameLocation location)
    {
        _readinessTicks = 0;
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (_getSurgeHighStage() < SurgeHighEscalationStoryService.CompleteStage || !_isSurgeHigh())
        {
            if (Is(location, "AdventureGuild"))
                Hud("story.entry-protocol.need-high");
            return;
        }

        if (_getUnlockedNpcSlots() < TeamUpRosterProgressionService.MaxStoryNpcSlots)
        {
            if (Is(location, "AdventureGuild"))
                Hud("story.entry-protocol.need-roster");
            return;
        }

        if (!CanPresent())
            return;

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            if (!TryGetBreachLocation(Game1.MasterPlayer, out _))
            {
                Hud("story.entry-protocol.missing-face");
                return;
            }

            Show("story.entry-protocol.briefing");
            SetStage(Game1.MasterPlayer, 1, "high-response-entry-protocol-briefed");
            Hud("story.entry-protocol.objective.reach");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
            {
                Hud("story.entry-protocol.wrong-shaft");
                return;
            }

            if (!HasFullOperationalFormation(location))
            {
                Hud("story.entry-protocol.need-full-team");
                return;
            }

            Show("story.entry-protocol.staging");
            SetStage(Game1.MasterPlayer, 2, $"entry-staging-line-established:{location.NameOrUniqueName}");
            Hud("story.entry-protocol.objective.hold");
            return;
        }

        if (_stage == 2 && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
                Hud("story.entry-protocol.wrong-shaft");
            else if (!HasFullOperationalFormation(location))
                Hud("story.entry-protocol.need-full-team");
            return;
        }

        if (_stage == 3 && Is(location, "AdventureGuild"))
        {
            Show("story.entry-protocol.report");
            Game1.MasterPlayer.modData[ProtocolReadyFlagKey] = "1";
            SetStage(Game1.MasterPlayer, CompleteStage, "lower-workings-entry-protocol-ready");
            Hud("story.entry-protocol.ready");
        }
    }

    public void Update()
    {
        if (_stage != 2 || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (location is not MineShaft
            || !IsBreachLocation(Game1.MasterPlayer, location)
            || !HasFullOperationalFormation(location)
            || !Context.IsPlayerFree)
        {
            _readinessTicks = 0;
            return;
        }

        _readinessTicks++;
        if (_readinessTicks < ReadinessHoldTicksRequired)
            return;

        _readinessTicks = 0;
        Show("story.entry-protocol.validated");
        SetStage(Game1.MasterPlayer, 3, $"withdrawal-line-validated:{location.NameOrUniqueName}");
        Hud("story.entry-protocol.objective.return");
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(ProtocolReadyFlagKey);
        _stage = 0;
        _readinessTicks = 0;
        _monitor.Log("[EntryProtocol] reset; SURGE HIGH and roster progression were not changed.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        _readinessTicks = 0;
        if (clamped >= CompleteStage)
            owner.modData[ProtocolReadyFlagKey] = "1";
        else
            owner.modData.Remove(ProtocolReadyFlagKey);
        SetStage(owner, clamped, "debug");
    }

    public int GetRequiredFieldPeople()
    {
        int peopleCap = Math.Clamp(_getConfiguredPeopleCap(), 1, 5);
        int farmers = Math.Clamp(_getOnlineFarmerCount(), 1, 5);
        int unlockedNpcSlots = Math.Clamp(_getUnlockedNpcSlots(), 0, TeamUpRosterProgressionService.MaxStoryNpcSlots);
        return Math.Min(peopleCap, farmers + unlockedNpcSlots);
    }

    public int GetRequiredNpcAllies()
    {
        int peopleCap = Math.Clamp(_getConfiguredPeopleCap(), 1, 5);
        int farmers = Math.Clamp(_getOnlineFarmerCount(), 1, 5);
        int unlockedNpcSlots = Math.Clamp(_getUnlockedNpcSlots(), 0, TeamUpRosterProgressionService.MaxStoryNpcSlots);
        return Math.Min(unlockedNpcSlots, Math.Max(0, peopleCap - farmers));
    }

    public string Describe(GameLocation currentLocation)
    {
        string breachLocation = TryGetBreachLocation(Game1.MasterPlayer, out string? stored) ? stored! : "none";
        string objective = _stage switch
        {
            0 when _getSurgeHighStage() < SurgeHighEscalationStoryService.CompleteStage || !_isSurgeHigh() => "finish-surge-high",
            0 when _getUnlockedNpcSlots() < TeamUpRosterProgressionService.MaxStoryNpcSlots => "restore-story-slot4-authorization",
            0 => "brief-with-marlon-at-guild",
            1 => "assemble-full-formation-at-recorded-breach-face",
            2 => "hold-full-formation-and-validate-withdrawal-line",
            3 => "return-to-guild-for-entry-protocol-authorization",
            _ => "lower-workings-entry-protocol-ready"
        };

        return $"Entry Protocol: surgeHighStage={_getSurgeHighStage()}/{SurgeHighEscalationStoryService.CompleteStage} | high={_isSurgeHigh()} | "
            + $"stage={_stage}/{CompleteStage} | ready={ProtocolReady} | fieldPeopleHere={_getFieldPeopleAt(currentLocation)}/{GetRequiredFieldPeople()} | "
            + $"npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)}/{GetRequiredNpcAllies()} | breachLocation={breachLocation} | "
            + $"readinessTicks={_readinessTicks}/{ReadinessHoldTicksRequired} | objective={objective}";
    }

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getSurgeHighStage() >= SurgeHighEscalationStoryService.CompleteStage
            && _isSurgeHigh()
            && _getUnlockedNpcSlots() >= TeamUpRosterProgressionService.MaxStoryNpcSlots
            && CanPresent();

    private bool CanPresent()
        => !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasFullOperationalFormation(GameLocation location)
        => _getFieldPeopleAt(location) >= GetRequiredFieldPeople()
            && _getActiveNpcAlliesAt(location) >= GetRequiredNpcAllies();

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
        _monitor.Log($"[EntryProtocol] stage -> {_stage}/{CompleteStage} ready={ProtocolReady} source={source}.", LogLevel.Info);
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
