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
STAGE = ROOT / "_stage_alpha6744_35"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_35.txt"
VERSION = "0.2.0-alpha.6.7.44.35"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.35_LEADER_PURSUIT_REACH_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.35_LEADER_PURSUIT_REACH_TEST.sha256.txt"
lines: list[str] = []


def log(value: str) -> None:
    print(value)
    lines.append(value)


def req(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def text(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


try:
    project = text("TeamUp.csproj")
    manifest = json.loads(text("manifest.json"))
    pairing = text("Core/Alpha674410PelipperSpeciesPairingService.cs")
    aggro = text("Core/Alpha674420MutationAggroService.cs")
    steering = text("Core/Alpha674423PelipperMutantLeaderSmoothingService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    wiring = text("ModEntry.Alpha67446.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    log("VERSION IDENTITY 6.7.44.35: PASS")

    forbidden = [
        "Core/Alpha674424EliteCombatFinalizationService.cs",
        "Core/Alpha674424EliteReachOverlayService.cs",
        "Core/Alpha674425PelipperLeaderContinuousChaseService.cs",
        "Core/Alpha674426PelipperMutantPhaseLifecycleService.cs",
        "Core/Alpha674427MutationRegressionGuardService.cs",
        "Core/Alpha674428LowerWorkingsRuntimeGateService.cs",
    ]
    for rel in forbidden:
        req(not (SRC / rel).exists(), f"post-44.23 crash-stack leaked into 44.35: {rel}")
    req("teamup_lower_runtime" not in text("ModEntry.Alpha6744.cs"), "Lower Workings runtime gate leaked into 44.35")
    log("POST-44.23 CRASH-STACK EXCLUSION: PASS")

    req("pokemonnpcencounter" in pairing.lower(), "PokemonNpcEncounter/v1 source identity support missing")
    req('.Replace("♂", " Male ", StringComparison.Ordinal)' in pairing, "Nidoran male pairing guard missing")
    req('.Replace("♀", " Female ", StringComparison.Ordinal)' in pairing, "Nidoran female pairing guard missing")
    log("44.33 SOURCE-ID PAIRING CARRY-FORWARD: PASS")

    req("BaseAggroRadiusTiles = 6" in aggro and "MutationAggroRadiusTiles = BaseAggroRadiusTiles * 3" in aggro,
        "44.34 x3 aggro arena missing")
    req("PelipperMutantDamageFloor = 8" in aggro and "PelipperOrdinaryDamageFloor = 4" in aggro,
        "44.34 damage floors missing")
    req("MutationIntendedDamageMarker" in mutation, "44.34 intended damage marker missing")
    req("OnAlpha674434RenderingWorld" in wiring, "44.34 pre-render scale stabilization missing")
    log("44.34 COMBAT PRESENCE CARRY-FORWARD: PASS")

    req("LeaderHoldCenterDistance = 128f" in steering, "leader hold distance is not 128px")
    req("LeaderAttackCenterDistance = 160f" in steering, "leader attack reach is not 160px")
    req("LeaderHoldingRange" in steering, "leader hold transition state missing")
    req("if (!state.LeaderHoldingRange)" in steering, "edge-triggered hold entry missing")
    req("continuous chase" in steering.lower(), "continuous-chase implementation marker missing")
    req("StopDirectionalFlags(source);" in steering, "leader chase directional reset missing")
    req("source.MovePosition(Game1.currentGameTime, Game1.viewport, location)" in steering,
        "leader/follower low-level movement missing")
    old_chase = """if (leader)
        {
            // Clear Pelipper's passive movement state/velocity before applying the Team Up chase step.
            source.Halt();"""
    req(old_chase not in steering, "legacy per-step leader Halt still present")
    req("StopSource(source);" in steering, "hold-band stop path missing")
    log("44.35 CONTINUOUS LEADER PURSUIT + 128PX HOLD + 160PX REACH: PASS")

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
    log("C# BUILD: PASS")

    dll = SRC / "bin" / "Release" / "net6.0" / "TeamUp.dll"
    req(dll.exists(), "TeamUp.dll missing")
    RELEASE.mkdir(parents=True, exist_ok=True)
    if STAGE.exists():
        shutil.rmtree(STAGE)
    MOD_STAGE.mkdir(parents=True)
    shutil.copy2(dll, MOD_STAGE / "TeamUp.dll")
    shutil.copy2(SRC / "manifest.json", MOD_STAGE / "manifest.json")
    shutil.copytree(SRC / "i18n", MOD_STAGE / "i18n")
    shutil.copytree(SRC / "assets", MOD_STAGE / "assets")

    if ZIP_PATH.exists():
        ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in sorted(MOD_STAGE.rglob("*")):
            if path.is_file():
                archive.write(path, path.relative_to(STAGE))

    with zipfile.ZipFile(ZIP_PATH, "r") as archive:
        names = set(archive.namelist())
        for required in [
            "Team Up/TeamUp.dll",
            "Team Up/manifest.json",
            "Team Up/i18n/default.json",
            "Team Up/i18n/vi.json",
            "Team Up/assets/LowerWorkings.tmx",
        ]:
            req(required in names, f"package missing {required}")
        packaged_manifest = json.loads(archive.read("Team Up/manifest.json").decode("utf-8"))
        req(packaged_manifest.get("Version") == VERSION, "packaged manifest mismatch")
    log("ZIP CONTENT AUDIT: PASS")

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")
    log("BUILD SUCCESS - ALPHA 6.7.44.35 LEADER PURSUIT + REACH")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
