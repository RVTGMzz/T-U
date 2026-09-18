using System.Globalization;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.18 source-native Mutation followers.
///
/// Design lock:
/// - a Mutant leader calls 2-4 ordinary followers matching the creature it was before Mutation;
/// - Pelipper followers are genuine Pelipper wild encounters, created through Pelipper's own
///   registered pokemon_spawn command so source/proxy identity and native capture stay owned by Pelipper;
/// - vanilla/custom monsters use the same runtime type only when Team Up can construct it safely;
/// - an unsupported source fails closed. Team Up never substitutes an unrelated GreenSlime.
///
/// The service prefixes MonsterMutationService.SpawnMinionWave and suppresses the legacy wave after
/// handling it. This isolates the architecture change from the already live-proven Mutation HP,
/// Shiny, x3 reward and story paths.
/// </summary>
internal sealed class Alpha674418NativeMutationMinionService
{
    public const string NativeMutationMinionMarker = "Ronvotri.TeamUp/NativeMutationMinion";
    public const string NativeProviderMarker = "Ronvotri.TeamUp/NativeMutationMinionProvider";
    public const string NativeSpeciesMarker = "Ronvotri.TeamUp/NativeMutationMinionSpecies";

    private sealed record PendingPelipperSpawn(
        GameLocation Location,
        Vector2 TargetPosition,
        string Species,
        string SourceType,
        List<NPC> ExistingActors,
        long ExpiresAtTick,
        bool SuppressLoot);

    private static readonly MethodInfo? SafeSpawnMethod = AccessTools.Method(typeof(MonsterMutationService), "TryFindSafeSpawnPosition");
    private static readonly MethodInfo? SuppressLootMethod = AccessTools.Method(typeof(MonsterMutationService), "SuppressKnownLootCollections");
    private static Alpha674418NativeMutationMinionService? Active;

    private readonly IMonitor _monitor;
    private readonly IModHelper _helper;
    private readonly Harmony _harmony;
    private readonly List<PendingPelipperSpawn> _pendingPelipper = new();
    private readonly List<Monster> _claimedPelipperProxies = new();
    private readonly List<NPC> _claimedPelipperSources = new();

    private Action<string, string[]>? _pokemonSpawnCallback;
    private bool _commandProbeComplete;
    private long _wavesHandled;
    private long _requested;
    private long _sameRuntimeSpawned;
    private long _pelipperNativeSpawned;
    private long _pelipperCommandInvoked;
    private long _sourceEquivalentFailures;
    private long _safeRejected;
    private long _deferredResolved;
    private long _deferredExpired;
    private string _last = "reset";

    public static long PelipperNativeSpawned => Active?._pelipperNativeSpawned ?? 0;
    public static long SameRuntimeSpawned => Active?._sameRuntimeSpawned ?? 0;
    public static long SourceEquivalentFailures => Active?._sourceEquivalentFailures ?? 0;

