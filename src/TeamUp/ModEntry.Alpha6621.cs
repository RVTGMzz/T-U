using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha6621Registered;
    // Alpha 6.6.23 acceptance: suppress repeated lifecycle retries to prevent flicker.

    /// <summary>
    /// Alpha 6.6.21 adds a source-lifecycle probe so a stubborn Pelipper integration can be
    /// diagnosed from the user's exact installed version without taking render/movement control.
    /// </summary>
    private void EnsureAlpha6621Registered()
    {
        if (Alpha6621Registered)
            return;

        Alpha6621Registered = true;
        Helper.ConsoleCommands.Add(
            "teamup_pelipper_probe",
            "Inspect Pelipper Town villager companion lifecycle candidates. Usage: teamup_pelipper_probe <NPC name>.",
            OnPelipperProbeAlpha6621);
    }

    private bool TrySetPelipperNpcSourceEnabledAlpha6621(NPC owner, bool enabled, string reason)
    {
        ConfigurePelipperApiBridgeAlpha6619();

        bool routed = PelipperVillagerLifecycleBridge.TrySetEnabled(owner.Name, owner, enabled, out string route);
        if (!routed)
            routed = PelipperApiRuntimeRootBridge.TrySetEnabled(owner.Name, owner, enabled, out route);
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
                    $"Alpha 6.6.21 verified Pelipper villager source {(enabled ? "deploy" : "recall")} {owner.Name} via {route} ({reason}).",
                    LogLevel.Debug);
                return true;
            }

            string convergenceKey = $"{owner.Name}|{enabled}|{route}";
            if (PelipperNonConvergedRoutesAlpha6619.Add(convergenceKey))
            {
                Monitor.Log(
                    $"Alpha 6.6.21 routed {(enabled ? "deploy" : "recall")} for {owner.Name} via {route}, but Pelipper source is still live={sourceLive}. The real slot remains occupied; Team Up will retry. If this persists, run teamup_pelipper_probe {owner.Name}.",
                    LogLevel.Warn);
            }
            return false;
        }

        if (PelipperNpcNativeControlWarningsAlpha6618.Add(owner.Name))
        {
            Monitor.Log(
                $"Alpha 6.6.21 found no Pelipper villager lifecycle route for {owner.Name}. runtimeRoot={PelipperVillagerLifecycleBridge.RootTypeName}. The source-live Pokemon stays counted. Run teamup_pelipper_probe {owner.Name} for exact candidates.",
                LogLevel.Warn);
        }
        return false;
    }

    private void RestorePelipperNpcSourceAlpha6621(NPC owner)
    {
        ConfigurePelipperApiBridgeAlpha6619();

        bool restored = PelipperVillagerLifecycleBridge.Restore(owner.Name, owner, out string route);
        if (!restored)
            restored = PelipperApiRuntimeRootBridge.Restore(owner.Name, owner, out route);
        if (!restored)
            restored = PelipperVillagerCompanionRuntimeBridge.Restore(owner.Name, owner, out route);

        if (restored)
            Monitor.Log($"Alpha 6.6.21 restored Pelipper villager companion source setting for {owner.Name} via {route}.", LogLevel.Debug);

        PelipperNpcNativeControlWarningsAlpha6618.Remove(owner.Name);
        PelipperNonConvergedRoutesAlpha6619.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));
    }

    private void OnPelipperProbeAlpha6621(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_pelipper_probe requires a loaded save.", LogLevel.Info);
            return;
        }

        string ownerName = string.Join(" ", args).Trim();
        if (string.IsNullOrWhiteSpace(ownerName))
        {
            Monitor.Log("Usage: teamup_pelipper_probe <NPC name>", LogLevel.Info);
            return;
        }

        ConfigurePelipperApiBridgeAlpha6619();
        NPC? owner = Game1.getCharacterFromName(ownerName);
        bool sourceLive = owner is not null && IsNpcPelipperSourceLiveAlpha6619(owner);

        Monitor.Log(
            $"Pelipper probe: owner={ownerName}, npcFound={owner is not null}, sourceLive={sourceLive}, runtimeRoot={PelipperVillagerLifecycleBridge.RootTypeName}.",
            LogLevel.Info);

        foreach (string line in PelipperVillagerLifecycleBridge.Probe(ownerName, 50))
            Monitor.Log($"Pelipper probe: {line}", LogLevel.Info);
    }
}
