using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.17 design lock:
/// - the Mutant leader is never a valid Pelipper capture target;
/// - Mutation minions are temporary combat summons and are never valid capture targets;
/// - naturally spawned, non-Mutant Pelipper wild Pokemon keep Pelipper's normal capture flow.
///
/// Team Up marks both the hidden combat proxy and paired visible Pokemon source, then conservatively
/// patches only Pelipper bool-returning capture/catch/PokeBall methods whose signature carries an
/// encounter/actor-shaped target. Unknown Pelipper internals fail open instead of breaking capture.
/// </summary>
internal sealed class Alpha674417MutationCaptureLockService
{
    public const string CaptureBlockedMarker = "Ronvotri.TeamUp/MutationCaptureBlocked";

    private static readonly string[] CaptureNameTokens = { "capture", "catch", "pokeball" };
    private static readonly string[] TargetTypeTokens =
    {
        "pokemonnpc", "wildencounter", "wildpokemon", "encounterruntime", "encounteractor",
        "combatproxy", "wildruntime"
    };

    private static Alpha674417MutationCaptureLockService? Active;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly Func<bool> _isVietnamese;
    private readonly HashSet<MethodBase> _captureHooks = new();

    private long _leadersMarked;
    private long _sourcesMarked;
    private long _minionsMarked;
    private long _captureAttempts;
    private long _blocked;
    private long _passthrough;
    private long _resolveFailures;
    private long _lastHudTick = -9999;
    private string _last = "reset";

    public int PatchedCaptureMethodCount => _captureHooks.Count;

