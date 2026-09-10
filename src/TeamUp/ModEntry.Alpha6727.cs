using Ronvotri.TeamUp.Core;
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
