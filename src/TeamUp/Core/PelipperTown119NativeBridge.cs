using System.Collections;
using System.Reflection;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Exact bridge for the Pelipper Town 1.1.9 surface verified from the user's PelipperTown.Mod.dll
/// (SHA256 7b4a0461a9ecffb62230887d3ef1be849404a644ac53b7e8aa149c663eb3c697).
///
/// Verified native signatures:
/// - PelipperTown.VillagerCompanionManager.SetConfiguredCompanionEnabled(string, bool)
/// - PelipperTown.VillagerCompanionManager.RefreshAssignments()
/// - PelipperTown.VillagerCompanionManager.ApplyConfiguredAssignments()
/// - PelipperTown.ModEntry.RecallToBall(long, bool)
/// - PelipperTown.ModEntry.DeployBesideOwner(long, bool)
///
/// Alpha 6.6.26 metadata audit additionally verified:
/// - VillagerCompanionManager._runtimes = Dictionary&lt;string, VillagerCompanionRuntime&gt;
/// - VillagerCompanionRuntime._npcName
/// - VillagerCompanionRuntime._entity
/// This gives Team Up an exact owner -> live actor lookup and removes the need for proximity-based
/// Pelipper ownership guesses when the 1.1.9 runtime is bound.
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
            : $"root={RuntimeRoot.GetType().FullName}, manager={VillagerManager?.GetType().FullName ?? "<none>"}, npcNative={HasVillagerLifecycle}, playerNative={HasPlayerLifecycle}, exactOwnerMap={HasExactVillagerRuntimeMap}";

    public static bool HasVillagerLifecycle
    {
        get
        {
            object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));
            if (manager?.GetType().FullName != "PelipperTown.VillagerCompanionManager")
                return false;

            Type type = manager.GetType();
            return type.GetMethod(
                    "SetConfiguredCompanionEnabled",
                    InstanceFlags,
                    binder: null,
                    types: new[] { typeof(string), typeof(bool) },
                    modifiers: null) is not null
                && type.GetMethod(
                    "ApplyConfiguredAssignments",
                    InstanceFlags,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null) is not null;
        }
    }

    public static bool HasExactVillagerRuntimeMap
    {
        get
        {
            object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));
            if (manager?.GetType().FullName != "PelipperTown.VillagerCompanionManager")
                return false;

            FieldInfo? field = manager.GetType().GetField("_runtimes", InstanceFlags);
            return field?.GetValue(manager) is IDictionary;
        }
    }

    public static bool HasPlayerLifecycle
    {
        get
        {
            object? root = RuntimeRoot;
            if (root?.GetType().FullName != "PelipperTown.ModEntry")
                return false;

            Type type = root.GetType();
            MethodInfo? recall = type.GetMethod(
                "RecallToBall",
                InstanceFlags,
                binder: null,
                types: new[] { typeof(long), typeof(bool) },
                modifiers: null);
            MethodInfo? deploy = type.GetMethod(
                "DeployBesideOwner",
                InstanceFlags,
                binder: null,
                types: new[] { typeof(long), typeof(bool) },
                modifiers: null);
            return recall?.ReturnType == typeof(bool) && deploy?.ReturnType == typeof(bool);
        }
    }

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

    /// <summary>
    /// Returns true when the exact 1.1.9 runtime map was queried successfully. A null descriptor
    /// then means the NPC has no live Pelipper companion. Returning false means callers may use
    /// older compatibility detection because the exact map isn't available.
    /// </summary>
    public static bool TryGetVillagerCompanionDescriptor(string npcName, out LiveCompanionDescriptor? descriptor)
    {
        descriptor = null;
        if (!TryGetVillagerCompanionActor(npcName, out NPC? actor))
            return false;
        if (actor is null)
            return true;

        string? stableId = TryGetStablePelipperUnitId(actor);
        string unitId = !string.IsNullOrWhiteSpace(stableId)
            ? $"{PelipperTownCompatibilityService.ProviderId}:{stableId}"
            : $"{PelipperTownCompatibilityService.ProviderId}:npc:{npcName}:{actor.Name}";

        descriptor = new LiveCompanionDescriptor
        {
            UnitId = unitId,
            CharacterName = actor.Name,
            DisplayName = string.IsNullOrWhiteSpace(actor.displayName) ? actor.Name : actor.displayName,
            OwnerKind = CompanionOwnerKind.PartyMember,
            OwnerCharacterName = npcName,
            ProviderId = PelipperTownCompatibilityService.ProviderId,
            ProviderUnitId = stableId
        };
        return true;
    }

    public static bool TryGetVillagerCompanionActor(string npcName, out NPC? actor)
    {
        actor = null;
        object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));
        if (manager?.GetType().FullName != "PelipperTown.VillagerCompanionManager")
            return false;

        FieldInfo? runtimesField = manager.GetType().GetField("_runtimes", InstanceFlags);
        if (runtimesField?.GetValue(manager) is not IDictionary runtimes)
            return false;

        // Exact key lookup first, then _npcName comparison for aliases/normalization.
        object? runtime = null;
        foreach (DictionaryEntry entry in runtimes)
        {
            if (entry.Key is string key && key.Equals(npcName, StringComparison.OrdinalIgnoreCase))
            {
                runtime = entry.Value;
                break;
            }

            object? candidate = entry.Value;
            if (candidate is null)
                continue;
            FieldInfo? npcNameField = candidate.GetType().GetField("_npcName", InstanceFlags);
            if (npcNameField?.GetValue(candidate) is string runtimeNpcName
                && runtimeNpcName.Equals(npcName, StringComparison.OrdinalIgnoreCase))
            {
                runtime = candidate;
                break;
            }
        }

        if (runtime is null)
            return true;
        if (runtime.GetType().FullName != "PelipperTown.VillagerCompanionRuntime")
            return false;

        FieldInfo? entityField = runtime.GetType().GetField("_entity", InstanceFlags);
        actor = entityField?.GetValue(runtime) as NPC;
        return true;
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
            apply.Invoke(manager, null);

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

        FieldInfo? exact = rootType.GetField("_villagerCompanions", InstanceFlags);
        object? value = SafeGet(() => exact?.GetValue(root));
        if (value?.GetType().FullName == "PelipperTown.VillagerCompanionManager")
            return value;

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

    private static string? TryGetStablePelipperUnitId(NPC actor)
    {
        foreach (var pair in actor.modData.Pairs)
        {
            if (!(pair.Key.Contains("pokemon", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("companion", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("partner", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if ((pair.Key.Contains("id", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("guid", StringComparison.OrdinalIgnoreCase))
                && !string.IsNullOrWhiteSpace(pair.Value))
            {
                return pair.Value;
            }
        }

        foreach (string hint in new[]
        {
            "PokemonId", "PokemonID", "CompanionId", "CompanionID", "PartnerId", "PartnerID", "UniqueId", "UniqueID"
        })
        {
            try
            {
                PropertyInfo? property = actor.GetType().GetProperty(hint, InstanceFlags | BindingFlags.IgnoreCase);
                object? value = property?.GetIndexParameters().Length == 0 ? property.GetValue(actor) : null;
                if (value is not null && !string.IsNullOrWhiteSpace(value.ToString()))
                    return value.ToString();

                FieldInfo? field = actor.GetType().GetField(hint, InstanceFlags | BindingFlags.IgnoreCase);
                value = field?.GetValue(actor);
                if (value is not null && !string.IsNullOrWhiteSpace(value.ToString()))
                    return value.ToString();
            }
            catch
            {
            }
        }

        return null;
    }

    private static T? SafeGet<T>(Func<T?> getter)
    {
        try { return getter(); }
        catch { return default; }
    }
}