    public Alpha674418NativeMutationMinionService(IMonitor monitor, IModHelper helper, string uniqueId)
    {
        _monitor = monitor;
        _helper = helper;
        _harmony = new Harmony($"{uniqueId}.Alpha674418NativeMutationMinions");
        Active = this;

        MethodInfo? spawnWave = AccessTools.Method(typeof(MonsterMutationService), "SpawnMinionWave");
        if (spawnWave is null)
        {
            _monitor.Log("6.7.44.18 native Mutation minions unavailable: SpawnMinionWave not found.", LogLevel.Error);
            return;
        }

        _harmony.Patch(
            spawnWave,
            prefix: new HarmonyMethod(typeof(Alpha674418NativeMutationMinionService), nameof(SpawnWavePrefix))
            {
                priority = Priority.First
            });

        _helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        _monitor.Log(
            "Team Up 6.7.44.37 Mutation minion stability enabled: Pelipper uses native pokemon_spawn; vanilla/custom require exact source-equivalent runtime type; unsupported sources fail closed; unrelated Slime fallback removed.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutation native minions: source-equivalent-only | requested={_requested} | waves={_wavesHandled} | "
            + $"pelipperNative={_pelipperNativeSpawned} | pelipperCommands={_pelipperCommandInvoked} | "
            + $"sameRuntime={_sameRuntimeSpawned} | sourceFailures={_sourceEquivalentFailures} | safeRejected={_safeRejected} | "
            + $"pending={_pendingPelipper.Count} | deferredResolved={_deferredResolved} | deferredExpired={_deferredExpired} | last={_last}";

    public void ResetTelemetry()
    {
        _pendingPelipper.Clear();
        _claimedPelipperProxies.Clear();
        _claimedPelipperSources.Clear();
        _wavesHandled = 0;
        _requested = 0;
        _sameRuntimeSpawned = 0;
        _pelipperNativeSpawned = 0;
        _pelipperCommandInvoked = 0;
        _sourceEquivalentFailures = 0;
        _safeRejected = 0;
        _deferredResolved = 0;
        _deferredExpired = 0;
        _last = "reset";
    }

    private static bool SpawnWavePrefix(object __0)
    {
        Alpha674418NativeMutationMinionService? service = Active;
        if (service is null)
            return true;

        try
        {
            service.HandleWave(__0);
        }
        catch (Exception ex)
        {
            service._sourceEquivalentFailures++;
            service._last = $"wave-error {ex.GetType().Name}: {ex.Message}";
            service._monitor.Log($"6.7.44.18 native Mutation wave failed closed: {ex}", LogLevel.Error);
        }

        // Never run the legacy wave after this service takes authority. In particular this prevents
        // an unsupported source from silently becoming a GreenSlime fallback.
        return false;
    }

    private void HandleWave(object wave)
    {
        GameLocation? location = ReadMember<GameLocation>(wave, "Location");
        Monster? leader = ReadMember<Monster>(wave, "Mutant");
        int requested = ReadMember<int?>(wave, "RequestedCount") ?? 0;
        int baseMaxHealth = ReadMember<int?>(wave, "BaseMaxHealth") ?? Math.Max(1, leader?.MaxHealth ?? 1);
        int baseDamage = ReadMember<int?>(wave, "BaseDamage") ?? 1;
        int baseSpeed = ReadMember<int?>(wave, "BaseSpeed") ?? Math.Max(1, leader?.Speed ?? 1);
        string sourceType = ReadMember<string>(wave, "SourceType") ?? leader?.GetType().FullName ?? "unknown";

        _wavesHandled++;
        _requested += Math.Max(0, requested);

        if (!Context.IsWorldReady || !Context.IsMainPlayer || location is null || leader is null
            || leader.Health <= 0 || !ReferenceEquals(leader.currentLocation, location) || !location.characters.Contains(leader))
        {
            _last = "wave-skipped leader/location no longer active";
            return;
        }

        bool suppressLoot = ShouldSuppressMinionLoot();
        bool pelipper = PelipperTownCompatibilityService.IsWildCombatActor(leader);
        string species = pelipper && PelipperWildEncounterIdentityService.TryResolve(leader, out PelipperWildEncounterIdentity identity)
            ? identity.DisplayName
            : string.Empty;

        int spawnedNow = 0;
        int pendingNow = 0;
        int failedNow = 0;
        int rejectedNow = 0;

        for (int i = 0; i < requested; i++)
        {
            if (!TryResolveSafePosition(location, leader, i, out Vector2 position))
            {
                _safeRejected++;
                rejectedNow++;
                continue;
            }

            if (pelipper)
            {
                if (string.IsNullOrWhiteSpace(species))
                {
                    _sourceEquivalentFailures++;
                    failedNow++;
                    continue;
                }

                NativeSpawnResult result = RequestPelipperNativeSpawn(location, leader, species, sourceType, position, suppressLoot);
                if (result == NativeSpawnResult.Resolved)
                    spawnedNow++;
                else if (result == NativeSpawnResult.Pending)
                    pendingNow++;
                else
                    failedNow++;
                continue;
            }

            Monster? candidate = MonsterMutationMinionFactory.Create(
                leader,
                position,
                baseMaxHealth,
                baseDamage,
                baseSpeed,
                out string mode);

            if (candidate is null || !mode.Equals("same-runtime-type", StringComparison.Ordinal))
            {
                _sourceEquivalentFailures++;
                failedNow++;
                continue;
            }

            PrepareCombatMinion(candidate, sourceType, provider: "same-runtime-type", species: candidate.Name, suppressLoot);
            location.characters.Add(candidate);
            _sameRuntimeSpawned++;
            spawnedNow++;
        }

        _last = $"wave location={location.NameOrUniqueName} leader={ReadLeaderName(leader)} requested={requested} "
            + $"spawnedNow={spawnedNow} pending={pendingNow} failed={failedNow} safeRejected={rejectedNow} provider={(pelipper ? "pelipper-native" : "same-runtime-type")}";
        _monitor.Log(
            $"[MutationNativeMinions] source={sourceType} leader={ReadLeaderName(leader)} location={location.NameOrUniqueName} "
            + $"requested={requested} spawnedNow={spawnedNow} pending={pendingNow} failed={failedNow} safeRejected={rejectedNow} "
            + $"provider={(pelipper ? "pelipper-native" : "same-runtime-type")}",
            LogLevel.Info);
    }

    private NativeSpawnResult RequestPelipperNativeSpawn(
        GameLocation location,
        Monster leader,
        string species,
        string sourceType,
        Vector2 targetPosition,
        bool suppressLoot)
    {
        Action<string, string[]>? callback = ResolvePokemonSpawnCallback();
        if (callback is null)
        {
            _sourceEquivalentFailures++;
            _last = "Pelipper pokemon_spawn callback unavailable";
            return NativeSpawnResult.Failed;
        }

        List<NPC> before = location.characters.OfType<NPC>().ToList();
        int? level = TryReadPelipperLevel(leader);
        string[] args = level is > 0
            ? new[] { species, level.Value.ToString(CultureInfo.InvariantCulture) }
            : new[] { species };

        try
        {
            callback("pokemon_spawn", args);
            _pelipperCommandInvoked++;
        }
        catch (Exception ex)
        {
            _sourceEquivalentFailures++;
            _last = $"pokemon_spawn error {ex.GetType().Name}: {ex.Message}";
            _monitor.Log($"Pelipper native follower spawn failed for {species}: {ex.GetType().Name}: {ex.Message}", LogLevel.Warn);
            return NativeSpawnResult.Failed;
        }

        var request = new PendingPelipperSpawn(
            location,
            targetPosition,
            species,
            sourceType,
            before,
            Game1.ticks + 45,
            suppressLoot);

        if (TryResolvePelipperRequest(request, out _))
            return NativeSpawnResult.Resolved;

        _pendingPelipper.Add(request);
        return NativeSpawnResult.Pending;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || _pendingPelipper.Count == 0)
            return;

        for (int i = _pendingPelipper.Count - 1; i >= 0; i--)
        {
            PendingPelipperSpawn request = _pendingPelipper[i];
            if (TryResolvePelipperRequest(request, out string detail))
            {
                _pendingPelipper.RemoveAt(i);
                _deferredResolved++;
                _last = $"deferred-resolved {detail}";
                continue;
            }

            if (Game1.ticks <= request.ExpiresAtTick)
                continue;

            _pendingPelipper.RemoveAt(i);
            _deferredExpired++;
            _sourceEquivalentFailures++;
            _last = $"native-timeout species={request.Species} location={request.Location.NameOrUniqueName}";
            _monitor.Log(
                $"Pelipper native Mutation follower timed out: species={request.Species} location={request.Location.NameOrUniqueName}. No Slime fallback was created.",
                LogLevel.Warn);
        }
    }

    private bool TryResolvePelipperRequest(PendingPelipperSpawn request, out string detail)
    {
        detail = string.Empty;
        if (!ReferenceEquals(Game1.getLocationFromName(request.Location.NameOrUniqueName), request.Location)
            && request.Location.characters.Count == 0)
        {
            return false;
        }

        List<Monster> candidates = request.Location.characters
            .OfType<Monster>()
            .Where(PelipperTownCompatibilityService.IsWildCombatActor)
            .Where(candidate => !_claimedPelipperProxies.Any(existing => ReferenceEquals(existing, candidate)))
            .Where(candidate => !request.ExistingActors.Any(existing => ReferenceEquals(existing, candidate)))
            .ToList();

        foreach (Monster proxy in candidates)
        {
            if (!PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity))
                continue;
            if (_claimedPelipperSources.Any(existing => ReferenceEquals(existing, identity.SourceActor)))
                continue;
            if (!NormalizeSpecies(identity.DisplayName).Equals(NormalizeSpecies(request.Species), StringComparison.Ordinal))
                continue;

            proxy.Position = request.TargetPosition;
            if (!ReferenceEquals(identity.SourceActor, proxy))
                identity.SourceActor.Position = request.TargetPosition;

            PrepareCombatMinion(proxy, request.SourceType, "pelipper-native", identity.DisplayName, request.SuppressLoot);
            identity.SourceActor.modData[MonsterMutationService.MutationExcludedMarker] = "true";
            identity.SourceActor.modData[NativeMutationMinionMarker] = "1";
            identity.SourceActor.modData[NativeProviderMarker] = "pelipper-native";
            identity.SourceActor.modData[NativeSpeciesMarker] = identity.DisplayName;

            _claimedPelipperProxies.Add(proxy);
            _claimedPelipperSources.Add(identity.SourceActor);
            _pelipperNativeSpawned++;
            detail = $"species={identity.DisplayName} encounter={identity.EncounterId} tile={(int)(request.TargetPosition.X / 64f)},{(int)(request.TargetPosition.Y / 64f)} capture=native";
            _monitor.Log($"[MutationNativePelipper] {detail}", LogLevel.Info);
            return true;
        }

        return false;
    }

