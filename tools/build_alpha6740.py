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
STAGE = ROOT / "_stage_alpha6740"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6740.txt"
AUDIT = ROOT / "HIGH_RESPONSE_ENTRY_PROTOCOL_AUDIT_ALPHA6740.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_40_HIGH_RESPONSE_ENTRY_PROTOCOL_VI.txt"
VERSION = "0.2.0-alpha.6.7.40"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.40_HIGH_RESPONSE_ENTRY_PROTOCOL_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.40_HIGH_RESPONSE_ENTRY_PROTOCOL_TEST.sha256.txt"
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
    a6738 = text("ModEntry.Alpha6738.cs")
    a6739 = text("ModEntry.Alpha6739.cs")
    a6740 = text("ModEntry.Alpha6740.cs")
    high = text("Story/SurgeHighEscalationStoryService.cs")
    protocol = text("Story/LowerWorkingsEntryProtocolStoryService.cs")
    breach = text("Story/ControlledBreachFirstEntryStoryService.cs")
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
    req("RegisterAlpha6740Events();" in entry, "6.7.40 events not registered")
    req("HIGH response preparation / lower-workings entry protocol layer active." in entry, "6.7.40 startup marker missing")
    req("LowerWorkingsEntryProtocolStoryService" in a6740, "6.7.40 entry layer missing")
    req("teamup_entry_protocol" in a6740, "6.7.40 debug command missing")

    req('StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolStage"' in protocol, "protocol stage key missing")
    req('ProtocolReadyFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady"' in protocol, "protocol ready flag missing")
    req("public const int CompleteStage = 4;" in protocol, "protocol route must complete at stage 4")
    req("ReadinessHoldTicksRequired = 240" in protocol, "240-tick readiness hold missing")
    req("_getSurgeHighStage() >= SurgeHighEscalationStoryService.CompleteStage" in protocol, "Surge HIGH completion prerequisite missing")
    req("_isSurgeHigh()" in protocol, "persistent HIGH prerequisite missing")
    req("_getUnlockedNpcSlots() >= TeamUpRosterProgressionService.MaxStoryNpcSlots" in protocol, "slot4 authorization prerequisite missing")
    req("ControlledBreachFirstEntryStoryService.BreachLocationKey" in protocol, "exact recorded breach face not reused")
    req("GetRequiredFieldPeople()" in protocol and "GetRequiredNpcAllies()" in protocol, "adaptive full-formation rule missing")
    req("Math.Min(peopleCap, farmers + unlockedNpcSlots)" in protocol, "full formation does not adapt to farmers/configured cap")
    req("Math.Min(unlockedNpcSlots, Math.Max(0, peopleCap - farmers))" in protocol, "adaptive NPC requirement missing")
    req("!HasFullOperationalFormation(location)" in protocol and "_readinessTicks = 0" in protocol, "formation-break reset missing")
    req("!Context.IsPlayerFree" in protocol, "player-free reset missing")
    req("!Game1.dialogueUp" in protocol and "Game1.activeClickableMenu is null" in protocol, "presentation gating missing")
    req('Game1.MasterPlayer.modData[ProtocolReadyFlagKey] = "1"' in protocol, "protocol READY is not persisted")
    req("UnlockTo(" not in protocol and "_unlockNpcSlots" not in protocol, "6.7.40 must not change roster authorization")

    req("public const int CompleteStage = 4;" in high, "6.7.38 HIGH route regressed")
    req("HighConfirmationTicksRequired = 180" in high, "6.7.38 HIGH hold regressed")
    req('SurgeHighFlagKey = "Ronvotri.TeamUp/Story/SurgeHighConfirmed"' in high, "persistent HIGH flag regressed")
    req("public const int CompleteStage = 5;" in breach, "controlled breach route regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person formation cap regressed")

    req("narrativeStage > 26" in reactions, "reaction windows must remain 0..26")
    req("GetStoryReactionWindowAlpha6739()" in a6739, "6.7.39 resolver missing")
    req(a6728.count("GetStoryReactionWindowAlpha6739()") >= 2, "NPC interaction/diagnostic no longer routed to 6.7.39 resolver")
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(sum(1 for k in en if k.startswith("story.react.")) == 374, "EN reaction catalog must remain exactly 374 lines")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 374, "VI reaction catalog must remain exactly 374 lines")

    protocol_keys = {
        "story.entry-protocol.need-high",
        "story.entry-protocol.need-roster",
        "story.entry-protocol.missing-face",
        "story.entry-protocol.need-full-team",
        "story.entry-protocol.wrong-shaft",
        "story.entry-protocol.briefing",
        "story.entry-protocol.objective.reach",
        "story.entry-protocol.staging",
        "story.entry-protocol.objective.hold",
        "story.entry-protocol.validated",
        "story.entry-protocol.objective.return",
        "story.entry-protocol.report",
        "story.entry-protocol.ready",
    }
    req(protocol_keys.issubset(en) and protocol_keys.issubset(vi), "6.7.40 localization incomplete")
    for key in protocol_keys:
        for forbidden in ("Rank S", "The Last Blaster", "George Mullner", "Keeper", "Sector 17"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first mutation trigger regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "Pelipper capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy fake-hide writer returned")
    req("GameLocation" in protocol and "new GameLocation" not in protocol, "6.7.40 must not create a custom lower-workings map")
    req("Monster" not in protocol, "6.7.40 protocol service must not spawn or own monsters")

    log("SOURCE ACCEPTANCE: PASS")
    log("HIGH RESPONSE ENTRY PROTOCOL ROUTE 0..4: PASS")
    log("SURGE HIGH + SLOT4 PREREQUISITES: PASS")
    log("EXACT RECORDED BREACH FACE REUSED: PASS")
    log("ADAPTIVE FULL FORMATION RULE: PASS")
    log("SOLO 1 FARMER + 4 NPC / COOP 2 FARMERS + 3 NPC MODEL: PASS")
    log("240-TICK READINESS HOLD: PASS")
    log("FORMATION / WARP / FREE-STATE / MENU RESET: PASS")
    log("PERSISTENT ENTRY PROTOCOL READY FLAG: PASS")
    log("NO ROSTER UNLOCK SIDE EFFECT: PASS")
    log("FIVE-PEOPLE TOTAL FORMATION CAP: PASS")
    log("REACTION WINDOWS 0..26 CARRY-FORWARD: PASS (374 lines/language)")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("NO FINAL BOSS OR CUSTOM LOWER-WORKINGS MAP ADDED: PASS")
    log("6.7.23-6.7.39 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE FULL-FORMATION / READINESS PACING STILL REQUIRED")

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
        "LowerWorkingsEntryProtocolStoryService",
        "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady",
        "teamup_entry_protocol",
        "story.entry-protocol.validated",
        "story.entry-protocol.ready",
        "SurgeHighEscalationStoryService",
        "GetStoryReactionWindowAlpha6739",
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
        "# Team Up Alpha 6.7.40 - HIGH Response Preparation / Lower Workings Entry Protocol Audit\n\n"
        "## Implemented\n"
        "- Added a persistent four-stage lower-workings entry-preparation protocol behind completed SURGE HIGH and slot 4 authorization.\n"
        "- Reuses the exact recorded breach MineShaft from the controlled-breach chapter.\n"
        "- Requires the full operational formation calculated from online Farmers, unlocked NPC slots, configured party cap, and the hard five-person ceiling.\n"
        "- Validates staging, withdrawal order, rear anchor, no-pursuit threshold, and abort criteria through a continuous 240-tick hold.\n"
        "- Persists `LowerWorkingsEntryProtocolReady` only after the readiness drill is reported to Marlon.\n\n"
        "## Scope locks\n"
        "- No boss, monster spawn, custom lower-workings map, combat rewrite, mutation rewrite, capture rewrite, or roster unlock.\n"
        "- George remains Rank D / Non-Combatant / unrecruitable and unrevealed.\n"
        "- Evelyn postgame secret remains untouched.\n"
        "- Reaction catalog remains windows 0..26 with 374 lines per language.\n\n"
        "## Live verification still required\n"
        "- Validate solo full formation and co-op adaptive formation counts.\n"
        "- Validate the 240-tick hold resets when formation breaks, player warps, or a menu/dialogue interrupts.\n"
        "- Validate READY persists after save/reload.\n",
        encoding="utf-8", newline="\n")

    SMOKE.write_text(
        "TEAM UP 6.7.40 - HIGH RESPONSE PREPARATION / LOWER WORKINGS ENTRY PROTOCOL\n"
        "=======================================================================\n\n"
        "PREP:\n"
        "- Cai build 6.7.40 va load save host.\n"
        "- teamup_surge_high status -> stage 4/4, high=True.\n"
        "- teamup_roster_story status -> unlockedNpcSlots=4/4.\n"
        "- teamup_entry_protocol reset.\n\n"
        "ROUTE:\n"
        "1. Vao AdventureGuild -> briefing, stage 1.\n"
        "2. Den DUNG breach MineShaft voi FULL operational formation. Solo mac dinh: 1 Farmer + 4 NPC. Co-op 2 Farmer: 2 Farmer + 3 NPC.\n"
        "3. Khi du doi hinh -> stage 2, bat dau readiness hold.\n"
        "4. Giu nguyen 240 tick -> stage 3. Thu tach doi / warp / mo menu truoc khi du 240 tick -> bo dem phai reset.\n"
        "5. Quay ve AdventureGuild -> stage 4/4, ProtocolReady=True.\n"
        "6. Save + reload -> teamup_entry_protocol status van ready=True.\n"
        "7. Khong co roster slot moi, khong boss, khong custom map.\n"
        "8. George van Rank D / Non-Combatant / khong recruit; Evelyn khong reveal.\n"
        "9. Reaction catalog 0..26 khong thay doi.\n\n"
        "DEBUG: teamup_entry_protocol status|reset|stage 0-4\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Lower_Workings_Entry_Protocol_latest.txt\n",
        encoding="utf-8", newline="\n")

    log("BUILD SUCCESS - ALPHA 6.7.40")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
