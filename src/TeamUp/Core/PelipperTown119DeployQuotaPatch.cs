using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.6.25 hard pre-spawn quota gate for Pelipper Town 1.1.9.
///
/// Verified from PelipperTown.Mod.dll: every normal player companion deployment reaches
/// PelipperTown.CompanionRuntime.DeployBeside(Farmer owner, bool celebrate). Patching this final
/// native boundary prevents a third companion from ever spawning when Team Up's shared 2/2 pool
/// is full. Pelipper remains responsible for all successful deployment, movement, render, and AI.
/// </summary>
internal static class PelipperTown119DeployQuotaPatch
{
    private static bool Applied;
    private static Func<long, bool>? CanDeploy;
    private static Action<long>? OnBlocked;

    public static bool IsApplied => Applied;

    public static void Configure(Func<long, bool> canDeploy, Action<long> onBlocked)
    {
        CanDeploy = canDeploy;
        OnBlocked = onBlocked;
    }

    public static bool Apply(IMonitor monitor, string uniqueId)
    {
        if (Applied)
            return true;

        Type? runtimeType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("PelipperTown.CompanionRuntime", throwOnError: false, ignoreCase: false))
            .FirstOrDefault(type => type is not null);
        if (runtimeType is null)
            return false;

        MethodInfo? deploy = runtimeType.GetMethod(
            "DeployBeside",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(Farmer), typeof(bool) },
            modifiers: null);
        if (deploy is null || deploy.ReturnType != typeof(bool))
        {
            monitor.Log("Alpha 6.6.25 could not find PelipperTown.CompanionRuntime.DeployBeside(Farmer,bool); native 2/2 pre-spawn gate is unavailable.", LogLevel.Warn);
            return false;
        }

        try
        {
            var harmony = new Harmony(uniqueId + ".Pelipper119DeployQuota");
            harmony.Patch(
                deploy,
                prefix: new HarmonyMethod(typeof(PelipperTown119DeployQuotaPatch), nameof(BeforeDeployBeside)));
            Applied = true;
            monitor.Log("Alpha 6.6.25 patched PelipperTown.CompanionRuntime.DeployBeside(Farmer,bool): shared 2/2 quota now blocks before native spawn.", LogLevel.Debug);
            return true;
        }
        catch (Exception ex)
        {
            monitor.Log($"Alpha 6.6.25 failed to patch Pelipper native deploy boundary: {ex.Message}", LogLevel.Warn);
            return false;
        }
    }

    // __0 binds by argument position to Farmer owner. Returning false skips Pelipper's method.
    private static bool BeforeDeployBeside(Farmer __0, ref bool __result)
    {
        Func<long, bool>? gate = CanDeploy;
        if (gate is null || gate(__0.UniqueMultiplayerID))
            return true;

        __result = false;
        OnBlocked?.Invoke(__0.UniqueMultiplayerID);
        return false;
    }
}
