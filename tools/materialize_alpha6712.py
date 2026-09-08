from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.12"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, content: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


project = read("TeamUp.csproj")
if f"<Version>{VERSION}</Version>" not in project:
    old = "<Version>0.2.0-alpha.6.7.11</Version>"
    if project.count(old) != 1:
        raise RuntimeError("Unexpected TeamUp.csproj version")
    write("TeamUp.csproj", project.replace(old, f"<Version>{VERSION}</Version>", 1))

# 6.7.12 hardens the capture floor in two important ways:
# 1) patch concrete takeDamage implementations even when they're declared on an abstract Monster base;
# 2) also clamp GameLocation.damageMonster area-damage entry points before they fan out to targets.
capture_patch = r'''using System.Reflection;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Core;

/// <summary>
/// Global Pelipper capture-floor safety. Team Up never owns Pelipper combat actors here; it only
/// prevents friendly damage from crossing the source mod's mercy/capture threshold.
///
/// Alpha 6.7.12 closes two gaps seen in live party play:
/// - a concrete takeDamage implementation inherited from an abstract custom Monster base was not
///   patched by the old concrete-type-only scan;
/// - party/companion area damage can enter through GameLocation.damageMonster before reaching a
///   custom proxy implementation.
/// </summary>
internal static class PelipperCaptureDamagePatch
{
    private static readonly HashSet<MethodBase> PatchedTakeDamageMethods = new();
    private static readonly HashSet<MethodBase> PatchedAreaDamageMethods = new();

    public static int TakeDamagePatchCount => PatchedTakeDamageMethods.Count;
    public static int AreaDamagePatchCount => PatchedAreaDamageMethods.Count;

    public static void Apply(IMonitor monitor, string uniqueId)
    {
        var harmony = new Harmony(uniqueId + ".PelipperCaptureFloor");
        HarmonyMethod takeDamagePrefix = new(typeof(PelipperCaptureDamagePatch), nameof(BeforeTakeDamage));
        HarmonyMethod areaDamagePrefix = new(typeof(PelipperCaptureDamagePatch), nameof(BeforeAreaDamage));
        int addedTakeDamage = 0;
        int addedAreaDamage = 0;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in SafeGetTypes(assembly))
            {
                if (typeof(Monster).IsAssignableFrom(type))
                {
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (!method.Name.Equals("takeDamage", StringComparison.Ordinal)
                            || method.IsAbstract
                            || method.GetParameters().Length == 0
                            || method.GetParameters()[0].ParameterType != typeof(int)
                            || !PatchedTakeDamageMethods.Add(method))
                        {
                            continue;
                        }

                        try
                        {
                            harmony.Patch(method, prefix: takeDamagePrefix);
                            addedTakeDamage++;
                        }
                        catch (Exception ex)
                        {
                            PatchedTakeDamageMethods.Remove(method);
                            monitor.LogOnce($"Capture floor could not patch {type.FullName}.{method.Name}: {ex.Message}", LogLevel.Trace);
                        }
                    }
                }

                if (!typeof(GameLocation).IsAssignableFrom(type))
                    continue;

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!method.Name.Equals("damageMonster", StringComparison.Ordinal)
                        || method.IsAbstract
                        || !HasAreaDamageShape(method)
                        || !PatchedAreaDamageMethods.Add(method))
                    {
                        continue;
                    }

                    try
                    {
                        harmony.Patch(method, prefix: areaDamagePrefix);
                        addedAreaDamage++;
                    }
                    catch (Exception ex)
                    {
                        PatchedAreaDamageMethods.Remove(method);
                        monitor.LogOnce($"Capture floor could not patch {type.FullName}.{method.Name}: {ex.Message}", LogLevel.Trace);
                    }
                }
            }
        }

        if (addedTakeDamage > 0 || addedAreaDamage > 0)
        {
            monitor.Log(
                $"Alpha 6.7.12 capture floor active: takeDamage={TakeDamagePatchCount}, areaDamage={AreaDamagePatchCount}.",
                LogLevel.Debug);
        }
    }

    // Harmony's __0 binds by argument position, so this remains compatible with different
    // takeDamage parameter names used by custom Monster subclasses.
    private static void BeforeTakeDamage(Monster __instance, ref int __0)
    {
        if (__0 <= 0)
            return;

        __0 = PelipperCaptureSafetyService.ClampDamage(__instance, __0);
    }

    // object[] lets this guard cover vanilla and compatible custom GameLocation.damageMonster
    // overloads without hard-binding one Stardew signature. We only touch Rectangle + named
    // damage integer arguments, never trajectory/precision/knockback/etc.
    private static void BeforeAreaDamage(GameLocation __instance, MethodBase __originalMethod, object[] __args)
    {
        if (__instance is null || __args.Length == 0)
            return;

        ParameterInfo[] parameters = __originalMethod.GetParameters();
        int areaIndex = -1;
        for (int i = 0; i < parameters.Length && i < __args.Length; i++)
        {
            if (parameters[i].ParameterType == typeof(Rectangle) && __args[i] is Rectangle)
            {
                areaIndex = i;
                break;
            }
        }

        if (areaIndex < 0 || __args[areaIndex] is not Rectangle area)
            return;

        int sharedBudget = int.MaxValue;
        foreach (Monster monster in __instance.characters.OfType<Monster>())
        {
            if (monster.Health <= 0 || !area.Intersects(monster.GetBoundingBox()))
                continue;
            if (!PelipperCaptureSafetyService.TryGetDamageBudget(monster, out int budget))
                continue;
            sharedBudget = Math.Min(sharedBudget, budget);
        }

        if (sharedBudget == int.MaxValue)
            return;

        for (int i = 0; i < parameters.Length && i < __args.Length; i++)
        {
            if (!IsDamageParameter(parameters[i]) || __args[i] is not int damage)
                continue;
            __args[i] = Math.Min(Math.Max(0, damage), sharedBudget);
        }
    }

    private static bool HasAreaDamageShape(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();
        return parameters.Any(parameter => parameter.ParameterType == typeof(Rectangle))
            && parameters.Any(IsDamageParameter);
    }

    private static bool IsDamageParameter(ParameterInfo parameter)
    {
        if (parameter.ParameterType != typeof(int))
            return false;

        string name = Normalize(parameter.Name ?? string.Empty);
        return name is "damage" or "mindamage" or "maxdamage";
    }

    private static string Normalize(string text)
        => new(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }
}
'''
write("Core/PelipperCaptureDamagePatch.cs", capture_patch)

