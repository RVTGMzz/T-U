using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.23 Pelipper Mutation steering refinement.
///
/// 6.7.44.22 live testing proved source-native followers can pursue and attack, but the x2 Mutant
/// leader still looked jittery and needed near-contact range to damage Farmer. This service keeps the
/// source/proxy/capture architecture while making the leader independent from follower separation,
/// clearing provider movement/velocity before Team Up chase steps, applying short cardinal-direction
/// hysteresis, and granting an x2-sized melee reach. Followers retain the proven 6.7.44.22 pack
/// separation/sidestep behavior.
///
/// Only Pelipper Mutation leaders/minions are affected. Natural wild Pokemon remain untouched.
/// </summary>
internal sealed class Alpha674423PelipperMutantLeaderSmoothingService
{
    public const string SteeringMarker = "Ronvotri.TeamUp/PelipperMutationLeaderSmoothing";

    private const float MinionAttackRingRadius = 34f;
    private const float SeparationRadius = 76f;
    private const float LeaderClearanceRadius = 112f;
    private const float StopDistance = 10f;
    private const float LeaderHoldCenterDistance = 92f;
    private const float LeaderAttackCenterDistance = 112f;
    private const float BlockedMovementEpsilon = 0.35f;
    private const float LeaderAxisSwitchBias = 24f;
    private const int LeaderDirectionLockTicks = 8;
    private const int BlockedTicksBeforeSidestep = 10;
    private const int SidestepTicks = 22;
    private const int ContactCooldownTicks = 45;

    private sealed class SteeringState
    {
        public NPC? Source { get; set; }
        public Vector2 LastPosition { get; set; }
        public int BlockedTicks { get; set; }
        public long SidestepUntilTick { get; set; }
        public int SidestepSign { get; set; } = 1;
        public long NextContactTick { get; set; }
        public int LockedDirection { get; set; } = -1;
        public long DirectionLockUntilTick { get; set; }
    }

    private sealed class Pair
    {
        public Monster Proxy { get; init; } = null!;
        public NPC Source { get; init; } = null!;
        public PelipperWildEncounterIdentity Identity { get; init; } = null!;
        public bool Leader { get; init; }
        public bool Minion { get; init; }
    }

    private readonly IMonitor _monitor;
    private readonly ConditionalWeakTable<Monster, SteeringState> _states = new();

    private long _ticks;
    private long _pairsSeen;
    private long _leaderMoves;
    private long _minionMoves;
    private long _separationAdjustments;
    private long _leaderClearanceAdjustments;
    private long _sidesteps;
    private long _blockedFrames;
    private long _proxySyncs;
    private long _contactDamageCalls;
    private long _leaderReachHits;
    private long _leaderRangeHolds;
    private long _leaderHaltResets;
    private long _leaderDirectionChanges;
    private long _leaderDirectionLocks;
    private long _identityMisses;
    private string _last = "reset";

