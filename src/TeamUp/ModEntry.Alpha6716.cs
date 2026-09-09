using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

/// <summary>
/// Alpha 6.7.16 is intentionally read-only. It adds combat telemetry and a runtime compatibility
/// matrix so live issues can be diagnosed without changing Pelipper ownership, combat behavior,
/// recruitment intent, party state, or save progression.
/// </summary>
public sealed partial class ModEntry
{
    private bool Alpha6716Registered;

    private void EnsureAlpha6716Registered()
    {
        if (Alpha6716Registered)
            return;

        Alpha6716Registered = true;
        Helper.ConsoleCommands.Add(
            "teamup_combat_report",
            "Export read-only Team Up combat targeting/cooldown telemetry for all online Farmers.",
            OnAlpha6716CombatReportCommand);
        Helper.ConsoleCommands.Add(
            "teamup_compat_audit",
            "Export read-only runtime NPC/profile/rank/kit compatibility coverage.",
            OnAlpha6716CompatibilityAuditCommand);
        Helper.ConsoleCommands.Add(
            "teamup_diag_all",
            "Export preflight + combat telemetry + runtime compatibility in one diagnostic bundle.",
            OnAlpha6716DiagnosticBundleCommand);

        Monitor.Log("Team Up Alpha 6.7.16 read-only combat telemetry + compatibility audit enabled.", LogLevel.Info);
    }

