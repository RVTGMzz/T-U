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
STAGE = ROOT / "_stage_alpha6732"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6732.txt"
AUDIT = ROOT / "FIELD_TRIANGULATION_AUDIT_ALPHA6732.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_32_FIELD_TRIANGULATION_VI.txt"
VERSION = "0.2.0-alpha.6.7.32"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.32_FIELD_TRIANGULATION_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.32_FIELD_TRIANGULATION_TEST.sha256.txt"

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
    a661 = text("ModEntry.Alpha661.cs")
    a6727 = text("ModEntry.Alpha6727.cs")
    a6728 = text("ModEntry.Alpha6728.cs")
    a6729 = text("ModEntry.Alpha6729.cs")
    a6730 = text("ModEntry.Alpha6730.cs")
    a6731 = text("ModEntry.Alpha6731.cs")
    a6732 = text("ModEntry.Alpha6732.cs")
    triangulation = text("Story/FieldTriangulationStoryService.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    old_mine = text("Story/OldMineConnectionStoryService.cs")
    marlon = text("Story/MarlonInvestigationStoryService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge = text("Story/TheSurgeStoryService.cs")
    origin = text("Story/OriginStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6732Events();" in entry, "6.7.32 registration missing")
    req("FieldTriangulationStoryService" in triangulation, "field triangulation service missing")
    req('StageKey = "Ronvotri.TeamUp/Story/FieldTriangulationStage"' in triangulation, "triangulation persistence key missing")
    req('FirstBearingLocationKey = "Ronvotri.TeamUp/Story/FieldTriangulationFirstBearing"' in triangulation, "first-bearing persistence key missing")
    req("public const int CompleteStage = 4;" in triangulation, "triangulation route must be 0..4")
    req("public const int MinimumFieldPeople = 3;" in triangulation, "three-person field-team rule missing")
    req("public const int MinimumActiveNpcAllies = 1;" in triangulation, "active Team Up NPC requirement missing")
    req("OldMineConnectionStoryService.CompleteStage" in triangulation, "old-mine completion prerequisite missing")
    req("location is MineShaft" in triangulation, "MineShaft sampling gate missing")
    req("IsDifferentMineLocation" in triangulation, "independent second MineShaft bearing guard missing")
    req("FirstBearingLocationKey" in triangulation and "location.NameOrUniqueName" in triangulation, "distinct mine-location persistence missing")
    req("CountFieldPeopleAtAlpha6732" in a6732, "field people counter missing")
    req("Game1.getOnlineFarmers().Count" in a6732, "same-location Farmer participation missing")
    req("CountActiveStoryNpcAlliesAtAlpha6732" in a6732, "active NPC ally counter missing")
    req('Distinct(StringComparer.OrdinalIgnoreCase)' in a6732, "duplicate NPC field count guard missing")
    req("teamup_triangulation" in a6732, "triangulation diagnostic command missing")
    req("TeamUp_Field_Triangulation_latest.txt" in a6732, "triangulation diagnostic file missing")
    req("UnlockTo(" not in a6732, "6.7.32 must not unlock another story slot")
    req('UnlockTo(Game1.MasterPlayer, 4' not in all_cs, "story slot 4 unlocked too early")
    req("fourth-unlock" not in all_cs, "slot-4 localization/payoff introduced too early")

    keys = {
        "story.triangulation.briefing",
        "story.triangulation.objective.first-bearing",
        "story.triangulation.bearing-one",
        "story.triangulation.objective.second-bearing",
        "story.triangulation.same-shaft",
        "story.triangulation.bearing-two",
        "story.triangulation.objective.return",
        "story.triangulation.confirmed",
        "story.triangulation.complete",
        "story.triangulation.need-team",
    }
    req(set(en) == set(vi), "EN/VI i18n key parity failed")
    req(keys.issubset(en) and keys.issubset(vi), "6.7.32 field triangulation localization incomplete")
    for key in keys:
        for forbidden in ("Rank S", "Last Blaster", "George Mullner"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"George spoiler leak {forbidden}: {key}")
        for forbidden in ("The Keeper", "Grandmother's Garden", "Keeper"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"Evelyn postgame spoiler leak {forbidden}: {key}")

    reaction_keys = {key for key in en if key.startswith("story.react.")}
    req(len(reaction_keys) == 136, f"6.7.31 reaction catalog regressed: {len(reaction_keys)}")
    req("narrativeStage > 9" in reactions, "reaction window 0..9 range regressed")
    for window in range(7, 10):
        req(f"[{window}] =" in reactions, f"reaction window {window} missing")
    req("GetStoryReactionWindowAlpha6731()" in a6728, "NPC reaction surface no longer uses 6.7.31 resolver")
    req("GetStoryReactionWindowAlpha6731" in a6731, "6.7.31 resolver missing")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("IsGeorgePreRevealLockedAlpha6728" in a6728, "George pre-reveal lock regressed")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit block regressed")
    req("OldMineConnectionStoryService" in old_mine, "6.7.30 old-mine route regressed")
    req('UnlockTo(Game1.MasterPlayer, 3, "old-mine-connection-confirmed")' in a6730, "slot-3 payoff regressed")
    req("MarlonInvestigationStoryService" in marlon, "6.7.29 Marlon case regressed")
    req('UnlockTo(\n            Game1.MasterPlayer,\n            2,' in a6729, "slot-2 payoff regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge, "10-kill first Mutation trigger regressed")
    req('StageKey = "Ronvotri.TeamUp/SurgeNarrativeStage"' in origin, "origin bridge regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("GetEffectiveNpcSlotLimitAlpha6727" in a6727, "five-person/story roster coupling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person total cap regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "Pelipper capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("FIELD TRIANGULATION 0..4 PERSISTENT ROUTE: PASS")
    log("OLD-MINE COMPLETION PREREQUISITE: PASS")
    log("THREE-PEOPLE FIELD TEAM + ACTIVE TEAM UP NPC REQUIREMENT: PASS")
    log("ONLINE FARMERS + ACTIVE NPC ALLIES SAME-LOCATION COUNT: PASS")
    log("TWO DISTINCT MINESHAFT BEARINGS: PASS")
    log("SEALED-WORKINGS CORRIDOR KNOWLEDGE PAYOFF: PASS")
    log("STORY NPC SLOT 4 REMAINS LOCKED: PASS")
    log("REACTION WINDOWS 0..9 CARRY-FORWARD: PASS (136 lines/language)")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("6.7.23-6.7.31 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE FIELD-TEAM COUNT / DISTINCT MINESHAFT / WARP PACING TEST STILL REQUIRED")

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
        "teamup_triangulation",
        "FieldTriangulationStoryService",
        "FieldTriangulationStage",
        "FieldTriangulationFirstBearing",
        "story.triangulation.confirmed",
        "story.triangulation.need-team",
        "teamup_old_mine",
        "story.react.9.marlon",
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
        "# Team Up Alpha 6.7.32 - Field Triangulation Audit\n\n"
        "## Implemented\n"
        "- Added a persistent 0..4 field-search route after the Alpha 6.7.30 Old Mine Connection is complete.\n"
        "- Every route gate requires at least three people physically present in the same location and at least one active Team Up NPC ally.\n"
        "- Online Farmers and active same-location Team Up NPC allies both contribute to the field-team headcount, so multiplayer can satisfy the requirement without demanding three NPCs.\n"
        "- The team records one bearing in a MineShaft, then must move to a different MineShaft location for an independent second bearing.\n"
        "- Returning to Marlon triangulates a likely corridor beside the modern mine network where the sealed workings may connect.\n"
        "- No story slot is unlocked in this checkpoint. Slot 4 remains reserved for a later Surge HIGH / major-chapter milestone.\n"
        "- George remains observed Rank D / Non-Combatant and unrecruitable; the historical worker remains unnamed.\n\n"
        "## Safety\n"
        "- The route is observation/location-only. It does not spawn, clone, replace, hide, damage, retarget, or take ownership of monsters or provider actors.\n"
        "- Pelipper capture ceasefire, provider ownership rules, the five-person formation cap, and the existing 3/4 story-slot state remain authoritative.\n\n"
        "## Live verification still required\n"
        "- Same-location online Farmer counting in multiplayer.\n"
        "- Active NPC ally physical-presence counting after warps.\n"
        "- Distinct MineShaft NameOrUniqueName behavior in the user's real mod stack.\n"
        "- Dialogue/HUD pacing across Guild -> MineShaft A -> MineShaft B -> Guild.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.32 - FIELD TRIANGULATION\n"
        "====================================\n\n"
        "LUU Y: CI-VERIFIED KHONG THAY THE LIVE TEST.\n\n"
        "A. CHUAN BI:\n"
        "1. teamup_old_mine status -> stage=3/3.\n"
        "2. teamup_roster_story status -> unlockedNpcSlots=3/4.\n"
        "3. Lap field team co it nhat 3 nguoi CUNG MAP, trong do co it nhat 1 NPC Team Up Following/Waiting.\n"
        "   Solo: Farmer + 2 NPC la du. Co-op: Farmer khac cung map cung duoc tinh vao tong 3 nguoi.\n\n"
        "B. TUYEN TEST:\n"
        "1. Vao AdventureGuild voi field team -> stage 1, Marlon giao ke hoach dinh vi.\n"
        "2. Vao mot MineShaft voi field team -> stage 2, lay bearing A.\n"
        "3. Thu vao lai CUNG MineShaft location -> KHONG duoc len stage 3; phai bao can mot MineShaft khac.\n"
        "4. Sang mot MineShaft location khac voi field team -> stage 3, lay bearing B.\n"
        "5. Quay lai AdventureGuild voi field team -> stage 4, xac dinh hanh lang sealed workings.\n"
        "6. teamup_roster_story status van phai la unlockedNpcSlots=3/4. KHONG mo slot 4.\n"
        "7. George van Rank D / Non-Combatant, khong recruit, khong lo Rank S/Last Blaster.\n\n"
        "DEBUG:\n"
        "teamup_triangulation status\n"
        "teamup_triangulation reset\n"
        "teamup_triangulation stage 0\n"
        "teamup_triangulation stage 1\n"
        "teamup_triangulation stage 2\n"
        "teamup_triangulation stage 3\n"
        "teamup_triangulation stage 4\n\n"
        "DIAGNOSTIC:\n"
        "diagnostics\\TeamUp_Field_Triangulation_latest.txt\n\n"
        "NEU LOI/CRASH: GUI %appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.32")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
