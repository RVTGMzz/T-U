$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$projectPath = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$modEntryPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$profilePath = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$rendererPath = Join-Path $root 'src\TeamUp\UI\TraitIconRenderer.cs'
$version = '0.2.0-alpha.6.3.2'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadUtf8([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing required Alpha 6.3.2 source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

function WriteUtf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

$project = ReadUtf8 $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteUtf8 $projectPath $project

$mod = ReadUtf8 $modEntryPath
$mod = $mod.Replace('0.2.0-alpha.6.3.1', $version)
$marker = '        Monitor.Log("Team Up DEBUG HARNESS READY | command: teamup_test | build: v{0}", LogLevel.Info);' -f $version
$mod = $mod.Replace($marker + "`r`n" + $marker, $marker)
$mod = $mod.Replace($marker + "`n" + $marker, $marker)
$mod = $mod.Replace("Team Up! v$version equipment RPG polish loaded.", "Team Up! v$version signature icon art pass loaded.")
WriteUtf8 $modEntryPath $mod

$profile = ReadUtf8 $profilePath
$renderer = ReadUtf8 $rendererPath

if ($profile -notmatch 'DrawTextTraitBlock') { throw 'Character Profile does not contain the text-only passive block.' }
if ($profile -notmatch 'DrawSignatureBlock') { throw 'Character Profile does not contain the single signature icon block.' }
if ($profile -match 'TraitIconKind\.Passive') { throw 'Character Profile still draws a passive icon; Alpha 6.3.2 must show one signature icon only.' }
if ($renderer -notmatch 'BespokeSignaturePatterns') { throw 'Bespoke signature pattern registry is missing.' }
if ($renderer -notmatch 'BuildProceduralPattern') { throw 'Procedural fallback for future NPCs is missing.' }

$expected = @(
    'Abigail','Alex','Harvey','Maru','Emily',
    'Alesia','Andy','Camilla','Claire','Isaac','Jadu','Lance','Martin','Morgan','Olivia','Sophia','Victor',
    'Aguar','Blair','Carmen','Daia','Ian','Jio','June','Kenneth','Kiarra','Maddie','Shiro','Ysabelle'
)
foreach ($name in $expected) {
    if ($renderer -notmatch ('\[\"' + [regex]::Escape($name) + '\"\]')) {
        throw "Missing bespoke signature icon pattern: $name"
    }
}

Write-Host 'Alpha 6.3.2 signature icon art pass integrated.'
Write-Host 'One signature icon per completed kit; passive remains text-only; future NPCs keep procedural fallback.'
