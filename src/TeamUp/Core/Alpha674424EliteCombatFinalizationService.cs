using System.Globalization;
using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.24 closes the remaining Pelipper Mutant-leader combat gaps proven by live testing:
/// preserve the Mutation x2 contact damage even if Pelipper rewrites its hidden proxy later, report
/// requested versus actual Farmer HP loss, and make the Mutant leader itself uncapturable while
/// leaving its ordinary native followers catchable.
/// </summary>
internal sealed class Alpha674424EliteCombatFinalizationService
{
    public const string IntendedDamageMarker = "Ronvotri.TeamUp/MutantIntendedDamage";
    public const string NoCaptureMarker = "Ronvotri.TeamUp/MutantLeaderNoCapture";

    private const int MaxTargetProbeDepth = 2;

    private static readonly HashSet<MethodBase> PatchedCaptureMethods = new();
    private static Alpha674424EliteCombatFinalizationService? ActiveInstance;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;

    private long _damageStamps;
    private long _damageRestores;
    private long _damageObservedHits;
    private long _requestedDamageTotal;
    private long _actualDamageTotal;
    private long _captureBlocks;
    private long _captureTargetMisses;
    private long _sourceNoCaptureMarks;
    private int _captureMethodsPatched;
    private string _last = "reset";

    private sealed class DamageProbeState
    {
        public Farmer Farmer { get; init; } = null!;
        public int HealthBefore { get; init; }
        public int RequestedDamage { get; init; }
        public bool Leader { get; init; }
        public string Species { get; init; } = "unknown";
    }

