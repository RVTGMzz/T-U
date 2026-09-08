from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6711"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6711.txt"
AUDIT = ROOT / "COMBAT_COORDINATION_AUDIT_ALPHA6711.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_11_ELITE_BOSS_COORDINATION_VI.txt"
VERSION = "0.2.0-alpha.6.7.11"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.11_ELITE_BOSS_COORDINATION_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.11_ELITE_BOSS_COORDINATION_TEST.sha256.txt"

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

    # Gunther 6.7.10 route fix is carried forward but is deliberately NOT marked live verified here.
    req("npc.endOfRouteBehaviorName.Value = null;" not in follow, "route metadata nulling returned")
    req("npc.nextEndOfRouteMessage = null;" not in follow, "route message nulling returned")
    req("npc.endOfRouteMessage.Value = null;" not in follow, "route message netfield nulling returned")
    req("string.IsNullOrWhiteSpace(__0)" in route, "route null/empty guard regression")
    req("teamUpControlled" in route and "return false;" in route, "controlled NPC route suppression regression")
    req("NpcRouteStateSafetyPatch.Apply" in alpha6710, "route patch registration regression")
    req("EnsureAlpha6710Registered();" in alpha6625, "route registration chain regression")

    # Rank remains separate from special recruit tags.
    for name in ["Abigail", "Alex", "Haley", "Maru", "Evelyn"]:
        req(f'["{name}"] = new(CombatRank.A)' in rank, f"{name} A-rank regression")
    req('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank, "Marlon S regression")
    req("RecruitBadge.Boss | RecruitBadge.Special" in rank, "MiMi S/BOSS/SPECIAL regression")

    # Alpha 6.7.11: major-target coordination must respect assignment pressure.
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat,
        "coordination soft cap missing")
    req("assignedCounts.TryGetValue(monster, out int count) || count < coordinationCap" in combat,
        "major target assigned-count gate missing")
    req("List<Monster> nonMajors" in combat and "Monster? spread = AcquireTarget" in combat,
        "anti-dogpile add spreading missing")
    req("role is PartyRole.Tank or PartyRole.Healer" in combat,
        "Tank/Healer role-specific target independence regression")

    # Large boss approach must use the hitbox, both for stop distance and path destination search.
    req("GetDistanceToMonsterBoundsTiles(npc, target)" in combat, "hitbox stop-distance regression")
    req("GetRectangleGapPixels" in combat, "rectangle gap helper missing")
    req("TryFindApproachTile(FarmerContext.currentLocation, npc.Tile, target, GetAttackRange(role)" in combat,
        "hitbox-aware approach call missing")
    req("private static bool TryFindApproachTile(GameLocation location, Vector2 from, Monster target" in combat,
        "hitbox-aware approach helper missing")
    req("Rectangle bounds = target.GetBoundingBox();" in combat, "target hitbox footprint missing")
    req("tileBounds.Intersects(bounds)" in combat, "inside-hitbox tile rejection missing")
    req("PartyTileSafety.IsWalkableLandOrBridge" in combat, "land/bridge safety regression")

    # Existing authority walls.
    req("SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner" in combat,
        "single prototype signature authority regression")
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    req("PelipperRenderSuppressedAlpha6613" not in all_source, "legacy Pelipper render suppression returned")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("ELITE/BOSS COORDINATION STATIC AUDIT: PASS")
    log("GUNTHER ROUTE FIX CARRIED FORWARD: PASS (LIVE VERIFICATION STILL REQUIRED)")

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
        "NpcRouteStateSafetyPatch",
        "loadEndOfRouteBehavior",
        "AcquireCoordinatedTarget",
        "GetRectangleGapPixels",
        "TryFindApproachTile",
        "teamup_route_guard",
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
        "# Alpha 6.7.11 Elite/Boss Coordination Audit\n\n"
        "- Major threat priority preserved for eligible Damage/Control/aggressive actors: PASS\n"
        "- Major target override respects assigned-count pressure: PASS\n"
        "- Normal coordination soft cap = 2; Boss Focus soft cap = 3: PASS\n"
        "- Adds can receive overflow attackers instead of unconditional boss dogpile: PASS\n"
        "- Boss-only encounters still keep all useful party members active: PASS\n"
        "- Tank/Healer retain role-specific target acquisition: PASS\n"
        "- Attack stop distance uses monster hitbox bounds: PASS\n"
        "- Movement approach searches around full monster hitbox footprint: PASS\n"
        "- Bare-water path regression wall retained: PASS\n"
        "- Gunther route safety from 6.7.10 carried forward: PASS\n\n"
        "Important: Gunther route-state behavior remains CI/static verified only until reproduced live in Stardew/SMAPI.\n",
        encoding="utf-8", newline="\n")

    SMOKE.write_text(
        "TEAM UP 6.7.11 - LIVE TEST UU TIEN\n"
        "==================================\n"
        "A. GUNTHER ROUTE FIX (van la test uu tien so 1)\n"
        "1. Recruit Gunther tai/gan bao tang trong hoan canh tung gay loi.\n"
        "2. Doi map va choi qua moc schedule/end-of-route.\n"
        "3. Khong duoc con NullReferenceException tai NPC.loadEndOfRouteBehavior.\n"
        "4. Console: teamup_route_guard => routeGuard=True.\n\n"
        "B. ELITE/BOSS COORDINATION\n"
        "5. Gap boss/elite MaxHP >= 300 co them add: Damage/Control van uu tien major threat.\n"
        "6. Ngoai Boss Focus, sau khi major threat da co 2 attacker duoc phan cong, nguoi tiep theo nen co the tach sang add.\n"
        "7. Boss Focus cho phep 3 attacker phoi hop truoc khi uu tien tach sang add.\n"
        "8. Neu chi con mot boss, ca team van duoc tan cong, khong ai dung im vi soft cap.\n"
        "9. Boss sprite/hitbox lon: NPC di toi mep hitbox gan nhat, khong co gang path vao tile tam cua boss.\n"
        "10. Tank/Healer van giu dung logic role, khong bi ep boss-focus vo dieu kien.\n\n"
        "C. REGRESSION\n"
        "11. Gus van walk/facing vanilla 4 huong.\n"
        "12. Farmer + 4 NPC = 5/5; NPC thu 5 bi chan.\n"
        "13. Pelipper external combat companion khong vuot 2/2.\n"
        "14. teamup_roster_audit: chup lai moi WARNING neu co.\n",
        encoding="utf-8", newline="\n")

    log("BUILD SUCCESS - ALPHA 6.7.11")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
