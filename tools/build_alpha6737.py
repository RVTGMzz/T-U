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
STAGE = ROOT / "_stage_alpha6737"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6737.txt"
AUDIT = ROOT / "CONTROLLED_BREACH_REACTIONS_AUDIT_ALPHA6737.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_37_CONTROLLED_BREACH_REACTIONS_VI.txt"
VERSION = "0.2.0-alpha.6.7.37"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.37_CONTROLLED_BREACH_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.37_CONTROLLED_BREACH_REACTIONS_TEST.sha256.txt"
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
    a6727 = text("ModEntry.Alpha6727.cs")
    a6728 = text("ModEntry.Alpha6728.cs")
    a6732 = text("ModEntry.Alpha6732.cs")
    a6734 = text("ModEntry.Alpha6734.cs")
    a6735 = text("ModEntry.Alpha6735.cs")
    a6736 = text("ModEntry.Alpha6736.cs")
    a6737 = text("ModEntry.Alpha6737.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    breach = text("Story/ControlledBreachFirstEntryStoryService.cs")
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
    req("narrativeStage > 22" in reactions, "reaction range must be 0..22")
    expected = {
        18: "breachBriefed",
        19: "breachFaceReady",
        20: "controlledOpeningComplete",
        21: "firstEntryProbeComplete",
        22: "firstEntryReported",
    }
    for window, name in expected.items():
        req(f"[{window}] = {name}" in reactions, f"reaction window {window} missing")

    req("GetStoryReactionWindowAlpha6737()" in a6737, "6.7.37 resolver missing")
    req("ControlledBreachAlpha6736.Stage switch" in a6737, "breach stage resolver missing")
    req("CorridorApproachAlpha6734.Stage < SealedCorridorApproachStoryService.CompleteStage" in a6737, "corridor fallback missing")
    for pair in ["<= 0 => 17", "1 => 18", "2 => 19", "3 => 20", "4 => 21", "_ => 22"]:
        req(pair in a6737, f"resolver mapping missing: {pair}")
    req(a6728.count("GetStoryReactionWindowAlpha6737()") >= 2, "interaction/diagnostic not routed to 6.7.37 resolver")

    req(set(en) == set(vi), "EN/VI i18n parity failed")
    new_keys = {f"story.react.{w}.{npc}" for w in range(18, 23) for npc in NPCS}
    req(new_keys.issubset(en) and new_keys.issubset(vi), "6.7.37 localization incomplete")
    for window in range(18, 23):
        req(sum(1 for k in en if k.startswith(f"story.react.{window}.")) == 14, f"EN window {window} must contain 14 NPCs")
        req(sum(1 for k in vi if k.startswith(f"story.react.{window}.")) == 14, f"VI window {window} must contain 14 NPCs")
    req(sum(1 for k in en if k.startswith("story.react.")) == 318, "EN reaction catalog must contain 318 lines")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 318, "VI reaction catalog must contain 318 lines")

    for key in new_keys:
        for forbidden in ("Rank S", "The Last Blaster", "George Mullner", "Keeper"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("public const int CompleteStage = 5;" in breach, "6.7.36 breach route regressed")
    req("BreachHoldTicksRequired = 180" in breach, "180-tick breach hold regressed")
    req("EntryProbeTicksRequired = 120" in breach, "120-tick probe hold regressed")
    req("BreachLocationKey" in breach and "ResolveSurveyLocation" in breach, "saved survey face reuse regressed")
    req("ResetRuntimeHolds();" in breach, "hold reset regressed")
    req("MinimumFieldPeople = 3" in breach and "MinimumActiveNpcAllies = 1" in breach, "breach field-team rule regressed")
    req("StableSurveyTicksRequired = 240" in corridor, "6.7.34 pressure survey regressed")
    req("Game1.getOnlineFarmers().Count" in a6732, "co-op field count regressed")
    req('UnlockTo(Game1.MasterPlayer, 4' not in all_cs, "story slot 4 unlocked too early")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person cap regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge, "10-kill trigger regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("REACTION WINDOWS 0..22: PASS")
    log("CONTROLLED BREACH WINDOWS 18..22: PASS (14 NPCs each)")
    log("CURATED MILESTONE REACTIONS: PASS (318 lines/language)")
    log("NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.37 WINDOW RESOLVER: PASS")
    log("CONTROLLED BREACH 6.7.36 CARRY-FORWARD: PASS")
    log("180-TICK OPENING + 120-TICK PROBE HOLDS: PASS")
    log("STORY NPC SLOT 4 REMAINS LOCKED: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("6.7.23-6.7.36 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE ONE-SHOT DIALOGUE PACING FOR WINDOWS 18..22 STILL REQUIRED")

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
        "GetStoryReactionWindowAlpha6737",
        "story.react.18.george",
        "story.react.19.marlon",
        "story.react.20.robin",
        "story.react.21.evelyn",
        "story.react.22.abigail",
        "teamup_story_reactions",
        "teamup_breach",
        "ControlledBreachFirstEntryStoryService",
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
        "# Team Up Alpha 6.7.37 - Controlled Breach Reactions Audit\n\n"
        "## Implemented\n"
        "- Expanded one-shot milestone reactions from windows 0..17 to 0..22.\n"
        "- Window 18: controlled-breach briefing.\n"
        "- Window 19: exact breach face reached and prepared.\n"
        "- Window 20: controlled opening stabilized.\n"
        "- Window 21: first-entry threshold probe completed.\n"
        "- Window 22: first-entry probe reported to Marlon.\n"
        "- Added 70 curated lines per language, total 318 reaction lines per language.\n\n"
        "## Safety\n"
        "- Dialogue-only checkpoint. No combat, monster, capture, map, breach mechanics, boss, or roster behavior changed.\n"
        "- Slot 4 remains locked. George and Evelyn reveal boundaries remain locked.\n\n"
        "## Live verification still required\n"
        "- One-shot interaction timing for windows 18..22.\n"
        "- No stale reaction replay after breach-stage advancement.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.37 - CONTROLLED BREACH REACTIONS\n"
        "=============================================\n\n"
        "1. teamup_story_reactions reset.\n"
        "2. teamup_breach stage 1 -> reaction window 18.\n"
        "3. Talk supported NPC -> one special reaction only; second talk returns normal.\n"
        "4. stage 2 -> window 19; stage 3 -> 20; stage 4 -> 21; stage 5 -> 22.\n"
        "5. Skip an old window then advance -> stale reaction must not replay.\n"
        "6. George remains Rank D / Non-Combatant / unrecruitable.\n"
        "7. Evelyn does not reveal postgame secret.\n"
        "8. teamup_roster_story status remains 3/4.\n"
        "DEBUG: teamup_story_reactions reset | status\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Milestone_Reactions_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.37")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
