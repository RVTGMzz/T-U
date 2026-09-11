from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.39"
NEW = "0.2.0-alpha.6.7.40"


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

service = r'''using StardewModdingAPI;
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
'''
(SRC / "Story" / "LowerWorkingsEntryProtocolStoryService.cs").write_text(service, encoding="utf-8", newline="\n")

entry_layer = r'''using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private LowerWorkingsEntryProtocolStoryService EntryProtocolAlpha6740 { get; set; } = null!;

    private void RegisterAlpha6740Events()
    {
        EntryProtocolAlpha6740 = new LowerWorkingsEntryProtocolStoryService(
            Helper,
            Monitor,
            () => SurgeHighAlpha6738.Stage,
            () => SurgeHighAlpha6738.IsHigh,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732,
            () => RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
            () => GetOnlineFarmerIds().Count,
            () => Config.MaxPartyMembers);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6740SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6740Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6740UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_entry_protocol",
            "Lower Workings entry protocol: status | reset | stage <0-4>.",
            OnAlpha6740Command);
    }

    private void OnAlpha6740SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            EntryProtocolAlpha6740.OnSaveLoaded();
    }

    private void OnAlpha6740Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            EntryProtocolAlpha6740.OnWarped(e.NewLocation);
    }

    private void OnAlpha6740UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            EntryProtocolAlpha6740.Update();
    }

    private void OnAlpha6740Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_entry_protocol.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            EntryProtocolAlpha6740.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 4)
            EntryProtocolAlpha6740.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_entry_protocol <status|reset|stage 0-4>", LogLevel.Info);
            return;
        }

        WriteAlpha6740Diagnostic();
    }

    private void WriteAlpha6740Diagnostic()
    {
        int unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        int active = GetActiveStoryNpcCountAlpha6727();
        int effective = GetEffectiveNpcSlotLimitAlpha6727();
        List<string> lines = new()
        {
            "TEAM UP 6.7.40 - HIGH RESPONSE PREPARATION / LOWER WORKINGS ENTRY PROTOCOL",
            Origin.Describe(),
            SurgeHighAlpha6738.Describe(Game1.currentLocation, unlocked),
            EntryProtocolAlpha6740.Describe(Game1.currentLocation),
            $"Story roster: unlocked={unlocked}/4 | effectiveNow={effective} | activeNpcAllies={active}",
            $"Full operational formation here requires people={EntryProtocolAlpha6740.GetRequiredFieldPeople()} and activeNpcAllies={EntryProtocolAlpha6740.GetRequiredNpcAllies()} under the configured five-person cap.",
            "Route: SURGE HIGH + slot4 -> Guild protocol briefing -> exact recorded breach face with full formation -> 240-tick readiness hold -> Guild report -> Entry Protocol READY.",
            "Abort criteria: broken formation, wrong shaft, warp, menu/dialogue ownership, or leaving player-free state resets the readiness hold.",
            "This checkpoint validates staging and withdrawal discipline only. It does not descend into a new lower-workings map and does not spawn a boss.",
            "Spoiler lock remains active: George is still observed Rank D / Non-Combatant / unrecruitable; historical worker identity remains unrevealed."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Lower_Workings_Entry_Protocol_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Entry Protocol diagnostic saved: {path}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6740.cs").write_text(entry_layer, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
needle = "        RegisterAlpha6738Events();\n"
if needle not in entry:
    raise RuntimeError("6.7.38 registration marker missing")
entry = entry.replace(needle, needle + "        RegisterAlpha6740Events();\n", 1)
old_startup = "Surge HIGH reaction layer active."
if old_startup not in entry:
    raise RuntimeError("6.7.39 startup marker missing")
entry = entry.replace(old_startup, "HIGH response preparation / lower-workings entry protocol layer active.", 1)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

EN = {
    "story.entry-protocol.need-high": "Marlon will not issue a lower-workings entry protocol until SURGE HIGH has been confirmed and reported.",
    "story.entry-protocol.need-roster": "The HIGH response requires story NPC slot 4 authorization before entry preparation can begin.",
    "story.entry-protocol.missing-face": "The recorded breach face is missing. Re-establish the controlled-breach checkpoint before preparing entry.",
    "story.entry-protocol.need-full-team": "Entry preparation requires the full operational formation allowed by the current five-person cap. Regroup before continuing.",
    "story.entry-protocol.wrong-shaft": "This is not the recorded breach face. The entry protocol must be validated at the exact HIGH-response access point.",
    "story.entry-protocol.briefing": "Marlon: HIGH changes the rules. The next descent gets no improvisation. Assemble the full operational formation at the breach face. Mark a staging line on the modern side, a clear withdrawal route, and a no-pursuit threshold. If formation breaks, supports shift, or pressure spikes, everyone withdraws. Today we validate the protocol. We do not descend.",
    "story.entry-protocol.objective.reach": "HIGH RESPONSE: assemble the full operational formation at the recorded breach face.",
    "story.entry-protocol.staging": "The team establishes a staging line on the modern side of the breach. Headcount is confirmed, the withdrawal order is assigned, the rear anchor watches the supports, and a no-pursuit threshold is marked. Hold the complete formation long enough to prove the fallback line can stay intact under HIGH conditions.",
    "story.entry-protocol.objective.hold": "ENTRY PROTOCOL: hold the full formation and keep the fallback line stable.",
    "story.entry-protocol.validated": "The readiness interval closes cleanly. Formation stayed intact, the withdrawal route remained clear, the braces held steady, and the HIGH pressure did not accelerate during the drill. The team has a viable entry-and-retreat protocol. Return to Marlon without crossing deeper today.",
    "story.entry-protocol.objective.return": "ENTRY PROTOCOL: readiness drill complete. Return to Marlon for authorization.",
    "story.entry-protocol.report": "Marlon: Good. Full formation, fixed fallback line, no-pursuit threshold, and clear abort conditions. That is how we enter a HIGH site without turning caution into panic. I am marking the Lower Workings Entry Protocol READY. The next operation may cross the threshold. Not before then.",
    "story.entry-protocol.ready": "LOWER WORKINGS ENTRY PROTOCOL • READY"
}

VI = {
    "story.entry-protocol.need-high": "Marlon sẽ chưa ban hành quy trình vào lower workings cho tới khi SURGE HIGH được xác nhận và báo cáo đầy đủ.",
    "story.entry-protocol.need-roster": "Phản ứng cấp HIGH yêu cầu Story NPC Slot 4 được mở trước khi bắt đầu chuẩn bị tiến vào.",
    "story.entry-protocol.missing-face": "Không còn dữ liệu về mặt breach đã ghi nhận. Hãy khôi phục checkpoint Controlled Breach trước khi chuẩn bị tiến vào.",
    "story.entry-protocol.need-full-team": "Chuẩn bị tiến vào cần đội hình tác chiến đầy đủ theo giới hạn tối đa 5 người hiện tại. Hãy tập hợp đủ đội trước khi tiếp tục.",
    "story.entry-protocol.wrong-shaft": "Đây không phải mặt breach đã ghi nhận. Entry Protocol phải được xác nhận đúng tại điểm tiếp cận của HIGH response.",
    "story.entry-protocol.briefing": "Marlon: HIGH làm thay đổi luật chơi. Lần xuống tiếp theo không được tùy cơ ứng biến. Tập hợp đội hình tác chiến đầy đủ tại mặt breach. Đánh dấu điểm tập kết ở phía hầm hiện đại, một đường rút rõ ràng và một ngưỡng tuyệt đối không truy đuổi vượt qua. Nếu đội hình vỡ, hệ chống dịch chuyển hoặc áp lực tăng vọt, tất cả rút ngay. Hôm nay ta chỉ xác nhận quy trình. Chưa xuống sâu.",
    "story.entry-protocol.objective.reach": "HIGH RESPONSE: tập hợp đội hình tác chiến đầy đủ tại mặt breach đã ghi nhận.",
    "story.entry-protocol.staging": "Cả đội thiết lập tuyến tập kết ở phía hiện đại của mặt breach. Quân số được xác nhận, thứ tự rút lui được phân công, người chốt sau theo dõi hệ chống và một ngưỡng cấm truy đuổi được đánh dấu. Hãy giữ nguyên đội hình đầy đủ đủ lâu để chứng minh tuyến rút có thể duy trì dưới điều kiện HIGH.",
    "story.entry-protocol.objective.hold": "ENTRY PROTOCOL: giữ đội hình đầy đủ và duy trì ổn định tuyến rút lui.",
    "story.entry-protocol.validated": "Khoảng kiểm tra kết thúc sạch. Đội hình không vỡ, đường rút luôn thông, hệ chống ổn định và áp lực HIGH không tăng tốc trong suốt buổi diễn tập. Đội đã có quy trình tiến vào và rút lui khả thi. Quay về gặp Marlon, hôm nay không vượt sâu hơn.",
    "story.entry-protocol.objective.return": "ENTRY PROTOCOL: diễn tập sẵn sàng hoàn tất. Quay về gặp Marlon để xin xác nhận.",
    "story.entry-protocol.report": "Marlon: Tốt. Đội hình đầy đủ, tuyến rút cố định, ngưỡng cấm truy đuổi và điều kiện hủy nhiệm vụ đều rõ. Đó là cách tiến vào một khu vực HIGH mà không biến thận trọng thành hoảng loạn. Tôi xác nhận Lower Workings Entry Protocol READY. Nhiệm vụ sau mới được phép vượt qua ngưỡng. Không sớm hơn.",
    "story.entry-protocol.ready": "LOWER WORKINGS ENTRY PROTOCOL • READY"
}

for rel, additions in (("i18n/default.json", EN), ("i18n/vi.json", VI)):
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(additions) & set(data)
    if overlap:
        raise RuntimeError(f"Localization keys already exist in {rel}: {sorted(overlap)}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.40 HIGH response preparation / lower-workings entry protocol materialized.")
