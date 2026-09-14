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
STAGE = ROOT / "_stage_alpha6744_17"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_17.txt"
VERSION = "0.2.0-alpha.6.7.44.17"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.17_MUTANT_CAPTURE_LOCK_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.17_MUTANT_CAPTURE_LOCK_TEST.sha256.txt"
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
    pairing = text("Core/Alpha674410PelipperSpeciesPairingService.cs")
    visible = text("Core/Alpha674413PelipperVisibleMutationService.cs")
    minion_spawn = text("Core/Alpha674413MutationMinionSpawnService.cs")
    reward = text("Core/Alpha674414PelipperMutantRewardService.cs")
    leader_minion = text("Core/Alpha674416MutationLeaderMinionPolicyService.cs")
    capture_lock = text("Core/Alpha674417MutationCaptureLockService.cs")
    factory = text("Combat/MonsterMutationMinionFactory.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    wiring = text("ModEntry.Alpha67446.cs")
    mutation_cmd = text("ModEntry.Alpha6719.cs")

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")

    for token in [
        "Griff.PelipperTown/WildCurrentHealth",
        "Griff.PelipperTown/WildMaxHealth",
        "ResolvePrefix",
        "WritePrefix",
        "SourceProxyMap",
    ]:
        req(token in binding, f"HP binding missing {token}")
    req("ConditionalWeakTable<Monster, CachedPair>" in pairing and "cacheHits=" in pairing,
        "pair cache carry-forward missing")
    req("[PelipperSpeciesPairing]" not in pairing, "pairing log spam regressed")
    log("PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD: PASS")

    for token in [
        "PelipperVisibleScaleCap = 2f",
        "Math.Min(configured, PelipperVisibleScaleCap)",
        "TryMutatePostfix",
        "RestoreAll",
        "PelipperWildEncounterIdentityService.TryResolve",
        "MonsterMutationService.IsMutant",
    ]:
        req(token in visible, f"x2 visible mutation scaling missing {token}")
    req("MutationVisualScaleMultiplier { get; set; } = 2f" in config,
        "new-config mutation visual scale is not x2")
    log("PELIPPER VISIBLE MUTATION X2 CAP + RESTORE AUDIT: PASS")

    for token in [
        "TryFindSafeSpawnPosition",
        "SpawnPostfix",
        "BuildSpawnOffsets",
        "radius <= 8",
        "pelipperSourceAnchors",
        "PelipperWildEncounterIdentityService.TryResolve",
        "minions=2-4",
    ]:
        req(token in minion_spawn, f"2-4 minion spawn fix missing {token}")
    req("MutationMinionMin { get; set; } = 2" in config and "MutationMinionMax { get; set; } = 4" in config,
        "2-4 minion config changed")
    log("MUTATION 2-4 MINION WIDE-SPAWN CARRY-FORWARD: PASS")

    for token in [
        "MutantLeaderMarker",
        "NormalHostileMinionMarker",
        "leader=Mutant",
        "minions=normal-hostile",
        "leaderLoot=x3",
        "minionLootBonus=none",
        "MutationExcludedMarker",
        "LootMultiplierMarker",
        "CombatTargetOptInKey",
    ]:
        req(token in leader_minion, f"leader/minion policy missing {token}")
    req("minion.modData.Remove(MonsterMutationService.MutantMarker)" in leader_minion,
        "minions are not forcibly kept non-Mutant")
    req("TryCreateSameRuntimeType" in factory and "same-runtime-type" in factory,
        "same-type normal minion priority missing")
    req("IsMutationMinion(monster)" in mutation,
        "Mutation eligibility does not exclude minions")
    log("MUTANT LEADER + NORMAL HOSTILE MINION POLICY CARRY-FORWARD: PASS")

    for token in [
        "MutantLootMultiplier = 3",
        "Ronvotri.TeamUp/MutantLootMultiplier",
        "scope=all-mutants",
        "monsterDrop",
        "MonsterDropPostfix",
        "__originalMethod.Invoke(__instance, __args)",
        "extraDropPasses",
    ]:
        req(token in reward, f"global x3 Mutant reward missing {token}")
    req("SpawnMinionWavePrefix" not in reward, "reward service must not suppress minions")
    log("GLOBAL LEADER-ONLY MUTANT LOOT-X3 CARRY-FORWARD: PASS")

    for token in [
        "MutationCaptureBlocked",
        "policy=leader+minions-blocked/natural-wild-allowed",
        "TryMutatePostfix",
        "SpawnWavePostfix",
        "PelipperWildEncounterIdentityService.TryResolve",
        "PatchPelipperCaptureMethods",
        "CapturePrefix",
        "capture",
        "catch",
        "pokeball",
        "__result = false",
        "Mutant Pokemon and their summoned minions can't be captured",
    ]:
        req(token in capture_lock, f"capture lock missing {token}")
    req("MutationCaptureLockAlpha674417 = new Alpha674417MutationCaptureLockService" in wiring,
        "capture lock not wired")
    req("MutationCaptureLockAlpha674417.Describe()" in mutation_cmd,
        "capture lock missing from mutation status")
    req("MutationCaptureLockAlpha674417.ResetTelemetry()" in wiring,
        "capture lock telemetry not reset on save load")
    log("MUTANT LEADER + MINION NON-CATCHABLE POLICY AUDIT: PASS")

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
    log("BUILD SUCCESS - ALPHA 6.7.44.17 MUTANT CAPTURE LOCK")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
