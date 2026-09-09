using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 6.7.20 mutation-minion factory.
///
/// A minion should look and behave like the normal monster that mutated whenever the runtime type
/// exposes a constructor shape we can call without guessing opaque third-party state. We therefore
/// accept only a position-only constructor, or a position + recognized level/difficulty integer.
/// Unknown custom constructor shapes fail closed to the existing GreenSlime fallback instead of
/// reflection-cloning NetFields or invoking arbitrary constructors with fabricated arguments.
/// </summary>
internal static class MonsterMutationMinionFactory
{
    public static int SameTypeSpawned { get; private set; }
    public static int FallbackSpawned { get; private set; }
    public static int SameTypeFailures { get; private set; }

    public static void ResetTelemetry()
    {
        SameTypeSpawned = 0;
        FallbackSpawned = 0;
        SameTypeFailures = 0;
    }

    public static Monster Create(
        Monster source,
        Vector2 position,
        int baseMaxHealth,
        int baseDamage,
        int baseSpeed,
        out string mode)
    {
        if (TryCreateSameRuntimeType(source, position, baseMaxHealth, out Monster? sameType))
        {
            NormalizeNormalStats(sameType, baseMaxHealth, baseDamage, baseSpeed);
            SameTypeSpawned++;
            mode = "same-runtime-type";
            return sameType;
        }

        SameTypeFailures++;
        int mineLevel = Math.Clamp(20 + Math.Max(1, baseMaxHealth) / 3, 20, 100);
        var fallback = new GreenSlime(position, mineLevel);
        NormalizeNormalStats(fallback, baseMaxHealth, baseDamage, baseSpeed);
        FallbackSpawned++;
        mode = "green-slime-fallback";
        return fallback;
    }

    private static bool TryCreateSameRuntimeType(Monster source, Vector2 position, int baseMaxHealth, out Monster? monster)
    {
        monster = null;
        Type type = source.GetType();
        if (type.IsAbstract || !typeof(Monster).IsAssignableFrom(type))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        ConstructorInfo[] constructors;
        try
        {
            constructors = type.GetConstructors(flags);
        }
        catch
        {
            return false;
        }

        // Safest shape for both vanilla and custom monsters: the constructor only needs position.
        ConstructorInfo? positionOnly = constructors.FirstOrDefault(ctor =>
        {
            ParameterInfo[] p = ctor.GetParameters();
            return p.Length == 1 && p[0].ParameterType == typeof(Vector2);
        });
        if (positionOnly is not null && TryInvoke(positionOnly, new object[] { position }, out monster))
            return true;

        // Common vanilla shape: (Vector2 position, int mineLevel). We only accept the second int
        // when metadata names it as a level/difficulty concept, so custom opaque IDs are never guessed.
        foreach (ConstructorInfo ctor in constructors)
        {
            ParameterInfo[] p = ctor.GetParameters();
            if (p.Length != 2 || p[0].ParameterType != typeof(Vector2) || p[1].ParameterType != typeof(int))
                continue;

            string parameterName = p[1].Name ?? string.Empty;
            if (!LooksLikeLevelParameter(parameterName))
                continue;

            int level = Math.Clamp(20 + Math.Max(1, baseMaxHealth) / 3, 1, 999);
            if (TryInvoke(ctor, new object[] { position, level }, out monster))
                return true;
        }

        return false;
    }

    private static bool LooksLikeLevelParameter(string name)
    {
        string normalized = new(name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
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
                // Continue through the base type or next candidate name.
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
}
