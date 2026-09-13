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
STAGE = ROOT / "_stage_alpha6744_9"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_9.txt"
AUDIT = ROOT / "PELIPPER_SOURCE_PROBE_AUDIT_ALPHA6744_9.md"
VERSION = "0.2.0-alpha.6.7.44.9"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.9_PELIPPER_SOURCE_PROBE_I18N_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.9_PELIPPER_SOURCE_PROBE_I18N_TEST.sha256.txt"
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
    probe = text("Core/Alpha67449PelipperSourceProbeService.cs")
    source_mutation = text("Core/Alpha67448PelipperSourceMutationService.cs")
    wiring = text("ModEntry.Alpha67446.cs")
    mutation_cmd = text("ModEntry.Alpha6719.cs")
    gift_wiring = wiring
    default_i18n = json.loads(text("i18n/default.json"))
    vi_i18n = json.loads(text("i18n/vi.json"))

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    req("<EnableHarmony>true</EnableHarmony>" in project, "Harmony build reference missing")
    req("<LangVersion>13.0</LangVersion>" in project, "C# 13 pin missing")
    log("VERSION + BUILD ENVIRONMENT AUDIT: PASS")

    # 6.7.44.9 must probe only genuine source-HP resolver failures and cache the expensive member scan.
    for token in [
        "TryResolveSourceHealth", "SourceHealthResolverPostfix", "_probedTypes", "PelipperSourceHPProbe",
        "candidates=[", "members=[", "CaptureForceTargetDisplayName", "IsEligibleMethod"
    ]:
        req(token in probe, f"source HP probe missing: {token}")
    req("if (__result || Active is null)" in probe, "probe is not fail-only")
    req("_probeCacheHits++" in probe, "probe type cache missing")
    req("PelipperSourceProbeAlpha67449 = new Alpha67449PelipperSourceProbeService" in wiring, "probe not wired")
    req("PelipperSourceProbeAlpha67449.Describe()" in wiring, "runtime status does not expose probe")
    req("PelipperSourceProbeAlpha67449.Describe()" in mutation_cmd, "mutation status does not expose probe")
    log("PELIPPER SOURCE HP FAILURE PROBE AUDIT: PASS")

    # Force command must localize EN/VI and resolve the exact eligible target before the core service runs.
    req("CaptureForceTargetDisplayName()" in mutation_cmd, "force command does not capture source display name")
    req("LocalizeMutationForceResult" in mutation_cmd, "force result localization missing")
    req('Helper.Translation.Locale.StartsWith("vi"' in mutation_cmd, "Vietnamese locale branch missing")
    req("Không thể cưỡng chế đột biến" in mutation_cmd, "Vietnamese rejected-force text missing")
    req("Đã cưỡng chế đột biến" in mutation_cmd, "Vietnamese success-force text missing")
    req("Force mutation was rejected for Green Slime." not in mutation_cmd, "hard-coded Green Slime rejection leaked into command")
    log("FORCE TARGET NAME + EN/VI LOCALIZATION AUDIT: PASS")

    # Carry-forward: source Mutation must still fail closed instead of mutating sentinel proxy HP.
    req("transform-blocked sourceHP-unresolved" in source_mutation, "source HP fail-closed guard regressed")
    req("PelipperWildEncounterIdentityService.TryResolve" in source_mutation, "source identity pairing regressed")
    req("ExtraLifeMarker" in source_mutation and "LogicalMaxHpMarker" in source_mutation, "source Mutation phase model regressed")
    req("DrawSourceAuras" in source_mutation, "visible source aura regressed")
    log("PELIPPER SOURCE MUTATION CARRY-FORWARD: PASS")

    # Active teammate gift guard and translation parity remain untouched.
    req("PartyMemberState.Following or PartyMemberState.Waiting" in gift_wiring, "gift guard active-state rule regressed")
    req("Helper.Input.Suppress(e.Button)" in gift_wiring, "gift guard input suppression regressed")
    req(set(default_i18n) == set(vi_i18n), "EN/VI translation key parity regressed")
    log("GIFT GUARD + EN/VI PARITY CARRY-FORWARD: PASS")

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
        "# Team Up 6.7.44.9 - Pelipper Source HP Probe + Localized Force Output\n\n"
        "- Failed source-HP resolution now produces a cached compact runtime-member probe.\n"
        "- `teamup_mutation status` exposes the probe line.\n"
        "- `teamup_mutation force` captures the actual eligible target and prefers the paired Pokemon display name.\n"
        "- Force success/rejection/no-target messages are localized for Vietnamese and English UI locales.\n"
        "- 6.7.44.8 source Mutation remains fail-closed; hidden Green Slime sentinel HP is not mutated.\n"
        "- Active teammate gift guard and EN/VI content parity are carried forward.\n",
        encoding="utf-8",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.9 PELIPPER SOURCE PROBE + LOCALIZATION")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
