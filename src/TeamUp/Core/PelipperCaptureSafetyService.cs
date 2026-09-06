using System.Reflection;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Read-only compatibility policy for Pelipper Town's low-HP capture/mercy mode.
/// Team Up never takes ownership of Pelipper actors here. The adapter only decides whether a
/// Pelipper combat proxy is safe to damage. It probes obvious Pelipper config/settings members
/// when available and falls back to the user's requested 10% capture threshold if the source
/// mod doesn't expose a stable public setting.
/// </summary>
internal static class PelipperCaptureSafetyService
{
    private const float FallbackThreshold = 0.10f;
    private const long ProbeIntervalMs = 2000;

    private static long _nextProbeAt;
    private static bool _enabled = true;
    private static float _threshold = FallbackThreshold;

    public static bool IsProtected(Monster monster)
        => TryGetDamageBudget(monster, out int budget) && budget <= 0;

    /// <summary>
    /// Returns true when capture-safety applies to this Pelipper combat proxy. The budget is the
    /// most damage Team Up may deal without crossing below the configured capture threshold.
    /// int.MaxValue means the target isn't capture-limited.
    /// </summary>
    public static bool TryGetDamageBudget(Monster monster, out int budget)
    {
        budget = int.MaxValue;
        if (monster.Health <= 0 || monster.MaxHealth <= 0)
            return false;
        if (!PelipperTownCompatibilityService.LooksLikePelipperActor(monster))
            return false;

        // Owned companions are already excluded from Team Up combat entirely. Capture safety is
        // only for unowned/wild battle proxies which Alpha 6.6.13 explicitly opted into combat.
        if (PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            return false;

        RefreshPolicyIfNeeded();
        if (!_enabled)
            return false;

        int stopAtHealth = Math.Max(1, (int)Math.Ceiling(monster.MaxHealth * _threshold));
        budget = Math.Max(0, monster.Health - stopAtHealth);
        return true;
    }

    public static int ClampDamage(Monster monster, int requestedDamage)
    {
        if (requestedDamage <= 0)
            return 0;
        return TryGetDamageBudget(monster, out int budget)
            ? Math.Min(requestedDamage, budget)
            : requestedDamage;
    }

    public static float CurrentThreshold => _threshold;
    public static bool CurrentEnabled => _enabled;

    private static void RefreshPolicyIfNeeded()
    {
        long now = Environment.TickCount64;
        if (now < _nextProbeAt)
            return;
        _nextProbeAt = now + ProbeIntervalMs;

        bool enabled = true;
        float threshold = FallbackThreshold;
        int bestModeScore = -1;
        int bestThresholdScore = -1;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string assemblyName = assembly.GetName().Name ?? string.Empty;
            if (!assemblyName.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase)
                && !assemblyName.Contains("Griff.PelipperTown", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (Type type in SafeGetTypes(assembly))
            {
                string typeName = type.FullName ?? type.Name;
                bool likelySettingsType = typeName.Contains("Config", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Setting", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Option", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("ModEntry", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Battle", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Combat", StringComparison.OrdinalIgnoreCase);
                if (!likelySettingsType)
                    continue;

                ProbeMembers(type, null, isStatic: true, ref enabled, ref threshold, ref bestModeScore, ref bestThresholdScore);

                foreach (object root in GetStaticRoots(type))
                    ProbeMembers(root.GetType(), root, isStatic: false, ref enabled, ref threshold, ref bestModeScore, ref bestThresholdScore);
            }
        }

        _enabled = enabled;
        _threshold = Math.Clamp(threshold, 0.01f, 0.95f);
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

    private static IEnumerable<object> GetStaticRoots(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!LooksLikeRootMember(field.Name))
                continue;
            object? value = TryGetValue(() => field.GetValue(null));
            if (value is not null && !IsSimple(value.GetType()))
                yield return value;
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !LooksLikeRootMember(property.Name))
                continue;
            object? value = TryGetValue(() => property.GetValue(null));
            if (value is not null && !IsSimple(value.GetType()))
                yield return value;
        }
    }

