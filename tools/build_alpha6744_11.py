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
STAGE = ROOT / "_stage_alpha6744_23"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_23.txt"
VERSION = "0.2.0-alpha.6.7.44.23"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.23_MUTANT_LEADER_SMOOTHING_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.23_MUTANT_LEADER_SMOOTHING_TEST.sha256.txt"
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
    aggro = text("Core/Alpha674420MutationAggroService.cs")
    steering = text("Core/Alpha674423PelipperMutantLeaderSmoothingService.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    wiring = text("ModEntry.Alpha67446.cs")
    mutation_cmd = text("ModEntry.Alpha6719.cs")

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
    log("PELIPPER INTERNAL SPAWN-COMMAND GATE + RESTORE: PASS")

    req("monster.focusedOnFarmers = true" in aggro and "monster.moveTowardPlayer" in aggro,
        "6.7.44.20 generic aggro carry-forward missing")

    for token in [
        "Alpha674423PelipperMutantLeaderSmoothingService",
        "LeaderHoldCenterDistance = 92f",
        "LeaderAttackCenterDistance = 112f",
        "LeaderAxisSwitchBias = 24f",
        "LeaderDirectionLockTicks = 8",
        "source.Halt()",
        "if (!pair.Leader)",
        "ApplyFollowerSeparation",
        "LeaderClearanceRadius = 112f",
        "source.MovePosition(Game1.currentGameTime, Game1.viewport, location)",
        "proxy.collidesWithOtherCharacters.Value = false",
        "farmerDistance <= LeaderAttackCenterDistance",
        "farmer.takeDamage(damage, overrideParry: false, proxy)",
        "leaderReachHits=",
        "leaderRangeHolds=",
        "leaderHaltResets=",
        "leaderDirectionChanges=",
        "leaderDirectionLocks=",
        "minionMoves=",
        "leaderClearance=",
        "sidesteps=",
    ]:
        req(token in steering, f"6.7.44.23 leader smoothing missing {token}")
    req("new PathFindController" not in steering,
        "6.7.44.23 must not restore tile PathFindController")
    req("new GreenSlime" not in steering,
        "leader smoothing must not create fallback monsters")
    req("PelipperMutationSteeringAlpha674422 = new" not in wiring,
        "6.7.44.22 steering runtime must be disabled in 6.7.44.23")
    req("PelipperMutationLeaderSmoothingAlpha674423 = new Alpha674423PelipperMutantLeaderSmoothingService" in wiring,
        "6.7.44.23 smoothing service not wired")
    req("PelipperMutationLeaderSmoothingAlpha674423.Describe()" in mutation_cmd,
        "6.7.44.23 telemetry missing from teamup_mutation status")
    log("PELIPPER MUTANT LEADER SMOOTHING + EXTENDED REACH AUDIT: PASS")

    for token in ["MutantLootMultiplier = 3", "scope=all-mutants", "monsterDrop", "extraDropPasses"]:
        req(token in reward, f"global x3 Mutant reward missing {token}")
    log("GLOBAL LEADER-ONLY MUTANT LOOT-X3: PASS")

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
    log("BUILD SUCCESS - ALPHA 6.7.44.23 MUTANT LEADER SMOOTHING")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
