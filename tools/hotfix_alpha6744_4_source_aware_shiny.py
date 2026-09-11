from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, content: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if old not in text:
        raise RuntimeError(f"missing patch anchor: {label}")
    if text.count(old) != 1:
        raise RuntimeError(f"patch anchor not unique ({text.count(old)}): {label}")
    return text.replace(old, new, 1)


def replace_section(text: str, start: str, end: str, replacement: str, label: str) -> str:
    a = text.find(start)
    if a < 0:
        raise RuntimeError(f"missing section start: {label}")
    b = text.find(end, a)
    if b < 0:
        raise RuntimeError(f"missing section end: {label}")
    return text[:a] + replacement + text[b:]


helper = r'''using System.Reflection;
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
'''
write("Core/PelipperWildEncounterIdentityService.cs", helper)

project = read("TeamUp.csproj")
project = replace_once(
    project,
    "<Version>0.2.0-alpha.6.7.44.3</Version>",
    "<Version>0.2.0-alpha.6.7.44.4</Version>",
    "project version",
)
write("TeamUp.csproj", project)

reaction = read("Core/EncounterReactionService.cs")
reaction = replace_once(
    reaction,
    "    private readonly Dictionary<string, long> _reactionCooldownUntil = new(StringComparer.OrdinalIgnoreCase);\n",
    "    private readonly Dictionary<string, long> _reactionCooldownUntil = new(StringComparer.OrdinalIgnoreCase);\n"
    "    private readonly Dictionary<string, ShinyTacticalOrder> _shinyOrdersByEncounterId = new(StringComparer.OrdinalIgnoreCase);\n",
    "encounter order dictionary",
)
reaction = replace_once(
    reaction,
    "        _reactionCooldownUntil.Clear();\n        _pendingReplies.Clear();",
    "        _reactionCooldownUntil.Clear();\n        _shinyOrdersByEncounterId.Clear();\n        _pendingReplies.Clear();",
    "reset encounter orders",
)

new_order_method = r'''    public bool TryApplyOrder(Farmer farmer, ShinyTacticalOrder order, string? expectedEncounterId, Vector2? expectedTile, out string message)
    {
        message = "No held Shiny encounter is available.";
        if (!Context.IsWorldReady || !Context.IsMainPlayer || farmer.currentLocation is null)
            return false;

        IEnumerable<Monster> held = farmer.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0 && IsConfirmedShiny(monster));

        if (!string.IsNullOrWhiteSpace(expectedEncounterId))
        {
            held = held.Where(monster => GetShinyEncounterId(monster)
                .Equals(expectedEncounterId, StringComparison.OrdinalIgnoreCase));
        }

        Monster? target = expectedTile.HasValue
            ? held.OrderBy(monster => Vector2.DistanceSquared(monster.Tile, expectedTile.Value)).FirstOrDefault()
            : held.OrderBy(monster => Vector2.DistanceSquared(monster.Position, farmer.Position)).FirstOrDefault();
        if (target is null)
            return false;

        string encounterId = GetShinyEncounterId(target);
        if (!string.IsNullOrWhiteSpace(encounterId))
            _shinyOrdersByEncounterId[encounterId] = order;
        ApplyShinyTacticalState(target, order);

        string displayName = GetShinyDisplayName(target);
        message = order switch
        {
            ShinyTacticalOrder.Engage => $"ENGAGE: Team Up may attack Shiny {displayName}.",
            ShinyTacticalOrder.Ignore => $"IGNORE: Team Up will leave Shiny {displayName} alone.",
            _ => $"HOLD FIRE: Team Up is waiting on Shiny {displayName}."
        };

        _monitor.Log($"[EncounterReaction] {message} encounter={encounterId}", LogLevel.Info);
        return true;
    }

'''
reaction = replace_section(
    reaction,
    "    public bool TryApplyOrder(",
    "    public string Describe(",
    new_order_method,
    "TryApplyOrder",
)

new_describe = r'''    public string Describe(Farmer farmer)
    {
        Monster? held = FindNearestHeldShiny(farmer);
        string heldText = held is null
            ? "none"
            : $"{GetShinyDisplayName(held)} proxy={held.Name} encounter={GetShinyEncounterId(held)} HP={held.Health}/{held.MaxHealth} tile={held.Tile} ignored={HasTrueModData(held, ShinyIgnoredMarker)}";
        return $"Encounter reactions: tracked={_announced.Count} | confirmedShiny={_confirmedShiny.Count} | shinyOrders={_shinyOrdersByEncounterId.Count} | heldShiny={heldText} | {PelipperCaptureSafetyService.DescribePolicy()}";
    }

    public static string GetShinyEncounterId(Monster monster)
        => PelipperWildEncounterIdentityService.GetEncounterId(monster);

    public static string GetShinyDisplayName(Monster monster)
        => PelipperWildEncounterIdentityService.GetDisplayName(monster);

'''
reaction = replace_section(
    reaction,
    "    public string Describe(",
    "    private EncounterReactionKind Classify(",
    new_describe,
    "Describe + shiny identity accessors",
)

