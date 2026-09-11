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
STAGE = ROOT / "_stage_alpha6744_4"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_4.txt"
AUDIT = ROOT / "PELIPPER_SOURCE_AWARE_SHINY_AUDIT_ALPHA6744_4.md"
VERSION = "0.2.0-alpha.6.7.44.4"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.4_PELIPPER_SOURCE_AWARE_SHINY_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.4_PELIPPER_SOURCE_AWARE_SHINY_TEST.sha256.txt"
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
    identity = text("Core/PelipperWildEncounterIdentityService.cs")
    reaction = text("Core/EncounterReactionService.cs")
    wiring = text("ModEntry.Alpha67442.cs")
    capture = text("Core/PelipperCaptureSafetyService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    lower_entry = text("ModEntry.Alpha6744.cs")
    tmx = text("assets/LowerWorkings.tmx")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong 6.7.44.4 version")

    # Source-aware Pelipper identity: source actor owns presentation/state; proxy owns combat.
    req("PelipperWildEncounterIdentity" in identity, "source/proxy encounter descriptor missing")
    req("SourceActor" in identity and "CombatProxy" in identity, "source/proxy roles are not explicit")
    req("ProxyEncounterIdMarker" in identity and "ProxyDisplayNameMarker" in identity, "proxy identity markers missing")
    req("GetOrCreateEncounterId" in identity, "stable encounter identity missing")
    req("ReadDisplayName" in identity and '"PokemonName"' in identity, "Pokemon source display name probing missing")
    req("Vector2.DistanceSquared" in identity and "GetBoundingBox" in identity, "source/proxy spatial pairing missing")
    log("PELIPPER SOURCE/PROXY PAIRING AUDIT: PASS")

    # Shiny state must be read from source actor, while hold/order state is applied to proxy.
    req("identity.SourceActor" in reaction and "HasExplicitShinyEvidence(identity.SourceActor)" in reaction,
        "Shiny state is not read from Pelipper source actor")
    req("GetShinyDisplayName" in reaction and "GetShinyEncounterId" in reaction, "Shiny identity accessors missing")
    req("_shinyOrdersByEncounterId" in reaction, "sticky tactical order dictionary missing")
    req("ApplyShinyTacticalState" in reaction, "proxy tactical state applicator missing")
    req("encounterId" in reaction.split("BuildReactionCooldownKey", 1)[1], "reaction cooldown is not encounter-aware")
    req("proxy={monster.Name}" in reaction, "diagnostics no longer expose source/proxy distinction")
    log("SOURCE-AWARE SHINY HOLD AUDIT: PASS")

    # Prompt and multiplayer orders must use stable encounter identity and Pokémon display name.
    prompt_section = wiring.split("private void TryShowShinyOrderPromptAlpha67442", 1)[1].split("private bool HasLocalFollowingPartyMemberAlpha67442", 1)[0]
    req("GetShinyDisplayName(target)" in prompt_section, "prompt still renders combat proxy name")
    req("EncounterId = EncounterReactionService.GetShinyEncounterId(target)" in wiring, "multiplayer order lacks encounter id")
    req("request.EncounterId" in wiring, "host order resolution does not use encounter id")
    token_section = wiring.split("BuildShinyPromptTokenAlpha67442", 1)[1].split("private sealed class", 1)[0]
    req("GetShinyEncounterId(target)" in token_section, "prompt token is not encounter-aware")
    req("target.Tile.X" not in token_section and "target.Tile.Y" not in token_section,
        "moving proxy can still change prompt token")
    log("SHINY PROMPT IDENTITY + STICKY ORDER AUDIT: PASS")

    # Carry forward 6.7.44.3 Pelipper runtime fixes.
    req("_enabled = pelipperDetected;" in capture, "Pelipper mercy priority regressed")
    req("MonsterMutationService.IsMutant(monster)" in capture, "mutant capture-floor exemption regressed")
    req("if (monster.MaxHealth >= 300)" not in reaction, "raw HP Elite/Boss false-positive regressed")
    req("bool pelipperWild = PelipperTownCompatibilityService.IsWildCombatActor(monster);" in mutation,
        "Pelipper wild Mutation eligibility regressed")
    req("EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster)" in mutation,
        "Shiny-over-Mutation priority regressed")
    log("6.7.44.3 PELIPPER RUNTIME HOTFIX CARRY-FORWARD: PASS")

    # Lower Workings + localization safety carry-forward.
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
        "# Team Up Alpha 6.7.44.4 - Pelipper Source-Aware Shiny Audit\n\n"
        "## Corrected runtime model\n"
        "- A Pelipper encounter may use a visible Pokémon source actor and a separate Monster combat proxy.\n"
        "- Team Up reads Pokémon display identity and Shiny state from the source actor.\n"
        "- Team Up applies HOLD FIRE, damage protection, targeting and tactical orders to the combat proxy.\n"
        "- The proxy is stamped with a source-derived encounter ID and Pokémon display name.\n\n"
        "## Prompt / order stability\n"
        "- Shiny prompt shows the Pokémon source name, not a proxy name such as Green Slime.\n"
        "- Prompt token is keyed by stable encounter ID, not tile position.\n"
        "- Farmer Engage/Hold/Ignore choice is remembered by encounter ID and reapplied if Pelipper recreates the proxy.\n\n"
        "## Carry-forward\n"
        "- Pelipper wild mercy floor remains prioritized.\n"
        "- Normal Pelipper wild Pokémon remain Mutation-eligible; confirmed Shiny remains Mutation-exempt.\n"
        "- Raw HP is not an Elite/Boss classifier.\n"
        "- Lower Workings 6.7.44.1 TMX fix remains intact.\n"
        "- 6.7.45 is still not started.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.4 PELIPPER SOURCE-AWARE SHINY")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
