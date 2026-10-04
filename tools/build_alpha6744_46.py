from __future__ import annotations

import hashlib
import json
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"
RELEASE = ROOT / "release"
STAGE = ROOT / "_stage_alpha6744_46"
MOD_STAGE = STAGE / "Team Up"
LOG = ROOT / "BUILD_LOG_ALPHA6744_46.txt"
VERSION = "0.2.0-alpha.6.7.44.46"
ZIP_NAME = "TeamUp_v0.2.0-alpha.6.7.44.46_AUTO_ROUTE_TEST.zip"
ZIP_PATH = RELEASE / ZIP_NAME
SHA_PATH = RELEASE / "TeamUp_v0.2.0-alpha.6.7.44.46_AUTO_ROUTE_TEST.sha256.txt"
lines: list[str] = []

def log(value: str) -> None:
    print(value)
    lines.append(value)

def req(condition: bool, message: str) -> None:
    if not condition:
        raise RuntimeError(message)

def text(rel: str) -> str:
    return (SRC / rel).read_text(encoding="utf-8")

try:
    project = text("TeamUp.csproj")
    manifest = json.loads(text("manifest.json"))
    factory = text("Combat/MonsterMutationMinionFactory.cs")
    mutation = text("Combat/MonsterMutationService.cs")
    native = text("Core/Alpha674418NativeMutationMinionService.cs")
    gate = text("Core/Alpha674438LowerWorkingsRuntimeGateV2Service.cs")
    lower_wiring = text("ModEntry.Alpha6744.cs")
    native_minions = text("Core/Alpha674418NativeMutationMinionService.cs")
    runtime_wiring = text("ModEntry.Alpha67446.cs")
    preflight = text("ModEntry.Alpha674440.cs")
    legacy_preflight = text("ModEntry.Alpha6715.cs")
    source_mutation = text("Core/Alpha67448PelipperSourceMutationService.cs")
    reward = text("Core/Alpha674414PelipperMutantRewardService.cs")
    capture = text("Core/Alpha674436EliteCaptureGuardService.cs")
    steering = text("Core/Alpha674423PelipperMutantLeaderSmoothingService.cs")
    aggro = text("Core/Alpha674420MutationAggroService.cs")
    story = text("Story/LowerWorkingsInteriorSurveyStoryService.cs")
    route_harness = text("ModEntry.Alpha674444.cs")
    auto_route = text("ModEntry.Alpha674446.cs")
    runtime_profiles = text("Core/RuntimeNpcProfileFallbackCatalog.cs")
    npc_profiles = text("Core/NpcProfileCatalog.cs")
    identity_catalog = text("Core/CharacterSkillIdentityCatalog.cs")
    i18n_default = json.loads(text("i18n/default.json"))
    i18n_vi = json.loads(text("i18n/vi.json"))
    map_path = SRC / "assets" / "LowerWorkings.tmx"

    req(f"<ProjectVersion>{VERSION}</ProjectVersion>" in project, "wrong project version")
    req(manifest.get("Version") == VERSION, "wrong manifest version")
    log("VERSION IDENTITY 6.7.44.46: PASS")

    forbidden = [
        "Core/Alpha674424EliteCombatFinalizationService.cs",
        "Core/Alpha674424EliteReachOverlayService.cs",
        "Core/Alpha674425PelipperLeaderContinuousChaseService.cs",
        "Core/Alpha674426PelipperMutantPhaseLifecycleService.cs",
        "Core/Alpha674427MutationRegressionGuardService.cs",
        "Core/Alpha674428LowerWorkingsRuntimeGateService.cs",
    ]
    for rel in forbidden:
        req(not (SRC / rel).exists(), f"old crash-stack service leaked into 44.45: {rel}")
    log("OLD 44.24-44.30 CRASH-STACK EXCLUSION: PASS")

    for token in [
        "Alpha674438LowerWorkingsRuntimeGateV2Service",
        "readonly=true",
        "ObserveWarp(GameLocation oldLocation, GameLocation newLocation)",
        'runtime-type-not-vanilla-gamelocation',
        "entry-mismatch",
        "return-mismatch",
        "TryReadPersistedBreach",
    ]:
        req(token in gate, f"Lower Workings v2 gate token missing: {token}")

    for forbidden_token in [
        "using HarmonyLib",
        "new Harmony(",
        "AppDomain.",
        "AssemblyLoad +=",
        "SaveLoaded +=",
        "UpdateTicked +=",
        "Game1.warpFarmer",
        "Helper.Events.",
    ]:
        req(forbidden_token not in gate, f"unsafe lifecycle/action leaked into v2 gate: {forbidden_token}")

    req('"teamup_lower_runtime"' in lower_wiring, "teamup_lower_runtime command missing")
    req("LowerWorkingsRuntimeGateV2Alpha674438.ObserveWarp(e.OldLocation, e.NewLocation);" in lower_wiring,
        "v2 gate is not attached to existing local Warped flow")
    req("LowerWorkingsRuntimeGateV2Alpha674438.ResetTelemetry();" in lower_wiring,
        "v2 telemetry reset command missing")
    req("LowerWorkingsRuntimeGateV2Alpha674438.Describe()" in lower_wiring,
        "v2 status output missing")
    req("LowerWorkingsRuntimeGateV2Alpha674438" not in lower_wiring.split("OnAlpha6744SaveLoaded",1)[1].split("OnAlpha6744Warped",1)[0],
        "v2 gate was added to SaveLoaded path")
    log("LOWER WORKINGS V2 READ-ONLY / LAZY WIRING: PASS")

    req('"teamup_lower_route_test"' in lower_wiring, "Lower route test command missing")
    req("_routeTestFormationBypass" in story, "runtime-only formation bypass field missing")
    req("SetRouteTestFormationBypass(bool enabled)" in story, "route test bypass setter missing")
    req("RouteTestFormationBypass => _routeTestFormationBypass" in story, "route test bypass state exposure missing")
    req("runtime-only formation bypass auto-cleared after return" in story, "route test bypass does not auto-clear after return")
    req("SetRouteTestFormationBypass(true)" in route_harness, "route harness cannot arm formation bypass")
    req("SetRouteTestFormationBypass(false)" in route_harness, "route harness cannot disable formation bypass")
    for forbidden_token in ["Game1.warpFarmer", "WriteConfig", "SetDebugStage", "MaxPartyMembers ="]:
        req(forbidden_token not in route_harness, f"route harness must not mutate config/story or direct-warp: {forbidden_token}")
    log("LOWER ROUTE RUNTIME-ONLY TEST HARNESS / NO CONFIG / NO DIRECT WARP: PASS")

    for token in [
        '"teamup_lower_route_auto"',
        "ResetTelemetry()",
        "SetRouteTestFormationBypass(true)",
        "SetDebugStage(Game1.MasterPlayer, 1)",
        "Game1.warpFarmer",
        "TryHandleLocalAction()",
        "GetPreflightSnapshot()",
        "RestoreAlpha674446InteriorState",
        "saveStateRestored=true",
        "AUTO TEST {status}",
    ]:
        req(token in auto_route, f"automatic Lower route token missing: {token}")
    for forbidden_token in ["WriteConfig", "MaxPartyMembers =", "MutationPhaseTotal", "PatchPelipperCaptureMethods("]:
        req(forbidden_token not in auto_route, f"automatic Lower route test leaked unrelated mutation/config behavior: {forbidden_token}")
    req("RegisterAlpha674446LowerRouteAutoTest();" in lower_wiring,
        "automatic Lower route test command is not registered")
    log("ONE-COMMAND LOWER ROUTE AUTO TEST / STORY SNAPSHOT RESTORE: PASS")

    for token in [
        'SourceId = "teamup-runtime-fallback"',
        "CompanionClassificationService.CanRecruitToMainParty",
        "PelipperTownCompatibilityService.LooksLikePelipperActor",
        "StableHash",
        "ProfileCache",
        "IdentityCache",
        "GetLiveProfiles()",
        "CharacterSkillIdentity",
    ]:
        req(token in runtime_profiles, f"runtime profile fallback token missing: {token}")
    req("RuntimeNpcProfileFallbackCatalog.GetProfile(characterName)" in npc_profiles,
        "NpcProfileCatalog does not resolve uncatalogued NPCs through fallback")
    req("RuntimeNpcProfileFallbackCatalog.GetLiveProfiles()" in npc_profiles,
        "available Codex profiles do not include live fallback NPCs")
    req("RuntimeNpcProfileFallbackCatalog.GetIdentity(characterName)" in identity_catalog,
        "fallback NPCs do not receive runtime signature identities")
    req("new GreenSlime" not in runtime_profiles, "runtime profile fallback must never create combat actors")
    req("WriteConfig" not in runtime_profiles and "modData[" not in runtime_profiles,
        "runtime profile fallback must not mutate config or story/save modData")
    for key in [
        "codex.runtime.tank.passive", "codex.runtime.tank.ability",
        "codex.runtime.damage.passive", "codex.runtime.damage.ability",
        "codex.runtime.support.passive", "codex.runtime.support.ability",
        "codex.runtime.healer.passive", "codex.runtime.healer.ability",
        "codex.runtime.control.passive", "codex.runtime.control.ability",
    ]:
        req(key in i18n_default, f"default runtime profile text missing: {key}")
        req(key in i18n_vi, f"Vietnamese runtime profile text missing: {key}")
    log("UNCATALOGUED NPC RUNTIME PROFILE + SKILL FALLBACK / COMPANION-SAFE: PASS")

    req(map_path.exists(), "LowerWorkings.tmx missing")
    root = ET.parse(map_path).getroot()
    req(root.tag == "map", "LowerWorkings.tmx root is not map")
    width = int(root.attrib.get("width", "0"))
    height = int(root.attrib.get("height", "0"))
    req((width, height) == (32, 24), f"LowerWorkings map must be 32x24, got {width}x{height}")

    properties = {p.attrib.get("name"): p.attrib.get("value", "") for p in root.findall("./properties/property")}
    req(properties.get("AmbientLight") == "45 50 60", "AmbientLight must be 45 50 60")
    req("Warp" not in properties, "static map Warp property is forbidden")

    layers = {layer.attrib.get("name") for layer in root.findall("./layer")}
    req({"Back", "Buildings", "Front"}.issubset(layers), f"required layers missing: {layers}")
    for layer in root.findall("./layer"):
        if layer.attrib.get("name") in {"Back", "Buildings", "Front"}:
            req(int(layer.attrib.get("width", "0")) == 32 and int(layer.attrib.get("height", "0")) == 24,
                f"layer size mismatch: {layer.attrib.get('name')}")

    image_sources = [img.attrib.get("source", "") for img in root.findall("./tileset/image")]
    req("Mines/mine.png" in image_sources, "vanilla Mines tilesheet missing")
    req(root.find("./objectgroup") is None, "static objectgroup/warp layer is not allowed in Lower Workings v2 contract")

    for token in [
        "ArrivalTile = new(15, 21)",
        "CribbingClueTile = new(8, 9)",
        "MutationTraceClueTile = new(22, 8)",
        "SealedDepthClueTile = new(23, 16)",
    ]:
        req(token in story, f"story point contract missing: {token}")
    log("LOWER WORKINGS TMX 32x24 / LAYERS / LIGHT / NO STATIC WARP: PASS")

    for token in [
        '"teamup_build"',
        '"teamup_mutation_regression"',
        '"teamup_preflight"',
        '[TeamUpBuild] version=',
        'branch=v0.2-alpha6-7-44-46-auto-route-test',
    ]:
        req(token in lower_wiring, f"runtime identity/early command token missing: {token}")
    req(lower_wiring.index('"teamup_mutation_regression"') < lower_wiring.index("RegisterAlpha67446RuntimeFixes();"),
        "regression command is not registered before runtime-fix initialization")
    req('"teamup_mutation_regression"' not in runtime_wiring,
        "duplicate late regression command registration still exists")
    req('"teamup_preflight",' not in legacy_preflight, "legacy Alpha6715 still registers the primary teamup_preflight command")
    req('"teamup_preflight_legacy",' in legacy_preflight, "legacy diagnostic command was not renamed to teamup_preflight_legacy")
    req(lower_wiring.count('"teamup_preflight",') == 1, "unified layer must own exactly one primary teamup_preflight registration")
    log("PREFLIGHT COMMAND DEDUP / SINGLE PRIMARY OWNER: PASS")
    log("EARLY COMMAND REGISTRATION + BUILD IDENTITY: PASS")

    req('"nidoranmale" => "nidoran-m"' in native_minions, "Nidoran male canonical spawn token missing")
    req('"nidoranfemale" => "nidoran-f"' in native_minions, "Nidoran female canonical spawn token missing")
    req("[MutationNativePelipperRequest]" in native_minions, "native Pelipper request token telemetry missing")
    log("NIDORAN GENDER-EXACT PELIPPER SPAWN TOKEN: PASS")

    for token in [
        "OnAlpha674440PreflightCommand",
        "Team Up preflight:",
        "BuildAlpha674440MutationAudit",
        "nidoranLive=",
        "lowerRoute=",
        "GetPreflightSnapshot()",
        "Alpha674440Version = \"0.2.0-alpha.6.7.44.46\"",
        "Alpha674440Branch = \"v0.2-alpha6-7-44-46-auto-route-test\"",
    ]:
        req(token in preflight, f"unified preflight token missing: {token}")
    req("public PreflightSnapshot GetPreflightSnapshot()" in gate, "Lower Workings preflight snapshot missing")
    req("internal static string ResolvePelipperSpawnToken" in native_minions, "spawn-token mapping is not exposed to preflight")
    log("UNIFIED FINAL RUNTIME PREFLIGHT: PASS")

    constructor = capture.split(
        "public Alpha674436EliteCaptureGuardService(IMonitor monitor, string uniqueId)", 1
    )[1].split("public string Describe()", 1)[0]
    req("PatchPelipperCaptureMethods(" not in constructor,
        "6.7.44.46 constructor still installs broad Pelipper capture Harmony hooks")
    req("Team Up 6.7.44.46 load-safe capture guard: Pelipper capture-method Harmony patching disabled; hooks=0." in capture,
        "6.7.44.46 load-safe capture guard startup proof token missing")
    req("load-safe-disabled" in capture,
        "6.7.44.46 capture guard diagnostic does not expose disabled state")
    req("PatchPelipperCaptureMethods(IEnumerable<Assembly> assemblies)" in capture,
        "dormant capture implementation unexpectedly removed; isolation should be reversible")
    log("CAPTURE GUARD LOAD-SAFE QUARANTINE / ZERO INSTALL CALL: PASS")

    req("public static Monster? Create(" in factory, "44.37 fail-closed factory regressed")
    req("new GreenSlime" not in factory, "GreenSlime fallback reintroduced")
    req("Monster? minion = MonsterMutationMinionFactory.Create(" in mutation, "legacy nullable fail-closed path regressed")
    req("Monster? candidate = MonsterMutationMinionFactory.Create(" in native, "native nullable fail-closed path regressed")
    req("OnAlpha674437MutationRegressionCommand" in runtime_wiring, "44.37 regression audit handler missing")
    req('"teamup_mutation_regression"' in lower_wiring, "44.37 regression audit command early registration missing")
    log("44.37 REGRESSION/STABILITY CARRY-FORWARD: PASS")

    req('PhaseTotalMarker = "Ronvotri.TeamUp/MutationPhaseTotal"' in source_mutation, "44.36 phase contract regressed")
    req("MonsterDropPrefix" in reward and "prematureDropBlocks" in reward, "44.36 reward guard regressed")
    req("Alpha674436EliteCaptureGuardService" in capture, "44.36 capture guard regressed")
    req("LeaderHoldCenterDistance = 128f" in steering and "LeaderAttackCenterDistance = 160f" in steering,
        "44.35 pursuit/reach regressed")
    req("MutationAggroRadiusTiles = BaseAggroRadiusTiles * 3" in aggro, "44.34 aggro arena regressed")
    log("44.34-44.36 MUTATION CONTRACT CARRY-FORWARD: PASS")

    proc = subprocess.run(
        ["dotnet", "build", str(SRC / "TeamUp.csproj"), "-c", "Release", "--nologo", "-warnaserror"],
        cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False,
    )
    print(proc.stdout, end="")
    lines.append(proc.stdout.rstrip())
    req(proc.returncode == 0, "dotnet build failed")
    req("0 Warning(s)" in proc.stdout, "build has warnings")
    req("0 Error(s)" in proc.stdout, "build has errors")
    log("C# BUILD: PASS")

    dll = SRC / "bin" / "Release" / "net6.0" / "TeamUp.dll"
    req(dll.exists(), "TeamUp.dll missing")
    RELEASE.mkdir(parents=True, exist_ok=True)
    if STAGE.exists():
        shutil.rmtree(STAGE)
    MOD_STAGE.mkdir(parents=True)
    shutil.copy2(dll, MOD_STAGE / "TeamUp.dll")
    shutil.copy2(SRC / "manifest.json", MOD_STAGE / "manifest.json")
    shutil.copytree(SRC / "i18n", MOD_STAGE / "i18n")
    shutil.copytree(SRC / "assets", MOD_STAGE / "assets")

    if ZIP_PATH.exists():
        ZIP_PATH.unlink()
    with zipfile.ZipFile(ZIP_PATH, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for path in sorted(MOD_STAGE.rglob("*")):
            if path.is_file():
                archive.write(path, path.relative_to(STAGE))

    with zipfile.ZipFile(ZIP_PATH, "r") as archive:
        names = set(archive.namelist())
        for required in [
            "Team Up/TeamUp.dll",
            "Team Up/manifest.json",
            "Team Up/i18n/default.json",
            "Team Up/i18n/vi.json",
            "Team Up/assets/LowerWorkings.tmx",
        ]:
            req(required in names, f"package missing {required}")
        packaged_manifest = json.loads(archive.read("Team Up/manifest.json").decode("utf-8"))
        req(packaged_manifest.get("Version") == VERSION, "packaged manifest mismatch")
    log("ZIP CONTENT AUDIT: PASS")

    digest = hashlib.sha256(ZIP_PATH.read_bytes()).hexdigest()
    SHA_PATH.write_text(f"{digest}  {ZIP_NAME}\n", encoding="utf-8")
    log("BUILD SUCCESS - ALPHA 6.7.44.46 AUTO LOWER ROUTE TEST")
    log(f"ZIP: {ZIP_NAME}")
    log(f"SHA256: {digest}")
finally:
    LOG.write_text("\n".join(lines) + "\n", encoding="utf-8")
