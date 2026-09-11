from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.33"
NEW = "0.2.0-alpha.6.7.34"


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
/// Alpha 6.7.34 sealed-corridor approach survey.
///
/// Field triangulation has already narrowed the old lower workings to a corridor beside the modern
/// mine network. This chapter does not breach the seal. Instead, a real three-person field team
/// approaches one MineShaft face, holds formation long enough to complete a pressure survey, and
/// reports the physical boundary back to Marlon. Story slot 4 remains reserved for a later Surge HIGH
/// milestone, and the historical miner remains unnamed.
/// </summary>
internal sealed class SealedCorridorApproachStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/SealedCorridorApproachStage";
    public const string SurveyLocationKey = "Ronvotri.TeamUp/Story/SealedCorridorSurveyLocation";
    public const int CompleteStage = 4;
    public const int MinimumFieldPeople = 3;
    public const int MinimumActiveNpcAllies = 1;
    public const int StableSurveyTicksRequired = 240;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getTriangulationStage;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private int _stage;
    private int _stableSurveyTicks;

    public SealedCorridorApproachStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getTriangulationStage,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt)
    {
        _helper = helper;
        _monitor = monitor;
        _getTriangulationStage = getTriangulationStage;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        _stableSurveyTicks = 0;
    }

    public void OnWarped(GameLocation location)
    {
        _stableSurveyTicks = 0;
        if (!CanAdvance())
            return;

        if (!HasRequiredFieldTeam(location))
        {
            if (IsRelevantLocationForCurrentStage(location))
                Hud("story.corridor.need-team");
            return;
        }

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            Show("story.corridor.briefing");
            SetStage(Game1.MasterPlayer, 1, "marlon-pressure-survey-plan");
            Hud("story.corridor.objective.reach");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            Game1.MasterPlayer.modData[SurveyLocationKey] = location.NameOrUniqueName;
            Show("story.corridor.approach-entry");
            SetStage(Game1.MasterPlayer, 2, $"approach-face:{location.NameOrUniqueName}");
            Hud("story.corridor.objective.hold");
            return;
        }

        if (_stage == 2 && location is MineShaft && !IsSurveyLocation(Game1.MasterPlayer, location))
            Hud("story.corridor.wrong-shaft");

        if (_stage == 3 && Is(location, "AdventureGuild"))
        {
            Show("story.corridor.confirmed");
            SetStage(Game1.MasterPlayer, CompleteStage, "sealed-access-face-confirmed");
            Hud("story.corridor.complete");
        }
    }

    public void Update()
    {
        if (_stage != 2 || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (location is not MineShaft
            || !IsSurveyLocation(Game1.MasterPlayer, location)
            || !HasRequiredFieldTeam(location)
            || !Context.IsPlayerFree)
        {
            _stableSurveyTicks = 0;
            return;
        }

        _stableSurveyTicks++;
        if (_stableSurveyTicks < StableSurveyTicksRequired)
            return;

        _stableSurveyTicks = 0;
        Show("story.corridor.pressure-survey");
        SetStage(Game1.MasterPlayer, 3, $"pressure-survey:{location.NameOrUniqueName}");
        Hud("story.corridor.objective.return");
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(SurveyLocationKey);
        _stage = 0;
        _stableSurveyTicks = 0;
        _monitor.Log("[SealedCorridorApproach] stage reset; triangulation and roster progress were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        if (clamped <= 1)
            owner.modData.Remove(SurveyLocationKey);
        else if (clamped == 2 && !owner.modData.ContainsKey(SurveyLocationKey) && Game1.currentLocation is MineShaft)
            owner.modData[SurveyLocationKey] = Game1.currentLocation.NameOrUniqueName;

        _stableSurveyTicks = 0;
        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string surveyLocation = Game1.MasterPlayer.modData.TryGetValue(SurveyLocationKey, out string? stored)
            ? stored
            : "none";
        string objective = _stage switch
        {
            0 when _getTriangulationStage() < FieldTriangulationStoryService.CompleteStage => "finish-field-triangulation",
            0 => "brief-with-marlon-at-guild",
            1 => "approach-triangulated-mine-face",
            2 => "hold-field-team-for-pressure-survey",
            3 => "return-to-marlon",
            _ => "sealed-access-face-confirmed"
        };

        return $"Sealed Corridor Approach: triangulationStage={_getTriangulationStage()}/4 | stage={_stage}/{CompleteStage} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)} | npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)} | "
            + $"surveyLocation={surveyLocation} | stableTicks={_stableSurveyTicks}/{StableSurveyTicksRequired} | objective={objective}";
    }

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getTriangulationStage() >= FieldTriangulationStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasRequiredFieldTeam(GameLocation location)
        => _getFieldPeopleAt(location) >= MinimumFieldPeople
            && _getActiveNpcAlliesAt(location) >= MinimumActiveNpcAllies;

    private bool IsRelevantLocationForCurrentStage(GameLocation location)
        => (_stage is 0 or 3 && Is(location, "AdventureGuild"))
            || (_stage is 1 or 2 && location is MineShaft);

    private static bool IsSurveyLocation(Farmer owner, GameLocation location)
        => owner.modData.TryGetValue(SurveyLocationKey, out string? stored)
            && !string.IsNullOrWhiteSpace(stored)
            && stored.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[SealedCorridorApproach] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
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
(SRC / "Story" / "SealedCorridorApproachStoryService.cs").write_text(service, encoding="utf-8", newline="\n")

entry = r'''using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private SealedCorridorApproachStoryService CorridorApproachAlpha6734 { get; set; } = null!;

    private void RegisterAlpha6734Events()
    {
        CorridorApproachAlpha6734 = new SealedCorridorApproachStoryService(
            Helper,
            Monitor,
            () => FieldTriangulationAlpha6732.Stage,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6734SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6734Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6734UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_corridor",
            "Sealed corridor approach: status | reset | stage <0-4>.",
            OnAlpha6734Command);
    }

    private void OnAlpha6734SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            CorridorApproachAlpha6734.OnSaveLoaded();
    }

    private void OnAlpha6734Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            CorridorApproachAlpha6734.OnWarped(e.NewLocation);
    }

    private void OnAlpha6734UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            CorridorApproachAlpha6734.Update();
    }

    private void OnAlpha6734Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_corridor.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            CorridorApproachAlpha6734.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 4)
            CorridorApproachAlpha6734.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_corridor <status|reset|stage 0-4>", LogLevel.Info);
            return;
        }

        WriteAlpha6734Diagnostic();
    }

    private void WriteAlpha6734Diagnostic()
    {
        int fieldPeople = CountFieldPeopleAtAlpha6732(Game1.currentLocation);
        int npcAllies = CountActiveStoryNpcAlliesAtAlpha6732(Game1.currentLocation);
        List<string> lines = new()
        {
            "TEAM UP 6.7.34 - SEALED CORRIDOR APPROACH",
            Origin.Describe(),
            FieldTriangulationAlpha6732.Describe(Game1.currentLocation),
            CorridorApproachAlpha6734.Describe(Game1.currentLocation),
            $"Field team here: people={fieldPeople} | active Team Up NPC allies={npcAllies}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Route: Guild pressure-survey briefing -> MineShaft approach face -> hold a valid field team for 240 ticks -> Guild report.",
            "This checkpoint confirms the sealed access face only. It does not breach the wall and does not unlock story slot 4.",
            "Spoiler lock: historical worker remains unnamed; George stays Rank D / Non-Combatant and unrecruitable."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Sealed_Corridor_Approach_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Sealed corridor approach diagnostic saved: {path}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6734.cs").write_text(entry, encoding="utf-8", newline="\n")

mod_entry_path = SRC / "ModEntry.cs"
mod_entry = mod_entry_path.read_text(encoding="utf-8")
needle = "        RegisterAlpha6732Events();\n        RegisterAlpha6720Events();"
replacement = "        RegisterAlpha6732Events();\n        RegisterAlpha6734Events();\n        RegisterAlpha6720Events();"
if needle not in mod_entry:
    raise RuntimeError("Alpha6732 registration anchor missing")
mod_entry = mod_entry.replace(needle, replacement, 1)
old_loaded = "loaded. Field triangulation reaction layer active."
if old_loaded not in mod_entry:
    raise RuntimeError("6.7.33 startup message missing")
mod_entry = mod_entry.replace(old_loaded, "loaded. Sealed corridor approach layer active.", 1)
mod_entry_path.write_text(mod_entry, encoding="utf-8", newline="\n")

en = {
    "story.corridor.need-team": "Marlon wants the full field team present: at least three people here, including one active Team Up ally.",
    "story.corridor.briefing": "Marlon: The bearings give us a corridor, not a safe door. Before anyone swings a pick, we check the boundary. Take the field team into the modern mine and approach the face nearest our triangulated zone. Hold formation long enough to read the draft, timber strain, and stone pressure. We confirm the seal first. We do not breach it.",
    "story.corridor.objective.reach": "Objective: take a valid three-person field team into a MineShaft and approach the suspected sealed face.",
    "story.corridor.approach-entry": "The team slows beside a section of newer stone. A thin cold draft slips sideways through a mortar seam. Beneath the newer reinforcement, an older timber scar cuts across the wall at the wrong angle. When the mine settles, the rock answers with a shallow hollow vibration from behind the face.",
    "story.corridor.objective.hold": "Hold the field team together here while the pressure survey stabilizes.",
    "story.corridor.wrong-shaft": "This is not the survey face you marked. Return to the MineShaft where the approach reading began.",
    "story.corridor.pressure-survey": "The reading settles. The draft does not behave like an ordinary cave-in, and the stress line runs laterally instead of down the active shaft. A faint inward-burn trace follows an obsolete support seam behind the newer wall. Whatever lies beyond this face belongs to older workings, and the seal is still carrying pressure. The team has found the boundary without opening it.",
    "story.corridor.objective.return": "Pressure survey complete. Return to Marlon at the Adventurer's Guild with the field team.",
    "story.corridor.confirmed": "Marlon studies the readings twice. The airflow, timber scar, and lateral stress all agree. This is the buried access face leading toward the sealed lower workings. He marks it on the map, then folds the page shut. 'We have the approach. That does not mean we have permission to open it. Next time we go there, we prepare for what a failed seal can do.'",
    "story.corridor.complete": "Sealed corridor approach confirmed. The access face is mapped, but the breach has not begun."
}

vi = {
    "story.corridor.need-team": "Marlon muốn đủ đội khảo sát: ít nhất ba người phải có mặt tại đây, trong đó có ít nhất một đồng đội Team Up đang hoạt động.",
    "story.corridor.briefing": "Marlon: Hai đường đo đã cho ta một hành lang, chứ chưa cho ta một cánh cửa an toàn. Trước khi bất kỳ ai vung cuốc, ta phải kiểm tra ranh giới. Đưa đội khảo sát vào khu mỏ hiện tại và tiếp cận mặt tường gần vùng đã khoanh nhất. Giữ đội hình đủ lâu để đọc luồng gió, sức căng của gỗ chống và áp lực trong đá. Ta xác nhận lớp phong kín trước. Chưa phá nó.",
    "story.corridor.objective.reach": "Mục tiêu: đưa đội khảo sát hợp lệ gồm ba người vào MineShaft và tiếp cận mặt tường nghi là lối phong kín.",
    "story.corridor.approach-entry": "Cả đội chậm lại bên một đoạn tường đá có vẻ mới hơn phần xung quanh. Một luồng khí lạnh mảnh lách ngang qua đường vữa. Bên dưới lớp gia cố mới, vết gỗ chống cũ cắt chéo theo một góc không thuộc cấu trúc hiện tại. Khi lòng mỏ khẽ chuyển mình, khối đá đáp lại bằng một tiếng rung rỗng nông từ phía sau mặt tường.",
    "story.corridor.objective.hold": "Giữ đội khảo sát tập trung tại đây trong khi phép đo áp lực ổn định.",
    "story.corridor.wrong-shaft": "Đây không phải mặt tường khảo sát đã đánh dấu. Hãy quay lại MineShaft nơi phép đo tiếp cận bắt đầu.",
    "story.corridor.pressure-survey": "Số đo dần ổn định. Luồng khí không giống một vụ sập hầm thông thường, còn đường ứng suất chạy ngang thay vì dọc theo trục mỏ đang hoạt động. Một vệt cháy hướng vào trong rất mờ bám theo đường nối của hệ chống cũ phía sau bức tường mới. Thứ nằm bên kia mặt tường này thuộc về những đường hầm cũ, và lớp phong kín vẫn đang chịu áp lực. Cả đội đã tìm thấy ranh giới mà chưa cần mở nó.",
    "story.corridor.objective.return": "Đã hoàn tất khảo sát áp lực. Hãy đưa đội trở lại Adventurer's Guild gặp Marlon.",
    "story.corridor.confirmed": "Marlon xem các số đo hai lần. Luồng khí, vết gỗ chống và đường ứng suất ngang đều khớp nhau. Đây chính là mặt tiếp cận bị chôn, dẫn về khu lower workings đã bị phong kín. Ông đánh dấu nó lên bản đồ rồi gấp tờ giấy lại. 'Ta đã tìm được lối tiếp cận. Không có nghĩa là ta được phép mở nó. Lần tới xuống đó, ta phải chuẩn bị cho chuyện một lớp phong kín thất bại có thể gây ra.'",
    "story.corridor.complete": "Đã xác nhận lối tiếp cận hành lang phong kín. Mặt tường đã được đánh dấu, nhưng việc phá phong vẫn chưa bắt đầu."
}

for locale_name, additions in [("default.json", en), ("vi.json", vi)]:
    path = SRC / "i18n" / locale_name
    data = json.loads(path.read_text(encoding="utf-8"))
    for key in additions:
        if key in data:
            raise RuntimeError(f"Localization key already exists in {locale_name}: {key}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.34 sealed-corridor approach materialized.")
