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
                if (PelipperSpeciesPairingAlpha674410 is not null)
                    Monitor.Log(PelipperSpeciesPairingAlpha674410.Describe(), LogLevel.Info);
                if (PelipperSourceMutationAlpha67448 is not null)
                    Monitor.Log(PelipperSourceMutationAlpha67448.Describe(), LogLevel.Info);
                if (PelipperSourceProbeAlpha67449 is not null)
                    Monitor.Log(PelipperSourceProbeAlpha67449.Describe(), LogLevel.Info);
                if (PelipperRuntimeAlpha67446 is not null)
                    Monitor.Log(PelipperRuntimeAlpha67446.DescribeMutationBridge(), LogLevel.Info);
                Monitor.Log(MutationAlpha6719.LastMutationLine, LogLevel.Info);
                return;

            case "list":
                foreach (string line in MutationAlpha6719.DescribeCurrentLocation())
                    Monitor.Log(line, LogLevel.Info);
                return;

            case "force":
                // Capture the exact eligible target display name before the generic service runs.
                // Pelipper's hidden combat actor is often named Green Slime; user-facing text must
                // always prefer the Pokemon-facing display name and never expose the proxy identity.
                string? displayName = PelipperSourceProbeAlpha67449?.CaptureForceTargetDisplayName();
                bool transformed = MutationAlpha6719.ForceNearestEligible(out string rawResult);
                string result = LocalizeMutationForceResult(transformed, rawResult, displayName);
                Monitor.Log(result, LogLevel.Info);
                if (Context.IsWorldReady)
                    Game1.showGlobalMessage(result);
                return;

            default:
                Monitor.Log("Usage: teamup_mutation <status|list|force>", LogLevel.Info);
                return;
        }
    }

    private string LocalizeMutationForceResult(bool transformed, string rawResult, string? displayName)
    {
        bool vi = Helper.Translation.Locale.StartsWith("vi", StringComparison.OrdinalIgnoreCase);

        if (!Context.IsWorldReady || rawResult.StartsWith("Load a save", StringComparison.OrdinalIgnoreCase))
            return vi ? "Hãy tải save trước khi thử cưỡng chế đột biến." : "Load a save before forcing a mutation.";

        if (string.IsNullOrWhiteSpace(displayName)
            && rawResult.StartsWith("No eligible", StringComparison.OrdinalIgnoreCase))
        {
            return vi
                ? "Không có quái thường hợp lệ gần đây để cưỡng chế đột biến."
                : "No eligible normal hostile is nearby for a forced mutation.";
        }

        string targetName = CleanMutationTargetName(string.IsNullOrWhiteSpace(displayName) ? "mục tiêu" : displayName.Trim());
        if (transformed)
        {
            return vi
                ? $"Đã cưỡng chế đột biến: {targetName}."
                : $"Forced mutation: {targetName}.";
        }

        return vi
            ? $"Không thể cưỡng chế đột biến cho {targetName}."
            : $"Forced mutation was rejected for {targetName}.";
    }

    private static string CleanMutationTargetName(string raw)
    {
        string value = raw.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (string prefix in new[] { "Wild ", "Shiny " })
            {
                if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                value = value[prefix.Length..].Trim();
                changed = true;
            }
        } while (changed && value.Length > 0);
        return string.IsNullOrWhiteSpace(value) ? "Pokémon" : value;
    }
}
