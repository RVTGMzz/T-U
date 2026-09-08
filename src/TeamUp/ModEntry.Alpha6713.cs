using Ronvotri.TeamUp.Core;
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
