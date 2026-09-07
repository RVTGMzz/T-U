using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
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
///
/// Alpha 6.6.27 hardens custom-NPC detection. A runtime's _entity can be a wrapper rather than the
/// visible NPC itself, so Team Up unwraps known actor/entity members before declaring the companion
/// absent. If Pelipper says a custom villager companion is enabled but the exact runtime map has not
/// materialized that owner yet, a fallback is allowed only for one unique, nearby, unclaimed,
/// non-wild Pelipper actor. Ambiguous clusters fail closed instead of assigning the wrong Pokemon.
/// </summary>
internal static class PelipperTown119NativeBridge
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float SafeCustomNpcFallbackRadius = 2.75f;

    private static readonly string[] ActorMemberHints =
    {
        "_entity", "Entity", "entity", "_actor", "Actor", "actor", "_npc", "Npc", "NPC",
        "_character", "Character", "character", "_visibleEntity", "VisibleEntity"
    };

    private static readonly string[] PlayerOwnerMemberHints =
    {
        "OwnerId", "OwnerID", "OwnerFarmerId", "OwnerFarmerID", "FarmerId", "FarmerID",
        "TrainerId", "TrainerID"
    };

    private static object? RuntimeRoot;
    private static object? VillagerManager;
    private static readonly Dictionary<string, bool> OriginalVillagerEnabled = new(StringComparer.OrdinalIgnoreCase);

    public static string Status
        => RuntimeRoot is null
            ? "root=<none>"
            : $"root={RuntimeRoot.GetType().FullName}, manager={VillagerManager?.GetType().FullName ?? "<none>"}, npcNative={HasVillagerLifecycle}, playerNative={HasPlayerLifecycle}, exactOwnerMap={HasExactVillagerRuntimeMap}, customNpcSafeFallback=True";

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
            return SafeGet(() => field?.GetValue(manager)) is IDictionary;
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
    /// Returns true when the exact 1.1.9 owner lookup was authoritative. A null descriptor then
    /// means Pelipper says the NPC currently has no live companion. False means the runtime map was
    /// unavailable or incomplete for an enabled custom NPC, so the safe custom fallback may run.
    /// </summary>
    public static bool TryGetVillagerCompanionDescriptor(string npcName, out LiveCompanionDescriptor? descriptor)
    {
        descriptor = null;
        if (!TryGetVillagerCompanionActor(npcName, out NPC? actor))
            return false;
        if (actor is null)
            return true;

        descriptor = BuildVillagerDescriptor(actor, npcName);
        return true;
    }

    public static bool TryGetVillagerCompanionActor(string npcName, out NPC? actor)
    {
        actor = null;
        if (!TryGetRuntimeMap(out IDictionary? runtimes) || runtimes is null)
            return false;

        object? runtime = FindVillagerRuntime(runtimes, npcName);
        if (runtime is null)
        {
            // A disabled NPC with no runtime is a precise negative. An enabled custom NPC can have
            // a visible partner while its runtime entry is temporarily absent, so let the unique
            // unclaimed-actor fallback decide that case instead of returning a false negative.
            if (TryIsVillagerCompanionConfiguredEnabled(npcName, out bool configuredEnabled))
                return !configuredEnabled;
            return true;
        }

        if (!string.Equals(runtime.GetType().FullName, "PelipperTown.VillagerCompanionRuntime", StringComparison.Ordinal))
            return false;

        FieldInfo? entityField = runtime.GetType().GetField("_entity", InstanceFlags);
        if (entityField is null)
            return false;

        object? entity = SafeGet(() => entityField.GetValue(runtime));
        if (entity is null)
            return true;

        if (TryUnwrapNpc(entity, out actor))
            return true;

        // Some Pelipper builds keep the visible actor one level beside the wrapper field. Inspect
        // only actor-like members on this exact owner's runtime; never scan unrelated world actors.
        if (TryUnwrapNpc(runtime, out actor))
            return true;

        // Runtime exists and claims this owner, but Team Up couldn't understand its entity shape.
        // Let the safe unique fallback inspect the world rather than reporting "no Pokemon".
        actor = null;
        return false;
    }

    /// <summary>
    /// Safe fallback for enabled custom villagers whose exact Pelipper runtime actor couldn't be
    /// materialized. It never picks "the nearest" from a crowd. Exactly one eligible unclaimed
    /// actor must exist within a tight radius or the lookup returns null.
    /// </summary>
    public static bool TryGetSafeCustomVillagerCompanionDescriptor(NPC owner, out LiveCompanionDescriptor? descriptor)
    {
        descriptor = null;
        if (!HasExactVillagerRuntimeMap || owner.currentLocation is null)
            return false;

        if (!TryIsVillagerCompanionConfiguredEnabled(owner.Name, out bool configuredEnabled))
            return false;
        if (!configuredEnabled)
            return true;

        HashSet<NPC> claimedActors = GetClaimedVillagerActors();
        float maxDistanceSquared = SafeCustomNpcFallbackRadius * SafeCustomNpcFallbackRadius;
        List<NPC> candidates = new();

        foreach (NPC candidate in owner.currentLocation.characters.OfType<NPC>())
        {
            if (ReferenceEquals(candidate, owner)
                || candidate.IsInvisible
                || !PelipperTownCompatibilityService.LooksLikePelipperActor(candidate)
                || PelipperTownCompatibilityService.IsWildCombatActor(candidate)
                || claimedActors.Contains(candidate)
                || LooksPlayerOwned(candidate))
            {
                continue;
            }

            if (candidate.modData.TryGetValue(PelipperDeploymentStateService.DeploymentOwnerKey, out string? desiredOwner)
                && !string.IsNullOrWhiteSpace(desiredOwner)
                && !desiredOwner.Equals(owner.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Vector2 delta = candidate.Tile - owner.Tile;
            if (delta.LengthSquared() > maxDistanceSquared)
                continue;

            candidates.Add(candidate);
            if (candidates.Count > 1)
                return true;
        }

        if (candidates.Count == 1)
            descriptor = BuildVillagerDescriptor(candidates[0], owner.Name);
        return true;
    }

    /// <summary>
    /// Alpha 6.7.1: asks Pelipper 1.1.9 for the configured villager partner even when its runtime
    /// actor is dormant. Verified DLL signature:
    /// TryGetConfiguredCompanion(string, out SpeciesDefinition, out bool) -> bool.
    /// This descriptor is recruitment intent only and must never be treated as source-live truth.
    /// </summary>
    public static bool TryGetConfiguredVillagerCompanionDescriptor(string npcName, out LiveCompanionDescriptor? descriptor)
    {
        descriptor = null;
        object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));
        if (manager?.GetType().FullName != "PelipperTown.VillagerCompanionManager"
            || string.IsNullOrWhiteSpace(npcName))
        {
            return false;
        }

        if (TryIsVillagerCompanionConfiguredEnabled(npcName, out bool enabled) && !enabled)
            return true;

        MethodInfo? method = manager.GetType().GetMethods(InstanceFlags)
            .FirstOrDefault(candidate =>
            {
                if (!candidate.Name.Equals("TryGetConfiguredCompanion", StringComparison.Ordinal))
                    return false;
                ParameterInfo[] parameters = candidate.GetParameters();
                return candidate.ReturnType == typeof(bool)
                    && parameters.Length == 3
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType.IsByRef
                    && parameters[2].ParameterType == typeof(bool).MakeByRefType();
            });
        if (method is null)
            return false;

        try
        {
            object?[] args = { npcName, null, false };
            if (method.Invoke(manager, args) is not bool found || !found || args[1] is null)
                return true;

            object species = args[1]!;
            Type speciesType = species.GetType();
            string speciesId = SafeGet(() => speciesType.GetProperty("Id", InstanceFlags)?.GetValue(species)?.ToString()) ?? string.Empty;
            string displayName = SafeGet(() => speciesType.GetProperty("DisplayName", InstanceFlags)?.GetValue(species)?.ToString()) ?? speciesId;
            if (string.IsNullOrWhiteSpace(speciesId))
                return false;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = speciesId;

            string providerUnitId = $"configured:npc:{npcName}:{speciesId}";
            descriptor = new LiveCompanionDescriptor
            {
                UnitId = $"{PelipperTownCompatibilityService.ProviderId}:{providerUnitId}",
                CharacterName = speciesId,
                DisplayName = displayName,
                OwnerKind = CompanionOwnerKind.PartyMember,
                OwnerCharacterName = npcName,
                ProviderId = PelipperTownCompatibilityService.ProviderId,
                ProviderUnitId = providerUnitId
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryIsVillagerCompanionConfiguredEnabled(string npcName, out bool enabled)
    {
        enabled = false;
        object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));
        if (manager?.GetType().FullName != "PelipperTown.VillagerCompanionManager"
            || string.IsNullOrWhiteSpace(npcName))
        {
            return false;
        }

        MethodInfo? getEnabled = manager.GetType().GetMethod(
            "GetConfiguredCompanionEnabled",
            InstanceFlags,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);
        if (getEnabled is null)
            return false;

        try
        {
            object? value = getEnabled.Invoke(manager, new object?[] { npcName });
            if (value is not bool result)
                return false;
            enabled = result;
            return true;
        }
        catch
        {
            return false;
        }
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

    private static bool TryGetRuntimeMap(out IDictionary? runtimes)
    {
        runtimes = null;
        object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));
        if (manager?.GetType().FullName != "PelipperTown.VillagerCompanionManager")
            return false;

        FieldInfo? field = manager.GetType().GetField("_runtimes", InstanceFlags);
        runtimes = SafeGet(() => field?.GetValue(manager)) as IDictionary;
        return runtimes is not null;
    }

    private static object? FindVillagerRuntime(IDictionary runtimes, string npcName)
    {
        foreach (DictionaryEntry entry in runtimes)
        {
            if (entry.Key is string key && key.Equals(npcName, StringComparison.OrdinalIgnoreCase))
                return entry.Value;

            object? candidate = entry.Value;
            if (candidate is null)
                continue;

            FieldInfo? npcNameField = candidate.GetType().GetField("_npcName", InstanceFlags);
            if (SafeGet(() => npcNameField?.GetValue(candidate)) is string runtimeNpcName
                && runtimeNpcName.Equals(npcName, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private static HashSet<NPC> GetClaimedVillagerActors()
    {
        var result = new HashSet<NPC>();
        if (!TryGetRuntimeMap(out IDictionary? runtimes) || runtimes is null)
            return result;

        foreach (DictionaryEntry entry in runtimes)
        {
            object? runtime = entry.Value;
            if (runtime is null)
                continue;

            FieldInfo? entityField = runtime.GetType().GetField("_entity", InstanceFlags);
            object? entity = SafeGet(() => entityField?.GetValue(runtime));
            if (entity is not null && TryUnwrapNpc(entity, out NPC? actor) && actor is not null)
                result.Add(actor);
        }

        return result;
    }

    private static bool TryUnwrapNpc(object value, out NPC? actor)
    {
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        return TryUnwrapNpc(value, 0, visited, out actor);
    }

    private static bool TryUnwrapNpc(object? value, int depth, HashSet<object> visited, out NPC? actor)
    {
        actor = value as NPC;
        if (actor is not null)
            return true;
        if (value is null || depth >= 3 || !visited.Add(value))
            return false;

        Type type = value.GetType();
        foreach (string name in ActorMemberHints)
        {
            FieldInfo? field = type.GetField(name, InstanceFlags | BindingFlags.IgnoreCase);
            object? fieldValue = SafeGet(() => field?.GetValue(value));
            if (fieldValue is not null && TryUnwrapNpc(fieldValue, depth + 1, visited, out actor))
                return true;

            PropertyInfo? property = type.GetProperty(name, InstanceFlags | BindingFlags.IgnoreCase);
            if (property is null || property.GetIndexParameters().Length != 0)
                continue;
            object? propertyValue = SafeGet(() => property.GetValue(value));
            if (propertyValue is not null && TryUnwrapNpc(propertyValue, depth + 1, visited, out actor))
                return true;
        }

        foreach (FieldInfo field in type.GetFields(InstanceFlags))
        {
            if (!typeof(NPC).IsAssignableFrom(field.FieldType))
                continue;
            actor = SafeGet(() => field.GetValue(value)) as NPC;
            if (actor is not null)
                return true;
        }

        actor = null;
        return false;
    }

    private static bool LooksPlayerOwned(NPC actor)
    {
        foreach (var pair in actor.modData.Pairs)
        {
            bool ownerish = pair.Key.Contains("owner", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("farmer", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("trainer", StringComparison.OrdinalIgnoreCase);
            if (ownerish && long.TryParse(pair.Value, out long id) && id != 0)
                return true;
        }

        foreach (string memberName in PlayerOwnerMemberHints)
        {
            FieldInfo? field = actor.GetType().GetField(memberName, InstanceFlags | BindingFlags.IgnoreCase);
            object? value = SafeGet(() => field?.GetValue(actor));
            if (TryReadNonZeroId(value))
                return true;

            PropertyInfo? property = actor.GetType().GetProperty(memberName, InstanceFlags | BindingFlags.IgnoreCase);
            if (property is null || property.GetIndexParameters().Length != 0)
                continue;
            value = SafeGet(() => property.GetValue(actor));
            if (TryReadNonZeroId(value))
                return true;
        }

        return false;
    }

    private static bool TryReadNonZeroId(object? value)
    {
        if (value is long direct)
            return direct != 0;
        if (value is int integer)
            return integer != 0;
        return value is not null && long.TryParse(value.ToString(), out long parsed) && parsed != 0;
    }

    private static LiveCompanionDescriptor BuildVillagerDescriptor(NPC actor, string npcName)
    {
        string? stableId = TryGetStablePelipperUnitId(actor);
        string unitId = !string.IsNullOrWhiteSpace(stableId)
            ? $"{PelipperTownCompatibilityService.ProviderId}:{stableId}"
            : $"{PelipperTownCompatibilityService.ProviderId}:npc:{npcName}:{actor.Name}";

        return new LiveCompanionDescriptor
        {
            UnitId = unitId,
            CharacterName = actor.Name,
            DisplayName = string.IsNullOrWhiteSpace(actor.displayName) ? actor.Name : actor.displayName,
            OwnerKind = CompanionOwnerKind.PartyMember,
            OwnerCharacterName = npcName,
            ProviderId = PelipperTownCompatibilityService.ProviderId,
            ProviderUnitId = stableId
        };
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
