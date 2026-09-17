using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.27 regression guard for non-Pelipper Mutation followers.
///
/// The source-native 6.7.44.18 wave service owns spawning, while the historical factory still has
/// an unrelated GreenSlime fallback branch. This guard takes authority at the factory boundary and
/// reproduces only the proven safe same-runtime constructor path. Unsupported/custom sources fail
/// closed, and Pelipper remains native-provider-only. The legacy factory body never runs while this
/// service is active, so its fallback object is never created at runtime.
/// </summary>
internal sealed class Alpha674427MutationRegressionGuardService
{
    private static Alpha674427MutationRegressionGuardService? Active { get; set; }

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;

    private long _factoryCalls;
    private long _sameRuntimeAllowed;
    private long _unsupportedBlocked;
    private long _pelipperBlocked;
    private long _greenSlimeFallbackPrevented;
    private long _constructorFailures;
    private string _last = "reset";

    public Alpha674427MutationRegressionGuardService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony(uniqueId + ".Alpha674427MutationRegressionGuard");
        Active = this;

        MethodInfo? create = AccessTools.Method(typeof(MonsterMutationMinionFactory), nameof(MonsterMutationMinionFactory.Create));
        if (create is null)
        {
            _monitor.Log("6.7.44.27 Mutation regression guard unavailable: minion factory Create() not found.", LogLevel.Error);
            return;
        }

        _harmony.Patch(
            create,
            prefix: new HarmonyMethod(typeof(Alpha674427MutationRegressionGuardService), nameof(BeforeFactoryCreate))
            {
                priority = Priority.First
            });

        _monitor.Log(
            "Team Up 6.7.44.27 Mutation regression guard enabled: vanilla/custom followers use safe source-equivalent runtime construction only; Pelipper stays native-provider-only; legacy unrelated GreenSlime fallback never executes.",
            LogLevel.Info);
    }

    public string Describe()
        => $"Mutation regression guard: source-equivalent-only | factoryCalls={_factoryCalls} | sameRuntimeAllowed={_sameRuntimeAllowed} | "
            + $"unsupportedBlocked={_unsupportedBlocked} | pelipperBlocked={_pelipperBlocked} | greenSlimeFallbackPrevented={_greenSlimeFallbackPrevented} | "
            + $"constructorFailures={_constructorFailures} | legacyFactoryOriginalRuns=0 | last={_last}";

    public void ResetTelemetry()
    {
        _factoryCalls = 0;
        _sameRuntimeAllowed = 0;
        _unsupportedBlocked = 0;
        _pelipperBlocked = 0;
        _greenSlimeFallbackPrevented = 0;
        _constructorFailures = 0;
        _last = "reset";
    }

    private static bool BeforeFactoryCreate(
        Monster source,
        Vector2 position,
        int baseMaxHealth,
        int baseDamage,
        int baseSpeed,
        ref Monster __result,
        ref string mode)
    {
        Alpha674427MutationRegressionGuardService? service = Active;
        if (service is null)
            return true;

        service._factoryCalls++;

        // Pelipper followers belong to the native pokemon_spawn provider. Reaching this factory is
        // itself a regression, so fail closed rather than constructing a synthetic combat actor.
        if (PelipperTownCompatibilityService.IsWildCombatActor(source))
        {
            mode = "pelipper-native-required";
            __result = null!;
            service._pelipperBlocked++;
            service._greenSlimeFallbackPrevented++;
            service._last = $"blocked Pelipper factory path source={ReadType(source)} provider=pelipper-native-required";
            return false;
        }

        if (service.TryCreateSameRuntimeType(
                source,
                position,
                baseMaxHealth,
                baseDamage,
                baseSpeed,
                out Monster? candidate,
                out string detail)
            && candidate is not null)
        {
            __result = candidate;
            mode = "same-runtime-type";
            service._sameRuntimeAllowed++;
            service._last = $"allowed same-runtime source={ReadType(source)} result={ReadType(candidate)} via={detail}";
            return false;
        }

        mode = "unsupported-fail-closed";
        __result = null!;
        service._unsupportedBlocked++;
        service._greenSlimeFallbackPrevented++;
        service._last = $"blocked unsupported source={ReadType(source)} reason={detail}";
        return false;
    }

    private bool TryCreateSameRuntimeType(
        Monster source,
        Vector2 position,
        int baseMaxHealth,
        int baseDamage,
        int baseSpeed,
        out Monster? monster,
        out string detail)
    {
        monster = null;
        Type type = source.GetType();
        if (type.IsAbstract || !typeof(Monster).IsAssignableFrom(type))
        {
            detail = "invalid-runtime-type";
            return false;
        }

        ConstructorInfo[] constructors;
        try
        {
            constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        catch
        {
            detail = "constructor-reflection-failed";
            return false;
        }

        ConstructorInfo? positionOnly = constructors.FirstOrDefault(ctor =>
        {
            ParameterInfo[] parameters = ctor.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(Vector2);
        });

        if (positionOnly is not null)
        {
            if (TryInvoke(positionOnly, new object[] { position }, out monster) && monster is not null)
            {
                NormalizeNormalStats(monster, baseMaxHealth, baseDamage, baseSpeed);
                detail = "position-only";
                return true;
            }
            _constructorFailures++;
        }

        foreach (ConstructorInfo constructor in constructors)
        {
            ParameterInfo[] parameters = constructor.GetParameters();
            if (parameters.Length != 2
                || parameters[0].ParameterType != typeof(Vector2)
                || parameters[1].ParameterType != typeof(int))
            {
                continue;
            }

            if (!LooksLikeLevelParameter(parameters[1].Name ?? string.Empty))
                continue;

            int level = Math.Clamp(20 + Math.Max(1, baseMaxHealth) / 3, 1, 999);
            if (TryInvoke(constructor, new object[] { position, level }, out monster) && monster is not null)
            {
                NormalizeNormalStats(monster, baseMaxHealth, baseDamage, baseSpeed);
                detail = "position-level";
                return true;
            }
            _constructorFailures++;
        }

        detail = positionOnly is null ? "no-safe-constructor" : "safe-constructor-invocation-failed";
        return false;
    }

    private static bool LooksLikeLevelParameter(string name)
    {
        string normalized = Normalize(name);
        return normalized.Contains("level", StringComparison.Ordinal)
            || normalized.Contains("mine", StringComparison.Ordinal)
            || normalized.Contains("difficulty", StringComparison.Ordinal)
            || normalized.Contains("depth", StringComparison.Ordinal);
    }

    private static bool TryInvoke(ConstructorInfo constructor, object[] args, out Monster? monster)
    {
        try
        {
            monster = constructor.Invoke(args) as Monster;
            return monster is not null;
        }
        catch
        {
            monster = null;
            return false;
        }
    }

    private static void NormalizeNormalStats(Monster monster, int baseMaxHealth, int baseDamage, int baseSpeed)
    {
        int health = Math.Clamp(Math.Max(1, baseMaxHealth), 1, 2_000_000);
        monster.MaxHealth = health;
        monster.Health = health;
        monster.Speed = Math.Clamp(Math.Max(1, baseSpeed), 1, 12);
        TryWriteNumericMember(monster, Math.Clamp(Math.Max(1, baseDamage), 1, 100_000), "DamageToFarmer", "damageToFarmer");
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
                // Best-effort compatibility. Continue through base types/candidate member names.
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
                // Best-effort compatibility.
            }
            type = type.BaseType;
        }
        return false;
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

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string ReadType(Monster? monster)
        => monster?.GetType().FullName ?? "null";
}
