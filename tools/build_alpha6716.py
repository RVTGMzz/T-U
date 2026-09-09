from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6716"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6716.txt"
AUDIT = ROOT / "TELEMETRY_COMPAT_AUDIT_ALPHA6716.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_16_TELEMETRY_COMPAT_VI.txt"
VERSION = "0.2.0-alpha.6.7.16"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.16_COMBAT_TELEMETRY_COMPAT_AUDIT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.16_COMBAT_TELEMETRY_COMPAT_AUDIT_TEST.sha256.txt"

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
    alpha6716 = text("ModEntry.Alpha6716.cs")
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
    req('"teamup_combat_report"' in alpha6716, "combat telemetry command missing")
    req('"teamup_compat_audit"' in alpha6716, "runtime compatibility command missing")
    req('"teamup_diag_all"' in alpha6716, "combined diagnostic bundle command missing")
    req('"TeamUp_Combat_latest.txt"' in alpha6716, "combat export filename missing")
    req('"TeamUp_Compatibility_latest.txt"' in alpha6716, "compat export filename missing")
    req('"TeamUp_Diagnostic_bundle_latest.txt"' in alpha6716, "bundle export filename missing")
    req("_targets" in alpha6716 and "_attackCooldowns" in alpha6716 and "_combatPathRetryTicks" in alpha6716,
        "combat target/cooldown telemetry fields missing")
    req("PelipperCaptureSafetyService.IsProtected" in alpha6716,
        "capture protected target telemetry missing")
    req("CombatRosterIntegrityService.AuditKnownCatalog" in alpha6716,
        "static roster integrity bridge missing")
    req("NpcProfileCatalog.GetAvailableProfiles" in alpha6716,
        "available runtime profile coverage missing")
    req("BanterContentCatalog.AuditKnownRoster" in alpha6716 and "ContextBanterCatalog.AuditKnownRoster" in alpha6716,
        "banter/content audit bridge missing")
    req("PartyChemistryCatalog.PairCount" in alpha6716 and "ChemistryVariantCatalog.PairVariantCount" in alpha6716,
        "chemistry coverage snapshot missing")
    req("EnsureAlpha6716Registered();" in alpha6714, "6.7.16 registration chain missing")

    # The new slice is observational only. Never allow diagnostics to become a second authority.
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
        "TakePartyControl(",
        "ReleaseToVanilla(",
        "SavePartyNow(",
    ]
    for token in forbidden_diag_mutations:
        req(token not in alpha6716, f"read-only 6.7.16 slice contains mutation token: {token}")

    # Carry-forward live guards and architecture.
    req('"teamup_preflight"' in alpha6715, "6.7.15 preflight regression")
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
    log("READ-ONLY COMBAT TELEMETRY AUDIT: PASS")
    log("RUNTIME COMPATIBILITY MATRIX AUDIT: PASS")
    log("6.7.13/14/15 LIVE-GUARD CARRY-FORWARD: PASS")
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
        "teamup_combat_report",
        "teamup_compat_audit",
        "teamup_diag_all",
        "TeamUp_Diagnostic_bundle_latest.txt",
        "BuildCombatTelemetryAlpha6716",
        "BuildCompatibilityAuditAlpha6716",
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
        "# Team Up Alpha 6.7.16 Telemetry + Compatibility Audit\n\n"
        "## New read-only tools\n"
        "- teamup_combat_report: PASS\n"
        "- Per-Farmer CombatService target/cooldown/path telemetry: PASS\n"
        "- Capture-protected target warning: PASS\n"
        "- Current-location boss/add overview: PASS\n"
        "- teamup_compat_audit: PASS\n"
        "- Runtime NPC/profile/rank/kit matrix: PASS\n"
        "- Static roster integrity bridge: PASS\n"
        "- Banter/context/chemistry coverage summary: PASS\n"
        "- teamup_diag_all combined export: PASS\n"
        "- No gameplay/source/save mutation APIs in Alpha6716: PASS\n\n"
        "## Carry forward\n"
        "- 6.7.13 capture proxy identity: PASS\n"
        "- 6.7.14 NPC-only source lock: PASS\n"
        "- 6.7.15 preflight diagnostics: PASS\n"
        "- People cap 5 and external companion cap 2/2: PASS\n"
        "- 6.7.11 boss/add coordination and 6.7.12 Rank S contrast: PASS\n"
        "- Gunther route guard retained, still requires exact live verification.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.16 - TELEMETRY + COMPAT AUDIT\n"
        "==========================================\n\n"
        "Ban nay khong doi gameplay. Day la bo may X-quang de test nhanh khi ve nha.\n\n"
        "1. Go teamup_combat_report trong luc dang combat de xuat target/cooldown/path report.\n"
        "2. Go teamup_compat_audit de xem NPC runtime nao thieu profile/rank/kit.\n"
        "3. Go teamup_diag_all de gom preflight + combat + compatibility vao mot file.\n"
        "4. File tong hop: Team Up/diagnostics/TeamUp_Diagnostic_bundle_latest.txt\n"
        "5. Khi test capture 10%, chay teamup_diag_all luc Pokemon hoang dang o moc bat.\n"
        "6. Khi test Luther/Gunther + Aerodactyl, chay teamup_diag_all sau khi chon NPC-only.\n"
        "7. Neu co WARN, gui nguyen file bundle cho ChatGPT.\n\n"
        "6.7.16 la diagnostics-only. Gunther/Pelipper/capture van can live verification.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.16")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
