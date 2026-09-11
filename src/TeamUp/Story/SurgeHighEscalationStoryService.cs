using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.38 major-chapter escalation.
///
/// The first-entry probe has already proven that the sealed lower workings are physically intact.
/// This service sends the same field discipline back to the recorded breach face to determine
/// whether the pressure response was only a transient release or a sustained escalation. A stable
/// 180-tick reading confirms SURGE HIGH. Only after the team reports that confirmed state to Marlon
/// is story NPC slot 4 unlocked. The historical worker remains unnamed and no final boss is spawned.
/// </summary>
internal sealed class SurgeHighEscalationStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/SurgeHighEscalationStage";
    public const string SurgeHighFlagKey = "Ronvotri.TeamUp/Story/SurgeHighConfirmed";
    public const int CompleteStage = 4;
    public const int MinimumFieldPeople = 3;
    public const int MinimumActiveNpcAllies = 1;
    public const int HighConfirmationTicksRequired = 180;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getControlledBreachStage;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private readonly Func<int, string, bool> _unlockNpcSlots;
    private readonly Action _enforceRosterCapacity;
    private int _stage;
    private int _highConfirmationTicks;

    public SurgeHighEscalationStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getControlledBreachStage,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt,
        Func<int, string, bool> unlockNpcSlots,
        Action enforceRosterCapacity)
    {
        _helper = helper;
        _monitor = monitor;
        _getControlledBreachStage = getControlledBreachStage;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
        _unlockNpcSlots = unlockNpcSlots;
        _enforceRosterCapacity = enforceRosterCapacity;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public bool IsHigh
        => Context.IsWorldReady
            && Game1.MasterPlayer.modData.TryGetValue(SurgeHighFlagKey, out string? value)
            && value == "1";

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        _highConfirmationTicks = 0;
        if (_stage >= 3)
            Game1.MasterPlayer.modData[SurgeHighFlagKey] = "1";
    }

    public void OnWarped(GameLocation location)
    {
        _highConfirmationTicks = 0;
        if (!CanAdvance())
            return;

        if (!HasRequiredFieldTeam(location))
        {
            if (IsRelevantLocationForCurrentStage(location))
                Hud("story.surge-high.need-team");
            return;
        }

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            if (!TryGetBreachLocation(Game1.MasterPlayer, out _))
            {
                Hud("story.surge-high.missing-face");
                return;
            }

            Show("story.surge-high.briefing");
            SetStage(Game1.MasterPlayer, 1, "surge-high-field-check-briefed");
            Hud("story.surge-high.objective.reach");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
            {
                Hud("story.surge-high.wrong-shaft");
                return;
            }

            Show("story.surge-high.arrival");
            SetStage(Game1.MasterPlayer, 2, $"high-reading-started:{location.NameOrUniqueName}");
            Hud("story.surge-high.objective.hold");
            return;
        }

        if (_stage == 2 && location is MineShaft && !IsBreachLocation(Game1.MasterPlayer, location))
        {
            Hud("story.surge-high.wrong-shaft");
            return;
        }

        if (_stage == 3 && Is(location, "AdventureGuild"))
        {
            Show("story.surge-high.report");
            bool newlyUnlocked = _unlockNpcSlots(4, "surge-high-confirmed");
            _enforceRosterCapacity();
            SetStage(Game1.MasterPlayer, CompleteStage, "surge-high-reported-slot4-authorized");
            Hud(newlyUnlocked ? "story.surge-high.slot4-unlocked" : "story.surge-high.complete");
        }
    }

    public void Update()
    {
        if (_stage != 2 || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (location is not MineShaft
            || !IsBreachLocation(Game1.MasterPlayer, location)
            || !HasRequiredFieldTeam(location)
            || !Context.IsPlayerFree)
        {
            _highConfirmationTicks = 0;
            return;
        }

        _highConfirmationTicks++;
        if (_highConfirmationTicks < HighConfirmationTicksRequired)
            return;

        _highConfirmationTicks = 0;
        Game1.MasterPlayer.modData[SurgeHighFlagKey] = "1";
        Show("story.surge-high.confirmed");
        SetStage(Game1.MasterPlayer, 3, $"surge-high-confirmed:{location.NameOrUniqueName}");
        Hud("story.surge-high.objective.return");
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(SurgeHighFlagKey);
        _stage = 0;
        _highConfirmationTicks = 0;
        _monitor.Log("[SurgeHigh] escalation reset; controlled-breach progress and already-unlocked roster slots were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        _highConfirmationTicks = 0;
        if (clamped >= 3)
            owner.modData[SurgeHighFlagKey] = "1";
        else
            owner.modData.Remove(SurgeHighFlagKey);

        SetStage(owner, clamped, "debug");
        if (clamped >= CompleteStage)
        {
            _unlockNpcSlots(4, "surge-high-debug-stage4");
            _enforceRosterCapacity();
        }
    }

    public string Describe(GameLocation currentLocation, int unlockedNpcSlots)
    {
        string breachLocation = TryGetBreachLocation(Game1.MasterPlayer, out string? stored)
            ? stored!
            : "none";
        string objective = _stage switch
        {
            0 when _getControlledBreachStage() < ControlledBreachFirstEntryStoryService.CompleteStage => "finish-controlled-breach",
            0 => "brief-with-marlon-at-guild",
            1 => "return-to-recorded-breach-face",
            2 => "hold-field-team-for-high-reading",
            3 => "report-surge-high-to-marlon",
            _ => "surge-high-confirmed-slot4-authorized"
        };

        return $"Surge HIGH: breachStage={_getControlledBreachStage()}/{ControlledBreachFirstEntryStoryService.CompleteStage} | "
            + $"stage={_stage}/{CompleteStage} | high={IsHigh} | fieldPeopleHere={_getFieldPeopleAt(currentLocation)} | "
            + $"npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)} | breachLocation={breachLocation} | "
            + $"highTicks={_highConfirmationTicks}/{HighConfirmationTicksRequired} | storyNpcSlots={unlockedNpcSlots}/4 | objective={objective}";
    }

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getControlledBreachStage() >= ControlledBreachFirstEntryStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasRequiredFieldTeam(GameLocation location)
        => _getFieldPeopleAt(location) >= MinimumFieldPeople
            && _getActiveNpcAlliesAt(location) >= MinimumActiveNpcAllies;

    private bool IsRelevantLocationForCurrentStage(GameLocation location)
        => (_stage is 0 or 3 && Is(location, "AdventureGuild"))
            || (_stage is 1 or 2 && location is MineShaft);

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
        _monitor.Log($"[SurgeHigh] stage -> {_stage}/{CompleteStage} high={IsHigh} source={source}.", LogLevel.Info);
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
