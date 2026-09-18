using System.Collections;
using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 6.7.19 mutation encounters.
///
/// A normal hostile monster gets one mutation roll when its death animation begins. On success,
/// Team Up cancels that death animation and transforms the SAME runtime monster into a mutant.
/// Reusing the live instance preserves custom-mod AI, sprites, NetFields and constructor-only state,
/// which is much safer than reflection-cloning arbitrary third-party monsters.
///
/// Policy is mod-agnostic by default: normal custom monsters and normal Pelipper wild combat proxies
/// are eligible. Confirmed Shiny, owned companions, boss/script/event actors, Surge spawns, mutation
/// minions and already-mutated monsters are excluded. Cardcha's normal monsters therefore work automatically, while its test harness and any
/// actor explicitly tagged Boss/Scripted/MutationExcluded fail closed.
/// </summary>
internal sealed class MonsterMutationService
{
    public const string MutantMarker = "Ronvotri.TeamUp/Mutant";
    public const string MutationMinionMarker = "Ronvotri.TeamUp/MutationMinion";
    public const string MutationExcludedMarker = "Ronvotri.TeamUp/MutationExcluded";
    public const string MutationBossMarker = "Ronvotri.TeamUp/MutationBoss";
    public const string MutationSourceMarker = "Ronvotri.TeamUp/MutationSource";
    public const string MutationScaleMarker = "Ronvotri.TeamUp/MutationVisualScale";
    public const string MutationIntendedDamageMarker = "Ronvotri.TeamUp/MutantIntendedDamage";

    // Pelipper wild combat uses a technical proxy whose DamageToFarmer can be the placeholder value 1.
    // Treat 4 as the ordinary compatibility floor, then apply the configured Mutation stat multiplier.
    private const int PelipperTechnicalBaseDamageFloor = 4;

    private static readonly Point[] SpawnOffsets =
    {
        new(2, 0), new(-2, 0), new(0, 2), new(0, -2),
        new(2, 2), new(-2, 2), new(2, -2), new(-2, -2),
        new(3, 0), new(-3, 0), new(0, 3), new(0, -3),
        new(3, 1), new(-3, 1), new(3, -1), new(-3, -1),
        new(1, 3), new(-1, 3), new(1, -3), new(-1, -3),
        new(4, 0), new(-4, 0), new(0, 4), new(0, -4)
    };

    private const float MinimumFarmerSpawnDistance = 96f;
    private const float MinimumMonsterSpawnDistance = 48f;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly Func<bool> _enabled;
    private readonly Func<float> _chancePercent;
    private readonly Func<float> _healthMultiplier;
    private readonly Func<float> _statMultiplier;
    private readonly Func<float> _visualScaleMultiplier;
    private readonly Func<int> _minionMin;
    private readonly Func<int> _minionMax;
    private readonly Func<bool> _minionLoot;
    private readonly HashSet<MethodBase> _patchedDeathMethods = new();
    private readonly Queue<PendingMinionWave> _pendingWaves = new();

    private int _rolls;
    private int _mutations;
    private int _minionsSpawned;
    private int _excludedDeaths;

    public static MonsterMutationService? ActiveInstance { get; private set; }
    public string LastMutationLine { get; private set; } = "[MutationTelemetry] no-record";
    public int PatchedDeathMethodCount => _patchedDeathMethods.Count;

