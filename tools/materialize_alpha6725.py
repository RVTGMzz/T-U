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
    "<Version>0.2.0-alpha.6.7.24</Version>",
    "<Version>0.2.0-alpha.6.7.25</Version>",
    "TeamUp.csproj version",
)
csproj_path.write_text(csproj, encoding="utf-8", newline="\n")

# Story state lives separately from the mutation and density engines. The engines remain generic;
# this service decides when the narrative unlock occurs.
story = r'''using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Story;

internal enum SurgeMutationDirective
{
    NormalRoll,
    SuppressMutation,
    ForceFirstMutation
}

/// <summary>
/// Alpha 6.7.25 narrative gate for The Surge.
/// Before the story activates, eligible lethal monster defeats are counted persistently and normal
/// random mutations are suppressed. The tenth eligible lethal defeat is converted into the first
/// guaranteed mutant. Only after that successful conversion do normal 5% mutation rolls and the
/// density overlay unlock. This keeps the first encounter deterministic without changing the
/// generic mutation engine's source-compatibility rules.
/// </summary>
internal sealed class TheSurgeStoryService
{
    public const int FirstMutationKillThreshold = 10;

    private const string KillCountKey = "Ronvotri.TeamUp/SurgeStory/Kills";
    private const string ActivatedKey = "Ronvotri.TeamUp/SurgeStory/Activated";
    private const string FirstMutationSourceKey = "Ronvotri.TeamUp/SurgeStory/FirstMutationSource";
    private const string CountedDeathMarker = "Ronvotri.TeamUp/SurgeStory/DeathCounted";

    private readonly IMonitor _monitor;

    public static TheSurgeStoryService? ActiveInstance { get; private set; }

    public TheSurgeStoryService(IMonitor monitor)
    {
        _monitor = monitor;
        ActiveInstance = this;
    }

    public bool IsActivated
        => Context.IsWorldReady
            && Game1.player.modData.TryGetValue(ActivatedKey, out string? value)
            && value == "1";

    public int KillCount
        => Context.IsWorldReady ? ReadKillCount(Game1.player) : 0;

    public SurgeMutationDirective ObserveEligibleDeath(Monster monster)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return SurgeMutationDirective.NormalRoll;

        if (IsActivated)
            return SurgeMutationDirective.NormalRoll;

        Farmer farmer = Game1.player;
        int count = ReadKillCount(farmer);

        // deathAnimation can be entered more than once by unusual custom monsters. Count each actor
        // only once, but if the threshold was already reached keep forcing the pending first mutation
        // until a conversion succeeds.
        if (!monster.modData.ContainsKey(CountedDeathMarker))
        {
            count = Math.Clamp(count + 1, 0, FirstMutationKillThreshold);
            farmer.modData[KillCountKey] = count.ToString();
            monster.modData[CountedDeathMarker] = "1";
            _monitor.Log($"[SurgeStory] eligible defeat {count}/{FirstMutationKillThreshold}: {monster.Name} ({monster.GetType().FullName})", LogLevel.Debug);
        }

        return count >= FirstMutationKillThreshold
            ? SurgeMutationDirective.ForceFirstMutation
            : SurgeMutationDirective.SuppressMutation;
    }

    public void CommitFirstMutation(Monster monster)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer || IsActivated)
            return;

        Farmer farmer = Game1.player;
        farmer.modData[KillCountKey] = FirstMutationKillThreshold.ToString();
        farmer.modData[ActivatedKey] = "1";
        farmer.modData[FirstMutationSourceKey] = monster.GetType().FullName ?? monster.GetType().Name;

        _monitor.Log(
            $"[SurgeStory] THE SURGE ACTIVATED by first mutant {monster.Name} ({monster.GetType().FullName}). Random mutations and density overlay are now unlocked.",
            LogLevel.Info);

        if (!Game1.eventUp)
            Game1.showGlobalMessage($"⚠ THE SURGE AWAKENS • {monster.Name} MUTATED");
    }

    public string Describe()
    {
        if (!Context.IsWorldReady)
            return "The Surge story: world not loaded.";

        Farmer farmer = Game1.player;
        farmer.modData.TryGetValue(FirstMutationSourceKey, out string? source);
        return $"The Surge Story: activated={IsActivated} | kills={ReadKillCount(farmer)}/{FirstMutationKillThreshold} | "
            + $"randomMutationUnlocked={IsActivated} | densityUnlocked={IsActivated} | firstMutationSource={source ?? "<none>"}";
    }

    public void Reset(Farmer farmer)
    {
        farmer.modData.Remove(KillCountKey);
        farmer.modData.Remove(ActivatedKey);
        farmer.modData.Remove(FirstMutationSourceKey);
        _monitor.Log("[SurgeStory] reset to pre-activation state.", LogLevel.Info);
    }

    public void SetPreActivationKillCount(Farmer farmer, int count)
    {
        int clamped = Math.Clamp(count, 0, FirstMutationKillThreshold - 1);
        farmer.modData[KillCountKey] = clamped.ToString();
        farmer.modData.Remove(ActivatedKey);
        farmer.modData.Remove(FirstMutationSourceKey);
        _monitor.Log($"[SurgeStory] pre-activation kill count set to {clamped}/{FirstMutationKillThreshold} for testing.", LogLevel.Info);
    }

    private static int ReadKillCount(Farmer farmer)
    {
        if (!farmer.modData.TryGetValue(KillCountKey, out string? raw) || !int.TryParse(raw, out int count))
            return 0;
        return Math.Clamp(count, 0, FirstMutationKillThreshold);
    }
}
'''
(SRC / "Story" / "TheSurgeStoryService.cs").write_text(story, encoding="utf-8", newline="\n")

