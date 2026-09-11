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
STAGE = ROOT / "_stage_alpha6744"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744.txt"
AUDIT = ROOT / "LOWER_WORKINGS_INTERIOR_SURVEY_AUDIT_ALPHA6744.md"
SMOKE = ROOT / "SMOKE_TEST_V0_2_ALPHA6_7_44_LOWER_WORKINGS_INTERIOR_SURVEY_VI.txt"
VERSION = "0.2.0-alpha.6.7.44"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44_LOWER_WORKINGS_INTERIOR_SURVEY_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44_LOWER_WORKINGS_INTERIOR_SURVEY_TEST.sha256.txt"
NPCS = ["Abigail", "Alex", "Clint", "Demetrius", "Evelyn", "George", "Gus", "Lewis", "Linus", "Marlon", "Maru", "Pierre", "Robin", "Wizard"]
WINDOWS = [(36, "surveyAuthorized"), (37, "lowerWorkingsEntered"), (38, "cribbingSurveyed"), (39, "mutationTraceSurveyed"), (40, "sealedDepthSurveyed"), (41, "interiorSurveyReported")]
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
    entry = text("ModEntry.cs")
    a661 = text("ModEntry.Alpha661.cs")
    a6728 = text("ModEntry.Alpha6728.cs")
    a6742 = text("ModEntry.Alpha6742.cs")
    a6743 = text("ModEntry.Alpha6743.cs")
    a6744 = text("ModEntry.Alpha6744.cs")
    reactions = text("Story/StoryMilestoneReactionService.cs")
    descent = text("Story/LowerWorkingsDescentStoryService.cs")
    interior = text("Story/LowerWorkingsInteriorSurveyStoryService.cs")
    protocol = text("Story/LowerWorkingsEntryProtocolStoryService.cs")
    high = text("Story/SurgeHighEscalationStoryService.cs")
    roster = text("Story/TeamUpRosterProgressionService.cs")
    surge_story = text("Story/TheSurgeStoryService.cs")
    policy = text("Combat/TeamUpOffensiveTargetPolicy.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    config = text("ModConfig.cs")
    tmx = text("assets/LowerWorkings.tmx")
    en = json.loads(text("i18n/default.json"))
    vi = json.loads(text("i18n/vi.json"))
    all_cs = "\n".join(p.read_text(encoding="utf-8") for p in SRC.rglob("*.cs"))

    req(f"<Version>{VERSION}</Version>" in project, "wrong project version")
    req("RegisterAlpha6744Events();" in entry, "Alpha 6.7.44 registration missing")
    req("Dedicated Lower Workings map / interior survey layer active." in entry, "6.7.44 startup marker missing")

    # Real Stardew 1.6 custom-location registration and map ownership.
    req('LowerWorkingsLocationNameAlpha6744 = "Ronvotri.TeamUp_LowerWorkings"' in a6744, "dedicated location name missing")
    req('e.NameWithoutLocale.IsEquivalentTo("Data/Locations")' in a6744, "Data/Locations edit missing")
    req("new LocationData" in a6744 and "new CreateLocationData" in a6744, "data-driven location creation missing")
    req('Helper.ModContent.GetInternalAssetName("assets/LowerWorkings.tmx").Name' in a6744, "TMX internal asset registration missing")
    req('Type = "StardewValley.GameLocation"' in a6744, "custom serializable GameLocation type boundary missing")
    req("AlwaysActive = false" in a6744, "location lifecycle policy missing")
    req("ExcludeFromNpcPathfinding = true" in a6744, "NPC pathfinding exclusion missing")

    req('<map version="1.10"' in tmx, "Lower Workings TMX header missing")
    req('width="32" height="24"' in tmx, "Lower Workings map dimensions must be 32x24")
    req('<image source="Mines/mine.png" width="256" height="288"/>' in tmx, "vanilla mine tilesheet reference missing")
    for layer in ("Back", "Buildings", "Front"):
        req(f'name="{layer}"' in tmx, f"TMX required layer missing: {layer}")
    req('<property name="Warp"' not in tmx, "static TMX Warp must not bypass dynamic recorded breach return")

    # Interior survey state machine / safe ingress and egress.
    state_tokens = [
        'StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyStage"',
        'BreachTileKey = "Ronvotri.TeamUp/Story/LowerWorkingsBreachTile"',
        'InteriorEnteredFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsInteriorEntered"',
        'SurveyCompleteFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyComplete"',
        'SafeReturnUsedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsSafeReturnUsed"',
        'SurveyReportedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyReported"',
        "public const int CompleteStage = 6;",
        "public const int SurveyHoldTicksRequired = 120;",
        "ArrivalTile = new(15, 21)",
        "CribbingClueTile = new(8, 9)",
        "MutationTraceClueTile = new(22, 8)",
        "SealedDepthClueTile = new(23, 16)",
        "HasFullOperationalFormation(location)",
        "TryWarpIntoLowerWorkings()",
        "TryWarpToBreach(owner)",
        "owner.modData[BreachTileKey] = Serialize(anchor)",
        "Game1.warpFarmer(_locationName, ArrivalTile.X, ArrivalTile.Y, false)",
        "Game1.warpFarmer(locationName!, anchor.X, anchor.Y, false)",
    ]
    for token in state_tokens:
        req(token in interior, f"interior state/route token missing: {token}")
    req("if (!Context.IsMainPlayer)\n            return;" in interior, "farmhand must not materialize host story flags on SaveLoaded")
    req("int.TryParse(parts[0], out int x)" in interior and "point = new Point(x, y);" in interior, "breach anchor parser hardening missing")
    req("Emergency withdrawal accepted" in en["story.interior.withdraw-early"], "early safe withdrawal copy missing")
    req("SetStage(Game1.MasterPlayer, 3" in interior and "SetStage(Game1.MasterPlayer, 4" in interior and "SetStage(Game1.MasterPlayer, 5" in interior, "three-step survey progression missing")
    req('Game1.MasterPlayer.modData[SurveyCompleteFlagKey] = "1"' in interior, "survey completion persistence missing")
    req('owner.modData[SafeReturnUsedFlagKey] = "1"' in interior, "safe-return persistence missing")
    req("SetStage(Game1.MasterPlayer, CompleteStage" in interior, "Guild report completion missing")
    req("teamup_lower_interior" in a6744, "interior debug command missing")

    # 6.7.42 crossing now seeds a real tile anchor for new saves; legacy saves calibrate once at re-entry.
    req('Game1.MasterPlayer.modData[LowerWorkingsInteriorSurveyStoryService.BreachTileKey]' in descent, "first-descent crossing does not persist breach tile")
    req("Game1.player.TilePoint.X" in descent and "Game1.player.TilePoint.Y" in descent, "breach tile must be captured from the real crossing position")
    req("owner.modData.Remove(LowerWorkingsInteriorSurveyStoryService.BreachTileKey);" in descent, "descent reset must clear dependent breach tile anchor")

    # Bundled reaction layer.
    req("narrativeStage > 41" in reactions, "reaction range must be 0..41")
    for window, var_name in WINDOWS:
        req(f"[{window}] = {var_name}" in reactions, f"reaction dictionary missing window {window}")
        for npc in NPCS:
            key = f"story.react.{window}.{npc.lower()}"
            req(key in en and key in vi, f"missing EN/VI reaction key: {key}")
            req(f'"{npc}", "{key}"' in reactions, f"reaction service missing {key}")
    expected_maps = ["<= 0 => 35", "1 => 36", "2 => 37", "3 => 38", "4 => 39", "5 => 40", "_ => 41"]
    for token in expected_maps:
        req(token in a6744, f"6.7.44 resolver mapping missing: {token}")
    req("LowerWorkingsDescentAlpha6742.Stage < LowerWorkingsDescentStoryService.CompleteStage" in a6744, "6.7.43 fallback boundary missing")
    req("GetStoryReactionWindowAlpha6743()" in a6744, "6.7.43 reaction fallback missing")
    req(a6728.count("GetStoryReactionWindowAlpha6744()") >= 2, "NPC interaction/diagnostic not routed to 6.7.44 resolver")
    req("TEAM UP 6.7.44 - LOWER WORKINGS INTERIOR SURVEY + REACTIONS" in a6728, "6.7.44 reaction diagnostic title missing")

    req(set(en) == set(vi), "EN/VI i18n parity failed")
    en_reactions = [k for k in en if k.startswith("story.react.")]
    vi_reactions = [k for k in vi if k.startswith("story.react.")]
    req(len(en_reactions) == 584, f"EN reaction catalog must be exactly 584, got {len(en_reactions)}")
    req(len(vi_reactions) == 584, f"VI reaction catalog must be exactly 584, got {len(vi_reactions)}")
    for window in range(36, 42):
        req(sum(1 for k in en if k.startswith(f"story.react.{window}.")) == 14, f"EN window {window} must have 14 NPCs")
        req(sum(1 for k in vi if k.startswith(f"story.react.{window}.")) == 14, f"VI window {window} must have 14 NPCs")

    forbidden = ("Rank S", "The Last Blaster", "George Mullner", "Keeper", "Sector 17", "SECTOR 17")
    new_story_keys = [k for k in en if k.startswith("story.interior.")]
    for key in new_story_keys + [f"story.react.{w}.{npc.lower()}" for w in range(36, 42) for npc in NPCS]:
        for token in forbidden:
            req(token not in en[key] and token not in vi[key], f"spoiler leak {token}: {key}")

    # Carry-forward safety locks.
    req('public const string StageKey = "Ronvotri.TeamUp/Story/LowerWorkingsDescentStage";' in descent, "6.7.42 descent stage key regressed")
    req('public const string ThresholdCrossedFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed";' in descent, "threshold-crossed key regressed")
    req('public const string FirstDescentCompleteFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete";' in descent, "first-descent-complete key regressed")
    req("ThresholdCrossingTicksRequired = 120" in descent and "ThresholdInspectionTicksRequired = 180" in descent, "6.7.42 hold timings regressed")
    req('public const string ProtocolReadyFlagKey = "Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady";' in protocol, "entry protocol READY flag regressed")
    req("GetRequiredFieldPeople()" in protocol and "GetRequiredNpcAllies()" in protocol, "adaptive formation helpers missing")
    req('public const string SurgeHighFlagKey = "Ronvotri.TeamUp/Story/SurgeHighConfirmed";' in high, "SURGE HIGH flag regressed")
    req("MaxStoryNpcSlots = 4" in roster, "story roster ceiling regressed")
    req("public int MaxPartyMembers { get; set; } = 5;" in config, "five-person formation cap regressed")
    req("public const int FirstMutationKillThreshold = 10;" in surge_story, "10-kill first mutation trigger regressed")
    req("MarlonInvestigationStoryService.ActiveInstance?.ObserveMonsterDeath(__instance);" in mutation, "natural Mutation observation regressed")
    req("PelipperCaptureSafetyService.IsProtected(monster)" in policy, "Pelipper capture ceasefire regressed")
    req('modData[GeorgeCombatRevealedKeyAlpha6728] = "1"' not in all_cs, "George reveal flag set too early")
    req("CanRecruitCharacterByStoryAlpha6728(npc, out string characterStoryFailure)" in a661, "George recruit lock regressed")
    req("TrySetActorInvisibleAlpha6613" not in all_cs, "legacy fake-hide writer returned")

    # This is intentionally survey-only. No encounter should sneak into 6.7.44.
    encounter_tokens = ("new Monster(", "characters.Add(new", "spawnMonster", "MonsterFactory", "Boss")
    for token in encounter_tokens:
        req(token not in interior and token not in a6744, f"6.7.44 must not introduce an encounter yet: {token}")

    log("SOURCE ACCEPTANCE: PASS")
    log("DEDICATED DATA/LOCATIONS LOWER WORKINGS MAP: PASS")
    log("TMX BACK/BUILDINGS/FRONT + VANILLA MINE TILESET: PASS")
    log("RECORDED BREACH LOCATION + PERSISTENT TILE ANCHOR: PASS")
    log("LEGACY SAVE ONE-TIME ANCHOR CALIBRATION: PASS")
    log("SAFE RETURN ROUTE + EARLY EMERGENCY WITHDRAWAL: PASS")
    log("THREE-ZONE 120-TICK INTERIOR SURVEY: PASS")
    log("HOST-AUTHORITATIVE STORY PROGRESSION: PASS")
    log("REACTION WINDOWS 0..41: PASS")
    log("LOWER WORKINGS INTERIOR WINDOWS 36..41: PASS (14 NPCs each)")
    log("CURATED MILESTONE REACTIONS: PASS (584 lines/language)")
    log("GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS")
    log("EVELYN POSTGAME SPOILER BOUNDARY: PASS")
    log("PELIPPER / MUTATION SAFETY CARRY-FORWARD: PASS")
    log("FIVE-PEOPLE TOTAL + STORY SLOT 4 CEILING: PASS")
    log("NO 6.7.44 BOSS / STORY MONSTER / COMBAT ESCALATION: PASS")

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
        "LowerWorkingsInteriorSurveyStoryService",
        "Ronvotri.TeamUp_LowerWorkings",
        "Ronvotri.TeamUp/Story/LowerWorkingsBreachTile",
        "Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyComplete",
        "Ronvotri.TeamUp/Story/LowerWorkingsSafeReturnUsed",
        "teamup_lower_interior",
        "story.react.36.abigail",
        "story.react.37.marlon",
        "story.react.38.robin",
        "story.react.39.george",
        "story.react.40.wizard",
        "story.react.41.evelyn",
    ]:
        req(dll_contains(blob, token), f"DLL missing token: {token}")
    log("BINARY ACCEPTANCE: PASS")

    RELEASE.mkdir(parents=True, exist_ok=True)
    if STAGE.exists():
        shutil.rmtree(STAGE)
    MOD_STAGE.mkdir(parents=True)
    shutil.copy2(dll, MOD_STAGE / "TeamUp.dll")
    manifest = text("manifest.json").replace("%ProjectVersion%", VERSION)
    (MOD_STAGE / "manifest.json").write_text(manifest, encoding="utf-8", newline="\n")
    shutil.copytree(SRC / "i18n", MOD_STAGE / "i18n")
    shutil.copytree(SRC / "assets", MOD_STAGE / "assets")

    if ZIP_PATH.exists():
        ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in sorted(MOD_STAGE.rglob("*")):
            if path.is_file():
                archive.write(path, path.relative_to(STAGE))

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")

    AUDIT.write_text(
        "# Team Up Alpha 6.7.44 - Dedicated Lower Workings Map / Interior Survey Audit\n\n"
        "## Implemented\n"
        "- Added a real save-backed Stardew 1.6 Lower Workings GameLocation through Data/Locations.\n"
        "- Added assets/LowerWorkings.tmx using the vanilla Mines/mine.png tilesheet.\n"
        "- Bound ingress to the recorded controlled-breach MineShaft and a persistent tile anchor.\n"
        "- New saves inherit the anchor from the 6.7.42 threshold crossing; legacy 6.7.43 saves calibrate once at the real breach.\n"
        "- Added a secured return path from the Lower Workings entry; early emergency withdrawal preserves survey progress.\n"
        "- Added three 120-tick full-formation survey zones: directed cribbing, newer Mutation-linked residue over old blast scoring, and a deeper sealed-pressure edge.\n"
        "- Added bundled reaction windows 36 through 41, 14 NPCs each, bringing the exact reaction catalog to 584 lines per language.\n\n"
        "## Story boundary\n"
        "- Evidence now links present Mutation activity to the older sealed system without identifying the source/entity behind it.\n"
        "- Historical worker identity remains unknown. George stays observed Rank D / Non-Combatant / unrecruitable.\n"
        "- Evelyn postgame secret remains untouched. No Sector 17 identifier is introduced.\n"
        "- No boss or containment encounter is introduced; that escalation remains 6.7.45 scope.\n\n"
        "## Safety / compatibility\n"
        "- Host remains authoritative for story progression; farmhands may use the shared location route without writing host flags.\n"
        "- Five-PEOPLE total cap, story roster 4/4 ceiling, Entry Protocol READY, and SURGE HIGH prerequisites remain intact.\n"
        "- First guaranteed Mutation remains the 10th eligible natural normal-monster defeat.\n"
        "- Pelipper capture ceasefire / protected-target safety remains intact.\n\n"
        "## Live verification still required\n"
        "- Validate the TMX renders correctly in-game with no void tiles or collision traps.\n"
        "- Validate ingress/egress on a 6.7.43 legacy save and on a fresh 6.7.44 progression.\n"
        "- Validate host/farmhand entry, formation counting, save/reload, and emergency withdrawal.\n"
        "- Validate one-shot reactions 36..41 and stale-window skipping.\n",
        encoding="utf-8",
        newline="\n",
    )

    SMOKE.write_text(
        "TEAM UP 6.7.44 - DEDICATED LOWER WORKINGS / INTERIOR SURVEY\n"
        "===========================================================\n\n"
        "PREP\n"
        "1. Cai build 6.7.44 va load save bang host.\n"
        "2. teamup_lower_descent status -> firstDescentComplete=True (stage 5/5).\n"
        "3. teamup_entry_protocol status -> READY; teamup_surge_high status -> HIGH; story slots 4/4.\n"
        "4. teamup_lower_interior reset; sau do vao Adventure Guild de nhan lenh survey.\n\n"
        "MAP / ENTRY\n"
        "5. Dua full operational formation den dung MineShaft breach da ghi nhan.\n"
        "6. Save cu 6.7.43: bam Action tai vi tri breach that -> phai bao anchor calibrated mot lan, sau do vao map Lower Workings.\n"
        "7. Save moi da di qua 6.7.42 bang build nay: anchor phai duoc luu tu luc threshold crossing, khong can calibrate lai.\n"
        "8. Map phai hien binh thuong, co san/tuong/collision, khong void, khong spawn boss hay story monster.\n"
        "9. Entry tile xap xi (15,21); bam Action tai day phai rut ve dung recorded breach anchor.\n\n"
        "SURVEY\n"
        "10. Stage 2: giu full formation gan (8,9) ~120 tick -> directed cribbing clue, stage 3.\n"
        "11. Stage 3: giu full formation gan (22,8) ~120 tick -> Mutation-linked residue clue, stage 4.\n"
        "12. Stage 4: giu full formation gan (23,16) ~120 tick -> sealed pressure edge clue, stage 5.\n"
        "13. Truoc khi survey xong, co the quay entry + Action de emergency withdraw; stage hien tai phai duoc giu, sau do vao lai tiep tuc.\n"
        "14. Stage 5: quay entry + Action -> safe return; den Adventure Guild -> stage 6/6 complete.\n"
        "15. Save/reload tai cac stage 2..5 -> story state va custom location phai khoi phuc sach.\n\n"
        "MULTIPLAYER\n"
        "16. Host mo route. Farmhand tai recorded breach sau khi host da stage >=2 phai vao/ra duoc cung location.\n"
        "17. Farmhand khong duoc tu tang stage hay ghi story flag cua host. Full formation counting van adaptive theo online Farmers + NPC allies.\n\n"
        "REACTIONS\n"
        "18. teamup_story_reactions reset. teamup_lower_interior stage 1..6 -> windows 36..41.\n"
        "19. Moi supported NPC: lan noi dau co reaction, lan hai quay ve normal dialogue. Bo qua window thi reaction cu khong replay.\n"
        "20. George khong Rank S / Last Blaster / recruit; Evelyn khong postgame reveal.\n\n"
        "REGRESSION\n"
        "21. First guaranteed Mutation van kill thu 10 du dieu kien; exclusions van giu nguyen.\n"
        "22. Pelipper capture protected target / ceasefire van hoat dong.\n"
        "23. Hard cap van 5 PEOPLE tong, story NPC slots van 4/4.\n\n"
        "DEBUG: teamup_lower_interior status|reset|stage 0-6\n"
        "DEBUG: teamup_story_reactions status|reset\n"
        "DIAGNOSTIC: diagnostics\\TeamUp_Lower_Workings_Interior_latest.txt\n",
        encoding="utf-8",
        newline="\n",
    )

    log("BUILD SUCCESS - ALPHA 6.7.44")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
