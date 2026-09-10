from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION_OLD = "0.2.0-alpha.6.7.27"
VERSION_NEW = "0.2.0-alpha.6.7.28"


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:220]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


# Version bump.
replace_once(
    SRC / "TeamUp.csproj",
    f"<Version>{VERSION_OLD}</Version>",
    f"<Version>{VERSION_NEW}</Version>",
)

# One-time, context-sensitive reactions between the three opening story milestones.
reaction_service = r'''using StardewValley;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.28 ambient story reactions.
///
/// Reactions are deliberately contextual rather than a global lore dump. A supported NPC can react
/// once during each opening window: after the first Mutant but before Linus, after Linus but before
/// Marlon, and after Marlon opens the first Team Up ally slot. Missing a window simply means that
/// reaction is missed; later milestones never replay stale dialogue.
/// </summary>
internal sealed class StoryMilestoneReactionService
{
    public const string SeenPrefix = "Ronvotri.TeamUp/StoryReaction/";

    private static readonly IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> Reactions = Build();

    public bool TryConsume(Farmer farmer, int narrativeStage, string characterName, out string translationKey)
    {
        translationKey = string.Empty;
        if (narrativeStage < 0 || narrativeStage > 2 || string.IsNullOrWhiteSpace(characterName))
            return false;

        if (!Reactions.TryGetValue(narrativeStage, out IReadOnlyDictionary<string, string>? stage)
            || !stage.TryGetValue(characterName, out translationKey))
        {
            return false;
        }

        string seenKey = BuildSeenKey(narrativeStage, characterName);
        if (farmer.modData.TryGetValue(seenKey, out string? value) && value == "1")
            return false;

        farmer.modData[seenKey] = "1";
        return true;
    }

    public int GetSeenCount(Farmer farmer, int narrativeStage)
    {
        if (!Reactions.TryGetValue(narrativeStage, out IReadOnlyDictionary<string, string>? stage))
            return 0;
        return stage.Keys.Count(name => farmer.modData.ContainsKey(BuildSeenKey(narrativeStage, name)));
    }

    public int GetAvailableCount(int narrativeStage)
        => Reactions.TryGetValue(narrativeStage, out IReadOnlyDictionary<string, string>? stage) ? stage.Count : 0;

    public void Reset(Farmer farmer)
    {
        string[] keys = farmer.modData.Keys
            .Where(key => key.StartsWith(SeenPrefix, StringComparison.Ordinal))
            .ToArray();
        foreach (string key in keys)
            farmer.modData.Remove(key);
    }

    private static string BuildSeenKey(int stage, string characterName)
        => $"{SeenPrefix}{stage}/{Uri.EscapeDataString(characterName.Trim().ToLowerInvariant())}";

    private static IReadOnlyDictionary<int, IReadOnlyDictionary<string, string>> Build()
    {
        Dictionary<string, string> firstMutant = Stage(
            "Abigail", "story.react.0.abigail",
            "Alex", "story.react.0.alex",
            "Clint", "story.react.0.clint",
            "Demetrius", "story.react.0.demetrius",
            "Evelyn", "story.react.0.evelyn",
            "George", "story.react.0.george",
            "Gus", "story.react.0.gus",
            "Lewis", "story.react.0.lewis",
            "Maru", "story.react.0.maru",
            "Pierre", "story.react.0.pierre",
            "Robin", "story.react.0.robin",
            "Wizard", "story.react.0.wizard");

        Dictionary<string, string> afterLinus = Stage(
            "Abigail", "story.react.1.abigail",
            "Alex", "story.react.1.alex",
            "Clint", "story.react.1.clint",
            "Demetrius", "story.react.1.demetrius",
            "Evelyn", "story.react.1.evelyn",
            "George", "story.react.1.george",
            "Gus", "story.react.1.gus",
            "Lewis", "story.react.1.lewis",
            "Maru", "story.react.1.maru",
            "Pierre", "story.react.1.pierre",
            "Robin", "story.react.1.robin",
            "Wizard", "story.react.1.wizard");

        Dictionary<string, string> afterMarlon = Stage(
            "Abigail", "story.react.2.abigail",
            "Alex", "story.react.2.alex",
            "Clint", "story.react.2.clint",
            "Demetrius", "story.react.2.demetrius",
            "Evelyn", "story.react.2.evelyn",
            "George", "story.react.2.george",
            "Gus", "story.react.2.gus",
            "Lewis", "story.react.2.lewis",
            "Linus", "story.react.2.linus",
            "Marlon", "story.react.2.marlon",
            "Maru", "story.react.2.maru",
            "Pierre", "story.react.2.pierre",
            "Robin", "story.react.2.robin",
            "Wizard", "story.react.2.wizard");

        return new Dictionary<int, IReadOnlyDictionary<string, string>>
        {
            [0] = firstMutant,
            [1] = afterLinus,
            [2] = afterMarlon
        };
    }

    private static Dictionary<string, string> Stage(params string[] pairs)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i + 1 < pairs.Length; i += 2)
            result[pairs[i]] = pairs[i + 1];
        return result;
    }
}
'''
(SRC / "Story" / "StoryMilestoneReactionService.cs").write_text(reaction_service, encoding="utf-8", newline="\n")

