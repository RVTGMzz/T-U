using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha6619EventsRegistered;
    private bool PelipperApiBridgeConfiguredAlpha6619;
    private bool PelipperRuntimeRootLookupLoggedAlpha6620;
    private readonly HashSet<string> PelipperNonConvergedRoutesAlpha6619 = new(StringComparer.OrdinalIgnoreCase);

    private void EnsureAlpha6619EventsRegistered()
    {
        ConfigurePelipperApiBridgeAlpha6619();
        if (Alpha6619EventsRegistered)
            return;

        Alpha6619EventsRegistered = true;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6619UpdateTicked;
        Helper.Events.GameLoop.DayEnding += OnAlpha6619DayEnding;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6619ReturnedToTitle;
    }

    private void ConfigurePelipperApiBridgeAlpha6619()
    {
        if (PelipperApiBridgeConfiguredAlpha6619)
            return;

        object? modInfo = Helper.ModRegistry.Get(PelipperTownCompatibilityService.ProviderId);
        if (modInfo is null)
            return;

        if (PelipperModRuntimeRootLocator.TryLocate(modInfo, out object? runtimeRoot, out string locatorRoute)
            && runtimeRoot is not null)
        {
            PelipperApiRuntimeRootBridge.Configure(runtimeRoot);
            PelipperApiBridgeConfiguredAlpha6619 = true;
            PelipperRuntimeRootLookupLoggedAlpha6620 = false;
            Monitor.Log(
                $"Alpha 6.6.20 bound Pelipper live runtime root {PelipperApiRuntimeRootBridge.ApiTypeName} via {locatorRoute}.",
                LogLevel.Debug);
            return;
        }

        if (!PelipperRuntimeRootLookupLoggedAlpha6620)
        {
            PelipperRuntimeRootLookupLoggedAlpha6620 = true;
            Monitor.Log(
                $"Alpha 6.6.20 could not resolve Pelipper live ModEntry from SMAPI metadata type {modInfo.GetType().FullName}. Falling back to the legacy assembly bridge; no invalid generic API retry will be attempted.",
                LogLevel.Warn);
        }
    }

    private void OnAlpha6619UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || !e.IsMultipleOf(60))
            return;

        ConfigurePelipperApiBridgeAlpha6619();
    }

    private void OnAlpha6619DayEnding(object? sender, DayEndingEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        PelipperApiRuntimeRootBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperNonConvergedRoutesAlpha6619.Clear();
    }

    private void OnAlpha6619ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PelipperApiRuntimeRootBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperNonConvergedRoutesAlpha6619.Clear();
        PelipperApiRuntimeRootBridge.Reset();
        PelipperApiBridgeConfiguredAlpha6619 = false;
        PelipperRuntimeRootLookupLoggedAlpha6620 = false;
    }

    private bool IsNpcPelipperSourceLiveAlpha6619(NPC owner)
        => PelipperTownCompatibilityService.IsPelipperDescriptor(CompanionIntegrationService.FindLinkedCompanion(owner));

    /// <summary>
    /// Source writes are not considered successful until Pelipper's live descriptor agrees. A
    /// failed or asynchronous route is retried on later reconciliation pulses instead of being
    /// permanently blacklisted after one attempt, which was the key 6.6.18 failure mode.
    /// </summary>
    private bool TrySetPelipperNpcSourceEnabledAlpha6619(NPC owner, bool enabled, string reason)
    {
        ConfigurePelipperApiBridgeAlpha6619();

        bool routed = PelipperApiRuntimeRootBridge.TrySetEnabled(owner.Name, owner, enabled, out string route);
        if (!routed)
            routed = PelipperVillagerCompanionRuntimeBridge.TrySetEnabled(owner.Name, owner, enabled, out route);

        if (routed)
        {
            bool sourceLive = IsNpcPelipperSourceLiveAlpha6619(owner);
            bool converged = enabled ? sourceLive : !sourceLive;
            if (converged)
            {
                PelipperNpcNativeControlWarningsAlpha6618.Remove(owner.Name);
                PelipperNonConvergedRoutesAlpha6619.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));
                Monitor.Log(
                    $"Alpha 6.6.20 verified Pelipper villager source {(enabled ? "deploy" : "recall")} {owner.Name} via {route} ({reason}).",
                    LogLevel.Debug);
                return true;
            }

            string convergenceKey = $"{owner.Name}|{enabled}|{route}";
            if (PelipperNonConvergedRoutesAlpha6619.Add(convergenceKey))
            {
                Monitor.Log(
                    $"Alpha 6.6.20 route {route} accepted the {(enabled ? "deploy" : "recall")} request for {owner.Name}, but Pelipper source is still live={sourceLive}. Team Up keeps the real slot occupied and will retry.",
                    LogLevel.Warn);
            }
            return false;
        }

        if (PelipperNpcNativeControlWarningsAlpha6618.Add(owner.Name))
        {
            Monitor.Log(
                $"Could not locate Pelipper's live villager companion enable/recall contract for {owner.Name}. API root={PelipperApiRuntimeRootBridge.ApiTypeName}. Team Up keeps the source-live Pokemon counted and will retry.",
                LogLevel.Warn);
        }
        return false;
    }

    private void RestorePelipperNpcSourceAlpha6619(NPC owner)
    {
        ConfigurePelipperApiBridgeAlpha6619();
        bool restored = PelipperApiRuntimeRootBridge.Restore(owner.Name, owner, out string route);
        if (!restored)
            restored = PelipperVillagerCompanionRuntimeBridge.Restore(owner.Name, owner, out route);

        if (restored)
            Monitor.Log($"Alpha 6.6.20 restored Pelipper villager companion source setting for {owner.Name} via {route}.", LogLevel.Debug);

        PelipperNpcNativeControlWarningsAlpha6618.Remove(owner.Name);
        PelipperNonConvergedRoutesAlpha6619.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));
    }
}