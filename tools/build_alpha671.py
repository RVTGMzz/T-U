from __future__ import annotations

import hashlib
import json
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha671"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA671.txt"
VERSION = "0.2.0-alpha.6.7.1"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.1_LIVE_POLISH_DORMANT_COMPANION_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.1_LIVE_POLISH_DORMANT_COMPANION_TEST.sha256.txt"

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
    ascii_bytes = token.encode("utf-8")
    utf16_bytes = token.encode("utf-16le")
    return ascii_bytes in blob or utf16_bytes in blob


try:
    project = text("TeamUp.csproj")
    config = text("ModConfig.cs")
    banter = text("Core/PartyBanterService.cs")
    alpha661 = text("ModEntry.Alpha661.cs")
    alpha663 = text("ModEntry.Alpha663.cs")
    alpha6626 = text("ModEntry.Alpha6626.cs")
    alpha671 = text("ModEntry.Alpha671.cs")
    bridge = text("Core/PelipperTown119NativeBridge.cs")
    profiles = text("Core/NpcProfileCatalog.cs")
    identities = text("Core/CharacterSkillIdentityCatalog.cs")
    combat = text("Combat/CombatService.cs")
    follow = text("Following/FollowService.cs")
    codex = text("UI/CodexBrowserMenu.cs")
    profile_ui = text("UI/CharacterProfileMenu.cs")

    require(f"<Version>{VERSION}</Version>" in project, "Wrong project version")
    require("public int MaxPartyMembers { get; set; } = 5;" in config, "5-person party cap missing")
    require("public int MaxActiveLinkedCompanions { get; set; } = 2;" in config, "2-companion cap missing")
    require("new Color(72, 42, 28)" in banter, "Readable dark banter text missing")
    require("245, 235, 205" not in banter, "Pale banter text remains")
    require("6/6 people" not in alpha661, "Legacy 6/6 party-full message remains")
    require('party.full-hud' in alpha661, "Dynamic party-full translation missing")
    require("FindRecruitCandidateCompanionAlpha671" in alpha661, "Dormant recruit lookup not wired")
    require('recruit.invite-only' in alpha661 and 'recruit.invite-together' in alpha661, "Localized recruit choices missing")
    require('TrySetVillagerCompanionEnabled(owner.Name, includeCompanion' in alpha663, "Native dormant recruit choice not wired")
    require("TryGetConfiguredVillagerCompanionDescriptor" in bridge, "Configured dormant partner bridge missing")
    require("TryGetConfiguredCompanion" in bridge, "Verified Pelipper configured companion call missing")
    require("IsDormantConfiguredReservationAlpha671" in alpha671, "Dormant reservation helper missing")
    require("IsDormantConfiguredReservationAlpha671" in alpha6626, "Single authority does not account for dormant reservation")
    require('["Gunther"] = P(' in profiles, "Gunther combat profile missing")
    require('["Gunther"] = I(' in identities and 'RELIC SEAL' in identities, "Gunther signature identity missing")
    require('npc.Name.Equals("Gus"' in combat and 'HOT PLATE' in combat, "Gus visible combat tell missing")

    # Preserve hard-earned regressions.
    all_source = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))
    require("PelipperRenderSuppressedAlpha6613" not in all_source, "Legacy Pelipper render suppression returned")
    require("TrySetActorInvisibleAlpha6613" not in all_source, "Legacy Pelipper visibility writer returned")
    require("isTileLocationTotallyClearAndPlaceable" not in follow, "Follow water/path regression returned")
    require("isTileLocationTotallyClearAndPlaceable" not in combat, "Combat water/path regression returned")
    require("private const int CombatPathRetryCooldownTicks = 24;" in combat, "Combat retry cadence changed")
    require("private const int CombatMovementPulseTicks = 3;" in combat, "Combat movement cadence changed")
    require("Math.Min(1739, Game1.uiViewport.Width - 12)" in codex, "Codex 115% regression")
    require("Math.Min(1518, Game1.uiViewport.Width - 16)" in profile_ui, "Profile 115% regression")

    default_json = json.loads((SRC / "i18n" / "default.json").read_text(encoding="utf-8"))
    vi_json = json.loads((SRC / "i18n" / "vi.json").read_text(encoding="utf-8"))
    default_keys = set(default_json)
    vi_keys = set(vi_json)
    missing_vi = sorted(default_keys - vi_keys)
    extra_vi = sorted(vi_keys - default_keys)
    require(not missing_vi, f"Vietnamese i18n missing keys: {missing_vi}")
    require(not extra_vi, f"Vietnamese i18n has unmatched keys: {extra_vi}")
    require(vi_json.get("recruit.invite-only") == "Chỉ {{name}}", "Vietnamese recruit-only label wrong")
    require("gunther" in " ".join(k.lower() for k in vi_keys), "Gunther Vietnamese profile keys missing")

    log("SOURCE ACCEPTANCE: PASS")
    log("Building Alpha 6.7.1...")

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
        "TryGetConfiguredVillagerCompanionDescriptor",
        "TryGetConfiguredCompanion",
        "IsDormantConfiguredReservationAlpha671",
        "Gunther",
        "RELIC SEAL",
        "HOT PLATE",
        "party.full-hud",
        "recruit.invite-only",
    ]:
        require(dll_contains(blob, token), f"DLL missing token: {token}")

    for forbidden in [
        "TEAM UP PARTY FULL • 6/6 people",
        "PelipperRenderSuppressedAlpha6613",
        "TrySetActorInvisibleAlpha6613",
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
    log("BUILD SUCCESS - ALPHA 6.7.1")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
