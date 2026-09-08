using Ronvotri.TeamUp.Core;
using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha6710Registered;

    private void EnsureAlpha6710Registered()
    {
        if (!Alpha6710Registered)
        {
            Alpha6710Registered = true;
            NpcRouteStateSafetyPatch.Apply(Monitor, ModManifest.UniqueID);
            Helper.ConsoleCommands.Add(
                "teamup_route_guard",
                "Show Alpha 6.7.10 NPC end-of-route crash guard status.",
                OnAlpha6710RouteGuardStatus);
        }

        EnsureAlpha6713Registered();
    }

    private void OnAlpha6710RouteGuardStatus(string command, string[] args)
        => Monitor.Log($"Alpha 6.7.10 routeGuard={NpcRouteStateSafetyPatch.IsApplied}.", LogLevel.Info);
}
