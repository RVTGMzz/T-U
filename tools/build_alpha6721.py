from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6721"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6721.txt"
AUDIT = ROOT / "UNIVERSAL_MONSTER_DENSITY_AUDIT_ALPHA6721.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_21_DENSITY_VI.txt"
VERSION = "0.2.0-alpha.6.7.21"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.21_UNIVERSAL_MONSTER_DENSITY_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.21_UNIVERSAL_MONSTER_DENSITY_TEST.sha256.txt"

lines: list[str] = []


def log(value: str) -> None:
    print(value)
    lines.append(value)


def req(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def text(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def dll_contains(blob: bytes, token: str) -> bool:
    return token.encode() in blob or token.encode("utf-16le") in blob


try:
    project = text("TeamUp.csproj")
    config = text("ModConfig.cs")
    entry = text("ModEntry.cs")
    surge = text("Combat/MonsterSurgeService.cs")
    factory = text("Combat/UniversalMonsterDensitySpawnFactory.cs")
    density_command = text("ModEntry.Alpha6721.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    capture = text("Core/PelipperCaptureSafetyService.cs")
    combat = text("Combat/CombatService.cs")
    rank = text("Core/CombatRankCatalog.cs")
    alpha6714 = text("ModEntry.Alpha6714.cs")
    alpha6716 = text("ModEntry.Alpha6716.cs")
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("public float MonsterDensityMultiplier { get; set; } = 2.5f;" in config, "2.5 density default missing")
    req("public int MonsterSurgeExtraCap { get; set; } = 36;" in config, "36 extra cap default missing")
    req("Math.Clamp(Config.MonsterSurgeExtraCap, 0, 60)" in entry, "density cap clamp 60 missing")
    req("RegisterAlpha6721Events();" in entry, "6.7.21 command registration missing")
    req('"teamup_density"' in density_command, "teamup_density command missing")

    # Universal actor-driven density policy.
    req("LooksLikeCombatZone(location)" not in surge, "legacy map-name combat gate still called")
    req("ClassifyDensitySource" in surge, "density source classifier missing")
    req("return \"ELIGIBLE\";" in surge, "mod-agnostic eligible path missing")
    req("PelipperTownCompatibilityService.IsWildCombatActor(monster)" in surge, "Pelipper source ownership guard missing")
    req("PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster)" in surge, "Pelipper companion/proxy guard missing")
    req("IsBossLike(monster)" in surge and 'return "BOSS";' in surge, "boss exclusion missing")
    req('"scripted", "questprotected", "densityexcluded", "surgeexcluded", "mutationexcluded"' in surge,
        "script/quest/custom exclusion policy missing")
    req("OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster)" in surge,
        "Cardcha harness exclusion missing")
    req("CardchaUniqueId" not in surge, "Cardcha source-wide exclusion returned")

    # >2x target and late-spawn retry.
    req("InitialDiscoveryDelayTicks = 90" in surge, "initial late-spawn delay missing")
    req("LateSpawnRetryTicks = 60" in surge and "MaxDiscoveryAttempts = 5" in surge,
        "bounded late-spawn retry missing")
    req("targetTotal" in surge and "baseline.Count * multiplier" in surge,
        "density target calculation missing")
    req("Math.Clamp(_extraCap(), 0, 60)" in surge, "runtime extra cap 60 missing")
    req("pelipper-only-source-owned-by-pelipper" in surge, "Pelipper-only diagnostic boundary missing")

    # Same-runtime-type custom spawning with no custom slime fallback.
    req("TryCreateSameRuntimeType" in factory, "same-runtime-type density factory missing")
    req("source.GetType().Assembly == typeof(Monster).Assembly" in factory,
        "vanilla-only fallback boundary missing")
    req("custom-unsafe-constructor-skip" in factory, "custom unsafe constructor skip missing")
    req("FormatterServices" not in factory and "MemberwiseClone" not in factory,
        "unsafe reflection clone returned")
    req("UniversalMonsterDensitySpawnFactory.TryCreate" in surge,
        "density service not using safe factory")

    # Carry-forward invariants.
    req("public float MutationChancePercent { get; set; } = 5f;" in config, "mutation 5% regression")
    req("public float MutationHealthMultiplier { get; set; } = 3f;" in config, "mutation HPx3 regression")
    req("MonsterMutationService.IsMutant" in surge and "MonsterMutationService.IsMutationMinion" in surge,
        "density/mutation recursion boundary missing")
    req("RepairCurrentLocationFloors" in capture, "Pelipper capture floor regression")
    req("NpcOnlySourceLockPulseTicksAlpha6714 = 10" in alpha6714, "NPC-only lock regression")
    req('"teamup_diag_all"' in alpha6716, "combined diagnostics regression")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat,
        "boss/add coordination regression")
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "Rank S contrast regression")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("UNIVERSAL DENSITY STATIC AUDIT: PASS (actor-driven, no map-name gate, default x2.5, extra cap 36/60)")
    log("BOSS/SCRIPT POLICY STATIC AUDIT: PASS (boss/script/quest/test/mutation actors excluded)")
    log("CUSTOM/CARDCHA SPAWN STATIC AUDIT: PASS (same-runtime-type safe constructors; unsafe custom types skipped)")
    log("PELIPPER OWNERSHIP STATIC AUDIT: PASS (wild/capture/companion actors detected but never fabricated)")
    log("LATE-SPAWN RETRY STATIC AUDIT: PASS (90-tick initial wait + bounded retries)")
    log("6.7.13-6.7.20 CARRY-FORWARD: PASS")
    log("LIVE VERIFICATION STILL REQUIRED")

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

    dll = SRC / "bin" / "Release" / "net6.0" / "TeamUp.dll"
    req(dll.exists(), "TeamUp.dll missing")
    blob = dll.read_bytes()
    for token in [
        "teamup_density",
        "DensityTelemetry",
        "custom-unsafe-constructor-skip",
        "pelipper-only-source-owned-by-pelipper",
        "Ronvotri.TeamUp/SurgeSpawn",
        "teamup_mutation",
        "teamup_diag_all",
    ]:
        req(dll_contains(blob, token), f"DLL missing token: {token}")
    log("BINARY ACCEPTANCE: PASS")

    RELEASE.mkdir(parents=True, exist_ok=True)
    if STAGE.exists():
        shutil.rmtree(STAGE)
    MOD_STAGE.mkdir(parents=True)
    shutil.copy2(dll, MOD_STAGE / "TeamUp.dll")
    manifest = (SRC / "manifest.json").read_text(encoding="utf-8").replace("%ProjectVersion%", VERSION)
    (MOD_STAGE / "manifest.json").write_text(manifest, encoding="utf-8", newline="\n")
    shutil.copytree(SRC / "i18n", MOD_STAGE / "i18n")

    if ZIP_PATH.exists():
        ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in sorted(MOD_STAGE.rglob("*")):
            if path.is_file():
                archive.write(path, path.relative_to(STAGE))

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Team Up Alpha 6.7.21 Universal Monster Density Audit\n\n"
        "## Runtime intent\n"
        "- Default density multiplier: x2.5 total normal-monster target.\n"
        "- Default extra cap: 36; hard runtime/config clamp: 60.\n"
        "- Combat-zone detection is actor-driven: a real eligible Monster is enough; map names are irrelevant.\n"
        "- Late-spawning mod monsters get a 90-tick initial discovery window plus up to five bounded retry attempts.\n\n"
        "## Exclusions\n"
        "- Boss by runtime/name policy or boss modData: excluded from baseline and never duplicated.\n"
        "- Scripted, quest-protected, explicit density/surge/mutation-excluded actors: excluded.\n"
        "- Cardcha test harness/arena: excluded; normal Cardcha monsters remain eligible.\n"
        "- Existing Surge actors, Mutants and Mutation Minions: excluded from recursive density calculation.\n\n"
        "## Custom/Cardcha spawning\n"
        "- Safe same-runtime-type constructors are used when available: (Vector2) or (Vector2,int level/difficulty).\n"
        "- Unknown third-party constructor shapes are skipped, not reflection-cloned and not replaced by random GreenSlime.\n"
        "- Vanilla-only constructor failures may use the legacy GreenSlime fallback.\n\n"
        "## Pelipper boundary\n"
        "- Pelipper wild/capture/companion/proxy actors are detected for telemetry but remain source-owned.\n"
        "- Team Up does NOT fabricate duplicate catchable Pokemon/proxies in 6.7.21. A Pelipper-only map therefore reports pelipper-only-source-owned-by-pelipper instead of silently doing the wrong thing.\n"
        "- A future Pelipper density adapter should call Pelipper's own spawn authority after its exact runtime spawn surface is proven.\n\n"
        "## Verification boundary\n"
        "CI validates source/build invariants only. Density counts, custom constructors, map collision, Cardcha monsters and Pelipper timing still require live Stardew testing.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.21 - UNIVERSAL MONSTER DENSITY\n"
        "============================================\n\n"
        "Muc tieu: quai thuong (TRU boss/script/quest/test/Pelipper-owned) dat muc tieu tong x2.5.\n"
        "Mac dinh ExtraCap=36, hard cap=60. Khong con dua vao TEN MAP de doan combat zone.\n\n"
        "TEST NHANH:\n"
        "1. Vao map co quai thuong Vanilla/Cardcha/custom. Doi khoang 2-6 giay de late-spawn retry hoan tat.\n"
        "2. Go: teamup_density status\n"
        "3. Go: teamup_density sources\n"
        "4. Neu muon ep chay lai: teamup_density reapply\n"
        "5. Kiem tra Eligible, Wanted, Spawned, BossExcluded, FactoryRejected, UnsafeRejected.\n"
        "6. Vi du 4 quai thuong, multiplier x2.5 => target ~10, tuc muon them 6 neu du tile an toan.\n"
        "7. Boss KHONG duoc tinh vao Eligible va KHONG duoc clone.\n\n"
        "CARDCHA/CUSTOM:\n"
        "8. Quai Cardcha thuong tren map that duoc phep lam density source.\n"
        "9. Factory uu tien spawn cung runtime type. Constructor custom khong an toan => skip, KHONG spawn slime xanh vo ly.\n"
        "10. Cardcha test arena/harness van bi loai.\n\n"
        "PELIPPER:\n"
        "11. Wild/capture Pokemon/proxy KHONG bi Team Up clone. teamup_density sources se hien PELIPPER.\n"
        "12. Neu map chi co Pelipper actors, status co the hien suppression=pelipper-only-source-owned-by-pelipper. Day la fail-safe co chu dich, khong phai x2.5 Pelipper da live hoat dong.\n"
        "13. Kiem tra lai capture 10% bang teamup_capture va teamup_capture_proxy.\n\n"
        "NEU SO LUONG VAN SAI:\n"
        "14. Gui output cua: teamup_density status + teamup_density sources\n"
        "15. Sau khi tai hien loi, thoat game ngay va gui file: %appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n"
        "16. Diagnostic Team Up: E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Diagnostic_bundle_latest.txt\n\n"
        "CI PASS KHONG dong nghia density da live-verified.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.21")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
