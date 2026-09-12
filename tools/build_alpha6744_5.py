from __future__ import annotations

import hashlib
import json
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6744_5"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_5.txt"
AUDIT = ROOT / "PELIPPER_CATCHMODE_GATE_AUDIT_ALPHA6744_5.md"
VERSION = "0.2.0-alpha.6.7.44.5"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.5_PELIPPER_CATCHMODE_GATE_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.5_PELIPPER_CATCHMODE_GATE_TEST.sha256.txt"
WIDTH = 32
HEIGHT = 24
EXPECTED = WIDTH * HEIGHT
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
    capture = text("Core/PelipperCaptureSafetyService.cs")
    reaction = text("Core/EncounterReactionService.cs")
    identity = text("Core/PelipperWildEncounterIdentityService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    wiring = text("ModEntry.Alpha67442.cs")
    lower_entry = text("ModEntry.Alpha6744.cs")
    tmx = text("assets/LowerWorkings.tmx")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong 6.7.44.5 project version")
    req(f"<Version>$(ProjectVersion)</Version>" in project, "project Version indirection regressed")
    log("VERSION AUDIT: PASS")

    # Catch Mode gate must fail closed. Pelipper presence by itself is never permission to clamp HP.
    req("bool enabled = false;" in capture, "policy probe no longer begins disabled")
    req("_modeConfirmed = pelipperDetected && bestModeScore >= 0;" in capture, "explicit mode confirmation gate missing")
    req("_modeEnabled = _modeConfirmed && enabled;" in capture, "confirmed mode value gate missing")
    req("_enabled = _modeEnabled;" in capture, "capture safety is not gated by confirmed enabled Catch Mode")
    req("_enabled = pelipperDetected;" not in capture, "Pelipper presence still enables capture safety")
    req('"fallback-10%-after-confirmed-catch-mode"' in capture, "10% fallback is not documented as post-confirmation only")
    req('"inactive"' in capture and '"unresolved"' in capture, "fail-closed diagnostic states missing")
    req("CatchModeDetected" in capture and "CatchModeEnabled" in capture, "Catch Mode diagnostics missing")
    req("ModeSource" in capture and "ThresholdSource" in capture, "policy source diagnostics missing")
    req("DescribePolicy()" in capture, "policy description diagnostic missing")
    log("STRICT CATCH MODE GATE AUDIT: PASS")

    # Mutants never inherit a mercy floor. Shiny tactical hold stays independent of Catch Mode.
    req("MonsterMutationService.IsMutant(monster)" in capture, "mutant capture-floor exemption missing")
    req("EncounterReactionService.IsShinyEmergencyHeld(monster)" in capture, "Shiny Emergency Hold damage protection missing")
    floor_section = capture.split("public static bool TryGetCaptureFloor", 1)[1].split("public static int RepairCurrentLocationFloors", 1)[0]
    req("IsShinyEmergencyHeld" not in floor_section, "Shiny Hold incorrectly creates an HP capture floor")
    req("if (location is null || !_enabled)" in capture, "floor repair can run while Catch Mode is disabled")
    log("SHINY / MUTANT POLICY SEPARATION AUDIT: PASS")

    # Runtime diagnostics must expose the gate without replacing existing encounter controls.
    status_section = wiring.split('if (action == "status")', 1)[1].split("ShinyTacticalOrder?", 1)[0]
    req("EncounterReactionsAlpha67442.Describe(Game1.player)" in status_section, "encounter status diagnostic regressed")
    req("PelipperCaptureSafetyService.DescribePolicy()" in status_section, "Catch Mode policy not wired to teamup_encounter status")
    log("RUNTIME DIAGNOSTIC WIRING AUDIT: PASS")

    # Carry forward source-aware Shiny identity and Shiny-over-Mutation priority.
    req("PelipperWildEncounterIdentity" in identity, "Pelipper source/proxy encounter identity missing")
    req("SourceActor" in identity and "CombatProxy" in identity, "Pelipper source/proxy role split missing")
    req("HasExplicitShinyEvidence(identity.SourceActor)" in reaction, "Shiny state no longer reads source actor first")
    req("_shinyOrdersByEncounterId" in reaction, "sticky Shiny tactical orders missing")
    req("EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster)" in mutation, "Shiny-over-Mutation priority regressed")
    req("bool pelipperWild = PelipperTownCompatibilityService.IsWildCombatActor(monster);" in mutation, "normal Pelipper wild Mutation eligibility regressed")
    log("6.7.44.4 SOURCE-AWARE SHINY CARRY-FORWARD: PASS")

    # Lower Workings / localization gate remains untouched.
    req("EnsureAlpha67442EncounterReactionsRegistered();" in lower_entry, "encounter reaction registration regressed")
    root = ET.fromstring(tmx)
    req(root.attrib.get("width") == str(WIDTH) and root.attrib.get("height") == str(HEIGHT), "Lower Workings TMX dimensions regressed")
    layers = root.findall("layer")
    req([layer.attrib.get("name") for layer in layers] == ["Back", "Buildings", "Front"], "Lower Workings layer order regressed")
    for layer in layers:
        data = layer.find("data")
        req(data is not None and data.attrib.get("encoding") == "csv", "Lower Workings layer encoding regressed")
        tokens = [token.strip() for token in ((data.text if data is not None else "") or "").split(",") if token.strip()]
        req(len(tokens) == EXPECTED, f"Lower Workings layer {layer.attrib.get('name')} token count regressed")
        for token in tokens:
            value = int(token)
            req(0 <= value <= 0xFFFFFFFF, "Lower Workings TMX UInt32 range regressed")
    req(tmx.count(",\n") >= (HEIGHT - 1) * 3, "6.7.44.1 TMX row-boundary comma fix regressed")
    req(set(en) == set(vi), "EN/VI translation parity regressed")
    log("LOWER WORKINGS + EN/VI CARRY-FORWARD: PASS")

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
    manifest = text("manifest.json").replace("%ProjectVersion%", VERSION)
    (MOD_STAGE / "manifest.json").write_text(manifest, encoding="utf-8", newline="\n")
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
        expected_files = {
            "Team Up/TeamUp.dll",
            "Team Up/manifest.json",
            "Team Up/i18n/default.json",
            "Team Up/i18n/vi.json",
            "Team Up/assets/LowerWorkings.tmx",
        }
        req(expected_files.issubset(names), f"package missing expected files: {sorted(expected_files - names)}")
        packaged_manifest = json.loads(archive.read("Team Up/manifest.json").decode("utf-8"))
        req(packaged_manifest.get("Version") == VERSION, "packaged manifest version mismatch")
        packaged_map = archive.read("Team Up/assets/LowerWorkings.tmx").decode("utf-8")
        package_root = ET.fromstring(packaged_map)
        for layer in package_root.findall("layer"):
            data = layer.find("data")
            tokens = [token.strip() for token in ((data.text if data is not None else "") or "").split(",") if token.strip()]
            req(len(tokens) == EXPECTED, "ZIP TMX token count regressed")
            for token in tokens:
                int(token)
    log("ZIP CONTENT + LOWER WORKINGS TMX RE-PARSE: PASS")

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Team Up Alpha 6.7.44.5 - Pelipper Catch Mode Gate Audit\n\n"
        "## Source rule\n"
        "- Pelipper absent: capture safety OFF.\n"
        "- Pelipper present + Catch Mode false: capture safety OFF.\n"
        "- Pelipper present + Catch Mode unresolved: capture safety OFF.\n"
        "- Pelipper present + Catch Mode confirmed true: capture safety ON.\n"
        "- 10% fallback threshold is allowed only after Catch Mode is confirmed true.\n\n"
        "## Separation of concerns\n"
        "- Shiny Emergency Hold is independent from Catch Mode and can still stop Team Up fire.\n"
        "- Shiny Hold does not create or repair an artificial 10% HP floor.\n"
        "- Mutated Pokemon never inherit Pelipper capture-floor protection.\n\n"
        "## Runtime diagnostics\n"
        "- `teamup_encounter status` reports Pelipper presence, Catch Mode detection/value, capture-safety state, mode source, threshold and threshold source.\n\n"
        "## Carry-forward\n"
        "- 6.7.44.4 source-aware Shiny identity remains intact.\n"
        "- Shiny-over-Mutation priority remains intact.\n"
        "- Lower Workings 6.7.44.1 TMX fix remains intact.\n"
        "- 6.7.45 story work is not started.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.5 PELIPPER CATCH MODE GATE")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