    public MonsterMutationService(
        IMonitor monitor,
        string uniqueId,
        Func<bool> enabled,
        Func<float> chancePercent,
        Func<float> healthMultiplier,
        Func<float> statMultiplier,
        Func<float> visualScaleMultiplier,
        Func<int> minionMin,
        Func<int> minionMax,
        Func<bool> minionLoot)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha6719MutationEncounters");
        _enabled = enabled;
        _chancePercent = chancePercent;
        _healthMultiplier = healthMultiplier;
        _statMultiplier = statMultiplier;
        _visualScaleMultiplier = visualScaleMultiplier;
        _minionMin = minionMin;
        _minionMax = minionMax;
        _minionLoot = minionLoot;
        ActiveInstance = this;
        ApplyDeathAnimationHooks();
    }

    public void ResetRuntime()
    {
        _pendingWaves.Clear();
        _rolls = 0;
        _mutations = 0;
        _minionsSpawned = 0;
        _excludedDeaths = 0;
        LastMutationLine = "[MutationTelemetry] reset";
        ApplyDeathAnimationHooks();
    }

    public void Update()
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (_pendingWaves.Count == 0)
            return;

        int count = _pendingWaves.Count;
        for (int i = 0; i < count; i++)
        {
            PendingMinionWave wave = _pendingWaves.Dequeue();
            if (wave.DelayTicks > 0)
            {
                _pendingWaves.Enqueue(wave with { DelayTicks = wave.DelayTicks - 1 });
                continue;
            }

            SpawnMinionWave(wave);
        }
    }

    public string Describe()
    {
        int active = 0;
        int minions = 0;
        if (Context.IsWorldReady && Game1.currentLocation is not null)
        {
            active = Game1.currentLocation.characters.OfType<Monster>().Count(IsMutant);
            minions = Game1.currentLocation.characters.OfType<Monster>().Count(IsMutationMinion);
        }

        return $"Mutation: Enabled={_enabled()} | Chance={Math.Clamp(_chancePercent(), 0f, 100f):0.##}% | "
            + $"HPx{Math.Clamp(_healthMultiplier(), 1f, 10f):0.##} | Statx{Math.Clamp(_statMultiplier(), 1f, 5f):0.##} | "
            + $"Scalex{Math.Clamp(_visualScaleMultiplier(), 1f, 5f):0.##} | Minions={Math.Clamp(_minionMin(), 0, 8)}-{Math.Clamp(_minionMax(), 0, 8)} | "
            + $"deathHooks={PatchedDeathMethodCount} | rolls={_rolls} | mutations={_mutations} | excluded={_excludedDeaths} | "
            + $"active={active} | activeMinions={minions} | spawnedMinions={_minionsSpawned} | "
            + $"footprintHooks={MonsterMutationFootprintPatch.PatchedMethodCount} | sameTypeMinions={MonsterMutationMinionFactory.SameTypeSpawned} | "
            + $"fallbackMinions={MonsterMutationMinionFactory.FallbackSpawned} | failClosed={MonsterMutationMinionFactory.FailClosedRejected}";
    }

    public IReadOnlyList<string> DescribeCurrentLocation()
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
            return new[] { "Mutation list unavailable until a save is loaded." };

        List<string> lines = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => IsMutant(monster) || IsMutationMinion(monster))
            .Select(monster =>
            {
                string kind = IsMutant(monster) ? "MUTANT" : "MINION";
                return $"{kind} {monster.Name} type={monster.GetType().FullName} HP={monster.Health}/{monster.MaxHealth} speed={monster.Speed}";
            })
            .ToList();

        if (lines.Count == 0)
            lines.Add("No Team Up mutants/minions in the current location.");
        return lines;
    }

    public bool ForceNearestEligible(out string result)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || Game1.currentLocation is null)
        {
            result = "Load a save as host before forcing a mutation.";
            return false;
        }

        Monster? target = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(IsEligible)
            .OrderBy(monster => Vector2.DistanceSquared(monster.Position, Game1.player.Position))
            .FirstOrDefault();

        if (target is null)
        {
            result = "No eligible normal hostile monster is available in this location.";
            return false;
        }

        bool transformed = TryMutate(target, force: true);
        result = transformed
            ? $"Forced mutation: {target.Name} -> HP {target.Health}/{target.MaxHealth}."
            : $"Force mutation was rejected for {target.Name}.";
        return transformed;
    }

    public static bool IsMutant(Monster monster)
        => monster.modData.ContainsKey(MutantMarker);

    public static bool IsMutationMinion(Monster monster)
        => monster.modData.ContainsKey(MutationMinionMarker);

    private void ApplyDeathAnimationHooks()
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (!typeof(Monster).IsAssignableFrom(type))
                    continue;

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (MethodInfo method in type.GetMethods(flags))
                {
                    if (!method.Name.Equals("deathAnimation", StringComparison.Ordinal)
                        || method.IsAbstract
                        || method.IsStatic
                        || method.ContainsGenericParameters
                        || method.ReturnType != typeof(void)
                        || _patchedDeathMethods.Contains(method))
                    {
                        continue;
                    }

                    try
                    {
                        _harmony.Patch(
                            method,
                            prefix: new HarmonyMethod(typeof(MonsterMutationService), nameof(DeathAnimationPrefix)));
                        _patchedDeathMethods.Add(method);
                    }
                    catch (Exception ex)
                    {
                        _monitor.Log($"Mutation hook skipped {type.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }

        _monitor.Log($"Alpha 6.7.19 mutation system patched {_patchedDeathMethods.Count} Monster.deathAnimation implementation(s).", LogLevel.Info);
    }

    private static bool DeathAnimationPrefix(Monster __instance)
    {
        MonsterMutationService? service = ActiveInstance;
        if (service is null)
            return true;

        try
        {
            MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);

            // Returning false suppresses the lethal death animation only when the same monster
            // has successfully been converted into a mutant and its Health restored above zero.
            return !service.TryMutate(__instance, force: false);
        }
        catch (Exception ex)
        {
            service._monitor.Log($"Mutation interception failed safely for {__instance.GetType().FullName}: {ex}", LogLevel.Error);
            return true;
        }
    }

    private bool TryMutate(Monster monster, bool force)
    {
        if (!_enabled() || !Context.IsWorldReady || !Context.IsMainPlayer)
            return false;

        if (!IsEligible(monster))
        {
            if (!force)
                _excludedDeaths++;
            return false;
        }

        SurgeMutationDirective storyDirective = SurgeMutationDirective.NormalRoll;
        if (!force)
            storyDirective = TheSurgeStoryService.ActiveInstance?.ObserveEligibleDeath(monster)
                ?? SurgeMutationDirective.NormalRoll;

        if (!force)
        {
            _rolls++;
            if (storyDirective == SurgeMutationDirective.SuppressMutation)
                return false;

            if (storyDirective != SurgeMutationDirective.ForceFirstMutation)
            {
                double chance = Math.Clamp(_chancePercent(), 0f, 100f) / 100d;
                if (chance <= 0d || Game1.random.NextDouble() >= chance)
                    return false;
            }
        }

        GameLocation? location = monster.currentLocation ?? Game1.currentLocation;
        if (location is null)
            return false;

        int baseMaxHealth = Math.Max(1, monster.MaxHealth);
        int rawBaseDamage = Math.Max(1, ReadIntMember(monster, "DamageToFarmer", "damageToFarmer") ?? 1);
        bool pelipperWild = PelipperTownCompatibilityService.IsWildCombatActor(monster);
        int baseDamage = pelipperWild
            ? Math.Max(rawBaseDamage, PelipperTechnicalBaseDamageFloor)
            : rawBaseDamage;
        int baseSpeed = Math.Max(1, monster.Speed);
        int baseResilience = Math.Max(0, ReadIntMember(monster, "Resilience", "resilience") ?? 0);

        float healthScale = Math.Clamp(_healthMultiplier(), 1f, 10f);
        float statScale = Math.Clamp(_statMultiplier(), 1f, 5f);
        float visualScale = Math.Clamp(_visualScaleMultiplier(), 1f, 5f);

        int mutantMax = SafeScaledInt(baseMaxHealth, healthScale, 1, 2_000_000);
        monster.MaxHealth = mutantMax;
        monster.Health = mutantMax;

        int intendedDamage = SafeScaledInt(baseDamage, statScale, 1, 100_000);
        TryWriteNumericMember(monster, intendedDamage, "DamageToFarmer", "damageToFarmer");
        if (baseResilience > 0)
            TryWriteNumericMember(monster, SafeScaledInt(baseResilience, statScale, 0, 100_000), "Resilience", "resilience");

        int mutantSpeed = SafeScaledInt(baseSpeed, statScale, 1, 12);
        monster.Speed = mutantSpeed;

        // Scale is best-effort reflection so custom monster classes without a writable Scale member
        // remain fully functional instead of failing mutation. Existing custom scale is multiplied.
        double existingScale = ReadNumericMember(monster, "Scale", "scale") ?? 1d;
        bool visualScaleApplied = TryWriteNumericMember(
            monster,
            Math.Clamp(existingScale * visualScale, 0.25d, 12d),
            "Scale", "scale");
        float effectiveFootprintScale = visualScaleApplied ? visualScale : 1f;

        monster.modData[MutantMarker] = "1";
        if (PelipperTownCompatibilityService.IsWildCombatActor(monster))
        {
            // Mutated wild proxies are combat threats, not capture-floor targets. Restore Team Up
            // targeting even if the proxy was previously removed at Pelipper's mercy threshold.
            monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
        }
        monster.modData[MutationSourceMarker] = monster.GetType().FullName ?? monster.GetType().Name;
        monster.modData[MutationScaleMarker] = effectiveFootprintScale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        monster.modData[MutationIntendedDamageMarker] = intendedDamage.ToString(System.Globalization.CultureInfo.InvariantCulture);

        int min = Math.Clamp(_minionMin(), 0, 8);
        int max = Math.Clamp(_minionMax(), 0, 8);
        if (max < min)
            (min, max) = (max, min);
        int requestedMinions = max <= 0 ? 0 : Game1.random.Next(min, max + 1);
        if (requestedMinions > 0)
        {
            _pendingWaves.Enqueue(new PendingMinionWave(
                location,
                monster,
                baseMaxHealth,
                baseDamage,
                baseSpeed,
                requestedMinions,
                monster.GetType().FullName ?? monster.GetType().Name,
                DelayTicks: 1));
        }

        _mutations++;
        LastMutationLine =
            $"[MutationTelemetry] source={monster.GetType().FullName} location={location.NameOrUniqueName} "
            + $"baseHP={baseMaxHealth} mutantHP={mutantMax} rawDamage={rawBaseDamage} baseDamage={baseDamage} intendedDamage={intendedDamage} damageX={statScale:0.##} "
            + $"baseResilience={baseResilience} speed={baseSpeed}->{mutantSpeed} scaleX={effectiveFootprintScale:0.##} "
            + $"scaleApplied={visualScaleApplied} minionsRequested={requestedMinions} force={force} storyDirective={storyDirective}";
        _monitor.Log(LastMutationLine, LogLevel.Info);

        bool transformed = monster.Health > 0;
        if (transformed && storyDirective == SurgeMutationDirective.ForceFirstMutation)
            TheSurgeStoryService.ActiveInstance?.CommitFirstMutation(monster);
        else if (transformed && !Game1.eventUp)
            Game1.showGlobalMessage($"⚠ MUTATION DETECTED • {monster.Name}");

        return transformed;
    }

    private bool IsEligible(Monster monster)
    {
        // Repair stale false-Shiny state written by 6.7.44.2 before applying normal exclusions.
        if (EncounterReactionService.IsConfirmedShiny(monster)
            && !EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster))
        {
            monster.modData.Remove(EncounterReactionService.ShinyConfirmedMarker);
            monster.modData.Remove(EncounterReactionService.ShinyEmergencyHoldMarker);
            monster.modData.Remove(EncounterReactionService.ShinyEngagedMarker);
            monster.modData.Remove(EncounterReactionService.ShinyIgnoredMarker);
            monster.modData.Remove(MutationExcludedMarker);
        }

        if (IsMutant(monster)
            || IsMutationMinion(monster)
            || MonsterSurgeService.IsSurgeMonster(monster)
            || monster.modData.ContainsKey(MutationExcludedMarker)
            || monster.modData.ContainsKey(MutationBossMarker))
        {
            return false;
        }

        if (Game1.eventUp || Game1.currentLocation is null)
            return false;

        GameLocation? location = monster.currentLocation ?? Game1.currentLocation;
        if (location is null)
            return false;

        // Cardcha normal map monsters are intentionally NOT excluded. Only the explicit test
        // harness or a Cardcha actor carrying Boss/Scripted/MutationExcluded tags is skipped.
        if (OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster)
            || location.NameOrUniqueName.Equals(OptionalTestHostCompatibility.CardchaArenaLocationName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // 6.7.44.3: normal Pelipper wild combat proxies are valid Mutation candidates.
        // Confirmed Shiny always wins over Mutation. Owned/source-controlled companions remain
        // excluded through the normal Team Up combat ownership gate.
        bool pelipperWild = PelipperTownCompatibilityService.IsWildCombatActor(monster);
        if (pelipperWild)
        {
            if (EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster))
            {
                monster.modData[MutationExcludedMarker] = "true";
                return false;
            }
        }
        else if (PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
        {
            return false;
        }

        string typeName = monster.GetType().Name;
        string fullTypeName = monster.GetType().FullName ?? typeName;
        string monsterName = monster.Name ?? string.Empty;
        if (typeName.Contains("Boss", StringComparison.OrdinalIgnoreCase)
            || fullTypeName.Contains(".Boss", StringComparison.OrdinalIgnoreCase)
            || monsterName.Contains("Boss", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (HasTruthyPolicyTag(monster, "boss", "scripted", "questprotected", "mutationexcluded"))
            return false;

        return true;
    }

    public void DrawAura(SpriteBatch spriteBatch)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null || Game1.eventUp)
            return;

        double time = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        float pulse = 0.5f + 0.5f * MathF.Sin((float)(time / 180d));
        Color outer = new Color(180, 65, 255) * (0.28f + pulse * 0.20f);
        Color inner = new Color(255, 85, 115) * (0.35f + (1f - pulse) * 0.20f);

        foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>().Where(IsMutant))
        {
            if (monster.Health <= 0)
                continue;

            Rectangle world = monster.GetBoundingBox();
            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(world.X, world.Y));
            Rectangle screen = new((int)screenPos.X, (int)screenPos.Y, world.Width, world.Height);
            int pad1 = 12 + (int)Math.Round(pulse * 8f);
            int pad2 = pad1 + 9;
            DrawOutline(spriteBatch, Inflate(screen, pad2), 3, outer);
            DrawOutline(spriteBatch, Inflate(screen, pad1), 2, inner);
        }
    }

    private void SpawnMinionWave(PendingMinionWave wave)
    {
        if (!Context.IsWorldReady
            || !Context.IsMainPlayer
            || wave.Mutant.Health <= 0
            || !ReferenceEquals(wave.Mutant.currentLocation, wave.Location)
            || !wave.Location.characters.Contains(wave.Mutant))
        {
            return;
        }

        int spawned = 0;
        int rejected = 0;
        int sameType = 0;
        int failClosed = 0;
        for (int i = 0; i < wave.RequestedCount; i++)
        {
            if (!TryFindSafeSpawnPosition(wave.Location, wave.Mutant, i, out Vector2 position))
            {
                rejected++;
                continue;
            }

            Monster? minion = MonsterMutationMinionFactory.Create(
                wave.Mutant,
                position,
                wave.BaseMaxHealth,
                wave.BaseDamage,
                wave.BaseSpeed,
                out string spawnMode);
            if (minion is null)
            {
                failClosed++;
                continue;
            }

            if (!spawnMode.Equals("same-runtime-type", StringComparison.Ordinal))
            {
                failClosed++;
                continue;
            }

            sameType++;
            minion.modData[MutationMinionMarker] = "1";
            minion.modData[MutationSourceMarker] = wave.SourceType;
            if (!_minionLoot())
                SuppressKnownLootCollections(minion);

            wave.Location.characters.Add(minion);
            spawned++;
        }

        _minionsSpawned += spawned;
        LastMutationLine += $" | minionsSpawned={spawned}/{wave.RequestedCount} sameType={sameType} failClosed={failClosed} safeRejected={rejected}";
        _monitor.Log(
            $"[MutationMinions] source={wave.SourceType} location={wave.Location.NameOrUniqueName} "
            + $"spawned={spawned}/{wave.RequestedCount} sameType={sameType} failClosed={failClosed} safeRejected={rejected}",
            LogLevel.Info);
    }

    private static bool TryFindSafeSpawnPosition(GameLocation location, Monster anchor, int seed, out Vector2 position)
    {
        int sourceTileX = (int)Math.Floor((anchor.Position.X + 32f) / 64f);
        int sourceTileY = (int)Math.Floor((anchor.Position.Y + 32f) / 64f);
        int start = Math.Abs(seed * 7 + sourceTileX * 3 + sourceTileY * 5) % SpawnOffsets.Length;

        for (int attempt = 0; attempt < SpawnOffsets.Length; attempt++)
        {
            Point offset = SpawnOffsets[(start + attempt) % SpawnOffsets.Length];
            int tileX = sourceTileX + offset.X;
            int tileY = sourceTileY + offset.Y;
            if (!IsSafeSpawnTile(location, tileX, tileY))
                continue;

            Vector2 candidate = new(tileX * 64f, tileY * 64f);
            Vector2 candidateCenter = candidate + new Vector2(32f, 32f);
            Vector2 farmerCenter = Game1.player.Position + new Vector2(32f, 32f);
            if (Vector2.DistanceSquared(candidateCenter, farmerCenter) < MinimumFarmerSpawnDistance * MinimumFarmerSpawnDistance)
                continue;

            bool tooClose = location.characters
                .OfType<Monster>()
                .Where(monster => monster.Health > 0)
                .Any(monster => Vector2.DistanceSquared(monster.Position, candidate) < MinimumMonsterSpawnDistance * MinimumMonsterSpawnDistance);
            if (tooClose)
                continue;

            position = candidate;
            return true;
        }

        position = Vector2.Zero;
        return false;
    }

    private static bool IsSafeSpawnTile(GameLocation location, int tileX, int tileY)
    {
        if (tileX < 0 || tileY < 0)
            return false;

        try
        {
            Vector2 tile = new(tileX, tileY);
            return location.isTileOnMap(tile)
                && location.isTilePassable(tile)
                && !location.IsTileBlockedBy(tile, ignorePassables: CollisionMask.All);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasTruthyPolicyTag(Monster monster, params string[] tokens)
    {
        foreach (string key in monster.modData.Keys)
        {
            string value = monster.modData.TryGetValue(key, out string? rawValue) ? rawValue ?? string.Empty : string.Empty;
            string normalizedKey = Normalize(key);
            if (!tokens.Any(token => normalizedKey.Contains(Normalize(token), StringComparison.Ordinal)))
                continue;
            if (IsTruthy(value))
                return true;
        }
        return false;
    }

    private static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        string normalized = value.Trim().ToLowerInvariant();
        return normalized is "1" or "true" or "yes" or "on" or "enabled";
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static int SafeScaledInt(int value, float multiplier, int minimum, int maximum)
    {
        double scaled = Math.Round(value * (double)multiplier, MidpointRounding.AwayFromZero);
        return (int)Math.Clamp(scaled, minimum, maximum);
    }

    private static int? ReadIntMember(object target, params string[] names)
    {
        double? value = ReadNumericMember(target, names);
        if (value is null)
            return null;
        return (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
    }

    private static double? ReadNumericMember(object target, params string[] names)
    {
        foreach (string name in names)
        {
            if (!TryGetMemberValue(target, name, out object? value) || value is null)
                continue;
            if (TryConvertNumeric(value, out double direct))
                return direct;
            if (TryGetMemberValue(value, "Value", out object? nested) && nested is not null && TryConvertNumeric(nested, out double wrapped))
                return wrapped;
        }
        return null;
    }

    private static bool TryWriteNumericMember(object target, double value, params string[] names)
    {
        foreach (string name in names)
        {
            if (TrySetMemberNumeric(target, name, value))
                return true;

            if (!TryGetMemberValue(target, name, out object? wrapper) || wrapper is null)
                continue;
            if (TrySetMemberNumeric(wrapper, "Value", value))
                return true;
        }
        return false;
    }

    private static bool TryGetMemberValue(object target, string name, out object? value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type? type = target.GetType();
        while (type is not null)
        {
            try
            {
                FieldInfo? field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field is not null)
                {
                    value = field.GetValue(target);
                    return true;
                }
                PropertyInfo? property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(target);
                    return true;
                }
            }
            catch
            {
                // Continue into the base type or the next candidate name.
            }
            type = type.BaseType;
        }
        value = null;
        return false;
    }

    private static bool TrySetMemberNumeric(object target, string name, double value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type? type = target.GetType();
        while (type is not null)
        {
            try
            {
                FieldInfo? field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field is not null && !field.IsInitOnly && TryConvertForType(value, field.FieldType, out object? fieldValue))
                {
                    field.SetValue(target, fieldValue);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property is not null && property.CanWrite && property.GetIndexParameters().Length == 0
                    && TryConvertForType(value, property.PropertyType, out object? propertyValue))
                {
                    property.SetValue(target, propertyValue);
                    return true;
                }
            }
            catch
            {
                // Best-effort compatibility: unknown mod internals fail closed.
            }
            type = type.BaseType;
        }
        return false;
    }

    private static bool TryConvertNumeric(object value, out double number)
    {
        try
        {
            number = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            number = 0d;
            return false;
        }
    }

    private static bool TryConvertForType(double value, Type targetType, out object? converted)
    {
        try
        {
            Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (type == typeof(int)) converted = (int)Math.Round(value);
            else if (type == typeof(float)) converted = (float)value;
            else if (type == typeof(double)) converted = value;
            else if (type == typeof(long)) converted = (long)Math.Round(value);
            else if (type == typeof(short)) converted = (short)Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue);
            else if (type == typeof(byte)) converted = (byte)Math.Clamp(Math.Round(value), byte.MinValue, byte.MaxValue);
            else
            {
                converted = null;
                return false;
            }
            return true;
        }
        catch
        {
            converted = null;
            return false;
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

    private static void SuppressKnownLootCollections(Monster monster)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        foreach (string memberName in new[] { "objectsToDrop", "itemsToDrop", "ObjectsToDrop", "ItemsToDrop" })
        {
            try
            {
                object? value = monster.GetType().GetField(memberName, flags)?.GetValue(monster)
                    ?? monster.GetType().GetProperty(memberName, flags)?.GetValue(monster);
                if (value is IList list)
                    list.Clear();
            }
            catch
            {
                // Economy guard is best-effort; never break combat for a custom reward model.
            }
        }
    }

    private static Rectangle Inflate(Rectangle rectangle, int padding)
        => new(rectangle.X - padding, rectangle.Y - padding, rectangle.Width + padding * 2, rectangle.Height + padding * 2);

    private static void DrawOutline(SpriteBatch spriteBatch, Rectangle rectangle, int thickness, Color color)
    {
        if (rectangle.Width <= 0 || rectangle.Height <= 0)
            return;
        spriteBatch.Draw(Game1.staminaRect, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, thickness), color);
        spriteBatch.Draw(Game1.staminaRect, new Rectangle(rectangle.X, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        spriteBatch.Draw(Game1.staminaRect, new Rectangle(rectangle.X, rectangle.Y, thickness, rectangle.Height), color);
        spriteBatch.Draw(Game1.staminaRect, new Rectangle(rectangle.Right - thickness, rectangle.Y, thickness, rectangle.Height), color);
    }

    private sealed record PendingMinionWave(
        GameLocation Location,
        Monster Mutant,
        int BaseMaxHealth,
        int BaseDamage,
        int BaseSpeed,
        int RequestedCount,
        string SourceType,
        int DelayTicks);
}
