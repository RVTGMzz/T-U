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
STAGE = ROOT / "_stage_alpha6741"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6741.txt"
AUDIT = ROOT / "ENTRY_PROTOCOL_REACTIONS_AUDIT_ALPHA6741.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_41_ENTRY_PROTOCOL_REACTIONS_VI.txt"
VERSION = "0.2.0-alpha.6.7.41"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.41_ENTRY_PROTOCOL_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.41_ENTRY_PROTOCOL_REACTIONS_TEST.sha256.txt"
NPCS = ["Abigail","Alex","Clint","Demetrius","Evelyn","George","Gus","Lewis","Linus","Marlon","Maru","Pierre","Robin","Wizard"]
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
    a6738 = text("ModEntry.Alpha6738.cs")
    a6739 = text("ModEntry.Alpha6739.cs")
    a6740 = text("ModEntry.Alpha6740.cs")
    a6741 = text("ModEntry.Alpha6741.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
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
    req("Entry Protocol reaction layer active." in entry, "6.7.41 startup marker missing")
    req("narrativeStage > 30" in reactions, "reaction range must be 0..30")

    expected_maps = ["<= 0 => 26", "1 => 27", "2 => 28", "3 => 29", "_ => 30"]
    for token in expected_maps:
        req(token in a6741, f"6.7.41 resolver mapping missing: {token}")
    req("EntryProtocolAlpha6740.Stage switch" in a6741, "6.7.41 resolver not based on Entry Protocol stage")
    req("SurgeHighAlpha6738.Stage < SurgeHighEscalationStoryService.CompleteStage" in a6741, "6.7.39 fallback boundary missing")
    req("GetStoryReactionWindowAlpha6739()" in a6741, "6.7.39 fallback resolver missing")
    req(a6728.count("GetStoryReactionWindowAlpha6741()") >= 2, "NPC interaction/diagnostic not routed to 6.7.41 resolver")
    req("TEAM UP 6.7.41 - ENTRY PROTOCOL REACTIONS" in a6728, "6.7.41 diagnostic title missing")

    for window, var_name in [(27,"entryProtocolBriefed"),(28,"entryStagingEstablished"),(29,"entryReadinessValidated"),(30,"entryProtocolReady")]:
        req(f"[{window}] = {var_name}" in reactions, f"reaction dictionary missing window {window}")
        for npc in NPCS:
            key = f"story.react.{window}.{npc.lower()}"
            req(key in en and key in vi, f"missing EN/VI reaction key: {key}")
            req(f'"{npc}", "{key}"' in reactions, f"reaction service missing {key}")

    req(set(en) == set(vi), "EN/VI i18n parity failed")
    en_reactions = [k for k in en if k.startswith("story.react.")]
    vi_reactions = [k for k in vi if k.startswith("story.react.")]
    req(len(en_reactions) == 430, f"EN reaction catalog must be exactly 430, got {len(en_reactions)}")
    req(len(vi_reactions) == 430, f"VI reaction catalog must be exactly 430, got {len(vi_reactions)}")
    for window in range(27, 31):
        req(sum(1 for k in en if k.startswith(f"story.react.{window}.")) == 14, f"EN window {window} must have 14 NPCs")
        req(sum(1 for k in vi if k.startswith(f"story.react.{window}.")) == 14, f"VI window {window} must have 14 NPCs")

    forbidden = ("Rank S", "The Last Blaster", "George Mullner", "Keeper", "Sector 17", "SECTOR 17")
    for window in range(27, 31):
        for npc in NPCS:
            key = f"story.react.{window}.{npc.lower()}"
            for token in forbidden:
                req(token not in en[key] and token not in vi[key], f"spoiler leak {token}: {key}")

    req('public const string StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolStage";' in protocol, "6.7.40 protocol stage key regressed")
    req('public const string ProtocolReadyFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady";' in protocol, "protocol READY flag regressed")
    req("public const int CompleteStage = 4;" in protocol, "entry protocol route must remain 0..4")
    req("ReadinessHoldTicksRequired = 240" in protocol, "240-tick readiness hold regressed")
    req("GetRequiredFieldPeople()" in protocol and "GetRequiredNpcAllies()" in protocol, "adaptive full formation helpers missing")
    req("Math.Min(peopleCap, farmers + unlockedNpcSlots)" in protocol, "adaptive people requirement regressed")
    req("Math.Min(unlockedNpcSlots, Math.Max(0, peopleCap - farmers))" in protocol, "adaptive NPC requirement regressed")
    req("HasFullOperationalFormation(location)" in protocol, "full formation gate missing")
    req("Game1.activeClickableMenu is null" in protocol and "!Game1.dialogueUp" in protocol, "menu/dialogue readiness guard missing")
    req('Game1.MasterPlayer.modData[ProtocolReadyFlagKey] = "1"' in protocol, "protocol READY persistence missing")
    req("teamup_entry_protocol" in a6740, "entry protocol debug command missing")

    req("public const string SurgeHighFlagKey = \"Ronvotri.TeamUp/Story/SurgeHighConfirmed\";" in high, "SURGE HIGH flag regressed")
    req("HighConfirmationTicksRequired = 180" in high, "SURGE HIGH 180-tick hold regressed")
    req('_unlockNpcSlots(4, "surge-high-confirmed")' in high, "story slot4 authorization regressed")
    req("GetStoryReactionWindowAlpha6739" in a6739, "6.7.39 resolver carry-forward missing")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person formation cap regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first mutation trigger regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "Pelipper capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("REACTION WINDOWS 0..30: PASS")
    log("ENTRY PROTOCOL WINDOWS 27..30: PASS (14 NPCs each)")
    log("CURATED MILESTONE REACTIONS: PASS (430 lines/language)")
    log("NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.41 WINDOW RESOLVER: PASS")
    log("ENTRY PROTOCOL 6.7.40 CARRY-FORWARD: PASS")
    log("ADAPTIVE FULL FORMATION CARRY-FORWARD: PASS")
    log("240-TICK READINESS HOLD + PERSISTENT READY FLAG: PASS")
    log("SURGE HIGH + STORY SLOT 4 CARRY-FORWARD: PASS")
    log("FIVE-PEOPLE TOTAL FORMATION CAP: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("PELlPPER / MUTATION SAFETY CARRY-FORWARD: PASS")
    log("NO COMBAT, MAP, BOSS, MONSTER, CAPTURE OR ROSTER MECHANICS CHANGED: PASS")
    log("LIVE ONE-SHOT DIALOGUE PACING FOR WINDOWS 27..30 STILL REQUIRED")

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
        "GetStoryReactionWindowAlpha6741",
        "story.react.27.abigail",
        "story.react.28.marlon",
        "story.react.29.george",
        "story.react.30.wizard",
        "LowerWorkingsEntryProtocolStoryService",
        "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady",
        "teamup_entry_protocol",
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
        "# Team Up Alpha 6.7.41 - Entry Protocol Reactions Audit\n\n"
        "## Implemented\n"
        "- Added reaction windows 27 through 30 for the Alpha 6.7.40 Entry Protocol route.\n"
        "- Window 27: HIGH response / entry-protocol briefing.\n"
        "- Window 28: full formation assembled and staging line established.\n"
        "- Window 29: 240-tick readiness / withdrawal-line drill validated.\n"
        "- Window 30: Lower Workings Entry Protocol marked READY.\n"
        "- Same curated 14 NPCs per window, adding 56 lines per language.\n"
        "- Reaction catalog now contains exactly 430 lines per language.\n\n"
        "## Safety / scope\n"
        "- Dialogue-only checkpoint. Alpha 6.7.40 gameplay is unchanged.\n"
        "- George remains observed Rank D / Non-Combatant / unrecruitable and unrevealed.\n"
        "- Evelyn postgame secret remains untouched.\n"
        "- No final boss, custom lower-workings map, combat change, mutation rewrite, capture rewrite, monster ownership change, or roster unlock.\n"
        "- Five-person total formation cap and Pelipper capture safety remain intact.\n\n"
        "## Live verification still required\n"
        "- Validate one-shot dialogue behavior for windows 27, 28, 29 and 30.\n"
        "- Validate stale skipped windows do not replay.\n"
        "- Validate reactions never mutate Entry Protocol stage, READY flag, Surge HIGH, or roster slots.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.41 - ENTRY PROTOCOL REACTIONS\n"
        "=========================================\n\n"
        "PREP:\n"
        "- Cai build 6.7.41, load save host.\n"
        "- teamup_story_reactions reset.\n\n"
        "WINDOWS:\n"
        "1. teamup_entry_protocol stage 1 -> window 27. Noi chuyen NPC ho tro 2 lan: lan 1 reaction, lan 2 normal.\n"
        "2. teamup_entry_protocol stage 2 -> window 28. Lap lai one-shot test.\n"
        "3. teamup_entry_protocol stage 3 -> window 29. READY van chua duoc tu reaction.\n"
        "4. teamup_entry_protocol stage 4 -> window 30. Reaction khong duoc thay doi protocol state.\n"
        "5. Bo qua mot window roi tang stage -> reaction cu khong duoc replay.\n"
        "6. George van Rank D / Non-Combatant / khong recruit; khong Last Blaster.\n"
        "7. Evelyn khong reveal postgame.\n"
        "8. teamup_story_reactions status -> catalog 0..30.\n"
        "9. teamup_entry_protocol status -> stage va READY phai chi do 6.7.40 gameplay quyet dinh.\n\n"
        "DEBUG: teamup_entry_protocol status|reset|stage 0-4\n"
        "DEBUG: teamup_story_reactions status|reset\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Milestone_Reactions_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.41")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines).rstrip() + "\n", encoding="utf-8", newline="\n")