new_shiny_detection = r'''    private bool IsConfirmedPelipperShiny(Monster monster)
    {
        if (!HasConfirmedPelipperShinyEvidence(monster))
            return false;

        PelipperWildEncounterIdentityService.TryResolve(monster, out PelipperWildEncounterIdentity? identity);
        if (!IsConfirmedShiny(monster))
        {
            monster.modData[ShinyConfirmedMarker] = "true";
            monster.modData[MonsterMutationService.MutationExcludedMarker] = "true";
            string displayName = identity?.DisplayName ?? GetShinyDisplayName(monster);
            string encounterId = identity?.EncounterId ?? GetShinyEncounterId(monster);
            _monitor.Log(
                $"[EncounterReaction] Confirmed Pelipper Shiny source={displayName} proxy={monster.Name} encounter={encounterId} tile={monster.Tile}.",
                LogLevel.Info);
        }

        _confirmedShiny.Add(monster);
        return true;
    }

    public static bool HasConfirmedPelipperShinyEvidence(Monster monster)
    {
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return false;

        // Source-aware rule: prefer the visible Pelipper wild Pokémon actor for encounter identity
        // and Shiny state. The Monster proxy may legitimately be named/type'd as a vanilla monster.
        if (PelipperWildEncounterIdentityService.TryResolve(monster, out PelipperWildEncounterIdentity? identity)
            && !ReferenceEquals(identity.SourceActor, monster)
            && HasExplicitShinyEvidence(identity.SourceActor))
        {
            return true;
        }

        // Some Pelipper versions may expose the current Shiny state directly on the combat proxy.
        return HasExplicitShinyEvidence(monster);
    }

'''
reaction = replace_section(
    reaction,
    "    private bool IsConfirmedPelipperShiny(",
    "    private static void ClearFalseShinyState(",
    new_shiny_detection,
    "source-aware Shiny detection",
)

new_hold_section = r'''    private static void ClearFalseShinyState(Monster monster)
    {
        monster.modData.Remove(ShinyConfirmedMarker);
        monster.modData.Remove(ShinyEmergencyHoldMarker);
        monster.modData.Remove(ShinyEngagedMarker);
        monster.modData.Remove(ShinyIgnoredMarker);
        monster.modData.Remove(MonsterMutationService.MutationExcludedMarker);
    }

    private void EnsureShinyEmergencyHold(Monster monster)
    {
        string encounterId = GetShinyEncounterId(monster);
        if (!string.IsNullOrWhiteSpace(encounterId)
            && _shinyOrdersByEncounterId.TryGetValue(encounterId, out ShinyTacticalOrder remembered))
        {
            ApplyShinyTacticalState(monster, remembered);
            return;
        }

        ApplyShinyTacticalState(monster, ShinyTacticalOrder.Hold);
    }

    private static void ApplyShinyTacticalState(Monster target, ShinyTacticalOrder order)
    {
        switch (order)
        {
            case ShinyTacticalOrder.Engage:
                target.modData.Remove(ShinyEmergencyHoldMarker);
                target.modData.Remove(ShinyIgnoredMarker);
                target.modData[ShinyEngagedMarker] = "true";
                if (PelipperTownCompatibilityService.IsWildCombatActor(target))
                    target.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
                break;

            case ShinyTacticalOrder.Ignore:
                target.modData.Remove(ShinyEngagedMarker);
                target.modData[ShinyEmergencyHoldMarker] = "true";
                target.modData[ShinyIgnoredMarker] = "true";
                target.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
                break;

            default:
                target.modData.Remove(ShinyEngagedMarker);
                target.modData.Remove(ShinyIgnoredMarker);
                target.modData[ShinyEmergencyHoldMarker] = "true";
                target.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
                break;
        }
    }

'''
reaction = replace_section(
    reaction,
    "    private static void ClearFalseShinyState(",
    "    private void ShowReaction(",
    new_hold_section,
    "sticky Shiny tactical state",
)

