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
STAGE = ROOT / "_stage_alpha6739"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6739.txt"
AUDIT = ROOT / "SURGE_HIGH_REACTIONS_AUDIT_ALPHA6739.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_39_SURGE_HIGH_REACTIONS_VI.txt"
VERSION = "0.2.0-alpha.6.7.39"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.39_SURGE_HIGH_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.39_SURGE_HIGH_REACTIONS_TEST.sha256.txt"
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
    a661 = text("ModEntry.Alpha661.cs")
    a6728 = text("ModEntry.Alpha6728.cs")
    a6732 = text("ModEntry.Alpha6732.cs")
    a6737 = text("ModEntry.Alpha6737.cs")
    a6738 = text("ModEntry.Alpha6738.cs")
    a6739 = text("ModEntry.Alpha6739.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    breach = text("Story/ControlledBreachFirstEntryStoryService.cs")
    high = text("Story/SurgeHighEscalationStoryService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge = text("Story/TheSurgeStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("narrativeStage > 26" in reactions, "reaction range must be 0..26")
    expected = {
        23: "surgeHighBriefed",
        24: "surgeHighReadingStarted",
        25: "surgeHighConfirmed",
        26: "surgeHighSlot4Authorized",
    }
    for window, name in expected.items():
        req(f"[{window}] = {name}" in reactions, f"reaction window {window} missing")

    req("GetStoryReactionWindowAlpha6739()" in a6739, "6.7.39 resolver missing")
    req("ControlledBreachAlpha6736.Stage < ControlledBreachFirstEntryStoryService.CompleteStage" in a6739, "controlled-breach fallback missing")
    req("return GetStoryReactionWindowAlpha6737();" in a6739, "6.7.37 fallback missing")
    req("SurgeHighAlpha6738.Stage switch" in a6739, "Surge HIGH stage resolver missing")
    for pair in ["<= 0 => 22", "1 => 23", "2 => 24", "3 => 25", "_ => 26"]:
        req(pair in a6739, f"resolver mapping missing: {pair}")
    req(a6728.count("GetStoryReactionWindowAlpha6739()") >= 2, "interaction/diagnostic not routed to 6.7.39 resolver")

    req(set(en) == set(vi), "EN/VI i18n parity failed")
    new_keys = {f"story.react.{w}.{npc}" for w in range(23, 27) for npc in NPCS}
    req(new_keys.issubset(en) and new_keys.issubset(vi), "6.7.39 localization incomplete")
    for window in range(23, 27):
        req(sum(1 for k in en if k.startswith(f"story.react.{window}.")) == 14, f"EN window {window} must contain 14 NPCs")
        req(sum(1 for k in vi if k.startswith(f"story.react.{window}.")) == 14, f"VI window {window} must contain 14 NPCs")
    req(sum(1 for k in en if k.startswith("story.react.")) == 374, "EN reaction catalog must contain 374 lines")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 374, "VI reaction catalog must contain 374 lines")

    for key in new_keys:
        for forbidden in ("Rank S", "The Last Blaster", "George Mullner", "Keeper", "Sector 17", "SECTOR 17"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("public const int CompleteStage = 4;" in high, "6.7.38 HIGH route regressed")
    req('SurgeHighFlagKey = "Ronvotri.TeamUp/Story/SurgeHighConfirmed"' in high, "persistent HIGH flag regressed")
    req("HighConfirmationTicksRequired = 180" in high, "180-tick HIGH hold regressed")
    req("MinimumFieldPeople = 3" in high and "MinimumActiveNpcAllies = 1" in high, "HIGH field-team rule regressed")
    req("ControlledBreachFirstEntryStoryService.CompleteStage" in high, "controlled-breach prerequisite regressed")
    req("ControlledBreachFirstEntryStoryService.BreachLocationKey" in high, "exact breach-face reuse regressed")
    req('if (_stage == 3 && Is(location, "AdventureGuild"))' in high, "final HIGH report gate missing")
    req('_unlockNpcSlots(4, "surge-high-confirmed")' in high, "slot 4 story unlock missing")
    req("Game1.MasterPlayer.modData[SurgeHighFlagKey] = \"1\";" in high, "HIGH persistence write missing")
    req("public const int CompleteStage = 5;" in breach, "controlled breach carry-forward regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person cap regressed")
    req("Game1.getOnlineFarmers().Count" in a6732, "co-op field count regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge, "10-kill trigger regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("REACTION WINDOWS 0..26: PASS")
    log("SURGE HIGH WINDOWS 23..26: PASS (14 NPCs each)")
    log("CURATED MILESTONE REACTIONS: PASS (374 lines/language)")
    log("NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.39 WINDOW RESOLVER: PASS")
    log("SURGE HIGH 6.7.38 CARRY-FORWARD: PASS")
    log("PERSISTENT HIGH + 180-TICK CONFIRMATION HOLD: PASS")
    log("STORY NPC SLOT 4 AUTHORIZATION CARRY-FORWARD: PASS")
    log("FIVE-PEOPLE TOTAL FORMATION CAP: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("6.7.23-6.7.38 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE ONE-SHOT DIALOGUE PACING FOR WINDOWS 23..26 STILL REQUIRED")

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
        "GetStoryReactionWindowAlpha6739",
        "story.react.23.george",
        "story.react.24.marlon",
        "story.react.25.robin",
        "story.react.26.evelyn",
        "teamup_story_reactions",
        "teamup_surge_high",
        "SurgeHighEscalationStoryService",
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
        "# Team Up Alpha 6.7.39 - Surge HIGH Reactions Audit\n\n"
        "## Implemented\n"
        "- Expanded one-shot milestone reactions from windows 0..22 to 0..26.\n"
        "- Window 23: Marlon HIGH-check briefing.\n"
        "- Window 24: stable reading started at the exact breach face.\n"
        "- Window 25: persistent SURGE HIGH confirmed.\n"
        "- Window 26: story NPC slot 4 authorized after Guild report.\n"
        "- Added 56 curated lines per language, total 374 reaction lines per language.\n\n"
        "## Safety\n"
        "- Dialogue-only checkpoint. Alpha 6.7.38 HIGH gameplay and roster behavior are unchanged.\n"
        "- George remains pre-reveal Rank D / Non-Combatant / unrecruitable. Evelyn postgame reveal remains untouched.\n"
        "- HIGH is an operational risk classification only; new dialogue does not identify a boss, entity, or historical worker.\n\n"
        "## Live verification still required\n"
        "- One-shot interaction timing for windows 23..26.\n"
        "- No stale reaction replay after Surge HIGH stage advancement.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.39 - SURGE HIGH REACTIONS\n"
        "=====================================\n\n"
        "1. teamup_story_reactions reset.\n"
        "2. teamup_surge_high stage 1 -> reaction window 23.\n"
        "3. Talk supported NPC -> one special reaction only; second talk returns normal.\n"
        "4. stage 2 -> window 24; stage 3 -> window 25; stage 4 -> window 26.\n"
        "5. Skip an old window then advance -> stale reaction must not replay.\n"
        "6. Reactions must not change Surge HIGH stage or roster slots.\n"
        "7. At stage 3 HIGH remains true; for a clean test roster can remain 3/4 until stage 4 report.\n"
        "8. George remains Rank D / Non-Combatant / unrecruitable.\n"
        "9. Evelyn does not reveal postgame secret.\n"
        "DEBUG: teamup_story_reactions reset | status; teamup_surge_high stage 0-4\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Milestone_Reactions_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.39")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
