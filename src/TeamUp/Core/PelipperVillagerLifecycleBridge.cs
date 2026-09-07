using System.Collections;
using System.Reflection;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.6.21: source-native villager Pokemon lifecycle bridge for Pelipper Town.
///
/// 6.6.20 reached PelipperTown.ModEntry correctly but only understood simple bool-backed config
/// plus a narrow set of refresh/action names. Pelipper can represent per-villager visibility with
/// enabled dictionaries, disabled/hidden string collections, or runtime manager methods such as
/// Remove/Despawn/Recall/Reconcile/Update. This bridge understands those shapes while preserving
/// Pelipper as the sole render/movement authority.
/// </summary>
internal static class PelipperVillagerLifecycleBridge
{
    private sealed class OverrideState
    {
        public bool OriginalEnabled { get; init; } = true;
        public bool OriginalKnown { get; init; }
    }

    private static object? RuntimeRoot;
    private static readonly Dictionary<string, OverrideState> Overrides = new(StringComparer.OrdinalIgnoreCase);

    public static string RootTypeName => RuntimeRoot?.GetType().FullName ?? "<none>";

    public static void Configure(object? runtimeRoot)
    {
        if (runtimeRoot is not null)
            RuntimeRoot = runtimeRoot;
    }

    public static void Reset()
    {
        RuntimeRoot = null;
        Overrides.Clear();
    }

    public static bool TrySetEnabled(string ownerName, NPC? owner, bool enabled, out string route)
    {
        route = string.Empty;
        object? root = RuntimeRoot;
        if (root is null || string.IsNullOrWhiteSpace(ownerName))
            return false;

        if (!Overrides.ContainsKey(ownerName))
        {
            bool known = TryReadConfigEnabled(root, ownerName, out bool original, out _);
            Overrides[ownerName] = new OverrideState
            {
                OriginalEnabled = known ? original : true,
                OriginalKnown = known
            };
        }

        List<string> routes = new();

        // Persist the desired per-villager source state first. This prevents Pelipper from
        // immediately respawning a companion after a runtime Remove/Despawn call.
        if (TrySetConfigEnabled(root, ownerName, enabled, out string configRoute))
            routes.Add(configRoute);

        // Invoke the source's own lifecycle action when present. 6.6.20 did not recognise
        // Remove/Despawn/Dismiss/Deactivate/Update/Reconcile families.
        if (TryInvokeLifecycleGraph(root, ownerName, owner, enabled, out string lifecycleRoute))
            routes.Add(lifecycleRoute);
        else if (TryInvokeStaticLifecycle(root.GetType().Assembly, ownerName, owner, enabled, out lifecycleRoute))
            routes.Add(lifecycleRoute);

        // A config change may only be consumed by a reconciliation/update pass.
        if (TryInvokeReconcileGraph(root, ownerName, owner, out string reconcileRoute))
            routes.Add(reconcileRoute);
        else if (TryInvokeStaticReconcile(root.GetType().Assembly, ownerName, owner, out reconcileRoute))
            routes.Add(reconcileRoute);

        if (routes.Count == 0)
            return false;

        route = string.Join(" + ", routes.Distinct(StringComparer.OrdinalIgnoreCase));
        return true;
    }

    public static bool Restore(string ownerName, NPC? owner, out string route)
    {
        route = string.Empty;
        if (!Overrides.TryGetValue(ownerName, out OverrideState? state))
            return false;

        bool desired = state.OriginalKnown ? state.OriginalEnabled : true;
        bool result = TrySetEnabled(ownerName, owner, desired, out route);
        Overrides.Remove(ownerName);
        return result;
    }

    public static void RestoreAll(Func<string, NPC?> ownerResolver)
    {
        foreach (string ownerName in Overrides.Keys.ToList())
            Restore(ownerName, ownerResolver(ownerName), out _);
        Overrides.Clear();
    }

    public static IReadOnlyList<string> Probe(string ownerName, int maxLines = 40)
    {
        List<string> lines = new();
        object? root = RuntimeRoot;
        if (root is null)
        {
            lines.Add("runtime root: <none>");
            return lines;
        }

        lines.Add($"runtime root: {root.GetType().FullName}");
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        ProbeObject(root, root.GetType().Name, ownerName, 0, visited, lines, maxLines);

        if (lines.Count < maxLines)
            ProbeStaticMethods(root.GetType().Assembly, lines, maxLines);

        return lines.Take(maxLines).ToList();
    }

