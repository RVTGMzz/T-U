from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.10"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, content: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def replace_once(rel: str, old: str, new: str, token: str) -> None:
    text = read(rel)
    if token in text:
        return
    count = text.count(old)
    if count != 1:
        raise RuntimeError(f"{rel}: expected one anchor for {token}, found {count}")
    write(rel, text.replace(old, new, 1))


# Version.
project = read("TeamUp.csproj")
if f"<Version>{VERSION}</Version>" not in project:
    if "<Version>0.2.0-alpha.6.7.9</Version>" not in project:
        raise RuntimeError("Unexpected TeamUp.csproj version")
    write("TeamUp.csproj", project.replace(
        "<Version>0.2.0-alpha.6.7.9</Version>",
        f"<Version>{VERSION}</Version>",
        1,
    ))

# 6.7.2 animation unlock must never destroy vanilla route metadata. That null assignment can leave
# NPC.update calling loadEndOfRouteBehavior(null), which matches the Gunther base-update crash log.
replace_once(
    "Following/FollowService.cs",
    "        npc.doingEndOfRouteAnimation.Value = false;\n"
    "        npc.goingToDoEndOfRouteAnimation.Value = false;\n"
    "        npc.endOfRouteBehaviorName.Value = null;\n"
    "        npc.nextEndOfRouteMessage = null;\n"
    "        npc.endOfRouteMessage.Value = null;\n",
    "        npc.doingEndOfRouteAnimation.Value = false;\n"
    "        npc.goingToDoEndOfRouteAnimation.Value = false;\n"
    "        // Alpha 6.7.10: preserve endOfRouteBehaviorName/messages. Team Up suspends route\n"
    "        // execution while it owns the NPC instead of corrupting the schedule metadata.\n",
    "Alpha 6.7.10: preserve endOfRouteBehaviorName/messages",
)

# Rank A overrides requested by design. Rank remains identity/ceiling, not a flat stat multiplier.
replace_once(
    "Core/CombatRankCatalog.cs",
    '        ["Morris"] = new(CombatRank.C),\n',
    '        ["Morris"] = new(CombatRank.C),\n'
    '        ["Abigail"] = new(CombatRank.A),\n'
    '        ["Alex"] = new(CombatRank.A),\n'
    '        ["Haley"] = new(CombatRank.A),\n'
    '        ["Maru"] = new(CombatRank.A),\n'
    '        ["Evelyn"] = new(CombatRank.A),\n',
    '["Evelyn"] = new(CombatRank.A)',
)

# A-rank identity tuning for Haley and Evelyn. Small cooldown/utility upgrades, not raw blanket buffs.
skills = read("Core/CharacterSkillIdentityCatalog.cs")
old_haley = '''            ["Haley"] = I("Haley", "FLASH SHOT", CharacterSignatureArchetype.Burst, 600, 510,
                damage: 11, radius: 8.0f, maxTargets: 1, stun3: 180, knockback: 1.2f,
                buffTicks: 240, damageBuff: 0.08f, cdrBuff: 2),'''
new_haley = '''            ["Haley"] = I("Haley", "FLASH SHOT", CharacterSignatureArchetype.Burst, 570, 480,
                damage: 11, radius: 8.5f, maxTargets: 1, stun2: 120, stun3: 260, knockback: 1.2f,
                buffTicks: 270, damageBuff: 0.08f, cdrBuff: 3),'''
if new_haley not in skills:
    if old_haley not in skills:
        raise RuntimeError("Haley skill anchor missing")
    skills = skills.replace(old_haley, new_haley, 1)

old_evelyn = '''            ["Evelyn"] = I("Evelyn", "GARDEN REMEDY", CharacterSignatureArchetype.Recovery, 810, 720,
                heal: 10, radius: 7.0f, maxTargets: 4, triggerHp: 0.82f,
                buffTicks: 420, healBuff: 0.10f, partyWide: true),'''
new_evelyn = '''            ["Evelyn"] = I("Evelyn", "GARDEN REMEDY", CharacterSignatureArchetype.Recovery, 750, 660,
                heal: 11, radius: 7.5f, maxTargets: 5, triggerHp: 0.86f,
                buffTicks: 480, healBuff: 0.12f, cdrBuff: 2, partyWide: true),'''
if new_evelyn not in skills:
    if old_evelyn not in skills:
        raise RuntimeError("Evelyn skill anchor missing")
    skills = skills.replace(old_evelyn, new_evelyn, 1)
