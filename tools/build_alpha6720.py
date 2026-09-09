from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6720"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6720.txt"
AUDIT = ROOT / "MUTANT_FOOTPRINT_SAME_TYPE_AUDIT_ALPHA6720.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_20_MUTANT_FOOTPRINT_VI.txt"
VERSION = "0.2.0-alpha.6.7.20"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.20_MUTANT_FOOTPRINT_SAME_TYPE_MINIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.20_MUTANT_FOOTPRINT_SAME_TYPE_MINIONS_TEST.sha256.txt"

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
    mutation = text("Combat/MonsterMutationService.cs")
    footprint = text("Combat/MonsterMutationFootprintPatch.cs")
    factory = text("Combat/MonsterMutationMinionFactory.cs")
    alpha6720 = text("ModEntry.Alpha6720.cs")
    alpha6719 = text("ModEntry.Alpha6719.cs")
    capture = text("Core/PelipperCaptureSafetyService.cs")
    alpha6714 = text("ModEntry.Alpha6714.cs")
    alpha6716 = text("ModEntry.Alpha6716.cs")
    alpha6710 = text("ModEntry.Alpha6710.cs")
    combat = text("Combat/CombatService.cs")
    rank = text("Core/CombatRankCatalog.cs")
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6719Events();" in entry, "6.7.19 mutation registration regression")
    req("RegisterAlpha6720Events();" in entry, "6.7.20 registration missing")
    req('"teamup_mutation"' in alpha6719, "mutation command regression")
    req('"teamup_mutation_detail"' in alpha6720, "6.7.20 detail command missing")

    # Mutation defaults remain exactly the requested first-pass behavior.
    req("public float MutationChancePercent { get; set; } = 5f;" in config, "5% mutation default regression")
    req("public float MutationHealthMultiplier { get; set; } = 3f;" in config, "HP x3 regression")
    req("public float MutationStatMultiplier { get; set; } = 2f;" in config, "stat x2 regression")
    req("public float MutationVisualScaleMultiplier { get; set; } = 3f;" in config, "visual x3 regression")
    req("public int MutationMinionMin { get; set; } = 2;" in config and "public int MutationMinionMax { get; set; } = 4;" in config,
        "2-4 minion default regression")

    # Footprint layer: patch base + runtime Monster overrides, and prevent nested double scaling.
    req('"GetBoundingBox"' in footprint and "typeof(Monster).IsAssignableFrom(type)" in footprint,
        "runtime monster bounding-box scanner missing")
    req("[ThreadStatic]" in footprint and "_boundingBoxDepth" in footprint and "ref bool __state" in footprint,
        "nested bounding-box double-scale guard missing")
    req("MonsterMutationService.MutationScaleMarker" in footprint, "footprint is not bound to effective mutation scale marker")
    req("MaximumFootprintPixels = 768" in footprint, "large custom-monster footprint safety cap missing")
    req("effectiveFootprintScale = visualScaleApplied ? visualScale : 1f" in mutation,
        "visual/footprint agreement guard missing")
    req("scaleApplied={visualScaleApplied}" in mutation, "visual scale telemetry missing")

    # Same-type minions: only safe constructor shapes, no MemberwiseClone / NetField copying.
    req("TryCreateSameRuntimeType" in factory, "same runtime type factory missing")
    req("p.Length == 1 && p[0].ParameterType == typeof(Vector2)" in factory,
        "safe position-only constructor path missing")
    req("LooksLikeLevelParameter" in factory and "p.Length != 2" in factory,
        "guarded Vector2+level constructor path missing")
    req("new GreenSlime(position, mineLevel)" in factory, "safe fallback minion missing")
    req("MemberwiseClone" not in factory and "FormatterServices" not in factory,
        "unsafe object cloning introduced")
    req("MonsterMutationMinionFactory.Create(" in mutation, "mutation wave is not using same-type factory")
    req('spawnMode == "same-runtime-type"' in mutation, "same-type/fallback wave telemetry missing")
    req("SuppressKnownLootCollections(minion)" in mutation, "minion economy guard regression")
    req("MutationMinionMarker" in mutation, "minion recursion marker regression")

    # Compatibility rules remain mod-agnostic: normal Cardcha/custom allowed, protected systems excluded.
    req("OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster)" in mutation,
        "Cardcha test harness exclusion regression")
    req("CardchaUniqueId" not in mutation, "Cardcha was source-wide blacklisted")
    req("PelipperTownCompatibilityService.IsWildCombatActor(monster)" in mutation,
        "Pelipper capture exclusion regression")
    req("PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster)" in mutation,
        "Pelipper source ownership exclusion regression")
    req('HasTruthyPolicyTag(monster, "boss", "scripted", "questprotected", "mutationexcluded")' in mutation,
        "boss/script/quest exclusion regression")
    req("MonsterSurgeService.IsSurgeMonster(monster)" in mutation and "IsMutant(monster)" in mutation and "IsMutationMinion(monster)" in mutation,
        "mutation recursion guards regression")

    # Carry-forward locked Team Up invariants.
    req('"teamup_diag_all"' in alpha6716, "combined diagnostics regression")
    req("NpcOnlySourceLockPulseTicksAlpha6714 = 10" in alpha6714, "NPC-only source lock regression")
    req("RepairCurrentLocationFloors" in capture, "capture floor watchdog regression")
    req("NpcRouteStateSafetyPatch.Apply" in alpha6710, "Gunther route guard regression")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat,
        "boss/add coordination regression")
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "Rank S contrast regression")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("MUTANT FOOTPRINT STATIC AUDIT: PASS (effective visual scale -> bounded combat footprint, nested override guard)")
    log("SAME-TYPE MINION STATIC AUDIT: PASS (safe constructor shapes + GreenSlime fallback, no reflection clone)")
    log("MUTATION POLICY CARRY-FORWARD: PASS (5%, HPx3, stat x2, scale x3, 2-4 minions; boss/script/Pelipper/test exclusions)")
    log("6.7.13-6.7.19 LIVE-GUARD/CONTENT CARRY-FORWARD: PASS")
    log("LOCKED PARTY/PELIPPER/BOSS/RANK INVARIANTS: PASS")
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
        "teamup_mutation",
        "teamup_mutation_detail",
        "Ronvotri.TeamUp/Mutant",
        "Ronvotri.TeamUp/MutationMinion",
        "MonsterMutationFootprintPatch",
        "same-runtime-type",
        "green-slime-fallback",
        "PelipperWildCombatProxy",
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
        "# Team Up Alpha 6.7.20 Mutant Footprint + Same-Type Minions Audit\n\n"
        "## Added in 6.7.20\n"
        "- Mutant GetBoundingBox footprint scales with the visual mutation multiplier that actually applied.\n"
        "- Default requested scale remains x3, with a 768px safety cap for very large custom monsters.\n"
        "- Thread-local nesting guard prevents an override + base GetBoundingBox chain from scaling twice.\n"
        "- Mutation minions now first attempt the same runtime Monster type using only safe constructor shapes.\n"
        "- Accepted constructor shapes: (Vector2) or (Vector2, int) where the int metadata is level/mine/difficulty/depth.\n"
        "- Unsupported custom constructors fail closed to the GreenSlime fallback. No MemberwiseClone/NetField cloning.\n"
        "- Same-type and fallback spawn counts are exposed by teamup_mutation status and teamup_mutation_detail.\n\n"
        "## Preserved policy\n"
        "- Normal vanilla and normal Cardcha/custom monsters remain eligible.\n"
        "- Boss/script/quest-protected, Pelipper wild/capture/companion, Cardcha test harness, Surge spawns, mutants and mutation minions remain excluded.\n\n"
        "## Verification boundary\n"
        "CI verifies compilation and static invariants only. Actual collision feel, narrow-map movement, same-type constructor behavior, and death interception still require live Stardew testing.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.20 - MUTANT FOOTPRINT + SAME-TYPE MINIONS\n"
        "===================================================\n\n"
        "1. Cai ban 6.7.20 vao E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n"
        "2. Vao khu co quai thuong va go: teamup_mutation force\n"
        "3. Mutant phai lon ~x3 neu runtime monster ho tro Scale, aura bao quanh footprint moi.\n"
        "4. Thu danh vao phan than da phong to: hitbox phai gan voi kich thuoc hien thi, khong con than XXL/hitbox nho.\n"
        "5. Quan sat 2-4 de: neu runtime type co constructor an toan, de nen cung loai voi mutant; neu khong se fallback GreenSlime.\n"
        "6. Go: teamup_mutation status\n"
        "7. Go: teamup_mutation_detail\n"
        "   Kiem tra footprintHooks>0, sameTypeMinions/fallbackMinions va sameTypeFailures.\n"
        "8. Thu o hanh lang hep/cave: mutant khong duoc crash game. Neu bi ket dia hinh, ghi lai map + loai quai.\n"
        "9. De MutationChancePercent=100 tam thoi de test death interception, sau do tra ve 5.\n"
        "10. Cardcha quai thuong: duoc mutate. Boss/script/test actor: khong.\n"
        "11. Pelipper wild/capture Pokemon: khong mutate. Test lai teamup_capture + teamup_capture_proxy.\n"
        "12. Neu loi, thoat game ngay va gui: %appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n"
        "13. Team Up diagnostic: E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Diagnostic_bundle_latest.txt\n\n"
        "CI PASS KHONG dong nghia gameplay da live-verified.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.20")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
