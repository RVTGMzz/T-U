from __future__ import annotations

import hashlib
import json
import shutil
import subprocess
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha673"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA673.txt"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_3_RANK_SPECIAL_RECRUITS_VI.txt"
VERSION = "0.2.0-alpha.6.7.3"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.3_RANK_SPECIAL_RECRUITS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.3_RANK_SPECIAL_RECRUITS_TEST.sha256.txt"

lines: list[str] = []


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
    rank = text("Core/CombatRankCatalog.cs")
    coverage = text("Core/CombatKitCoverageService.cs")
    profiles = text("Core/ExpansionNpcProfileCatalog.cs")
    classifier = text("Core/CompanionClassificationService.cs")
    skills = text("Combat/ExpansionSkillService.FullRoster.cs")
    tuning = text("Combat/ExpansionSkillService.IdentityBalance.cs")
    special = text("Combat/SpecialRecruitCombatService.cs")
    alpha673 = text("ModEntry.Alpha673.cs")
    alpha6625 = text("ModEntry.Alpha6625.cs")
    follow = text("Following/FollowService.cs")
    combat = text("Combat/CombatService.cs")
    codex = text("UI/CodexBrowserMenu.cs")
    profile_ui = text("UI/CharacterProfileMenu.cs")

    require(f"<Version>{VERSION}</Version>" in project, "Wrong project version")
    require("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person people cap regression")
    require("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2-companion cap regression")

    # Rank/badge system.
    require("public enum CombatRank" in rank and all(f"    {letter}" in rank for letter in ["D", "C", "B", "A", "S"]), "D/C/B/A/S rank enum incomplete")
    require("RecruitBadge.Special" in rank and "RecruitBadge.Boss" in rank and "RecruitBadge.Legendary" in rank, "Rank badges incomplete")
    require('[CustomNpcCompatibilityService.MimiNpcId] = new(CombatRank.S, RecruitBadge.Boss | RecruitBadge.Special)' in rank, "MiMi S BOSS SPECIAL missing")
    require('["Marlon"] = new(CombatRank.S, RecruitBadge.Legendary)' in rank, "Marlon S LEGENDARY missing")
    require('[CustomNpcCompatibilityService.SudokuCanonicalNpcId] = new(CombatRank.A, RecruitBadge.Special)' in rank, "Sudoku A SPECIAL missing")
    require('["Henchman"] = new(CombatRank.B, RecruitBadge.Special)' in rank, "Henchman B SPECIAL missing")
    require("CombatRankCatalog.Get(_characterName, _profile)" in profile_ui, "Profile rank badge missing")
    require('string rankedName = $"[{rankInfo.Rank}]' in codex, "Codex rank marker missing")

    # SVE roster completion.
    for npc in ["Marlon", "Morris", "Henchman"]:
        require(f'E("{npc}", SveSourceId' in profiles, f"{npc} SVE combat profile missing")
    require('Skills["Morris"] = S("CORPORATE LEVERAGE"' in skills, "Morris skill missing")
    require('["Morris"] = T(-1, 1, 1, -1)' in tuning, "Morris zero-sum identity tuning missing")
    require("HasUsableDirectionalSprite(npc)" in classifier, "Henchman directional-animation recruitment gate missing")
    require("npc.Sprite.Texture.Width >= frameWidth * 4" in classifier, "Henchman sprite width gate missing")
    require("npc.Sprite.Texture.Height >= frameHeight * 4" in classifier, "Henchman sprite height gate missing")

    # Special combat identities.
    require("MimiTrueFormDurationTicks = 240" in special, "MiMi TRUE FORM must stay ~4 seconds")
    require("MimiTrueFormCooldownTicks = 6000" in special, "MiMi TRUE FORM long cooldown missing")
    require("MONSTER HUNTER" in special, "Marlon signature missing")
    require("VOID MAYO SPLASH" in special, "Henchman special signature missing")
    require("SetMimiBossForm" in special and "SetMimiTrueForm" in special, "Future Cardcha presentation bridge missing")
    require("Cardcha currently exposes no reusable MiMi boss-form presentation API" in special, "MiMi source-authority fallback log missing")
    require("assets/mimi" not in special.lower(), "Team Up must not own/copy MiMi boss assets")
    require("EnsureAlpha673SpecialRecruitRegistered();" in alpha6625, "Alpha 6.7.3 registration missing")
    require("teamup_roster_audit" in alpha673, "Runtime roster coverage command missing")
    require("CombatKitCoverageService.HasCombatKit" in alpha673, "Runtime skill coverage check missing")
    require("SpecialRecruitCombatService.HasSpecialCombatKit" in coverage, "Special kit coverage hook missing")

    # Preserve 6.7.2 and Pelipper/source-authority regressions.
    require("UnlockVanillaMovementAnimation(npc);" in follow, "6.7.2 vanilla animation unlock regression")
    require("npc.Sprite.ignoreStopAnimation = false;" in follow, "Gus animation lock regression")
    require("hotPlateColor" not in combat, "Accidental Gus custom visual returned")
    require("private const int CombatPathRetryCooldownTicks = 24;" in combat, "Combat retry cadence changed")
    require("private const int CombatMovementPulseTicks = 3;" in combat, "Combat movement cadence changed")
    require("Math.Min(1739, Game1.uiViewport.Width - 12)" in codex, "Codex 115% regression")
    require("Math.Min(1518, Game1.uiViewport.Width - 16)" in profile_ui, "Profile 115% regression")

    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    require("PelipperRenderSuppressedAlpha6613" not in all_source, "Legacy Pelipper render suppression returned")
    require("TrySetActorInvisibleAlpha6613" not in all_source, "Legacy Pelipper visibility writer returned")
    require("isTileLocationTotallyClearAndPlaceable" not in follow, "Follow water/path regression returned")
    require("isTileLocationTotallyClearAndPlaceable" not in combat, "Combat water/path regression returned")

    default_json = json.loads((SRC / "i18n" / "default.json").read_text(encoding="utf-8"))
    vi_json = json.loads((SRC / "i18n" / "vi.json").read_text(encoding="utf-8"))
    require(set(default_json) == set(vi_json), "default/vi i18n key sets differ")

    # Static known-roster coverage gate for the new SVE entries. Dynamic mod-updated actors are
    # audited in-game by teamup_roster_audit because CI can't load the user's external mod set.
    for npc in ["Marlon", "Henchman"]:
        require(f'characterName.Equals("{npc}"' in special or f'|| characterName.Equals("{npc}"' in special, f"{npc} special combat kit coverage missing")
    require('Skills["Morris"]' in skills, "Morris standard combat kit coverage missing")

    log("SOURCE ACCEPTANCE: PASS")
    log("Building Alpha 6.7.3...")
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
        "CombatRankCatalog",
        "SpecialRecruitCombatService",
        "MimiTrueFormDurationTicks",
        "TRUE FORM",
        "MONSTER HUNTER",
        "VOID MAYO SPLASH",
        "CORPORATE LEVERAGE",
        "teamup_roster_audit",
        "Henchman",
        "Marlon",
        "Morris",
        "UnlockVanillaMovementAnimation",
    ]:
        require(dll_contains(blob, token), f"DLL missing token: {token}")
    for forbidden in [
        "PelipperRenderSuppressedAlpha6613",
        "TrySetActorInvisibleAlpha6613",
        "hotPlateColor",
    ]:
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

    SMOKE.write_text(
        "TEAM UP v0.2.0-alpha.6.7.3 - SMOKE TEST RANK + SPECIAL RECRUITS\n"
        "=============================================================\n\n"
        "1) Mở Codex/Profile: mọi NPC phải có Rank D/C/B/A/S.\n"
        "   MiMi = S + BOSS + SPECIAL. Marlon = S + LEGENDARY.\n"
        "   Sudoku = A + SPECIAL. Henchman = B + SPECIAL.\n\n"
        "2) MiMi trong party, vào combat áp lực cao (4+ quái / Farmer thấp máu / boss).\n"
        "   TRUE FORM chỉ kéo dài khoảng 4 giây, cooldown rất dài.\n"
        "   Hiện tại nếu Cardcha chưa expose boss-form API, MiMi dùng celestial aura; KHÔNG copy sprite boss.\n\n"
        "3) Marlon: MONSTER HUNTER ưu tiên mục tiêu MaxHP cao, không spam liên tục.\n\n"
        "4) Henchman: chỉ recruit nếu sprite live đủ directional surface; VOID MAYO SPLASH cần >=2 quái gần.\n\n"
        "5) Morris: profile Support/Control và skill CORPORATE LEVERAGE hoạt động ở tier signature.\n\n"
        "6) Console: teamup_roster_audit\n"
        "   Kỳ vọng PASS với roster hiện đang load; nếu WARNING gửi log NPC bị thiếu.\n\n"
        "7) Regression: party 5 người tổng (Farmer+4 NPC), Companion 2/2, Gus walk/facing vanilla, Pelipper không 3/2.\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.3")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
