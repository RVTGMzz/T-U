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
STAGE = ROOT / "_stage_alpha6744_10"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_10.txt"
AUDIT = ROOT / "PELIPPER_SPECIES_PAIRING_AUDIT_ALPHA6744_10.md"
VERSION = "0.2.0-alpha.6.7.44.10"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.10_PELIPPER_SPECIES_PAIRING_PROBE_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.10_PELIPPER_SPECIES_PAIRING_PROBE_TEST.sha256.txt"
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
    probe = text("Core/Alpha67449PelipperSourceProbeService.cs")
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
        "TryResolvePostfix", "Priority.Last", "NormalizeSpecies", "sameSpecies.Count == 1",
        "intersecting.Count == 1", "ProxyEncounterIdMarker", "ProxyDisplayNameMarker",
        "ambiguous species=", "no-species-match"
    ]:
        req(token in pairing, f"species pairing missing: {token}")
    req("PelipperSpeciesPairingAlpha674410 = new Alpha674410PelipperSpeciesPairingService" in wiring,
        "species pairing service not wired")
    req(wiring.index("PelipperSpeciesPairingAlpha674410 = new") < wiring.index("PelipperSourceMutationAlpha67448 = new"),
        "species pairing must be installed before source Mutation resolver")
    req("PelipperSpeciesPairingAlpha674410.Describe()" in wiring, "runtime status missing species pairing telemetry")
    req("PelipperSpeciesPairingAlpha674410.Describe()" in mutation_cmd, "mutation status missing species pairing telemetry")
    log("PELIPPER UNIQUE-SPECIES SOURCE PAIRING AUDIT: PASS")

    for token in [
        "TryResolveSourceHealth", "SourceHealthResolverPostfix", "_probedTypes", "PelipperSourceHPProbe",
        "candidates=[", "members=[", "CaptureForceTargetDisplayName"
    ]:
        req(token in probe, f"6.7.44.9 source HP probe carry-forward missing: {token}")
    log("PELIPPER SOURCE HP PROBE CARRY-FORWARD: PASS")

    req("transform-blocked sourceHP-unresolved" in source_mutation, "source HP fail-closed guard regressed")
    req("PelipperWildEncounterIdentityService.TryResolve" in source_mutation, "source identity resolver regressed")
    req("ExtraLifeMarker" in source_mutation and "LogicalMaxHpMarker" in source_mutation,
        "source Mutation phase model regressed")
    log("PELIPPER SOURCE MUTATION FAIL-CLOSED AUDIT: PASS")

    req("Không thể cưỡng chế đột biến" in mutation_cmd, "Vietnamese rejected-force text missing")
    req("Đã cưỡng chế đột biến" in mutation_cmd, "Vietnamese success-force text missing")
    req("CleanMutationTargetName" in mutation_cmd, "Wild/Shiny user-facing prefix cleanup missing")
    req('new[] { "Wild ", "Shiny " }' in mutation_cmd, "target prefix cleanup policy missing")
    req("Green Slime" not in mutation_cmd, "Green Slime leaked into mutation command UI")
    log("FORCE TARGET EN/VI + DISPLAY CLEANUP AUDIT: PASS")

    req("PartyMemberState.Following or PartyMemberState.Waiting" in wiring, "gift guard active-state rule regressed")
    req("Helper.Input.Suppress(e.Button)" in wiring, "gift guard input suppression regressed")
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
        "# Team Up 6.7.44.10 - Pelipper Species Pairing Probe\n\n"
        "- Adds a late, conservative source/proxy pairing fallback for Pelipper 1.2.0.\n"
        "- The fallback uses the proxy's Pokemon-facing display name and accepts only a unique same-species visible wild actor, or one uniquely intersecting same-species actor.\n"
        "- Duplicate-species ambiguity remains fail-closed.\n"
        "- Pairing telemetry is exposed through `teamup_mutation status` and `teamup_pelipper_runtime`.\n"
        "- The 6.7.44.9 source-HP probe remains active, so once identity is repaired it can reveal Pelipper's real HP layout.\n"
        "- Mutation still fails closed until source HP is proven; sentinel Green Slime HP is never treated as Pokemon HP.\n"
        "- Vietnamese force messages remain localized and Wild/Shiny prefixes are removed from the user-facing target name.\n",
        encoding="utf-8",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.10 PELIPPER SPECIES PAIRING PROBE")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
