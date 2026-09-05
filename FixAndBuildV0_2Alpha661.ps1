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

# Bootstrap-only repairs. Materialization persists these fixes into the direct builder,
# then this temporary bootstrap file is removed before the authoritative run.
$builder = Join-Path $root 'BuildV0_2Alpha661.ps1'
$builderText = [System.IO.File]::ReadAllText($builder, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")

$badGuard = @'
if (-not $followText.Contains('long recruiterId,`n        Farmer owner')) {
'@.Trim()
$goodGuard = @'
if (-not $followText.Contains("UpdatePartyMembers(members, recruiterId, owner);")) {
'@.Trim()
if ($builderText.Contains($badGuard)) {
    $builderText = $builderText.Replace($badGuard, $goodGuard)
}

$badCompanionGuard = @'
if (-not $followText.Contains("long recruiterId,`n        Farmer owner)")) {
'@.Trim()
$goodCompanionGuard = @'
if (-not $followText.Contains("FindCompanionTile(owner.currentLocation, owner.Tile, playerPetIndex++)")) {
'@.Trim()
if ($builderText.Contains($badCompanionGuard)) {
    $builderText = $builderText.Replace($badCompanionGuard, $goodCompanionGuard)
}

# PowerShell does not use backslash as its string escape. The builder originally emitted
# backslashes into C# (\"text\"). Use PowerShell's backtick escape in the generator instead.
$builderText = $builderText.Replace(
    '= \"Ronvotri.TeamUp/PartyControlled\";',
    '= `"Ronvotri.TeamUp/PartyControlled`";')
$builderText = $builderText.Replace(
    '= \"Ronvotri.TeamUp/PartyControllerOwner\";',
    '= `"Ronvotri.TeamUp/PartyControllerOwner`";')

[System.IO.File]::WriteAllText($builder, $builderText, $utf8NoBom)

& (Join-Path $root 'BuildV0_2Alpha661.ps1')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