# Mutation engine: suppress random rolls during the prologue, force the first mutation on lethal
# defeat #10, then return to the existing configured random chance after activation.
mutation_path = SRC / "Combat" / "MonsterMutationService.cs"
mutation = mutation_path.read_text(encoding="utf-8")
mutation = replace_once(
    mutation,
    "using Ronvotri.TeamUp.Core;\n",
    "using Ronvotri.TeamUp.Core;\nusing Ronvotri.TeamUp.Story;\n",
    "mutation story using",
)
mutation = replace_once(
    mutation,
    '''        if (!force)
        {
            _rolls++;
            double chance = Math.Clamp(_chancePercent(), 0f, 100f) / 100d;
            if (chance <= 0d || Game1.random.NextDouble() >= chance)
                return false;
        }

        GameLocation? location = monster.currentLocation ?? Game1.currentLocation;''',
    '''        SurgeMutationDirective storyDirective = SurgeMutationDirective.NormalRoll;
        if (!force)
            storyDirective = TheSurgeStoryService.ActiveInstance?.ObserveEligibleDeath(monster)
                ?? SurgeMutationDirective.NormalRoll;

        if (!force)
        {
            _rolls++;
            if (storyDirective == SurgeMutationDirective.SuppressMutation)
                return false;

            if (storyDirective != SurgeMutationDirective.ForceFirstMutation)
            {
                double chance = Math.Clamp(_chancePercent(), 0f, 100f) / 100d;
                if (chance <= 0d || Game1.random.NextDouble() >= chance)
                    return false;
            }
        }

        GameLocation? location = monster.currentLocation ?? Game1.currentLocation;''',
    "mutation narrative gate",
)
mutation = replace_once(
    mutation,
    '''            + $"baseResilience={baseResilience} speed={baseSpeed}->{mutantSpeed} scaleX={effectiveFootprintScale:0.##} "
            + $"scaleApplied={visualScaleApplied} minionsRequested={requestedMinions} force={force}";
        _monitor.Log(LastMutationLine, LogLevel.Info);

        if (!Game1.eventUp)
            Game1.showGlobalMessage($"⚠ MUTATION DETECTED • {monster.Name}");

        return monster.Health > 0;''',
    '''            + $"baseResilience={baseResilience} speed={baseSpeed}->{mutantSpeed} scaleX={effectiveFootprintScale:0.##} "
            + $"scaleApplied={visualScaleApplied} minionsRequested={requestedMinions} force={force} storyDirective={storyDirective}";
        _monitor.Log(LastMutationLine, LogLevel.Info);

        bool transformed = monster.Health > 0;
        if (transformed && storyDirective == SurgeMutationDirective.ForceFirstMutation)
            TheSurgeStoryService.ActiveInstance?.CommitFirstMutation(monster);
        else if (transformed && !Game1.eventUp)
            Game1.showGlobalMessage($"⚠ MUTATION DETECTED • {monster.Name}");

        return transformed;''',
    "mutation activation commit",
)
mutation_path.write_text(mutation, encoding="utf-8", newline="\n")

