using System.Reflection;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Optional, reflection-only compatibility with Pelipper Town villager/player Pokémon.
/// Team Up never references Pelipper Town's DLL or private save model. Detection is based on
/// live actor identity/modData plus owner metadata when available, with a conservative
/// same-location proximity fallback for decorative villager partners.
/// </summary>
public static class PelipperTownCompatibilityService
{
    public const string ProviderId = "Griff.PelipperTown";
    public const string CompanionOptOutKey = "Ronvotri.TeamUp/PelipperCompanionOptOut";
    public const string SuppressedKey = "Ronvotri.TeamUp/PelipperSuppressed";
    public const string SuppressedOwnerKey = "Ronvotri.TeamUp/PelipperSuppressedOwner";
    public const string CombatTargetOptInKey = "Ronvotri.TeamUp/CombatTarget";
    public const string WildCombatProxyKey = "Ronvotri.TeamUp/PelipperWildCombatProxy";
    private const string OriginalInvisibleKey = "Ronvotri.TeamUp/PelipperOriginalInvisible";

    private static readonly string[] OwnerStringMemberHints =
    {
        "OwnerName", "OwnerNpcName", "PartnerOwner", "PartnerName", "VillagerName",
        "NpcName", "AssignedNpc", "AssignedVillager", "PartnerVillager"
    };

    private static readonly string[] OwnerIdMemberHints =
    {
        "OwnerId", "OwnerID", "OwnerFarmerId", "OwnerFarmerID", "FarmerId", "FarmerID",
        "TrainerId", "TrainerID"
    };

    public static LiveCompanionDescriptor? FindVillagerPartner(NPC owner)
    {
        if (owner.currentLocation is null)
            return null;

        NPC? best = null;
        float bestScore = float.MaxValue;

        foreach (NPC candidate in owner.currentLocation.characters.OfType<NPC>())
        {
            bool teamUpSuppressed = candidate.modData.TryGetValue(SuppressedKey, out string? rawSuppressed)
                && rawSuppressed.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (ReferenceEquals(candidate, owner)
                || !LooksLikePelipperActor(candidate)
                || LooksWild(candidate))
            {
                continue;
            }

            bool explicitOwner = HasOwnerName(candidate, owner.Name)
                || (candidate.modData.TryGetValue(SuppressedOwnerKey, out string? suppressedOwner)
                    && suppressedOwner.Equals(owner.Name, StringComparison.OrdinalIgnoreCase));

            // Source-hidden partners are safe to discover only with explicit ownership metadata.
            // This restores a recall path for Standby Pokemon without proximity-matching random
            // invisible Pelipper actors.
            if (candidate.IsInvisible && !teamUpSuppressed && !explicitOwner)
                continue;

            float distance = Vector2Distance(candidate.Tile, owner.Tile);
            if (!explicitOwner && distance > 3.25f)
                continue;

            float score = explicitOwner ? distance - 100f : distance;
            if (score >= bestScore)
                continue;

            best = candidate;
            bestScore = score;
        }

        return best is null ? null : DescribeVillagerPartner(best, owner.Name);
    }

