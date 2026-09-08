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
STAGE = ROOT / "_stage_alpha678"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA678.txt"
AUDIT = ROOT / "PARTY_CHEMISTRY_AUDIT_ALPHA678.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_8_PARTY_CHEMISTRY_VI.txt"
VERSION = "0.2.0-alpha.6.7.8"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.8_PARTY_CHEMISTRY_AUDIT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.8_PARTY_CHEMISTRY_AUDIT_TEST.sha256.txt"

lines: list[str] = []
audit_lines: list[str] = ["# Team Up Alpha 6.7.8 Party Chemistry Audit", ""]


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
    service = text("Core/PartyBanterService.cs")
    memory = text("Core/BanterMemoryTracker.cs")
    chemistry = text("Core/PartyChemistryCatalog.cs")
    pair_catalog = text("Core/BanterContentCatalog.cs")
    context_catalog = text("Core/ContextBanterCatalog.cs")
    alpha678 = text("ModEntry.Alpha678.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    combat = text("Combat/CombatService.cs")
    follow = text("Following/FollowService.cs")
    special = text("Combat/SpecialRecruitCombatService.cs")
    rank = text("Core/CombatRankCatalog.cs")

    require(f"<Version>{VERSION}</Version>" in project, "Wrong Alpha 6.7.8 project version")
    require("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person party cap regression")
    require("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2-companion cap regression")

    # Preserve authored banter layers from 6.7.5-6.7.7.
    pair_count = len(re.findall(r'P\("pair:[^"]+"', pair_catalog))
    ship_count = len(re.findall(r'S\("ship:[^"]+"', pair_catalog))
    context_count = len(re.findall(r'C\("ctx:[^"]+"', context_catalog))
    require(pair_count >= 28, f"6.7.5 pair scripts regressed: {pair_count}")
    require(ship_count >= 8, f"6.7.5 MiMi shipping scripts regressed: {ship_count}")
    require(context_count >= 37, f"6.7.6 context scripts regressed: {context_count}")
    require("internal sealed class BanterMemoryTracker" in memory, "6.7.7 BanterMemoryTracker missing")
    require("MaxRecentExchangeIds = 12" in memory and "MaxRecentSpeakers = 8" in memory, "6.7.7 memory bounds regressed")
    require("BanterMemory.Record(" in service and "BanterMemory.Score(" in service, "6.7.7 memory integration regressed")

    # Chemistry vocabulary and catalog shape.
    required_types = [
        "Neutral", "Friends", "Rivals", "Family", "Mentor", "Awkward",
        "Protective", "Respectful", "ShipperTarget",
    ]
    require("[Flags]" in chemistry and "internal enum PartyChemistryType" in chemistry, "Chemistry flags enum missing")
    for chemistry_type in required_types:
        require(chemistry_type in chemistry, f"Chemistry type missing: {chemistry_type}")

    rows = re.findall(
        r'P\("(?P<a>[^"]+)",\s*"(?P<b>[^"]+)",\s*(?P<types>PartyChemistryType\.[^\n]+)\)',
        chemistry,
    )
    require(len(rows) >= 30, f"Expected >=30 authored chemistry pairs, got {len(rows)}")
    seen: set[str] = set()
    duplicate_pairs: list[str] = []
    self_pairs: list[str] = []
    type_usage: set[str] = set()
    for a, b, types in rows:
        key = pair_key(a, b)
        if key in seen:
            duplicate_pairs.append(f"{a}/{b}")
        seen.add(key)
        if a.lower() == b.lower():
            self_pairs.append(a)
        type_usage.update(re.findall(r"PartyChemistryType\.([A-Za-z]+)", types))
    require(not duplicate_pairs, "Duplicate chemistry pairs: " + ", ".join(duplicate_pairs))
    require(not self_pairs, "Self chemistry pairs: " + ", ".join(self_pairs))
    for authored_type in ["Friends", "Rivals", "Family", "Mentor", "Awkward", "Protective", "Respectful"]:
        require(authored_type in type_usage, f"No authored pair uses chemistry type: {authored_type}")

    # ShipperTarget must be derived from the existing MiMi authored catalog, not a duplicated static list.
    require("BanterContentCatalog.TryGetShippingPair(a, b, out _)" in chemistry, "ShipperTarget is not derived from MiMi ship catalog")
    require("chemistry |= PartyChemistryType.ShipperTarget" in chemistry, "ShipperTarget overlay missing")

    # Chemistry guides, but does not hard-control, pair selection.
    require("PartyChemistryCatalog.SelectionBias(" in service, "Chemistry selection bias not integrated")
    require("memoryScore + chemistryBias" in service, "Memory + chemistry score composition missing")
    require("Math.Max(-18, bias)" in chemistry, "Chemistry bias bound missing")
    require("RecentExchangePenalty = 100" in memory, "Memory dominance guard regressed")
    require(".OrderBy(candidate => candidate.SelectionScore)" in service, "Combined pair ranking missing")

    # Authored 6.7.5 pair dialogue remains first priority; chemistry only supplies a typed fallback tone.
    pair_lookup_pos = service.find("BanterContentCatalog.TryGetPair")
    chemistry_lookup_pos = service.find("PartyChemistryCatalog.Resolve")
    require(pair_lookup_pos >= 0 and chemistry_lookup_pos > pair_lookup_pos, "Chemistry displaced authored pair-script priority")
    require("PartyChemistryCatalog.TryBuildAmbientLines(" in service, "Chemistry ambient tone builder not integrated")
    require('"chem:{chemistryToneId}' in service, "Chemistry exchange identity missing")

    # Runtime inspection command and registration.
    require("teamup_chemistry" in alpha678, "Party Chemistry command missing")
    require("PartyChemistryCatalog.Describe(args[0], args[1])" in alpha678, "Chemistry pair inspector missing")
    require("EnsureAlpha678PartyChemistryRegistered();" in alpha6625, "Alpha 6.7.8 registration missing")

    # Chemistry is presentation metadata only. Never mutate real social/gameplay state.
    chemistry_surface = chemistry + "\n" + alpha678
    for forbidden in [
        "friendshipData", ".spouse", "dating", "controller =", "temporaryController =", ".Halt()",
        "CurrentHealth =", "damageMonster(", "changeSchedule", "schedule =", "money =", "addItem",
        "WriteSave", "SaveData", "modData[",
    ]:
        require(forbidden not in chemistry_surface, f"Party Chemistry cosmetic-only contract violated: {forbidden}")
    require("showTextAboveHead" in service, "Banter presentation must remain non-blocking overhead bubbles")

    # Locked regression wall.
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    require("PelipperRenderSuppressedAlpha6613" not in all_source, "Legacy Pelipper render suppression returned")
    require("TrySetActorInvisibleAlpha6613" not in all_source, "Legacy Pelipper visibility writer returned")
    require("isTileLocationTotallyClearAndPlaceable" not in follow, "Follow water/path regression returned")
    require("isTileLocationTotallyClearAndPlaceable" not in combat, "Combat water/path regression returned")
    require("private const int CombatPathRetryCooldownTicks = 24;" in combat, "Combat path retry cadence changed")
    require("private const int CombatMovementPulseTicks = 3;" in combat, "Combat movement pulse changed")
    require("UnlockVanillaMovementAnimation(npc);" in follow, "Gus animation unlock regression")
    require(re.search(r"MimiTrueFormDurationTicks\s*=\s*240", special) is not None, "MiMi TRUE FORM duration regression")
    require(re.search(r"MimiTrueFormCooldownTicks\s*=\s*6000", special) is not None, "MiMi TRUE FORM cooldown regression")
    require('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank, "Marlon S rank regression")
    require("RecruitBadge.Boss | RecruitBadge.Special" in rank, "MiMi S/BOSS/SPECIAL rank regression")
    require("SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner" in combat, "6.7.4 signature authority regression")

    audit_lines.extend([
        f"- Authored chemistry pairs: **{len(rows)}**",
        "- Chemistry vocabulary: **Friends / Rivals / Family / Mentor / Awkward / Protective / Respectful / ShipperTarget / Neutral**",
        f"- Preserved authored ambient pairs: **{pair_count}**",
        f"- Preserved MiMi shipping pairs: **{ship_count}**",
        f"- Preserved context scripts: **{context_count}**",
        "- ShipperTarget source of truth: **existing MiMi shipping catalog**",
        "- Chemistry selection bias bounded to **-18**, below 6.7.7 recent-exchange penalty **100**",
        "- Authored pair dialogue priority before chemistry fallback: **PASS**",
        "- Cosmetic-only contract: **PASS**",
        "- Core combat/path/Pelipper regression wall: **PASS**",
        "",
        "## Important",
        "This build is source/CI validated only. Chemistry tone, pair frequency and subjective social feel still need later in-game validation.",
    ])

    log("SOURCE ACCEPTANCE: PASS")
    log(f"PARTY CHEMISTRY STATIC AUDIT: PASS ({len(rows)} authored relationships, {len(required_types)} types)")
    log("Building Alpha 6.7.8...")
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
        "PartyChemistryCatalog", "PartyChemistryType", "teamup_chemistry", "ShipperTarget", "chem:",
        "BanterMemoryTracker", "ContextBanterCatalog", "BanterContentCatalog", "SignatureAuthorityService",
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
        "TEAM UP v0.2.0-alpha.6.7.8 - PARTY CHEMISTRY LIVE TEST CHECKLIST\n"
        "================================================================\n\n"
        "Bản này đã qua chemistry/static/compile CI nhưng CHƯA live-test.\n\n"
        "Khi có máy test lại:\n"
        "1) teamup_chemistry status -> thấy số cặp chemistry và danh sách type.\n"
        "2) teamup_chemistry Alex Sebastian -> Rivals, Awkward.\n"
        "3) teamup_chemistry Robin Sebastian -> Family, Protective.\n"
        "4) Cặp có authored 6.7.5 dialogue vẫn dùng câu riêng trước chemistry fallback.\n"
        "5) Cặp chemistry không có authored pair script -> tone Friends/Family/Rivals/... xuất hiện tự nhiên.\n"
        "6) 6.7.7 memory vẫn thắng recency: một cặp chemistry không được chiếm mic liên tục.\n"
        "7) MiMi ship pair có thêm ShipperTarget metadata nhưng không sửa quan hệ thật.\n"
        "8) Không đổi friendship/dating/spouse/save; party 5 người, companion 2/2, Pelipper/Gus/combat không regression.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.8")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
    if not AUDIT.exists() and audit_lines:
        AUDIT.write_text("\n".join(audit_lines) + "\n", encoding="utf-8")
