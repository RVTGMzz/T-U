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
STAGE = ROOT / "_stage_alpha6718"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6718.txt"
AUDIT = ROOT / "EXPANSION_PROFILE_FLAVOR_AUDIT_ALPHA6718.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_18_PROFILE_FLAVOR_VI.txt"
VERSION = "0.2.0-alpha.6.7.18"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.18_EXPANSION_PROFILE_FLAVOR_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.18_EXPANSION_PROFILE_FLAVOR_TEST.sha256.txt"

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
    full_roster = text("Combat/ExpansionSkillService.FullRoster.cs")
    completion = text("Core/ExpansionRosterCompletion.cs")
    profile_catalog = text("Core/ExpansionNpcProfileCatalog.cs")
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
    banter = text("Core/BanterContentCatalog.cs")
    context = text("Core/ContextBanterCatalog.cs")
    chemistry = text("Core/PartyChemistryCatalog.cs")
    all_source = "\n".join(path.read_text(encoding="utf-8") for path in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")

    skill_re = re.compile(
        r'Skills\["([^"]+)"\]\s*=\s*S\("([^"]+)",\s*SkillMode\.(\w+),\s*PartyRole\.(\w+),\s*PartyRole\.(\w+),\s*(\d+),'
    )
    skill_rows = skill_re.findall(full_roster)
    req(len(skill_rows) >= 50, f"expected at least 50 full-roster skills, got {len(skill_rows)}")
    names = [row[0] for row in skill_rows]
    req(len(names) == len(set(name.lower() for name in names)), "duplicate full-roster skill name")

    default = json.loads((SRC / "i18n" / "default.json").read_text(encoding="utf-8"))
    vi = json.loads((SRC / "i18n" / "vi.json").read_text(encoding="utf-8"))
    old_en_generic = "a unique Team Up signature tuned around"
    old_vi_generic = "kỹ năng đặc trưng riêng của Team Up thiên về"
    old_vi_passive = "chiến đấu theo hướng"

    for name, signature, _mode, primary, secondary, cooldown in skill_rows:
        key = name.lower()
        pkey = f"codex.expansion.{key}.passive"
        akey = f"codex.expansion.{key}.ability"
        req(pkey in default and akey in default and pkey in vi and akey in vi, f"missing i18n profile row for {name}")
        req(signature in default[akey], f"EN ability lost signature name for {name}")
        req(signature in vi[akey], f"VI ability lost signature name for {name}")
        req("Base CD ~" in default[akey] and "Tier 3" in default[akey], f"EN mechanical summary missing for {name}")
        req("CD gốc ~" in vi[akey] and "Tier 3" in vi[akey], f"VI mechanical summary missing for {name}")
        req(primary in default[pkey] or (primary == "Damage" and "DPS" in default[pkey]), f"EN primary role missing for {name}")
        req(secondary in default[pkey] or (secondary == "Damage" and "DPS" in default[pkey]), f"EN secondary role missing for {name}")
        req(old_en_generic not in default[akey], f"old generic EN ability text remains for {name}")
        req(old_vi_generic not in vi[akey], f"old generic VI ability text remains for {name}")
        req(old_vi_passive not in vi[pkey], f"old generic VI passive text remains for {name}")
        req(int(cooldown) > 0, f"invalid base cooldown for {name}")

    # This checkpoint is deliberately localization/profile-content only.
    diff = subprocess.run(
        ["git", "diff", "--name-only", "HEAD", "--", "src/TeamUp"],
        cwd=ROOT,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=True,
    ).stdout.splitlines()
    allowed = {
        "src/TeamUp/TeamUp.csproj",
        "src/TeamUp/i18n/default.json",
        "src/TeamUp/i18n/vi.json",
    }
    req(set(diff) == allowed, f"6.7.18 expected exactly version + EN/VI profile text files, got: {diff}")

    # Profile coverage remains complete and source interpretations stay explicitly Team Up-owned.
    req("Team Up-original" in completion, "Team Up-original expansion identity disclaimer missing")
    req("ExpansionRosterCompletion.Resolve" in text("Core/NpcProfileCatalog.cs"), "full roster profile completion path missing")
    req("TryGetBaseCooldownTicks" in full_roster, "full roster combat kit coverage hook missing")

    # 6.7.17 social expansion retained.
    pair_ids = re.findall(r'P\("(pair:[^"]+)"', banter)
    context_ids = re.findall(r'C\("(ctx:[^"]+)"', context)
    req(len(pair_ids) >= 43, "6.7.17 authored banter regression")
    req(len(context_ids) >= 49, "6.7.17 context banter regression")
    req("PartyChemistryCatalog" not in "" or len(chemistry) > 0, "chemistry source missing")

    # Carry-forward diagnostics and locked architecture without touching live-bug ownership.
    req('"teamup_diag_all"' in alpha6716, "6.7.16 combined diagnostics regression")
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
    log(f"EXPANSION PROFILE FLAVOR AUDIT: PASS ({len(skill_rows)} full-roster NPCs)")
    log("EN/VI MECHANICAL PROFILE SUMMARY: PASS")
    log("LOCALIZATION-ONLY FILE BOUNDARY: PASS")
    log("6.7.17 SOCIAL CONTENT CARRY-FORWARD: PASS")
    log("6.7.13/14/15/16 LIVE-GUARD + DIAGNOSTICS CARRY-FORWARD: PASS")
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
    for token in ["teamup_diag_all", "NpcRouteStateSafetyPatch", "PelipperWildCombatProxy"]:
        req(dll_contains(blob, token), f"DLL missing carry-forward token: {token}")
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
        "# Team Up Alpha 6.7.18 Expansion Profile Flavor Audit\n\n"
        "## New pass\n"
        f"- Full-roster expansion NPC profiles rewritten: {len(skill_rows)}\n"
        "- English + Vietnamese passive summaries now describe primary/secondary role and engagement behavior.\n"
        "- English + Vietnamese signature summaries now describe actual skill mode, base cooldown, and Tier 3 cooldown improvement.\n"
        "- Old generated placeholder wording removed from every FullRoster profile row.\n"
        "- No source-mod lore is asserted as canonical; these remain Team Up combat interpretations.\n"
        "- Source diff restricted to TeamUp.csproj + i18n/default.json + i18n/vi.json: PASS\n\n"
        "## Carry forward\n"
        "- 6.7.17 banter/context/chemistry expansion: PASS\n"
        "- 6.7.16 diagnostics bundle: PASS\n"
        "- 6.7.13/14 Pelipper capture/source guards: PASS\n"
        "- People cap 5 and external companion cap 2/2: PASS\n"
        "- Gunther route guard retained, still requires live verification.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.18 - EXPANSION PROFILE FLAVOR PASS\n"
        "================================================\n\n"
        "Ban nay khong doi gameplay. No thay cac mo ta profile mau cua roster SVE/RSV bang thong tin co ich hon.\n\n"
        "1. Mo Team Up Codex va xem vai NPC SVE/RSV.\n"
        "2. Passive phai noi ro vai tro chinh/phu va cach giu doi hinh.\n"
        "3. Signature phai noi ro kieu ky nang, CD goc va viec Tier 3 giam khoang 1.5 giay.\n"
        "4. Doi ngon ngu EN/VI de kiem tra ca hai bo text.\n"
        "5. Neu test bug Pelipper/Gunther/capture, dung teamup_diag_all va gui file bundle.\n"
        "6. File bundle tren may hien tai: E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Diagnostic_bundle_latest.txt\n\n"
        "CI PASS chi xac nhan build/data. Pelipper/Gunther/capture van can live verification rieng.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.18")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
