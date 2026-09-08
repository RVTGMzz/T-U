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
        string nativeOwnerKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
        bool sourceLiveBefore = IsNpcPelipperSourceLiveAlpha6619(owner);
        bool hasConfiguredEnabled = PelipperTown119NativeBridge.TryIsVillagerCompanionConfiguredEnabled(
            nativeOwnerKey,
            out bool configuredEnabled);
        bool alreadyConverged = enabled
            ? sourceLiveBefore && (!hasConfiguredEnabled || configuredEnabled)
            : !sourceLiveBefore && (!hasConfiguredEnabled || !configuredEnabled);
        if (alreadyConverged)
        {
            PelipperNonConvergedSourceRequestsAlpha6623.Remove(requestKey);
            return true;
        }

        // Alpha 6.6.26: the native villager Call path gets the same hard shared-pool preflight as
        // player DeployBeside. Some legacy UI paths set the target Team Up record Active before
        // asking Pelipper to deploy it, so subtract that target reservation and judge only slots
        // occupied by everyone else. This closes the NPC route that could produce physical 3/2.
        if (enabled && !sourceLiveBefore)
        {
            int max = GetCompanionCapAlpha6618();
            List<CompanionUnitData> effectiveUnits = GetEffectiveCombatCompanionsAlpha6618();
            CompanionUnitData? target = Party.Members
                .Where(member => member.CharacterName.Equals(owner.Name, StringComparison.OrdinalIgnoreCase))
                .Select(member => Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId))
                .FirstOrDefault(unit => unit is not null && PelipperTownCompatibilityService.IsSourceControlled(unit));

            bool targetAlreadyReserved = target is not null
                && effectiveUnits.Any(unit =>
                    unit.RecruiterId == target.RecruiterId
                    && unit.UnitId.Equals(target.UnitId, StringComparison.OrdinalIgnoreCase));
            int occupiedByOthers = Math.Max(0, effectiveUnits.Count - (targetAlreadyReserved ? 1 : 0));

            if (max <= 0 || occupiedByOthers >= max)
            {
                Monitor.Log(
                    $"Alpha 6.6.26 blocked Pelipper NPC deploy for {owner.Name}: occupiedByOthers={occupiedByOthers}, effective={effectiveUnits.Count}/{max} ({reason}).",
                    LogLevel.Debug);
                return false;
            }
        }

        if (PelipperNonConvergedSourceRequestsAlpha6623.Contains(requestKey))
            return false;

        // Exact Pelipper Town 1.1.9 route verified from the user's DLL. This calls
        // VillagerCompanionManager.ApplyConfiguredAssignments(), whose IL directly invokes
        // VillagerCompanionRuntime.Despawn() for disabled NPC partners.
        bool nativeAvailable = PelipperTown119NativeBridge.HasVillagerLifecycle;
        bool routed = PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled(nativeOwnerKey, enabled, out string route);

        // Only unsupported Pelipper builds may fall back to older discovery bridges. If the exact
        // 1.1.9 surface is bound, its false result is authoritative and must never be bypassed.
        if (!routed && !nativeAvailable)
            routed = PelipperVillagerLifecycleBridge.TrySetEnabled(owner.Name, owner, enabled, out route);
        if (!routed && !nativeAvailable)
            routed = PelipperApiRuntimeRootBridge.TrySetEnabled(owner.Name, owner, enabled, out route);
        if (!routed && !nativeAvailable)
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
                    $"Alpha 6.6.26 verified Pelipper villager source {(enabled ? "deploy" : "recall")} {owner.Name} via {route} ({reason}).",
                    LogLevel.Debug);
                return true;
            }

            string convergenceKey = $"{owner.Name}|{enabled}|{route}";
            PelipperNonConvergedSourceRequestsAlpha6623.Add(requestKey);
            if (PelipperNonConvergedRoutesAlpha6619.Add(convergenceKey))
            {
                Monitor.Log(
                    $"Alpha 6.6.26 routed {(enabled ? "deploy" : "recall")} for {owner.Name} via {route}, but Pelipper source is still live={sourceLive}. The real slot remains occupied and retries stay latched to avoid flicker.",
                    LogLevel.Warn);
            }
            return false;
        }

        PelipperNonConvergedSourceRequestsAlpha6623.Add(requestKey);
        if (PelipperNpcNativeControlWarningsAlpha6618.Add(owner.Name))
        {
            string detail = nativeAvailable
                ? "the exact 1.1.9 native lifecycle returned false; compatibility fallback was intentionally suppressed"
                : "no Pelipper villager lifecycle route was available";
            Monitor.Log(
                $"Alpha 6.6.26 {detail} for {owner.Name}. native119={PelipperTown119NativeBridge.Status}. The source-live Pokemon stays counted.",
                LogLevel.Warn);
        }
        return false;
    }

    private void RestorePelipperNpcSourceAlpha6621(NPC owner)
    {
        ConfigurePelipperApiBridgeAlpha6619();
        PelipperNonConvergedSourceRequestsAlpha6623.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));

        bool nativeAvailable = PelipperTown119NativeBridge.HasVillagerLifecycle;
        string nativeOwnerKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
        bool restored = PelipperTown119NativeBridge.RestoreVillager(nativeOwnerKey, out string route);

        if (!restored && !nativeAvailable)
            restored = PelipperVillagerLifecycleBridge.Restore(owner.Name, owner, out route);
        if (!restored && !nativeAvailable)
            restored = PelipperApiRuntimeRootBridge.Restore(owner.Name, owner, out route);
        if (!restored && !nativeAvailable)
            restored = PelipperVillagerCompanionRuntimeBridge.Restore(owner.Name, owner, out route);

        if (restored)
            Monitor.Log($"Alpha 6.6.26 restored Pelipper villager companion source setting for {owner.Name} via {route}.", LogLevel.Debug);

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
