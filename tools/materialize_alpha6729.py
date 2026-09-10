from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
VERSION_OLD = "0.2.0-alpha.6.7.28"
VERSION_NEW = "0.2.0-alpha.6.7.29"


def replace_once(path: Path, old: str, new: str) -> None:
    text = path.read_text(encoding="utf-8")
    if old not in text:
        raise RuntimeError(f"Expected text not found in {path}: {old[:220]!r}")
    path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")


# Version bump.
replace_once(
    SRC / "TeamUp.csproj",
    f"<Version>{VERSION_OLD}</Version>",
    f"<Version>{VERSION_NEW}</Version>",
)

# Main investigation chapter. It deliberately uses the existing mutant instance/death surface and
# never creates, replaces, or takes ownership of monsters from another provider.
investigation_service = r'''using Ronvotri.TeamUp.Combat;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace Ronvotri.TeamUp.Story;

internal enum MarlonInvestigationTransition
{
    None,
    BriefingStarted,
    MineTrailFound,
    MutantEvidenceSecured,
    DebriefCompleted
}

/// <summary>
/// Alpha 6.7.29 first post-Marlon investigation chapter.
///
/// The chapter requires a real active Team Up NPC ally to accompany the host. Marlon briefs the
/// party at the Guild, the party traces the anomaly inside a MineShaft, a naturally occurring
/// Team Up Mutant must then be defeated in the mine, and returning to Marlon with the ally completes
/// the case. The payoff unlocks story NPC slot 2. No synthetic quest monster is spawned here.
/// </summary>
internal sealed class MarlonInvestigationStoryService
{
    public const string StageKey = "Ronvotri.TeamUp/Story/MarlonInvestigationStage";
    public const string EvidenceMonsterMarker = "Ronvotri.TeamUp/Story/MarlonEvidenceCounted";
    public const int CompleteStage = 4;

    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<int> _getOriginStage;
    private readonly Func<GameLocation, bool> _hasActiveAllyAt;
    private readonly Action _onDebriefComplete;
    private int _stage;

    public static MarlonInvestigationStoryService? ActiveInstance { get; private set; }

    public MarlonInvestigationStoryService(
        IModHelper helper,
        IMonitor monitor,
        Func<int> getOriginStage,
        Func<GameLocation, bool> hasActiveAllyAt,
        Action onDebriefComplete)
    {
        _helper = helper;
        _monitor = monitor;
        _getOriginStage = getOriginStage;
        _hasActiveAllyAt = hasActiveAllyAt;
        _onDebriefComplete = onDebriefComplete;
        ActiveInstance = this;
    }

    /// <summary>
    /// 0 = waiting for first ally, 1 = Marlon briefing accepted, 2 = mine trail found,
    /// 3 = mutant evidence secured, 4 = debrief complete / second NPC slot unlocked.
    /// </summary>
    public int Stage => _stage;
    public bool Completed => _stage >= CompleteStage;

    public void OnSaveLoaded()
        => _stage = ReadStage(Game1.MasterPlayer);

    public MarlonInvestigationTransition OnWarped(GameLocation location)
    {
        if (!CanAdvance(location))
            return MarlonInvestigationTransition.None;

        if (_stage == 0
            && location.NameOrUniqueName.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase)
            && _hasActiveAllyAt(location))
        {
            ShowLine("story.marlon-case.briefing");
            SetStage(Game1.MasterPlayer, 1, "briefing-with-first-ally");
            ShowObjective("story.marlon-case.objective.mine");
            return MarlonInvestigationTransition.BriefingStarted;
        }

        if (_stage == 1 && location is MineShaft && _hasActiveAllyAt(location))
        {
            ShowLine("story.marlon-case.mine-trail");
            SetStage(Game1.MasterPlayer, 2, "mine-trail-found");
            ShowObjective("story.marlon-case.objective.mutant");
            return MarlonInvestigationTransition.MineTrailFound;
        }

        if (_stage == 3
            && location.NameOrUniqueName.Equals("AdventureGuild", StringComparison.OrdinalIgnoreCase)
            && _hasActiveAllyAt(location))
        {
            ShowLine("story.marlon-case.debrief");
            SetStage(Game1.MasterPlayer, CompleteStage, "marlon-debrief");
            _onDebriefComplete();
            ShowObjective("story.marlon-case.complete");
            return MarlonInvestigationTransition.DebriefCompleted;
        }

        return MarlonInvestigationTransition.None;
    }

    public bool ObserveMonsterDeath(Monster monster)
    {
        if (!Context.IsWorldReady
            || !Context.IsMainPlayer
            || _getOriginStage() < 2
            || _stage != 2
            || Game1.eventUp
            || !MonsterMutationService.IsMutant(monster)
            || monster.modData.ContainsKey(EvidenceMonsterMarker))
        {
            return false;
        }

        GameLocation? location = monster.currentLocation ?? Game1.currentLocation;
        if (location is not MineShaft || !_hasActiveAllyAt(location))
            return false;

        monster.modData[EvidenceMonsterMarker] = "1";
        SetStage(Game1.MasterPlayer, 3, $"mutant-evidence:{monster.GetType().FullName ?? monster.Name}");
        ShowLine("story.marlon-case.evidence");
        ShowObjective("story.marlon-case.objective.return");
        _monitor.Log(
            $"[MarlonCase] mutant evidence secured from {monster.Name} ({monster.GetType().FullName}) in {location.NameOrUniqueName}.",
            LogLevel.Info);
        return true;
    }

    public void Reset(Farmer storyOwner)
    {
        storyOwner.modData.Remove(StageKey);
        _stage = 0;
        _monitor.Log("[MarlonCase] investigation stage reset. Roster unlocks were not reduced.", LogLevel.Info);
    }

    public void SetDebugStage(Farmer storyOwner, int stage)
        => SetStage(storyOwner, Math.Clamp(stage, 0, CompleteStage), "debug");

    public string Describe()
    {
        if (!Context.IsWorldReady)
            return "Marlon Investigation: world not loaded.";

        string objective = _stage switch
        {
            0 when _getOriginStage() < 2 => "finish-marlon-origin-bridge",
            0 => "bring-first-ally-to-guild",
            1 => "enter-mineshaft-with-ally",
            2 => "defeat-a-natural-mutant-in-mineshaft-with-ally",
            3 => "return-evidence-to-marlon-with-ally",
            _ => "case-complete-second-slot-unlocked"
        };

        return $"Marlon Investigation: originStage={_getOriginStage()} | stage={_stage}/{CompleteStage} | objective={objective}";
    }

    private bool CanAdvance(GameLocation location)
        => Context.IsWorldReady
            && Context.IsMainPlayer
            && _getOriginStage() >= 2
            && location is not null
            && !Game1.eventUp
            && !Game1.dialogueUp
            && Game1.activeClickableMenu is null;

    private void SetStage(Farmer storyOwner, int stage, string source)
    {
        _stage = Math.Clamp(stage, 0, CompleteStage);
        storyOwner.modData[StageKey] = _stage.ToString();
        _monitor.Log($"[MarlonCase] stage -> {_stage}/{CompleteStage} source={source}.", LogLevel.Info);
    }

    private static int ReadStage(Farmer storyOwner)
    {
        if (!storyOwner.modData.TryGetValue(StageKey, out string? raw) || !int.TryParse(raw, out int value))
            return 0;
        return Math.Clamp(value, 0, CompleteStage);
    }

    private void ShowLine(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text))
            Game1.drawObjectDialogue(text);
    }

    private void ShowObjective(string key)
    {
        string text = _helper.Translation.Get(key).ToString();
        if (!string.IsNullOrWhiteSpace(text) && !Game1.eventUp)
            Game1.showGlobalMessage(text);
    }
}
'''
(SRC / "Story" / "MarlonInvestigationStoryService.cs").write_text(investigation_service, encoding="utf-8", newline="\n")

