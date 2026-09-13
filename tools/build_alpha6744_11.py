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
STAGE = ROOT / "_stage_alpha6744_11"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_11.txt"
AUDIT = ROOT / "PELIPPER_PAIR_CACHE_DUAL_PROBE_AUDIT_ALPHA6744_11.md"
VERSION = "0.2.0-alpha.6.7.44.11"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.11_PELIPPER_PAIR_CACHE_DUAL_PROBE_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.11_PELIPPER_PAIR_CACHE_DUAL_PROBE_TEST.sha256.txt"
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
    dual_probe = text("Core/Alpha674411PelipperDualHpProbeService.cs")
    source_probe = text("Core/Alpha67449PelipperSourceProbeService.cs")
    source_mutation = text("Core/Alpha67448PelipperSourceMutationService.cs")
    wiring = text("ModEntry.Alpha67446.cs")
    mutation_cmd = text("ModEntry.Alpha6719.cs")
    default_i18n = json.loads(text("i18n/default.json"))
    vi_i18n = json.loads(text("i18n/vi.json"))

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    req("<EnableHarmony>true</EnableHarmony>" in project, "Harmony build reference missing")
    req("<LangVersion>13.0</LangVersion>" in project, "C# 13 pin missing")
    log("VERSION + BUILD ENVIRONMENT AUDIT: PASS")

    for token in [
        "ConditionalWeakTable<Monster, CachedPair>", "TryUseCachedPair", "cacheHits=", "cacheInvalidated=",
        "sourceStillPresent", "No per-pair log", "sameSpecies.Count == 1", "intersecting.Count == 1"
    ]:
        req(token in pairing, f"pair-cache policy missing: {token}")
    req("[PelipperSpeciesPairing]" not in pairing, "per-pair species logging still present")
    log("PELIPPER PAIR CACHE + LOG-SPAM AUDIT: PASS")

    for token in [
        "Alpha674411PelipperDualHpProbeService", "ProbeNow", "_probedTypePairs", "sourceCandidates=[",
        "proxyCandidates=[", "sourceModData=[", "proxyModData=[", "sourceMembers=[", "proxyMembers=["
    ]:
        req(token in dual_probe, f"dual HP probe missing: {token}")
    req("PelipperDualHpProbeAlpha674411 = new Alpha674411PelipperDualHpProbeService" in wiring,
        "dual HP probe not wired")
    req("PelipperDualHpProbeAlpha674411.Describe()" in wiring, "runtime command missing dual probe telemetry")
    req("PelipperDualHpProbeAlpha674411.Describe()" in mutation_cmd, "mutation status missing dual probe telemetry")
    req("Alpha674411PelipperDualHpProbeService.ProbeNow(probeTarget)" in mutation_cmd,
        "failed force does not trigger deterministic dual probe")
    log("PELIPPER DUAL SOURCE+PROXY HP PROBE AUDIT: PASS")

    req("CaptureForceTargetDisplayName" in source_probe, "source-aware force naming regressed")
    req("transform-blocked sourceHP-unresolved" in source_mutation, "source HP fail-closed guard regressed")
    req("ExtraLifeMarker" in source_mutation and "LogicalMaxHpMarker" in source_mutation,
        "source Mutation phase model regressed")
    log("SOURCE MUTATION FAIL-CLOSED CARRY-FORWARD: PASS")

    req("Không thể cưỡng chế đột biến" in mutation_cmd, "Vietnamese rejected-force text missing")
    req("Đã cưỡng chế đột biến" in mutation_cmd, "Vietnamese success-force text missing")
    req("CleanMutationTargetName" in mutation_cmd, "target display cleanup missing")
    req("PartyMemberState.Following or PartyMemberState.Waiting" in wiring, "active teammate gift guard regressed")
    req("Helper.Input.Suppress(e.Button)" in wiring, "gift guard suppression regressed")
    req(set(default_i18n) == set(vi_i18n), "EN/VI translation key parity regressed")
    log("USER-FACING + GIFT GUARD + EN/VI CARRY-FORWARD: PASS")

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
            "Team Up/TeamUp.dll", "Team Up/manifest.json", "Team Up/i18n/default.json",
            "Team Up/i18n/vi.json", "Team Up/assets/LowerWorkings.tmx"
        ]:
            req(required in names, f"package missing {required}")
        packaged_manifest = json.loads(archive.read("Team Up/manifest.json").decode("utf-8"))
        req(packaged_manifest.get("Version") == VERSION, "packaged manifest mismatch")
    log("ZIP CONTENT AUDIT: PASS")

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")
    AUDIT.write_text(
        "# Team Up 6.7.44.11 - Pelipper Pair Cache + Dual HP Probe\n\n"
        "- Successful species pairing is cached by combat proxy instance and validated before reuse.\n"
        "- Per-resolution species pairing logs are removed; aggregate counters remain in status.\n"
        "- Rejected force attempts immediately probe both the visible PokemonNpc and hidden combat proxy.\n"
        "- Dual probe is cached by source/proxy runtime type pair and is not a normal per-tick scanner.\n"
        "- Source Mutation remains fail-closed until real Pelipper HP ownership is proven.\n"
        "- Shiny behavior, active teammate gift guard, Lower Workings and EN/VI parity are preserved.\n",
        encoding="utf-8",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.11 PELIPPER PAIR CACHE + DUAL HP PROBE")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
