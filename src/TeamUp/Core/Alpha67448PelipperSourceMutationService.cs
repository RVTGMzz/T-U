using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.8 source-aware Pelipper Mutation bridge.
///
/// Pelipper wild encounters use two runtime actors: a visible Pokemon source NPC and a hidden
/// Monster combat proxy. The proxy can use a very large sentinel Health/MaxHealth value, so it is
/// not a valid source of truth for Pokemon defeat or Mutation HP. This layer pairs the actors via
/// PelipperWildEncounterIdentityService, reads/writes the source Pokemon's live HP conservatively,
/// and lets the existing MonsterMutationService keep ownership of rolls, stat scaling and minions.
/// </summary>
internal sealed class Alpha67448PelipperSourceMutationService
{
    public const string SourceMutantMarker = "Ronvotri.TeamUp/PelipperSourceMutant";
    public const string ExtraLifeMarker = "Ronvotri.TeamUp/PelipperMutantExtraLives";
    public const string LogicalMaxHpMarker = "Ronvotri.TeamUp/PelipperMutantLogicalMaxHp";
    public const string SourceHpMarker = "Ronvotri.TeamUp/PelipperSourceHpAccessor";
    public const string PhaseTotalMarker = "Ronvotri.TeamUp/MutationPhaseTotal";
    public const string PhaseCurrentMarker = "Ronvotri.TeamUp/MutationPhaseCurrent";
    public const string NoCaptureMarker = "Ronvotri.TeamUp/MutantLeaderNoCapture";

    private const int MaximumReasonableSourceHp = 100_000;

    private sealed class DamageAttemptState
    {
        public long Tick { get; set; } = -1;
        public bool CancelDamage { get; set; }
    }

    private sealed record MemberPath(MemberInfo[] Chain, string Label, int Score);

    private sealed record SourceHealthAccessor(MemberPath Current, MemberPath Maximum);

    private sealed record SourceHealthSnapshot(
        PelipperWildEncounterIdentity Identity,
        SourceHealthAccessor Accessor,
        int Current,
        int Maximum,
        string SourceLabel);

    private sealed record ProxySnapshot(
        int Health,
        int MaxHealth,
        string Name,
        string DisplayName,
        double? Scale);

    private sealed class MutationPatchState
    {
        public bool IsPelipper { get; init; }
        public bool Blocked { get; init; }
        public bool Force { get; init; }
        public SourceHealthSnapshot? SourceHealth { get; init; }
        public ProxySnapshot? Proxy { get; init; }
        public bool NameChanged { get; init; }
        public bool DisplayNameChanged { get; init; }
        public float HealthMultiplier { get; init; }
    }

    private sealed record ForceSummary(string DisplayName, int BaseMaxHp, int Bars, float Multiplier, long Tick);

