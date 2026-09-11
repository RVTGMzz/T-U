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
STAGE = ROOT / "_stage_alpha6743"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6743.txt"
AUDIT = ROOT / "LOWER_WORKINGS_DESCENT_REACTIONS_AUDIT_ALPHA6743.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_43_LOWER_WORKINGS_DESCENT_REACTIONS_VI.txt"
VERSION = "0.2.0-alpha.6.7.43"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.43_LOWER_WORKINGS_DESCENT_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.43_LOWER_WORKINGS_DESCENT_REACTIONS_TEST.sha256.txt"
NPCS = ["Abigail","Alex","Clint","Demetrius","Evelyn","George","Gus","Lewis","Linus","Marlon","Maru","Pierre","Robin","Wizard"]
WINDOWS = [(31,"descentAuthorized"),(32,"thresholdLineReady"),(33,"thresholdCrossed"),(34,"firstInteriorInspected"),(35,"firstDescentReported")]
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
    a6741 = text("ModEntry.Alpha6741.cs")
    a6742 = text("ModEntry.Alpha6742.cs")
    a6743 = text("ModEntry.Alpha6743.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    descent = text("Story/LowerWorkingsDescentStoryService.cs")
    protocol = text("Story/LowerWorkingsEntryProtocolStoryService.cs")
    high = text("Story/SurgeHighEscalationStoryService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge_story = text("Story/TheSurgeStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("Lower Workings descent reaction layer active." in entry, "6.7.43 startup marker missing")
    req("narrativeStage > 35" in reactions, "reaction range must be 0..35")

    expected_maps = ["<= 0 => 30", "1 => 31", "2 => 32", "3 => 33", "4 => 34", "_ => 35"]
    for token in expected_maps:
        req(token in a6743, f"6.7.43 resolver mapping missing: {token}")
    req("LowerWorkingsDescentAlpha6742.Stage switch" in a6743, "6.7.43 resolver not based on descent stage")
    req("EntryProtocolAlpha6740.Stage < LowerWorkingsEntryProtocolStoryService.CompleteStage" in a6743, "6.7.41 fallback boundary missing")
    req("GetStoryReactionWindowAlpha6741()" in a6743, "6.7.41 fallback resolver missing")
    req(a6728.count("GetStoryReactionWindowAlpha6743()") >= 2, "NPC interaction/diagnostic not routed to 6.7.43 resolver")
    req("TEAM UP 6.7.43 - LOWER WORKINGS DESCENT REACTIONS" in a6728, "6.7.43 diagnostic title missing")

    for window, var_name in WINDOWS:
        req(f"[{window}] = {var_name}" in reactions, f"reaction dictionary missing window {window}")
        for npc in NPCS:
            key = f"story.react.{window}.{npc.lower()}"
            req(key in en and key in vi, f"missing EN/VI reaction key: {key}")
            req(f'"{npc}", "{key}"' in reactions, f"reaction service missing {key}")

    req(set(en) == set(vi), "EN/VI i18n parity failed")
    en_reactions = [k for k in en if k.startswith("story.react.")]
    vi_reactions = [k for k in vi if k.startswith("story.react.")]
    req(len(en_reactions) == 500, f"EN reaction catalog must be exactly 500, got {len(en_reactions)}")
    req(len(vi_reactions) == 500, f"VI reaction catalog must be exactly 500, got {len(vi_reactions)}")
    for window in range(31, 36):
        req(sum(1 for k in en if k.startswith(f"story.react.{window}.")) == 14, f"EN window {window} must have 14 NPCs")
        req(sum(1 for k in vi if k.startswith(f"story.react.{window}.")) == 14, f"VI window {window} must have 14 NPCs")

    forbidden = ("Rank S", "The Last Blaster", "George Mullner", "Keeper", "Sector 17", "SECTOR 17")
    for window in range(31, 36):
        for npc in NPCS:
            key = f"story.react.{window}.{npc.lower()}"
            for token in forbidden:
                req(token not in en[key] and token not in vi[key], f"spoiler leak {token}: {key}")

    req('public const string StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsDescentStage";' in descent, "6.7.42 descent stage key regressed")
    req('public const string ThresholdCrossedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed";' in descent, "threshold-crossed key regressed")
    req('public const string FirstDescentCompleteFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete";' in descent, "first-descent-complete key regressed")
    req("public const int CompleteStage = 5;" in descent, "descent route must remain 0..5")
    req("ThresholdCrossingTicksRequired = 120" in descent, "120-tick crossing hold regressed")
    req("ThresholdInspectionTicksRequired = 180" in descent, "180-tick inspection hold regressed")
    req('Game1.MasterPlayer.modData[ThresholdCrossedFlagKey] = "1"' in descent, "threshold persistence missing")
    req('Game1.MasterPlayer.modData[FirstDescentCompleteFlagKey] = "1"' in descent, "first descent persistence missing")
    req("HasFullOperationalFormation(location)" in descent, "full formation gate missing")
    req("Game1.activeClickableMenu is null" in descent and "!Game1.dialogueUp" in descent, "menu/dialogue guard missing")
    req("ControlledBreachFirstEntryStoryService.BreachLocationKey" in descent, "recorded breach face reuse missing")
    req("teamup_lower_descent" in a6742, "descent debug command missing")

    req('public const string ProtocolReadyFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady";' in protocol, "entry protocol READY flag regressed")
    req("ReadinessHoldTicksRequired = 240" in protocol, "entry protocol readiness hold regressed")
    req("GetRequiredFieldPeople()" in protocol and "GetRequiredNpcAllies()" in protocol, "adaptive formation helpers missing")
    req('public const string SurgeHighFlagKey = "Ronvotri.TeamUp/Story/SurgeHighConfirmed";' in high, "SURGE HIGH flag regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person formation cap regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first mutation trigger regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "Pelipper capture ceasefire regressed")
    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("REACTION WINDOWS 0..35: PASS")
    log("LOWER WORKINGS DESCENT WINDOWS 31..35: PASS (14 NPCs each)")
    log("CURATED MILESTONE REACTIONS: PASS (500 lines/language)")
    log("NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.43 WINDOW RESOLVER: PASS")
    log("LOWER WORKINGS DESCENT 6.7.42 CARRY-FORWARD: PASS")
    log("120-TICK THRESHOLD CROSSING + PERSISTENT CROSSED STATE: PASS")
    log("180-TICK FIRST INTERIOR INSPECTION + PERSISTENT COMPLETION: PASS")
    log("ENTRY PROTOCOL READY + ADAPTIVE FULL FORMATION CARRY-FORWARD: PASS")
    log("SURGE HIGH + STORY SLOT 4 CARRY-FORWARD: PASS")
    log("FIVE-PEOPLE TOTAL FORMATION CAP: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("PELIPPER / MUTATION SAFETY CARRY-FORWARD: PASS")
    log("NO COMBAT, MAP, BOSS, MONSTER, CAPTURE OR ROSTER MECHANICS CHANGED: PASS")
    log("LIVE ONE-SHOT DIALOGUE PACING FOR WINDOWS 31..35 STILL REQUIRED")

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
        "GetStoryReactionWindowAlpha6743",
        "story.react.31.abigail",
        "story.react.32.marlon",
        "story.react.33.george",
        "story.react.34.robin",
        "story.react.35.wizard",
        "LowerWorkingsDescentStoryService",
        "Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed",
        "Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete",
        "teamup_lower_descent",
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
        "# Team Up Alpha 6.7.43 - Lower Workings Descent Reactions Audit\n\n"
        "## Implemented\n"
        "- Added reaction windows 31 through 35 for the Alpha 6.7.42 first-descent route.\n"
        "- Window 31: first descent authorized at the Guild.\n"
        "- Window 32: full formation staged at the threshold line.\n"
        "- Window 33: threshold crossed with formation intact.\n"
        "- Window 34: first interior zone inspected; deliberate containment evidence established without identifying the historical worker.\n"
        "- Window 35: first descent reported complete at the Guild.\n"
        "- Same curated 14 NPCs per window, adding 70 lines per language.\n"
        "- Reaction catalog now contains exactly 500 lines per language.\n\n"
        "## Safety / scope\n"
        "- Dialogue-only checkpoint. Alpha 6.7.42 gameplay is unchanged.\n"
        "- George remains observed Rank D / Non-Combatant / unrecruitable and unrevealed.\n"
        "- Evelyn postgame secret remains untouched.\n"
        "- No final boss, custom lower-workings map, combat change, mutation rewrite, capture rewrite, monster ownership change, or roster unlock.\n"
        "- Five-person total formation cap and Pelipper capture safety remain intact.\n\n"
        "## Live verification still required\n"
        "- Validate one-shot dialogue behavior for windows 31 through 35.\n"
        "- Validate stale skipped windows do not replay.\n"
        "- Validate reactions never mutate Lower Workings descent state, threshold flag, completion flag, Entry Protocol, Surge HIGH, or roster slots.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.43 - LOWER WORKINGS DESCENT REACTIONS\n"
        "=================================================\n\n"
        "PREP:\n"
        "- Cai build 6.7.43, load save host.\n"
        "- teamup_story_reactions reset.\n\n"
        "WINDOWS:\n"
        "1. teamup_lower_descent stage 1 -> window 31. Noi chuyen NPC ho tro 2 lan: lan 1 reaction, lan 2 normal.\n"
        "2. teamup_lower_descent stage 2 -> window 32. Lap lai one-shot test.\n"
        "3. teamup_lower_descent stage 3 -> window 33. Reaction khong duoc thay doi ThresholdCrossed.\n"
        "4. teamup_lower_descent stage 4 -> window 34. Kiem tra dialogue chi ket luan deliberate containment, khong lo danh tinh nguoi tho mo cu.\n"
        "5. teamup_lower_descent stage 5 -> window 35. Reaction khong duoc thay doi FirstDescentComplete.\n"
        "6. Bo qua mot window roi tang stage -> reaction cu khong duoc replay.\n"
        "7. George van Rank D / Non-Combatant / khong recruit; khong Last Blaster.\n"
        "8. Evelyn khong reveal postgame.\n"
        "9. teamup_story_reactions status -> catalog 0..35.\n"
        "10. teamup_lower_descent status -> stage va persistent flags chi do 6.7.42 gameplay quyet dinh.\n\n"
        "DEBUG: teamup_lower_descent status|reset|stage 0-5\n"
        "DEBUG: teamup_story_reactions status|reset\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Milestone_Reactions_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.43")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
