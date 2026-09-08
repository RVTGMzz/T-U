from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6713"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6713.txt"
AUDIT = ROOT / "LIVE_REGRESSION_AUDIT_ALPHA6713.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_13_CAPTURE_PROXY_RECRUIT_INTENT_VI.txt"
VERSION = "0.2.0-alpha.6.7.13"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.13_CAPTURE_PROXY_RECRUIT_INTENT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.13_CAPTURE_PROXY_RECRUIT_INTENT_TEST.sha256.txt"

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
    compat = text("Core/PelipperTownCompatibilityService.cs")
    capture_patch = text("Core/PelipperCaptureDamagePatch.cs")
    capture_safety = text("Core/PelipperCaptureSafetyService.cs")
    native = text("Core/PelipperTown119NativeBridge.cs")
    alpha6613 = text("ModEntry.Alpha6613.cs")
    alpha6615 = text("ModEntry.Alpha6615.cs")
    alpha6621 = text("ModEntry.Alpha6621.cs")
    alpha6626 = text("ModEntry.Alpha6626.cs")
    alpha671 = text("ModEntry.Alpha671.cs")
    alpha6710 = text("ModEntry.Alpha6710.cs")
    alpha6713 = text("ModEntry.Alpha6713.cs")
    combat = text("Combat/CombatService.cs")
    config = text("ModConfig.cs")
    rank = text("Core/CombatRankCatalog.cs")
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")

    # Live regression #1: Pelipper wild visible NPC and Monster proxy are separate entities.
    req("PelipperWildCombatProxy" in compat, "wild combat proxy marker key missing")
    req("actor is not Monster" in compat, "capture proxy type boundary missing")
    req("HasTrueModData(actor, WildCombatProxyKey)" in compat, "capture policy does not consume wild proxy marker")
    req("HasTrueModData(actor, CombatTargetOptInKey)" in compat, "capture policy migration/race fallback missing")
    req("monster.modData[PelipperTownCompatibilityService.WildCombatProxyKey] = \"true\";" in alpha6613,
        "combat scanner does not stamp wild proxy marker")
    req("monster.modData.Remove(PelipperTownCompatibilityService.WildCombatProxyKey);" in alpha6613,
        "owned Pelipper actor does not clear wild proxy marker")

    # 6.7.12 global damage clamps remain in force.
    req("PatchedTakeDamageMethods" in capture_patch and "BeforeTakeDamage" in capture_patch,
        "takeDamage hard floor regression")
    req("PatchedAreaDamageMethods" in capture_patch and "BeforeAreaDamage" in capture_patch,
        "damageMonster area hard floor regression")
    req("RepairCurrentLocationFloors" in capture_safety, "last-resort floor repair missing")

    # Critical event-wiring regression: 6.7.12 put the repair into OnAlpha6615UpdateTicked, but
    # Alpha 6.6.26 intentionally unsubscribes that legacy handler. 6.7.13 owns a dedicated event.
    req("UpdateTicked += OnAlpha6713CaptureFloorUpdateTicked" in alpha6713,
        "dedicated 6.7.13 capture watchdog is not registered")
    req("RepairCurrentLocationFloors(Game1.currentLocation)" in alpha6713,
        "dedicated capture watchdog does not repair the active location")
    req("UpdateTicked -= OnAlpha6713CaptureFloorUpdateTicked" not in all_source,
        "dedicated capture watchdog is unsubscribed somewhere")
    req("6.7.12: mercy/capture is a world combat invariant" not in alpha6615,
        "dead 6.7.12 watchdog duplicate still lives in legacy unsubscribed handler")
    req("UpdateTicked -= OnAlpha6615UpdateTicked" in alpha6626,
        "test premise changed: Alpha6626 no longer unsubscribes legacy handler")

    # Live regression #2: configured species must survive Companion enabled=false.
    forbidden_disabled_gate = "TryIsVillagerCompanionConfiguredEnabled(npcName, out bool enabled) && !enabled"
    req(forbidden_disabled_gate not in native,
        "configured partner lookup still hides species when source is disabled")
    req("GetConfiguredRecruitDescriptorAlpha6713(owner)" in alpha671,
        "recruitment does not use configured species intent")
    req("ResolvePelipperVillagerConfigKeyAlpha6713(owner)" in alpha6621,
        "source lifecycle does not resolve the Pelipper owner config key")
    req("configuredEnabled" in alpha6621 and "alreadyConverged" in alpha6621,
        "source convergence ignores configured enabled state")
    req("ResolvePelipperVillagerConfigKeyAlpha6713(owner)" in text("ModEntry.Alpha663.cs"),
        "initial NPC-only/NPC+Pokemon recruit does not use resolved source key")

    # Diagnostics for the exact two live issues.
    req('"teamup_recruit_probe"' in alpha6713, "recruit probe command missing")
    req('"teamup_capture_proxy"' in alpha6713, "capture proxy probe command missing")
    req("EnsureAlpha6713Registered();" in alpha6710, "6.7.13 registration chain missing")

    # Locked architecture/balance invariants.
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat,
        "6.7.11 boss/add coordination regression")
    req("GetRectangleGapPixels" in combat, "large-hitbox approach regression")
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "6.7.12 S-rank contrast regression")
    req("NpcRouteStateSafetyPatch.Apply" in alpha6710, "Gunther route guard regression")
    req("PelipperRenderSuppressedAlpha6613" not in all_source, "legacy Pelipper render suppression returned")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("LIVE CAPTURE-PROXY REGRESSION AUDIT: PASS")
    log("RECRUITMENT INTENT REGRESSION AUDIT: PASS")
    log("DEDICATED WATCHDOG EVENT AUDIT: PASS")
    log("6.7.11/6.7.12 REGRESSIONS CARRIED FORWARD: PASS")
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
        "PelipperWildCombatProxy",
        "OnAlpha6713CaptureFloorUpdateTicked",
        "ResolvePelipperVillagerConfigKeyAlpha6713",
        "teamup_recruit_probe",
        "teamup_capture_proxy",
        "BeforeAreaDamage",
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
        "# Team Up Alpha 6.7.13 Live Regression Audit\n\n"
        "## Capture floor\n"
        "- Pelipper visible Wild NPC and Monster combat proxy are treated as distinct layers: PASS\n"
        "- Unowned Pelipper Monster combat proxies receive an explicit WildCombatProxy marker: PASS\n"
        "- Capture identity consumes WildCombatProxy and CombatTarget markers: PASS\n"
        "- Monster.takeDamage hard clamp retained: PASS\n"
        "- GameLocation.damageMonster hard clamp retained: PASS\n"
        "- Dedicated per-tick repair handler is NOT the legacy Alpha6615 handler: PASS\n"
        "- Alpha6626 cannot unsubscribe the new capture handler: PASS\n\n"
        "## NPC + Pokemon recruitment\n"
        "- Configured species lookup no longer disappears when Companion enabled=false: PASS\n"
        "- Recruitment checks configured species after live lookup: PASS\n"
        "- Internal NPC name / display-name config key resolver present: PASS\n"
        "- Initial NPC-only/NPC+Pokemon source write uses resolved key: PASS\n"
        "- Native convergence checks configured enabled state as well as source-live detection: PASS\n\n"
        "Static/CI PASS is not a substitute for the exact Luther/Gunther + Aerodactyl and <10% live tests.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.13 - LIVE TEST BAT BUOC\n"
        "=================================\n\n"
        "A. LUTHER/GUNTHER + AERODACTYL\n"
        "1. Neu NPC dang o Team Up, cho NPC roi team truoc.\n"
        "2. Noi chuyen va moi lai NPC co Aerodactyl da cau hinh trong Pelipper Town.\n"
        "3. Hop thoai PHAI co 2 lua chon: moi NPC mot minh / moi NPC + Pokemon.\n"
        "4. Chon NPC mot minh: Aerodactyl KHONG duoc vao theo sau.\n"
        "5. Cho NPC roi team, moi lai va chon NPC + Pokemon: Aerodactyl duoc vao neu con slot.\n"
        "6. Neu buoc 3 sai: chay teamup_recruit_probe <ten NPC> va gui dong output.\n\n"
        "B. CAPTURE FLOOR 10% - FARMER + NPC + POKEMON\n"
        "7. Bat che do Pelipper dung danh khi Pokemon hoang <=10% HP.\n"
        "8. Dua 1 NPC Team Up va Pokemon cua Farmer vao tran.\n"
        "9. Danh Pokemon hoang xuong moc bat. NPC Team Up PHAI bo muc tieu tan cong.\n"
        "10. Pokemon dong hanh cung KHONG duoc ha guc Pokemon hoang qua floor.\n"
        "11. Thu 2-3 Pokemon hoang khac nhau de loai tru mot proxy dac biet.\n"
        "12. Neu con loi, khi muc tieu dang song chay teamup_capture va teamup_capture_proxy; gui ca hai dong.\n\n"
        "C. REGRESSION\n"
        "13. Farmer + 4 NPC = 5/5; NPC tiep theo bi chan.\n"
        "14. External combat companion khong vuot 2/2.\n"
        "15. Rank S van de doc tren nen cam.\n"
        "16. Gunther route guard van khong duoc coi la live-verified neu chua test lai dung tinh huong crash cu.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.13")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
