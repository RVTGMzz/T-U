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
STAGE = ROOT / "_stage_alpha6731"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6731.txt"
AUDIT = ROOT / "OLD_MINE_REACTIONS_AUDIT_ALPHA6731.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_31_OLD_MINE_REACTIONS_VI.txt"
VERSION = "0.2.0-alpha.6.7.31"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.31_OLD_MINE_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.31_OLD_MINE_REACTIONS_TEST.sha256.txt"
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
    a6729 = text("ModEntry.Alpha6729.cs")
    a6730 = text("ModEntry.Alpha6730.cs")
    a6731 = text("ModEntry.Alpha6731.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    old_mine = text("Story/OldMineConnectionStoryService.cs")
    marlon = text("Story/MarlonInvestigationStoryService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge = text("Story/TheSurgeStoryService.cs")
    origin = text("Story/OriginStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("GetStoryReactionWindowAlpha6731" in a6731, "6.7.31 reaction resolver missing")
    req("OldMineConnectionAlpha6730.Stage switch" in a6731, "old-mine stage resolver missing")
    req("1 => 7" in a6731 and "2 => 8" in a6731 and "_ => 9" in a6731, "windows 7..9 resolver incomplete")
    req(a6728.count("GetStoryReactionWindowAlpha6731()") >= 2, "NPC interaction/diagnostic not routed to 6.7.31 resolver")
    req("narrativeStage > 9" in reactions, "reaction range did not expand to 9")
    for window in range(10):
        req(f"[{window}] =" in reactions, f"reaction window {window} missing")

    reaction_keys = {key for key in en if key.startswith("story.react.")}
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(len(reaction_keys) == 136, f"expected exactly 136 milestone reactions per language, found {len(reaction_keys)}")
    for window in (7, 8, 9):
        keys = {key for key in reaction_keys if key.startswith(f"story.react.{window}.")}
        req(len(keys) == 14, f"window {window} expected 14 reactions, found {len(keys)}")
        for npc in ("george", "evelyn", "marlon", "alex"):
            req(f"story.react.{window}.{npc}" in keys, f"window {window} missing {npc} reaction")

    new_keys = {key for key in reaction_keys if key.startswith(("story.react.7.", "story.react.8.", "story.react.9."))}
    for key in new_keys:
        for forbidden in ("Rank S", "Last Blaster", "George Mullner", "George is the miner", "George là người thợ mỏ"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("IsGeorgePreRevealLockedAlpha6728" in a6728, "George pre-reveal lock regressed")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit block regressed")
    req('StageKey = "Ronvotri.TeamUp/Story/OldMineConnectionStage"' in old_mine, "6.7.30 old mine state regressed")
    req('UnlockTo(Game1.MasterPlayer, 3, "old-mine-connection-confirmed")' in a6730, "slot-3 payoff regressed")
    req("MarlonInvestigationStoryService" in marlon, "6.7.29 Marlon case regressed")
    req('UnlockTo(\n            Game1.MasterPlayer,\n            2,' in a6729, "6.7.29 slot-2 payoff regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge, "10-kill trigger regressed")
    req('StageKey = "Ronvotri.TeamUp/SurgeNarrativeStage"' in origin, "origin bridge regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("GetEffectiveNpcSlotLimitAlpha6727" in a6727, "five-person roster coupling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person cap regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("REACTION WINDOWS 0..9: PASS")
    log("OLD-MINE WINDOWS 7/8/9: PASS (14 NPCs each)")
    log(f"CURATED MILESTONE REACTIONS: PASS ({len(reaction_keys)} lines/language)")
    log("NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.31 WINDOW RESOLVER: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("6.7.23-6.7.30 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE ONE-SHOT DIALOGUE PACING STILL REQUIRED")

    proc = subprocess.run(
        ["dotnet", "build", str(SRC / "TeamUp.csproj"), "-c", "Release", "--nologo", "-warnaserror"],
        cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False,
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
        "GetStoryReactionWindowAlpha6731",
        "story.react.7.george",
        "story.react.8.marlon",
        "story.react.9.abigail",
        "teamup_story_reactions",
        "teamup_old_mine",
        "story.roster.third-unlock",
        "teamup_capture_ceasefire",
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
        "# Team Up Alpha 6.7.31 - Old Mine Milestone Reactions Audit\n\n"
        "## Implemented\n"
        "- Extended the one-shot story reaction catalog from windows 0..6 to 0..9.\n"
        "- Window 7: Marlon finds the surviving municipal-record lead for the missing company page.\n"
        "- Window 8: the ManorHouse safety ledger reveals sealed lower workings, a redacted employee line, and the matching hooked mark.\n"
        "- Window 9: the old coal-mine connection is confirmed and story NPC ally capacity is already 3/4 from Alpha 6.7.30.\n"
        "- Added 14 curated NPC reactions per new window, 42 new lines per language, for 136 milestone lines per language total.\n"
        "- George remains observed Rank D / Non-Combatant and unrecruitable. His reactions read as ordinary mining experience only.\n"
        "- Evelyn remains spoiler-safe; no postgame Rank S reveal is exposed.\n\n"
        "## Safety\n"
        "- Dialogue-only expansion. No monster, provider, capture, combat, roster-cap, or map ownership behavior is modified.\n"
        "- Existing one-shot seen keys remain stable; old reactions are not replayed after later milestones.\n\n"
        "## Live verification still required\n"
        "- Each supported NPC should react exactly once in windows 7, 8, and 9.\n"
        "- Missing an earlier window should not cause stale dialogue to replay later.\n"
        "- Normal NPC interaction should resume after the one-shot reaction is consumed.\n",
        encoding="utf-8", newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.31 - OLD MINE MILESTONE REACTIONS\n"
        "==============================================\n\n"
        "BAN NAY CI-VERIFIED SAU KHI WORKFLOW PASS, NHUNG VAN CAN LIVE TEST.\n\n"
        "A. WINDOW 7 - ARCHIVE LEAD\n"
        "1. teamup_old_mine stage 1\n"
        "2. Noi chuyen tay rong voi Abigail/Alex/Clint/Demetrius/Evelyn/George/Gus/Lewis/Linus/Marlon/Maru/Pierre/Robin/Wizard.\n"
        "3. Moi NPC chi noi reaction 1 lan; noi lai phai ve interaction binh thuong.\n\n"
        "B. WINDOW 8 - SEALED RECORD\n"
        "1. teamup_old_mine stage 2\n"
        "2. Lap lai test 14 NPC, moi NPC 1 lan.\n\n"
        "C. WINDOW 9 - CONNECTION CONFIRMED\n"
        "1. teamup_old_mine stage 3\n"
        "2. Lap lai test 14 NPC, moi NPC 1 lan.\n"
        "3. George KHONG duoc lo Rank S / Last Blaster, van Non-Combatant va khong recruit duoc.\n"
        "4. Evelyn KHONG duoc lo twist postgame.\n\n"
        "LENH: teamup_story_reactions status | reset ; teamup_old_mine status | stage 0..3\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Milestone_Reactions_latest.txt\n"
        "NEU LOI: gui %appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8", newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.31")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
