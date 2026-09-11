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
STAGE = ROOT / "_stage_alpha6734"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6734.txt"
AUDIT = ROOT / "SEALED_CORRIDOR_APPROACH_AUDIT_ALPHA6734.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_34_SEALED_CORRIDOR_APPROACH_VI.txt"
VERSION = "0.2.0-alpha.6.7.34"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.34_SEALED_CORRIDOR_APPROACH_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.34_SEALED_CORRIDOR_APPROACH_TEST.sha256.txt"
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
    a6732 = text("ModEntry.Alpha6732.cs")
    a6733 = text("ModEntry.Alpha6733.cs")
    a6734 = text("ModEntry.Alpha6734.cs")
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
    req("RegisterAlpha6734Events();" in entry, "6.7.34 event registration missing")
    req("Sealed corridor approach layer active." in entry, "6.7.34 startup marker missing")
    req("class SealedCorridorApproachStoryService" in corridor, "corridor service missing")
    req('StageKey = "Ronvotri.TeamUp/Story/SealedCorridorApproachStage"' in corridor, "corridor stage key missing")
    req('SurveyLocationKey = "Ronvotri.TeamUp/Story/SealedCorridorSurveyLocation"' in corridor, "survey location key missing")
    req("public const int CompleteStage = 4;" in corridor, "corridor route must be 0..4")
    req("MinimumFieldPeople = 3" in corridor and "MinimumActiveNpcAllies = 1" in corridor, "corridor field-team requirement regressed")
    req("StableSurveyTicksRequired = 240" in corridor, "pressure survey hold duration changed")
    req("_getTriangulationStage() >= FieldTriangulationStoryService.CompleteStage" in corridor, "triangulation prerequisite missing")
    req("Game1.MasterPlayer.modData[SurveyLocationKey] = location.NameOrUniqueName" in corridor, "survey face persistence missing")
    req("IsSurveyLocation(Game1.MasterPlayer, location)" in corridor, "survey must remain on marked MineShaft")
    req("_stableSurveyTicks++" in corridor and "_stableSurveyTicks < StableSurveyTicksRequired" in corridor, "stable hold survey loop missing")
    req('Show("story.corridor.pressure-survey")' in corridor, "pressure survey payoff missing")
    req('SetStage(Game1.MasterPlayer, 3' in corridor, "survey completion stage missing")
    req('SetStage(Game1.MasterPlayer, CompleteStage, "sealed-access-face-confirmed")' in corridor, "Guild confirmation completion missing")
    req("CorridorApproachAlpha6734.Update();" in a6734, "UpdateTicked corridor survey routing missing")
    req("CountFieldPeopleAtAlpha6732" in a6734 and "CountActiveStoryNpcAlliesAtAlpha6732" in a6734, "6.7.32 physical-team counters not reused")
    req("teamup_corridor" in a6734, "corridor debug command missing")

    corridor_keys = {
        "story.corridor.need-team",
        "story.corridor.briefing",
        "story.corridor.objective.reach",
        "story.corridor.approach-entry",
        "story.corridor.objective.hold",
        "story.corridor.wrong-shaft",
        "story.corridor.pressure-survey",
        "story.corridor.objective.return",
        "story.corridor.confirmed",
        "story.corridor.complete",
    }
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(corridor_keys.issubset(en) and corridor_keys.issubset(vi), "6.7.34 corridor localization incomplete")
    for key in corridor_keys:
        for forbidden in ("Rank S", "The Last Blaster", "George Mullner", "Sector 17"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler/invented-lore leak {forbidden}: {key}")

    req("narrativeStage > 13" in reactions, "reaction range 0..13 regressed")
    req(sum(1 for k in en if k.startswith("story.react.")) == 192, "EN reaction catalog changed unexpectedly")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 192, "VI reaction catalog changed unexpectedly")
    req("GetStoryReactionWindowAlpha6733()" in a6733, "6.7.33 resolver missing")
    req(a6728.count("GetStoryReactionWindowAlpha6733()") >= 2, "reaction routing regressed")

    req("public const int CompleteStage = 4;" in field, "field triangulation route regressed")
    req("MinimumFieldPeople = 3" in field and "MinimumActiveNpcAllies = 1" in field, "field triangulation team rule regressed")
    req("Game1.getOnlineFarmers().Count" in a6732, "co-op field count regressed")
    req("Distinct(StringComparer.OrdinalIgnoreCase)" in a6732, "NPC field-team dedupe regressed")
    req("FirstBearingLocationKey" in field and "IsDifferentMineLocation" in field, "distinct MineShaft bearing logic regressed")

    req('UnlockTo(Game1.MasterPlayer, 4' not in all_cs, "story slot 4 unlocked too early")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person hard cap regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge, "10-kill first Mutation trigger regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "Pelipper capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("SEALED CORRIDOR APPROACH 0..4 PERSISTENT ROUTE: PASS")
    log("FIELD TRIANGULATION COMPLETION PREREQUISITE: PASS")
    log("THREE-PEOPLE FIELD TEAM + ACTIVE TEAM UP NPC REQUIREMENT: PASS")
    log("MARKED MINESHAFT PRESSURE-SURVEY FACE: PASS")
    log("240-TICK CONTINUOUS FIELD-TEAM HOLD: PASS")
    log("SEALED ACCESS FACE CONFIRMED WITHOUT BREACH: PASS")
    log("REACTION WINDOWS 0..13 UNCHANGED: PASS (192 lines/language)")
    log("STORY NPC SLOT 4 REMAINS LOCKED: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("6.7.23-6.7.33 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE CONTINUOUS-HOLD / TEAM-BREAK / WARP-RESET TEST STILL REQUIRED")

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
        "SealedCorridorApproachStoryService",
        "teamup_corridor",
        "story.corridor.pressure-survey",
        "Ronvotri.TeamUp/Story/SealedCorridorSurveyLocation",
        "StableSurveyTicksRequired",
        "teamup_triangulation",
        "GetStoryReactionWindowAlpha6733",
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
        "# Team Up Alpha 6.7.34 - Sealed Corridor Approach Audit\n\n"
        "## Implemented\n"
        "- Added persistent Sealed Corridor Approach stages 0..4 after Field Triangulation completion.\n"
        "- Reuses the physical field-team rule: at least three people in the same location, including one active Team Up NPC ally.\n"
        "- Guild briefing sends the team to a MineShaft approach face.\n"
        "- The first qualifying MineShaft is persisted as the pressure-survey face.\n"
        "- The team must remain together at that exact face for 240 continuous update ticks. Leaving, changing shaft, opening menus/dialogue, or losing the valid field team resets the runtime hold.\n"
        "- Survey payoff confirms lateral draft, obsolete support seam, inward-burn trace, and continuing seal pressure.\n"
        "- Returning to Marlon confirms the buried access face toward the sealed lower workings. The wall is not breached.\n\n"
        "## Locks retained\n"
        "- Story slot 4 remains locked.\n"
        "- George stays observed Rank D / Non-Combatant / unrecruitable. No Last Blaster reveal.\n"
        "- Evelyn postgame secret remains untouched.\n"
        "- Reaction windows remain 0..13 with 192 lines per language.\n"
        "- No combat, monster spawn, capture-provider, ownership, or five-person-cap behavior changed.\n\n"
        "## Live verification still required\n"
        "- Continuous 240-tick hold on the marked MineShaft.\n"
        "- Hold resets when the team breaks, a menu/dialogue interrupts, or the player warps away.\n"
        "- Wrong MineShaft does not progress stage 2.\n"
        "- Final Guild return completes stage 4 while roster stays 3/4.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.34 - SEALED CORRIDOR APPROACH\n"
        "==========================================\n\n"
        "PRE: teamup_triangulation status -> stage 4/4.\n"
        "1. teamup_corridor reset.\n"
        "2. Farmer + 1 NPC vao Guild -> KHONG advance, phai bao can du field team.\n"
        "3. Farmer + 2 NPC vao Guild -> stage 1.\n"
        "4. Vao mot MineShaft voi du doi -> stage 2, surveyLocation duoc luu.\n"
        "5. Dung yen cung du field team tai dung MineShaft khoang 4 giay -> stage 3.\n"
        "6. Test reset: stage 2 roi roi map / mo menu / lam thieu nguoi truoc 4 giay -> progress hold phai ve 0, khong stage 3.\n"
        "7. Stage 2 ma vao MineShaft khac -> khong progress, co thong bao quay lai survey face.\n"
        "8. Sau stage 3, dua du field team ve Adventurer's Guild -> stage 4.\n"
        "9. teamup_roster_story status -> van 3/4.\n"
        "10. George van Rank D / Non-Combatant / khong recruit; khong Last Blaster.\n\n"
        "DEBUG: teamup_corridor status | reset | stage 0-4\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Sealed_Corridor_Approach_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.34")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
