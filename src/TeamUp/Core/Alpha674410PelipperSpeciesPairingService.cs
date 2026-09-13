using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Late identity fallback for Pelipper 1.2.0 wild encounters.
/// 6.7.44.11 adds a proxy-instance cache so a successful species pairing is reused instead of
/// rescanning the entire location repeatedly. Ambiguous duplicate-species scenes still fail closed.
/// </summary>
internal sealed class Alpha674410PelipperSpeciesPairingService
{
    private sealed class CachedPair
    {
        public NPC Source { get; init; } = null!;
        public string DisplayName { get; init; } = string.Empty;
        public string EncounterId { get; init; } = string.Empty;
        public string LocationName { get; init; } = string.Empty;
        public string NormalizedSpecies { get; init; } = string.Empty;
    }

    private readonly IMonitor _monitor;
    private readonly Harmony _harmony;
    private readonly ConditionalWeakTable<Monster, CachedPair> _cache = new();

    private long _attempts;
    private long _resolved;
    private long _cacheHits;
    private long _cacheInvalidated;
    private long _ambiguous;
    private long _noMatch;
    private string _last = "reset";

    private static Alpha674410PelipperSpeciesPairingService? Active { get; set; }

    public Alpha674410PelipperSpeciesPairingService(IMonitor monitor, string uniqueId)
    {
        _monitor = monitor;
        _harmony = new Harmony($"{uniqueId}.Alpha674410PelipperSpeciesPairing");
        Active = this;

        var method = AccessTools.Method(typeof(PelipperWildEncounterIdentityService), nameof(PelipperWildEncounterIdentityService.TryResolve));
        if (method is null)
        {
            _monitor.Log("6.7.44.11 species pairing skipped: Pelipper identity resolver not found.", LogLevel.Warn);
            return;
        }

        _harmony.Patch(
            method,
            postfix: new HarmonyMethod(typeof(Alpha674410PelipperSpeciesPairingService), nameof(TryResolvePostfix))
            {
                priority = Priority.Last
            });
    }

    public string Describe()
        => $"Pelipper species pairing: attempts={_attempts} | resolved={_resolved} | cacheHits={_cacheHits} | cacheInvalidated={_cacheInvalidated} | ambiguous={_ambiguous} | noMatch={_noMatch} | last={_last}";

    private static void TryResolvePostfix(Monster proxy, ref PelipperWildEncounterIdentity identity, ref bool __result)
    {
        if (__result || Active is null)
            return;

        if (Active.TryUseCachedPair(proxy, out identity))
        {
            __result = true;
            return;
        }

        Active.TryLatePair(proxy, out identity, out __result);
    }

    private bool TryUseCachedPair(Monster proxy, out PelipperWildEncounterIdentity identity)
    {
        identity = null!;
        if (!_cache.TryGetValue(proxy, out CachedPair? cached))
            return false;

        GameLocation? location = proxy.currentLocation ?? Game1.currentLocation;
        string locationName = location?.NameOrUniqueName ?? string.Empty;
        string proxyRaw = string.IsNullOrWhiteSpace(proxy.displayName) ? proxy.Name : proxy.displayName;
        string proxySpecies = NormalizeSpecies(proxyRaw);
        bool sourceStillPresent = location is not null
            && location.characters.OfType<NPC>().Any(actor => ReferenceEquals(actor, cached.Source));
        bool valid = location is not null
            && cached.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase)
            && sourceStillPresent
            && ReferenceEquals(cached.Source.currentLocation, location)
            && cached.NormalizedSpecies.Equals(proxySpecies, StringComparison.Ordinal)
            && PelipperTownCompatibilityService.LooksLikePelipperActor(cached.Source)
            && PelipperTownCompatibilityService.IsWildCombatActor(cached.Source)
            && !IsKnownCombatProxy(cached.Source);

        if (!valid)
        {
            _cache.Remove(proxy);
            _cacheInvalidated++;
            return false;
        }

