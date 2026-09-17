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
STAGE = ROOT / "_stage_alpha6744_27"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_27.txt"
VERSION = "0.2.0-alpha.6.7.44.27"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.27_MUTATION_REGRESSION_GUARD_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.27_MUTATION_REGRESSION_GUARD_TEST.sha256.txt"
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
    config = text("ModConfig.cs")
    binding = text("Core/Alpha674412PelipperModDataHpBindingService.cs")
    visible = text("Core/Alpha674413PelipperVisibleMutationService.cs")
    reward = text("Core/Alpha674414PelipperMutantRewardService.cs")
    native_minions = text("Core/Alpha674418NativeMutationMinionService.cs")
    command_gate = text("Core/Alpha674419PelipperSpawnCommandGateService.cs")
    steering = text("Core/Alpha674423PelipperMutantLeaderSmoothingService.cs")
    elite = text("Core/Alpha674424EliteCombatFinalizationService.cs")
    reach = text("Core/Alpha674424EliteReachOverlayService.cs")
    continuous = text("Core/Alpha674425PelipperLeaderContinuousChaseService.cs")
    phases = text("Core/Alpha674426PelipperMutantPhaseLifecycleService.cs")
    regression = text("Core/Alpha674427MutationRegressionGuardService.cs")
    source_mutation = text("Core/Alpha67448PelipperSourceMutationService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    factory = text("Combat/MonsterMutationMinionFactory.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")

    for token in ["Griff.PelipperTown/WildCurrentHealth", "Griff.PelipperTown/WildMaxHealth", "SourceProxyMap"]:
        req(token in binding, f"HP binding missing {token}")
    req("MutationVisualScaleMultiplier { get; set; } = 2f" in config, "Mutation visible scale default is not x2")
    req("PelipperVisibleScaleCap = 2f" in visible, "Pelipper visible x2 cap missing")
    log("PELIPPER HP + VISIBLE SCALE CARRY-FORWARD: PASS")

    for token in ["pokemon_spawn", "callback(\"pokemon_spawn\", args)", "pelipper-native", "No Slime fallback"]:
        req(token in native_minions, f"native source minion pipeline missing {token}")
    req("MutationMinionMin { get; set; } = 2" in config and "MutationMinionMax { get; set; } = 4" in config,
        "2-4 minion contract changed")
    req("IsMutationMinion(monster)" in mutation, "Mutation minions are not excluded from recursive Mutation")
    log("SOURCE-NATIVE 2-4 MINIONS + NO-SLIME CONTRACT: PASS")

    for token in ["RequestFinalizer", "RestoreGate", "TryWrite(true)", "state.Binding.TryWrite(state.OriginalValue)"]:
        req(token in command_gate, f"Pelipper spawn-command gate missing {token}")
    req("WriteConfig" not in command_gate and "writeConfig" not in command_gate,
        "spawn-command gate must not persist Pelipper config")
    log("PELIPPER SPAWN COMMAND GATE CARRY-FORWARD: PASS")

    for token in [
        "ApplyFollowerSeparation",
        "source.MovePosition(Game1.currentGameTime, Game1.viewport, location)",
        "proxy.collidesWithOtherCharacters.Value = false",
        "farmer.takeDamage(damage, overrideParry: false, proxy)",
        "minionMoves=",
    ]:
        req(token in steering, f"follower steering carry-forward missing {token}")
    req("new PathFindController" not in steering, "tile PathFindController must stay disabled")
    log("FOLLOWER PACK STEERING CARRY-FORWARD: PASS")

    for token in [
        "ExtendedLeaderAttackDistance = 160f",
        "ExtendedLeaderHoldDistance = 128f",
        "_continuousChase = new Alpha674425PelipperLeaderContinuousChaseService",
        "_phaseLifecycle = new Alpha674426PelipperMutantPhaseLifecycleService",
        "_regressionGuard = new Alpha674427MutationRegressionGuardService",
        "_continuousChase.Describe()",
        "_phaseLifecycle.Describe()",
        "_regressionGuard.Describe()",
        "_regressionGuard.ResetTelemetry()",
    ]:
        req(token in reach, f"elite runtime chain missing {token}")
    log("ELITE REACH + CHASE + PHASE + REGRESSION CHAIN: PASS")

    for token in [
        "Alpha674425PelipperLeaderContinuousChaseService",
        "HoldDistance = 128f",
        "MoveLeaderContinuously",
        "ApplyDirectionalState",
        "legacyHaltBypassed=",
        "providerPullbacks=",
        "continuous-chase",
    ]:
        req(token in continuous, f"6.7.44.25 continuous chase missing {token}")
    req("source.Halt()" not in continuous, "leader continuous chase must not Halt() every step")
    req("source.Position =" not in continuous, "leader continuous chase must not teleport source")
    req("controller = null" not in continuous and "temporaryController = null" not in continuous,
        "continuous chase must not destroy provider controllers")
    log("MUTANT LEADER CONTINUOUS CHASE / NO PER-STEP HALT: PASS")

    for token in [
        "MutantIntendedDamage",
        "MutantLeaderNoCapture",
        "requestedDamageTotal=",
        "actualDamageTotal=",
        "captureBlocks=",
        "ContainsMutantLeaderTarget",
        "[MutationCaptureGuard]",
    ]:
        req(token in elite, f"elite damage/capture carry-forward missing {token}")
    req("IsMutationMinion" not in elite,
        "capture guard must not block ordinary Mutation followers")
    log("MUTANT X2 DAMAGE + LEADER CAPTURE GUARD CARRY-FORWARD: PASS")

    for token in [
        "ExtraLifeMarker",
        "LogicalMaxHpMarker",
        "mutant-phase-guard",
        "mutant-final-lethal",
        "phaseGuards=",
        "finalLethalPasses=",
    ]:
        req(token in source_mutation, f"source-aware phase engine missing {token}")
    log("SOURCE-AWARE THREE-BAR ENGINE CARRY-FORWARD: PASS")

    for token in [
        "Alpha674426PelipperMutantPhaseLifecycleService",
        "PhaseTotalMarker",
        "PhaseCurrentMarker",
        "BeforeMonsterDrop",
        "prematureDropBlocks=",
        "finalDropPasses=",
        "restoreVerified=",
        "explicit-3-phase",
    ]:
        req(token in phases, f"6.7.44.26 phase lifecycle missing {token}")
    req("new GreenSlime" not in phases, "phase lifecycle must not create fallback monsters")
    log("PELIPPER EXPLICIT 1/3 -> 2/3 -> 3/3 LIFECYCLE: PASS")

    for token in [
        "Alpha674427MutationRegressionGuardService",
        "MonsterMutationMinionFactory.Create",
        "Priority.First",
        "pelipper-native-required",
        "unsupported-fail-closed",
        "same-runtime-type",
        "TryCreateSameRuntimeType",
        "NormalizeNormalStats",
        "legacyFactoryOriginalRuns=0",
        "greenSlimeFallbackPrevented=",
        "constructorFailures=",
        "return false;",
    ]:
        req(token in regression, f"6.7.44.27 regression guard missing {token}")
    req("new GreenSlime" not in regression, "6.7.44.27 guard must never instantiate an unrelated GreenSlime")
    req("new PathFindController" not in regression, "regression guard must not alter movement architecture")
    req("Position =" not in regression, "regression guard must not teleport actors")
    req("green-slime-fallback" in factory, "legacy fallback audit fixture unexpectedly disappeared; review guard assumptions")
    req("return false;" in regression and "legacyFactoryOriginalRuns=0" in regression,
        "guard must suppress legacy factory body while active")
    log("VANILLA/CUSTOM SOURCE-EQUIVALENT REGRESSION GUARD + FAIL-CLOSED UNSUPPORTED SOURCES: PASS")

    for token in ["MutantLootMultiplier = 3", "scope=all-mutants", "monsterDrop", "extraDropPasses"]:
        req(token in reward, f"global x3 Mutant reward missing {token}")
    log("FINAL-PHASE GLOBAL NATIVE LOOT-X3 CARRY-FORWARD: PASS")

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
    log("BUILD SUCCESS - ALPHA 6.7.44.27 MUTATION REGRESSION GUARD")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
