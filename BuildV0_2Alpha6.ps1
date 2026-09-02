$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$projectDir = Split-Path -Parent $project
$buildOut = Join-Path $projectDir 'bin\Release\net6.0'
$releaseDir = Join-Path $root 'release\Team Up'
$releaseRoot = Join-Path $root 'release'
$archive = Join-Path $releaseRoot 'TeamUp_v0.2.0-alpha.6.1.2_CARDCHA_DEBUG_FOLLOW_GHOST_TEST.zip'
$log = Join-Path $root 'BUILD_LOG.txt'
$finalizer = Join-Path $root '_build_support\FinalizeV0_2Alpha6.ps1'
$compileFixer = Join-Path $root '_build_support\FixCompileV0_2Alpha6.ps1'
$uxFixer = Join-Path $root '_build_support\FixAlpha612UxRegressions.ps1'
$followPerfFixer = Join-Path $root '_build_support\FixAlpha611FollowPerformance.ps1'
$partyGhostFixer = Join-Path $root '_build_support\FixAlpha612PartyGhosting.ps1'
$debugIntegrator = Join-Path $root '_build_support\IntegrateAlpha61DebugHarness.ps1'
$modEntry = Join-Path $projectDir 'ModEntry.cs'
$followSourcePath = Join-Path $projectDir 'Following\FollowService.cs'

