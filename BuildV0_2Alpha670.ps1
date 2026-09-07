$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src/TeamUp/TeamUp.csproj'
$manifest = Join-Path $root 'src/TeamUp/manifest.json'
$sourceDir = Join-Path $root 'src/TeamUp'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha670'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG_ALPHA670.txt'
$version = '0.2.0-alpha.6.7.0'
$zipName = 'TeamUp_v0.2.0-alpha.6.7.0_PARTY_BANTER_MIMI_SHIPPER_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.7.0_PARTY_BANTER_MIMI_SHIPPER_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) }
function Require([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

if (Test-Path $log) { Remove-Item $log -Force }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

$projectText = ReadText $project
$config = ReadText (Join-Path $sourceDir 'ModConfig.cs')
$banter = ReadText (Join-Path $sourceDir 'Core/PartyBanterService.cs')
$banterEntry = ReadText (Join-Path $sourceDir 'ModEntry.Alpha67.cs')
$playerGate = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6625.cs')
$authority = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6626.cs')
$bridge = ReadText (Join-Path $sourceDir 'Core/PelipperTown119NativeBridge.cs')
$combat = ReadText (Join-Path $sourceDir 'Combat/CombatService.cs')
$codex = ReadText (Join-Path $sourceDir 'UI/CodexBrowserMenu.cs')
$profile = ReadText (Join-Path $sourceDir 'UI/CharacterProfileMenu.cs')
$allSource = (Get-ChildItem $sourceDir -Recurse -Filter '*.cs' | ForEach-Object { ReadText $_.FullName }) -join "`n"

Require ($projectText.Contains('<Version>0.2.0-alpha.6.7.0</Version>')) 'Wrong project version.'
Require ($config.Contains('public int MaxPartyMembers { get; set; } = 5;')) 'Five-person formation default missing.'
Require ($config.Contains('public int MaxActiveLinkedCompanions { get; set; } = 2;')) 'Two-companion cap changed.'
Require ($config.Contains('EnablePartyBanter')) 'Party Banter config toggle missing.'
Require ($config.Contains('EnableMimiShippingBanter')) 'MiMi shipping toggle missing.'
Require ($banterEntry.Contains('Config.MaxPartyMembers = 5;')) 'Runtime five-person hard cap missing.'
Require ($banterEntry.Contains('P / L+R Companion')) 'Provider-neutral Companion shortcut missing.'
Require ($banterEntry.Contains('teamup_banter')) 'Banter debug command missing.'
Require ($playerGate.Contains('EnsureAlpha67BanterRegistered')) 'Banter registration is not chained into runtime.'
Require ($banter.Contains('internal sealed class PartyBanterService')) 'PartyBanterService missing.'
Require ($banter.Contains('TryAmbientExchange')) 'Ambient banter context missing.'
Require ($banter.Contains('TryCombatExchange')) 'Combat banter context missing.'
Require ($banter.Contains('TryLowHealthEncouragement')) 'Low-HP encouragement context missing.'
Require ($banter.Contains('TryVictoryExchange')) 'Post-combat banter context missing.'
Require ($banter.Contains('BanterTrait.Shipper')) 'MiMi Shipper trait missing.'
Require ($banter.Contains('males.Count < 2')) 'MiMi 2+ male NPC gate missing.'
Require ($banter.Contains('TryMimiShippingExchange')) 'MiMi shipping exchange missing.'
Require ($banter.Contains('showTextAboveHead')) 'Non-blocking overhead speech presentation missing.'
Require ($banter.IndexOf('friendshipData', [StringComparison]::OrdinalIgnoreCase) -lt 0) 'Banter must never mutate friendship data.'
Require ($banter.IndexOf('spouse', [StringComparison]::OrdinalIgnoreCase) -lt 0) 'Banter must never mutate romance/spouse state.'
Require ($banter.IndexOf('dating', [StringComparison]::OrdinalIgnoreCase) -lt 0) 'Banter must never mutate dating state.'
Require ($authority.Contains('ReconcileSingleCompanionAuthorityAlpha6626')) 'Single companion authority regression.'
Require ($bridge.Contains('TryGetSafeCustomVillagerCompanionDescriptor')) '6.6.27 custom NPC companion detection regression.'
Require ($allSource.IndexOf('PelipperRenderSuppressedAlpha6613') -lt 0) 'Legacy render suppression returned.'
Require ($allSource.IndexOf('TrySetActorInvisibleAlpha6613') -lt 0) 'Legacy visibility writer returned.'
Require ($combat.Contains('private const int CombatPathRetryCooldownTicks = 24;')) 'Combat retry regression.'
Require ($combat.Contains('private const int CombatMovementPulseTicks = 3;')) 'Combat movement pulse regression.'
Require ($codex.Contains('Math.Min(1739, Game1.uiViewport.Width - 12)')) 'Codex 115% regression.'
Require ($profile.Contains('Math.Min(1518, Game1.uiViewport.Width - 16)')) 'Profile 115% regression.'

Log 'Building Alpha 6.7.0 Party Banter...'
Log 'FORMATION: 5 people total (Farmer included), 2 external companions.'
Log 'TERMINOLOGY: P / L+R Companion.'
Log 'BANTER: ambient + combat + low HP encouragement + victory response.'
Log 'PERSONALITY: explicit vanilla traits with role/engagement fallback for mod NPCs.'
Log 'MIMI: Shipper trait activates only with 2+ positively identified male party NPCs.'
Log 'CANON SAFETY: banter never mutates friendship/romance state.'
Log 'PELIPPER: 6.6.27 native bridge + single companion authority preserved.'

dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

$dll = Join-Path $sourceDir 'bin/Release/net6.0/TeamUp.dll'
Require (Test-Path $dll) 'TeamUp.dll missing after build.'
$dllAscii = (& strings $dll) -join "`n"
$dllUtf16 = (& strings -el $dll) -join "`n"
$dllStrings = $dllAscii + "`n" + $dllUtf16

Require ($dllStrings.Contains('PartyBanterService')) 'Party Banter absent from DLL.'
Require ($dllStrings.Contains('TryMimiShippingExchange')) 'MiMi shipping logic absent from DLL.'
Require ($dllStrings.Contains('TryLowHealthEncouragement')) 'Low HP encouragement absent from DLL.'
Require ($dllStrings.Contains('TryCombatExchange')) 'Combat banter absent from DLL.'
Require ($dllStrings.Contains('P / L+R Companion')) 'Companion shortcut absent from DLL.'
Require ($dllStrings.Contains('ReconcileSingleCompanionAuthorityAlpha6626')) 'Single companion authority absent from DLL.'
Require ($dllStrings.Contains('TryGetSafeCustomVillagerCompanionDescriptor')) 'Custom NPC detection absent from DLL.'
Require ($dllStrings.Contains('SetConfiguredCompanionEnabled')) 'Pelipper native NPC lifecycle absent from DLL.'
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
Log 'BUILD SUCCESS - ALPHA 6.7.0'
Log "ZIP: $zipName"
Log "SHA256: $hash"
Write-Host 'BUILD SUCCESS - ALPHA 6.7.0'
Write-Host "ZIP: $zip"
Write-Host "SHA256: $hash"
