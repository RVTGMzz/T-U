from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
OLD = "0.2.0-alpha.6.7.29"
NEW = "0.2.0-alpha.6.7.30"


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:160]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


replace_once(SRC / "TeamUp.csproj", f"<Version>{OLD}</Version>", f"<Version>{NEW}</Version>")

service = r'''using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Story;

internal sealed class OldMineConnectionStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/OldMineConnectionStage";
    public const int CompleteStage = 3;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getMarlonStage;
    private readonly Func<GameLocation, bool> _hasActiveAllyAt;
    private readonly Action _onComplete;
    private int _stage;

    public OldMineConnectionStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getMarlonStage,
        Func<GameLocation, bool> hasActiveAllyAt,
        Action onComplete)
    {
        _helper = helper;
        _monitor = monitor;
        _getMarlonStage = getMarlonStage;
        _hasActiveAllyAt = hasActiveAllyAt;
        _onComplete = onComplete;
    }

    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded() => _stage = ReadStage(Game1.MasterPlayer);

    public void OnWarped(GameLocation location)
    {
        if (!CanAdvance(location) || !_hasActiveAllyAt(location))
            return;

        if (_stage == 0 && Is(location, "AdventureGuild"))
        {
            Show("story.old-mine.archive-lead");
            SetStage(Game1.MasterPlayer, 1, "guild-archive-lead");
            Hud("story.old-mine.objective.records");
        }
        else if (_stage == 1 && Is(location, "ManorHouse"))
        {
            Show("story.old-mine.sealed-record");
            SetStage(Game1.MasterPlayer, 2, "municipal-safety-ledger");
            Hud("story.old-mine.objective.return");
        }
        else if (_stage == 2 && Is(location, "AdventureGuild"))
        {
            Show("story.old-mine.connection-confirmed");
            SetStage(Game1.MasterPlayer, CompleteStage, "old-mine-connection-confirmed");
            _onComplete();
            Hud("story.old-mine.complete");
        }
    }

    public void Reset(Farmer owner)
    {
        owner.modData.Remove(StageKey);
        _stage = 0;
        _monitor.Log("[OldMineConnection] stage reset; roster unlocks were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer owner, int stage)
        => SetStage(owner, Math.Clamp(stage, 0, CompleteStage), "debug");

    public string Describe()
    {
        string objective = _stage switch
        {
            0 when _getMarlonStage() < MarlonInvestigationStoryService.CompleteStage => "finish-marlon-case",
            0 => "guild-archive-lead-with-ally",
            1 => "manorhouse-records-with-ally",
            2 => "return-to-marlon-with-ally",
            _ => "connection-confirmed-slot-3-unlocked"
        };
        return $"Old Mine Connection: marlonStage={_getMarlonStage()}/4 | stage={_stage}/{CompleteStage} | objective={objective}";
    }

    private bool CanAdvance(GameLocation location)
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getMarlonStage() >= MarlonInvestigationStoryService.CompleteStage
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private void SetStage(Farmer owner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        owner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[OldMineConnection] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
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
        if (!string.IsNullOrWhiteSpace(text)) Game1.drawObjectDialogue(text);
    }

    private void Hud(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text) && !Game1.eventUp) Game1.showGlobalMessage(text);
    }
}
'''
(SRC / "Story" / "OldMineConnectionStoryService.cs").write_text(service, encoding="utf-8", newline="\n")

