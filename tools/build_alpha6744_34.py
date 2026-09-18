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
STAGE = ROOT / "_stage_alpha6744_34"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_34.txt"
VERSION = "0.2.0-alpha.6.7.44.34"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.34_COMBAT_PRESENCE_FIX_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.34_COMBAT_PRESENCE_FIX_TEST.sha256.txt"
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
    aggro = text("Core/Alpha674420MutationAggroService.cs")
    steering = text("Core/Alpha674423PelipperMutantLeaderSmoothingService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    wiring = text("ModEntry.Alpha67446.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    log("VERSION IDENTITY 6.7.44.34: PASS")

    # Keep the user-live-loaded 6.7.44.23 runtime shape. Do not reintroduce the post-.23 crash stack.
    forbidden = [
        "Core/Alpha674424EliteCombatFinalizationService.cs",
        "Core/Alpha674424EliteReachOverlayService.cs",
        "Core/Alpha674425PelipperLeaderContinuousChaseService.cs",
        "Core/Alpha674426PelipperMutantPhaseLifecycleService.cs",
        "Core/Alpha674427MutationRegressionGuardService.cs",
        "Core/Alpha674428LowerWorkingsRuntimeGateService.cs",
    ]
    for rel in forbidden:
        req(not (SRC / rel).exists(), f"unsafe post-44.23 runtime leaked into 44.34: {rel}")
    req("teamup_lower_runtime" not in text("ModEntry.Alpha6744.cs"), "44.28 lower runtime command leaked into 44.34")
    log("POST-44.23 CRASH-STACK EXCLUSION: PASS")

    # 44.34 functional restoration: Pelipper source identity and Nidoran gender-safe pairing.
    req("pokemonnpcencounter" in pairing.lower(), "PokemonNpcEncounter/v1 source identity support missing")
    req('.Replace("♂", " Male ", StringComparison.Ordinal)' in pairing, "Nidoran male normalization guard missing")
    req('.Replace("♀", " Female ", StringComparison.Ordinal)' in pairing, "Nidoran female normalization guard missing")
    req("pokemonNpcEncounterKey" in pairing, "source encounter key branch missing")
    log("PELIPPER SOURCE-ID + NIDORAN GENDER PAIRING CARRY-FORWARD: PASS")

    # 44.34 combat-presence fixes requested by live runtime feedback.
    req("PelipperTechnicalBaseDamageFloor = 4" in mutation, "Pelipper technical damage floor missing")
    req("MutationIntendedDamageMarker" in mutation, "Mutation intended-damage marker missing")
    req("BaseAggroRadiusTiles = 6" in aggro and "MutationAggroRadiusTiles = BaseAggroRadiusTiles * 3" in aggro,
        "x3 Mutation aggro arena missing")
    req('PelipperEngagedKey = "Griff.PelipperTown/WildCombatEngaged"' in aggro,
        "Pelipper combat-engaged override missing")
    req('PelipperPassiveKey = "Griff.PelipperTown/PassiveUntilAttacked"' in aggro,
        "Pelipper passive-until-attacked override missing")
    req('monster.modData[PelipperPassiveKey] = "false"' in aggro and 'source.modData[PelipperPassiveKey] = "false"' in aggro,
        "Pelipper passive override not applied to source+proxy")
    req("PelipperMutantDamageFloor = 8" in aggro and "PelipperOrdinaryDamageFloor = 4" in aggro,
        "Pelipper leader/minion damage floors missing")
    req("Alpha674420MutationAggroService.PelipperMutantDamageFloor" in steering,
        "leader hit path does not preserve damage floor")
    req("OnAlpha674434RenderingWorld" in wiring and "Helper.Events.Display.RenderingWorld += OnAlpha674434RenderingWorld" in wiring,
        "pre-render visible scale stabilization missing")
    req("PelipperVisibleMutationAlpha674413.Update();" in wiring.split("OnAlpha674434RenderingWorld", 1)[1],
        "visible Mutation scale is not asserted at pre-render")
    log("COMBAT PRESENCE: FLICKER + X3 AGGRO + DAMAGE: PASS")

    for token in [
        "Alpha674423PelipperMutantLeaderSmoothingService",
        "LeaderHoldCenterDistance = 92f",
        "LeaderAttackCenterDistance = 112f",
        "source.Halt()",
        "if (!pair.Leader)",
        "source.MovePosition(Game1.currentGameTime, Game1.viewport, location)",
    ]:
        req(token in steering, f"44.23 carry-forward steering token missing: {token}")
    req("PelipperMutationLeaderSmoothingAlpha674423 = new Alpha674423PelipperMutantLeaderSmoothingService" in wiring,
        "44.23 steering service not wired")
    log("KNOWN-GOOD 6.7.44.23 RUNTIME CARRY-FORWARD: PASS")

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
            "Team Up/TeamUp.dll",
            "Team Up/manifest.json",
            "Team Up/i18n/default.json",
            "Team Up/i18n/vi.json",
            "Team Up/assets/LowerWorkings.tmx",
        ]:
            req(required in names, f"package missing {required}")
        packaged_manifest = json.loads(archive.read("Team Up/manifest.json").decode("utf-8"))
        req(packaged_manifest.get("Version") == VERSION, "packaged manifest mismatch")
    log("ZIP CONTENT AUDIT: PASS")

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")
    log("BUILD SUCCESS - ALPHA 6.7.44.34 COMBAT PRESENCE FIX")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
