using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Locations;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const string LowerWorkingsLocationNameAlpha6744 = "Ronvotri.TeamUp_LowerWorkings";
    private LowerWorkingsInteriorSurveyStoryService LowerWorkingsInteriorSurveyAlpha6744 { get; set; } = null!;

    private void RegisterAlpha6744Events()
    {
        LowerWorkingsInteriorSurveyAlpha6744 = new LowerWorkingsInteriorSurveyStoryService(
            Helper,
            Monitor,
            LowerWorkingsLocationNameAlpha6744,
            () => LowerWorkingsDescentAlpha6742.FirstDescentComplete,
            () => EntryProtocolAlpha6740.ProtocolReady,
            () => SurgeHighAlpha6738.IsHigh,
            () => RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
            CountFieldPeopleAtAlpha6732,
            CountActiveStoryNpcAlliesAtAlpha6732,
            () => EntryProtocolAlpha6740.GetRequiredFieldPeople(),
            () => EntryProtocolAlpha6740.GetRequiredNpcAllies());

        Helper.Events.Content.AssetRequested += OnAlpha6744AssetRequested;
        Helper.Events.GameLoop.SaveLoaded += OnAlpha6744SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6744Warped;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6744UpdateTicked;
        Helper.Events.Input.ButtonPressed += OnAlpha6744ButtonPressed;
        Helper.ConsoleCommands.Add(
            "teamup_lower_interior",
            "Lower Workings dedicated interior survey: status | reset | stage <0-6>.",
            OnAlpha6744Command);

        EnsureAlpha67442EncounterReactionsRegistered();
    }

    private void OnAlpha6744AssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Locations"))
            return;

        e.Edit(asset =>
        {
            IDictionary<string, LocationData> data = asset.AsDictionary<string, LocationData>().Data;
            data[LowerWorkingsLocationNameAlpha6744] = new LocationData
            {
                DisplayName = "Lower Workings",
                DefaultArrivalTile = LowerWorkingsInteriorSurveyStoryService.ArrivalTile,
                CreateOnLoad = new CreateLocationData
                {
                    MapPath = Helper.ModContent.GetInternalAssetName("assets/LowerWorkings.tmx").Name,
                    Type = "StardewValley.GameLocation",
                    AlwaysActive = false
                },
                CanPlantHere = false,
                ExcludeFromNpcPathfinding = true,
                MinDailyWeeds = 0,
                MaxDailyWeeds = 0,
                MinDailyForageSpawn = 0,
                MaxDailyForageSpawn = 0,
                MaxSpawnedForageAtOnce = 0
            };
        });
    }

    private void OnAlpha6744SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Context.IsWorldReady)
            LowerWorkingsInteriorSurveyAlpha6744.OnSaveLoaded();
    }

    private void OnAlpha6744Warped(object? sender, WarpedEventArgs e)
    {
        if (Context.IsWorldReady && e.IsLocalPlayer)
            LowerWorkingsInteriorSurveyAlpha6744.OnLocalWarped(e.NewLocation);
    }

    private void OnAlpha6744UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Context.IsWorldReady && Context.IsMainPlayer)
            LowerWorkingsInteriorSurveyAlpha6744.Update();
    }

    private void OnAlpha6744ButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree || !e.Button.IsActionButton())
            return;

        if (LowerWorkingsInteriorSurveyAlpha6744.TryHandleLocalAction())
            Helper.Input.Suppress(e.Button);
    }

    private void OnAlpha6744Command(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_lower_interior.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (action == "reset")
            LowerWorkingsInteriorSurveyAlpha6744.Reset(Game1.MasterPlayer);
        else if (action == "stage" && args.Length >= 2 && int.TryParse(args[1], out int stage) && stage is >= 0 and <= 6)
            LowerWorkingsInteriorSurveyAlpha6744.SetDebugStage(Game1.MasterPlayer, stage);
        else if (action != "status")
        {
            Monitor.Log("Usage: teamup_lower_interior <status|reset|stage 0-6>", LogLevel.Info);
            return;
        }

        WriteAlpha6744Diagnostic();
    }

    private void WriteAlpha6744Diagnostic()
    {
        GameLocation? lower = Game1.getLocationFromName(LowerWorkingsLocationNameAlpha6744);
        List<string> lines = new()
        {
            "TEAM UP 6.7.44 - DEDICATED LOWER WORKINGS MAP / INTERIOR SURVEY",
            Origin.Describe(),
            LowerWorkingsDescentAlpha6742.Describe(Game1.currentLocation),
            LowerWorkingsInteriorSurveyAlpha6744.Describe(Game1.currentLocation),
            $"Dedicated location: name={LowerWorkingsLocationNameAlpha6744} loaded={lower is not null} asset=assets/LowerWorkings.tmx",
            $"Formation requirement: people={EntryProtocolAlpha6740.GetRequiredFieldPeople()} | activeNpcAllies={EntryProtocolAlpha6740.GetRequiredNpcAllies()} | hard people cap remains 5.",
            "Route: Guild survey order -> persisted breach MineShaft + tile anchor -> dedicated Lower Workings map -> three 120-tick clue surveys -> safe return at entry -> Guild report.",
            "Clues: directed emergency cribbing -> fresh Mutation-linked residue over older blast scoring -> deeper sealed pressure edge. Historical worker identity remains unknown.",
            "No story monster, boss, George reveal, Evelyn postgame reveal, roster expansion, Mutation trigger rewrite, or Pelipper capture rewrite is introduced by 6.7.44."
        };

        string dir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "TeamUp_Lower_Workings_Interior_latest.txt");
        File.WriteAllLines(path, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Lower Workings interior diagnostic saved: {path}", LogLevel.Info);
    }

    private int GetStoryReactionWindowAlpha6744()
    {
        if (LowerWorkingsDescentAlpha6742.Stage < LowerWorkingsDescentStoryService.CompleteStage)
            return GetStoryReactionWindowAlpha6743();

        return LowerWorkingsInteriorSurveyAlpha6744.Stage switch
        {
            <= 0 => 35,
            1 => 36,
            2 => 37,
            3 => 38,
            4 => 39,
            5 => 40,
            _ => 41
        };
    }
}
