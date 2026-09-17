using System.Globalization;
using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.26 hardens the Pelipper Mutant three-phase lifecycle without replacing the proven
/// source-aware HP engine. It observes the existing phase guard, records explicit phase state, and
/// suppresses native monsterDrop calls if a provider tries to reward a Mutant during a guarded
/// non-final phase. Final-phase native death/drop behavior remains authoritative.
/// </summary>
internal sealed class Alpha674426PelipperMutantPhaseLifecycleService
{
    public const string PhaseTotalMarker = "Ronvotri.TeamUp/MutationPhaseTotal";
    public const string PhaseCurrentMarker = "Ronvotri.TeamUp/MutationPhaseCurrent";
    public const string PhaseGuardTickMarker = "Ronvotri.TeamUp/MutationPhaseGuardTick";
    public const string FinalLethalTickMarker = "Ronvotri.TeamUp/MutationFinalLethalTick";

    private const string CurrentHpKey = "Griff.PelipperTown/WildCurrentHealth";
    private const string MaxHpKey = "Griff.PelipperTown/WildMaxHealth";

    private sealed class DamageState
    {
        public bool Mutant { get; init; }
        public int Incoming { get; init; }
        public int ExtraLivesBefore { get; init; }
        public int CurrentHpBefore { get; init; }
        public int MaxHp { get; init; }
        public int TotalPhases { get; init; }
        public int CurrentPhaseBefore { get; init; }
    }

    private static Alpha674426PelipperMutantPhaseLifecycleService? Active { get; set; }

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly HashSet<MethodBase> _dropHooks = new();

    private long _trackedMutants;
    private long _phaseTransitions;
    private long _enteredPhase2;
    private long _enteredPhase3;
    private long _phaseRestoreVerified;
    private long _phaseRestoreMismatch;
    private long _finalLethalArmed;
    private long _prematureDropBlocks;
    private long _finalDropPasses;
    private long _unverifiedFinalDropPasses;
    private long _invalidPhaseState;
    private string _last = "reset";

    public Alpha674426PelipperMutantPhaseLifecycleService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony(uniqueId + ".Alpha674426PelipperMutantPhaseLifecycle");
        Active = this;

        ApplyMutationHook();
        ApplySourceDamageObserver();
        ApplyDropGuards();