safety = read("Core/PelipperCaptureSafetyService.cs")
if "using StardewValley;\n" not in safety:
    needle = "using System.Reflection;\nusing StardewValley.Monsters;"
    if safety.count(needle) != 1:
        raise RuntimeError("Unexpected PelipperCaptureSafetyService using block")
    safety = safety.replace(needle, "using System.Reflection;\nusing StardewValley;\nusing StardewValley.Monsters;", 1)

if "public static int RepairCurrentLocationFloors" not in safety:
    anchor = "    public static float CurrentThreshold => _threshold;\n    public static bool CurrentEnabled => _enabled;\n"
    if safety.count(anchor) != 1:
        raise RuntimeError("PelipperCaptureSafetyService status anchor missing")
    addition = r'''    public static bool TryGetCaptureFloor(Monster monster, out int stopAtHealth)
    {
        stopAtHealth = 0;
        if (monster.Health <= 0 || monster.MaxHealth <= 0)
            return false;
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return false;

        RefreshPolicyIfNeeded();
        if (!_enabled)
            return false;

        stopAtHealth = Math.Max(1, (int)Math.Ceiling(monster.MaxHealth * _threshold));
        return true;
    }

    /// <summary>
    /// Last-resort repair for custom friendly damage paths that directly lower Health without
    /// crossing a patched damage entry point. This never revives a dead/removed monster; it only
    /// restores a still-live wild Pelipper proxy to the active capture floor.
    /// </summary>
    public static int RepairCurrentLocationFloors(GameLocation? location)
    {
        if (location is null || !_enabled)
            return 0;

        int repaired = 0;
        foreach (Monster monster in location.characters.OfType<Monster>())
        {
            if (monster.Health <= 0
                || !TryGetCaptureFloor(monster, out int floor)
                || monster.Health >= floor)
            {
                continue;
            }

            monster.Health = floor;
            repaired++;
        }
        return repaired;
    }

'''
    safety = safety.replace(anchor, addition + anchor, 1)