    private void PrepareCombatMinion(Monster minion, string sourceType, string provider, string species, bool suppressLoot)
    {
        minion.modData.Remove(MonsterMutationService.MutantMarker);
        minion.modData.Remove(MonsterMutationService.MutationScaleMarker);
        minion.modData.Remove(Alpha674416MutationLeaderMinionPolicyService.MutantLeaderMarker);
        minion.modData.Remove(Alpha674414PelipperMutantRewardService.LootMultiplierMarker);
        minion.modData[MonsterMutationService.MutationMinionMarker] = "1";
        minion.modData[MonsterMutationService.MutationExcludedMarker] = "true";
        minion.modData[MonsterMutationService.MutationSourceMarker] = sourceType;
        minion.modData[Alpha674416MutationLeaderMinionPolicyService.NormalHostileMinionMarker] = "1";
        minion.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
        minion.modData[NativeMutationMinionMarker] = "1";
        minion.modData[NativeProviderMarker] = provider;
        minion.modData[NativeSpeciesMarker] = species;

        if (suppressLoot && SuppressLootMethod is not null)
        {
            try { SuppressLootMethod.Invoke(null, new object[] { minion }); }
            catch { /* optional reward suppression stays best-effort */ }
        }
    }

    private bool ShouldSuppressMinionLoot()
    {
        MonsterMutationService? service = MonsterMutationService.ActiveInstance;
        if (service is null)
            return true;

        Func<bool>? getter = ReadMember<Func<bool>>(service, "_minionLoot");
        try { return getter is null || !getter(); }
        catch { return true; }
    }

