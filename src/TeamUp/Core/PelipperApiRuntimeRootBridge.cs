using System.Collections;
using System.Reflection;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.6.20 graph bridge rooted at Pelipper Town's live ModEntry/config object. The 6.6.18 bridge could
/// only discover static roots, while Pelipper's per-villager "Companion enabled" setting lives
/// behind the live mod/API instance. This bridge walks only semantically relevant API/config
/// members, changes the in-memory per-villager enable flag, and asks Pelipper to refresh itself.
/// It never takes render, movement, Halt, controller, or save-data authority from Pelipper.
/// </summary>
internal static class PelipperApiRuntimeRootBridge
{
    private sealed class OverrideState
    {
        public bool OriginalEnabled { get; init; } = true;
        public bool OriginalKnown { get; init; }
    }

    private static object? ApiRoot;
    private static readonly Dictionary<string, OverrideState> Overrides =
        new(StringComparer.OrdinalIgnoreCase);

    public static bool IsConfigured => ApiRoot is not null;
    public static string ApiTypeName => ApiRoot?.GetType().FullName ?? "<none>";

    public static void Configure(object? apiRoot)
    {
        ApiRoot = apiRoot;
    }

    public static void Reset()
    {
        ApiRoot = null;
        Overrides.Clear();
    }

    public static bool TrySetEnabled(string ownerName, NPC? owner, bool enabled, out string route)
    {
        route = string.Empty;
        object? root = ApiRoot;
        if (root is null || string.IsNullOrWhiteSpace(ownerName))
            return false;

        if (!Overrides.ContainsKey(ownerName))
        {
            bool known = TryReadEnabledGraph(root, ownerName, out bool original, out _);
            Overrides[ownerName] = new OverrideState
            {
                OriginalEnabled = known ? original : true,
                OriginalKnown = known
            };
        }

        // The known GMCM option is config-backed. Prefer changing that exact live state before
        // trying public/runtime action methods so Team Up doesn't accidentally hit a look-alike API.
        if (TrySetEnabledGraph(root, ownerName, enabled, out route))
        {
            TryRefreshGraph(root, ownerName, owner);
            return true;
        }

        if (TryInvokeActionGraph(root, ownerName, owner, enabled, out route))
        {
            TryRefreshGraph(root, ownerName, owner);
            return true;
        }

        return false;
    }

    public static bool Restore(string ownerName, NPC? owner, out string route)
    {
        route = string.Empty;
        object? root = ApiRoot;
        if (root is null || !Overrides.TryGetValue(ownerName, out OverrideState? state))
            return false;

        bool desired = state.OriginalKnown ? state.OriginalEnabled : true;
        bool restored = TrySetEnabledGraph(root, ownerName, desired, out route)
            || TryInvokeActionGraph(root, ownerName, owner, desired, out route);
        if (restored)
            TryRefreshGraph(root, ownerName, owner);

        Overrides.Remove(ownerName);
        return restored;
    }

    public static void RestoreAll(Func<string, NPC?> ownerResolver)
    {
        foreach (string ownerName in Overrides.Keys.ToList())
            Restore(ownerName, ownerResolver(ownerName), out _);
        Overrides.Clear();
    }

    private static bool TrySetEnabledGraph(object root, string ownerName, bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TrySetEnabledObject(root, root.GetType().Name, ownerName, enabled, 0, visited, out route);
    }