# Runtime integration, diagnostics, slot-2 payoff, and reaction-window resolver.
alpha6729 = r'''using Ronvotri.TeamUp.Core;
using Ronvotri.TeamUp.Story;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Ronvotri.TeamUp;

public sealed partial class ModEntry
{
    private MarlonInvestigationStoryService MarlonInvestigationAlpha6729 { get; set; } = null!;

    private void RegisterAlpha6729Events()
    {
        MarlonInvestigationAlpha6729 = new MarlonInvestigationStoryService(
            Helper,
            Monitor,
            () => Origin.Stage,
            HasActiveStoryAllyAtAlpha6729,
            OnMarlonInvestigationDebriefCompleteAlpha6729);

        Helper.Events.GameLoop.SaveLoaded += OnAlpha6729SaveLoaded;
        Helper.Events.Player.Warped += OnAlpha6729Warped;
        Helper.ConsoleCommands.Add(
            "teamup_marlon_case",
            "Marlon investigation: status | reset | stage <0-4>. Status writes diagnostics/TeamUp_Marlon_Investigation_latest.txt.",
            OnAlpha6729MarlonCaseCommand);
    }

    private void OnAlpha6729SaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady)
            return;
        MarlonInvestigationAlpha6729.OnSaveLoaded();
    }

    private void OnAlpha6729Warped(object? sender, WarpedEventArgs e)
    {
        if (!Context.IsMainPlayer || !Context.IsWorldReady || !e.IsLocalPlayer)
            return;
        MarlonInvestigationAlpha6729.OnWarped(e.NewLocation);
    }

    private bool HasActiveStoryAllyAtAlpha6729(GameLocation location)
    {
        HashSet<long> online = GetOnlineFarmerIds().ToHashSet();
        foreach (PartyMemberData member in Party.Members)
        {
            if (!online.Contains(member.RecruiterId)
                || member.State is not (PartyMemberState.Following or PartyMemberState.Waiting))
            {
                continue;
            }

            NPC? npc = Game1.getCharacterFromName(member.CharacterName);
            if (npc?.currentLocation == location)
                return true;
        }
        return false;
    }

    private void OnMarlonInvestigationDebriefCompleteAlpha6729()
    {
        bool unlocked = RosterProgressionAlpha6727.UnlockTo(
            Game1.MasterPlayer,
            2,
            "marlon-investigation-debrief");

        EnforceStoryRosterCapacityAlpha6727();
        SavePartyNow();
        BroadcastPartySnapshot();

        if (unlocked)
        {
            ShowHud(Helper.Translation.Get("story.roster.second-unlock", new
            {
                unlocked = RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer),
                max = TeamUpRosterProgressionService.MaxStoryNpcSlots
            }));
        }
    }

    private int GetStoryReactionWindowAlpha6729()
    {
        if (Origin.Stage < 2)
            return Origin.Stage;

        return MarlonInvestigationAlpha6729.Stage switch
        {
            <= 0 => 2,
            1 => 3,
            2 => 4,
            3 => 5,
            _ => 6
        };
    }

    private void OnAlpha6729MarlonCaseCommand(string command, string[] args)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
        {
            Monitor.Log("Load a save as host before using teamup_marlon_case.", LogLevel.Info);
            return;
        }

        string action = args.Length == 0 ? "status" : args[0].Trim().ToLowerInvariant();
        switch (action)
        {
            case "status":
                WriteAlpha6729MarlonCaseDiagnostic();
                return;

            case "reset":
                MarlonInvestigationAlpha6729.Reset(Game1.MasterPlayer);
                WriteAlpha6729MarlonCaseDiagnostic();
                return;

            case "stage":
                if (args.Length < 2 || !int.TryParse(args[1], out int stage) || stage < 0 || stage > MarlonInvestigationStoryService.CompleteStage)
                {
                    Monitor.Log("Usage: teamup_marlon_case stage <0-4>", LogLevel.Info);
                    return;
                }
                MarlonInvestigationAlpha6729.SetDebugStage(Game1.MasterPlayer, stage);
                WriteAlpha6729MarlonCaseDiagnostic();
                return;

            default:
                Monitor.Log("Usage: teamup_marlon_case <status|reset|stage 0-4>", LogLevel.Info);
                return;
        }
    }

    private void WriteAlpha6729MarlonCaseDiagnostic()
    {
        int reactionWindow = GetStoryReactionWindowAlpha6729();
        List<string> lines = new()
        {
            "TEAM UP 6.7.29 - MARLON INVESTIGATION + REACTIONS",
            Origin.Describe(),
            MarlonInvestigationAlpha6729.Describe(),
            $"Active story ally present here: {HasActiveStoryAllyAtAlpha6729(Game1.currentLocation)}",
            $"Reaction window: {reactionWindow} | seen={MilestoneReactionsAlpha6728.GetSeenCount(Game1.player, reactionWindow)}/{MilestoneReactionsAlpha6728.GetAvailableCount(reactionWindow)}",
            $"Story NPC slots: {RosterProgressionAlpha6727.GetUnlockedNpcSlots(Game1.MasterPlayer)}/{TeamUpRosterProgressionService.MaxStoryNpcSlots}",
            "Expected route: first ally -> Guild briefing -> MineShaft trail -> defeat a natural Mutant with ally -> Guild debrief -> slot 2.",
            "George remains pre-reveal non-combatant. This chapter must not set GeorgeCombatRevealed."
        };

        string diagnosticsDir = Path.Combine(Helper.DirectoryPath, "diagnostics");
        Directory.CreateDirectory(diagnosticsDir);
        string outputPath = Path.Combine(diagnosticsDir, "TeamUp_Marlon_Investigation_latest.txt");
        File.WriteAllLines(outputPath, lines);
        foreach (string line in lines)
            Monitor.Log(line, LogLevel.Info);
        Monitor.Log($"Marlon investigation diagnostic saved: {outputPath}", LogLevel.Info);
    }
}
'''
(SRC / "ModEntry.Alpha6729.cs").write_text(alpha6729, encoding="utf-8", newline="\n")

