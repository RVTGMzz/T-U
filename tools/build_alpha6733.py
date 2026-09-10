from __future__ import annotations
import hashlib, json, shutil, subprocess, zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6733"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6733.txt"
AUDIT = ROOT / "FIELD_TRIANGULATION_REACTIONS_AUDIT_ALPHA6733.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_33_FIELD_TRIANGULATION_REACTIONS_VI.txt"
VERSION = "0.2.0-alpha.6.7.33"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.33_FIELD_TRIANGULATION_REACTIONS_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.33_FIELD_TRIANGULATION_REACTIONS_TEST.sha256.txt"
lines: list[str] = []

def log(v: str) -> None:
    print(v); lines.append(v)
def req(c: bool, m: str) -> None:
    if not c: raise RuntimeError(m)
def text(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")
def dll_contains(blob: bytes, token: str) -> bool:
    return token.encode() in blob or token.encode("utf-16le") in blob

try:
    project = text("TeamUp.csproj")
    entry = text("ModEntry.cs")
    a661 = text("ModEntry.Alpha661.cs")
    a6727 = text("ModEntry.Alpha6727.cs")
    a6728 = text("ModEntry.Alpha6728.cs")
    a6729 = text("ModEntry.Alpha6729.cs")
    a6730 = text("ModEntry.Alpha6730.cs")
    a6731 = text("ModEntry.Alpha6731.cs")
    a6732 = text("ModEntry.Alpha6732.cs")
    a6733 = text("ModEntry.Alpha6733.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    field = text("Story/FieldTriangulationStoryService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge = text("Story/TheSurgeStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("narrativeStage > 13" in reactions, "reaction range must be 0..13")
    for w, name in [(10,"triangulationBriefed"),(11,"firstBearingRecorded"),(12,"secondBearingRecorded"),(13,"sealedCorridorTriangulated")]:
        req(f"[{w}] = {name}" in reactions, f"reaction window {w} missing")
    req("GetStoryReactionWindowAlpha6733()" in a6733, "6.7.33 resolver missing")
    req("FieldTriangulationAlpha6732.Stage switch" in a6733, "field stage resolver missing")
    req(a6728.count("GetStoryReactionWindowAlpha6733()") >= 2, "NPC interaction/diagnostic not routed to 6.7.33 resolver")

    new_keys = {f"story.react.{w}.{npc}" for w in range(10,14) for npc in ["abigail","alex","clint","demetrius","evelyn","george","gus","lewis","linus","marlon","maru","pierre","robin","wizard"]}
    req(set(en) == set(vi), "EN/VI i18n parity failed")
    req(new_keys.issubset(en) and new_keys.issubset(vi), "6.7.33 reaction localization incomplete")
    for w in range(10,14):
        req(sum(1 for k in en if k.startswith(f"story.react.{w}.")) == 14, f"EN window {w} must contain 14 NPCs")
        req(sum(1 for k in vi if k.startswith(f"story.react.{w}.")) == 14, f"VI window {w} must contain 14 NPCs")
    req(sum(1 for k in en if k.startswith("story.react.")) == 192, "EN reaction catalog must contain 192 lines")
    req(sum(1 for k in vi if k.startswith("story.react.")) == 192, "VI reaction catalog must contain 192 lines")

    for key in new_keys:
        for forbidden in ("Rank S", "The Last Blaster", "George Mullner", "Keeper"):
            req(forbidden not in en[key] and forbidden not in vi[key], f"spoiler leak {forbidden}: {key}")

    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("public const int CompleteStage = 4;" in field, "6.7.32 field route regressed")
    req("MinimumFieldPeople = 3" in field and "MinimumActiveNpcAllies = 1" in field, "field-team rule regressed")
    req("Game1.getOnlineFarmers().Count" in a6732, "co-op field count regressed")
    req("Distinct(StringComparer.OrdinalIgnoreCase)" in a6732, "NPC dedupe regressed")
    req("FirstBearingLocationKey" in field and "IsDifferentMineLocation" in field, "distinct MineShaft bearings regressed")
    req('UnlockTo(Game1.MasterPlayer, 4' not in all_cs, "story slot 4 unlocked too early")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person cap regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutant observation regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge, "10-kill trigger regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "capture ceasefire regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy Pelipper fake-hide writer returned")

    log("SOURCE ACCEPTANCE: PASS")
    log("REACTION WINDOWS 0..13: PASS")
    log("FIELD TRIANGULATION WINDOWS 10/11/12/13: PASS (14 NPCs each)")
    log("CURATED MILESTONE REACTIONS: PASS (192 lines/language)")
    log("NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.33 WINDOW RESOLVER: PASS")
    log("FIELD TRIANGULATION 6.7.32 CARRY-FORWARD: PASS")
    log("STORY NPC SLOT 4 REMAINS LOCKED: PASS")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("6.7.23-6.7.32 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS")
    log("LIVE ONE-SHOT DIALOGUE PACING STILL REQUIRED")

    proc = subprocess.run(["dotnet","build",str(SRC/"TeamUp.csproj"),"-c","Release","--nologo","-warnaserror"],cwd=ROOT,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,check=False)
    print(proc.stdout,end=""); lines.append(proc.stdout.rstrip())
    req(proc.returncode == 0, "dotnet build failed")
    req("0 Warning(s)" in proc.stdout, "build has warnings")
    req("0 Error(s)" in proc.stdout, "build has errors")

    dll = SRC/"bin"/"Release"/"net6.0"/"TeamUp.dll"
    req(dll.exists(), "TeamUp.dll missing")
    blob = dll.read_bytes()
    for token in ["GetStoryReactionWindowAlpha6733","story.react.10.george","story.react.13.marlon","teamup_story_reactions","teamup_triangulation","FieldTriangulationStoryService"]:
        req(dll_contains(blob, token), f"DLL missing token: {token}")
    log("BINARY ACCEPTANCE: PASS")

    RELEASE.mkdir(parents=True, exist_ok=True)
    if STAGE.exists(): shutil.rmtree(STAGE)
    MOD_STAGE.mkdir(parents=True)
    shutil.copy2(dll, MOD_STAGE/"TeamUp.dll")
    manifest = text("manifest.json").replace("%ProjectVersion%", VERSION)
    (MOD_STAGE/"manifest.json").write_text(manifest,encoding="utf-8",newline="\n")
    shutil.copytree(SRC/"i18n", MOD_STAGE/"i18n")
    if ZIP_PATH.exists(): ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH,"w",compression=zipfile.ZIP_DEFLATED,compresslevel=9) as archive:
        for p in sorted(MOD_STAGE.rglob("*")):
            if p.is_file(): archive.write(p,p.relative_to(STAGE))
    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n",encoding="utf-8")

    AUDIT.write_text("# Team Up Alpha 6.7.33 - Field Triangulation Reactions Audit\n\n## Implemented\n- Expanded one-shot milestone reactions from windows 0..9 to 0..13.\n- Window 10: three-person field briefing.\n- Window 11: first MineShaft bearing recorded.\n- Window 12: second independent bearing recorded.\n- Window 13: sealed-workings corridor triangulated.\n- Added 56 curated lines per language, total 192 reaction lines per language.\n- George remains plausible as an ordinary experienced miner; identity reveal stays locked.\n\n## Safety\n- Dialogue-only checkpoint. No combat, monster, capture, provider, map ownership, or roster-cap behavior changed.\n- Slot 4 remains locked.\n\n## Live verification still required\n- One-shot interaction timing for windows 10..13.\n- No stale reaction replay after field-stage advancement.\n",encoding="utf-8",newline="\n")

    SMOKE.write_text("TEAM UP 6.7.33 - FIELD TRIANGULATION REACTIONS\n================================================\n\n1. teamup_triangulation stage 1 -> reaction window 10.\n2. Noi chuyen NPC ho tro -> moi NPC chi co 1 reaction window 10.\n3. stage 2 -> window 11; stage 3 -> window 12; stage 4 -> window 13.\n4. Noi lai cung NPC trong cung window -> quay ve tuong tac binh thuong.\n5. Bo qua window cu roi advance -> khong replay reaction cu.\n6. George van Rank D / Non-Combatant / khong recruit.\n7. teamup_roster_story status -> van 3/4; slot 4 chua mo.\nDEBUG: teamup_story_reactions reset | status\nDIAGNOSTIC: diagnostics\\TeamUp_Milestone_Reactions_latest.txt\n",encoding="utf-8",newline="\n")

    log("BUILD SUCCESS - ALPHA 6.7.33")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines)+"\n",encoding="utf-8")
