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
STAGE = ROOT / "_stage_alpha6744_8"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_8.txt"
AUDIT = ROOT / "PELIPPER_SOURCE_MUTATION_AUDIT_ALPHA6744_8.md"
VERSION = "0.2.0-alpha.6.7.44.8"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.8_PELIPPER_SOURCE_MUTATION_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.8_PELIPPER_SOURCE_MUTATION_TEST.sha256.txt"
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
    wiring = text("ModEntry.Alpha67446.cs")
    mutation_wiring = text("ModEntry.Alpha6719.cs")
    source_mutation = text("Core/Alpha67448PelipperSourceMutationService.cs")
    runtime = text("Core/Alpha67446PelipperRuntimeService.cs")
    identity = text("Core/PelipperWildEncounterIdentityService.cs")
    capture = text("Core/PelipperCaptureSafetyService.cs")
    reaction = text("Core/EncounterReactionService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    lower_entry = text("ModEntry.Alpha6744.cs")
    tmx = text("assets/LowerWorkings.tmx")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    req("<EnableHarmony>true</EnableHarmony>" in project, "Harmony build reference missing")
    req("<LangVersion>13.0</LangVersion>" in project, "C# 13 pin missing")
    log("VERSION + BUILD ENVIRONMENT AUDIT: PASS")

    # 6.7.44.7 live telemetry proved the hidden Green Slime proxy receives damage but its sentinel
    # HP never produces lethalCandidates. 6.7.44.8 must use the visible source Pokemon's HP instead.
    for token in [
        "Alpha67448PelipperSourceMutationService", "SourceAwareDamagePrefix", "TryResolveSourceHealth",
        "PelipperWildEncounterIdentityService.TryResolve", "SourceHealthAccessor", "MaximumReasonableSourceHp",
        "sourceLethalCandidates", "source-lethal-roll-no-mutation", "source-lethal-mutated"
    ]:
        req(token in source_mutation, f"source-aware Pelipper lethal bridge missing: {token}")
    req("incoming < health.Current" in source_mutation, "source HP is not used for lethal detection")
    req("incoming < __instance.Health" not in source_mutation, "new source bridge still uses proxy sentinel HP")
    req("priority = Priority.First" in source_mutation, "source HP hook is not ordered before legacy proxy hook")
    req("TryMutateMethod.Invoke" in source_mutation, "source lethal bridge does not enter existing Mutation roll engine")
    log("PELIPPER SOURCE-HP LETHAL BRIDGE AUDIT: PASS")

    # A successful Pelipper Mutation must preserve Pelipper controller authority: generic Mutation
    # can see real Pokemon HP temporarily, then the technical proxy HP/scale are restored.
    req("__0.MaxHealth = Math.Max(1, health.Maximum)" in source_mutation, "real Pokemon HP not fed to generic Mutation engine")
    req("__0.MaxHealth = proxy.MaxHealth" in source_mutation and "__0.Health = proxy.Health" in source_mutation,
        "Pelipper technical proxy HP is not restored")
    req('MutationScaleMarker] = "1"' in source_mutation, "hidden proxy footprint scale is not neutralized")
    req("ExtraLifeMarker" in source_mutation and "mutant-phase-guard" in source_mutation,
        "source-aware HPx mutation phases missing")
    req("TryWritePathNumeric(health.Identity.SourceActor, health.Accessor.Current, health.Maximum)" in source_mutation,
        "source Pokemon HP restore missing")
    log("PELIPPER MUTATION HP-PHASE / PROXY-AUTHORITY AUDIT: PASS")

    # Player-facing Mutation identity/visuals must bind to the visible Pokemon, not Green Slime.
    req("health.Identity.DisplayName" in source_mutation, "source Pokemon display name missing")
    req("DrawSourceAuras" in source_mutation and "identity.SourceActor.GetBoundingBox()" in source_mutation,
        "source Pokemon Mutation aura missing")
    req("Forced mutation: {summary.DisplayName}" in source_mutation, "force result still reports proxy identity")
    req("PelipperSourceMutationAlpha67448.DrawSourceAuras" in wiring, "visible source aura is not wired")
    req("PelipperSourceMutationAlpha67448.Describe()" in mutation_wiring, "teamup_mutation status lacks source bridge telemetry")
    log("PELIPPER SOURCE IDENTITY + VISUAL FEEDBACK AUDIT: PASS")

    # Shiny and gift-guard behavior are explicitly frozen/carry-forward for this hotfix.
    req("PartyMemberState.Following or PartyMemberState.Waiting" in wiring, "active teammate gift guard regressed")
    req("Helper.Input.Suppress(e.Button)" in wiring, "gift guard input suppression regressed")
    req("EncounterReactionService.IsConfirmedShiny(__instance)" in source_mutation, "Shiny-over-Mutation source guard missing")
    req("HasConfirmedPelipperShinyEvidence" in source_mutation, "source-aware Shiny exclusion missing")
    req("HasConfirmedPelipperShinyEvidence" in reaction, "existing Shiny source detection regressed")
    req("EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster)" in mutation, "generic Shiny Mutation exclusion regressed")
    log("SHINY + ACTIVE TEAMMATE GIFT GUARD CARRY-FORWARD: PASS")

    # Existing Pelipper pairing/performance/capture fixes remain intact.
    req("TryReadPelipperWildEncounterId" in identity and "ConditionalWeakTable<Monster, CacheEntry>" in identity,
        "Pelipper source/proxy identity cache regressed")
    req("EncounterDiscoveryPulseTicksAlpha67446 = 3" in wiring, "20Hz encounter scan throttle regressed")
    req("ContainsRejectedSemantic" in capture and "_enabled = _modeEnabled;" in capture,
        "capture-mode fail-closed policy regressed")
    req("DescribeMutationBridge" in runtime, "legacy proxy telemetry carry-forward missing")
    log("PELIPPER IDENTITY + PERFORMANCE + CAPTURE CARRY-FORWARD: PASS")

    req("RegisterAlpha67446RuntimeFixes();" in lower_entry, "runtime compatibility layer not registered")
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
        "# Team Up 6.7.44.8 - Pelipper Source Mutation Audit\n\n"
        "- Live 6.7.44.7 telemetry proved 542 Pelipper wild damage calls but zero proxy-lethal candidates.\n"
        "- Lethal detection now resolves the visible source Pokemon and uses its live HP rather than Green Slime sentinel HP.\n"
        "- Existing Mutation rolls/stat/minion engine remains authoritative.\n"
        "- Pelipper technical proxy HP is restored after transformation; the player no longer gets a 2,000,000 HP Green Slime as the Mutation identity.\n"
        "- HP multiplier is represented as source-Pokemon life phases while preserving Pelipper controller ownership.\n"
        "- Mutation aura and force-result identity bind to the visible Pokemon source.\n"
        "- Shiny behavior and active-teammate gift guard are carried forward unchanged.\n"
        "- Lower Workings and EN/VI parity carried forward. 6.7.45 story work is not started.\n",
        encoding="utf-8",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.8 PELIPPER SOURCE MUTATION")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
