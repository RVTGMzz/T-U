using Ronvotri.TeamUp.Core;
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

        if (!MilestoneReactionsAlpha6728.TryConsume(Game1.player, GetStoryReactionWindowAlpha6735(), npc.Name, out string key))
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
        int stage = GetStoryReactionWindowAlpha6735();
        List<string> lines = new()
        {
            "TEAM UP 6.7.35 - SEALED CORRIDOR APPROACH REACTIONS",
            Origin.Describe(),
            $"Current reaction window: stage={stage} seen={MilestoneReactionsAlpha6728.GetSeenCount(Game1.player, stage)}/{MilestoneReactionsAlpha6728.GetAvailableCount(stage)}",
            $"George combat reveal flag: {IsGeorgeCombatRevealedAlpha6728()}",
            $"George pre-reveal combat lock: {IsGeorgePreRevealLockedAlpha6728("George")}",
            "George expected before the future finale reveal: observed Rank D, NON-COMBATANT, no visible combat skill, recruitment blocked.",
            "Reaction windows: 0..13 previous story | 14=pressure-survey briefing | 15=survey face marked | 16=pressure survey complete | 17=sealed access face confirmed."
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