    public Alpha674423PelipperMutantLeaderSmoothingService(IMonitor monitor, IModHelper helper)
    {
        _monitor = monitor;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        _monitor.Log(
            "Team Up 6.7.44.23 Pelipper Mutation leader smoothing enabled: follower pack steering retained; Mutant leader gets clean velocity reset, direction hysteresis, independent right-of-way and extended x2 melee reach.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Pelipper Mutation steering: leader-smooth-reach | ticks={_ticks} | pairs={_pairsSeen} | leaderMoves={_leaderMoves} | minionMoves={_minionMoves} | "
            + $"separation={_separationAdjustments} | leaderClearance={_leaderClearanceAdjustments} | sidesteps={_sidesteps} | blockedFrames={_blockedFrames} | "
            + $"leaderReachHits={_leaderReachHits} | leaderRangeHolds={_leaderRangeHolds} | leaderHaltResets={_leaderHaltResets} | "
            + $"leaderDirectionChanges={_leaderDirectionChanges} | leaderDirectionLocks={_leaderDirectionLocks} | proxySyncs={_proxySyncs} | "
            + $"contactDamageCalls={_contactDamageCalls} | identityMisses={_identityMisses} | last={_last}";

    public void ResetTelemetry()
    {
        _ticks = 0;
        _pairsSeen = 0;
        _leaderMoves = 0;
        _minionMoves = 0;
        _separationAdjustments = 0;
        _leaderClearanceAdjustments = 0;
        _sidesteps = 0;
        _blockedFrames = 0;
        _proxySyncs = 0;
        _contactDamageCalls = 0;
        _leaderReachHits = 0;
        _leaderRangeHolds = 0;
        _leaderHaltResets = 0;
        _leaderDirectionChanges = 0;
        _leaderDirectionLocks = 0;
        _identityMisses = 0;
        _last = "reset";
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
            return;

        _ticks++;
        GameLocation location = Game1.currentLocation;
        Farmer farmer = Game1.player;
        List<Pair> pairs = ResolvePairs(location);
        if (pairs.Count == 0)
            return;

        Dictionary<string, List<Pair>> groups = pairs
            .GroupBy(pair => pair.Identity.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (List<Pair> group in groups.Values)
        {
            List<Pair> minions = group
                .Where(pair => pair.Minion)
                .OrderBy(pair => RuntimeHelpers.GetHashCode(pair.Proxy))
                .ToList();

            foreach (Pair pair in group)
            {
                int minionIndex = pair.Minion ? minions.FindIndex(candidate => ReferenceEquals(candidate.Proxy, pair.Proxy)) : -1;
                UpdatePair(pair, group, minionIndex, minions.Count, location, farmer);
            }
        }
    }

    private List<Pair> ResolvePairs(GameLocation location)
    {
        List<Pair> result = new();
        foreach (Monster proxy in location.characters.OfType<Monster>().ToList())
        {
            if (proxy.Health <= 0)
                continue;

            bool leader = MonsterMutationService.IsMutant(proxy);
            bool minion = MonsterMutationService.IsMutationMinion(proxy);
            if ((!leader && !minion) || !PelipperTownCompatibilityService.IsWildCombatActor(proxy))
                continue;

            if (!PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity))
            {
                _identityMisses++;
                _last = $"identity-miss proxy={proxy.Name}";
                continue;
            }

            NPC source = identity.SourceActor;
            if (ReferenceEquals(source, proxy)
                || !ReferenceEquals(source.currentLocation, location)
                || !location.characters.Contains(source))
            {
                _identityMisses++;
                _last = $"source-unavailable species={identity.DisplayName}";
                continue;
            }

            result.Add(new Pair
            {
                Proxy = proxy,
                Source = source,
                Identity = identity,
                Leader = leader,
                Minion = minion
            });
            _pairsSeen++;
        }

        return result;
    }

    private void UpdatePair(
        Pair pair,
        List<Pair> group,
        int minionIndex,
        int minionCount,
        GameLocation location,
        Farmer farmer)
    {
        Monster proxy = pair.Proxy;
        NPC source = pair.Source;
        SteeringState state = _states.GetOrCreateValue(proxy);
        if (!ReferenceEquals(state.Source, source))
        {
            state.Source = source;
            state.LastPosition = source.Position;
            state.BlockedTicks = 0;
            state.SidestepUntilTick = 0;
            state.SidestepSign = (RuntimeHelpers.GetHashCode(proxy) & 1) == 0 ? 1 : -1;
            state.NextContactTick = 0;
            state.LockedDirection = -1;
            state.DirectionLockUntilTick = 0;
        }

        source.modData[SteeringMarker] = "1";
        proxy.modData[SteeringMarker] = "1";
        proxy.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
        proxy.collidesWithOtherCharacters.Value = false;

        Vector2 farmerCenter = Center(farmer.GetBoundingBox());
        Vector2 sourceCenter = Center(source.GetBoundingBox());
        float farmerDistance = Vector2.Distance(sourceCenter, farmerCenter);
        Vector2 target = ResolveAttackTarget(pair, farmerCenter, minionIndex, minionCount);
        Vector2 steering = target - sourceCenter;

        // Followers keep flock separation. The x2 leader never yields to followers; forcing separation on
        // the leader was one source of axis-flip jitter in the 6.7.44.22 live test.
        if (!pair.Leader)
            ApplyFollowerSeparation(pair, group, sourceCenter, ref steering);

        if (pair.Minion && Game1.ticks < state.SidestepUntilTick && farmerDistance > 72f)
        {
            Vector2 towardFarmer = farmerCenter - sourceCenter;
            if (towardFarmer.LengthSquared() > 0.001f)
            {
                towardFarmer.Normalize();
                steering = new Vector2(-towardFarmer.Y, towardFarmer.X) * state.SidestepSign;
            }
        }

        bool holdLeaderRange = pair.Leader && farmerDistance <= LeaderHoldCenterDistance;
        bool moved;
        if (holdLeaderRange)
        {
            StopSource(source);
            moved = false;
            _leaderRangeHolds++;
        }
        else
        {
            moved = MoveSource(source, steering, pair.Leader, state, location);
        }

        float movedDistance = Vector2.Distance(state.LastPosition, source.Position);
        bool shouldBeMoving = !holdLeaderRange
            && steering.LengthSquared() > StopDistance * StopDistance
            && farmerDistance > 64f;

        if (pair.Minion && shouldBeMoving && moved && movedDistance <= BlockedMovementEpsilon)
        {
            state.BlockedTicks++;
            _blockedFrames++;
            if (state.BlockedTicks >= BlockedTicksBeforeSidestep)
            {
                state.BlockedTicks = 0;
                state.SidestepUntilTick = Game1.ticks + SidestepTicks;
                state.SidestepSign *= -1;
                _sidesteps++;
            }
        }
        else if (movedDistance > BlockedMovementEpsilon || !shouldBeMoving || pair.Leader)
        {
            state.BlockedTicks = 0;
        }

        state.LastPosition = source.Position;
        SyncProxy(proxy, source);
        TryDamage(state, pair, farmerDistance, farmer);

        if (moved)
        {
            if (pair.Leader)
                _leaderMoves++;
            if (pair.Minion)
                _minionMoves++;
        }

        _last = $"steering species={pair.Identity.DisplayName} leader={pair.Leader} minion={pair.Minion} farmerDist={farmerDistance:0.0} hold={holdLeaderRange} dir={state.LockedDirection}";
    }

    private static Vector2 ResolveAttackTarget(Pair pair, Vector2 farmerCenter, int minionIndex, int minionCount)
    {
        if (pair.Leader || minionIndex < 0 || minionCount <= 0)
            return farmerCenter;

        float angle = MathHelper.TwoPi * minionIndex / minionCount;
        return farmerCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * MinionAttackRingRadius;
    }

    private void ApplyFollowerSeparation(Pair pair, List<Pair> group, Vector2 sourceCenter, ref Vector2 steering)
    {
        foreach (Pair other in group)
        {
            if (ReferenceEquals(other.Proxy, pair.Proxy))
                continue;

            Vector2 delta = sourceCenter - Center(other.Source.GetBoundingBox());
            float distanceSquared = delta.LengthSquared();
            if (distanceSquared < 0.001f)
            {
                delta = new Vector2((RuntimeHelpers.GetHashCode(pair.Proxy) & 1) == 0 ? 1f : -1f, 0f);
                distanceSquared = 1f;
            }

            float radius = other.Leader ? LeaderClearanceRadius : SeparationRadius;
            float weight = other.Leader ? 2.4f : 1.0f;
            if (distanceSquared >= radius * radius)
                continue;

            float distance = MathF.Sqrt(distanceSquared);
            delta /= Math.Max(1f, distance);
            float strength = (radius - distance) / radius;
            steering += delta * (strength * radius * weight);
            _separationAdjustments++;
            if (other.Leader)
                _leaderClearanceAdjustments++;
        }
    }

    private bool MoveSource(NPC source, Vector2 steering, bool leader, SteeringState state, GameLocation location)
    {
        if (steering.LengthSquared() <= StopDistance * StopDistance)
        {
            StopSource(source);
            return false;
        }

        int originalSpeed = source.Speed;
        int attackSpeed = leader
            ? Math.Clamp(Math.Max(originalSpeed, 3), 3, 4)
            : Math.Clamp(Math.Max(originalSpeed, 2), 2, 4);

        if (leader)
        {
            // Clear Pelipper's passive movement state/velocity before applying the Team Up chase step.
            source.Halt();
            _leaderHaltResets++;
        }
        else
        {
            StopDirectionalFlags(source);
        }

        int direction = ResolveDirection(steering, leader, state);
        SetDirection(source, direction);

        try
        {
            source.Speed = attackSpeed;
            source.MovePosition(Game1.currentGameTime, Game1.viewport, location);
            return true;
        }
        finally
        {
            source.Speed = originalSpeed;
        }
    }

    private int ResolveDirection(Vector2 steering, bool leader, SteeringState state)
    {
        int desired = CardinalFromVector(steering);
        if (!leader)
            return desired;

        if (state.LockedDirection < 0)
        {
            state.LockedDirection = desired;
            state.DirectionLockUntilTick = Game1.ticks + LeaderDirectionLockTicks;
            _leaderDirectionLocks++;
            return desired;
        }

        if (desired == state.LockedDirection)
        {
            state.DirectionLockUntilTick = Math.Max(state.DirectionLockUntilTick, Game1.ticks + 2);
            return desired;
        }

        bool reverse = IsOpposite(state.LockedDirection, desired);
        float x = Math.Abs(steering.X);
        float y = Math.Abs(steering.Y);
        bool dominantAxisChanged = DirectionIsHorizontal(desired)
            ? x >= y + LeaderAxisSwitchBias
            : y >= x + LeaderAxisSwitchBias;

        if (!reverse && Game1.ticks < state.DirectionLockUntilTick && !dominantAxisChanged)
            return state.LockedDirection;

        state.LockedDirection = desired;
        state.DirectionLockUntilTick = Game1.ticks + LeaderDirectionLockTicks;
        _leaderDirectionChanges++;
        _leaderDirectionLocks++;
        return desired;
    }

    private void TryDamage(SteeringState state, Pair pair, float farmerDistance, Farmer farmer)
    {
        if (Game1.ticks < state.NextContactTick)
            return;

        Monster proxy = pair.Proxy;
        NPC source = pair.Source;
        bool canHit;
        if (pair.Leader)
        {
            canHit = farmerDistance <= LeaderAttackCenterDistance;
            if (canHit)
                _leaderReachHits++;
        }
        else
        {
            canHit = source.GetBoundingBox().Intersects(farmer.GetBoundingBox())
                || proxy.GetBoundingBox().Intersects(farmer.GetBoundingBox());
        }

        if (!canHit)
            return;

        int damage = Math.Max(1, proxy.DamageToFarmer);
        if (pair.Minion)
            damage = Math.Max(damage, Alpha674420MutationAggroService.PelipperOrdinaryDamageFloor);
        if (pair.Leader)
        {
            damage = Math.Max(damage, Alpha674420MutationAggroService.PelipperMutantDamageFloor);
            if (proxy.modData.TryGetValue(MonsterMutationService.MutationIntendedDamageMarker, out string? intendedRaw)
                && int.TryParse(intendedRaw, out int intendedDamage))
            {
                damage = Math.Max(damage, intendedDamage);
            }
        }
        try
        {
            farmer.takeDamage(damage, overrideParry: false, proxy);
            state.NextContactTick = Game1.ticks + ContactCooldownTicks;
            _contactDamageCalls++;
            _last = $"damage species={pair.Identity.DisplayName} leader={pair.Leader} damage={damage} dist={farmerDistance:0.0} cooldown={ContactCooldownTicks}";
        }
        catch (Exception ex)
        {
            state.NextContactTick = Game1.ticks + ContactCooldownTicks;
            _last = $"damage-error species={pair.Identity.DisplayName} {ex.GetType().Name}: {ex.Message}";
        }
    }

    private void SyncProxy(Monster proxy, NPC source)
    {
        if (Vector2.DistanceSquared(proxy.Position, source.Position) <= 1f)
            return;

        proxy.Position = source.Position;
        proxy.FacingDirection = source.FacingDirection;
        _proxySyncs++;
    }

    private static void StopSource(NPC source)
    {
        source.Halt();
        StopDirectionalFlags(source);
    }

    private static void StopDirectionalFlags(NPC source)
    {
        source.SetMovingUp(false);
        source.SetMovingRight(false);
        source.SetMovingDown(false);
        source.SetMovingLeft(false);
    }

    private static void SetDirection(NPC source, int direction)
    {
        switch (direction)
        {
            case 0:
                source.SetMovingUp(true);
                break;
            case 1:
                source.SetMovingRight(true);
                break;
            case 2:
                source.SetMovingDown(true);
                break;
            default:
                source.SetMovingLeft(true);
                break;
        }
    }

    private static int CardinalFromVector(Vector2 value)
    {
        if (Math.Abs(value.X) >= Math.Abs(value.Y))
            return value.X >= 0f ? 1 : 3;
        return value.Y >= 0f ? 2 : 0;
    }

    private static bool DirectionIsHorizontal(int direction)
        => direction is 1 or 3;

    private static bool IsOpposite(int a, int b)
        => (a == 0 && b == 2)
            || (a == 2 && b == 0)
            || (a == 1 && b == 3)
            || (a == 3 && b == 1);

    private static Vector2 Center(Microsoft.Xna.Framework.Rectangle rectangle)
        => new(rectangle.Center.X, rectangle.Center.Y);
}
