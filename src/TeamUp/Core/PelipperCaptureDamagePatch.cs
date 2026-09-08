using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Global Pelipper capture-floor safety. Team Up never owns Pelipper combat actors here; it only
/// prevents friendly damage from crossing the source mod's mercy/capture threshold.
///
/// Alpha 6.7.12 closes two gaps seen in live party play:
/// - a concrete takeDamage implementation inherited from an abstract custom Monster base was not
///   patched by the old concrete-type-only scan;
/// - party/companion area damage can enter through GameLocation.damageMonster before reaching a
///   custom proxy implementation.
/// </summary>
internal static class PelipperCaptureDamagePatch
{
    private static readonly HashSet<MethodBase> PatchedTakeDamageMethods = new();
    private static readonly HashSet<MethodBase> PatchedAreaDamageMethods = new();

    public static int TakeDamagePatchCount => PatchedTakeDamageMethods.Count;
    public static int AreaDamagePatchCount => PatchedAreaDamageMethods.Count;

    public static void Apply(IMonitor monitor, string uniqueId)
    {
        var harmony = new Harmony(uniqueId + ".PelipperCaptureFloor");
        HarmonyMethod takeDamagePrefix = new(typeof(PelipperCaptureDamagePatch), nameof(BeforeTakeDamage));
        HarmonyMethod areaDamagePrefix = new(typeof(PelipperCaptureDamagePatch), nameof(BeforeAreaDamage));
        int addedTakeDamage = 0;
        int addedAreaDamage = 0;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (typeof(Monster).IsAssignableFrom(type))
                {
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (!method.Name.Equals("takeDamage", StringComparison.Ordinal)
                            || method.IsAbstract
                            || method.GetParameters().Length == 0
                            || method.GetParameters()[0].ParameterType != typeof(int)
                            || !PatchedTakeDamageMethods.Add(method))
                        {
                            continue;
                        }

                        try
                        {
                            harmony.Patch(method, prefix: takeDamagePrefix);
                            addedTakeDamage++;
                        }
                        catch (Exception ex)
                        {
                            PatchedTakeDamageMethods.Remove(method);
                            monitor.LogOnce($"Capture floor could not patch {type.FullName}.{method.Name}: {ex.Message}", LogLevel.Trace);
                        }
                    }
                }

                if (!typeof(GameLocation).IsAssignableFrom(type))
                    continue;

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!method.Name.Equals("damageMonster", StringComparison.Ordinal)
                        || method.IsAbstract
                        || !HasAreaDamageShape(method)
                        || !PatchedAreaDamageMethods.Add(method))
                    {
                        continue;
                    }

                    try
                    {
                        harmony.Patch(method, prefix: areaDamagePrefix);
                        addedAreaDamage++;
                    }
                    catch (Exception ex)
                    {
                        PatchedAreaDamageMethods.Remove(method);
                        monitor.LogOnce($"Capture floor could not patch {type.FullName}.{method.Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }

        if (addedTakeDamage > 0 || addedAreaDamage > 0)
        {
            monitor.Log(
                $"Alpha 6.7.12 capture floor active: takeDamage={TakeDamagePatchCount}, areaDamage={AreaDamagePatchCount}.",
                LogLevel.Debug);
        }
    }

    // Harmony's __0 binds by argument position, so this remains compatible with different
    // takeDamage parameter names used by custom Monster subclasses.
    private static void BeforeTakeDamage(Monster __instance, ref int __0)
    {
        if (__0 <= 0)
            return;

        __0 = PelipperCaptureSafetyService.ClampDamage(__instance, __0);
    }

    // object[] lets this guard cover vanilla and compatible custom GameLocation.damageMonster
    // overloads without hard-binding one Stardew signature. We only touch Rectangle + named
    // damage integer arguments, never trajectory/precision/knockback/etc.
    private static void BeforeAreaDamage(GameLocation __instance, MethodBase __originalMethod, object[] __args)
    {
        if (__instance is null || __args.Length == 0)
            return;

        ParameterInfo[] parameters = __originalMethod.GetParameters();
        int areaIndex = -1;
        for (int i = 0; i < parameters.Length && i < __args.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(Rectangle) && __args[i] is Rectangle)
            {
                areaIndex = i;
                break;
            }
        }

        if (areaIndex < 0 || __args[areaIndex] is not Rectangle area)
            return;

        int sharedBudget = int.MaxValue;
        foreach (Monster monster in __instance.characters.OfType<Monster>())
        {
            if (monster.Health <= 0 || !area.Intersects(monster.GetBoundingBox()))
                continue;
            if (!PelipperCaptureSafetyService.TryGetDamageBudget(monster, out int budget))
                continue;
            sharedBudget = Math.Min(sharedBudget, budget);
        }

        if (sharedBudget == int.MaxValue)
            return;

        for (int i = 0; i < parameters.Length && i < __args.Length; i++)
        {
            if (!IsDamageParameter(parameters[i]) || __args[i] is not int damage)
                continue;
            __args[i] = Math.Min(Math.Max(0, damage), sharedBudget);
        }
    }

    private static bool HasAreaDamageShape(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Any(parameter => parameter.ParameterType == typeof(Rectangle))
            && parameters.Any(IsDamageParameter);
    }

    private static bool IsDamageParameter(ParameterInfo parameter)
    {
        if (parameter.ParameterType != typeof(int))
            return false;

        string name = Normalize(parameter.Name ?? string.Empty);
        return name is "damage" or "mindamage" or "maxdamage";
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

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
