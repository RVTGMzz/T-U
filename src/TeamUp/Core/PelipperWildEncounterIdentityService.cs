using System.Reflection;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Alpha 6.7.44.4 source-aware identity for Pelipper wild encounters.
/// Pelipper may render a visible Pokémon NPC while combat is executed through a separate Monster
/// proxy (often a vanilla monster type/name). Team Up must read identity/Shiny state from the source
/// actor while applying damage/targeting state to the combat proxy.
/// </summary>
internal sealed record PelipperWildEncounterIdentity(
    Monster CombatProxy,
    NPC SourceActor,
    string DisplayName,
    string EncounterId);

internal static class PelipperWildEncounterIdentityService
{
    public const string SourceEncounterIdMarker = "Ronvotri.TeamUp/PelipperSourceEncounterId";
    public const string ProxyEncounterIdMarker = "Ronvotri.TeamUp/PelipperEncounterId";
    public const string ProxyDisplayNameMarker = "Ronvotri.TeamUp/PelipperDisplayName";

    private static readonly string[] StableIdHints =
    {
        "EncounterId", "EncounterID", "SpawnId", "SpawnID", "InstanceId", "InstanceID",
        "UniqueId", "UniqueID", "Guid", "GUID"
    };

    private static readonly string[] DisplayNameHints =
    {
        "PokemonName", "PokémonName", "SpeciesName", "SpeciesDisplayName", "EncounterName"
    };

    public static bool TryResolve(Monster proxy, out PelipperWildEncounterIdentity identity)
    {
        identity = null!;
        if (!PelipperTownCompatibilityService.IsWildCombatActor(proxy))
            return false;

        GameLocation? location = proxy.currentLocation ?? Game1.currentLocation;
        if (location is null)
            return TryResolveFromProxyMarkers(proxy, out identity);

        Rectangle proxyBounds = proxy.GetBoundingBox();
        NPC? source = location.characters
            .OfType<NPC>()
            .Where(candidate => !ReferenceEquals(candidate, proxy))
            .Where(PelipperTownCompatibilityService.LooksLikePelipperActor)
            .Where(PelipperTownCompatibilityService.IsWildCombatActor)
            .Select(candidate => new
            {
                Actor = candidate,
                Distance = Vector2.DistanceSquared(candidate.Position, proxy.Position),
                Intersects = proxyBounds.Intersects(candidate.GetBoundingBox()),
                ProxyPenalty = candidate is Monster && HasTrueModData(candidate, PelipperTownCompatibilityService.WildCombatProxyKey)
                    ? 100000000f
                    : 0f
            })
            .Where(item => item.Intersects || item.Distance <= 128f * 128f)
            .OrderBy(item => item.ProxyPenalty + (item.Intersects ? -1000000f : 0f) + item.Distance)
            .Select(item => item.Actor)
            .FirstOrDefault();

        if (source is null)
            return TryResolveFromProxyMarkers(proxy, out identity);

        string displayName = ReadDisplayName(source);
        string encounterId = GetOrCreateEncounterId(source, location, displayName);
        proxy.modData[ProxyEncounterIdMarker] = encounterId;
        proxy.modData[ProxyDisplayNameMarker] = displayName;

        identity = new PelipperWildEncounterIdentity(proxy, source, displayName, encounterId);
        return true;
    }

    public static string GetEncounterId(Monster proxy)
    {
        if (proxy.modData.TryGetValue(ProxyEncounterIdMarker, out string? stored) && !string.IsNullOrWhiteSpace(stored))
            return stored;
        return TryResolve(proxy, out PelipperWildEncounterIdentity? identity)
            ? identity.EncounterId
            : string.Empty;
    }

    public static string GetDisplayName(Monster proxy)
    {
        if (proxy.modData.TryGetValue(ProxyDisplayNameMarker, out string? stored) && !string.IsNullOrWhiteSpace(stored))
            return stored;
        if (TryResolve(proxy, out PelipperWildEncounterIdentity? identity))
            return identity.DisplayName;
        return CleanDisplayName(string.IsNullOrWhiteSpace(proxy.displayName) ? proxy.Name : proxy.displayName);
    }

    private static bool TryResolveFromProxyMarkers(Monster proxy, out PelipperWildEncounterIdentity identity)
    {
        identity = null!;
        if (!proxy.modData.TryGetValue(ProxyEncounterIdMarker, out string? encounterId)
            || string.IsNullOrWhiteSpace(encounterId))
        {
            return false;
        }

        string displayName = proxy.modData.TryGetValue(ProxyDisplayNameMarker, out string? storedName)
            && !string.IsNullOrWhiteSpace(storedName)
                ? storedName
                : CleanDisplayName(string.IsNullOrWhiteSpace(proxy.displayName) ? proxy.Name : proxy.displayName);
        identity = new PelipperWildEncounterIdentity(proxy, proxy, displayName, encounterId);
        return true;
    }

    private static string GetOrCreateEncounterId(NPC source, GameLocation location, string displayName)
    {
        if (source.modData.TryGetValue(SourceEncounterIdMarker, out string? stored) && !string.IsNullOrWhiteSpace(stored))
            return stored;

        string? stable = ReadStableSourceId(source);
        string encounterId = !string.IsNullOrWhiteSpace(stable)
            ? $"pelipper:{location.NameOrUniqueName}:{stable}"
            : $"teamup:{Game1.uniqueIDForThisGame}:{location.NameOrUniqueName}:{Normalize(displayName)}:{Game1.ticks}:{Guid.NewGuid():N}";

        source.modData[SourceEncounterIdMarker] = encounterId;
        return encounterId;
    }

    private static string? ReadStableSourceId(NPC source)
    {
        foreach (var pair in source.modData.Pairs)
        {
            string key = Normalize(pair.Key);
            bool stableKey = key.Contains("uniqueid") || key.Contains("guid")
                || key.Contains("encounterid") || key.Contains("spawnid") || key.Contains("instanceid");
            if (stableKey && !string.IsNullOrWhiteSpace(pair.Value))
                return $"moddata:{key}:{pair.Value}";
        }

        foreach (string hint in StableIdHints)
        {
            if (TryReadMember(source, hint, out object? value)
                && value is not null
                && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return $"member:{Normalize(hint)}:{value}";
            }
        }

        return null;
    }

    private static string ReadDisplayName(NPC source)
    {
        foreach (string hint in DisplayNameHints)
        {
            if (TryReadMember(source, hint, out object? value)
                && value is string text
                && !string.IsNullOrWhiteSpace(text))
            {
                return CleanDisplayName(text);
            }
        }

        string raw = string.IsNullOrWhiteSpace(source.displayName) ? source.Name : source.displayName;
        return CleanDisplayName(raw);
    }

    private static string CleanDisplayName(string? raw)
    {
        string value = string.IsNullOrWhiteSpace(raw) ? "Pokémon" : raw.Trim();
        bool changed;
        do
        {
            changed = false;
            foreach (string prefix in new[] { "Wild ", "Shiny " })
            {
                if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                value = value[prefix.Length..].Trim();
                changed = true;
            }
        } while (changed && value.Length > 0);

        return string.IsNullOrWhiteSpace(value) ? "Pokémon" : value;
    }

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
            // Optional compatibility: identity probing must fail safely.
        }
        return false;
    }

    private static bool HasTrueModData(NPC actor, string key)
        => actor.modData.TryGetValue(key, out string? raw)
            && raw.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
