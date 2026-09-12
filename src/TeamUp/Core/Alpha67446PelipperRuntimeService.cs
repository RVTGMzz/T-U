using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.6 runtime compatibility layer.
/// Keeps source-aware Shiny safety while removing repeated reflection work, suppresses ordinary
/// Pelipper proxy Elite/Boss false positives, and gives Pelipper lethal damage a pre-death Mutation
/// interception point because its wild proxy lifecycle may bypass Monster.deathAnimation.
/// </summary>
internal sealed class Alpha67446PelipperRuntimeService
{
    public const string PreLethalMarker = "Ronvotri.TeamUp/PelipperPreLethalChecked";

    private sealed class ShinyCacheEntry
    {
        public bool Result { get; init; }
        public long ValidUntilTick { get; init; }
    }

    private sealed class LethalAttemptEntry
    {
        public long Tick { get; set; } = -1;
    }

    private static readonly ConditionalWeakTable<NPC, ShinyCacheEntry> ShinyEvidenceCache = new();
    private static readonly ConditionalWeakTable<Monster, LethalAttemptEntry> LethalAttemptCache = new();
    private static readonly MethodInfo? TryMutateMethod = typeof(MonsterMutationService).GetMethod(
        "TryMutate",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly HashSet<MethodBase> _damageHooks = new();

    public int PatchedDamageMethodCount => _damageHooks.Count;

    public Alpha67446PelipperRuntimeService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha67446PelipperRuntime");
        ApplyShinyEvidenceCachePatch();
        ApplyEliteFalsePositivePatch();
        ApplyPelipperPreLethalHooks();
    }

    public string Describe()
        => $"6.7.44.6 Pelipper runtime: damageHooks={_damageHooks.Count} | identityCache=weak-per-proxy | shinyReflectionCache=enabled | eliteProxyGuard=enabled | preLethalMutation=enabled";

    private void ApplyShinyEvidenceCachePatch()
    {
        MethodInfo? method = typeof(EncounterReactionService).GetMethod(
            "HasExplicitShinyEvidence",
            BindingFlags.Static | BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(NPC) },
            modifiers: null);
        if (method is null)
        {
            _monitor.Log("6.7.44.6 Shiny reflection cache patch skipped: method not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            method,
            prefix: new HarmonyMethod(typeof(Alpha67446PelipperRuntimeService), nameof(ShinyEvidencePrefix)),
            postfix: new HarmonyMethod(typeof(Alpha67446PelipperRuntimeService), nameof(ShinyEvidencePostfix)));
    }

    private static bool ShinyEvidencePrefix(NPC actor, ref bool __result)
    {
        if (ShinyEvidenceCache.TryGetValue(actor, out ShinyCacheEntry? cached)
            && cached.ValidUntilTick >= Game1.ticks)
        {
            __result = cached.Result;
            return false;
        }
        return true;
    }

    private static void ShinyEvidencePostfix(NPC actor, ref bool __result)
    {
        ShinyEvidenceCache.Remove(actor);
        // A positive Shiny state is immutable for that spawn. Negative evidence is refreshed every
        // two seconds so a just-created Pelipper actor still gets a short stabilization window.
        ShinyEvidenceCache.Add(actor, new ShinyCacheEntry
        {
            Result = __result,
            ValidUntilTick = Game1.ticks + (__result ? 3600 : 120)
        });
    }

    private void ApplyEliteFalsePositivePatch()
    {
        MethodInfo? method = typeof(EncounterReactionService).GetMethod(
            "LooksEliteOrBoss",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (method is null)
        {
            _monitor.Log("6.7.44.6 Pelipper Elite proxy guard skipped: method not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            method,
            postfix: new HarmonyMethod(typeof(Alpha67446PelipperRuntimeService), nameof(EliteBossPostfix)));
    }

    private static void EliteBossPostfix(Monster monster, ref bool __result)
    {
        if (!__result || !PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return;

        // Pelipper's ordinary wild combat proxy can carry generic metadata whose key happens to
        // contain Boss/Elite. Do not turn that proxy metadata into a Team Up boss reaction.
        // Explicit Team Up Mutation/Boss story actors are handled before this classifier.
        __result = false;
    }

    private void ApplyPelipperPreLethalHooks()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (!typeof(Monster).IsAssignableFrom(type))
                    continue;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    if (!method.Name.Equals("takeDamage", StringComparison.Ordinal)
                        || method.IsAbstract
                        || method.IsStatic
                        || method.ContainsGenericParameters
                        || _damageHooks.Contains(method))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0 || parameters[0].ParameterType != typeof(int))
                        continue;

                    try
                    {
                        _harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(typeof(Alpha67446PelipperRuntimeService), nameof(PelipperPreLethalPrefix)));
                        _damageHooks.Add(method);
                    }
                    catch (Exception ex)
                    {
                        _monitor.Log($"6.7.44.6 pre-lethal hook skipped {type.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }

        _monitor.Log($"Team Up 6.7.44.6 Pelipper pre-lethal Mutation patched {_damageHooks.Count} Monster.takeDamage implementation(s).", LogLevel.Info);
    }

    private static void PelipperPreLethalPrefix(Monster __instance, object[] __args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || __instance.Health <= 0
            || __args.Length == 0 || __args[0] is not int incoming || incoming <= 0)
        {
            return;
        }

        if (!PelipperTownCompatibilityService.IsWildCombatActor(__instance)
            || MonsterMutationService.IsMutant(__instance)
            || MonsterMutationService.IsMutationMinion(__instance)
            || EncounterReactionService.IsConfirmedShiny(__instance))
        {
            return;
        }

        // Only intercept a hit which can actually finish the current proxy. This makes the hook
        // effectively free during normal chip damage and gives the existing Mutation story/roll
        // exactly the lifecycle point Pelipper's proxy removal was bypassing.
        if (incoming < __instance.Health)
            return;

        LethalAttemptEntry stamp = LethalAttemptCache.GetOrCreateValue(__instance);
        if (stamp.Tick == Game1.ticks)
            return;
        stamp.Tick = Game1.ticks;
        __instance.modData[PreLethalMarker] = Game1.ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);

        MonsterMutationService? service = MonsterMutationService.ActiveInstance;
        if (service is null || TryMutateMethod is null)
            return;

        try
        {
            MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);
            bool mutated = TryMutateMethod.Invoke(service, new object[] { __instance, false }) is true;
            if (!mutated)
                return;

            // The lethal hit became the mutation trigger. Do not immediately apply that same lethal
            // damage to the freshly transformed Mutant.
            __args[0] = 0;
        }
        catch
        {
            // Compatibility path must never break Pelipper's own damage lifecycle.
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(type => type is not null).Cast<Type>(); }
        catch { return Array.Empty<Type>(); }
    }
}
