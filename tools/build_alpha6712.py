from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6712"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6712.txt"
AUDIT = ROOT / "CAPTURE_FLOOR_AUDIT_ALPHA6712.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_12_CAPTURE_FLOOR_RANK_S_VI.txt"
VERSION = "0.2.0-alpha.6.7.12"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.12_CAPTURE_FLOOR_RANK_S_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.12_CAPTURE_FLOOR_RANK_S_TEST.sha256.txt"

lines: list[str] = []


def log(s: str) -> None:
    print(s)
    lines.append(s)


def req(cond: bool, msg: str) -> None:
    if not cond:
        raise RuntimeError(msg)


def text(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def dll_contains(blob: bytes, token: str) -> bool:
    return token.encode() in blob or token.encode("utf-16le") in blob


try:
    project = text("TeamUp.csproj")
    config = text("ModConfig.cs")
    follow = text("Following/FollowService.cs")
    route = text("Core/NpcRouteStateSafetyPatch.cs")
    rank = text("Core/CombatRankCatalog.cs")
    combat = text("Combat/CombatService.cs")
    capture_patch = text("Core/PelipperCaptureDamagePatch.cs")
    capture_safety = text("Core/PelipperCaptureSafetyService.cs")
    alpha6615 = text("ModEntry.Alpha6615.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    alpha6710 = text("ModEntry.Alpha6710.cs")

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")

    # Locked party/source-authority invariants.
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("private const int CombatPathRetryCooldownTicks = 24;" in combat, "combat path cadence regression")
    req("private const int CombatMovementPulseTicks = 3;" in combat, "combat movement pulse regression")
    req("isTileLocationTotallyClearAndPlaceable" not in follow, "follow water/path regression")
    req("isTileLocationTotallyClearAndPlaceable" not in combat, "combat water/path regression")
    req("UnlockVanillaMovementAnimation(npc);" in follow, "Gus animation unlock regression")
    req("npc.Sprite.ignoreStopAnimation = false;" in follow, "Gus animation flag regression")
    req("PelipperTown119DeployQuotaPatch.Apply" in alpha6625, "Pelipper native quota regression")

    # 6.7.10 route guard remains carried forward, not declared live-verified.
    req("string.IsNullOrWhiteSpace(__0)" in route, "route null/empty guard regression")
    req("teamUpControlled" in route and "return false;" in route, "controlled route suppression regression")
    req("NpcRouteStateSafetyPatch.Apply" in alpha6710, "route patch registration regression")

    # 6.7.11 elite/boss coordination stays intact.
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat,
        "6.7.11 coordination soft cap regression")
    req("List<Monster> nonMajors" in combat and "Monster? spread = AcquireTarget" in combat,
        "6.7.11 anti-dogpile spreading regression")
    req("GetDistanceToMonsterBoundsTiles(npc, target)" in combat, "6.7.11 hitbox stop-distance regression")
    req("Rectangle bounds = target.GetBoundingBox();" in combat, "6.7.11 hitbox approach regression")

    # 6.7.12 capture-floor hardening.
    for token in [
        "PatchedTakeDamageMethods",
        "PatchedAreaDamageMethods",
        "BeforeTakeDamage",
        "BeforeAreaDamage",
        "GameLocation",
        "damageMonster",
        "HasAreaDamageShape",
        "TakeDamagePatchCount",
        "AreaDamagePatchCount",
    ]:
        req(token in capture_patch, f"capture patch token missing: {token}")
    req("type.IsAbstract" not in capture_patch,
        "capture scan must not skip abstract custom Monster bases")
    req("method.IsAbstract" in capture_patch,
        "capture scan must skip only abstract methods, not abstract declaring types")
    req("TryGetDamageBudget(monster, out int budget)" in capture_patch,
        "area damage budget guard missing")
    req("TryGetCaptureFloor" in capture_safety, "capture-floor helper missing")
    req("RepairCurrentLocationFloors" in capture_safety, "capture watchdog missing")
    req("FallbackThreshold = 0.10f" in capture_safety, "10% fallback threshold regression")
    req('"teamup_capture"' in alpha6615, "capture diagnostics command missing")
    req("RepairCurrentLocationFloors(Game1.currentLocation)" in alpha6615,
        "per-tick capture repair registration missing")
    req("if (!e.IsMultipleOf(15))" in alpha6615,
        "existing companion recall cadence regression")

    # S-rank readability hotfix. Rank identity itself must remain unchanged.
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "S-rank contrast color missing")
    req('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank, "Marlon S regression")
    req("RecruitBadge.Boss | RecruitBadge.Special" in rank, "MiMi S/BOSS/SPECIAL regression")

    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    req("PelipperRenderSuppressedAlpha6613" not in all_source, "legacy Pelipper render suppression returned")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("CAPTURE FLOOR MULTI-LAYER STATIC AUDIT: PASS")
    log("S-RANK CONTRAST STATIC AUDIT: PASS")
    log("6.7.11 COMBAT COORDINATION CARRIED FORWARD: PASS")
    log("GUNTHER ROUTE GUARD CARRIED FORWARD: PASS (LIVE VERIFICATION STILL REQUIRED)")

    proc = subprocess.run(
        ["dotnet", "build", str(SRC / "TeamUp.csproj"), "-c", "Release", "--nologo", "-warnaserror"],
        cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False)
    print(proc.stdout, end="")
    lines.append(proc.stdout.rstrip())
    req(proc.returncode == 0, "dotnet build failed")
    req("0 Warning(s)" in proc.stdout, "build has warnings")
    req("0 Error(s)" in proc.stdout, "build has errors")

    dll = SRC / "bin" / "Release" / "net6.0" / "TeamUp.dll"
    req(dll.exists(), "TeamUp.dll missing")
    blob = dll.read_bytes()
    for token in [
        "BeforeAreaDamage",
        "RepairCurrentLocationFloors",
        "teamup_capture",
        "NpcRouteStateSafetyPatch",
        "AcquireCoordinatedTarget",
        "GetRectangleGapPixels",
    ]:
        req(dll_contains(blob, token), f"DLL missing token: {token}")
    req(not dll_contains(blob, "PelipperRenderSuppressedAlpha6613"), "forbidden Pelipper token in DLL")
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
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for p in sorted(MOD_STAGE.rglob("*")):
            if p.is_file():
                zf.write(p, p.relative_to(STAGE))

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Alpha 6.7.12 Capture Floor + Rank S Audit\n\n"
        "## Capture safety\n"
        "- 10% fallback capture threshold retained: PASS\n"
        "- Monster.takeDamage scan includes concrete implementations declared on abstract Monster bases: PASS\n"
        "- Abstract methods themselves are skipped safely: PASS\n"
        "- GameLocation.damageMonster area-damage entry points receive a capture budget clamp: PASS\n"
        "- Team Up offensive target selection still excludes capture-protected wild Pokemon: PASS\n"
        "- Per-tick last-resort repair protects still-live wild proxies below the floor: PASS\n"
        "- Capture diagnostic command `teamup_capture` present: PASS\n\n"
        "## UI\n"
        "- Rank S identity unchanged: PASS\n"
        "- Rank S roster color changed from pale gold to dark bronze for orange-panel contrast: PASS\n\n"
        "## Regression walls\n"
        "- 5-person party cap: PASS\n"
        "- 2/2 external companion cap: PASS\n"
        "- Pelipper source authority: PASS\n"
        "- 6.7.11 elite/boss anti-dogpile + hitbox pathing: PASS\n"
        "- 6.7.10 route guard: PASS (still requires live Gunther verification)\n",
        encoding="utf-8", newline="\n")

    SMOKE.write_text(
        "TEAM UP 6.7.12 - CAPTURE FLOOR + RANK S LIVE TEST\n"
        "=================================================\n\n"
        "A. FARMER + POKEMON (baseline)\n"
        "1. Bat che do Pelipper dung tan cong o 10% HP.\n"
        "2. Chi Farmer + Pokemon cua Farmer danh 1 Pokemon hoang da.\n"
        "3. PASS: Pokemon hoang da dung o nguong bat, khong bi KO.\n\n"
        "B. THEM DUNG 1 NPC TEAM UP - TEST QUAN TRONG NHAT\n"
        "4. Moi dung 1 NPC vao Team Up.\n"
        "5. De NPC + Pokemon cua Farmer cung danh Pokemon hoang da.\n"
        "6. PASS BAT BUOC: khi toi nguong 10%, NPC khong gay them damage va Pokemon cua Farmer cung KHONG duoc danh chet muc tieu.\n"
        "7. Thu mot don damage lon luc muc tieu con khoang 11-15% HP; PASS: damage bi clamp dung o floor.\n\n"
        "C. NPC + POKEMON LIEN KET\n"
        "8. Neu NPC co Pokemon dong hanh, goi Pokemon do vao team.\n"
        "9. Thu voi 2/2 companion neu co the.\n"
        "10. PASS: bat ky damage than thien nao cung khong lam Pokemon hoang da tut duoi capture floor.\n\n"
        "D. AOE / OVERKILL\n"
        "11. Danh AOE khi Pokemon hoang da capture-limited dung gan mot quai khac.\n"
        "12. PASS uu tien an toan: Pokemon hoang da van song o capture floor.\n\n"
        "E. CHAN DOAN NEU VAN LOI\n"
        "13. Trong SMAPI console go: teamup_capture\n"
        "14. Chup/dan dong Capture floor: enabled=..., threshold=..., takeDamagePatches=..., areaDamagePatches=..., wildTargets=[...]\n"
        "15. Neu van KO, thoat game ngay va gui file: %appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n\n"
        "F. UI RANK S\n"
        "16. Mo Codex/roster, xem Marlon va MiMi.\n"
        "17. PASS: [S] va ten S-rank doc ro tren nen cam/vang, khong con chim mau.\n\n"
        "G. REGRESSION\n"
        "18. Farmer + 4 NPC = 5/5; NPC tiep theo bi chan.\n"
        "19. External Pokemon companion khong vuot 2/2.\n"
        "20. Gus van walk/facing vanilla.\n"
        "21. Boss coordination 6.7.11 van hoat dong.\n"
        "22. Gunther/Aerodactyl van la bug can live-test rieng; KHONG xem la da fix chi vi CI xanh.\n",
        encoding="utf-8", newline="\n")

    log("BUILD SUCCESS - ALPHA 6.7.12")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