    private static bool TrySetEnabledObject(
        object target,
        string path,
        string ownerName,
        bool enabled,
        int depth,
        HashSet<object> visited,
        out string route)
    {
        route = string.Empty;
        if (depth > 6 || !visited.Add(target))
            return false;

        if (target is IDictionary dictionary)
        {
            object? matchingKey = FindOwnerKey(dictionary, ownerName);
            if (matchingKey is not null)
            {
                object? value = SafeGet(() => dictionary[matchingKey]);
                if (value is bool && PathLooksCompanionRelated(path))
                {
                    dictionary[matchingKey] = enabled;
                    route = $"api-config:{path}[{ownerName}]";
                    return true;
                }

                if (value is not null && TrySetEnabledMember(value, $"{path}[{ownerName}]", enabled, out route))
                    return true;
            }
        }

        if (TrySetOwnerNamedBool(target, path, ownerName, enabled, out route))
            return true;

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TrySetEnabledObject(child, $"{path}.{name}", ownerName, enabled, depth + 1, visited, out route))
                return true;
        }

        return false;
    }

    private static bool TryReadEnabledGraph(object root, string ownerName, out bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryReadEnabledObject(root, root.GetType().Name, ownerName, 0, visited, out enabled, out route);
    }

    private static bool TryReadEnabledObject(
        object target,
        string path,
        string ownerName,
        int depth,
        HashSet<object> visited,
        out bool enabled,
        out string route)
    {
        enabled = true;
        route = string.Empty;
        if (depth > 6 || !visited.Add(target))
            return false;

        if (target is IDictionary dictionary)
        {
            object? matchingKey = FindOwnerKey(dictionary, ownerName);
            if (matchingKey is not null)
            {
                object? value = SafeGet(() => dictionary[matchingKey]);
                if (value is bool boolean && PathLooksCompanionRelated(path))
                {
                    enabled = boolean;
                    route = $"api-config:{path}[{ownerName}]";
                    return true;
                }

                if (value is not null && TryReadEnabledMember(value, $"{path}[{ownerName}]", out enabled, out route))
                    return true;
            }
        }

        if (TryReadOwnerNamedBool(target, path, ownerName, out enabled, out route))
            return true;

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TryReadEnabledObject(child, $"{path}.{name}", ownerName, depth + 1, visited, out enabled, out route))
                return true;
        }

        return false;
    }

    private static bool TrySetEnabledMember(object target, string path, bool enabled, out string route)
    {
        route = string.Empty;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type type = target.GetType();
        foreach (string name in EnabledMemberNames)
        {
            PropertyInfo? property = type.GetProperty(name, flags);
            if (property?.CanWrite == true && property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0)
            {
                property.SetValue(target, enabled);
                route = $"api-config:{path}.{property.Name}";
                return true;
            }

            FieldInfo? field = type.GetField(name, flags);
            if (field?.FieldType == typeof(bool))
            {
                field.SetValue(target, enabled);
                route = $"api-config:{path}.{field.Name}";
                return true;
            }
        }
        return false;
    }

    private static bool TryReadEnabledMember(object target, string path, out bool enabled, out string route)
    {
        enabled = true;
        route = string.Empty;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
        Type type = target.GetType();
        foreach (string name in EnabledMemberNames)
        {
            PropertyInfo? property = type.GetProperty(name, flags);
            if (property?.CanRead == true && property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0)
            {
                enabled = (bool)(SafeGet(() => property.GetValue(target)) ?? true);
                route = $"api-config:{path}.{property.Name}";
                return true;
            }

            FieldInfo? field = type.GetField(name, flags);
            if (field?.FieldType == typeof(bool))
            {
                enabled = (bool)(SafeGet(() => field.GetValue(target)) ?? true);
                route = $"api-config:{path}.{field.Name}";
                return true;
            }
        }
        return false;
    }

    private static bool TrySetOwnerNamedBool(object target, string path, string ownerName, bool enabled, out string route)
    {
        route = string.Empty;
        string ownerToken = Normalize(ownerName);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanWrite || property.PropertyType != typeof(bool) || property.GetIndexParameters().Length != 0)
                continue;
            string n = Normalize(property.Name);
            if (!n.Contains(ownerToken) || !(n.Contains("companion") || n.Contains("partner")) || !(n.Contains("enable") || n.Contains("show")))
                continue;
            property.SetValue(target, enabled);
            route = $"api-config:{path}.{property.Name}";
            return true;
        }

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (field.FieldType != typeof(bool))
                continue;
            string n = Normalize(field.Name);
            if (!n.Contains(ownerToken) || !(n.Contains("companion") || n.Contains("partner")) || !(n.Contains("enable") || n.Contains("show")))
                continue;
            field.SetValue(target, enabled);
            route = $"api-config:{path}.{field.Name}";
            return true;
        }
        return false;
    }

    private static bool TryReadOwnerNamedBool(object target, string path, string ownerName, out bool enabled, out string route)
    {
        enabled = true;
        route = string.Empty;
        string ownerToken = Normalize(ownerName);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.PropertyType != typeof(bool) || property.GetIndexParameters().Length != 0)
                continue;
            string n = Normalize(property.Name);
            if (!n.Contains(ownerToken) || !(n.Contains("companion") || n.Contains("partner")) || !(n.Contains("enable") || n.Contains("show")))
                continue;
            enabled = (bool)(SafeGet(() => property.GetValue(target)) ?? true);
            route = $"api-config:{path}.{property.Name}";
            return true;
        }

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (field.FieldType != typeof(bool))
                continue;
            string n = Normalize(field.Name);
            if (!n.Contains(ownerToken) || !(n.Contains("companion") || n.Contains("partner")) || !(n.Contains("enable") || n.Contains("show")))
                continue;
            enabled = (bool)(SafeGet(() => field.GetValue(target)) ?? true);
            route = $"api-config:{path}.{field.Name}";
            return true;
        }
        return false;
    }

    private static bool TryInvokeActionGraph(object root, string ownerName, NPC? owner, bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryInvokeActionObject(root, root.GetType().Name, ownerName, owner, enabled, 0, visited, out route);
    }

    private static bool TryInvokeActionObject(
        object target,
        string path,
        string ownerName,
        NPC? owner,
        bool enabled,
        int depth,
        HashSet<object> visited,
        out string route)
    {
        route = string.Empty;
        if (depth > 4 || !visited.Add(target))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in target.GetType().GetMethods(flags))
        {
            if (method.IsGenericMethodDefinition)
                continue;
            string n = Normalize(method.Name);
            bool companion = n.Contains("companion") || n.Contains("partner");
            bool villager = n.Contains("villager") || n.Contains("npc");
            bool setter = n.Contains("set") || n.Contains("enable") || n.Contains("disable")
                || n.Contains("show") || n.Contains("hide") || n.Contains("recall") || n.Contains("deploy");
            if (!companion || !setter)
                continue;

            ParameterInfo[] p = method.GetParameters();
            object?[]? args = null;
            if (p.Length == 2 && p[1].ParameterType == typeof(bool)
                && (villager || n == "setcompanionenabled" || n == "setpartnerenabled"))
            {
                if (p[0].ParameterType == typeof(string))
                    args = new object?[] { ownerName, enabled };
                else if (owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
                    args = new object?[] { owner, enabled };
            }
            else if (p.Length == 1 && villager)
            {
                bool enableMethod = n.Contains("enable") || n.Contains("show") || n.Contains("deploy");
                bool disableMethod = n.Contains("disable") || n.Contains("hide") || n.Contains("recall");
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
                object? result = method.Invoke(target, args);
                if (method.ReturnType == typeof(bool) && result is bool ok && !ok)
                    continue;
                route = $"api-method:{path}.{method.Name}";
                return true;
            }
            catch
            {
            }
        }

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TryInvokeActionObject(child, $"{path}.{name}", ownerName, owner, enabled, depth + 1, visited, out route))
                return true;
        }
        return false;
    }

    private static void TryRefreshGraph(object root, string ownerName, NPC? owner)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        TryRefreshObject(root, ownerName, owner, 0, visited);
    }

    private static bool TryRefreshObject(object target, string ownerName, NPC? owner, int depth, HashSet<object> visited)
    {
        if (depth > 4 || !visited.Add(target))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in target.GetType().GetMethods(flags))
        {
            if (method.IsGenericMethodDefinition)
                continue;
            string n = Normalize(method.Name);
            bool refresh = n.Contains("refresh") || n.Contains("rebuild") || n.Contains("sync") || n.Contains("reload") || n.Contains("apply");
            bool companion = n.Contains("villager") && (n.Contains("companion") || n.Contains("partner"));
            if (!refresh || !companion)
                continue;

            ParameterInfo[] p = method.GetParameters();
            object?[]? args = p.Length == 0
                ? Array.Empty<object?>()
                : p.Length == 1 && p[0].ParameterType == typeof(string)
                    ? new object?[] { ownerName }
                    : p.Length == 1 && owner is not null && p[0].ParameterType.IsInstanceOfType(owner)
                        ? new object?[] { owner }
                        : null;
            if (args is null)
                continue;

            try
            {
                method.Invoke(target, args);
                return true;
            }
            catch
            {
            }
        }

        foreach ((_, object child) in GetRelevantChildren(target))
        {
            if (TryRefreshObject(child, ownerName, owner, depth + 1, visited))
                return true;
        }
        return false;
    }

    private static IEnumerable<(string Name, object Value)> GetRelevantChildren(object target)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!ShouldTraverse(field.Name))
                continue;
            object? value = SafeGet(() => field.GetValue(target));
            if (value is not null && !IsSimple(value.GetType()))
                yield return (field.Name, value);
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !ShouldTraverse(property.Name))
                continue;
            object? value = SafeGet(() => property.GetValue(target));
            if (value is not null && !IsSimple(value.GetType()))
                yield return (property.Name, value);
        }
    }

    private static object? FindOwnerKey(IDictionary dictionary, string ownerName)
    {
        foreach (object? key in dictionary.Keys)
        {
            if (key is string text && text.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                return key;
        }
        return null;
    }

    private static bool PathLooksCompanionRelated(string path)
    {
        string n = Normalize(path);
        return n.Contains("villager") || n.Contains("companion") || n.Contains("partner");
    }

    private static bool ShouldTraverse(string name)
    {
        string n = Normalize(name);
        return n == "mod" || n.Contains("modentry") || n.Contains("entry") || n.Contains("instance")
            || n.Contains("config") || n.Contains("setting") || n.Contains("option")
            || n.Contains("villager") || n.Contains("companion") || n.Contains("partner")
            || n.Contains("manager") || n.Contains("service") || n.Contains("state") || n.Contains("data");
    }

    private static bool IsSimple(Type type)
        => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid);

    private static object? SafeGet(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static readonly string[] EnabledMemberNames =
    {
        "CompanionEnabled", "IsCompanionEnabled", "PartnerEnabled", "IsPartnerEnabled",
        "Enabled", "IsEnabled", "ShowCompanion", "ShowPartner"
    };
}