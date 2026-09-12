using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

internal sealed record PelipperWildEncounterIdentity(
    Monster CombatProxy,
    NPC SourceActor,
    string DisplayName,
    string EncounterId);

/// <summary>
/// Source-aware identity for Pelipper wild encounters.
/// 6.7.44.6 prefers Pelipper's own WildEncounterId shared by source/proxy, then uses a conservative
/// spatial fallback only when no stable ID exists. This prevents dense wild populations from
/// transferring Shiny/HOLD state between neighboring Pokemon.
/// </summary>
internal static class PelipperWildEncounterIdentityService
{
    public const string SourceEncounterIdMarker = "Ronvotri.TeamUp/PelipperSourceEncounterId";
    public const string ProxyEncounterIdMarker = "Ronvotri.TeamUp/PelipperEncounterId";
    public const string ProxyDisplayNameMarker = "Ronvotri.TeamUp/PelipperDisplayName";

    private const long PositiveCacheTicks = 240;
    private const long NegativeCacheTicks = 15;

    private sealed class CacheEntry
    {
        public PelipperWildEncounterIdentity? Identity { get; init; }
        public long ValidUntilTick { get; init; }
        public string LocationName { get; init; } = string.Empty;
    }

    private sealed record SourceCandidate(
        NPC Actor,
        float Distance,
        bool Intersects,
        bool IsProxy,
        string? WildEncounterId);

    private static readonly ConditionalWeakTable<Monster, CacheEntry> Cache = new();

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
        string locationName = location?.NameOrUniqueName ?? string.Empty;
        long now = Game1.ticks;
        string? proxyWildEncounterId = TryReadPelipperWildEncounterId(proxy);

