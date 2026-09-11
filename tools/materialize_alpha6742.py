from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.41"
NEW = "0.2.0-alpha.6.7.42"


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
/// Alpha 6.7.42 first real Lower Workings operation.
///
/// Alpha 6.7.40 proved that the HIGH-response formation can hold a safe staging and withdrawal line.
/// This checkpoint authorizes the team to cross the no-pursuit threshold through the already-braced
/// controlled breach, then inspect only the first interior threshold zone. Until a dedicated Lower
/// Workings map exists, that threshold zone is represented by persistent story state at the exact
/// recorded breach face rather than by pretending a new dungeon map already exists.
/// </summary>
internal sealed class LowerWorkingsDescentStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsDescentStage";
    public const string ThresholdCrossedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed";
    public const string FirstDescentCompleteFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete";
    public const int CompleteStage = 5;
    public const int ThresholdCrossingTicksRequired = 120;
    public const int ThresholdInspectionTicksRequired = 180;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getEntryProtocolStage;
    private readonly Func<bool> _isEntryProtocolReady;
    private readonly Func<bool> _isSurgeHigh;
    private readonly Func<int> _getUnlockedNpcSlots;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private readonly Func<int> _getRequiredFieldPeople;
    private readonly Func<int> _getRequiredNpcAllies;
    private int _stage;
    private int _crossingTicks;
    private int _inspectionTicks;

    public LowerWorkingsDescentStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getEntryProtocolStage,
        Func<bool> isEntryProtocolReady,
        Func<bool> isSurgeHigh,
        Func<int> getUnlockedNpcSlots,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt,
        Func<int> getRequiredFieldPeople,
        Func<int> getRequiredNpcAllies)
    {
        _helper = helper;
        _monitor = monitor;
        _getEntryProtocolStage = getEntryProtocolStage;
        _isEntryProtocolReady = isEntryProtocolReady;
        _isSurgeHigh = isSurgeHigh;
        _getUnlockedNpcSlots = getUnlockedNpcSlots;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
        _getRequiredFieldPeople = getRequiredFieldPeople;
        _getRequiredNpcAllies = getRequiredNpcAllies;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public bool ThresholdCrossed
        => Context.IsWorldReady
            && Game1.MasterPlayer.modData.TryGetValue(ThresholdCrossedFlagKey, out string? value)
            && value == "1";

    public bool FirstDescentComplete
        => Context.IsWorldReady
            && Game1.MasterPlayer.modData.TryGetValue(FirstDescentCompleteFlagKey, out string? value)
            && value == "1";

    public void OnSaveLoaded()
    {
        _stage = ReadStage(Game1.MasterPlayer);
        _crossingTicks = 0;
        _inspectionTicks = 0;
        if (_stage >= 3)
            Game1.MasterPlayer.modData[ThresholdCrossedFlagKey] = "1";
        if (_stage >= CompleteStage)
            Game1.MasterPlayer.modData[FirstDescentCompleteFlagKey] = "1";
    }

    public void OnWarped(GameLocation location)
    {
        _crossingTicks = 0;
        _inspectionTicks = 0;
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (!PrerequisitesReady())
        {
            if (Is(location, "AdventureGuild"))
                Hud("story.descent.need-protocol");
            return;
        }

        if (!CanPresent())
            return;

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            if (!TryGetBreachLocation(Game1.MasterPlayer, out _))
            {
                Hud("story.descent.missing-face");
                return;
            }

            Show("story.descent.briefing");
            SetStage(Game1.MasterPlayer, 1, "lower-workings-first-descent-authorized");
            Hud("story.descent.objective.reach");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
            {
                Hud("story.descent.wrong-shaft");
                return;
            }

            if (!HasFullOperationalFormation(location))
            {
                Hud("story.descent.need-full-team");
                return;
            }

            Show("story.descent.threshold-ready");
            SetStage(Game1.MasterPlayer, 2, $"threshold-crossing-line-ready:{location.NameOrUniqueName}");
            Hud("story.descent.objective.cross");
            return;
        }

        if ((_stage == 2 || _stage == 3) && location is MineShaft)
        {
            if (!IsBreachLocation(Game1.MasterPlayer, location))
                Hud("story.descent.wrong-shaft");
            else if (!HasFullOperationalFormation(location))
                Hud("story.descent.need-full-team");
            return;
        }

        if (_stage == 4 && Is(location, "AdventureGuild"))
        {
            Show("story.descent.report");
            Game1.MasterPlayer.modData[FirstDescentCompleteFlagKey] = "1";
            SetStage(Game1.MasterPlayer, CompleteStage, "first-lower-workings-descent-reported");
            Hud("story.descent.complete");
        }
    }

    public void Update()
    {
        if ((_stage != 2 && _stage != 3) || !CanAdvance())
            return;

        GameLocation location = Game1.currentLocation;
        if (location is not MineShaft
            || !IsBreachLocation(Game1.MasterPlayer, location)
            || !HasFullOperationalFormation(location)
            || !Context.IsPlayerFree)
        {
            _crossingTicks = 0;
            _inspectionTicks = 0;
            return;
        }

        if (_stage == 2)
        {
            _crossingTicks++;
            if (_crossingTicks < ThresholdCrossingTicksRequired)
                return;

            _crossingTicks = 0;
            Game1.MasterPlayer.modData[ThresholdCrossedFlagKey] = "1";
            Show("story.descent.crossed");
            SetStage(Game1.MasterPlayer, 3, $"lower-workings-threshold-crossed:{location.NameOrUniqueName}");
            Hud("story.descent.objective.inspect");
            return;
        }

        _inspectionTicks++;
        if (_inspectionTicks < ThresholdInspectionTicksRequired)
            return;

        _inspectionTicks = 0;
        Show("story.descent.evidence");
        SetStage(Game1.MasterPlayer, 4, $"first-threshold-zone-inspected:{location.NameOrUniqueName}");
        Hud("story.descent.objective.return");
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(ThresholdCrossedFlagKey);
        owner.modData.Remove(FirstDescentCompleteFlagKey);
        _stage = 0;
        _crossingTicks = 0;
        _inspectionTicks = 0;
        _monitor.Log("[LowerWorkingsDescent] reset; Entry Protocol, SURGE HIGH, and roster progression were not changed.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        _crossingTicks = 0;
        _inspectionTicks = 0;

        if (clamped >= 3)
            owner.modData[ThresholdCrossedFlagKey] = "1";
        else
            owner.modData.Remove(ThresholdCrossedFlagKey);

        if (clamped >= CompleteStage)
            owner.modData[FirstDescentCompleteFlagKey] = "1";
        else
            owner.modData.Remove(FirstDescentCompleteFlagKey);

        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string breachLocation = TryGetBreachLocation(Game1.MasterPlayer, out string? stored) ? stored! : "none";
        string objective = _stage switch
        {
            0 when _getEntryProtocolStage() < LowerWorkingsEntryProtocolStoryService.CompleteStage || !_isEntryProtocolReady() => "finish-entry-protocol",
            0 when !_isSurgeHigh() => "restore-surge-high-state",
            0 when _getUnlockedNpcSlots() < TeamUpRosterProgressionService.MaxStoryNpcSlots => "restore-story-slot4-authorization",
            0 => "receive-first-descent-order-at-guild",
            1 => "assemble-full-formation-at-recorded-breach-face",
            2 => "hold-full-formation-and-cross-threshold",
            3 => "inspect-first-lower-threshold-zone",
            4 => "return-to-guild-and-report-first-descent",
            _ => "first-lower-workings-descent-complete"
        };

        return $"Lower Workings Descent: entryProtocol={_getEntryProtocolStage()}/{LowerWorkingsEntryProtocolStoryService.CompleteStage} | "
            + $"protocolReady={_isEntryProtocolReady()} | high={_isSurgeHigh()} | stage={_stage}/{CompleteStage} | "
            + $"thresholdCrossed={ThresholdCrossed} | firstDescentComplete={FirstDescentComplete} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)}/{_getRequiredFieldPeople()} | "
            + $"npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)}/{_getRequiredNpcAllies()} | breachLocation={breachLocation} | "
            + $"crossingTicks={_crossingTicks}/{ThresholdCrossingTicksRequired} | inspectionTicks={_inspectionTicks}/{ThresholdInspectionTicksRequired} | objective={objective}";
    }

    private bool PrerequisitesReady()
        => _getEntryProtocolStage() >= LowerWorkingsEntryProtocolStoryService.CompleteStage
            && _isEntryProtocolReady()
            && _isSurgeHigh()
            && _getUnlockedNpcSlots() >= TeamUpRosterProgressionService.MaxStoryNpcSlots;

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && PrerequisitesReady()
            && CanPresent();

    private bool CanPresent()
        => !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasFullOperationalFormation(GameLocation location)
        => _getFieldPeopleAt(location) >= _getRequiredFieldPeople()
            && _getActiveNpcAlliesAt(location) >= _getRequiredNpcAllies();

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
        _monitor.Log($"[LowerWorkingsDescent] stage -> {_stage}/{CompleteStage} crossed={ThresholdCrossed} complete={FirstDescentComplete} source={source}.", LogLevel.Info);
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
(SRC / "Story" / "LowerWorkingsDescentStoryService.cs").write_text(service, encoding="utf-8", newline="\n")

entry_layer = r'''using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private LowerWorkingsDescentStoryService LowerWorkingsDescentAlpha6742 { get; set; } = null!;

    private void RegisterAlpha6742Events()
    {
        LowerWorkingsDescentAlpha6742 = new LowerWorkingsDescentStoryService(
            Helper,
            Monitor,
            () => EntryProtocolAlpha6740.Stage,
            () => EntryProtocolAlpha6740.ProtocolReady,
            () => SurgeHighAlpha6738.IsHigh,
            () => RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732,
            () => EntryProtocolAlpha6740.GetRequiredFieldPeople(),
            () => EntryProtocolAlpha6740.GetRequiredNpcAllies());

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6742SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6742Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6742UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_lower_descent",
            "Lower Workings first descent: status | reset | stage <0-5>.",
            OnAlpha6742Command);
    }

    private void OnAlpha6742SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            LowerWorkingsDescentAlpha6742.OnSaveLoaded();
    }

    private void OnAlpha6742Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            LowerWorkingsDescentAlpha6742.OnWarped(e.NewLocation);
    }

    private void OnAlpha6742UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            LowerWorkingsDescentAlpha6742.Update();
    }

    private void OnAlpha6742Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_lower_descent.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            LowerWorkingsDescentAlpha6742.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 5)
            LowerWorkingsDescentAlpha6742.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_lower_descent <status|reset|stage 0-5>", LogLevel.Info);
            return;
        }

        WriteAlpha6742Diagnostic();
    }

    private void WriteAlpha6742Diagnostic()
    {
        int unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        List<string> lines = new()
        {
            "TEAM UP 6.7.42 - LOWER WORKINGS DESCENT / THRESHOLD CROSSING",
            Origin.Describe(),
            EntryProtocolAlpha6740.Describe(Game1.currentLocation),
            LowerWorkingsDescentAlpha6742.Describe(Game1.currentLocation),
            $"Story roster: unlocked={unlocked}/4 | effectiveNow={GetEffectiveNpcSlotLimitAlpha6727()} | activeNpcAllies={GetActiveStoryNpcCountAlpha6727()}",
            $"Full operational formation requirement: people={EntryProtocolAlpha6740.GetRequiredFieldPeople()} | activeNpcAllies={EntryProtocolAlpha6740.GetRequiredNpcAllies()}.",
            "Route: Entry Protocol READY -> Guild descent order -> exact breach face with full formation -> 120-tick threshold crossing -> 180-tick first interior inspection -> Guild report.",
            "Threshold crossing and first-descent completion are persisted separately so a future dedicated Lower Workings map can inherit the state cleanly.",
            "Physical evidence now supports deliberate emergency containment: directed cribbing, shaped blast scoring, and a collapse pattern designed to close the passage. Historical worker identity remains unknown.",
            "No custom dungeon map, final boss, George reveal, or roster expansion is introduced by this checkpoint."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Lower_Workings_Descent_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Lower Workings descent diagnostic saved: {path}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6742.cs").write_text(entry_layer, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
needle = "        RegisterAlpha6740Events();\n"
if needle not in entry:
    raise RuntimeError("RegisterAlpha6740Events marker missing")
entry = entry.replace(needle, needle + "        RegisterAlpha6742Events();\n", 1)
entry = entry.replace(
    "Team Up! v{ModManifest.Version} loaded. Entry Protocol reaction layer active.",
    "Team Up! v{ModManifest.Version} loaded. Lower Workings descent / threshold crossing layer active.",
    1,
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

EN = {
    "story.descent.need-protocol": "Marlon: Not yet. SURGE HIGH is still active, but nobody crosses that breach until the Entry Protocol is READY and the full response roster is authorized.",
    "story.descent.missing-face": "Marlon: The recorded breach face is missing from our field record. Re-establish the controlled-breach route before we attempt a descent.",
    "story.descent.briefing": "Marlon: Protocol is READY. This is the first real descent, and it stays small. Full formation to the recorded breach. Cross only when the withdrawal line is stable. Nobody pursues anything beyond the first threshold zone. If the formation breaks, you come back out. We are proving we can enter and leave, not conquering the lower workings tonight.",
    "story.descent.objective.reach": "Objective updated: assemble the full operational formation at the recorded breach face.",
    "story.descent.wrong-shaft": "This is not the recorded controlled-breach face. Return to the exact MineShaft used by the Entry Protocol.",
    "story.descent.need-full-team": "The first descent requires the full operational formation for the current multiplayer party size.",
    "story.descent.threshold-ready": "The full formation locks into the staging line. The braced opening is stable, the rear anchor has the supports, and the withdrawal route is clear. The no-pursuit threshold is now in front of you. Hold formation and cross together.",
    "story.descent.objective.cross": "Objective updated: hold the full formation through the 120-tick threshold crossing.",
    "story.descent.crossed": "The team passes one by one through the narrow braced opening. The air beyond is colder and older, carrying coal dust that has not moved in years. Behind you, the withdrawal line remains visible. For the first time, Team Up is physically beyond the old seal.",
    "story.descent.objective.inspect": "Objective updated: hold the threshold zone for 180 ticks and inspect only the first interior section.",
    "story.descent.evidence": "The first interior section tells a clearer story. Old timber cribbing is not scattered by a random cave-in; it is wedged to steer a collapse across the passage. Blast cups score the rock in a deliberate fan, and charred support seams follow the closing line. Someone turned this corridor into a barrier under emergency pressure. The evidence says containment. It still does not say who did it, or what they were trying to hold back.",
    "story.descent.objective.return": "Objective updated: withdraw along the marked line and report the first descent to Marlon.",
    "story.descent.report": "Marlon: Good. You crossed, held the line, read the first threshold zone, and came back without chasing the unknown. The old accident story is getting harder to defend. That collapse was shaped into a seal. We now have enough to plan a dedicated lower-workings operation, but we still do not know who built the containment or what forced them to do it.",
    "story.descent.complete": "First Lower Workings descent complete. Threshold-crossed and first-descent state saved."
}

VI = {
    "story.descent.need-protocol": "Marlon: Chưa được. SURGE HIGH vẫn còn, nhưng không ai được vượt qua khe đó cho tới khi Entry Protocol ở trạng thái READY và đội phản ứng đầy đủ đã được cho phép.",
    "story.descent.missing-face": "Marlon: Điểm breach đã ghi trước đó không còn trong hồ sơ hiện trường. Hãy xác lập lại tuyến controlled breach trước khi thử xuống sâu.",
    "story.descent.briefing": "Marlon: Protocol đã READY. Đây là lần xuống Lower Workings thật sự đầu tiên, và ta sẽ giữ phạm vi thật nhỏ. Đưa full formation tới đúng breach face đã ghi. Chỉ vượt qua khi tuyến rút lui ổn định. Không ai được đuổi theo bất cứ thứ gì vượt quá khu threshold đầu tiên. Đội hình vỡ là rút ra ngay. Tối nay ta chứng minh mình có thể vào rồi trở ra, không phải đi chinh phục cả khu hầm dưới.",
    "story.descent.objective.reach": "Mục tiêu mới: tập hợp full operational formation tại đúng breach face đã ghi.",
    "story.descent.wrong-shaft": "Đây không phải controlled-breach face đã ghi. Hãy quay lại đúng MineShaft dùng trong Entry Protocol.",
    "story.descent.need-full-team": "Lần xuống đầu tiên yêu cầu full operational formation phù hợp với số Farmer multiplayer hiện tại.",
    "story.descent.threshold-ready": "Full formation vào đúng staging line. Khe mở đã được chống giữ ổn định, rear anchor đang canh hệ chống và tuyến rút lui đã thông. No-pursuit threshold nằm ngay phía trước. Giữ đội hình và cùng nhau vượt qua.",
    "story.descent.objective.cross": "Mục tiêu mới: giữ full formation xuyên suốt 120 tick để vượt threshold.",
    "story.descent.crossed": "Cả đội lần lượt lách qua khe mở hẹp đã được chống giữ. Không khí phía bên kia lạnh và cũ hơn, mang theo bụi than đã nằm yên nhiều năm. Phía sau, tuyến rút lui vẫn còn trong tầm mắt. Lần đầu tiên Team Up đã thật sự đứng bên kia lớp phong kín cũ.",
    "story.descent.objective.inspect": "Mục tiêu mới: giữ khu threshold trong 180 tick và chỉ khảo sát phần bên trong đầu tiên.",
    "story.descent.evidence": "Phần bên trong đầu tiên kể một câu chuyện rõ hơn. Các thanh gỗ chống cũ không hề văng ngẫu nhiên do sập hầm; chúng được chèn để hướng khối sập chắn ngang lối đi. Dấu hốc nổ xòe theo một hướng có chủ đích, còn vết cháy trên hệ chống chạy dọc theo đường phong kín. Có người đã biến hành lang này thành một bức chắn trong tình huống khẩn cấp. Bằng chứng nói rằng đây là containment có chủ ý. Nó vẫn chưa cho biết ai đã làm, hay họ đang cố giữ thứ gì ở phía sau.",
    "story.descent.objective.return": "Mục tiêu mới: rút theo tuyến đã đánh dấu và báo cáo lần xuống đầu tiên cho Marlon.",
    "story.descent.report": "Marlon: Tốt. Các cậu đã vượt qua, giữ được tuyến, đọc khu threshold đầu tiên và quay ra mà không đuổi theo điều chưa biết. Câu chuyện 'tai nạn công nghiệp' giờ càng khó đứng vững. Vụ sập đó đã được tạo hình thành một lớp phong kín. Ta đã có đủ dữ kiện để chuẩn bị một chiến dịch Lower Workings riêng, nhưng vẫn chưa biết ai dựng lớp containment đó hay điều gì đã buộc họ phải làm vậy.",
    "story.descent.complete": "Lần xuống Lower Workings đầu tiên hoàn tất. Trạng thái threshold-crossed và first-descent đã được lưu."
}

for rel, additions in [("i18n/default.json", EN), ("i18n/vi.json", VI)]:
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(additions).intersection(data)
    if overlap:
        raise RuntimeError(f"Unexpected existing 6.7.42 localization keys in {rel}: {sorted(overlap)}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.42 Lower Workings descent / threshold crossing materialized.")
