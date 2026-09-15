using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.22 Pelipper Mutation pack steering.
///
/// 6.7.44.21 proved Team Up-owned hostility works, but tile PathFindController movement made large
/// x2 Mutant leaders easy to jam behind their followers and made passive Pelipper species move like
/// grid NPCs. This service keeps the same real Pokemon source/proxy/capture identity but replaces the
/// tile-path chase with low-level Stardew NPC movement plus flock-style separation and a short
/// sidestep recovery when an actor is blocked.
///
/// Only Pelipper Mutation leaders/minions are affected. Natural wild Pokemon remain untouched.
/// </summary>
internal sealed class Alpha674422PelipperMutationSteeringService
{
    public const string SteeringMarker = "Ronvotri.TeamUp/PelipperMutationPackSteering";

    private const float MinionAttackRingRadius = 34f;
    private const float SeparationRadius = 76f;
    private const float LeaderClearanceRadius = 104f;
    private const float StopDistance = 10f;
    private const float BlockedMovementEpsilon = 0.35f;
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
    private long _identityMisses;
    private string _last = "reset";

    public Alpha674422PelipperMutationSteeringService(IMonitor monitor, IModHelper helper)
    {
        _monitor = monitor;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        _monitor.Log(
            "Team Up 6.7.44.22 Pelipper Mutation pack steering enabled: pixel/cardinal NPC movement, pack separation, leader clearance, blocked sidestep recovery and proxy contact damage.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Pelipper Mutation steering: pack-steering | ticks={_ticks} | pairs={_pairsSeen} | leaderMoves={_leaderMoves} | minionMoves={_minionMoves} | "
            + $"separation={_separationAdjustments} | leaderClearance={_leaderClearanceAdjustments} | sidesteps={_sidesteps} | blockedFrames={_blockedFrames} | "
            + $"proxySyncs={_proxySyncs} | contactDamageCalls={_contactDamageCalls} | identityMisses={_identityMisses} | last={_last}";

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
                int minionCount = minions.Count;
                UpdatePair(pair, group, minionIndex, minionCount, location, farmer);
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
        }

        source.modData[SteeringMarker] = "1";
        proxy.modData[SteeringMarker] = "1";
        proxy.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";

        // The hidden combat proxy must never become a physical obstacle for its own visible Pokemon.
        proxy.collidesWithOtherCharacters.Value = false;

        Vector2 farmerCenter = Center(farmer.GetBoundingBox());
        Vector2 sourceCenter = Center(source.GetBoundingBox());
        Vector2 target = ResolveAttackTarget(pair, farmerCenter, minionIndex, minionCount);
        Vector2 steering = target - sourceCenter;

        ApplyPackSeparation(pair, group, sourceCenter, ref steering);

        float farmerDistance = Vector2.Distance(sourceCenter, farmerCenter);
        if (Game1.ticks < state.SidestepUntilTick && farmerDistance > 72f)
        {
            Vector2 towardFarmer = farmerCenter - sourceCenter;
            if (towardFarmer.LengthSquared() > 0.001f)
            {
                towardFarmer.Normalize();
                steering = new Vector2(-towardFarmer.Y, towardFarmer.X) * state.SidestepSign;
            }
        }

        bool moved = MoveSource(source, steering, pair.Leader, location);
        float movedDistance = Vector2.Distance(state.LastPosition, source.Position);
        bool shouldBeMoving = steering.LengthSquared() > StopDistance * StopDistance && farmerDistance > 64f;

        if (shouldBeMoving && moved && movedDistance <= BlockedMovementEpsilon)
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
        else if (movedDistance > BlockedMovementEpsilon || !shouldBeMoving)
        {
            state.BlockedTicks = 0;
        }

        state.LastPosition = source.Position;
        SyncProxy(proxy, source);
        TryContactDamage(state, proxy, source, farmer, pair.Identity.DisplayName);

        if (moved)
        {
            if (pair.Leader)
                _leaderMoves++;
            if (pair.Minion)
                _minionMoves++;
        }

        _last = $"steering species={pair.Identity.DisplayName} leader={pair.Leader} minion={pair.Minion} farmerDist={farmerDistance:0.0} blocked={state.BlockedTicks} sidestep={(Game1.ticks < state.SidestepUntilTick)}";
    }

