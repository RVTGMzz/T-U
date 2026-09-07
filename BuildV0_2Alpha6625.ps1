$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$sourceDir = Join-Path $root 'src\TeamUp'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6625'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG_ALPHA6625.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_25_PELIPPER_NATIVE_119_BRIDGE_VI.txt'
$audit = Join-Path $root 'PELIPPER_119_NATIVE_SURFACE_AUDIT.md'
$version = '0.2.0-alpha.6.6.25'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.25_PELIPPER_NATIVE_119_BRIDGE_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.25_PELIPPER_NATIVE_119_BRIDGE_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) }
function Require([bool]$condition, [string]$message) { if (-not $condition) { throw $message } }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

if (Test-Path $log) { Remove-Item $log -Force }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
if (Test-Path $releaseDir) { New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null } else { New-Item -ItemType Directory -Path $releaseDir | Out-Null }

$projectText = ReadText $project
$native = ReadText (Join-Path $sourceDir 'Core\PelipperTown119NativeBridge.cs')
$quota = ReadText (Join-Path $sourceDir 'Core\PelipperTown119DeployQuotaPatch.cs')
$a21 = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6621.cs')
$a25 = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6625.cs')
$a18 = ReadText (Join-Path $sourceDir 'ModEntry.Alpha6618.cs')
$combat = ReadText (Join-Path $sourceDir 'Combat\CombatService.cs')
$codex = ReadText (Join-Path $sourceDir 'UI\CodexBrowserMenu.cs')
$profile = ReadText (Join-Path $sourceDir 'UI\CharacterProfileMenu.cs')
$allSource = (Get-ChildItem $sourceDir -Recurse -Filter '*.cs' | ForEach-Object { ReadText $_.FullName }) -join "`n"

Require ($projectText.Contains('<Version>0.2.0-alpha.6.6.25</Version>')) 'Wrong project version.'
Require ($native.Contains('SetConfiguredCompanionEnabled')) 'Native villager enable method missing.'
Require ($native.Contains('ApplyConfiguredAssignments')) 'Native villager assignment apply missing.'
Require ($native.Contains('VillagerCompanionManager')) 'Exact villager manager type missing.'
Require ($native.Contains('RecallToBall')) 'Native player recall route missing.'
Require ($native.Contains('DeployBesideOwner')) 'Native player deploy route missing.'
Require ($quota.Contains('PelipperTown.CompanionRuntime')) 'Native player runtime patch target missing.'
Require ($quota.Contains('DeployBeside')) 'Native final deploy boundary missing.'
Require ($quota.Contains('BeforeDeployBeside')) 'Native deploy prefix missing.'
Require ($a21.IndexOf('PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled') -ge 0) 'Alpha 6.6.21 does not use exact native bridge.'
Require ($a21.IndexOf('PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled') -lt $a21.IndexOf('PelipperVillagerLifecycleBridge.TrySetEnabled')) 'Exact native bridge is not first priority.'
Require ($a25.Contains('GetEffectiveCombatCompanionCountAlpha6618')) 'Native quota gate is not using effective source-live truth.'
Require ($a18.Contains('physically deployed and must block a third companion')) 'Source-live hard cap regression.'
Require ($allSource.IndexOf('PelipperRenderSuppressedAlpha6613') -lt 0) 'Legacy render suppression returned.'
Require ($allSource.IndexOf('PelipperRenderRestoreAlpha6613') -lt 0) 'Legacy render restore returned.'
Require ($allSource.IndexOf('TrySetActorInvisibleAlpha6613') -lt 0) 'Legacy IsInvisible writer returned.'
Require ($combat.Contains('private const int CombatPathRetryCooldownTicks = 24;')) 'Combat retry regression.'
Require ($combat.Contains('private const int CombatMovementPulseTicks = 3;')) 'Combat movement pulse regression.'
Require ($codex.Contains('Math.Min(1739, Game1.uiViewport.Width - 12)')) 'Codex 115% regression.'
Require ($profile.Contains('Math.Min(1518, Game1.uiViewport.Width - 16)')) 'Profile 115% regression.'

Log 'Building Alpha 6.6.25 Pelipper Town 1.1.9 Native Bridge...'
Log 'NATIVE NPC RECALL: exact VillagerCompanionManager lifecycle wired.'
Log 'NATIVE PLAYER QUOTA: CompanionRuntime.DeployBeside(Farmer,bool) pre-spawn gate wired.'
Log 'SOURCE-LIVE 2/2 HARD CAP: preserved.'
Log 'PELIPPER SOURCE AUTHORITY: render/movement/controller ownership untouched.'

dotnet build $project -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

$dll = Join-Path $sourceDir 'bin\Release\net6.0\TeamUp.dll'
Require (Test-Path $dll) 'TeamUp.dll missing after build.'

# .NET reflection lookup names live in the #US heap as UTF-16 while type/method metadata names
# are visible to normal strings. Audit both representations so the gate checks the compiled DLL,
# not just source text.
$dllAscii = (& strings $dll) -join "`n"
$dllUtf16 = (& strings -el $dll) -join "`n"
$dllStrings = $dllAscii + "`n" + $dllUtf16
Require ($dllStrings.Contains('PelipperTown119NativeBridge')) 'Native bridge absent from DLL.'
Require ($dllStrings.Contains('SetConfiguredCompanionEnabled')) 'Native villager method absent from DLL.'
Require ($dllStrings.Contains('ApplyConfiguredAssignments')) 'Native apply method absent from DLL.'
Require ($dllStrings.Contains('PelipperTown119DeployQuotaPatch')) 'Native deploy quota patch absent from DLL.'
Require ($dllStrings.Contains('DeployBeside')) 'Native deploy boundary absent from DLL.'
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

Log 'NATIVE 1.1.9 SURFACE: PRESENT IN DLL'
Log 'NPC RETURN: SetConfiguredCompanionEnabled + ApplyConfiguredAssignments PRESENT'
Log 'PLAYER THIRD-SPAWN GATE: DeployBeside PREFIX PRESENT'
Log 'SOURCE AUTHORITY LEGACY RENDER SYMBOLS: ABSENT IN DLL'
Log 'BUILD SUCCESS - ALPHA 6.6.25'
Log "ZIP: $zipName"
Log "SHA256: $hash"

Write-Host "BUILD SUCCESS - ALPHA 6.6.25"
Write-Host "ZIP: $zip"
Write-Host "SHA256: $hash"
