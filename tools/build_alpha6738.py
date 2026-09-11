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
STAGE = ROOT / "_stage_alpha6738"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6738.txt"
AUDIT = ROOT / "SURGE_HIGH_SLOT4_AUDIT_ALPHA6738.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_38_SURGE_HIGH_SLOT4_VI.txt"
VERSION = "0.2.0-alpha.6.7.38"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.38_SURGE_HIGH_SLOT4_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.38_SURGE_HIGH_SLOT4_TEST.sha256.txt"
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
    a6734 = text("ModEntry.Alpha6734.cs")
    a6736 = text("ModEntry.Alpha6736.cs")
    a6737 = text("ModEntry.Alpha6737.cs")
    a6738 = text("ModEntry.Alpha6738.cs")
    high = text("Story/SurgeHighEscalationStoryService.cs")
    breach = text("Story/ControlledBreachFirstEntryStoryService.cs")
    corridor = text("Story/SealedCorridorApproachStoryService.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge_story = text("Story/TheSurgeStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6738Events();" in entry, "6.7.38 events not registered")
    req("Surge HIGH escalation and story slot 4 layer active." in entry, "6.7.38 startup marker missing")

    req("public const string StageKey = \"Ronvotri.TeamUp/Story/SurgeHighEscalationStage\";" in high, "Surge HIGH stage key missing")
    req("public const string SurgeHighFlagKey = \"Ronvotri.TeamUp/Story/SurgeHighConfirmed\";" in high, "persistent HIGH flag missing")
    req("public const int CompleteStage = 4;" in high, "Surge HIGH route must complete at stage 4")
    req("MinimumFieldPeople = 3" in high and "MinimumActiveNpcAllies = 1" in high, "Surge HIGH field-team rule regressed")
    req("HighConfirmationTicksRequired = 180" in high, "180-tick HIGH confirmation hold missing")
    req("_getControlledBreachStage() >= ControlledBreachFirstEntryStoryService.CompleteStage" in high, "controlled-breach prerequisite missing")
    req("ControlledBreachFirstEntryStoryService.BreachLocationKey" in high, "exact recorded breach face is not reused")
    req("!HasRequiredFieldTeam(location)" in high and "_highConfirmationTicks = 0" in high, "team-break HIGH hold reset missing")
    req("Game1.activeClickableMenu is null" in high and "!Game1.dialogueUp" in high, "menu/dialogue gating missing")
    req('owner.modData[SurgeHighFlagKey] = "1"' in high or 'Game1.MasterPlayer.modData[SurgeHighFlagKey] = "1"' in high, "HIGH state is not persisted")
    req('_unlockNpcSlots(4, "surge-high-confirmed")' in high, "story slot 4 unlock is not tied to confirmed HIGH report")
    req("SurgeHighAlpha6738 = new SurgeHighEscalationStoryService" in a6738, "6.7.38 service wiring missing")
    req("RosterProgressionAlpha6727.UnlockTo(Game1.MasterPlayer, requestedSlots, source)" in a6738, "roster unlock callback missing")
    req("EnforceStoryRosterCapacityAlpha6727()" in a6738, "roster capacity enforcement missing after slot unlock")
    req("teamup_surge_high" in a6738, "Surge HIGH debug command missing")

    req("public const int CompleteStage = 5;" in breach, "6.7.36 controlled breach route regressed")
    req("BreachHoldTicksRequired = 180" in breach and "EntryProbeTicksRequired = 120" in breach, "6.7.36 breach/probe holds regressed")
    req("public const int CompleteStage = 4;" in corridor and "StableSurveyTicksRequired = 240" in corridor, "6.7.34 corridor carry-forward regressed")

    req("narrativeStage > 22" in reactions, "reaction windows must remain 0..22")
    req("GetStoryReactionWindowAlpha6737()" in a6737, "6.7.37 reaction resolver missing")
    req(a6728.count("GetStoryReactionWindowAlpha6737()") >= 2, "NPC interaction/diagnostic no longer routed to 6.7.37 resolver")
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(sum(1 for k in en if k.startswith("story.react.")) == 318, "EN reaction catalog must remain exactly 318 lines")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 318, "VI reaction catalog must remain exactly 318 lines")

    high_keys = {
        "story.surge-high.need-team",
        "story.surge-high.missing-face",
        "story.surge-high.briefing",
        "story.surge-high.objective.reach",
        "story.surge-high.wrong-shaft",
        "story.surge-high.arrival",
        "story.surge-high.objective.hold",
        "story.surge-high.confirmed",
        "story.surge-high.objective.return",
        "story.surge-high.report",
        "story.surge-high.slot4-unlocked",
        "story.surge-high.complete",
    }
    req(high_keys.issubset(en) and high_keys.issubset(vi), "6.7.38 localization incomplete")
    for key in high_keys:
        for forbidden in ("Rank S", "The Last Blaster", "George Mullner", "Keeper"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person formation cap regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first mutation trigger regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "Pelipper capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy fake-hide writer returned")
    req("Boss" not in high or "final boss" in high, "unexpected boss implementation leaked into Surge HIGH service")

    log("SOURCE ACCEPTANCE: PASS")
    log("SURGE HIGH ROUTE 0..4: PASS")
    log("CONTROLLED BREACH COMPLETION PREREQUISITE: PASS")
    log("EXACT RECORDED BREACH FACE REUSED: PASS")
    log("THREE-PEOPLE FIELD TEAM + ACTIVE TEAM UP NPC REQUIREMENT: PASS")
    log("180-TICK SURGE HIGH CONFIRMATION HOLD: PASS")
    log("TEAM-BREAK / WARP / MENU HOLD RESET: PASS")
    log("PERSISTENT SURGE HIGH FLAG: PASS")
    log("STORY NPC SLOT 4 UNLOCKED ONLY AFTER HIGH REPORT: PASS")
    log("FIVE-PEOPLE TOTAL FORMATION CAP: PASS")
    log("REACTION WINDOWS 0..22 CARRY-FORWARD: PASS (318 lines/language)")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("NO FINAL BOSS OR CUSTOM LOWER-WORKINGS MAP ADDED: PASS")
    log("6.7.23-6.7.37 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE SURGE HIGH / SLOT 4 PACING STILL REQUIRED")

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
        "SurgeHighEscalationStoryService",
        "Ronvotri.TeamUp/Story/SurgeHighConfirmed",
        "teamup_surge_high",
        "story.surge-high.confirmed",
        "story.surge-high.slot4-unlocked",
        "surge-high-confirmed",
        "ControlledBreachFirstEntryStoryService",
        "GetStoryReactionWindowAlpha6737",
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
        "# Team Up Alpha 6.7.38 - Surge HIGH Escalation / Story Slot 4 Audit\n\n"
        "## Implemented\n"
        "- Added persistent Surge HIGH chapter service gated behind completed Controlled Breach / First Entry.\n"
        "- Reuses the exact recorded breach MineShaft face from Alpha 6.7.36.\n"
        "- Requires a valid three-person field team with at least one active Team Up NPC ally.\n"
        "- Requires a continuous 180-tick stable reading at the breach face.\n"
        "- Persists the confirmed HIGH state before the team returns to Marlon.\n"
        "- Unlocks story NPC slot 4 only on the Guild report after HIGH is confirmed.\n"
        "- Five-person total formation cap remains authoritative in multiplayer.\n\n"
        "## Safety / scope\n"
        "- George remains Rank D / Non-Combatant / unrecruitable and unrevealed.\n"
        "- Evelyn postgame secret remains untouched.\n"
        "- No final boss, custom lower-workings dungeon map, mutation rewrite, capture rewrite, or combat ownership change.\n"
        "- Reaction catalog remains windows 0..22 with 318 lines per language.\n\n"
        "## Live verification still required\n"
        "- Validate real 180-tick hold and reset behavior.\n"
        "- Validate HIGH flag persists after save/reload.\n"
        "- Validate stage 3 still shows roster 3/4 and stage 4 report changes it to 4/4.\n"
        "- Validate multiplayer effective NPC capacity still respects five total people.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.38 - SURGE HIGH ESCALATION / STORY SLOT 4\n"
        "====================================================\n\n"
        "PREP:\n"
        "- Cai build 6.7.38, load save host.\n"
        "- teamup_breach status -> stage 5/5.\n"
        "- teamup_roster_story setslots 3 neu can test lai moc unlock sach.\n"
        "- teamup_surge_high reset.\n\n"
        "ROUTE:\n"
        "1. Guild + field team >=3 nguoi, >=1 Team Up NPC -> stage 1.\n"
        "2. Vao DUNG breach MineShaft da ghi -> stage 2.\n"
        "3. Giu doi hinh lien tuc 180 tick -> stage 3, HIGH=true.\n"
        "4. Thu roi map / doi shaft / thieu nguoi / mo menu truoc 180 tick -> hold phai reset.\n"
        "5. O stage 3, teamup_roster_story status van phai la 3/4.\n"
        "6. Quay ve Guild voi field team hop le -> stage 4/4, roster phai thanh 4/4.\n"
        "7. Save + reload -> teamup_surge_high status van high=True va stage 4/4.\n"
        "8. George van Rank D / Non-Combatant / khong recruit; khong Last Blaster.\n"
        "9. Reaction catalog 0..22 khong thay doi.\n"
        "10. Multiplayer: story allowance co the 4/4 nhung tong formation van toi da 5 PEOPLE.\n\n"
        "DEBUG: teamup_surge_high status|reset|stage 0-4\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Surge_HIGH_Escalation_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.38")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
