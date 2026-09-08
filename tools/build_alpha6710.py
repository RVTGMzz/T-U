from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6710"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6710.txt"
AUDIT = ROOT / "ROUTE_STATE_AUDIT_ALPHA6710.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_10_GUNTHER_ROUTE_RANK_A_VI.txt"
VERSION = "0.2.0-alpha.6.7.10"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.10_GUNTHER_ROUTE_RANK_A_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.10_GUNTHER_ROUTE_RANK_A_TEST.sha256.txt"

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
    skills = text("Core/CharacterSkillIdentityCatalog.cs")
    polish = text("Combat/Alpha6CombatPolishService.cs")
    combat = text("Combat/CombatService.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    alpha6710 = text("ModEntry.Alpha6710.cs")

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")

    # Exact crash regression wall.
    req("npc.endOfRouteBehaviorName.Value = null;" not in follow, "route metadata nulling still present")
    req("npc.nextEndOfRouteMessage = null;" not in follow, "route message nulling still present")
    req("npc.endOfRouteMessage.Value = null;" not in follow, "route message netfield nulling still present")
    req("Alpha 6.7.10: preserve endOfRouteBehaviorName/messages" in follow, "route preservation marker missing")
    req('AccessTools.Method(typeof(NPC), "loadEndOfRouteBehavior", new[] { typeof(string) })' in route, "loadEndOfRouteBehavior exact Harmony target missing")
    req("teamUpControlled" in route and "return false;" in route, "controlled NPC route suppression missing")
    req("string.IsNullOrWhiteSpace(__0)" in route, "stale null/empty route guard missing")
    req("NpcRouteStateSafetyPatch.Apply" in alpha6710, "route guard registration missing")
    req("EnsureAlpha6710Registered();" in alpha6625, "Alpha6710 registration chain missing")

    # A ranks requested by user.
    for name in ["Abigail", "Alex", "Haley", "Maru", "Evelyn"]:
        req(f'["{name}"] = new(CombatRank.A)' in rank, f"{name} A-rank missing")
    req('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank, "Marlon S regression")
    req("RecruitBadge.Boss | RecruitBadge.Special" in rank, "MiMi S/BOSS/SPECIAL regression")

    # A-rank identity tuning, no blanket rank multiplier.
    req('FLASH SHOT", CharacterSignatureArchetype.Burst, 570, 480' in skills, "Haley A-rank tempo tuning missing")
    req("stun2: 120, stun3: 260" in skills, "Haley flash control tuning missing")
    req('GARDEN REMEDY", CharacterSignatureArchetype.Recovery, 750, 660' in skills, "Evelyn A-rank cooldown tuning missing")
    req("heal: 11, radius: 7.5f, maxTargets: 5" in skills, "Evelyn A-rank recovery tuning missing")
    req("tier >= 3 ? 450 : 600" in polish, "Abigail A-rank tempo tuning missing")
    req("farmerRatio > 0.55f" in polish and "/ 10" in polish, "Alex A-rank guard tuning missing")
    req("candidates.Count < 2 && !candidates.Any(monster => monster.MaxHealth >= 300)" in polish, "Maru elite/boss single-target control gate missing")

    # General behavior improvement for major threats and giant hitboxes.
    req("AcquireCoordinatedTarget(" in combat, "coordinated target acquisition missing")
    req("monster.MaxHealth >= 300" in combat, "major-threat threshold missing")
    req("role is PartyRole.Tank or PartyRole.Healer" in combat, "role-specific target independence missing")
    req("GetDistanceToMonsterBoundsTiles(npc, target)" in combat, "monster hitbox distance usage missing")
    req("target.GetBoundingBox()" in combat, "giant-monster hitbox helper missing")

    # Locked regressions.
    req("UnlockVanillaMovementAnimation(npc);" in follow, "Gus animation unlock regression")
    req("npc.Sprite.ignoreStopAnimation = false;" in follow, "Gus animation flag regression")
    req("private const int CombatPathRetryCooldownTicks = 24;" in combat, "combat path cadence regression")
    req("private const int CombatMovementPulseTicks = 3;" in combat, "combat movement pulse regression")
    req("SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner" in combat, "single prototype signature authority regression")
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    req("PelipperRenderSuppressedAlpha6613" not in all_source, "legacy Pelipper render suppression returned")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")
    req("isTileLocationTotallyClearAndPlaceable" not in follow, "follow water/path regression")
    req("isTileLocationTotallyClearAndPlaceable" not in combat, "combat water/path regression")

    log("SOURCE ACCEPTANCE: PASS")
    log("ROUTE STATE STATIC AUDIT: PASS")
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
    for token in ["NpcRouteStateSafetyPatch", "loadEndOfRouteBehavior", "AcquireCoordinatedTarget", "GetDistanceToMonsterBoundsTiles", "FLASH SHOT", "GARDEN REMEDY", "teamup_route_guard"]:
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
        "# Alpha 6.7.10 Route State Audit\n\n"
        "- Removed Team Up assignment of `endOfRouteBehaviorName = null`: PASS\n"
        "- Team Up controlled NPCs suppress vanilla `loadEndOfRouteBehavior`: PASS\n"
        "- Null/empty stale route names fail safe instead of base-loop crash: PASS\n"
        "- Vanilla route metadata preserved for release back to schedule: PASS\n"
        "- Gus 4-direction vanilla animation unlock preserved: PASS\n"
        "- Major-threat coordination + giant hitbox distance: PASS\n"
        "- Abigail/Alex/Haley/Maru/Evelyn explicit Rank A: PASS\n"
        "- Pelipper 2/2/source-authority regressions blocked: PASS\n\n"
        "Runtime confirmation is still required for the original Gunther reproduction.\n",
        encoding="utf-8", newline="\n")
    SMOKE.write_text(
        "TEAM UP 6.7.10 - TEST UU TIEN\n"
        "===============================\n"
        "1. Gunther: recruit tai/gan bao tang, di chuyen/doi map/choi den qua moc schedule.\n"
        "   KHONG duoc con NullReferenceException tai NPC.loadEndOfRouteBehavior.\n"
        "2. Gus: van quay 4 huong + walk animation vanilla.\n"
        "3. Combat: Damage/Control uu tien boss/elite MaxHP lon gan Farmer; Tank/Healer van lam dung role.\n"
        "4. Boss sprite lon: NPC dung lai khi hitbox da nam trong attack range, khong can cham tile tam.\n"
        "5. Rank: Abigail/Alex/Haley/Maru/Evelyn = A; MiMi/Marlon van S.\n"
        "6. Party 5/5, Pelipper 2/2, khong 3/2.\n"
        "7. Console: teamup_route_guard => routeGuard=True.\n",
        encoding="utf-8", newline="\n")

    log("BUILD SUCCESS - ALPHA 6.7.10")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
