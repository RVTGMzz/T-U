from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.35"
NEW = "0.2.0-alpha.6.7.36"


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
/// Alpha 6.7.36 controlled breach / first-entry probe.
///
/// The sealed access face has already been triangulated and pressure-surveyed. This checkpoint
/// returns a real three-person field team to that exact MineShaft face, holds formation while a
/// narrow controlled opening is stabilized, then performs only a short threshold probe into the
/// old workings. No custom lower-workings map is claimed here, no boss is spawned, story slot 4
/// remains locked, and the historical miner remains unnamed.
/// </summary>
internal sealed class ControlledBreachFirstEntryStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/ControlledBreachFirstEntryStage";
    public const string BreachLocationKey = "Ronvotri.TeamUp/Story/ControlledBreachLocation";
    public const int CompleteStage = 5;
    public const int MinimumFieldPeople = 3;
    public const int MinimumActiveNpcAllies = 1;
    public const int BreachHoldTicksRequired = 180;
    public const int EntryProbeTicksRequired = 120;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getCorridorStage;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private int _stage;
    private int _breachHoldTicks;
    private int _entryProbeTicks;

    public ControlledBreachFirstEntryStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getCorridorStage,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt)
    {
        _helper = helper;
        _monitor = monitor;
        _getCorridorStage = getCorridorStage;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        ResetRuntimeHolds();
    }

    public void OnWarped(GameLocation location)
    {
        ResetRuntimeHolds();
        if (!CanAdvance())
            return;

        if (!HasRequiredFieldTeam(location))
        {
            if (IsRelevantLocationForCurrentStage(location))
                Hud("story.breach.need-team");
            return;
        }

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            string? surveyLocation = ResolveSurveyLocation(Game1.MasterPlayer);
            if (string.IsNullOrWhiteSpace(surveyLocation))
            {
                Hud("story.breach.missing-face");
                return;
            }

            Game1.MasterPlayer.modData[BreachLocationKey] = surveyLocation;
            Show("story.breach.briefing");
            SetStage(Game1.MasterPlayer, 1, $"controlled-breach-briefing:{surveyLocation}");
            Hud("story.breach.objective.reach");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
            {
                Hud("story.breach.wrong-shaft");
                return;
            }

            Show("story.breach.arrival");
            SetStage(Game1.MasterPlayer, 2, $"breach-face-ready:{location.NameOrUniqueName}");
            Hud("story.breach.objective.stabilize");
            return;
        }

        if (_stage is 2 or 3 && location is MineShaft && !IsBreachLocation(Game1.MasterPlayer, location))
            Hud("story.breach.wrong-shaft");

        if (_stage == 4 && Is(location, "AdventureGuild"))
        {
            Show("story.breach.report");
            SetStage(Game1.MasterPlayer, CompleteStage, "first-entry-probe-reported");
            Hud("story.breach.complete");
        }
    }

    public void Update()
    {
        if (_stage is not (2 or 3) || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (location is not MineShaft
            || !IsBreachLocation(Game1.MasterPlayer, location)
            || !HasRequiredFieldTeam(location)
            || !Context.IsPlayerFree)
        {
            ResetRuntimeHolds();
            return;
        }

        if (_stage == 2)
        {
            _entryProbeTicks = 0;
            _breachHoldTicks++;
            if (_breachHoldTicks < BreachHoldTicksRequired)
                return;

            _breachHoldTicks = 0;
            Show("story.breach.opened");
            SetStage(Game1.MasterPlayer, 3, $"controlled-opening:{location.NameOrUniqueName}");
            Hud("story.breach.objective.probe");
            return;
        }

        _breachHoldTicks = 0;
        _entryProbeTicks++;
        if (_entryProbeTicks < EntryProbeTicksRequired)
            return;

        _entryProbeTicks = 0;
        Show("story.breach.first-entry");
        SetStage(Game1.MasterPlayer, 4, $"first-entry-threshold-probe:{location.NameOrUniqueName}");
        Hud("story.breach.objective.return");
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(BreachLocationKey);
        _stage = 0;
        ResetRuntimeHolds();
        _monitor.Log("[ControlledBreach] stage reset; corridor, triangulation, reactions, and roster progress were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        if (clamped == 0)
            owner.modData.Remove(BreachLocationKey);
        else if (!owner.modData.ContainsKey(BreachLocationKey))
        {
            string? surveyLocation = ResolveSurveyLocation(owner);
            if (!string.IsNullOrWhiteSpace(surveyLocation))
                owner.modData[BreachLocationKey] = surveyLocation;
            else if (Game1.currentLocation is MineShaft)
                owner.modData[BreachLocationKey] = Game1.currentLocation.NameOrUniqueName;
        }

        ResetRuntimeHolds();
        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string breachLocation = Game1.MasterPlayer.modData.TryGetValue(BreachLocationKey, out string? stored)
            ? stored
            : "none";
        string objective = _stage switch
        {
            0 when _getCorridorStage() < SealedCorridorApproachStoryService.CompleteStage => "finish-sealed-corridor-approach",
            0 => "brief-with-marlon-at-guild",
            1 => "return-to-confirmed-survey-face",
            2 => "hold-field-team-for-controlled-opening",
            3 => "hold-field-team-for-threshold-probe",
            4 => "return-to-marlon",
            _ => "first-entry-probe-reported"
        };

        return $"Controlled Breach / First Entry: corridorStage={_getCorridorStage()}/4 | stage={_stage}/{CompleteStage} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)} | npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)} | "
            + $"breachLocation={breachLocation} | breachTicks={_breachHoldTicks}/{BreachHoldTicksRequired} | "
            + $"probeTicks={_entryProbeTicks}/{EntryProbeTicksRequired} | objective={objective}";
    }

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getCorridorStage() >= SealedCorridorApproachStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasRequiredFieldTeam(GameLocation location)
        => _getFieldPeopleAt(location) >= MinimumFieldPeople
            && _getActiveNpcAlliesAt(location) >= MinimumActiveNpcAllies;

    private bool IsRelevantLocationForCurrentStage(GameLocation location)
        => (_stage is 0 or 4 && Is(location, "AdventureGuild"))
            || (_stage is 1 or 2 or 3 && location is MineShaft);

    private static string? ResolveSurveyLocation(Farmer owner)
        => owner.modData.TryGetValue(SealedCorridorApproachStoryService.SurveyLocationKey, out string? stored)
            && !string.IsNullOrWhiteSpace(stored)
            ? stored
            : null;

    private static bool IsBreachLocation(Farmer owner, GameLocation location)
        => owner.modData.TryGetValue(BreachLocationKey, out string? stored)
            && !string.IsNullOrWhiteSpace(stored)
            && stored.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[ControlledBreach] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
    }

    private static int ReadStage(Farmer owner)
        => owner.modData.TryGetValue(StageKey, out string? raw) && int.TryParse(raw, out int value)
            ? Math.Clamp(value, 0, CompleteStage)
            : 0;

    private void ResetRuntimeHolds()
    {
        _breachHoldTicks = 0;
        _entryProbeTicks = 0;
    }

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
(SRC / "Story" / "ControlledBreachFirstEntryStoryService.cs").write_text(service, encoding="utf-8", newline="\n")

entry_layer = r'''using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private ControlledBreachFirstEntryStoryService ControlledBreachAlpha6736 { get; set; } = null!;

    private void RegisterAlpha6736Events()
    {
        ControlledBreachAlpha6736 = new ControlledBreachFirstEntryStoryService(
            Helper,
            Monitor,
            () => CorridorApproachAlpha6734.Stage,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6736SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6736Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6736UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_breach",
            "Controlled breach / first entry: status | reset | stage <0-5>.",
            OnAlpha6736Command);
    }

    private void OnAlpha6736SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            ControlledBreachAlpha6736.OnSaveLoaded();
    }

    private void OnAlpha6736Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            ControlledBreachAlpha6736.OnWarped(e.NewLocation);
    }

    private void OnAlpha6736UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            ControlledBreachAlpha6736.Update();
    }

    private void OnAlpha6736Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_breach.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            ControlledBreachAlpha6736.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 5)
            ControlledBreachAlpha6736.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_breach <status|reset|stage 0-5>", LogLevel.Info);
            return;
        }

        WriteAlpha6736Diagnostic();
    }

    private void WriteAlpha6736Diagnostic()
    {
        int fieldPeople = CountFieldPeopleAtAlpha6732(Game1.currentLocation);
        int npcAllies = CountActiveStoryNpcAlliesAtAlpha6732(Game1.currentLocation);
        List<string> lines = new()
        {
            "TEAM UP 6.7.36 - CONTROLLED BREACH / FIRST ENTRY",
            Origin.Describe(),
            CorridorApproachAlpha6734.Describe(Game1.currentLocation),
            ControlledBreachAlpha6736.Describe(Game1.currentLocation),
            $"Field team here: people={fieldPeople} | active Team Up NPC allies={npcAllies}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Route: Guild breach briefing -> exact saved survey face -> 180-tick controlled opening -> 120-tick threshold probe -> Guild report.",
            "First entry is deliberately a short threshold probe at the existing MineShaft face. No custom lower-workings map or boss is claimed yet.",
            "The pressure pulse after opening is a story escalation hook only. Story slot 4 remains locked.",
            "Spoiler lock: historical worker remains unnamed; George stays Rank D / Non-Combatant and unrecruitable."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Controlled_Breach_First_Entry_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Controlled breach diagnostic saved: {path}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6736.cs").write_text(entry_layer, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
if "RegisterAlpha6736Events();" not in entry:
    anchor = "        RegisterAlpha6734Events();\n"
    if anchor not in entry:
        raise RuntimeError("6.7.34 registration anchor missing")
    entry = entry.replace(anchor, anchor + "        RegisterAlpha6736Events();\n", 1)
old_loaded = "loaded. Sealed corridor approach reaction layer active."
if old_loaded not in entry:
    raise RuntimeError("6.7.35 startup message missing")
entry = entry.replace(old_loaded, "loaded. Controlled breach / first-entry probe layer active.", 1)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

en = {
    "story.breach.need-team": "The controlled breach requires at least three people here, including one active Team Up NPC ally.",
    "story.breach.missing-face": "The sealed survey face is not recorded. Complete the sealed-corridor approach before planning a breach.",
    "story.breach.briefing": "Marlon: We have a confirmed face and a pressure reading. Now we make one controlled opening, no wider than we can brace. No charges. No heroics. Keep the field team together, return to the exact survey face, and open only enough space to read what is immediately beyond the seal. If the pressure changes, you stop. If anything moves, you stop. The purpose is first entry, not conquest.",
    "story.breach.objective.reach": "Objective: return with a valid field team to the exact MineShaft survey face recorded during the pressure survey.",
    "story.breach.wrong-shaft": "This is not the recorded sealed face. Return to the MineShaft where the pressure survey was completed.",
    "story.breach.arrival": "The team finds the same newer stone face and cold mortar seam. Braces are set against the surrounding rock, tools placed along the weakest joint, and everyone takes a position where the wall can be watched without crowding it. The seal is ready for a controlled opening.",
    "story.breach.objective.stabilize": "Hold the field team together while the controlled opening is stabilized.",
    "story.breach.opened": "The joint gives by degrees instead of all at once. A narrow black gap appears behind the newer stone. Cold air pushes through, then reverses for a heartbeat as a low pressure pulse rolls out of the buried workings. Dust lifts from the floor in a thin ring. The braces hold. The opening is small, stable, and just wide enough for a careful threshold probe.",
    "story.breach.objective.probe": "Keep the field team together and hold position while the first-entry threshold probe is completed.",
    "story.breach.first-entry": "The team crosses only a few steps beyond the broken seal. The space immediately behind it is an old maintenance throat, not a natural cave: rusted rail brackets, cut timber sockets, and soot-black stone continue sideways into darkness. The familiar inward-burn trace runs along one buried support. More troubling, fine black shard residue near the threshold has been disturbed recently. Nothing attacks. Nothing is seen. But the pressure pulse repeats once from deeper inside, strong enough to tremble through the braces. The team withdraws without widening the opening.",
    "story.breach.objective.return": "First-entry probe complete. Return to Marlon at the Adventurer's Guild with the field team.",
    "story.breach.report": "Marlon listens without interrupting. The old maintenance throat proves the sealed workings are physically intact beyond the face, and the recently disturbed shard residue means the space cannot be treated as dead history. He circles the pressure-pulse note. 'That is our next problem. Opening the seal changed the response. We do not widen this breach until we understand what answered from deeper inside.'",
    "story.breach.complete": "Controlled breach complete. The threshold was probed and withdrawn from safely; the deeper lower workings remain unopened."
}

vi = {
    "story.breach.need-team": "Lần mở lối có kiểm soát cần ít nhất ba người tại đây, trong đó phải có ít nhất một NPC Team Up đang hoạt động.",
    "story.breach.missing-face": "Chưa có dữ liệu về mặt tường đã khảo sát. Hãy hoàn thành tuyến tiếp cận hành lang bị phong kín trước khi chuẩn bị mở lối.",
    "story.breach.briefing": "Marlon: Ta đã có đúng vị trí và số liệu áp lực. Bây giờ chỉ mở một khe có kiểm soát, không rộng hơn mức chúng ta có thể chống giữ. Không thuốc nổ. Không liều mạng. Giữ đội khảo sát đi cùng nhau, quay lại chính xác mặt tường đã đo và chỉ mở đủ để xem phần ngay sau lớp phong kín. Áp lực đổi thì dừng. Có thứ gì chuyển động thì dừng. Mục tiêu là bước vào lần đầu, không phải chinh phục nơi đó.",
    "story.breach.objective.reach": "Mục tiêu: đưa đội khảo sát hợp lệ quay lại đúng MineShaft nơi đã hoàn tất phép đo áp lực.",
    "story.breach.wrong-shaft": "Đây không phải mặt tường đã được ghi nhận. Hãy quay lại MineShaft nơi bạn đã hoàn tất pressure survey.",
    "story.breach.arrival": "Cả đội tìm lại đúng lớp đá mới hơn cùng khe vữa lạnh. Các thanh chống được kê vào phần đá xung quanh, dụng cụ đặt dọc theo mối nối yếu nhất, mọi người đứng ở vị trí có thể quan sát bức tường mà không dồn sát vào nhau. Mặt phong kín đã sẵn sàng để mở có kiểm soát.",
    "story.breach.objective.stabilize": "Giữ đội hình đủ người tại đây trong lúc khe mở được ổn định.",
    "story.breach.opened": "Mối nối nhả ra từng chút thay vì vỡ ập xuống. Một khe đen hẹp hiện ra sau lớp đá mới. Luồng khí lạnh phụt ra, rồi đảo chiều trong một nhịp ngắn khi một xung áp lực trầm lăn ra từ phần hầm bị chôn vùi. Bụi trên nền nhấc lên thành một vòng mỏng. Các thanh chống vẫn giữ được. Khe mở nhỏ, ổn định và vừa đủ cho một lần thăm dò ngay qua ngưỡng.",
    "story.breach.objective.probe": "Tiếp tục giữ đội hình cùng nhau trong lúc hoàn tất lần thăm dò đầu tiên qua ngưỡng mở.",
    "story.breach.first-entry": "Cả đội chỉ bước vài bước qua lớp phong kín đã mở. Phần ngay phía sau không phải hang tự nhiên mà là một lối bảo trì cũ: giá đỡ đường ray đã rỉ, hốc gỗ chống được đục vuông vức và lớp đá ám muội kéo ngang vào bóng tối. Vệt cháy hướng vào trong quen thuộc chạy dọc một thanh chống bị chôn. Đáng ngại hơn, lớp bụi vụn đen gần ngưỡng đã có dấu hiệu bị xáo trộn gần đây. Không thứ gì tấn công. Không ai nhìn thấy sinh vật nào. Nhưng một xung áp lực khác vọng lên từ sâu bên trong, đủ mạnh để rung qua các thanh chống. Cả đội rút ra mà không mở rộng khe.",
    "story.breach.objective.return": "Đã hoàn tất lần thăm dò đầu tiên. Đưa đội trở về Adventurer's Guild báo cáo với Marlon.",
    "story.breach.report": "Marlon nghe hết mà không ngắt lời. Lối bảo trì cũ chứng minh khu hầm bị phong kín vẫn còn nguyên không gian phía sau, còn lớp bụi vụn đen vừa bị xáo trộn khiến nơi đó không thể được xem như một di tích đã chết. Ông khoanh tròn ghi chú về xung áp lực. 'Đây là vấn đề tiếp theo. Việc mở lớp phong kín đã làm phản ứng thay đổi. Chúng ta sẽ không mở rộng khe này cho tới khi hiểu thứ gì ở sâu bên trong vừa đáp lại.'",
    "story.breach.complete": "Đã hoàn tất controlled breach. Cả đội thăm dò qua ngưỡng rồi rút ra an toàn; phần lower workings sâu hơn vẫn chưa được mở."
}

for filename, additions in (("default.json", en), ("vi.json", vi)):
    path = SRC / "i18n" / filename
    data = json.loads(path.read_text(encoding="utf-8"))
    collisions = sorted(set(additions).intersection(data))
    if collisions:
        raise RuntimeError(f"Localization keys already exist in {filename}: {collisions}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.36 controlled breach / first-entry probe materialized.")
