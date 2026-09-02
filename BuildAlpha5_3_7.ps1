$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$projectDir = Split-Path -Parent $project
$buildOut = Join-Path $projectDir 'bin\Release\net6.0'
$releaseDir = Join-Path $root 'release\Team Up'
$releaseRoot = Join-Path $root 'release'
$archive = Join-Path $releaseRoot 'TeamUp_v0.1.0-alpha.5.3.7_SMOKE_TEST.zip'
$log = Join-Path $root 'BUILD_LOG.txt'
$patch536 = Join-Path $root 'ApplyAlpha5_3_6Patches.ps1'
$patch537 = Join-Path $root 'ApplyAlpha5_3_7Patches.ps1'
$modEntry = Join-Path $projectDir 'ModEntry.cs'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found. Install the .NET SDK first.'
}

"Team Up alpha.5.3.7 build started: $(Get-Date -Format o)" | Set-Content $log
"dotnet: $(& dotnet --version)" | Add-Content $log

$modSource = Get-Content $modEntry -Raw
if ($modSource.Contains('v0.1.0-alpha.5.3.3 dialogue hint anchor hotfix loaded.')) {
    "Applying carried-forward alpha.5.3.6 integration patches..." | Tee-Object -FilePath $log -Append
    & $patch536 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'alpha.5.3.6 patch step failed.' }
}

$modSource = Get-Content $modEntry -Raw
if ($modSource.Contains('v0.1.0-alpha.5.3.6 vault + member hint hotfix loaded.')) {
    "Applying alpha.5.3.7 lifecycle patches..." | Tee-Object -FilePath $log -Append
    & $patch537 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'alpha.5.3.7 patch step failed.' }
}
elseif (-not $modSource.Contains('v0.1.0-alpha.5.3.7 native vault + special lifecycle loaded.')) {
    throw 'Source is not at a recognized alpha.5.3.6/5.3.7 integration state.'
}

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
    $manifest = $manifest.Replace('%ProjectVersion%', '0.1.0-alpha.5.3.7')
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

$smoke = Join-Path $root 'SMOKE_TEST_ALPHA5_3_7_VI.txt'
if (Test-Path $smoke) {
    Copy-Item $smoke (Join-Path $releaseDir 'SMOKE_TEST_ALPHA5_3_7_VI.txt')
}

if (Test-Path $archive) {
    Remove-Item $archive -Force
}
Compress-Archive -Path $releaseDir -DestinationPath $archive -CompressionLevel Optimal

$hash = (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $archive -Leaf)" | Set-Content (Join-Path $releaseRoot 'TeamUp_v0.1.0-alpha.5.3.7_SMOKE_TEST.sha256.txt')

Write-Host ''
Write-Host '==============================================='
Write-Host 'BUILD SUCCESS'
Write-Host "ZIP: $archive"
Write-Host "SHA256: $hash"
Write-Host '==============================================='
