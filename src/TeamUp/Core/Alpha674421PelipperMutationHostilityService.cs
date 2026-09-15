using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.21 Pelipper Mutation hostility layer.
///
/// Live 6.7.44.20 proved Stardew's Monster pursuit flags are successfully armed on Pelipper
/// source/proxy pairs but Pelipper wild Pokemon do not consume those flags as hostile AI. Wild
/// Pelipper encounters are normally passive targets for companions, so Mutation needs a Team Up
/// owned hostility layer without replacing the real Pokemon actor or its capture identity.
///
/// Only Pelipper Mutation leaders and Mutation minions are handled here. The visible PokemonNpc is
/// moved by a private PathFindController owned by this service, while its genuine hidden Monster
/// combat proxy is kept synchronized to the same position. Contact damage is then routed through
/// Farmer.takeDamage with that real proxy as the damager. Natural Pelipper wild Pokemon are untouched.
/// </summary>
internal sealed class Alpha674421PelipperMutationHostilityService
{
    public const string HostilityMarker = "Ronvotri.TeamUp/PelipperMutationHostility";

    private const int RepathTicks = 12;
    private const int ContactCooldownTicks = 45;
    private const int MaxPathNodes = 160;

    private sealed class HostilityState
    {
        public NPC? Source { get; set; }
        public PathFindController? Controller { get; set; }
        public long NextRepathTick { get; set; }
        public long NextContactTick { get; set; }
        public Point LastTarget { get; set; } = new(-9999, -9999);
    }

    private readonly IMonitor _monitor;
    private readonly ConditionalWeakTable<Monster, HostilityState> _states = new();

    private long _ticks;
    private long _pairsSeen;
    private long _leaderPairs;
    private long _minionPairs;
    private long _pathsBuilt;
    private long _pathBuildFailures;
    private long _pathSteps;
    private long _proxySyncs;
    private long _contactDamageCalls;
    private long _identityMisses;
    private string _last = "reset";

    public Alpha674421PelipperMutationHostilityService(IMonitor monitor, IModHelper helper)
    {
        _monitor = monitor;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        _monitor.Log(
            "Team Up 6.7.44.21 Pelipper Mutation hostility enabled: real Pokemon source chases Farmer through Team Up pathing, real combat proxy stays synchronized, contact damage keeps proxy identity.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Pelipper Mutation hostility: teamup-pathing | ticks={_ticks} | pairs={_pairsSeen} | leaders={_leaderPairs} | minions={_minionPairs} | "
            + $"pathsBuilt={_pathsBuilt} | pathFailures={_pathBuildFailures} | pathSteps={_pathSteps} | proxySyncs={_proxySyncs} | "
            + $"contactDamageCalls={_contactDamageCalls} | identityMisses={_identityMisses} | last={_last}";

    public void ResetTelemetry()
    {
        _ticks = 0;
        _pairsSeen = 0;
        _leaderPairs = 0;
        _minionPairs = 0;
        _pathsBuilt = 0;
        _pathBuildFailures = 0;
        _pathSteps = 0;
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

            _pairsSeen++;
            if (leader)
                _leaderPairs++;
            if (minion)
                _minionPairs++;

            HostilityState state = _states.GetOrCreateValue(proxy);
            if (!ReferenceEquals(state.Source, source))
            {
                state.Source = source;
                state.Controller = null;
                state.NextRepathTick = 0;
                state.NextContactTick = 0;
            }

            source.modData[HostilityMarker] = "1";
            proxy.modData[HostilityMarker] = "1";
            proxy.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";

            UpdatePursuit(state, source, proxy, location, farmer, identity.DisplayName);
            SyncProxy(proxy, source);
            TryContactDamage(state, proxy, source, farmer, identity.DisplayName);
        }
    }

    private void UpdatePursuit(
        HostilityState state,
        NPC source,
        Monster proxy,
        GameLocation location,
        Farmer farmer,
        string species)
    {
        Point target = ResolveAdjacentTarget(location, source, farmer);
        bool needsRepath = state.Controller is null
            || Game1.ticks >= state.NextRepathTick
            || state.LastTarget != target;

        if (needsRepath)
        {
            state.Controller = BuildController(source, location, target);
            state.NextRepathTick = Game1.ticks + RepathTicks;
            state.LastTarget = target;
            if (state.Controller is null)
            {
                _pathBuildFailures++;
                _last = $"path-failed species={species} target={target.X},{target.Y}";
                return;
            }

            _pathsBuilt++;
        }

        PathFindController? controller = state.Controller;
        if (controller is null)
            return;

        try
        {
            bool done = controller.update(Game1.currentGameTime);
            _pathSteps++;
            if (done)
            {
                state.Controller = null;
                state.NextRepathTick = Game1.ticks;
            }
            _last = $"pursuit species={species} sourceTile={(int)source.Tile.X},{(int)source.Tile.Y} target={target.X},{target.Y} proxyDamage={Math.Max(1, proxy.DamageToFarmer)}";
        }
        catch (Exception ex)
        {
            state.Controller = null;
            state.NextRepathTick = Game1.ticks + RepathTicks;
            _pathBuildFailures++;
            _last = $"path-error species={species} {ex.GetType().Name}: {ex.Message}";
        }
    }

    private static PathFindController? BuildController(NPC source, GameLocation location, Point target)
    {
        try
        {
            var controller = new PathFindController(source, location, target, 2)
            {
                nonDestructivePathing = true
            };

            if (controller.pathToEndPoint is null || controller.pathToEndPoint.Count == 0)
                return null;
            if (controller.pathToEndPoint.Count > MaxPathNodes)
                return null;
            return controller;
        }
        catch
        {
            return null;
        }
    }

    private static Point ResolveAdjacentTarget(GameLocation location, NPC source, Farmer farmer)
    {
        Point farmerTile = new((int)farmer.Tile.X, (int)farmer.Tile.Y);
        Point[] candidates =
        {
            new(farmerTile.X - 1, farmerTile.Y),
            new(farmerTile.X + 1, farmerTile.Y),
            new(farmerTile.X, farmerTile.Y - 1),
            new(farmerTile.X, farmerTile.Y + 1)
        };

        Point sourceTile = new((int)source.Tile.X, (int)source.Tile.Y);
        return candidates
            .Where(candidate => IsPassableTile(location, candidate))
            .OrderBy(candidate => Math.Abs(candidate.X - sourceTile.X) + Math.Abs(candidate.Y - sourceTile.Y))
            .FirstOrDefault(farmerTile);
    }

    private static bool IsPassableTile(GameLocation location, Point tile)
    {
        if (tile.X < 0 || tile.Y < 0)
            return false;

        try
        {
            Vector2 vector = new(tile.X, tile.Y);
            return location.isTileOnMap(vector) && location.isTilePassable(vector);
        }
        catch
        {
            return false;
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

    private void TryContactDamage(HostilityState state, Monster proxy, NPC source, Farmer farmer, string species)
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
}
