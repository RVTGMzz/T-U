from __future__ import annotations

import hashlib
import re
import shutil
import subprocess
import zipfile
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha679"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA679.txt"
AUDIT = ROOT / "CHEMISTRY_VARIANTS_AUDIT_ALPHA679.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_9_CHEMISTRY_VARIANTS_VI.txt"
VERSION = "0.2.0-alpha.6.7.9"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.9_CHEMISTRY_VARIANTS_AUDIT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.9_CHEMISTRY_VARIANTS_AUDIT_TEST.sha256.txt"

lines: list[str] = []
audit_lines: list[str] = ["# Team Up Alpha 6.7.9 Chemistry Variants Audit", ""]


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
    return "|".join(sorted((a.casefold(), b.casefold())))


try:
    project = text("TeamUp.csproj")
    config = text("ModConfig.cs")
    service = text("Core/PartyBanterService.cs")
    chemistry = text("Core/PartyChemistryCatalog.cs")
    variants = text("Core/ChemistryVariantCatalog.cs")
    memory = text("Core/BanterMemoryTracker.cs")
    pair_catalog = text("Core/BanterContentCatalog.cs")
    context_catalog = text("Core/ContextBanterCatalog.cs")
    alpha679 = text("ModEntry.Alpha679.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    combat = text("Combat/CombatService.cs")
    follow = text("Following/FollowService.cs")
    special = text("Combat/SpecialRecruitCombatService.cs")
    rank = text("Core/CombatRankCatalog.cs")

    require(f"<Version>{VERSION}</Version>" in project, "Wrong Alpha 6.7.9 project version")
    require("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person party cap regression")
    require("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2-companion cap regression")

    # Preserve banter depth built in 6.7.5-6.7.8.
    pair_count = len(re.findall(r'P\("pair:[^"]+"', pair_catalog))
    ship_count = len(re.findall(r'S\("ship:[^"]+"', pair_catalog))
    context_count = len(re.findall(r'C\("ctx:[^"]+"', context_catalog))
    chemistry_pairs = re.findall(r'P\("([^"]+)",\s*"([^"]+)",\s*PartyChemistryType\.', chemistry)
    chemistry_pair_keys = {pair_key(a, b) for a, b in chemistry_pairs}
    chemistry_types = set(re.findall(r'^\s{4}(Neutral|Friends|Rivals|Family|Mentor|Awkward|Protective|Respectful|ShipperTarget)(?:\s*=|,)', chemistry, re.M))
    require(pair_count >= 28, f"Authored pair banter regressed: {pair_count}")
    require(ship_count >= 8, f"MiMi ship banter regressed: {ship_count}")
    require(context_count >= 37, f"Context banter regressed: {context_count}")
    require(len(chemistry_pairs) == 30, f"Party Chemistry pair count changed unexpectedly: {len(chemistry_pairs)}")
    require(len(chemistry_types) == 9, f"Party Chemistry vocabulary changed unexpectedly: {sorted(chemistry_types)}")

    # Variant taxonomy and exact-pair coverage.
    variant_names = set(re.findall(r'^\s{4}([A-Za-z][A-Za-z0-9]+),?$', variants.split("internal sealed record ChemistryPairVariant", 1)[0], re.M))
    require("None" in variant_names, "ChemistryVariant.None missing")
    non_none_variants = variant_names - {"None"}
    require(len(non_none_variants) >= 17, f"Chemistry variant vocabulary too shallow: {sorted(non_none_variants)}")

    variant_rows = re.findall(
        r'V\("([^"]+)",\s*"([^"]+)",\s*ChemistryVariant\.([A-Za-z0-9]+)(?:,\s*"([^"]+)")?\)',
        variants,
    )
    require(len(variant_rows) == 30, f"Expected 30 exact chemistry variant pair profiles, found {len(variant_rows)}")
    variant_pair_keys = {pair_key(a, b) for a, b, _, _ in variant_rows}
    require(len(variant_pair_keys) == 30, "Duplicate exact chemistry variant pair detected")
    require(variant_pair_keys == chemistry_pair_keys, "Chemistry variant pair set must exactly cover the 30 Party Chemistry pairs")
    for a, b, variant, preferred in variant_rows:
        require(variant in non_none_variants, f"Unknown/None exact pair variant for {a}/{b}: {variant}")
        if preferred:
            require(preferred.casefold() in {a.casefold(), b.casefold()}, f"Preferred lead {preferred} is not a member of {a}/{b}")

    # Every tone has a multi-line pool. This is the core 6.7.9 contract.
    pool_matches = re.findall(
        r'\[ChemistryVariant\.([A-Za-z0-9]+)\]\s*=\s*new\[\]\s*\{(.*?)\n\s*\},',
        variants,
        re.S,
    )
    pool_counts: dict[str, int] = {}
    for variant, body in pool_matches:
        pool_counts[variant] = len(re.findall(r'\bL\("', body))
    # Last dictionary item can be followed by } instead of }, depending formatter; catch it explicitly.
    if "ProfessionalRespect" not in pool_counts:
        match = re.search(r'\[ChemistryVariant\.ProfessionalRespect\]\s*=\s*new\[\]\s*\{(.*?)\n\s*\}\n\s*\};', variants, re.S)
        if match:
            pool_counts["ProfessionalRespect"] = len(re.findall(r'\bL\("', match.group(1)))
    require(set(pool_counts) == non_none_variants, f"Missing line pools for variants: {sorted(non_none_variants - set(pool_counts))}")
    require(all(count >= 3 for count in pool_counts.values()), f"Every chemistry variant needs >=3 line pairs: {pool_counts}")

    line_rows = re.findall(
        r'L\("([^"]+)",\s*"([^"]*)",\s*"([^"]*)",\s*"([^"]*)",\s*"([^"]*)"\)',
        variants,
    )
    line_ids = [row[0] for row in line_rows]
    require(len(line_ids) >= 51, f"Expected at least 51 bilingual chemistry line pairs, found {len(line_ids)}")
    duplicate_ids = [key for key, count in Counter(line_ids).items() if count > 1]
    require(not duplicate_ids, f"Duplicate chemistry line IDs: {duplicate_ids}")
    for line_id, vi_lead, vi_reply, en_lead, en_reply in line_rows:
        require(all(value.strip() for value in (vi_lead, vi_reply, en_lead, en_reply)), f"Empty bilingual field in chemistry line {line_id}")
        require(max(map(len, (vi_lead, vi_reply, en_lead, en_reply))) <= 125, f"Chemistry line too long for bubble ({line_id})")

    # Precedence: authored pair > exact chemistry variant pool > old chemistry generic > trait generic.
    authored_pos = service.find("BanterContentCatalog.TryGetPair")
    variant_pos = service.find("ChemistryVariantCatalog.ResolveProfile(")
    legacy_chem_pos = service.find("PartyChemistryCatalog.TryBuildAmbientLines(", variant_pos)
    generic_pos = service.find("return BuildGenericAmbientExchange(first, second, vi);", variant_pos)
    require(min(authored_pos, variant_pos, legacy_chem_pos, generic_pos) >= 0, "Banter precedence layers incomplete")
    require(authored_pos < variant_pos < legacy_chem_pos < generic_pos, "Banter precedence must be authored > variant > legacy chemistry > generic")

    # Role-aware orientation and memory-aware line selection.
    require("PreferredLeadName" in variants and "PreferredLeadName" in service, "Role-aware chemistry lead orientation missing")
    require("lead = first.Member.CharacterName.Equals(variantProfile.PreferredLeadName" in service, "Preferred chemistry speaker is not enforced")
    require("variantLines\n                    .OrderBy(line => BanterMemory.Score(" in service, "Chemistry line pool is not memory-ranked")
    require("selectedLine.Id" in service and '"chemvar:' in service, "Chemistry line identity is not recorded per selected line")
    require("int variantBias = variant == ChemistryVariant.None ? 0 : -3;" in service, "Small chemistry variant selection bias missing")
    require("RecentExchangePenalty = 100" in memory, "6.7.7 memory penalty regression")
    require("MaxRecentExchangeIds = 12" in memory and "MaxRecentSpeakers = 8" in memory, "6.7.7 bounded memory regression")

    # Runtime inspector and registration.
    require("teamup_chemistry_variant" in alpha679, "Chemistry variant debug command missing")
    require("linePool=" in alpha679 and "preferredLead=" in alpha679, "Chemistry variant inspector lacks useful detail")
    require("EnsureAlpha679ChemistryVariantsRegistered();" in alpha6625, "Alpha 6.7.9 registration missing")

    # Cosmetic-only wall. Match actual mutations, not harmless explanatory words in comments.
    chemistry_surface = variants + "\n" + alpha679
    for forbidden in [
        "friendshipData[", ".spouse =", "dating =", "controller =", "temporaryController =", ".Halt()",
        "CurrentHealth =", "damageMonster(", "changeSchedule", "schedule =", "money =", "addItem(",
        "WriteSave", "SaveData", "modData[",
    ]:
        require(forbidden not in chemistry_surface, f"Chemistry Variants cosmetic-only contract violated: {forbidden}")
    require("showTextAboveHead" in service, "Banter presentation must remain overhead bubble only")

    # Locked gameplay regression wall while live testing is unavailable.
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    require("PelipperRenderSuppressedAlpha6613" not in all_source, "Legacy Pelipper render suppression returned")
    require("TrySetActorInvisibleAlpha6613" not in all_source, "Legacy Pelipper visibility writer returned")
    require("isTileLocationTotallyClearAndPlaceable" not in follow, "Follow water/path regression returned")
    require("isTileLocationTotallyClearAndPlaceable" not in combat, "Combat water/path regression returned")
    require("private const int CombatPathRetryCooldownTicks = 24;" in combat, "Combat path retry cadence changed")
    require("private const int CombatMovementPulseTicks = 3;" in combat, "Combat movement pulse changed")
    require("UnlockVanillaMovementAnimation(npc);" in follow, "6.7.2 vanilla animation unlock regression")
    require(re.search(r"MimiTrueFormDurationTicks\s*=\s*240", special) is not None, "MiMi TRUE FORM duration regression")
    require(re.search(r"MimiTrueFormCooldownTicks\s*=\s*6000", special) is not None, "MiMi TRUE FORM cooldown regression")
    require('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank, "Marlon S rank regression")
    require("RecruitBadge.Boss | RecruitBadge.Special" in rank, "MiMi S/BOSS/SPECIAL rank regression")
    require("SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner" in combat, "6.7.4 signature authority regression")

    audit_lines.extend([
        f"- Party Chemistry exact relationships preserved: **{len(chemistry_pairs)}**",
        f"- Chemistry tone variants: **{len(non_none_variants)}**",
        f"- Exact pair variant profiles: **{len(variant_rows)} / 30**",
        f"- Bilingual variant line pairs: **{len(line_ids)}**",
        "- Minimum lines per variant: **3**",
        "- Preferred lead validation for parent/grandparent/mentor/protective relationships: **PASS**",
        "- Authored pair > variant pool > legacy chemistry > generic precedence: **PASS**",
        "- Banter Memory chooses the least-recent variant line before random tie-break: **PASS**",
        "- Variant pair selection bias is only -3 vs recent exchange penalty +100: **PASS**",
        "- Cosmetic-only + gameplay regression wall: **PASS**",
        "",
        "## Important",
        "This build is source/CI verified only. Tone quality, bubble pacing and preferred-speaker feel still need later in-game validation.",
    ])

    log("SOURCE ACCEPTANCE: PASS")
    log(f"CHEMISTRY VARIANTS STATIC AUDIT: PASS ({len(variant_rows)} pair profiles, {len(non_none_variants)} variants, {len(line_ids)} bilingual line pairs)")
    log("Building Alpha 6.7.9...")
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
        "ChemistryVariantCatalog", "ChemistryPairVariant", "ChemistryVariantLine",
        "EasygoingFriends", "FriendlyRivalry", "SharpRivalry", "ParentChild",
        "GrandparentGrandchild", "SiblingLike", "StrictMentor", "OldTension",
        "teamup_chemistry_variant", "chemvar:", "BanterMemoryTracker",
        "ContextBanterCatalog", "TRUE FORM", "SignatureAuthorityService",
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
        "TEAM UP v0.2.0-alpha.6.7.9 - LATER LIVE TEST CHECKLIST\n"
        "=============================================================\n\n"
        "Bản này đã qua static/compile CI nhưng CHƯA live-test.\n\n"
        "Khi có máy test lại:\n"
        "1) teamup_chemistry_variant Robin Sebastian -> ParentChild, preferredLead=Robin, linePool>=3.\n"
        "2) teamup_chemistry_variant George Alex -> GrandparentGrandchild, preferredLead=George.\n"
        "3) teamup_chemistry_variant Alex Sebastian -> FriendlyRivalry.\n"
        "4) teamup_chemistry_variant Pierre Morris -> SharpRivalry.\n"
        "5) Gọi teamup_banter now nhiều lần: cùng chemistry phải đổi giữa nhiều câu, tránh lặp nhờ Banter Memory.\n"
        "6) Authored pair dialogue 6.7.5 vẫn phải thắng chemistry fallback nếu cặp có script riêng.\n"
        "7) Party 5 người, companion 2/2, Pelipper/Gus/MiMi rank/signature không regression.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.9")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
    if not AUDIT.exists() and audit_lines:
        AUDIT.write_text("\n".join(audit_lines) + "\n", encoding="utf-8")