    public static IEnumerable<LiveCompanionDescriptor> FindPlayerCompanions(IEnumerable<long> onlineFarmerIds)
    {
        HashSet<long> online = onlineFarmerIds.ToHashSet();
        if (online.Count == 0)
            yield break;

        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC candidate in location.characters.OfType<NPC>())
            {
                if ((candidate.IsInvisible && !candidate.modData.ContainsKey(SuppressedKey))
                    || !LooksLikePelipperActor(candidate)
                    || LooksWild(candidate))
                {
                    continue;
                }

                if (!TryReadOwnerFarmerId(candidate, out long ownerId) || !online.Contains(ownerId))
                    continue;

                yield return DescribePlayerCompanion(candidate, ownerId);
            }
        }
    }

    public static bool IsPelipperDescriptor(LiveCompanionDescriptor? descriptor)
        => descriptor is not null
            && descriptor.ProviderId.Equals(ProviderId, StringComparison.OrdinalIgnoreCase);

    public static bool IsSourceControlled(CompanionUnitData unit)
        => unit.ProviderId.Equals(ProviderId, StringComparison.OrdinalIgnoreCase);

    public static bool IsOwnerOptedOut(NPC owner)
        => owner.modData.TryGetValue(CompanionOptOutKey, out string? raw)
            && raw.Equals("true", StringComparison.OrdinalIgnoreCase);

    public static void SetOwnerOptOut(NPC owner, bool optedOut)
    {
        if (optedOut)
            owner.modData[CompanionOptOutKey] = "true";
        else
            owner.modData.Remove(CompanionOptOutKey);
    }

    public static NPC? ResolveActor(LiveCompanionDescriptor descriptor)
    {
        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC actor in location.characters.OfType<NPC>())
            {
                if (!LooksLikePelipperActor(actor))
                    continue;

                string ownerIdentity = descriptor.OwnerKind == CompanionOwnerKind.PartyMember
                    ? $"npc:{descriptor.OwnerCharacterName ?? "npc"}"
                    : $"farmer:{descriptor.OwnerFarmerId?.ToString() ?? "farmer"}";
                if (BuildUnitId(actor, ownerIdentity).Equals(descriptor.UnitId, StringComparison.OrdinalIgnoreCase))
                    return actor;
            }
        }

        return null;
    }

    public static NPC? ResolveActor(CompanionUnitData unit)
    {
        if (!IsSourceControlled(unit))
            return null;

        var descriptor = new LiveCompanionDescriptor
        {
            UnitId = unit.UnitId,
            CharacterName = unit.CharacterName,
            DisplayName = unit.DisplayName,
            OwnerKind = unit.OwnerKind,
            OwnerCharacterName = unit.OwnerCharacterName,
            OwnerFarmerId = unit.OwnerKind == CompanionOwnerKind.Player ? unit.RecruiterId : null,
            ProviderId = unit.ProviderId,
            ProviderUnitId = unit.ProviderUnitId
        };
        return ResolveActor(descriptor);
    }

    public static void SetSuppressed(NPC actor, string ownerName, bool suppressed)
    {
        if (suppressed)
        {
            if (!actor.modData.ContainsKey(OriginalInvisibleKey))
                actor.modData[OriginalInvisibleKey] = actor.IsInvisible ? "true" : "false";

            actor.modData[SuppressedKey] = "true";
            actor.modData[SuppressedOwnerKey] = ownerName;
            TrySetInvisible(actor, true);
            actor.Halt();
            actor.controller = null;
            actor.temporaryController = null;
            return;
        }

        if (!actor.modData.ContainsKey(SuppressedKey))
            return;

        bool originalInvisible = actor.modData.TryGetValue(OriginalInvisibleKey, out string? raw)
            && bool.TryParse(raw, out bool parsed)
            && parsed;
        TrySetInvisible(actor, originalInvisible);
        actor.modData.Remove(SuppressedKey);
        actor.modData.Remove(SuppressedOwnerKey);
        actor.modData.Remove(OriginalInvisibleKey);
    }

    public static void CleanupOrphanedSuppression(IReadOnlyCollection<string> activeTeamUpOwnerNames)
    {
        HashSet<string> active = activeTeamUpOwnerNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC actor in location.characters.OfType<NPC>())
            {
                if (!actor.modData.TryGetValue(SuppressedKey, out string? raw)
                    || !raw.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string owner = actor.modData.TryGetValue(SuppressedOwnerKey, out string? storedOwner)
                    ? storedOwner ?? string.Empty
                    : string.Empty;
                if (!string.IsNullOrWhiteSpace(owner) && active.Contains(owner))
                    continue;

                SetSuppressed(actor, owner, false);
            }
        }
    }

    public static bool ShouldExcludeFromTeamUpCombat(NPC actor)
    {
        if (!LooksLikePelipperActor(actor))
            return false;

        return !actor.modData.TryGetValue(CombatTargetOptInKey, out string? raw)
            || !raw.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    // Pelipper Town builds each wild encounter as a visible Wild NPC plus a separate Monster
    // combat proxy. The proxy can lack Wild in its name/type/modData even though Team Up has already
    // classified it as the attackable unowned Pelipper target. Capture identity therefore accepts
    // both the source-facing Wild signals and Team Up's explicit proxy/target markers.
    public static bool IsWildCombatActor(NPC actor)
    {
        if (!LooksLikePelipperActor(actor))
            return false;
        if (LooksWild(actor))
            return true;
        if (actor is not Monster)
            return false;

        return HasTrueModData(actor, WildCombatProxyKey)
            || HasTrueModData(actor, CombatTargetOptInKey);
    }

    private static bool HasTrueModData(NPC actor, string key)
        => actor.modData.TryGetValue(key, out string? raw)
            && raw.Equals("true", StringComparison.OrdinalIgnoreCase);

    public static bool LooksLikePelipperActor(NPC actor)
    {
        string typeName = actor.GetType().FullName ?? string.Empty;
        string assemblyName = actor.GetType().Assembly.GetName().Name ?? string.Empty;
        if (ContainsPelipperToken(typeName) || ContainsPelipperToken(assemblyName))
            return true;

        foreach (var pair in actor.modData.Pairs)
        {
            if (ContainsPelipperToken(pair.Key) || ContainsPelipperToken(pair.Value))
                return true;
        }

        object? sprite = actor.Sprite;
        if (sprite is not null)
        {
            foreach (string memberName in new[] { "textureName", "TextureName", "texturePath", "TexturePath" })
            {
                if (TryReadMember(sprite, memberName, out object? value)
                    && value is string text
                    && ContainsPelipperToken(text))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool LooksWild(NPC actor)
    {
        if (actor.Name.StartsWith("Wild ", StringComparison.OrdinalIgnoreCase)
            || actor.displayName.StartsWith("Wild ", StringComparison.OrdinalIgnoreCase)
            || (actor.GetType().FullName?.Contains("Wild", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return true;
        }

        foreach (var pair in actor.modData.Pairs)
        {
            bool keyLooksWild = pair.Key.Contains("wild", StringComparison.OrdinalIgnoreCase);
            bool valueLooksWild = pair.Value?.Contains("wild", StringComparison.OrdinalIgnoreCase) == true;
            if (keyLooksWild && (valueLooksWild || pair.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true))
                return true;
        }

        return false;
    }

    private static bool HasOwnerName(NPC actor, string ownerName)
    {
        foreach (var pair in actor.modData.Pairs)
        {
            if (!(pair.Key.Contains("owner", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("partner", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("villager", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (pair.Value?.Equals(ownerName, StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }

        foreach (string hint in OwnerStringMemberHints)
        {
            if (!TryReadMember(actor, hint, out object? value) || value is null)
                continue;
            if (value is string text && text.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                return true;
            if (value is NPC npc && npc.Name.Equals(ownerName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool TryReadOwnerFarmerId(NPC actor, out long ownerId)
    {
        ownerId = 0;
        foreach (var pair in actor.modData.Pairs)
        {
            if (!(pair.Key.Contains("owner", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("farmer", StringComparison.OrdinalIgnoreCase)
                || pair.Key.Contains("trainer", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (long.TryParse(pair.Value, out ownerId))
                return true;
        }

        foreach (string hint in OwnerIdMemberHints)
        {
            if (!TryReadMember(actor, hint, out object? value) || value is null)
                continue;

            if (value is long direct)
            {
                ownerId = direct;
                return true;
            }
            if (value is int integer)
            {
                ownerId = integer;
                return true;
            }
            if (long.TryParse(value.ToString(), out ownerId))
                return true;
        }

        return false;
    }

    private static LiveCompanionDescriptor DescribeVillagerPartner(NPC actor, string ownerName)
        => new()
        {
            UnitId = BuildUnitId(actor, $"npc:{ownerName}"),
            CharacterName = actor.Name,
            DisplayName = string.IsNullOrWhiteSpace(actor.displayName) ? actor.Name : actor.displayName,
            OwnerKind = CompanionOwnerKind.PartyMember,
            OwnerCharacterName = ownerName,
            ProviderId = ProviderId,
            ProviderUnitId = TryGetStablePelipperUnitId(actor)
        };

    private static LiveCompanionDescriptor DescribePlayerCompanion(NPC actor, long ownerId)
        => new()
        {
            UnitId = BuildUnitId(actor, $"farmer:{ownerId}"),
            CharacterName = actor.Name,
            DisplayName = string.IsNullOrWhiteSpace(actor.displayName) ? actor.Name : actor.displayName,
            OwnerKind = CompanionOwnerKind.Player,
            OwnerFarmerId = ownerId,
            ProviderId = ProviderId,
            ProviderUnitId = TryGetStablePelipperUnitId(actor)
        };

    private static string BuildUnitId(NPC actor, string ownerIdentity)
    {
        string? stable = TryGetStablePelipperUnitId(actor);
        if (!string.IsNullOrWhiteSpace(stable))
            return $"{ProviderId}:{stable}";
        return $"{ProviderId}:{ownerIdentity}:{actor.Name}";
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

        foreach (string hint in new[] { "PokemonId", "PokemonID", "CompanionId", "CompanionID", "PartnerId", "PartnerID", "UniqueId", "UniqueID" })
        {
            if (TryReadMember(actor, hint, out object? value) && value is not null && !string.IsNullOrWhiteSpace(value.ToString()))
                return value.ToString();
        }

        return null;
    }

    private static bool ContainsPelipperToken(string? text)
        => !string.IsNullOrWhiteSpace(text)
            && (text.Contains("Griff.PelipperTown", StringComparison.OrdinalIgnoreCase)
                || text.Contains("PelipperTown", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Pelipper Town", StringComparison.OrdinalIgnoreCase));

    private static bool TryReadMember(object target, string memberName, out object? value)
    {
        value = null;
        try
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            Type type = target.GetType();
            PropertyInfo? property = type.GetProperty(memberName, flags);
            if (property is not null && property.GetIndexParameters().Length == 0)
            {
                value = property.GetValue(target);
                return true;
            }

            FieldInfo? field = type.GetField(memberName, flags);
            if (field is not null)
            {
                value = field.GetValue(target);
                return true;
            }
        }
        catch
        {
            // Optional compatibility must fail closed.
        }

        return false;
    }

    private static void TrySetInvisible(NPC actor, bool invisible)
    {
        try
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            PropertyInfo? property = actor.GetType().GetProperty("IsInvisible", flags)
                ?? typeof(NPC).GetProperty("IsInvisible", flags);
            if (property?.CanWrite == true)
            {
                property.SetValue(actor, invisible);
                return;
            }

            FieldInfo? field = actor.GetType().GetField("isInvisible", flags)
                ?? typeof(NPC).GetField("isInvisible", flags);
            if (field is not null)
            {
                if (field.FieldType == typeof(bool))
                    field.SetValue(actor, invisible);
                else if (field.GetValue(actor) is object netBool)
                {
                    PropertyInfo? valueProperty = netBool.GetType().GetProperty("Value", flags);
                    if (valueProperty?.CanWrite == true)
                        valueProperty.SetValue(netBool, invisible);
                }
            }
        }
        catch
        {
            // Visual suppression is best-effort and must never crash another mod.
        }
    }

    private static float Vector2Distance(Microsoft.Xna.Framework.Vector2 a, Microsoft.Xna.Framework.Vector2 b)
        => Microsoft.Xna.Framework.Vector2.Distance(a, b);
}