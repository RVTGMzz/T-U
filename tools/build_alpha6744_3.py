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
STAGE = ROOT / "_stage_alpha6744_3"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_3.txt"
AUDIT = ROOT / "PELIPPER_RUNTIME_HOTFIX_AUDIT_ALPHA6744_3.md"
VERSION = "0.2.0-alpha.6.7.44.3"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.3_PELIPPER_RUNTIME_HOTFIX_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.3_PELIPPER_RUNTIME_HOTFIX_TEST.sha256.txt"
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
    mutation = text("Combat/MonsterMutationService.cs")
    lower_entry = text("ModEntry.Alpha6744.cs")
    tmx = text("assets/LowerWorkings.tmx")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong 6.7.44.3 project version")

    # Pelipper capture authority regression gate.
    req("_enabled = pelipperDetected;" in capture, "Pelipper wild mercy floor is still gated by guessed Catch Mode")
    req("priority=pelipper-wild" in capture, "capture policy diagnostic does not expose Pelipper-priority authority")
    req("MonsterMutationService.IsMutant(monster)" in capture, "mutants can still inherit Pelipper capture floor")
    req("_enabled = _modeConfirmed && enabled;" not in capture, "obsolete mode-confirmation capture gate remains")
    log("PELIPPER WILD MERCY PRIORITY AUDIT: PASS")

    # Shiny false-positive and prompt-spam gates.
    req("HasConfirmedPelipperShinyEvidence" in reaction, "strict Pelipper Shiny evidence gate missing")
    req("IsAuthoritativeShinyKey" in reaction, "authoritative Shiny-state key filter missing")
    req("_reactionCooldownUntil" in reaction, "stable encounter reaction cooldown missing")
    req("if (monster.MaxHealth >= 300)" not in reaction, "raw HP still classifies ordinary monsters as Elite/Boss")
    req("ClearFalseShinyState" in reaction, "6.7.44.2 false-Shiny state repair missing")
    req('ShinyPromptCooldownAlpha67442[token] = long.MaxValue;' in wiring, "Farmer Shiny answer is not sticky")
    req('=> $"{location.NameOrUniqueName}|{target.Name}|{target.GetType().FullName}";' in wiring,
        "Shiny prompt token is not stable across proxy movement")
    log("SHINY FALSE-POSITIVE + PROMPT SPAM AUDIT: PASS")

    # Mutation + Pelipper compatibility gate.
    old_blanket = "PelipperTownCompatibilityService.IsWildCombatActor(monster)\n            || PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster)"
    req(old_blanket not in mutation, "Pelipper wild blanket Mutation exclusion remains")
    req("bool pelipperWild = PelipperTownCompatibilityService.IsWildCombatActor(monster);" in mutation,
        "normal Pelipper wild Mutation path missing")
    req("EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster)" in mutation,
        "Shiny-over-Mutation priority missing")
    req('monster.modData[PelipperTownCompatibilityService.CombatTargetOptInKey] = "true";' in mutation,
        "mutated Pelipper wild target is not restored to combat targeting")
    log("PELIPPER WILD MUTATION ELIGIBILITY AUDIT: PASS")

    # Existing 6.7.44.2 reaction wiring and 6.7.44.1 map safety must survive.
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
        "# Team Up Alpha 6.7.44.3 - Pelipper Runtime Hotfix Audit\n\n"
        "## Live regressions addressed\n"
        "- Ordinary Green Slime / normal monsters must not become Shiny because a runtime type exposes Shiny capability/chance members.\n"
        "- Raw MaxHealth is no longer an Elite/Boss classifier.\n"
        "- Encounter reactions receive a stable cooldown so proxy recreation cannot spam the same reaction continuously.\n"
        "- A Farmer response to a Shiny prompt is sticky for that encounter and movement cannot create a new prompt token.\n\n"
        "## Pelipper capture priority\n"
        "- Pelipper presence + genuine wild combat proxy now activates Team Up low-HP mercy protection.\n"
        "- Team Up prefers a reflected source threshold when available, otherwise uses the existing 10% fallback.\n"
        "- Mutations are combat-only Team Up threats and do not inherit the Pelipper mercy floor.\n\n"
        "## Pelipper Mutation compatibility\n"
        "- Normal Pelipper wild combat proxies are eligible for Team Up Mutation.\n"
        "- Confirmed natural Shiny remains Mutation-exempt.\n"
        "- Owned/source-controlled companions remain excluded.\n"
        "- A Pelipper wild proxy transformed into a Mutant is restored as a Team Up combat target.\n\n"
        "## Carry-forward locks\n"
        "- Lower Workings 6.7.44.1 TMX CSV fix remains intact.\n"
        "- 6.7.45 remains reserved for Containment Chamber Escalation and is not started here.\n"
        "- George/Evelyn reveals, party caps, Pelipper source ownership and story progression are unchanged.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.3 PELIPPER RUNTIME HOTFIX")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