# Register the chapter and let the existing interaction surface consume the expanded reaction window.
entry_path = SRC / "ModEntry.cs"
entry = entry_path.read_text(encoding="utf-8")
if "RegisterAlpha6729Events();" not in entry:
    entry = entry.replace("        RegisterAlpha6728Events();\n", "        RegisterAlpha6728Events();\n        RegisterAlpha6729Events();\n", 1)
entry = entry.replace(
    "loaded. Story roster + milestone reaction layer active.",
    "loaded. Marlon investigation + milestone reaction layer active.",
    1,
)
entry = entry.replace(
    "MilestoneReactionsAlpha6728.TryConsume(Game1.player, Origin.Stage, npc.Name, out string key)",
    "MilestoneReactionsAlpha6728.TryConsume(Game1.player, GetStoryReactionWindowAlpha6729(), npc.Name, out string key)",
    1,
)
entry_path.write_text(entry, encoding="utf-8", newline="\n")

# Keep the 6.7.28 diagnostic useful after the reaction catalog expands.
alpha6728_path = SRC / "ModEntry.Alpha6728.cs"
alpha6728_text = alpha6728_path.read_text(encoding="utf-8")
alpha6728_text = alpha6728_text.replace("        int stage = Origin.Stage;\n", "        int stage = GetStoryReactionWindowAlpha6729();\n", 1)
alpha6728_path.write_text(alpha6728_text, encoding="utf-8", newline="\n")

# Observe the natural death of an already-mutated monster. This is telemetry/story observation only;
# mutation ownership, damage, spawn behavior and death handling remain in their existing services.
mutation_path = SRC / "Combat" / "MonsterMutationService.cs"
mutation = mutation_path.read_text(encoding="utf-8")
needle = '''        try\n        {\n            // Returning false suppresses the lethal death animation only when the same monster\n            // has successfully been converted into a mutant and its Health restored above zero.\n            return !service.TryMutate(__instance, force: false);\n        }'''
replacement = '''        try\n        {\n            MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);\n\n            // Returning false suppresses the lethal death animation only when the same monster\n            // has successfully been converted into a mutant and its Health restored above zero.\n            return !service.TryMutate(__instance, force: false);\n        }'''
if needle not in mutation:
    raise RuntimeError("Mutation death observation insertion point missing")
mutation_path.write_text(mutation.replace(needle, replacement, 1), encoding="utf-8", newline="\n")