    private static void ProbeMembers(
        Type type,
        object? instance,
        bool isStatic,
        ref bool enabled,
        ref float threshold,
        ref int bestModeScore,
        ref int bestThresholdScore)
    {
        BindingFlags flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance)
            | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (FieldInfo field in type.GetFields(flags))
        {
            object? value = TryGetValue(() => field.GetValue(instance));
            Consider(field.Name, value, ref enabled, ref threshold, ref bestModeScore, ref bestThresholdScore);
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
                continue;
            object? value = TryGetValue(() => property.GetValue(instance));
            Consider(property.Name, value, ref enabled, ref threshold, ref bestModeScore, ref bestThresholdScore);
        }
    }

    private static void Consider(
        string memberName,
        object? value,
        ref bool enabled,
        ref float threshold,
        ref int bestModeScore,
        ref int bestThresholdScore)
    {
        if (value is null)
            return;

        string name = Normalize(memberName);
        if (TryInterpretMode(name, value, out bool modeEnabled, out int modeScore) && modeScore > bestModeScore)
        {
            enabled = modeEnabled;
            bestModeScore = modeScore;
        }

        if (TryInterpretThreshold(name, value, out float candidateThreshold, out int thresholdScore)
            && thresholdScore > bestThresholdScore)
        {
            threshold = candidateThreshold;
            bestThresholdScore = thresholdScore;
        }
    }

    private static bool TryInterpretMode(string name, object value, out bool enabled, out int score)
    {
        enabled = true;
        score = -1;

        bool strongName = name.Contains("nonlethal")
            || name.Contains("mercy")
            || (name.Contains("capture") && (name.Contains("safety") || name.Contains("mode") || name.Contains("stop")))
            || (name.Contains("stop") && name.Contains("attack") && (name.Contains("health") || name.Contains("hp") || name.Contains("capture")))
            || (name.Contains("prevent") && (name.Contains("faint") || name.Contains("ko") || name.Contains("kill")));

        if (value is bool boolean && strongName)
        {
            enabled = boolean;
            score = 100;
            return true;
        }

        if (value is Enum || value is string)
        {
            string text = Normalize(value.ToString() ?? string.Empty);
            bool modeName = strongName || name.Contains("battlemode") || name.Contains("combatmode") || name.Contains("capturemode");
            if (!modeName)
                return false;

            if (text.Contains("nonlethal") || text.Contains("mercy") || text.Contains("capture") || text.Contains("10percent") || text == "10")
            {
                enabled = true;
                score = 90;
                return true;
            }

            if (text.Contains("lethal") || text.Contains("kill") || text.Contains("disabled") || text == "off")
            {
                enabled = false;
                score = 90;
                return true;
            }
        }

        return false;
    }

    private static bool TryInterpretThreshold(string name, object value, out float threshold, out int score)
    {
        threshold = FallbackThreshold;
        score = -1;

        bool thresholdName = (name.Contains("capture") || name.Contains("mercy") || name.Contains("lowhealth") || name.Contains("stopattack"))
            && (name.Contains("threshold") || name.Contains("percent") || name.Contains("health") || name.Contains("hp"));
        if (!thresholdName)
            return false;

        if (!TryConvertNumber(value, out double raw))
            return false;
        if (raw <= 0d)
            return false;

        double normalized = raw > 1d ? raw / 100d : raw;
        if (normalized < 0.01d || normalized > 0.95d)
            return false;

        threshold = (float)normalized;
        score = name.Contains("capture") ? 95 : 80;
        return true;
    }

    private static bool TryConvertNumber(object value, out double number)
    {
        try
        {
            switch (value)
            {
                case byte v: number = v; return true;
                case sbyte v: number = v; return true;
                case short v: number = v; return true;
                case ushort v: number = v; return true;
                case int v: number = v; return true;
                case uint v: number = v; return true;
                case long v: number = v; return true;
                case ulong v: number = v; return true;
                case float v: number = v; return true;
                case double v: number = v; return true;
                case decimal v: number = (double)v; return true;
                default:
                    return double.TryParse(value.ToString(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out number);
            }
        }
        catch
        {
            number = 0d;
            return false;
        }
    }

    private static bool LooksLikeRootMember(string name)
    {
        string normalized = Normalize(name);
        return normalized.Contains("config")
            || normalized.Contains("setting")
            || normalized.Contains("option")
            || normalized.Contains("instance")
            || normalized == "mod"
            || normalized.EndsWith("modentry", StringComparison.Ordinal);
    }

    private static bool IsSimple(Type type)
        => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);

    private static object? TryGetValue(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
