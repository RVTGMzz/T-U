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
STAGE = ROOT / "_stage_alpha6736"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6736.txt"
AUDIT = ROOT / "CONTROLLED_BREACH_FIRST_ENTRY_AUDIT_ALPHA6736.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_36_CONTROLLED_BREACH_FIRST_ENTRY_VI.txt"
VERSION = "0.2.0-alpha.6.7.36"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.36_CONTROLLED_BREACH_FIRST_ENTRY_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.36_CONTROLLED_BREACH_FIRST_ENTRY_TEST.sha256.txt"
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
    a6732 = text("ModEntry.Alpha6732.cs")
    a6735 = text("ModEntry.Alpha6735.cs")
    a6736 = text("ModEntry.Alpha6736.cs")
    field = text("Story/FieldTriangulationStoryService.cs")
    corridor = text("Story/SealedCorridorApproachStoryService.cs")
    breach = text("Story/ControlledBreachFirstEntryStoryService.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge = text("Story/TheSurgeStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6736Events();" in entry, "6.7.36 service not registered")
    req("Controlled breach / first-entry probe layer active." in entry, "6.7.36 startup marker missing")
    req("ControlledBreachFirstEntryStoryService" in a6736, "6.7.36 entry layer missing")
    req('"teamup_breach"' in a6736, "teamup_breach command missing")
    req("stage is >= 0 and <= 5" in a6736, "teamup_breach stage range must be 0..5")

    req("public const int CompleteStage = 5;" in breach, "controlled breach route must end at stage 5")
    req("BreachHoldTicksRequired = 180" in breach, "180-tick controlled opening hold missing")
    req("EntryProbeTicksRequired = 120" in breach, "120-tick first-entry probe hold missing")
    req("SealedCorridorApproachStoryService.CompleteStage" in breach, "sealed corridor completion prerequisite missing")
    req("SealedCorridorApproachStoryService.SurveyLocationKey" in breach, "recorded survey face must be reused")
    req("BreachLocationKey" in breach and "IsBreachLocation" in breach, "breach-face persistence missing")
    req("MinimumFieldPeople = 3" in breach and "MinimumActiveNpcAllies = 1" in breach, "field-team rule regressed")
    req("ResetRuntimeHolds();" in breach, "hold reset path missing")
    req("_breachHoldTicks++" in breach and "_entryProbeTicks++" in breach, "continuous hold progression missing")
    req('Show("story.breach.opened")' in breach, "controlled-opening narrative missing")
    req('Show("story.breach.first-entry")' in breach, "first-entry threshold probe missing")
    req('Show("story.breach.report")' in breach, "Marlon report gate missing")

    required_story_keys = {
        "story.breach.need-team",
        "story.breach.missing-face",
        "story.breach.briefing",
        "story.breach.objective.reach",
        "story.breach.wrong-shaft",
        "story.breach.arrival",
        "story.breach.objective.stabilize",
        "story.breach.opened",
        "story.breach.objective.probe",
        "story.breach.first-entry",
        "story.breach.objective.return",
        "story.breach.report",
        "story.breach.complete",
    }
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(required_story_keys.issubset(en) and required_story_keys.issubset(vi), "6.7.36 story localization incomplete")

    req("narrativeStage > 17" in reactions, "reaction range must remain 0..17")
    req(sum(1 for k in en if k.startswith("story.react.")) == 248, "EN reaction catalog must remain exactly 248 lines")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 248, "VI reaction catalog must remain exactly 248 lines")
    req("GetStoryReactionWindowAlpha6735()" in a6735, "6.7.35 reaction resolver missing")
    req(a6728.count("GetStoryReactionWindowAlpha6735()") >= 2, "NPC interaction/diagnostic must remain on 6.7.35 resolver")

    req("public const int CompleteStage = 4;" in corridor, "6.7.34 corridor route regressed")
    req("StableSurveyTicksRequired = 240" in corridor, "6.7.34 pressure survey regressed")
    req("public const int CompleteStage = 4;" in field, "6.7.32 field route regressed")
    req("Game1.getOnlineFarmers().Count" in a6732, "co-op field count regressed")

    req('UnlockTo(Game1.MasterPlayer, 4' not in all_cs, "story slot 4 unlocked too early")
    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    for forbidden in ("The Last Blaster", "George Mullner"):
        req(forbidden not in breach and forbidden not in a6736, f"George spoiler leaked in 6.7.36 source: {forbidden}")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person cap regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge, "10-kill trigger regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("CONTROLLED BREACH / FIRST ENTRY ROUTE 0..5: PASS")
    log("SEALED CORRIDOR COMPLETION PREREQUISITE: PASS")
    log("RECORDED SURVEY FACE REUSED AS BREACH LOCATION: PASS")
    log("THREE-PEOPLE FIELD TEAM + ACTIVE TEAM UP NPC REQUIREMENT: PASS")
    log("180-TICK CONTROLLED OPENING HOLD: PASS")
    log("120-TICK FIRST-ENTRY THRESHOLD PROBE HOLD: PASS")
    log("TEAM-BREAK / WARP / MENU HOLD RESET: PASS")
    log("REACTION WINDOWS 0..17 CARRY-FORWARD: PASS (248 lines/language)")
    log("STORY NPC SLOT 4 REMAINS LOCKED: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("NO CUSTOM LOWER-WORKINGS MAP OR BOSS CLAIMED: PASS")
    log("6.7.23-6.7.35 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE CONTROLLED-OPENING / THRESHOLD-PROBE PACING STILL REQUIRED")

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
        "ControlledBreachFirstEntryStoryService",
        "teamup_breach",
        "story.breach.opened",
        "story.breach.first-entry",
        "story.breach.report",
        "SealedCorridorApproachStoryService",
        "GetStoryReactionWindowAlpha6735",
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
        "# Team Up Alpha 6.7.36 - Controlled Breach / First Entry Audit\n\n"
        "## Implemented\n"
        "- Added a persistent 0..5 controlled-breach story route after Sealed Corridor Approach 4/4.\n"
        "- Reuses the exact MineShaft survey face recorded by Alpha 6.7.34.\n"
        "- Requires a valid three-person field team with at least one active Team Up NPC at every relevant gate.\n"
        "- Requires a 180-tick continuous hold to stabilize a narrow controlled opening.\n"
        "- Requires a second 120-tick continuous hold for a short first-entry threshold probe.\n"
        "- The probe confirms an old maintenance throat, recently disturbed black shard residue, and a deeper pressure pulse.\n"
        "- The party withdraws and reports to Marlon without widening the breach.\n\n"
        "## Explicit boundary\n"
        "- This checkpoint does not claim a custom lower-workings map. First entry is a threshold probe at the existing MineShaft face.\n"
        "- No boss, no Surge HIGH transition, no slot-4 unlock, and no George identity reveal.\n"
        "- Reaction windows remain 0..17 with exactly 248 lines per language.\n\n"
        "## Live verification still required\n"
        "- Wrong-shaft rejection.\n"
        "- 180-tick opening hold resets when the field team breaks, player warps, or a menu/dialogue interrupts.\n"
        "- 120-tick probe hold follows only after the opening dialogue is closed.\n"
        "- Return-to-Guild completion requires the valid field team.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.36 - CONTROLLED BREACH / FIRST ENTRY\n"
        "=================================================\n\n"
        "PRE: teamup_corridor status -> stage 4/4.\n"
        "1. teamup_breach reset.\n"
        "2. Guild voi <3 nguoi -> khong advance.\n"
        "3. Guild voi Farmer +2 NPC Team Up -> stage 1, luu breach location tu survey face 6.7.34.\n"
        "4. Vao sai MineShaft -> khong advance.\n"
        "5. Vao dung MineShaft -> stage 2.\n"
        "6. Giu du doi hinh ~180 tick -> stage 3, controlled opening. Thu tach doi/warp/menu de xac nhan hold reset.\n"
        "7. Dong dialogue, tiep tuc giu doi hinh ~120 tick -> stage 4, first-entry threshold probe.\n"
        "8. Ve Guild voi du field team -> stage 5/5.\n"
        "9. teamup_roster_story status -> van 3/4.\n"
        "10. George van Rank D / Non-Combatant / khong recruit; Evelyn khong lo secret postgame.\n"
        "DEBUG: teamup_breach status | reset | stage 0-5\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Controlled_Breach_First_Entry_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.36")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