    private static bool TryResolveSafePosition(GameLocation location, Monster leader, int seed, out Vector2 position)
    {
        position = Vector2.Zero;
        if (SafeSpawnMethod is null)
            return false;

        object?[] args = { location, leader, seed, Vector2.Zero };
        try
        {
            bool resolved = SafeSpawnMethod.Invoke(null, args) is true;
            if (resolved && args[3] is Vector2 found)
            {
                position = found;
                return true;
            }
        }
        catch
        {
            return false;
        }
        return false;
    }

    private Action<string, string[]>? ResolvePokemonSpawnCallback()
    {
        if (_commandProbeComplete)
            return _pokemonSpawnCallback;
        _commandProbeComplete = true;

        try
        {
            object commandHelper = _helper.ConsoleCommands;
            const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo? managerField = commandHelper.GetType().GetField("CommandManager", all)
                ?? commandHelper.GetType().GetFields(all).FirstOrDefault(field =>
                    field.FieldType.GetMethod("Get", all, binder: null, types: new[] { typeof(string) }, modifiers: null) is not null);
            object? manager = managerField?.GetValue(commandHelper);
            if (manager is null)
            {
                _monitor.Log("6.7.44.18 could not resolve SMAPI CommandManager for Pelipper native spawning.", LogLevel.Warn);
                return null;
            }

            MethodInfo? get = manager.GetType().GetMethod(
                "Get",
                all,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);
            object? command = get?.Invoke(manager, new object[] { "pokemon_spawn" });
            if (command is null)
            {
                _monitor.Log("6.7.44.18 Pelipper native command 'pokemon_spawn' is not registered.", LogLevel.Warn);
                return null;
            }

            PropertyInfo? callbackProperty = command.GetType().GetProperty("Callback", all);
            _pokemonSpawnCallback = callbackProperty?.GetValue(command) as Action<string, string[]>;
            if (_pokemonSpawnCallback is null)
                _monitor.Log("6.7.44.18 found pokemon_spawn but could not bind its callback.", LogLevel.Warn);
        }
        catch (Exception ex)
        {
            _monitor.Log($"6.7.44.18 pokemon_spawn callback probe failed safely: {ex.GetType().Name}: {ex.Message}", LogLevel.Warn);
        }

        return _pokemonSpawnCallback;
    }