        if (Cache.TryGetValue(proxy, out CacheEntry? cached)
            && cached.ValidUntilTick >= now
            && cached.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase))
        {
            if (cached.Identity is null)
                return false;

            NPC cachedSource = cached.Identity.SourceActor;
            string? cachedSourceWildId = ReferenceEquals(cachedSource, proxy)
                ? proxyWildEncounterId
                : TryReadPelipperWildEncounterId(cachedSource);
            bool sourceStillHere = ReferenceEquals(cachedSource, proxy)
                || location is null
                || ReferenceEquals(cachedSource.currentLocation, location);
            bool stableIdsAgree = string.IsNullOrWhiteSpace(proxyWildEncounterId)
                || string.IsNullOrWhiteSpace(cachedSourceWildId)
                || proxyWildEncounterId.Equals(cachedSourceWildId, StringComparison.OrdinalIgnoreCase);
            bool shouldUpgradeFallbackId = !string.IsNullOrWhiteSpace(proxyWildEncounterId)
                && !string.IsNullOrWhiteSpace(cachedSourceWildId)
                && cached.Identity.EncounterId.StartsWith("teamup:", StringComparison.OrdinalIgnoreCase);

            if (sourceStillHere && stableIdsAgree && !shouldUpgradeFallbackId)
            {
                identity = cached.Identity;
                return true;
            }
        }

        Cache.Remove(proxy);

        if (location is null)
            return CacheMarkerFallback(proxy, locationName, now, out identity);

        Rectangle proxyBounds = proxy.GetBoundingBox();
        List<SourceCandidate> candidates = location.characters
            .OfType<NPC>()
            .Where(candidate => !ReferenceEquals(candidate, proxy))
            .Where(PelipperTownCompatibilityService.LooksLikePelipperActor)
            .Where(PelipperTownCompatibilityService.IsWildCombatActor)
            .Select(candidate => new SourceCandidate(
                candidate,
                Vector2.DistanceSquared(candidate.Position, proxy.Position),
                proxyBounds.Intersects(candidate.GetBoundingBox()),
                candidate is Monster && HasTrueModData(candidate, PelipperTownCompatibilityService.WildCombatProxyKey),
                TryReadPelipperWildEncounterId(candidate)))
            .Where(item => item.Intersects || item.Distance <= 128f * 128f)
            .ToList();

        NPC? source = null;

        // Pelipper 1.2.0 exposes a stable WildEncounterId on runtime actors. When the proxy has one,
        // matching that ID is mandatory. Never fall back to a merely nearby Pokemon with another ID.
        if (!string.IsNullOrWhiteSpace(proxyWildEncounterId))
        {
            source = candidates
                .Where(item => !item.IsProxy)
                .Where(item => !string.IsNullOrWhiteSpace(item.WildEncounterId)
                    && item.WildEncounterId!.Equals(proxyWildEncounterId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Intersects ? 0 : 1)
                .ThenBy(item => item.Distance)
                .Select(item => item.Actor)
                .FirstOrDefault();

            if (source is null)
            {
                CacheNegative(proxy, locationName, now);
                return false;
            }
        }
        else
        {
            // Legacy/fallback path: exact-overlap is acceptable. Pure proximity is accepted only
            // when exactly one non-proxy wild actor is nearby. Ambiguous scenes fail closed.
            List<SourceCandidate> visible = candidates.Where(item => !item.IsProxy).ToList();
            List<SourceCandidate> intersecting = visible.Where(item => item.Intersects).OrderBy(item => item.Distance).ToList();
            if (intersecting.Count == 1)
                source = intersecting[0].Actor;
            else if (intersecting.Count > 1)
            {
                CacheNegative(proxy, locationName, now);
                return false;
            }
            else if (visible.Count == 1)
                source = visible[0].Actor;
            else
            {
                CacheNegative(proxy, locationName, now);
                return false;
            }
        }

        string displayName = ReadDisplayName(source);
        string encounterId = GetOrCreateEncounterId(source, location, displayName);
        proxy.modData[ProxyEncounterIdMarker] = encounterId;
        proxy.modData[ProxyDisplayNameMarker] = displayName;

        identity = new PelipperWildEncounterIdentity(proxy, source, displayName, encounterId);
        Cache.Add(proxy, new CacheEntry
        {
            Identity = identity,
            ValidUntilTick = now + PositiveCacheTicks,
            LocationName = locationName
        });
        return true;
    }

    public static string GetEncounterId(Monster proxy)
    {
        if (TryResolve(proxy, out PelipperWildEncounterIdentity? identity))
            return identity.EncounterId;
        return proxy.modData.TryGetValue(ProxyEncounterIdMarker, out string? stored) && !string.IsNullOrWhiteSpace(stored)
            ? stored
            : string.Empty;
    }

    public static string GetDisplayName(Monster proxy)
    {
        if (TryResolve(proxy, out PelipperWildEncounterIdentity? identity))
            return identity.DisplayName;
        if (proxy.modData.TryGetValue(ProxyDisplayNameMarker, out string? stored) && !string.IsNullOrWhiteSpace(stored))
            return stored;
        return CleanDisplayName(string.IsNullOrWhiteSpace(proxy.displayName) ? proxy.Name : proxy.displayName);
    }

    private static bool CacheMarkerFallback(Monster proxy, string locationName, long now, out PelipperWildEncounterIdentity identity)
    {
        bool markerResolved = TryResolveFromProxyMarkers(proxy, out identity);
        Cache.Add(proxy, new CacheEntry
        {
            Identity = markerResolved ? identity : null,
            ValidUntilTick = now + (markerResolved ? PositiveCacheTicks : NegativeCacheTicks),
            LocationName = locationName
        });
        return markerResolved;
    }

    private static void CacheNegative(Monster proxy, string locationName, long now)
    {
        Cache.Remove(proxy);
        Cache.Add(proxy, new CacheEntry
        {
            Identity = null,
            ValidUntilTick = now + NegativeCacheTicks,
            LocationName = locationName
        });
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
        // Prefer Pelipper's own ID even if Team Up created a temporary fallback ID during the first
        // frames of spawn construction. This upgrades the encounter identity as soon as Pelipper
        // finishes publishing its runtime metadata.
        string? pelipperWildId = TryReadPelipperWildEncounterId(source);
        if (!string.IsNullOrWhiteSpace(pelipperWildId))
        {
            string canonical = $"pelipper:{location.NameOrUniqueName}:wild:{pelipperWildId}";
            source.modData[SourceEncounterIdMarker] = canonical;
            return canonical;
        }

        if (source.modData.TryGetValue(SourceEncounterIdMarker, out string? stored) && !string.IsNullOrWhiteSpace(stored))
            return stored;

        string? stable = ReadStableSourceId(source);
        string encounterId = !string.IsNullOrWhiteSpace(stable)
            ? $"pelipper:{location.NameOrUniqueName}:{stable}"
            : $"teamup:{Game1.uniqueIDForThisGame}:{location.NameOrUniqueName}:{Normalize(displayName)}:{Game1.ticks}:{Guid.NewGuid():N}";

        source.modData[SourceEncounterIdMarker] = encounterId;
        return encounterId;
    }

    private static string? TryReadPelipperWildEncounterId(NPC actor)
    {
        foreach (var pair in actor.modData.Pairs)
        {
            string key = Normalize(pair.Key);
            if ((key.Contains("pelipper") && key.Contains("wildencounterid"))
                || key.EndsWith("wildencounterid", StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                    return pair.Value.Trim();
            }
        }

        foreach (string hint in new[] { "WildEncounterId", "WildEncounterID" })
        {
            if (TryReadMember(actor, hint, out object? value)
                && value is not null
                && !string.IsNullOrWhiteSpace(value.ToString()))
            {
                return value.ToString()!.Trim();
            }
        }

        return null;
    }

    private static string? ReadStableSourceId(NPC source)
    {
        foreach (var pair in source.modData.Pairs)
        {
            string key = Normalize(pair.Key);
            if (key.Contains("ronvotriteamup"))
                continue;
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
            // Compatibility probing must fail safely.
        }
        return false;
    }

    private static bool HasTrueModData(NPC actor, string key)
        => actor.modData.TryGetValue(key, out string? raw)
            && raw.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
