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
STAGE = ROOT / "_stage_alpha6735"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6735.txt"
AUDIT = ROOT / "SEALED_CORRIDOR_REACTIONS_AUDIT_ALPHA6735.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_35_SEALED_CORRIDOR_REACTIONS_VI.txt"
VERSION = "0.2.0-alpha.6.7.35"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.35_SEALED_CORRIDOR_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.35_SEALED_CORRIDOR_REACTIONS_TEST.sha256.txt"
NPCS = ["abigail","alex","clint","demetrius","evelyn","george","gus","lewis","linus","marlon","maru","pierre","robin","wizard"]
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
    a6733 = text("ModEntry.Alpha6733.cs")
    a6734 = text("ModEntry.Alpha6734.cs")
    a6735 = text("ModEntry.Alpha6735.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    field = text("Story/FieldTriangulationStoryService.cs")
    corridor = text("Story/SealedCorridorApproachStoryService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge = text("Story/TheSurgeStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("narrativeStage > 17" in reactions, "reaction range must be 0..17")
    for w, name in [(14,"pressureSurveyBriefed"),(15,"surveyFaceMarked"),(16,"pressureSurveyComplete"),(17,"sealedAccessFaceConfirmed")]:
        req(f"[{w}] = {name}" in reactions, f"reaction window {w} missing")

    req("GetStoryReactionWindowAlpha6735()" in a6735, "6.7.35 resolver missing")
    req("CorridorApproachAlpha6734.Stage switch" in a6735, "corridor stage resolver missing")
    req("FieldTriangulationAlpha6732.Stage < FieldTriangulationStoryService.CompleteStage" in a6735, "triangulation fallback missing")
    req(a6728.count("GetStoryReactionWindowAlpha6735()") >= 2, "NPC interaction/diagnostic not routed to 6.7.35 resolver")

    new_keys = {f"story.react.{w}.{npc}" for w in range(14, 18) for npc in NPCS}
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(new_keys.issubset(en) and new_keys.issubset(vi), "6.7.35 reaction localization incomplete")
    for w in range(14, 18):
        req(sum(1 for k in en if k.startswith(f"story.react.{w}.")) == 14, f"EN window {w} must contain 14 NPCs")
        req(sum(1 for k in vi if k.startswith(f"story.react.{w}.")) == 14, f"VI window {w} must contain 14 NPCs")
    req(sum(1 for k in en if k.startswith("story.react.")) == 248, "EN reaction catalog must contain 248 lines")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 248, "VI reaction catalog must contain 248 lines")

    for key in new_keys:
        for forbidden in ("Rank S", "The Last Blaster", "George Mullner", "Keeper"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")

    req("public const int CompleteStage = 4;" in corridor, "6.7.34 corridor route regressed")
    req("StableSurveyTicksRequired = 240" in corridor, "240-tick pressure survey regressed")
    req("SurveyLocationKey" in corridor and "IsSurveyLocation" in corridor, "survey-face persistence regressed")
    req("!HasRequiredFieldTeam(location)" in corridor and "_stableSurveyTicks = 0" in corridor, "team-break reset regressed")
    req("RegisterAlpha6734Events();" in entry, "6.7.34 corridor service not registered")
    req("MinimumFieldPeople = 3" in corridor and "MinimumActiveNpcAllies = 1" in corridor, "corridor field-team rule regressed")

    req("public const int CompleteStage = 4;" in field, "6.7.32 field route regressed")
    req("Game1.getOnlineFarmers().Count" in a6732, "co-op field count regressed")
    req('UnlockTo(Game1.MasterPlayer, 4' not in all_cs, "story slot 4 unlocked too early")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person cap regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge, "10-kill trigger regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("REACTION WINDOWS 0..17: PASS")
    log("SEALED CORRIDOR WINDOWS 14/15/16/17: PASS (14 NPCs each)")
    log("CURATED MILESTONE REACTIONS: PASS (248 lines/language)")
    log("NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.35 WINDOW RESOLVER: PASS")
    log("SEALED CORRIDOR APPROACH 6.7.34 CARRY-FORWARD: PASS")
    log("240-TICK CONTINUOUS PRESSURE SURVEY CARRY-FORWARD: PASS")
    log("STORY NPC SLOT 4 REMAINS LOCKED: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("6.7.23-6.7.34 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE ONE-SHOT DIALOGUE PACING FOR WINDOWS 14..17 STILL REQUIRED")

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
        "GetStoryReactionWindowAlpha6735",
        "story.react.14.george",
        "story.react.15.marlon",
        "story.react.16.robin",
        "story.react.17.evelyn",
        "teamup_story_reactions",
        "teamup_corridor",
        "SealedCorridorApproachStoryService",
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
        "# Team Up Alpha 6.7.35 - Sealed Corridor Approach Reactions Audit\n\n"
        "## Implemented\n"
        "- Expanded one-shot milestone reactions from windows 0..13 to 0..17.\n"
        "- Window 14: Marlon pressure-survey briefing.\n"
        "- Window 15: sealed-corridor survey face marked.\n"
        "- Window 16: continuous pressure survey complete.\n"
        "- Window 17: sealed access face confirmed and mapped.\n"
        "- Added 56 curated lines per language, total 248 reaction lines per language.\n"
        "- George remains ordinary pre-reveal miner knowledge only; Evelyn remains ordinary elder/support.\n\n"
        "## Safety\n"
        "- Dialogue-only checkpoint. No combat, monster, capture, map ownership, breach, boss, or roster-cap behavior changed.\n"
        "- Slot 4 remains locked. The sealed wall remains unbreached.\n\n"
        "## Live verification still required\n"
        "- One-shot interaction timing for windows 14..17.\n"
        "- No stale reaction replay after corridor-stage advancement.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.35 - SEALED CORRIDOR APPROACH REACTIONS\n"
        "===================================================\n\n"
        "1. teamup_corridor stage 1 -> reaction window 14.\n"
        "2. Noi chuyen NPC ho tro -> moi NPC chi co 1 reaction window 14.\n"
        "3. stage 2 -> window 15; stage 3 -> window 16; stage 4 -> window 17.\n"
        "4. Noi lai cung NPC trong cung window -> quay ve tuong tac binh thuong.\n"
        "5. Bo qua window cu roi advance -> khong replay reaction cu.\n"
        "6. George van Rank D / Non-Combatant / khong recruit; khong Last Blaster.\n"
        "7. Evelyn khong lo secret postgame.\n"
        "8. teamup_roster_story status -> van 3/4; slot 4 chua mo.\n"
        "DEBUG: teamup_story_reactions reset | status\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Milestone_Reactions_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.35")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