    private static Vector2 ResolveAttackTarget(Pair pair, Vector2 farmerCenter, int minionIndex, int minionCount)
    {
        if (pair.Leader || minionIndex < 0 || minionCount <= 0)
            return farmerCenter;

        float angle = MathHelper.TwoPi * minionIndex / minionCount;
        return farmerCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * MinionAttackRingRadius;
    }

    private void ApplyPackSeparation(Pair pair, List<Pair> group, Vector2 sourceCenter, ref Vector2 steering)
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

            float radius = SeparationRadius;
            float weight = 1.0f;
            if (pair.Minion && other.Leader)
            {
                radius = LeaderClearanceRadius;
                weight = 2.2f;
            }
            else if (pair.Leader && other.Minion)
            {
                // The leader gets right of way. It only nudges away slightly instead of yielding.
                weight = 0.35f;
            }

            if (distanceSquared >= radius * radius)
                continue;

            float distance = MathF.Sqrt(distanceSquared);
            delta /= Math.Max(1f, distance);
            float strength = (radius - distance) / radius;
            steering += delta * (strength * radius * weight);
            _separationAdjustments++;
            if (pair.Minion && other.Leader)
                _leaderClearanceAdjustments++;
        }
    }

    private static bool MoveSource(NPC source, Vector2 steering, bool leader, GameLocation location)
    {
        if (steering.LengthSquared() <= StopDistance * StopDistance)
        {
            source.SetMovingUp(false);
            source.SetMovingRight(false);
            source.SetMovingDown(false);
            source.SetMovingLeft(false);
            return false;
        }

        int originalSpeed = source.Speed;
        int attackSpeed = leader ? Math.Clamp(Math.Max(originalSpeed, 3), 3, 5) : Math.Clamp(Math.Max(originalSpeed, 2), 2, 4);

        source.SetMovingUp(false);
        source.SetMovingRight(false);
        source.SetMovingDown(false);
        source.SetMovingLeft(false);

        if (Math.Abs(steering.X) >= Math.Abs(steering.Y))
        {
            if (steering.X >= 0f)
                source.SetMovingRight(true);
            else
                source.SetMovingLeft(true);
        }
        else
        {
            if (steering.Y >= 0f)
                source.SetMovingDown(true);
            else
                source.SetMovingUp(true);
        }

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

    private void SyncProxy(Monster proxy, NPC source)
    {
        if (Vector2.DistanceSquared(proxy.Position, source.Position) <= 1f)
            return;

        proxy.Position = source.Position;
        proxy.FacingDirection = source.FacingDirection;
        _proxySyncs++;
    }

    private void TryContactDamage(SteeringState state, Monster proxy, NPC source, Farmer farmer, string species)
    {
        if (Game1.ticks < state.NextContactTick)
            return;

        bool touching = source.GetBoundingBox().Intersects(farmer.GetBoundingBox())
            || proxy.GetBoundingBox().Intersects(farmer.GetBoundingBox());
        if (!touching)
            return;

        int damage = Math.Max(1, proxy.DamageToFarmer);
        try
        {
            farmer.takeDamage(damage, overrideParry: false, proxy);
            state.NextContactTick = Game1.ticks + ContactCooldownTicks;
            _contactDamageCalls++;
            _last = $"contact-damage species={species} damage={damage} cooldown={ContactCooldownTicks}";
        }
        catch (Exception ex)
        {
            state.NextContactTick = Game1.ticks + ContactCooldownTicks;
            _last = $"contact-error species={species} {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static Vector2 Center(Microsoft.Xna.Framework.Rectangle rectangle)
        => new(rectangle.Center.X, rectangle.Center.Y);
}
