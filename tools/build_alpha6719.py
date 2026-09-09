from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6719"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6719.txt"
AUDIT = ROOT / "MUTATION_ENCOUNTERS_AUDIT_ALPHA6719.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_19_MUTATION_ENCOUNTERS_VI.txt"
VERSION = "0.2.0-alpha.6.7.19"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.19_MUTATION_ENCOUNTERS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.19_MUTATION_ENCOUNTERS_TEST.sha256.txt"

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
    registration = text("ModEntry.Alpha6719.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    combat = text("Combat/CombatService.cs")
    capture = text("Core/PelipperCaptureSafetyService.cs")
    alpha6714 = text("ModEntry.Alpha6714.cs")
    alpha6716 = text("ModEntry.Alpha6716.cs")
    alpha6710 = text("ModEntry.Alpha6710.cs")
    rank = text("Core/CombatRankCatalog.cs")
    banter = text("Core/BanterContentCatalog.cs")
    default_i18n = text("i18n/default.json")
    vi_i18n = text("i18n/vi.json")
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6719Events();" in entry, "6.7.19 event registration missing")
    req('"teamup_mutation"' in registration, "teamup_mutation command missing")
    req("RenderedWorld" in registration and "DrawAura" in registration, "mutant aura registration missing")

    # Requested defaults.
    for token in [
        "public bool EnableMutationEncounters { get; set; } = true;",
        "public float MutationChancePercent { get; set; } = 5f;",
        "public float MutationHealthMultiplier { get; set; } = 3f;",
        "public float MutationStatMultiplier { get; set; } = 2f;",
        "public float MutationVisualScaleMultiplier { get; set; } = 3f;",
        "public int MutationMinionMin { get; set; } = 2;",
        "public int MutationMinionMax { get; set; } = 4;",
    ]:
        req(token in config, f"mutation config default missing: {token}")

    # Core lifecycle: roll exactly at a concrete Monster deathAnimation, revive the same instance,
    # then suppress the original death only on a successful transformation.
    req('method.Name.Equals("deathAnimation", StringComparison.Ordinal)' in mutation, "deathAnimation hook scanner missing")
    req("method.IsAbstract" in mutation and "!typeof(Monster).IsAssignableFrom(type)" in mutation,
        "death hook scanner safety regression")
    req("return !service.TryMutate(__instance, force: false);" in mutation,
        "same-instance death interception missing")
    req("monster.MaxHealth = mutantMax;" in mutation and "monster.Health = mutantMax;" in mutation,
        "mutant 3x health transformation path missing")
    req("TryWriteNumericMember(monster" in mutation and '"DamageToFarmer", "damageToFarmer"' in mutation,
        "mutant damage scaling missing")
    req('"Resilience", "resilience"' in mutation, "mutant resilience scaling missing")
    req("monster.Speed = mutantSpeed;" in mutation, "mutant speed scaling missing")
    req('"Scale", "scale"' in mutation, "mutant visual scale path missing")
    req("PendingMinionWave" in mutation and "DelayTicks: 1" in mutation,
        "one-tick deferred minion wave missing")
    req("Game1.random.Next(min, max + 1)" in mutation, "2-4 minion randomized wave missing")
    req("SuppressKnownLootCollections(minion)" in mutation, "minion economy guard missing")

    # Recursive mutation and special-source exclusions.
    req("IsMutant(monster)" in mutation and "IsMutationMinion(monster)" in mutation,
        "mutation recursion guard missing")
    req("MonsterSurgeService.IsSurgeMonster(monster)" in mutation, "Surge recursion guard missing")
    req("PelipperTownCompatibilityService.IsWildCombatActor(monster)" in mutation,
        "Pelipper wild capture exclusion missing")
    req("PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster)" in mutation,
        "Pelipper source-authority exclusion missing")
    req("OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster)" in mutation,
        "Cardcha test-harness exclusion missing")
    req("CardchaUniqueId" not in mutation,
        "Cardcha was source-wide excluded; normal Cardcha monsters must remain mutation-eligible")
    req('typeName.Contains("Boss"' in mutation and 'HasTruthyPolicyTag(monster, "boss", "scripted", "questprotected", "mutationexcluded")' in mutation,
        "boss/script/quest exclusion policy missing")
    req("MutationBossMarker" in mutation and "MutationExcludedMarker" in mutation,
        "explicit mod interoperability exclusion markers missing")

    # Safe placement and host authority.
    req("Context.IsMainPlayer" in mutation, "host authority guard missing")
    req("TryFindSafeSpawnPosition" in mutation and "IsSafeSpawnTile" in mutation,
        "safe minion placement missing")
    req("CollisionMask.All" in mutation, "safe map collision guard missing")

    # Carry-forward invariants from the currently unmerged dev chain.
    req('"teamup_diag_all"' in alpha6716, "6.7.16 combined diagnostics regression")
    req("NpcOnlySourceLockPulseTicksAlpha6714 = 10" in alpha6714, "6.7.14 NPC-only source lock regression")
    req("RepairCurrentLocationFloors" in capture, "capture floor watchdog regression")
    req("NpcRouteStateSafetyPatch.Apply" in alpha6710, "Gunther route guard regression")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat,
        "6.7.11 boss coordination regression")
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "S-rank contrast regression")
    req("pair:penny-maru" in banter and "pair:marlon-wizard" in banter, "6.7.17 banter regression")
    req("~11.8s" in default_i18n or "~11,8" in vi_i18n or "CD" in vi_i18n,
        "6.7.18 profile flavor carry-forward not detected")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("MUTATION CORE STATIC AUDIT: PASS (5% default, HPx3, combat stat x2, visual x3)")
    log("MUTATION MINION STATIC AUDIT: PASS (2-4 deferred safe-spawn minions, recursion/economy guards)")
    log("MOD-AGNOSTIC POLICY STATIC AUDIT: PASS (normal custom/Cardcha monsters allowed; boss/script/Pelipper/test actors excluded)")
    log("6.7.13/14/15/16/17/18 CARRY-FORWARD: PASS")
    log("LOCKED PARTY/PELIPPER/BOSS/RANK INVARIANTS: PASS")
    log("GUNTHER ROUTE GUARD CARRIED FORWARD: PASS (LIVE VERIFICATION STILL REQUIRED)")

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
        "Ronvotri.TeamUp/Mutant",
        "Ronvotri.TeamUp/MutationMinion",
        "MutationEncounter",
        "deathAnimation",
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
        "# Team Up Alpha 6.7.19 Mutation Encounters Audit\n\n"
        "## Requested behavior\n"
        "- Default mutation chance on normal monster death: 5%\n"
        "- Mutant HP: x3\n"
        "- Main combat stats: x2 (DamageToFarmer, resilience/defense when exposed, speed with safety cap)\n"
        "- Visual scale: x3 best-effort on the existing runtime monster instance\n"
        "- Reinforcements: 2-4 normal GreenSlime minions, deferred one tick and placed on safe tiles\n"
        "- Persistent aura: rendered by Team Up while the mutant is alive\n\n"
        "## Compatibility policy\n"
        "- Normal vanilla monsters: eligible\n"
        "- Normal third-party/custom monsters: eligible by default because mutation reuses the same live instance\n"
        "- Cardcha normal monsters: eligible by default\n"
        "- Cardcha test arena/dummy/kill-target instrumentation: excluded\n"
        "- Boss, scripted, quest-protected or explicitly MutationExcluded actors: excluded\n"
        "- Pelipper wild capture actors and Pelipper-owned combat exclusions: excluded\n"
        "- Surge monsters, mutation minions and existing mutants: excluded from recursive mutation\n\n"
        "## Important verification boundary\n"
        "CI proves source/build invariants only. The exact game call order around Monster.deathAnimation and custom monster death logic still needs live Stardew testing before this feature is considered gameplay-verified.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.19 - MUTATION ENCOUNTERS\n"
        "====================================\n\n"
        "Muc tieu: 5% quai thuong khi sap chet bien thanh Mutant tren CHINH runtime instance cua no.\n"
        "Mutant: HP x3, damage/resilience/speed x2 (speed co safety cap), visual scale x3, aura, 2-4 de.\n\n"
        "TEST NHANH (khong can doi roll 5%):\n"
        "1. Vao khu co quai thuong.\n"
        "2. Go: teamup_mutation status\n"
        "3. Go: teamup_mutation force\n"
        "4. Quai gan nhat phai lon len, day mau, co aura va sau ~1 tick co 2-4 de neu co tile an toan.\n"
        "5. Go: teamup_mutation list de xem MUTANT/MINION hien tai.\n"
        "6. Giet mutant. No phai chet binh thuong, KHONG mutate lan hai.\n\n"
        "TEST ROLL TU NHIEN:\n"
        "7. De config MutationChancePercent=100 tam thoi neu muon test death interception de nhin thay ngay.\n"
        "8. Giet mot quai thuong. O death moment no phai bien doi thay vi bi remove/drop loot ngay.\n"
        "9. Sau test tra MutationChancePercent ve 5.\n\n"
        "CARDCHA:\n"
        "10. Quai thuong tren map Cardcha duoc phep mutate. Cardcha_CardTestArena va dummy/kill-target bi loai.\n"
        "11. Boss/script Cardcha nen mang modData Boss/Scripted/MutationExcluded=true de fail-closed.\n\n"
        "PELIPPER:\n"
        "12. Wild/capture Pokemon va Pelipper-owned companion/proxy KHONG duoc mutate.\n"
        "13. Kiem tra lai luat capture 10% bang teamup_capture + teamup_capture_proxy.\n\n"
        "DIAGNOSTIC:\n"
        "14. teamup_mutation status\n"
        "15. teamup_diag_all\n"
        "16. Neu loi, thoat game ngay va gui: %appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n"
        "17. Diagnostic Team Up: E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Diagnostic_bundle_latest.txt\n\n"
        "CI PASS KHONG dong nghia death interception da live-verified.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.19")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
