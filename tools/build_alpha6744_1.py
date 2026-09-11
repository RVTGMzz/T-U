from __future__ import annotations

import hashlib
import json
import re
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6744_1"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_1.txt"
AUDIT = ROOT / "LOWER_WORKINGS_TMX_CSV_HOTFIX_AUDIT_ALPHA6744_1.md"
VERSION = "0.2.0-alpha.6.7.44.1"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.1_LOWER_WORKINGS_TMX_CSV_HOTFIX_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.1_LOWER_WORKINGS_TMX_CSV_HOTFIX_TEST.sha256.txt"
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
    entry = text("ModEntry.Alpha6744.cs")
    interior = text("Story/LowerWorkingsInteriorSurveyStoryService.cs")
    tmx = text("assets/LowerWorkings.tmx")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong hotfix project version")
    req('LowerWorkingsLocationNameAlpha6744 = "Ronvotri.TeamUp_LowerWorkings"' in entry, "6.7.44 location registration regressed")
    req('Helper.ModContent.GetInternalAssetName("assets/LowerWorkings.tmx").Name' in entry, "Lower Workings map asset registration regressed")
    req("TryWarpIntoLowerWorkings()" in interior and "TryWarpToBreach(owner)" in interior, "6.7.44 ingress/egress regressed")
    req(set(en) == set(vi), "EN/VI parity regressed")

    # Parse the XML itself first.
    root = ET.fromstring(tmx)
    req(root.tag == "map", "TMX root element missing")
    req(root.attrib.get("width") == str(WIDTH), "TMX width regressed")
    req(root.attrib.get("height") == str(HEIGHT), "TMX height regressed")

    layers = root.findall("layer")
    req(len(layers) == 3, f"expected 3 tile layers, found {len(layers)}")
    names = [layer.attrib.get("name") for layer in layers]
    req(names == ["Back", "Buildings", "Front"], f"unexpected layer order/names: {names}")

    for layer in layers:
        data = layer.find("data")
        req(data is not None, f"layer {layer.attrib.get('name')} has no data element")
        req(data.attrib.get("encoding") == "csv", f"layer {layer.attrib.get('name')} is not CSV encoded")
        body = data.text or ""
        tokens = [token.strip() for token in body.split(",") if token.strip()]
        req(len(tokens) == EXPECTED, f"layer {layer.attrib.get('name')} expected {EXPECTED} comma-separated IDs, got {len(tokens)}")
        for i, token in enumerate(tokens):
            try:
                value = int(token)
            except ValueError as exc:
                raise RuntimeError(f"layer {layer.attrib.get('name')} token {i} is not an integer: {token!r}") from exc
            req(0 <= value <= 0xFFFFFFFF, f"layer {layer.attrib.get('name')} token {i} outside UInt32 range: {value}")

        # Exact regression check for the reported TMXTile failure mode: no token may
        # contain whitespace-separated multiple numbers such as '151\n151'.
        req(not any(re.search(r"\d\s+\d", token) for token in tokens), f"layer {layer.attrib.get('name')} still contains merged row-boundary numbers")

    req(tmx.count(",\n") >= (HEIGHT - 1) * 3, "CSV rows are not comma-separated across line breaks")
    log("TMX XML PARSE: PASS")
    log("TMX CSV TOKEN COUNT: PASS (768 IDs per layer)")
    log("TMX CSV UINT32 PARSE: PASS")
    log("TMX ROW-BOUNDARY COMMA REGRESSION: PASS")
    log("6.7.44 STORY / INGRESS / EGRESS CARRY-FORWARD: PASS")
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

    # Validate the exact TMX that will ship, not just the source path.
    shipped_tmx = (MOD_STAGE / "assets" / "LowerWorkings.tmx").read_text(encoding="utf-8")
    shipped_root = ET.fromstring(shipped_tmx)
    for layer in shipped_root.findall("layer"):
        data = layer.find("data")
        tokens = [token.strip() for token in ((data.text if data is not None else "") or "").split(",") if token.strip()]
        req(len(tokens) == EXPECTED, "packaged TMX token count regressed")
        for token in tokens:
            int(token)
    log("PACKAGED TMX RE-PARSE: PASS")

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
        packaged_map = archive.read("Team Up/assets/LowerWorkings.tmx").decode("utf-8")
        package_root = ET.fromstring(packaged_map)
        for layer in package_root.findall("layer"):
            data = layer.find("data")
            tokens = [token.strip() for token in ((data.text if data is not None else "") or "").split(",") if token.strip()]
            req(len(tokens) == EXPECTED, "ZIP TMX token count regressed")
            for token in tokens:
                int(token)
    log("ZIP CONTENT + TMX RE-PARSE: PASS")

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Team Up Alpha 6.7.44.1 - Lower Workings TMX CSV Hotfix Audit\n\n"
        "## Root cause\n"
        "The original 6.7.44 TMX generator joined each visual CSV row with a newline only. TMXTile splits CSV data on commas, so the final tile ID of one row and the first ID of the next row were read as one invalid token such as `151\\n151`, causing `UInt32.Parse` to throw `FormatException`.\n\n"
        "## Fix\n"
        "- Changed the generated TMX row boundary to comma + newline.\n"
        "- Kept the map dimensions, tile IDs, layers, story logic, ingress/egress, survey route, reactions, and safety locks unchanged.\n"
        "- Bumped the test version to 0.2.0-alpha.6.7.44.1 so the fixed artifact cannot be confused with the broken 6.7.44 ZIP.\n\n"
        "## CI regression coverage\n"
        "- XML parses successfully.\n"
        "- Back, Buildings, and Front each contain exactly 768 comma-separated tile IDs.\n"
        "- Every tile ID parses independently as UInt32.\n"
        "- Explicitly rejects row-boundary merged-number tokens.\n"
        "- Re-parses the staged TMX and the TMX read back from the final ZIP.\n"
        "- C# build remains 0 warnings / 0 errors.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.1 TMX CSV HOTFIX")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