integration = r'''using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private OldMineConnectionStoryService OldMineConnectionAlpha6730 { get; set; } = null!;

    private void RegisterAlpha6730Events()
    {
        OldMineConnectionAlpha6730 = new OldMineConnectionStoryService(
            Helper,
            Monitor,
            () => MarlonInvestigationAlpha6729.Stage,
            HasActiveStoryAllyAtAlpha6729,
            OnOldMineConnectionCompleteAlpha6730);
        Helper.Events.GameLoop.SaveLoaded += OnAlpha6730SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6730Warped;
        Helper.ConsoleCommands.Add(
            "teamup_old_mine",
            "Old mine connection: status | reset | stage <0-3>.",
            OnAlpha6730Command);
    }

    private void OnAlpha6730SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady)
            OldMineConnectionAlpha6730.OnSaveLoaded();
    }

    private void OnAlpha6730Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsMainPlayer && Context.IsWorldReady && e.IsLocalPlayer)
            OldMineConnectionAlpha6730.OnWarped(e.NewLocation);
    }

    private void OnOldMineConnectionCompleteAlpha6730()
    {
        bool unlocked = RosterProgressionAlpha6727.UnlockTo(Game1.MasterPlayer, 3, "old-mine-connection-confirmed");
        EnforceStoryRosterCapacityAlpha6727();
        SavePartyNow();
        BroadcastPartySnapshot();
        if (unlocked)
        {
            ShowHud(Helper.Translation.Get("story.roster.third-unlock", new
            {
                unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
                max = TeamUpRosterProgressionService.MaxStoryNpcSlots
            }));
        }
    }

    private void OnAlpha6730Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_old_mine.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            OldMineConnectionAlpha6730.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 3)
            OldMineConnectionAlpha6730.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_old_mine <status|reset|stage 0-3>", LogLevel.Info);
            return;
        }
        WriteAlpha6730Diagnostic();
    }

    private void WriteAlpha6730Diagnostic()
    {
        List<string> lines = new()
        {
            "TEAM UP 6.7.30 - OLD MINE CONNECTION",
            Origin.Describe(),
            MarlonInvestigationAlpha6729.Describe(),
            OldMineConnectionAlpha6730.Describe(),
            $"Active story ally here: {HasActiveStoryAllyAtAlpha6729(Game1.currentLocation)}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Route: Marlon case complete -> Guild archive lead -> ManorHouse sealed record -> Guild confirmation -> slot 3.",
            "Spoiler lock: the old miner remains unnamed; George stays Rank D / Non-Combatant and unrecruitable."
        };
        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Old_Mine_Connection_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines) Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Old mine connection diagnostic saved: {path}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6730.cs").write_text(integration, encoding="utf-8", newline="\n")

entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
if "RegisterAlpha6730Events();" not in entry:
    needle = "        RegisterAlpha6729Events();\n"
    if needle not in entry:
        raise RuntimeError("Alpha 6.7.29 registration point missing")
    entry = entry.replace(needle, needle + "        RegisterAlpha6730Events();\n", 1)
entry = entry.replace(
    "loaded. Marlon investigation + milestone reaction layer active.",
    "loaded. Old mine connection layer active.",
    1,
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

en = {
    "story.old-mine.archive-lead": "Marlon: I found the index card for the missing company page. It points to a municipal safety filing for an old coal mine, about thirty years back. The Guild copy was cut apart. Lewis's office may still have the town ledger. Take someone with you. Nobody follows this trail alone.",
    "story.old-mine.objective.records": "OLD MINE FILE • Visit the Mayor's Manor with an active Team Up ally and check the municipal safety records.",
    "story.old-mine.sealed-record": "Behind the public accident entry is a second notation: lower workings sealed after an explosive incident. The employee line is redacted. A hand-drawn inspection mark beside the closure order matches the hooked pattern on the Mutant shard.",
    "story.old-mine.objective.return": "OLD MINE FILE • Return to Marlon with your ally and report the sealed record.",
    "story.old-mine.connection-confirmed": "Marlon: That's enough. The current Mutants, the burn inside the stone, and this old closure record all point to the same incident. The Surge is touching a coal mine sealed thirty years ago. The miner's name stays missing, and I won't fill that blank with a guess. We follow evidence, not legends.",
    "story.old-mine.complete": "OLD MINE FILE • Old coal-mine connection confirmed.",
    "story.roster.third-unlock": "TEAM UP • The old mine connection is confirmed. Story ally capacity increased to {{unlocked}}/{{max}}."
}
vi = {
    "story.old-mine.archive-lead": "Marlon: Tôi tìm thấy thẻ mục lục của trang hồ sơ công ty bị mất. Nó dẫn tới một hồ sơ an toàn của thị trấn về một mỏ than cũ, khoảng ba mươi năm trước. Bản của Hội đã bị cắt mất. Văn phòng Lewis có thể vẫn còn sổ lưu. Dẫn theo một người. Không ai lần dấu này một mình.",
    "story.old-mine.objective.records": "HỒ SƠ MỎ CŨ • Đến Dinh Thị trưởng cùng một đồng đội Team Up đang hoạt động và kiểm tra hồ sơ an toàn của thị trấn.",
    "story.old-mine.sealed-record": "Phía sau mục tai nạn công khai có thêm một ghi chú: khu khai thác tầng dưới bị niêm phong sau một sự cố chất nổ. Dòng tên nhân viên đã bị bôi đen. Bên cạnh lệnh đóng mỏ là một dấu kiểm tra vẽ tay trùng với hoa văn móc trên mảnh Mutant.",
    "story.old-mine.objective.return": "HỒ SƠ MỎ CŨ • Cùng đồng đội quay lại gặp Marlon và báo cáo về hồ sơ bị niêm phong.",
    "story.old-mine.connection-confirmed": "Marlon: Vậy là đủ. Mutant hiện tại, vết cháy bên trong đá và hồ sơ đóng mỏ cũ đều chỉ về cùng một sự cố. The Surge đang chạm tới một mỏ than đã bị niêm phong ba mươi năm trước. Tên người thợ mỏ vẫn còn thiếu, và tôi sẽ không lấy phỏng đoán lấp vào đó. Ta theo bằng chứng, không theo huyền thoại.",
    "story.old-mine.complete": "HỒ SƠ MỎ CŨ • Đã xác nhận mối liên hệ với mỏ than cũ.",
    "story.roster.third-unlock": "TEAM UP • Mối liên hệ với mỏ cũ đã được xác nhận. Sức chứa đồng đội cốt truyện tăng lên {{unlocked}}/{{max}}."
}

for rel, additions in [("i18n/default.json", en), ("i18n/vi.json", vi)]:
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(additions).intersection(data)
    if overlap:
        raise RuntimeError(f"6.7.30 localization keys already exist in {rel}: {sorted(overlap)}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.30 old mine connection materialized.")
