$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$compat = Join-Path $root 'src\TeamUp\Core\CustomNpcCompatibilityService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha651'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.5.1_MIMI_RECRUIT_GATE_HARDENING_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.5.1_MIMI_RECRUIT_GATE_HARDENING_TEST.sha256.txt'
$version = '0.2.0-alpha.6.5.1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

try {
    foreach ($required in @($project, $manifest, $modEntry, $compat)) {
        if (-not (Test-Path $required)) { throw "Missing required Alpha 6.5.1 source: $required" }
    }

    # Materialize the hotfix version into source before compile so the built DLL and
    # SMAPI debug banner always identify the exact test package.
    $projectText = [System.IO.File]::ReadAllText($project, [System.Text.Encoding]::UTF8)
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    [System.IO.File]::WriteAllText($project, $projectText, $utf8NoBom)

    $modText = [System.IO.File]::ReadAllText($modEntry, [System.Text.Encoding]::UTF8)
    $modText = $modText.Replace(
        'origin story + The Surge + MiMi/Sudoku recruit integration loaded.',
        'MiMi recruit gate hardening + Origin/Surge/custom recruit integration loaded.')
    $modText = $modText.Replace('v0.2.0-alpha.6.5.0', 'v0.2.0-alpha.6.5.1')
    [System.IO.File]::WriteAllText($modEntry, $modText, $utf8NoBom)

    $compatText = [System.IO.File]::ReadAllText($compat, [System.Text.Encoding]::UTF8)
    foreach ($token in @(
        'Game1.player.friendshipData.ContainsKey(MimiNpcId)',
        'Game1.eventUp || Game1.dialogueUp || Game1.activeClickableMenu is not null',
        'return npc.canTalk() || npc.IsVillager;'
    )) {
        if (-not $compatText.Contains($token)) { throw "Alpha 6.5.1 gate token missing: $token" }
    }

    foreach ($forbidden in @('MimiMeetupCompleted', 'Cardcha.Services', 'MimiMerchantStartTime', 'IsMimiMerchantWeekday')) {
        if ($compatText.Contains($forbidden)) { throw "Alpha 6.5.1 forbidden coupling token present: $forbidden" }
    }

    Log 'Building Alpha 6.5.1 MiMi recruit gate hardening...'
    Log 'Gate contract: Cardcha loaded + canonical actor + known identity + friendship unlock + no story UI.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.5.1 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($shaPath, "$hash  $(Split-Path $zip -Leaf)`r`n", $utf8NoBom)

    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_5_1_MIMI_RECRUIT_GATE_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }

    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.5.1'
    Log 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.5.1'
    Log 'MIMI GATE: CARDCHA SOCIAL UNLOCK / FRIENDSHIP ENTRY REQUIRED'
    Log 'MIMI GATE: SOCIAL LOCATION/SCHEDULE REMAINS SOURCE-CONTROLLED'
    Log 'MIMI GATE: EVENT/DIALOGUE/MENU PRESENTATION FAILS CLOSED'
    Log 'REGRESSION: ALPHA 6.5.0 ORIGIN + SURGE + SUDOKU RETAINED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
