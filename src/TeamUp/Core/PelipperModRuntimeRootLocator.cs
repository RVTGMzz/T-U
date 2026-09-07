using System.Collections;
using System.Reflection;
using StardewModdingAPI;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Resolves Pelipper Town's live Mod instance from SMAPI's IModInfo metadata without calling
/// GetApi&lt;object&gt;. SMAPI only maps mod APIs to public interfaces, so Alpha 6.6.20 deliberately
/// avoids that invalid generic route. Reflection is shallow and only follows metadata members
/// that look like mod/instance/entry containers.
/// </summary>
internal static class PelipperModRuntimeRootLocator
{
    public static bool TryLocate(object? modInfo, out object? root, out string route)
    {
        root = null;
        route = string.Empty;
        if (modInfo is null)
            return false;

        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryLocateObject(modInfo, modInfo.GetType().Name, 0, visited, out root, out route);
    }

    private static bool TryLocateObject(
        object target,
        string path,
        int depth,
        HashSet<object> visited,
        out object? root,
        out string route)
    {
        root = null;
        route = string.Empty;
        if (depth > 4 || !visited.Add(target))
            return false;

        if (IsPelipperRuntimeObject(target))
        {
            root = target;
            route = path;
            return true;
        }

        if (target is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                object? value = entry.Value;
                if (value is null || IsSimple(value.GetType()))
                    continue;
                string key = entry.Key?.ToString() ?? "?";
                if (TryLocateObject(value, $"{path}[{key}]", depth + 1, visited, out root, out route))
                    return true;
            }
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = target.GetType();

        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!ShouldTraverse(field.Name))
                continue;
            object? value = SafeGet(() => field.GetValue(target));
            if (value is null || IsSimple(value.GetType()))
                continue;
            if (TryLocateObject(value, $"{path}.{field.Name}", depth + 1, visited, out root, out route))
                return true;
        }

        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !ShouldTraverse(property.Name))
                continue;
            object? value = SafeGet(() => property.GetValue(target));
            if (value is null || IsSimple(value.GetType()))
                continue;
            if (TryLocateObject(value, $"{path}.{property.Name}", depth + 1, visited, out root, out route))
                return true;
        }

        return false;
    }

    private static bool IsPelipperRuntimeObject(object value)
    {
        Type type = value.GetType();
        string assembly = type.Assembly.GetName().Name ?? string.Empty;
        string fullName = type.FullName ?? type.Name;
        bool pelipperType = assembly.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase)
            || fullName.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase);
        if (!pelipperType)
            return false;

        // Prefer the actual Mod/ModEntry object over arbitrary data classes. The graph bridge can
        // then walk Config and companion manager members from this stable runtime root.
        return value is Mod
            || fullName.Contains("ModEntry", StringComparison.OrdinalIgnoreCase)
            || fullName.EndsWith(".Mod", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldTraverse(string memberName)
    {
        string n = Normalize(memberName);
        return n.Contains("mod") || n.Contains("instance") || n.Contains("entry")
            || n.Contains("implementation") || n.Contains("metadata") || n.Contains("loaded")
            || n.Contains("contentpack") || n.Contains("container") || n.Contains("value");
    }

    private static bool IsSimple(Type type)
        => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
            || type == typeof(DateTime) || type == typeof(TimeSpan) || type == typeof(Guid)
            || typeof(Delegate).IsAssignableFrom(type) || type == typeof(Type) || type == typeof(Assembly);

    private static object? SafeGet(Func<object?> getter)
    {
        try { return getter(); }
        catch { return null; }
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}