reaction = replace_once(
    reaction,
    "        _monitor.Log($\"[EncounterReaction] kind={kind} target={monster.Name} speaker={primary.Member.CharacterName} hold={IsShinyEmergencyHeld(monster)}\", LogLevel.Debug);",
    "        string targetLabel = kind == EncounterReactionKind.Shiny ? GetShinyDisplayName(monster) : monster.Name;\n"
    "        _monitor.Log($\"[EncounterReaction] kind={kind} target={targetLabel} proxy={monster.Name} speaker={primary.Member.CharacterName} hold={IsShinyEmergencyHeld(monster)}\", LogLevel.Debug);",
    "source-aware reaction log",
)

old_cooldown = r'''    private static string BuildReactionCooldownKey(Monster monster, EncounterReactionKind kind)
    {
        string location = monster.currentLocation?.NameOrUniqueName ?? Game1.currentLocation?.NameOrUniqueName ?? "unknown";
        return $"{location}|{Normalize(monster.Name ?? string.Empty)}|{Normalize(monster.GetType().FullName ?? monster.GetType().Name)}|{kind}";
    }
'''
new_cooldown = r'''    private static string BuildReactionCooldownKey(Monster monster, EncounterReactionKind kind)
    {
        string location = monster.currentLocation?.NameOrUniqueName ?? Game1.currentLocation?.NameOrUniqueName ?? "unknown";
        if (kind == EncounterReactionKind.Shiny)
        {
            string encounterId = GetShinyEncounterId(monster);
            if (!string.IsNullOrWhiteSpace(encounterId))
                return $"{location}|{encounterId}|{kind}";
        }
        return $"{location}|{Normalize(monster.Name ?? string.Empty)}|{Normalize(monster.GetType().FullName ?? monster.GetType().Name)}|{kind}";
    }
'''
reaction = replace_once(reaction, old_cooldown, new_cooldown, "source-aware reaction cooldown")
write("Core/EncounterReactionService.cs", reaction)

wiring = read("ModEntry.Alpha67442.cs")
wiring = replace_once(
    wiring,
    '            ? $"✨ SHINY: {target.Name}! Team Up đang ngừng tấn công. Lệnh của bạn?"\n            : $"✨ SHINY: {target.Name}! Team Up is holding fire. Your order?";',
    '            ? $"✨ SHINY: {EncounterReactionService.GetShinyDisplayName(target)}! Team Up đang ngừng tấn công. Lệnh của bạn?"\n            : $"✨ SHINY: {EncounterReactionService.GetShinyDisplayName(target)}! Team Up is holding fire. Your order?";',
    "Shiny prompt display name",
)
wiring = replace_once(
    wiring,
    "            MonsterName = target.Name,\n            TileX = target.Tile.X,",
    "            MonsterName = target.Name,\n            EncounterId = EncounterReactionService.GetShinyEncounterId(target),\n            DisplayName = EncounterReactionService.GetShinyDisplayName(target),\n            TileX = target.Tile.X,",
    "Shiny request source identity",
)
wiring = replace_once(
    wiring,
    "                request.MonsterName,\n                new Vector2(request.TileX, request.TileY),",
    "                request.EncounterId,\n                new Vector2(request.TileX, request.TileY),",
    "apply order by encounter id",
)
wiring = replace_once(
    wiring,
    '    private static string BuildShinyPromptTokenAlpha67442(GameLocation location, Monster target)\n        => $"{location.NameOrUniqueName}|{target.Name}|{target.GetType().FullName}";',
    '    private static string BuildShinyPromptTokenAlpha67442(GameLocation location, Monster target)\n    {\n        string encounterId = EncounterReactionService.GetShinyEncounterId(target);\n        return !string.IsNullOrWhiteSpace(encounterId)\n            ? $"{location.NameOrUniqueName}|{encounterId}"\n            : $"{location.NameOrUniqueName}|{target.Name}|{target.GetType().FullName}";\n    }',
    "stable Shiny prompt token",
)
wiring = replace_once(
    wiring,
    "        public string MonsterName { get; set; } = string.Empty;\n        public float TileX { get; set; }",
    "        public string MonsterName { get; set; } = string.Empty;\n        public string EncounterId { get; set; } = string.Empty;\n        public string DisplayName { get; set; } = string.Empty;\n        public float TileX { get; set; }",
    "Shiny request identity fields",
)
wiring = wiring.replace("Team Up 6.7.44.2 Encounter Reactions enabled:", "Team Up 6.7.44.4 Encounter Reactions enabled:")
write("ModEntry.Alpha67442.cs", wiring)

print("6.7.44.4 source-aware Shiny hotfix materialized successfully.")
