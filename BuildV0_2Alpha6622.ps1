$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$profile = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$alpha6618 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6618.cs'
$alpha6619 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6619.cs'
$alpha6621 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6621.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6622'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_22_CODEX_115_UI_POLISH_VI.txt'
$version = '0.2.0-alpha.6.6.22'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.22_CODEX_115_UI_POLISH_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.22_CODEX_115_UI_POLISH_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function Write-Utf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}
function RequireReplace([string]$text, [string]$old, [string]$new, [string]$label) {
    if (-not $text.Contains($old) -and -not $text.Contains($new)) { throw "Missing 6.6.22 patch anchor: $label" }
    return $text.Replace($old, $new)
}
function Log([string]$text) {
    $text | Tee-Object -FilePath $log -Append
}

$projectText = Read-Lf $project
$projectText = RequireReplace $projectText '<Version>0.2.0-alpha.6.6.21</Version>' '<Version>0.2.0-alpha.6.6.22</Version>' 'project version'
Write-Utf8 $project $projectText

$modText = Read-Lf $modEntry
$modText = RequireReplace $modText 'build: v0.2.0-alpha.6.6.21' 'build: v0.2.0-alpha.6.6.22' 'debug build label'
$modText = RequireReplace $modText 'Team Up! v0.2.0-alpha.6.6.21 Pelipper Villager Lifecycle Recall + Probe Hotfix loaded.' 'Team Up! v0.2.0-alpha.6.6.22 Codex 115% UI Polish loaded. Pelipper 6.6.21 lifecycle test path preserved.' 'load label'
Write-Utf8 $modEntry $modText

$codexText = Read-Lf $codex
$codexText = RequireReplace $codexText 'Math.Min(1512, Game1.uiViewport.Width - 12)' 'Math.Min(1739, Game1.uiViewport.Width - 12)' 'Codex max width +15%'
$codexText = RequireReplace $codexText 'Math.Min(912, Game1.uiViewport.Height - 12)' 'Math.Min(1049, Game1.uiViewport.Height - 12)' 'Codex max height +15%'
$codexText = RequireReplace $codexText '_visibleRows = Math.Clamp((height - 272) / 67, 3, 9);' '_visibleRows = Math.Clamp((height - 272) / 67, 3, height >= 1000 ? 11 : 9);' 'Codex expanded row capacity'
Write-Utf8 $codex $codexText

$profileText = Read-Lf $profile
$profileText = RequireReplace $profileText 'Math.Min(1320, Game1.uiViewport.Width - 16)' 'Math.Min(1518, Game1.uiViewport.Width - 16)' 'Profile max width +15%'
$profileText = RequireReplace $profileText 'Math.Min(780, Game1.uiViewport.Height - 16)' 'Math.Min(897, Game1.uiViewport.Height - 16)' 'Profile max height +15%'
$profileText = RequireReplace $profileText 'int leftWidth = Math.Min(340, Math.Max(260, width / 3));' 'int leftWidth = Math.Min(width >= 1450 ? 391 : 340, Math.Max(260, width / 3));' 'Large-profile identity column'
$profileText = RequireReplace $profileText 'int portraitSize = Math.Min(184, Math.Max(126, panelWidth - 96));' 'int portraitSize = Math.Min(width >= 1450 ? 212 : 184, Math.Max(126, panelWidth - 96));' 'Large-profile portrait'
Write-Utf8 $profile $profileText

if (Test-Path $log) { Remove-Item $log -Force }

$projectText = Read-Lf $project
$modText = Read-Lf $modEntry
$codexText = Read-Lf $codex
$profileText = Read-Lf $profile
$a18 = Read-Lf $alpha6618
$a19 = Read-Lf $alpha6619
$a21 = Read-Lf $alpha6621
$followText = Read-Lf $follow
$combatText = Read-Lf $combat

if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.22</Version>')) { throw '6.6.22 version missing.' }
if (-not $modText.Contains('build: v0.2.0-alpha.6.6.22')) { throw '6.6.22 debug build label missing.' }
if (-not $codexText.Contains('Math.Min(1739, Game1.uiViewport.Width - 12)')) { throw 'Codex 115% width missing.' }
if (-not $codexText.Contains('Math.Min(1049, Game1.uiViewport.Height - 12)')) { throw 'Codex 115% height missing.' }
if (-not $codexText.Contains('height >= 1000 ? 11 : 9')) { throw 'Codex large-screen row expansion missing.' }
if (-not $profileText.Contains('Math.Min(1518, Game1.uiViewport.Width - 16)')) { throw 'Profile 115% width missing.' }
if (-not $profileText.Contains('Math.Min(897, Game1.uiViewport.Height - 16)')) { throw 'Profile 115% height missing.' }
if (-not $profileText.Contains('width >= 1450 ? 391 : 340')) { throw 'Profile large-screen identity column scaling missing.' }
if (-not $profileText.Contains('width >= 1450 ? 212 : 184')) { throw 'Profile large-screen portrait scaling missing.' }

foreach ($token in @(
    'GetEffectiveCombatCompanionCountAlpha6618',
    'PrepareNpcCompanionRecruitCapacityAlpha6618',
    'physically deployed and must block a third companion'
)) {
    if (-not $a18.Contains($token)) { throw "6.6.18 hard-cap regression token missing: $token" }
}
if (-not $a19.Contains('PelipperVillagerLifecycleBridge.Configure(runtimeRoot);')) { throw '6.6.21 Pelipper runtime root regression.' }
if (-not $a21.Contains('teamup_pelipper_probe')) { throw '6.6.21 Pelipper probe regression.' }
if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService water/pathfinding performance regression.' }
if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService water/pathfinding performance regression.' }

Log 'Building Alpha 6.6.22 Codex 115% UI Polish...'
Log 'CODEX BROWSER: max canvas 1512x912 -> 1739x1049 (+15%).'
Log 'CODEX ROWS: large-height browser can show up to 11 rows instead of leaving dead space.'
Log 'CHARACTER PROFILE: max canvas 1320x780 -> 1518x897 (+15%).'
Log 'PROFILE LARGE SCREEN: identity column and portrait expand only when viewport supports the larger layout.'
Log 'SMALL VIEWPORT SAFETY: original viewport clamps remain active.'
Log 'PELIPPER: Alpha 6.6.21 runtime/lifecycle/probe path preserved unchanged.'
Log 'HARD CAP: source-live 2/2 guard preserved.'
Log 'COMBAT/FOLLOW: no behavior changes in this build.'

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
if (-not (Test-Path $zip)) { throw 'Alpha 6.6.22 ZIP was not created.' }

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Utf8 $shaPath ("$hash  $zipName`r`n")
Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

Log 'CODEX 115% MAX CANVAS: ENABLED'
Log 'CODEX LARGE-SCREEN 11 ROWS: ENABLED'
Log 'PROFILE 115% MAX CANVAS: ENABLED'
Log 'PROFILE LARGE-SCREEN PORTRAIT EXPANSION: ENABLED'
Log 'SMALL VIEWPORT CLAMP: PRESERVED'
Log 'PELIPPER 6.6.21 LIFECYCLE PATH: PRESERVED'
Log 'SOURCE-LIVE 2/2 HARD CAP: PRESERVED'
Log 'BUILD SUCCESS - ALPHA 6.6.22'
Log "ZIP: $zipName"
Log "SHA256: $hash"
