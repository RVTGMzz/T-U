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
STAGE = ROOT / "_stage_alpha6728"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6728.txt"
AUDIT = ROOT / "GEORGE_CAMOUFLAGE_REACTIONS_AUDIT_ALPHA6728.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_28_GEORGE_REACTIONS_VI.txt"
VERSION = "0.2.0-alpha.6.7.28"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.28_GEORGE_CAMOUFLAGE_MILESTONE_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.28_GEORGE_CAMOUFLAGE_MILESTONE_REACTIONS_TEST.sha256.txt"

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
    alpha6728 = text("ModEntry.Alpha6728.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    codex = text("Core/CodexAssessmentService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge_story = text("Story/TheSurgeStoryService.cs")
    origin = text("Story/OriginStoryService.cs")
    config = text("ModConfig.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    default_i18n = json.loads(text("i18n/default.json"))
    vi_i18n = json.loads(text("i18n/vi.json"))
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6728Events();" in entry, "6.7.28 registration missing")

    # George camouflage must be boring and non-spoilery before the future reveal.
    req('"George"' in codex and "InitialRankD" in codex, "George observed Rank D camouflage missing")
    req('codex.george.observed.ability' in codex, "George observed no-skill profile missing")
    req('GeorgeCombatRevealedKeyAlpha6728 = "Ronvotri.TeamUp/Story/GeorgeCombatRevealed"' in alpha6728,
        "future George reveal flag contract missing")
    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_source,
        "6.7.28 must never reveal George early")
    req("IsGeorgePreRevealLockedAlpha6728" in alpha6728, "George pre-reveal state gate missing")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in alpha661,
        "authoritative George recruitment block missing")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out _)" in alpha661,
        "UI George recruitment block missing")
    req("IsGeorgePreRevealLockedAlpha6728(request.CharacterName)" in alpha661,
        "inactive George reactivation block missing")
    req("EnforceGeorgePreRevealLockAlpha6728" in alpha6728,
        "old-save active George hardening missing")
    req("TryGetCharacterStoryProfileStatusAlpha6728" in entry,
        "George Codex Non-Combatant status hook missing")
    req("ShowGeorgePreRevealRecruitTeaseAlpha6728" in entry,
        "George deliberate recruit-tease hook missing")
    req("hint.recruit-try" in entry,
        "George pre-reveal dialogue invite lure missing")

    # Reactions should exist in all current between-milestone windows and be one-shot per NPC/window.
    req("StoryMilestoneReactionService" in reactions, "milestone reaction service missing")
    req('SeenPrefix = "Ronvotri.TeamUp/StoryReaction/"' in reactions, "reaction persistence prefix missing")
    req("TryConsume(Farmer farmer, int narrativeStage" in reactions, "reaction consume gate missing")
    req("[0] = firstMutant" in reactions and "[1] = afterLinus" in reactions and "[2] = afterMarlon" in reactions,
        "three opening reaction windows missing")
    req("TryShowMilestoneReactionAlpha6728(npc)" in entry, "world interaction reaction hook missing")
    req('"George", "story.react.0.george"' in reactions and '"George", "story.react.1.george"' in reactions and '"George", "story.react.2.george"' in reactions,
        "George camouflage reactions must span all three windows")
    req('"Evelyn", "story.react.0.evelyn"' in reactions and '"Evelyn", "story.react.1.evelyn"' in reactions and '"Evelyn", "story.react.2.evelyn"' in reactions,
        "Evelyn mundane-support reactions must span all three windows")
    req('"Marlon", "story.react.2.marlon"' in reactions, "Marlon post-bridge first-companion reaction missing")

    reaction_keys = {key for key in default_i18n if key.startswith("story.react.")}
    required_keys = {
        "hint.recruit-try",
        "profile.status-noncombatant",
        "story.george.pre-reveal.recruit-blocked",
        "story.george.pre-reveal.recruit-tease",
        "story.react.0.george",
        "story.react.1.george",
        "story.react.2.george",
        "story.react.0.evelyn",
        "story.react.1.evelyn",
        "story.react.2.evelyn",
        "story.react.2.marlon",
    }
    req(set(default_i18n.keys()) == set(vi_i18n.keys()), "EN/VI i18n key parity failed")
    req(required_keys.issubset(default_i18n) and required_keys.issubset(vi_i18n), "6.7.28 localization incomplete")
    req(len(reaction_keys) >= 38, f"expected at least 38 milestone reactions, found {len(reaction_keys)}")
    req(all("Rank S" not in default_i18n[key] and "Rank S" not in vi_i18n[key] for key in reaction_keys),
        "ambient reactions must not leak secret Rank S")
    req("Last Blaster" not in default_i18n["story.george.pre-reveal.recruit-tease"]
        and "Last Blaster" not in vi_i18n["story.george.pre-reveal.recruit-tease"],
        "George tease leaks future title")

    # Carry forward current story and safety invariants.
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first Mutant trigger regressed")
    req('StageKey = "Ronvotri.TeamUp/SurgeNarrativeStage"' in origin, "Linus/Marlon bridge regressed")
    req("MaxStoryNpcSlots = 4" in roster, "0..4 story NPC slot progression regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person total cap regressed")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("GEORGE OBSERVED RANK D / NON-COMBATANT CAMOUFLAGE: PASS")
    log("GEORGE PRE-REVEAL RECRUIT + REACTIVATION BLOCK: PASS")
    log("GEORGE OLD-SAVE ACTIVE ROSTER HARDENING: PASS")
    log("GEORGE SECRET S / LAST BLASTER SPOILER LEAK: PASS")
    log("THREE BETWEEN-MILESTONE REACTION WINDOWS: PASS")
    log(f"CURATED MILESTONE REACTION LOCALIZATION: PASS ({len(reaction_keys)} lines/language)")
    log("GEORGE + EVELYN MUNDANE PRE-REVEAL REACTIONS: PASS")
    log("MARLON FIRST-COMPANION POST-BRIDGE REACTION: PASS")
    log("6.7.25-6.7.27 STORY/ROSTER CARRY-FORWARD: PASS")
    log("PELIPPER/CAPTURE/PARTY SAFETY CARRY-FORWARD: PASS")
    log("LIVE DIALOGUE PACING / INPUT TEST STILL REQUIRED")

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
        "teamup_story_reactions",
        "StoryMilestoneReactionService",
        "GeorgeCombatRevealed",
        "story.george.pre-reveal.recruit-tease",
        "story.react.2.marlon",
        "teamup_roster_story",
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
        "# Team Up Alpha 6.7.28 - George Camouflage + Milestone Reactions Audit\n\n"
        "## Implemented\n"
        "- George remains a clean Codex false-negative before his future main-story reveal: observed Rank D, Non-Combatant, and no visible combat skill. There is no ??? marker and no Rank S/Last Blaster leak.\n"
        "- Pressing the Team Up Recruit input while speaking to George deliberately produces the 'really invite an old man in a wheelchair?' tease, but recruitment stays blocked.\n"
        "- Host-authoritative recruit requests, inactive-member reactivation, and old-save active George entries are all hardened against pre-reveal combat.\n"
        "- The future GeorgeCombatRevealed flag contract exists but this checkpoint never sets it. A later finale checkpoint owns that reveal.\n"
        "- Added one-time NPC reactions for all three current story windows: first Mutant before Linus, after Linus before Marlon, and after Marlon when the first ally slot opens.\n"
        f"- Added {len(reaction_keys)} curated reaction lines per language across Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Maru, Pierre, Robin, Wizard, plus post-bridge Linus/Marlon.\n"
        "- George's reactions are intentionally mundane. Evelyn's reactions remain caring/domestic and do not foreshadow her postgame Rank S reveal.\n\n"
        "## Presentation rule\n"
        "- A milestone reaction is consumed only when the player interacts empty-handed with that NPC during the matching story window. It replaces that single interaction, then ordinary vanilla/Team Up interaction resumes next time.\n"
        "- Missing an old window does not replay stale dialogue after the story advances.\n\n"
        "## Deliberately not implemented yet\n"
        "- George Rank S reveal, The Last Blaster kit, or any way to set GeorgeCombatRevealed through normal gameplay.\n"
        "- Main-story chapters that unlock ally slots 2, 3, and 4. Those future milestones should add their own reaction windows.\n"
        "- Evelyn postgame Awakening / Grandmother's Garden.\n\n"
        "CI verifies code, localization and spoiler boundaries only. Live dialogue pacing/input still requires gameplay verification.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.28 - GEORGE CAMOUFLAGE + NPC MILESTONE REACTIONS\n"
        "=============================================================\n\n"
        "CAI DAT:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n\n"
        "A. GEORGE TRUOC REVEAL:\n"
        "1. Gap George it nhat 1 lan de mo Codex entry.\n"
        "2. Codex George phai hien Rank D, trang thai 'Khong tham chien', va ky nang dac trung = Khong co.\n"
        "3. Sau khi story roster da mo, noi chuyen voi George roi bam phim Recruit (E / R controller).\n"
        "4. Phai hien cau tease kieu 'that su dinh moi mot ong lao ngoi xe lan di san quai sao?' va George KHONG gia nhap.\n"
        "5. George khong duoc co ???, Rank S, Last Blaster, Hard Stop trong thong tin observed.\n\n"
        "B. PHAN UNG GIUA CAC COT MOC:\n"
        "1. Go: teamup_story_reactions reset\n"
        "2. Sau Mutant dau tien, khi story stage=0, tay rong noi chuyen Abigail/George/Evelyn/...: lan dau phai co reaction rieng. Lan thu hai khong lap lai reaction do.\n"
        "3. Sau Linus, stage=1: cung NPC do co reaction MOI cua moc Linus.\n"
        "4. Sau Marlon, stage=2: reaction lai doi; Marlon co cau 'Ky nang quan trong. Long tin con quan trong hon.'\n"
        "5. George o ca 3 moc phai noi nhu mot ong gia binh thuong, khong tu tiet lo qua khu. Evelyn cung khong duoc lo Rank S.\n\n"
        "LENH:\n"
        "teamup_story_reactions status\n"
        "teamup_story_reactions reset\n"
        "teamup_story_intro stage 0\n"
        "teamup_story_intro stage 1\n"
        "teamup_story_intro stage 2\n\n"
        "FILE CHAN DOAN:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Milestone_Reactions_latest.txt\n\n"
        "Neu crash/error, THOAT GAME NGAY va gui:\n"
        "%appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.28")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
