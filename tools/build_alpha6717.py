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
STAGE = ROOT / "_stage_alpha6717"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6717.txt"
AUDIT = ROOT / "SOCIAL_CONTENT_AUDIT_ALPHA6717.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_17_BANTER_CHEMISTRY_VI.txt"
VERSION = "0.2.0-alpha.6.7.17"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.17_BANTER_CHEMISTRY_EXPANSION_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.17_BANTER_CHEMISTRY_EXPANSION_TEST.sha256.txt"

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


def max_csharp_string_length(source: str) -> tuple[int, str]:
    values = re.findall(r'"((?:[^"\\]|\\.)*)"', source)
    if not values:
        return 0, ""
    longest = max(values, key=len)
    return len(longest), longest


try:
    project = text("TeamUp.csproj")
    banter = text("Core/BanterContentCatalog.cs")
    context = text("Core/ContextBanterCatalog.cs")
    chemistry = text("Core/PartyChemistryCatalog.cs")
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

    expected_pair_ids = [
        "pair:penny-maru", "pair:leah-emily", "pair:sam-abigail", "pair:george-evelyn",
        "pair:gus-willy", "pair:robin-leah", "pair:sandy-emily", "pair:lewis-marnie",
        "pair:harvey-elliott", "pair:sophia-victor", "pair:olivia-claire", "pair:lance-wizard",
        "pair:andy-gus", "pair:victor-lance", "pair:marlon-wizard",
    ]
    expected_context_ids = [
        "ctx:rain:leah-emily", "ctx:storm:sam-sebastian", "ctx:night:harvey-elliott",
        "ctx:mine:maru-clint", "ctx:saloon:sandy-emily", "ctx:beach:sam-alex",
        "ctx:forest:leah-robin", "ctx:guild:abigail-marlon", "ctx:post:alex-sebastian",
        "ctx:post:penny-maru", "ctx:rain:sophia-claire", "ctx:mine:jio-daia",
    ]
    expected_chemistry_rows = [
        'P("Penny", "Maru", PartyChemistryType.Friends | PartyChemistryType.Respectful)',
        'P("Leah", "Emily", PartyChemistryType.Friends | PartyChemistryType.Respectful)',
        'P("Gus", "Willy", PartyChemistryType.Friends | PartyChemistryType.Respectful)',
        'P("Sandy", "Emily", PartyChemistryType.Friends)',
        'P("Harvey", "Elliott", PartyChemistryType.Respectful)',
        'P("Robin", "Leah", PartyChemistryType.Respectful)',
        'P("Olivia", "Victor", PartyChemistryType.Family | PartyChemistryType.Protective)',
        'P("Sophia", "Claire", PartyChemistryType.Friends)',
        'P("Martin", "Claire", PartyChemistryType.Friends)',
        'P("Jio", "Daia", PartyChemistryType.Rivals | PartyChemistryType.Respectful)',
        'P("Kenneth", "Philip", PartyChemistryType.Rivals | PartyChemistryType.Respectful)',
        'P("Shiro", "Carmen", PartyChemistryType.Friends | PartyChemistryType.Protective)',
        'P("Maddie", "Blair", PartyChemistryType.Rivals | PartyChemistryType.Friends)',
        'P("June", "Ysabelle", PartyChemistryType.Friends)',
        'P("Lance", "Wizard", PartyChemistryType.Respectful)',
        'P("Andy", "Morris", PartyChemistryType.Rivals)',
    ]

    for item in expected_pair_ids:
        req(banter.count(item) == 1, f"missing/duplicate new banter id: {item}")
    for item in expected_context_ids:
        req(context.count(item) == 1, f"missing/duplicate new context id: {item}")
    for row in expected_chemistry_rows:
        req(chemistry.count(row) == 1, f"missing/duplicate chemistry row: {row}")

    pair_ids = re.findall(r'P\("(pair:[^"]+)"', banter)
    context_ids = re.findall(r'C\("(ctx:[^"]+)"', context)
    req(len(pair_ids) == len(set(pair_ids)), "duplicate authored pair banter ID")
    req(len(context_ids) == len(set(context_ids)), "duplicate context banter ID")
    req(len(pair_ids) >= 43, f"expected at least 43 pair scripts after expansion, got {len(pair_ids)}")
    req(len(context_ids) >= 49, f"expected at least 49 context scripts after expansion, got {len(context_ids)}")

    for label, source in [("banter", banter), ("context", context)]:
        longest_len, longest = max_csharp_string_length(source)
        req(longest_len <= 120, f"{label} string exceeds bubble cap ({longest_len}): {longest}")

    # Ensure this checkpoint is data/content-only. The materializer may change only the version
    # plus the three social content catalogs.
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
        "src/TeamUp/Core/BanterContentCatalog.cs",
        "src/TeamUp/Core/ContextBanterCatalog.cs",
        "src/TeamUp/Core/PartyChemistryCatalog.cs",
    }
    req(set(diff).issubset(allowed), f"6.7.17 changed gameplay/source files: {sorted(set(diff) - allowed)}")
    req(set(diff) == allowed, f"6.7.17 expected exactly four content/version files, got: {diff}")

    # Cosmetic-only contract stays explicit in source.
    req("never read or write Stardew" in chemistry or "never read or write Stardew" in chemistry.lower(),
        "chemistry cosmetic-only contract missing")
    req("dialogue-only data" in banter.lower(), "banter dialogue-only contract missing")
    req("Data only" in context or "data only" in context.lower(), "context banter data-only contract missing")

    # Carry-forward diagnostics and live guards untouched.
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
    log(f"BANTER EXPANSION STATIC AUDIT: PASS ({len(pair_ids)} authored pair scripts)")
    log(f"CONTEXT BANTER STATIC AUDIT: PASS ({len(context_ids)} context scripts)")
    log("CHEMISTRY EXPANSION STATIC AUDIT: PASS (+16 authored chemistry pairs)")
    log("CONTENT-ONLY FILE BOUNDARY: PASS")
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
    for token in [
        "pair:penny-maru",
        "pair:marlon-wizard",
        "ctx:rain:leah-emily",
        "ctx:mine:jio-daia",
        "teamup_diag_all",
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
        "# Team Up Alpha 6.7.17 Social Content Audit\n\n"
        "## Content-only expansion\n"
        f"- Authored pair banter scripts: {len(pair_ids)} total (+15 new)\n"
        f"- Context banter scripts: {len(context_ids)} total (+12 new)\n"
        "- Party Chemistry: +16 authored cosmetic pair rows\n"
        "- VI/EN string bubble cap <=120 characters: PASS\n"
        "- Duplicate pair/context IDs: none\n"
        "- Materialized diff restricted to csproj + BanterContentCatalog + ContextBanterCatalog + PartyChemistryCatalog: PASS\n"
        "- No Pelipper/Gunther/capture/CombatService gameplay source changed: PASS\n\n"
        "## Carry forward\n"
        "- 6.7.13 capture proxy identity: PASS\n"
        "- 6.7.14 NPC-only source lock + capture ceasefire: PASS\n"
        "- 6.7.15 preflight diagnostics: PASS\n"
        "- 6.7.16 combat telemetry + compatibility audit: PASS\n"
        "- People cap 5 and external companion cap 2/2: PASS\n"
        "- Gunther route guard retained, still requires live verification.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.17 - BANTER + CHEMISTRY EXPANSION\n"
        "=============================================\n\n"
        "Ban nay chi mo rong noi dung hoi thoai/chemistry. Khong doi combat, Pelipper, capture floor hay Gunther route.\n\n"
        "1. Vao game va dung teamup_banter_catalog_audit.\n"
        "2. Dung teamup_context_banter audit.\n"
        "3. Dung teamup_chemistry status.\n"
        "4. Co the dung teamup_banter now / teamup_context_banter now de xem vibe hoi thoai.\n"
        "5. Neu test bug Pelipper/Gunther/capture, dung teamup_diag_all va gui file bundle.\n"
        "6. File bundle tren may hien tai: E:\\SteamLibrary\\steamapps\\common\\Stardew Valley\\Mods\\Team Up\\diagnostics\\TeamUp_Diagnostic_bundle_latest.txt\n\n"
        "CI PASS chi xac nhan data/build. Cam nhan hoi thoai trong game co the polish sau khi live test.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.17")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