        _monitor.Log(
            $"Team Up 6.7.44.26 Pelipper Mutant phase lifecycle enabled: explicit phase 1/3 -> 2/3 -> 3/3 tracking, guarded-phase loot suppression, final native death/drop preserved; patched {_dropHooks.Count} monsterDrop method(s).",
            LogLevel.Info);
    }

    public string Describe()
        => $"Pelipper Mutation phases: explicit-3-phase | tracked={_trackedMutants} | transitions={_phaseTransitions} | phase2={_enteredPhase2} | phase3={_enteredPhase3} | "
            + $"restoreVerified={_phaseRestoreVerified} | restoreMismatch={_phaseRestoreMismatch} | finalLethalArmed={_finalLethalArmed} | "
            + $"prematureDropBlocks={_prematureDropBlocks} | finalDropPasses={_finalDropPasses} | unverifiedFinalDrops={_unverifiedFinalDropPasses} | "
            + $"invalidState={_invalidPhaseState} | dropHooks={_dropHooks.Count} | last={_last}";

    public void ResetTelemetry()
    {
        _trackedMutants = 0;
        _phaseTransitions = 0;
        _enteredPhase2 = 0;
        _enteredPhase3 = 0;
        _phaseRestoreVerified = 0;
        _phaseRestoreMismatch = 0;
        _finalLethalArmed = 0;
        _prematureDropBlocks = 0;
        _finalDropPasses = 0;
        _unverifiedFinalDropPasses = 0;
        _invalidPhaseState = 0;
        _last = "reset";
    }

    private void ApplyMutationHook()
    {
        MethodInfo? tryMutate = AccessTools.Method(typeof(MonsterMutationService), "TryMutate");
        if (tryMutate is null)
        {
            _monitor.Log("6.7.44.26 phase lifecycle mutation hook unavailable: TryMutate not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            tryMutate,
            postfix: new HarmonyMethod(typeof(Alpha674426PelipperMutantPhaseLifecycleService), nameof(AfterTryMutate))
            {
                priority = Priority.Last
            });
    }

    private void ApplySourceDamageObserver()
    {
        MethodInfo? sourceDamage = AccessTools.Method(typeof(Alpha67448PelipperSourceMutationService), "SourceAwareDamagePrefix");
        if (sourceDamage is null)
        {
            _monitor.Log("6.7.44.26 phase lifecycle damage observer unavailable: source-aware damage hook not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            sourceDamage,
            prefix: new HarmonyMethod(typeof(Alpha674426PelipperMutantPhaseLifecycleService), nameof(BeforeSourceDamage))
            {
                priority = Priority.First
            },
            postfix: new HarmonyMethod(typeof(Alpha674426PelipperMutantPhaseLifecycleService), nameof(AfterSourceDamage))
            {
                priority = Priority.Last
            });
    }

    private void ApplyDropGuards()
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

                    if (!method.GetParameters().Any(parameter => typeof(Monster).IsAssignableFrom(parameter.ParameterType)))
                        continue;

                    try
                    {
                        _harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(typeof(Alpha674426PelipperMutantPhaseLifecycleService), nameof(BeforeMonsterDrop))
                            {
                                priority = Priority.First
                            });
                        _dropHooks.Add(method);
                    }
                    catch (Exception ex)
                    {
                        _monitor.Log($"6.7.44.26 phase loot guard skipped {type.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }
    }

    private static void AfterTryMutate(Monster __0, bool __result)
    {
        Alpha674426PelipperMutantPhaseLifecycleService? service = Active;
        if (service is null || !__result || !Context.IsWorldReady || !MonsterMutationService.IsMutant(__0)
            || !PelipperTownCompatibilityService.IsWildCombatActor(__0))
        {
            return;
        }

        if (!service.EnsurePhaseState(__0, countTracked: true, out int total, out int current))
            return;

        __0.modData[PhaseGuardTickMarker] = "-1";
        __0.modData[FinalLethalTickMarker] = "-1";
        service._last = $"tracked {ReadDisplayName(__0)} phase={current}/{total} hp={ReadHpText(__0)}";
    }

    private static void BeforeSourceDamage(Monster __0, object[] __1, out DamageState? __state)
    {
        __state = null;
        Alpha674426PelipperMutantPhaseLifecycleService? service = Active;
        if (service is null || !Context.IsWorldReady || !MonsterMutationService.IsMutant(__0)
            || !PelipperTownCompatibilityService.IsWildCombatActor(__0)
            || __1.Length == 0 || __1[0] is not int incoming || incoming <= 0)
        {
            return;
        }

        if (!service.EnsurePhaseState(__0, countTracked: false, out int total, out int current))
            return;

        TryReadHp(__0, CurrentHpKey, out int currentHp);
        TryReadHp(__0, MaxHpKey, out int maxHp);
        __state = new DamageState
        {
            Mutant = true,
            Incoming = incoming,
            ExtraLivesBefore = ReadInt(__0, Alpha67448PelipperSourceMutationService.ExtraLifeMarker),
            CurrentHpBefore = currentHp,
            MaxHp = maxHp,
            TotalPhases = total,
            CurrentPhaseBefore = current
        };
    }

    private static void AfterSourceDamage(Monster __0, object[] __1, DamageState? __state)
    {
        Alpha674426PelipperMutantPhaseLifecycleService? service = Active;
        if (service is null || __state is null || !__state.Mutant)
            return;

        int extraAfter = ReadInt(__0, Alpha67448PelipperSourceMutationService.ExtraLifeMarker);
        int currentAfter = Math.Clamp(__state.TotalPhases - extraAfter, 1, __state.TotalPhases);
        __0.modData[PhaseCurrentMarker] = currentAfter.ToString(CultureInfo.InvariantCulture);

        TryReadHp(__0, CurrentHpKey, out int hpAfter);
        TryReadHp(__0, MaxHpKey, out int maxAfter);
        if (maxAfter <= 0)
            maxAfter = __state.MaxHp;

        if (extraAfter < __state.ExtraLivesBefore)
        {
            long tick = Game1.ticks;
            long previousGuardTick = ReadLong(__0, PhaseGuardTickMarker);
            __0.modData[PhaseGuardTickMarker] = tick.ToString(CultureInfo.InvariantCulture);

            if (previousGuardTick != tick)
            {
                service._phaseTransitions++;
                if (currentAfter == 2)
                    service._enteredPhase2++;
                if (currentAfter == 3)
                    service._enteredPhase3++;

                if (maxAfter > 0 && hpAfter == maxAfter)
                    service._phaseRestoreVerified++;
                else
                    service._phaseRestoreMismatch++;

                service._last = $"phase-transition {ReadDisplayName(__0)} {__state.CurrentPhaseBefore}/{__state.TotalPhases}->{currentAfter}/{__state.TotalPhases} hp={hpAfter}/{maxAfter} extraLives={extraAfter}";
            }
            return;
        }

        bool lethalCandidate = __state.ExtraLivesBefore <= 0
            && __state.CurrentHpBefore > 0
            && __state.Incoming >= __state.CurrentHpBefore;
        if (!lethalCandidate)
            return;

        long finalTick = Game1.ticks;
        if (ReadLong(__0, FinalLethalTickMarker) != finalTick)
        {
            __0.modData[FinalLethalTickMarker] = finalTick.ToString(CultureInfo.InvariantCulture);
            __0.modData[PhaseCurrentMarker] = __state.TotalPhases.ToString(CultureInfo.InvariantCulture);
            service._finalLethalArmed++;
            service._last = $"final-lethal-armed {ReadDisplayName(__0)} phase={__state.TotalPhases}/{__state.TotalPhases} hp={__state.CurrentHpBefore}/{__state.MaxHp} incoming={__state.Incoming}";
        }
    }

    private static bool BeforeMonsterDrop(object[] __args)
    {
        Alpha674426PelipperMutantPhaseLifecycleService? service = Active;
        if (service is null || !Context.IsWorldReady || !Context.IsMainPlayer)
            return true;

        Monster? monster = __args.OfType<Monster>().FirstOrDefault();
        if (monster is null || !MonsterMutationService.IsMutant(monster)
            || !PelipperTownCompatibilityService.IsWildCombatActor(monster))
        {
            return true;
        }

        if (!service.EnsurePhaseState(monster, countTracked: false, out int total, out int current))
            return true;

        long guardTick = ReadLong(monster, PhaseGuardTickMarker);
        long finalTick = ReadLong(monster, FinalLethalTickMarker);
        bool guardedTransitionWindow = guardTick >= 0
            && Game1.ticks - guardTick <= 1
            && finalTick < guardTick;

        if (current < total || guardedTransitionWindow)
        {
            service._prematureDropBlocks++;
            service._last = $"blocked-premature-drop {ReadDisplayName(monster)} phase={current}/{total} guardTick={guardTick} finalTick={finalTick}";
            return false;
        }

        if (finalTick >= 0 && Game1.ticks - finalTick <= 3)
        {
            service._finalDropPasses++;
            service._last = $"allowed-final-drop {ReadDisplayName(monster)} phase={current}/{total} finalTick={finalTick}";
        }
        else
        {
            service._unverifiedFinalDropPasses++;
            service._last = $"allowed-unverified-final-drop {ReadDisplayName(monster)} phase={current}/{total} guardTick={guardTick} finalTick={finalTick}";
        }
        return true;
    }

    private bool EnsurePhaseState(Monster monster, bool countTracked, out int total, out int current)
    {
        total = ReadInt(monster, PhaseTotalMarker);
        current = ReadInt(monster, PhaseCurrentMarker);

        int extraLives = Math.Max(0, ReadInt(monster, Alpha67448PelipperSourceMutationService.ExtraLifeMarker));
        TryReadHp(monster, MaxHpKey, out int maxHp);
        int logicalMax = ReadInt(monster, Alpha67448PelipperSourceMutationService.LogicalMaxHpMarker);

        if (total <= 0)
        {
            if (maxHp > 0 && logicalMax > 0)
                total = Math.Max(1, (int)Math.Round(logicalMax / (double)maxHp, MidpointRounding.AwayFromZero));
            total = Math.Max(total, extraLives + 1);
        }

        if (total <= 0 || total > 10)
        {
            _invalidPhaseState++;
            _last = $"invalid-phase-state {ReadDisplayName(monster)} total={total} extraLives={extraLives} hp={ReadHpText(monster)} logicalMax={logicalMax}";
            total = 0;
            current = 0;
            return false;
        }

        int resolvedCurrent = Math.Clamp(total - extraLives, 1, total);
        if (current <= 0 || current > total || current != resolvedCurrent)
            current = resolvedCurrent;

        monster.modData[PhaseTotalMarker] = total.ToString(CultureInfo.InvariantCulture);
        monster.modData[PhaseCurrentMarker] = current.ToString(CultureInfo.InvariantCulture);
        if (!monster.modData.ContainsKey(PhaseGuardTickMarker))
            monster.modData[PhaseGuardTickMarker] = "-1";
        if (!monster.modData.ContainsKey(FinalLethalTickMarker))
            monster.modData[FinalLethalTickMarker] = "-1";

        if (countTracked)
            _trackedMutants++;
        return true;
    }

    private static int ReadInt(Monster monster, string key)
        => monster.modData.TryGetValue(key, out string? raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : 0;

    private static long ReadLong(Monster monster, string key)
        => monster.modData.TryGetValue(key, out string? raw)
            && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
            ? parsed
            : -1L;

    private static bool TryReadHp(Monster monster, string key, out int value)
    {
        value = 0;
        return monster.modData.TryGetValue(key, out string? raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static string ReadHpText(Monster monster)
    {
        TryReadHp(monster, CurrentHpKey, out int current);
        TryReadHp(monster, MaxHpKey, out int max);
        return $"{current}/{max}";
    }

    private static string ReadDisplayName(Monster monster)
    {
        if (PelipperWildEncounterIdentityService.TryResolve(monster, out PelipperWildEncounterIdentity identity)
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
