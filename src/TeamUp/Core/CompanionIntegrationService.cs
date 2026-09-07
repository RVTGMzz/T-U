using StardewValley;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Lightweight runtime contract for creature/summon providers. External mods can expose a live
/// NPC/monster as a Team Up companion by setting modData on that actor.
/// </summary>
public static class CompanionIntegrationService
{
    public const string CompanionKindKey = CompanionClassificationService.CompanionKindModDataKey;
    public const string OwnerCharacterNameKey = "Ronvotri.TeamUp/CompanionOwnerCharacter";
    public const string OwnerFarmerIdKey = "Ronvotri.TeamUp/CompanionOwnerFarmerId";
    public const string ProviderIdKey = "Ronvotri.TeamUp/CompanionProviderId";
    public const string ProviderUnitIdKey = "Ronvotri.TeamUp/CompanionProviderUnitId";

    public static LiveCompanionDescriptor? FindLinkedCompanion(NPC owner)
    {
        foreach (NPC candidate in GetAllCharacters())
        {
            if (ReferenceEquals(candidate, owner) || candidate.IsInvisible)
                continue;

            if (!candidate.modData.TryGetValue(CompanionKindKey, out string? kind)
                || !kind.Equals(CompanionClassificationService.LinkedCompanionKind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!candidate.modData.TryGetValue(OwnerCharacterNameKey, out string? ownerName)
                || !ownerName.Equals(owner.Name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return Describe(candidate, CompanionOwnerKind.PartyMember, owner.Name, null);
        }

        // Alpha 6.6.26: Pelipper Town 1.1.9 exposes an exact VillagerName -> runtime -> entity map.
        // When that map is available its answer is authoritative, including a null result after
        // native Despawn. This prevents a different nearby Pokemon from being proximity-matched to
        // the NPC that was just recalled.
        if (PelipperTown119NativeBridge.TryGetVillagerCompanionDescriptor(owner.Name, out LiveCompanionDescriptor? nativePelipper))
            return nativePelipper;

        // Older/unsupported Pelipper builds keep the conservative compatibility adapter.
        return PelipperTownCompatibilityService.FindVillagerPartner(owner);
    }

    public static IEnumerable<LiveCompanionDescriptor> FindPlayerSummons()
    {
        HashSet<string> emitted = new(StringComparer.OrdinalIgnoreCase);

        foreach (NPC candidate in GetAllCharacters())
        {
            if (candidate.IsInvisible
                || CompanionClassificationService.IsSpecialName(candidate.Name, null)
                || !candidate.modData.TryGetValue(CompanionKindKey, out string? kind)
                || !kind.Equals(CompanionClassificationService.FarmerSummonKind, StringComparison.OrdinalIgnoreCase)
                || !candidate.modData.TryGetValue(OwnerFarmerIdKey, out string? rawFarmerId)
                || !long.TryParse(rawFarmerId, out long farmerId))
            {
                continue;
            }

            LiveCompanionDescriptor descriptor = Describe(candidate, CompanionOwnerKind.Player, null, farmerId);
            if (emitted.Add(descriptor.UnitId))
                yield return descriptor;
        }

        long[] onlineFarmerIds = Game1.getOnlineFarmers()
            .Select(farmer => farmer.UniqueMultiplayerID)
            .Distinct()
            .ToArray();
        foreach (LiveCompanionDescriptor descriptor in PelipperTownCompatibilityService.FindPlayerCompanions(onlineFarmerIds))
        {
            if (emitted.Add(descriptor.UnitId))
                yield return descriptor;
        }
    }

    public static string BuildUnitId(NPC actor, string providerId, string? providerUnitId, string ownerIdentity)
    {
        if (!string.IsNullOrWhiteSpace(providerUnitId))
            return $"{providerId}:{providerUnitId}";

        return $"{providerId}:{ownerIdentity}:{actor.Name}";
    }

    private static LiveCompanionDescriptor Describe(
        NPC actor,
        CompanionOwnerKind ownerKind,
        string? ownerCharacterName,
        long? ownerFarmerId)
    {
        string providerId = actor.modData.TryGetValue(ProviderIdKey, out string? rawProvider)
            && !string.IsNullOrWhiteSpace(rawProvider)
                ? rawProvider
                : "RuntimeContract";

        string? providerUnitId = actor.modData.TryGetValue(ProviderUnitIdKey, out string? rawUnit)
            && !string.IsNullOrWhiteSpace(rawUnit)
                ? rawUnit
                : null;

        string ownerIdentity = ownerKind == CompanionOwnerKind.PartyMember
            ? ownerCharacterName ?? "npc"
            : ownerFarmerId?.ToString() ?? "farmer";

        return new LiveCompanionDescriptor
        {
            UnitId = BuildUnitId(actor, providerId, providerUnitId, ownerIdentity),
            CharacterName = actor.Name,
            DisplayName = string.IsNullOrWhiteSpace(actor.displayName) ? actor.Name : actor.displayName,
            OwnerKind = ownerKind,
            OwnerCharacterName = ownerCharacterName,
            OwnerFarmerId = ownerFarmerId,
            ProviderId = providerId,
            ProviderUnitId = providerUnitId
        };
    }

    private static IEnumerable<NPC> GetAllCharacters()
    {
        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC npc in location.characters.OfType<NPC>())
                yield return npc;
        }
    }
}

public sealed class LiveCompanionDescriptor
{
    public string UnitId { get; init; } = string.Empty;
    public string CharacterName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public CompanionOwnerKind OwnerKind { get; init; }
    public string? OwnerCharacterName { get; init; }
    public long? OwnerFarmerId { get; init; }
    public string ProviderId { get; init; } = "RuntimeContract";
    public string? ProviderUnitId { get; init; }
}
