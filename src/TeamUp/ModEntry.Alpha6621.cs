using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha6621Registered;
    private readonly HashSet<string> PelipperNonConvergedSourceRequestsAlpha6623 = new(StringComparer.OrdinalIgnoreCase);

    private void EnsureAlpha6621Registered()
    {
        if (Alpha6621Registered)
            return;

        Alpha6621Registered = true;
        Helper.Events.GameLoop.DayEnding += (_, _) => PelipperNonConvergedSourceRequestsAlpha6623.Clear();
        Helper.Events.GameLoop.ReturnedToTitle += (_, _) => PelipperNonConvergedSourceRequestsAlpha6623.Clear();
        Helper.ConsoleCommands.Add(
            "teamup_pelipper_probe",
            "Inspect Pelipper Town villager companion lifecycle candidates. Usage: teamup_pelipper_probe <NPC name>.",
            OnPelipperProbeAlpha6621);
    }

    private bool TrySetPelipperNpcSourceEnabledAlpha6621(NPC owner, bool enabled, string reason)
    {
        ConfigurePelipperApiBridgeAlpha6619();

        string requestKey = $"{owner.Name}|{enabled}";
        bool sourceLiveBefore = IsNpcPelipperSourceLiveAlpha6619(owner);
        bool alreadyConverged = enabled ? sourceLiveBefore : !sourceLiveBefore;
        if (alreadyConverged)
        {
            PelipperNonConvergedSourceRequestsAlpha6623.Remove(requestKey);
            return true;
        }

        if (PelipperNonConvergedSourceRequestsAlpha6623.Contains(requestKey))
            return false;

        // Alpha 6.6.25: exact Pelipper Town 1.1.9 route verified from the user's DLL.
        // This path calls VillagerCompanionManager.ApplyConfiguredAssignments(), whose IL directly
        // invokes VillagerCompanionRuntime.Despawn() for disabled NPC partners.
        bool routed = PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled(owner.Name, enabled, out string route);
        if (!routed)
            routed = PelipperVillagerLifecycleBridge.TrySetEnabled(owner.Name, owner, enabled, out route);
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
                PelipperNonConvergedSourceRequestsAlpha6623.Remove(requestKey);
                Monitor.Log(
                    $"Alpha 6.6.25 verified Pelipper villager source {(enabled ? "deploy" : "recall")} {owner.Name} via {route} ({reason}).",
                    LogLevel.Debug);
                return true;
            }

            string convergenceKey = $"{owner.Name}|{enabled}|{route}";
            PelipperNonConvergedSourceRequestsAlpha6623.Add(requestKey);
            if (PelipperNonConvergedRoutesAlpha6619.Add(convergenceKey))
            {
                Monitor.Log(
                    $"Alpha 6.6.25 routed {(enabled ? "deploy" : "recall")} for {owner.Name} via {route}, but Pelipper source is still live={sourceLive}. The real slot remains occupied and retries stay latched to avoid flicker.",
                    LogLevel.Warn);
            }
            return false;
        }

        PelipperNonConvergedSourceRequestsAlpha6623.Add(requestKey);
        if (PelipperNpcNativeControlWarningsAlpha6618.Add(owner.Name))
        {
            Monitor.Log(
                $"Alpha 6.6.25 found no Pelipper villager lifecycle route for {owner.Name}. native119={PelipperTown119NativeBridge.Status}. The source-live Pokemon stays counted.",
                LogLevel.Warn);
        }
        return false;
    }

    private void RestorePelipperNpcSourceAlpha6621(NPC owner)
    {
        ConfigurePelipperApiBridgeAlpha6619();
        PelipperNonConvergedSourceRequestsAlpha6623.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));

        bool restored = PelipperTown119NativeBridge.RestoreVillager(owner.Name, out string route);
        if (!restored)
            restored = PelipperVillagerLifecycleBridge.Restore(owner.Name, owner, out route);
        if (!restored)
            restored = PelipperApiRuntimeRootBridge.Restore(owner.Name, owner, out route);
        if (!restored)
            restored = PelipperVillagerCompanionRuntimeBridge.Restore(owner.Name, owner, out route);

        if (restored)
            Monitor.Log($"Alpha 6.6.25 restored Pelipper villager companion source setting for {owner.Name} via {route}.", LogLevel.Debug);

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
            $"Pelipper probe: owner={ownerName}, npcFound={owner is not null}, sourceLive={sourceLive}, runtimeRoot={PelipperVillagerLifecycleBridge.RootTypeName}, native119={PelipperTown119NativeBridge.Status}.",
            LogLevel.Info);

        foreach (string line in PelipperVillagerLifecycleBridge.Probe(ownerName, 50))
            Monitor.Log($"Pelipper probe: {line}", LogLevel.Info);
    }
}
