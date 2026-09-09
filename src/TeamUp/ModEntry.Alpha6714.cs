using Ronvotri.TeamUp.Core;
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

        EnsureAlpha6715Registered();
        EnsureAlpha6716Registered();
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
