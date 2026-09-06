$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$baseline = Join-Path $root 'BuildV0_2Alpha6617.ps1'
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha661 = Join-Path $root 'src\TeamUp\ModEntry.Alpha661.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$alpha6615 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6615.cs'
$alpha6616 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6616.cs'
$alpha6617 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6617.cs'
$alpha6618 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6618.cs'
$bridge = Join-Path $root 'src\TeamUp\Core\PelipperVillagerCompanionRuntimeBridge.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6618'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_18_NPC_COMPANION_SOURCE_TRUTH_HARD_PREFLIGHT_VI.txt'
$version = '0.2.0-alpha.6.6.18'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.18_NPC_COMPANION_SOURCE_TRUTH_HARD_PREFLIGHT_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.18_NPC_COMPANION_SOURCE_TRUTH_HARD_PREFLIGHT_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Replace-Exact([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

try {
    foreach ($required in @($baseline,$project,$manifest,$modEntry,$alpha661,$alpha663,$alpha6615,$alpha6616,$alpha6617,$alpha6618,$bridge,$follow,$combat,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.18 source: $required" }
    }

    # Preserve all accepted 6.6.17 materialization first.
    & $baseline
    if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.6.17 baseline builder failed.' }
    if (Test-Path $log) { Remove-Item $log -Force }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.18')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.18 NPC Companion Source Truth + Hard Preflight Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    # Register 6.6.18 after 6.6.17 so source-truth repair runs after all legacy handlers.
    $a16 = Read-Lf $alpha6616
    $a16 = Replace-Exact $a16 @'
        EnsureAlpha6617EventsRegistered();

        if (!PendingDialogueShoulderAlpha6616.HasValue)
'@ @'
        EnsureAlpha6617EventsRegistered();
        EnsureAlpha6618EventsRegistered();

        if (!PendingDialogueShoulderAlpha6616.HasValue)
'@ '6.6.18 lazy registration'
    Write-Utf8 $alpha6616 $a16

    # Route NPC-linked Pelipper Call/Return through the source-native villager companion bridge.
    $a15 = Read-Lf $alpha6615
    $a15 = Replace-Exact $a15 @'
        NPC? actor = PelipperTownCompatibilityService.ResolveActor(unit);
        if (actor is null)
            return;

        PelipperDeploymentStateService.SetDesiredDeployment(
            actor,
            unit.OwnerCharacterName ?? string.Empty,
            deployed);
        TrySetPelipperSourceDeploymentAlpha6613(actor, deployed);
'@ @'
        if (unit.OwnerKind == CompanionOwnerKind.PartyMember
            && !string.IsNullOrWhiteSpace(unit.OwnerCharacterName))
        {
            NPC? owner = Game1.getCharacterFromName(unit.OwnerCharacterName);
            if (owner is not null && TrySetPelipperNpcSourceEnabledAlpha6618(
                owner,
                deployed,
                deployed ? "explicit Call" : "explicit Return/Standby"))
            {
                NPC? sourceActor = PelipperTownCompatibilityService.ResolveActor(unit);
                if (sourceActor is not null)
                {
                    PelipperDeploymentStateService.SetDesiredDeployment(
                        sourceActor,
                        unit.OwnerCharacterName,
                        deployed);
                }
                return;
            }
        }

        NPC? actor = PelipperTownCompatibilityService.ResolveActor(unit);
        if (actor is null)
            return;

        PelipperDeploymentStateService.SetDesiredDeployment(
            actor,
            unit.OwnerCharacterName ?? string.Empty,
            deployed);
        TrySetPelipperSourceDeploymentAlpha6613(actor, deployed);
'@ 'NPC source-native Call/Return bridge'
    Write-Utf8 $alpha6615 $a15

    # Restore Pelipper's original villager partner setting when the NPC itself leaves Team Up.
    $a663 = Read-Lf $alpha663
    $a663 = Replace-Exact $a663 @'
    private void ReleasePelipperOwnerAlpha663(NPC owner, CompanionUnitData? linked)
    {
        PelipperTownCompatibilityService.SetOwnerOptOut(owner, false);
'@ @'
    private void ReleasePelipperOwnerAlpha663(NPC owner, CompanionUnitData? linked)
    {
        RestorePelipperNpcSourceAlpha6618(owner);
        PelipperTownCompatibilityService.SetOwnerOptOut(owner, false);
'@ 'restore source setting when owner leaves'
    Write-Utf8 $alpha663 $a663

    # Hard authoritative preflight. UI cannot race or bypass 2/2 anymore.
    $a661 = Read-Lf $alpha661
    $a661 = Replace-Exact $a661 @'
        LiveCompanionDescriptor? detectedCompanion = CompanionIntegrationService.FindLinkedCompanion(npc);
        LiveCompanionDescriptor? liveCompanion = includeCompanion ? detectedCompanion : null;

        if (includeCompanion && liveCompanion is not null && !string.IsNullOrWhiteSpace(replacementCompanionUnitId))
        {
            CompanionUnitData? replacement = Party.GetCompanionByUnitIdAnyOwner(replacementCompanionUnitId);
            bool requesterMayReplace = replacement is not null
                && (replacement.RecruiterId == recruiterId || recruiterId == Game1.player.UniqueMultiplayerID);
            if (!requesterMayReplace)
            {
                SendActionResult(responsePlayerId, false, "That companion slot can no longer be replaced by this player.");
                return;
            }

            if (replacement is not null)
            {
                Party.SetCompanionState(replacement.UnitId, replacement.RecruiterId, CompanionDeploymentState.Standby);
                if (PelipperTownCompatibilityService.IsSourceControlled(replacement))
                {
                    NPC? replacementActor = PelipperTownCompatibilityService.ResolveActor(replacement);
                    if (replacementActor is not null)
                        PelipperDeploymentStateService.SetDesiredDeployment(replacementActor, replacement.OwnerCharacterName ?? string.Empty, false);
                }
                else
                {
                    NPC? replacementNpc = Game1.getCharacterFromName(replacement.CharacterName);
                    if (replacementNpc is not null)
                        Follow.ReleaseToVanilla(replacementNpc);
                }
            }
        }

        PartyAddResult result = Party.TryAddMember(npc.Name, recruiterId, GetOnlineFarmerIds());
'@ @'
        LiveCompanionDescriptor? detectedCompanion = CompanionIntegrationService.FindLinkedCompanion(npc);
        LiveCompanionDescriptor? liveCompanion = includeCompanion ? detectedCompanion : null;

        // Alpha 6.6.18: UI checks are not authoritative. Reconcile source-live slot truth at the
        // exact commit point. A together-recruit at real 2/2 must replace a slot successfully or
        // fail before the NPC is added; it can never silently create a third live companion.
        if (includeCompanion && liveCompanion is not null
            && !PrepareNpcCompanionRecruitCapacityAlpha6618(
                liveCompanion,
                recruiterId,
                replacementCompanionUnitId,
                out string capacityFailure))
        {
            SendActionResult(responsePlayerId, false, capacityFailure);
            return;
        }

        PartyAddResult result = Party.TryAddMember(npc.Name, recruiterId, GetOnlineFarmerIds());
'@ 'authoritative NPC+Pokemon preflight'

    $a661 = Replace-Exact $a661 @'
                if (Party.GetActiveCombatCompanionCount() < Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2))
                {
                    RequestOrRecruitAlpha661(npc, includeCompanion: true, replacementCompanionUnitId: null);
                    return;
                }
'@ @'
                if (HasFreeEffectiveCompanionSlotAlpha6618())
                {
                    RequestOrRecruitAlpha661(npc, includeCompanion: true, replacementCompanionUnitId: null);
                    return;
                }
'@ 'recruit UI uses effective live slots'

    $a661 = Replace-Exact $a661 @'
        List<CompanionUnitData> replaceable = Party.GetActiveCombatCompanions()
            .Where(unit => requesterIsHost || unit.RecruiterId == recruiterId)
            .Take(2)
            .ToList();
'@ @'
        List<CompanionUnitData> replaceable = GetEffectiveReplaceableCompanionsAlpha6618(
            recruiterId,
            requesterIsHost).ToList();
'@ 'replacement UI uses effective live slots'
    Write-Utf8 $alpha661 $a661

    # Avoid repeated expensive reflection scans if Pelipper exposes no compatible source contract.
    $a18 = Read-Lf $alpha6618
    $a18 = Replace-Exact $a18 @'
    private bool TrySetPelipperNpcSourceEnabledAlpha6618(NPC owner, bool enabled, string reason)
    {
        if (PelipperVillagerCompanionRuntimeBridge.TrySetEnabled(owner.Name, owner, enabled, out string route))
'@ @'
    private bool TrySetPelipperNpcSourceEnabledAlpha6618(NPC owner, bool enabled, string reason)
    {
        if (PelipperNpcNativeControlWarningsAlpha6618.Contains(owner.Name))
            return false;

        if (PelipperVillagerCompanionRuntimeBridge.TrySetEnabled(owner.Name, owner, enabled, out string route))
'@ 'native bridge failed-probe cooldown'
    Write-Utf8 $alpha6618 $a18

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $a661 = Read-Lf $alpha661
    $a663 = Read-Lf $alpha663
    $a15 = Read-Lf $alpha6615
    $a16 = Read-Lf $alpha6616
    $a17 = Read-Lf $alpha6617
    $a18 = Read-Lf $alpha6618
    $bridgeText = Read-Lf $bridge
    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.18</Version>')) { throw 'Version materialization failed.' }
    if (-not $modText.Contains('build: v0.2.0-alpha.6.6.18')) { throw 'ModEntry build string missing.' }
    if (-not $modText.Contains('NPC Companion Source Truth + Hard Preflight Hotfix loaded.')) { throw 'Loaded string missing.' }
    if (-not $a16.Contains('EnsureAlpha6618EventsRegistered();')) { throw '6.6.18 event registration missing.' }

    foreach ($token in @(
        'ReconcilePelipperNpcSlotTruthAlpha6618',
        'GetEffectiveCombatCompanionCountAlpha6618',
        'GetEffectiveReplaceableCompanionsAlpha6618',
        'PrepareNpcCompanionRecruitCapacityAlpha6618',
        'TrySetPelipperNpcSourceEnabledAlpha6618',
        'RestorePelipperNpcSourceAlpha6618',
        'source-live partner continues to consume a real slot'
    )) {
        if (-not $a18.Contains($token)) { throw "Alpha 6.6.18 token missing: $token" }
    }

    foreach ($token in @('TrySetEnabled','TrySetConfigState','TryInvokeSourceAction','RestoreAll','CompanionEnabled','PartnerEnabled')) {
        if (-not $bridgeText.Contains($token)) { throw "Pelipper runtime bridge token missing: $token" }
    }

    if (-not $a15.Contains('explicit Return/Standby')) { throw 'NPC Return source-native route missing.' }
    if (-not $a663.Contains('RestorePelipperNpcSourceAlpha6618(owner);')) { throw 'Owner source restore missing.' }
    if (-not $a661.Contains('PrepareNpcCompanionRecruitCapacityAlpha6618')) { throw 'Authoritative recruit preflight missing.' }
    if (-not $a661.Contains('HasFreeEffectiveCompanionSlotAlpha6618')) { throw 'Effective slot UI gate missing.' }
    if (-not $a661.Contains('GetEffectiveReplaceableCompanionsAlpha6618')) { throw 'Effective replacement list missing.' }

    if ($bridgeText.Contains('IsInvisible') -or $bridgeText.Contains('.Halt(') -or $bridgeText.Contains('.controller')) {
        throw '6.6.18 source bridge must not use render/movement suppression.'
    }
    if ($a18.Contains('TrySetActorInvisibleAlpha6613')) { throw '6.6.18 must not use invisibility fallback.' }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }
    if (-not $a17.Contains('ReconcilePelipperPlayerSlotTruthAlpha6617')) { throw '6.6.17 player slot truth regression.' }

    Log 'Building Alpha 6.6.18 NPC Companion Source Truth + Hard Preflight Hotfix...'
    Log 'NPC RETURN: linked Pelipper Pokemon uses source-native villager companion enable/recall bridge; no IsInvisible/Halt/controller fallback.'
    Log 'NPC SOURCE TRUTH: source-live partner remains a real slot consumer until Pelipper actually recalls it.'
    Log 'HARD PREFLIGHT: NPC+Pokemon recruitment rechecks effective source-live 2/2 immediately before commit.'
    Log 'REPLACE: full 2/2 requires a real replacement; if source recall fails, recruit fails instead of allowing 3/2.'
    Log 'NPC ONLY: people-only recruitment remains allowed when people capacity permits; opted-out partner is source-recalled while owner is active.'
    Log 'REGRESSION: 6.6.17 player ghost-slot truth, 6.6.16 P/L+R, 6.6.14 capture safety and 6.6.7 water performance preserved.'

    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    & dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    $dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

    if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
    if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
    Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force

    $manifestText = Read-Lf $manifest
    $manifestText = $manifestText.Replace('%ProjectVersion%', $version)
    Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.18 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $zipName`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

    Log 'PELIPPER NPC SOURCE-NATIVE RECALL/DEPLOY BRIDGE: ENABLED'
    Log 'NPC SOURCE-LIVE EFFECTIVE SLOT ACCOUNTING: ENABLED'
    Log 'NPC+POKEMON AUTHORITATIVE 2/2 PREFLIGHT: ENABLED'
    Log 'FULL POOL REQUIRES REAL REPLACEMENT OR REJECT: ENABLED'
    Log 'RENDER-ONLY INVISIBILITY FALLBACK: DISABLED'
    Log 'P / L+R NPC POKEMON ROUTING: PRESERVED'
    Log 'BUILD SUCCESS - ALPHA 6.6.18'
    Log "ZIP: $zipName"
    Log "SHA256: $hash"
}
catch {
    if (-not (Test-Path $log)) { New-Item -ItemType File -Path $log | Out-Null }
    "BUILD FAILED - ALPHA 6.6.18`r`n$($_.Exception.Message)" | Tee-Object -FilePath $log -Append
    throw
}
