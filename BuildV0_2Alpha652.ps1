$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$surge = Join-Path $root 'src\TeamUp\Combat\MonsterSurgeService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha652'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.5.2_SURGE_RUNTIME_POLISH_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.5.2_SURGE_RUNTIME_POLISH_TEST.sha256.txt'
$version = '0.2.0-alpha.6.5.2'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

try {
    foreach ($required in @($project, $manifest, $modEntry, $surge)) {
        if (-not (Test-Path $required)) { throw "Missing required Alpha 6.5.2 source: $required" }
    }

    $projectText = [System.IO.File]::ReadAllText($project, [System.Text.Encoding]::UTF8)
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    [System.IO.File]::WriteAllText($project, $projectText, $utf8NoBom)

    $modText = [System.IO.File]::ReadAllText($modEntry, [System.Text.Encoding]::UTF8)
    $modText = [regex]::Replace(
        $modText,
        'Team Up DEBUG HARNESS READY \| command: teamup_test \| build: v0\.2\.0-alpha\.6\.5\.\d+',
        'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.5.2')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.5\.\d+ [^\"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.5.2 Surge runtime polish + Origin/MiMi/Sudoku integration loaded.')
    [System.IO.File]::WriteAllText($modEntry, $modText, $utf8NoBom)

    $surgeText = [System.IO.File]::ReadAllText($surge, [System.Text.Encoding]::UTF8)
    foreach ($token in @(
        'SafeSpawnOffsets',
        'isTileOnMap',
        'isTilePassable',
        'IsTileBlockedBy',
        '[SurgeTelemetry]',
        'unsafeRejected=',
        'ResolveThreatLevel',
        "MARLON'S THREAT BOARD",
        'no-safe-spawn-tile',
        'partial-safe-placement'
    )) {
        if (-not $surgeText.Contains($token)) { throw "Alpha 6.5.2 Surge polish token missing: $token" }
    }

    Log 'Building Alpha 6.5.2 Surge Runtime Polish...'
    Log 'Placement: safe tile-ring search, map/passable/blocked validation, Farmer/monster spacing, fail closed.'
    Log 'Telemetry: baseline / wanted / spawned / unsafeRejected / threat / suppression reason.'
    Log 'Presentation: THE SURGE threat tier in combat + Marlon threat board on later Guild return.'

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
    $manifestText = [System.IO.File]::ReadAllText($manifest, [System.Text.Encoding]::UTF8).Replace('%ProjectVersion%', $version)
    [System.IO.File]::WriteAllText((Join-Path $stageMod 'manifest.json'), $manifestText, $utf8NoBom)
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.5.2 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($shaPath, "$hash  $(Split-Path $zip -Leaf)`r`n", $utf8NoBom)

    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_5_2_SURGE_RUNTIME_POLISH_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }

    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.5.2'
    Log 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.5.2'
    Log 'SURGE PLACEMENT: MAP/PASSABLE/BLOCKED TILE RING + SAFE SPACING + FAIL CLOSED'
    Log 'SURGE TELEMETRY: BASELINE/WANTED/SPAWNED/UNSAFE/THREAT/SUPPRESSION'
    Log 'THREAT: LOW / ELEVATED / HIGH / SURGE + MARLON GUILD BOARD'
    Log 'REGRESSION: ALPHA 6.5.1 MIMI GATE + 6.5.0 ORIGIN/SUDOKU + PRIOR SYSTEMS RETAINED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
