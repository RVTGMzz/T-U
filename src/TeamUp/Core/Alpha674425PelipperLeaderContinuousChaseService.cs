using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.25 Mutant leader locomotion refinement.
///
/// Live 6.7.44.23 telemetry showed leaderHaltResets tracking leaderMoves almost one-for-one,
/// which means the x2 Pelipper Mutant was being Halt()'d before every chase step. That creates a
/// visible stop/start cadence even when direction hysteresis is working. This overlay intercepts only
/// Mutant-leader MoveSource calls outside the 6.7.44.24 128px hold band and performs continuous
/// directional movement without per-step Halt(). Followers remain on the proven 6.7.44.22/23 pack
/// steering path, and 6.7.44.24 still owns close-range hold/reach/damage/capture finalization.
/// </summary>
internal sealed class Alpha674425PelipperLeaderContinuousChaseService
{
    private const float HoldDistance = 128f;
    private const float AxisSwitchBias = 28f;
    private const float MovementEpsilon = 0.35f;
    private const int DirectionLockTicks = 10;
    private const int MinLeaderSpeed = 3;
    private const int MaxLeaderSpeed = 4;

    private sealed class ChaseState
    {
        public bool Active { get; set; }
        public int Direction { get; set; } = -1;
        public long DirectionLockUntilTick { get; set; }
        public Vector2 LastIssuedPosition { get; set; }
        public int OriginalSpeed { get; set; }
    }

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly ConditionalWeakTable<NPC, ChaseState> _states = new();

    private long _calls;
    private long _continuousMoves;
    private long _chaseStarts;
    private long _directionChanges;
    private long _directionLocks;
    private long _directionRefreshes;
    private long _legacyHaltBypassed;
    private long _blockedMoves;
    private long _providerPullbacks;
    private string _last = "reset";

