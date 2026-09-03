$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$integrator = Join-Path $root '_build_support\IntegrateAlpha643CardchaCombatSandbox.ps1'
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha643'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.4.3_CARDCHA_COMBAT_SANDBOX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.4.3_CARDCHA_COMBAT_SANDBOX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.4.3'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

try {
    if (-not (Test-Path $integrator)) { throw "Missing Alpha 6.4.3 integrator: $integrator" }
    if (-not (Test-Path $project)) { throw "Missing Team Up project: $project" }

    Log 'Integrating Alpha 6.4.3 Cardcha Combat Sandbox...'
    & $integrator 2>&1 | Tee-Object -FilePath $log -Append

    Log 'Restoring Team Up...'
    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    Log 'Compiling Team Up Alpha 6.4.3...'
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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.4.3 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText($shaPath, "$hash  $(Split-Path $zip -Leaf)`r`n", $utf8NoBom)

    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_4_3_CARDCHA_SANDBOX_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }

    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.4.3'
    Log 'SMAPI MUST SHOW: Team Up DEBUG HARNESS READY ... 6.4.3'
    Log 'HOST: CARDCHA CARD TEST ARENA (NOT REGION I / AIRSHIP MAP)'
    Log 'WAVES: EASY CAP 5 / NORMAL CAP 8 / HARD CAP 11'
    Log 'PACING: NEXT WAVE WHEN <=1 REMAINS, ~2.2 SECOND DELAY'
    Log 'SAFETY: TEAM UP CLEARS ONLY ITS OWN TAGGED MONSTERS'
    Log 'TARGETING: CARDCHA DUMMY + KILL TARGETS EXCLUDED FROM TEAM UP AI'
    Log 'LOCKS: UI READABILITY + BOND + SKILLS + EQUIPMENT + ICONS RETAINED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
