using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.13: the generic Mutation engine scales the hidden Pelipper Monster proxy, which
/// does not affect the Pokemon sprite the player actually sees. Mirror Mutation scale onto the
/// paired PokemonNpc source and restore its original presentation scale as soon as the encounter
/// stops being an active wild Mutant.
/// </summary>
internal sealed class Alpha674413PelipperVisibleMutationService
{
    private const string WildRoleKey = "Griff.PelipperTown/PokemonNpcRole/v1";
    private const string WildRoleValue = "WildEncounter";

    private sealed class VisualState
    {
        public Monster Proxy { get; init; } = null!;
        public string MemberName { get; init; } = string.Empty;
        public double OriginalValue { get; init; }
        public double AppliedValue { get; init; }
        public string DisplayName { get; init; } = string.Empty;
    }

    private static Alpha674413PelipperVisibleMutationService? Active;

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly Func<float> _visualScaleMultiplier;
    private readonly Dictionary<NPC, VisualState> _tracked = new();

    private long _applied;
    private long _reapplied;
    private long _restored;
    private long _failed;
    private string _last = "reset";

    public Alpha674413PelipperVisibleMutationService(
        IMonitor monitor,
        string uniqueId,
        Func<float> visualScaleMultiplier)
    {
        _monitor = monitor;
        _visualScaleMultiplier = visualScaleMultiplier;
        _harmony = new Harmony($"{uniqueId}.Alpha674413PelipperVisibleMutation");
        Active = this;

        MethodInfo? tryMutate = typeof(MonsterMutationService).GetMethod(
            "TryMutate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (tryMutate is null)
        {
            _monitor.Log("6.7.44.13 visible Pelipper Mutation scale unavailable: TryMutate not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            tryMutate,
            postfix: new HarmonyMethod(typeof(Alpha674413PelipperVisibleMutationService), nameof(TryMutatePostfix))
            {
                priority = Priority.Last
            });

        _monitor.Log("Team Up 6.7.44.13 visible Pelipper Mutation scaling enabled.", LogLevel.Info);
    }

    public string Describe()
        => $"Pelipper visible Mutation: tracked={_tracked.Count} | applied={_applied} | reapplied={_reapplied} | restored={_restored} | failed={_failed} | last={_last}";

    public void ResetTelemetry()
    {
        RestoreAll();
        _applied = 0;
        _reapplied = 0;
        _restored = 0;
        _failed = 0;
        _last = "reset";
    }

    public void Update()
    {
        if (_tracked.Count == 0)
            return;

        foreach ((NPC source, VisualState state) in _tracked.ToArray())
        {
            bool active = Context.IsWorldReady
                && source.currentLocation is not null
                && ReferenceEquals(source.currentLocation, state.Proxy.currentLocation)
                && source.currentLocation.characters.Contains(source)
                && source.currentLocation.characters.Contains(state.Proxy)
                && MonsterMutationService.IsMutant(state.Proxy)
                && PelipperTownCompatibilityService.IsWildCombatActor(state.Proxy)
                && source.modData.TryGetValue(WildRoleKey, out string? role)
                && role.Equals(WildRoleValue, StringComparison.OrdinalIgnoreCase);

            if (!active)
            {
                Restore(source, state);
                _tracked.Remove(source);
                continue;
            }

            if (!TryReadNumeric(source, state.MemberName, out double current))
                continue;
            if (Math.Abs(current - state.AppliedValue) <= 0.001d)
                continue;

            if (TryWriteNumeric(source, state.MemberName, state.AppliedValue))
            {
                _reapplied++;
                _last = $"reapplied source={state.DisplayName} member={state.MemberName} scale={state.AppliedValue:0.###}";
            }
        }
    }

    private static void TryMutatePostfix(Monster __0, bool __1, bool __result)
    {
        Alpha674413PelipperVisibleMutationService? service = Active;
        if (service is null || !__result || !Context.IsWorldReady)
            return;
        if (!PelipperTownCompatibilityService.IsWildCombatActor(__0))
            return;
        if (!PelipperWildEncounterIdentityService.TryResolve(__0, out PelipperWildEncounterIdentity identity)
            || ReferenceEquals(identity.SourceActor, __0))
        {
            service._failed++;
            service._last = $"scale-failed identity-unresolved proxy={__0.Name}";
            return;
        }

        NPC source = identity.SourceActor;
        if (service._tracked.ContainsKey(source))
            return;

        float multiplier = Math.Clamp(service._visualScaleMultiplier(), 1f, 5f);
        if (multiplier <= 1.001f)
        {
            service._last = $"scale-skipped source={identity.DisplayName} multiplier={multiplier:0.###}";
            return;
        }

        foreach (string member in new[] { "_visualScaleMultiplier", "visualScaleMultiplier", "_drawScale", "drawScale" })
        {
            if (!TryReadNumeric(source, member, out double original)
                || original <= 0d
                || original > 16d)
            {
                continue;
            }

            double applied = Math.Clamp(original * multiplier, 0.25d, 24d);
            if (!TryWriteNumeric(source, member, applied))
                continue;

            service._tracked[source] = new VisualState
            {
                Proxy = __0,
                MemberName = member,
                OriginalValue = original,
                AppliedValue = applied,
                DisplayName = identity.DisplayName
            };
            service._applied++;
            service._last = $"scaled source={identity.DisplayName} member={member} {original:0.###}->{applied:0.###} force={__1}";
            return;
        }

        service._failed++;
        service._last = $"scale-failed source={identity.DisplayName} no-writable-scale-member";
    }

    private void RestoreAll()
    {
        foreach ((NPC source, VisualState state) in _tracked.ToArray())
            Restore(source, state);
        _tracked.Clear();
    }

    private void Restore(NPC source, VisualState state)
    {
        if (!TryWriteNumeric(source, state.MemberName, state.OriginalValue))
            return;
        _restored++;
        _last = $"restored source={state.DisplayName} member={state.MemberName} scale={state.OriginalValue:0.###}";
    }

    private static bool TryReadNumeric(object target, string name, out double value)
    {
        value = 0d;
        if (!TryGetMember(target, name, out MemberInfo? member) || member is null)
            return false;
        try
        {
            object? raw = member switch
            {
                FieldInfo field => field.GetValue(target),
                PropertyInfo property when property.CanRead => property.GetValue(target),
                _ => null
            };
            if (raw is null)
                return false;
            value = Convert.ToDouble(raw, System.Globalization.CultureInfo.InvariantCulture);
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryWriteNumeric(object target, string name, double value)
    {
        if (!TryGetMember(target, name, out MemberInfo? member) || member is null)
            return false;
        try
        {
            switch (member)
            {
                case FieldInfo field when !field.IsInitOnly:
                    field.SetValue(target, ConvertNumeric(value, field.FieldType));
                    return true;
                case PropertyInfo property when property.CanWrite:
                    property.SetValue(target, ConvertNumeric(value, property.PropertyType));
                    return true;
            }
        }
        catch
        {
            // Best-effort presentation compatibility.
        }
        return false;
    }

    private static bool TryGetMember(object target, string name, out MemberInfo? member)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, flags | BindingFlags.DeclaredOnly);
            if (field is not null)
            {
                member = field;
                return true;
            }
            PropertyInfo? property = type.GetProperty(name, flags | BindingFlags.DeclaredOnly);
            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                member = property;
                return true;
            }
        }
        member = null;
        return false;
    }

    private static object ConvertNumeric(double value, Type targetType)
    {
        Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (type == typeof(float)) return (float)value;
        if (type == typeof(double)) return value;
        if (type == typeof(int)) return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        if (type == typeof(long)) return (long)Math.Round(value, MidpointRounding.AwayFromZero);
        if (type == typeof(short)) return (short)Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue);
        if (type == typeof(byte)) return (byte)Math.Clamp(Math.Round(value), byte.MinValue, byte.MaxValue);
        throw new InvalidOperationException($"Unsupported numeric member type {targetType.FullName}.");
    }
}
