$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$projectDir = Split-Path -Parent $project
$buildOut = Join-Path $projectDir 'bin\Release\net6.0'
$releaseDir = Join-Path $root 'release\Team Up'
$releaseRoot = Join-Path $root 'release'
$archive = Join-Path $releaseRoot 'TeamUp_v0.2.0-alpha.6_SIGNATURE_RESCUE_POLISH_TEST.zip'
$log = Join-Path $root 'BUILD_LOG.txt'
$finalizer = Join-Path $root '_build_support\FinalizeV0_2Alpha5.ps1'
$compileFixer = Join-Path $root '_build_support\FixCompileV0_2Alpha5.ps1'
$modEntry = Join-Path $projectDir 'ModEntry.cs'

function Ensure-Replace([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Alpha 6 integration could not locate $label." }
    return $text.Replace($old, $new)
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found. Install the .NET SDK first.'
}

foreach ($requiredScript in @($finalizer, $compileFixer)) {
    if (-not (Test-Path $requiredScript)) {
        throw "Required build helper is missing: $requiredScript"
    }
}

"Team Up v0.2.0-alpha.6 build started: $(Get-Date -Format o)" | Set-Content $log
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

"Preparing stable Alpha 5 foundation..." | Tee-Object -FilePath $log -Append
& $finalizer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) {
    throw 'Foundation finalization failed.'
}

"Applying compile compatibility fixes..." | Tee-Object -FilePath $log -Append
& $compileFixer 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) {
    throw 'Compile compatibility step failed.'
}

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
    if (-not $modSource.Contains('        Combat.Clear();')) {
        throw 'Alpha 6 integration could not locate combat clear hooks.'
    }
    $modSource = $modSource.Replace(
        '        Combat.Clear();',
        "        Combat.Clear();`r`n        Alpha6Polish.Clear();")
}

$modSource = $modSource.Replace(
    'Team Up! v0.2.0-alpha.3.4 survival + progression + mastery + equipment loaded.',
    'Team Up! v0.2.0-alpha.6 signature skills + farmer rescue + combat polish loaded.')

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
    $manifest = $manifest.Replace('%ProjectVersion%', '0.2.0-alpha.6')
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

$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_VI.txt'
if (Test-Path $smoke) {
    Copy-Item $smoke (Join-Path $releaseDir 'SMOKE_TEST_V0_2_ALPHA6_VI.txt')
}

if (Test-Path $archive) {
    Remove-Item $archive -Force
}
if (-not (Test-Path $releaseRoot)) {
    New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null
}
Compress-Archive -Path $releaseDir -DestinationPath $archive -CompressionLevel Optimal

$hash = (Get-FileHash $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $(Split-Path $archive -Leaf)" | Set-Content (Join-Path $releaseRoot 'TeamUp_v0.2.0-alpha.6_SIGNATURE_RESCUE_POLISH_TEST.sha256.txt')

Write-Host ''
Write-Host '========================================================='
Write-Host 'BUILD SUCCESS - SIGNATURE + RESCUE + COMBAT POLISH'
Write-Host "ZIP: $archive"
Write-Host "SHA256: $hash"
Write-Host '========================================================='
