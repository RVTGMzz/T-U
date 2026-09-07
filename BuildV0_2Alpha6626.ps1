$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src/TeamUp/TeamUp.csproj'
$manifest = Join-Path $root 'src/TeamUp/manifest.json'
$sourceDir = Join-Path $root 'src/TeamUp'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6626'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG_ALPHA6626.txt'
$version = '0.2.0-alpha.6.6.26'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.26_SINGLE_COMPANION_AUTHORITY_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.26_SINGLE_COMPANION_AUTHORITY_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) }
function Require([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

if (Test-Path $log) { Remove-Item $log -Force }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

$projectText = ReadText $project
$authority = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6626.cs')
$native = ReadText (Join-Path $sourceDir 'Core/PelipperTown119NativeBridge.cs')
$integration = ReadText (Join-Path $sourceDir 'Core/CompanionIntegrationService.cs')
$npcGate = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6621.cs')
$playerGate = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6625.cs')
$combat = ReadText (Join-Path $sourceDir 'Combat/CombatService.cs')
$codex = ReadText (Join-Path $sourceDir 'UI/CodexBrowserMenu.cs')
$profile = ReadText (Join-Path $sourceDir 'UI/CharacterProfileMenu.cs')
$allSource = (Get-ChildItem $sourceDir -Recurse -Filter '*.cs' | ForEach-Object { ReadText $_.FullName }) -join "`n"

Require ($projectText.Contains('<Version>0.2.0-alpha.6.6.26</Version>')) 'Wrong project version.'
Require ($authority.Contains('UpdateTicked -= OnAlpha663UpdateTicked')) 'Legacy Alpha663 autonomous reconcile is still subscribed.'
Require ($authority.Contains('UpdateTicked -= OnAlpha6615UpdateTicked')) 'Legacy Alpha6615 guessed-field poll is still subscribed.'
Require ($authority.Contains('UpdateTicked -= OnAlpha6617UpdateTicked')) 'Alpha6617 still owns an independent periodic loop.'
Require ($authority.Contains('UpdateTicked -= OnAlpha6618UpdateTicked')) 'Alpha6618 still owns an independent periodic loop.'
Require ($authority.Contains('UpdateTicked += OnAlpha6626UpdateTicked')) 'Single companion authority pulse is missing.'
Require ($authority.Contains('ReconcileSingleCompanionAuthorityAlpha6626')) 'Single companion authority implementation missing.'
Require ($authority.Contains('RequestAuthorityReturnAlpha6626')) 'Physical overflow native Return repair missing.'
Require ($authority.Contains('GetEffectiveCombatCompanionCountAlpha6618')) 'Single authority is not checking effective source-live truth.'
Require ($playerGate.Contains('EnsureAlpha6626Registered')) 'Single authority is not registered from the native bridge lifecycle.'
Require ($npcGate.Contains('occupiedByOthers')) 'NPC native Call is not hard-gated by effective slots.'
Require ($native.Contains('"_runtimes"')) 'Exact Pelipper villager runtime dictionary lookup missing.'
Require ($native.Contains('"_npcName"')) 'Exact Pelipper runtime NPC owner field lookup missing.'
Require ($native.Contains('"_entity"')) 'Exact Pelipper runtime actor field lookup missing.'
Require ($native.Contains('TryGetVillagerCompanionDescriptor')) 'Exact Pelipper owner descriptor lookup missing.'
Require ($integration.IndexOf('PelipperTown119NativeBridge.TryGetVillagerCompanionDescriptor') -ge 0) 'Companion integration does not use exact Pelipper owner map.'
Require ($integration.IndexOf('PelipperTown119NativeBridge.TryGetVillagerCompanionDescriptor') -lt $integration.IndexOf('PelipperTownCompatibilityService.FindVillagerPartner')) 'Proximity fallback still runs before exact Pelipper ownership.'
Require ($allSource.IndexOf('PelipperRenderSuppressedAlpha6613') -lt 0) 'Legacy render suppression returned.'
Require ($allSource.IndexOf('PelipperRenderRestoreAlpha6613') -lt 0) 'Legacy render restore returned.'
Require ($allSource.IndexOf('TrySetActorInvisibleAlpha6613') -lt 0) 'Legacy render IsInvisible writer returned.'
Require ($combat.Contains('private const int CombatPathRetryCooldownTicks = 24;')) 'Combat retry regression.'
Require ($combat.Contains('private const int CombatMovementPulseTicks = 3;')) 'Combat movement pulse regression.'
Require ($codex.Contains('Math.Min(1739, Game1.uiViewport.Width - 12)')) 'Codex 115% regression.'
Require ($profile.Contains('Math.Min(1518, Game1.uiViewport.Width - 16)')) 'Profile 115% regression.'

Log 'Building Alpha 6.6.26 Single Companion Authority...'
Log 'LEGACY PERIODIC COMPANION JUDGES: UNSUBSCRIBED (663/6615/6617/6618).'
Log 'SINGLE PERIODIC AUTHORITY: 10-tick source-truth pass.'
Log 'EXACT NPC OWNERSHIP: Pelipper _runtimes -> _npcName -> _entity.'
Log 'NPC NATIVE CALL: effective shared-cap preflight.'
Log 'PLAYER NATIVE CALL: DeployBeside 2/2 pre-spawn gate preserved.'
Log 'OVERFLOW REPAIR: source-native Return, no render/controller manipulation.'

dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

$dll = Join-Path $sourceDir 'bin/Release/net6.0/TeamUp.dll'
Require (Test-Path $dll) 'TeamUp.dll missing after build.'
$dllAscii = (& strings $dll) -join "`n"
$dllUtf16 = (& strings -el $dll) -join "`n"
$dllStrings = $dllAscii + "`n" + $dllUtf16

Require ($dllStrings.Contains('ReconcileSingleCompanionAuthorityAlpha6626')) 'Single authority absent from DLL.'
Require ($dllStrings.Contains('OnAlpha6626UpdateTicked')) 'Single authority pulse absent from DLL.'
Require ($dllStrings.Contains('TryGetVillagerCompanionDescriptor')) 'Exact owner descriptor lookup absent from DLL.'
Require ($dllStrings.Contains('_runtimes')) 'Exact Pelipper runtime map literal absent from DLL.'
Require ($dllStrings.Contains('_npcName')) 'Exact Pelipper owner field literal absent from DLL.'
Require ($dllStrings.Contains('_entity')) 'Exact Pelipper actor field literal absent from DLL.'
Require ($dllStrings.Contains('occupiedByOthers')) 'Effective shared-cap gate absent from DLL.'
Require ($dllStrings.Contains('SetConfiguredCompanionEnabled')) 'Native NPC lifecycle absent from DLL.'
Require ($dllStrings.Contains('RecallToBall')) 'Native player Return absent from DLL.'
Require ($dllStrings.Contains('DeployBesideOwner')) 'Native player Call absent from DLL.'
Require ($dllStrings.Contains('GetEffectiveCombatCompanionCountAlpha6618')) 'Effective slot truth absent from DLL.'
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
Log 'BUILD SUCCESS - ALPHA 6.6.26'
Log "ZIP: $zipName"
Log "SHA256: $hash"
Write-Host 'BUILD SUCCESS - ALPHA 6.6.26'
Write-Host "ZIP: $zip"
Write-Host "SHA256: $hash"
