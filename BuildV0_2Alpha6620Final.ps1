$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$alpha6619 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6619.cs'
$materializer = Join-Path $root 'BuildV0_2Alpha6620.ps1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }

# Repair the first generated 6.6.20 materializer before PowerShell parses it. Its C# here-string
# terminator landed on the same line as the final method signature, which is invalid PowerShell.
# Also remove the literal invalid API expression from the generated warning/acceptance token so
# source acceptance can correctly prove there are no GetApi<object> calls left in ModEntry code.
$materializerText = Read-Lf $materializer
$badHereString = "    private void OnAlpha6619UpdateTicked'@`n"
$goodHereString = "    private void OnAlpha6619UpdateTicked`n'@`n"
if ($materializerText.Contains($badHereString)) {
    $materializerText = $materializerText.Replace($badHereString, $goodHereString)
}
$materializerText = $materializerText.Replace(
    'no GetApi<object> retry will be attempted',
    'no invalid generic API retry will be attempted')
Write-Utf8 $materializer $materializerText

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
