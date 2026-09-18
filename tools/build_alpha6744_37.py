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
STAGE = ROOT / "_stage_alpha6744_37"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_37.txt"
VERSION = "0.2.0-alpha.6.7.44.37"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.37_REGRESSION_STABILITY_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.37_REGRESSION_STABILITY_TEST.sha256.txt"
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
    factory = text("Combat/MonsterMutationMinionFactory.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    native = text("Core/Alpha674418NativeMutationMinionService.cs")
    source_mutation = text("Core/Alpha67448PelipperSourceMutationService.cs")
    reward = text("Core/Alpha674414PelipperMutantRewardService.cs")
    capture = text("Core/Alpha674436EliteCaptureGuardService.cs")
    steering = text("Core/Alpha674423PelipperMutantLeaderSmoothingService.cs")
    aggro = text("Core/Alpha674420MutationAggroService.cs")
    wiring = text("ModEntry.Alpha67446.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    log("VERSION IDENTITY 6.7.44.37: PASS")

    forbidden = [
        "Core/Alpha674424EliteCombatFinalizationService.cs",
        "Core/Alpha674424EliteReachOverlayService.cs",
        "Core/Alpha674425PelipperLeaderContinuousChaseService.cs",
        "Core/Alpha674426PelipperMutantPhaseLifecycleService.cs",
        "Core/Alpha674427MutationRegressionGuardService.cs",
        "Core/Alpha674428LowerWorkingsRuntimeGateService.cs",
    ]
    for rel in forbidden:
        req(not (SRC / rel).exists(), f"old crash-stack service leaked into 44.37: {rel}")
    req("teamup_lower_runtime" not in text("ModEntry.Alpha6744.cs"), "Lower Workings crash-path leaked into 44.37")
    log("OLD 44.24-44.30 CRASH-STACK EXCLUSION: PASS")

    req("public static Monster? Create(" in factory, "factory is not nullable fail-closed")
    req('mode = "pelipper-native-required"' in factory, "Pelipper native-required refusal missing")
    req('mode = "unsupported-fail-closed"' in factory, "unsupported fail-closed mode missing")
    req("FailClosedRejected" in factory, "fail-closed telemetry missing")
    req("new GreenSlime" not in factory, "GreenSlime construction still exists in factory")
    req("green-slime-fallback" not in factory, "legacy GreenSlime fallback mode still exists")
    log("FACTORY-LEVEL NO-FALLBACK / FAIL-CLOSED: PASS")

    req("Monster? minion = MonsterMutationMinionFactory.Create(" in mutation, "legacy wave nullable factory handling missing")
    req("if (minion is null)" in mutation, "legacy wave null rejection missing")
    req("failClosed={failClosed}" in mutation, "legacy wave fail-closed telemetry missing")
    req("Monster? candidate = MonsterMutationMinionFactory.Create(" in native, "native service nullable factory handling missing")
    req('!mode.Equals("same-runtime-type", StringComparison.Ordinal)' in native, "exact runtime type requirement missing")
    req("unsupported sources fail closed" in native, "native service stability log missing")
    log("VANILLA/CUSTOM SAME-TYPE + UNSUPPORTED FAIL-CLOSED: PASS")

    for token in [
        '"teamup_mutation_regression"',
        "Mutation regression audit:",
        "typeMismatch",
        "recursiveMutants",
        "missingExcluded",
        "factoryFallback",
        "failClosed",
        "pelipperDuplicateEncounter",
    ]:
        req(token in wiring, f"runtime regression audit token missing: {token}")
    log("RUNTIME REGRESSION AUDIT COMMAND: PASS")

    req('PhaseTotalMarker = "Ronvotri.TeamUp/MutationPhaseTotal"' in source_mutation, "44.36 phase total marker regressed")
    req('PhaseCurrentMarker = "Ronvotri.TeamUp/MutationPhaseCurrent"' in source_mutation, "44.36 phase current marker regressed")
    req("MonsterDropPrefix" in reward and "prematureDropBlocks" in reward, "44.36 final-only reward guard regressed")
    req("Alpha674436EliteCaptureGuardService" in capture, "44.36 capture guard regressed")
    req("LeaderHoldCenterDistance = 128f" in steering and "LeaderAttackCenterDistance = 160f" in steering,
        "44.35 pursuit/reach regressed")
    req("MutationAggroRadiusTiles = BaseAggroRadiusTiles * 3" in aggro, "44.34 aggro arena regressed")
    log("44.34-44.36 CONTRACT CARRY-FORWARD: PASS")

    proc = subprocess.run(
        ["dotnet", "build", str(SRC / "TeamUp.csproj"), "-c", "Release", "--nologo", "-warnaserror"],
        cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False,
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
    log("BUILD SUCCESS - ALPHA 6.7.44.37 REGRESSION + STABILITY")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