write("Core/PelipperCaptureSafetyService.cs", safety)

alpha6615 = read("ModEntry.Alpha6615.cs")
if '"teamup_capture"' not in alpha6615:
    old = "        Helper.Events.Display.RenderedActiveMenu += OnAlpha6615RenderedActiveMenu;\n    }"
    new = "        Helper.Events.Display.RenderedActiveMenu += OnAlpha6615RenderedActiveMenu;\n        Helper.ConsoleCommands.Add(\n            \"teamup_capture\",\n            \"Show Team Up Pelipper capture-floor diagnostics.\",\n            OnAlpha6712CaptureStatus);\n    }"
    if alpha6615.count(old) != 1:
        raise RuntimeError("Alpha6615 registration anchor missing")
    alpha6615 = alpha6615.replace(old, new, 1)

old_update = r'''    private void OnAlpha6615UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !e.IsMultipleOf(15))
            return;

        DetectPlayerCompanionRecallAttemptsAlpha6615();
    }
'''
new_update = r'''    private void OnAlpha6615UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        // 6.7.12: mercy/capture is a world combat invariant, not an NPC-party behavior. Keep a
        // per-tick last-resort floor repair active even while party composition changes.
        int repaired = PelipperCaptureSafetyService.RepairCurrentLocationFloors(Game1.currentLocation);
        if (repaired > 0)
            Monitor.LogOnce("Alpha 6.7.12 repaired a live wild Pokemon below the active capture floor.", LogLevel.Trace);

        if (!e.IsMultipleOf(15))
            return;

        DetectPlayerCompanionRecallAttemptsAlpha6615();
    }
'''
if new_update not in alpha6615:
    if alpha6615.count(old_update) != 1:
        raise RuntimeError("Alpha6615 UpdateTicked block missing/ambiguous")
    alpha6615 = alpha6615.replace(old_update, new_update, 1)

if "private void OnAlpha6712CaptureStatus" not in alpha6615:
    anchor = "    private void OnAlpha6615ReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)\n"
    if alpha6615.count(anchor) != 1:
        raise RuntimeError("Alpha6615 ReturnedToTitle anchor missing")
    method = r'''    private void OnAlpha6712CaptureStatus(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_capture requires a loaded save.", LogLevel.Info);
            return;
        }

        List<string> targets = new();
        if (Game1.currentLocation is not null)
        {
            foreach (StardewValley.Monsters.Monster monster in Game1.currentLocation.characters.OfType<StardewValley.Monsters.Monster>())
            {
                if (!PelipperCaptureSafetyService.TryGetDamageBudget(monster, out int budget))
                    continue;
                int floor = Math.Max(1, (int)Math.Ceiling(monster.MaxHealth * PelipperCaptureSafetyService.CurrentThreshold));
                targets.Add($"{monster.Name}:{monster.Health}/{monster.MaxHealth}:floor={floor}:budget={budget}");
            }
        }

        Monitor.Log(
            $"Capture floor: enabled={PelipperCaptureSafetyService.CurrentEnabled}, threshold={PelipperCaptureSafetyService.CurrentThreshold:P0}, " +
            $"takeDamagePatches={PelipperCaptureDamagePatch.TakeDamagePatchCount}, areaDamagePatches={PelipperCaptureDamagePatch.AreaDamagePatchCount}, " +
            $"wildTargets=[{string.Join(", ", targets)}]",
            LogLevel.Info);
    }

'''
    alpha6615 = alpha6615.replace(anchor, method + anchor, 1)
write("ModEntry.Alpha6615.cs", alpha6615)

rank = read("Core/CombatRankCatalog.cs")
old_rank = "            CombatRank.S => new Color(218, 155, 48),"
new_rank = "            CombatRank.S => new Color(132, 70, 12),"
if new_rank not in rank:
    if rank.count(old_rank) != 1:
        raise RuntimeError("S-rank color anchor missing")
    rank = rank.replace(old_rank, new_rank, 1)
write("Core/CombatRankCatalog.cs", rank)

print("Alpha 6.7.12 source materialized.")
