using System.Text;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

/// <summary>
/// Alpha 6.7.15 is deliberately diagnostics-only. It gives live testers one command which captures
/// the party, Pelipper source truth, capture-floor proxies, route guard and obvious invariant
/// violations without changing party/source/combat state.
/// </summary>
public sealed partial class ModEntry
{
    private bool Alpha6715Registered;

    private void EnsureAlpha6715Registered()
    {
        if (Alpha6715Registered)
            return;

        Alpha6715Registered = true;
        Helper.ConsoleCommands.Add(
            "teamup_preflight",
            "Create a read-only Team Up live-test snapshot and export it to the mod diagnostics folder.",
            OnAlpha6715PreflightCommand);

        Monitor.Log("Team Up Alpha 6.7.15 read-only preflight diagnostics enabled.", LogLevel.Info);
    }

    private void OnAlpha6715PreflightCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_preflight requires a loaded save.", LogLevel.Info);
            return;
        }

        string report = BuildAlpha6715PreflightReport(out int warningCount);
        string? exportedPath = TryExportAlpha6715Preflight(report);

        Monitor.Log(report, warningCount > 0 ? LogLevel.Warn : LogLevel.Info);
        if (!string.IsNullOrWhiteSpace(exportedPath))
            Monitor.Log($"Team Up preflight exported: {exportedPath}", LogLevel.Info);
    }

    private string BuildAlpha6715PreflightReport(out int warningCount)
    {
        warningCount = 0;
        ConfigurePelipperApiBridgeAlpha6619();

        var warnings = new List<string>();
        var sb = new StringBuilder();
        sb.AppendLine("TEAM UP LIVE PREFLIGHT");
        sb.AppendLine("======================");
        sb.AppendLine($"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"Version: {ModManifest.Version}");
        sb.AppendLine($"Location: {Game1.currentLocation?.Name ?? "none"}");
        sb.AppendLine($"Host: {Context.IsMainPlayer}");
        sb.AppendLine();

        bool routeGuard = NpcRouteStateSafetyPatch.IsApplied;
        sb.AppendLine("[CORE]");
        sb.AppendLine($"RouteGuard: {routeGuard}");
        sb.AppendLine($"CaptureSafety: enabled={PelipperCaptureSafetyService.CurrentEnabled}, threshold={PelipperCaptureSafetyService.CurrentThreshold:P0}");
        sb.AppendLine($"PelipperNative: {PelipperTown119NativeBridge.Status}");
        sb.AppendLine($"Pelipper119: villagerLifecycle={PelipperTown119NativeBridge.HasVillagerLifecycle}, playerLifecycle={PelipperTown119NativeBridge.HasPlayerLifecycle}, exactRuntimeMap={PelipperTown119NativeBridge.HasExactVillagerRuntimeMap}");
        if (!routeGuard)
            warnings.Add("NPC route guard is not applied.");
        sb.AppendLine();

        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        List<PartyMemberData> activeMembers = Party.Members
            .Where(member => online.Contains(member.RecruiterId)
                && member.State is PartyMemberState.Following or PartyMemberState.Waiting)
            .ToList();
        int peopleCap = Math.Max(1, Config.MaxPartyMembers);
        int peopleEffective = online.Count + activeMembers.Count;

        sb.AppendLine("[PARTY]");
        sb.AppendLine($"People: farmers={online.Count}, activeNPCs={activeMembers.Count}, effective={peopleEffective}/{peopleCap}");
        if (peopleEffective > peopleCap)
            warnings.Add($"People cap overflow: {peopleEffective}/{peopleCap}.");

        int companionCap = GetCompanionCapAlpha6618();
        int companionReserved = Party.GetActiveCombatCompanionCount();
        int companionEffective = GetEffectiveCombatCompanionCountAlpha6618();
        sb.AppendLine($"CombatCompanions: reserved={companionReserved}/{companionCap}, effective={companionEffective}/{companionCap}, records={Party.CompanionUnits.Count}");
        if (companionEffective > companionCap)
            warnings.Add($"Physical/effective companion overflow: {companionEffective}/{companionCap}.");
        sb.AppendLine();

        sb.AppendLine("[ACTIVE NPCS]");
        if (activeMembers.Count == 0)
        {
            sb.AppendLine("none");
        }
        else
        {
            foreach (PartyMemberData member in activeMembers)
            {
                NPC? owner = Game1.getCharacterFromName(member.CharacterName);
                string rank = CombatRankCatalog.Get(member.CharacterName).ToCompactLabel();
                if (owner is null)
                {
                    sb.AppendLine($"- {member.CharacterName}: state={member.State}, {rank}, npcRuntime=missing");
                    continue;
                }

                bool optedOut = PelipperTownCompatibilityService.IsOwnerOptedOut(owner);
                string configKey = ResolvePelipperVillagerConfigKeyAlpha6713(owner);
                bool hasEnabled = PelipperTown119NativeBridge.TryIsVillagerCompanionConfiguredEnabled(configKey, out bool configuredEnabled);
                bool sourceLive = IsNpcPelipperSourceLiveAlpha6619(owner);
                CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);

                sb.AppendLine(
                    $"- {owner.Name}: state={member.State}, {rank}, npcOnly={optedOut}, pelipperKey={configKey}, " +
                    $"configuredEnabled={(hasEnabled ? configuredEnabled.ToString() : "unknown")}, sourceLive={sourceLive}, linked={linked?.DisplayName ?? "none"}");

                if (optedOut && (sourceLive || (hasEnabled && configuredEnabled)))
                {
                    warnings.Add(
                        $"NPC-only source lock not converged for {owner.Name}: configuredEnabled={(hasEnabled ? configuredEnabled.ToString() : "unknown")}, sourceLive={sourceLive}.");
                }
            }
        }
        sb.AppendLine();

        sb.AppendLine("[PELIPPER COMBAT PROXIES]");
        int proxyCount = 0;
        if (Game1.currentLocation is not null)
        {
            foreach (Monster monster in Game1.currentLocation.characters.OfType<Monster>())
            {
                if (!PelipperTownCompatibilityService.LooksLikePelipperActor(monster))
                    continue;

                proxyCount++;
                bool target = monster.modData.TryGetValue(PelipperTownCompatibilityService.CombatTargetOptInKey, out string? targetRaw)
                    && targetRaw.Equals("true", StringComparison.OrdinalIgnoreCase);
                bool proxy = monster.modData.TryGetValue(PelipperTownCompatibilityService.WildCombatProxyKey, out string? proxyRaw)
                    && proxyRaw.Equals("true", StringComparison.OrdinalIgnoreCase);
                bool wild = PelipperTownCompatibilityService.IsWildCombatActor(monster);
                bool limited = PelipperCaptureSafetyService.TryGetDamageBudget(monster, out int budget);
                bool protectedAtFloor = limited && budget <= 0;

                sb.AppendLine(
                    $"- {monster.Name}: hp={monster.Health}/{monster.MaxHealth}, type={monster.GetType().FullName}, " +
                    $"target={target}, proxy={proxy}, wild={wild}, limited={limited}, budget={(limited ? budget : -1)}, protected={protectedAtFloor}");

                if (protectedAtFloor && target)
                {
                    warnings.Add(
                        $"Capture ceasefire not converged for {monster.Name}: budget=0 but CombatTarget is still true.");
                }
            }
        }
        if (proxyCount == 0)
            sb.AppendLine("none in current location");
        sb.AppendLine();

        warningCount = warnings.Count;
        sb.AppendLine($"[RESULT] {(warningCount == 0 ? "PASS" : "WARN")} ({warningCount} warning(s))");
        foreach (string warning in warnings)
            sb.AppendLine($"! {warning}");

        return sb.ToString().TrimEnd();
    }

    private string? TryExportAlpha6715Preflight(string report)
    {
        try
        {
            string directory = Path.Combine(Helper.DirectoryPath, "diagnostics");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "TeamUp_Diagnostic_latest.txt");
            File.WriteAllText(path, report + Environment.NewLine, Encoding.UTF8);
            return Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Couldn't export Team Up preflight diagnostic: {ex.Message}", LogLevel.Warn);
            return null;
        }
    }
}
