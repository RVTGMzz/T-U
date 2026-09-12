using System.Collections;
using System.Reflection;
using Ronvotri.TeamUp.Combat;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Pelipper Town capture-safety compatibility.
/// 6.7.44.6 deliberately separates Pelipper's active combat mode from capture-chance bonuses.
/// Unknown mode fails closed. A 10% mercy floor is used only after the live mode is positively
/// identified as Capture/Catch mode and no explicit non-lethal floor is exposed by Pelipper.
/// </summary>
internal static class PelipperCaptureSafetyService
{
    private const float FallbackThreshold = 0.10f;
    private const long ProbeIntervalMs = 1500;
    private const int MaxRuntimeDepth = 2;

    private static readonly string[] RejectedSemanticWords =
    {
        "bonus", "chance", "multiplier", "rate", "accuracy", "pity", "odds", "weight", "roll"
    };

    private static long _nextProbeAt;
    private static bool _enabled;
    private static bool _pelipperDetected;
    private static bool _modeConfirmed;
    private static bool _modeEnabled;
    private static float _threshold = FallbackThreshold;
    private static string _modeSource = "unresolved";
    private static string _modeValue = "unresolved";
    private static string _thresholdSource = "inactive";

    public static bool IsProtected(Monster monster)
        => TryGetDamageBudget(monster, out int budget) && budget <= 0;

