from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6725"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6725.txt"
AUDIT = ROOT / "FIRST_SURGE_TRIGGER_AUDIT_ALPHA6725.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_25_FIRST_SURGE_TRIGGER_VI.txt"
VERSION = "0.2.0-alpha.6.7.25"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.25_FIRST_SURGE_TRIGGER_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.25_FIRST_SURGE_TRIGGER_TEST.sha256.txt"

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
    mutation = text("Combat/MonsterMutationService.cs")
    surge = text("Combat/MonsterSurgeService.cs")
    story = text("Story/TheSurgeStoryService.cs")
    alpha6725 = text("ModEntry.Alpha6725.cs")
    discovery = text("Core/CodexDiscoveryService.cs")
    assessment = text("Core/CodexAssessmentService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    alpha6723 = text("ModEntry.Alpha6723.cs")
    config = text("ModConfig.cs")
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6725Events();" in entry, "6.7.25 registration missing")
    req('"teamup_surge_story"' in alpha6725, "Surge story diagnostic command missing")
    req("TeamUp_Surge_Story_latest.txt" in alpha6725, "Surge story diagnostic output missing")

    # Persistent deterministic prologue.
    req("FirstMutationKillThreshold = 10" in story, "first mutation threshold is not 10")
    req('"Ronvotri.TeamUp/SurgeStory/Kills"' in story, "persistent Surge kill key missing")
    req('"Ronvotri.TeamUp/SurgeStory/Activated"' in story, "persistent Surge activation key missing")
    req('"Ronvotri.TeamUp/SurgeStory/DeathCounted"' in story, "duplicate death guard missing")
    req("monster.modData.ContainsKey(CountedDeathMarker)" in story, "same-actor death double-count protection missing")
    req("SurgeMutationDirective.SuppressMutation" in story, "pre-trigger mutation suppression missing")
    req("SurgeMutationDirective.ForceFirstMutation" in story, "tenth-defeat forced mutation decision missing")
    req("farmer.modData[ActivatedKey] = \"1\";" in story, "Surge activation commit missing")

    # Mutation engine must obey the story directive without weakening the generic source-safety path.
    req("TheSurgeStoryService.ActiveInstance?.ObserveEligibleDeath(monster)" in mutation,
        "mutation engine is not observing eligible lethal defeats")
    req("storyDirective == SurgeMutationDirective.SuppressMutation" in mutation,
        "random mutation can still occur before story activation")
    req("storyDirective != SurgeMutationDirective.ForceFirstMutation" in mutation,
        "tenth eligible lethal defeat is not bypassing the random roll")
    req("TheSurgeStoryService.ActiveInstance?.CommitFirstMutation(monster)" in mutation,
        "successful first mutant does not commit Surge activation")
    req("Game1.random.NextDouble()" in mutation,
        "post-activation configured random mutation roll was removed")
    req("force: true" in mutation,
        "existing developer force-mutation path regression")

    # Density is narrative-locked until the first mutation successfully exists.
    req("Config.EnableMonsterSurge && (SurgeStoryAlpha6725?.IsActivated ?? false)" in entry,
        "density overlay is not gated by Surge story activation")
    req("public bool EnableMonsterSurge { get; set; } = true;" in config,
        "user config switch for density was removed")
    req("MonsterDensityMultiplier { get; set; } = 2.5f" in config,
        "2.5x density configuration regression")

    # The previous Codex narrative foundation remains intact.
    req("farmer.friendshipData.Keys" in discovery, "6.7.24 existing-save Codex seed regression")
    req("InitialRankD" in assessment and '"George"' in assessment and '"Evelyn"' in assessment,
        "6.7.24 observed-rank camouflage regression")
    req("SetObservedRank" in assessment, "future rank-revision hook regression")

    # Combat/source ownership locks remain untouched.
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy,
        "6.7.23 capture ceasefire regression")
    req('"teamup_capture_ceasefire"' in alpha6723,
        "capture diagnostic regression")
    req("PelipperTownCompatibilityService.IsWildCombatActor(monster)" in surge,
        "Pelipper density fail-closed regression")
    req("public int MaxPartyMembers { get; set; } = 5;" in config,
        "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config,
        "2/2 companion cap regression")
    req("TrySetActorInvisibleAlpha6613" not in all_source,
        "legacy Pelipper visibility takeover returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("THE SURGE PERSISTENT STORY STATE: PASS")
    log("ELIGIBLE DEFEATS 1-9 SUPPRESS RANDOM MUTATION: PASS")
    log("LETHAL DEFEAT #10 FORCES FIRST MUTANT: PASS")
    log("FIRST MUTANT SUCCESS UNLOCKS NORMAL 5% ROLLS: PASS")
    log("DENSITY OVERLAY LOCKED UNTIL SURGE ACTIVATION: PASS")
    log("DUPLICATE DEATH COUNT GUARD: PASS")
    log("6.7.24 CODEX STORY FOUNDATION: PASS")
    log("PELIPPER/CAPTURE/PARTY SAFETY CARRY-FORWARD: PASS")
    log("LIVE TEN-DEFEAT STORY TEST STILL REQUIRED")

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
        "teamup_surge_story",
        "TeamUp_Surge_Story_latest.txt",
        "SurgeStory/Kills",
        "SurgeStory/Activated",
        "ForceFirstMutation",
        "teamup_mutation",
        "teamup_codex_discovery",
        "teamup_capture_ceasefire",
        "teamup_pelipper_density_probe",
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
        "# Team Up Alpha 6.7.25 First Surge Trigger Audit\n\n"
        "## Narrative rule implemented\n"
        "The Surge does not start as an invisible always-on system anymore. Before activation, Team Up persistently counts eligible lethal monster defeats for the host. Normal random mutations are suppressed during this prologue. The tenth eligible lethal defeat is guaranteed to mutate instead of dying. Only after that transformation succeeds is The Surge marked active.\n\n"
        "## What activation unlocks\n"
        "- Existing configured random mutation chance resumes after the first mutant.\n"
        "- Existing 2.5x density overlay becomes eligible after activation.\n"
        "- Activation persists in Farmer.modData.\n"
        "- A per-monster marker prevents unusual repeated deathAnimation calls from counting one actor twice.\n\n"
        "## What is not added yet\n"
        "- Linus reaction/quest.\n"
        "- Marlon investigation or Threat Board narrative handoff.\n"
        "- George reveal/recruitment.\n"
        "- Evelyn awakening.\n\n"
        "## Compatibility\n"
        "The checkpoint does not broaden mutation eligibility. Existing exclusions for bosses, scripts, Pelipper wild/capture actors, Cardcha harness actors, mutation minions, and Surge-spawned extras remain owned by the existing mutation service. Debug force-mutation still bypasses story counting and is not allowed to activate The Surge.\n\n"
        "CI validates source/build invariants only. The ten-defeat sequence still needs a live game test.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.25 - FIRST SURGE TRIGGER\n"
        "=====================================\n\n"
        "MUC TIEU:\n"
        "9 lan ha guc dau tien khong duoc random Mutant. Don ha guc hop le thu 10 bat buoc bien chinh con quai do thanh Mutant dau tien, sau do The Surge moi chinh thuc duoc mo.\n\n"
        "CAI DAT:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n\n"
        "TEST NHANH DE KHOI PHAI GIET 9 CON:\n"
        "1. Vao save voi host.\n"
        "2. Go: teamup_surge_story reset\n"
        "3. Go: teamup_surge_story setkills 9\n"
        "4. Go: teamup_surge_story status\n"
        "5. Truoc khi danh con thu 10, diagnostic phai co activated=False, kills=9/10, randomMutationUnlocked=False, densityUnlocked=False.\n"
        "6. Ha guc 1 monster thuong hop le. Con do PHAI song lai thanh Mutant thay vi chet.\n"
        "7. Mutant dau tien van giu bo quy tac hien co: HP x3, combat stats x2, visual scale x3 neu runtime cho phep, 2-4 minion.\n"
        "8. Go lai: teamup_surge_story status\n"
        "9. Sau trigger phai co activated=True, kills=10/10, randomMutationUnlocked=True, densityUnlocked=True.\n"
        "10. Doi khoang 90 tick/di sang combat zone hop le de density overlay bat dau hoat dong theo he thong 2.5x hien co.\n\n"
        "TEST DAY DU:\n"
        "Dung teamup_surge_story reset, sau do tu danh 10 monster hop le. Ca 9 con dau phai chet binh thuong, khong con nao random mutate. Con thu 10 phai mutate.\n\n"
        "FILE DIAGNOSTIC:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Surge_Story_latest.txt\n\n"
        "LENH:\n"
        "teamup_surge_story status\n"
        "teamup_surge_story reset\n"
        "teamup_surge_story setkills 9\n"
        "teamup_mutation status\n"
        "teamup_density status\n\n"
        "Neu co crash/error, THOAT GAME NGAY va gui:\n"
        "%appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.25")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
