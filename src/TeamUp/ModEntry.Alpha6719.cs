using System.Reflection;
using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private static readonly MethodInfo? MutationIsEligibleAlpha674432 = typeof(MonsterMutationService).GetMethod(
        "IsEligible", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo? MutationTryMutateAlpha674432 = typeof(MonsterMutationService).GetMethod(
        "TryMutate", BindingFlags.Instance | BindingFlags.NonPublic);

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
    {
        MutationAlpha6719.ResetRuntime();
        PelipperVisibleMutationAlpha674413?.ResetTelemetry();
        MutationMinionSpawnAlpha674413?.ResetTelemetry();
        PelipperMutantRewardAlpha674414?.ResetTelemetry();
        MutationLeaderMinionPolicyAlpha674416?.ResetTelemetry();
        NativeMutationMinionsAlpha674418?.ResetTelemetry();
        PelipperSpawnCommandGateAlpha674419?.ResetTelemetry();
        MutationAggroAlpha674420?.ResetTelemetry();
        PelipperMutationLeaderSmoothingAlpha674423?.ResetTelemetry();
    }

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
                if (PelipperModDataHpBindingAlpha674412 is not null)
                    Monitor.Log(PelipperModDataHpBindingAlpha674412.Describe(), LogLevel.Info);
                if (PelipperVisibleMutationAlpha674413 is not null)
                    Monitor.Log(PelipperVisibleMutationAlpha674413.Describe(), LogLevel.Info);
                if (PelipperMutantRewardAlpha674414 is not null)
                    Monitor.Log(PelipperMutantRewardAlpha674414.Describe(), LogLevel.Info);
                if (MutationLeaderMinionPolicyAlpha674416 is not null)
                    Monitor.Log(MutationLeaderMinionPolicyAlpha674416.Describe(), LogLevel.Info);
                if (NativeMutationMinionsAlpha674418 is not null)
                    Monitor.Log(NativeMutationMinionsAlpha674418.Describe(), LogLevel.Info);
                if (PelipperSpawnCommandGateAlpha674419 is not null)
                    Monitor.Log(PelipperSpawnCommandGateAlpha674419.Describe(), LogLevel.Info);
                if (MutationAggroAlpha674420 is not null)
                    Monitor.Log(MutationAggroAlpha674420.Describe(), LogLevel.Info);
                if (PelipperMutationLeaderSmoothingAlpha674423 is not null)
                    Monitor.Log(PelipperMutationLeaderSmoothingAlpha674423.Describe(), LogLevel.Info);
                if (MutationMinionSpawnAlpha674413 is not null)
                    Monitor.Log(MutationMinionSpawnAlpha674413.Describe(), LogLevel.Info);
                if (PelipperSourceProbeAlpha67449 is not null)
                    Monitor.Log(PelipperSourceProbeAlpha67449.Describe(), LogLevel.Info);
                if (PelipperDualHpProbeAlpha674411 is not null)
                    Monitor.Log(PelipperDualHpProbeAlpha674411.Describe(), LogLevel.Info);
                if (PelipperRuntimeAlpha67446 is not null)
                    Monitor.Log(PelipperRuntimeAlpha67446.DescribeMutationBridge(), LogLevel.Info);
                Monitor.Log(MutationAlpha6719.LastMutationLine, LogLevel.Info);
                return;

            case "list":
                foreach (string line in MutationAlpha6719.DescribeCurrentLocation())
                    Monitor.Log(line, LogLevel.Info);
                return;

            case "force":
                Monster? forceTarget = FindPinnedMutationForceTargetAlpha674432();
                if (forceTarget is null)
                {
                    string noTarget = LocalizeMutationForceResult(
                        transformed: false,
                        "No eligible normal hostile monster is available in this location.",
                        displayName: null);
                    Monitor.Log(noTarget, LogLevel.Info);
                    if (Context.IsWorldReady)
                        Game1.showGlobalMessage(noTarget);
                    return;
                }

                string? displayName = ResolvePinnedMutationDisplayNameAlpha674432(forceTarget);
                LogPinnedMutationForceTargetAlpha674432(forceTarget, displayName);

                bool transformed = TryForcePinnedMutationAlpha674432(forceTarget);
                string rawResult = transformed
                    ? $"Forced mutation: {forceTarget.Name} -> HP {forceTarget.Health}/{forceTarget.MaxHealth}."
                    : $"Force mutation was rejected for {forceTarget.Name}.";

                if (!transformed)
                {
                    Alpha674411PelipperDualHpProbeService.ProbeNow(forceTarget);
                    if (PelipperModDataHpBindingAlpha674412 is not null)
                        Monitor.Log(PelipperModDataHpBindingAlpha674412.Describe(), LogLevel.Info);
                    if (PelipperSourceMutationAlpha67448 is not null)
                        Monitor.Log(PelipperSourceMutationAlpha67448.Describe(), LogLevel.Info);
                }

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

    private Monster? FindPinnedMutationForceTargetAlpha674432()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
            return null;

        return Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(IsPinnedMutationEligibleAlpha674432)
            .OrderBy(monster => Microsoft.Xna.Framework.Vector2.DistanceSquared(monster.Position, Game1.player.Position))
            .FirstOrDefault();
    }

    private bool IsPinnedMutationEligibleAlpha674432(Monster monster)
    {
        if (MutationIsEligibleAlpha674432 is null)
            return false;
        try
        {
            return MutationIsEligibleAlpha674432.Invoke(MutationAlpha6719, new object[] { monster }) is true;
        }
        catch (Exception ex)
        {
            Monitor.Log($"[MutationForceTarget] eligibility reflection failed for {monster.GetType().FullName}: {ex.GetType().Name}: {ex.Message}", LogLevel.Warn);
            return false;
        }
    }

    private bool TryForcePinnedMutationAlpha674432(Monster target)
    {
        if (MutationTryMutateAlpha674432 is null)
            return false;
        try
        {
            return MutationTryMutateAlpha674432.Invoke(MutationAlpha6719, new object[] { target, true }) is true;
        }
        catch (TargetInvocationException ex)
        {
            Exception root = ex.InnerException ?? ex;
            Monitor.Log($"[MutationForceTarget] pinned force threw {root.GetType().Name}: {root.Message}", LogLevel.Error);
            return false;
        }
        catch (Exception ex)
        {
            Monitor.Log($"[MutationForceTarget] pinned force failed: {ex.GetType().Name}: {ex.Message}", LogLevel.Error);
            return false;
        }
    }

    private static string? ResolvePinnedMutationDisplayNameAlpha674432(Monster target)
    {
        if (PelipperTownCompatibilityService.IsWildCombatActor(target)
            && PelipperWildEncounterIdentityService.TryResolve(target, out PelipperWildEncounterIdentity identity)
            && !ReferenceEquals(identity.SourceActor, target))
        {
            return identity.DisplayName;
        }

        return string.IsNullOrWhiteSpace(target.displayName) ? target.Name : target.displayName;
    }

    private void LogPinnedMutationForceTargetAlpha674432(Monster target, string? displayName)
    {
        const string currentHpKey = "Griff.PelipperTown/WildCurrentHealth";
        const string maxHpKey = "Griff.PelipperTown/WildMaxHealth";

        bool pelipper = PelipperTownCompatibilityService.IsWildCombatActor(target);
        string currentHp = target.modData.TryGetValue(currentHpKey, out string? currentRaw) ? currentRaw : "<missing>";
        string maxHp = target.modData.TryGetValue(maxHpKey, out string? maxRaw) ? maxRaw : "<missing>";
        string encounter = "<none>";
        string sourceType = "<none>";
        string identityName = displayName ?? target.Name;

        if (pelipper
            && PelipperWildEncounterIdentityService.TryResolve(target, out PelipperWildEncounterIdentity identity)
            && !ReferenceEquals(identity.SourceActor, target))
        {
            encounter = identity.EncounterId;
            sourceType = identity.SourceActor.GetType().FullName ?? identity.SourceActor.GetType().Name;
            identityName = identity.DisplayName;
        }

        Monitor.Log(
            $"[MutationForceTarget] pinned={identityName} proxyType={target.GetType().FullName} pelipper={pelipper} "
            + $"proxyHP={target.Health}/{target.MaxHealth} wildHP={currentHp}/{maxHp} encounter={encounter} sourceType={sourceType}",
            LogLevel.Info);
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
