$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$curfew = Join-Path $root 'src\TeamUp\ModEntry.Alpha6612.cs'
$healthCoordinator = Join-Path $root 'src\TeamUp\ModEntry.Alpha669.cs'
$healthOverlay = Join-Path $root 'src\TeamUp\UI\PartyHealthOverlayService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6612'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.12_RELATIONSHIP_CURFEW_CONTEXT_HEALTH_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.12_RELATIONSHIP_CURFEW_CONTEXT_HEALTH_TEST.sha256.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_12_RELATIONSHIP_CURFEW_CONTEXT_HEALTH_VI.txt'
$version = '0.2.0-alpha.6.6.12'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }

try {
    foreach ($required in @($project,$manifest,$modEntry,$curfew,$healthCoordinator,$healthOverlay,$follow,$combat,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.12 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.12')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.12 Relationship Curfew + Context Health loaded.')
    if (-not $modText.Contains('RegisterAlpha6612Events();')) {
        $anchor = "        RegisterAlpha669HotfixEvents();`n"
        if (-not $modText.Contains($anchor)) { throw 'RegisterAlpha669HotfixEvents anchor missing.' }
        $modText = $modText.Replace($anchor, $anchor + "        RegisterAlpha6612Events();`n")
    }
    Write-Utf8 $modEntry $modText

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $curfewText = Read-Lf $curfew
    $healthText = Read-Lf $healthCoordinator
    $overlayText = Read-Lf $healthOverlay
    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.12</Version>')) { throw 'Version materialization failed.' }
    foreach ($token in @('build: v0.2.0-alpha.6.6.12','RegisterAlpha6612Events();','Relationship Curfew + Context Health loaded.')) {
        if (-not $modText.Contains($token)) { throw "ModEntry 6.6.12 token missing: $token" }
    }

    foreach ($token in @(
        'Game1.timeOfDay < 2300',
        'GetCurfewTimeAlpha6612',
        'return 2900;',
        'hearts >= 8',
        'return 2600;',
        'hearts >= 6',
        'return 2500;',
        'hearts >= 3',
        'return 2400;',
        'PartyMemberState.Inactive',
        'Follow.ReleaseToVanillaAndResumeSchedule(npc)',
        'CompanionDeploymentState.Standby',
        'PelipperDeploymentStateService.SetDesiredDeployment'
    )) {
        if (-not $curfewText.Contains($token)) { throw "Curfew acceptance token missing: $token" }
    }

    foreach ($token in @(
        'TimeSpan.FromSeconds(3)',
        'currentHealth < previousHealth',
        'bool engaged = isEngaged(member)',
        'bool talking = isTalking(member)',
        'int y = (int)local.Y + 60',
        'member.IsDowned || engaged || talking || heldVisible'
    )) {
        if (-not $overlayText.Contains($token)) { throw "Context health acceptance token missing: $token" }
    }
    if ($overlayText.Contains('DrawHud(')) { throw 'Persistent party HUD regression detected in health overlay.' }
    if ($healthText.Contains('RenderedHud +=')) { throw 'RenderedHud registration regression detected.' }
    foreach ($token in @('IsMemberTalkingAlpha669','Game1.activeClickableMenu is not null && !Game1.dialogueUp','HealthOverlayAlpha669.Reset()')) {
        if (-not $healthText.Contains($token)) { throw "Health coordinator token missing: $token" }
    }

    # Preserve previous performance and Pelipper follow authority locks.
    $companionStart = $followText.IndexOf('    private void UpdateCompanionUnits(')
    $companionEnd = $followText.IndexOf('    private void FollowTarget(', $companionStart)
    if ($companionStart -lt 0 -or $companionEnd -lt 0) { throw 'Unable to inspect UpdateCompanionUnits.' }
    $companionBody = $followText.Substring($companionStart, $companionEnd - $companionStart)
    if ($companionBody.IndexOf('PelipperTownCompatibilityService.IsSourceControlled(unit)') -gt $companionBody.IndexOf('ResolveCharacter(unit.CharacterName)')) {
        throw 'Pelipper source authority regression: skip is no longer before actor resolution.'
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }

    Log 'Building Alpha 6.6.12 Relationship Curfew + Context Health...'
    Log 'CURFEW: 0-2 hearts 23:00; 3-5 00:00; 6-7 01:00; 8-9 02:00; 10+/spouse Team Up cap 03:00.'
    Log 'CURFEW: NPC returns to vanilla/mod schedule and linked companion moves to Standby.'
    Log 'HEALTH: persistent left HUD removed; under-foot bar only.'
    Log 'HEALTH: visible during combat, while talking, when downed, and for 3 seconds after damage/combat.'
    Log 'REGRESSION: 6.6.10 Pelipper movement authority and 6.6.7/6.6.8 performance/land safety preserved.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.12 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.12'
    Log 'RELATIONSHIP CURFEW: ENABLED'
    Log 'NPC VANILLA SCHEDULE RETURN: ENABLED'
    Log 'LINKED COMPANION CURFEW STANDBY: ENABLED'
    Log 'PERSISTENT PARTY HEALTH HUD: REMOVED'
    Log 'UNDER-FOOT CONTEXT HEALTH BAR: ENABLED'
    Log 'THREE-SECOND DAMAGE/COMBAT HOLD: ENABLED'
    Log 'DIALOGUE HEALTH VISIBILITY: ENABLED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