# Expand the existing one-shot reaction catalog from windows 0..2 to 0..6.
reaction_path = SRC / "Story" / "StoryMilestoneReactionService.cs"
reactions = reaction_path.read_text(encoding="utf-8")
reactions = reactions.replace("narrativeStage > 2", "narrativeStage > 6", 1)
reactions = reactions.replace(
    "and after Marlon opens the first Team Up ally slot. Missing a window simply means that\n/// reaction is missed; later milestones never replay stale dialogue.",
    "after Marlon opens the first Team Up ally slot, and the four beats of Marlon's first field case. Missing a window simply means that\n/// reaction is missed; later milestones never replay stale dialogue.",
    1,
)
insert_point = '''        return new Dictionary<int, IReadOnlyDictionary<string, string>>\n        {\n            [0] = firstMutant,\n            [1] = afterLinus,\n            [2] = afterMarlon\n        };'''
expanded = '''        Dictionary<string, string> investigationAssigned = Stage(\n            "Abigail", "story.react.3.abigail",\n            "Alex", "story.react.3.alex",\n            "Clint", "story.react.3.clint",\n            "Demetrius", "story.react.3.demetrius",\n            "Evelyn", "story.react.3.evelyn",\n            "George", "story.react.3.george",\n            "Gus", "story.react.3.gus",\n            "Lewis", "story.react.3.lewis",\n            "Linus", "story.react.3.linus",\n            "Marlon", "story.react.3.marlon",\n            "Maru", "story.react.3.maru",\n            "Pierre", "story.react.3.pierre",\n            "Robin", "story.react.3.robin",\n            "Wizard", "story.react.3.wizard");\n\n        Dictionary<string, string> mineTrailFound = Stage(\n            "Abigail", "story.react.4.abigail",\n            "Alex", "story.react.4.alex",\n            "Clint", "story.react.4.clint",\n            "Demetrius", "story.react.4.demetrius",\n            "Evelyn", "story.react.4.evelyn",\n            "George", "story.react.4.george",\n            "Gus", "story.react.4.gus",\n            "Lewis", "story.react.4.lewis",\n            "Linus", "story.react.4.linus",\n            "Marlon", "story.react.4.marlon",\n            "Maru", "story.react.4.maru",\n            "Pierre", "story.react.4.pierre",\n            "Robin", "story.react.4.robin",\n            "Wizard", "story.react.4.wizard");\n\n        Dictionary<string, string> evidenceSecured = Stage(\n            "Abigail", "story.react.5.abigail",\n            "Alex", "story.react.5.alex",\n            "Clint", "story.react.5.clint",\n            "Demetrius", "story.react.5.demetrius",\n            "Evelyn", "story.react.5.evelyn",\n            "George", "story.react.5.george",\n            "Gus", "story.react.5.gus",\n            "Lewis", "story.react.5.lewis",\n            "Linus", "story.react.5.linus",\n            "Marlon", "story.react.5.marlon",\n            "Maru", "story.react.5.maru",\n            "Pierre", "story.react.5.pierre",\n            "Robin", "story.react.5.robin",\n            "Wizard", "story.react.5.wizard");\n\n        Dictionary<string, string> secondSlotUnlocked = Stage(\n            "Abigail", "story.react.6.abigail",\n            "Alex", "story.react.6.alex",\n            "Clint", "story.react.6.clint",\n            "Demetrius", "story.react.6.demetrius",\n            "Evelyn", "story.react.6.evelyn",\n            "George", "story.react.6.george",\n            "Gus", "story.react.6.gus",\n            "Lewis", "story.react.6.lewis",\n            "Linus", "story.react.6.linus",\n            "Marlon", "story.react.6.marlon",\n            "Maru", "story.react.6.maru",\n            "Pierre", "story.react.6.pierre",\n            "Robin", "story.react.6.robin",\n            "Wizard", "story.react.6.wizard");\n\n        return new Dictionary<int, IReadOnlyDictionary<string, string>>\n        {\n            [0] = firstMutant,\n            [1] = afterLinus,\n            [2] = afterMarlon,\n            [3] = investigationAssigned,\n            [4] = mineTrailFound,\n            [5] = evidenceSecured,\n            [6] = secondSlotUnlocked\n        };'''
if insert_point not in reactions:
    raise RuntimeError("Reaction catalog expansion point missing")
reaction_path.write_text(reactions.replace(insert_point, expanded, 1), encoding="utf-8", newline="\n")

