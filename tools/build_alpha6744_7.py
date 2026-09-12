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
STAGE = ROOT / "_stage_alpha6744_7"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_7.txt"
AUDIT = ROOT / "ACTIVE_TEAMMATE_GIFT_GUARD_AUDIT_ALPHA6744_7.md"
VERSION = "0.2.0-alpha.6.7.44.7"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.7_ACTIVE_TEAMMATE_GIFT_GUARD_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.7_ACTIVE_TEAMMATE_GIFT_GUARD_TEST.sha256.txt"
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

    # Active teammate gift guard: only held-item action on Following/Waiting members is blocked.
    req("OnAlpha67447GiftGuardButtonPressed" in wiring, "gift guard handler missing")
    req("Game1.player.ActiveObject is null" in wiring, "gift guard is not held-item gated")
    req("Party.Get(npc.Name, Game1.player.UniqueMultiplayerID)" in wiring, "gift guard is not roster-owner scoped")
    req("PartyMemberState.Following or PartyMemberState.Waiting" in wiring, "gift guard does not restrict itself to active teammates")
    req("Helper.Input.Suppress(e.Button)" in wiring, "gift guard does not suppress vanilla gifting input")
    req("Blocked held-item gift to active teammate" in wiring, "gift guard diagnostic missing")
    for destructive in ["reduceActiveItemByOne", "ActiveObject = null", "removeItemFromInventory", "changeFriendship"]:
        req(destructive not in wiring, f"gift guard unexpectedly mutates inventory/friendship: {destructive}")
    log("ACTIVE TEAMMATE GIFT GUARD AUDIT: PASS")

    # Mutation bridge telemetry must tell live tests whether Pelipper lethal hits actually reach rolls.
    for token in [
        "DescribeMutationBridge", "_wildDamageCalls", "_lethalCandidates", "_mutationAttempts",
        "_mutationIntercepts", "_duplicateSuppressed", "_shinyLethalExcluded", "_lastLethalLine"
    ]:
        req(token in runtime, f"mutation bridge telemetry missing: {token}")
    req("PelipperRuntimeAlpha67446.DescribeMutationBridge()" in mutation_wiring, "teamup_mutation status does not expose bridge telemetry")
    req("PelipperPreLethalPrefix" in runtime and 'method.Name.Equals("takeDamage"' in runtime, "pre-lethal Mutation hook regressed")
    req("__args[0] = 0" in runtime, "successful pre-lethal Mutation no longer cancels lethal hit")
    req("EncounterReactionService.IsConfirmedShiny(__instance)" in runtime, "Shiny-over-Mutation guard regressed")
    log("PELIPPER MUTATION BRIDGE TELEMETRY AUDIT: PASS")

    # Dense Pelipper scenes must pair source/proxy by stable encounter ID when available.
    req("TryReadPelipperWildEncounterId" in identity, "Pelipper WildEncounterId resolver missing")
    req("proxyWildEncounterId" in identity, "proxy encounter ID matching missing")
    req("Ambiguous scenes fail closed" in identity, "ambiguous spatial fallback is not documented/fail-closed")
    req("ConditionalWeakTable<Monster, CacheEntry>" in identity, "identity performance cache missing")
    req("EncounterDiscoveryPulseTicksAlpha67446 = 3" in wiring, "20Hz encounter scan throttle regressed")
    log("PELIPPER IDENTITY + PERFORMANCE CARRY-FORWARD: PASS")

    # Capture mode must still reject bonus/chance semantics.
    req("ContainsRejectedSemantic" in capture, "capture bonus/chance rejection regressed")
    req("_enabled = _modeEnabled;" in capture, "capture safety no longer fails closed")
    req("combatmode" in capture and "battlemode" in capture, "real combat mode detection regressed")
    req("HasConfirmedPelipperShinyEvidence" in reaction, "source-aware Shiny evidence regressed")
    req("EncounterReactionService.HasConfirmedPelipperShinyEvidence(monster)" in mutation, "Shiny Mutation exclusion regressed")
    log("PELIPPER CAPTURE + SHINY CARRY-FORWARD: PASS")

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
        "# Team Up 6.7.44.7 - Active Teammate Gift Guard + Mutation Telemetry\n\n"
        "- Active Following/Waiting Team Up members cannot receive vanilla held-item gifts.\n"
        "- Inactive roster members retain normal vanilla gifting.\n"
        "- Gift guard suppresses input only; it never removes the held item or changes friendship.\n"
        "- `teamup_mutation status` exposes Pelipper damage/lethal/attempt/intercept counters.\n"
        "- 6.7.44.6 pre-lethal Mutation hook, encounter-ID pairing, performance cache, Shiny safety and Capture-mode fixes are carried forward.\n"
        "- Lower Workings and EN/VI parity carried forward. 6.7.45 story work is not started.\n",
        encoding="utf-8",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44.7 ACTIVE TEAMMATE GIFT GUARD")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
