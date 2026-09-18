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
STAGE = ROOT / "_stage_alpha6744_36"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_36.txt"
VERSION = "0.2.0-alpha.6.7.44.36"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.36_ELITE_CONTRACT_FINALIZATION_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.36_ELITE_CONTRACT_FINALIZATION_TEST.sha256.txt"
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
    source_mutation = text("Core/Alpha67448PelipperSourceMutationService.cs")
    reward = text("Core/Alpha674414PelipperMutantRewardService.cs")
    capture = text("Core/Alpha674436EliteCaptureGuardService.cs")
    steering = text("Core/Alpha674423PelipperMutantLeaderSmoothingService.cs")
    wiring = text("ModEntry.Alpha67446.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    log("VERSION IDENTITY 6.7.44.36: PASS")

    forbidden = [
        "Core/Alpha674424EliteCombatFinalizationService.cs",
        "Core/Alpha674424EliteReachOverlayService.cs",
        "Core/Alpha674425PelipperLeaderContinuousChaseService.cs",
        "Core/Alpha674426PelipperMutantPhaseLifecycleService.cs",
        "Core/Alpha674427MutationRegressionGuardService.cs",
        "Core/Alpha674428LowerWorkingsRuntimeGateService.cs",
    ]
    for rel in forbidden:
        req(not (SRC / rel).exists(), f"old crash-stack service leaked into 44.36: {rel}")
    req("teamup_lower_runtime" not in text("ModEntry.Alpha6744.cs"), "Lower Workings crash-path leaked into 44.36")
    log("OLD 44.24-44.30 CRASH-STACK EXCLUSION: PASS")

    for token in [
        'PhaseTotalMarker = "Ronvotri.TeamUp/MutationPhaseTotal"',
        'PhaseCurrentMarker = "Ronvotri.TeamUp/MutationPhaseCurrent"',
        'NoCaptureMarker = "Ronvotri.TeamUp/MutantLeaderNoCapture"',
        '__0.modData[PhaseTotalMarker] = bars.ToString',
        '__0.modData[PhaseCurrentMarker] = "1"',
        'health.Identity.SourceActor.modData[NoCaptureMarker] = "true"',
        'currentPhase = Math.Clamp(totalPhases - extraLives, 1, totalPhases)',
        '__instance.modData[PhaseCurrentMarker] = currentPhase.ToString',
        '__instance.modData[PhaseCurrentMarker] = finalTotal.ToString',
    ]:
        req(token in source_mutation, f"three-phase lifecycle token missing: {token}")
    req('MutationHealthMultiplier { get; set; } = 3f' in text("ModConfig.cs"), "default Mutation HP multiplier is not x3")
    log("THREE HP PHASE CONTRACT: PASS")

    for token in [
        "MonsterDropPrefix",
        "prematureDropBlocks",
        "extraLives > 0",
        "currentPhase < totalPhases",
        "return false;",
        "MutantLootMultiplier = 3",
        "__originalMethod.Invoke(__instance, __args)",
    ]:
        req(token in reward, f"final-only x3 reward token missing: {token}")
    log("FINAL-PHASE-ONLY X3 NATIVE LOOT: PASS")

    for token in [
        "Alpha674436EliteCaptureGuardService",
        "leader=blocked",
        "followers=native",
        "LooksLikeCaptureMethod",
        "MonsterMutationService.IsMutant(monster)",
        "NoCaptureMarker",
    ]:
        req(token in capture, f"capture guard token missing: {token}")
    req("EliteCaptureGuardAlpha674436 = new Alpha674436EliteCaptureGuardService" in wiring,
        "capture guard is not wired")
    req("EliteCaptureGuardAlpha674436.ResetTelemetry();" in wiring,
        "capture guard telemetry reset missing")
    log("MUTANT LEADER CAPTURE BLOCK / FOLLOWER NATIVE CAPTURE: PASS")

    req("LeaderHoldCenterDistance = 128f" in steering, "44.35 hold distance regressed")
    req("LeaderAttackCenterDistance = 160f" in steering, "44.35 reach regressed")
    req("MutationAggroRadiusTiles = BaseAggroRadiusTiles * 3" in text("Core/Alpha674420MutationAggroService.cs"),
        "44.34 aggro arena regressed")
    log("44.34-44.35 COMBAT CARRY-FORWARD: PASS")

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
    log("BUILD SUCCESS - ALPHA 6.7.44.36 ELITE CONTRACT FINALIZATION")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
