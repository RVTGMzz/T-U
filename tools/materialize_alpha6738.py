from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.37"
NEW = "0.2.0-alpha.6.7.38"


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
'''
(SRC / "Story" / "SurgeHighEscalationStoryService.cs").write_text(service, encoding="utf-8", newline="\n")

entry_layer = r'''using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private SurgeHighEscalationStoryService SurgeHighAlpha6738 { get; set; } = null!;

    private void RegisterAlpha6738Events()
    {
        SurgeHighAlpha6738 = new SurgeHighEscalationStoryService(
            Helper,
            Monitor,
            () => ControlledBreachAlpha6736.Stage,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732,
            (requestedSlots, source) => RosterProgressionAlpha6727.UnlockTo(Game1.MasterPlayer, requestedSlots, source),
            () => EnforceStoryRosterCapacityAlpha6727());

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6738SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6738Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6738UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_surge_high",
            "Surge HIGH escalation: status | reset | stage <0-4>.",
            OnAlpha6738Command);
    }

    private void OnAlpha6738SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            SurgeHighAlpha6738.OnSaveLoaded();
    }

    private void OnAlpha6738Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            SurgeHighAlpha6738.OnWarped(e.NewLocation);
    }

    private void OnAlpha6738UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            SurgeHighAlpha6738.Update();
    }

    private void OnAlpha6738Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_surge_high.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            SurgeHighAlpha6738.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 4)
            SurgeHighAlpha6738.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_surge_high <status|reset|stage 0-4>", LogLevel.Info);
            return;
        }

        WriteAlpha6738Diagnostic();
    }

    private void WriteAlpha6738Diagnostic()
    {
        int unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        int active = GetActiveStoryNpcCountAlpha6727();
        int effective = GetEffectiveNpcSlotLimitAlpha6727();
        List<string> lines = new()
        {
            "TEAM UP 6.7.38 - SURGE HIGH ESCALATION / STORY SLOT 4",
            Origin.Describe(),
            SurgeStoryAlpha6725.Describe(),
            ControlledBreachAlpha6736.Describe(Game1.currentLocation),
            SurgeHighAlpha6738.Describe(Game1.currentLocation, unlocked),
            $"Story roster: unlocked={unlocked}/4 | effectiveNow={effective} | activeNpcAllies={active}",
            "Route: completed first-entry probe -> Guild HIGH check briefing -> exact recorded breach face -> 180-tick stable reading -> SURGE HIGH -> Guild report -> story slot 4.",
            "SURGE HIGH is a persistent chapter state. This checkpoint does not spawn the final boss or create a lower-workings dungeon map.",
            "Formation rule remains five PEOPLE total, so multiplayer Farmers can reduce effective NPC capacity below the story allowance.",
            "Spoiler lock remains active: George is still observed Rank D / Non-Combatant / unrecruitable; historical worker identity remains unrevealed."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Surge_HIGH_Escalation_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Surge HIGH diagnostic saved: {path}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6738.cs").write_text(entry_layer, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
needle = "        RegisterAlpha6736Events();\n"
if needle not in entry:
    raise RuntimeError("6.7.36 registration missing")
entry = entry.replace(needle, needle + "        RegisterAlpha6738Events();\n", 1)
old_loaded = "loaded. Controlled breach / first-entry probe layer active."
if old_loaded not in entry:
    raise RuntimeError("6.7.36 startup message missing")
entry = entry.replace(old_loaded, "loaded. Surge HIGH escalation and story slot 4 layer active.", 1)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

roster_path = SRC / "ModEntry.Alpha6727.cs"
roster = roster_path.read_text(encoding="utf-8")
old_roster_diag = "Future chapters will unlock slots 2, 3, and 4. Five PEOPLE total remains the hard cap including online Farmers."
new_roster_diag = "Story progression can now reach the full 4/4 NPC allowance at SURGE HIGH. Five PEOPLE total remains the hard cap including online Farmers."
if old_roster_diag in roster:
    roster = roster.replace(old_roster_diag, new_roster_diag, 1)
roster_path.write_text(roster, encoding="utf-8", newline="\n")

en = {
    "story.surge-high.need-team": "Surge HIGH check requires a field team of at least three people with at least one active Team Up NPC ally in the same location.",
    "story.surge-high.missing-face": "The recorded breach face is missing. Complete the controlled breach / first-entry probe before starting the HIGH check.",
    "story.surge-high.briefing": "Marlon spreads the first-entry notes beside the pressure sketch. 'The pulse after the opening is the part I cannot dismiss. We need to know whether it was a one-time release or a sustained response. Take the field team back to the exact breach face. Do not widen it. Hold position, read the braces and shard residue, and give me a clean interval. If the pattern repeats under a stable team, we stop calling this a local disturbance.'",
    "story.surge-high.objective.reach": "Objective: return with a valid field team to the exact recorded breach face. Do not widen the opening.",
    "story.surge-high.wrong-shaft": "This is not the recorded breach face. Return to the MineShaft used for the controlled opening.",
    "story.surge-high.arrival": "The narrow breach is unchanged, but the threshold is not quiet. Fine black residue trembles in tiny outward jumps while the braces answer with a low pulse too regular to be ordinary settling. Hold formation and keep the reading clean.",
    "story.surge-high.objective.hold": "Hold the field team together at the breach face while the HIGH reading stabilizes.",
    "story.surge-high.confirmed": "The interval holds. The pulse returns through the braces in a repeating sequence, and the black shard residue lifts with each pressure wave before settling again. The response is sustained, not a single release from the breach. Team Up classification updates: THE SURGE — HIGH. The deeper workings are now actively answering the opened threshold.",
    "story.surge-high.objective.return": "SURGE HIGH confirmed. Return to Marlon at the Adventurer's Guild with the field team.",
    "story.surge-high.report": "Marlon reads the stabilized interval twice, then draws a hard line beneath HIGH. 'Three people were enough to investigate a mystery. They are not enough for what comes next. From this point forward, Team Up authorizes one more NPC field slot. Build the stronger formation before we widen that breach again. We still do this by evidence, not legends.'",
    "story.surge-high.slot4-unlocked": "SURGE HIGH confirmed — Story NPC slot 4 unlocked. The five-person total formation cap still applies.",
    "story.surge-high.complete": "SURGE HIGH confirmed. Story roster authorization is already at 4/4; the five-person total formation cap still applies."
}
vi = {
    "story.surge-high.need-team": "Kiểm tra SURGE HIGH cần một đội hiện trường ít nhất 3 người, trong đó có ít nhất 1 NPC Team Up đang hoạt động và ở cùng khu vực.",
    "story.surge-high.missing-face": "Không còn dữ liệu về điểm breach đã ghi nhận. Hãy hoàn tất Controlled Breach / First Entry trước khi bắt đầu kiểm tra HIGH.",
    "story.surge-high.briefing": "Marlon trải ghi chép của lần thăm dò đầu tiên cạnh sơ đồ áp lực. 'Thứ tôi không thể bỏ qua là nhịp chấn động sau khi mở khe. Ta phải biết đó chỉ là áp lực thoát ra một lần hay là phản ứng đang tiếp diễn. Đưa đội hiện trường trở lại đúng điểm breach. Không mở rộng thêm. Giữ nguyên đội hình, đọc phản ứng của thanh chống và bụi shard, rồi cho tôi một khoảng đo sạch. Nếu nhịp đó lặp lại khi đội hình ổn định, ta sẽ không còn gọi đây là một nhiễu động cục bộ nữa.'",
    "story.surge-high.objective.reach": "Mục tiêu: đưa đội hiện trường hợp lệ trở lại đúng điểm breach đã ghi nhận. Không mở rộng khe.",
    "story.surge-high.wrong-shaft": "Đây không phải điểm breach đã ghi nhận. Hãy quay lại MineShaft nơi đội đã thực hiện lần mở khe có kiểm soát.",
    "story.surge-high.arrival": "Khe mở hẹp vẫn như cũ, nhưng ngưỡng phía sau không còn yên. Lớp bụi shard đen rung nhẹ thành từng nhịp hướng ra ngoài, còn các thanh chống đáp lại bằng một dao động trầm đều đặn quá mức để chỉ là đá tự lún. Giữ nguyên đội hình và duy trì phép đo sạch.",
    "story.surge-high.objective.hold": "Giữ đội hiện trường tập trung tại điểm breach cho tới khi phép đo HIGH ổn định.",
    "story.surge-high.confirmed": "Khoảng đo đã ổn định. Nhịp áp lực quay lại qua các thanh chống theo chuỗi lặp, bụi shard đen bật lên theo từng đợt rồi mới lắng xuống. Đây là một phản ứng kéo dài, không phải áp lực thoát ra duy nhất khi mở khe. Phân loại Team Up cập nhật: THE SURGE — HIGH. Khu vực sâu hơn đang chủ động đáp lại ngưỡng đã được mở.",
    "story.surge-high.objective.return": "Đã xác nhận SURGE HIGH. Hãy trở lại Adventurer's Guild báo cáo cho Marlon cùng đội hiện trường.",
    "story.surge-high.report": "Marlon đọc lại toàn bộ khoảng đo hai lần rồi kẻ một đường đậm dưới chữ HIGH. 'Ba người đủ để điều tra một bí ẩn. Nhưng không đủ cho thứ sắp tới. Từ giờ Team Up cho phép thêm một vị trí NPC trong đội hiện trường. Hãy dựng đội hình mạnh hơn trước khi chúng ta mở rộng khe đó. Ta vẫn đi theo bằng chứng, không chạy theo truyền thuyết.'",
    "story.surge-high.slot4-unlocked": "Đã xác nhận SURGE HIGH — mở Story NPC slot 4. Giới hạn đội hình tổng cộng 5 người vẫn được giữ nguyên.",
    "story.surge-high.complete": "Đã xác nhận SURGE HIGH. Quyền đội hình cốt truyện đã ở mức 4/4; giới hạn tổng cộng 5 người vẫn được giữ nguyên."
}

for filename, additions in (("default.json", en), ("vi.json", vi)):
    path = SRC / "i18n" / filename
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(additions).intersection(data)
    if overlap:
        raise RuntimeError(f"6.7.38 localization keys already exist in {filename}: {sorted(overlap)}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.38 Surge HIGH escalation / story slot 4 materialized.")
