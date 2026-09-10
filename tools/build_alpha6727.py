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
STAGE = ROOT / "_stage_alpha6727"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6727.txt"
AUDIT = ROOT / "STORY_ROSTER_PROGRESSION_AUDIT_ALPHA6727.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_27_STORY_ROSTER_VI.txt"
VERSION = "0.2.0-alpha.6.7.27"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.27_STORY_ROSTER_PROGRESSION_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.27_STORY_ROSTER_PROGRESSION_TEST.sha256.txt"

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
    alpha661 = text("ModEntry.Alpha661.cs")
    alpha6727 = text("ModEntry.Alpha6727.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    origin = text("Story/OriginStoryService.cs")
    surge_story = text("Story/TheSurgeStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    config = text("ModConfig.cs")
    default_i18n = json.loads(text("i18n/default.json"))
    vi_i18n = json.loads(text("i18n/vi.json"))
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6727Events();" in entry, "6.7.27 registration missing")
    req("Math.Clamp(Config.MaxPartyMembers, 1, 5)" in entry, "five-person hard cap is not normalized")
    req("Math.Clamp(Config.MaxPartyMembers, 1, 6)" not in entry, "six-person config path still present in main entry")

    req("MaxStoryNpcSlots = 4" in roster, "story NPC slot maximum must be four")
    req('UnlockedNpcSlotsKey = "Ronvotri.TeamUp/Story/UnlockedNpcSlots"' in roster, "story roster persistence key missing")
    req("_getNarrativeStage() >= 2 && after < 1" in roster, "Marlon bridge does not unlock first NPC slot")
    req("UnlockTo(Farmer storyOwner, int requestedSlots, string source)" in roster, "future chapter unlock hook missing")

    req('"teamup_roster_story"' in alpha6727, "story roster debug/diagnostic command missing")
    req("GetEffectiveNpcSlotLimitAlpha6727" in alpha6727, "effective NPC slot resolver missing")
    req("Math.Max(0, peopleCap - farmerCount)" in alpha6727, "online Farmers are not deducted from five-person cap")
    req("GetActiveStoryNpcCountAlpha6727" in alpha6727, "active NPC story count missing")
    req("EnforceStoryRosterCapacityAlpha6727" in alpha6727, "story overflow enforcement missing")
    req("TeamUp_Roster_Progression_latest.txt" in alpha6727, "story roster diagnostic path missing")

    req("IsBaseRecruitableNpcForAlpha6727(npc, farmer)" in alpha661, "base recruitability separation missing")
    req("CanRecruitByStoryAlpha6727(recruiter, out string rosterFailure)" in alpha661,
        "authoritative recruit path does not enforce story roster")
    req("CanActivateRosterSlotAlpha6727(recruiterId, out string rosterFailure)" in alpha661,
        "inactive member reactivation bypasses story roster")
    req("GetRosterProfileStatusAlpha6727(npc)" in entry, "Codex/profile story lock status missing")

    required_keys = {
        "story.roster.first-unlock",
        "story.roster.locked",
        "story.roster.limit",
        "story.roster.multiplayer-full",
        "profile.status-story-locked",
        "profile.status-team-limit",
    }
    req(set(default_i18n.keys()) == set(vi_i18n.keys()), "EN/VI i18n key parity failed")
    req(required_keys.issubset(default_i18n) and required_keys.issubset(vi_i18n), "story roster localization incomplete")

    # Carry forward the story trigger and safety invariants.
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first Mutant trigger regressed")
    req('StageKey = "Ronvotri.TeamUp/SurgeNarrativeStage"' in origin, "Linus/Marlon story bridge regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "default five-person cap regressed")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire policy regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("MARLON STAGE 2 -> FIRST NPC ALLY SLOT: PASS")
    log("STORY NPC SLOT RANGE 0..4: PASS")
    log("FIVE PEOPLE TOTAL INCLUDING ONLINE FARMERS: PASS")
    log("AUTHORITATIVE RECRUIT STORY GATE: PASS")
    log("INACTIVE MEMBER REACTIVATION STORY GATE: PASS")
    log("CODEX STORY-LOCK / TEAM-LIMIT STATUS: PASS")
    log("FUTURE CHAPTER SLOT-UNLOCK HOOK: PASS")
    log("6.7.25/6.7.26 STORY CARRY-FORWARD: PASS")
    log("PELIPPER/CAPTURE/COMPANION SAFETY CARRY-FORWARD: PASS")
    log("LIVE ROSTER PROGRESSION TEST STILL REQUIRED")

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
        "teamup_roster_story",
        "UnlockedNpcSlots",
        "TeamUpRosterProgressionService",
        "profile.status-story-locked",
        "teamup_story_intro",
        "teamup_surge_story",
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
        "# Team Up Alpha 6.7.27 - Story Roster Progression Audit\n\n"
        "## Implemented\n"
        "- Team Up recruitment is now story-gated instead of exposing all NPC capacity immediately.\n"
        "- Before the Marlon bridge completes, ordinary NPC recruitment is locked.\n"
        "- Marlon story stage 2 unlocks the first NPC ally slot.\n"
        "- Story allowance is persisted as 0..4 NPC slots and has an UnlockTo hook for later chapters.\n"
        "- The hard formation rule is five people total, including every online Farmer. Multiplayer can therefore reduce effective NPC slots below the story allowance.\n"
        "- Host-authoritative recruit and inactive-member reactivation both enforce the story allowance.\n"
        "- Codex/profile status distinguishes Team Up Locked and Team Limit Reached from Special/Companion.\n"
        "- Existing saves at Marlon stage 2 silently receive the first slot on load.\n\n"
        "## Deliberately not implemented yet\n"
        "- Story milestones that unlock NPC slots 2, 3, and 4.\n"
        "- Old mine investigation chapters and evidence chain.\n"
        "- George reveal/final boss and Evelyn postgame Awakening.\n\n"
        "CI verifies source/build invariants only. Live recruitment, multiplayer, and UI pacing still require gameplay verification.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.27 - STORY ROSTER PROGRESSION\n"
        "==========================================\n\n"
        "CAI DAT:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n\n"
        "TEST NHANH:\n"
        "1. Mo save host. Go: teamup_story_intro reset\n"
        "2. Go: teamup_roster_story reset\n"
        "3. Kiem tra 1 NPC thuong: Team Up phai bi khoa, khong the recruit.\n"
        "4. De test nhanh, can The Surge da active roi go: teamup_story_intro stage 2\n"
        "5. Go: teamup_roster_story sync\n"
        "6. Go: teamup_roster_story status. Ky vong unlockedNpcSlots=1/4.\n"
        "7. Recruit NPC thu nhat: thanh cong.\n"
        "8. Thu recruit NPC thu hai: bi chan boi gioi han story 1/1.\n"
        "9. Go: teamup_roster_story setslots 2. NPC thu hai luc nay phai recruit duoc.\n"
        "10. setslots 4 chi mo toi da 4 NPC, nhung tong Farmer + NPC van khong duoc vuot 5 nguoi.\n\n"
        "LENH DEBUG:\n"
        "teamup_roster_story status\n"
        "teamup_roster_story reset\n"
        "teamup_roster_story setslots 0\n"
        "teamup_roster_story setslots 1\n"
        "teamup_roster_story setslots 2\n"
        "teamup_roster_story setslots 3\n"
        "teamup_roster_story setslots 4\n"
        "teamup_roster_story sync\n\n"
        "FILE CHAN DOAN:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Roster_Progression_latest.txt\n\n"
        "Neu crash/error, THOAT GAME NGAY va gui:\n"
        "%appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.27")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
