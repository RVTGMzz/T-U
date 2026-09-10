from __future__ import annotations

import hashlib
import json
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6729"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6729.txt"
AUDIT = ROOT / "MARLON_INVESTIGATION_REACTIONS_AUDIT_ALPHA6729.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_29_MARLON_INVESTIGATION_VI.txt"
VERSION = "0.2.0-alpha.6.7.29"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.29_MARLON_INVESTIGATION_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.29_MARLON_INVESTIGATION_REACTIONS_TEST.sha256.txt"

lines: list[str] = []


def log(value: str) -> None:
    print(value)
    lines.append(value)


def req(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def text(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def dll_contains(blob: bytes, token: str) -> bool:
    return token.encode() in blob or token.encode("utf-16le") in blob


try:
    project = text("TeamUp.csproj")
    entry = text("ModEntry.cs")
    alpha661 = text("ModEntry.Alpha661.cs")
    alpha6727 = text("ModEntry.Alpha6727.cs")
    alpha6728 = text("ModEntry.Alpha6728.cs")
    alpha6729 = text("ModEntry.Alpha6729.cs")
    investigation = text("Story/MarlonInvestigationStoryService.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge_story = text("Story/TheSurgeStoryService.cs")
    origin = text("Story/OriginStoryService.cs")
    config = text("ModConfig.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    default_i18n = json.loads(text("i18n/default.json"))
    vi_i18n = json.loads(text("i18n/vi.json"))
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6729Events();" in entry, "6.7.29 registration missing")
    req("MarlonInvestigationStoryService" in investigation, "Marlon investigation service missing")
    req('StageKey = "Ronvotri.TeamUp/Story/MarlonInvestigationStage"' in investigation, "investigation persistence key missing")
    req("public const int CompleteStage = 4;" in investigation, "investigation four-stage route missing")
    req("_hasActiveAllyAt(location)" in investigation, "investigation does not require active ally")
    req("location is MineShaft" in investigation, "MineShaft field trail gate missing")
    req("MonsterMutationService.IsMutant(monster)" in investigation, "evidence is not restricted to real Mutants")
    req("monster.modData.ContainsKey(EvidenceMonsterMarker)" in investigation, "duplicate evidence death guard missing")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural mutant death observation hook missing")
    req('UnlockTo(\n            Game1.MasterPlayer,\n            2,' in alpha6729, "slot-2 story payoff missing")
    req('"marlon-investigation-debrief"' in alpha6729, "slot-2 unlock source tag missing")
    req("GetStoryReactionWindowAlpha6729" in alpha6729, "expanded reaction window resolver missing")
    req("GetStoryReactionWindowAlpha6729(), npc.Name" in entry, "interaction surface is not using 6.7.29 reaction window")

    # All opening and investigation reaction windows must coexist.
    for window in range(7):
        req(f"[{window}] =" in reactions, f"reaction window {window} missing")
    req("narrativeStage > 6" in reactions, "reaction service range did not expand to window 6")
    for window in range(3, 7):
        req(f'"George", "story.react.{window}.george"' in reactions, f"George reaction missing in window {window}")
        req(f'"Evelyn", "story.react.{window}.evelyn"' in reactions, f"Evelyn reaction missing in window {window}")
        req(f'"Marlon", "story.react.{window}.marlon"' in reactions, f"Marlon reaction missing in window {window}")

    reaction_keys = {key for key in default_i18n if key.startswith("story.react.")}
    required_story_keys = {
        "story.marlon-case.briefing",
        "story.marlon-case.objective.mine",
        "story.marlon-case.mine-trail",
        "story.marlon-case.objective.mutant",
        "story.marlon-case.evidence",
        "story.marlon-case.objective.return",
        "story.marlon-case.debrief",
        "story.marlon-case.complete",
        "story.roster.second-unlock",
    }
    req(set(default_i18n.keys()) == set(vi_i18n.keys()), "EN/VI i18n key parity failed")
    req(required_story_keys.issubset(default_i18n) and required_story_keys.issubset(vi_i18n), "Marlon case localization incomplete")
    req(len(reaction_keys) >= 94, f"expected at least 94 total milestone reactions, found {len(reaction_keys)}")
    for key in reaction_keys:
        req("Rank S" not in default_i18n[key] and "Rank S" not in vi_i18n[key], f"reaction leaks Rank S: {key}")
        req("Last Blaster" not in default_i18n[key] and "Last Blaster" not in vi_i18n[key], f"reaction leaks Last Blaster: {key}")

    # George remains sealed until a later finale chapter.
    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_source, "6.7.29 reveals George too early")
    req("IsGeorgePreRevealLockedAlpha6728" in alpha6728, "George pre-reveal lock regressed")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in alpha661, "authoritative George block regressed")

    # Carry-forward invariants.
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first Mutant trigger regressed")
    req('StageKey = "Ronvotri.TeamUp/SurgeNarrativeStage"' in origin, "Linus/Marlon origin bridge regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story NPC slot ceiling regressed")
    req("GetEffectiveNpcSlotLimitAlpha6727" in alpha6727, "five-person/story roster coupling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person total cap regressed")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("MARLON FIELD CASE 0..4 PERSISTENT ROUTE: PASS")
    log("REAL ACTIVE NPC ALLY REQUIRED AT STORY GATES: PASS")
    log("MINESHAFT TRAIL GATE: PASS")
    log("NATURAL MUTANT DEATH EVIDENCE GATE: PASS")
    log("DUPLICATE EVIDENCE DEATH GUARD: PASS")
    log("MARLON DEBRIEF -> STORY NPC SLOT 2: PASS")
    log("REACTION WINDOWS 0..6: PASS")
    log(f"CURATED MILESTONE REACTION LOCALIZATION: PASS ({len(reaction_keys)} lines/language total)")
    log("GEORGE/EVELYN/MARLON BETWEEN-MILESTONE COVERAGE: PASS")
    log("GEORGE SECRET RANK S / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("6.7.25-6.7.28 STORY/ROSTER CARRY-FORWARD: PASS")
    log("PELIPPER/CAPTURE/PARTY SAFETY CARRY-FORWARD: PASS")
    log("LIVE STORY PACING / MUTANT KILL / WARP TEST STILL REQUIRED")

    proc = subprocess.run(
        ["dotnet", "build", str(SRC / "TeamUp.csproj"), "-c", "Release", "--nologo", "-warnaserror"],
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    print(proc.stdout, end="")
    lines.append(proc.stdout.rstrip())
    req(proc.returncode == 0, "dotnet build failed")
    req("0 Warning(s)" in proc.stdout, "build has warnings")
    req("0 Error(s)" in proc.stdout, "build has errors")

    dll = SRC / "bin" / "Release" / "net6.0" / "TeamUp.dll"
    req(dll.exists(), "TeamUp.dll missing")
    blob = dll.read_bytes()
    for token in [
        "teamup_marlon_case",
        "MarlonInvestigationStoryService",
        "MarlonInvestigationStage",
        "story.marlon-case.debrief",
        "story.roster.second-unlock",
        "story.react.6.marlon",
        "teamup_story_reactions",
        "teamup_roster_story",
        "teamup_capture_ceasefire",
    ]:
        req(dll_contains(blob, token), f"DLL missing token: {token}")
    log("BINARY ACCEPTANCE: PASS")

    RELEASE.mkdir(parents=True, exist_ok=True)
    if STAGE.exists():
        shutil.rmtree(STAGE)
    MOD_STAGE.mkdir(parents=True)
    shutil.copy2(dll, MOD_STAGE / "TeamUp.dll")
    manifest = text("manifest.json").replace("%ProjectVersion%", VERSION)
    (MOD_STAGE / "manifest.json").write_text(manifest, encoding="utf-8", newline="\n")
    shutil.copytree(SRC / "i18n", MOD_STAGE / "i18n")

    if ZIP_PATH.exists():
        ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in sorted(MOD_STAGE.rglob("*")):
            if path.is_file():
                archive.write(path, path.relative_to(STAGE))

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Team Up Alpha 6.7.29 - Marlon Investigation + Milestone Reactions Audit\n\n"
        "## Implemented\n"
        "- Added the first post-Marlon field investigation chapter. The host must bring a real active Team Up NPC ally to the Adventurer's Guild for Marlon's briefing.\n"
        "- The party must then enter a MineShaft together, where an environmental trail advances the case.\n"
        "- Evidence is accepted only when an already-mutated, naturally occurring Team Up Mutant dies inside a MineShaft while an active NPC ally is physically present. No quest monster is fabricated.\n"
        "- Returning to Marlon with the ally completes the debrief and unlocks story NPC ally slot 2/4. The global five-person cap still applies.\n"
        "- Expanded ambient one-shot reaction windows from 0..2 to 0..6, covering briefing, mine trail, evidence secured, and post-debrief/slot-2 state.\n"
        f"- Reaction catalog now contains {len(reaction_keys)} bilingual lines per language in total.\n"
        "- George remains observed Rank D / Non-Combatant and cannot be recruited. His lines may sound like ordinary miner experience but never reveal Rank S, The Last Blaster, or his future role. Evelyn also remains spoiler-safe.\n\n"
        "## Safety\n"
        "- Monster ownership is unchanged. The story observes the existing deathAnimation path but does not create, clone, retarget, hide, or replace provider-owned actors.\n"
        "- Pelipper capture ceasefire and source ownership rules are carried forward.\n"
        "- Story slot 2 is additive via UnlockTo and cannot exceed the 4-NPC story ceiling or 5-person live formation ceiling.\n\n"
        "## Live verification still required\n"
        "- Party-follow warp timing into AdventureGuild and MineShaft.\n"
        "- Natural Mutant evidence capture on a real deathAnimation.\n"
        "- One-shot dialogue pacing for windows 3..6.\n"
        "- Multiplayer physical-ally presence behavior.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.29 - MARLON INVESTIGATION + NPC REACTIONS\n"
        "=====================================================\n\n"
        "CAI DAT:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n\n"
        "LUU Y: BAN NAY CI-VERIFIED, VAN CAN LIVE TEST.\n\n"
        "A. CHUAN BI NHANH:\n"
        "1. Can The Surge da active va Origin stage=2 (da gap Marlon).\n"
        "2. Story roster phai co it nhat 1 slot, recruit 1 NPC thuong va cho NPC dang Following/Waiting cung map.\n"
        "3. Neu can debug: teamup_story_intro stage 2 ; teamup_roster_story setslots 1 ; teamup_marlon_case reset.\n\n"
        "B. TUYEN DIEU TRA:\n"
        "1. Cung dong doi di vao AdventureGuild. Ky vong Marlon briefing, case stage=1, reaction window=3.\n"
        "2. Cung dong doi di vao MineShaft. Ky vong tim dau vet da chay, case stage=2, reaction window=4.\n"
        "3. Ha MOT MUTANT that su trong MineShaft khi dong doi van o cung map. Ky vong lay manh bang chung, case stage=3, reaction window=5.\n"
        "4. Cung dong doi quay lai AdventureGuild. Ky vong Marlon debrief, case stage=4, reaction window=6.\n"
        "5. teamup_roster_story status phai bao unlockedNpcSlots=2/4. Luc nay co the recruit NPC thu 2 neu tong nguoi chua cham cap 5.\n\n"
        "C. PHAN UNG NPC GIUA CAC COT MOC:\n"
        "1. Moi window 3,4,5,6 noi chuyen tay rong voi Abigail/Alex/Clint/Demetrius/Evelyn/George/Gus/Lewis/Linus/Marlon/Maru/Pierre/Robin/Wizard.\n"
        "2. Moi NPC chi reaction 1 lan/window; lan noi tiep quay ve interaction binh thuong.\n"
        "3. George khong duoc lo Rank S/Last Blaster va van khong recruit duoc. Evelyn cung khong duoc lo twist sau game.\n\n"
        "LENH:\n"
        "teamup_marlon_case status\n"
        "teamup_marlon_case reset\n"
        "teamup_marlon_case stage 0\n"
        "teamup_marlon_case stage 1\n"
        "teamup_marlon_case stage 2\n"
        "teamup_marlon_case stage 3\n"
        "teamup_marlon_case stage 4\n"
        "teamup_story_reactions status\n"
        "teamup_story_reactions reset\n"
        "teamup_roster_story status\n\n"
        "CHAN DOAN:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Marlon_Investigation_latest.txt\n\n"
        "NEU LOI/CRASH: THOAT GAME NGAY VA GUI:\n"
        "%appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.29")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