    public Alpha674424EliteCombatFinalizationService(IMonitor monitor, IModHelper helper, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony(uniqueId + ".Alpha674424EliteCombatFinalization");
        ActiveInstance = this;

        PatchMutationStamp();
        PatchLeaderDamageProbe();
        PatchPelipperCaptureMethods(AppDomain.CurrentDomain.GetAssemblies());
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        _monitor.Log(
            $"Team Up 6.7.44.24 Elite combat finalization enabled: persistent Mutation damage, requested-vs-actual damage telemetry, Mutant-leader capture guard; captureHooks={_captureMethodsPatched}.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutation elite finalization: reach=160px | hold=128px | damage=preserve-mutant-stat | leaderCapture=blocked | "
            + $"damageStamps={_damageStamps} | damageRestores={_damageRestores} | damageObservedHits={_damageObservedHits} | "
            + $"requestedDamageTotal={_requestedDamageTotal} | actualDamageTotal={_actualDamageTotal} | "
            + $"captureMethodsPatched={_captureMethodsPatched} | captureBlocks={_captureBlocks} | captureTargetMisses={_captureTargetMisses} | "
            + $"sourceNoCaptureMarks={_sourceNoCaptureMarks} | last={_last}";

    public void ResetTelemetry()
    {
        _damageStamps = 0;
        _damageRestores = 0;
        _damageObservedHits = 0;
        _requestedDamageTotal = 0;
        _actualDamageTotal = 0;
        _captureBlocks = 0;
        _captureTargetMisses = 0;
        _sourceNoCaptureMarks = 0;
        _last = "reset";
    }

    private void PatchMutationStamp()
    {
        MethodInfo? method = AccessTools.Method(typeof(MonsterMutationService), "TryMutate");
        if (method is null)
        {
            _last = "TryMutate hook unavailable";
            return;
        }

        _harmony.Patch(
            method,
            postfix: new HarmonyMethod(typeof(Alpha674424EliteCombatFinalizationService), nameof(AfterTryMutate)));
    }

    private void PatchLeaderDamageProbe()
    {
        MethodInfo? method = AccessTools.Method(typeof(Alpha674423PelipperMutantLeaderSmoothingService), "TryDamage");
        if (method is null)
        {
            _last = "TryDamage hook unavailable";
            return;
        }

        _harmony.Patch(
            method,
            prefix: new HarmonyMethod(typeof(Alpha674424EliteCombatFinalizationService), nameof(BeforeLeaderDamage)),
            postfix: new HarmonyMethod(typeof(Alpha674424EliteCombatFinalizationService), nameof(AfterLeaderDamage)));
    }

    private static void AfterTryMutate(Monster __0, bool __result)
    {
        Alpha674424EliteCombatFinalizationService? service = ActiveInstance;
        if (service is null || !__result || !MonsterMutationService.IsMutant(__0))
            return;

        int intended = Math.Max(1, __0.DamageToFarmer);
        __0.modData[IntendedDamageMarker] = intended.ToString(CultureInfo.InvariantCulture);
        service._damageStamps++;
        service.MarkLeaderSourceNoCapture(__0);
        service._last = $"damage-stamped leader={__0.Name} intended={intended}";
    }

    private static void BeforeLeaderDamage(object[] __args, out DamageProbeState? __state)
    {
        __state = null;
        Alpha674424EliteCombatFinalizationService? service = ActiveInstance;
        if (service is null || __args.Length < 4 || __args[3] is not Farmer farmer)
            return;

        object? pair = __args[1];
        if (!TryReadPair(pair, out Monster? proxy, out bool leader, out string species) || proxy is null)
            return;

        int requested = Math.Max(1, proxy.DamageToFarmer);
        if (leader && TryReadIntendedDamage(proxy, out int intended))
        {
            requested = intended;
            if (proxy.DamageToFarmer != intended)
            {
                proxy.DamageToFarmer = intended;
                service._damageRestores++;
                service._last = $"damage-restored species={species} proxy={proxy.Name} intended={intended}";
            }
        }

        __state = new DamageProbeState
        {
            Farmer = farmer,
            HealthBefore = farmer.health,
            RequestedDamage = requested,
            Leader = leader,
            Species = species
        };
    }

    private static void AfterLeaderDamage(DamageProbeState? __state)
    {
        Alpha674424EliteCombatFinalizationService? service = ActiveInstance;
        if (service is null || __state is null)
            return;

        int actual = Math.Max(0, __state.HealthBefore - __state.Farmer.health);
        if (actual <= 0)
            return;

        service._damageObservedHits++;
        service._requestedDamageTotal += __state.RequestedDamage;
        service._actualDamageTotal += actual;
        service._last = $"damage-observed species={__state.Species} leader={__state.Leader} requested={__state.RequestedDamage} actual={actual}";
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
            return;

        foreach (Monster proxy in Game1.currentLocation.characters.OfType<Monster>())
        {
            if (proxy.Health <= 0 || !MonsterMutationService.IsMutant(proxy))
                continue;

            if (TryReadIntendedDamage(proxy, out int intended) && proxy.DamageToFarmer != intended)
            {
                proxy.DamageToFarmer = intended;
                _damageRestores++;
                _last = $"tick-damage-restored leader={proxy.Name} intended={intended}";
            }

            MarkLeaderSourceNoCapture(proxy);
        }
    }

    private void MarkLeaderSourceNoCapture(Monster proxy)
    {
        if (!MonsterMutationService.IsMutant(proxy)
            || !PelipperTownCompatibilityService.IsWildCombatActor(proxy)
            || !PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity))
        {
            return;
        }

        NPC source = identity.SourceActor;
        proxy.modData[NoCaptureMarker] = "true";
        if (!source.modData.TryGetValue(NoCaptureMarker, out string? raw)
            || !raw.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            source.modData[NoCaptureMarker] = "true";
            _sourceNoCaptureMarks++;
        }
    }

    private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs e)
    {
        PatchPelipperCaptureMethods(new[] { e.LoadedAssembly });
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
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.IsAbstract || method.ContainsGenericParameters || method.IsSpecialName || !LooksLikeCaptureMethod(typeKey, method))
                        continue;
                    if (!PatchedCaptureMethods.Add(method))
                        continue;

