using System.Collections;
using System.Reflection;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Best-effort runtime bridge for Pelipper Town's per-villager companion enabled state.
/// Team Up never writes Pelipper save data and never uses IsInvisible/Halt/controller as a
/// substitute for recall. The bridge first looks for an explicit source method, then for the
/// in-memory per-villager config entry exposed by Pelipper's GMCM-backed config.
/// </summary>
internal static class PelipperVillagerCompanionRuntimeBridge
{
    private sealed class OverrideState
    {
        public bool OriginalEnabled { get; init; } = true;
        public string Route { get; init; } = string.Empty;
    }

    private static readonly Dictionary<string, OverrideState> Overrides =
        new(StringComparer.OrdinalIgnoreCase);

    public static bool TrySetEnabled(string ownerName, NPC? owner, bool enabled, out string route)
    {
        route = string.Empty;
        if (string.IsNullOrWhiteSpace(ownerName))
            return false;

        bool original = true;
        bool foundOriginal = false;

        if (TryInvokeSourceAction(ownerName, owner, enabled, out route))
        {
            if (!Overrides.ContainsKey(ownerName))
                Overrides[ownerName] = new OverrideState { OriginalEnabled = true, Route = route };
            TryRefreshSource(ownerName, owner);
            return true;
        }

        if (TrySetConfigState(ownerName, enabled, out original, out foundOriginal, out route))
        {
            if (!Overrides.ContainsKey(ownerName))
            {
                Overrides[ownerName] = new OverrideState
                {
                    OriginalEnabled = foundOriginal ? original : true,
                    Route = route
                };
            }

            TryRefreshSource(ownerName, owner);
            return true;
        }

        return false;
    }

    public static bool Restore(string ownerName, NPC? owner, out string route)
    {
        route = string.Empty;
        if (!Overrides.TryGetValue(ownerName, out OverrideState? state))
            return false;

        bool restored = TryInvokeSourceAction(ownerName, owner, state.OriginalEnabled, out route)
            || TrySetConfigState(ownerName, state.OriginalEnabled, out _, out _, out route);

        if (restored)
            TryRefreshSource(ownerName, owner);

        Overrides.Remove(ownerName);
        return restored;
    }

    public static void RestoreAll(Func<string, NPC?> ownerResolver)
    {
        foreach (string ownerName in Overrides.Keys.ToList())
            Restore(ownerName, ownerResolver(ownerName), out _);
        Overrides.Clear();
    }

