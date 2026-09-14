using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.14: Pelipper Mutants temporarily trade the unreliable 2-4 minion wave for x3 loot.
/// Rather than fabricating items, Team Up repeats Stardew's native monsterDrop pass two extra times
/// for the same slain Pelipper Mutant. This preserves the source game's / other mods' actual drop
/// generation while keeping the reward scoped to Pelipper wild Mutants only.
/// </summary>
internal sealed class Alpha674414PelipperMutantRewardService
{
    public const string LootMultiplierMarker = "Ronvotri.TeamUp/PelipperMutantLootMultiplier";
    public const int PelipperLootMultiplier = 3;

    [ThreadStatic]
    private static bool _reentry;

    private static Alpha674414PelipperMutantRewardService? Active;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly HashSet<MethodBase> _dropHooks = new();

    private long _dropCalls;
    private long _extraDropPasses;
    private long _errors;
    private string _last = "reset";

    public int PatchedDropMethodCount => _dropHooks.Count;

    public Alpha674414PelipperMutantRewardService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha674414PelipperMutantReward");
        Active = this;
        ApplyDropHooks();

        _monitor.Log(
            $"Team Up 6.7.44.14 Pelipper Mutant reward enabled: loot x{PelipperLootMultiplier}, Pelipper minion wave temporarily suppressed; patched {_dropHooks.Count} monsterDrop method(s).",
            LogLevel.Info);
    }

    public string Describe()
        => $"Pelipper Mutant reward: lootX{PelipperLootMultiplier} | minions=off | dropHooks={_dropHooks.Count} | dropCalls={_dropCalls} | extraDropPasses={_extraDropPasses} | errors={_errors} | last={_last}";

    public void ResetTelemetry()
    {
        _dropCalls = 0;
        _extraDropPasses = 0;
        _errors = 0;
        _last = "reset";
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
                        _monitor.Log($"6.7.44.14 loot hook skipped {type.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
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
            || !PelipperTownCompatibilityService.IsWildCombatActor(monster)
            || !monster.modData.TryGetValue(LootMultiplierMarker, out string? raw)
            || !int.TryParse(raw, out int multiplier)
            || multiplier <= 1)
        {
            return;
        }

        int cappedMultiplier = Math.Clamp(multiplier, 1, PelipperLootMultiplier);
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
            string name = monster.modData.TryGetValue("Ronvotri.TeamUp/PelipperDisplayName", out string? display)
                && !string.IsNullOrWhiteSpace(display)
                    ? display
                    : monster.Name;
            service._last = $"rewarded source={name} nativeDropPasses={1 + extra}";
        }
        catch (Exception ex)
        {
            service._errors++;
            service._last = $"drop-repeat-failed {ex.GetType().Name}: {ex.InnerException?.Message ?? ex.Message}";
            service._monitor.Log($"6.7.44.14 Pelipper Mutant x3 loot failed safely: {ex}", LogLevel.Warn);
        }
        finally
        {
            _reentry = false;
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
