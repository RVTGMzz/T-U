using System.Reflection;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.6.25 exact bridge for the Pelipper Town 1.1.9 surface verified from the user's
/// PelipperTown.Mod.dll (SHA256 7b4a0461a9ecffb62230887d3ef1be849404a644ac53b7e8aa149c663eb3c697).
///
/// Verified native signatures:
/// - PelipperTown.VillagerCompanionManager.SetConfiguredCompanionEnabled(string, bool)
/// - PelipperTown.VillagerCompanionManager.RefreshAssignments()
/// - PelipperTown.VillagerCompanionManager.ApplyConfiguredAssignments()
/// - PelipperTown.ModEntry.RecallToBall(long, bool)
/// - PelipperTown.ModEntry.DeployBesideOwner(long, bool)
///
/// IL inspection confirms ApplyConfiguredAssignments() calls VillagerCompanionRuntime.Despawn()
/// for disabled villagers. Team Up therefore asks the source manager to own the full lifecycle;
/// it never hides, halts, or takes controller ownership of Pelipper actors.
/// </summary>
internal static class PelipperTown119NativeBridge
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static object? RuntimeRoot;
    private static object? VillagerManager;
    private static readonly Dictionary<string, bool> OriginalVillagerEnabled = new(StringComparer.OrdinalIgnoreCase);

    public static string Status
        => RuntimeRoot is null
            ? "root=<none>"
            : $"root={RuntimeRoot.GetType().FullName}, manager={VillagerManager?.GetType().FullName ?? "<none>"}";

    public static void Configure(object runtimeRoot)
    {
        RuntimeRoot = runtimeRoot;
        VillagerManager = ResolveVillagerManager(runtimeRoot);
    }

    public static void Reset()
    {
        RuntimeRoot = null;
        VillagerManager = null;
        OriginalVillagerEnabled.Clear();
    }

    public static bool TrySetVillagerCompanionEnabled(string npcName, bool enabled, out string route)
    {
        route = string.Empty;
        object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));
        if (manager is null || string.IsNullOrWhiteSpace(npcName))
            return false;

        Type type = manager.GetType();
        MethodInfo? getEnabled = type.GetMethod(
            "GetConfiguredCompanionEnabled",
            InstanceFlags,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        MethodInfo? setEnabled = type.GetMethod(
            "SetConfiguredCompanionEnabled",
            InstanceFlags,
            binder: null,
            types: new[] { typeof(string), typeof(bool) },
            modifiers: null);
        MethodInfo? refresh = type.GetMethod("RefreshAssignments", InstanceFlags, binder: null, Type.EmptyTypes, modifiers: null);
        MethodInfo? apply = type.GetMethod("ApplyConfiguredAssignments", InstanceFlags, binder: null, Type.EmptyTypes, modifiers: null);
        MethodInfo? update = type.GetMethod("Update", InstanceFlags, binder: null, Type.EmptyTypes, modifiers: null);

        if (setEnabled is null || apply is null)
            return false;

        try
        {
            if (!OriginalVillagerEnabled.ContainsKey(npcName) && getEnabled is not null)
            {
                object? original = getEnabled.Invoke(manager, new object?[] { npcName });
                if (original is bool originalBool)
                    OriginalVillagerEnabled[npcName] = originalBool;
            }

            setEnabled.Invoke(manager, new object?[] { npcName, enabled });
            refresh?.Invoke(manager, null);

            // This is the decisive 1.1.9 lifecycle call. Its IL removes the disabled runtime and
            // invokes VillagerCompanionRuntime.Despawn(), so sourceLive can converge immediately.
            apply.Invoke(manager, null);

            // Enabling may need one normal manager update before the actor becomes source-live.
            // Calling the source manager's own Update is safer than synthesizing actor state.
            if (enabled && Context.IsWorldReady)
                update?.Invoke(manager, null);

            route = "native119:VillagerCompanionManager.SetConfiguredCompanionEnabled+ApplyConfiguredAssignments";
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool RestoreVillager(string npcName, out string route)
    {
        route = string.Empty;
        if (!OriginalVillagerEnabled.TryGetValue(npcName, out bool original))
            return false;

        bool ok = TrySetVillagerCompanionEnabled(npcName, original, out route);
        if (ok)
            OriginalVillagerEnabled.Remove(npcName);
        return ok;
    }

    public static void RestoreAll()
    {
        foreach (string name in OriginalVillagerEnabled.Keys.ToList())
            RestoreVillager(name, out _);
        OriginalVillagerEnabled.Clear();
    }

    public static bool TrySetPlayerDeployment(long ownerId, bool deployed, out string route)
    {
        route = string.Empty;
        object? root = RuntimeRoot;
        if (root is null)
            return false;

        string methodName = deployed ? "DeployBesideOwner" : "RecallToBall";
        MethodInfo? method = root.GetType().GetMethod(
            methodName,
            InstanceFlags,
            binder: null,
            types: new[] { typeof(long), typeof(bool) },
            modifiers: null);
        if (method is null || method.ReturnType != typeof(bool))
            return false;

        try
        {
            object? result = method.Invoke(root, new object?[] { ownerId, false });
            bool ok = result is bool value && value;
            if (ok)
                route = $"native119:ModEntry.{methodName}(ownerId,false)";
            return ok;
        }
        catch
        {
            return false;
        }
    }

    private static object? ResolveVillagerManager(object root)
    {
        Type rootType = root.GetType();
        if (!string.Equals(rootType.FullName, "PelipperTown.ModEntry", StringComparison.Ordinal))
            return null;

        // Exact 1.1.9 field verified from metadata.
        FieldInfo? exact = rootType.GetField("_villagerCompanions", InstanceFlags);
        object? value = SafeGet(() => exact?.GetValue(root));
        if (value?.GetType().FullName == "PelipperTown.VillagerCompanionManager")
            return value;

        // Narrow structural fallback for a future 1.1.x field rename; never traverses arbitrary
        // graphs or mutates unknown objects.
        foreach (FieldInfo field in rootType.GetFields(InstanceFlags))
        {
            if (field.FieldType.FullName != "PelipperTown.VillagerCompanionManager")
                continue;
            value = SafeGet(() => field.GetValue(root));
            if (value is not null)
                return value;
        }

        return null;
    }

    private static T? SafeGet<T>(Func<T?> getter)
    {
        try { return getter(); }
        catch { return default; }
    }
}
