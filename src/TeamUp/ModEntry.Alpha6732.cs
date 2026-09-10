using Ronvotri.TeamUp.Core;
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