    private static int? TryReadPelipperLevel(Monster leader)
    {
        NPC source = leader;
        if (PelipperWildEncounterIdentityService.TryResolve(leader, out PelipperWildEncounterIdentity identity))
            source = identity.SourceActor;

        foreach (var pair in source.modData.Pairs)
        {
            if (!pair.Key.Contains("level", StringComparison.OrdinalIgnoreCase))
                continue;
            if (int.TryParse(pair.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed is >= 1 and <= 100)
            {
                return parsed;
            }
        }

        foreach (string name in new[] { "Level", "PokemonLevel", "WildLevel", "level" })
        {
            object? raw = ReadMember<object>(source, name);
            if (raw is not null && int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && parsed is >= 1 and <= 100)
            {
                return parsed;
            }
        }

        return null;
    }

    private static T? ReadMember<T>(object target, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            try
            {
                FieldInfo? field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field is not null)
                {
                    object? value = field.GetValue(target);
                    if (value is T typed)
                        return typed;
                    if (typeof(T) == typeof(int?) && value is int integer)
                        return (T)(object)(int?)integer;
                }

                PropertyInfo? property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    object? value = property.GetValue(target);
                    if (value is T typed)
                        return typed;
                    if (typeof(T) == typeof(int?) && value is int integer)
                        return (T)(object)(int?)integer;
                }
            }
            catch
            {
                return default;
            }
        }
        return default;
    }

    private static string ReadLeaderName(Monster leader)
    {
        if (PelipperWildEncounterIdentityService.TryResolve(leader, out PelipperWildEncounterIdentity identity)
            && !string.IsNullOrWhiteSpace(identity.DisplayName))
        {
            return identity.DisplayName;
        }
        return string.IsNullOrWhiteSpace(leader.displayName) ? leader.Name : leader.displayName;
    }

    private static string NormalizeSpecies(string value)
    {
        string text = value.Trim()
            .Replace("♂", "male", StringComparison.Ordinal)
            .Replace("♀", "female", StringComparison.Ordinal);
        bool changed;
        do
        {
            changed = false;
            foreach (string prefix in new[] { "Wild ", "Shiny " })
            {
                if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                text = text[prefix.Length..].Trim();
                changed = true;
            }
        } while (changed && text.Length > 0);
        return new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private enum NativeSpawnResult
    {
        Failed,
        Pending,
        Resolved
    }
}