    private static readonly ConditionalWeakTable<Monster, DamageAttemptState> DamageAttemptCache = new();
    private static readonly Dictionary<Type, SourceHealthAccessor?> HealthAccessorCache = new();
    private static readonly object HealthAccessorLock = new();
    private static readonly MethodInfo? TryMutateMethod = typeof(MonsterMutationService).GetMethod(
        "TryMutate",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static Alpha67448PelipperSourceMutationService? ActiveInstance;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly Func<float> _healthMultiplier;
    private readonly HashSet<MethodBase> _damageHooks = new();

    private long _sourceDamageCalls;
    private long _sourceHpResolved;
    private long _sourceHpUnresolved;
    private long _sourceLethalCandidates;
    private long _sourceMutationAttempts;
    private long _sourceMutationIntercepts;
    private long _sourceShinyExcluded;
    private long _sourceDuplicateSuppressed;
    private long _phaseGuards;
    private long _finalLethalPasses;
    private long _transformBlocked;
    private long _forceTransforms;
    private long _sourceAuraDraws;
    private string _lastLine = "reset";
    private ForceSummary? _lastForceSummary;

    public int PatchedDamageMethodCount => _damageHooks.Count;

    public Alpha67448PelipperSourceMutationService(IMonitor monitor, string uniqueId, Func<float> healthMultiplier)
    {
        _monitor = monitor;
        _healthMultiplier = healthMultiplier;
        _harmony = new Harmony($"{uniqueId}.Alpha67448PelipperSourceMutation");
        ActiveInstance = this;

        ApplyTryMutatePatch();
        ApplyForceResultPatch();
        ApplySourceAwareDamageHooks();
    }

    public string Describe()
        => $"Pelipper SOURCE mutation: sourceDamageCalls={_sourceDamageCalls} | hpResolved={_sourceHpResolved} | hpUnresolved={_sourceHpUnresolved} | "
            + $"sourceLethalCandidates={_sourceLethalCandidates} | mutationAttempts={_sourceMutationAttempts} | mutationIntercepts={_sourceMutationIntercepts} | "
            + $"shinyExcluded={_sourceShinyExcluded} | duplicateSuppressed={_sourceDuplicateSuppressed} | phaseGuards={_phaseGuards} | "
            + $"finalLethalPasses={_finalLethalPasses} | transformBlocked={_transformBlocked} | forceTransforms={_forceTransforms} | "
            + $"auraDraws={_sourceAuraDraws} | damageHooks={_damageHooks.Count} | last={_lastLine}";

    public void ResetTelemetry()
    {
        _sourceDamageCalls = 0;
        _sourceHpResolved = 0;
        _sourceHpUnresolved = 0;
        _sourceLethalCandidates = 0;
        _sourceMutationAttempts = 0;
        _sourceMutationIntercepts = 0;
        _sourceShinyExcluded = 0;
        _sourceDuplicateSuppressed = 0;
        _phaseGuards = 0;
        _finalLethalPasses = 0;
        _transformBlocked = 0;
        _forceTransforms = 0;
        _sourceAuraDraws = 0;
        _lastLine = "reset";
        _lastForceSummary = null;
    }

    public void DrawSourceAuras(SpriteBatch spriteBatch)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null || Game1.eventUp)
            return;

        double time = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        float pulse = 0.5f + 0.5f * MathF.Sin((float)(time / 160d));
        Color outer = new Color(190, 70, 255) * (0.34f + pulse * 0.26f);
        Color inner = new Color(255, 70, 105) * (0.40f + (1f - pulse) * 0.24f);

        foreach (Monster proxy in Game1.currentLocation.characters.OfType<Monster>().Where(MonsterMutationService.IsMutant))
        {
            if (!PelipperTownCompatibilityService.IsWildCombatActor(proxy)
                || !PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity)
                || ReferenceEquals(identity.SourceActor, proxy))
            {
                continue;
            }

