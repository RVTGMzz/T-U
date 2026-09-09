using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Alpha 6.7.21 safe factory for Universal Monster Density.
///
/// Normal custom monsters are duplicated only when their runtime type exposes a constructor shape
/// Team Up can call without inventing opaque mod state. Vanilla types may fall back to a normal
/// GreenSlime when no safe same-type constructor exists. Third-party/custom types never fall back
/// to a visually unrelated slime: they are skipped instead and recorded in telemetry.
/// </summary>
internal static class UniversalMonsterDensitySpawnFactory
{
    public static int SameTypeSpawned { get; private set; }
    public static int VanillaFallbackSpawned { get; private set; }
    public static int CustomRejected { get; private set; }

    public static void ResetTelemetry()
    {
        SameTypeSpawned = 0;
        VanillaFallbackSpawned = 0;
        CustomRejected = 0;
    }

    public static bool TryCreate(
        Monster source,
        Vector2 position,
        out Monster? monster,
        out string mode)
    {
        if (TryCreateSameRuntimeType(source, position, out Monster? sameType) && sameType is not null)
        {
            sameType.Health = Math.Max(1, sameType.MaxHealth);
            SameTypeSpawned++;
            monster = sameType;
            mode = "same-runtime-type";
            return true;
        }

        // A vanilla fallback keeps the density promise without constructing unknown third-party
        // state. Custom/Cardcha monsters intentionally fail closed instead of spawning random slime.
        if (source.GetType().Assembly == typeof(Monster).Assembly)
        {
            int mineLevel = Math.Clamp(20 + Math.Max(1, source.MaxHealth) / 3, 20, 100);
            var fallback = new GreenSlime(position, mineLevel)
            {
                MaxHealth = Math.Clamp((int)Math.Round(Math.Max(1, source.MaxHealth) * 0.82d), 36, 900),
                Speed = Math.Clamp(Math.Max(1, source.Speed), 2, 6)
            };
            fallback.Health = fallback.MaxHealth;
            VanillaFallbackSpawned++;
            monster = fallback;
            mode = "vanilla-green-slime-fallback";
            return true;
        }

        CustomRejected++;
        monster = null;
        mode = "custom-unsafe-constructor-skip";
        return false;
    }

    private static bool TryCreateSameRuntimeType(Monster source, Vector2 position, out Monster? monster)
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

        ConstructorInfo? positionOnly = constructors.FirstOrDefault(ctor =>
        {
            ParameterInfo[] p = ctor.GetParameters();
            return p.Length == 1 && p[0].ParameterType == typeof(Vector2);
        });
        if (positionOnly is not null && TryInvoke(positionOnly, new object[] { position }, out monster))
            return true;

        foreach (ConstructorInfo ctor in constructors)
        {
            ParameterInfo[] p = ctor.GetParameters();
            if (p.Length != 2 || p[0].ParameterType != typeof(Vector2) || p[1].ParameterType != typeof(int))
                continue;

            if (!LooksLikeLevelParameter(p[1].Name ?? string.Empty))
                continue;

            int level = Math.Clamp(20 + Math.Max(1, source.MaxHealth) / 3, 1, 999);
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
}
