from __future__ import annotations

import hashlib
import json
import re
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha674"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA674.txt"
AUDIT = ROOT / "ROSTER_AUDIT_ALPHA674.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_4_ROSTER_BALANCE_AUDIT_VI.txt"
VERSION = "0.2.0-alpha.6.7.4"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.4_ROSTER_BALANCE_AUDIT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.4_ROSTER_BALANCE_AUDIT_TEST.sha256.txt"

lines: list[str] = []
audit_lines: list[str] = ["# Team Up Alpha 6.7.4 Roster Audit", ""]


def log(text: str) -> None:
    print(text)
    lines.append(text)


def require(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)


def text(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")


def dll_contains(blob: bytes, token: str) -> bool:
    return token.encode("utf-8") in blob or token.encode("utf-16le") in blob


def extract_direct_profiles(source: str) -> dict[str, tuple[str, str, list[int]]]:
    result: dict[str, tuple[str, str, list[int]]] = {}
    pattern = re.compile(
        r'\["(?P<name>[^"]+)"\]\s*=\s*P\("[^"]+",\s*PartyRole\.(?P<primary>\w+),\s*PartyRole\.(?P<secondary>\w+),\s*EngagementStyle\.\w+,\s*(?P<tank>\d+),\s*(?P<damage>\d+),\s*(?P<support>\d+),\s*(?P<healer>\d+),\s*(?P<control>\d+)\)'
    )
    for match in pattern.finditer(source):
        result[match.group("name")] = (
            match.group("primary"),
            match.group("secondary"),
            [int(match.group(key)) for key in ("tank", "damage", "support", "healer", "control")],
        )
    return result


def extract_explicit_expansion_profiles(source: str) -> dict[str, tuple[str, str, list[int]]]:
    result: dict[str, tuple[str, str, list[int]]] = {}
    pattern = re.compile(
        r'E\("(?P<name>[^"]+)",\s*[^,]+,\s*"[^"]+",\s*PartyRole\.(?P<primary>\w+),\s*PartyRole\.(?P<secondary>\w+),\s*EngagementStyle\.\w+,\s*(?P<tank>\d+),\s*(?P<damage>\d+),\s*(?P<support>\d+),\s*(?P<healer>\d+),\s*(?P<control>\d+)\)'
    )
    for match in pattern.finditer(source):
        result[match.group("name")] = (
            match.group("primary"),
            match.group("secondary"),
            [int(match.group(key)) for key in ("tank", "damage", "support", "healer", "control")],
        )
    return result


def extract_completion_profiles(source: str) -> dict[str, tuple[str, str, list[int]]]:
    result: dict[str, tuple[str, str, list[int]]] = {}
    pattern = re.compile(
        r'\["(?P<name>[^"]+)"\]\s*=\s*R\(PartyRole\.(?P<primary>\w+),\s*PartyRole\.(?P<secondary>\w+),\s*EngagementStyle\.\w+,\s*(?P<tank>\d+),\s*(?P<damage>\d+),\s*(?P<support>\d+),\s*(?P<healer>\d+),\s*(?P<control>\d+)\)'
    )
    for match in pattern.finditer(source):
        result[match.group("name")] = (
            match.group("primary"),
            match.group("secondary"),
            [int(match.group(key)) for key in ("tank", "damage", "support", "healer", "control")],
        )
    return result


def validate_profile(name: str, primary: str, secondary: str, affinities: list[int]) -> list[str]:
    issues: list[str] = []
    role_index = {"Tank": 0, "Damage": 1, "Support": 2, "Healer": 3, "Control": 4}
    if primary == secondary:
        issues.append(f"{name}: primary == secondary ({primary})")
    if primary not in role_index or secondary not in role_index:
        issues.append(f"{name}: invalid role")
        return issues
    if any(value < 0 or value > 5 for value in affinities):
        issues.append(f"{name}: affinity outside 0..5")
    primary_value = affinities[role_index[primary]]
    secondary_value = affinities[role_index[secondary]]
    if primary_value < 4:
        issues.append(f"{name}: primary affinity {primary_value} < 4")
    if secondary_value < 2:
        issues.append(f"{name}: secondary affinity {secondary_value} < 2")
    if primary_value < secondary_value:
        issues.append(f"{name}: secondary affinity exceeds primary")
    if affinities.count(5) > 2:
        issues.append(f"{name}: more than two affinities at 5")
    return issues


try:
    project = text("TeamUp.csproj")
    config = text("ModConfig.cs")
    authority = text("Combat/SignatureAuthorityService.cs")
    integrity = text("Core/CombatRosterIntegrityService.cs")
    coverage = text("Core/CombatKitCoverageService.cs")
    core_profiles_src = text("Core/NpcProfileCatalog.cs")
    expansion_profiles_src = text("Core/ExpansionNpcProfileCatalog.cs")
    completion_src = text("Core/ExpansionRosterCompletion.cs")
    identities_src = text("Core/CharacterSkillIdentityCatalog.cs")
    expansion_skills_src = text("Combat/ExpansionSkillService.cs")
    full_skills_src = text("Combat/ExpansionSkillService.FullRoster.cs")
    tuning_src = text("Combat/ExpansionSkillService.IdentityBalance.cs")
    special_src = text("Combat/SpecialRecruitCombatService.cs")
    polish_src = text("Combat/Alpha6CombatPolishService.cs")
    combat_src = text("Combat/CombatService.cs")
    alpha674 = text("ModEntry.Alpha674.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    follow = text("Following/FollowService.cs")
    codex = text("UI/CodexBrowserMenu.cs")
    profile_ui = text("UI/CharacterProfileMenu.cs")
    rank_src = text("Core/CombatRankCatalog.cs")

    require(f"<Version>{VERSION}</Version>" in project, "Wrong project version")
    require("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person party cap regression")
    require("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2-companion cap regression")

    # 6.7.4 signature authority.
    prototype_names = {"Abigail", "Alex", "Harvey", "Maru", "Emily"}
    authority_names = set(re.findall(r'^\s*"([A-Za-z]+)",?\s*$', authority, re.MULTILINE))
    require(prototype_names <= authority_names, f"Signature authority missing prototype(s): {sorted(prototype_names - authority_names)}")
    require("Alpha6PrototypeNames" not in coverage, "Duplicate prototype owner table remains in CombatKitCoverageService")
    require("SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner(characterName)" in coverage, "Coverage does not use signature authority")
    require(combat_src.count("SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner(member.CharacterName)") >= 2, "CombatService must gate attack + recovery legacy signatures")
    for name in prototype_names:
        require(f'"{name}" => Try{name}Upgrade' in polish_src, f"Alpha6CombatPolish lost {name} authoritative signature")

    require("CombatRosterIntegrityService" in integrity, "Static roster integrity service missing")
    require("teamup_roster_static_audit" in alpha674, "Static roster audit console command missing")
    require("EnsureAlpha674RosterIntegrityRegistered();" in alpha6625, "Alpha 6.7.4 registration missing")

    # Parse known catalog shape and completion parity.
    core_profiles = extract_direct_profiles(core_profiles_src)
    explicit_expansion = extract_explicit_expansion_profiles(expansion_profiles_src)
    placeholders = set(re.findall(r'X\("([^"]+)"', expansion_profiles_src))
    completion_profiles = extract_completion_profiles(completion_src)
    require(placeholders == set(completion_profiles),
            f"Expansion placeholder/completion mismatch; missing={sorted(placeholders - set(completion_profiles))}; orphan={sorted(set(completion_profiles) - placeholders)}")

    all_profiles: dict[str, tuple[str, str, list[int]]] = {}
    all_profiles.update(core_profiles)
    all_profiles.update(explicit_expansion)
    all_profiles.update(completion_profiles)
    profile_issues: list[str] = []
    for name, (primary, secondary, affinities) in sorted(all_profiles.items()):
        profile_issues.extend(validate_profile(name, primary, secondary, affinities))
    require(not profile_issues, "Profile balance audit failed: " + " | ".join(profile_issues))

    # Combat kit coverage: core profiles are prototype-authority or CharacterSkillIdentity.
    identity_names = set(re.findall(r'\["([^"]+)"\]\s*=\s*I\(', identities_src))
    # Constants aren't captured by regex; MiMi/Sudoku handled separately below.
    core_missing = sorted(set(core_profiles) - prototype_names - identity_names)
    require(not core_missing, f"Core profiles missing combat kit: {core_missing}")

    wave1_skill_names = set(re.findall(r'\["([^"]+)"\]\s*=\s*S\(', expansion_skills_src))
    full_skill_names = set(re.findall(r'Skills\["([^"]+)"\]\s*=\s*S\(', full_skills_src))
    expansion_skill_names = wave1_skill_names | full_skill_names
    special_expansion = {"Marlon", "Henchman"}
    expansion_names = set(explicit_expansion) | placeholders
    expansion_missing = sorted(expansion_names - expansion_skill_names - special_expansion)
    require(not expansion_missing, f"Expansion profiles missing combat kit: {expansion_missing}")

    require("CustomNpcCompatibilityService.MimiNpcId" in identities_src, "MiMi base signature identity missing")
    require('new(CombatRank.S, RecruitBadge.Boss | RecruitBadge.Special)' in rank_src, "MiMi S/BOSS/SPECIAL rank regression")
    require("SudokuCanonicalNpcId" in identities_src and '["Sudoku"] = I(' in identities_src, "Sudoku signature identity/alias missing")
    require('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank_src, "Marlon S LEGENDARY regression")
    require('["Henchman"] = new(CombatRank.B, RecruitBadge.Special)' in rank_src, "Henchman SPECIAL regression")

    # Full-roster identity balance must match exactly the full-roster skill rows.
    tuning_rows = {
        match.group(1): tuple(int(match.group(i)) for i in range(2, 6))
        for match in re.finditer(r'\["([^"]+)"\]\s*=\s*T\((-?\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+)\)', tuning_src)
    }
    require(full_skill_names == set(tuning_rows),
            f"Full-roster skill/tuning mismatch; no_tuning={sorted(full_skill_names - set(tuning_rows))}; orphan_tuning={sorted(set(tuning_rows) - full_skill_names)}")
    tuning_issues: list[str] = []
    for name, values in sorted(tuning_rows.items()):
        if sum(values) != 0:
            tuning_issues.append(f"{name}: tuning sum={sum(values)}")
        if any(value < -2 or value > 2 for value in values):
            tuning_issues.append(f"{name}: tuning axis outside -2..2")
    require(not tuning_issues, "Identity tuning audit failed: " + " | ".join(tuning_issues))

    # i18n parity and every catalog profile's passive/ability keys.
    default_json = json.loads((SRC / "i18n" / "default.json").read_text(encoding="utf-8"))
    vi_json = json.loads((SRC / "i18n" / "vi.json").read_text(encoding="utf-8"))
    require(set(default_json) == set(vi_json), "default/vi i18n key sets differ")
    missing_i18n: list[str] = []
    for name in sorted(core_profiles):
        key = name.lower()
        for suffix in ("passive", "ability"):
            wanted = f"codex.{key}.{suffix}"
            if wanted not in default_json:
                missing_i18n.append(wanted)
    for name in sorted(expansion_names):
        key = name.lower()
        for suffix in ("passive", "ability"):
            wanted = f"codex.expansion.{key}.{suffix}"
            if wanted not in default_json:
                missing_i18n.append(wanted)
    for wanted in [
        "codex.custom.mimi.passive", "codex.custom.mimi.ability",
        "codex.custom.sudoku.passive", "codex.custom.sudoku.ability",
    ]:
        if wanted not in default_json:
            missing_i18n.append(wanted)
    require(not missing_i18n, f"Missing i18n combat profile keys: {missing_i18n}")

    # Preserve hard-earned regressions.
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    require("PelipperRenderSuppressedAlpha6613" not in all_source, "Legacy Pelipper render suppression returned")
    require("TrySetActorInvisibleAlpha6613" not in all_source, "Legacy Pelipper visibility writer returned")
    require("isTileLocationTotallyClearAndPlaceable" not in follow, "Follow water/path regression returned")
    require("isTileLocationTotallyClearAndPlaceable" not in combat_src, "Combat water/path regression returned")
    require("private const int CombatPathRetryCooldownTicks = 24;" in combat_src, "Combat path retry cadence changed")
    require("private const int CombatMovementPulseTicks = 3;" in combat_src, "Combat movement pulse changed")
    require("UnlockVanillaMovementAnimation(npc);" in follow, "6.7.2 Gus animation unlock regression")
    require("npc.Sprite.ignoreStopAnimation = false;" in follow, "Gus ignoreStopAnimation regression")
    require("hotPlateColor" not in combat_src, "Accidental Gus custom visual returned")
    require("Math.Min(1739, Game1.uiViewport.Width - 12)" in codex, "Codex width regression")
    require("Math.Min(1518, Game1.uiViewport.Width - 16)" in profile_ui, "Profile width regression")
    require("MimiTrueFormDurationTicks = 240" in special_src, "MiMi 4-second TRUE FORM regression")
    require("MimiTrueFormCooldownTicks = 6000" in special_src, "MiMi TRUE FORM cooldown regression")
    require("MONSTER HUNTER" in special_src, "Marlon special regression")
    require("VOID MAYO SPLASH" in special_src, "Henchman special regression")

    known_profile_rows = len(all_profiles) + 3  # MiMi + Sudoku canonical + Sudoku runtime alias.
    audit_lines.extend([
        f"- Core direct profiles: **{len(core_profiles)}**",
        f"- Expansion profiles: **{len(expansion_names)}**",
        f"- Custom profile rows: **3** (MiMi, Sudoku canonical, Sudoku alias)",
        f"- Total known profile rows represented: **{known_profile_rows}**",
        f"- Expansion full-roster skills with zero-sum tuning: **{len(full_skill_names)}**",
        "- Prototype signature authority: **Abigail, Alex, Harvey, Maru, Emily → Alpha6CombatPolishService only**",
        "- Profile balance rules: **PASS**",
        "- Combat kit coverage: **PASS**",
        "- i18n default/vi key parity: **PASS**",
        "- Rank path coverage: **PASS**",
        "",
        "## Important",
        "This is a source/CI audit. Alpha 6.7.3/6.7.4 still require later in-game validation when a test machine is available.",
    ])

    log("SOURCE ACCEPTANCE: PASS")
    log(f"ROSTER STATIC AUDIT: PASS ({known_profile_rows} known profile rows)")
    log("Building Alpha 6.7.4...")
    proc = subprocess.run(
        ["dotnet", "build", str(SRC / "TeamUp.csproj"), "-c", "Release", "--nologo"],
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    print(proc.stdout, end="")
    lines.append(proc.stdout.rstrip())
    require(proc.returncode == 0, "dotnet build failed")
    require("0 Warning(s)" in proc.stdout, "Build has warnings")
    require("0 Error(s)" in proc.stdout, "Build has errors")

    dll = SRC / "bin" / "Release" / "net6.0" / "TeamUp.dll"
    require(dll.exists(), "TeamUp.dll missing")
    blob = dll.read_bytes()
    for token in [
        "SignatureAuthorityService",
        "CombatRosterIntegrityService",
        "teamup_roster_static_audit",
        "CombatRankCatalog",
        "SpecialRecruitCombatService",
        "TRUE FORM",
        "MONSTER HUNTER",
        "VOID MAYO SPLASH",
        "CORPORATE LEVERAGE",
        "UnlockVanillaMovementAnimation",
    ]:
        require(dll_contains(blob, token), f"DLL missing token: {token}")
    for forbidden in ["PelipperRenderSuppressedAlpha6613", "TrySetActorInvisibleAlpha6613", "hotPlateColor"]:
        require(not dll_contains(blob, forbidden), f"Forbidden DLL token remains: {forbidden}")

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
        for path in sorted(MOD_STAGE.rglob("*")):
            if path.is_file():
                zf.write(path, path.relative_to(STAGE))

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")
    AUDIT.write_text("\n".join(audit_lines) + "\n", encoding="utf-8")
    SMOKE.write_text(
        "TEAM UP v0.2.0-alpha.6.7.4 - LATER LIVE TEST CHECKLIST\n"
        "======================================================\n\n"
        "Bản này đã qua static roster/balance CI nhưng CHƯA live-test.\n\n"
        "Khi có máy test lại:\n"
        "1) Abigail/Alex/Harvey/Maru/Emily: chỉ một signature nâng cấp được kích, không double-signature.\n"
        "2) teamup_roster_static_audit -> PASS.\n"
        "3) teamup_roster_audit trong save có SVE/RSV/Cardcha -> không WARNING bất ngờ.\n"
        "4) MiMi S/BOSS/SPECIAL; Marlon S/LEGENDARY; Sudoku A/SPECIAL; Henchman B/SPECIAL.\n"
        "5) Party 5 người tổng, companion 2/2, Pelipper không vượt quota.\n"
        "6) Gus vẫn quay hướng/walk cycle vanilla bình thường.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.4")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
    if not AUDIT.exists() and audit_lines:
        AUDIT.write_text("\n".join(audit_lines) + "\n", encoding="utf-8")