    public Alpha674425PelipperLeaderContinuousChaseService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony(uniqueId + ".Alpha674425PelipperLeaderContinuousChase");
        ActiveInstance = this;
        Apply();
        _monitor.Log(
            "Team Up 6.7.44.25 Mutant leader continuous chase enabled: per-step Halt() bypassed outside the 128px hold band; followers unchanged.",
            LogLevel.Info);
    }

    private static Alpha674425PelipperLeaderContinuousChaseService? ActiveInstance { get; set; }

    public string Describe()
        => $"Pelipper Mutation leader motion: continuous-chase | calls={_calls} | moves={_continuousMoves} | chaseStarts={_chaseStarts} | "
            + $"directionChanges={_directionChanges} | directionLocks={_directionLocks} | directionRefreshes={_directionRefreshes} | "
            + $"legacyHaltBypassed={_legacyHaltBypassed} | blockedMoves={_blockedMoves} | providerPullbacks={_providerPullbacks} | last={_last}";

    public void ResetTelemetry()
    {
        _calls = 0;
        _continuousMoves = 0;
        _chaseStarts = 0;
        _directionChanges = 0;
        _directionLocks = 0;
        _directionRefreshes = 0;
        _legacyHaltBypassed = 0;
        _blockedMoves = 0;
        _providerPullbacks = 0;
        _last = "reset";
    }

    private void Apply()
    {
        var target = AccessTools.Method(typeof(Alpha674423PelipperMutantLeaderSmoothingService), "MoveSource");
        if (target is null)
        {
            _last = "patch-target-missing";
            _monitor.Log("6.7.44.25 continuous chase could not find 6.7.44.23 MoveSource().", LogLevel.Warn);
            return;
        }

        HarmonyMethod prefix = new(typeof(Alpha674425PelipperLeaderContinuousChaseService), nameof(BeforeMoveSource))
        {
            priority = Priority.First
        };
        _harmony.Patch(target, prefix: prefix);
    }

    private static bool BeforeMoveSource(
        NPC source,
        Vector2 steering,
        bool leader,
        GameLocation location,
        ref bool __result)
    {
        Alpha674425PelipperLeaderContinuousChaseService? service = ActiveInstance;
        if (service is null || !leader)
            return true;

        service._calls++;

        float distance = steering.Length();
        if (distance <= HoldDistance)
        {
            ChaseState holdState = service._states.GetOrCreateValue(source);
            holdState.Active = false;
            service._last = $"handoff-to-44.24-hold distance={distance:0.0}px";
            return true;
        }

        __result = service.MoveLeaderContinuously(source, steering, location);
        service._legacyHaltBypassed++;
        return false;
    }

    private bool MoveLeaderContinuously(NPC source, Vector2 steering, GameLocation location)
    {
        ChaseState state = _states.GetOrCreateValue(source);
        int desired = ResolveDesiredDirection(steering, state);

        if (!state.Active)
        {
            state.Active = true;
            state.OriginalSpeed = source.Speed;
            state.LastIssuedPosition = source.Position;
            state.Direction = desired;
            state.DirectionLockUntilTick = Game1.ticks + DirectionLockTicks;
            ApplyDirectionalState(source, desired);
            _chaseStarts++;
            _directionLocks++;
        }
        else if (desired != state.Direction)
        {
            state.Direction = desired;
            state.DirectionLockUntilTick = Game1.ticks + DirectionLockTicks;
            ApplyDirectionalState(source, desired);
            _directionChanges++;
            _directionLocks++;
        }
        else
        {
            // Reassert the same direction without toggling it off first. This keeps Pelipper from
            // leaving a conflicting passive direction set while avoiding animation restart jitter.
            ApplyDirectionalState(source, desired);
            _directionRefreshes++;
        }

        Vector2 before = source.Position;
        Vector2 drift = before - state.LastIssuedPosition;
        if (state.Active && Vector2.Dot(drift, DirectionVector(state.Direction)) < -1.25f)
            _providerPullbacks++;

        source.Speed = Math.Clamp(Math.Max(source.Speed, MinLeaderSpeed), MinLeaderSpeed, MaxLeaderSpeed);
        source.MovePosition(Game1.currentGameTime, Game1.viewport, location);

        float moved = Vector2.Distance(before, source.Position);
        if (moved <= MovementEpsilon)
            _blockedMoves++;
        else
            _continuousMoves++;

        state.LastIssuedPosition = source.Position;
        _last = $"continuous source={source.Name} distance={steering.Length():0.0}px dir={state.Direction} moved={moved:0.00}px speed={source.Speed}";
        return true;
    }

    private int ResolveDesiredDirection(Vector2 steering, ChaseState state)
    {
        int desired = CardinalFromVector(steering);
        if (!state.Active || state.Direction < 0 || desired == state.Direction)
            return desired;

        bool reverse = IsOpposite(state.Direction, desired);
        float x = Math.Abs(steering.X);
        float y = Math.Abs(steering.Y);
        bool dominantAxisChanged = DirectionIsHorizontal(desired)
            ? x >= y + AxisSwitchBias
            : y >= x + AxisSwitchBias;

        if (!reverse && Game1.ticks < state.DirectionLockUntilTick && !dominantAxisChanged)
            return state.Direction;

        return desired;
    }

    private static void ApplyDirectionalState(NPC source, int direction)
    {
        source.SetMovingUp(direction == 0);
        source.SetMovingRight(direction == 1);
        source.SetMovingDown(direction == 2);
        source.SetMovingLeft(direction == 3);
    }

    private static int CardinalFromVector(Vector2 value)
    {
        if (Math.Abs(value.X) >= Math.Abs(value.Y))
            return value.X >= 0f ? 1 : 3;
        return value.Y >= 0f ? 2 : 0;
    }

    private static Vector2 DirectionVector(int direction)
        => direction switch
        {
            0 => new Vector2(0f, -1f),
            1 => new Vector2(1f, 0f),
            2 => new Vector2(0f, 1f),
            _ => new Vector2(-1f, 0f)
        };

    private static bool DirectionIsHorizontal(int direction)
        => direction is 1 or 3;

    private static bool IsOpposite(int a, int b)
        => (a == 0 && b == 2)
            || (a == 2 && b == 0)
            || (a == 1 && b == 3)
            || (a == 3 && b == 1);
}
