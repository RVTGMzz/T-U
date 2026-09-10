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
STAGE = ROOT / "_stage_alpha6724"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6724.txt"
AUDIT = ROOT / "CODEX_DISCOVERY_AUDIT_ALPHA6724.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_24_CODEX_DISCOVERY_VI.txt"
VERSION = "0.2.0-alpha.6.7.24"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.24_CODEX_DISCOVERY_OBSERVED_RANKS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.24_CODEX_DISCOVERY_OBSERVED_RANKS_TEST.sha256.txt"

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
    discovery = text("Core/CodexDiscoveryService.cs")
    assessment = text("Core/CodexAssessmentService.cs")
    codex = text("UI/CodexBrowserMenu.cs")
    profile = text("UI/CharacterProfileMenu.cs")
    alpha6724 = text("ModEntry.Alpha6724.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    alpha6723 = text("ModEntry.Alpha6723.cs")
    config = text("ModConfig.cs")
    surge = text("Combat/MonsterSurgeService.cs")
    default_i18n = json.loads(text("i18n/default.json"))
    vi_i18n = json.loads(text("i18n/vi.json"))
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6724Events();" in entry, "6.7.24 event registration missing")
    req("CodexDiscovery.OnSaveLoaded(Game1.player, Party.Members);" in entry, "legacy-safe discovery seed missing")
    req("CodexDiscovery.GetDiscoveredProfiles(Game1.player" in entry, "Codex still receives omniscient roster")
    req("CodexDiscovery.Discover(Game1.player, characterName);" in entry, "direct profile does not record discovery")

    req("farmer.friendshipData.Keys" in discovery, "existing-save social seed missing")
    req("member.RecruiterId == farmer.UniqueMultiplayerID" in discovery, "existing Team Up party seed missing")
    req("DiscoveryPrefix" in discovery and "farmer.modData" in discovery, "discovery is not persisted per Farmer")
    req(".Where(profile => IsDiscovered(farmer, profile.CharacterName))" in discovery, "undiscovered profile filter missing")

    req('"George"' in assessment and '"Evelyn"' in assessment, "secret low-rank camouflage missing")
    for genuine_d in ["Pierre", "Lewis", "Elliott", "Caroline", "Jodi", "Gil"]:
        req(f'"{genuine_d}"' in assessment, f"genuine Rank D camouflage missing: {genuine_d}")
    req("return new CombatRankInfo(CombatRank.D);" in assessment, "observed Rank D resolver missing")
    req("CombatRankCatalog.Get(characterName, profile)" in assessment, "ordinary observed rank fallback missing")
    req("SetObservedRank" in assessment and "RevisionPrefix" in assessment, "future Combat Assessment Revised hook missing")
    req("PartyRole.Unassigned" in assessment and "codex.george.observed.ability" in assessment,
        "George pre-reveal non-combat dossier overlay missing")

    req("CodexAssessmentService.GetObservedRank(Game1.player, profile.CharacterName, profile)" in codex,
        "Codex rows still expose real rank")
    req("CodexAssessmentService.GetObservedRank(Game1.player, _characterName, _profile)" in profile,
        "character dossier still exposes real rank")
    req("???" not in assessment and "???" not in discovery,
        "secret-rank question-mark spoiler introduced")

    req(set(default_i18n.keys()) == set(vi_i18n.keys()), "EN/VI i18n key parity failed")
    req("codex.george.observed.passive" in default_i18n and "codex.george.observed.ability" in vi_i18n,
        "George observed dossier localization missing")

    # Fix identified in the 6.7.23 recheck: do not hardcode obsolete build versions in startup logs.
    req("build: v{ModManifest.Version}" in entry, "startup debug version is still hardcoded")
    req("Team Up! v{ModManifest.Version}" in entry, "startup load version is still hardcoded")
    req("v0.2.0-alpha.6.6.24" not in entry, "obsolete 6.6.24 startup label remains")

    # Carry forward safety locks while touching UI/story foundations.
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "6.7.23 capture ceasefire policy regression")
    req('"teamup_capture_ceasefire"' in alpha6723, "6.7.23 capture diagnostic regression")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person total party cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("public float MonsterDensityMultiplier { get; set; } = 2.5f;" in config, "2.5x density default regression")
    req("PelipperTownCompatibilityService.IsWildCombatActor(monster)" in surge, "Pelipper density fail-closed regression")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("CODEX FIRST-MEETING DISCOVERY: PASS")
    log("EXISTING-SAVE FRIENDSHIP/PARTY SEED: PASS")
    log("OBSERVED RANK != COMBAT RANK SEPARATION: PASS")
    log("GEORGE/EVELYN SECRET-RANK CAMOUFLAGE FOUNDATION: PASS")
    log("GENUINE RANK D RED-HERRING SET: PASS")
    log("NO ??? SECRET-RANK SPOILER: PASS")
    log("DYNAMIC STARTUP VERSION LOG: PASS")
    log("PELIPPER/CAPTURE/PARTY/DENSITY CARRY-FORWARD: PASS")

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
        "teamup_codex_discovery",
        "CodexDiscoveryService",
        "CodexAssessmentService",
        "CodexDiscovered",
        "CodexObservedRank",
        "teamup_capture_ceasefire",
        "teamup_pelipper_density_probe",
        "teamup_density",
        "teamup_mutation",
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
        "# Team Up Alpha 6.7.24 Codex Discovery / Observed Rank Audit\n\n"
        "This checkpoint implements the first narrative foundation only.\n\n"
        "## Implemented\n"
        "- Codex entries are filtered to NPCs the current Farmer has discovered.\n"
        "- Existing saves seed discovery from Stardew friendshipData and Team Up party history.\n"
        "- Direct conversations/actions discover NPCs, with a periodic friendship sync for event-based introductions.\n"
        "- Observed Codex rank is separated from real CombatRankCatalog rank.\n"
        "- George and Evelyn initially present as Rank D without a question-mark spoiler.\n"
        "- George's observed dossier hides the prototype combat kit and presents him as non-combat.\n"
        "- Pierre, Lewis, Elliott, Caroline, Jodi, and Gil are reserved as genuine Rank D observed assessments.\n"
        "- A generic SetObservedRank hook exists for later Combat Assessment Revised story reveals.\n"
        "- Marlon and MiMi are not artificially downgraded by this layer.\n"
        "- Startup logs now read ModManifest.Version instead of stale 6.6.24 text.\n\n"
        "## Deliberately not implemented yet\n"
        "- The tenth-kill forced Mutation story trigger.\n"
        "- Progressive party-slot story gates.\n"
        "- George recruitment lock/reveal and final boss.\n"
        "- Evelyn postgame Awakening.\n\n"
        "CI validates source/build invariants only. Codex discovery still requires a live UI/save test before promotion.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.24 - CODEX DISCOVERY + OBSERVED RANK\n"
        "=================================================\n\n"
        "MUC TIEU:\n"
        "Codex khong con biet truoc toan bo NPC. Gap NPC roi moi co ho so, va Rank bi mat khong dung ???.\n\n"
        "TEST NHANH:\n"
        "1. Cai mod vao E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n"
        "2. Mo save. Go: teamup_codex_discovery status\n"
        "3. NPC da quen tren save cu phai van co trong Codex.\n"
        "4. Gap/noi chuyen voi mot NPC chua co trong Codex, mo lai Codex va kiem tra NPC vua duoc them.\n"
        "5. George neu da duoc discover: Rank D, role/affinity combat bi an di theo observed dossier, khong co ???.\n"
        "6. Evelyn neu da discover: Rank D voi Garden Remedy yeu, khong co dau hieu Rank S.\n"
        "7. Marlon: van hien Rank S Legendary neu da discover.\n"
        "8. Thu dong/mo game de xac nhan discovery van duoc luu trong Farmer.modData.\n\n"
        "LENH DEBUG:\n"
        "teamup_codex_discovery status\n"
        "teamup_codex_discovery discover George\n"
        "teamup_codex_discovery seed\n"
        "teamup_codex_discovery reset\n\n"
        "LUU Y: reset chi dung de test. Sau reset, go seed de nap lai NPC da biet tu friendship/party.\n\n"
        "Neu co crash/error, THOAT GAME NGAY va gui:\n"
        "%appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.24")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
