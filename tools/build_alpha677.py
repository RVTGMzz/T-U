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
STAGE = ROOT / "_stage_alpha677"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA677.txt"
AUDIT = ROOT / "BANTER_MEMORY_AUDIT_ALPHA677.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_7_BANTER_MEMORY_VI.txt"
VERSION = "0.2.0-alpha.6.7.7"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.7_BANTER_MEMORY_AUDIT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.7_BANTER_MEMORY_AUDIT_TEST.sha256.txt"

lines: list[str] = []
audit_lines: list[str] = ["# Team Up Alpha 6.7.7 Banter Memory Audit", ""]


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


try:
    project = text("TeamUp.csproj")
    config = text("ModConfig.cs")
    service = text("Core/PartyBanterService.cs")
    memory = text("Core/BanterMemoryTracker.cs")
    pair_catalog = text("Core/BanterContentCatalog.cs")
    context_catalog = text("Core/ContextBanterCatalog.cs")
    alpha677 = text("ModEntry.Alpha677.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    combat = text("Combat/CombatService.cs")
    follow = text("Following/FollowService.cs")
    special = text("Combat/SpecialRecruitCombatService.cs")
    rank = text("Core/CombatRankCatalog.cs")

    require(f"<Version>{VERSION}</Version>" in project, "Wrong Alpha 6.7.7 project version")
    require("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person party cap regression")
    require("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2-companion cap regression")

    # Preserve 6.7.5 + 6.7.6 content depth.
    pair_count = len(re.findall(r'P\("pair:[^"]+"', pair_catalog))
    ship_count = len(re.findall(r'S\("ship:[^"]+"', pair_catalog))
    context_count = len(re.findall(r'C\("ctx:[^"]+"', context_catalog))
    context_families = set(re.findall(r'BanterContextKind\.(Rain|Storm|Night|Mine|Saloon|Beach|Forest|AdventurerGuild|PostCombat)', context_catalog))
    require(pair_count >= 28, f"6.7.5 pair scripts regressed: {pair_count}")
    require(ship_count >= 8, f"6.7.5 MiMi ship scripts regressed: {ship_count}")
    require(context_count >= 37, f"6.7.6 context scripts regressed: {context_count}")
    require(len(context_families) == 9, f"6.7.6 context family coverage regressed: {sorted(context_families)}")

    # Banter memory shape and bounded-memory contract.
    require("internal sealed class BanterMemoryTracker" in memory, "BanterMemoryTracker missing")
    require("MaxRecentExchangeIds = 12" in memory, "Recent exchange memory bound changed/missing")
    require("MaxRecentSpeakers = 8" in memory, "Recent speaker memory bound changed/missing")
    require("RecentExchangePenalty = 100" in memory, "Exchange repetition penalty missing")
    require("RecentSpeakerPenalty = 18" in memory, "Speaker repetition penalty missing")
    require("while (_recentExchangeIds.Count > MaxRecentExchangeIds)" in memory, "Exchange ring-buffer trim missing")
    require("while (_recentSpeakers.Count > MaxRecentSpeakers)" in memory, "Speaker ring-buffer trim missing")
    require("_sessionUseCounts.Clear();" in memory, "Session memory reset incomplete")
    require("Math.Min(20, uses * 2)" in memory, "Soft session-use penalty missing")

    # Runtime integration: score before selection, record only once content is actually queued.
    require("private readonly BanterMemoryTracker BanterMemory = new();" in service, "PartyBanterService memory instance missing")
    require("candidate => BanterMemory.Score(" in service, "Context candidate recency scoring missing")
    require('"pair-choice:" + key' in service, "Pair-choice recency scoring missing")
    require(".OrderBy(candidate => candidate.MemoryScore)" in service, "Pair memory ranking missing")
    require("BanterMemory.Record(id, speaker.Member.CharacterName);" in service, "Single-line memory recording missing")
    require("BanterMemory.Record(\n            exchange.Id" in service, "Exchange memory recording missing")
    require("BanterMemory.Reset();" in service, "Daily/title banter reset does not clear memory")
    require("public string DescribeMemory()" in service and "public void ResetMemory()" in service, "Memory debug surface incomplete")
    require("teamup_banter_memory" in alpha677, "Memory console command missing")
    require("status | reset" in alpha677, "Memory command actions missing")
    require("EnsureAlpha677BanterMemoryRegistered();" in alpha6625, "Alpha 6.7.7 registration missing")

    # Soft fallback guarantee: no hard recent-memory exclusion. The only hard gating remains existing cooldowns.
    require("IsRecent" not in service, "Recent memory must not hard-block banter candidates")
    require("return int.MaxValue" not in memory, "Memory scoring must remain soft, not absolute exclusion")
    require("PairCooldownUntil.TryGetValue" in service, "Existing anti-spam pair cooldown was lost")
    require("NextContextTick = tick + Game1.random.Next(1800, 2701);" in service, "6.7.6 context cadence regression")

    # Cosmetic-only wall.
    memory_surface = memory + "\n" + alpha677 + "\n" + service
    for forbidden in [
        "friendshipData[", ".spouse =", "dating =", "controller =", "temporaryController =", ".Halt()",
        "CurrentHealth =", "damageMonster(", "changeSchedule", "schedule =", "money =", "addItem",
    ]:
        require(forbidden not in memory_surface, f"Banter memory cosmetic-only contract violated: {forbidden}")
    require("showTextAboveHead" in service, "Banter presentation must remain overhead bubble only")

    # Locked regression wall while live testing is unavailable.
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
        f"- Authored ambient pairs preserved: **{pair_count}**",
        f"- MiMi ship pairs preserved: **{ship_count}**",
        f"- Context scripts preserved: **{context_count} across {len(context_families)} families**",
        "- Recent exchange ring buffer: **12 IDs**",
        "- Recent speaker ring buffer: **8 speakers**",
        "- Exchange repetition penalty + speaker recency penalty: **PASS**",
        "- Soft fallback (memory ranks candidates, never hard-blocks the only valid line): **PASS**",
        "- Memory reset on Team Up banter reset: **PASS**",
        "- Debug command `teamup_banter_memory status|reset`: **PASS**",
        "- Cosmetic-only + core regression wall: **PASS**",
        "",
        "## Important",
        "This is source/CI validation only. Subjective repetition feel and ideal memory window size still need later in-game validation.",
    ])

    log("SOURCE ACCEPTANCE: PASS")
    log(f"BANTER MEMORY STATIC AUDIT: PASS ({pair_count} pairs + {ship_count} ships + {context_count} contexts)")
    log("Building Alpha 6.7.7...")
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
        "BanterMemoryTracker", "teamup_banter_memory", "pair-choice:",
        "ContextBanterCatalog", "BanterContentCatalog", "TRUE FORM", "SignatureAuthorityService",
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
        "TEAM UP v0.2.0-alpha.6.7.7 - LATER LIVE TEST CHECKLIST\n"
        "=======================================================\n\n"
        "Bản này đã qua memory/static/compile CI nhưng CHƯA live-test.\n\n"
        "Khi có máy test lại:\n"
        "1) teamup_banter_memory status -> xem recent IDs/speakers thay đổi sau mỗi banter.\n"
        "2) Party 3-4 NPC: cùng một speaker/cặp không nên liên tục chiếm lượt nếu còn lựa chọn khác.\n"
        "3) Party chỉ 1-2 NPC: memory là soft penalty, không được làm banter im hẳn.\n"
        "4) Context rain/night/mine/post-combat vẫn ưu tiên đúng hoàn cảnh.\n"
        "5) teamup_banter_memory reset chỉ xóa cosmetic memory, không đụng save/progression.\n"
        "6) Party 5 người, companion 2/2, Pelipper/Gus/signature authority không regression.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.7")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
    if not AUDIT.exists() and audit_lines:
        AUDIT.write_text("\n".join(audit_lines) + "\n", encoding="utf-8")