# Bilingual story beats and reaction lines.
en = {
    "story.marlon-case.briefing": "Marlon: Good. You brought someone. I found a survey mark in an old Guild report that matches the pattern around your Mutant. Take your partner into the mines and look for stone scorched from the inside, not by a surface blast.",
    "story.marlon-case.objective.mine": "MARLON'S CASE • Enter a MineShaft with an active Team Up ally.",
    "story.marlon-case.mine-trail": "The coal dust thins around a ring of blackened stone. The burn runs through the rock instead of across it. Your ally notices the same hooked scratch shown in Marlon's old sketch.",
    "story.marlon-case.objective.mutant": "MARLON'S CASE • With your ally beside you, defeat a naturally occurring Mutant inside the mines.",
    "story.marlon-case.evidence": "Something brittle remains where the Mutant fell: a dark shard veined with a dull ember glow. It carries the same hooked mark as the old Guild record.",
    "story.marlon-case.objective.return": "MARLON'S CASE • Return the evidence to Marlon with your ally.",
    "story.marlon-case.debrief": "Marlon: Same mark. Same burn pattern. The incident report is thirty years old, but the name of the miner who stopped it is missing and the company page was cut from the file. This is no longer a one-person search. Build the team carefully. I'll keep digging through the archive.",
    "story.marlon-case.complete": "MARLON'S CASE • First field investigation complete.",
    "story.roster.second-unlock": "TEAM UP • Marlon's first field case is complete. Story ally capacity increased to {{unlocked}}/{{max}}.",

    "story.react.3.abigail": "Abigail: An old Guild map, a missing name, and a mine nobody wants to talk about? I know that's terrible, but that is also extremely hard not to investigate.",
    "story.react.3.alex": "Alex: Marlon sent you back down there already? Fine. Just make sure the person with you can pull you out if the floor gives way.",
    "story.react.3.clint": "Clint: Burned stone can tell you a lot. A normal charge fractures outward. If the heat came from inside the seam, that is a different problem.",
    "story.react.3.demetrius": "Demetrius: Compare geometry, residue, temperature and depth. Memory is persuasive; repeatable measurements are better.",
    "story.react.3.evelyn": "Evelyn: Stay close to your companion down there. You don't have to prove who is braver. You only have to come home together.",
    "story.react.3.george": "George: If Marlon wants you checking old blast marks, look at the ceiling before the floor. Falling rock won't wait for you to finish reading a map.",
    "story.react.3.gus": "Gus: So the first official Team Up outing is into a suspicious old mine. I was hoping for something with chairs and soup.",
    "story.react.3.lewis": "Lewis: If this involves an old industrial incident, document what you find before rumors turn it into ten different stories.",
    "story.react.3.linus": "Linus: Stone keeps memories differently than people do. Pressure, heat, water... all of it leaves a language behind.",
    "story.react.3.marlon": "Marlon: Do not chase a theory. Chase the evidence. If the two meet underground, then we have something worth fearing.",
    "story.react.3.maru": "Maru: Take notes before touching anything unusual. Position first, sample second. We lose information every time something moves.",
    "story.react.3.pierre": "Pierre: You're investigating the mines with a partner? Great. Wonderful. Please don't let this become a reason everyone stops shopping after dark.",
    "story.react.3.robin": "Robin: Old tunnels lie. A wall that looks solid can be carrying weight from three rooms away. Watch the supports, not just the monsters.",
    "story.react.3.wizard": "Wizard: The mark Marlon remembers may be less a symbol than a wound. Some wounds in stone close very slowly.",

    "story.react.4.abigail": "Abigail: You actually found the mark? Then the old report wasn't just some hunter's campfire story. That somehow makes this better and worse.",
    "story.react.4.alex": "Alex: So something burned through the rock itself. I don't know geology, but I know when 'keep going' starts sounding like a bad plan.",
    "story.react.4.clint": "Clint: Heat inside the seam with no normal blast fan... yeah, I don't like that. Bring me a fragment only if Marlon says it's safe.",
    "story.react.4.demetrius": "Demetrius: Internal scorching narrows the possibilities dramatically. If a Mutant carries matching material, correlation becomes much stronger.",
    "story.react.4.evelyn": "Evelyn: You found what Marlon was looking for? Then don't let curiosity hurry your feet. Careful people still get answers.",
    "story.react.4.george": "George: A charge leaves a direction. If the stone is burned in a ring, the blast wasn't doing what a normal charge does. Hmph. Keep your distance.",
    "story.react.4.gus": "Gus: Every time you come back from the mines, your story gets less comforting. The soup offer remains open.",
    "story.react.4.lewis": "Lewis: A physical match to an old record changes this from rumor to investigation. Keep the evidence chain clean.",
    "story.react.4.linus": "Linus: The animals near the mountain have been avoiding certain paths. Perhaps they felt that old scar before any of us saw it.",
    "story.react.4.marlon": "Marlon: Good. Now we need a living link between the old mark and the current Surge. Do not manufacture one. Wait for the mines to show you the truth.",
    "story.react.4.maru": "Maru: If the pattern repeats on a Mutant, photograph or note the location before the body disappears. That comparison matters.",
    "story.react.4.pierre": "Pierre: You found burned rock? I sell backpacks, not geological insurance. Please tell me geological insurance isn't about to become a thing.",
    "story.react.4.robin": "Robin: Stone scorched through its thickness can get brittle in ways you can't see. No leaning on suspicious walls, okay?",
    "story.react.4.wizard": "Wizard: A scar has answered another scar. The next answer may arrive wearing teeth.",

    "story.react.5.abigail": "Abigail: The Mutant dropped the same kind of shard? Okay. Now I officially hate how neatly the pieces fit together.",
    "story.react.5.alex": "Alex: You got the evidence and both of you made it back up. Good. Marlon can do the creepy archive part now.",
    "story.react.5.clint": "Clint: Don't scrape that shard clean. Residue on the surface might be the most useful part of it.",
    "story.react.5.demetrius": "Demetrius: Matching morphology across a thirty-year record and a current organism is significant. We still need provenance before claiming causation.",
    "story.react.5.evelyn": "Evelyn: Wrap that thing before you carry it through town, dear. And wash your hands before dinner. I mean it.",
    "story.react.5.george": "George: Put it in cloth. Not your pocket. If you don't know what a hot rock is carrying, you don't let it sit against your leg all afternoon.",
    "story.react.5.gus": "Gus: A glowing shard from a dead Mutant is absolutely not coming onto my kitchen counter. Table by the door, maybe.",
    "story.react.5.lewis": "Lewis: Take it straight to Marlon. Until we understand it, I would rather the town not start collecting souvenirs.",
    "story.react.5.linus": "Linus: The thing you carry feels quiet now, but quiet is not the same as harmless.",
    "story.react.5.marlon": "Marlon: Bring it here. Don't polish it, break it, or let anyone sell it. The damage is part of the record.",
    "story.react.5.maru": "Maru: Great sample, terrible circumstances. Keep it isolated and tell Marlon exactly where the Mutant fell.",
    "story.react.5.pierre": "Pierre: Before anyone asks, no, I am not stocking mysterious Mutant shards. I do have display cases, though. Completely unrelated.",
    "story.react.5.robin": "Robin: Bag the dust with the shard if you can. Sometimes the tiny debris tells you which side failed first.",
    "story.react.5.wizard": "Wizard: That fragment remembers pressure. It has forgotten the creature, but not the force that shaped it.",

    "story.react.6.abigail": "Abigail: Two ally slots now? That's starting to sound less like a rescue pair and more like an actual expedition. I approve.",
    "story.react.6.alex": "Alex: Two people watching your back is better than one. Just don't turn 'bigger team' into 'bigger risks.'",
    "story.react.6.clint": "Clint: More people means more gear to maintain. Bring damaged weapons in before a small crack becomes a snapped blade.",
    "story.react.6.demetrius": "Demetrius: A second observer improves coverage, but only if the team communicates. Redundant confusion is still confusion.",
    "story.react.6.evelyn": "Evelyn: Two companions means three lunches. I am not joking. Heroes get hungry too.",
    "story.react.6.george": "George: Two people can make the same bad decision together. More bodies don't replace good judgment.",
    "story.react.6.gus": "Gus: Your team is growing. If you all survive the next trip, first round of dinner is on me. Dinner, not drinks.",
    "story.react.6.lewis": "Lewis: Marlon trusts this investigation enough to expand the team. Treat that trust as responsibility, not permission to escalate everything.",
    "story.react.6.linus": "Linus: More footsteps change the way danger hears you. Move as one group, not several lonely people standing close together.",
    "story.react.6.marlon": "Marlon: Two allies. Good. From here on, learn what each person sees that the others miss. That is what makes a team dangerous to monsters.",
    "story.react.6.maru": "Maru: Three viewpoints can triangulate movement much better. Maybe Team Up is becoming a field instrument with opinions.",
    "story.react.6.pierre": "Pierre: Two companions? Excellent. Statistically that's... more customers needing supplies. What? Preparedness is important.",
    "story.react.6.robin": "Robin: With three people, assign jobs before going underground. Scout, support, exit route. Improvisation is not a structural plan.",
    "story.react.6.wizard": "Wizard: The constellation gains another star. Be certain the pattern you form is one you intend to keep."
}