    private static bool TrySetConfigEnabled(object root, string ownerName, bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TrySetConfigEnabledObject(root, root.GetType().Name, ownerName, enabled, 0, visited, out route);
    }

    private static bool TrySetConfigEnabledObject(
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

        bool pathRelevant = LooksCompanionPath(path);
        bool negativePath = LooksNegativeEnabledPath(path);
        bool positivePath = LooksPositiveEnabledPath(path);

        if (target is IDictionary dictionary)
        {
            object? key = FindStringKey(dictionary.Keys, ownerName);
            if (key is not null)
            {
                object? value = SafeGet(() => dictionary[key]);
                if (value is bool && pathRelevant)
                {
                    dictionary[key] = negativePath ? !enabled : enabled;
                    route = $"config-dictionary:{path}[{ownerName}]";
                    return true;
                }

                if (value is not null && TrySetEnabledMember(value, $"{path}[{ownerName}]", enabled, out route))
                    return true;
            }
        }

        // HashSet<string>, List<string>, and similar collections are a common compact way to
        // store only disabled/hidden villagers. Handle both negative and positive semantics.
        if (pathRelevant && (negativePath || positivePath)
            && TrySetStringCollectionMembership(target, ownerName, shouldContain: negativePath ? !enabled : enabled, out bool collectionChanged))
        {
            route = $"config-string-collection:{path}[{ownerName}]={(negativePath ? !enabled : enabled)}{(collectionChanged ? "" : " (already)")}";
            return true;
        }

        if (TrySetOwnerNamedBool(target, path, ownerName, enabled, out route))
            return true;

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TrySetConfigEnabledObject(child, $"{path}.{name}", ownerName, enabled, depth + 1, visited, out route))
                return true;
        }

        return false;
    }

    private static bool TryReadConfigEnabled(object root, string ownerName, out bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryReadConfigEnabledObject(root, root.GetType().Name, ownerName, 0, visited, out enabled, out route);
    }

    private static bool TryReadConfigEnabledObject(
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

        bool pathRelevant = LooksCompanionPath(path);
        bool negativePath = LooksNegativeEnabledPath(path);
        bool positivePath = LooksPositiveEnabledPath(path);

        if (target is IDictionary dictionary)
        {
            object? key = FindStringKey(dictionary.Keys, ownerName);
            if (key is not null)
            {
                object? value = SafeGet(() => dictionary[key]);
                if (value is bool boolean && pathRelevant)
                {
                    enabled = negativePath ? !boolean : boolean;
                    route = $"config-dictionary:{path}[{ownerName}]";
                    return true;
                }

                if (value is not null && TryReadEnabledMember(value, $"{path}[{ownerName}]", out enabled, out route))
                    return true;
            }
        }

        if (pathRelevant && (negativePath || positivePath)
            && TryContainsString(target, ownerName, out bool contains))
        {
            enabled = negativePath ? !contains : contains;
            route = $"config-string-collection:{path}[{ownerName}]";
            return true;
        }

        if (TryReadOwnerNamedBool(target, path, ownerName, out enabled, out route))
            return true;

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TryReadConfigEnabledObject(child, $"{path}.{name}", ownerName, depth + 1, visited, out enabled, out route))
                return true;
        }

        return false;
    }

    private static bool TryInvokeLifecycleGraph(object root, string ownerName, NPC? owner, bool enabled, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryInvokeLifecycleObject(root, root.GetType().Name, ownerName, owner, enabled, 0, visited, out route);
    }

    private static bool TryInvokeLifecycleObject(
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
        if (depth > 5 || !visited.Add(target))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in target.GetType().GetMethods(flags))
        {
            if (TryInvokeLifecycleMethod(target, method, path, ownerName, owner, enabled, out route))
                return true;
        }

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TryInvokeLifecycleObject(child, $"{path}.{name}", ownerName, owner, enabled, depth + 1, visited, out route))
                return true;
        }

        return false;
    }

    private static bool TryInvokeStaticLifecycle(Assembly assembly, string ownerName, NPC? owner, bool enabled, out string route)
    {
        route = string.Empty;
        foreach (Type type in SafeGetTypes(assembly))
        {
            if (!LooksRuntimeType(type))
                continue;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (TryInvokeLifecycleMethod(null, method, type.FullName ?? type.Name, ownerName, owner, enabled, out route))
                    return true;
            }
        }
        return false;
    }

    private static bool TryInvokeLifecycleMethod(
        object? target,
        MethodInfo method,
        string path,
        string ownerName,
        NPC? owner,
        bool enabled,
        out string route)
    {
        route = string.Empty;
        if (method.IsGenericMethodDefinition || method.IsSpecialName)
            return false;

        string n = Normalize(method.Name);
        if (!LooksCompanionActionName(n, enabled))
            return false;

        ParameterInfo[] p = method.GetParameters();
        object?[]? args = null;

        if (p.Length == 1)
        {
            if (p[0].ParameterType == typeof(string))
                args = new object?[] { ownerName };
            else if (owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
                args = new object?[] { owner };
        }
        else if (p.Length == 2 && p[1].ParameterType == typeof(bool)
            && (n.Contains("set") || n.Contains("enable") || n.Contains("disable") || n.Contains("visible") || n.Contains("show") || n.Contains("hide")))
        {
            if (p[0].ParameterType == typeof(string))
                args = new object?[] { ownerName, enabled };
            else if (owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
                args = new object?[] { owner, enabled };
        }

        if (args is null)
            return false;

        try
        {
            object? result = method.Invoke(target, args);
            if (method.ReturnType == typeof(bool) && result is bool ok && !ok)
                return false;
            route = $"lifecycle:{path}.{method.Name}";
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryInvokeReconcileGraph(object root, string ownerName, NPC? owner, out string route)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryInvokeReconcileObject(root, root.GetType().Name, ownerName, owner, 0, visited, out route);
    }

    private static bool TryInvokeReconcileObject(
        object target,
        string path,
        string ownerName,
        NPC? owner,
        int depth,
        HashSet<object> visited,
        out string route)
    {
        route = string.Empty;
        if (depth > 5 || !visited.Add(target))
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in target.GetType().GetMethods(flags))
        {
            if (TryInvokeReconcileMethod(target, method, path, ownerName, owner, out route))
                return true;
        }

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            if (TryInvokeReconcileObject(child, $"{path}.{name}", ownerName, owner, depth + 1, visited, out route))
                return true;
        }
        return false;
    }

    private static bool TryInvokeStaticReconcile(Assembly assembly, string ownerName, NPC? owner, out string route)
    {
        route = string.Empty;
        foreach (Type type in SafeGetTypes(assembly))
        {
            if (!LooksRuntimeType(type))
                continue;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (TryInvokeReconcileMethod(null, method, type.FullName ?? type.Name, ownerName, owner, out route))
                    return true;
            }
        }
        return false;
    }

    private static bool TryInvokeReconcileMethod(object? target, MethodInfo method, string path, string ownerName, NPC? owner, out string route)
    {
        route = string.Empty;
        if (method.IsGenericMethodDefinition || method.IsSpecialName)
            return false;

        string n = Normalize(method.Name);
        bool relation = n.Contains("villager") || n.Contains("companion") || n.Contains("partner") || n.Contains("pokemon");
        bool refresh = n.Contains("update") || n.Contains("refresh") || n.Contains("reconcile") || n.Contains("rebuild")
            || n.Contains("sync") || n.Contains("reload") || n.Contains("apply") || n.Contains("ensure") || n.Contains("maintain")
            || n.Contains("populate") || n.Contains("prune");
        if (!relation || !refresh)
            return false;

        ParameterInfo[] p = method.GetParameters();
        object?[]? args = null;
        if (p.Length == 0)
            args = Array.Empty<object?>();
        else if (p.Length == 1 && p[0].ParameterType == typeof(string))
            args = new object?[] { ownerName };
        else if (p.Length == 1 && owner is not null && p[0].ParameterType.IsInstanceOfType(owner))
            args = new object?[] { owner };

        if (args is null)
            return false;

        try
        {
            method.Invoke(target, args);
            route = $"reconcile:{path}.{method.Name}";
            return true;
        }
        catch
        {
            return false;
        }
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
                route = $"config-member:{path}.{property.Name}";
                return true;
            }
            FieldInfo? field = type.GetField(name, flags);
            if (field?.FieldType == typeof(bool))
            {
                field.SetValue(target, enabled);
                route = $"config-member:{path}.{field.Name}";
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
                route = $"config-member:{path}.{property.Name}";
                return true;
            }
            FieldInfo? field = type.GetField(name, flags);
            if (field?.FieldType == typeof(bool))
            {
                enabled = (bool)(SafeGet(() => field.GetValue(target)) ?? true);
                route = $"config-member:{path}.{field.Name}";
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
            if (!n.Contains(ownerToken) || !LooksCompanionPath(n))
                continue;
            property.SetValue(target, LooksNegativeEnabledPath(n) ? !enabled : enabled);
            route = $"config-owner-bool:{path}.{property.Name}";
            return true;
        }

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (field.FieldType != typeof(bool))
                continue;
            string n = Normalize(field.Name);
            if (!n.Contains(ownerToken) || !LooksCompanionPath(n))
                continue;
            field.SetValue(target, LooksNegativeEnabledPath(n) ? !enabled : enabled);
            route = $"config-owner-bool:{path}.{field.Name}";
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
            if (!n.Contains(ownerToken) || !LooksCompanionPath(n))
                continue;
            bool value = (bool)(SafeGet(() => property.GetValue(target)) ?? true);
            enabled = LooksNegativeEnabledPath(n) ? !value : value;
            route = $"config-owner-bool:{path}.{property.Name}";
            return true;
        }

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (field.FieldType != typeof(bool))
                continue;
            string n = Normalize(field.Name);
            if (!n.Contains(ownerToken) || !LooksCompanionPath(n))
                continue;
            bool value = (bool)(SafeGet(() => field.GetValue(target)) ?? true);
            enabled = LooksNegativeEnabledPath(n) ? !value : value;
            route = $"config-owner-bool:{path}.{field.Name}";
            return true;
        }
        return false;
    }

    private static bool TrySetStringCollectionMembership(object target, string ownerName, bool shouldContain, out bool changed)
    {
        changed = false;
        if (!TryContainsString(target, ownerName, out bool contains))
            return false;
        if (contains == shouldContain)
            return true;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        string methodName = shouldContain ? "Add" : "Remove";
        MethodInfo? method = target.GetType().GetMethods(flags)
            .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase)
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(string));
        if (method is null)
            return false;

        try
        {
            method.Invoke(target, new object?[] { ownerName });
            changed = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryContainsString(object target, string ownerName, out bool contains)
    {
        contains = false;
        if (target is string || target is IDictionary)
            return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo? containsMethod = target.GetType().GetMethods(flags)
            .FirstOrDefault(m => m.Name.Equals("Contains", StringComparison.OrdinalIgnoreCase)
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(string)
                && m.ReturnType == typeof(bool));
        if (containsMethod is null)
            return false;

        try
        {
            contains = (bool)(containsMethod.Invoke(target, new object?[] { ownerName }) ?? false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static object? FindStringKey(ICollection keys, string ownerName)
    {
        foreach (object? key in keys)
        {
            if (key is string text && text.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                return key;
        }
        return null;
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

    private static void ProbeObject(
        object target,
        string path,
        string ownerName,
        int depth,
        HashSet<object> visited,
        List<string> lines,
        int maxLines)
    {
        if (lines.Count >= maxLines || depth > 5 || !visited.Add(target))
            return;

        Type type = target.GetType();
        if (LooksCompanionPath(path))
        {
            if (target is IDictionary dictionary)
            {
                object? key = FindStringKey(dictionary.Keys, ownerName);
                if (key is not null)
                {
                    object? value = SafeGet(() => dictionary[key]);
                    lines.Add($"candidate config dictionary {path}[{ownerName}] type={value?.GetType().FullName ?? "<null>"} value={FormatValue(value)}");
                }
            }

            if ((LooksNegativeEnabledPath(path) || LooksPositiveEnabledPath(path))
                && TryContainsString(target, ownerName, out bool contains))
            {
                lines.Add($"candidate string collection {path} contains({ownerName})={contains}");
            }
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (MethodInfo method in type.GetMethods(flags))
        {
            string n = Normalize(method.Name);
            if ((LooksCompanionActionName(n, false) || LooksCompanionActionName(n, true) || LooksReconcileName(n))
                && lines.Count < maxLines)
            {
                lines.Add($"candidate method {path}.{FormatMethod(method)}");
            }
        }

        foreach ((string name, object child) in GetRelevantChildren(target))
        {
            ProbeObject(child, $"{path}.{name}", ownerName, depth + 1, visited, lines, maxLines);
            if (lines.Count >= maxLines)
                return;
        }
    }

    private static void ProbeStaticMethods(Assembly assembly, List<string> lines, int maxLines)
    {
        foreach (Type type in SafeGetTypes(assembly))
        {
            if (!LooksRuntimeType(type))
                continue;
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                string n = Normalize(method.Name);
                if (!(LooksCompanionActionName(n, false) || LooksCompanionActionName(n, true) || LooksReconcileName(n)))
                    continue;
                lines.Add($"candidate static {type.FullName}.{FormatMethod(method)}");
                if (lines.Count >= maxLines)
                    return;
            }
        }
    }

    private static bool LooksCompanionActionName(string normalizedName, bool enabled)
    {
        bool relation = normalizedName.Contains("villager") || normalizedName.Contains("npc")
            || normalizedName.Contains("companion") || normalizedName.Contains("partner") || normalizedName.Contains("pokemon");
        if (!relation)
            return false;

        if (enabled)
        {
            return normalizedName.Contains("spawn") || normalizedName.Contains("add") || normalizedName.Contains("deploy")
                || normalizedName.Contains("summon") || normalizedName.Contains("activate") || normalizedName.Contains("enable")
                || normalizedName.Contains("show") || normalizedName.Contains("create") || normalizedName.Contains("ensure");
        }

        return normalizedName.Contains("recall") || normalizedName.Contains("remove") || normalizedName.Contains("despawn")
            || normalizedName.Contains("dismiss") || normalizedName.Contains("deactivate") || normalizedName.Contains("disable")
            || normalizedName.Contains("hide") || normalizedName.Contains("clear") || normalizedName.Contains("stop")
            || normalizedName.Contains("prune");
    }

    private static bool LooksReconcileName(string normalizedName)
    {
        bool relation = normalizedName.Contains("villager") || normalizedName.Contains("companion")
            || normalizedName.Contains("partner") || normalizedName.Contains("pokemon");
        bool action = normalizedName.Contains("update") || normalizedName.Contains("refresh") || normalizedName.Contains("reconcile")
            || normalizedName.Contains("rebuild") || normalizedName.Contains("sync") || normalizedName.Contains("reload")
            || normalizedName.Contains("apply") || normalizedName.Contains("ensure") || normalizedName.Contains("maintain")
            || normalizedName.Contains("populate") || normalizedName.Contains("prune");
        return relation && action;
    }

    private static bool LooksCompanionPath(string path)
    {
        string n = Normalize(path);
        return n.Contains("villager") || n.Contains("npc") || n.Contains("companion") || n.Contains("partner") || n.Contains("pokemon");
    }

    private static bool LooksNegativeEnabledPath(string path)
    {
        string n = Normalize(path);
        return n.Contains("disabled") || n.Contains("disable") || n.Contains("hidden") || n.Contains("hide")
            || n.Contains("suppressed") || n.Contains("suppress") || n.Contains("excluded") || n.Contains("exclude")
            || n.Contains("optout") || n.Contains("inactive") || n.Contains("blocked");
    }

    private static bool LooksPositiveEnabledPath(string path)
    {
        string n = Normalize(path);
        return n.Contains("enabled") || n.Contains("enable") || n.Contains("visible") || n.Contains("shown")
            || n.Contains("active") || n.Contains("allowed");
    }

    private static bool ShouldTraverse(string name)
    {
        string n = Normalize(name);
        return n.Contains("config") || n.Contains("setting") || n.Contains("option") || n.Contains("villager")
            || n.Contains("npc") || n.Contains("companion") || n.Contains("partner") || n.Contains("pokemon")
            || n.Contains("manager") || n.Contains("service") || n.Contains("runtime") || n.Contains("state")
            || n.Contains("registry") || n.Contains("data") || n.Contains("instance") || n == "mod";
    }

    private static bool LooksRuntimeType(Type type)
    {
        string n = Normalize(type.FullName ?? type.Name);
        return n.Contains("modentry") || n.Contains("villager") || n.Contains("companion")
            || n.Contains("partner") || n.Contains("pokemon") || n.Contains("config") || n.Contains("manager")
            || n.Contains("service");
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null).Cast<Type>(); }
        catch { return Array.Empty<Type>(); }
    }

    private static bool IsSimple(Type type)
        => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid)
            || type == typeof(Type) || typeof(Delegate).IsAssignableFrom(type);

    private static object? SafeGet(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string FormatValue(object? value)
        => value switch
        {
            null => "<null>",
            string text => text,
            bool boolean => boolean.ToString(),
            Enum e => e.ToString(),
            _ => value.GetType().Name
        };

    private static string FormatMethod(MethodInfo method)
        => $"{method.Name}({string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name))})";

    private static readonly string[] EnabledMemberNames =
    {
        "CompanionEnabled", "IsCompanionEnabled", "PartnerEnabled", "IsPartnerEnabled",
        "PokemonEnabled", "IsPokemonEnabled", "Enabled", "IsEnabled", "Visible", "IsVisible",
        "ShowCompanion", "ShowPartner", "ShowPokemon"
    };
}