function Ensure-Replace([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Alpha 6 integration could not locate $label." }
    return $text.Replace($old, $new)
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found. Install the .NET SDK first.'
}

foreach ($requiredScript in @($finalizer, $compileFixer, $uxFixer, $followPerfFixer, $partyGhostFixer, $debugIntegrator)) {
    if (-not (Test-Path $requiredScript)) {
        throw "Required build helper is missing: $requiredScript"
    }
}

"Team Up v0.2.0-alpha.6.1.2 build started: $(Get-Date -Format o)" | Set-Content $log
"dotnet: $(& dotnet --version)" | Add-Content $log

foreach ($script in @($finalizer, $compileFixer, $uxFixer, $followPerfFixer, $partyGhostFixer, $debugIntegrator)) {
    try {
        [void][scriptblock]::Create([System.IO.File]::ReadAllText($script))
        "Preflight OK: $(Split-Path $script -Leaf)" | Tee-Object -FilePath $log -Append
    }
    catch {
        "Preflight FAILED: $(Split-Path $script -Leaf) - $($_.Exception.Message)" | Tee-Object -FilePath $log -Append
        throw
    }
}

"Preparing stable Alpha 5 foundation..." | Tee-Object -FilePath $log -Append
& $finalizer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Foundation finalization failed.' }

"Applying compile compatibility fixes..." | Tee-Object -FilePath $log -Append
& $compileFixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Compile compatibility step failed.' }

"Integrating Alpha 6 signature/rescue layer..." | Tee-Object -FilePath $log -Append
$modSource = [System.IO.File]::ReadAllText($modEntry)
$modSource = Ensure-Replace $modSource `
    '    private CombatService Combat { get; set; } = null!;' `
    "    private CombatService Combat { get; set; } = null!;`r`n    private Alpha6CombatPolishService Alpha6Polish { get; set; } = null!;" `
    'Alpha 6 combat service field'
$modSource = Ensure-Replace $modSource `
    '        Combat = new CombatService(Monitor, Follow, Progression);' `
    "        Combat = new CombatService(Monitor, Follow, Progression);`r`n        Alpha6Polish = new Alpha6CombatPolishService(Monitor, Progression);" `
    'Alpha 6 service construction'
$modSource = Ensure-Replace $modSource `
    '        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);' `
    "        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);`r`n        Alpha6Polish.Update(Party.Members, Game1.player.UniqueMultiplayerID);" `
    'Alpha 6 update hook'
if (-not $modSource.Contains('Alpha6Polish.Clear();')) {
    if (-not $modSource.Contains('        Combat.Clear();')) { throw 'Alpha 6 integration could not locate combat clear hooks.' }
    $modSource = $modSource.Replace('        Combat.Clear();', "        Combat.Clear();`r`n        Alpha6Polish.Clear();")
}
$modSource = $modSource.Replace('Team Up! v0.2.0-alpha.3.4 survival + progression + mastery + equipment loaded.', 'Team Up! v0.2.0-alpha.6.1.2 Cardcha debug + follow anti-thrash + party ghosting loaded.')
$modSource = $modSource.Replace('Team Up! v0.2.0-alpha.6 signature skills + farmer rescue + combat polish loaded.', 'Team Up! v0.2.0-alpha.6.1.2 Cardcha debug + follow anti-thrash + party ghosting loaded.')
$modSource = $modSource.Replace('Team Up! v0.2.0-alpha.6.1 signature skills + rescue + follow/codex/i18n hotfix loaded.', 'Team Up! v0.2.0-alpha.6.1.2 Cardcha debug + follow anti-thrash + party ghosting loaded.')
$modSource = $modSource.Replace('Team Up! v0.2.0-alpha.6.1 Cardcha test bridge + debug presets loaded.', 'Team Up! v0.2.0-alpha.6.1.2 Cardcha debug + follow anti-thrash + party ghosting loaded.')
$modSource = $modSource.Replace('Team Up! v0.2.0-alpha.6.1.1 Cardcha test bridge + debug presets loaded.', 'Team Up! v0.2.0-alpha.6.1.2 Cardcha debug + follow anti-thrash + party ghosting loaded.')
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($modEntry, $modSource, $utf8NoBom)

"Applying Alpha 6.1.2 robust Follow + Codex + dialogue hint + Vietnamese i18n fixes..." | Tee-Object -FilePath $log -Append
& $uxFixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.1.2 UX hotfix step failed.' }

"Applying generic follow anti-thrash safeguard..." | Tee-Object -FilePath $log -Append
& $followPerfFixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Follow performance step failed.' }

"Applying Alpha 6.1.2 Farmer-through-party collision behavior..." | Tee-Object -FilePath $log -Append
& $partyGhostFixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Party ghosting step failed.' }

"Integrating Cardcha arena bridge + Team Up debug presets..." | Tee-Object -FilePath $log -Append
& $debugIntegrator 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'Debug harness integration failed.' }

$integratedSource = [System.IO.File]::ReadAllText($modEntry)
if (-not $integratedSource.Contains('DebugTools.RegisterCommands();') -or -not $integratedSource.Contains('Team Up DEBUG HARNESS READY')) {
    throw 'Debug harness verification failed before compile: registration marker missing from ModEntry.cs.'
}
$followSource = [System.IO.File]::ReadAllText($followSourcePath)
if (-not $followSource.Contains('RepathCooldownUpdates') -or -not $followSource.Contains('SuspendForUnsafeTarget')) {
    throw 'Follow performance verification failed before compile: anti-thrash markers missing from FollowService.cs.'
}
if (-not $followSource.Contains('EnableFarmerPassThrough') -or -not $followSource.Contains('npc.farmerPassesThrough = true;')) {
    throw 'Party ghosting verification failed before compile: pass-through markers missing from FollowService.cs.'
}
"Debug harness source verification: OK" | Tee-Object -FilePath $log -Append
"Follow anti-thrash source verification: OK" | Tee-Object -FilePath $log -Append
"Party pass-through source verification: OK" | Tee-Object -FilePath $log -Append

Push-Location $root
try {
    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    & dotnet build $project -c Release --nologo --no-restore 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}
finally { Pop-Location }

$dll = Join-Path $buildOut 'TeamUp.dll'
if (-not (Test-Path $dll)) { throw "Compilation returned success but TeamUp.dll was not found at $dll" }
if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
Copy-Item $dll $releaseDir

$manifestBuilt = Join-Path $buildOut 'manifest.json'
$manifestSource = Join-Path $projectDir 'manifest.json'
$manifestDest = Join-Path $releaseDir 'manifest.json'
if (Test-Path $manifestBuilt) { Copy-Item $manifestBuilt $manifestDest }
else {
    $manifest = Get-Content $manifestSource -Raw
    $manifest = $manifest.Replace('%ProjectVersion%', '0.2.0-alpha.6.1.2')
    Set-Content -Path $manifestDest -Value $manifest -Encoding UTF8
}

$i18nBuilt = Join-Path $buildOut 'i18n'
$i18nSource = Join-Path $projectDir 'i18n'
if (Test-Path $i18nBuilt) { Copy-Item $i18nBuilt (Join-Path $releaseDir 'i18n') -Recurse }
else { Copy-Item $i18nSource (Join-Path $releaseDir 'i18n') -Recurse }

$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_1_VI.txt'
if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir 'SMOKE_TEST_V0_2_ALPHA6_1_VI.txt') }

$buildInfo = @'
TEAM UP DEBUG BUILD
Version: 0.2.0-alpha.6.1.2
Checkpoint: Cardcha Debug + Follow Anti-Thrash + Farmer-through-Party
Expected SMAPI startup marker:
Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.2
Primary command: teamup_test help
Follow safeguard: invalid/unwalkable targets suspend pathfinding; rapid target movement is repath-throttled.
Party collision: Farmer can pass through active/waiting Team Up members. Original NPC pass-through state is restored when released to vanilla.
'@
Set-Content -Path (Join-Path $releaseDir 'DEBUG_BUILD_INFO.txt') -Value $buildInfo -Encoding UTF8

if (Test-Path $archive) { Remove-Item $archive -Force }
if (-not (Test-Path $releaseRoot)) { New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null }
Compress-Archive -Path $releaseDir -DestinationPath $archive -CompressionLevel Optimal
$hash = (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $archive -Leaf)" | Set-Content (Join-Path $releaseRoot 'TeamUp_v0.2.0-alpha.6.1.2_CARDCHA_DEBUG_FOLLOW_GHOST_TEST.sha256.txt')

Write-Host ''
Write-Host '========================================================='
Write-Host 'BUILD SUCCESS - ALPHA 6.1.2'
Write-Host 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.1.2'
Write-Host 'FOLLOW SAFEGUARD: INVALID TARGET + REPATH THROTTLE ENABLED'
Write-Host 'PARTY GHOSTING: FARMER CAN PASS THROUGH TEAM MEMBERS'
Write-Host "ZIP: $archive"
Write-Host "SHA256: $hash"
Write-Host '========================================================='