# Register the story service, gate density until the first mutant, and make the startup label dynamic.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
entry = replace_once(
    entry,
    "            () => Config.EnableMonsterSurge,\n",
    "            () => Config.EnableMonsterSurge && (SurgeStoryAlpha6725?.IsActivated ?? false),\n",
    "density story gate",
)
entry = replace_once(
    entry,
    "        RegisterAlpha6724Events();\n",
    "        RegisterAlpha6724Events();\n        RegisterAlpha6725Events();\n",
    "Alpha 6.7.25 registration",
)
entry = replace_once(
    entry,
    '        Monitor.Log($"Team Up! v{ModManifest.Version} loaded. Codex discovery + observed assessment active.", LogLevel.Info);',
    '        Monitor.Log($"Team Up! v{ModManifest.Version} loaded. Codex discovery + first Surge trigger active.", LogLevel.Info);',
    "startup log",
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

# Developer/test surface. Status writes a small diagnostic so live failures can be returned without
# relying on screenshots of the SMAPI console.
modentry = r'''using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private TheSurgeStoryService SurgeStoryAlpha6725 { get; set; } = null!;

    private void RegisterAlpha6725Events()
    {
        SurgeStoryAlpha6725 = new TheSurgeStoryService(Monitor);
        Helper.ConsoleCommands.Add(
            "teamup_surge_story",
            "The Surge story gate: status | reset | setkills <0-9>. Status writes diagnostics/TeamUp_Surge_Story_latest.txt.",
            OnAlpha6725SurgeStoryCommand);
    }

    private void OnAlpha6725SurgeStoryCommand(string command, string[] args)
    {
        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_surge_story.", LogLevel.Info);
            return;
        }

        switch (action)
        {
            case "status":
                WriteAlpha6725SurgeStoryDiagnostic();
                return;

            case "reset":
                SurgeStoryAlpha6725.Reset(Game1.player);
                MutationAlpha6719.ResetRuntime();
                Surge.Reset();
                if (Game1.currentLocation is not null)
                    Surge.OnWarped(Game1.currentLocation);
                WriteAlpha6725SurgeStoryDiagnostic();
                Game1.showGlobalMessage("THE SURGE STORY • RESET TO 0/10");
                return;

            case "setkills":
                if (args.Length < 2 || !int.TryParse(args[1], out int count) || count < 0 || count >= TheSurgeStoryService.FirstMutationKillThreshold)
                {
                    Monitor.Log("Usage: teamup_surge_story setkills <0-9>", LogLevel.Info);
                    return;
                }
                SurgeStoryAlpha6725.SetPreActivationKillCount(Game1.player, count);
                MutationAlpha6719.ResetRuntime();
                Surge.Reset();
                if (Game1.currentLocation is not null)
                    Surge.OnWarped(Game1.currentLocation);
                WriteAlpha6725SurgeStoryDiagnostic();
                Game1.showGlobalMessage($"THE SURGE STORY • {count}/10");
                return;

            default:
                Monitor.Log("Usage: teamup_surge_story <status|reset|setkills 0-9>", LogLevel.Info);
                return;
        }
    }

    private void WriteAlpha6725SurgeStoryDiagnostic()
    {
        List<string> lines = new()
        {
            "TEAM UP 6.7.25 - FIRST SURGE TRIGGER",
            SurgeStoryAlpha6725.Describe(),
            MutationAlpha6719.Describe(),
            Surge.Describe(),
            "Expected pre-trigger: activated=False, kills<10, randomMutationUnlocked=False, densityUnlocked=False.",
            "Expected after lethal defeat #10 mutates: activated=True, kills=10/10, randomMutationUnlocked=True, densityUnlocked=True."
        };

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Surge_Story_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Surge story diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6725.cs").write_text(modentry, encoding="utf-8", newline="\n")

print("Alpha 6.7.25 first Surge trigger materialized.")
