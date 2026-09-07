$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcRoot = Join-Path $root 'src\TeamUp'
$project = Join-Path $srcRoot 'TeamUp.csproj'
$manifest = Join-Path $srcRoot 'manifest.json'
$modEntry = Join-Path $srcRoot 'ModEntry.cs'
$alpha6613 = Join-Path $srcRoot 'ModEntry.Alpha6613.cs'
$alpha6615 = Join-Path $srcRoot 'ModEntry.Alpha6615.cs'
$alpha6617 = Join-Path $srcRoot 'ModEntry.Alpha6617.cs'
$alpha6618 = Join-Path $srcRoot 'ModEntry.Alpha6618.cs'
$alpha6621 = Join-Path $srcRoot 'ModEntry.Alpha6621.cs'
$deployment = Join-Path $srcRoot 'Core\PelipperDeploymentStateService.cs'
$threat = Join-Path $srcRoot 'Combat\ThreatService.cs'
$combat = Join-Path $srcRoot 'Combat\CombatService.cs'
$follow = Join-Path $srcRoot 'Following\FollowService.cs'
$codex = Join-Path $srcRoot 'UI\CodexBrowserMenu.cs'
$profile = Join-Path $srcRoot 'UI\CharacterProfileMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6624'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_24_SOURCE_AUTHORITY_HARD_TAUNT_AUDIT_VI.txt'
$version = '0.2.0-alpha.6.6.24'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.24_SOURCE_AUTHORITY_HARD_TAUNT_AUDIT_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.24_SOURCE_AUTHORITY_HARD_TAUNT_AUDIT_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function Write-Utf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}
function Patch-IfNeeded([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Missing 6.6.24 materialization anchor: $label" }
    return $text.Replace($old, $new)
}
function Require-Contains([string]$text, [string]$token, [string]$label) {
    if (-not $text.Contains($token)) { throw "6.6.24 required token missing: $label -> $token" }
}
function Require-Absent([string]$text, [string]$token, [string]$label) {
    if ($text.Contains($token)) { throw "6.6.24 forbidden token still present: $label -> $token" }
}
function Log([string]$text) {
    $text | Tee-Object -FilePath $log -Append
}

# Only materialize version labels and the small Hard-Taunt geometry delta.
# Pelipper source-authority files (Alpha6613/6615/6617) are real source now and MUST NOT be rewritten here.
$projectText = Read-Lf $project
$projectText = Patch-IfNeeded $projectText '<Version>0.2.0-alpha.6.6.23</Version>' '<Version>0.2.0-alpha.6.6.24</Version>' 'project version'
Write-Utf8 $project $projectText

$modText = Read-Lf $modEntry
$modText = Patch-IfNeeded $modText 'build: v0.2.0-alpha.6.6.23' 'build: v0.2.0-alpha.6.6.24' 'debug build label'
$modText = Patch-IfNeeded $modText 'Team Up! v0.2.0-alpha.6.6.23 Anti-Flicker + Hard Taunt Hotfix loaded. Codex 115% preserved.' 'Team Up! v0.2.0-alpha.6.6.24 Source Authority + Hard Taunt Audit loaded. Codex 115% preserved.' 'load label'
Write-Utf8 $modEntry $modText

$combatText = Read-Lf $combat
$combatText = Patch-IfNeeded $combatText @'
    private const float HardLeashTiles = 12f;
    private const float RepathThresholdTiles = 1.35f;
'@ @'
    private const float HardLeashTiles = 12f;
    private const float NormalGuardRangeTiles = 4f;
    private const float HardTauntGuardRangeTiles = 7f;
    private const float RepathThresholdTiles = 1.35f;
'@ 'guard range constants'

$combatText = Patch-IfNeeded $combatText @'
            .Where(member =>
            {
                NPC? npc = Game1.getCharacterFromName(member.CharacterName);
                return npc is not null
                    && ReferenceEquals(npc.currentLocation, FarmerContext.currentLocation)
                    && Vector2.Distance(npc.Tile, FarmerContext.Tile) <= 4f;
            })
            .OrderByDescending(member => nearbyThreats.Count(monster =>
                _threat.GetAggroActor(monster, validThreatActors).Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(member => _threat.GetTotalThreat(member.CharacterName, nearbyThreats))
            .FirstOrDefault();
'@ @'
            .Where(member =>
            {
                NPC? npc = Game1.getCharacterFromName(member.CharacterName);
                if (npc is null || !ReferenceEquals(npc.currentLocation, FarmerContext.currentLocation))
                    return false;

                bool ownsHardTaunt = nearbyThreats.Any(monster =>
                    _threat.IsForcedAggro(monster, member.CharacterName));
                float guardRange = ownsHardTaunt ? HardTauntGuardRangeTiles : NormalGuardRangeTiles;
                return Vector2.Distance(npc.Tile, FarmerContext.Tile) <= guardRange;
            })
            .OrderByDescending(member => nearbyThreats.Count(monster =>
                _threat.IsForcedAggro(monster, member.CharacterName)))
            .ThenByDescending(member => nearbyThreats.Count(monster =>
                _threat.GetAggroActor(monster, validThreatActors).Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(member => _threat.GetTotalThreat(member.CharacterName, nearbyThreats))
            .FirstOrDefault();
'@ 'hard taunt guard-owner range and priority'
Write-Utf8 $combat $combatText

if (Test-Path $log) { Remove-Item $log -Force }

$projectText = Read-Lf $project
$modText = Read-Lf $modEntry
$a13 = Read-Lf $alpha6613
$a15 = Read-Lf $alpha6615
$a17 = Read-Lf $alpha6617
$a18 = Read-Lf $alpha6618
$a21 = Read-Lf $alpha6621
$deploymentText = Read-Lf $deployment
$threatText = Read-Lf $threat
$combatText = Read-Lf $combat
$followText = Read-Lf $follow
$codexText = Read-Lf $codex
$profileText = Read-Lf $profile

Require-Contains $projectText '<Version>0.2.0-alpha.6.6.24</Version>' 'version'
Require-Contains $modText 'build: v0.2.0-alpha.6.6.24' 'debug build version'
Require-Contains $modText 'Math.Clamp(Config.MaxPartyMembers, 1, 6)' 'party cap 6'
Require-Contains $modText 'Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2)' 'companion cap 2'

# Global source scan. A legacy visibility/render symbol anywhere in Team Up is a hard failure.
$allSource = (Get-ChildItem $srcRoot -Recurse -Filter '*.cs' | ForEach-Object { Read-Lf $_.FullName }) -join "`n"
foreach ($token in @(
    'PelipperRenderSuppressedAlpha6613',
    'PelipperRenderRestoreAlpha6613',
    'TrySetActorInvisibleAlpha6613',
    'OnAlpha6613RenderingWorld',
    'OnAlpha6613RenderedWorld'
)) {
    Require-Absent $allSource $token 'legacy Pelipper render ownership anywhere in source'
}

Require-Contains $a13 'PelipperDeploymentStateService.SetDesiredDeployment' 'soft marker quota authority'
Require-Absent $a13 'Helper.Events.Display.RenderingWorld' 'Alpha6613 render subscription'
Require-Absent $a13 'Helper.Events.Display.RenderedWorld' 'Alpha6613 rendered subscription'
Require-Absent $a17 'Helper.Events.Display.RenderingWorld' 'Alpha6617 obsolete render cleanup'

# Deeply isolate the NPC-owned path. It must use native lifecycle + marker and terminate before player fallback.
$ensureStart = $a15.IndexOf('private CompanionUnitData? EnsureLinkedCompanionRecordAlpha6615')
$ensureEnd = $a15.IndexOf('private void ShowLinkedCompanionControlAlpha6615', $ensureStart)
if ($ensureStart -lt 0 -or $ensureEnd -le $ensureStart) { throw 'Could not isolate EnsureLinkedCompanionRecordAlpha6615.' }
$ensureBlock = $a15.Substring($ensureStart, $ensureEnd - $ensureStart)
Require-Contains $ensureBlock 'PelipperDeploymentStateService.SetDesiredDeployment' 'linked record soft marker'
Require-Absent $ensureBlock 'TrySetPelipperSourceDeploymentAlpha6613' 'linked record actor fallback'

$setStart = $a15.IndexOf('private void SetPelipperSourceDeploymentForUnitAlpha6615')
$npcStart = $a15.IndexOf('if (unit.OwnerKind == CompanionOwnerKind.PartyMember', $setStart)
$fallbackStart = $a15.IndexOf('// Player-owned / non-NPC-linked Pelipper units', $npcStart)
if ($setStart -lt 0 -or $npcStart -lt 0 -or $fallbackStart -le $npcStart) { throw 'Could not isolate NPC-owned Pelipper deployment block.' }
$npcBlock = $a15.Substring($npcStart, $fallbackStart - $npcStart)
Require-Contains $npcBlock 'TrySetPelipperNpcSourceEnabledAlpha6618' 'NPC native lifecycle route'
Require-Contains $npcBlock 'PelipperDeploymentStateService.SetDesiredDeployment' 'NPC soft marker'
Require-Contains $npcBlock 'return;' 'NPC source-authority termination'
Require-Absent $npcBlock 'TrySetPelipperSourceDeploymentAlpha6613' 'NPC actor fallback'

foreach ($token in @(
    'GetEffectiveCombatCompanionCountAlpha6618',
    'PrepareNpcCompanionRecruitCapacityAlpha6618',
    'physically deployed and must block a third companion'
)) {
    Require-Contains $a18 $token 'source-live hard 2/2 accounting'
}
Require-Contains $a21 'PelipperNonConvergedSourceRequestsAlpha6623' 'non-converged lifecycle latch'
Require-Contains $deploymentText 'Ronvotri.TeamUp/PelipperDeployment' 'soft deployment marker key'

Require-Contains $threatText 'ForceAggro' 'forced aggro service'
Require-Contains $threatText 'IsForcedAggro' 'forced aggro query'
Require-Contains $combatText 'private const float NormalGuardRangeTiles = 4f;' 'normal guard range'
Require-Contains $combatText 'private const float HardTauntGuardRangeTiles = 7f;' 'forced guard range'
Require-Contains $combatText 'hardTauntOwnsPressure ? 1f' '100 percent hard taunt redirect'
Require-Contains $combatText 'HARD TAUNT' 'hard taunt activation'
Require-Contains $combatText 'private const int CombatPathRetryCooldownTicks = 24;' 'combat retry 24'
Require-Contains $combatText 'private const int CombatMovementPulseTicks = 3;' 'combat movement pulse 3'
Require-Contains $combatText '_incomingDamageCooldowns[guard.CharacterName]' 'redirect anti-double-hit gate'
Require-Absent $followText 'isTileLocationTotallyClearAndPlaceable' 'follow water regression'
Require-Absent $combatText 'isTileLocationTotallyClearAndPlaceable' 'combat water regression'
Require-Contains $codexText 'Math.Min(1739, Game1.uiViewport.Width - 12)' 'Codex 115% width'
Require-Contains $codexText 'Math.Min(1049, Game1.uiViewport.Height - 12)' 'Codex 115% height'
Require-Contains $profileText 'Math.Min(1518, Game1.uiViewport.Width - 16)' 'Profile 115% width'
Require-Contains $profileText 'Math.Min(897, Game1.uiViewport.Height - 16)' 'Profile 115% height'

Log 'Building Alpha 6.6.24 Source Authority + Hard Taunt Audit...'
Log 'GLOBAL SOURCE SCAN: legacy Pelipper render ownership absent.'
Log 'PELIPPER NPC: native lifecycle path terminates before actor fallback.'
Log 'PELIPPER SLOT TRUTH: source-live Pokemon still occupies a real 2/2 slot.'
Log 'HARD TAUNT: forced aggro + 100% Farmer damage redirect preserved.'
Log 'HARD TAUNT: forced owner eligible to redirect at 7 tiles; normal Guard remains 4 tiles.'
Log 'REGRESSION: party cap 6, companion cap 2/2, combat retry 24, movement pulse 3 preserved.'
Log 'REGRESSION: Codex/Profile 115% and water-pathfinding safety preserved.'

& dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
& dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

$dll = Get-ChildItem (Join-Path $srcRoot 'bin\Release') -Recurse -Filter 'TeamUp.dll' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

$dllStrings = (& strings $dll.FullName | Out-String)
foreach ($token in @('PelipperRenderSuppressedAlpha6613', 'PelipperRenderRestoreAlpha6613', 'TrySetActorInvisibleAlpha6613')) {
    if ($dllStrings.Contains($token)) { throw "DLL AUDIT FAILED: legacy render symbol still present: $token" }
}
foreach ($token in @('ForceAggro', 'PelipperNonConvergedSourceRequestsAlpha6623', 'HardTauntGuardRangeTiles')) {
    if (-not $dllStrings.Contains($token)) { throw "DLL AUDIT FAILED: expected runtime token missing: $token" }
}

if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force

$manifestText = Read-Lf $manifest
$manifestText = $manifestText.Replace('%ProjectVersion%', $version)
Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
Copy-Item (Join-Path $srcRoot 'i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
if (-not (Test-Path $zip)) { throw 'Alpha 6.6.24 ZIP was not created.' }

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Utf8 $shaPath ("$hash  $zipName`r`n")
Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

Log 'GLOBAL LEGACY PELIPPER RENDER SOURCE: ABSENT'
Log 'SOURCE AUTHORITY LEGACY RENDER SYMBOLS: ABSENT IN DLL'
Log 'NPC NATIVE-LIFECYCLE FALLTHROUGH: BLOCKED'
Log 'SOURCE-LIVE 2/2 HARD CAP: PRESERVED'
Log 'HARD TAUNT FORCED AGGRO: PRESENT IN DLL'
Log 'HARD TAUNT RANGE AUDIT: 7 TILES WHILE FORCED / 4 TILES NORMAL'
Log 'CODEX/PROFILE 115%: PRESERVED'
Log 'WATER PATHFINDING REGRESSION GATE: PASS'
Log 'BUILD SUCCESS - ALPHA 6.6.24'
Log "ZIP: $zipName"
Log "SHA256: $hash"
