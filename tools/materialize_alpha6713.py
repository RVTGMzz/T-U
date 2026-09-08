from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.13"


def read(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def write(rel: str, content: str) -> None:
    path = SRC / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if new in text:
        return text
    if text.count(old) != 1:
        raise RuntimeError(f"{label}: expected exactly one old block, found {text.count(old)}")
    return text.replace(old, new, 1)


# Version.
project = read("TeamUp.csproj")
project = replace_once(
    project,
    "<Version>0.2.0-alpha.6.7.12</Version>",
    f"<Version>{VERSION}</Version>",
    "project version",
)
write("TeamUp.csproj", project)

# -----------------------------------------------------------------------------
# CAPTURE FIX A: align capture identity with Team Up's existing Pelipper combat
# proxy classification. Pelipper creates a visible Wild NPC plus a separate
# Monster combat proxy; the proxy doesn't always expose 'Wild' in name/type.
# -----------------------------------------------------------------------------
compat = read("Core/PelipperTownCompatibilityService.cs")
compat = replace_once(
    compat,
    "using System.Reflection;\nusing StardewValley;",
    "using System.Reflection;\nusing StardewValley;\nusing StardewValley.Monsters;",
    "compat Monster using",
)
compat = replace_once(
    compat,
    '    public const string CombatTargetOptInKey = "Ronvotri.TeamUp/CombatTarget";\n',
    '    public const string CombatTargetOptInKey = "Ronvotri.TeamUp/CombatTarget";\n'
    '    public const string WildCombatProxyKey = "Ronvotri.TeamUp/PelipperWildCombatProxy";\n',
    "wild proxy key",
)
old_wild = '''    // Capture safety must not depend on Team Up's 5-tick combat opt-in marker. Pelipper's own
    // Pokemon can damage a wild proxy before that marker exists, so expose direct wild identity.
    public static bool IsWildCombatActor(NPC actor)
        => LooksLikePelipperActor(actor) && LooksWild(actor);
'''
new_wild = '''    // Pelipper Town builds each wild encounter as a visible Wild NPC plus a separate Monster
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
'''
compat = replace_once(compat, old_wild, new_wild, "wild combat identity")
write("Core/PelipperTownCompatibilityService.cs", compat)

alpha6613 = read("ModEntry.Alpha6613.cs")
old_proxy = '''            if (owned.Contains(monster))
            {
                monster.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
                continue;
            }

            monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
'''
new_proxy = '''            if (owned.Contains(monster))
            {
                monster.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
                monster.modData.Remove(PelipperTownCompatibilityService.WildCombatProxyKey);
                continue;
            }

            // The same unowned Pelipper Monster proxy that Team Up opts into combat is the exact
            // entity whose HP must obey Pelipper's capture/mercy floor.
            monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
            monster.modData[PelipperTownCompatibilityService.WildCombatProxyKey] = "true";
'''
alpha6613 = replace_once(alpha6613, old_proxy, new_proxy, "proxy marker")
write("ModEntry.Alpha6613.cs", alpha6613)

# -----------------------------------------------------------------------------
# RECRUIT FIX: selected species and source-enabled state are different concepts.
# Pelipper GMCM explicitly keeps the selected Pokemon when Companion enabled is off.
# Recruitment must still show NPC only vs NPC + Pokemon in that state.
# -----------------------------------------------------------------------------
native = read("Core/PelipperTown119NativeBridge.cs")
old_disabled_gate = '''        if (TryIsVillagerCompanionConfiguredEnabled(npcName, out bool enabled) && !enabled)
            return true;

'''
if old_disabled_gate in native:
    native = native.replace(old_disabled_gate, "", 1)
write("Core/PelipperTown119NativeBridge.cs", native)

# A small canonical-owner helper also tries displayName for expansion NPCs whose Pelipper config
# key differs from Stardew's internal NPC name. Returned Team Up descriptors remain bound to the
# actual NPC.Name so party ownership stays stable.
alpha6713 = r'''using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private bool Alpha6713Registered;

    private void EnsureAlpha6713Registered()
    {
        if (Alpha6713Registered)
            return;
        Alpha6713Registered = true;

        // Dedicated safety event. Alpha 6.6.26 intentionally unsubscribes legacy Pelipper polling
        // handlers, including OnAlpha6615UpdateTicked, so capture safety must never live there.
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6713CaptureFloorUpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_recruit_probe",
            "Inspect Pelipper configured recruitment intent. Usage: teamup_recruit_probe <NPC name>.",
            OnAlpha6713RecruitProbe);
        Helper.ConsoleCommands.Add(
            "teamup_capture_proxy",
            "Inspect every Pelipper Monster proxy in the current location.",
            OnAlpha6713CaptureProxyProbe);

        Monitor.Log("Team Up Alpha 6.7.13 capture-proxy + recruitment-intent guard enabled.", LogLevel.Info);
    }

    private void OnAlpha6713CaptureFloorUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;

        int repaired = PelipperCaptureSafetyService.RepairCurrentLocationFloors(Game1.currentLocation);
        if (repaired > 0)
        {
            Monitor.LogOnce(
                "Alpha 6.7.13 repaired a still-live Pelipper combat proxy below the active capture floor.",
                LogLevel.Trace);
        }
    }

    private string ResolvePelipperVillagerConfigKeyAlpha6713(NPC owner)
    {
        string[] candidates = new[] { owner.Name, owner.displayName }
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (string candidate in candidates)
        {
            if (PelipperTown119NativeBridge.TryGetConfiguredVillagerCompanionDescriptor(
                    candidate,
                    out LiveCompanionDescriptor? descriptor)
                && descriptor is not null)
            {
                return candidate;
            }
        }

        return owner.Name;
    }

    private LiveCompanionDescriptor? GetConfiguredRecruitDescriptorAlpha6713(NPC owner)
    {
        string configKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
        if (!PelipperTown119NativeBridge.TryGetConfiguredVillagerCompanionDescriptor(
                configKey,
                out LiveCompanionDescriptor? descriptor)
            || descriptor is null)
        {
            return null;
        }

        if (configKey.Equals(owner.Name, StringComparison.OrdinalIgnoreCase))
            return descriptor;

        string providerUnitId = $"configured:npc:{owner.Name}:{descriptor.CharacterName}";
        return new LiveCompanionDescriptor
        {
            UnitId = $"{PelipperTownCompatibilityService.ProviderId}:{providerUnitId}",
            CharacterName = descriptor.CharacterName,
            DisplayName = descriptor.DisplayName,
            OwnerKind = CompanionOwnerKind.PartyMember,
            OwnerCharacterName = owner.Name,
            ProviderId = PelipperTownCompatibilityService.ProviderId,
            ProviderUnitId = providerUnitId
        };
    }

    private void OnAlpha6713RecruitProbe(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_recruit_probe requires a loaded save.", LogLevel.Info);
            return;
        }

        string requested = string.Join(" ", args).Trim();
        if (string.IsNullOrWhiteSpace(requested))
        {
            Monitor.Log("Usage: teamup_recruit_probe <NPC name>.", LogLevel.Info);
            return;
        }

        NPC? owner = Game1.getCharacterFromName(requested)
            ?? Game1.locations.SelectMany(location => location.characters.OfType<NPC>())
                .FirstOrDefault(npc => npc.displayName.Equals(requested, StringComparison.OrdinalIgnoreCase));
        if (owner is null)
        {
            Monitor.Log($"Recruit probe: NPC '{requested}' not found.", LogLevel.Info);
            return;
        }

        ConfigurePelipperApiBridgeAlpha6619();
        string configKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
        bool hasEnabled = PelipperTown119NativeBridge.TryIsVillagerCompanionConfiguredEnabled(configKey, out bool enabled);
        PelipperTown119NativeBridge.TryGetConfiguredVillagerCompanionDescriptor(configKey, out LiveCompanionDescriptor? configured);
        LiveCompanionDescriptor? live = CompanionIntegrationService.FindLinkedCompanion(owner);
        LiveCompanionDescriptor? recruit = FindRecruitCandidateCompanionAlpha671(owner);

        Monitor.Log(
            $"Recruit probe: internal={owner.Name}, display={owner.displayName}, configKey={configKey}, " +
            $"configuredEnabled={(hasEnabled ? enabled.ToString() : "unknown")}, configured={configured?.DisplayName ?? "none"}, " +
            $"live={live?.DisplayName ?? "none"}, recruitChoice={recruit?.DisplayName ?? "none"}.",
            LogLevel.Info);
    }

    private void OnAlpha6713CaptureProxyProbe(string command, string[] args)
    {
        if (!Context.IsWorldReady || Game1.currentLocation is null)
        {
            Monitor.Log("teamup_capture_proxy requires a loaded location.", LogLevel.Info);
            return;
        }

        List<string> rows = new();
        foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>())
        {
            if (!PelipperTownCompatibilityService.LooksLikePelipperActor(monster))
                continue;

            bool target = monster.modData.TryGetValue(PelipperTownCompatibilityService.CombatTargetOptInKey, out string? targetRaw)
                && targetRaw.Equals("true", StringComparison.OrdinalIgnoreCase);
            bool proxy = monster.modData.TryGetValue(PelipperTownCompatibilityService.WildCombatProxyKey, out string? proxyRaw)
                && proxyRaw.Equals("true", StringComparison.OrdinalIgnoreCase);
            bool wild = PelipperTownCompatibilityService.IsWildCombatActor(monster);
            bool limited = PelipperCaptureSafetyService.TryGetDamageBudget(monster, out int budget);
            rows.Add($"{monster.Name}:{monster.Health}/{monster.MaxHealth}:type={monster.GetType().FullName}:target={target}:proxy={proxy}:wild={wild}:budget={(limited ? budget : -1)}");
        }

        Monitor.Log($"Capture proxy probe [{string.Join(" | ", rows)}]", LogLevel.Info);
    }
}
'''
write("ModEntry.Alpha6713.cs", alpha6713)

# Recruitment helper now asks live runtime first, then configured intent independent of current
# source visibility/enabled state.
alpha671 = read("ModEntry.Alpha671.cs")
old_recruit = '''    private LiveCompanionDescriptor? FindRecruitCandidateCompanionAlpha671(NPC owner)
    {
        ConfigurePelipperApiBridgeAlpha6619();

        LiveCompanionDescriptor? live = CompanionIntegrationService.FindLinkedCompanion(owner);
        if (live is not null)
            return live;

        return PelipperTown119NativeBridge.TryGetConfiguredVillagerCompanionDescriptor(
            owner.Name,
            out LiveCompanionDescriptor? configured)
                ? configured
                : null;
    }
'''
new_recruit = '''    private LiveCompanionDescriptor? FindRecruitCandidateCompanionAlpha671(NPC owner)
    {
        ConfigurePelipperApiBridgeAlpha6619();

        LiveCompanionDescriptor? live = CompanionIntegrationService.FindLinkedCompanion(owner);
        if (live is not null)
            return live;

        // Recruitment intent is the configured species, not the current enabled/render state.
        // This also tries displayName for expansion NPCs while rebinding ownership to NPC.Name.
        return GetConfiguredRecruitDescriptorAlpha6713(owner);
    }
'''
alpha671 = replace_once(alpha671, old_recruit, new_recruit, "recruit helper")
old_dormant = '''        if (!PelipperTown119NativeBridge.TryGetConfiguredVillagerCompanionDescriptor(
                owner.Name,
                out LiveCompanionDescriptor? configured)
            || configured is null)
        {
            return false;
        }
'''
new_dormant = '''        LiveCompanionDescriptor? configured = GetConfiguredRecruitDescriptorAlpha6713(owner);
        if (configured is null)
            return false;
'''
alpha671 = replace_once(alpha671, old_dormant, new_dormant, "dormant configured helper")
write("ModEntry.Alpha671.cs", alpha671)

# NPC recruitment applies the source setting using the key Pelipper actually recognizes.
alpha663 = read("ModEntry.Alpha663.cs")
old_apply = '''        if (PelipperTown119NativeBridge.HasVillagerLifecycle)
            PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled(owner.Name, includeCompanion, out _);
'''
new_apply = '''        if (PelipperTown119NativeBridge.HasVillagerLifecycle)
        {
            string pelipperOwnerKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
            PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled(pelipperOwnerKey, includeCompanion, out _);
        }
'''
alpha663 = replace_once(alpha663, old_apply, new_apply, "recruit source key")
write("ModEntry.Alpha663.cs", alpha663)

# Native lifecycle convergence now checks both runtime visibility and Pelipper's configured enabled
# bit. A missing runtime lookup must not cause an NPC-only request to skip disabling the source.
alpha6621 = read("ModEntry.Alpha6621.cs")
old_start = '''        string requestKey = $"{owner.Name}|{enabled}";
        bool sourceLiveBefore = IsNpcPelipperSourceLiveAlpha6619(owner);
        bool alreadyConverged = enabled ? sourceLiveBefore : !sourceLiveBefore;
        if (alreadyConverged)
        {
            PelipperNonConvergedSourceRequestsAlpha6623.Remove(requestKey);
            return true;
        }
'''
new_start = '''        string requestKey = $"{owner.Name}|{enabled}";
        string nativeOwnerKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
        bool sourceLiveBefore = IsNpcPelipperSourceLiveAlpha6619(owner);
        bool hasConfiguredEnabled = PelipperTown119NativeBridge.TryIsVillagerCompanionConfiguredEnabled(
            nativeOwnerKey,
            out bool configuredEnabled);
        bool alreadyConverged = enabled
            ? sourceLiveBefore && (!hasConfiguredEnabled || configuredEnabled)
            : !sourceLiveBefore && (!hasConfiguredEnabled || !configuredEnabled);
        if (alreadyConverged)
        {
            PelipperNonConvergedSourceRequestsAlpha6623.Remove(requestKey);
            return true;
        }
'''
alpha6621 = replace_once(alpha6621, old_start, new_start, "source convergence start")
alpha6621 = replace_once(
    alpha6621,
    "        bool routed = PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled(owner.Name, enabled, out string route);",
    "        bool routed = PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled(nativeOwnerKey, enabled, out string route);",
    "native source key",
)
old_restore = '''        bool nativeAvailable = PelipperTown119NativeBridge.HasVillagerLifecycle;
        bool restored = PelipperTown119NativeBridge.RestoreVillager(owner.Name, out string route);
'''
new_restore = '''        bool nativeAvailable = PelipperTown119NativeBridge.HasVillagerLifecycle;
        string nativeOwnerKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
        bool restored = PelipperTown119NativeBridge.RestoreVillager(nativeOwnerKey, out string route);
'''
alpha6621 = replace_once(alpha6621, old_restore, new_restore, "native restore key")
write("ModEntry.Alpha6621.cs", alpha6621)

# 6.7.12 accidentally placed its per-tick repair in a legacy handler which Alpha 6.6.26 later
# unsubscribes. Remove that dead duplicate; 6.7.13 owns a dedicated handler above.
alpha6615 = read("ModEntry.Alpha6615.cs")
old_update = '''    private void OnAlpha6615UpdateTicked(object? sender, UpdateTickedEventArgs e)
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
new_update = '''    private void OnAlpha6615UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !e.IsMultipleOf(15))
            return;

        DetectPlayerCompanionRecallAttemptsAlpha6615();
    }
'''
alpha6615 = replace_once(alpha6615, old_update, new_update, "remove dead 6.7.12 watchdog")
write("ModEntry.Alpha6615.cs", alpha6615)

# Register 6.7.13 from the active 6.7.x chain, with idempotence even if 6.7.10 was already set up.
alpha6710 = read("ModEntry.Alpha6710.cs")
old_reg = '''    private void EnsureAlpha6710Registered()
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
'''
new_reg = '''    private void EnsureAlpha6710Registered()
    {
        if (!Alpha6710Registered)
        {
            Alpha6710Registered = true;
            NpcRouteStateSafetyPatch.Apply(Monitor, ModManifest.UniqueID);
            Helper.ConsoleCommands.Add(
                "teamup_route_guard",
                "Show Alpha 6.7.10 NPC end-of-route crash guard status.",
                OnAlpha6710RouteGuardStatus);
        }

        EnsureAlpha6713Registered();
    }
'''
alpha6710 = replace_once(alpha6710, old_reg, new_reg, "6.7.13 registration chain")
write("ModEntry.Alpha6710.cs", alpha6710)

print("Alpha 6.7.13 source materialized.")
