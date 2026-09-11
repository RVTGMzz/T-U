using System.Reflection;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Read-only compatibility policy for Pelipper Town's low-HP capture/mercy mode.
/// Capture-floor protection is fail-closed: it is active only when Pelipper Town is present AND
/// Team Up positively resolves an enabled Catch/Capture/Mercy mode. A 10% threshold is used only
/// after that mode is confirmed on and Pelipper does not expose a stable threshold value.
/// Shiny Emergency Hold is a separate Team Up safety state and does not imply Catch Mode is on.
/// </summary>
internal static class PelipperCaptureSafetyService
{
    private const float FallbackThreshold = 0.10f;
    private const long ProbeIntervalMs = 2000;

    private static long _nextProbeAt;
    private static bool _enabled;
    private static bool _pelipperDetected;
    private static bool _modeConfirmed;
    private static float _threshold = FallbackThreshold;

    public static bool IsProtected(Monster monster)
        => TryGetDamageBudget(monster, out int budget) && budget <= 0;

    /// <summary>
    /// Returns true when Team Up friendly-damage safety applies to this Pelipper combat proxy.
    /// A Shiny Emergency Hold always returns a zero budget. Otherwise this returns a capture-floor
    /// budget only while Pelipper Town Catch/Capture/Mercy mode is positively confirmed enabled.
    /// int.MaxValue means the target isn't currently friendly-damage limited.
    /// </summary>
    public static bool TryGetDamageBudget(Monster monster, out int budget)
    {
        budget = int.MaxValue;
        if (monster.Health <= 0 || monster.MaxHealth <= 0)
            return false;

        // Shiny Hold is deliberately independent from Pelipper Catch Mode. It is a Team Up tactical
        // pause so the Farmer can decide what to do with a rare encounter before allies attack it.
        if (EncounterReactionService.IsShinyEmergencyHeld(monster))
        {
            budget = 0;
            return true;
        }

        // Capture-floor identity is the Pelipper wild/battle proxy itself, not Team Up's transient
        // CombatTarget opt-in marker.
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
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

    /// <summary>
    /// Returns the actual Pelipper capture floor. Shiny Emergency Hold intentionally does not
    /// participate here, so it can never create or repair an artificial 10% HP floor by itself.
    /// </summary>
    public static bool TryGetCaptureFloor(Monster monster, out int stopAtHealth)
    {
        stopAtHealth = 0;
        if (monster.Health <= 0 || monster.MaxHealth <= 0)
            return false;
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return false;

        RefreshPolicyIfNeeded();
        if (!_enabled)
            return false;

        stopAtHealth = Math.Max(1, (int)Math.Ceiling(monster.MaxHealth * _threshold));
        return true;
    }

    /// <summary>
    /// Last-resort repair for custom friendly damage paths that directly lower Health without
    /// crossing a patched damage entry point. This never revives a dead/removed monster and runs
    /// only for the positively confirmed Pelipper capture floor, never for Shiny Emergency Hold.
    /// </summary>
    public static int RepairCurrentLocationFloors(GameLocation? location)
    {
        RefreshPolicyIfNeeded();
        if (location is null || !_enabled)
            return 0;

        int repaired = 0;
        foreach (Monster monster in location.characters.OfType<Monster>())
        {
            if (monster.Health <= 0
                || !TryGetCaptureFloor(monster, out int floor)
                || monster.Health >= floor)
            {
                continue;
            }

            monster.Health = floor;
            repaired++;
        }
        return repaired;
    }

    public static float CurrentThreshold
    {
        get
        {
            RefreshPolicyIfNeeded();
            return _threshold;
        }
    }

    public static bool CurrentEnabled
    {
        get
        {
            RefreshPolicyIfNeeded();
            return _enabled;
        }
    }

    public static bool PelipperDetected
    {
        get
        {
            RefreshPolicyIfNeeded();
            return _pelipperDetected;
        }
    }

    public static bool ModeConfirmed
    {
        get
        {
            RefreshPolicyIfNeeded();
            return _modeConfirmed;
        }
    }

    public static string DescribePolicy()
    {
        RefreshPolicyIfNeeded();
        return $"Pelipper capture safety: pelipper={_pelipperDetected} | modeConfirmed={_modeConfirmed} | enabled={_enabled} | threshold={_threshold:P0}";
    }

    private static void RefreshPolicyIfNeeded()
    {
        long now = Environment.TickCount64;
        if (now < _nextProbeAt)
            return;
        _nextProbeAt = now + ProbeIntervalMs;

        bool pelipperDetected = false;
        bool enabled = false;
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

            pelipperDetected = true;
            foreach (Type type in SafeGetTypes(assembly))
            {
                string typeName = type.FullName ?? type.Name;
                bool likelySettingsType = typeName.Contains("Config", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Setting", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Option", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("ModEntry", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Battle", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Combat", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Capture", StringComparison.OrdinalIgnoreCase)
                    || typeName.Contains("Catch", StringComparison.OrdinalIgnoreCase);
                if (!likelySettingsType)
                    continue;

                ProbeMembers(type, null, isStatic: true, ref enabled, ref threshold, ref bestModeScore, ref bestThresholdScore);

                foreach (object root in GetStaticRoots(type))
                    ProbeMembers(root.GetType(), root, isStatic: false, ref enabled, ref threshold, ref bestModeScore, ref bestThresholdScore);
            }
        }

        _pelipperDetected = pelipperDetected;
        _modeConfirmed = pelipperDetected && bestModeScore >= 0;
        _enabled = _modeConfirmed && enabled;
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
        enabled = false;
        score = -1;

        bool explicitCatchToggle = name.Contains("enablecatch")
            || name.Contains("catchenabled")
            || name.Contains("catchingenabled")
            || name.Contains("allowcatch")
            || name.Contains("cancatch")
            || name.Contains("enablecapture")
            || name.Contains("captureenabled")
            || name.Contains("allowcapture");

        bool strongName = explicitCatchToggle
            || name.Contains("nonlethal")
            || name.Contains("mercy")
            || (name.Contains("capture") && (name.Contains("safety") || name.Contains("mode") || name.Contains("stop")))
            || (name.Contains("catch") && (name.Contains("mode") || name.Contains("safety") || name.Contains("stop")))
            || (name.Contains("stop") && name.Contains("attack") && (name.Contains("health") || name.Contains("hp") || name.Contains("capture") || name.Contains("catch")))
            || (name.Contains("prevent") && (name.Contains("faint") || name.Contains("ko") || name.Contains("kill")));

        if (value is bool boolean && strongName)
        {
            enabled = boolean;
            score = explicitCatchToggle ? 120 : 100;
            return true;
        }

        if (value is Enum || value is string)
        {
            string text = Normalize(value.ToString() ?? string.Empty);
            bool modeName = strongName
                || name.Contains("battlemode")
                || name.Contains("combatmode")
                || name.Contains("capturemode")
                || name.Contains("catchmode");
            if (!modeName)
                return false;

            if (text.Contains("nonlethal")
                || text.Contains("mercy")
                || text.Contains("capture")
                || text.Contains("catch")
                || text.Contains("10percent")
                || text == "10"
                || text == "on"
                || text == "enabled")
            {
                enabled = true;
                score = 90;
                return true;
            }

            if (text.Contains("lethal")
                || text.Contains("kill")
                || text.Contains("defeat")
                || text.Contains("disabled")
                || text == "off")
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

        bool thresholdName = (name.Contains("capture") || name.Contains("catch") || name.Contains("mercy") || name.Contains("lowhealth") || name.Contains("stopattack"))
            && (name.Contains("threshold") || name.Contains("percent") || name.Contains("health") || name.Contains("hp"));
        if (!thresholdName)
            return false;

        if (!TryConvertNumber(value, out double raw) || raw <= 0d)
            return false;

        double normalized = raw > 1d ? raw / 100d : raw;
        if (normalized < 0.01d || normalized > 0.95d)
            return false;

        threshold = (float)normalized;
        score = name.Contains("capture") || name.Contains("catch") ? 95 : 80;
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
