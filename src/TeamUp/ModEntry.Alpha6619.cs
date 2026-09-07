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
        EnsureAlpha6621Registered();
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
            PelipperVillagerLifecycleBridge.Configure(runtimeRoot);
            PelipperApiBridgeConfiguredAlpha6619 = true;
            PelipperRuntimeRootLookupLoggedAlpha6620 = false;
            Monitor.Log(
                $"Alpha 6.6.21 bound Pelipper live runtime root {PelipperVillagerLifecycleBridge.RootTypeName} via {locatorRoute}.",
                LogLevel.Debug);
            return;
        }

        if (!PelipperRuntimeRootLookupLoggedAlpha6620)
        {
            PelipperRuntimeRootLookupLoggedAlpha6620 = true;
            Monitor.Log(
                $"Alpha 6.6.21 could not resolve Pelipper live ModEntry from SMAPI metadata type {modInfo.GetType().FullName}. Falling back to the legacy assembly bridge; no invalid generic API retry will be attempted.",
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

        PelipperVillagerLifecycleBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperApiRuntimeRootBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperNonConvergedRoutesAlpha6619.Clear();
    }

    private void OnAlpha6619ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        PelipperVillagerLifecycleBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperApiRuntimeRootBridge.RestoreAll(name => Game1.getCharacterFromName(name));
        PelipperNonConvergedRoutesAlpha6619.Clear();
        PelipperVillagerLifecycleBridge.Reset();
        PelipperApiRuntimeRootBridge.Reset();
        PelipperApiBridgeConfiguredAlpha6619 = false;
        PelipperRuntimeRootLookupLoggedAlpha6620 = false;
    }

    private bool IsNpcPelipperSourceLiveAlpha6619(NPC owner)
        => PelipperTownCompatibilityService.IsPelipperDescriptor(CompanionIntegrationService.FindLinkedCompanion(owner));

    private bool TrySetPelipperNpcSourceEnabledAlpha6619(NPC owner, bool enabled, string reason)
        => TrySetPelipperNpcSourceEnabledAlpha6621(owner, enabled, reason);

    private void RestorePelipperNpcSourceAlpha6619(NPC owner)
        => RestorePelipperNpcSourceAlpha6621(owner);
}
