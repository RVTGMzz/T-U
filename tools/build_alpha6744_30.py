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
STAGE = ROOT / "_stage_alpha6744_30"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_30.txt"
VERSION = "0.2.0-alpha.6.7.44.30"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.30_SAFE_ROLLBACK_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.30_SAFE_ROLLBACK_TEST.sha256.txt"
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
    modentry_6744 = text("ModEntry.Alpha6744.cs")
    reach = text("Core/Alpha674424EliteReachOverlayService.cs")
    continuous = text("Core/Alpha674425PelipperLeaderContinuousChaseService.cs")
    phases = text("Core/Alpha674426PelipperMutantPhaseLifecycleService.cs")
    regression = text("Core/Alpha674427MutationRegressionGuardService.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    log("VERSION IDENTITY 6.7.44.30: PASS")

    # Hard rollback boundary: none of the 6.7.44.28/29 runtime-gate code may exist.
    req(not (SRC / "Core/Alpha674428LowerWorkingsRuntimeGateService.cs").exists(),
        "unsafe 6.7.44.28 Lower Workings runtime gate file is present")
    req("LowerWorkingsRuntimeGateAlpha674428" not in modentry_6744,
        "unsafe 6.7.44.28 runtime gate wiring is present")
    req("teamup_lower_runtime" not in modentry_6744,
        "unsafe 6.7.44.28 runtime command is present")
    log("6.7.44.28/29 LOWER WORKINGS RUNTIME-GATE ROLLBACK: PASS")

    # Preserve the previously built Mutation chain from 6.7.44.27.
    for token in [
        "ExtendedLeaderAttackDistance = 160f",
        "ExtendedLeaderHoldDistance = 128f",
        "_continuousChase = new Alpha674425PelipperLeaderContinuousChaseService",
        "_phaseLifecycle = new Alpha674426PelipperMutantPhaseLifecycleService",
        "_regressionGuard = new Alpha674427MutationRegressionGuardService",
    ]:
        req(token in reach, f"Mutation carry-forward missing {token}")
    req("source.Halt()" not in continuous, "continuous chase regressed to per-step Halt")
    req("explicit-3-phase" in phases, "three-phase lifecycle missing")
    req("unsupported-fail-closed" in regression and "legacyFactoryOriginalRuns=0" in regression,
        "Mutation regression guard missing")
    log("6.7.44.24-27 MUTATION CHAIN CARRY-FORWARD: PASS")

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
    log("BUILD SUCCESS - ALPHA 6.7.44.30 SAFE ROLLBACK")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
