from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"

VERSION_OLD = "0.2.0-alpha.6.7.25"
VERSION_NEW = "0.2.0-alpha.6.7.26"


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:120]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


# Version bump.
replace_once(
    SRC / "TeamUp.csproj",
    f"<Version>{VERSION_OLD}</Version>",
    f"<Version>{VERSION_NEW}</Version>",
)

# Replace the obsolete four-beat prototype with the first real main-story bridge.
# The old service could advance merely because a live monster existed in the current location.
# 6.7.26 hard-gates narrative progression on the deterministic tenth-kill Surge activation.
origin = r'''using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.26 main-story opening bridge.
///
/// Narrative progression is no longer triggered by merely seeing a monster. The first story beat
/// becomes available only after Alpha 6.7.25 has successfully transformed lethal defeat #10 into
/// the first Mutant and committed The Surge activation. The player is then directed to Linus in
/// the Forest, and Linus directs them to Marlon at the Adventurer's Guild.
///
/// This checkpoint deliberately stops after Marlon's first investigation scene. The old prototype
/// auto-Awakening and instant Team Up completion are retired so later chapters can own those beats.
/// </summary>
public sealed class OriginStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/SurgeNarrativeStage";
    public const string LinusPromptShownKey = "Ronvotri.TeamUp/SurgeNarrative/LinusPromptShown";

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<bool> _enabled;
    private int _stage;
    private int _ticks;

    public OriginStoryService(IModHelper helper, IMonitor monitor, Func<bool> enabled)
    {
        _helper = helper;
        _monitor = monitor;
        _enabled = enabled;
    }

    /// <summary>0 = find Linus, 1 = find Marlon, 2 = Marlon intro complete.</summary>
    public int Stage => _stage;
    public bool Completed => _stage >= 2;

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
        if (!_enabled()
            || !Context.IsWorldReady
            || !Context.IsMainPlayer
            || !IsSurgeActivated()
            || Game1.eventUp
            || Game1.dialogueUp
            || Game1.activeClickableMenu is not null)
        {
            return;
        }

        string name = location.NameOrUniqueName;
        if (_stage == 0 && name.Equals("Forest", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.linus.first-surge");
            SetStage(1);
            Game1.showGlobalMessage(_helper.Translation.Get("origin.quest.marlon").ToString());
            return;
        }

        if (_stage == 1 && name.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase))
        {
            ShowLine("origin.marlon.first-surge");
            SetStage(2);
            Game1.showGlobalMessage(_helper.Translation.Get("origin.first-surge-bridge.complete").ToString());
        }
    }

    public void Update()
    {
        if (!_enabled() || !Context.IsWorldReady || !Context.IsMainPlayer || !IsSurgeActivated())
            return;

        _ticks++;
        if (_ticks % 30 != 0
            || _stage != 0
            || HasFlag(LinusPromptShownKey)
            || Game1.eventUp
            || Game1.dialogueUp
            || Game1.activeClickableMenu is not null)
        {
            return;
        }

        Game1.player.modData[LinusPromptShownKey] = "1";
        Game1.showGlobalMessage(_helper.Translation.Get("origin.quest.linus").ToString());
        _monitor.Log("[Story] First Surge committed. Objective opened: find Linus in the Forest.", LogLevel.Info);
    }

    public string Describe()
    {
        if (!Context.IsWorldReady)
            return "First Surge narrative: world not loaded.";

        string objective = _stage switch
        {
            0 when !IsSurgeActivated() => "waiting-for-first-mutant",
            0 => "find-linus-forest",
            1 => "find-marlon-adventure-guild",
            _ => "marlon-investigation-open"
        };

        return $"First Surge Narrative: surgeActivated={IsSurgeActivated()} | stage={_stage}/2 | objective={objective} | linusPrompt={HasFlag(LinusPromptShownKey)}";
    }

    public void ResetStory(Farmer farmer)
    {
        farmer.modData.Remove(StageKey);
        farmer.modData.Remove(LinusPromptShownKey);
        _stage = 0;
        _ticks = 0;
        _monitor.Log("[Story] First Surge narrative bridge reset. Surge activation itself was not changed.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer farmer, int stage)
    {
        _stage = Math.Clamp(stage, 0, 2);
        farmer.modData[StageKey] = _stage.ToString();
        if (_stage == 0)
            farmer.modData.Remove(LinusPromptShownKey);
        _monitor.Log($"[Story] First Surge narrative debug stage set to {_stage}/2.", LogLevel.Info);
    }

    private bool IsSurgeActivated()
        => TheSurgeStoryService.ActiveInstance?.IsActivated == true;

    private void ShowLine(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text))
            Game1.drawObjectDialogue(text);
    }

    private int ReadInt(string key)
        => Game1.player.modData.TryGetValue(key, out string? raw) && int.TryParse(raw, out int value)
            ? Math.Clamp(value, 0, 2)
            : 0;

    private bool HasFlag(string key)
        => Game1.player.modData.TryGetValue(key, out string? value) && value == "1";

    private void SetStage(int stage)
    {
        _stage = Math.Clamp(stage, 0, 2);
        Game1.player.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[Story] First Surge narrative advanced to stage {_stage}/2.", LogLevel.Info);
    }
}
'''
(SRC / "Story" / "OriginStoryService.cs").write_text(origin, encoding="utf-8", newline="\n")

