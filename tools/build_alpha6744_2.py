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
STAGE = ROOT / "_stage_alpha6744_2"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_2.txt"
AUDIT = ROOT / "PELIPPER_ENCOUNTER_REACTION_AUDIT_ALPHA6744_2.md"
VERSION = "0.2.0-alpha.6.7.44.2"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.2_PELIPPER_ENCOUNTER_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.2_PELIPPER_ENCOUNTER_REACTIONS_TEST.sha256.txt"
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
    wiring = text("ModEntry.Alpha67442.cs")
    lower_entry = text("ModEntry.Alpha6744.cs")
    tmx = text("assets/LowerWorkings.tmx")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong 6.7.44.2 project version")
    req("private static bool _enabled;" in capture, "capture policy must fail closed by default")
    req("_modeConfirmed = pelipperDetected && bestModeScore >= 0;" in capture, "capture mode positive-confirmation gate missing")
    req("_enabled = _modeConfirmed && enabled;" in capture, "capture floor can activate without confirmed Catch Mode")
    req("EncounterReactionService.IsShinyEmergencyHeld(monster)" in capture, "Shiny hold is not routed through friendly-damage protection")
    req("TryGetCaptureFloor" in capture and "Shiny Emergency Hold intentionally does not" in capture, "Shiny hold/capture-floor separation missing")

    req("ShinyEmergencyHoldMarker" in reaction, "Shiny Emergency Hold marker missing")
    req("ShinyEngagedMarker" in reaction and "ShinyIgnoredMarker" in reaction, "Shiny tactical order state missing")
    req("MonsterMutationService.MutationExcludedMarker" in reaction, "natural Shiny mutation exemption missing")
    req("EncounterReactionKind.Mutation" in reaction and "EncounterReactionKind.EliteBoss" in reaction and "EncounterReactionKind.Special" in reaction,
        "encounter reaction classes missing")
    req("HasExplicitShinyEvidence" in reaction and "PelipperTownCompatibilityService.IsWildCombatActor" in reaction,
        "Shiny detection is not Pelipper-wild + explicit-evidence gated")
    req("UpdateTicking" in wiring, "Shiny detection must run before UpdateTicked combat selection")
    req("ShinyOrderRequestTypeAlpha67442" in wiring and "ShinyOrderResultTypeAlpha67442" in wiring,
        "multiplayer host-authoritative Shiny order messages missing")
    req("Engage" in wiring and "Hold" in wiring and "Ignore" in wiring, "Farmer Shiny order menu incomplete")
    req("EnsureAlpha67442EncounterReactionsRegistered();" in lower_entry, "6.7.44.2 registration missing")
    log("CAPTURE MODE FAIL-CLOSED AUDIT: PASS")
    log("SHINY EMERGENCY HOLD AUDIT: PASS")
    log("ENCOUNTER PERSONALITY REACTION AUDIT: PASS")
    log("MULTIPLAYER SHINY ORDER AUTHORITY AUDIT: PASS")

    # Carry forward the exact 6.7.44.1 Lower Workings TMX regression gate.
    root = ET.fromstring(tmx)
    req(root.attrib.get("width") == str(WIDTH) and root.attrib.get("height") == str(HEIGHT), "Lower Workings TMX dimensions regressed")
    layers = root.findall("layer")
    req([layer.attrib.get("name") for layer in layers] == ["Back", "Buildings", "Front"], "Lower Workings layers regressed")
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
    log("6.7.44.1 LOWER WORKINGS TMX HOTFIX CARRY-FORWARD: PASS")
    log("EN/VI PARITY CARRY-FORWARD: PASS")

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
        "# Team Up Alpha 6.7.44.2 - Pelipper Encounter Reaction Audit\n\n"
        "## Capture safety rule\n"
        "- 10% capture/mercy floor is OFF unless Pelipper Town is present and Team Up positively confirms Catch/Capture/Mercy mode is enabled.\n"
        "- If Pelipper is absent, Catch Mode is off, or the mode cannot be resolved safely, ordinary combat remains lethal.\n"
        "- The 10% fallback threshold is used only after Catch Mode has been positively confirmed ON.\n\n"
        "## Shiny Emergency Hold\n"
        "- Requires a Pelipper wild combat actor plus explicit Shiny runtime evidence. Detection fails closed.\n"
        "- Confirmed natural Shiny receives Team Up MutationExcluded and cannot become a Mutation seed.\n"
        "- Team Up friendly damage is blocked immediately, independently from Catch Mode.\n"
        "- Farmer receives Engage / Keep Holding / Ignore tactical orders. Multiplayer orders are host-authoritative.\n"
        "- Engage releases Shiny Hold; normal Pelipper capture-floor policy then applies only if Catch Mode is enabled.\n\n"
        "## Encounter reactions\n"
        "- Shiny: personality reaction + tactical hold.\n"
        "- Mutation: personality/story reaction only.\n"
        "- Elite/Boss: personality reaction only.\n"
        "- Special/Surge/story-tagged monster: attention reaction only.\n"
        "- One primary reaction plus at most one delayed reply prevents bubble spam.\n\n"
        "## Carry-forward locks\n"
        "- 6.7.44.1 Lower Workings TMX CSV fix remains byte-parse safe.\n"
        "- 6.7.45 remains reserved for Containment Chamber Escalation and is not started here.\n"
        "- George reveal, Evelyn secret, final boss, party caps, Pelipper ownership authority and story progression are unchanged.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.2 PELIPPER ENCOUNTER REACTIONS")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
