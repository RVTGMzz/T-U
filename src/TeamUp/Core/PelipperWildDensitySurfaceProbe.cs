using System.Collections;
using System.Reflection;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.22 read-only discovery probe for Pelipper Town's wild population surface.
/// It inspects metadata and reads strongly named primitive runtime values only. It never invokes
/// unknown Pelipper spawn/population methods, never writes Pelipper state, and never creates actors.
/// </summary>
internal static class PelipperWildDensitySurfaceProbe
{
    private const BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly string[] SurfaceTokens = { "wild", "population", "populate", "entry", "encounter", "spawn", "resident", "zone" };
    private static readonly string[] CountTokens = { "desired", "target", "population", "count", "max", "cap", "limit", "resident" };

    private sealed record MethodCandidate(int Score, string Signature);
    private sealed record ValueCandidate(int Score, string Path, string Value);

    public static IReadOnlyList<string> BuildReport(object runtimeRoot, GameLocation? location)
    {
        List<string> lines = new();
        Type rootType = runtimeRoot.GetType();
        Assembly assembly = rootType.Assembly;
        AssemblyName identity = assembly.GetName();

        lines.Add("TEAM UP 6.7.22 - PELIPPER WILD DENSITY SURFACE PROBE");
        lines.Add("mode=READ_ONLY noInvoke=True noWrites=True noFabrication=True");
        lines.Add($"root={rootType.FullName}");
        lines.Add($"assembly={identity.Name} version={identity.Version}");
        lines.Add($"location={location?.NameOrUniqueName ?? "<none>"}");

        CountLiveActors(location, out int visibleWild, out int combatProxy, out int otherPelipper);
        lines.Add($"live visibleWild={visibleWild} combatProxy={combatProxy} otherPelipper={otherPelipper}");

        List<MethodCandidate> methods = DiscoverMethods(assembly);
        List<ValueCandidate> values = DiscoverRuntimeValues(runtimeRoot);

        string recommendation = ResolveRecommendation(methods, values);
        lines.Add($"recommendation={recommendation}");
        lines.Add($"methodCandidates={methods.Count} runtimeValueCandidates={values.Count}");

        foreach (MethodCandidate candidate in methods.Take(40))
            lines.Add($"METHOD score={candidate.Score} {candidate.Signature}");
        foreach (ValueCandidate candidate in values.Take(30))
            lines.Add($"VALUE score={candidate.Score} path={candidate.Path} value={candidate.Value}");

        if (methods.Count == 0)
            lines.Add("METHOD none");
        if (values.Count == 0)
            lines.Add("VALUE none");

        lines.Add("NEXT: only an exact, unambiguous source-owned population target/spawn route may be adapted in a later build.");
        return lines;
    }

    private static void CountLiveActors(GameLocation? location, out int visibleWild, out int combatProxy, out int otherPelipper)
    {
        visibleWild = 0;
        combatProxy = 0;
        otherPelipper = 0;
        if (location is null)
            return;

        foreach (NPC actor in location.characters.OfType<NPC>())
        {
            if (!PelipperTownCompatibilityService.LooksLikePelipperActor(actor))
                continue;

            if (PelipperTownCompatibilityService.IsWildCombatActor(actor))
            {
                if (actor is Monster)
                    combatProxy++;
                else
                    visibleWild++;
            }
            else
            {
                otherPelipper++;
            }
        }
    }

    private static List<MethodCandidate> DiscoverMethods(Assembly assembly)
    {
        List<MethodCandidate> result = new();
        foreach (Type type in GetTypesSafe(assembly))
        {
            if (type.FullName is null || !type.FullName.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase))
                continue;

            MethodInfo[] methods;
            try { methods = type.GetMethods(AllFlags | BindingFlags.DeclaredOnly); }
            catch { continue; }

            foreach (MethodInfo method in methods)
            {
                if (method.IsSpecialName)
                    continue;

                int score = ScoreSurfaceName(type.Name) + ScoreSurfaceName(method.Name) * 2;
                ParameterInfo[] parameters;
                try { parameters = method.GetParameters(); }
                catch { continue; }

                if (parameters.Any(p => typeof(GameLocation).IsAssignableFrom(p.ParameterType)))
                    score += 4;
                if (parameters.Any(p => p.ParameterType == typeof(int) || p.ParameterType == typeof(float) || p.ParameterType == typeof(double)))
                    score += 2;
                if (method.ReturnType == typeof(int))
                    score += 2;
                if (method.Name.Contains("population", StringComparison.OrdinalIgnoreCase)
                    || method.Name.Contains("populate", StringComparison.OrdinalIgnoreCase))
                    score += 5;
                if (method.Name.Contains("entry", StringComparison.OrdinalIgnoreCase))
                    score += 3;
                if (method.Name.Contains("wild", StringComparison.OrdinalIgnoreCase))
                    score += 3;
                if (method.Name.Contains("spawn", StringComparison.OrdinalIgnoreCase))
                    score += 3;

                if (score < 8)
                    continue;

                string signature = $"{type.FullName}.{method.Name}({string.Join(", ", parameters.Select(FormatParameter))}) -> {FriendlyType(method.ReturnType)}";
                result.Add(new MethodCandidate(score, signature));
            }
        }