    public Alpha674417MutationCaptureLockService(IMonitor monitor, string uniqueId, Func<bool> isVietnamese)
    {
        _monitor = monitor;
        _isVietnamese = isVietnamese;
        _harmony = new Harmony($"{uniqueId}.Alpha674417MutationCaptureLock");
        Active = this;

        PatchMutationLifecycle();
        PatchPelipperCaptureMethods();

        _monitor.Log(
            $"Team Up 6.7.44.17 Mutation capture lock enabled: leader/minions non-catchable, natural wild capture unchanged; Pelipper capture hooks={_captureHooks.Count}.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutation capture lock: policy=leader+minions-blocked/natural-wild-allowed | hooks={_captureHooks.Count} | "
            + $"leadersMarked={_leadersMarked} | sourcesMarked={_sourcesMarked} | minionsMarked={_minionsMarked} | "
            + $"attempts={_captureAttempts} | blocked={_blocked} | passthrough={_passthrough} | resolveFailures={_resolveFailures} | last={_last}";

    public void ResetTelemetry()
    {
        _leadersMarked = 0;
        _sourcesMarked = 0;
        _minionsMarked = 0;
        _captureAttempts = 0;
        _blocked = 0;
        _passthrough = 0;
        _resolveFailures = 0;
        _lastHudTick = -9999;
        _last = _captureHooks.Count > 0 ? "reset" : "reset-no-supported-capture-hook";
    }

    private void PatchMutationLifecycle()
    {
        MethodInfo? tryMutate = AccessTools.Method(typeof(MonsterMutationService), "TryMutate");
        if (tryMutate is not null)
        {
            _harmony.Patch(
                tryMutate,
                postfix: new HarmonyMethod(typeof(Alpha674417MutationCaptureLockService), nameof(TryMutatePostfix))
                {
                    priority = Priority.Last
                });
        }

        MethodInfo? spawnWave = AccessTools.Method(typeof(MonsterMutationService), "SpawnMinionWave");
        if (spawnWave is not null)
        {
            _harmony.Patch(
                spawnWave,
                postfix: new HarmonyMethod(typeof(Alpha674417MutationCaptureLockService), nameof(SpawnWavePostfix))
                {
                    priority = Priority.Last
                });
        }
    }

    private void PatchPelipperCaptureMethods()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string assemblyName = assembly.GetName().Name ?? string.Empty;
            if (!assemblyName.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (Type type in SafeGetTypes(assembly))
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    if (!IsSupportedCaptureMethod(method) || !_captureHooks.Add(method))
                        continue;

                    try
                    {
                        _harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(typeof(Alpha674417MutationCaptureLockService), nameof(CapturePrefix))
                            {
                                priority = Priority.First
                            });
                    }
                    catch (Exception ex)
                    {
                        _captureHooks.Remove(method);
                        _monitor.Log($"6.7.44.17 capture-lock hook skipped {type.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }

        if (_captureHooks.Count == 0)
        {
            _last = "no-supported-capture-hook";
            _monitor.Log(
                "6.7.44.17 capture-lock found no conservative Pelipper target-bearing bool capture method. Mutation actors are marked non-catchable, but exact Poke Ball rejection still needs a live Pelipper surface probe.",
                LogLevel.Warn);
        }
    }

    private static bool IsSupportedCaptureMethod(MethodInfo method)
    {
        if (method.IsAbstract || method.IsGenericMethodDefinition || method.IsSpecialName || method.ReturnType != typeof(bool))
            return false;

        string name = Normalize(method.Name);
        if (!CaptureNameTokens.Any(name.Contains))
            return false;

        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Any(parameter => IsActorType(parameter.ParameterType) || LooksEncounterTargetType(parameter.ParameterType));
    }

    private static bool IsActorType(Type type)
        => typeof(NPC).IsAssignableFrom(type) || typeof(Monster).IsAssignableFrom(type);

    private static bool LooksEncounterTargetType(Type type)
    {
        string name = Normalize(type.FullName ?? type.Name);
        return TargetTypeTokens.Any(name.Contains);
    }

    private static void TryMutatePostfix(Monster __0, bool __result)
    {
        Alpha674417MutationCaptureLockService? service = Active;
        if (service is null || !__result || !MonsterMutationService.IsMutant(__0))
            return;

        bool newlyMarked = !__0.modData.ContainsKey(CaptureBlockedMarker);
        __0.modData[CaptureBlockedMarker] = "leader";
        if (newlyMarked)
            service._leadersMarked++;

        if (PelipperTownCompatibilityService.IsWildCombatActor(__0)
            && PelipperWildEncounterIdentityService.TryResolve(__0, out PelipperWildEncounterIdentity identity))
        {
            bool sourceNew = !identity.SourceActor.modData.ContainsKey(CaptureBlockedMarker);
            identity.SourceActor.modData[CaptureBlockedMarker] = "leader-source";
            if (sourceNew)
                service._sourcesMarked++;
            service._last = $"marked leader={identity.DisplayName} proxy+source captureBlocked=true";
        }
        else
        {
            service._last = $"marked leader={ReadName(__0)} captureBlocked=true";
        }
    }

    private static void SpawnWavePostfix(object __0)
    {
        Alpha674417MutationCaptureLockService? service = Active;
        if (service is null || !Context.IsWorldReady)
            return;

        GameLocation? location = TryReadMember(__0, "Location") as GameLocation
            ?? (TryReadMember(__0, "Mutant") as Monster)?.currentLocation;
        if (location is null)
            return;

        int added = 0;
        foreach (Monster minion in location.characters.OfType<Monster>().Where(MonsterMutationService.IsMutationMinion))
        {
            if (minion.modData.ContainsKey(CaptureBlockedMarker))
                continue;

            minion.modData[CaptureBlockedMarker] = "minion";
            service._minionsMarked++;
            added++;
        }

        if (added > 0)
            service._last = $"marked {added} temporary Mutation minion(s) captureBlocked=true";
    }

    private static bool CapturePrefix(object? __instance, MethodBase __originalMethod, object[] __args, ref bool __result)
    {
        Alpha674417MutationCaptureLockService? service = Active;
        if (service is null || !Context.IsWorldReady)
            return true;

        service._captureAttempts++;
        NPC? target = FindBlockedActor(__args, __instance);
        if (target is null)
        {
            service._passthrough++;
            service._last = $"pass {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name} natural/unknown target";
            return true;
        }

        __result = false;
        service._blocked++;
        service._last = $"blocked {__originalMethod.DeclaringType?.Name}.{__originalMethod.Name} target={ReadName(target)} reason=mutation";
        service.ShowBlockedMessage();
        return false;
    }

    private void ShowBlockedMessage()
    {
        long tick = Game1.ticks;
        if (tick - _lastHudTick < 45)
            return;
        _lastHudTick = tick;
        Game1.showGlobalMessage(_isVietnamese()
            ? "Pokémon Đột biến và đệ triệu hồi không thể bị bắt."
            : "Mutant Pokémon and their summoned minions can't be captured.");
    }

    private static NPC? FindBlockedActor(IEnumerable<object?> args, object? instance)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (object? arg in args)
        {
            NPC? found = FindBlockedActorRecursive(arg, 0, visited);
            if (found is not null)
                return found;
        }
        return FindBlockedActorRecursive(instance, 0, visited);
    }

    private static NPC? FindBlockedActorRecursive(object? value, int depth, HashSet<object> visited)
    {
        if (value is null || depth > 2)
            return null;

        if (value is NPC actor)
        {
            if (IsCaptureBlocked(actor))
                return actor;
            return null;
        }

        Type type = value.GetType();
        if (type.IsPrimitive || type.IsEnum || value is string || !visited.Add(value))
            return null;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            Type fieldType = field.FieldType;
            string fieldName = Normalize(field.Name);
            bool actorLike = IsActorType(fieldType)
                || LooksEncounterTargetType(fieldType)
                || fieldName.Contains("actor")
                || fieldName.Contains("pokemon")
                || fieldName.Contains("wild")
                || fieldName.Contains("encounter")
                || fieldName.Contains("proxy")
                || fieldName.Contains("target");
            if (!actorLike)
                continue;

            try
            {
                NPC? found = FindBlockedActorRecursive(field.GetValue(value), depth + 1, visited);
                if (found is not null)
                    return found;
            }
            catch
            {
            }
        }

        return null;
    }

    private static bool IsCaptureBlocked(NPC actor)
    {
        if (actor.modData.ContainsKey(CaptureBlockedMarker))
            return true;
        if (actor is Monster monster)
            return MonsterMutationService.IsMutant(monster) || MonsterMutationService.IsMutationMinion(monster);
        return false;
    }

    private static object? TryReadMember(object target, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            try
            {
                FieldInfo? field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field is not null)
                    return field.GetValue(target);
                PropertyInfo? property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0)
                    return property.GetValue(target);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    private static string ReadName(NPC actor)
        => string.IsNullOrWhiteSpace(actor.displayName) ? actor.Name : actor.displayName;

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