    public static bool TryGetDamageBudget(Monster monster, out int budget)
    {
        budget = int.MaxValue;
        if (monster.Health <= 0 || monster.MaxHealth <= 0)
            return false;
        if (MonsterMutationService.IsMutant(monster))
            return false;

        // Shiny Emergency Hold is independent from Pelipper Capture mode.
        if (EncounterReactionService.IsShinyEmergencyHeld(monster))
        {
            budget = 0;
            return true;
        }

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

    public static bool TryGetCaptureFloor(Monster monster, out int stopAtHealth)
    {
        stopAtHealth = 0;
        if (monster.Health <= 0 || monster.MaxHealth <= 0)
            return false;
        if (MonsterMutationService.IsMutant(monster))
            return false;
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return false;

        RefreshPolicyIfNeeded();
        if (!_enabled)
            return false;

        stopAtHealth = Math.Max(1, (int)Math.Ceiling(monster.MaxHealth * _threshold));
        return true;
    }

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

    public static float CurrentThreshold { get { RefreshPolicyIfNeeded(); return _threshold; } }
    public static bool CurrentEnabled { get { RefreshPolicyIfNeeded(); return _enabled; } }
    public static bool PelipperDetected { get { RefreshPolicyIfNeeded(); return _pelipperDetected; } }
    public static bool ModeConfirmed { get { RefreshPolicyIfNeeded(); return _modeConfirmed; } }
    public static bool CatchModeDetected => ModeConfirmed;
    public static bool CatchModeEnabled { get { RefreshPolicyIfNeeded(); return _modeEnabled; } }
    public static string ModeSource { get { RefreshPolicyIfNeeded(); return _modeSource; } }
    public static string ModeValue { get { RefreshPolicyIfNeeded(); return _modeValue; } }
    public static string ThresholdSource { get { RefreshPolicyIfNeeded(); return _thresholdSource; } }

    public static string DescribePolicy()
    {
        RefreshPolicyIfNeeded();
        return $"Pelipper capture safety: pelipperPresent={_pelipperDetected} | catchModeDetected={_modeConfirmed} | catchModeEnabled={_modeEnabled} | captureSafetyEnabled={_enabled} | modeValue={_modeValue} | modeSource={_modeSource} | threshold={_threshold:P0} | thresholdSource={_thresholdSource}";
    }

    private static void RefreshPolicyIfNeeded()
    {
        long now = Environment.TickCount64;
        if (now < _nextProbeAt)
            return;
        _nextProbeAt = now + ProbeIntervalMs;

        bool pelipperDetected = false;
        bool modeEnabled = false;
        float threshold = FallbackThreshold;
        int bestModeScore = -1;
        int bestThresholdScore = -1;
        string modeSource = "unresolved";
        string modeValue = "unresolved";
        string thresholdSource = "unresolved";

        // First try Pelipper-owned player state. This is cheap and avoids reflecting through the whole
        // mod when the active battle mode is persisted in player modData.
        if (Context.IsWorldReady)
        {
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                foreach (var pair in farmer.modData.Pairs)
                {
                    string key = Normalize(pair.Key);
                    if (!key.Contains("pelipper"))
                        continue;
                    ConsiderMode(key, pair.Value, $"Farmer.modData[{pair.Key}]", ref modeEnabled,
                        ref bestModeScore, ref modeSource, ref modeValue);
                    ConsiderThreshold(key, pair.Value, $"Farmer.modData[{pair.Key}]", ref threshold,
                        ref bestThresholdScore, ref thresholdSource);
                }
            }
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string assemblyName = assembly.GetName().Name ?? string.Empty;
            if (!assemblyName.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase)
                && !assemblyName.Contains("Griff.PelipperTown", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            pelipperDetected = true;
            HashSet<object> visited = new(ReferenceEqualityComparer.Instance);

            foreach (Type type in SafeGetTypes(assembly))
            {
                string typeName = Normalize(type.FullName ?? type.Name);
                if (!LooksLikeRuntimeType(typeName) || IsRejectedContainer(typeName))
                    continue;

                ProbeMembers(type, null, isStatic: true, type.FullName ?? type.Name,
                    ref modeEnabled, ref threshold, ref bestModeScore, ref bestThresholdScore,
                    ref modeSource, ref modeValue, ref thresholdSource);

                foreach (object root in GetRuntimeRoots(type))
                    ProbeRuntimeObject(root, type.FullName ?? type.Name, depth: 0, visited,
                        ref modeEnabled, ref threshold, ref bestModeScore, ref bestThresholdScore,
                        ref modeSource, ref modeValue, ref thresholdSource);
            }
        }

        _pelipperDetected = pelipperDetected;
        _modeConfirmed = pelipperDetected && bestModeScore >= 0;
        _modeEnabled = _modeConfirmed && modeEnabled;
        _enabled = _modeEnabled;
        _modeSource = _modeConfirmed ? modeSource : "unresolved";
        _modeValue = _modeConfirmed ? modeValue : "unresolved";
        _threshold = Math.Clamp(bestThresholdScore >= 0 ? threshold : FallbackThreshold, 0.01f, 0.95f);
        _thresholdSource = !_enabled
            ? "inactive"
            : bestThresholdScore >= 0
                ? thresholdSource
                : "fallback-10%-confirmed-capture-mode";
    }

    private static void ProbeRuntimeObject(
        object root,
        string sourcePrefix,
        int depth,
        HashSet<object> visited,
        ref bool modeEnabled,
        ref float threshold,
        ref int bestModeScore,
        ref int bestThresholdScore,
        ref string modeSource,
        ref string modeValue,
        ref string thresholdSource)
    {
        if (depth > MaxRuntimeDepth || !visited.Add(root))
            return;

        Type type = root.GetType();
        string typeName = Normalize(type.FullName ?? type.Name);
        if (IsRejectedContainer(typeName))
            return;

        ProbeMembers(type, root, isStatic: false, sourcePrefix,
            ref modeEnabled, ref threshold, ref bestModeScore, ref bestThresholdScore,
            ref modeSource, ref modeValue, ref thresholdSource);

        if (depth == MaxRuntimeDepth)
            return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            string name = Normalize(field.Name);
            if (!LooksLikeRuntimeRootMember(name) || IsRejectedContainer(name))
                continue;
            object? nested = TryGetValue(() => field.GetValue(root));
            if (nested is null || IsSimple(nested.GetType()))
                continue;
            ProbeRuntimeObject(nested, $"{sourcePrefix}.{field.Name}", depth + 1, visited,
                ref modeEnabled, ref threshold, ref bestModeScore, ref bestThresholdScore,
                ref modeSource, ref modeValue, ref thresholdSource);
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            string name = Normalize(property.Name);
            if (!property.CanRead || property.GetIndexParameters().Length != 0
                || !LooksLikeRuntimeRootMember(name) || IsRejectedContainer(name))
            {
                continue;
            }
            object? nested = TryGetValue(() => property.GetValue(root));
            if (nested is null || IsSimple(nested.GetType()))
                continue;
            ProbeRuntimeObject(nested, $"{sourcePrefix}.{property.Name}", depth + 1, visited,
                ref modeEnabled, ref threshold, ref bestModeScore, ref bestThresholdScore,
                ref modeSource, ref modeValue, ref thresholdSource);
        }
    }

