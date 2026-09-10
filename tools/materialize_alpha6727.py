from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"

VERSION_OLD = "0.2.0-alpha.6.7.26"
VERSION_NEW = "0.2.0-alpha.6.7.27"


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:180]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


# Version bump.
replace_once(
    SRC / "TeamUp.csproj",
    f"<Version>{VERSION_OLD}</Version>",
    f"<Version>{VERSION_NEW}</Version>",
)

# Story-owned roster progression. The long-term party invariant remains five PEOPLE total,
# including every online Farmer. The story unlocks NPC ally slots 0 -> 1 -> 2 -> 3 -> 4.
roster_service = r'''using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Story;

/// <summary>
/// Alpha 6.7.27 story roster progression.
///
/// The main story unlocks NPC ally capacity gradually instead of exposing the full party on install.
/// This service stores only the story allowance (0..4 NPC allies). The global five-person formation
/// cap, including online Farmers, remains authoritative elsewhere and can further reduce the number
/// of NPC allies that may be active in multiplayer.
///
/// 6.7.27 implements the first real unlock only: once the Linus -> Marlon bridge reaches stage 2,
/// one NPC ally slot becomes available. Later chapters can call UnlockTo(...) for slots 2..4.
/// </summary>
internal sealed class TeamUpRosterProgressionService
{
    public const int MaxStoryNpcSlots = 4;
    public const string UnlockedNpcSlotsKey = "Ronvotri.TeamUp/Story/UnlockedNpcSlots";

    private readonly IMonitor _monitor;
    private readonly Func<int> _getNarrativeStage;

    public TeamUpRosterProgressionService(IMonitor monitor, Func<int> getNarrativeStage)
    {
        _monitor = monitor;
        _getNarrativeStage = getNarrativeStage;
    }

    public int GetUnlockedNpcSlots(Farmer storyOwner)
        => Math.Clamp(ReadStoredSlots(storyOwner), 0, MaxStoryNpcSlots);

    public bool SyncFromStory(Farmer storyOwner, out int before, out int after)
    {
        before = GetUnlockedNpcSlots(storyOwner);
        after = before;

        if (_getNarrativeStage() >= 2 && after < 1)
            after = 1;

        if (after == before)
            return false;

        storyOwner.modData[UnlockedNpcSlotsKey] = after.ToString();
        _monitor.Log($"[RosterStory] story sync unlocked NPC ally slots {before}->{after}.", LogLevel.Info);
        return true;
    }

    public bool UnlockTo(Farmer storyOwner, int requestedSlots, string source)
    {
        int before = GetUnlockedNpcSlots(storyOwner);
        int after = Math.Clamp(Math.Max(before, requestedSlots), 0, MaxStoryNpcSlots);
        if (after == before)
            return false;

        storyOwner.modData[UnlockedNpcSlotsKey] = after.ToString();
        _monitor.Log($"[RosterStory] NPC ally slots {before}->{after} source={source}.", LogLevel.Info);
        return true;
    }

    public void SetDebugSlots(Farmer storyOwner, int slots)
    {
        int clamped = Math.Clamp(slots, 0, MaxStoryNpcSlots);
        storyOwner.modData[UnlockedNpcSlotsKey] = clamped.ToString();
        _monitor.Log($"[RosterStory] debug NPC ally slots set to {clamped}/{MaxStoryNpcSlots}.", LogLevel.Info);
    }

    public void Reset(Farmer storyOwner)
    {
        storyOwner.modData.Remove(UnlockedNpcSlotsKey);
        _monitor.Log("[RosterStory] story roster allowance reset. Narrative progress was not changed.", LogLevel.Info);
    }

    public string Describe(Farmer storyOwner, int onlineFarmers, int activeNpcAllies, int configuredPeopleCap)
    {
        int unlocked = GetUnlockedNpcSlots(storyOwner);
        int peopleCap = Math.Clamp(configuredPeopleCap, 1, 5);
        int farmerCount = Math.Clamp(onlineFarmers, 1, 5);
        int effectiveNpcSlots = Math.Min(unlocked, Math.Max(0, peopleCap - farmerCount));
        return $"Story Roster: narrativeStage={_getNarrativeStage()} | unlockedNpcSlots={unlocked}/{MaxStoryNpcSlots} | "
            + $"onlineFarmers={farmerCount} | activeNpcAllies={activeNpcAllies}/{effectiveNpcSlots} | peopleCap={peopleCap}/5";
    }

    private static int ReadStoredSlots(Farmer storyOwner)
    {
        if (!storyOwner.modData.TryGetValue(UnlockedNpcSlotsKey, out string? raw)
            || !int.TryParse(raw, out int value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, MaxStoryNpcSlots);
    }
}
'''
(SRC / "Story" / "TeamUpRosterProgressionService.cs").write_text(roster_service, encoding="utf-8", newline="\n")

