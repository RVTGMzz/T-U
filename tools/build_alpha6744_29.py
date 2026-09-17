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
STAGE = ROOT / "_stage_alpha6744_29"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_29.txt"
VERSION = "0.2.0-alpha.6.7.44.29"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.29_LOWER_RUNTIME_COMMAND_HARDENING_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.29_LOWER_RUNTIME_COMMAND_HARDENING_TEST.sha256.txt"
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
    entry = text("ModEntry.cs")
    lower = text("ModEntry.Alpha6744.cs")
    gate = text("Core/Alpha674428LowerWorkingsRuntimeGateService.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    log("VERSION IDENTITY 6.7.44.29: PASS")

    req("RegisterAlpha6744Events();" in entry, "Entry() does not call RegisterAlpha6744Events")
    req('"teamup_lower_runtime"' in lower, "teamup_lower_runtime command registration missing")
    req("OnAlpha674428Command" in lower, "teamup_lower_runtime handler missing")
    req("LowerWorkingsRuntimeGateAlpha674428 = new Alpha674428LowerWorkingsRuntimeGateService" in lower,
        "Lower Workings runtime gate initialization missing")
    req("LowerWorkingsRuntimeGateAlpha674428.OnSaveLoaded()" in lower,
        "Lower Workings runtime gate SaveLoaded wiring missing")
    req("LowerWorkingsRuntimeGateAlpha674428.OnLocalWarped(e.OldLocation, e.NewLocation)" in lower,
        "Lower Workings runtime gate Warped wiring missing")
    log("LOWER RUNTIME COMMAND REGISTRATION + ENTRY CHAIN: PASS")

    for token in [
        "ExpectedMapWidth = 32",
        "ExpectedMapHeight = 24",
        "entriesObserved=",
        "arrivalPasses=",
        "exactReturnPasses=",
        "mapProbeFailures=",
        'return "INVALID"',
        'return "READY_TO_ENTER"',
        'return "INSIDE"',
        'return "COMPLETE"',
    ]:
        req(token in gate, f"Lower Workings runtime gate missing {token}")
    req("Game1.warpFarmer" not in gate, "runtime gate must stay read-only")
    req("modData[" not in gate, "runtime gate must not write modData")
    log("LOWER WORKINGS READ-ONLY RUNTIME GATE CARRY-FORWARD: PASS")

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
    (MOD_STAGE / "assets").mkdir(parents=True, exist_ok=True)
    shutil.copy2(SRC / "assets" / "LowerWorkings.tmx", MOD_STAGE / "assets" / "LowerWorkings.tmx")
    (MOD_STAGE / "BUILD_MARKER_674429.txt").write_text(
        "Team Up 0.2.0-alpha.6.7.44.29\nExpected console build stamp: v0.2.0-alpha.6.7.44.29\nExpected command: teamup_lower_runtime\n",
        encoding="utf-8",
    )

    if ZIP_PATH.exists():
        ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED) as zf:
        for path in sorted(STAGE.rglob("*")):
            if path.is_file():
                zf.write(path, path.relative_to(STAGE))

    with zipfile.ZipFile(ZIP_PATH, "r") as zf:
        names = set(zf.namelist())
        for required in [
            "Team Up/TeamUp.dll",
            "Team Up/manifest.json",
            "Team Up/assets/LowerWorkings.tmx",
            "Team Up/BUILD_MARKER_674429.txt",
        ]:
            req(required in names, f"ZIP missing {required}")
        packaged_manifest = json.loads(zf.read("Team Up/manifest.json").decode("utf-8"))
        req(packaged_manifest.get("Version") == VERSION, "packaged manifest version mismatch")
    log("ZIP CONTENT + BUILD MARKER AUDIT: PASS")

    sha = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{sha}  {ZIP_NAME}\n", encoding="utf-8")
    log("BUILD SUCCESS - ALPHA 6.7.44.29 LOWER RUNTIME COMMAND HARDENING")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {sha}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
