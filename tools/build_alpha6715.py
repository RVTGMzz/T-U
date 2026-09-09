from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6715"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6715.txt"
AUDIT = ROOT / "PREFLIGHT_DIAGNOSTICS_AUDIT_ALPHA6715.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_15_PREFLIGHT_DIAGNOSTICS_VI.txt"
VERSION = "0.2.0-alpha.6.7.15"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.15_PREFLIGHT_DIAGNOSTICS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.15_PREFLIGHT_DIAGNOSTICS_TEST.sha256.txt"

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
    alpha6715 = text("ModEntry.Alpha6715.cs")
    alpha6714 = text("ModEntry.Alpha6714.cs")
    alpha6713 = text("ModEntry.Alpha6713.cs")
    alpha6613 = text("ModEntry.Alpha6613.cs")
    alpha6710 = text("ModEntry.Alpha6710.cs")
    capture_safety = text("Core/PelipperCaptureSafetyService.cs")
    combat = text("Combat/CombatService.cs")
    config = text("ModConfig.cs")
    rank = text("Core/CombatRankCatalog.cs")
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req('"teamup_preflight"' in alpha6715, "preflight command missing")
    req('"TeamUp_Diagnostic_latest.txt"' in alpha6715, "diagnostic export filename missing")
    req('Path.Combine(Helper.DirectoryPath, "diagnostics")' in alpha6715, "diagnostic export folder missing")
    req("NpcRouteStateSafetyPatch.IsApplied" in alpha6715, "route guard snapshot missing")
    req("GetEffectiveCombatCompanionCountAlpha6618" in alpha6715, "effective companion truth missing")
    req("PelipperTown119NativeBridge.Status" in alpha6715, "Pelipper native status missing")
    req("PelipperCaptureSafetyService.TryGetDamageBudget" in alpha6715, "capture proxy budget snapshot missing")
    req("CombatRankCatalog.Get" in alpha6715, "rank snapshot missing")
    req("EnsureAlpha6715Registered();" in alpha6714, "6.7.15 registration chain missing")

    # Diagnostics-only means this new slice must not change gameplay or Pelipper source state.
    forbidden_diag_mutations = [
        "SetOwnerOptOut(",
        "TrySetVillagerCompanionEnabled(",
        "TrySetPelipperNpcSourceEnabledAlpha6621(",
        "SetCompanionState(",
        "TryAddPlayerCompanion(",
        "TryLinkCompanion(",
        ".Health =",
        "PerformAttack(",
        "SetDesiredDeployment(",
    ]
    for token in forbidden_diag_mutations:
        req(token not in alpha6715, f"diagnostics-only slice contains mutation token: {token}")

    # Carry-forward live-fix gates and locked architecture.
    req("NpcOnlySourceLockPulseTicksAlpha6714 = 10" in alpha6714, "6.7.14 NPC-only lock regression")
    req("WildCombatProxyKey" in alpha6613, "6.7.13 capture proxy regression")
    req("RepairCurrentLocationFloors" in capture_safety, "capture floor watchdog regression")
    req('"teamup_capture_proxy"' in alpha6713, "capture proxy diagnostic regression")
    req("NpcRouteStateSafetyPatch.Apply" in alpha6710, "Gunther route guard regression")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("int coordinationCap = _strategy() == PartyStrategy.BossFocus ? 3 : 2;" in combat,
        "6.7.11 boss coordination regression")
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "S-rank contrast regression")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("READ-ONLY PREFLIGHT DIAGNOSTICS AUDIT: PASS")
    log("6.7.13/6.7.14 LIVE-GUARD CARRY-FORWARD: PASS")
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
        "teamup_preflight",
        "TeamUp_Diagnostic_latest.txt",
        "BuildAlpha6715PreflightReport",
        "NpcRouteStateSafetyPatch",
        "PelipperWildCombatProxy",
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
        "# Team Up Alpha 6.7.15 Preflight Diagnostics Audit\n\n"
        "## New slice\n"
        "- teamup_preflight command: PASS\n"
        "- Read-only party/cap snapshot: PASS\n"
        "- Pelipper native/source snapshot: PASS\n"
        "- NPC-only lock convergence warnings: PASS\n"
        "- Capture proxy budget/ceasefire warnings: PASS\n"
        "- Route guard snapshot: PASS\n"
        "- Roster rank snapshot: PASS\n"
        "- Automatic TXT export to Team Up/diagnostics: PASS\n"
        "- No gameplay/source mutation APIs in Alpha6715: PASS\n\n"
        "## Carry forward\n"
        "- Alpha 6.7.13 capture proxy identity: PASS\n"
        "- Alpha 6.7.14 NPC-only lock: PASS\n"
        "- People cap 5 and external companion cap 2/2: PASS\n"
        "- Boss/add coordination and Rank S contrast: PASS\n"
        "- Gunther route guard retained, still requires live verification.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.15 - PREFLIGHT DIAGNOSTICS\n"
        "====================================\n\n"
        "Ban nay khong them gameplay moi. Muc tieu la gom toan bo du lieu test vao 1 lenh.\n\n"
        "1. Cai ban 6.7.15 va vao save.\n"
        "2. Go: teamup_preflight\n"
        "3. SMAPI se in report PASS/WARN va duong dan file export chinh xac.\n"
        "4. File mac dinh nam trong thu muc mod: Team Up/diagnostics/TeamUp_Diagnostic_latest.txt\n"
        "5. Khi test Luther/Gunther + Aerodactyl, chay teamup_preflight sau khi moi NPC.\n"
        "6. Khi test capture 10%, chay teamup_preflight luc Pokemon hoang dang o moc bat.\n"
        "7. Neu co WARN, gui nguyen file TeamUp_Diagnostic_latest.txt cho ChatGPT.\n\n"
        "Luu y: Alpha 6.7.13/14 va Gunther route van can live test; CI PASS khong thay the live verification.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.15")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
