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
STAGE = ROOT / "_stage_alpha6744_32"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_32.txt"
VERSION = "0.2.0-alpha.6.7.44.32"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.32_FORCE_TARGET_PIN_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.32_FORCE_TARGET_PIN_TEST.sha256.txt"
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
    command = text("ModEntry.Alpha6719.cs")
    binding = text("Core/Alpha674412PelipperModDataHpBindingService.cs")
    source_mutation = text("Core/Alpha67448PelipperSourceMutationService.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    log("VERSION IDENTITY 6.7.44.32: PASS")

    for token in [
        "FindPinnedMutationForceTargetAlpha674432",
        "TryForcePinnedMutationAlpha674432",
        "ResolvePinnedMutationDisplayNameAlpha674432",
        "LogPinnedMutationForceTargetAlpha674432",
        "[MutationForceTarget] pinned=",
        "Alpha674411PelipperDualHpProbeService.ProbeNow(forceTarget)",
        "PelipperModDataHpBindingAlpha674412.Describe()",
        "PelipperSourceMutationAlpha67448.Describe()",
    ]:
        req(token in command, f"force-target pin missing {token}")
    req("ForceNearestEligible(out string rawResult)" not in command,
        "legacy re-selection force path must not remain in command handler")
    log("EXACT FORCE TARGET PIN + EXACT FAILURE PROBE: PASS")

    for token in ["Griff.PelipperTown/WildCurrentHealth", "Griff.PelipperTown/WildMaxHealth", "SourceProxyMap"]:
        req(token in binding, f"Pelipper HP binding missing {token}")
    req("transform-blocked sourceHP-unresolved" in source_mutation,
        "source-aware fail-closed HP guard missing")
    log("PELIPPER AUTHORITATIVE HP + FAIL-CLOSED CARRY-FORWARD: PASS")

    # This checkpoint is intentionally based on the user-live-loaded 6.7.44.23 runtime.
    forbidden = [
        "Core/Alpha674424EliteCombatFinalizationService.cs",
        "Core/Alpha674424EliteReachOverlayService.cs",
        "Core/Alpha674425PelipperLeaderContinuousChaseService.cs",
        "Core/Alpha674426PelipperMutantPhaseLifecycleService.cs",
        "Core/Alpha674427MutationRegressionGuardService.cs",
        "Core/Alpha674428LowerWorkingsRuntimeGateService.cs",
    ]
    for rel in forbidden:
        req(not (SRC / rel).exists(), f"unsafe post-44.23 runtime service leaked into safe baseline: {rel}")
    log("NO 6.7.44.24-30 RUNTIME SERVICE REINTRODUCTION: PASS")

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
    log("BUILD SUCCESS - ALPHA 6.7.44.32 FORCE TARGET PIN")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