        return result
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Signature, StringComparer.Ordinal)
            .ToList();
    }

    private static List<ValueCandidate> DiscoverRuntimeValues(object root)
    {
        List<ValueCandidate> values = new();
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);
        WalkObject(root, root.GetType().Name, 0, visited, values);
        return values
            .OrderByDescending(p => p.Score)
            .ThenBy(p => p.Path, StringComparer.Ordinal)
            .Take(100)
            .ToList();
    }

    private static void WalkObject(object target, string path, int depth, HashSet<object> visited, List<ValueCandidate> values)
    {
        if (depth > 5 || !visited.Add(target))
            return;

        if (target is IDictionary dictionary)
        {
            int inspected = 0;
            foreach (DictionaryEntry entry in dictionary)
            {
                if (++inspected > 100)
                    break;
                string key = entry.Key?.ToString() ?? "?";
                object? value = SafeGet(() => entry.Value);
                if (value is null)
                    continue;
                string childPath = $"{path}[{key}]";
                RecordPrimitive(childPath, key, value, values);
                if (ShouldTraverse(key, value.GetType()))
                    WalkObject(value, childPath, depth + 1, visited, values);
            }
            return;
        }

        Type type = target.GetType();
        FieldInfo[] fields;
        PropertyInfo[] properties;
        try
        {
            fields = type.GetFields(AllFlags | BindingFlags.DeclaredOnly);
            properties = type.GetProperties(AllFlags | BindingFlags.DeclaredOnly);
        }
        catch { return; }

        foreach (FieldInfo field in fields)
        {
            if (field.IsStatic || field.FieldType.IsPointer)
                continue;
            object? value = SafeGet(() => field.GetValue(target));
            if (value is null)
                continue;
            string childPath = $"{path}.{field.Name}";
            RecordPrimitive(childPath, field.Name, value, values);
            if (ShouldTraverse(field.Name, value.GetType()))
                WalkObject(value, childPath, depth + 1, visited, values);
        }

        // Primitive, strongly named properties only. We do not walk arbitrary property graphs.
        foreach (PropertyInfo property in properties)
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !IsPrimitiveLike(property.PropertyType))
                continue;
            if (ScoreCountName(property.Name) < 3 && ScoreSurfaceName(property.Name) < 3)
                continue;
            object? value = SafeGet(() => property.GetValue(target));
            if (value is null)
                continue;
            RecordPrimitive($"{path}.{property.Name}", property.Name, value, values);
        }
    }

    private static void RecordPrimitive(string path, string memberName, object value, List<ValueCandidate> values)
    {
        if (!IsPrimitiveLike(value.GetType()))
            return;

        int score = ScoreCountName(memberName) + ScoreSurfaceName(path);
        if (IsNumeric(value))
            score += 2;
        if (memberName.Contains("desired", StringComparison.OrdinalIgnoreCase)
            || memberName.Contains("target", StringComparison.OrdinalIgnoreCase))
            score += 5;
        if (path.Contains("population", StringComparison.OrdinalIgnoreCase))
            score += 3;
        if (path.Contains("entry", StringComparison.OrdinalIgnoreCase))
            score += 2;
        if (score < 7)
            return;

        values.Add(new ValueCandidate(score, path, value.ToString() ?? "<null>"));
    }

    private static bool ShouldTraverse(string memberName, Type valueType)
    {
        if (IsPrimitiveLike(valueType) || typeof(Delegate).IsAssignableFrom(valueType) || valueType == typeof(Type) || valueType == typeof(Assembly))
            return false;

        string fullName = valueType.FullName ?? valueType.Name;
        bool pelipperType = fullName.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase)
            || (valueType.Assembly.GetName().Name?.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase) ?? false);
        return pelipperType && (ScoreSurfaceName(memberName) > 0
            || ScoreSurfaceName(fullName) > 0
            || memberName.Contains("config", StringComparison.OrdinalIgnoreCase)
            || memberName.Contains("manager", StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveRecommendation(IReadOnlyList<MethodCandidate> methods, IReadOnlyList<ValueCandidate> values)
    {
        int methodTop = methods.Count > 0 ? methods[0].Score : 0;
        int methodSecond = methods.Count > 1 ? methods[1].Score : 0;
        int valueTop = values.Count > 0 ? values[0].Score : 0;

        if (methodTop >= 24 && methodTop - methodSecond >= 3 && valueTop >= 12)
            return "EXACT_CANDIDATE";
        if (methodTop >= 16 || valueTop >= 12)
            return "AMBIGUOUS";
        return "NONE";
    }

    private static int ScoreSurfaceName(string text)
    {
        int score = 0;
        foreach (string token in SurfaceTokens)
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += token is "population" or "populate" ? 4 : 2;
        }
        return score;
    }

    private static int ScoreCountName(string text)
    {
        int score = 0;
        foreach (string token in CountTokens)
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                score += token is "desired" or "target" ? 5 : 2;
        }
        return score;
    }

    private static bool IsNumeric(object value)
        => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool IsPrimitiveLike(Type type)
    {
        Type actual = Nullable.GetUnderlyingType(type) ?? type;
        return actual.IsPrimitive || actual.IsEnum || actual == typeof(string) || actual == typeof(decimal)
            || actual == typeof(DateTime) || actual == typeof(TimeSpan) || actual == typeof(Guid);
    }

    private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(type => type is not null).Cast<Type>(); }
        catch { return Array.Empty<Type>(); }
    }

    private static string FormatParameter(ParameterInfo parameter)
        => $"{FriendlyType(parameter.ParameterType)} {parameter.Name}";

    private static string FriendlyType(Type type)
    {
        if (type.IsByRef)
            return $"ref {FriendlyType(type.GetElementType() ?? typeof(object))}";
        if (!type.IsGenericType)
            return type.Name;
        string name = type.Name.Split('`')[0];
        return $"{name}<{string.Join(",", type.GetGenericArguments().Select(FriendlyType))}>";
    }

    private static object? SafeGet(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }
}
