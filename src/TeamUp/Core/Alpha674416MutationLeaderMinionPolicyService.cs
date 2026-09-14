using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.16 design lock:
/// - the transformed monster is the single Mutant leader;
/// - its 2-4 followers are ordinary hostile monsters, never Mutants;
/// - only the leader is eligible for the global x3 Mutant reward;
/// - minions remain valid Team Up combat targets and retain their normal AI/stats.
///
/// Alpha 6.7.44.17 performance lock: Pelipper leaders use Team Up lightweight temporary minions
/// instead of asking Pelipper Town to create complete source/proxy wild encounters. Non-Pelipper
/// leaders still prefer same-runtime-type minions whenever safe.
/// </summary>
internal sealed class Alpha674416MutationLeaderMinionPolicyService
{
    public const string MutantLeaderMarker = "Ronvotri.TeamUp/MutantLeader";
    public const string NormalHostileMinionMarker = "Ronvotri.TeamUp/NormalHostileMutationMinion";

    private sealed class WaveSnapshot
    {
        public GameLocation? Location { get; init; }
        public List<Monster> ExistingMinions { get; init; } = new();
    }

    private static Alpha674416MutationLeaderMinionPolicyService? Active;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;

    private long _leadersMarked;
    private long _minionsNormalized;
    private long _hostileReady;
    private long _mutationGuards;
    private long _lootMarkersStripped;
    private long _sameTypeSeen;
    private long _fallbackSeen;
    private string _last = "reset";

    public Alpha674416MutationLeaderMinionPolicyService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha674416MutationLeaderMinionPolicy");
        Active = this;

        MethodInfo? tryMutate = AccessTools.Method(typeof(MonsterMutationService), "TryMutate");
        if (tryMutate is not null)
        {
            _harmony.Patch(
                tryMutate,
                postfix: new HarmonyMethod(typeof(Alpha674416MutationLeaderMinionPolicyService), nameof(TryMutatePostfix))
                {
                    priority = Priority.Last
                });
        }
        else
        {
            _monitor.Log("6.7.44.16 leader policy unavailable: TryMutate not found.", LogLevel.Warn);
        }

        MethodInfo? spawnWave = AccessTools.Method(typeof(MonsterMutationService), "SpawnMinionWave");
        if (spawnWave is not null)
        {
            _harmony.Patch(
                spawnWave,
                prefix: new HarmonyMethod(typeof(Alpha674416MutationLeaderMinionPolicyService), nameof(SpawnWavePrefix))
                {
                    priority = Priority.First
                },
                postfix: new HarmonyMethod(typeof(Alpha674416MutationLeaderMinionPolicyService), nameof(SpawnWavePostfix))
                {
                    priority = Priority.Last
                });
        }
        else
        {
            _monitor.Log("6.7.44.16 minion policy unavailable: SpawnMinionWave not found.", LogLevel.Warn);
        }

