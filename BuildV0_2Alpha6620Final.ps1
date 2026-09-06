$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$alpha6619 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6619.cs'
$materializer = Join-Path $root 'BuildV0_2Alpha6620.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }

# Pre-seed the two 6.6.20 state fields with real newlines. The first materializer was generated
# with literal `n inside single-quoted PowerShell strings for these two replacements; seeding the
# intended source here makes that script idempotent and keeps the actual C# source authoritative.
$a19 = Read-Lf $alpha6619
if (-not $a19.Contains('PelipperRuntimeRootLookupLoggedAlpha6620')) {
    $needle = "    private bool PelipperApiBridgeConfiguredAlpha6619;`n"
    $replacement = "    private bool PelipperApiBridgeConfiguredAlpha6619;`n    private bool PelipperRuntimeRootLookupLoggedAlpha6620;`n"
    if (-not $a19.Contains($needle)) { throw 'Alpha6619 bridge field anchor missing.' }
    $a19 = $a19.Replace($needle, $replacement)
}

if (-not $a19.Contains('PelipperApiRuntimeRootBridge.Reset();')) {
    $needle = "        PelipperApiBridgeConfiguredAlpha6619 = false;`n"
    $replacement = "        PelipperApiRuntimeRootBridge.Reset();`n        PelipperApiBridgeConfiguredAlpha6619 = false;`n        PelipperRuntimeRootLookupLoggedAlpha6620 = false;`n"
    if (-not $a19.Contains($needle)) { throw 'Alpha6619 returned-title reset anchor missing.' }
    $a19 = $a19.Replace($needle, $replacement)
}
Write-Utf8 $alpha6619 $a19

& $materializer
if ($LASTEXITCODE -ne 0) { throw 'Alpha 6.6.20 materializer failed.' }
