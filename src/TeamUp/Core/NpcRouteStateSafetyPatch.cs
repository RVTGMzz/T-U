using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

internal static class NpcRouteStateSafetyPatch
{
    public static bool IsApplied { get; private set; }

    public static void Apply(IMonitor monitor, string uniqueId)
    {
        if (IsApplied)
            return;

        MethodInfo? original = AccessTools.Method(typeof(NPC), "loadEndOfRouteBehavior", new[] { typeof(string) });
        if (original is null)
        {
            monitor.Log("Alpha 6.7.10 could not locate NPC.loadEndOfRouteBehavior(String). Route-state crash guard is unavailable.", LogLevel.Error);
            return;
        }

        var harmony = new Harmony($"{uniqueId}.Alpha6710RouteStateSafety");
        harmony.Patch(
            original,
            prefix: new HarmonyMethod(typeof(NpcRouteStateSafetyPatch), nameof(Prefix)));
        IsApplied = true;
        monitor.Log("Alpha 6.7.10 NPC route-state safety patch applied.", LogLevel.Info);
    }

    private static bool Prefix(NPC __instance, string? __0)
    {
        bool teamUpControlled = __instance.modData.ContainsKey(FollowService.PartyControlledModDataKey);
        if (teamUpControlled)
            return false;

        // Defensive recovery for stale state produced by an older Team Up build or another mod.
        // Vanilla cannot do useful work with an empty behavior name, so skipping is safer than
        // letting NPC.update repeatedly crash the entire base update loop.
        return !string.IsNullOrWhiteSpace(__0);
    }
}