        proxy.modData[PelipperWildEncounterIdentityService.ProxyEncounterIdMarker] = cached.EncounterId;
        proxy.modData[PelipperWildEncounterIdentityService.ProxyDisplayNameMarker] = cached.DisplayName;
        identity = new PelipperWildEncounterIdentity(proxy, cached.Source, cached.DisplayName, cached.EncounterId);
        _cacheHits++;
        _last = $"cache-hit species={cached.DisplayName} sourceType={cached.Source.GetType().FullName ?? cached.Source.GetType().Name}";
        return true;
    }

    private void TryLatePair(Monster proxy, out PelipperWildEncounterIdentity identity, out bool resolved)
    {
        identity = null!;
        resolved = false;
        _attempts++;

        if (!PelipperTownCompatibilityService.IsWildCombatActor(proxy))
        {
            _noMatch++;
            _last = $"not-pelipper-wild proxy={proxy.Name}";
            return;
        }

        GameLocation? location = proxy.currentLocation ?? Game1.currentLocation;
        if (location is null)
        {
            _noMatch++;
            _last = $"no-location proxy={proxy.Name}";
            return;
        }

        string proxyRaw = string.IsNullOrWhiteSpace(proxy.displayName) ? proxy.Name : proxy.displayName;
        string proxySpecies = NormalizeSpecies(proxyRaw);
        if (string.IsNullOrWhiteSpace(proxySpecies) || proxySpecies == "greenslime")
        {
            _noMatch++;
            _last = $"proxy-species-unusable name={proxyRaw} raw={proxy.Name}";
            return;
        }

        List<NPC> sameSpecies = location.characters
            .OfType<NPC>()
            .Where(candidate => !ReferenceEquals(candidate, proxy))
            .Where(PelipperTownCompatibilityService.LooksLikePelipperActor)
            .Where(PelipperTownCompatibilityService.IsWildCombatActor)
            .Where(candidate => !IsKnownCombatProxy(candidate))
            .Where(candidate => NormalizeSpecies(string.IsNullOrWhiteSpace(candidate.displayName) ? candidate.Name : candidate.displayName) == proxySpecies)
            .ToList();

        NPC? source = null;
        if (sameSpecies.Count == 1)
            source = sameSpecies[0];
        else if (sameSpecies.Count > 1)
        {
            List<NPC> intersecting = sameSpecies.Where(candidate => proxy.GetBoundingBox().Intersects(candidate.GetBoundingBox())).ToList();
            if (intersecting.Count == 1)
                source = intersecting[0];
        }

        if (source is null)
        {
            if (sameSpecies.Count > 1)
            {
                _ambiguous++;
                _last = $"ambiguous species={CleanSpecies(proxyRaw)} matches={sameSpecies.Count} proxy={proxy.Name}";
            }
            else
            {
                _noMatch++;
                _last = $"no-species-match proxyDisplay={proxyRaw} species={CleanSpecies(proxyRaw)}";
            }
            return;
        }

        string displayName = CleanSpecies(string.IsNullOrWhiteSpace(source.displayName) ? source.Name : source.displayName);
        string encounterId = ResolveEncounterId(source, location, displayName);
        source.modData[PelipperWildEncounterIdentityService.SourceEncounterIdMarker] = encounterId;
        proxy.modData[PelipperWildEncounterIdentityService.ProxyEncounterIdMarker] = encounterId;
        proxy.modData[PelipperWildEncounterIdentityService.ProxyDisplayNameMarker] = displayName;

        identity = new PelipperWildEncounterIdentity(proxy, source, displayName, encounterId);
        resolved = true;
        _resolved++;
        _last = $"paired species={displayName} sourceType={source.GetType().FullName ?? source.GetType().Name} proxy={proxy.Name} encounter={encounterId}";

        _cache.Remove(proxy);
        _cache.Add(proxy, new CachedPair
        {
            Source = source,
            DisplayName = displayName,
            EncounterId = encounterId,
            LocationName = location.NameOrUniqueName,
            NormalizedSpecies = proxySpecies
        });

        // No per-pair log: live 6.7.44.10 proved it can flood SMAPI and amplify hitching.
    }

    private static bool IsKnownCombatProxy(NPC actor)
        => actor is Monster
            && actor.modData.TryGetValue(PelipperTownCompatibilityService.WildCombatProxyKey, out string? raw)
            && raw.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static string ResolveEncounterId(NPC source, GameLocation location, string displayName)
    {
        if (source.modData.TryGetValue(PelipperWildEncounterIdentityService.SourceEncounterIdMarker, out string? existing)
            && !string.IsNullOrWhiteSpace(existing))
            return existing;

        foreach (var pair in source.modData.Pairs)
        {
            string key = NormalizeToken(pair.Key);
            if (((key.Contains("pelipper") && key.Contains("wildencounterid")) || key.EndsWith("wildencounterid", StringComparison.Ordinal))
                && !string.IsNullOrWhiteSpace(pair.Value))
            {
                return $"pelipper:{location.NameOrUniqueName}:wild:{pair.Value.Trim()}";
            }
        }

        return $"teamup:{Game1.uniqueIDForThisGame}:{location.NameOrUniqueName}:{NormalizeSpecies(displayName)}:{Game1.ticks}:{Guid.NewGuid():N}";
    }

    private static string CleanSpecies(string? raw)
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

    private static string NormalizeSpecies(string? text)
        => NormalizeToken(CleanSpecies(text));

    private static string NormalizeToken(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
