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
STAGE = ROOT / "_stage_alpha6744_12"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_12.txt"
VERSION = "0.2.0-alpha.6.7.44.12"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.12_PELIPPER_MODDATA_HP_BINDING_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.12_PELIPPER_MODDATA_HP_BINDING_TEST.sha256.txt"
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
    binding = text("Core/Alpha674412PelipperModDataHpBindingService.cs")
    pairing = text("Core/Alpha674410PelipperSpeciesPairingService.cs")
    wiring = text("ModEntry.Alpha67446.cs")
    mutation_cmd = text("ModEntry.Alpha6719.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    for token in [
        "Griff.PelipperTown/WildCurrentHealth",
        "Griff.PelipperTown/WildMaxHealth",
        "ResolvePrefix",
        "WritePrefix",
        "SourceProxyMap",
        "PelipperProxyModData.WildCurrentHealth",
        "PelipperProxyModData.WildMaxHealth",
    ]:
        req(token in binding, f"HP binding missing {token}")
    req("PelipperModDataHpBindingAlpha674412 = new Alpha674412PelipperModDataHpBindingService" in wiring,
        "HP binding not wired")
    req("PelipperModDataHpBindingAlpha674412.Describe()" in wiring,
        "runtime status missing HP binding")
    req("PelipperModDataHpBindingAlpha674412.Describe()" in mutation_cmd,
        "mutation status missing HP binding")
    req("ConditionalWeakTable<Monster, CachedPair>" in pairing and "cacheHits=" in pairing,
        "pair cache carry-forward missing")
    req("[PelipperSpeciesPairing]" not in pairing, "pairing log spam regressed")
    log("PELIPPER MODDATA HP + PAIR CACHE AUDIT: PASS")

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

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")
    log("ZIP CONTENT AUDIT: PASS")
    log("BUILD SUCCESS - ALPHA 6.7.44.12 PELIPPER MODDATA HP BINDING")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
