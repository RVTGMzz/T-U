$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$alpha6615 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6615.cs'
$alpha6616 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6616.cs'
$pelipperCompat = Join-Path $root 'src\TeamUp\Core\PelipperTownCompatibilityService.cs'
$captureSafety = Join-Path $root 'src\TeamUp\Core\PelipperCaptureSafetyService.cs'
$capturePatch = Join-Path $root 'src\TeamUp\Core\PelipperCaptureDamagePatch.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6616'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.16_CONTROLLER_COMPANION_SLOT_RUNTIME_FIX_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.16_CONTROLLER_COMPANION_SLOT_RUNTIME_FIX_TEST.sha256.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_16_CONTROLLER_COMPANION_SLOT_RUNTIME_FIX_VI.txt'
$version = '0.2.0-alpha.6.6.16'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Replace-Exact([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

try {
    foreach ($required in @($project,$manifest,$modEntry,$alpha663,$alpha6615,$alpha6616,$pelipperCompat,$captureSafety,$capturePatch,$follow,$combat,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.16 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    $projectText = [regex]::Replace($projectText, '<EnableHarmony>[^<]+</EnableHarmony>', '<EnableHarmony>true</EnableHarmony>')
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.16')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.16 Controller Companion Slot Runtime Fix loaded.')

    $oldTick = @'
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        RunPendingUiAction();
'@
    $newTick = @'
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        UpdateDialogueCompanionInputAlpha6616();
        RunPendingUiAction();
'@
    $modText = Replace-Exact $modText $oldTick $newTick 'deferred L/R chord update'

    $oldDialogueInput = @'
            NPC? speaker = ResolveDialogueSpeaker();
            if (speaker is null || PartyActionConfirmationOpen)
                return;

            if (Config.ProfileKey.JustPressed())
'@
    $newDialogueInput = @'
            NPC? speaker = ResolveDialogueSpeaker();
            if (speaker is null || PartyActionConfirmationOpen)
                return;

            // Alpha 6.6.16: recruited-member controller shoulders are routed through a short
            // chord window so L+R opens Pokemon management before L=Profile or R=Leave can fire.
            // Keyboard P opens the same linked-companion menu.
            if (HandleDialogueCompanionInputAlpha6616(e, speaker))
                return;

            if (Config.ProfileKey.JustPressed())
'@
    $modText = Replace-Exact $modText $oldDialogueInput $newDialogueInput 'dialogue companion input precedence'
    Write-Utf8 $modEntry $modText

    $a15 = Read-Lf $alpha6615
    $oldHint = @'
        string keyLabel = Config.PartyMenuKey.ToString();
        string text = IsVietnameseAlpha6615()
            ? $"{keyLabel} Pokémon"
            : $"{keyLabel} Companion";
'@
    $newHint = @'
        // Alpha 6.6.16: make the actual keyboard/controller chord explicit. L+R is handled
        // before the legacy L=Profile / R=Leave actions, so it can never kick the NPC.
        string text = IsVietnameseAlpha6615()
            ? "P / L+R Pokémon"
            : "P / L+R Pokemon";
'@
    $a15 = Replace-Exact $a15 $oldHint $newHint 'P / L+R dialogue hint'

    $oldManage = @'
        CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
        if (linked is not null && PelipperTownCompatibilityService.IsSourceControlled(linked))
            return true;

        LiveCompanionDescriptor? detected = CompanionIntegrationService.FindLinkedCompanion(owner);
'@
    $newManage = @'
        CompanionUnitData? linked = Party.GetLinkedCompanion(member.CharacterName, member.RecruiterId);
        if (linked is not null && PelipperTownCompatibilityService.IsSourceControlled(linked))
            return true;

        // NPC-only is a durable intent marker even if Pelipper has already hidden the actor.
        // Keep the Pokemon shortcut visible so the player always has a path to Call it later.
        if (PelipperTownCompatibilityService.IsOwnerOptedOut(owner))
            return true;

        LiveCompanionDescriptor? detected = CompanionIntegrationService.FindLinkedCompanion(owner);
'@
    $a15 = Replace-Exact $a15 $oldManage $newManage 'NPC-only shortcut persistence'
    Write-Utf8 $alpha6615 $a15

    $a663 = Read-Lf $alpha663
    $oldRecruitChoice = @'
        if (!PelipperTownCompatibilityService.IsPelipperDescriptor(detectedCompanion))
            return;

        NPC? actor = PelipperTownCompatibilityService.ResolveActor(detectedCompanion!);
        if (actor is not null)
            PelipperDeploymentStateService.SetDesiredDeployment(actor, owner.Name, includeCompanion);
'@
    $newRecruitChoice = @'
        if (!PelipperTownCompatibilityService.IsPelipperDescriptor(detectedCompanion))
            return;

        // If the player explicitly chose NPC-only while the partner is visible, register the
        // partner immediately as Standby. This makes P / L+R recall deterministic instead of
        // waiting for a later 30-tick discovery pass which may miss a source-hidden actor.
        if (!includeCompanion)
        {
            PartyMemberData? member = Party.GetAnyOwner(owner.Name);
            if (member is not null && Party.GetLinkedCompanion(owner.Name, member.RecruiterId) is null)
            {
                Party.TryLinkCompanion(
                    detectedCompanion!.UnitId,
                    detectedCompanion.CharacterName,
                    detectedCompanion.DisplayName,
                    member.RecruiterId,
                    owner.Name,
                    CompanionUnitKind.ExternalCreature,
                    detectedCompanion.ProviderId,
                    detectedCompanion.ProviderUnitId,
                    requestActive: false);
            }
        }

        NPC? actor = PelipperTownCompatibilityService.ResolveActor(detectedCompanion!);
        if (actor is not null)
            PelipperDeploymentStateService.SetDesiredDeployment(actor, owner.Name, includeCompanion);
'@
    $a663 = Replace-Exact $a663 $oldRecruitChoice $newRecruitChoice 'immediate NPC-only standby registration'
    Write-Utf8 $alpha663 $a663

    $compat = Read-Lf $pelipperCompat
    $oldPartnerScan = @'
            bool teamUpSuppressed = candidate.modData.TryGetValue(SuppressedKey, out string? rawSuppressed)
                && rawSuppressed.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (ReferenceEquals(candidate, owner)
                || (candidate.IsInvisible && !teamUpSuppressed)
                || !LooksLikePelipperActor(candidate)
                || LooksWild(candidate))
            {
                continue;
            }

            float distance = Vector2Distance(candidate.Tile, owner.Tile);
            bool explicitOwner = HasOwnerName(candidate, owner.Name)
                || (candidate.modData.TryGetValue(SuppressedOwnerKey, out string? suppressedOwner)
                    && suppressedOwner.Equals(owner.Name, StringComparison.OrdinalIgnoreCase));
            if (!explicitOwner && distance > 3.25f)
                continue;
'@
    $newPartnerScan = @'
            bool teamUpSuppressed = candidate.modData.TryGetValue(SuppressedKey, out string? rawSuppressed)
                && rawSuppressed.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (ReferenceEquals(candidate, owner)
                || !LooksLikePelipperActor(candidate)
                || LooksWild(candidate))
            {
                continue;
            }

            bool explicitOwner = HasOwnerName(candidate, owner.Name)
                || (candidate.modData.TryGetValue(SuppressedOwnerKey, out string? suppressedOwner)
                    && suppressedOwner.Equals(owner.Name, StringComparison.OrdinalIgnoreCase));

            // Source-hidden partners are safe to discover only with explicit ownership metadata.
            // This restores a recall path for Standby Pokemon without proximity-matching random
            // invisible Pelipper actors.
            if (candidate.IsInvisible && !teamUpSuppressed && !explicitOwner)
                continue;

            float distance = Vector2Distance(candidate.Tile, owner.Tile);
            if (!explicitOwner && distance > 3.25f)
                continue;
'@
    $compat = Replace-Exact $compat $oldPartnerScan $newPartnerScan 'hidden explicit-owner partner detection'

    $oldCombatOpt = @'
    public static bool ShouldExcludeFromTeamUpCombat(NPC actor)
    {
        if (!LooksLikePelipperActor(actor))
            return false;

        return !actor.modData.TryGetValue(CombatTargetOptInKey, out string? raw)
            || !raw.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
    public static bool LooksLikePelipperActor(NPC actor)
'@
    $newCombatOpt = @'
    public static bool ShouldExcludeFromTeamUpCombat(NPC actor)
    {
        if (!LooksLikePelipperActor(actor))
            return false;

        return !actor.modData.TryGetValue(CombatTargetOptInKey, out string? raw)
            || !raw.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    // Capture safety must not depend on Team Up's 5-tick combat opt-in marker. Pelipper's own
    // Pokemon can damage a wild proxy before that marker exists, so expose direct wild identity.
    public static bool IsWildCombatActor(NPC actor)
        => LooksLikePelipperActor(actor) && LooksWild(actor);

    public static bool LooksLikePelipperActor(NPC actor)
'@
    $compat = Replace-Exact $compat $oldCombatOpt $newCombatOpt 'wild capture identity helper'
    Write-Utf8 $pelipperCompat $compat

    $capture = Read-Lf $captureSafety
    $oldCaptureIdentity = @'
        if (!PelipperTownCompatibilityService.LooksLikePelipperActor(monster))
            return false;

        // Owned companions are already excluded from Team Up combat entirely. Capture safety is
        // only for unowned/wild battle proxies which Alpha 6.6.13 explicitly opted into combat.
        if (PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            return false;
'@
    $newCaptureIdentity = @'
        // Alpha 6.6.16: capture-floor identity is the Pelipper wild/battle proxy itself, not
        // Team Up's transient CombatTarget opt-in marker. This closes the window where a source
        // Pokemon could land a lethal hit before Team Up's combat probe marked the target.
        if (!PelipperTownCompatibilityService.IsWildCombatActor(monster))
            return false;
'@
    $capture = Replace-Exact $capture $oldCaptureIdentity $newCaptureIdentity 'capture floor independent wild identity'
    Write-Utf8 $captureSafety $capture

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $a663 = Read-Lf $alpha663
    $a15 = Read-Lf $alpha6615
    $a16 = Read-Lf $alpha6616
    $compat = Read-Lf $pelipperCompat
    $capture = Read-Lf $captureSafety
    $capturePatchText = Read-Lf $capturePatch
    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.16</Version>')) { throw 'Version materialization failed.' }
    if (-not $projectText.Contains('<EnableHarmony>true</EnableHarmony>')) { throw 'Harmony reference is not enabled.' }
    foreach ($token in @('build: v0.2.0-alpha.6.6.16','Controller Companion Slot Runtime Fix loaded.','HandleDialogueCompanionInputAlpha6616','UpdateDialogueCompanionInputAlpha6616')) {
        if (-not $modText.Contains($token)) { throw "ModEntry token missing: $token" }
    }
    foreach ($token in @('P / L+R Pokémon','IsOwnerOptedOut(owner)','ShowLinkedCompanionControlAlpha6615')) {
        if (-not $a15.Contains($token)) { throw "Alpha 6.6.15 bridge token missing: $token" }
    }
    foreach ($token in @('requestActive: false','SetOwnerOptOut(owner, !includeCompanion)')) {
        if (-not $a663.Contains($token)) { throw "NPC-only intent token missing: $token" }
    }
    foreach ($token in @('DialogueShoulderChordWindowMsAlpha6616','Buttons.LeftShoulder','Buttons.RightShoulder','OpenLinkedCompanionControlAlpha6616','ShowLeaveQuestion')) {
        if (-not $a16.Contains($token)) { throw "Controller chord token missing: $token" }
    }
    foreach ($token in @('Source-hidden partners','IsWildCombatActor')) {
        if (-not $compat.Contains($token)) { throw "Pelipper compatibility token missing: $token" }
    }
    if (-not $capture.Contains('IsWildCombatActor(monster)')) { throw 'Capture wild identity fix missing.' }
    if (-not $capture.Contains('FallbackThreshold = 0.10f')) { throw '10 percent capture fallback missing.' }
    foreach ($token in @('Harmony','Monster.takeDamage','BeforeTakeDamage','ClampDamage')) {
        if (-not $capturePatchText.Contains($token)) { throw "Capture patch token missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }

    Log 'Building Alpha 6.6.16 Controller Companion Slot Runtime Fix...'
    Log 'INPUT: P or controller L+R opens the recruited NPC linked-Pokemon menu; L/R singles are deferred so the chord cannot kick the NPC.'
    Log 'NPC-ONLY: visible partners are registered immediately in Standby and source-hidden explicit-owner partners remain discoverable.'
    Log 'SLOTS: linked Pokemon Call/Return keeps the 2/2 replacement flow from Alpha 6.6.15.'
    Log 'CAPTURE: 10% floor now keys directly from Pelipper wild identity instead of waiting for Team Up CombatTarget opt-in.'
    Log 'REGRESSION: 6.6.7+ water performance, 6.6.10 source movement authority, health bars, Codex, curfew/farewell and combat preserved.'

    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    & dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    $dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' | Select-Object -First 1
    if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

    if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
    if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
    New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
    Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force
    $manifestText = Read-Lf $manifest
    $manifestText = $manifestText.Replace('%ProjectVersion%', $version)
    Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.16 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $zipName`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.16'
    Log 'P / L+R NPC POKEMON ROUTING: ENABLED'
    Log 'L+R CANNOT FALL THROUGH TO NPC LEAVE: ENABLED'
    Log 'NPC-ONLY STANDBY RECALL PATH: ENABLED'
    Log 'PELIPPER WILD CAPTURE IDENTITY FLOOR: ENABLED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}