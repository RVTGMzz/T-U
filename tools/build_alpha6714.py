from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6714"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6714.txt"
AUDIT = ROOT / "LIVE_GUARD_AUDIT_ALPHA6714.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_14_NPC_ONLY_LOCK_CAPTURE_CEASEFIRE_VI.txt"
VERSION = "0.2.0-alpha.6.7.14"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.14_NPC_ONLY_LOCK_CAPTURE_CEASEFIRE_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.14_NPC_ONLY_LOCK_CAPTURE_CEASEFIRE_TEST.sha256.txt"

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
    alpha6613 = text("ModEntry.Alpha6613.cs")
    alpha6626 = text("ModEntry.Alpha6626.cs")
    alpha6710 = text("ModEntry.Alpha6710.cs")
    alpha6713 = text("ModEntry.Alpha6713.cs")
    alpha6714 = text("ModEntry.Alpha6714.cs")
    compat = text("Core/PelipperTownCompatibilityService.cs")
    capture_patch = text("Core/PelipperCaptureDamagePatch.cs")
    capture_safety = text("Core/PelipperCaptureSafetyService.cs")
    native = text("Core/PelipperTown119NativeBridge.cs")
    combat = text("Combat/CombatService.cs")
    config = text("ModConfig.cs")
    rank = text("Core/CombatRankCatalog.cs")
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")

    # 6.7.14 capture ceasefire semantics.
    req('monster.modData[PelipperTownCompatibilityService.WildCombatProxyKey] = "true";' in alpha6613,
        "wild proxy identity not retained")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in alpha6613,
        "capture floor ceasefire check missing")
    req("monster.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);" in alpha6613,
        "capture floor does not remove offensive target marker")
    req("HasTrueModData(actor, WildCombatProxyKey)" in compat,
        "capture classification no longer survives CombatTarget removal")
    req("RepairCurrentLocationFloors" in capture_safety,
        "capture watchdog regression")
    req("BeforeTakeDamage" in capture_patch and "BeforeAreaDamage" in capture_patch,
        "hard damage clamp regression")
    req("UpdateTicked += OnAlpha6713CaptureFloorUpdateTicked" in alpha6713,
        "dedicated capture watchdog registration missing")
    req("UpdateTicked -= OnAlpha6713CaptureFloorUpdateTicked" not in all_source,
        "capture watchdog unexpectedly unsubscribed")

    # 6.7.14 durable NPC-only source lock. It must NOT depend on a linked companion row.
    req("NpcOnlySourceLockPulseTicksAlpha6714 = 10" in alpha6714,
        "NPC-only source lock cadence missing")
    req("PelipperTownCompatibilityService.IsOwnerOptedOut(owner)" in alpha6714,
        "NPC-only lock ignores durable opt-out intent")
    req("TryIsVillagerCompanionConfiguredEnabled" in alpha6714,
        "NPC-only lock does not inspect source enabled state")
    req("TrySetPelipperNpcSourceEnabledAlpha6621" in alpha6714 and "enabled: false" in alpha6714,
        "NPC-only lock does not enforce source disable")
    lock_body = alpha6714.split("private int EnforceNpcOnlySourceLocksAlpha6714", 1)[1]
    req("Party.GetLinkedCompanion" not in lock_body.split("private void OnAlpha6714NpcOnlyLockCommand", 1)[0],
        "NPC-only enforcement incorrectly requires a linked Team Up companion row")
    req('"teamup_npc_only_lock"' in alpha6714,
        "NPC-only lock diagnostic command missing")
    req("EnsureAlpha6714Registered();" in alpha6710,
        "6.7.14 registration chain missing")

    # 6.7.13 live-fix carry-forward.
    req("PelipperWildCombatProxy" in compat, "6.7.13 wild proxy marker regression")
    req("ResolvePelipperVillagerConfigKeyAlpha6713" in alpha6713,
        "6.7.13 expansion-NPC config-key resolver regression")
    req('"teamup_recruit_probe"' in alpha6713 and '"teamup_capture_proxy"' in alpha6713,
        "6.7.13 diagnostics regression")
    forbidden_disabled_gate = "TryIsVillagerCompanionConfiguredEnabled(npcName, out bool enabled) && !enabled"
    req(forbidden_disabled_gate not in native,
        "configured species again disappears when Companion enabled=false")

    # Locked architecture/balance invariants.
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat,
        "boss/add coordination regression")
    req("GetRectangleGapPixels" in combat, "large-hitbox approach regression")
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "S-rank contrast regression")
    req("NpcRouteStateSafetyPatch.Apply" in alpha6710, "Gunther route guard regression")
    req("UpdateTicked -= OnAlpha6615UpdateTicked" in alpha6626,
        "single companion authority premise changed")
    req("PelipperRenderSuppressedAlpha6613" not in all_source, "legacy Pelipper render suppression returned")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("CAPTURE CEASEFIRE STATIC AUDIT: PASS")
    log("NPC-ONLY SOURCE LOCK STATIC AUDIT: PASS")
    log("6.7.13 LIVE-FIX CARRY-FORWARD: PASS")
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
        "NpcOnlySourceLockPulseTicksAlpha6714",
        "EnforceNpcOnlySourceLocksAlpha6714",
        "teamup_npc_only_lock",
        "PelipperWildCombatProxy",
        "OnAlpha6713CaptureFloorUpdateTicked",
        "ResolvePelipperVillagerConfigKeyAlpha6713",
        "NpcRouteStateSafetyPatch",
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
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for p in sorted(MOD_STAGE.rglob("*")):
            if p.is_file():
                zf.write(p, p.relative_to(STAGE))

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Team Up Alpha 6.7.14 Live Guard Audit\n\n"
        "## Capture ceasefire\n"
        "- Wild combat proxy identity remains stamped at all HP levels: PASS\n"
        "- At capture floor, Team Up removes only CombatTarget opt-in: PASS\n"
        "- Wild proxy identity survives so takeDamage/areaDamage/watchdog remain active: PASS\n\n"
        "## NPC-only source lock\n"
        "- Active Team Up NPC opt-out intent is checked every 10 ticks: PASS\n"
        "- Source configured enabled bit is inspected: PASS\n"
        "- Native source disable is enforced without requiring a linked companion row: PASS\n"
        "- Diagnostic command teamup_npc_only_lock is present: PASS\n\n"
        "Static/CI PASS does not replace live Aerodactyl and <=10% capture-floor testing.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.14 - LIVE TEST UU TIEN\n"
        "================================\n\n"
        "A. NPC-ONLY + AERODACTYL\n"
        "1. Moi NPC co Aerodactyl va chon NPC mot minh.\n"
        "2. Doi map, cho 10-20 giay, noi chuyen/chien dau; Aerodactyl KHONG duoc tu spawn vao team.\n"
        "3. Go teamup_npc_only_lock. Dong NPC phai co enabled=False va sourceLive=False.\n"
        "4. Neu menu van chi co 1 lua chon, go teamup_recruit_probe <ten NPC>.\n\n"
        "B. CAPTURE CEASEFIRE <=10%\n"
        "5. Farmer + 1 NPC Team Up + Pokemon cua Farmer.\n"
        "6. Danh Pokemon hoang xuong capture floor.\n"
        "7. NPC Team Up phai bo target; Pokemon hoang khong duoc mat them HP qua floor.\n"
        "8. Go teamup_capture_proxy: proxy o floor nen target=False, proxy=True, budget=0.\n"
        "9. Thu it nhat 2 Pokemon hoang khac nhau.\n\n"
        "C. REGRESSION\n"
        "10. Farmer + 4 NPC = 5/5; nguoi thu 6 bi chan.\n"
        "11. Companion ngoai <=2/2.\n"
        "12. Rank S van de doc.\n"
        "13. Gunther route crash van can live-test rieng truoc khi danh dau fixed.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.14")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
