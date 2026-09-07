$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$deployment = Join-Path $root 'src\TeamUp\Core\PelipperDeploymentStateService.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$alpha669 = Join-Path $root 'src\TeamUp\ModEntry.Alpha669.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$tileSafety = Join-Path $root 'src\TeamUp\Core\PartyTileSafety.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6610'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.10_PELIPPER_FOLLOW_AUTHORITY_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.10_PELIPPER_FOLLOW_AUTHORITY_HOTFIX_TEST.sha256.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_10_PELIPPER_FOLLOW_AUTHORITY_VI.txt'
$version = '0.2.0-alpha.6.6.10'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

try {
    foreach ($required in @($project,$manifest,$modEntry,$follow,$deployment,$alpha663,$alpha669,$combat,$tileSafety,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.10 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.10')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.10 Pelipper Follow Authority Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    $followText = Read-Lf $follow
    $oldFollow = @'
        foreach (CompanionUnitData unit in activeUnits)
        {
            if (_releasedCharacters.Contains(unit.CharacterName))
'@
    $newFollow = @'
        foreach (CompanionUnitData unit in activeUnits)
        {
            // Alpha 6.6.10: Pelipper Town is the sole movement/render authority for its Pokemon.
            // Skip before resolving the actor so Team Up cannot PrepareForParty, HoldPosition,
            // FollowTarget, warp, Halt, or replace controllers for source-owned companions.
            if (PelipperTownCompatibilityService.IsSourceControlled(unit))
                continue;

            if (_releasedCharacters.Contains(unit.CharacterName))
'@
    $followText = Replace-Required $followText $oldFollow $newFollow 'Pelipper early skip in UpdateCompanionUnits'
    Write-Utf8 $follow $followText

    $deploymentText = Read-Lf $deployment
    $oldDeployment = @'
        ReleaseLegacySuppression(actor);
        actor.modData[DeploymentStateKey] = deployed ? ActiveValue : StandbyValue;

        if (string.IsNullOrWhiteSpace(ownerName))
            actor.modData.Remove(DeploymentOwnerKey);
        else
            actor.modData[DeploymentOwnerKey] = ownerName;
'@
    $newDeployment = @'
        ReleaseLegacySuppression(actor);

        string desiredState = deployed ? ActiveValue : StandbyValue;
        if (!actor.modData.TryGetValue(DeploymentStateKey, out string? currentState)
            || !currentState.Equals(desiredState, StringComparison.OrdinalIgnoreCase))
        {
            actor.modData[DeploymentStateKey] = desiredState;
        }

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            if (actor.modData.ContainsKey(DeploymentOwnerKey))
                actor.modData.Remove(DeploymentOwnerKey);
        }
        else if (!actor.modData.TryGetValue(DeploymentOwnerKey, out string? currentOwner)
            || !currentOwner.Equals(ownerName, StringComparison.Ordinal))
        {
            actor.modData[DeploymentOwnerKey] = ownerName;
        }
'@
    $deploymentText = Replace-Required $deploymentText $oldDeployment $newDeployment 'idempotent Pelipper deployment marker writes'
    Write-Utf8 $deployment $deploymentText

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $followText = Read-Lf $follow
    $deploymentText = Read-Lf $deployment
    $alpha663Text = Read-Lf $alpha663
    $alpha669Text = Read-Lf $alpha669
    $combatText = Read-Lf $combat
    $tileText = Read-Lf $tileSafety

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.10</Version>')) { throw 'Version materialization failed.' }
    foreach ($token in @('build: v0.2.0-alpha.6.6.10','Pelipper Follow Authority Hotfix loaded.')) {
        if (-not $modText.Contains($token)) { throw "ModEntry 6.6.10 token missing: $token" }
    }

    $companionStart = $followText.IndexOf('    private void UpdateCompanionUnits(')
    $companionEnd = $followText.IndexOf('    private void FollowTarget(', $companionStart)
    if ($companionStart -lt 0 -or $companionEnd -lt 0) { throw 'Unable to inspect UpdateCompanionUnits.' }
    $companionBody = $followText.Substring($companionStart, $companionEnd - $companionStart)
    $skipIndex = $companionBody.IndexOf('PelipperTownCompatibilityService.IsSourceControlled(unit)')
    $resolveIndex = $companionBody.IndexOf('ResolveCharacter(unit.CharacterName)')
    if ($skipIndex -lt 0) { throw 'Pelipper source-controlled early skip missing from UpdateCompanionUnits.' }
    if ($resolveIndex -lt 0 -or $skipIndex -gt $resolveIndex) { throw 'Pelipper skip must occur before actor resolution/control.' }
    foreach ($token in @('PrepareForParty(npc, recruiterId)','FollowTarget(npc','HoldPosition(npc)','WarpNearTarget')) {
        if (-not $followText.Contains($token)) { throw "Follow regression token missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService regressed to expensive placement query.' }

    foreach ($token in @('desiredState = deployed ? ActiveValue : StandbyValue','currentState.Equals(desiredState','currentOwner.Equals(ownerName')) {
        if (-not $deploymentText.Contains($token)) { throw "Idempotent deployment token missing: $token" }
    }
    if ($deploymentText.Contains('SetSuppressed(actor, owner, true)')) { throw 'Pelipper suppression=true regression detected.' }
    if ($alpha663Text.Contains('PelipperTownCompatibilityService.SetSuppressed(')) { throw 'Alpha663 direct Pelipper suppression regression detected.' }

    foreach ($token in @('RenderedWorld','RenderedHud','BuildHealthSnapshotSignatureAlpha669')) {
        if (-not $alpha669Text.Contains($token)) { throw "6.6.9 health regression missing: $token" }
    }
    foreach ($token in @('CombatPathRetryCooldownTicks = 24','CombatMovementPulseTicks = 3','PartyTileSafety.IsWalkableLandOrBridge(location, tile)')) {
        if (-not $combatText.Contains($token)) { throw "Combat regression missing: $token" }
    }
    foreach ($token in @('isWaterTile(x, y)','GetLayer("Buildings")','isTilePassable(tile)')) {
        if (-not $tileText.Contains($token)) { throw "Land-safe regression missing: $token" }
    }

    Log 'Building Alpha 6.6.10 Pelipper Follow Authority Hotfix...'
    Log 'ROOT FIX: source-controlled Pelipper companions are skipped before FollowService resolves or controls their actor.'
    Log 'ROOT FIX: Team Up cannot Hold/Follow/Warp/Halt/controller Pelipper Pokemon through UpdateCompanionUnits.'
    Log 'POLISH: Pelipper soft deployment markers are idempotent; unchanged reconcile cycles do not rewrite modData.'
    Log 'REGRESSION: Alpha 6.6.7 performance, 6.6.8 land-safe, and 6.6.9 health UI remain locked.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.10 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.10'
    Log 'PELIPPER FOLLOW AUTHORITY: SOURCE ONLY'
    Log 'PELIPPER EARLY SKIP BEFORE ACTOR RESOLUTION: ENABLED'
    Log 'PELIPPER DEPLOYMENT MARKER WRITES: IDEMPOTENT'
    Log '6.6.7 PERFORMANCE REGRESSION: PRESERVED'
    Log '6.6.8 LAND-SAFE REGRESSION: PRESERVED'
    Log '6.6.9 HEALTH UI REGRESSION: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