# George is an intentional false-negative in the early Codex: observed Rank D, no visible combat kit,
# and no party access until the future main-story reveal. The reveal flag is defined now but 6.7.28
# never sets it, so a later finale checkpoint owns the actual Rank S transition.
alpha6728 = r'''using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    internal const string GeorgeCombatRevealedKeyAlpha6728 = "Ronvotri.TeamUp/Story/GeorgeCombatRevealed";
    private StoryMilestoneReactionService MilestoneReactionsAlpha6728 { get; set; } = null!;

    private void RegisterAlpha6728Events()
    {
        MilestoneReactionsAlpha6728 = new StoryMilestoneReactionService();
        Helper.Events.GameLoop.SaveLoaded += OnAlpha6728SaveLoaded;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6728UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_story_reactions",
            "Opening story reactions: status | reset. Status writes diagnostics/TeamUp_Milestone_Reactions_latest.txt.",
            OnAlpha6728StoryReactionCommand);
    }

    private bool IsGeorgePreRevealLockedAlpha6728(string characterName)
        => characterName.Equals("George", StringComparison.OrdinalIgnoreCase)
            && !IsGeorgeCombatRevealedAlpha6728();

    private bool IsGeorgeCombatRevealedAlpha6728()
        => Context.IsWorldReady
            && Game1.MasterPlayer.modData.TryGetValue(GeorgeCombatRevealedKeyAlpha6728, out string? value)
            && value == "1";

    private bool CanRecruitCharacterByStoryAlpha6728(NPC npc, out string failure)
    {
        failure = string.Empty;
        if (!IsGeorgePreRevealLockedAlpha6728(npc.Name))
            return true;

        failure = Helper.Translation.Get("story.george.pre-reveal.recruit-blocked");
        return false;
    }

    private bool TryGetCharacterStoryProfileStatusAlpha6728(NPC npc, out string status)
    {
        if (IsGeorgePreRevealLockedAlpha6728(npc.Name))
        {
            status = Helper.Translation.Get("profile.status-noncombatant");
            return true;
        }

        status = string.Empty;
        return false;
    }

    private void ShowGeorgePreRevealRecruitTeaseAlpha6728(NPC npc)
    {
        RecruitHintNpcName = npc.Name;
        string text = Helper.Translation.Get("story.george.pre-reveal.recruit-tease", new { name = npc.displayName });
        Game1.drawObjectDialogue(text);
    }

    private bool TryShowMilestoneReactionAlpha6728(NPC npc)
    {
        if (!Context.IsWorldReady
            || Game1.eventUp
            || Game1.dialogueUp
            || Game1.activeClickableMenu is not null
            || TheSurgeStoryService.ActiveInstance?.IsActivated != true)
        {
            return false;
        }

        if (!MilestoneReactionsAlpha6728.TryConsume(Game1.player, Origin.Stage, npc.Name, out string key))
            return false;

        string text = Helper.Translation.Get(key).ToString();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        RecruitHintNpcName = npc.Name;
        Game1.drawObjectDialogue(text);
        Monitor.Log($"[StoryReaction] stage={Origin.Stage} npc={npc.Name} key={key}", LogLevel.Debug);
        return true;
    }

    private void OnAlpha6728SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;
        EnforceGeorgePreRevealLockAlpha6728();
    }

    private void OnAlpha6728UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !e.IsMultipleOf(60))
            return;
        EnforceGeorgePreRevealLockAlpha6728();
    }

    private int EnforceGeorgePreRevealLockAlpha6728()
    {
        if (!Context.IsWorldReady || IsGeorgeCombatRevealedAlpha6728())
            return 0;

        int changed = 0;
        foreach (PartyMemberData member in Party.Members.Where(member =>
            member.CharacterName.Equals("George", StringComparison.OrdinalIgnoreCase)
            && member.State is PartyMemberState.Following or PartyMemberState.Waiting))
        {
            member.State = PartyMemberState.Inactive;
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is not null)
                Follow.ReleaseToVanillaAndResumeSchedule(npc);
            changed++;
        }

        if (changed > 0)
        {
            SavePartyNow();
            BroadcastPartySnapshot();
            Monitor.Log($"[GeorgeStory] forced {changed} pre-reveal George roster entry/entries inactive.", LogLevel.Info);
        }
        return changed;
    }

    private void OnAlpha6728StoryReactionCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("Load a save before using teamup_story_reactions.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            MilestoneReactionsAlpha6728.Reset(Game1.player);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_story_reactions <status|reset>", LogLevel.Info);
            return;
        }

        WriteAlpha6728StoryReactionDiagnostic();
    }

    private void WriteAlpha6728StoryReactionDiagnostic()
    {
        int stage = Origin.Stage;
        List<string> lines = new()
        {
            "TEAM UP 6.7.28 - GEORGE CAMOUFLAGE + MILESTONE REACTIONS",
            Origin.Describe(),
            $"Current reaction window: stage={stage} seen={MilestoneReactionsAlpha6728.GetSeenCount(Game1.player, stage)}/{MilestoneReactionsAlpha6728.GetAvailableCount(stage)}",
            $"George combat reveal flag: {IsGeorgeCombatRevealedAlpha6728()}",
            $"George pre-reveal combat lock: {IsGeorgePreRevealLockedAlpha6728(\"George\")}",
            "George expected before the future finale reveal: observed Rank D, NON-COMBATANT, no visible combat skill, recruitment blocked.",
            "Reaction windows: 0=first Mutant before Linus | 1=after Linus before Marlon | 2=after Marlon / first ally slot."
        };

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Milestone_Reactions_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Story reaction diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6728.cs").write_text(alpha6728, encoding="utf-8", newline="\n")

# Main entry integration: register 6.7.28, let the Recruit key deliberately tease George's locked
# status, present a one-time ambient reaction before the ordinary action interaction, and show a
# non-combatant profile status instead of a suspicious ???/Special marker.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
if "RegisterAlpha6728Events();" not in entry:
    entry = entry.replace("        RegisterAlpha6727Events();\n", "        RegisterAlpha6727Events();\n        RegisterAlpha6728Events();\n", 1)
entry = entry.replace(
    "loaded. Codex discovery + story roster progression active.",
    "loaded. Story roster + milestone reaction layer active.",
    1,
)

old_recruit_dialogue = '''                if (IsRecruitableNpc(speaker))\n                {\n                    Helper.Input.Suppress(e.Button);\n                    ShowRecruitQuestion(speaker);\n                    return;\n                }'''
new_recruit_dialogue = '''                if (IsGeorgePreRevealLockedAlpha6728(speaker.Name))\n                {\n                    Helper.Input.Suppress(e.Button);\n                    ShowGeorgePreRevealRecruitTeaseAlpha6728(speaker);\n                    return;\n                }\n\n                if (IsRecruitableNpc(speaker))\n                {\n                    Helper.Input.Suppress(e.Button);\n                    ShowRecruitQuestion(speaker);\n                    return;\n                }'''
if old_recruit_dialogue not in entry:
    raise RuntimeError("George dialogue recruit insertion point missing")
entry = entry.replace(old_recruit_dialogue, new_recruit_dialogue, 1)

old_action = '''        if (Game1.player.ActiveObject is not null)\n        {\n            RecruitHintNpcName = null;\n            return;\n        }\n\n        PartyMemberData? memberData = Party.Get(npc.Name, recruiterId);'''
new_action = '''        if (Game1.player.ActiveObject is not null)\n        {\n            RecruitHintNpcName = null;\n            return;\n        }\n\n        if (TryShowMilestoneReactionAlpha6728(npc))\n        {\n            Helper.Input.Suppress(e.Button);\n            return;\n        }\n\n        PartyMemberData? memberData = Party.Get(npc.Name, recruiterId);'''
if old_action not in entry:
    raise RuntimeError("Milestone reaction interaction insertion point missing")
entry = entry.replace(old_action, new_action, 1)

old_hint = '''        string? rightText = member is not null\n            ? Helper.Translation.Get("hint.leave").ToString()\n            : IsRecruitableNpc(speaker)\n                ? Helper.Translation.Get("hint.recruit").ToString()\n                : null;'''
new_hint = '''        string? rightText = member is not null\n            ? Helper.Translation.Get("hint.leave").ToString()\n            : IsGeorgePreRevealLockedAlpha6728(speaker.Name)\n                ? Helper.Translation.Get("hint.recruit-try").ToString()\n                : IsRecruitableNpc(speaker)\n                    ? Helper.Translation.Get("hint.recruit").ToString()\n                    : null;'''
if old_hint not in entry:
    raise RuntimeError("George dialogue hint insertion point missing")
entry = entry.replace(old_hint, new_hint, 1)

old_status = '''        NPC? npc = Game1.getCharacterFromName(characterName);\n        if (npc is not null)\n            return GetRosterProfileStatusAlpha6727(npc);'''
new_status = '''        NPC? npc = Game1.getCharacterFromName(characterName);\n        if (npc is not null)\n        {\n            if (TryGetCharacterStoryProfileStatusAlpha6728(npc, out string storyStatus))\n                return storyStatus;\n            return GetRosterProfileStatusAlpha6727(npc);\n        }'''
if old_status not in entry:
    raise RuntimeError("George profile status insertion point missing")
entry = entry.replace(old_status, new_status, 1)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

# Host-authoritative recruitment and reactivation cannot bypass George's pre-reveal lock.
alpha661_path = SRC / "ModEntry.Alpha661.cs"
alpha661 = alpha661_path.read_text(encoding="utf-8")
old_authoritative = '''        long recruiterId = recruiter.UniqueMultiplayerID;\n        if (!IsBaseRecruitableNpcForAlpha6727(npc, recruiter))'''
new_authoritative = '''        long recruiterId = recruiter.UniqueMultiplayerID;\n        if (!CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure))\n        {\n            SendActionResult(responsePlayerId, false, characterStoryFailure);\n            return;\n        }\n\n        if (!IsBaseRecruitableNpcForAlpha6727(npc, recruiter))'''
if old_authoritative not in alpha661:
    raise RuntimeError("Authoritative George recruit gate insertion point missing")
alpha661 = alpha661.replace(old_authoritative, new_authoritative, 1)

old_move = '''        if (request.Command == "Movement" && Enum.TryParse(request.Value, out PartyMemberState state))\n        {\n            if (state == PartyMemberState.Following'''
new_move = '''        if (request.Command == "Movement" && Enum.TryParse(request.Value, out PartyMemberState state))\n        {\n            if (state is PartyMemberState.Following or PartyMemberState.Waiting\n                && IsGeorgePreRevealLockedAlpha6728(request.CharacterName))\n            {\n                SendActionResult(responsePlayerId, false, Helper.Translation.Get("story.george.pre-reveal.recruit-blocked"));\n                return false;\n            }\n\n            if (state == PartyMemberState.Following'''
if old_move not in alpha661:
    raise RuntimeError("George reactivation gate insertion point missing")
alpha661 = alpha661.replace(old_move, new_move, 1)

old_is_recruitable = '''    private bool IsRecruitableNpcFor(NPC npc, Farmer farmer)\n    {\n        if (!IsBaseRecruitableNpcForAlpha6727(npc, farmer))'''
new_is_recruitable = '''    private bool IsRecruitableNpcFor(NPC npc, Farmer farmer)\n    {\n        if (!CanRecruitCharacterByStoryAlpha6728(npc, out _))\n            return false;\n\n        if (!IsBaseRecruitableNpcForAlpha6727(npc, farmer))'''
if old_is_recruitable not in alpha661:
    raise RuntimeError("UI George recruit gate insertion point missing")
alpha661 = alpha661.replace(old_is_recruitable, new_is_recruitable, 1)
alpha661_path.write_text(alpha661, encoding="utf-8", newline="\n")

# Localization. George and Evelyn deliberately remain mundane here. No secret Rank S wording is used.
localization = {
    "default.json": {
        "hint.recruit-try": "Try Invite  R (Controller) / E",
        "profile.status-noncombatant": "Non-Combatant",
        "story.george.pre-reveal.recruit-blocked": "George is currently assessed as a non-combatant and cannot join field combat.",
        "story.george.pre-reveal.recruit-tease": "You really want to ask an old man in a wheelchair to go monster hunting? Really?\nGeorge: \"Are you serious?\"",
        "story.react.0.abigail": "Abigail: The mines have felt strange all day. Not scarier. Quieter, like everything is listening.",
        "story.react.0.alex": "Alex: You look like you just went ten rounds with something ugly. You're okay, right?",
        "story.react.0.clint": "Clint: My tools rattled on the wall earlier. I thought it was a cart... maybe not.",
        "story.react.0.demetrius": "Demetrius: Sudden behavioral changes usually mean outside pressure. One mutant may only be the first visible symptom.",
        "story.react.0.evelyn": "Evelyn: Sit for a moment, dear. Whatever you saw can wait long enough for you to catch your breath.",
        "story.react.0.george": "George: Hmph. Mines are dangerous. Always have been. Don't go looking for trouble just because it looked strange.",
        "story.react.0.gus": "Gus: People are already whispering. Funny how a rumor can cross town faster than the bus.",
        "story.react.0.lewis": "Lewis: Tell me exactly what happened, but let's not start a panic until we know more.",
        "story.react.0.maru": "Maru: If you remember where and when it changed, write it down. Patterns hide in details.",
        "story.react.0.pierre": "Pierre: Mutant monsters? Great. As if getting people to shop locally wasn't hard enough.",
        "story.react.0.robin": "Robin: I've heard odd knocks from the mountain lately. I blamed old timber settling.",
        "story.react.0.wizard": "Wizard: Something below the valley disturbed its rhythm. I can feel the echo, but not its source.",
        "story.react.1.abigail": "Abigail: If Linus is worried, listen. He notices things everyone else walks past.",
        "story.react.1.alex": "Alex: So Linus thinks they're running from something? That's... actually worse than them just getting meaner.",
        "story.react.1.clint": "Clint: Marlon knows the deep levels better than anyone. If he goes quiet after you tell him, pay attention.",
        "story.react.1.demetrius": "Demetrius: Flight behavior implies external pressure. Whatever is causing this may be deeper than the affected creatures.",
        "story.react.1.evelyn": "Evelyn: Marlon is a careful man. Hear him out before you decide what to do next.",
        "story.react.1.george": "George: Marlon knows the mines. That's what the Guild is for. Let the man do his job.",
        "story.react.1.gus": "Gus: If you're heading to the Guild, eat first. Bad news sounds worse on an empty stomach.",
        "story.react.1.lewis": "Lewis: The Guild keeps records the town office doesn't. If Marlon recognizes this, I want to know.",
        "story.react.1.maru": "Maru: Ask whether the Guild has old incident maps. Location data might tell us more than monster samples.",
        "story.react.1.pierre": "Pierre: Marlon? Well, if anyone can handle monster trouble, it's the guy with the sword.",
        "story.react.1.robin": "Robin: If Marlon mentions old shafts, don't enter one without checking the supports. I mean it.",
        "story.react.1.wizard": "Wizard: The hermit heard fear. The swordsman may recognize its shape.",
        "story.react.2.abigail": "Abigail: You're putting a team together? Finally. Going alone into a mystery is how people become cautionary tales.",
        "story.react.2.alex": "Alex: If you're going back down there, don't do it alone. Strong doesn't mean invincible.",
        "story.react.2.clint": "Clint: A team needs gear that survives the trip. Bring me anything bent before it becomes broken.",
        "story.react.2.demetrius": "Demetrius: Multiple observers reduce blind spots. For once, teamwork is also statistically sound.",
        "story.react.2.evelyn": "Evelyn: Take someone with you, dear. And take food. Bravery is no substitute for lunch.",
        "story.react.2.george": "George: A team? Hmph. At least that's smarter than charging into a mine alone.",
        "story.react.2.gus": "Gus: Everyone needs someone watching their back. Even the people who swear they don't.",
        "story.react.2.lewis": "Lewis: If you're forming a group to protect the valley, keep each other safe. That's not a suggestion.",
        "story.react.2.linus": "Linus: Good. You found Marlon. Now don't mistake being warned for being safe.",
        "story.react.2.marlon": "Marlon: Choose your first companion carefully. Skill matters. Trust matters more.",
        "story.react.2.maru": "Maru: Different skills, shared information. That's a much better survival model.",
        "story.react.2.pierre": "Pierre: Teamwork, huh? Fine. Just remember supplies still cost money.",
        "story.react.2.robin": "Robin: Pick people who know what they can do, not people who only want to look brave.",
        "story.react.2.wizard": "Wizard: One flame is easily smothered. A constellation is harder to erase."
    },
    "vi.json": {
        "hint.recruit-try": "Thử mời  R (Controller) / E",
        "profile.status-noncombatant": "Không tham chiến",
        "story.george.pre-reveal.recruit-blocked": "George hiện được đánh giá là nhân vật không tham chiến và chưa thể gia nhập đội chiến đấu.",
        "story.george.pre-reveal.recruit-tease": "Bạn thật sự định mời một ông lão ngồi xe lăn đi săn quái sao? Thật đấy à?\nGeorge: \"Cậu nghiêm túc đấy à?\"",
        "story.react.0.abigail": "Abigail: Hôm nay hầm mỏ có cảm giác rất lạ. Không đáng sợ hơn... mà yên hơn, như thể mọi thứ đang lắng nghe.",
        "story.react.0.alex": "Alex: Trông cậu như vừa đánh mười hiệp với thứ gì kinh khủng lắm. Cậu ổn chứ?",
        "story.react.0.clint": "Clint: Lúc nãy mấy dụng cụ trên tường tự rung lên. Tôi tưởng xe goòng chạy qua... giờ thì không chắc nữa.",
        "story.react.0.demetrius": "Demetrius: Hành vi thay đổi đột ngột thường có nghĩa là đang chịu áp lực từ bên ngoài. Một con đột biến có thể chỉ là triệu chứng đầu tiên ta nhìn thấy.",
        "story.react.0.evelyn": "Evelyn: Ngồi nghỉ một chút đi cháu. Dù cháu vừa thấy gì, nó cũng có thể chờ cháu lấy lại hơi thở.",
        "story.react.0.george": "George: Hừm. Hầm mỏ vốn nguy hiểm. Xưa giờ vẫn thế. Đừng tự đi tìm rắc rối chỉ vì lần này nó trông hơi khác.",
        "story.react.0.gus": "Gus: Cả thị trấn bắt đầu xì xào rồi. Tin đồn đúng là chạy còn nhanh hơn cả xe buýt.",
        "story.react.0.lewis": "Lewis: Kể cho tôi chính xác chuyện đã xảy ra, nhưng đừng làm cả thị trấn hoảng lên khi ta còn chưa biết rõ.",
        "story.react.0.maru": "Maru: Nếu còn nhớ nó biến đổi ở đâu và lúc nào thì ghi lại nhé. Quy luật thường trốn trong những chi tiết nhỏ.",
        "story.react.0.pierre": "Pierre: Quái vật đột biến à? Tuyệt thật. Cứ như thuyết phục mọi người mua hàng địa phương chưa đủ khó vậy.",
        "story.react.0.robin": "Robin: Gần đây tôi có nghe vài tiếng động lạ từ phía núi. Tôi cứ tưởng gỗ chống cũ đang co lại thôi.",
        "story.react.0.wizard": "Wizard: Có thứ gì đó dưới Thung Lũng vừa làm nhịp của nó chao đảo. Ta cảm nhận được tiếng vọng, nhưng chưa thấy nguồn.",
        "story.react.1.abigail": "Abigail: Nếu Linus thấy lo thì nên nghe ông ấy. Ông ấy để ý những thứ mà mọi người thường đi ngang qua.",
        "story.react.1.alex": "Alex: Vậy Linus nghĩ chúng đang chạy trốn một thứ gì đó à? Nghe... còn tệ hơn chuyện chúng chỉ trở nên hung dữ.",
        "story.react.1.clint": "Clint: Marlon hiểu những tầng sâu hơn bất kỳ ai. Nếu nghe xong mà ông ấy bỗng im lặng, nhớ để ý đấy.",
        "story.react.1.demetrius": "Demetrius: Hành vi bỏ chạy cho thấy có áp lực từ bên ngoài. Nguyên nhân có thể nằm sâu hơn cả những sinh vật đang bị ảnh hưởng.",
        "story.react.1.evelyn": "Evelyn: Marlon là người cẩn trọng. Hãy nghe ông ấy nói hết rồi mới quyết định cháu sẽ làm gì tiếp theo.",
        "story.react.1.george": "George: Marlon biết chuyện hầm mỏ. Hội Mạo Hiểm tồn tại để làm việc đó. Cứ để ông ta làm phần của mình.",
        "story.react.1.gus": "Gus: Nếu sắp tới Hội Mạo Hiểm thì ăn gì trước đã. Tin xấu lúc bụng đói nghe còn tệ hơn nữa.",
        "story.react.1.lewis": "Lewis: Hội Mạo Hiểm giữ những hồ sơ mà văn phòng thị trấn không có. Nếu Marlon nhận ra chuyện này, tôi muốn được biết.",
        "story.react.1.maru": "Maru: Hỏi xem Hội có bản đồ các sự cố cũ không. Dữ liệu vị trí có khi còn hữu ích hơn mẫu quái vật.",
        "story.react.1.pierre": "Pierre: Marlon à? Nếu có ai xử lý nổi chuyện quái vật thì chắc là ông chú lúc nào cũng cầm kiếm đó.",
        "story.react.1.robin": "Robin: Nếu Marlon nhắc tới hầm cũ thì đừng chui vào trước khi kiểm tra cột chống. Tôi nói thật đấy.",
        "story.react.1.wizard": "Wizard: Người ẩn sĩ đã nghe được nỗi sợ. Có lẽ người cầm kiếm sẽ nhận ra hình dạng của nó.",
        "story.react.2.abigail": "Abigail: Cậu đang lập đội à? Cuối cùng cũng chịu. Đi một mình vào một bí ẩn là cách nhanh nhất để biến thành câu chuyện cảnh báo đấy.",
        "story.react.2.alex": "Alex: Nếu còn định xuống đó thì đừng đi một mình. Mạnh không có nghĩa là bất khả chiến bại.",
        "story.react.2.clint": "Clint: Một đội cần trang bị sống sót được qua chuyến đi. Thứ gì cong thì mang tới tôi trước khi nó gãy hẳn.",
        "story.react.2.demetrius": "Demetrius: Nhiều người quan sát sẽ giảm điểm mù. Lần này 'phối hợp' cũng hợp lý về mặt thống kê.",
        "story.react.2.evelyn": "Evelyn: Dẫn ai đó đi cùng nhé cháu. Và mang theo đồ ăn nữa. Can đảm không thể thay cho bữa trưa đâu.",
        "story.react.2.george": "George: Lập đội à? Hừm. Ít ra cũng khôn hơn việc một mình lao thẳng xuống hầm mỏ.",
        "story.react.2.gus": "Gus: Ai cũng cần một người trông chừng sau lưng. Kể cả mấy người cứ khăng khăng rằng mình chẳng cần ai.",
        "story.react.2.lewis": "Lewis: Nếu cậu đang lập một nhóm để bảo vệ Thung Lũng thì nhớ bảo vệ cả nhau nữa. Đây không phải lời gợi ý đâu.",
        "story.react.2.linus": "Linus: Tốt. Cậu đã gặp Marlon. Nhưng đừng nhầm việc được cảnh báo với việc đã an toàn.",
        "story.react.2.marlon": "Marlon: Chọn người đồng hành đầu tiên cho kỹ. Kỹ năng quan trọng. Lòng tin còn quan trọng hơn.",
        "story.react.2.maru": "Maru: Kỹ năng khác nhau, thông tin dùng chung. Đó là mô hình sống sót tốt hơn nhiều.",
        "story.react.2.pierre": "Pierre: Phối hợp à? Được thôi. Chỉ cần nhớ vật tư vẫn phải trả tiền.",
        "story.react.2.robin": "Robin: Chọn những người hiểu mình làm được gì, đừng chọn những người chỉ muốn tỏ ra gan dạ.",
        "story.react.2.wizard": "Wizard: Một ngọn lửa rất dễ bị dập tắt. Một chòm sao thì khó hơn nhiều."
    }
}

for file_name, additions in localization.items():
    path = SRC / "i18n" / file_name
    data = json.loads(path.read_text(encoding="utf-8"))
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.28 George camouflage + milestone reactions materialized.")