            Rectangle world = identity.SourceActor.GetBoundingBox();
            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(world.X, world.Y));
            Rectangle screen = new((int)screenPos.X, (int)screenPos.Y, world.Width, world.Height);
            int pad1 = 12 + (int)Math.Round(pulse * 10f);
            int pad2 = pad1 + 11;
            DrawOutline(spriteBatch, Inflate(screen, pad2), 4, outer);
            DrawOutline(spriteBatch, Inflate(screen, pad1), 3, inner);
            _sourceAuraDraws++;
        }
    }

    private void ApplyTryMutatePatch()
    {
        if (TryMutateMethod is null)
        {
            _monitor.Log("6.7.44.8 source-aware Mutation patch skipped: TryMutate not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            TryMutateMethod,
            prefix: new HarmonyMethod(typeof(Alpha67448PelipperSourceMutationService), nameof(MutationPrefix)),
            postfix: new HarmonyMethod(typeof(Alpha67448PelipperSourceMutationService), nameof(MutationPostfix)));
    }

    private void ApplyForceResultPatch()
    {
        MethodInfo? force = typeof(MonsterMutationService).GetMethod(
            nameof(MonsterMutationService.ForceNearestEligible),
            BindingFlags.Instance | BindingFlags.Public);
        if (force is null)
            return;

        _harmony.Patch(
            force,
            postfix: new HarmonyMethod(typeof(Alpha67448PelipperSourceMutationService), nameof(ForceNearestEligiblePostfix)));
    }

    private void ApplySourceAwareDamageHooks()
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
                    if (!method.Name.Equals("takeDamage", StringComparison.Ordinal)
                        || method.IsAbstract
                        || method.IsStatic
                        || method.ContainsGenericParameters
                        || _damageHooks.Contains(method))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0 || parameters[0].ParameterType != typeof(int))
                        continue;

                    try
                    {
                        HarmonyMethod prefix = new(typeof(Alpha67448PelipperSourceMutationService), nameof(SourceAwareDamagePrefix))
                        {
                            priority = Priority.First
                        };
                        _harmony.Patch(method, prefix: prefix);
                        _damageHooks.Add(method);
                    }
                    catch (Exception ex)
                    {
                        _monitor.Log($"6.7.44.8 source HP hook skipped {type.FullName}.{method.Name}: {ex.GetType().Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }

        _monitor.Log($"Team Up 6.7.44.8 source-aware Pelipper Mutation patched {_damageHooks.Count} Monster.takeDamage implementation(s).", LogLevel.Info);
    }

    private static bool MutationPrefix(Monster __0, bool __1, ref bool __result, out MutationPatchState? __state)
    {
        __state = null;
        Alpha67448PelipperSourceMutationService? service = ActiveInstance;
        if (service is null || !PelipperTownCompatibilityService.IsWildCombatActor(__0))
            return true;

        if (!service.TryResolveSourceHealth(__0, out SourceHealthSnapshot health))
        {
            service._transformBlocked++;
            service._lastLine = $"transform-blocked sourceHP-unresolved proxy={__0.Name} force={__1}";
            __state = new MutationPatchState { IsPelipper = true, Blocked = true, Force = __1 };
            __result = false;
            return false;
        }

        float multiplier = Math.Clamp(service._healthMultiplier(), 1f, 10f);
        ProxySnapshot proxy = new(
            __0.Health,
            __0.MaxHealth,
            __0.Name ?? string.Empty,
            __0.displayName ?? string.Empty,
            TryReadNamedNumeric(__0, "Scale", out double scale) ? scale : null);

        // Feed the existing generic Mutation engine real Pokemon HP instead of Pelipper's technical
        // proxy sentinel. The proxy values are restored immediately in the postfix so Pelipper keeps
        // authority over its own runtime controller.
        __0.MaxHealth = Math.Max(1, health.Maximum);
        __0.Health = Math.Clamp(health.Current, 1, __0.MaxHealth);
        bool nameChanged = TryWriteStringMember(__0, "Name", health.Identity.DisplayName);
        bool displayNameChanged = TryWriteStringMember(__0, "displayName", health.Identity.DisplayName);

        __state = new MutationPatchState
        {
            IsPelipper = true,
            Force = __1,
            SourceHealth = health,
            Proxy = proxy,
            NameChanged = nameChanged,
            DisplayNameChanged = displayNameChanged,
            HealthMultiplier = multiplier
        };
        return true;
    }

    private static void MutationPostfix(Monster __0, bool __1, ref bool __result, MutationPatchState? __state)
    {
        Alpha67448PelipperSourceMutationService? service = ActiveInstance;
        if (service is null || __state is null || !__state.IsPelipper || __state.Blocked)
            return;

        SourceHealthSnapshot? health = __state.SourceHealth;
        ProxySnapshot? proxy = __state.Proxy;
        if (health is null || proxy is null)
            return;

        // Restore Pelipper's technical combat-proxy state. Keep Team Up Mutation markers/stat changes,
        // but do not replace Pelipper's sentinel HP or enlarge the invisible proxy footprint.
        __0.MaxHealth = proxy.MaxHealth;
        __0.Health = proxy.Health;
        if (proxy.Scale.HasValue)
            TryWriteNamedNumeric(__0, "Scale", proxy.Scale.Value);
        __0.modData[MonsterMutationService.MutationScaleMarker] = "1";
        if (__state.NameChanged)
            TryWriteStringMember(__0, "Name", proxy.Name);
        if (__state.DisplayNameChanged)
            TryWriteStringMember(__0, "displayName", proxy.DisplayName);

        if (!__result)
            return;

        int bars = Math.Max(1, (int)Math.Round(__state.HealthMultiplier, MidpointRounding.AwayFromZero));
        int extraLives = Math.Max(0, bars - 1);
        int logicalMaxHp = SafeScaledInt(health.Maximum, __state.HealthMultiplier, 1, 2_000_000);

        if (!TryWritePathNumeric(health.Identity.SourceActor, health.Accessor.Current, health.Maximum))
        {
            // Mutation already succeeded in the generic engine, so fail safely by keeping one normal
            // source HP bar rather than corrupting Pelipper state. Telemetry makes this visible.
            extraLives = 0;
            service._lastLine = $"mutated-but-sourceHP-write-failed {health.Identity.DisplayName} via={health.SourceLabel}";
        }

        __0.modData[ExtraLifeMarker] = extraLives.ToString(System.Globalization.CultureInfo.InvariantCulture);
        __0.modData[LogicalMaxHpMarker] = logicalMaxHp.ToString(System.Globalization.CultureInfo.InvariantCulture);
        __0.modData[SourceHpMarker] = health.SourceLabel;
        __0.modData[PhaseTotalMarker] = bars.ToString(System.Globalization.CultureInfo.InvariantCulture);
        __0.modData[PhaseCurrentMarker] = "1";
        __0.modData[NoCaptureMarker] = "true";
        health.Identity.SourceActor.modData[SourceMutantMarker] = health.Identity.EncounterId;
        health.Identity.SourceActor.modData[NoCaptureMarker] = "true";

        service._lastLine = $"mutated source={health.Identity.DisplayName} hp={health.Maximum}/{health.Maximum} logicalHP={logicalMaxHp} bars={bars} extraLives={extraLives} via={health.SourceLabel} force={__1}";
        if (__1)
        {
            service._forceTransforms++;
            service._lastForceSummary = new ForceSummary(
                health.Identity.DisplayName,
                health.Maximum,
                bars,
                __state.HealthMultiplier,
                Game1.ticks);
        }

        // If Character.Name could not be changed temporarily, the generic engine may have emitted a
        // Green Slime message. Emit the correct source-aware line last so the player sees the Pokemon.
        if (!__state.NameChanged && !Game1.eventUp)
            Game1.showGlobalMessage($"⚠ MUTATION DETECTED • {health.Identity.DisplayName}");
    }

    private static void ForceNearestEligiblePostfix(bool __result, ref string __0)
    {
        Alpha67448PelipperSourceMutationService? service = ActiveInstance;
        ForceSummary? summary = service?._lastForceSummary;
        if (!__result || summary is null || Game1.ticks - summary.Tick > 2)
            return;

        __0 = $"Forced mutation: {summary.DisplayName} -> source HP {summary.BaseMaxHp}/{summary.BaseMaxHp}, HPx{summary.Multiplier:0.##} ({summary.Bars} phase(s)).";
    }

    private static void SourceAwareDamagePrefix(Monster __instance, object[] __args)
    {
        Alpha67448PelipperSourceMutationService? service = ActiveInstance;
        if (service is null || !Context.IsWorldReady || !Context.IsMainPlayer || __args.Length == 0
            || __args[0] is not int incoming || incoming <= 0
            || !PelipperTownCompatibilityService.IsWildCombatActor(__instance))
        {
            return;
        }

        service._sourceDamageCalls++;
        if (!service.TryResolveSourceHealth(__instance, out SourceHealthSnapshot health))
        {
            service._sourceHpUnresolved++;
            service._lastLine = $"sourceHP-unresolved proxy={__instance.Name} incoming={incoming}";
            return;
        }

        service._sourceHpResolved++;
        if (incoming < health.Current || health.Current <= 0)
            return;

        DamageAttemptState stamp = DamageAttemptCache.GetOrCreateValue(__instance);
        if (stamp.Tick == Game1.ticks)
        {
            service._sourceDuplicateSuppressed++;
            if (stamp.CancelDamage)
                __args[0] = 0;
            return;
        }

        stamp.Tick = Game1.ticks;
        stamp.CancelDamage = false;
        service._sourceLethalCandidates++;

        if (MonsterMutationService.IsMutant(__instance))
        {
            int extraLives = ReadIntMarker(__instance, ExtraLifeMarker);
            if (extraLives > 0)
            {
                if (TryWritePathNumeric(health.Identity.SourceActor, health.Accessor.Current, health.Maximum))
                {
                    extraLives--;
                    __instance.modData[ExtraLifeMarker] = extraLives.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    int totalPhases = Math.Max(1, ReadIntMarker(__instance, PhaseTotalMarker));
                    int currentPhase = Math.Clamp(totalPhases - extraLives, 1, totalPhases);
                    __instance.modData[PhaseCurrentMarker] = currentPhase.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    __args[0] = 0;
                    stamp.CancelDamage = true;
                    service._phaseGuards++;
                    service._lastLine = $"mutant-phase-guard source={health.Identity.DisplayName} restored={health.Maximum}/{health.Maximum} extraLives={extraLives} incoming={incoming}";
                }
                else
                {
                    service._lastLine = $"mutant-phase-write-failed source={health.Identity.DisplayName} incoming={incoming}";
                }
                return;
            }

            int finalTotal = Math.Max(1, ReadIntMarker(__instance, PhaseTotalMarker));
            __instance.modData[PhaseCurrentMarker] = finalTotal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            service._finalLethalPasses++;
            service._lastLine = $"mutant-final-lethal source={health.Identity.DisplayName} phase={finalTotal}/{finalTotal} hp={health.Current}/{health.Maximum} incoming={incoming}";
            return;
        }

        if (MonsterMutationService.IsMutationMinion(__instance))
            return;

        if (EncounterReactionService.IsConfirmedShiny(__instance)
            || EncounterReactionService.HasConfirmedPelipperShinyEvidence(__instance))
        {
            service._sourceShinyExcluded++;
            service._lastLine = $"source-lethal-shiny-excluded source={health.Identity.DisplayName} hp={health.Current}/{health.Maximum} incoming={incoming}";
            return;
        }

        service._sourceMutationAttempts++;
        MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);
        MonsterMutationService? mutation = MonsterMutationService.ActiveInstance;
        if (mutation is null || TryMutateMethod is null)
            return;

        try
        {
            bool mutated = TryMutateMethod.Invoke(mutation, new object[] { __instance, false }) is true;
            if (!mutated)
            {
                service._lastLine = $"source-lethal-roll-no-mutation source={health.Identity.DisplayName} hp={health.Current}/{health.Maximum} incoming={incoming}";
                return;
            }

            __args[0] = 0;
            stamp.CancelDamage = true;
            service._sourceMutationIntercepts++;
            service._lastLine = $"source-lethal-mutated source={health.Identity.DisplayName} hp={health.Current}/{health.Maximum} incoming={incoming}";
        }
        catch (Exception ex)
        {
            service._lastLine = $"source-lethal-exception {ex.GetType().Name}: {ex.Message}";
        }
    }

    private bool TryResolveSourceHealth(Monster proxy, out SourceHealthSnapshot snapshot)
    {
        snapshot = null!;
        if (!PelipperWildEncounterIdentityService.TryResolve(proxy, out PelipperWildEncounterIdentity identity)
            || ReferenceEquals(identity.SourceActor, proxy))
        {
            return false;
        }

        NPC source = identity.SourceActor;
        SourceHealthAccessor? accessor;
        lock (HealthAccessorLock)
        {
            if (!HealthAccessorCache.TryGetValue(source.GetType(), out accessor))
            {
                accessor = DiscoverHealthAccessor(source);
                HealthAccessorCache[source.GetType()] = accessor;
            }
        }

        if (accessor is null
            || !TryReadPathNumeric(source, accessor.Current, out int current)
            || !TryReadPathNumeric(source, accessor.Maximum, out int maximum)
            || maximum <= 0
            || maximum > MaximumReasonableSourceHp
            || current < 0
            || current > maximum)
        {
            // A cached accessor can become invalid if Pelipper swaps a nested runtime state object.
            lock (HealthAccessorLock)
                HealthAccessorCache.Remove(source.GetType());
            accessor = DiscoverHealthAccessor(source);
            lock (HealthAccessorLock)
                HealthAccessorCache[source.GetType()] = accessor;

            if (accessor is null
                || !TryReadPathNumeric(source, accessor.Current, out current)
                || !TryReadPathNumeric(source, accessor.Maximum, out maximum)
                || maximum <= 0
                || maximum > MaximumReasonableSourceHp
                || current < 0
                || current > maximum)
            {
                return false;
            }
        }

        string label = $"{accessor.Current.Label}/{accessor.Maximum.Label}";
        snapshot = new SourceHealthSnapshot(identity, accessor, current, maximum, label);
        return true;
    }

    private static SourceHealthAccessor? DiscoverHealthAccessor(NPC source)
    {
        List<MemberPath> current = new();
        List<MemberPath> maximum = new();
        Type sourceType = source.GetType();

        foreach (MemberInfo member in EnumerateReadableMembers(sourceType))
        {
            string normalized = Normalize(member.Name);
            int currentScore = ScoreCurrentHealth(normalized);
            int maxScore = ScoreMaxHealth(normalized);
            if (currentScore > 0)
                current.Add(new MemberPath(new[] { member }, $"{sourceType.Name}.{member.Name}", currentScore));
            if (maxScore > 0)
                maximum.Add(new MemberPath(new[] { member }, $"{sourceType.Name}.{member.Name}", maxScore));

            Type? nestedType = GetMemberType(member);
            if (nestedType is null || IsSimpleType(nestedType) || !LooksLikeHealthContainer(normalized))
                continue;

            foreach (MemberInfo nested in EnumerateReadableMembers(nestedType))
            {
                string nestedName = Normalize(nested.Name);
                int nestedCurrentScore = ScoreCurrentHealth(nestedName);
                int nestedMaxScore = ScoreMaxHealth(nestedName);
                if (nestedCurrentScore > 0)
                    current.Add(new MemberPath(new[] { member, nested }, $"{sourceType.Name}.{member.Name}.{nested.Name}", nestedCurrentScore + 20));
                if (nestedMaxScore > 0)
                    maximum.Add(new MemberPath(new[] { member, nested }, $"{sourceType.Name}.{member.Name}.{nested.Name}", nestedMaxScore + 20));
            }
        }

        foreach (MemberPath cur in current.OrderByDescending(path => path.Score).Take(12))
        {
            if (!TryReadPathNumeric(source, cur, out int currentValue))
                continue;

            foreach (MemberPath max in maximum.OrderByDescending(path => path.Score).Take(12))
            {
                if (cur.Label.Equals(max.Label, StringComparison.OrdinalIgnoreCase)
                    || !TryReadPathNumeric(source, max, out int maxValue)
                    || maxValue <= 0
                    || maxValue > MaximumReasonableSourceHp
                    || currentValue < 0
                    || currentValue > maxValue
                    || !CanWritePath(source, cur))
                {
                    continue;
                }

                return new SourceHealthAccessor(cur, max);
            }
        }

        return null;
    }

    private static IEnumerable<MemberInfo> EnumerateReadableMembers(Type type)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        Type? cursor = type;
        while (cursor is not null && cursor != typeof(object))
        {
            foreach (FieldInfo field in cursor.GetFields(flags))
                yield return field;
            foreach (PropertyInfo property in cursor.GetProperties(flags))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0)
                    yield return property;
            }
            cursor = cursor.BaseType;
        }
    }

    private static int ScoreCurrentHealth(string name)
    {
        if (name.Contains("max") || name.Contains("maximum") || name.Contains("base")
            || name.Contains("bonus") || name.Contains("regen") || name.Contains("chance")
            || name.Contains("threshold") || name.Contains("percent"))
        {
            return -1;
        }

        if (name is "currenthp" or "currenthealth") return 240;
        if (name.Contains("currenthp") || name.Contains("currenthealth")) return 225;
        if (name is "hp" or "health") return 210;
        if (name.Contains("battlehp") || name.Contains("remaininghp")) return 205;
        if (name.Contains("hitpoints") || name.EndsWith("hp", StringComparison.Ordinal)) return 185;
        if (name.EndsWith("health", StringComparison.Ordinal)) return 180;
        return -1;
    }

    private static int ScoreMaxHealth(string name)
    {
        if (name.Contains("bonus") || name.Contains("regen") || name.Contains("chance")
            || name.Contains("threshold") || name.Contains("percent"))
        {
            return -1;
        }

        if (name is "maxhp" or "maxhealth" or "maximumhp" or "maximumhealth") return 250;
        if (name.Contains("maxhp") || name.Contains("maxhealth") || name.Contains("maximumhp") || name.Contains("maximumhealth")) return 235;
        if (name.Contains("totalhp") || name.Contains("totalhealth")) return 195;
        return -1;
    }

    private static bool LooksLikeHealthContainer(string name)
        => name.Contains("battle") || name.Contains("combat") || name.Contains("stat")
            || name.Contains("state") || name.Contains("pokemon") || name.Contains("pokémon")
            || name.Contains("wild") || name.Contains("encounter") || name.Contains("health")
            || name.Contains("hp");

    private static bool TryReadPathNumeric(object root, MemberPath path, out int value)
    {
        value = 0;
        object? cursor = root;
        foreach (MemberInfo member in path.Chain)
        {
            if (cursor is null || !TryGetMemberValue(cursor, member, out cursor))
                return false;
        }

        if (!TryConvertNumeric(cursor, out double number)
            && !(cursor is not null && TryReadWrappedValue(cursor, out number)))
        {
            return false;
        }

        if (double.IsNaN(number) || double.IsInfinity(number))
            return false;
        value = (int)Math.Round(number, MidpointRounding.AwayFromZero);
        return true;
    }

    private static bool TryWritePathNumeric(object root, MemberPath path, int value)
    {
        object? cursor = root;
        for (int i = 0; i < path.Chain.Length - 1; i++)
        {
            if (cursor is null || !TryGetMemberValue(cursor, path.Chain[i], out cursor))
                return false;
        }

        if (cursor is null)
            return false;

        MemberInfo final = path.Chain[^1];
        if (TrySetNumericMember(cursor, final, value))
            return true;
        if (!TryGetMemberValue(cursor, final, out object? wrapper) || wrapper is null)
            return false;
        return TrySetWrappedValue(wrapper, value);
    }

    private static bool CanWritePath(object root, MemberPath path)
    {
        if (!TryReadPathNumeric(root, path, out int value))
            return false;
        return TryWritePathNumeric(root, path, value);
    }

    private static bool TryGetMemberValue(object target, MemberInfo member, out object? value)
    {
        value = null;
        try
        {
            switch (member)
            {
                case FieldInfo field:
                    value = field.GetValue(target);
                    return true;
                case PropertyInfo property when property.CanRead && property.GetIndexParameters().Length == 0:
                    value = property.GetValue(target);
                    return true;
            }
        }
        catch
        {
            // Runtime compatibility probing must fail closed.
        }
        return false;
    }

    private static bool TrySetNumericMember(object target, MemberInfo member, double value)
    {
        try
        {
            Type memberType;
            switch (member)
            {
                case FieldInfo field when !field.IsInitOnly:
                    memberType = field.FieldType;
                    if (!TryConvertForType(value, memberType, out object? fieldValue))
                        return false;
                    field.SetValue(target, fieldValue);
                    return true;
                case PropertyInfo property when property.CanWrite && property.GetIndexParameters().Length == 0:
                    memberType = property.PropertyType;
                    if (!TryConvertForType(value, memberType, out object? propertyValue))
                        return false;
                    property.SetValue(target, propertyValue);
                    return true;
            }
        }
        catch
        {
            // Fail closed.
        }
        return false;
    }

    private static bool TryReadWrappedValue(object wrapper, out double value)
    {
        value = 0d;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        try
        {
            PropertyInfo? property = wrapper.GetType().GetProperty("Value", flags);
            if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0
                && TryConvertNumeric(property.GetValue(wrapper), out value))
            {
                return true;
            }
            FieldInfo? field = wrapper.GetType().GetField("Value", flags);
            return field is not null && TryConvertNumeric(field.GetValue(wrapper), out value);
        }
        catch
        {
            return false;
        }
    }

    private static bool TrySetWrappedValue(object wrapper, double value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        try
        {
            PropertyInfo? property = wrapper.GetType().GetProperty("Value", flags);
            if (property is not null && property.CanWrite && property.GetIndexParameters().Length == 0
                && TryConvertForType(value, property.PropertyType, out object? propertyValue))
            {
                property.SetValue(wrapper, propertyValue);
                return true;
            }
            FieldInfo? field = wrapper.GetType().GetField("Value", flags);
            if (field is not null && !field.IsInitOnly && TryConvertForType(value, field.FieldType, out object? fieldValue))
            {
                field.SetValue(wrapper, fieldValue);
                return true;
            }
        }
        catch
        {
            // Fail closed.
        }
        return false;
    }

    private static Type? GetMemberType(MemberInfo member)
        => member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo property => property.PropertyType,
            _ => null
        };

    private static bool IsSimpleType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(Vector2) || type == typeof(Rectangle);
    }

    private static bool TryConvertNumeric(object? raw, out double number)
    {
        if (raw is null)
        {
            number = 0d;
            return false;
        }
        try
        {
            TypeCode code = Type.GetTypeCode(Nullable.GetUnderlyingType(raw.GetType()) ?? raw.GetType());
            if (code is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
                or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64
                or TypeCode.Single or TypeCode.Double or TypeCode.Decimal)
            {
                number = Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
        }
        catch
        {
            // Not numeric.
        }
        number = 0d;
        return false;
    }

    private static bool TryConvertForType(double value, Type targetType, out object? converted)
    {
        try
        {
            Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (type == typeof(int)) converted = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            else if (type == typeof(float)) converted = (float)value;
            else if (type == typeof(double)) converted = value;
            else if (type == typeof(long)) converted = (long)Math.Round(value, MidpointRounding.AwayFromZero);
            else if (type == typeof(short)) converted = (short)Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue);
            else if (type == typeof(byte)) converted = (byte)Math.Clamp(Math.Round(value), byte.MinValue, byte.MaxValue);
            else if (type == typeof(uint)) converted = (uint)Math.Max(0, Math.Round(value));
            else if (type == typeof(ushort)) converted = (ushort)Math.Clamp(Math.Round(value), ushort.MinValue, ushort.MaxValue);
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

    private static bool TryReadNamedNumeric(object target, string name, out double value)
    {
        value = 0d;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type? cursor = target.GetType();
        while (cursor is not null)
        {
            try
            {
                FieldInfo? field = cursor.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field is not null)
                {
                    object? raw = field.GetValue(target);
                    return TryConvertNumeric(raw, out value) || (raw is not null && TryReadWrappedValue(raw, out value));
                }
                PropertyInfo? property = cursor.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property is not null && property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    object? raw = property.GetValue(target);
                    return TryConvertNumeric(raw, out value) || (raw is not null && TryReadWrappedValue(raw, out value));
                }
            }
            catch
            {
                // Continue through base types.
            }
            cursor = cursor.BaseType;
        }
        return false;
    }

    private static bool TryWriteNamedNumeric(object target, string name, double value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type? cursor = target.GetType();
        while (cursor is not null)
        {
            try
            {
                FieldInfo? field = cursor.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field is not null)
                {
                    if (TrySetNumericMember(target, field, value))
                        return true;
                    object? wrapper = field.GetValue(target);
                    if (wrapper is not null && TrySetWrappedValue(wrapper, value))
                        return true;
                }
                PropertyInfo? property = cursor.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property is not null)
                {
                    if (TrySetNumericMember(target, property, value))
                        return true;
                    object? wrapper = property.CanRead ? property.GetValue(target) : null;
                    if (wrapper is not null && TrySetWrappedValue(wrapper, value))
                        return true;
                }
            }
            catch
            {
                // Continue through base types.
            }
            cursor = cursor.BaseType;
        }
        return false;
    }

    private static bool TryWriteStringMember(object target, string name, string value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type? cursor = target.GetType();
        while (cursor is not null)
        {
            try
            {
                FieldInfo? field = cursor.GetField(name, flags | BindingFlags.DeclaredOnly);
                if (field is not null && !field.IsInitOnly && field.FieldType == typeof(string))
                {
                    field.SetValue(target, value);
                    return true;
                }
                PropertyInfo? property = cursor.GetProperty(name, flags | BindingFlags.DeclaredOnly);
                if (property is not null && property.CanWrite && property.PropertyType == typeof(string)
                    && property.GetIndexParameters().Length == 0)
                {
                    property.SetValue(target, value);
                    return true;
                }
            }
            catch
            {
                // Continue through base types.
            }
            cursor = cursor.BaseType;
        }
        return false;
    }

    private static int ReadIntMarker(Monster monster, string key)
        => monster.modData.TryGetValue(key, out string? raw)
            && int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int value)
                ? Math.Max(0, value)
                : 0;

    private static int SafeScaledInt(int value, float multiplier, int minimum, int maximum)
    {
        double scaled = Math.Round(value * (double)multiplier, MidpointRounding.AwayFromZero);
        return (int)Math.Clamp(scaled, minimum, maximum);
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

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

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(type => type is not null).Cast<Type>(); }
        catch { return Array.Empty<Type>(); }
    }
}