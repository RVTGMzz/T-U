using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private MarlonInvestigationStoryService MarlonInvestigationAlpha6729 { get; set; } = null!;

    private void RegisterAlpha6729Events()
    {
        MarlonInvestigationAlpha6729 = new MarlonInvestigationStoryService(
            Helper,
            Monitor,
            () => Origin.Stage,
            HasActiveStoryAllyAtAlpha6729,
            OnMarlonInvestigationDebriefCompleteAlpha6729);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6729SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6729Warped;
        Helper.ConsoleCommands.Add(
            "teamup_marlon_case",
            "Marlon investigation: status | reset | stage <0-4>. Status writes diagnostics/TeamUp_Marlon_Investigation_latest.txt.",
            OnAlpha6729MarlonCaseCommand);
    }

    private void OnAlpha6729SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;
        MarlonInvestigationAlpha6729.OnSaveLoaded();
    }

    private void OnAlpha6729Warped(object? sender, WarpedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !e.IsLocalPlayer)
            return;
        MarlonInvestigationAlpha6729.OnWarped(e.NewLocation);
    }

    private bool HasActiveStoryAllyAtAlpha6729(GameLocation location)
    {
        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        foreach (PartyMemberData member in Party.Members)
        {
            if (!online.Contains(member.RecruiterId)
                || member.State is not (PartyMemberState.Following or PartyMemberState.Waiting))
            {
                continue;
            }

            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc?.currentLocation == location)
                return true;
        }
        return false;
    }

    private void OnMarlonInvestigationDebriefCompleteAlpha6729()
    {
        bool unlocked = RosterProgressionAlpha6727.UnlockTo(
            Game1.MasterPlayer,
            2,
            "marlon-investigation-debrief");

        EnforceStoryRosterCapacityAlpha6727();
        SavePartyNow();
        BroadcastPartySnapshot();

        if (unlocked)
        {
            ShowHud(Helper.Translation.Get("story.roster.second-unlock", new
            {
                unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
                max = TeamUpRosterProgressionService.MaxStoryNpcSlots
            }));
        }
    }

    private int GetStoryReactionWindowAlpha6729()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        return MarlonInvestigationAlpha6729.Stage switch
        {
            <= 0 => 2,
            1 => 3,
            2 => 4,
            3 => 5,
            _ => 6
        };
    }

    private void OnAlpha6729MarlonCaseCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_marlon_case.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "status":
                WriteAlpha6729MarlonCaseDiagnostic();
                return;

            case "reset":
                MarlonInvestigationAlpha6729.Reset(Game1.MasterPlayer);
                WriteAlpha6729MarlonCaseDiagnostic();
                return;

            case "stage":
                if (args.Length < 2 || !int.TryParse(args[1], out int stage) || stage < 0 || stage > MarlonInvestigationStoryService.CompleteStage)
                {
                    Monitor.Log("Usage: teamup_marlon_case stage <0-4>", LogLevel.Info);
                    return;
                }
                MarlonInvestigationAlpha6729.SetDebugStage(Game1.MasterPlayer, stage);
                WriteAlpha6729MarlonCaseDiagnostic();
                return;

            default:
                Monitor.Log("Usage: teamup_marlon_case <status|reset|stage 0-4>", LogLevel.Info);
                return;
        }
    }

    private void WriteAlpha6729MarlonCaseDiagnostic()
    {
        int reactionWindow = GetStoryReactionWindowAlpha6729();
        List<string> lines = new()
        {
            "TEAM UP 6.7.29 - MARLON INVESTIGATION + REACTIONS",
            Origin.Describe(),
            MarlonInvestigationAlpha6729.Describe(),
            $"Active story ally present here: {HasActiveStoryAllyAtAlpha6729(Game1.currentLocation)}",
            $"Reaction window: {reactionWindow} | seen={MilestoneReactionsAlpha6728.GetSeenCount(Game1.player, reactionWindow)}/{MilestoneReactionsAlpha6728.GetAvailableCount(reactionWindow)}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Expected route: first ally -> Guild briefing -> MineShaft trail -> defeat a natural Mutant with ally -> Guild debrief -> slot 2.",
            "George remains pre-reveal non-combatant. This chapter must not set GeorgeCombatRevealed."
        };

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Marlon_Investigation_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Marlon investigation diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