write("Core/CharacterSkillIdentityCatalog.cs", skills)

# A-rank tuning for the Alpha6 prototype owners. Preserve single signature authority.
polish = read("Combat/Alpha6CombatPolishService.cs")
repls = [
    (".Take(tier >= 3 ? 5 : 3)", ".Take(tier >= 3 ? 6 : 3)"),
    ("int baseDamage = tier >= 3 ? 8 + affinity * 2 : 5 + affinity;", "int baseDamage = tier >= 3 ? 9 + affinity * 2 : 5 + affinity;"),
    ("tier >= 3 ? 480 : 600", "tier >= 3 ? 450 : 600"),
    ("if (nearby.Count < 2 && farmerRatio > 0.45f)", "if (nearby.Count < 2 && farmerRatio > 0.55f)"),
    ("_progression.GetMaxHealth(member) / 12", "_progression.GetMaxHealth(member) / 10"),
    ("tier >= 3 ? 600 : 720", "tier >= 3 ? 570 : 720"),
]
for old, new in repls:
    if new in polish:
        continue
    if old not in polish:
        raise RuntimeError(f"Alpha6 polish anchor missing: {old}")
    polish = polish.replace(old, new, 1)

# Maru can justify A-rank control against one elite/boss instead of requiring two trash mobs.
old_maru_gate = '''        if (candidates.Count < 2)
            return false;
'''
new_maru_gate = '''        if (candidates.Count == 0)
            return false;
        if (candidates.Count < 2 && !candidates.Any(monster => monster.MaxHealth >= 300))
            return false;
'''
if new_maru_gate not in polish:
    # Scope this replacement after TryMaruUpgrade so we don't alter another method by accident.
    pos = polish.find("private bool TryMaruUpgrade")
    if pos < 0:
        raise RuntimeError("TryMaruUpgrade missing")
    tail = polish[pos:]
    if old_maru_gate not in tail:
        raise RuntimeError("Maru candidate gate anchor missing")
    tail = tail.replace(old_maru_gate, new_maru_gate, 1)
    polish = polish[:pos] + tail
write("Combat/Alpha6CombatPolishService.cs", polish)

# General combat coordination: major threats near Farmer get priority for Damage/Control/Aggressive
# members, while Tank/Healer retain their role-specific targeting. Also use hitbox distance for giant bosses.
combat = read("Combat/CombatService.cs")
old_target = "            Monster? target = AcquireTarget(npc, member, role, monsters, activeMembers, validThreatActors, assignedCounts);"
new_target = "            Monster? target = AcquireCoordinatedTarget(npc, member, role, monsters, activeMembers, validThreatActors, assignedCounts);"
if new_target not in combat:
    if combat.count(old_target) != 1:
        raise RuntimeError("Combat target acquisition anchor missing/ambiguous")
    combat = combat.replace(old_target, new_target, 1)

old_distance = "            float distanceToTarget = Vector2.Distance(npc.Tile, target.Tile);"
new_distance = "            float distanceToTarget = GetDistanceToMonsterBoundsTiles(npc, target);"
if new_distance not in combat:
    if combat.count(old_distance) != 1:
        raise RuntimeError("Combat distance anchor missing/ambiguous")
    combat = combat.replace(old_distance, new_distance, 1)

helper_anchor = "    private void PulseAmbientThreat(IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)\n"
helper_code = r'''    private Monster? AcquireCoordinatedTarget(
        NPC npc,
        PartyMemberData member,
        PartyRole role,
        IReadOnlyList<Monster> monsters,
        IReadOnlyList<PartyMemberData> activeMembers,
        IReadOnlyCollection<string> validThreatActors,
        IReadOnlyDictionary<Monster, int> assignedCounts)
    {
        Monster? normal = AcquireTarget(npc, member, role, monsters, activeMembers, validThreatActors, assignedCounts);
        if (role is PartyRole.Tank or PartyRole.Healer || monsters.Count == 0)
            return normal;

        bool shouldCoordinate = _strategy() == PartyStrategy.BossFocus
            || role is PartyRole.Damage or PartyRole.Control
            || member.Engagement is EngagementStyle.Aggressive or EngagementStyle.Reckless;
        if (!shouldCoordinate)
            return normal;

        Monster? major = monsters
            .Where(monster => monster.Health > 0)
            .Where(monster => Vector2.Distance(monster.Tile, FarmerContext.Tile) <= 10f)
            .Where(monster => monster.MaxHealth >= 300)
            .OrderByDescending(monster => monster.MaxHealth)
            .ThenBy(monster => Vector2.DistanceSquared(monster.Tile, FarmerContext.Tile))
            .FirstOrDefault();

        return major ?? normal;
    }

    private static float GetDistanceToMonsterBoundsTiles(NPC npc, Monster target)
    {
        Rectangle a = npc.GetBoundingBox();
        Rectangle b = target.GetBoundingBox();
        int dx = a.Right < b.Left ? b.Left - a.Right : b.Right < a.Left ? a.Left - b.Right : 0;
        int dy = a.Bottom < b.Top ? b.Top - a.Bottom : b.Bottom < a.Top ? a.Top - b.Bottom : 0;
        return MathF.Sqrt(dx * dx + dy * dy) / 64f;
    }

'''
if "private Monster? AcquireCoordinatedTarget(" not in combat:
    if helper_anchor not in combat:
        raise RuntimeError("Combat helper insertion anchor missing")
    combat = combat.replace(helper_anchor, helper_code + helper_anchor, 1)