# Wire the new constructor, registration, and build label.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
entry = entry.replace(
    "Origin = new OriginStoryService(Helper, Monitor, () => Party.Members, () => Config.EnableOriginStory);",
    "Origin = new OriginStoryService(Helper, Monitor, () => Config.EnableOriginStory);",
)
if "RegisterAlpha6726Events();" not in entry:
    entry = entry.replace("        RegisterAlpha6725Events();\n", "        RegisterAlpha6725Events();\n        RegisterAlpha6726Events();\n", 1)
entry = entry.replace(
    "loaded. Codex discovery + first Surge trigger active.",
    "loaded. Codex discovery + first Surge narrative bridge active.",
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

# Developer/test bridge. This changes only the short narrative stage, never the 10-kill Surge state.
alpha6726 = r'''using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6726Events()
    {
        Helper.ConsoleCommands.Add(
            "teamup_story_intro",
            "First Surge narrative bridge: status | reset | stage <0-2>. Status writes diagnostics/TeamUp_Story_Intro_latest.txt.",
            OnAlpha6726StoryIntroCommand);
    }

    private void OnAlpha6726StoryIntroCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_story_intro.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "status":
                WriteAlpha6726StoryDiagnostic();
                return;

            case "reset":
                Origin.ResetStory(Game1.player);
                WriteAlpha6726StoryDiagnostic();
                Game1.showGlobalMessage("FIRST SURGE STORY • INTRO RESET");
                return;

            case "stage":
                if (args.Length < 2 || !int.TryParse(args[1], out int stage) || stage < 0 || stage > 2)
                {
                    Monitor.Log("Usage: teamup_story_intro stage <0-2>", LogLevel.Info);
                    return;
                }
                Origin.SetDebugStage(Game1.player, stage);
                WriteAlpha6726StoryDiagnostic();
                return;

            default:
                Monitor.Log("Usage: teamup_story_intro <status|reset|stage 0-2>", LogLevel.Info);
                return;
        }
    }

    private void WriteAlpha6726StoryDiagnostic()
    {
        List<string> lines = new()
        {
            "TEAM UP 6.7.26 - LINUS / MARLON FIRST SURGE BRIDGE",
            SurgeStoryAlpha6725.Describe(),
            Origin.Describe(),
            "Expected before first Mutant: stage=0/2 objective=waiting-for-first-mutant.",
            "After first Mutant: objective=find-linus-forest.",
            "After entering Forest: stage=1/2 objective=find-marlon-adventure-guild.",
            "After entering AdventureGuild: stage=2/2 objective=marlon-investigation-open."
        };

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Story_Intro_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Story intro diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6726.cs").write_text(alpha6726, encoding="utf-8", newline="\n")

# Localized story copy. Keep legacy origin keys for backward compatibility but stop calling them.
localization = {
    "default.json": {
        "origin.quest.linus": "QUEST UPDATED • The mutant was not an isolated incident. Linus may have noticed what is changing in the Valley.",
        "origin.linus.first-surge": "Linus: I felt it before you came. Those creatures aren't simply becoming more violent. They're running from something. Find Marlon. If this has reached the mines, he needs to know what you saw.",
        "origin.quest.marlon": "QUEST UPDATED • Tell Marlon what happened to the first mutant.",
        "origin.marlon.first-surge": "Marlon: ...I've seen marks like this before. Not on a living creature, and not in my time. An old Guild report. We arrived after the fighting was already over. Someone outside the Guild had stopped it. For now, remember this: if the Valley is changing, don't go below alone.",
        "origin.first-surge-bridge.complete": "THE SURGE • Marlon has begun investigating the old record."
    },
    "vi.json": {
        "origin.quest.linus": "NHIỆM VỤ CẬP NHẬT • Con quái đột biến không phải chuyện ngẫu nhiên. Có lẽ Linus đã nhận ra Thung Lũng đang thay đổi.",
        "origin.linus.first-surge": "Linus: Tôi đã cảm nhận được nó trước khi bạn tới. Những sinh vật ấy không chỉ trở nên hung dữ hơn. Chúng đang chạy trốn khỏi một thứ gì đó. Hãy tìm Marlon. Nếu chuyện này đã lan tới các hầm mỏ, ông ấy cần biết bạn vừa chứng kiến điều gì.",
        "origin.quest.marlon": "NHIỆM VỤ CẬP NHẬT • Hãy kể cho Marlon chuyện đã xảy ra với con quái đột biến đầu tiên.",
        "origin.marlon.first-surge": "Marlon: ...Tôi từng thấy những dấu vết như thế này. Không phải trên một sinh vật sống, và cũng không phải vào thời của tôi. Trong một hồ sơ cũ của Hội. Khi chúng tôi tới nơi, trận chiến đã kết thúc. Một người không thuộc Hội đã ngăn nó lại. Còn bây giờ, hãy nhớ điều này: nếu Thung Lũng đang thay đổi, đừng xuống dưới đó một mình.",
        "origin.first-surge-bridge.complete": "THE SURGE • Marlon đã bắt đầu điều tra hồ sơ cũ."
    }
}

for file_name, additions in localization.items():
    path = SRC / "i18n" / file_name
    data = json.loads(path.read_text(encoding="utf-8"))
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.26 Linus/Marlon first Surge story bridge materialized.")
