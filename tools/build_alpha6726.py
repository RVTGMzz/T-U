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
STAGE = ROOT / "_stage_alpha6726"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6726.txt"
AUDIT = ROOT / "FIRST_SURGE_STORY_BRIDGE_AUDIT_ALPHA6726.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_26_STORY_BRIDGE_VI.txt"
VERSION = "0.2.0-alpha.6.7.26"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.26_LINUS_MARLON_STORY_BRIDGE_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.26_LINUS_MARLON_STORY_BRIDGE_TEST.sha256.txt"

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
    origin = text("Story/OriginStoryService.cs")
    surge_story = text("Story/TheSurgeStoryService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    alpha6726 = text("ModEntry.Alpha6726.cs")
    config = text("ModConfig.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    default_i18n = json.loads(text("i18n/default.json"))
    vi_i18n = json.loads(text("i18n/vi.json"))
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6726Events();" in entry, "6.7.26 registration missing")
    req("new OriginStoryService(Helper, Monitor, () => Config.EnableOriginStory)" in entry,
        "main entry still uses obsolete origin constructor")

    req('StageKey = "Ronvotri.TeamUp/SurgeNarrativeStage"' in origin, "new narrative stage key missing")
    req("TheSurgeStoryService.ActiveInstance?.IsActivated == true" in origin,
        "Origin narrative is not gated by committed first Surge activation")
    req('name.Equals("Forest"' in origin and 'ShowLine("origin.linus.first-surge")' in origin,
        "Linus first-Surge beat missing")
    req('name.Equals("AdventureGuild"' in origin and 'ShowLine("origin.marlon.first-surge")' in origin,
        "Marlon first-Surge beat missing")
    req('"origin.quest.linus"' in origin and '"origin.quest.marlon"' in origin,
        "story objective prompts missing")
    req("CombatSeenKey" not in origin, "legacy mere-combat trigger still exists")
    req("origin.awakening" not in origin and "origin.marlon.teamup" not in origin,
        "obsolete prototype auto-Awakening/instant Team Up completion still active")
    req("_stage >= 2" in origin and "Math.Clamp(stage, 0, 2)" in origin,
        "6.7.26 bridge does not stop after Marlon investigation")

    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill threshold regressed")
    req("ForceFirstMutation" in surge_story and "CommitFirstMutation" in surge_story,
        "first Mutant commitment flow regressed")
    req("ObserveEligibleDeath(monster)" in mutation and "CommitFirstMutation(monster)" in mutation,
        "mutation engine no longer bridges to Surge story")

    req('"teamup_story_intro"' in alpha6726, "story bridge diagnostic command missing")
    req("Origin.ResetStory(Game1.player);" in alpha6726, "story reset command missing")
    req("SurgeStoryAlpha6725.Reset" not in alpha6726,
        "story-intro reset must not erase the 10-kill Surge activation")
    req("TeamUp_Story_Intro_latest.txt" in alpha6726, "story bridge diagnostic file missing")

    required_keys = {
        "origin.quest.linus",
        "origin.linus.first-surge",
        "origin.quest.marlon",
        "origin.marlon.first-surge",
        "origin.first-surge-bridge.complete",
    }
    req(set(default_i18n.keys()) == set(vi_i18n.keys()), "EN/VI i18n key parity failed")
    req(required_keys.issubset(default_i18n.keys()) and required_keys.issubset(vi_i18n.keys()),
        "first Surge narrative localization incomplete")
    req("Someone outside the Guild" in default_i18n["origin.marlon.first-surge"],
        "anonymous-hero Marlon hint missing")
    req("Một người không thuộc Hội" in vi_i18n["origin.marlon.first-surge"],
        "Vietnamese anonymous-hero Marlon hint missing")

    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person total party cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regression")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("FIRST MUTANT -> LINUS STORY GATE: PASS")
    log("LINUS -> MARLON OBJECTIVE BRIDGE: PASS")
    log("MARLON ANONYMOUS HERO HINT: PASS")
    log("LEGACY MERE-COMBAT ORIGIN TRIGGER RETIRED: PASS")
    log("LEGACY AUTO-AWAKENING / INSTANT COMPLETION RETIRED: PASS")
    log("6.7.25 TEN-KILL FIRST MUTANT CARRY-FORWARD: PASS")
    log("6.7.24 CODEX / SECRET-RANK FOUNDATION CARRY-FORWARD: PASS")
    log("PELIPPER/CAPTURE/PARTY SAFETY CARRY-FORWARD: PASS")
    log("LIVE STORY PRESENTATION TEST STILL REQUIRED")

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
        "teamup_story_intro",
        "SurgeNarrativeStage",
        "LinusPromptShown",
        "origin.linus.first-surge",
        "origin.marlon.first-surge",
        "teamup_surge_story",
        "CodexObservedRank",
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
        "# Team Up Alpha 6.7.26 - First Surge Story Bridge Audit\n\n"
        "## Implemented\n"
        "- The main-story opening now waits for the successfully committed tenth-kill Mutant from 6.7.25.\n"
        "- The old 'any monster seen' origin trigger is retired.\n"
        "- After the first Mutant, the player receives an objective to seek Linus.\n"
        "- Entering Forest advances the Linus beat and points the player to Marlon.\n"
        "- Entering AdventureGuild advances the Marlon beat. Marlon hints that an unnamed outsider stopped a similar incident in an old Guild record.\n"
        "- The bridge deliberately stops there. The obsolete automatic Awakening and instant Team Up completion are retired.\n"
        "- Story intro state uses a fresh Farmer.modData key so old prototype OriginStage values cannot skip the new narrative.\n"
        "- teamup_story_intro provides status/reset/stage diagnostics without changing the 10-kill Surge activation.\n\n"
        "## Deliberately not implemented yet\n"
        "- Progressive recruit-slot unlocks.\n"
        "- Full Marlon investigation chapters / old mine evidence.\n"
        "- George reveal, final boss, Evelyn postgame Awakening.\n"
        "- Cinematic NPC placement. This checkpoint uses low-risk dialogue presentation and needs live pacing verification.\n\n"
        "CI verifies source/build invariants only. Live gameplay verification is still required.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.26 - LINUS / MARLON FIRST SURGE STORY BRIDGE\n"
        "========================================================\n\n"
        "CAI DAT:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n\n"
        "TEST NHANH TU DAU:\n"
        "1. Mo save voi host.\n"
        "2. Go: teamup_surge_story reset\n"
        "3. Go: teamup_story_intro reset\n"
        "4. Go: teamup_surge_story setkills 9\n"
        "5. Ha 1 monster thuong hop le. Monster thu 10 phai bien thanh Mutant dau tien.\n"
        "6. Sau do phai co objective tim Linus.\n"
        "7. Di vao Forest: thoai Linus phai xuat hien, sau do objective chuyen sang tim Marlon.\n"
        "8. Di vao AdventureGuild: thoai Marlon phai xuat hien va hint ve mot nguoi ngoai Guild tung ngan su co tuong tu.\n"
        "9. Sau scene Marlon KHONG duoc tu dong Awakening NPC va KHONG duoc tu dong ket thuc toan bo main story.\n\n"
        "KIEM TRA TRANG THAI:\n"
        "teamup_story_intro status\n"
        "Khi xong Marlon ky vong: stage=2/2 objective=marlon-investigation-open\n\n"
        "FILE CHAN DOAN:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Story_Intro_latest.txt\n\n"
        "Neu crash/error, THOAT GAME NGAY va gui:\n"
        "%appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.26")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
