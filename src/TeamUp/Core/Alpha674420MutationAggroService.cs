using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.20 Mutation hostility bridge.
///
/// Pelipper wild encounters use a visible Pokemon source plus a very generic Monster combat proxy.
/// The proxy is targetable, but targetability alone does not make it actively pursue Farmers.
/// Mutation leaders and their ordinary source-equivalent followers must be real hostile encounters,
/// so Team Up re-arms Stardew's own pursuit flags instead of teleporting or replacing source AI.
///
/// The refresh is deliberately lightweight: scan the current location at the existing 20Hz cadence,
/// but only rewrite a given actor's aggro flags once per second. This also recovers if an optional
/// provider clears its movement flags after spawn while leaving ordinary non-Mutation monsters alone.
/// </summary>
internal sealed class Alpha674420MutationAggroService
{
    public const string AggroMarker = "Ronvotri.TeamUp/MutationAggroArmed";

    private const int ScanPulseTicks = 3;
    private const int RefreshTicks = 15;
    private const int BaseAggroRadiusTiles = 6;
    public const int MutationAggroRadiusTiles = BaseAggroRadiusTiles * 3;
    public const int PelipperOrdinaryDamageFloor = 4;
    public const int PelipperMutantDamageFloor = 8;
    private const string PelipperEngagedKey = "Griff.PelipperTown/WildCombatEngaged";
    private const string PelipperPassiveKey = "Griff.PelipperTown/PassiveUntilAttacked";

    private sealed class AggroStamp
    {
        public long NextRefreshTick { get; set; }
    }

    private readonly IMonitor _monitor;
    private readonly ConditionalWeakTable<Monster, AggroStamp> _refresh = new();

    private long _scanPulses;
    private long _leaderArms;
    private long _minionArms;
    private long _pelipperProxyArms;
    private long _pelipperSourceArms;
    private long _damageFloors;
    private long _identityMisses;
    private string _last = "reset";

    public Alpha674420MutationAggroService(IMonitor monitor, IModHelper helper)
    {
        _monitor = monitor;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        _monitor.Log(
            "Team Up 6.7.44.20 Mutation aggro enabled: leaders + source-equivalent followers use native Stardew pursuit flags; Pelipper source/proxy pairs are armed together.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutation aggro: x3-arena={MutationAggroRadiusTiles}tiles | scans={_scanPulses} | leaderArms={_leaderArms} | minionArms={_minionArms} | "
            + $"pelipperProxyArms={_pelipperProxyArms} | pelipperSourceArms={_pelipperSourceArms} | "
            + $"damageFloors={_damageFloors} | identityMisses={_identityMisses} | last={_last}";

    public void ResetTelemetry()
    {
        _scanPulses = 0;
        _leaderArms = 0;
        _minionArms = 0;
        _pelipperProxyArms = 0;
        _pelipperSourceArms = 0;
        _damageFloors = 0;
        _identityMisses = 0;
        _last = "reset";
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
            return;
        if (Game1.ticks % ScanPulseTicks != 0)
            return;

        _scanPulses++;
        foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>())
        {
            bool leader = MonsterMutationService.IsMutant(monster);
            bool minion = MonsterMutationService.IsMutationMinion(monster);
            if ((!leader && !minion) || monster.Health <= 0)
                continue;

            Vector2 monsterCenter = new(monster.GetBoundingBox().Center.X, monster.GetBoundingBox().Center.Y);
            Vector2 farmerCenter = new(Game1.player.GetBoundingBox().Center.X, Game1.player.GetBoundingBox().Center.Y);
            float aggroRadiusPixels = MutationAggroRadiusTiles * 64f;
            if (Vector2.DistanceSquared(monsterCenter, farmerCenter) > aggroRadiusPixels * aggroRadiusPixels)
                continue;

            AggroStamp stamp = _refresh.GetOrCreateValue(monster);
            if (stamp.NextRefreshTick > Game1.ticks)
                continue;

            ArmMonster(monster, leader, minion);
            stamp.NextRefreshTick = Game1.ticks + RefreshTicks;
        }
    }

    private void ArmMonster(Monster monster, bool leader, bool minion)
    {
        // These are Stardew's native pursuit controls. Do not replace the monster's controller or
        // manually move its coordinates; custom/native movement remains authoritative.
        monster.focusedOnFarmers = true;
        monster.moveTowardPlayer(MutationAggroRadiusTiles);
        monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
        monster.modData[AggroMarker] = "1";

        int desiredDamage = minion ? PelipperOrdinaryDamageFloor : 1;
        if (leader)
        {
            desiredDamage = PelipperMutantDamageFloor;
            if (monster.modData.TryGetValue(MonsterMutationService.MutationIntendedDamageMarker, out string? intendedRaw)
                && int.TryParse(intendedRaw, out int intended))
            {
                desiredDamage = Math.Max(desiredDamage, intended);
            }
        }

        if (monster.DamageToFarmer < desiredDamage)
        {
            monster.DamageToFarmer = desiredDamage;
            _damageFloors++;
        }

        if (leader)
            _leaderArms++;
        if (minion)
            _minionArms++;

        bool pelipper = PelipperTownCompatibilityService.IsWildCombatActor(monster);
        if (pelipper)
        {
            monster.modData[PelipperEngagedKey] = "true";
            monster.modData[PelipperPassiveKey] = "false";
        }
        string species = string.IsNullOrWhiteSpace(monster.displayName) ? monster.Name : monster.displayName;
        if (!pelipper)
        {
            _last = $"armed type={monster.GetType().Name} name={species} leader={leader} minion={minion} pursuit=native";
            return;
        }

        _pelipperProxyArms++;
        if (!PelipperWildEncounterIdentityService.TryResolve(monster, out PelipperWildEncounterIdentity identity))
        {
            _identityMisses++;
            _last = $"armed Pelipper proxy={species} leader={leader} minion={minion} source=unresolved";
            return;
        }

        // The visible Pokemon is a separate NPC. Arm its native walk-toward-player flag too so
        // visuals remain aggressive while Pelipper retains render/capture/source ownership.
        NPC source = identity.SourceActor;
        source.moveTowardPlayer(MutationAggroRadiusTiles);
        source.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
        source.modData[PelipperEngagedKey] = "true";
        source.modData[PelipperPassiveKey] = "false";
        source.modData[AggroMarker] = "1";
        if (source is Monster sourceMonster)
        {
            sourceMonster.focusedOnFarmers = true;
            if (sourceMonster.DamageToFarmer < desiredDamage)
            {
                sourceMonster.DamageToFarmer = desiredDamage;
                _damageFloors++;
            }
        }

        _pelipperSourceArms++;
        _last = $"armed Pelipper species={identity.DisplayName} leader={leader} minion={minion} proxy+source=true arena={MutationAggroRadiusTiles}tiles damage={desiredDamage} engaged=true";
    }
}