# Roster gate / diagnostics live in a partial entry file so the old multiplayer party manager can
# keep its existing hard-cap semantics. Story capacity is checked before authoritative recruit and
# before reactivating an inactive member.
alpha6727 = r'''using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private TeamUpRosterProgressionService RosterProgressionAlpha6727 { get; set; } = null!;

    private void RegisterAlpha6727Events()
    {
        RosterProgressionAlpha6727 = new TeamUpRosterProgressionService(Monitor, () => Origin.Stage);
        Helper.Events.GameLoop.SaveLoaded += OnAlpha6727SaveLoaded;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6727UpdateTicked;

        Helper.ConsoleCommands.Add(
            "teamup_roster_story",
            "Story roster progression: status | reset | setslots <0-4> | sync. Status writes diagnostics/TeamUp_Roster_Progression_latest.txt.",
            OnAlpha6727RosterStoryCommand);
    }

    private void OnAlpha6727SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        // Existing saves that already completed Marlon's bridge receive slot 1 silently.
        RosterProgressionAlpha6727.SyncFromStory(Game1.MasterPlayer, out _, out _);
        EnforceStoryRosterCapacityAlpha6727();
    }

    private void OnAlpha6727UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !e.IsMultipleOf(15))
            return;

        if (!RosterProgressionAlpha6727.SyncFromStory(Game1.MasterPlayer, out int before, out int after))
            return;

        EnforceStoryRosterCapacityAlpha6727();
        if (after > before)
        {
            ShowHud(Helper.Translation.Get("story.roster.first-unlock", new
            {
                unlocked = after,
                max = TeamUpRosterProgressionService.MaxStoryNpcSlots
            }));
        }
    }

    private bool IsBaseRecruitableNpcForAlpha6727(NPC npc, Farmer farmer)
    {
        if (CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc))
            return CustomNpcCompatibilityService.CanRecruit(npc, Helper.ModRegistry, farmer);

        return CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames);
    }

    private bool CanRecruitByStoryAlpha6727(Farmer farmer, out string failure)
        => CanUseNpcSlotAlpha6727(farmer.UniqueMultiplayerID, out failure);

    private bool CanActivateRosterSlotAlpha6727(long recruiterId, out string failure)
        => CanUseNpcSlotAlpha6727(recruiterId, out failure);

    private bool CanUseNpcSlotAlpha6727(long recruiterId, out string failure)
    {
        failure = string.Empty;
        if (!Context.IsWorldReady)
        {
            failure = "Team Up story roster is unavailable until a save is loaded.";
            return false;
        }

        int unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        if (unlocked <= 0)
        {
            failure = Helper.Translation.Get("story.roster.locked");
            return false;
        }

        int effective = GetEffectiveNpcSlotLimitAlpha6727();
        int active = GetActiveStoryNpcCountAlpha6727();
        if (effective <= 0)
        {
            failure = Helper.Translation.Get("story.roster.multiplayer-full", new
            {
                farmers = GetOnlineFarmerIds().Count,
                people = Math.Clamp(Config.MaxPartyMembers, 1, 5)
            });
            return false;
        }

        if (active >= effective)
        {
            failure = Helper.Translation.Get("story.roster.limit", new
            {
                used = active,
                max = effective,
                unlocked
            });
            return false;
        }

        return true;
    }

    private int GetEffectiveNpcSlotLimitAlpha6727()
    {
        int storySlots = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        int peopleCap = Math.Clamp(Config.MaxPartyMembers, 1, 5);
        int farmerCount = Math.Clamp(GetOnlineFarmerIds().Count, 1, 5);
        return Math.Min(storySlots, Math.Max(0, peopleCap - farmerCount));
    }

    private int GetActiveStoryNpcCountAlpha6727()
    {
        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        return Party.Members.Count(member =>
            online.Contains(member.RecruiterId)
            && member.State is PartyMemberState.Following or PartyMemberState.Waiting);
    }

    private string GetRosterProfileStatusAlpha6727(NPC npc)
    {
        if (!IsBaseRecruitableNpcForAlpha6727(npc, Game1.player))
            return Helper.Translation.Get("profile.status-special");

        int unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer);
        if (unlocked <= 0)
            return Helper.Translation.Get("profile.status-story-locked");

        return CanRecruitByStoryAlpha6727(Game1.player, out _)
            ? Helper.Translation.Get("profile.status-recruitable")
            : Helper.Translation.Get("profile.status-team-limit");
    }

    private int EnforceStoryRosterCapacityAlpha6727()
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return 0;

        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        int allowed = GetEffectiveNpcSlotLimitAlpha6727();
        List<PartyMemberData> active = Party.Members
            .Where(member => online.Contains(member.RecruiterId))
            .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .ToList();

        int deactivated = 0;
        for (int i = allowed; i < active.Count; i++)
        {
            PartyMemberData member = active[i];
            member.State = PartyMemberState.Inactive;
            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc is not null)
                Follow.ReleaseToVanillaAndResumeSchedule(npc);
            deactivated++;
        }

        if (deactivated > 0)
        {
            SavePartyNow();
            BroadcastPartySnapshot();
            Monitor.Log($"[RosterStory] deactivated {deactivated} NPC ally/allies to enforce story capacity {allowed}.", LogLevel.Info);
        }

        return deactivated;
    }

    private void OnAlpha6727RosterStoryCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_roster_story.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "status":
                WriteAlpha6727RosterDiagnostic();
                return;

            case "reset":
                RosterProgressionAlpha6727.Reset(Game1.MasterPlayer);
                EnforceStoryRosterCapacityAlpha6727();
                WriteAlpha6727RosterDiagnostic();
                return;

            case "setslots":
                if (args.Length < 2 || !int.TryParse(args[1], out int slots) || slots < 0 || slots > TeamUpRosterProgressionService.MaxStoryNpcSlots)
                {
                    Monitor.Log("Usage: teamup_roster_story setslots <0-4>", LogLevel.Info);
                    return;
                }
                RosterProgressionAlpha6727.SetDebugSlots(Game1.MasterPlayer, slots);
                EnforceStoryRosterCapacityAlpha6727();
                WriteAlpha6727RosterDiagnostic();
                return;

            case "sync":
                RosterProgressionAlpha6727.SyncFromStory(Game1.MasterPlayer, out _, out _);
                EnforceStoryRosterCapacityAlpha6727();
                WriteAlpha6727RosterDiagnostic();
                return;

            default:
                Monitor.Log("Usage: teamup_roster_story <status|reset|setslots 0-4|sync>", LogLevel.Info);
                return;
        }
    }

    private void WriteAlpha6727RosterDiagnostic()
    {
        int active = GetActiveStoryNpcCountAlpha6727();
        int online = GetOnlineFarmerIds().Count;
        int effective = GetEffectiveNpcSlotLimitAlpha6727();
        List<string> lines = new()
        {
            "TEAM UP 6.7.27 - STORY ROSTER PROGRESSION",
            Origin.Describe(),
            RosterProgressionAlpha6727.Describe(Game1.MasterPlayer, online, active, Config.MaxPartyMembers),
            $"Effective NPC ally slots now: {effective}",
            "Expected before Marlon bridge: unlockedNpcSlots=0 and recruitment blocked.",
            "Expected after Marlon bridge stage 2: unlockedNpcSlots=1 and exactly one NPC ally may be active.",
            "Future chapters will unlock slots 2, 3, and 4. Five PEOPLE total remains the hard cap including online Farmers."
        };

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Roster_Progression_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Story roster diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6727.cs").write_text(alpha6727, encoding="utf-8", newline="\n")

# Main entry: five people is now enforced as a real maximum at config normalization, register 6.7.27,
# and make Codex status explain story locks instead of mislabelling ordinary villagers as companions.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
entry = entry.replace(
    "Config.MaxPartyMembers = Math.Clamp(Config.MaxPartyMembers, 1, 6);",
    "Config.MaxPartyMembers = Math.Clamp(Config.MaxPartyMembers, 1, 5);",
    1,
)
if "RegisterAlpha6727Events();" not in entry:
    entry = entry.replace("        RegisterAlpha6726Events();\n", "        RegisterAlpha6726Events();\n        RegisterAlpha6727Events();\n", 1)
entry = entry.replace(
    "loaded. Codex discovery + first Surge narrative bridge active.",
    "loaded. Codex discovery + story roster progression active.",
    1,
)
entry = entry.replace(
    '''        NPC? npc = Game1.getCharacterFromName(characterName);\n        if (npc is not null && IsRecruitableNpc(npc))\n            return Helper.Translation.Get("profile.status-recruitable");\n\n        return Helper.Translation.Get("profile.status-special");''',
    '''        NPC? npc = Game1.getCharacterFromName(characterName);\n        if (npc is not null)\n            return GetRosterProfileStatusAlpha6727(npc);\n\n        return Helper.Translation.Get("profile.status-special");''',
    1,
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

# Multiplayer authoritative recruitment / reactivation must respect story slots, not only the hard
# five-person cap. Keep companion logic and all provider ownership untouched.
alpha661_path = SRC / "ModEntry.Alpha661.cs"
alpha661 = alpha661_path.read_text(encoding="utf-8")
old_recruitability = '''    private bool IsRecruitableNpcFor(NPC npc, Farmer farmer)\n    {\n        if (CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc))\n            return CustomNpcCompatibilityService.CanRecruit(npc, Helper.ModRegistry, farmer);\n\n        return CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames);\n    }'''
new_recruitability = '''    private bool IsRecruitableNpcFor(NPC npc, Farmer farmer)\n    {\n        if (!IsBaseRecruitableNpcForAlpha6727(npc, farmer))\n            return false;\n\n        return CanRecruitByStoryAlpha6727(farmer, out _);\n    }'''
if old_recruitability not in alpha661:
    raise RuntimeError("Alpha661 base recruitability block not found")
alpha661 = alpha661.replace(old_recruitability, new_recruitability, 1)

old_authoritative_gate = '''        long recruiterId = recruiter.UniqueMultiplayerID;\n        if (!IsRecruitableNpcFor(npc, recruiter))\n        {\n            SendActionResult(responsePlayerId, false, "This NPC is not currently recruitable.");\n            return;\n        }'''
new_authoritative_gate = '''        long recruiterId = recruiter.UniqueMultiplayerID;\n        if (!IsBaseRecruitableNpcForAlpha6727(npc, recruiter))\n        {\n            SendActionResult(responsePlayerId, false, "This NPC is not currently recruitable.");\n            return;\n        }\n\n        if (!CanRecruitByStoryAlpha6727(recruiter, out string rosterFailure))\n        {\n            SendActionResult(responsePlayerId, false, rosterFailure);\n            return;\n        }'''
if old_authoritative_gate not in alpha661:
    raise RuntimeError("Alpha661 authoritative recruit gate not found")
alpha661 = alpha661.replace(old_authoritative_gate, new_authoritative_gate, 1)

old_activation_gate = '''            if (state == PartyMemberState.Following\n                && member.State == PartyMemberState.Inactive\n                && Party.GetSharedPeopleCount(GetOnlineFarmerIds()) >= Math.Clamp(Config.MaxPartyMembers, 1, 6))\n            {\n                SendActionResult(responsePlayerId, false, Helper.Translation.Get("party.full-hud", new { max = Config.MaxPartyMembers }));\n                return false;\n            }'''
new_activation_gate = '''            if (state == PartyMemberState.Following\n                && member.State == PartyMemberState.Inactive\n                && !CanActivateRosterSlotAlpha6727(recruiterId, out string rosterFailure))\n            {\n                SendActionResult(responsePlayerId, false, rosterFailure);\n                return false;\n            }'''
if old_activation_gate not in alpha661:
    raise RuntimeError("Alpha661 inactive-member activation gate not found")
alpha661 = alpha661.replace(old_activation_gate, new_activation_gate, 1)
alpha661_path.write_text(alpha661, encoding="utf-8", newline="\n")

# Localized story roster UI. Keep EN/VI key parity exact.
localization = {
    "default.json": {
        "story.roster.first-unlock": "TEAM UP • Marlon's warning changes the rules. You can now bring one NPC ally. Story slots: {{unlocked}}/{{max}}.",
        "story.roster.locked": "TEAM UP is not available yet. Follow the Surge investigation and speak with Marlon.",
        "story.roster.limit": "TEAM UP LIMIT • {{used}}/{{max}} NPC ally slots are active. Continue the story to expand the team. Story unlock: {{unlocked}}/4.",
        "story.roster.multiplayer-full": "TEAM UP LIMIT • {{farmers}} online Farmers already use the five-person formation capacity (configured people cap: {{people}}).",
        "profile.status-story-locked": "Team Up Locked",
        "profile.status-team-limit": "Team Limit Reached"
    },
    "vi.json": {
        "story.roster.first-unlock": "TEAM UP • Lời cảnh báo của Marlon đã thay đổi cuộc chơi. Giờ bạn có thể đưa 1 NPC đồng hành. Ô cốt truyện: {{unlocked}}/{{max}}.",
        "story.roster.locked": "TEAM UP chưa được mở. Hãy tiếp tục điều tra The Surge và tìm Marlon.",
        "story.roster.limit": "GIỚI HẠN TEAM UP • Đang dùng {{used}}/{{max}} ô NPC đồng hành. Hãy tiếp tục cốt truyện để mở rộng đội. Tiến độ mở khóa: {{unlocked}}/4.",
        "story.roster.multiplayer-full": "GIỚI HẠN TEAM UP • {{farmers}} Farmer đang online đã chiếm dung lượng đội hình 5 người (giới hạn cấu hình: {{people}} người).",
        "profile.status-story-locked": "Team Up chưa mở",
        "profile.status-team-limit": "Đã chạm giới hạn đội"
    }
}

for file_name, additions in localization.items():
    path = SRC / "i18n" / file_name
    data = json.loads(path.read_text(encoding="utf-8"))
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.27 story roster progression materialized.")
