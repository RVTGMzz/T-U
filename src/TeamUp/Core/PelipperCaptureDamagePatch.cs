using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.6.15 hard capture floor. Alpha 6.6.14 only clamped damage issued by Team Up's own
/// CombatService, so source-owned Pelipper companions could still finish the same wild target.
/// This patch clamps the first damage argument of Monster.takeDamage for Pelipper capture targets,
/// regardless of which friendly source issued the hit. It doesn't change movement/visibility or
/// Pelipper save data.
/// </summary>
internal static class PelipperCaptureDamagePatch
{
    private static readonly HashSet<MethodBase> PatchedMethods = new();

    public static void Apply(IMonitor monitor, string uniqueId)
    {
        var harmony = new Harmony(uniqueId + ".PelipperCaptureFloor");
        HarmonyMethod prefix = new(typeof(PelipperCaptureDamagePatch), nameof(BeforeTakeDamage));
        int added = 0;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (!typeof(Monster).IsAssignableFrom(type) || type.IsAbstract)
                    continue;

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!method.Name.Equals("takeDamage", StringComparison.Ordinal)
                        || method.GetParameters().Length == 0
                        || method.GetParameters()[0].ParameterType != typeof(int)
                        || !PatchedMethods.Add(method))
                    {
                        continue;
                    }

                    try
                    {
                        harmony.Patch(method, prefix: prefix);
                        added++;
                    }
                    catch (Exception ex)
                    {
                        PatchedMethods.Remove(method);
                        monitor.LogOnce($"Capture floor could not patch {type.FullName}.{method.Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }

        if (added > 0)
            monitor.Log($"Alpha 6.6.15 capture floor patched {added} Monster.takeDamage implementation(s).", LogLevel.Debug);
    }

    // Harmony's __0 binds by argument position, so this remains compatible with different
    // takeDamage overload parameter names used by custom Monster subclasses.
    private static void BeforeTakeDamage(Monster __instance, ref int __0)
    {
        if (__0 <= 0)
            return;

        __0 = PelipperCaptureSafetyService.ClampDamage(__instance, __0);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }
}