        _monitor.Log(
            "Team Up 6.7.44.17 Mutation leader/minion policy enabled: one Mutant leader + 2-4 ordinary hostile minions; only leader gets x3 loot; Pelipper minions use lightweight Team Up actors.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutation leader/minion policy: leader=Mutant | minions=normal-hostile | leaderLoot=x3 | minionLootBonus=none | "
            + $"leadersMarked={_leadersMarked} | minionsNormalized={_minionsNormalized} | hostileReady={_hostileReady} | "
            + $"mutationGuards={_mutationGuards} | lootMarkersStripped={_lootMarkersStripped} | "
            + $"sameType={_sameTypeSeen} | fallback={_fallbackSeen} | "
            + $"pelipperLightweight={MonsterMutationMinionFactory.PelipperLightweightSpawned} | "
            + $"nativePelipperSpawnsAvoided={MonsterMutationMinionFactory.PelipperNativeSpawnAvoided} | last={_last}";

    public void ResetTelemetry()
    {
        _leadersMarked = 0;
        _minionsNormalized = 0;
        _hostileReady = 0;
        _mutationGuards = 0;
        _lootMarkersStripped = 0;
        _sameTypeSeen = 0;
        _fallbackSeen = 0;
        _last = "reset";
    }

    private static void TryMutatePostfix(Monster __0, bool __result)
    {
        Alpha674416MutationLeaderMinionPolicyService? service = Active;
        if (service is null || !__result || !Context.IsWorldReady || !MonsterMutationService.IsMutant(__0))
            return;

        __0.modData[MutantLeaderMarker] = "1";
        __0.modData.Remove(MonsterMutationService.MutationMinionMarker);
        __0.modData.Remove(NormalHostileMinionMarker);
        __0.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";

        service._leadersMarked++;
        service._last = $"leader source={ReadDisplayName(__0)} hostile=true loot=x3";
    }

    private static void SpawnWavePrefix(object __0, out WaveSnapshot __state)
    {
        GameLocation? location = TryReadMember(__0, "Location") as GameLocation;
        Monster? leader = TryReadMember(__0, "Mutant") as Monster;
        location ??= leader?.currentLocation;

        __state = new WaveSnapshot
        {
            Location = location,
            ExistingMinions = location is null
                ? new List<Monster>()
                : location.characters.OfType<Monster>().Where(MonsterMutationService.IsMutationMinion).ToList()
        };
    }

    private static void SpawnWavePostfix(object __0, WaveSnapshot __state)
    {
        Alpha674416MutationLeaderMinionPolicyService? service = Active;
        if (service is null || !Context.IsWorldReady)
            return;

        GameLocation? location = __state.Location
            ?? TryReadMember(__0, "Location") as GameLocation
            ?? (TryReadMember(__0, "Mutant") as Monster)?.currentLocation;
        if (location is null)
            return;

        List<Monster> spawned = location.characters
            .OfType<Monster>()
            .Where(MonsterMutationService.IsMutationMinion)
            .Where(candidate => !__state.ExistingMinions.Any(existing => ReferenceEquals(existing, candidate)))
            .ToList();

        foreach (Monster minion in spawned)
            service.NormalizeMinion(minion);

        if (spawned.Count == 0)
            service._last = $"wave-complete location={location.NameOrUniqueName} newMinions=0";
    }

    private void NormalizeMinion(Monster minion)
    {
        minion.modData.Remove(MonsterMutationService.MutantMarker);
        minion.modData.Remove(MonsterMutationService.MutationScaleMarker);
        minion.modData.Remove(MutantLeaderMarker);

        if (minion.modData.Remove(Alpha674414PelipperMutantRewardService.LootMultiplierMarker))
            _lootMarkersStripped++;

        minion.modData[MonsterMutationService.MutationMinionMarker] = "1";
        minion.modData[MonsterMutationService.MutationExcludedMarker] = "true";
        minion.modData[NormalHostileMinionMarker] = "1";
        minion.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";

        _minionsNormalized++;
        _mutationGuards++;

        bool ready = minion.Health > 0 && minion.MaxHealth > 0;
        if (ready)
            _hostileReady++;

        string sourceType = minion.modData.TryGetValue(MonsterMutationService.MutationSourceMarker, out string? rawType)
            ? rawType ?? string.Empty
            : string.Empty;
        bool sameType = !string.IsNullOrWhiteSpace(sourceType)
            && minion.GetType().FullName?.Equals(sourceType, StringComparison.Ordinal) == true;
        if (sameType)
            _sameTypeSeen++;
        else
            _fallbackSeen++;

        bool lightweightPelipper = minion.modData.ContainsKey(MonsterMutationMinionFactory.PelipperLightweightMinionMarker);
        string species = minion.modData.TryGetValue(MonsterMutationMinionFactory.PelipperLeaderSpeciesMarker, out string? rawSpecies)
            ? rawSpecies ?? string.Empty
            : string.Empty;
        _last = $"minion type={minion.GetType().Name} ordinary=true hostileReady={ready} sameType={sameType} "
            + $"lightweightPelipper={lightweightPelipper} species={species} lootBonus=none";
    }

    private static object? TryReadMember(object target, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            try
            {
                FieldInfo? field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field is not null)
                    return field.GetValue(target);

                PropertyInfo? property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0)
                    return property.GetValue(target);
            }
            catch
            {
                return null;
            }
        }
        return null;
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
}
