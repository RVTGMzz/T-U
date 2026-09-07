$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src/TeamUp/TeamUp.csproj'
$manifest = Join-Path $root 'src/TeamUp/manifest.json'
$sourceDir = Join-Path $root 'src/TeamUp'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6627'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG_ALPHA6627.txt'
$version = '0.2.0-alpha.6.6.27'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.27_CUSTOM_NPC_COMPANION_DETECTION_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.27_CUSTOM_NPC_COMPANION_DETECTION_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) }
function Require([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

if (Test-Path $log) { Remove-Item $log -Force }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

$projectText = ReadText $project
$bridge = ReadText (Join-Path $sourceDir 'Core/PelipperTown119NativeBridge.cs')
$integration = ReadText (Join-Path $sourceDir 'Core/CompanionIntegrationService.cs')
$authority = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6626.cs')
$npcGate = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6621.cs')
$playerGate = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6625.cs')
$combat = ReadText (Join-Path $sourceDir 'Combat/CombatService.cs')
$codex = ReadText (Join-Path $sourceDir 'UI/CodexBrowserMenu.cs')
$profile = ReadText (Join-Path $sourceDir 'UI/CharacterProfileMenu.cs')
$allSource = (Get-ChildItem $sourceDir -Recurse -Filter '*.cs' | ForEach-Object { ReadText $_.FullName }) -join "`n"

Require ($projectText.Contains('<Version>0.2.0-alpha.6.6.27</Version>')) 'Wrong project version.'
Require ($bridge.Contains('TryUnwrapNpc')) 'Wrapped Pelipper runtime entity support missing.'
Require ($bridge.Contains('TryGetSafeCustomVillagerCompanionDescriptor')) 'Safe custom-NPC fallback missing.'
Require ($bridge.Contains('SafeCustomNpcFallbackRadius = 2.75f')) 'Custom-NPC fallback radius changed unexpectedly.'
Require ($bridge.Contains('candidates.Count > 1')) 'Ambiguous Pokemon cluster fail-closed guard missing.'
Require ($bridge.Contains('claimedActors.Contains(candidate)')) 'Already-claimed Pokemon exclusion missing.'
Require ($bridge.Contains('LooksPlayerOwned(candidate)')) 'Player Pokemon exclusion missing from custom NPC fallback.'
Require ($bridge.Contains('TryIsVillagerCompanionConfiguredEnabled')) 'Pelipper native Companion enabled check missing.'
Require ($integration.Contains('PelipperTown119NativeBridge.HasExactVillagerRuntimeMap')) 'Exact 1.1.9 map boundary missing.'
Require ($integration.Contains('TryGetSafeCustomVillagerCompanionDescriptor')) 'Companion integration does not route through safe custom fallback.'
Require ($integration.IndexOf('TryGetSafeCustomVillagerCompanionDescriptor') -lt $integration.IndexOf('PelipperTownCompatibilityService.FindVillagerPartner')) 'Old proximity fallback still precedes safe 1.1.9 custom detection.'
Require ($authority.Contains('UpdateTicked -= OnAlpha663UpdateTicked')) 'Legacy Alpha663 loop returned.'
Require ($authority.Contains('UpdateTicked -= OnAlpha6615UpdateTicked')) 'Legacy Alpha6615 loop returned.'
Require ($authority.Contains('UpdateTicked -= OnAlpha6617UpdateTicked')) 'Legacy Alpha6617 loop returned.'
Require ($authority.Contains('UpdateTicked -= OnAlpha6618UpdateTicked')) 'Legacy Alpha6618 loop returned.'
Require ($authority.Contains('ReconcileSingleCompanionAuthorityAlpha6626')) 'Single companion authority missing.'
Require ($npcGate.Contains('occupiedByOthers')) 'NPC native Call effective quota gate missing.'
Require ($playerGate.Contains('EnsureAlpha6626Registered')) 'Player native gate no longer registers single authority.'
Require ($allSource.IndexOf('PelipperRenderSuppressedAlpha6613') -lt 0) 'Legacy render suppression returned.'
Require ($allSource.IndexOf('TrySetActorInvisibleAlpha6613') -lt 0) 'Legacy visibility writer returned.'
Require ($combat.Contains('private const int CombatPathRetryCooldownTicks = 24;')) 'Combat retry regression.'
Require ($combat.Contains('private const int CombatMovementPulseTicks = 3;')) 'Combat movement pulse regression.'
Require ($codex.Contains('Math.Min(1739, Game1.uiViewport.Width - 12)')) 'Codex 115% regression.'
Require ($profile.Contains('Math.Min(1518, Game1.uiViewport.Width - 16)')) 'Profile 115% regression.'

Log 'Building Alpha 6.6.27 Custom NPC Companion Detection...'
Log 'CUSTOM NPC EXACT LOOKUP: runtime _entity wrapper unwrapping enabled.'
Log 'CUSTOM NPC SAFE FALLBACK: unique + nearby + unclaimed + non-wild only.'
Log 'AMBIGUOUS CLUSTERS: fail closed, never nearest-Pokemon assignment.'
Log 'SINGLE COMPANION AUTHORITY: Alpha 6.6.26 architecture preserved.'
Log 'HARD QUOTA: shared effective 2/2 gates preserved.'

dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

$dll = Join-Path $sourceDir 'bin/Release/net6.0/TeamUp.dll'
Require (Test-Path $dll) 'TeamUp.dll missing after build.'
$dllAscii = (& strings $dll) -join "`n"
$dllUtf16 = (& strings -el $dll) -join "`n"
$dllStrings = $dllAscii + "`n" + $dllUtf16

Require ($dllStrings.Contains('TryUnwrapNpc')) 'Wrapped entity support absent from DLL.'
Require ($dllStrings.Contains('TryGetSafeCustomVillagerCompanionDescriptor')) 'Safe custom NPC fallback absent from DLL.'
Require ($dllStrings.Contains('TryIsVillagerCompanionConfiguredEnabled')) 'Native enabled-state check absent from DLL.'
Require ($dllStrings.Contains('ReconcileSingleCompanionAuthorityAlpha6626')) 'Single authority absent from DLL.'
Require ($dllStrings.Contains('SetConfiguredCompanionEnabled')) 'Native NPC lifecycle absent from DLL.'
Require ($dllStrings.Contains('RecallToBall')) 'Native player Return absent from DLL.'
Require ($dllStrings.Contains('DeployBesideOwner')) 'Native player Call absent from DLL.'
Require (-not $dllStrings.Contains('PelipperRenderSuppressedAlpha6613')) 'Legacy render suppression symbol remains in DLL.'
Require (-not $dllStrings.Contains('TrySetActorInvisibleAlpha6613')) 'Legacy visibility writer remains in DLL.'

New-Item -ItemType Directory -Force -Path $stageMod | Out-Null
Copy-Item $dll (Join-Path $stageMod 'TeamUp.dll') -Force
$manifestText = (ReadText $manifest).Replace('%ProjectVersion%', $version)
[System.IO.File]::WriteAllText((Join-Path $stageMod 'manifest.json'), $manifestText, $utf8NoBom)
Copy-Item (Join-Path $sourceDir 'i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
[System.IO.File]::WriteAllText($shaPath, "$hash  $zipName`n", $utf8NoBom)

Log 'SOURCE ACCEPTANCE: PASS'
Log 'BINARY ACCEPTANCE: PASS'
Log 'BUILD SUCCESS - ALPHA 6.6.27'
Log "ZIP: $zipName"
Log "SHA256: $hash"
Write-Host 'BUILD SUCCESS - ALPHA 6.6.27'
Write-Host "ZIP: $zip"
Write-Host "SHA256: $hash"
