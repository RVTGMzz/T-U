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
STAGE = ROOT / "_stage_alpha6744_14"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_14.txt"
VERSION = "0.2.0-alpha.6.7.44.14"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.14_PELIPPER_X2_LOOT_X3_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.14_PELIPPER_X2_LOOT_X3_TEST.sha256.txt"
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
        "cap=x",
    ]:
        req(token in visible, f"x2 visible mutation scaling missing {token}")
    req("MutationVisualScaleMultiplier { get; set; } = 2f" in config,
        "new-config mutation visual scale is not x2")
    req("PelipperVisibleMutationAlpha674413 = new Alpha674413PelipperVisibleMutationService" in wiring,
        "visible Pelipper Mutation service not wired")
    req("PelipperVisibleMutationAlpha674413.Update()" in wiring,
        "visible scale upkeep not wired")
    req("PelipperVisibleMutationAlpha674413.Describe()" in mutation_cmd,
        "mutation status missing visible scale telemetry")
    log("PELIPPER VISIBLE MUTATION X2 CAP + RESTORE AUDIT: PASS")

    # Keep the 6.7.44.13 fallback for generic/non-Pelipper minions. 6.7.44.14 suppresses only
    # Pelipper mutant waves and replaces them with x3 loot.
    for token in [
        "TryFindSafeSpawnPosition",
        "SpawnPostfix",
        "fallbackResolved",
    ]:
        req(token in minion_spawn, f"generic minion fallback carry-forward missing {token}")
    log("GENERIC MUTATION MINION FALLBACK CARRY-FORWARD: PASS")

    for token in [
        "PelipperLootMultiplier = 3",
        "LootMultiplierMarker",
        "TryMutatePostfix",
        "SpawnMinionWavePrefix",
        "monsterDrop",
        "MonsterDropPostfix",
        "__originalMethod.Invoke(__instance, __args)",
        "minionWavesSuppressed",
        "extraDropPasses",
    ]:
        req(token in reward, f"Pelipper x3 loot reward missing {token}")
    req("PelipperMutantRewardAlpha674414 = new Alpha674414PelipperMutantRewardService" in wiring,
        "Pelipper reward service not wired")
    req("PelipperMutantRewardAlpha674414.Describe()" in mutation_cmd,
        "mutation status missing Pelipper reward telemetry")
    req("PelipperMutantRewardAlpha674414.ResetTelemetry()" in wiring,
        "Pelipper reward telemetry not reset on save load")
    log("PELIPPER MINIONS-OFF + LOOT-X3 REWARD AUDIT: PASS")

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
    log("BUILD SUCCESS - ALPHA 6.7.44.14 PELIPPER X2 + LOOT X3")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
