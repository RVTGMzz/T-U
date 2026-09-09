from __future__ import annotations

import hashlib
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6723"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6723.txt"
AUDIT = ROOT / "CAPTURE_CEASEFIRE_AUDIT_ALPHA6723.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_23_CAPTURE_CEASEFIRE_VI.txt"
VERSION = "0.2.0-alpha.6.7.23"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.23_CAPTURE_CEASEFIRE_HARDENING_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.23_CAPTURE_CEASEFIRE_HARDENING_TEST.sha256.txt"

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
    entry = text("ModEntry.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    combat = text("Combat/CombatService.cs")
    alpha6 = text("Combat/Alpha6CombatPolishService.cs")
    skill = text("Combat/CharacterSkillIdentityService.cs")
    expansion = text("Combat/ExpansionSkillService.cs")
    special = text("Combat/SpecialRecruitCombatService.cs")
    capture = text("Core/PelipperCaptureSafetyService.cs")
    scanner = text("ModEntry.Alpha6613.cs")
    alpha6723 = text("ModEntry.Alpha6723.cs")
    probe6722 = text("Core/PelipperWildDensitySurfaceProbe.cs")
    surge = text("Combat/MonsterSurgeService.cs")
    config = text("ModConfig.cs")
    rank = text("Core/CombatRankCatalog.cs")
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6723Events();" in entry, "6.7.23 registration missing")
    req('"teamup_capture_ceasefire"' in alpha6723, "capture ceasefire diagnostic command missing")
    req("TeamUp_Capture_Ceasefire_latest.txt" in alpha6723, "capture ceasefire diagnostic file missing")

    # Canonical policy must fail closed for Pelipper capture-floor actors.
    req("PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster)" in policy,
        "canonical Pelipper source-control gate missing")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy,
        "canonical <=10% ceasefire gate missing")
    req("OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster)" in policy,
        "Cardcha test actor exclusion regression")

    # Every autonomous offensive layer with an independent Monster list must share the same gate.
    req(".Where(TeamUpOffensiveTargetPolicy.IsEligible)" in alpha6,
        "Alpha6 signature layer still bypasses capture ceasefire")
    req(".Where(TeamUpOffensiveTargetPolicy.IsEligible)" in skill,
        "Character skill layer still bypasses capture ceasefire")
    req(".Where(TeamUpOffensiveTargetPolicy.IsEligible)" in special,
        "Special recruit layer can still animate/stun capture-floor target")
    req("PelipperCaptureSafetyService.ClampDamage(monster, damage)" in skill,
        "Character skill second-line damage clamp missing")

    # CombatService already has the immediate pre-swing floor check; expansion skills receive its
    # filtered offensive list. Lock both assumptions so future edits cannot reopen the gap silently.
    req("if (PelipperCaptureSafetyService.IsProtected(target))" in combat,
        "generic pre-swing capture-floor recheck missing")
    req("_expansionSkills.Update(activeMembers, monsters);" in combat,
        "expansion skills no longer consume CombatService filtered targets")
    req("public void Update(IReadOnlyList<PartyMemberData> members, IReadOnlyList<Monster> monsters)" in expansion,
        "expansion skill target contract changed")

    # Durable proxy identity survives ceasefire while transient Team Up target opt-in is removed.
    req("monster.modData.Remove(PelipperTownCompatibilityService.CombatTargetOptInKey);" in alpha6723,
        "ceasefire watchdog does not remove transient target opt-in")
    req("WildCombatProxyKey" in scanner and "CombatTargetOptInKey" in scanner,
        "durable proxy / transient target split regression")
    req("monster.modData.Remove(PelipperTownCompatibilityService.WildCombatProxyKey);" not in alpha6723,
        "ceasefire watchdog incorrectly removes durable wild-proxy identity")

    # Existing damage-floor backstops remain intact.
    req("RepairCurrentLocationFloors" in capture, "capture floor repair regression")
    req("ClampDamage" in capture and "TryGetDamageBudget" in capture, "capture budget/clamp regression")

    # Carry forward 6.7.22 + universal density + mutation + locked roster invariants.
    req("recommendation=\"" not in probe6722, "6.7.22 probe should remain runtime-derived")
    req("public float MonsterDensityMultiplier { get; set; } = 2.5f;" in config, "x2.5 density regression")
    req("public float MutationChancePercent { get; set; } = 5f;" in config, "mutation 5% regression")
    req("PelipperTownCompatibilityService.IsWildCombatActor(monster)" in surge, "Pelipper density fail-closed regression")
    req("CombatRank.S => new Color(132, 70, 12)" in rank, "Rank S contrast regression")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person cap regression")
    req("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2/2 companion cap regression")
    req("TrySetActorInvisibleAlpha6613" not in all_source, "legacy Pelipper visibility writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("CAPTURE CEASEFIRE ROOT-CAUSE AUDIT: PASS (Alpha6 + CharacterSkill independent target lists closed)")
    log("CANONICAL OFFENSIVE TARGET POLICY: PASS")
    log("SPECIAL RECRUIT VISUAL/STUN CEASEFIRE: PASS")
    log("GENERIC + EXPANSION COMBAT CARRY-FORWARD: PASS")
    log("DURABLE WILD-PROXY IDENTITY / TRANSIENT TARGET MARKER: PASS")
    log("6.7.22 DENSITY PROBE + 6.7.21 DENSITY + MUTATION CARRY-FORWARD: PASS")
    log("LOCKED PARTY/PELIPPER/CAPTURE/RANK INVARIANTS: PASS")
    log("LIVE CAPTURE TEST STILL REQUIRED")

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
        "teamup_capture_ceasefire",
        "TeamUp_Capture_Ceasefire_latest.txt",
        "TeamUpOffensiveTargetPolicy",
        "teamup_pelipper_density_probe",
        "teamup_density",
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
        "# Team Up Alpha 6.7.23 Capture Ceasefire Hardening Audit\n\n"
        "## Live bug received\n"
        "User confirmed that when a Pelipper wild Pokemon reaches the <=10% capture floor, Team Up companions still attack it.\n\n"
        "## Root cause found in source\n"
        "CombatService already filtered protected Pelipper targets and re-checked immediately before generic swings. However, Alpha6CombatPolishService and CharacterSkillIdentityService each built their own raw Monster lists outside CombatService, so upgraded/signature attacks could still select the protected combat proxy. SpecialRecruitCombatService clamped damage but could still animate/stun the protected target.\n\n"
        "## 6.7.23 fix\n"
        "- Adds TeamUpOffensiveTargetPolicy as the canonical autonomous-offense gate.\n"
        "- Alpha6 signature prototypes use the canonical gate.\n"
        "- Character Skill Identity uses the canonical gate and also clamps damage as a second backstop.\n"
        "- Special recruits use the canonical gate, preventing zero-damage attack/stun animation at the capture floor.\n"
        "- A per-tick watchdog removes only Team Up's transient CombatTarget opt-in from protected wild proxies.\n"
        "- Durable WildCombatProxy identity remains untouched, so capture-floor detection stays alive.\n"
        "- Pelipper remains source authority for Pokemon AI/render/controller/capture lifecycle.\n\n"
        "## Scope not changed\n"
        "Universal density 2.5x, mutation encounters, party cap 5, companion cap 2/2, Gunther route work, and the 6.7.22 Pelipper density probe are carried forward unchanged.\n\n"
        "CI verifies code/build invariants only. The user's <=10% capture scenario must still be live-tested.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.23 - CAPTURE CEASEFIRE HARDENING\n"
        "==============================================\n\n"
        "LOI LIVE DA XAC NHAN:\n"
        "Pokemon hoang Pelipper duoi/tại nguong 10% van bi dong doi Team Up tan cong.\n\n"
        "TEST NGAN:\n"
        "1. Cai 6.7.23 vao E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\n"
        "2. Bao dam Pelipper dang bat che do dung danh o nguong bat Pokemon.\n"
        "3. Di cung it nhat 1 NPC Team Up, co the them Pokemon dong doi.\n"
        "4. Danh Pokemon hoang xuong nguong <=10%.\n"
        "5. NPC Team Up phai NGUNG tan cong muc tieu do. Khong signature, khong stun, khong AoE tiep tuc danh vao no.\n"
        "6. Pokemon hoang phai song o capture floor, khong bi Team Up keo HP xuong them.\n"
        "7. Go: teamup_capture_ceasefire\n\n"
        "MONG DOI trong diagnostic:\n"
        "protected=True\n"
        "eligible=False\n"
        "target=False\n"
        "proxy=True\n"
        "budget=0\n\n"
        "FILE DIAGNOSTIC:\n"
        "E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Capture_Ceasefire_latest.txt\n\n"
        "Neu van bi danh, thoat game NGAY va gui them:\n"
        "%appdata%\\StardewValley\\ErrorLogs\\SMAPI-latest.txt\n\n"
        "Luu y: neu chi Pokemon dong doi cua Pelipper van co animation tan cong nhung HP khong giam, do co the la AI source-owned cua Pelipper. Team Up khong duoc chiem quyen controller cua Pelipper de tat animation bang cach gia.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.23")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
