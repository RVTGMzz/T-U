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
STAGE = ROOT / "_stage_alpha676"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA676.txt"
AUDIT = ROOT / "CONTEXT_BANTER_AUDIT_ALPHA676.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_6_CONTEXT_BANTER_VI.txt"
VERSION = "0.2.0-alpha.6.7.6"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.6_CONTEXT_BANTER_AUDIT_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.6_CONTEXT_BANTER_AUDIT_TEST.sha256.txt"

lines: list[str] = []
audit_lines: list[str] = ["# Team Up Alpha 6.7.6 Context Banter Audit", ""]


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
    pair_catalog = text("Core/BanterContentCatalog.cs")
    context_catalog = text("Core/ContextBanterCatalog.cs")
    alpha676 = text("ModEntry.Alpha676.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    combat = text("Combat/CombatService.cs")
    follow = text("Following/FollowService.cs")
    special = text("Combat/SpecialRecruitCombatService.cs")
    rank = text("Core/CombatRankCatalog.cs")
    core_profiles = text("Core/NpcProfileCatalog.cs")
    expansion_profiles = text("Core/ExpansionNpcProfileCatalog.cs")

    require(f"<Version>{VERSION}</Version>" in project, "Wrong Alpha 6.7.6 project version")
    require("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person party cap regression")
    require("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2-companion cap regression")

    # Preserve 6.7.5 authored banter layer.
    pair_rows = re.findall(r'P\("pair:[^"]+"', pair_catalog)
    ship_rows = re.findall(r'S\("ship:[^"]+"', pair_catalog)
    require(len(pair_rows) >= 28, f"6.7.5 authored pairs regressed: {len(pair_rows)}")
    require(len(ship_rows) >= 8, f"6.7.5 MiMi ship pairs regressed: {len(ship_rows)}")
    require("BanterContentCatalog.TryGetPair" in service, "6.7.5 pair catalog runtime lookup missing")
    require("BanterContentCatalog.TryGetShippingPair" in service, "6.7.5 MiMi shipping runtime lookup missing")

    # Parse Alpha 6.7.6 context rows.
    context_pattern = re.compile(
        r'C\("(?P<id>ctx:[^"]+)",\s*BanterContextKind\.(?P<context>\w+),\s*'
        r'"(?P<speaker>[^"]*)",\s*"(?P<partner>[^"]*)",\s*'
        r'"(?P<vi>[^"]*)",\s*"(?P<en>[^"]*)",\s*"(?P<vi_reply>[^"]*)",\s*"(?P<en_reply>[^"]*)"\)',
        re.DOTALL,
    )
    rows = [m.groupdict() for m in context_pattern.finditer(context_catalog)]
    require(len(rows) >= 37, f"Expected >=37 context scripts, got {len(rows)}")

    known_names = set(re.findall(r'\["([^"]+)"\]\s*=\s*P\(', core_profiles))
    known_names |= set(re.findall(r'E\("([^"]+)"', expansion_profiles))
    known_names |= set(re.findall(r'X\("([^"]+)"', expansion_profiles))
    known_names |= {
        "Marlon", "Morris", "Henchman", "Sudoku",
        "ronvotri.HeyYoureCursed_Sudoku", "Ronvotri.Cardcha_MiMi"
    }

    ids: set[str] = set()
    slots: set[str] = set()
    issues: list[str] = []
    contexts: dict[str, int] = {}
    special_seen = {"Ronvotri.Cardcha_MiMi": 0, "ronvotri.HeyYoureCursed_Sudoku": 0, "Marlon": 0, "Henchman": 0}

    for row in rows:
        rid = row["id"].lower()
        if rid in ids:
            issues.append(f"duplicate id {row['id']}")
        ids.add(rid)
        slot = f"{row['context'].lower()}|{row['speaker'].lower()}|{row['partner'].lower()}"
        if slot in slots:
            issues.append(f"duplicate context speaker slot {slot}")
        slots.add(slot)
        contexts[row["context"]] = contexts.get(row["context"], 0) + 1

        if row["speaker"] not in known_names:
            issues.append(f"{row['id']}: unknown speaker {row['speaker']}")
        if row["partner"] and row["partner"] not in known_names:
            issues.append(f"{row['id']}: unknown partner {row['partner']}")
        if row["speaker"] in special_seen:
            special_seen[row["speaker"]] += 1
        if row["partner"] in special_seen:
            special_seen[row["partner"]] += 1

        for field in ("vi", "en"):
            value = row[field].strip()
            if not value:
                issues.append(f"{row['id']}: empty {field}")
            if len(value) > 120:
                issues.append(f"{row['id']}: {field} exceeds 120 chars ({len(value)})")
            if "\\n" in value or "\\r" in value:
                issues.append(f"{row['id']}: line break in {field}")

        if row["partner"]:
            for field in ("vi_reply", "en_reply"):
                value = row[field].strip()
                if not value:
                    issues.append(f"{row['id']}: partner script missing {field}")
                if len(value) > 120:
                    issues.append(f"{row['id']}: {field} exceeds 120 chars ({len(value)})")
        elif row["vi_reply"].strip() or row["en_reply"].strip():
            issues.append(f"{row['id']}: single-speaker script unexpectedly has reply")

    require(not issues, "Context banter catalog audit failed: " + " | ".join(issues))
    for required_context in ["Rain", "Storm", "Night", "Mine", "Saloon", "Beach", "Forest", "AdventurerGuild", "PostCombat"]:
        require(contexts.get(required_context, 0) > 0, f"Missing context family: {required_context}")
    for special, count in special_seen.items():
        require(count > 0, f"Special recruit has no context script: {special}")

    # Runtime integration contract.
    require("private long NextContextTick;" in service, "Context cooldown scheduler missing")
    require("TryContextExchange(active, BanterContextKind.PostCombat" in service, "Post-combat context hook missing")
    require("NextContextTick = tick + Game1.random.Next(1800, 2701);" in service, "Context anti-spam cadence missing")
    require("ContextBanterCatalog.Get(context)" in service, "Context catalog runtime lookup missing")
    require("ResolveCurrentContexts()" in service, "Context resolver missing")
    require("ReadStaticGameBoolean(\"isRaining\")" in service, "Rain context detector missing")
    require("ReadStaticGameBoolean(\"isLightning\")" in service, "Storm context detector missing")
    require("Game1.timeOfDay >= 1900" in service, "Night context detector missing")
    require("Mine\", \"SkullCave\", \"VolcanoDungeon" in service, "Mine/danger location detector missing")
    require("public bool ForceContext()" in service, "ForceContext test hook missing")
    require("teamup_context_banter" in alpha676, "Context banter console command missing")
    require("EnsureAlpha676ContextBanterRegistered();" in alpha6625, "Alpha 6.7.6 registration missing")

    # Presentation-only guarantee. Context chatter may inspect weather/location/time but may not mutate game state.
    context_surface = context_catalog + "\n" + alpha676 + "\n" + service
    for forbidden in [
        "friendshipData[", ".spouse =", "dating =", "controller =", "temporaryController =", ".Halt()",
        "CurrentHealth =", "damageMonster(", "changeSchedule", "schedule =", "money =", "addItem",
    ]:
        require(forbidden not in context_surface, f"Context banter cosmetic-only contract violated: {forbidden}")
    require("showTextAboveHead" in service, "Context banter must use non-blocking overhead bubbles")

    # Locked regression wall while no live test machine is available.
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    require("PelipperRenderSuppressedAlpha6613" not in all_source, "Legacy Pelipper render suppression returned")
    require("TrySetActorInvisibleAlpha6613" not in all_source, "Legacy Pelipper visibility writer returned")
    require("isTileLocationTotallyClearAndPlaceable" not in follow, "Follow water/path regression returned")
    require("isTileLocationTotallyClearAndPlaceable" not in combat, "Combat water/path regression returned")
    require("private const int CombatPathRetryCooldownTicks = 24;" in combat, "Combat path retry cadence changed")
    require("private const int CombatMovementPulseTicks = 3;" in combat, "Combat movement pulse changed")
    require("UnlockVanillaMovementAnimation(npc);" in follow, "Gus vanilla animation unlock regression")
    require("MimiTrueFormDurationTicks = 240" in special, "MiMi TRUE FORM duration regression")
    require("MimiTrueFormCooldownTicks = 6000" in special, "MiMi TRUE FORM cooldown regression")
    require('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank, "Marlon S rank regression")
    require("RecruitBadge.Boss | RecruitBadge.Special" in rank, "MiMi S/BOSS/SPECIAL rank regression")
    require("SignatureAuthorityService.IsAlpha6PrototypeSignatureOwner" in combat, "6.7.4 signature authority regression")

    audit_lines.extend([
        f"- Context scripts: **{len(rows)}**",
        f"- Context families: **{len(contexts)}** ({', '.join(sorted(contexts))})",
        f"- Existing 6.7.5 ambient pair scripts preserved: **{len(pair_rows)}**",
        f"- Existing 6.7.5 MiMi ship scripts preserved: **{len(ship_rows)}**",
        "- Rain/storm/night/mine/location/post-combat coverage: **PASS**",
        "- MiMi/Sudoku/Marlon/Henchman special context presence: **PASS**",
        "- VI/EN + 120-character overhead bubble guard: **PASS**",
        "- Duplicate ID/context-slot detection: **PASS**",
        "- Presentation-only contract: **PASS**",
        "- 5-person / 2-companion / Pelipper / Gus / combat cadence regression wall: **PASS**",
        "",
        "## Important",
        "This is source/CI validation. Timing, location-name coverage for every external map, and subjective chatter frequency still require later live testing.",
    ])

    log("SOURCE ACCEPTANCE: PASS")
    log(f"CONTEXT BANTER STATIC AUDIT: PASS ({len(rows)} scripts across {len(contexts)} contexts)")
    log("Building Alpha 6.7.6...")
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
        "ContextBanterCatalog", "BanterContextKind", "teamup_context_banter",
        "ctx:rain:sebastian", "ctx:storm:wizard", "ctx:mine:marlon-lance",
        "ctx:post:mimi", "BanterContentCatalog", "TRUE FORM", "SignatureAuthorityService",
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
        "TEAM UP v0.2.0-alpha.6.7.6 - LATER LIVE TEST CHECKLIST\n"
        "=======================================================\n\n"
        "Bản này đã qua context/static/compile CI nhưng CHƯA live-test.\n\n"
        "Khi có máy test lại:\n"
        "1) teamup_context_banter audit -> PASS.\n"
        "2) teamup_context_banter now ở trời mưa, ban đêm, Mine, Saloon, Beach/Forest/Guild.\n"
        "3) Sau combat: MiMi/Marlon/Harvey/Sudoku/Henchman có thể dùng post-combat line trước victory generic.\n"
        "4) Bubble vẫn tối chữ/dễ đọc, không DialogueBox, không controller takeover.\n"
        "5) Context không spam: khoảng nghỉ dài hơn ambient generic.\n"
        "6) Party 5 người tổng, companion 2/2, Pelipper không regression.\n"
        "7) Gus walk/facing vanilla; 5 prototype không double-signature.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.6")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
    if not AUDIT.exists() and audit_lines:
        AUDIT.write_text("\n".join(audit_lines) + "\n", encoding="utf-8")
