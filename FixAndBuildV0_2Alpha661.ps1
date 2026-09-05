$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$normalize = @(
    'src\TeamUp\Core\PartyManager.cs',
    'src\TeamUp\Following\FollowService.cs',
    'src\TeamUp\Combat\CombatService.cs',
    'src\TeamUp\ModEntry.cs'
)

foreach ($relative in $normalize) {
    $path = Join-Path $root $relative
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $text = $text.Replace("`r`n", "`n")
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}

# Bootstrap-only repair: the original direct builder used a single-quoted string containing
# a backtick-n sequence, so its idempotency check could never match a real newline. Patch the
# builder itself once; materialization will persist the corrected direct builder.
$builder = Join-Path $root 'BuildV0_2Alpha661.ps1'
$builderText = [System.IO.File]::ReadAllText($builder, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
$bad = @'
if (-not $followText.Contains('long recruiterId,`n        Farmer owner')) {
'@.Trim()
$good = @'
if (-not $followText.Contains("UpdatePartyMembers(members, recruiterId, owner);")) {
'@.Trim()
if ($builderText.Contains($bad)) {
    $builderText = $builderText.Replace($bad, $good)
}
[System.IO.File]::WriteAllText($builder, $builderText, $utf8NoBom)

& (Join-Path $root 'BuildV0_2Alpha661.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
