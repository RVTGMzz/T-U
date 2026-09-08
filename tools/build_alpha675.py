from __future__ import annotations

import hashlib
import re
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha675"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA675.txt"
AUDIT = ROOT / "BANTER_AUDIT_ALPHA675.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_5_BANTER_DEPTH_VI.txt"
VERSION = "0.2.0-alpha.6.7.5"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.5_BANTER_DEPTH_AUDIT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.5_BANTER_DEPTH_AUDIT_TEST.sha256.txt"

lines: list[str] = []
audit_lines: list[str] = ["# Team Up Alpha 6.7.5 Banter Audit", ""]


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


def pair_key(a: str, b: str) -> str:
    return "|".join(sorted((a.lower(), b.lower())))


try:
    project = text("TeamUp.csproj")
    config = text("ModConfig.cs")
    banter = text("Core/PartyBanterService.cs")
    catalog = text("Core/BanterContentCatalog.cs")
    alpha675 = text("ModEntry.Alpha675.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    combat = text("Combat/CombatService.cs")
    follow = text("Following/FollowService.cs")
    special = text("Combat/SpecialRecruitCombatService.cs")
    rank = text("Core/CombatRankCatalog.cs")
    core_profiles = text("Core/NpcProfileCatalog.cs")
    expansion_profiles = text("Core/ExpansionNpcProfileCatalog.cs")

    require(f"<Version>{VERSION}</Version>" in project, "Wrong project version")
    require("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person party cap regression")
    require("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2-companion cap regression")
    require("SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner" in combat, "6.7.4 signature authority regression")
    require((SRC / "Core" / "CombatRosterIntegrityService.cs").exists(), "6.7.4 roster integrity service missing")
    require("EnsureAlpha675BanterCatalogRegistered();" in alpha6625, "6.7.5 registration missing")
    require("teamup_banter_catalog_audit" in alpha675, "6.7.5 console audit command missing")

    # Authored pair scripts: parse literal catalog rows and validate parity/shape without a game runtime.
    pair_pattern = re.compile(
        r'P\("(?P<id>pair:[^"]+)",\s*"(?P<a>[^"]+)",\s*"(?P<b>[^"]+)",\s*"(?P<lead>[^"]+)",\s*'
        r'"(?P<vi1>[^"]*)",\s*"(?P<en1>[^"]*)",\s*"(?P<vi2>[^"]*)",\s*"(?P<en2>[^"]*)"\)',
        re.DOTALL,
    )
    ship_pattern = re.compile(
        r'S\("(?P<id>ship:[^"]+)",\s*"(?P<a>[^"]+)",\s*"(?P<b>[^"]+)",\s*'
        r'"(?P<vi1>[^"]*)",\s*"(?P<en1>[^"]*)",\s*"(?P<vi2>[^"]*)",\s*"(?P<en2>[^"]*)"\)',
        re.DOTALL,
    )
    pair_rows = [m.groupdict() for m in pair_pattern.finditer(catalog)]
    ship_rows = [m.groupdict() for m in ship_pattern.finditer(catalog)]
    require(len(pair_rows) >= 28, f"Expected >=28 authored pair scripts, got {len(pair_rows)}")
    require(len(ship_rows) >= 8, f"Expected >=8 MiMi shipping scripts, got {len(ship_rows)}")

    known_names = set(re.findall(r'\["([^"]+)"\]\s*=\s*P\(', core_profiles))
    known_names |= set(re.findall(r'E\("([^"]+)"', expansion_profiles))
    known_names |= set(re.findall(r'X\("([^"]+)"', expansion_profiles))
    # Custom/special rows that are constructed via constants or dedicated helpers.
    known_names |= {"Marlon", "Morris", "Henchman", "Sudoku", "ronvotri.HeyYoureCursed_Sudoku", "Ronvotri.Cardcha_MiMi"}

    ids: set[str] = set()
    authored_pairs: set[str] = set()
    dialogue_issues: list[str] = []
    for row in pair_rows:
        if row["id"].lower() in ids:
            dialogue_issues.append(f"duplicate id {row['id']}")
        ids.add(row["id"].lower())
        key = pair_key(row["a"], row["b"])
        if key in authored_pairs:
            dialogue_issues.append(f"duplicate pair {row['a']}/{row['b']}")
        authored_pairs.add(key)
        if row["lead"].lower() not in {row["a"].lower(), row["b"].lower()}:
            dialogue_issues.append(f"{row['id']}: lead outside pair")
        for name in (row["a"], row["b"]):
            if name not in known_names:
                dialogue_issues.append(f"{row['id']}: unknown roster NPC {name}")
        for key_name in ("vi1", "en1", "vi2", "en2"):
            value = row[key_name].strip()
            if not value:
                dialogue_issues.append(f"{row['id']}: empty {key_name}")
            if len(value) > 120:
                dialogue_issues.append(f"{row['id']}: {key_name} exceeds 120 chars")
            if "\\n" in value or "\\r" in value:
                dialogue_issues.append(f"{row['id']}: line break in {key_name}")

    for row in ship_rows:
        if row["id"].lower() in ids:
            dialogue_issues.append(f"duplicate id {row['id']}")
        ids.add(row["id"].lower())
        for name in (row["a"], row["b"]):
            if name not in known_names:
                dialogue_issues.append(f"{row['id']}: unknown roster NPC {name}")
        for key_name in ("vi1", "en1", "vi2", "en2"):
            value = row[key_name].strip()
            if not value:
                dialogue_issues.append(f"{row['id']}: empty {key_name}")
            if len(value) > 120:
                dialogue_issues.append(f"{row['id']}: {key_name} exceeds 120 chars")
            if "\\n" in value or "\\r" in value:
                dialogue_issues.append(f"{row['id']}: line break in {key_name}")

    require(not dialogue_issues, "Banter catalog audit failed: " + " | ".join(dialogue_issues))
    require("BanterContentCatalog.TryGetPair" in banter, "PartyBanterService does not use authored pair catalog")
    require("BanterContentCatalog.TryGetShippingPair" in banter, "MiMi shipping does not use authored ship catalog")
    require("if (pair == BuildPairKey" not in banter, "Legacy hardcoded pair ladder still remains")
    require('"Morris", "Marlon"' in banter, "Marlon male fallback missing")
    require("showTextAboveHead" in banter, "Non-blocking overhead speech rendering missing")

    # Banter must remain presentation-only. No controller, schedule, combat, friendship or romance mutation.
    banter_surface = catalog + "\n" + banter + "\n" + alpha675
    for forbidden in [
        "friendshipData", ".spouse", "dating", "controller =", "temporaryController =", ".Halt()",
        "CurrentHealth =", "damageMonster(", "changeSchedule", "schedule =",
    ]:
        require(forbidden not in banter_surface, f"Banter presentation-only contract violated by token: {forbidden}")

    # Preserve key regressions while no live-test machine is available.
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    require("PelipperRenderSuppressedAlpha6613" not in all_source, "Legacy Pelipper render suppression returned")
    require("TrySetActorInvisibleAlpha6613" not in all_source, "Legacy Pelipper visibility writer returned")
    require("isTileLocationTotallyClearAndPlaceable" not in follow, "Follow water/path regression returned")
    require("isTileLocationTotallyClearAndPlaceable" not in combat, "Combat water/path regression returned")
    require("private const int CombatPathRetryCooldownTicks = 24;" in combat, "Combat path retry cadence changed")
    require("private const int CombatMovementPulseTicks = 3;" in combat, "Combat movement pulse changed")
    require("UnlockVanillaMovementAnimation(npc);" in follow, "Gus animation unlock regression")
    require("MimiTrueFormDurationTicks = 240" in special, "MiMi TRUE FORM duration regression")
    require("MimiTrueFormCooldownTicks = 6000" in special, "MiMi TRUE FORM cooldown regression")
    require('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank, "Marlon S rank regression")
    require("RecruitBadge.Boss | RecruitBadge.Special" in rank, "MiMi S/BOSS/SPECIAL rank regression")

    audit_lines.extend([
        f"- Authored ambient pair scripts: **{len(pair_rows)}**",
        f"- Authored MiMi male/male shipping scripts: **{len(ship_rows)}**",
        f"- Unique dialogue IDs: **{len(ids)}**",
        "- VI/EN line presence + 120-character overhead bubble guard: **PASS**",
        "- Pair roster references: **PASS**",
        "- Duplicate pair/ID detection: **PASS**",
        "- Presentation-only contract (no friendship/romance/movement/combat mutation): **PASS**",
        "- Existing 6.7.4 roster/signature authority preserved: **PASS**",
        "",
        "## Important",
        "This build is source/CI validated only. Runtime timing, bubble overlap and subjective dialogue feel still need later in-game validation.",
    ])

    log("SOURCE ACCEPTANCE: PASS")
    log(f"BANTER STATIC AUDIT: PASS ({len(pair_rows)} pairs + {len(ship_rows)} MiMi ship pairs)")
    log("Building Alpha 6.7.5...")
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
    require(proc.returncode == 0, "dotnet build failed")
    require("0 Warning(s)" in proc.stdout, "Build has warnings")
    require("0 Error(s)" in proc.stdout, "Build has errors")

    dll = SRC / "bin" / "Release" / "net6.0" / "TeamUp.dll"
    require(dll.exists(), "TeamUp.dll missing")
    blob = dll.read_bytes()
    for token in [
        "BanterContentCatalog",
        "teamup_banter_catalog_audit",
        "pair:alex-sebastian",
        "pair:lance-marlon",
        "pair:jio-daia",
        "ship:marlon-wizard",
        "SignatureAuthorityService",
        "CombatRosterIntegrityService",
        "TRUE FORM",
    ]:
        require(dll_contains(blob, token), f"DLL missing token: {token}")
    for forbidden in ["PelipperRenderSuppressedAlpha6613", "TrySetActorInvisibleAlpha6613"]:
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
        "TEAM UP v0.2.0-alpha.6.7.5 - LATER LIVE TEST CHECKLIST\n"
        "==================================================\n\n"
        "Bản này đã qua static dialogue/compile CI nhưng CHƯA live-test.\n\n"
        "Khi có máy test lại:\n"
        "1) teamup_banter_catalog_audit -> PASS.\n"
        "2) teamup_banter now với các cặp authored: đúng người nói trước, câu trả lời không đảo nhân vật.\n"
        "3) MiMi + Alex + Sebastian, hoặc MiMi + Marlon + Wizard: opener/closer riêng xuất hiện tự nhiên.\n"
        "4) Banter vẫn là bubble trên đầu, không mở DialogueBox và không giật quyền follow/combat.\n"
        "5) Không spam: cooldown ambient/combat/MiMi vẫn hoạt động như 6.7.0.\n"
        "6) Party 5 người tổng, companion 2/2, Pelipper không regression.\n"
        "7) Abigail/Alex/Harvey/Maru/Emily không double-signature.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.5")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
    if not AUDIT.exists() and audit_lines:
        AUDIT.write_text("\n".join(audit_lines) + "\n", encoding="utf-8")
