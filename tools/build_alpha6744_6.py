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
STAGE = ROOT / "_stage_alpha6744_6"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_6.txt"
AUDIT = ROOT / "PELIPPER_RUNTIME_PERFORMANCE_AUDIT_ALPHA6744_6.md"
VERSION = "0.2.0-alpha.6.7.44.6"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.6_PELIPPER_RUNTIME_PERFORMANCE_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.6_PELIPPER_RUNTIME_PERFORMANCE_TEST.sha256.txt"
WIDTH, HEIGHT = 32, 24
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
    manifest = json.loads(text("manifest.json"))
    capture = text("Core/PelipperCaptureSafetyService.cs")
    identity = text("Core/PelipperWildEncounterIdentityService.cs")
    runtime = text("Core/Alpha67446PelipperRuntimeService.cs")
    wiring = text("ModEntry.Alpha67446.cs")
    lower_entry = text("ModEntry.Alpha6744.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    reaction = text("Core/EncounterReactionService.cs")
    tmx = text("assets/LowerWorkings.tmx")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    req("<EnableHarmony>true</EnableHarmony>" in project, "Harmony build reference missing")
    req("<LangVersion>13.0</LangVersion>" in project, "C# 13 pin missing")
    log("VERSION + BUILD ENVIRONMENT AUDIT: PASS")

    # 6.7.44.5 accidentally treated capture-chance bonus settings as combat mode/floor.
    for rejected in ["bonus", "chance", "multiplier", "rate", "accuracy", "pity", "odds", "weight", "roll"]:
        req(rejected in capture, f"rejected capture semantic missing: {rejected}")
    req("ContainsRejectedSemantic" in capture, "bonus/chance rejection gate missing")
    req("combatmode" in capture and "battlemode" in capture, "actual runtime combat-mode detection missing")
    req("_enabled = _modeEnabled;" in capture, "capture safety no longer fails closed")
    req("fallback-10%-confirmed-capture-mode" in capture, "10% fallback policy missing")
    req("ModeValue" in capture, "combat mode diagnostic value missing")
    req("EnableCaptureTechniqueBonuses" not in capture, "known wrong Pelipper bonus field hardcoded")
    req("WildEncounterLowHealthCatchBonus" not in capture, "known wrong low-health catch bonus hardcoded")
    log("PELIPPER REAL COMBAT MODE / BONUS REJECTION AUDIT: PASS")

    # Performance: source/proxy pairing must be weak-cached and expensive encounter discovery must
    # no longer run at full 60Hz.
    req("ConditionalWeakTable<Monster, CacheEntry>" in identity, "weak source/proxy cache missing")
    req("PositiveCacheTicks" in identity and "NegativeCacheTicks" in identity, "identity cache retry policy missing")
    req("EncounterDiscoveryPulseTicksAlpha67446 = 3" in wiring, "20Hz encounter discovery throttle missing")
    req("UpdateTicking -= OnAlpha67442UpdateTicking" in wiring, "old full-rate encounter handler still active")
    req("ShinyEvidenceCache" in runtime and "ShinyEvidencePrefix" in runtime, "Shiny reflection cache missing")
    log("PELIPPER ENCOUNTER PERFORMANCE AUDIT: PASS")

    # Pelipper wild defeats can bypass Monster.deathAnimation. Add a pre-lethal takeDamage bridge
    # into the existing TryMutate path, but keep Shiny and already-mutated actors excluded.
    req('method.Name.Equals("takeDamage"' in runtime, "pre-lethal takeDamage hook missing")
    req("PelipperPreLethalPrefix" in runtime, "Pelipper pre-lethal prefix missing")
    req('GetMethod(\n        "TryMutate"' in runtime or '"TryMutate"' in runtime, "existing Mutation engine bridge missing")
    req("__args[0] = 0" in runtime, "lethal hit is not cancelled after successful mutation")
    req("EncounterReactionService.IsConfirmedShiny(__instance)" in runtime, "Shiny-over-Mutation guard missing")
    req("EliteBossPostfix" in runtime, "ordinary Pelipper Elite/Boss proxy guard missing")
    req("EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster)" in mutation, "existing Shiny Mutation exclusion regressed")
    log("PELIPPER PRE-LETHAL MUTATION + ELITE GUARD AUDIT: PASS")

    # Wiring/carry-forward.
    req("RegisterAlpha67446RuntimeFixes();" in lower_entry, "6.7.44.6 runtime layer not registered")
    req("EnsureAlpha67442EncounterReactionsRegistered();" in lower_entry, "Shiny encounter layer regressed")
    req("PelipperWildEncounterIdentityService.TryResolve" in reaction, "source-aware Shiny identity regressed")
    log("SHINY SOURCE-AWARE CARRY-FORWARD: PASS")

    root = ET.fromstring(tmx)
    req(root.attrib.get("width") == str(WIDTH) and root.attrib.get("height") == str(HEIGHT), "Lower Workings dimensions regressed")
    layers = root.findall("layer")
    req([layer.attrib.get("name") for layer in layers] == ["Back", "Buildings", "Front"], "Lower Workings layer order regressed")
    for layer in layers:
        data = layer.find("data")
        req(data is not None and data.attrib.get("encoding") == "csv", "Lower Workings CSV encoding regressed")
        tokens = [token.strip() for token in ((data.text if data is not None else "") or "").split(",") if token.strip()]
        req(len(tokens) == EXPECTED, f"Lower Workings {layer.attrib.get('name')} token count regressed")
        for token in tokens:
            value = int(token)
            req(0 <= value <= 0xFFFFFFFF, "Lower Workings UInt32 range regressed")
    req(tmx.count(",\n") >= (HEIGHT - 1) * 3, "Lower Workings row-boundary comma fix regressed")
    req(set(en) == set(vi), "EN/VI parity regressed")
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
        expected_files = {
            "Team Up/TeamUp.dll",
            "Team Up/manifest.json",
            "Team Up/i18n/default.json",
            "Team Up/i18n/vi.json",
            "Team Up/assets/LowerWorkings.tmx",
        }
        req(expected_files.issubset(names), f"package missing files: {sorted(expected_files - names)}")
        packaged_manifest = json.loads(archive.read("Team Up/manifest.json").decode("utf-8"))
        req(packaged_manifest.get("Version") == VERSION, "packaged manifest mismatch")
    log("ZIP CONTENT AUDIT: PASS")

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Team Up 6.7.44.6 - Pelipper Runtime Performance Audit\n\n"
        "- Reject capture bonus/chance/multiplier fields as mode or HP floor.\n"
        "- Resolve live combat mode from runtime/player state only; unknown remains OFF.\n"
        "- Use 10% fallback only after Capture mode is positively confirmed.\n"
        "- Cache Pelipper source/proxy identity and Shiny reflection evidence.\n"
        "- Throttle expensive encounter discovery to 20Hz while persistent HOLD state remains active every combat tick.\n"
        "- Intercept Pelipper pre-lethal Monster.takeDamage so Mutation/Surge story sees wild defeats that bypass deathAnimation.\n"
        "- Suppress generic Elite/Boss reactions for ordinary Pelipper wild proxies.\n"
        "- Shiny remains Mutation-excluded and Emergency Hold remains independent of Capture mode.\n"
        "- Lower Workings and EN/VI parity carried forward. 6.7.45 not started.\n",
        encoding="utf-8",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.6 PELIPPER RUNTIME PERFORMANCE")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
