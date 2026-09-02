$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw

$old = 'Team Up! v0.2.0-alpha.1 full vanilla Codex + real NPC combat loaded.'
$new = 'Team Up! v0.2.0-alpha.2 combat feedback + signature VFX loaded.'

if ($mod.Contains($new)) {
    Write-Host 'v0.2-alpha.2 integration already applied.'
    exit 0
}

if (-not $mod.Contains($old)) {
    throw 'v0.2-alpha.2 patch failed: alpha.1 integration marker not found.'
}

$mod = $mod.Replace($old, $new)
Set-Content -Path $modPath -Value $mod -Encoding UTF8
Write-Host 'v0.2-alpha.2 integration patch applied.'
