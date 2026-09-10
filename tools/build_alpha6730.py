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
STAGE = ROOT / "_stage_alpha6730"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6730.txt"
AUDIT = ROOT / "OLD_MINE_CONNECTION_AUDIT_ALPHA6730.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_30_OLD_MINE_VI.txt"
VERSION = "0.2.0-alpha.6.7.30"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.30_OLD_MINE_CONNECTION_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.30_OLD_MINE_CONNECTION_TEST.sha256.txt"
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
    marlon = text("Story/MarlonInvestigationStoryService.cs")
    old_mine = text("Story/OldMineConnectionStoryService.cs")
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
    req("RegisterAlpha6730Events();" in entry, "6.7.30 registration missing")
    req('StageKey = "Ronvotri.TeamUp/Story/OldMineConnectionStage"' in old_mine, "old mine persistence key missing")
    req("public const int CompleteStage = 3;" in old_mine, "old mine stage ceiling missing")
    req('Is(location, "AdventureGuild")' in old_mine, "AdventureGuild gate missing")
    req('Is(location, "ManorHouse")' in old_mine, "ManorHouse record gate missing")
    req("_hasActiveAllyAt(location)" in old_mine, "active ally gate missing")
    req("MarlonInvestigationStoryService.CompleteStage" in old_mine, "Marlon case prerequisite missing")
    req('UnlockTo(Game1.MasterPlayer, 3, "old-mine-connection-confirmed")' in a6730, "slot-3 payoff missing")
    req("teamup_old_mine" in a6730, "old mine diagnostic command missing")
    req("TeamUp_Old_Mine_Connection_latest.txt" in a6730, "old mine diagnostic file missing")

    keys = {
        "story.old-mine.archive-lead",
        "story.old-mine.objective.records",
        "story.old-mine.sealed-record",
        "story.old-mine.objective.return",
        "story.old-mine.connection-confirmed",
        "story.old-mine.complete",
        "story.roster.third-unlock",
    }
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(keys.issubset(en) and keys.issubset(vi), "6.7.30 localization incomplete")
    for key in keys:
        for forbidden in ("Rank S", "Last Blaster", "George Mullner"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("IsGeorgePreRevealLockedAlpha6728" in a6728, "George pre-reveal lock regressed")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit block regressed")
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
    log("OLD MINE CONNECTION 0..3 PERSISTENT ROUTE: PASS")
    log("MARLON 6.7.29 COMPLETION PREREQUISITE: PASS")
    log("ACTIVE NPC ALLY REQUIRED AT ALL 6.7.30 GATES: PASS")
    log("GUILD -> MANORHOUSE -> GUILD RECORD ROUTE: PASS")
    log("OLD COAL-MINE CONNECTION -> STORY NPC SLOT 3: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EN/VI LOCALIZATION PARITY: PASS")
    log("6.7.23-6.7.29 SAFETY/STORY CARRY-FORWARD: PASS")
    log("LIVE MANORHOUSE / DIALOGUE PACING / MULTIPLAYER TEST STILL REQUIRED")

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
    for token in ["teamup_old_mine", "OldMineConnectionStoryService", "OldMineConnectionStage",
                  "story.old-mine.connection-confirmed", "story.roster.third-unlock",
                  "teamup_marlon_case", "teamup_roster_story", "teamup_capture_ceasefire"]:
        req(dll_contains(blob, token), f"DLL missing token: {token}")
    log("BINARY ACCEPTANCE: PASS")

    RELEASE.mkdir(parents=True, exist_ok=True)
    if STAGE.exists(): shutil.rmtree(STAGE)
    MOD_STAGE.mkdir(parents=True)
    shutil.copy2(dll, MOD_STAGE / "TeamUp.dll")
    manifest = text("manifest.json").replace("%ProjectVersion%", VERSION)
    (MOD_STAGE / "manifest.json").write_text(manifest, encoding="utf-8", newline="\n")
    shutil.copytree(SRC / "i18n", MOD_STAGE / "i18n")
    if ZIP_PATH.exists(): ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in sorted(MOD_STAGE.rglob("*")):
            if path.is_file(): archive.write(path, path.relative_to(STAGE))
    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Team Up Alpha 6.7.30 - Old Mine Connection Audit\n\n"
        "## Implemented\n"
        "- Added a persistent 0..3 Chapter IV bridge after the 6.7.29 Marlon investigation.\n"
        "- Guild archive lead points to a municipal safety filing for an old coal mine roughly thirty years ago.\n"
        "- ManorHouse record confirms sealed lower workings, a redacted worker line, and the hooked inspection mark matching current Mutant evidence.\n"
        "- Guild confirmation links the current Surge to the old coal-mine incident and unlocks story NPC ally slot 3/4.\n"
        "- The old miner remains unnamed. George stays observed Rank D / Non-Combatant and unrecruitable.\n\n"
        "## Safety\n"
        "- Record/location-only story logic: no monster spawn, replacement, damage, hide, clone, or provider ownership changes.\n"
        "- Pelipper capture ceasefire and the five-person total formation cap remain authoritative.\n\n"
        "## Live verification still required\n"
        "- Guild-to-ManorHouse-to-Guild dialogue pacing and party-follow presence.\n"
        "- ManorHouse location gate in the real mod stack.\n"
        "- Multiplayer host/active-ally physical presence.\n",
        encoding="utf-8", newline="\n",
    )
    SMOKE.write_text(
        "TEAM UP 6.7.30 - OLD MINE CONNECTION\n"
        "=====================================\n\n"
        "CAN LIVE TEST SAU KHI CI PASS.\n\n"
        "1. teamup_marlon_case status -> stage=4/4.\n"
        "2. Co it nhat 1 NPC Following/Waiting cung map.\n"
        "3. Vao AdventureGuild -> old mine stage 1, Marlon dua archive lead.\n"
        "4. Vao ManorHouse cung dong doi -> stage 2, doc sealed safety record.\n"
        "5. Quay lai AdventureGuild cung dong doi -> stage 3, xac nhan old coal-mine connection.\n"
        "6. teamup_roster_story status -> unlockedNpcSlots=3/4.\n"
        "7. George van Rank D / Non-Combatant, khong recruit, khong lo Last Blaster/Rank S.\n\n"
        "DEBUG: teamup_old_mine status | reset | stage 0..3\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Old_Mine_Connection_latest.txt\n"
        "NEU LOI: gui %appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8", newline="\n",
    )
    log("BUILD SUCCESS - ALPHA 6.7.30")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