                    try
                    {
                        string prefix = method.IsStatic ? nameof(BeforeStaticCapture) : nameof(BeforeInstanceCapture);
                        _harmony.Patch(method, prefix: new HarmonyMethod(typeof(Alpha674424EliteCombatFinalizationService), prefix));
                        _captureMethodsPatched++;
                    }
                    catch (Exception ex)
                    {
                        PatchedCaptureMethods.Remove(method);
                        _monitor.LogOnce($"Mutation capture guard skipped {type.FullName}.{method.Name}: {ex.Message}", LogLevel.Trace);
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
        Alpha674424EliteCombatFinalizationService? service = ActiveInstance;
        if (service is null || !Context.IsWorldReady)
            return false;

        if (service.ContainsMutantLeaderTarget(instance) || args.Any(service.ContainsMutantLeaderTarget))
        {
            service._captureBlocks++;
            service._last = $"capture-blocked method={method.DeclaringType?.FullName}.{method.Name}";
            service._monitor.Log(
                $"[MutationCaptureGuard] Blocked Poké Ball/capture path for Mutant leader via {method.DeclaringType?.FullName}.{method.Name}.",
                LogLevel.Debug);
            return true;
        }

        service._captureTargetMisses++;
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
            return MonsterMutationService.IsMutant(monster);
        if (candidate is NPC npc)
        {
            if (HasTrueMarker(npc, NoCaptureMarker))
                return true;
            if (IsSourceOfMutantLeader(npc))
                return true;
        }

        Type type = candidate.GetType();
        if (IsSimple(type) || !visited.Add(candidate))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!LooksLikeTargetMember(field.Name))
                continue;
            object? value = TryGetValue(() => field.GetValue(candidate));
            if (ContainsMutantLeaderTarget(value, depth + 1, visited))
                return true;
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !LooksLikeTargetMember(property.Name))
                continue;
            object? value = TryGetValue(() => property.GetValue(candidate));
            if (ContainsMutantLeaderTarget(value, depth + 1, visited))
                return true;
        }

        return false;
    }

    private static bool IsSourceOfMutantLeader(NPC source)
    {
        GameLocation? location = source.currentLocation ?? Game1.currentLocation;
        if (location is null)
            return false;

        foreach (Monster proxy in location.characters.OfType<Monster>())
        {
            if (proxy.Health <= 0 || !MonsterMutationService.IsMutant(proxy))
                continue;
            if (!PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity))
                continue;
            if (ReferenceEquals(identity.SourceActor, source))
                return true;
        }
        return false;
    }

    private static bool TryReadPair(object? pair, out Monster? proxy, out bool leader, out string species)
    {
        proxy = null;
        leader = false;
        species = "unknown";
        if (pair is null)
            return false;

        Type type = pair.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        try
        {
            proxy = type.GetProperty("Proxy", flags)?.GetValue(pair) as Monster;
            leader = type.GetProperty("Leader", flags)?.GetValue(pair) is bool rawLeader && rawLeader;
            object? identity = type.GetProperty("Identity", flags)?.GetValue(pair);
            species = identity?.GetType().GetProperty("DisplayName", flags)?.GetValue(identity)?.ToString() ?? proxy?.Name ?? "unknown";
            return proxy is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryReadIntendedDamage(Monster monster, out int damage)
        => monster.modData.TryGetValue(IntendedDamageMarker, out string? raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out damage)
            && damage > 0;

    private static bool HasTrueMarker(NPC npc, string key)
        => npc.modData.TryGetValue(key, out string? raw)
            && raw.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeCaptureMethod(string typeKey, MethodInfo method)
    {
        string methodKey = Normalize(method.Name);
        string combined = typeKey + methodKey;
        return combined.Contains("capture")
            || combined.Contains("catch")
            || combined.Contains("pokeball")
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
        try { return getter(); }
        catch { return null; }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(type => type is not null).Cast<Type>(); }
        catch { return Array.Empty<Type>(); }
    }
}
