using Ronvotri.TeamUp.Combat;
using Ronvotri.TeamUp.Core;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private const string Alpha674440Branch = "v0.2-alpha6-7-44-41-capture-guard-scope-fix";
    private const string Alpha674440Version = "0.2.0-alpha.6.7.44.41";

    private void OnAlpha674440PreflightCommand(string command, string[] args)
    {
        bool buildPass = ModManifest.Version.ToString().Equals(Alpha674440Version, StringComparison.Ordinal);
        string maleToken = Alpha674418NativeMutationMinionService.ResolvePelipperSpawnToken("Nidoran♂");
        string femaleToken = Alpha674418NativeMutationMinionService.ResolvePelipperSpawnToken("Nidoran♀");
        bool tokenMapPass = maleToken == "nidoran-m" && femaleToken == "nidoran-f";

        if (!Context.IsWorldReady || Game1.currentLocation is null)
        {
            Monitor.Log(
                $"Team Up preflight: PENDING | build={(buildPass ? "PASS" : "FAIL")} | "
                + $"nidoranTokenMap={(tokenMapPass ? "PASS" : "FAIL")} | world=PENDING | "
                + $"version={ModManifest.Version} | branch={Alpha674440Branch}",
                buildPass && tokenMapPass ? LogLevel.Info : LogLevel.Warn);
            return;
        }

        Alpha674440MutationAudit mutation = BuildAlpha674440MutationAudit();
        Alpha674438LowerWorkingsRuntimeGateV2Service.PreflightSnapshot lower =
            LowerWorkingsRuntimeGateV2Alpha674438.GetPreflightSnapshot();

        Monster? pelipperMutant = Game1.currentLocation.characters
            .OfType<Monster>()
            .FirstOrDefault(monster =>
                MonsterMutationService.IsMutant(monster)
                && PelipperTownCompatibilityService.IsWildCombatActor(monster));

        bool eliteObserved = pelipperMutant is not null;
        bool elitePass = false;
        string eliteDetail = "not-observed";
        bool nidoranObserved = false;
        bool nidoranLivePass = false;
        string nidoranDetail = "not-observed";

        if (pelipperMutant is not null
            && PelipperWildEncounterIdentityService.TryResolve(pelipperMutant, out PelipperWildEncounterIdentity leaderIdentity))
        {
            int phaseTotal = ReadAlpha674440IntMarker(pelipperMutant, Alpha67448PelipperSourceMutationService.PhaseTotalMarker);
            int phaseCurrent = ReadAlpha674440IntMarker(pelipperMutant, Alpha67448PelipperSourceMutationService.PhaseCurrentMarker);
            int lootMultiplier = ReadAlpha674440IntMarker(pelipperMutant, Alpha674414PelipperMutantRewardService.LootMultiplierMarker);
            bool noCapture = pelipperMutant.modData.TryGetValue(
                    Alpha67448PelipperSourceMutationService.NoCaptureMarker,
                    out string? noCaptureRaw)
                && noCaptureRaw.Equals("true", StringComparison.OrdinalIgnoreCase);

            elitePass = phaseTotal == 3
                && phaseCurrent is >= 1 and <= 3
                && lootMultiplier == Alpha674414PelipperMutantRewardService.MutantLootMultiplier
                && noCapture;
            eliteDetail = $"species={leaderIdentity.DisplayName},phase={phaseCurrent}/{phaseTotal},lootX={lootMultiplier},noCapture={noCapture}";

            string leaderSpecies = NormalizeAlpha674440Species(leaderIdentity.DisplayName);
            nidoranObserved = leaderSpecies is "nidoranmale" or "nidoranfemale";
            if (nidoranObserved)
            {
                List<string> followerSpecies = new();
                foreach (Monster minion in Game1.currentLocation.characters.OfType<Monster>())
                {
                    if (!MonsterMutationService.IsMutationMinion(minion))
                        continue;
                    if (!minion.modData.TryGetValue(Alpha674418NativeMutationMinionService.NativeProviderMarker, out string? provider)
                        || !provider.Equals("pelipper-native", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (PelipperWildEncounterIdentityService.TryResolve(minion, out PelipperWildEncounterIdentity minionIdentity))
                        followerSpecies.Add(NormalizeAlpha674440Species(minionIdentity.DisplayName));
                }

                nidoranLivePass = followerSpecies.Count > 0
                    && followerSpecies.All(species => species.Equals(leaderSpecies, StringComparison.Ordinal));
                nidoranDetail = $"leader={leaderIdentity.DisplayName},followers={followerSpecies.Count},exact={nidoranLivePass}";
            }
        }

        bool fail = !buildPass
            || !tokenMapPass
            || !lower.MapValid
            || (mutation.Observed && !mutation.Pass)
            || (eliteObserved && !elitePass)
            || (nidoranObserved && !nidoranLivePass)
            || (lower.RouteObserved && !lower.RoutePass);

        List<string> pending = new();
        if (!mutation.Observed)
            pending.Add("mutation-wave");
        if (!eliteObserved)
            pending.Add("elite-markers");
        if (!nidoranObserved)
            pending.Add("nidoran-live");
        if (!lower.RouteObserved)
            pending.Add("lower-route");

        string status = fail ? "FAIL" : pending.Count == 0 ? "PASS" : "PENDING";

        Monitor.Log(
            $"Team Up preflight: {status} | build={(buildPass ? "PASS" : "FAIL")} | "
            + $"tokenMap={(tokenMapPass ? "PASS" : "FAIL")}({maleToken}/{femaleToken}) | "
            + $"mutation={(mutation.Observed ? (mutation.Pass ? "PASS" : "FAIL") : "PENDING")} | "
            + $"elite={(eliteObserved ? (elitePass ? "PASS" : "FAIL") : "PENDING")} | "
            + $"nidoranLive={(nidoranObserved ? (nidoranLivePass ? "PASS" : "FAIL") : "PENDING")} | "
            + $"lowerMap={(lower.MapValid ? "PASS" : "FAIL")} | "
            + $"lowerRoute={(lower.RouteObserved ? (lower.RoutePass ? "PASS" : "FAIL") : "PENDING")} | "
            + $"pending={(pending.Count == 0 ? "none" : string.Join(",", pending))}",
            fail ? LogLevel.Warn : LogLevel.Info);

        Monitor.Log(
            $"[PreflightMutation] {mutation.Line}",
            mutation.Observed && !mutation.Pass ? LogLevel.Warn : LogLevel.Info);
        Monitor.Log($"[PreflightElite] {eliteDetail}", eliteObserved && !elitePass ? LogLevel.Warn : LogLevel.Info);
        Monitor.Log($"[PreflightNidoran] {nidoranDetail}", nidoranObserved && !nidoranLivePass ? LogLevel.Warn : LogLevel.Info);
        Monitor.Log(
            $"[PreflightLower] mapValid={lower.MapValid} state={lower.State} routeObserved={lower.RouteObserved} routePass={lower.RoutePass} "
            + $"entries={lower.EntryPasses}/{lower.EntryObservations} entryMismatch={lower.EntryMismatches} "
            + $"returns={lower.ReturnPasses}/{lower.ReturnObservations} returnMismatch={lower.ReturnMismatches} "
            + $"mapFailures={lower.MapFailures} errors={lower.Errors} last={lower.Last}",
            lower.MapValid && (!lower.RouteObserved || lower.RoutePass) ? LogLevel.Info : LogLevel.Warn);
    }

    private Alpha674440MutationAudit BuildAlpha674440MutationAudit()
    {
        GameLocation location = Game1.currentLocation;
        List<Monster> minions = location.characters
            .OfType<Monster>()
            .Where(MonsterMutationService.IsMutationMinion)
            .ToList();

        int typeMismatch = 0;
        int recursiveMutants = 0;
        int missingExcluded = 0;
        int pelipperNative = 0;
        int pelipperIdentityMiss = 0;
        int pelipperDuplicateEncounter = 0;
        HashSet<string> pelipperEncounterIds = new(StringComparer.Ordinal);

        foreach (Monster minion in minions)
        {
            if (MonsterMutationService.IsMutant(minion))
                recursiveMutants++;

            if (!minion.modData.ContainsKey(MonsterMutationService.MutationExcludedMarker))
                missingExcluded++;

            minion.modData.TryGetValue(Alpha674418NativeMutationMinionService.NativeProviderMarker, out string? provider);
            minion.modData.TryGetValue(MonsterMutationService.MutationSourceMarker, out string? sourceType);

            if (string.Equals(provider, "pelipper-native", StringComparison.Ordinal))
            {
                pelipperNative++;
                if (!PelipperTownCompatibilityService.IsWildCombatActor(minion)
                    || !PelipperWildEncounterIdentityService.TryResolve(minion, out PelipperWildEncounterIdentity identity))
                {
                    pelipperIdentityMiss++;
                    continue;
                }

                if (!pelipperEncounterIds.Add(identity.EncounterId))
                    pelipperDuplicateEncounter++;
                continue;
            }

            string runtimeType = minion.GetType().FullName ?? minion.GetType().Name;
            if (!string.IsNullOrWhiteSpace(sourceType)
                && !runtimeType.Equals(sourceType, StringComparison.Ordinal))
            {
                typeMismatch++;
            }
        }

        int factoryFallback = MonsterMutationMinionFactory.FallbackSpawned;
        int failClosed = MonsterMutationMinionFactory.FailClosedRejected;
        bool observed = minions.Count > 0;
        bool pass = typeMismatch == 0
            && recursiveMutants == 0
            && missingExcluded == 0
            && factoryFallback == 0
            && pelipperIdentityMiss == 0
            && pelipperDuplicateEncounter == 0;

        string line = $"location={location.NameOrUniqueName} minions={minions.Count} typeMismatch={typeMismatch} "
            + $"recursiveMutants={recursiveMutants} missingExcluded={missingExcluded} factoryFallback={factoryFallback} "
            + $"failClosed={failClosed} pelipperNative={pelipperNative} pelipperIdentityMiss={pelipperIdentityMiss} "
            + $"pelipperDuplicateEncounter={pelipperDuplicateEncounter}";

        return new Alpha674440MutationAudit(observed, pass, line);
    }

    private static int ReadAlpha674440IntMarker(Monster monster, string key)
        => monster.modData.TryGetValue(key, out string? raw)
            && int.TryParse(raw, out int value)
                ? value
                : 0;

    private static string NormalizeAlpha674440Species(string value)
    {
        string text = value.Trim()
            .Replace("♂", "male", StringComparison.Ordinal)
            .Replace("♀", "female", StringComparison.Ordinal);

        bool changed;
        do
        {
            changed = false;
            foreach (string prefix in new[] { "Wild ", "Shiny " })
            {
                if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                text = text[prefix.Length..].Trim();
                changed = true;
            }
        } while (changed && text.Length > 0);

        return new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private readonly record struct Alpha674440MutationAudit(bool Observed, bool Pass, string Line);
}
