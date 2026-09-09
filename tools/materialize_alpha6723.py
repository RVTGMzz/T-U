from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if new in text:
        return text
    if old not in text:
        raise RuntimeError(f"{label}: anchor not found")
    return text.replace(old, new, 1)


# Version.
csproj_path = SRC / "TeamUp.csproj"
csproj = csproj_path.read_text(encoding="utf-8")
csproj = replace_once(
    csproj,
    "<Version>0.2.0-alpha.6.7.22</Version>",
    "<Version>0.2.0-alpha.6.7.23</Version>",
    "TeamUp.csproj version",
)
csproj_path.write_text(csproj, encoding="utf-8", newline="\n")

# Register the ceasefire watchdog + diagnostic command.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
entry = replace_once(
    entry,
    "        RegisterAlpha6722Events();\n",
    "        RegisterAlpha6722Events();\n        RegisterAlpha6723Events();\n",
    "Alpha 6.7.23 registration",
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

# One canonical offensive-target policy used by every autonomous Team Up damage layer.
policy = r'''using Ronvotri.TeamUp.Core;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Combat;

/// <summary>
/// Canonical autonomous-offense gate. A Pelipper wild combat proxy at its capture floor is never
/// a legal Team Up offensive target. This is intentionally separate from incoming-threat context:
/// allies may still guard/heal while the wild target remains alive, but no Team Up attack/signature
/// layer may select or damage it.
/// </summary>
internal static class TeamUpOffensiveTargetPolicy
{
    public static bool IsEligible(Monster monster)
    {
        if (monster.Health <= 0)
            return false;
        if (OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            return false;
        if (PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            return false;
        if (PelipperCaptureSafetyService.IsProtected(monster))
            return false;
        return true;
    }
}
'''
(SRC / "Combat" / "TeamUpOffensiveTargetPolicy.cs").write_text(policy, encoding="utf-8", newline="\n")

# Alpha6 signature prototype layer previously built its own raw Monster list and could bypass
# CombatService's <=10% offensive target filter.
alpha6_path = SRC / "Combat" / "Alpha6CombatPolishService.cs"
alpha6 = alpha6_path.read_text(encoding="utf-8")
alpha6 = replace_once(
    alpha6,
    '''        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();''',
    '''        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(TeamUpOffensiveTargetPolicy.IsEligible)
            .ToList();''',
    "Alpha6 signature offensive target filter",
)
alpha6_path.write_text(alpha6, encoding="utf-8", newline="\n")

# General Character Skill Identity layer had the same independent raw Monster list.
skill_path = SRC / "Combat" / "CharacterSkillIdentityService.cs"
skill = skill_path.read_text(encoding="utf-8")
skill = replace_once(
    skill,
    '''        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .ToList();''',
    '''        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(TeamUpOffensiveTargetPolicy.IsEligible)
            .ToList();''',
    "CharacterSkillIdentity offensive target filter",
)
# Second line of defense: even if a future caller passes a Pelipper target directly to this helper,
# clamp the actual requested damage to the active capture budget.
skill = replace_once(
    skill,
    '''    private static void DamageMonster(Monster monster, int damage, float knockback)
    {
        if (damage <= 0 || Game1.currentLocation is null)
            return;

        Game1.currentLocation.damageMonster(''',
    '''    private static void DamageMonster(Monster monster, int damage, float knockback)
    {
        if (damage <= 0 || Game1.currentLocation is null)
            return;

        damage = PelipperCaptureSafetyService.ClampDamage(monster, damage);
        if (damage <= 0)
            return;

        Game1.currentLocation.damageMonster(''',
    "CharacterSkillIdentity capture damage clamp",
)
skill_path.write_text(skill, encoding="utf-8", newline="\n")

# Special recruits already clamp damage, but they could still animate/stun at the floor. Remove
# protected targets from their candidate list so the behavior is a true ceasefire, not just 0 damage.
special_path = SRC / "Combat" / "SpecialRecruitCombatService.cs"
special = special_path.read_text(encoding="utf-8")
special = replace_once(
    special,
    '''        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            .ToList();''',
    '''        List<Monster> monsters = Game1.currentLocation.characters
            .OfType<Monster>()
            .Where(TeamUpOffensiveTargetPolicy.IsEligible)
            .ToList();''',
    "Special recruit offensive target filter",
)
special_path.write_text(special, encoding="utf-8", newline="\n")

# Dedicated floor watchdog. It only removes Team Up's own transient offensive marker; the durable
# wild-proxy identity remains intact, and Pelipper still owns its actor/controller/capture lifecycle.
command = r'''using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private void RegisterAlpha6723Events()
    {
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6723CaptureCeasefireUpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_capture_ceasefire",
            "Report Pelipper <=10% capture ceasefire state. Writes diagnostics/TeamUp_Capture_Ceasefire_latest.txt.",
            OnAlpha6723CaptureCeasefireCommand);
    }

    private void OnAlpha6723CaptureCeasefireUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || Game1.currentLocation is null)
            return;

        foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>())
        {
            if (!PelipperCaptureSafetyService.IsProtected(monster))
                continue;

            // Remove only Team Up's transient attack opt-in. Keep WildCombatProxyKey so the
            // capture-floor identity/watchdog remains durable and source-owned Pelipper behavior
            // is never taken over.
            monster.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
        }
    }

    private void OnAlpha6723CaptureCeasefireCommand(string command, string[] args)
    {
        List<string> lines = new()
        {
            "TEAM UP 6.7.23 - PELIPPER CAPTURE CEASEFIRE",
            $"captureEnabled={PelipperCaptureSafetyService.CurrentEnabled} threshold={PelipperCaptureSafetyService.CurrentThreshold:P1}",
            $"location={Game1.currentLocation?.NameOrUniqueName ?? "<none>"}"
        };

        if (!Context.IsWorldReady || Game1.currentLocation is null)
        {
            lines.Add("worldReady=False");
        }
        else
        {
            int rows = 0;
            foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>())
            {
                if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
                    continue;

                rows++;
                bool hasBudget = PelipperCaptureSafetyService.TryGetDamageBudget(monster, out int budget);
                bool protectedNow = PelipperCaptureSafetyService.IsProtected(monster);
                bool eligible = TeamUpOffensiveTargetPolicy.IsEligible(monster);
                bool target = monster.modData.TryGetValue(PelipperTownCompatibilityService.CombatTargetOptInKey, out string? rawTarget)
                    && rawTarget.Equals("true", StringComparison.OrdinalIgnoreCase);
                bool proxy = monster.modData.TryGetValue(PelipperTownCompatibilityService.WildCombatProxyKey, out string? rawProxy)
                    && rawProxy.Equals("true", StringComparison.OrdinalIgnoreCase);
                int floor = 0;
                PelipperCaptureSafetyService.TryGetCaptureFloor(monster, out floor);
                lines.Add($"wild type={monster.GetType().FullName} hp={monster.Health}/{monster.MaxHealth} floor={floor} budget={(hasBudget ? budget : -1)} protected={protectedNow} eligible={eligible} target={target} proxy={proxy}");
            }
            if (rows == 0)
                lines.Add("wild none");
        }

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Capture_Ceasefire_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Capture ceasefire diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6723.cs").write_text(command, encoding="utf-8", newline="\n")

print("Alpha 6.7.23 capture ceasefire hardening materialized.")
