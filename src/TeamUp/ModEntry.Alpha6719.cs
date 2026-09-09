using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private MonsterMutationService MutationAlpha6719 { get; set; } = null!;

    private void RegisterAlpha6719Events()
    {
        MutationAlpha6719 = new MonsterMutationService(
            Monitor,
            ModManifest.UniqueID,
            () => Config.EnableMutationEncounters,
            () => Config.MutationChancePercent,
            () => Config.MutationHealthMultiplier,
            () => Config.MutationStatMultiplier,
            () => Config.MutationVisualScaleMultiplier,
            () => Config.MutationMinionMin,
            () => Config.MutationMinionMax,
            () => Config.MutationMinionsDropLoot);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6719SaveLoaded;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6719ReturnedToTitle;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6719UpdateTicked;
        Helper.Events.Display.RenderedWorld += OnAlpha6719RenderedWorld;

        Helper.ConsoleCommands.Add(
            "teamup_mutation",
            "Mutation encounters: status | list | force. Force transforms the nearest eligible normal monster for testing.",
            OnAlpha6719MutationCommand);
    }

    private void OnAlpha6719SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer)
            return;
        MutationAlpha6719.ResetRuntime();
    }

    private void OnAlpha6719ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        => MutationAlpha6719.ResetRuntime();

    private void OnAlpha6719UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;
        MutationAlpha6719.Update();
    }

    private void OnAlpha6719RenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.eventUp)
            return;
        if (Game1.activeClickableMenu is not null && !Game1.dialogueUp)
            return;
        MutationAlpha6719.DrawAura(e.SpriteBatch);
    }

    private void OnAlpha6719MutationCommand(string command, string[] args)
    {
        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "status":
                Monitor.Log(MutationAlpha6719.Describe(), LogLevel.Info);
                Monitor.Log(MutationAlpha6719.LastMutationLine, LogLevel.Info);
                return;

            case "list":
                foreach (string line in MutationAlpha6719.DescribeCurrentLocation())
                    Monitor.Log(line, LogLevel.Info);
                return;

            case "force":
                MutationAlpha6719.ForceNearestEligible(out string result);
                Monitor.Log(result, LogLevel.Info);
                if (Context.IsWorldReady)
                    Game1.showGlobalMessage(result);
                return;

            default:
                Monitor.Log("Usage: teamup_mutation <status|list|force>", LogLevel.Info);
                return;
        }
    }
}
