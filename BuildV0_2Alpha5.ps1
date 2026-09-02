$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$projectDir = Split-Path -Parent $project
$buildOut = Join-Path $projectDir 'bin\Release\net6.0'
$releaseDir = Join-Path $root 'release\Team Up'
$releaseRoot = Join-Path $root 'release'
$archive = Join-Path $releaseRoot 'TeamUp_v0.2.0-alpha.5_THREAT_AGGRO_AI_BALANCE_TEST.zip'
$log = Join-Path $root 'BUILD_LOG.txt'
$finalizer = Join-Path $root '_build_support\FinalizeV0_2Alpha5.ps1'
$compileFixer = Join-Path $root '_build_support\FixCompileV0_2Alpha5.ps1'
$modEntry = Join-Path $projectDir 'ModEntry.cs'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found. Install the .NET SDK first.'
}

foreach ($requiredScript in @($finalizer, $compileFixer)) {
    if (-not (Test-Path $requiredScript)) {
        throw "Required build helper is missing: $requiredScript"
    }
}

"Team Up v0.2.0-alpha.5 build started: $(Get-Date -Format o)" | Set-Content $log
"dotnet: $(& dotnet --version)" | Add-Content $log

foreach ($script in @($finalizer, $compileFixer)) {
    try {
        [void][scriptblock]::Create([System.IO.File]::ReadAllText($script))
        "Preflight OK: $(Split-Path $script -Leaf)" | Tee-Object -FilePath $log -Append
    }
    catch {
        "Preflight FAILED: $(Split-Path $script -Leaf) - $($_.Exception.Message)" | Tee-Object -FilePath $log -Append
        throw
    }
}

"Preparing consolidated alpha 3+4 foundation..." | Tee-Object -FilePath $log -Append
& $finalizer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) {
    throw 'Alpha 3+4 foundation finalization failed.'
}

"Applying compile compatibility fixes..." | Tee-Object -FilePath $log -Append
& $compileFixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) {
    throw 'Compile compatibility step failed.'
}

# Alpha 5 combat lives directly in src/TeamUp/Combat. The carried-forward finalizer only
# materializes the stable ModEntry/follow/equipment foundation, then this updates the runtime marker.
$modSource = [System.IO.File]::ReadAllText($modEntry)
$modSource = $modSource.Replace(
    'Team Up! v0.2.0-alpha.3.4 survival + progression + mastery + equipment loaded.',
    'Team Up! v0.2.0-alpha.5 threat + aggro + combat AI + balance loaded.')
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($modEntry, $modSource, $utf8NoBom)

Push-Location $root
try {
    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    & dotnet build $project -c Release --nologo --no-restore 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
}
finally {
    Pop-Location
}

$dll = Join-Path $buildOut 'TeamUp.dll'
if (-not (Test-Path $dll)) {
    throw "Compilation returned success but TeamUp.dll was not found at $dll"
}

if (Test-Path $releaseDir) {
    Remove-Item $releaseDir -Recurse -Force
}
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
Copy-Item $dll $releaseDir

$manifestBuilt = Join-Path $buildOut 'manifest.json'
$manifestSource = Join-Path $projectDir 'manifest.json'
$manifestDest = Join-Path $releaseDir 'manifest.json'
if (Test-Path $manifestBuilt) {
    Copy-Item $manifestBuilt $manifestDest
}
else {
    $manifest = Get-Content $manifestSource -Raw
    $manifest = $manifest.Replace('%ProjectVersion%', '0.2.0-alpha.5')
    Set-Content -Path $manifestDest -Value $manifest -Encoding UTF8
}

$i18nBuilt = Join-Path $buildOut 'i18n'
$i18nSource = Join-Path $projectDir 'i18n'
if (Test-Path $i18nBuilt) {
    Copy-Item $i18nBuilt (Join-Path $releaseDir 'i18n') -Recurse
}
else {
    Copy-Item $i18nSource (Join-Path $releaseDir 'i18n') -Recurse
}

$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA5_VI.txt'
if (Test-Path $smoke) {
    Copy-Item $smoke (Join-Path $releaseDir 'SMOKE_TEST_V0_2_ALPHA5_VI.txt')
}

if (Test-Path $archive) {
    Remove-Item $archive -Force
}
if (-not (Test-Path $releaseRoot)) {
    New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
}
Compress-Archive -Path $releaseDir -DestinationPath $archive -CompressionLevel Optimal

$hash = (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $archive -Leaf)" | Set-Content (Join-Path $releaseRoot 'TeamUp_v0.2.0-alpha.5_THREAT_AGGRO_AI_BALANCE_TEST.sha256.txt')

Write-Host ''
Write-Host '========================================================='
Write-Host 'BUILD SUCCESS - THREAT + AGGRO + COMBAT AI + BALANCE'
Write-Host "ZIP: $archive"
Write-Host "SHA256: $hash"
Write-Host '========================================================='