    private static void ProbeMembers(
        Type type,
        object? instance,
        bool isStatic,
        string sourcePrefix,
        ref bool modeEnabled,
        ref float threshold,
        ref int bestModeScore,
        ref int bestThresholdScore,
        ref string modeSource,
        ref string modeValue,
        ref string thresholdSource)
    {
        BindingFlags flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance)
            | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (FieldInfo field in type.GetFields(flags))
        {
            object? value = TryGetValue(() => field.GetValue(instance));
            ConsiderMode(field.Name, value, $"{sourcePrefix}.{field.Name}", ref modeEnabled,
                ref bestModeScore, ref modeSource, ref modeValue);
            ConsiderThreshold(field.Name, value, $"{sourcePrefix}.{field.Name}", ref threshold,
                ref bestThresholdScore, ref thresholdSource);
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0)
                continue;
            object? value = TryGetValue(() => property.GetValue(instance));
            ConsiderMode(property.Name, value, $"{sourcePrefix}.{property.Name}", ref modeEnabled,
                ref bestModeScore, ref modeSource, ref modeValue);
            ConsiderThreshold(property.Name, value, $"{sourcePrefix}.{property.Name}", ref threshold,
                ref bestThresholdScore, ref thresholdSource);
        }
    }

    private static void ConsiderMode(
        string rawName,
        object? value,
        string source,
        ref bool enabled,
        ref int bestScore,
        ref string bestSource,
        ref string bestValue)
    {
        if (value is null)
            return;

        string name = Normalize(rawName);
        if (ContainsRejectedSemantic(name))
            return;

        bool booleanMode = name.Contains("iscapturemode") || name.Contains("capturemodeactive")
            || name.Contains("iscatchmode") || name.Contains("catchmodeactive") || name.Contains("mercyactive");
        if (booleanMode && value is bool boolean)
        {
            const int score = 150;
            if (score > bestScore)
            {
                enabled = boolean;
                bestScore = score;
                bestSource = source;
                bestValue = boolean ? "capture" : "not-capture";
            }
            return;
        }

        bool modeName = name.Contains("combatmode") || name.Contains("battlemode")
            || name.Contains("currentmode") || name.Contains("activemode")
            || name.Contains("behaviormode") || name.Contains("partymode")
            || name.Contains("pokemonmode") || name.Contains("pokémonmode");
        if (!modeName || (value is not string && !value.GetType().IsEnum))
            return;

        string text = Normalize(value.ToString() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(text))
            return;

        bool? capture = text switch
        {
            "capture" or "capturemode" or "catch" or "catchmode" or "capturing" => true,
            "defensive" or "defense" or "aggressive" or "peaceful" or "passive" or "normal" => false,
            _ => text.Contains("capture") || text.Contains("catch") ? true
                : text.Contains("defens") || text.Contains("aggress") || text.Contains("peace") ? false
                : null
        };
        if (!capture.HasValue)
            return;

        int score = name.Contains("combatmode") || name.Contains("battlemode") ? 140 : 120;
        if (score <= bestScore)
            return;

        enabled = capture.Value;
        bestScore = score;
        bestSource = source;
        bestValue = value.ToString() ?? text;
    }

    private static void ConsiderThreshold(
        string rawName,
        object? value,
        string source,
        ref float threshold,
        ref int bestScore,
        ref string bestSource)
    {
        if (value is null)
            return;

        string name = Normalize(rawName);
        if (ContainsRejectedSemantic(name))
            return;

        bool explicitFloor = name.Contains("capturefloor") || name.Contains("catchfloor")
            || name.Contains("mercyhealth") || name.Contains("mercyhp")
            || name.Contains("stopattackhealth") || name.Contains("stopattackhp")
            || name.Contains("nonlethalhealth") || name.Contains("nonlethalhp");
        bool thresholdLike = explicitFloor
            || ((name.Contains("mercy") || name.Contains("stopattack") || name.Contains("nonlethal"))
                && (name.Contains("threshold") || name.Contains("percent") || name.Contains("health") || name.Contains("hp")));
        if (!thresholdLike || !TryConvertNumber(value, out double raw) || raw <= 0d)
            return;

        double normalized = raw > 1d ? raw / 100d : raw;
        if (normalized < 0.01d || normalized > 0.50d)
            return;

        int score = explicitFloor ? 140 : 110;
        if (score <= bestScore)
            return;

        threshold = (float)normalized;
        bestScore = score;
        bestSource = source;
    }

    private static IEnumerable<object> GetRuntimeRoots(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            string name = Normalize(field.Name);
            if (!LooksLikeRuntimeRootMember(name) || IsRejectedContainer(name))
                continue;
            object? value = TryGetValue(() => field.GetValue(null));
            if (value is not null && !IsSimple(value.GetType()))
                yield return value;
        }
        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            string name = Normalize(property.Name);
            if (!property.CanRead || property.GetIndexParameters().Length != 0
                || !LooksLikeRuntimeRootMember(name) || IsRejectedContainer(name))
            {
                continue;
            }
            object? value = TryGetValue(() => property.GetValue(null));
            if (value is not null && !IsSimple(value.GetType()))
                yield return value;
        }
    }

    private static bool LooksLikeRuntimeType(string name)
        => name.Contains("modentry") || name.Contains("runtime") || name.Contains("combat")
            || name.Contains("battle") || name.Contains("companion") || name.Contains("party")
            || name.Contains("manager") || name.Contains("controller");

    private static bool LooksLikeRuntimeRootMember(string name)
        => name.Contains("instance") || name.Contains("runtime") || name.Contains("combat")
            || name.Contains("battle") || name.Contains("companion") || name.Contains("party")
            || name.Contains("manager") || name.Contains("controller") || name.Contains("state")
            || name == "mod" || name.EndsWith("modentry", StringComparison.Ordinal);

    private static bool IsRejectedContainer(string name)
        => name.Contains("config") || name.Contains("setting") || name.Contains("option")
            || name.Contains("gmcm") || name.Contains("menu");

    private static bool ContainsRejectedSemantic(string name)
        => RejectedSemanticWords.Any(name.Contains);

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(type => type is not null).Cast<Type>(); }
        catch { return Array.Empty<Type>(); }
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
        catch { number = 0d; return false; }
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
