from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.31"
NEW = "0.2.0-alpha.6.7.32"


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
/// Alpha 6.7.32 physical field-triangulation chapter.
///
/// The route begins only after the Old Mine Connection is confirmed. It requires a real field team:
/// at least three people physically present in the same location, with at least one active Team Up
/// NPC ally. Two different MineShaft locations must be sampled before Marlon can triangulate the
/// likely sealed-workings corridor. This chapter does not unlock story slot 4 and does not reveal
/// the historical miner's identity.
/// </summary>
internal sealed class FieldTriangulationStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/FieldTriangulationStage";
    public const string FirstBearingLocationKey = "Ronvotri.TeamUp/Story/FieldTriangulationFirstBearing";
    public const int CompleteStage = 4;
    public const int MinimumFieldPeople = 3;
    public const int MinimumActiveNpcAllies = 1;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getOldMineStage;
    private readonly Func<GameLocation, int> _getFieldPeopleAt;
    private readonly Func<GameLocation, int> _getActiveNpcAlliesAt;
    private int _stage;

    public FieldTriangulationStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getOldMineStage,
        Func<GameLocation, int> getFieldPeopleAt,
        Func<GameLocation, int> getActiveNpcAlliesAt)
    {
        _helper = helper;
        _monitor = monitor;
        _getOldMineStage = getOldMineStage;
        _getFieldPeopleAt = getFieldPeopleAt;
        _getActiveNpcAlliesAt = getActiveNpcAlliesAt;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded()
        => _stage = ReadStage(Game1.MasterPlayer);

    public void OnWarped(GameLocation location)
    {
        if (!CanAdvance())
            return;

        if (!HasRequiredFieldTeam(location))
        {
            if (IsRelevantLocationForCurrentStage(location))
                Hud("story.triangulation.need-team");
            return;
        }

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            Show("story.triangulation.briefing");
            SetStage(Game1.MasterPlayer, 1, "marlon-field-plan");
            Hud("story.triangulation.objective.first-bearing");
            return;
        }

        if (_stage == 1 && location is MineShaft)
        {
            Game1.MasterPlayer.modData[FirstBearingLocationKey] = location.NameOrUniqueName;
            Show("story.triangulation.bearing-one");
            SetStage(Game1.MasterPlayer, 2, $"first-bearing:{location.NameOrUniqueName}");
            Hud("story.triangulation.objective.second-bearing");
            return;
        }

        if (_stage == 2 && location is MineShaft)
        {
            if (!IsDifferentMineLocation(Game1.MasterPlayer, location))
            {
                Hud("story.triangulation.same-shaft");
                return;
            }

            Show("story.triangulation.bearing-two");
            SetStage(Game1.MasterPlayer, 3, $"second-bearing:{location.NameOrUniqueName}");
            Hud("story.triangulation.objective.return");
            return;
        }

        if (_stage == 3 && Is(location, "AdventureGuild"))
        {
            Show("story.triangulation.confirmed");
            SetStage(Game1.MasterPlayer, CompleteStage, "sealed-workings-triangulated");
            Hud("story.triangulation.complete");
        }
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        owner.modData.Remove(FirstBearingLocationKey);
        _stage = 0;
        _monitor.Log("[FieldTriangulation] stage reset; old-mine and roster progress were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
    {
        int clamped = Math.Clamp(stage, 0, CompleteStage);
        if (clamped <= 1)
            owner.modData.Remove(FirstBearingLocationKey);
        SetStage(owner, clamped, "debug");
    }

    public string Describe(GameLocation currentLocation)
    {
        string firstBearing = Game1.MasterPlayer.modData.TryGetValue(FirstBearingLocationKey, out string? stored)
            ? stored
            : "none";
        string objective = _stage switch
        {
            0 when _getOldMineStage() < OldMineConnectionStoryService.CompleteStage => "finish-old-mine-connection",
            0 => "assemble-field-team-at-guild",
            1 => "sample-first-mineshaft-bearing",
            2 => "sample-different-mineshaft-bearing",
            3 => "return-to-marlon-with-field-team",
            _ => "sealed-workings-corridor-triangulated"
        };

        return $"Field Triangulation: oldMineStage={_getOldMineStage()}/3 | stage={_stage}/{CompleteStage} | "
            + $"fieldPeopleHere={_getFieldPeopleAt(currentLocation)} | npcAlliesHere={_getActiveNpcAlliesAt(currentLocation)} | "
            + $"firstBearing={firstBearing} | objective={objective}";
    }

    private bool CanAdvance()
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getOldMineStage() >= OldMineConnectionStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private bool HasRequiredFieldTeam(GameLocation location)
        => _getFieldPeopleAt(location) >= MinimumFieldPeople
            && _getActiveNpcAlliesAt(location) >= MinimumActiveNpcAllies;

    private bool IsRelevantLocationForCurrentStage(GameLocation location)
        => (_stage is 0 or 3 && Is(location, "AdventureGuild"))
            || (_stage is 1 or 2 && location is MineShaft);

    private static bool IsDifferentMineLocation(Farmer owner, GameLocation location)
        => !owner.modData.TryGetValue(FirstBearingLocationKey, out string? first)
            || string.IsNullOrWhiteSpace(first)
            || !first.Equals(location.NameOrUniqueName, StringComparison.OrdinalIgnoreCase);

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[FieldTriangulation] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
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
(SRC / "Story" / "FieldTriangulationStoryService.cs").write_text(service, encoding="utf-8", newline="\n")

integration = r'''using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private FieldTriangulationStoryService FieldTriangulationAlpha6732 { get; set; } = null!;

    private void RegisterAlpha6732Events()
    {
        FieldTriangulationAlpha6732 = new FieldTriangulationStoryService(
            Helper,
            Monitor,
            () => OldMineConnectionAlpha6730.Stage,
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6732SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6732Warped;
        Helper.ConsoleCommands.Add(
            "teamup_triangulation",
            "Field triangulation: status | reset | stage <0-4>.",
            OnAlpha6732Command);
    }

    private void OnAlpha6732SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            FieldTriangulationAlpha6732.OnSaveLoaded();
    }

    private void OnAlpha6732Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            FieldTriangulationAlpha6732.OnWarped(e.NewLocation);
    }

    private int CountActiveStoryNpcAlliesAtAlpha6732(GameLocation location)
    {
        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        return Party.Members
            .Where(member => online.Contains(member.RecruiterId))
            .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .Select(member => member.CharacterName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(name => Game1.getCharacterFromName(name)?.currentLocation == location);
    }

    private int CountFieldPeopleAtAlpha6732(GameLocation location)
    {
        int farmers = Game1.getOnlineFarmers().Count(farmer => farmer.currentLocation == location);
        return farmers + CountActiveStoryNpcAlliesAtAlpha6732(location);
    }

    private void OnAlpha6732Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_triangulation.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            FieldTriangulationAlpha6732.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 4)
            FieldTriangulationAlpha6732.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_triangulation <status|reset|stage 0-4>", LogLevel.Info);
            return;
        }

        WriteAlpha6732Diagnostic();
    }

    private void WriteAlpha6732Diagnostic()
    {
        int fieldPeople = CountFieldPeopleAtAlpha6732(Game1.currentLocation);
        int npcAllies = CountActiveStoryNpcAlliesAtAlpha6732(Game1.currentLocation);
        List<string> lines = new()
        {
            "TEAM UP 6.7.32 - FIELD TRIANGULATION",
            Origin.Describe(),
            MarlonInvestigationAlpha6729.Describe(),
            OldMineConnectionAlpha6730.Describe(),
            FieldTriangulationAlpha6732.Describe(Game1.currentLocation),
            $"Field team here: people={fieldPeople} | active Team Up NPC allies={npcAllies}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Required at each gate: at least 3 people physically present here, including at least 1 active Team Up NPC ally.",
            "Route: Guild field plan -> MineShaft bearing A -> different MineShaft bearing B -> Guild triangulation.",
            "Payoff: sealed-workings corridor narrowed. Story slot 4 remains locked for a later Surge HIGH / major-chapter milestone.",
            "Spoiler lock: historical worker remains unnamed; George stays Rank D / Non-Combatant and unrecruitable."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Field_Triangulation_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Field triangulation diagnostic saved: {path}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6732.cs").write_text(integration, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
if "RegisterAlpha6732Events();" not in entry:
    needle = "        RegisterAlpha6730Events();\n"
    if needle not in entry:
        raise RuntimeError("Alpha 6.7.30 registration point missing")
    entry = entry.replace(needle, needle + "        RegisterAlpha6732Events();\n", 1)
entry = entry.replace(
    "loaded. Old mine milestone reaction layer active.",
    "loaded. Field triangulation layer active.",
    1,
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

en = {
    "story.triangulation.briefing": "Marlon: The records give us a closure order, not a location. Old survey bearings suggest the sealed workings cross beneath the modern mine network, but the map is incomplete. We need observations from more than one shaft. Bring a real field team. Three people minimum, and at least one Team Up ally. We triangulate this together.",
    "story.triangulation.objective.first-bearing": "FIELD TRIANGULATION • With at least 3 people present, including 1 active Team Up NPC ally, enter a MineShaft and take the first bearing.",
    "story.triangulation.bearing-one": "The team spreads out instead of staring at the same wall. One person finds coal dust collecting against the wrong draft; another notices old timber cuts hidden behind newer stone. The two signs point across the shaft, not deeper into it. First bearing recorded.",
    "story.triangulation.objective.second-bearing": "FIELD TRIANGULATION • Move to a different MineShaft location with the field team and take a second bearing.",
    "story.triangulation.same-shaft": "FIELD TRIANGULATION • This shaft repeats the first reading. Move to a different MineShaft location for an independent bearing.",
    "story.triangulation.bearing-two": "A second shaft gives the missing angle. A faint inward-burn line in the coal seam aligns with an obsolete support notch, and the air pulls sideways toward solid rock. The bearings intersect beyond the mapped tunnel edge.",
    "story.triangulation.objective.return": "FIELD TRIANGULATION • Return to Marlon with the field team and compare both bearings.",
    "story.triangulation.confirmed": "Marlon: Two independent bearings, same buried direction. Good. The sealed workings are not somewhere below the mine in general. They sit beside the modern network, behind a corridor the current maps no longer show. We finally know where to search next. We still do not know who sealed it, and we are not guessing.",
    "story.triangulation.complete": "FIELD TRIANGULATION • Likely sealed-workings corridor identified. Story ally slot 4 remains locked.",
    "story.triangulation.need-team": "FIELD TRIANGULATION • Assemble at least 3 people in this location, including at least 1 active Team Up NPC ally."
}

vi = {
    "story.triangulation.briefing": "Marlon: Hồ sơ cho ta lệnh đóng mỏ, chứ không cho vị trí. Các hướng khảo sát cũ cho thấy khu khai thác bị niêm phong cắt ngang bên dưới mạng hầm hiện tại, nhưng bản đồ đã thiếu mất một phần. Ta cần quan sát từ nhiều hầm khác nhau. Hãy dẫn một đội thực địa đúng nghĩa. Tối thiểu ba người, và phải có ít nhất một đồng đội Team Up. Chúng ta sẽ xác định giao điểm cùng nhau.",
    "story.triangulation.objective.first-bearing": "ĐỊNH VỊ THỰC ĐỊA • Khi có ít nhất 3 người tại chỗ, gồm 1 NPC Team Up đang hoạt động, vào một MineShaft để lấy hướng đo đầu tiên.",
    "story.triangulation.bearing-one": "Cả đội tản ra thay vì cùng nhìn một bức tường. Một người phát hiện bụi than dồn ngược theo luồng gió; người khác thấy những vết cắt gỗ chống cũ bị đá mới che lấp. Hai dấu hiệu đều chỉ ngang qua hầm, không chỉ xuống sâu hơn. Đã ghi nhận hướng đo thứ nhất.",
    "story.triangulation.objective.second-bearing": "ĐỊNH VỊ THỰC ĐỊA • Cùng đội chuyển sang một MineShaft khác và lấy hướng đo thứ hai.",
    "story.triangulation.same-shaft": "ĐỊNH VỊ THỰC ĐỊA • Hầm này lặp lại hướng đo đầu tiên. Hãy sang một MineShaft khác để có phép đo độc lập.",
    "story.triangulation.bearing-two": "Hầm thứ hai cho ra góc còn thiếu. Một đường cháy mờ chạy ngược vào vỉa than thẳng với dấu cột chống kiểu cũ, còn luồng khí lại kéo ngang về phía khối đá đặc. Hai hướng đo giao nhau ở ngoài rìa đường hầm đang có trên bản đồ.",
    "story.triangulation.objective.return": "ĐỊNH VỊ THỰC ĐỊA • Cùng đội quay lại gặp Marlon và đối chiếu hai hướng đo.",
    "story.triangulation.confirmed": "Marlon: Hai hướng đo độc lập, cùng chỉ về một vùng bị chôn. Tốt. Khu khai thác bị niêm phong không đơn giản là nằm đâu đó phía dưới mỏ. Nó nằm sát mạng hầm hiện tại, sau một hành lang đã biến mất khỏi bản đồ mới. Cuối cùng ta cũng biết phải tìm ở đâu tiếp theo. Ta vẫn chưa biết ai đã niêm phong nó, và sẽ không đoán mò.",
    "story.triangulation.complete": "ĐỊNH VỊ THỰC ĐỊA • Đã xác định vùng hành lang có khả năng dẫn tới khu khai thác bị niêm phong. Slot đồng đội cốt truyện thứ 4 vẫn chưa mở.",
    "story.triangulation.need-team": "ĐỊNH VỊ THỰC ĐỊA • Tập hợp ít nhất 3 người tại khu vực này, gồm ít nhất 1 NPC Team Up đang hoạt động."
}

for rel, additions in [("i18n/default.json", en), ("i18n/vi.json", vi)]:
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(additions).intersection(data)
    if overlap:
        raise RuntimeError(f"6.7.32 localization keys already exist in {rel}: {sorted(overlap)}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.32 field triangulation materialized.")
