from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6722"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6722.txt"
AUDIT = ROOT / "PELIPPER_WILD_DENSITY_SURFACE_PROBE_AUDIT_ALPHA6722.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_22_PELIPPER_DENSITY_PROBE_VI.txt"
VERSION = "0.2.0-alpha.6.7.22"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.22_PELIPPER_WILD_DENSITY_SURFACE_PROBE_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.22_PELIPPER_WILD_DENSITY_SURFACE_PROBE_TEST.sha256.txt"

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
    probe = text("Core/PelipperWildDensitySurfaceProbe.cs")
    command = text("ModEntry.Alpha6722.cs")
    surge = text("Combat/MonsterSurgeService.cs")
    config = text("ModConfig.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    capture = text("Core/PelipperCaptureSafetyService.cs")
    alpha6714 = text("ModEntry.Alpha6714.cs")
    alpha6716 = text("ModEntry.Alpha6716.cs")
    combat = text("Combat/CombatService.cs")
    rank = text("Core/CombatRankCatalog.cs")
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6722Events();" in entry, "6.7.22 registration missing")
    req('"teamup_pelipper_density_probe"' in command, "probe command missing")
    req("Pelipper_Wild_Density_Probe_latest.txt" in command, "probe diagnostic output missing")
    req("PelipperModRuntimeRootLocator.TryLocate" in command, "runtime-root lookup missing")

    # Probe must be read-only against Pelipper. Metadata and primitive GetValue reads are allowed;
    # unknown source methods are never invoked and source state is never written.
    req("assembly.GetTypes()" in probe, "assembly metadata scan missing")
    req("GetMethods" in probe, "method signature scan missing")
    req(".Invoke(" not in probe, "probe invokes a Pelipper method")
    req("SetValue(" not in probe, "probe writes a field/property")
    req("location.characters.Add" not in probe, "probe fabricates an actor")
    req("Activator.CreateInstance" not in probe, "probe constructs Pelipper runtime actors")
    req("FormatterServices" not in probe and "MemberwiseClone" not in probe, "unsafe clone path returned")
    req("recommendation=\"" not in probe, "recommendation should be runtime-derived")
    req('return "EXACT_CANDIDATE";' in probe and 'return "AMBIGUOUS";' in probe and 'return "NONE";' in probe,
        "probe recommendation classifier missing")
    req("visibleWild" in probe and "combatProxy" in probe, "wild/proxy live counts missing")
    req("PelipperTownCompatibilityService.IsWildCombatActor" in probe, "canonical wild/proxy identity not reused")
    req("METHOD score=" in probe and "VALUE score=" in probe, "candidate diagnostics missing")

    # 6.7.21 universal density stays fail-closed for Pelipper until the source surface is proven.
    req("public float MonsterDensityMultiplier { get; set; } = 2.5f;" in config, "x2.5 density default regression")
    req("public int MonsterSurgeExtraCap { get; set; } = 36;" in config, "density extra cap regression")
    req("PelipperTownCompatibilityService.IsWildCombatActor(monster)" in surge, "Pelipper density boundary missing")
    req("pelipper-only-source-owned-by-pelipper" in surge, "Pelipper fail-closed density diagnostic regression")
    req("UniversalMonsterDensitySpawnFactory.TryCreate" in surge, "custom density factory regression")
    req("LooksLikeCombatZone(location)" not in surge, "legacy map-name gate returned")

    # Locked carry-forward invariants.
    req("public float MutationChancePercent { get; set; } = 5f;" in config, "mutation 5% regression")
    req("public float MutationHealthMultiplier { get; set; } = 3f;" in config, "mutation HPx3 regression")
    req("RepairCurrentLocationFloors" in capture, "Pelipper capture floor regression")
    req("NpcOnlySourceLockPulseTicksAlpha6714 = 10" in alpha6714, "NPC-only lock regression")
    req('"teamup_diag_all"' in alpha6716, "combined diagnostics regression")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat, "boss/add coordination regression")
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "Rank S contrast regression")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("PELIPPER WILD DENSITY PROBE STATIC AUDIT: PASS (metadata + primitive reads only)")
    log("NO SOURCE INVOCATION/WRITE STATIC AUDIT: PASS")
    log("WILD/PROXY IDENTITY STATIC AUDIT: PASS")
    log("6.7.21 UNIVERSAL DENSITY FAIL-CLOSED CARRY-FORWARD: PASS")
    log("LOCKED PARTY/PELIPPER/CAPTURE/MUTATION/RANK INVARIANTS: PASS")
    log("LIVE PROBE OUTPUT STILL REQUIRED BEFORE A NATIVE PELIPPER DENSITY ADAPTER")

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
        "teamup_pelipper_density_probe",
        "Pelipper_Wild_Density_Probe_latest.txt",
        "EXACT_CANDIDATE",
        "AMBIGUOUS",
        "combatProxy",
        "teamup_density",
        "teamup_mutation",
        "teamup_diag_all",
    ]:
        req(dll_contains(blob, token), f"DLL missing token: {token}")
    log("BINARY ACCEPTANCE: PASS")

    RELEASE.mkdir(parents=True, exist_ok=True)
    if STAGE.exists():
        shutil.rmtree(STAGE)
    MOD_STAGE.mkdir(parents=True)
    shutil.copy2(dll, MOD_STAGE / "TeamUp.dll")
    manifest = (SRC / "manifest.json").read_text(encoding="utf-8").replace("%ProjectVersion%", VERSION)
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
        "# Team Up Alpha 6.7.22 Pelipper Wild Density Surface Probe Audit\n\n"
        "## Why this checkpoint exists\n"
        "Pelipper's logs prove that wild encounters are source-created as a visible NPC plus a combat proxy, and that entry population has a source-side desired count. The exact runtime method/member controlling that desired count is not yet verified in repository metadata.\n\n"
        "## 6.7.22 behavior\n"
        "- Adds teamup_pelipper_density_probe.\n"
        "- Locates Pelipper's already-bound/live ModEntry through the existing runtime-root locator.\n"
        "- Scans Pelipper assembly type/method signatures for Wild/Population/Entry/Encounter/Spawn surfaces.\n"
        "- Reads only strongly named primitive runtime fields/properties to expose likely desired/target/cap values.\n"
        "- Counts current visible wild actors and Monster combat proxies using Team Up's existing canonical Pelipper identity logic.\n"
        "- Writes diagnostics/Pelipper_Wild_Density_Probe_latest.txt.\n\n"
        "## Authority boundary\n"
        "- No unknown Pelipper method invocation.\n"
        "- No Pelipper field/property writes.\n"
        "- No source actor construction or location.characters insertion.\n"
        "- 6.7.21 continues to leave Pelipper wild density source-owned and fail-closed.\n\n"
        "## Next gate\n"
        "A later native adapter should only be built after a live probe reports an exact, unambiguous source-owned population target/spawn route.\n\n"
        "CI validates source/build invariants only. Runtime Pelipper surface discovery remains live-unverified.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.22 - PELIPPER WILD DENSITY SURFACE PROBE\n"
        "=====================================================\n\n"
        "Ban nay CHUA tang mat do Pokemon hoang. Day la checkpoint doc-only voi Pelipper de tim dung source surface truoc khi can thiep.\n\n"
        "TEST KHI VE NHA:\n"
        "1. Cai ban 6.7.22 vao E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n"
        "2. Vao Town/Forest/Beach noi Pelipper da spawn Pokemon hoang.\n"
        "3. Go: teamup_pelipper_density_probe\n"
        "4. File se duoc tao tai:\n"
        "   E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\Pelipper_Wild_Density_Probe_latest.txt\n"
        "5. Gui dung file do de build native adapter tiep theo.\n\n"
        "MONG DOI TRONG FILE:\n"
        "- root + assembly/version\n"
        "- visibleWild + combatProxy\n"
        "- METHOD score=...\n"
        "- VALUE score=...\n"
        "- recommendation=EXACT_CANDIDATE / AMBIGUOUS / NONE\n\n"
        "AN TOAN:\n"
        "- Probe khong goi method spawn/population cua Pelipper.\n"
        "- Probe khong ghi config/state Pelipper.\n"
        "- Probe khong clone Pokemon/proxy.\n"
        "- 6.7.21 universal density + mutation/capture/party rules duoc giu nguyen.\n\n"
        "Neu co loi SMAPI: thoat game ngay va lay %appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n"
        "CI PASS KHONG dong nghia Pelipper native density da live-verified.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.22")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
