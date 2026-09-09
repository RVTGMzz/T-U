using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 6.7.20 keeps a mutant's combat/collision footprint aligned with the visual scale that
/// actually succeeded in Alpha 6.7.19. The patch is applied to Character.GetBoundingBox plus
/// Monster overrides discovered at runtime. A thread-local nesting guard prevents an override
/// which calls a patched base implementation from multiplying the rectangle twice.
/// </summary>
internal static class MonsterMutationFootprintPatch
{
    private const int MaximumFootprintPixels = 768;

    [ThreadStatic]
    private static int _boundingBoxDepth;

    private static readonly HashSet<MethodBase> PatchedMethods = new();

    public static int PatchedMethodCount => PatchedMethods.Count;

    public static void Apply(IMonitor monitor, string uniqueId)
    {
        var harmony = new Harmony($"{uniqueId}.Alpha6720MutantFootprint");

        PatchMethod(typeof(Character).GetMethod(
            "GetBoundingBox",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null), harmony, monitor);

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (!typeof(Monster).IsAssignableFrom(type))
                    continue;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    if (!method.Name.Equals("GetBoundingBox", StringComparison.Ordinal)
                        || method.IsAbstract
                        || method.IsStatic
                        || method.ContainsGenericParameters
                        || method.ReturnType != typeof(Rectangle)
                        || method.GetParameters().Length != 0)
                    {
                        continue;
                    }

                    PatchMethod(method, harmony, monitor);
                }
            }
        }

        monitor.Log($"Alpha 6.7.20 mutant footprint patched {PatchedMethodCount} GetBoundingBox implementation(s).", LogLevel.Info);
    }

    private static void PatchMethod(MethodInfo? method, Harmony harmony, IMonitor monitor)
    {
        if (method is null || !PatchedMethods.Add(method))
            return;

        try
        {
            harmony.Patch(
                method,
                prefix: new HarmonyMethod(typeof(MonsterMutationFootprintPatch), nameof(Prefix)),
                postfix: new HarmonyMethod(typeof(MonsterMutationFootprintPatch), nameof(Postfix)));
        }
        catch (Exception ex)
        {
            PatchedMethods.Remove(method);
            monitor.Log($"Mutant footprint hook skipped {method.DeclaringType?.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
        }
    }

    private static void Prefix(ref bool __state)
    {
        __state = _boundingBoxDepth == 0;
        _boundingBoxDepth++;
    }

    private static void Postfix(object __instance, ref Rectangle __result, bool __state)
    {
        try
        {
            if (!__state || __instance is not Monster monster || !MonsterMutationService.IsMutant(monster))
                return;

            if (!monster.modData.TryGetValue(MonsterMutationService.MutationScaleMarker, out string? raw)
                || !float.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale))
            {
                return;
            }

            scale = Math.Clamp(scale, 1f, 5f);
            if (scale <= 1.001f || __result.Width <= 0 || __result.Height <= 0)
                return;

            int centerX = __result.X + __result.Width / 2;
            int centerY = __result.Y + __result.Height / 2;
            int width = Math.Clamp((int)Math.Round(__result.Width * scale), __result.Width, MaximumFootprintPixels);
            int height = Math.Clamp((int)Math.Round(__result.Height * scale), __result.Height, MaximumFootprintPixels);
            __result = new Rectangle(centerX - width / 2, centerY - height / 2, width, height);
        }
        finally
        {
            _boundingBoxDepth = Math.Max(0, _boundingBoxDepth - 1);
        }
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