    private static bool TryInvokeSourceAction(string ownerName, NPC? owner, bool enabled, out string route)
    {
        route = string.Empty;
        foreach (Assembly assembly in GetPelipperAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (!LooksLikeRuntimeType(type))
                    continue;

                if (TryInvokeMethods(type, null, isStatic: true, ownerName, owner, enabled, out route))
                    return true;

                foreach (object root in GetStaticRoots(type))
                {
                    if (TryInvokeMethods(root.GetType(), root, isStatic: false, ownerName, owner, enabled, out route))
                        return true;
                }
            }
        }
        return false;
    }

    private static bool TryInvokeMethods(
        Type type,
        object? instance,
        bool isStatic,
        string ownerName,
        NPC? owner,
        bool enabled,
        out string route)
    {
        route = string.Empty;
        BindingFlags flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance)
            | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (MethodInfo method in type.GetMethods(flags))
        {
            if (method.IsGenericMethodDefinition)
                continue;

            string normalized = Normalize(method.Name);
            bool companionSemantic = normalized.Contains("companion") || normalized.Contains("partner");
            bool villagerSemantic = normalized.Contains("villager") || normalized.Contains("npc");
            bool setterSemantic = normalized.Contains("set") || normalized.Contains("enable")
                || normalized.Contains("disable") || normalized.Contains("show") || normalized.Contains("deploy");
            if (!companionSemantic || !setterSemantic)
                continue;

            ParameterInfo[] p = method.GetParameters();
            object?[]? args = null;

            if (p.Length == 2 && p[1].ParameterType == typeof(bool)
                && (villagerSemantic || normalized == "setcompanionenabled" || normalized == "setpartnerenabled"))
            {
                if (p[0].ParameterType == typeof(string))
                    args = new object?[] { ownerName, enabled };
                else if (owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
                    args = new object?[] { owner, enabled };
            }
            else if (p.Length == 1 && villagerSemantic)
            {
                bool enableMethod = normalized.Contains("enable") || normalized.Contains("show") || normalized.Contains("deploy");
                bool disableMethod = normalized.Contains("disable") || normalized.Contains("hide") || normalized.Contains("recall");
                if ((enabled && enableMethod) || (!enabled && disableMethod))
                {
                    if (p[0].ParameterType == typeof(string))
                        args = new object?[] { ownerName };
                    else if (owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
                        args = new object?[] { owner };
                }
            }

            if (args is null)
                continue;

            try
            {
                method.Invoke(instance, args);
                route = $"method:{type.FullName}.{method.Name}";
                return true;
            }
            catch
            {
                // Optional compatibility: try the next strongly-shaped source contract.
            }
        }

        return false;
    }

    private static bool TrySetConfigState(
        string ownerName,
        bool enabled,
        out bool original,
        out bool foundOriginal,
        out string route)
    {
        original = true;
        foundOriginal = false;
        route = string.Empty;

        foreach (Assembly assembly in GetPelipperAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                string typeName = Normalize(type.FullName ?? type.Name);
                if (!(typeName.Contains("config") || typeName.Contains("setting") || typeName.Contains("option")
                    || typeName.Contains("villager") || typeName.Contains("companion") || typeName.Contains("modentry")))
                {
                    continue;
                }

                foreach (object root in GetStaticRoots(type))
                {
                    var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
                    if (TrySetOnObject(root, root.GetType().Name, ownerName, enabled, 0, visited,
                        out original, out foundOriginal, out route))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool TrySetOnObject(
        object target,
        string path,
        string ownerName,
        bool enabled,
        int depth,
        HashSet<object> visited,
        out bool original,
        out bool foundOriginal,
        out string route)
    {
        original = true;
        foundOriginal = false;
        route = string.Empty;
        if (depth > 3 || !visited.Add(target))
            return false;

        if (target is IDictionary dictionary)
        {
            object? matchingKey = null;
            foreach (object? key in dictionary.Keys)
            {
                if (key is string text && text.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                {
                    matchingKey = key;
                    break;
                }
            }

            if (matchingKey is not null)
            {
                object? value = dictionary[matchingKey];
                string normalizedPath = Normalize(path);
                if (value is bool boolean
                    && (normalizedPath.Contains("villager") || normalizedPath.Contains("companion") || normalizedPath.Contains("partner")))
                {
                    original = boolean;
                    foundOriginal = true;
                    dictionary[matchingKey] = enabled;
                    route = $"config:{path}[{ownerName}]";
                    return true;
                }

                if (value is not null && TrySetEnabledMember(value, $"{path}[{ownerName}]", enabled,
                    out original, out foundOriginal, out route))
                {
                    return true;
                }
            }
        }

        if (TrySetOwnerNamedBool(target, path, ownerName, enabled,
            out original, out foundOriginal, out route))
        {
            return true;
        }

        if (depth == 3)
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!ShouldTraverseMember(field.Name))
                continue;
            object? value = SafeGet(() => field.GetValue(target));
            if (value is null || IsSimple(value.GetType()))
                continue;
            if (TrySetOnObject(value, $"{path}.{field.Name}", ownerName, enabled, depth + 1, visited,
                out original, out foundOriginal, out route))
            {
                return true;
            }
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !ShouldTraverseMember(property.Name))
                continue;
            object? value = SafeGet(() => property.GetValue(target));
            if (value is null || IsSimple(value.GetType()))
                continue;
            if (TrySetOnObject(value, $"{path}.{property.Name}", ownerName, enabled, depth + 1, visited,
                out original, out foundOriginal, out route))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TrySetEnabledMember(
        object target,
        string path,
        bool enabled,
        out bool original,
        out bool foundOriginal,
        out string route)
    {
        original = true;
        foundOriginal = false;
        route = string.Empty;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type type = target.GetType();

        string[] names = { "CompanionEnabled", "IsCompanionEnabled", "PartnerEnabled", "IsPartnerEnabled", "Enabled", "IsEnabled" };
        foreach (string name in names)
        {
            PropertyInfo? property = type.GetProperty(name, flags);
            if (property?.CanRead == true && property.CanWrite && property.PropertyType == typeof(bool))
            {
                bool old = (bool)(SafeGet(() => property.GetValue(target)) ?? true);
                property.SetValue(target, enabled);
                original = old;
                foundOriginal = true;
                route = $"config:{path}.{property.Name}";
                return true;
            }

            FieldInfo? field = type.GetField(name, flags);
            if (field?.FieldType == typeof(bool))
            {
                bool old = (bool)(SafeGet(() => field.GetValue(target)) ?? true);
                field.SetValue(target, enabled);
                original = old;
                foundOriginal = true;
                route = $"config:{path}.{field.Name}";
                return true;
            }
        }

        return false;
    }

    private static bool TrySetOwnerNamedBool(
        object target,
        string path,
        string ownerName,
        bool enabled,
        out bool original,
        out bool foundOriginal,
        out string route)
    {
        original = true;
        foundOriginal = false;
        route = string.Empty;
        string ownerToken = Normalize(ownerName);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || !property.CanWrite || property.PropertyType != typeof(bool) || property.GetIndexParameters().Length != 0)
                continue;
            string name = Normalize(property.Name);
            if (!name.Contains(ownerToken) || !(name.Contains("companion") || name.Contains("partner")) || !name.Contains("enable"))
                continue;
            bool old = (bool)(SafeGet(() => property.GetValue(target)) ?? true);
            property.SetValue(target, enabled);
            original = old;
            foundOriginal = true;
            route = $"config:{path}.{property.Name}";
            return true;
        }

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (field.FieldType != typeof(bool))
                continue;
            string name = Normalize(field.Name);
            if (!name.Contains(ownerToken) || !(name.Contains("companion") || name.Contains("partner")) || !name.Contains("enable"))
                continue;
            bool old = (bool)(SafeGet(() => field.GetValue(target)) ?? true);
            field.SetValue(target, enabled);
            original = old;
            foundOriginal = true;
            route = $"config:{path}.{field.Name}";
            return true;
        }

        return false;
    }

    private static void TryRefreshSource(string ownerName, NPC? owner)
    {
        foreach (Assembly assembly in GetPelipperAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (!LooksLikeRuntimeType(type))
                    continue;

                if (TryInvokeRefreshMethods(type, null, true, ownerName, owner))
                    return;
                foreach (object root in GetStaticRoots(type))
                {
                    if (TryInvokeRefreshMethods(root.GetType(), root, false, ownerName, owner))
                        return;
                }
            }
        }
    }

    private static bool TryInvokeRefreshMethods(Type type, object? instance, bool isStatic, string ownerName, NPC? owner)
    {
        BindingFlags flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance)
            | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in type.GetMethods(flags))
        {
            string name = Normalize(method.Name);
            if (!(name.Contains("refresh") || name.Contains("rebuild") || name.Contains("sync")))
                continue;
            if (!(name.Contains("villager") && (name.Contains("companion") || name.Contains("partner"))))
                continue;

            ParameterInfo[] p = method.GetParameters();
            object?[]? args = null;
            if (p.Length == 0)
                args = Array.Empty<object?>();
            else if (p.Length == 1 && p[0].ParameterType == typeof(string))
                args = new object?[] { ownerName };
            else if (p.Length == 1 && owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
                args = new object?[] { owner };

            if (args is null)
                continue;
            try
            {
                method.Invoke(instance, args);
                return true;
            }
            catch
            {
            }
        }
        return false;
    }

    private static IEnumerable<Assembly> GetPelipperAssemblies()
        => AppDomain.CurrentDomain.GetAssemblies().Where(assembly =>
        {
            string name = assembly.GetName().Name ?? string.Empty;
            return name.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Griff.PelipperTown", StringComparison.OrdinalIgnoreCase);
        });

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null).Cast<Type>(); }
        catch { return Array.Empty<Type>(); }
    }

    private static IEnumerable<object> GetStaticRoots(Type type)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!ShouldTraverseMember(field.Name))
                continue;
            object? value = SafeGet(() => field.GetValue(null));
            if (value is not null && !IsSimple(value.GetType()))
                yield return value;
        }
        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !ShouldTraverseMember(property.Name))
                continue;
            object? value = SafeGet(() => property.GetValue(null));
            if (value is not null && !IsSimple(value.GetType()))
                yield return value;
        }
    }

    private static bool LooksLikeRuntimeType(Type type)
    {
        string name = Normalize(type.FullName ?? type.Name);
        return name.Contains("villager") || name.Contains("companion") || name.Contains("partner")
            || name.Contains("config") || name.Contains("setting") || name.Contains("modentry");
    }

    private static bool ShouldTraverseMember(string name)
    {
        string n = Normalize(name);
        return n.Contains("config") || n.Contains("setting") || n.Contains("option")
            || n.Contains("villager") || n.Contains("companion") || n.Contains("partner")
            || n.Contains("manager") || n.Contains("service") || n.Contains("instance")
            || n == "mod";
    }

    private static bool IsSimple(Type type)
        => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);

    private static object? SafeGet(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