vi = {
    "story.marlon-case.briefing": "Marlon: Tốt. Cậu đã dẫn theo một người. Tôi tìm thấy một dấu khảo sát trong hồ sơ cũ của Hội, trùng với hoa văn quanh con Mutant của cậu. Hãy cùng đồng đội xuống mỏ và tìm loại đá bị cháy từ bên trong, không phải vết nổ từ bề mặt.",
    "story.marlon-case.objective.mine": "HỒ SƠ MARLON • Đi vào một tầng hầm mỏ cùng ít nhất 1 đồng đội Team Up đang hoạt động.",
    "story.marlon-case.mine-trail": "Lớp bụi than mỏng dần quanh một vòng đá cháy đen. Vết nhiệt xuyên qua lòng đá thay vì quét trên bề mặt. Đồng đội của bạn nhận ra một vết móc giống hệt nét vẽ trong hồ sơ cũ của Marlon.",
    "story.marlon-case.objective.mutant": "HỒ SƠ MARLON • Khi đồng đội ở bên cạnh, hạ một Mutant xuất hiện tự nhiên bên trong hầm mỏ.",
    "story.marlon-case.evidence": "Nơi Mutant ngã xuống còn lại một mảnh vật chất giòn, đen sẫm, bên trong le lói những đường sáng như than hồng. Trên nó có cùng dấu móc với hồ sơ cũ của Hội.",
    "story.marlon-case.objective.return": "HỒ SƠ MARLON • Mang bằng chứng trở lại gặp Marlon cùng đồng đội của bạn.",
    "story.marlon-case.debrief": "Marlon: Cùng một dấu. Cùng kiểu cháy. Báo cáo sự cố này đã ba mươi năm, nhưng tên người thợ mỏ đã ngăn nó lại bị bỏ trống, còn trang của công ty thì bị cắt khỏi hồ sơ. Đây không còn là cuộc điều tra cho một người nữa. Hãy xây đội thật cẩn thận. Tôi sẽ tiếp tục đào kho lưu trữ.",
    "story.marlon-case.complete": "HỒ SƠ MARLON • Hoàn thành cuộc điều tra thực địa đầu tiên.",
    "story.roster.second-unlock": "TEAM UP • Vụ điều tra thực địa đầu tiên của Marlon đã hoàn tất. Sức chứa đồng đội cốt truyện tăng lên {{unlocked}}/{{max}}.",

    "story.react.3.abigail": "Abigail: Bản đồ Hội cũ, một cái tên bị mất, rồi một khu mỏ chẳng ai muốn nhắc tới? Biết là tệ thật, nhưng nghe xong rất khó mà không muốn điều tra.",
    "story.react.3.alex": "Alex: Marlon lại bảo cậu xuống đó ngay à? Được thôi. Chỉ cần chắc người đi cùng đủ sức kéo cậu ra nếu sàn mỏ sập.",
    "story.react.3.clint": "Clint: Đá cháy có thể kể khá nhiều thứ. Kíp nổ bình thường làm nứt đá theo hướng bung ra. Nếu nhiệt xuất phát từ trong vỉa thì đó là chuyện khác.",
    "story.react.3.demetrius": "Demetrius: So sánh hình dạng, cặn bám, nhiệt độ và độ sâu. Ký ức rất thuyết phục, nhưng số đo lặp lại được còn tốt hơn.",
    "story.react.3.evelyn": "Evelyn: Xuống đó thì nhớ ở gần người đồng hành nhé cháu. Không cần chứng minh ai gan hơn ai. Chỉ cần cả hai cùng trở về.",
    "story.react.3.george": "George: Marlon bảo xem vết nổ cũ thì nhìn trần trước rồi hẵng nhìn sàn. Đá rơi không chờ cậu đọc xong bản đồ đâu.",
    "story.react.3.gus": "Gus: Vậy chuyến Team Up chính thức đầu tiên là chui vào một khu mỏ cũ đáng ngờ. Tôi đã hy vọng nó có ghế và súp cơ.",
    "story.react.3.lewis": "Lewis: Nếu chuyện này liên quan đến một tai nạn công nghiệp cũ, hãy ghi lại thứ cậu tìm thấy trước khi tin đồn biến nó thành mười câu chuyện khác nhau.",
    "story.react.3.linus": "Linus: Đá lưu ký ức khác con người. Áp lực, nhiệt, nước... tất cả đều để lại một thứ ngôn ngữ.",
    "story.react.3.marlon": "Marlon: Đừng săn đuổi một giả thuyết. Hãy săn bằng chứng. Nếu hai thứ gặp nhau dưới lòng đất, lúc đó chúng ta mới có thứ đáng sợ.",
    "story.react.3.maru": "Maru: Ghi lại mọi thứ trước khi chạm vào vật lạ nhé. Vị trí trước, mẫu vật sau. Cứ di chuyển một thứ là ta mất thêm thông tin.",
    "story.react.3.pierre": "Pierre: Cậu đi điều tra hầm mỏ với đồng đội à? Tuyệt. Tuyệt lắm. Làm ơn đừng để chuyện này thành lý do mọi người ngừng mua sắm sau khi trời tối.",
    "story.react.3.robin": "Robin: Hầm cũ biết nói dối đấy. Một bức tường trông chắc chắn có khi đang gánh lực từ tận ba phòng khác. Nhìn cột chống nữa, đừng chỉ nhìn quái.",
    "story.react.3.wizard": "Wizard: Dấu mà Marlon nhớ có lẽ không hẳn là một ký hiệu, mà là một vết thương. Có những vết thương trên đá khép lại rất chậm.",

    "story.react.4.abigail": "Abigail: Cậu thật sự tìm thấy cái dấu đó rồi à? Vậy hồ sơ cũ không chỉ là chuyện kể quanh lửa trại. Không hiểu sao chuyện đó vừa tốt hơn vừa tệ hơn.",
    "story.react.4.alex": "Alex: Vậy có thứ gì đó đốt xuyên chính khối đá. Tôi không rành địa chất, nhưng tôi biết lúc nào câu 'đi tiếp đi' bắt đầu nghe như một ý tệ.",
    "story.react.4.clint": "Clint: Nhiệt nằm trong vỉa mà không có hướng bung của vụ nổ bình thường... ừ, tôi không thích chuyện này. Chỉ mang mảnh đá về nếu Marlon bảo an toàn.",
    "story.react.4.demetrius": "Demetrius: Vết cháy từ bên trong thu hẹp khả năng rất nhiều. Nếu một Mutant mang vật chất trùng khớp, mối tương quan sẽ mạnh hơn hẳn.",
    "story.react.4.evelyn": "Evelyn: Cháu tìm thấy thứ Marlon cần rồi à? Vậy thì đừng để tò mò làm chân mình vội. Người cẩn thận vẫn tìm được câu trả lời mà.",
    "story.react.4.george": "George: Kíp nổ luôn để lại hướng. Nếu đá cháy thành vòng thì vụ nổ đó không làm thứ một kíp bình thường phải làm. Hừm. Đứng xa ra.",
    "story.react.4.gus": "Gus: Mỗi lần cậu trở về từ hầm mỏ, câu chuyện lại bớt dễ chịu thêm một chút. Lời mời ăn súp vẫn còn hiệu lực nhé.",
    "story.react.4.lewis": "Lewis: Có vật chứng trùng với hồ sơ cũ thì chuyện này không còn là tin đồn nữa. Giữ chuỗi bằng chứng cho sạch.",
    "story.react.4.linus": "Linus: Động vật gần núi dạo này tránh vài lối đi. Có lẽ chúng cảm được vết sẹo cũ đó trước khi chúng ta nhìn thấy.",
    "story.react.4.marlon": "Marlon: Tốt. Giờ ta cần một mắt xích sống giữa dấu cũ và The Surge hiện tại. Đừng tạo ra nó. Hãy chờ hầm mỏ tự cho cậu thấy sự thật.",
    "story.react.4.maru": "Maru: Nếu hoa văn đó lặp lại trên Mutant, ghi vị trí trước khi xác nó biến mất nhé. So sánh đó rất quan trọng.",
    "story.react.4.pierre": "Pierre: Cậu tìm thấy đá cháy à? Tôi bán ba lô chứ không bán bảo hiểm địa chất. Làm ơn đừng bảo sắp có thứ gọi là bảo hiểm địa chất.",
    "story.react.4.robin": "Robin: Đá bị cháy xuyên bề dày có thể giòn theo cách mắt thường không thấy. Không được tựa vào mấy bức tường đáng ngờ, rõ chưa?",
    "story.react.4.wizard": "Wizard: Một vết sẹo vừa trả lời một vết sẹo khác. Câu trả lời tiếp theo có thể sẽ mọc răng.",

    "story.react.5.abigail": "Abigail: Mutant rơi ra đúng loại mảnh đó luôn à? Được rồi. Giờ tôi chính thức ghét cái cách mọi mảnh ghép khớp nhau quá gọn.",
    "story.react.5.alex": "Alex: Có bằng chứng rồi, cả hai cũng lên được mặt đất. Tốt. Giờ để Marlon lo phần kho hồ sơ rợn người đi.",
    "story.react.5.clint": "Clint: Đừng cạo sạch mảnh đó. Lớp cặn trên bề mặt có khi lại là phần hữu ích nhất.",
    "story.react.5.demetrius": "Demetrius: Hình thái trùng nhau giữa một hồ sơ ba mươi năm trước và sinh vật hiện tại là đáng kể. Nhưng vẫn cần nguồn gốc rõ ràng trước khi kết luận nguyên nhân.",
    "story.react.5.evelyn": "Evelyn: Bọc cái thứ đó lại trước khi mang qua thị trấn nhé cháu. Với rửa tay trước bữa tối. Bà nói thật đấy.",
    "story.react.5.george": "George: Bọc nó bằng vải. Đừng nhét túi quần. Không biết một cục đá nóng đang mang thứ gì thì đừng để nó áp vào chân cả buổi chiều.",
    "story.react.5.gus": "Gus: Một mảnh phát sáng từ Mutant chết tuyệt đối không được đặt lên quầy bếp của tôi. Bàn cạnh cửa thì... còn xem xét.",
    "story.react.5.lewis": "Lewis: Mang thẳng tới Marlon. Trước khi hiểu nó là gì, tôi không muốn dân thị trấn bắt đầu sưu tầm làm kỷ niệm.",
    "story.react.5.linus": "Linus: Thứ cậu mang theo giờ rất yên, nhưng yên không đồng nghĩa với vô hại.",
    "story.react.5.marlon": "Marlon: Mang nó vào đây. Đừng đánh bóng, đừng bẻ, cũng đừng để ai bán nó. Chính phần hư hại mới là hồ sơ.",
    "story.react.5.maru": "Maru: Mẫu vật tuyệt vời, hoàn cảnh thì tệ. Cách ly nó và nói cho Marlon chính xác Mutant ngã ở đâu.",
    "story.react.5.pierre": "Pierre: Nói trước nhé, tôi không nhập mấy mảnh Mutant bí ẩn đâu. Nhưng tôi có tủ kính trưng bày. Hoàn toàn không liên quan.",
    "story.react.5.robin": "Robin: Nếu được thì gom cả bụi cùng mảnh đá. Đôi lúc chính vụn nhỏ cho biết phía nào vỡ trước.",
    "story.react.5.wizard": "Wizard: Mảnh đó vẫn nhớ áp lực. Nó đã quên con quái vật, nhưng chưa quên thứ lực đã tạo hình cho nó.",

    "story.react.6.abigail": "Abigail: Giờ có hai ô đồng đội rồi à? Nghe bắt đầu giống một đoàn thám hiểm thật sự hơn là cặp cứu hộ rồi đấy. Tôi duyệt.",
    "story.react.6.alex": "Alex: Hai người trông lưng cho cậu tốt hơn một. Chỉ đừng biến 'đội đông hơn' thành 'liều lớn hơn.'",
    "story.react.6.clint": "Clint: Nhiều người nghĩa là nhiều trang bị cần bảo dưỡng. Đem vũ khí hỏng tới trước khi một vết nứt nhỏ thành lưỡi kiếm gãy.",
    "story.react.6.demetrius": "Demetrius: Có người quan sát thứ hai sẽ tăng độ bao phủ, nhưng chỉ khi cả đội giao tiếp. Ba người cùng rối vẫn chỉ là rối thôi.",
    "story.react.6.evelyn": "Evelyn: Hai người đồng hành nghĩa là ba phần ăn trưa. Bà không đùa đâu. Anh hùng cũng biết đói mà.",
    "story.react.6.george": "George: Hai người vẫn có thể cùng đưa ra một quyết định tệ. Đông người hơn không thay được đầu óc tỉnh táo.",
    "story.react.6.gus": "Gus: Đội của cậu đang lớn dần rồi. Nếu cả nhóm sống sót sau chuyến tới, bữa tối đầu tiên tôi mời. Bữa tối nhé, không phải rượu.",
    "story.react.6.lewis": "Lewis: Marlon tin cuộc điều tra này đủ để mở rộng đội. Hãy xem đó là trách nhiệm, không phải giấy phép để nâng mức nguy hiểm của mọi thứ.",
    "story.react.6.linus": "Linus: Nhiều bước chân sẽ thay đổi cách hiểm nguy nghe thấy cậu. Hãy di chuyển như một nhóm, đừng như vài người cô độc đứng gần nhau.",
    "story.react.6.marlon": "Marlon: Hai đồng đội. Tốt. Từ giờ hãy học xem mỗi người nhìn thấy điều gì mà người khác bỏ sót. Đó mới là thứ khiến một đội trở nên nguy hiểm với quái vật.",
    "story.react.6.maru": "Maru: Ba góc nhìn giúp tam giác hóa chuyển động tốt hơn nhiều. Có khi Team Up đang biến thành một dụng cụ thực địa biết cãi nhau.",
    "story.react.6.pierre": "Pierre: Hai đồng đội à? Tuyệt. Xét về thống kê thì đó là... nhiều khách cần vật tư hơn. Gì chứ? Chuẩn bị kỹ là quan trọng mà.",
    "story.react.6.robin": "Robin: Có ba người thì phân việc trước khi xuống mỏ. Trinh sát, hỗ trợ, đường lui. Ứng biến không phải kế hoạch kết cấu đâu.",
    "story.react.6.wizard": "Wizard: Chòm sao vừa có thêm một vì tinh tú. Hãy chắc rằng hình mà các ngươi tạo thành là hình các ngươi muốn giữ lại."
}

for rel, additions in [("i18n/default.json", en), ("i18n/vi.json", vi)]:
    path = SRC / rel
    data = json.loads(path.read_text(encoding="utf-8"))
    overlap = set(additions).intersection(data)
    if overlap:
        raise RuntimeError(f"6.7.29 localization keys already exist in {rel}: {sorted(overlap)[:5]}")
    data.update(additions)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8", newline="\n")

print("Alpha 6.7.29 Marlon investigation + reaction windows materialized.")
