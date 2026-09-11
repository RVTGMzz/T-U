from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def patch(rel: str, old: str, new: str) -> None:
    path = SRC / rel
    text = path.read_text(encoding="utf-8")
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{rel}: expected exactly one patch target, found {count}: {old[:90]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


# Version bump. 6.7.45 remains reserved for the story encounter.
patch(
    "TeamUp.csproj",
    "<Version>0.2.0-alpha.6.7.44.2</Version>",
    "<Version>0.2.0-alpha.6.7.44.3</Version>",
)

# Pelipper capture safety: Pelipper itself has capture settings, but no stable universal
# Catch-Mode boolean for Team Up to positively resolve. If Pelipper is loaded and an actor is a
# genuine wild combat proxy, Team Up must mirror Pelipper's low-HP mercy behavior. The threshold
# still prefers a reflected Pelipper value when available and otherwise falls back to 10%.
patch(
    "Core/PelipperCaptureSafetyService.cs",
    "using System.Reflection;\nusing StardewValley;",
    "using System.Reflection;\nusing Ronvotri.TeamUp.Combat;\nusing StardewValley;",
)
patch(
    "Core/PelipperCaptureSafetyService.cs",
    "        if (monster.Health <= 0 || monster.MaxHealth <= 0)\n            return false;\n\n        // Shiny Hold is deliberately independent from Pelipper Catch Mode.",
    "        if (monster.Health <= 0 || monster.MaxHealth <= 0)\n            return false;\n\n        // Mutations are combat-only Team Up threats. They must not inherit Pelipper's mercy floor.\n        if (MonsterMutationService.IsMutant(monster))\n            return false;\n\n        // Shiny Hold is deliberately independent from Pelipper Catch Mode.",
)
patch(
    "Core/PelipperCaptureSafetyService.cs",
    "        if (monster.Health <= 0 || monster.MaxHealth <= 0)\n            return false;\n        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))",
    "        if (monster.Health <= 0 || monster.MaxHealth <= 0)\n            return false;\n        if (MonsterMutationService.IsMutant(monster))\n            return false;\n        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))",
)
patch(
    "Core/PelipperCaptureSafetyService.cs",
    "        _modeConfirmed = pelipperDetected && bestModeScore >= 0;\n        _enabled = _modeConfirmed && enabled;\n        _threshold = Math.Clamp(threshold, 0.01f, 0.95f);",
    "        _modeConfirmed = pelipperDetected && bestModeScore >= 0;\n        // 6.7.44.3: Pelipper presence + genuine wild proxy is the authority. Pelipper does not\n        // expose one stable Catch-Mode toggle across current builds, so requiring one disabled\n        // mercy protection entirely. A reflected mode remains diagnostic only.\n        _enabled = pelipperDetected;\n        _threshold = Math.Clamp(threshold, 0.01f, 0.95f);",
)
patch(
    "Core/PelipperCaptureSafetyService.cs",
    "        return $\"Pelipper capture safety: pelipper={_pelipperDetected} | modeConfirmed={_modeConfirmed} | enabled={_enabled} | threshold={_threshold:P0}\";",
    "        return $\"Pelipper capture safety: pelipper={_pelipperDetected} | priority=pelipper-wild | modeHint={_modeConfirmed} | enabled={_enabled} | threshold={_threshold:P0}\";",
)

# Shiny / encounter reactions.
patch(
    "Core/EncounterReactionService.cs",
    "    private readonly Dictionary<Monster, Dictionary<long, HashSet<EncounterReactionKind>>> _announced = new();\n    private readonly Queue<PendingReply> _pendingReplies = new();",
    "    private readonly Dictionary<Monster, Dictionary<long, HashSet<EncounterReactionKind>>> _announced = new();\n    private readonly Dictionary<string, long> _reactionCooldownUntil = new(StringComparer.OrdinalIgnoreCase);\n    private readonly Queue<PendingReply> _pendingReplies = new();",
)
patch(
    "Core/EncounterReactionService.cs",
    "        _announced.Clear();\n        _pendingReplies.Clear();",
    "        _announced.Clear();\n        _reactionCooldownUntil.Clear();\n        _pendingReplies.Clear();",
)
patch(
    "Core/EncounterReactionService.cs",
    "    private EncounterReactionKind Classify(Monster monster)\n    {\n        if (IsConfirmedShiny(monster) || IsConfirmedPelipperShiny(monster))\n            return EncounterReactionKind.Shiny;\n        if (MonsterMutationService.IsMutant(monster))\n            return EncounterReactionKind.Mutation;\n        if (LooksEliteOrBoss(monster))\n            return EncounterReactionKind.EliteBoss;\n        return LooksSpecial(monster) ? EncounterReactionKind.Special : EncounterReactionKind.None;\n    }",
    "    private EncounterReactionKind Classify(Monster monster)\n    {\n        // Repair 6.7.44.2 false-positive Shiny markers before they can keep a normal monster in\n        // HOLD FIRE after upgrading. The old detector accepted capability fields such as\n        // CanBeShiny/ShinyChance as if they described the current encounter.\n        if (IsConfirmedShiny(monster) && !HasConfirmedPelipperShinyEvidence(monster))\n            ClearFalseShinyState(monster);\n\n        if (IsConfirmedPelipperShiny(monster))\n            return EncounterReactionKind.Shiny;\n        if (MonsterMutationService.IsMutant(monster))\n            return EncounterReactionKind.Mutation;\n        if (LooksEliteOrBoss(monster))\n            return EncounterReactionKind.EliteBoss;\n        return LooksSpecial(monster) ? EncounterReactionKind.Special : EncounterReactionKind.None;\n    }",
)
old_shiny_method = '''    private bool IsConfirmedPelipperShiny(Monster monster)
    {
        if (_confirmedShiny.Contains(monster))
            return true;
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return false;

        bool shiny = HasExplicitShinyEvidence(monster);
        if (!shiny && monster.currentLocation is GameLocation location)
        {
            Rectangle proxyBounds = monster.GetBoundingBox();
            foreach (NPC candidate in location.characters.OfType<NPC>())
            {
                if (ReferenceEquals(candidate, monster)
                    || !PelipperTownCompatibilityService.LooksLikePelipperActor(candidate)
                    || !PelipperTownCompatibilityService.IsWildCombatActor(candidate))
                    continue;

                bool paired = proxyBounds.Intersects(candidate.GetBoundingBox())
                    || Vector2.DistanceSquared(monster.Position, candidate.Position) <= 96f * 96f;
                if (paired && HasExplicitShinyEvidence(candidate))
                {
                    shiny = true;
                    break;
                }
            }
        }

        if (!shiny)
            return false;

        monster.modData[ShinyConfirmedMarker] = "true";
        monster.modData[MonsterMutationService.MutationExcludedMarker] = "true";
        _confirmedShiny.Add(monster);
        _monitor.Log($"[EncounterReaction] Confirmed Pelipper Shiny proxy: {monster.Name} at {monster.Tile}.", LogLevel.Info);
        return true;
    }
'''
new_shiny_method = '''    private bool IsConfirmedPelipperShiny(Monster monster)
    {
        if (!HasConfirmedPelipperShinyEvidence(monster))
            return false;

        if (!IsConfirmedShiny(monster))
        {
            monster.modData[ShinyConfirmedMarker] = "true";
            monster.modData[MonsterMutationService.MutationExcludedMarker] = "true";
            _monitor.Log($"[EncounterReaction] Confirmed Pelipper Shiny proxy: {monster.Name} at {monster.Tile}.", LogLevel.Info);
        }

        _confirmedShiny.Add(monster);
        return true;
    }

    public static bool HasConfirmedPelipperShinyEvidence(Monster monster)
    {
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return false;
        if (HasExplicitShinyEvidence(monster))
            return true;
        if (monster.currentLocation is not GameLocation location)
            return false;

        Rectangle proxyBounds = monster.GetBoundingBox();
        foreach (NPC candidate in location.characters.OfType<NPC>())
        {
            if (ReferenceEquals(candidate, monster)
                || !PelipperTownCompatibilityService.LooksLikePelipperActor(candidate)
                || !PelipperTownCompatibilityService.IsWildCombatActor(candidate))
                continue;

            bool paired = proxyBounds.Intersects(candidate.GetBoundingBox())
                || Vector2.DistanceSquared(monster.Position, candidate.Position) <= 96f * 96f;
            if (paired && HasExplicitShinyEvidence(candidate))
                return true;
        }

        return false;
    }

    private static void ClearFalseShinyState(Monster monster)
    {
        monster.modData.Remove(ShinyConfirmedMarker);
        monster.modData.Remove(ShinyEmergencyHoldMarker);
        monster.modData.Remove(ShinyEngagedMarker);
        monster.modData.Remove(ShinyIgnoredMarker);
        monster.modData.Remove(MonsterMutationService.MutationExcludedMarker);
    }
'''
patch("Core/EncounterReactionService.cs", old_shiny_method, new_shiny_method)
patch(
    "Core/EncounterReactionService.cs",
    "    private void ShowReaction(IReadOnlyList<ActiveMember> active, Monster monster, EncounterReactionKind kind)\n    {\n        List<ActiveMember> ordered = active",
    "    private void ShowReaction(IReadOnlyList<ActiveMember> active, Monster monster, EncounterReactionKind kind)\n    {\n        string reactionKey = BuildReactionCooldownKey(monster, kind);\n        long now = Game1.ticks;\n        if (_reactionCooldownUntil.TryGetValue(reactionKey, out long until) && now < until)\n            return;\n        _reactionCooldownUntil[reactionKey] = now + 3600;\n        if (_reactionCooldownUntil.Count > 256)\n        {\n            foreach (string expired in _reactionCooldownUntil.Where(pair => pair.Value <= now).Select(pair => pair.Key).ToList())\n                _reactionCooldownUntil.Remove(expired);\n        }\n\n        List<ActiveMember> ordered = active",
)
patch(
    "Core/EncounterReactionService.cs",
    "    private static bool LooksEliteOrBoss(Monster monster)\n    {\n        if (monster.MaxHealth >= 300)\n            return true;\n        string identity = Normalize($\"{monster.Name} {monster.GetType().FullName}\");",
    "    private static bool LooksEliteOrBoss(Monster monster)\n    {\n        // Raw HP is not an elite signal. Pelipper and other combat mods legitimately scale normal\n        // proxies above 300 HP, which made ordinary Green Slimes/Pokémon look like bosses.\n        string identity = Normalize($\"{monster.Name} {monster.GetType().FullName}\");",
)
patch(
    "Core/EncounterReactionService.cs",
    "            if (key.Contains(\"shiny\") && IsTruthy(rawValue))",
    "            if (IsAuthoritativeShinyKey(key) && IsTruthy(rawValue))",
)
old_member = '''    private static bool HasShinyMember(Type type, object instance)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!Normalize(field.Name).Contains("shiny"))
                continue;
            if (InterpretShinyValue(TryGet(() => field.GetValue(instance))))
                return true;
        }
        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !Normalize(property.Name).Contains("shiny"))
                continue;
            if (InterpretShinyValue(TryGet(() => property.GetValue(instance))))
                return true;
        }
        return false;
    }
'''
new_member = '''    private static bool HasShinyMember(Type type, object instance)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!IsAuthoritativeShinyKey(field.Name))
                continue;
            if (InterpretShinyValue(TryGet(() => field.GetValue(instance))))
                return true;
        }
        foreach (PropertyInfo property in type.GetProperties(flags))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0 || !IsAuthoritativeShinyKey(property.Name))
                continue;
            if (InterpretShinyValue(TryGet(() => property.GetValue(instance))))
                return true;
        }
        return false;
    }

    private static bool IsAuthoritativeShinyKey(string rawName)
    {
        string name = Normalize(rawName);
        // Never confuse a capability/odds/config field with current encounter state.
        if (name.Contains("chance") || name.Contains("odds") || name.Contains("rate")
            || name.Contains("weight") || name.Contains("roll") || name.Contains("eligible")
            || name.Contains("allowshiny") || name.Contains("canshiny") || name.Contains("enable shiny".Replace(" ", string.Empty)))
            return false;

        return name is "shiny" or "isshiny" or "shinyflag" or "shinyform" or "shinyvariant"
            || name.EndsWith("isshiny", StringComparison.Ordinal)
            || name.EndsWith("shinyflag", StringComparison.Ordinal)
            || (name.EndsWith("shiny", StringComparison.Ordinal)
                && !name.EndsWith("canshiny", StringComparison.Ordinal)
                && !name.EndsWith("allowshiny", StringComparison.Ordinal));
    }
'''
patch("Core/EncounterReactionService.cs", old_member, new_member)
patch(
    "Core/EncounterReactionService.cs",
    "    private static Color ReactionColor(EncounterReactionKind kind)",
    "    private static string BuildReactionCooldownKey(Monster monster, EncounterReactionKind kind)\n    {\n        string location = monster.currentLocation?.NameOrUniqueName ?? Game1.currentLocation?.NameOrUniqueName ?? \"unknown\";\n        return $\"{location}|{Normalize(monster.Name ?? string.Empty)}|{Normalize(monster.GetType().FullName ?? monster.GetType().Name)}|{kind}\";\n    }\n\n    private static Color ReactionColor(EncounterReactionKind kind)",
)

# Once the Farmer has answered the Shiny prompt, do not reopen it every ten seconds. The player can
# still change the order manually with teamup_encounter. Use a stable non-tile prompt token so a
# moving proxy cannot create a fresh dialog every step.
patch(
    "ModEntry.Alpha67442.cs",
    "            if (order == ShinyTacticalOrder.Engage)\n                ShinyPromptCooldownAlpha67442[token] = long.MaxValue;",
    "            ShinyPromptCooldownAlpha67442[token] = long.MaxValue;",
)
patch(
    "ModEntry.Alpha67442.cs",
    "    private static string BuildShinyPromptTokenAlpha67442(GameLocation location, Monster target)\n        => $\"{location.NameOrUniqueName}|{target.Name}|{target.Tile.X:0.##}|{target.Tile.Y:0.##}\";",
    "    private static string BuildShinyPromptTokenAlpha67442(GameLocation location, Monster target)\n        => $\"{location.NameOrUniqueName}|{target.Name}|{target.GetType().FullName}\";",
)

# Mutation + Pelipper compatibility. Normal wild Pelipper actors are eligible; confirmed Shiny,
# owned companions, bosses, scripted actors and existing Team Up exclusions remain protected.
patch(
    "Combat/MonsterMutationService.cs",
    "    private bool IsEligible(Monster monster)\n    {\n        if (IsMutant(monster)",
    "    private bool IsEligible(Monster monster)\n    {\n        // Repair stale false-Shiny state written by 6.7.44.2 before applying normal exclusions.\n        if (EncounterReactionService.IsConfirmedShiny(monster)\n            && !EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster))\n        {\n            monster.modData.Remove(EncounterReactionService.ShinyConfirmedMarker);\n            monster.modData.Remove(EncounterReactionService.ShinyEmergencyHoldMarker);\n            monster.modData.Remove(EncounterReactionService.ShinyEngagedMarker);\n            monster.modData.Remove(EncounterReactionService.ShinyIgnoredMarker);\n            monster.modData.Remove(MutationExcludedMarker);\n        }\n\n        if (IsMutant(monster)",
)
patch(
    "Combat/MonsterMutationService.cs",
    "        // Pelipper wild combat actors are capture entities, not mutation candidates. Companion\n        // and proxy actors excluded from Team Up combat are also left entirely under Pelipper ownership.\n        if (PelipperTownCompatibilityService.IsWildCombatActor(monster)\n            || PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))\n        {\n            return false;\n        }",
    "        // 6.7.44.3: normal Pelipper wild combat proxies are valid Mutation candidates.\n        // Confirmed Shiny always wins over Mutation. Owned/source-controlled companions remain\n        // excluded through the normal Team Up combat ownership gate.\n        bool pelipperWild = PelipperTownCompatibilityService.IsWildCombatActor(monster);\n        if (pelipperWild)\n        {\n            if (EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster))\n            {\n                monster.modData[MutationExcludedMarker] = \"true\";\n                return false;\n            }\n        }\n        else if (PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))\n        {\n            return false;\n        }",
)
patch(
    "Combat/MonsterMutationService.cs",
    "        monster.modData[MutantMarker] = \"1\";\n        monster.modData[MutationSourceMarker] = monster.GetType().FullName ?? monster.GetType().Name;",
    "        monster.modData[MutantMarker] = \"1\";\n        if (PelipperTownCompatibilityService.IsWildCombatActor(monster))\n        {\n            // Mutated wild proxies are combat threats, not capture-floor targets. Restore Team Up\n            // targeting even if the proxy was previously removed at Pelipper's mercy threshold.\n            monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = \"true\";\n        }\n        monster.modData[MutationSourceMarker] = monster.GetType().FullName ?? monster.GetType().Name;",
)
patch(
    "Combat/MonsterMutationService.cs",
    "/// Policy is mod-agnostic by default: normal custom monsters are eligible. Boss/script/event actors,\n/// Pelipper capture/companion actors, Surge spawns, mutation minions and already-mutated monsters are\n/// excluded.",
    "/// Policy is mod-agnostic by default: normal custom monsters and normal Pelipper wild combat proxies\n/// are eligible. Confirmed Shiny, owned companions, boss/script/event actors, Surge spawns, mutation\n/// minions and already-mutated monsters are excluded.",
)

print("6.7.44.3 runtime hotfix materialized successfully.")
