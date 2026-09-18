using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.36 capture guard for Pelipper Mutation leaders only.
/// Followers keep Pelipper's native capture lifecycle.
/// </summary>
internal sealed class Alpha674436EliteCaptureGuardService
{
    private const int MaxTargetProbeDepth = 2;
    private static readonly HashSet<MethodBase> PatchedCaptureMethods = new();
    private static Alpha674436EliteCaptureGuardService? Active;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;

    private int _captureMethodsPatched;
    private long _captureBlocks;
    private long _targetMisses;
    private string _last = "reset";

    public Alpha674436EliteCaptureGuardService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony(uniqueId + ".Alpha674436EliteCaptureGuard");
        Active = this;

        PatchPelipperCaptureMethods(AppDomain.CurrentDomain.GetAssemblies());

        _monitor.Log(
            $"Team Up 6.7.44.36 elite capture guard enabled: Mutant leaders blocked, ordinary followers untouched; hooks={_captureMethodsPatched}.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutation capture guard: leader=blocked | followers=native | hooks={_captureMethodsPatched} | blocks={_captureBlocks} | targetMisses={_targetMisses} | last={_last}";

    public void ResetTelemetry()
    {
        _captureBlocks = 0;
        _targetMisses = 0;
        _last = "reset";
    }

    private void PatchPelipperCaptureMethods(IEnumerable<Assembly> assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            string assemblyName = assembly.GetName().Name ?? string.Empty;
            if (!assemblyName.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase)
                && !assemblyName.Contains("Griff.PelipperTown", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (Type type in SafeGetTypes(assembly))
            {
                string typeKey = Normalize(type.FullName ?? type.Name);
                foreach (MethodInfo method in type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.IsAbstract
                        || method.ContainsGenericParameters
                        || method.IsSpecialName
                        || !LooksLikeCaptureMethod(typeKey, method)
                        || !PatchedCaptureMethods.Add(method))
                    {
                        continue;
                    }

                    try
                    {
                        string prefix = method.IsStatic ? nameof(BeforeStaticCapture) : nameof(BeforeInstanceCapture);
                        _harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(typeof(Alpha674436EliteCaptureGuardService), prefix)
                            {
                                priority = Priority.First
                            });
                        _captureMethodsPatched++;
                    }
                    catch (Exception ex)
                    {
                        PatchedCaptureMethods.Remove(method);
                        _monitor.LogOnce(
                            $"6.7.44.36 capture guard skipped {type.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}",
                            LogLevel.Trace);
                    }
                }
            }
        }
    }

    private static bool BeforeInstanceCapture(object __instance, MethodBase __originalMethod, object[] __args)
        => !ShouldBlockCapture(__instance, __originalMethod, __args);

    private static bool BeforeStaticCapture(MethodBase __originalMethod, object[] __args)
        => !ShouldBlockCapture(null, __originalMethod, __args);

    private static bool ShouldBlockCapture(object? instance, MethodBase method, object[] args)
    {
        Alpha674436EliteCaptureGuardService? service = Active;
        if (service is null || !Context.IsWorldReady)
            return false;

        if (service.ContainsMutantLeaderTarget(instance) || args.Any(service.ContainsMutantLeaderTarget))
        {
            service._captureBlocks++;
            service._last = $"blocked {method.DeclaringType?.FullName}.{method.Name}";
            service._monitor.Log(
                $"[MutationCaptureGuard] Blocked capture path for Mutant leader via {method.DeclaringType?.FullName}.{method.Name}.",
                LogLevel.Debug);
            return true;
        }

        service._targetMisses++;
        return false;
    }

    private bool ContainsMutantLeaderTarget(object? candidate)
    {
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
        return ContainsMutantLeaderTarget(candidate, 0, visited);
    }

    private bool ContainsMutantLeaderTarget(object? candidate, int depth, HashSet<object> visited)
    {
        if (candidate is null || depth > MaxTargetProbeDepth)
            return false;

        if (candidate is Monster monster)
            return MonsterMutationService.IsMutant(monster)
                || HasTrueMarker(monster, Alpha67448PelipperSourceMutationService.NoCaptureMarker);

        if (candidate is NPC npc)
            return HasTrueMarker(npc, Alpha67448PelipperSourceMutationService.NoCaptureMarker);

        Type type = candidate.GetType();
        if (IsSimple(type) || !visited.Add(candidate))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!LooksLikeTargetMember(field.Name))
                continue;

            object? nested = TryGetValue(() => field.GetValue(candidate));
            if (ContainsMutantLeaderTarget(nested, depth + 1, visited))
                return true;
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !LooksLikeTargetMember(property.Name))
                continue;

            object? nested = TryGetValue(() => property.GetValue(candidate));
            if (ContainsMutantLeaderTarget(nested, depth + 1, visited))
                return true;
        }

        return false;
    }

    private static bool HasTrueMarker(NPC npc, string key)
        => npc.modData.TryGetValue(key, out string? raw)
            && raw.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeCaptureMethod(string typeKey, MethodInfo method)
    {
        string combined = typeKey + Normalize(method.Name);
        return combined.Contains("capture")
            || combined.Contains("catch")
            || combined.Contains("pokeball");
    }

    private static bool LooksLikeTargetMember(string name)
    {
        string key = Normalize(name);
        return key.Contains("target")
            || key.Contains("pokemon")
            || key.Contains("wild")
            || key.Contains("encounter")
            || key.Contains("monster")
            || key.Contains("npc");
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static bool IsSimple(Type type)
        => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid);

    private static object? TryGetValue(Func<object?> getter)
    {
        try
        {
            return getter();
        }
        catch
        {
            return null;
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