write("Combat/CombatService.cs", combat)

# Route behavior Harmony safety. While Team Up owns an NPC, vanilla route actions are suspended,
# not deleted. Empty/null names are also treated as a no-op instead of crashing the base update loop.
route_patch = r'''using System.Reflection;
using HarmonyLib;
using Ronvotri.TeamUp.Following;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp.Core;

internal static class NpcRouteStateSafetyPatch
{
    public static bool IsApplied { get; private set; }

    public static void Apply(IMonitor monitor, string uniqueId)
    {
        if (IsApplied)
            return;

        MethodInfo? original = AccessTools.Method(typeof(NPC), "loadEndOfRouteBehavior", new[] { typeof(string) });
        if (original is null)
        {
            monitor.Log("Alpha 6.7.10 could not locate NPC.loadEndOfRouteBehavior(String). Route-state crash guard is unavailable.", LogLevel.Error);
            return;
        }

        var harmony = new Harmony($"{uniqueId}.Alpha6710RouteStateSafety");
        harmony.Patch(
            original,
            prefix: new HarmonyMethod(typeof(NpcRouteStateSafetyPatch), nameof(Prefix)));
        IsApplied = true;
        monitor.Log("Alpha 6.7.10 NPC route-state safety patch applied.", LogLevel.Info);
    }

    private static bool Prefix(NPC __instance, string? __0)
    {
        bool teamUpControlled = __instance.modData.ContainsKey(FollowService.PartyControlledModDataKey);
        if (teamUpControlled)
            return false;

        // Defensive recovery for stale state produced by an older Team Up build or another mod.
        // Vanilla cannot do useful work with an empty behavior name, so skipping is safer than
        // letting NPC.update repeatedly crash the entire base update loop.
        return !string.IsNullOrWhiteSpace(__0);
    }
}
'''
write("Core/NpcRouteStateSafetyPatch.cs", route_patch)

alpha6710 = r'''using Ronvotri.TeamUp.Core;
using StardewModdingAPI;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha6710Registered;

    private void EnsureAlpha6710Registered()
    {
        if (Alpha6710Registered)
            return;
        Alpha6710Registered = true;

        NpcRouteStateSafetyPatch.Apply(Monitor, ModManifest.UniqueID);
        Helper.ConsoleCommands.Add(
            "teamup_route_guard",
            "Show Alpha 6.7.10 NPC end-of-route crash guard status.",
            OnAlpha6710RouteGuardStatus);
    }

    private void OnAlpha6710RouteGuardStatus(string command, string[] args)
        => Monitor.Log($"Alpha 6.7.10 routeGuard={NpcRouteStateSafetyPatch.IsApplied}.", LogLevel.Info);
}
'''
write("ModEntry.Alpha6710.cs", alpha6710)

replace_once(
    "ModEntry.Alpha6625.cs",
    "        // Alpha 6.7.9: chemistry variants + memory-aware line pools.\n        EnsureAlpha679ChemistryVariantsRegistered();\n",
    "        // Alpha 6.7.9: chemistry variants + memory-aware line pools.\n        EnsureAlpha679ChemistryVariantsRegistered();\n\n"
    "        // Alpha 6.7.10: preserve route metadata, guard loadEndOfRouteBehavior and tune A-rank combat.\n"
    "        EnsureAlpha6710Registered();\n",
    "EnsureAlpha6710Registered();",
)

print("Alpha 6.7.10 source materialized.")
