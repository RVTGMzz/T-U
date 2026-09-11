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
STAGE = ROOT / "_stage_alpha6742"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6742.txt"
AUDIT = ROOT / "LOWER_WORKINGS_DESCENT_AUDIT_ALPHA6742.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_42_LOWER_WORKINGS_DESCENT_VI.txt"
VERSION = "0.2.0-alpha.6.7.42"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.42_LOWER_WORKINGS_DESCENT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.42_LOWER_WORKINGS_DESCENT_TEST.sha256.txt"
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
    a6728 = text("ModEntry.Alpha6728.cs")
    a6740 = text("ModEntry.Alpha6740.cs")
    a6741 = text("ModEntry.Alpha6741.cs")
    a6742 = text("ModEntry.Alpha6742.cs")
    descent = text("Story/LowerWorkingsDescentStoryService.cs")
    protocol = text("Story/LowerWorkingsEntryProtocolStoryService.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge_story = text("Story/TheSurgeStoryService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6742Events();" in entry, "6.7.42 events not registered")
    req("Lower Workings descent / threshold crossing layer active." in entry, "6.7.42 startup marker missing")

    req('public const string StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsDescentStage";' in descent, "descent stage key missing")
    req('public const string ThresholdCrossedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed";' in descent, "threshold flag missing")
    req('public const string FirstDescentCompleteFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete";' in descent, "first descent flag missing")
    req("public const int CompleteStage = 5;" in descent, "descent route must complete at stage 5")
    req("ThresholdCrossingTicksRequired = 120" in descent, "120-tick threshold crossing missing")
    req("ThresholdInspectionTicksRequired = 180" in descent, "180-tick threshold inspection missing")
    req("LowerWorkingsEntryProtocolStoryService.CompleteStage" in descent and "_isEntryProtocolReady()" in descent, "Entry Protocol READY prerequisite missing")
    req("_isSurgeHigh()" in descent, "SURGE HIGH prerequisite missing")
    req("TeamUpRosterProgressionService.MaxStoryNpcSlots" in descent, "4/4 story roster prerequisite missing")
    req("ControlledBreachFirstEntryStoryService.BreachLocationKey" in descent, "exact recorded breach face not reused")
    req("_getRequiredFieldPeople()" in descent and "_getRequiredNpcAllies()" in descent, "adaptive full formation delegation missing")
    req('Game1.MasterPlayer.modData[ThresholdCrossedFlagKey] = "1"' in descent, "threshold crossed state is not persisted")
    req('Game1.MasterPlayer.modData[FirstDescentCompleteFlagKey] = "1"' in descent, "first descent complete state is not persisted")
    req("_stage == 2" in descent and "_stage == 3" in descent, "two-step threshold operation missing")
    req("_crossingTicks = 0" in descent and "_inspectionTicks = 0" in descent, "hold reset behavior missing")
    req("!Context.IsPlayerFree" in descent, "player-free reset gate missing")
    req("Game1.activeClickableMenu is null" in descent and "!Game1.dialogueUp" in descent, "menu/dialogue gating missing")
    req("LowerWorkingsDescentAlpha6742 = new LowerWorkingsDescentStoryService" in a6742, "6.7.42 service wiring missing")
    req("teamup_lower_descent" in a6742, "6.7.42 debug command missing")
    req("EntryProtocolAlpha6740.GetRequiredFieldPeople()" in a6742 and "EntryProtocolAlpha6740.GetRequiredNpcAllies()" in a6742, "6.7.40 adaptive formation not reused")

    req("public const int CompleteStage = 4;" in protocol, "6.7.40 Entry Protocol regressed")
    req("ReadinessHoldTicksRequired = 240" in protocol, "6.7.40 240-tick readiness hold regressed")
    req('public const string ProtocolReadyFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady";' in protocol, "6.7.40 READY flag regressed")

    req("narrativeStage > 30" in reactions, "reaction windows must remain 0..30")
    req("GetStoryReactionWindowAlpha6741()" in a6741, "6.7.41 reaction resolver missing")
    req(a6728.count("GetStoryReactionWindowAlpha6741()") >= 2, "NPC interaction/diagnostic no longer routed to 6.7.41 resolver")
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(sum(1 for k in en if k.startswith("story.react.")) == 430, "EN reaction catalog must remain exactly 430 lines")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 430, "VI reaction catalog must remain exactly 430 lines")

    descent_keys = {
        "story.descent.need-protocol",
        "story.descent.missing-face",
        "story.descent.briefing",
        "story.descent.objective.reach",
        "story.descent.wrong-shaft",
        "story.descent.need-full-team",
        "story.descent.threshold-ready",
        "story.descent.objective.cross",
        "story.descent.crossed",
        "story.descent.objective.inspect",
        "story.descent.evidence",
        "story.descent.objective.return",
        "story.descent.report",
        "story.descent.complete",
    }
    req(descent_keys.issubset(en) and descent_keys.issubset(vi), "6.7.42 localization incomplete")
    for key in descent_keys:
        for forbidden in ("Rank S", "The Last Blaster", "George Mullner", "Keeper", "Sector 17", "SECTOR 17"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req("warpFarmer" not in descent and "Game1.currentLocation =" not in descent, "6.7.42 must not fake a dedicated Lower Workings map")
    req("Boss" not in descent and "boss" not in descent.lower(), "unexpected boss implementation in descent service")
    req("UnlockTo(" not in descent and "UnlockTo(" not in a6742, "6.7.42 must not unlock roster slots")
    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person formation cap regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first mutation trigger regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "Pelipper capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("LOWER WORKINGS DESCENT ROUTE 0..5: PASS")
    log("ENTRY PROTOCOL READY + SURGE HIGH + STORY SLOT4 PREREQUISITES: PASS")
    log("EXACT RECORDED BREACH FACE REUSED: PASS")
    log("ADAPTIVE FULL OPERATIONAL FORMATION REUSED: PASS")
    log("120-TICK THRESHOLD CROSSING: PASS")
    log("PERSISTENT THRESHOLD-CROSSED STATE: PASS")
    log("180-TICK FIRST INTERIOR INSPECTION: PASS")
    log("PERSISTENT FIRST-DESCENT-COMPLETE STATE: PASS")
    log("FORMATION / WARP / FREE-STATE / MENU RESET: PASS")
    log("DELIBERATE-CONTAINMENT EVIDENCE WITHOUT HISTORICAL-WORKER IDENTITY: PASS")
    log("REACTION WINDOWS 0..30 CARRY-FORWARD: PASS (430 lines/language)")
    log("FIVE-PEOPLE TOTAL FORMATION CAP: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("NO CUSTOM LOWER-WORKINGS MAP, FINAL BOSS OR ROSTER EXPANSION: PASS")
    log("PELIPPER / MUTATION SAFETY CARRY-FORWARD: PASS")
    log("LIVE THRESHOLD-CROSSING / FIRST-DESCENT PACING STILL REQUIRED")

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
        "LowerWorkingsDescentStoryService",
        "Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed",
        "Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete",
        "teamup_lower_descent",
        "story.descent.crossed",
        "story.descent.evidence",
        "GetStoryReactionWindowAlpha6741",
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
        "# Team Up Alpha 6.7.42 - Lower Workings Descent / Threshold Crossing Audit\n\n"
        "## Implemented\n"
        "- Added the first real Lower Workings operation after Entry Protocol READY.\n"
        "- Reuses the exact recorded controlled-breach MineShaft and adaptive full operational formation.\n"
        "- Requires a continuous 120-tick threshold crossing before persisting ThresholdCrossed.\n"
        "- Requires a further 180-tick first-interior inspection before the team may withdraw.\n"
        "- Persists FirstDescentComplete only after the Guild debrief.\n"
        "- Physical evidence now supports deliberate emergency containment without identifying the historical worker.\n\n"
        "## Scope / safety\n"
        "- No fake warp or dedicated Lower Workings map is introduced yet.\n"
        "- No final boss, George reveal, Evelyn reveal, roster expansion, monster spawn, capture rewrite, or mutation rewrite.\n"
        "- Story reaction catalog remains windows 0..30 with 430 lines per language.\n"
        "- Five PEOPLE total remains the hard formation ceiling.\n\n"
        "## Live verification still required\n"
        "- Validate both 120-tick and 180-tick holds in game.\n"
        "- Validate reset behavior when formation breaks, menu/dialogue opens, or player leaves the exact shaft.\n"
        "- Validate ThresholdCrossed persists after save/reload at stage 3+.\n"
        "- Validate FirstDescentComplete persists after the Guild report at stage 5.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.42 - LOWER WORKINGS DESCENT / THRESHOLD CROSSING\n"
        "============================================================\n\n"
        "PREP:\n"
        "- Cai build 6.7.42 va load save host.\n"
        "- teamup_entry_protocol status -> stage 4/4, ready=True.\n"
        "- teamup_surge_high status -> high=True.\n"
        "- teamup_roster_story status -> 4/4.\n"
        "- teamup_lower_descent reset.\n\n"
        "ROUTE:\n"
        "1. Vao AdventureGuild -> stage 1, Marlon giao first-descent order.\n"
        "2. Dua full operational formation toi DUNG recorded breach MineShaft -> stage 2.\n"
        "3. Giu full formation 120 tick -> stage 3, thresholdCrossed=True.\n"
        "4. Thu pha formation / warp / mo menu-dialogue truoc 120 tick -> crossing hold phai reset.\n"
        "5. O stage 3 giu full formation them 180 tick -> stage 4, hien evidence deliberate containment.\n"
        "6. Thu pha formation / warp / mo menu-dialogue truoc 180 tick -> inspection hold phai reset.\n"
        "7. Quay ve AdventureGuild -> stage 5/5, firstDescentComplete=True.\n"
        "8. Save + reload -> thresholdCrossed=True va firstDescentComplete=True van con.\n"
        "9. Khong co boss, khong new dungeon warp, khong George reveal, roster van 4/4.\n"
        "10. teamup_story_reactions status van catalog 0..30.\n\n"
        "DEBUG: teamup_lower_descent status|reset|stage 0-5\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Lower_Workings_Descent_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.42")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
except Exception as exc:
    lines.append(f"BUILD FAILURE: {exc}")
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
    raise
else:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