    private void OnAlpha6716CombatReportCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_combat_report requires a loaded save.", LogLevel.Info);
            return;
        }

        string report = BuildCombatTelemetryAlpha6716(out int warnings);
        string? path = ExportDiagnosticAlpha6716("TeamUp_Combat_latest.txt", report);
        Monitor.Log(report, warnings > 0 ? LogLevel.Warn : LogLevel.Info);
        if (path is not null)
            Monitor.Log($"Team Up combat report exported: {path}", LogLevel.Info);
    }

    private void OnAlpha6716CompatibilityAuditCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_compat_audit requires a loaded save.", LogLevel.Info);
            return;
        }

        string report = BuildCompatibilityAuditAlpha6716(out int warnings);
        string? path = ExportDiagnosticAlpha6716("TeamUp_Compatibility_latest.txt", report);
        Monitor.Log(report, warnings > 0 ? LogLevel.Warn : LogLevel.Info);
        if (path is not null)
            Monitor.Log($"Team Up compatibility audit exported: {path}", LogLevel.Info);
    }

    private void OnAlpha6716DiagnosticBundleCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log("teamup_diag_all requires a loaded save.", LogLevel.Info);
            return;
        }

        string preflight = BuildAlpha6715PreflightReport(out int preflightWarnings);
        string combat = BuildCombatTelemetryAlpha6716(out int combatWarnings);
        string compat = BuildCompatibilityAuditAlpha6716(out int compatWarnings);
        int totalWarnings = preflightWarnings + combatWarnings + compatWarnings;

        string report = string.Join(
            Environment.NewLine + Environment.NewLine + "============================================================" + Environment.NewLine + Environment.NewLine,
            preflight,
            combat,
            compat,
            $"TEAM UP DIAGNOSTIC BUNDLE RESULT: {(totalWarnings == 0 ? "PASS" : "WARN")} ({totalWarnings} warning(s))");

        string? path = ExportDiagnosticAlpha6716("TeamUp_Diagnostic_bundle_latest.txt", report);
        Monitor.Log(
            $"Team Up diagnostic bundle: {(totalWarnings == 0 ? "PASS" : "WARN")} ({totalWarnings} warning(s)).",
            totalWarnings > 0 ? LogLevel.Warn : LogLevel.Info);
        if (path is not null)
            Monitor.Log($"Team Up diagnostic bundle exported: {path}", LogLevel.Info);
    }

    private string BuildCombatTelemetryAlpha6716(out int warningCount)
    {
        var warnings = new List<string>();
        var sb = new StringBuilder();
        sb.AppendLine("TEAM UP COMBAT TELEMETRY");
        sb.AppendLine("========================");
        sb.AppendLine($"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"Version: {ModManifest.Version}");
        sb.AppendLine($"Strategy: {Combat.CurrentStrategy}");
        sb.AppendLine($"Location: {Game1.currentLocation?.Name ?? "none"}");
        sb.AppendLine();

        List<Farmer> farmers = Game1.getOnlineFarmers().ToList();
        if (farmers.Count == 0)
            farmers.Add(Game1.player);

        foreach (Farmer farmer in farmers)
        {
            CombatService? service = farmer.UniqueMultiplayerID == Game1.player.UniqueMultiplayerID
                ? Combat
                : RemoteCombatServices.TryGetValue(farmer.UniqueMultiplayerID, out CombatService? remote)
                    ? remote
                    : null;

            List<PartyMemberData> members = Party.Members
                .Where(member => member.RecruiterId == farmer.UniqueMultiplayerID)
                .Where(member => member.State is PartyMemberState.Following or PartyMemberState.Waiting)
                .OrderBy(member => member.CharacterName)
                .ToList();

            sb.AppendLine($"[FARMER] {farmer.Name} ({farmer.UniqueMultiplayerID}) members={members.Count}");
            if (service is null)
            {
                sb.AppendLine("combatService=not-created");
                if (members.Any(member => member.State == PartyMemberState.Following))
                    warnings.Add($"{farmer.Name}: following NPCs exist but remote combat service is not created.");
                sb.AppendLine();
                continue;
            }

            Dictionary<string, Monster>? targets = ReadCombatFieldAlpha6716<Dictionary<string, Monster>>(service, "_targets");
            Dictionary<string, int>? attackCooldowns = ReadCombatFieldAlpha6716<Dictionary<string, int>>(service, "_attackCooldowns");
            Dictionary<string, int>? signatureCooldowns = ReadCombatFieldAlpha6716<Dictionary<string, int>>(service, "_signatureCooldowns");
            Dictionary<string, int>? targetLocks = ReadCombatFieldAlpha6716<Dictionary<string, int>>(service, "_targetLockTicks");
            Dictionary<string, int>? pathRetries = ReadCombatFieldAlpha6716<Dictionary<string, int>>(service, "_combatPathRetryTicks");

            sb.AppendLine($"strategy={service.CurrentStrategy}; targetRows={targets?.Count ?? -1}");
            if (members.Count == 0)
            {
                sb.AppendLine("none");
                sb.AppendLine();
                continue;
            }

            foreach (PartyMemberData member in members)
            {
                NPC? npc = Game1.getCharacterFromName(member.CharacterName);
                NpcCombatProfile? profile = NpcProfileCatalog.Get(member.CharacterName);
                PartyRole resolvedRole = member.Role != PartyRole.Unassigned
                    ? member.Role
                    : profile?.PrimaryRole ?? PartyRole.Unassigned;
                string rank = CombatRankCatalog.Get(member.CharacterName, profile).ToCompactLabel();
                int attackCd = GetTelemetryValueAlpha6716(attackCooldowns, member.CharacterName);
                int signatureCd = GetTelemetryValueAlpha6716(signatureCooldowns, member.CharacterName);
                int targetLock = GetTelemetryValueAlpha6716(targetLocks, member.CharacterName);
                int pathRetry = GetTelemetryValueAlpha6716(pathRetries, member.CharacterName);

                Monster? target = null;
                targets?.TryGetValue(member.CharacterName, out target);

                if (npc is null)
                {
                    sb.AppendLine($"- {member.CharacterName}: runtime=missing; state={member.State}; role={resolvedRole}; {rank}; hp={member.CurrentHealth}; target={target?.Name ?? "none"}");
                    if (member.State == PartyMemberState.Following)
                        warnings.Add($"{member.CharacterName}: Following but NPC runtime is missing.");
                    continue;
                }

                string targetText = "none";
                if (target is not null)
                {
                    float distance = GetRectangleGapTilesAlpha6716(npc.GetBoundingBox(), target.GetBoundingBox());
                    float attackRange = ReadAttackRangeAlpha6716(resolvedRole);
                    bool protectedCapture = PelipperCaptureSafetyService.IsProtected(target);
                    targetText = $"{target.Name} hp={target.Health}/{target.MaxHealth} dist={distance:0.00} range={(attackRange >= 0 ? attackRange.ToString("0.00") : "?")} captureProtected={protectedCapture}";

                    if (target.Health <= 0)
                        warnings.Add($"{member.CharacterName}: stale dead target {target.Name} remains assigned.");
                    if (protectedCapture)
                        warnings.Add($"{member.CharacterName}: capture-protected target {target.Name} remains in CombatService target table.");
                }

                sb.AppendLine(
                    $"- {member.CharacterName}: state={member.State}; role={resolvedRole}; engagement={member.Engagement}; {rank}; " +
                    $"hp={member.CurrentHealth}; downed={member.IsDowned}; withdrawn={member.IsWithdrawn}; " +
                    $"attackCD={attackCd}; signatureCD={signatureCd}; lock={targetLock}; pathRetry={pathRetry}; target={targetText}");
            }

            sb.AppendLine();
        }

        if (Game1.currentLocation is not null)
        {
            List<Monster> alive = Game1.currentLocation.characters.OfType<Monster>()
                .Where(monster => monster.Health > 0)
                .ToList();
            int majors = alive.Count(monster => monster.MaxHealth >= 300);
            int protectedTargets = alive.Count(PelipperCaptureSafetyService.IsProtected);
            sb.AppendLine($"[CURRENT LOCATION THREATS] alive={alive.Count}; major(MaxHP>=300)={majors}; captureProtected={protectedTargets}");
            foreach (Monster monster in alive.OrderByDescending(monster => monster.MaxHealth).Take(20))
            {
                bool pelipper = PelipperTownCompatibilityService.LooksLikePelipperActor(monster);
                bool protectedCapture = PelipperCaptureSafetyService.IsProtected(monster);
                sb.AppendLine($"- {monster.Name}: hp={monster.Health}/{monster.MaxHealth}; type={monster.GetType().Name}; pelipper={pelipper}; captureProtected={protectedCapture}");
            }
        }

        warningCount = warnings.Count;
        sb.AppendLine();
        sb.AppendLine($"[RESULT] {(warningCount == 0 ? "PASS" : "WARN")} ({warningCount} warning(s))");
        foreach (string warning in warnings)
            sb.AppendLine($"! {warning}");
        return sb.ToString().TrimEnd();
    }

    private string BuildCompatibilityAuditAlpha6716(out int warningCount)
    {
        var warnings = new List<string>();
        var sb = new StringBuilder();
        CombatRosterIntegrityReport staticAudit = CombatRosterIntegrityService.AuditKnownCatalog();

        sb.AppendLine("TEAM UP RUNTIME COMPATIBILITY AUDIT");
        sb.AppendLine("===================================");
        sb.AppendLine($"Timestamp: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"Version: {ModManifest.Version}");
        sb.AppendLine($"StaticProfiles: {staticAudit.ProfileCount}; staticAudit={(staticAudit.Passed ? "PASS" : "FAIL")}");
        foreach (string issue in staticAudit.Issues)
            warnings.Add($"static: {issue}");
        sb.AppendLine();

        List<NPC> runtime = Game1.locations
            .SelectMany(location => location.characters.OfType<NPC>())
            .Where(npc => npc is not Monster)
            .Where(npc => !string.IsNullOrWhiteSpace(npc.Name))
            .Where(npc => !PelipperTownCompatibilityService.LooksLikePelipperActor(npc))
            .GroupBy(npc => npc.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(npc => npc.Name)
            .ToList();

        int profiled = 0;
        int mainPartyCandidates = 0;
        int missingCandidateProfiles = 0;
        sb.AppendLine($"[RUNTIME NPC MATRIX] uniqueNpc={runtime.Count}");
        foreach (NPC npc in runtime)
        {
            NpcCombatProfile? profile = NpcProfileCatalog.Get(npc.Name);
            bool explicitCustom = CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc);
            bool mainPartyClass = CompanionClassificationService.CanRecruitToMainParty(npc, Config.SpecialCompanionNpcNames);
            bool candidate = explicitCustom || mainPartyClass;
            bool specialName = CompanionClassificationService.IsSpecialName(npc.Name, Config.SpecialCompanionNpcNames);
            bool kit = CombatKitCoverageService.HasCombatKit(npc.Name);
            CombatRankInfo rank = CombatRankCatalog.Get(npc.Name, profile);

            if (profile is not null)
                profiled++;
            if (candidate)
                mainPartyCandidates++;
            if (candidate && profile is null)
            {
                missingCandidateProfiles++;
                warnings.Add($"runtime recruit candidate {npc.Name} has no combat profile.");
            }
            if (candidate && profile is not null && !kit)
                warnings.Add($"runtime recruit candidate {npc.Name} has a profile but no combat kit coverage.");

            sb.AppendLine(
                $"- {npc.Name}: display={npc.displayName}; type={npc.GetType().FullName}; assembly={npc.GetType().Assembly.GetName().Name}; " +
                $"candidate={candidate}; special={specialName}; explicitCustom={explicitCustom}; profile={(profile is null ? "MISSING" : "OK")}; " +
                $"source={profile?.SourceLabel ?? "runtime-only"}; primary={profile?.PrimaryRole.ToString() ?? "?"}; secondary={profile?.SecondaryRole.ToString() ?? "?"}; " +
                $"rank={rank.ToCompactLabel()}; kit={kit}");
        }

        sb.AppendLine();
        sb.AppendLine($"[COVERAGE] profiledRuntime={profiled}/{runtime.Count}; mainPartyCandidates={mainPartyCandidates}; missingCandidateProfiles={missingCandidateProfiles}");
        IReadOnlyList<NpcCombatProfile> availableProfiles = NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry);
        sb.AppendLine($"AvailableProfileCatalogRows={availableProfiles.Count}/{NpcProfileCatalog.All.Count}");
        foreach (IGrouping<string, NpcCombatProfile> sourceGroup in availableProfiles.GroupBy(profile => profile.SourceLabel).OrderBy(group => group.Key))
            sb.AppendLine($"- source {sourceGroup.Key}: {sourceGroup.Count()} profile(s)");

        sb.AppendLine();
        sb.AppendLine("[CONTENT SYSTEMS]");
        BanterCatalogAuditReport banter = BanterContentCatalog.AuditKnownRoster();
        ContextBanterAuditReport context = ContextBanterCatalog.AuditKnownRoster();
        sb.AppendLine($"BanterCatalog: {(banter.Passed ? "PASS" : "FAIL")} pairScripts={banter.PairScripts} shippingScripts={banter.ShippingScripts}");
        sb.AppendLine($"ContextBanter: {(context.Passed ? "PASS" : "FAIL")} scripts={context.Scripts}");
        sb.AppendLine($"Chemistry: authoredPairs={PartyChemistryCatalog.PairCount}; variantPairs={ChemistryVariantCatalog.PairVariantCount}; bilingualVariantLines={ChemistryVariantCatalog.LineCount}");
        foreach (string issue in banter.Issues)
            warnings.Add($"banter: {issue}");
        foreach (string issue in context.Issues)
            warnings.Add($"context banter: {issue}");

        warningCount = warnings.Count;
        sb.AppendLine();
        sb.AppendLine($"[RESULT] {(warningCount == 0 ? "PASS" : "WARN")} ({warningCount} warning(s))");
        foreach (string warning in warnings)
            sb.AppendLine($"! {warning}");
        return sb.ToString().TrimEnd();
    }

    private static T? ReadCombatFieldAlpha6716<T>(CombatService service, string fieldName) where T : class
    {
        try
        {
            FieldInfo? field = typeof(CombatService).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(service) as T;
        }
        catch
        {
            return null;
        }
    }

    private static int GetTelemetryValueAlpha6716(Dictionary<string, int>? values, string key)
        => values is not null && values.TryGetValue(key, out int value) ? value : 0;

    private static float ReadAttackRangeAlpha6716(PartyRole role)
    {
        try
        {
            MethodInfo? method = typeof(CombatService).GetMethod("GetAttackRange", BindingFlags.Static | BindingFlags.NonPublic);
            return method?.Invoke(null, new object[] { role }) is float value ? value : -1f;
        }
        catch
        {
            return -1f;
        }
    }

    private static float GetRectangleGapTilesAlpha6716(Rectangle a, Rectangle b)
    {
        int dx = a.Right < b.Left ? b.Left - a.Right : b.Right < a.Left ? a.Left - b.Right : 0;
        int dy = a.Bottom < b.Top ? b.Top - a.Bottom : b.Bottom < a.Top ? a.Top - b.Bottom : 0;
        return MathF.Sqrt(dx * dx + dy * dy) / 64f;
    }

    private string? ExportDiagnosticAlpha6716(string fileName, string report)
    {
        try
        {
            string directory = Path.Combine(Helper.DirectoryPath, "diagnostics");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            File.WriteAllText(path, report + Environment.NewLine, Encoding.UTF8);
            return Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Couldn't export {fileName}: {ex.Message}", LogLevel.Warn);
            return null;
        }
    }
}
