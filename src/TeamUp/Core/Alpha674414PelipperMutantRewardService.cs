using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.15: every Team Up Mutant keeps its 2-4 minion wave and earns x3 native loot
/// when finally defeated. Team Up does not fabricate reward items; it repeats Stardew's actual
/// monsterDrop pass two extra times for the same Mutant so vanilla/custom drop logic remains authoritative.
/// </summary>
internal sealed class Alpha674414PelipperMutantRewardService
{
    public const string LootMultiplierMarker = "Ronvotri.TeamUp/MutantLootMultiplier";
    public const int MutantLootMultiplier = 3;

    [ThreadStatic]
    private static bool _reentry;

    private static Alpha674414PelipperMutantRewardService? Active;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly HashSet<MethodBase> _dropHooks = new();

    private long _markedMutants;
    private long _dropCalls;
    private long _extraDropPasses;
    private long _errors;
    private string _last = "reset";

    public int PatchedDropMethodCount => _dropHooks.Count;

    public Alpha674414PelipperMutantRewardService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha674415GlobalMutantReward");
        Active = this;

        ApplyMutationRewardHook();
        ApplyDropHooks();

        _monitor.Log(
            $"Team Up 6.7.44.15 global Mutant reward enabled: all Mutants keep 2-4 minions and receive loot x{MutantLootMultiplier}; patched {_dropHooks.Count} monsterDrop method(s).",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutant reward: lootX{MutantLootMultiplier} | minions=2-4 | scope=all-mutants | marked={_markedMutants} | "
            + $"dropHooks={_dropHooks.Count} | dropCalls={_dropCalls} | extraDropPasses={_extraDropPasses} | errors={_errors} | last={_last}";

    public void ResetTelemetry()
    {
        _markedMutants = 0;
        _dropCalls = 0;
        _extraDropPasses = 0;
        _errors = 0;
        _last = "reset";
    }

    private void ApplyMutationRewardHook()
    {
        MethodInfo? tryMutate = AccessTools.Method(typeof(MonsterMutationService), "TryMutate");
        if (tryMutate is null)
        {
            _monitor.Log("6.7.44.15 global Mutant loot marker unavailable: TryMutate not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            tryMutate,
            postfix: new HarmonyMethod(typeof(Alpha674414PelipperMutantRewardService), nameof(TryMutatePostfix))
            {
                priority = Priority.Last
            });
    }

    private static void TryMutatePostfix(Monster __0, bool __result)
    {
        Alpha674414PelipperMutantRewardService? service = Active;
        if (service is null || !__result || !Context.IsWorldReady || !MonsterMutationService.IsMutant(__0))
            return;

        __0.modData[LootMultiplierMarker] = MutantLootMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture);
        service._markedMutants++;
        service._last = $"marked source={ReadDisplayName(__0)} lootX{MutantLootMultiplier} minions=2-4";
    }

    private void ApplyDropHooks()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (!typeof(GameLocation).IsAssignableFrom(type))
                    continue;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    if (!method.Name.Equals("monsterDrop", StringComparison.Ordinal)
                        || method.IsStatic
                        || method.IsAbstract
                        || method.ContainsGenericParameters
                        || method.ReturnType != typeof(void)
                        || _dropHooks.Contains(method))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (!parameters.Any(parameter => typeof(Monster).IsAssignableFrom(parameter.ParameterType)))
                        continue;

                    try
                    {
                        _harmony.Patch(
                            method,
                            postfix: new HarmonyMethod(typeof(Alpha674414PelipperMutantRewardService), nameof(MonsterDropPostfix))
                            {
                                priority = Priority.Last
                            });
                        _dropHooks.Add(method);
                    }
                    catch (Exception ex)
                    {
                        _monitor.Log($"6.7.44.15 loot hook skipped {type.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }
    }

    private static void MonsterDropPostfix(GameLocation __instance, MethodBase __originalMethod, object[] __args)
    {
        Alpha674414PelipperMutantRewardService? service = Active;
        if (service is null || _reentry || !Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        Monster? monster = __args.OfType<Monster>().FirstOrDefault();
        if (monster is null
            || !MonsterMutationService.IsMutant(monster)
            || !monster.modData.TryGetValue(LootMultiplierMarker, out string? raw)
            || !int.TryParse(raw, out int multiplier)
            || multiplier <= 1)
        {
            return;
        }

        int cappedMultiplier = Math.Clamp(multiplier, 1, MutantLootMultiplier);
        service._dropCalls++;

        try
        {
            _reentry = true;
            int extra = 0;
            for (int pass = 1; pass < cappedMultiplier; pass++)
            {
                __originalMethod.Invoke(__instance, __args);
                extra++;
            }

            service._extraDropPasses += extra;
            service._last = $"rewarded source={ReadDisplayName(monster)} nativeDropPasses={1 + extra}";
        }
        catch (Exception ex)
        {
            service._errors++;
            service._last = $"drop-repeat-failed {ex.GetType().Name}: {ex.InnerException?.Message ?? ex.Message}";
            service._monitor.Log($"6.7.44.15 Mutant x3 loot failed safely: {ex}", LogLevel.Warn);
        }
        finally
        {
            _reentry = false;
        }
    }

    private static string ReadDisplayName(Monster monster)
    {
        if (PelipperTownCompatibilityService.IsWildCombatActor(monster)
            && PelipperWildEncounterIdentityService.TryResolve(monster, out PelipperWildEncounterIdentity identity)
            && !string.IsNullOrWhiteSpace(identity.DisplayName))
        {
            return identity.DisplayName;
        }

        return string.IsNullOrWhiteSpace(monster.displayName) ? monster.Name : monster.displayName;
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
