using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Small overlay on the proven 6.7.44.23 steering runtime. It widens only the Mutant leader's
/// effective melee band without duplicating or replacing follower steering. 6.7.44.25 chains a
/// continuous-chase child overlay here so existing ModEntry registration/status/reset wiring stays
/// stable while per-step leader Halt() calls are bypassed outside the 128px hold band. 6.7.44.26
/// chains explicit three-phase lifecycle telemetry/loot guarding through the same stable surface.
/// 6.7.44.27 adds a source-equivalent regression guard so unsupported non-Pelipper follower sources
/// fail closed instead of ever escaping as an unrelated GreenSlime fallback.
/// </summary>
internal sealed class Alpha674424EliteReachOverlayService
{
    private const float NativeLeaderAttackDistance = 112f;
    private const float ExtendedLeaderAttackDistance = 160f;
    private const float ExtendedLeaderHoldDistance = 128f;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly Alpha674425PelipperLeaderContinuousChaseService _continuousChase;
    private readonly Alpha674426PelipperMutantPhaseLifecycleService _phaseLifecycle;
    private readonly Alpha674427MutationRegressionGuardService _regressionGuard;
    private long _reachExpansions;
    private long _holdOverrides;
    private string _last = "reset";

    public Alpha674424EliteReachOverlayService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony(uniqueId + ".Alpha674424EliteReachOverlay");
        ActiveInstance = this;
        Apply();
        _continuousChase = new Alpha674425PelipperLeaderContinuousChaseService(monitor, uniqueId);
        _phaseLifecycle = new Alpha674426PelipperMutantPhaseLifecycleService(monitor, uniqueId);
        _regressionGuard = new Alpha674427MutationRegressionGuardService(monitor, uniqueId);
        _monitor.Log(
            "Team Up 6.7.44.27 Elite runtime chain enabled: 160px reach, 128px hold, continuous leader chase, explicit three-phase lifecycle, guarded-phase loot suppression and source-equivalent-only Mutation followers; follower steering unchanged.",
            LogLevel.Info);
    }

    private static Alpha674424EliteReachOverlayService? ActiveInstance { get; set; }

    public string Describe()
        => $"Mutation elite reach overlay: attack=160px | hold=128px | reachExpansions={_reachExpansions} | holdOverrides={_holdOverrides} | last={_last}"
            + Environment.NewLine
            + _continuousChase.Describe()
            + Environment.NewLine
            + _phaseLifecycle.Describe()
            + Environment.NewLine
            + _regressionGuard.Describe();

    public void ResetTelemetry()
    {
        _reachExpansions = 0;
        _holdOverrides = 0;
        _last = "reset";
        _continuousChase.ResetTelemetry();
        _phaseLifecycle.ResetTelemetry();
        _regressionGuard.ResetTelemetry();
    }

    private void Apply()
    {
        MethodInfo? damage = AccessTools.Method(typeof(Alpha674423PelipperMutantLeaderSmoothingService), "TryDamage");
        if (damage is not null)
        {
            _harmony.Patch(
                damage,
                prefix: new HarmonyMethod(typeof(Alpha674424EliteReachOverlayService), nameof(BeforeTryDamage)));
        }

        MethodInfo? move = AccessTools.Method(typeof(Alpha674423PelipperMutantLeaderSmoothingService), "MoveSource");
        if (move is not null)
        {
            _harmony.Patch(
                move,
                prefix: new HarmonyMethod(typeof(Alpha674424EliteReachOverlayService), nameof(BeforeMoveSource)));
        }
    }

    private static void BeforeTryDamage(object[] __args)
    {
        Alpha674424EliteReachOverlayService? service = ActiveInstance;
        if (service is null || __args.Length < 3 || __args[2] is not float actualDistance)
            return;
        if (!TryReadLeader(__args[1], out bool leader) || !leader)
            return;
        if (actualDistance <= NativeLeaderAttackDistance || actualDistance > ExtendedLeaderAttackDistance)
            return;

        // 6.7.44.23 compares this argument against its frozen 112px constant. Scale only the
        // leader distance argument so the existing cooldown/damage path accepts up to 160px.
        __args[2] = actualDistance * (NativeLeaderAttackDistance / ExtendedLeaderAttackDistance);
        service._reachExpansions++;
        service._last = $"reach-expanded actual={actualDistance:0.0}px";
    }

    private static bool BeforeMoveSource(object[] __args, ref bool __result)
    {
        Alpha674424EliteReachOverlayService? service = ActiveInstance;
        if (service is null || __args.Length < 3 || __args[0] is not NPC source || __args[1] is not Vector2 steering || __args[2] is not bool leader || !leader)
            return true;

        float distance = steering.Length();
        if (distance > ExtendedLeaderHoldDistance)
            return true;

        source.Halt();
        source.SetMovingUp(false);
        source.SetMovingRight(false);
        source.SetMovingDown(false);
        source.SetMovingLeft(false);
        __result = false;
        service._holdOverrides++;
        service._last = $"hold-override distance={distance:0.0}px";
        return false;
    }

    private static bool TryReadLeader(object? pair, out bool leader)
    {
        leader = false;
        if (pair is null)
            return false;
        try
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            object? raw = pair.GetType().GetProperty("Leader", flags)?.GetValue(pair);
            if (raw is not bool parsed)
                return false;
            leader = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
