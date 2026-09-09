from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION = "0.2.0-alpha.6.7.14"


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
    "<Version>0.2.0-alpha.6.7.13</Version>",
    f"<Version>{VERSION}</Version>",
    "project version",
)
write("TeamUp.csproj", project)

# -----------------------------------------------------------------------------
# CAPTURE CEASEFIRE: retain durable wild-proxy identity, but remove Team Up's
# offensive opt-in marker once the active Pelipper capture floor is reached.
# The hard damage clamps/watchdog continue to classify the proxy through the
# separate WildCombatProxy marker.
# -----------------------------------------------------------------------------
alpha6613 = read("ModEntry.Alpha6613.cs")
old_proxy = '''            // The same unowned Pelipper Monster proxy that Team Up opts into combat is the exact
            // entity whose HP must obey Pelipper's capture/mercy floor.
            monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
            monster.modData[PelipperTownCompatibilityService.WildCombatProxyKey] = "true";
'''
new_proxy = '''            // Keep durable wild-proxy identity even during capture ceasefire. The offensive
            // CombatTarget marker is separate and may be removed at the floor without disabling
            // the hard capture clamp/watchdog.
            monster.modData[PelipperTownCompatibilityService.WildCombatProxyKey] = "true";
            if (PelipperCaptureSafetyService.IsProtected(monster))
            {
                monster.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);
                continue;
            }

            monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";
'''
alpha6613 = replace_once(alpha6613, old_proxy, new_proxy, "capture ceasefire marker lifecycle")
write("ModEntry.Alpha6613.cs", alpha6613)

# -----------------------------------------------------------------------------
# NPC-ONLY SOURCE LOCK: a player's explicit NPC-only choice must keep Pelipper's
# configured villager companion disabled even when no Team Up linked-companion
# row exists yet. This prevents a dormant configured Pokemon from materializing
# late and bypassing the recruitment choice.
# -----------------------------------------------------------------------------
alpha6714 = r'''using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const int NpcOnlySourceLockPulseTicksAlpha6714 = 10;
    private bool Alpha6714Registered;

    private void EnsureAlpha6714Registered()
    {
        if (Alpha6714Registered)
            return;

        Alpha6714Registered = true;
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6714UpdateTicked;
        Helper.ConsoleCommands.Add(
            "teamup_npc_only_lock",
            "Force/check Team Up NPC-only Pelipper source locks.",
            OnAlpha6714NpcOnlyLockCommand);

        Monitor.Log("Team Up Alpha 6.7.14 NPC-only source lock + capture ceasefire enabled.", LogLevel.Info);
    }

    private void OnAlpha6714UpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsMainPlayer
            || !Context.IsWorldReady
            || !e.IsMultipleOf(NpcOnlySourceLockPulseTicksAlpha6714))
        {
            return;
        }

        EnforceNpcOnlySourceLocksAlpha6714(logChanges: false);
    }

    /// <summary>
    /// Enforce the durable NPC-only choice at Pelipper's source layer even when Team Up has no
    /// linked companion row yet. A dormant configured Pokemon therefore cannot appear later simply
    /// because Pelipper refreshes assignments after recruitment/map changes.
    /// </summary>
    private int EnforceNpcOnlySourceLocksAlpha6714(bool logChanges)
    {
        ConfigurePelipperApiBridgeAlpha6619();
        if (!PelipperTown119NativeBridge.HasVillagerLifecycle)
            return 0;

        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        int enforced = 0;

        foreach (PartyMemberData member in Party.Members.Where(member =>
            online.Contains(member.RecruiterId)
            && member.State is PartyMemberState.Following or PartyMemberState.Waiting))
        {
            NPC? owner = Game1.getCharacterFromName(member.CharacterName);
            if (owner is null || !PelipperTownCompatibilityService.IsOwnerOptedOut(owner))
                continue;

            string configKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
            bool hasConfiguredEnabled = PelipperTown119NativeBridge.TryIsVillagerCompanionConfiguredEnabled(
                configKey,
                out bool configuredEnabled);
            bool sourceLive = IsNpcPelipperSourceLiveAlpha6619(owner);

            // If the source bit is already false and no actor is live, the lock is converged.
            if (hasConfiguredEnabled && !configuredEnabled && !sourceLive)
                continue;

            bool converged = TrySetPelipperNpcSourceEnabledAlpha6621(
                owner,
                enabled: false,
                reason: "Alpha 6.7.14 durable NPC-only source lock");
            if (!converged)
                continue;

            enforced++;
            if (logChanges)
            {
                Monitor.Log(
                    $"NPC-only lock enforced: owner={owner.Name}, configKey={configKey}, sourceLiveBefore={sourceLive}, configuredEnabledBefore={(hasConfiguredEnabled ? configuredEnabled.ToString() : "unknown")}.",
                    LogLevel.Info);
            }
        }

        return enforced;
    }

    private void OnAlpha6714NpcOnlyLockCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_npc_only_lock requires a loaded save.", LogLevel.Info);
            return;
        }

        int enforced = Context.IsMainPlayer
            ? EnforceNpcOnlySourceLocksAlpha6714(logChanges: true)
            : 0;

        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        List<string> rows = new();
        foreach (PartyMemberData member in Party.Members.Where(member =>
            online.Contains(member.RecruiterId)
            && member.State is PartyMemberState.Following or PartyMemberState.Waiting))
        {
            NPC? owner = Game1.getCharacterFromName(member.CharacterName);
            if (owner is null || !PelipperTownCompatibilityService.IsOwnerOptedOut(owner))
                continue;

            string configKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
            bool hasEnabled = PelipperTown119NativeBridge.TryIsVillagerCompanionConfiguredEnabled(configKey, out bool enabled);
            bool sourceLive = IsNpcPelipperSourceLiveAlpha6619(owner);
            CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
            rows.Add($"{owner.Name}:key={configKey}:enabled={(hasEnabled ? enabled.ToString() : "unknown")}:sourceLive={sourceLive}:linked={(linked?.DisplayName ?? "none")}");
        }

        Monitor.Log(
            $"NPC-only source lock: enforcedNow={enforced}, rows=[{string.Join(" | ", rows)}]",
            LogLevel.Info);
    }
}
'''
write("ModEntry.Alpha6714.cs", alpha6714)

# Registration chain.
alpha6710 = read("ModEntry.Alpha6710.cs")
old_chain = '''        EnsureAlpha6713Registered();
'''
new_chain = '''        EnsureAlpha6713Registered();
        EnsureAlpha6714Registered();
'''
alpha6710 = replace_once(alpha6710, old_chain, new_chain, "6.7.14 registration chain")
write("ModEntry.Alpha6710.cs", alpha6710)

print("Alpha 6.7.14 source materialized.")
