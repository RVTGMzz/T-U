$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$builder = Join-Path $root 'BuildV0_2Alpha660.ps1'
$text = [System.IO.File]::ReadAllText($builder, [System.Text.Encoding]::UTF8)
$old = "'            float radius = GetEngagementRadius(member.Engagement);'"
$new = "'        float radius = GetEngagementRadius(member.Engagement);'"
if ($text.Contains($old)) {
    $text = $text.Replace($old, $new)
    [System.IO.File]::WriteAllText($builder, $text, (New-Object System.Text.UTF8Encoding($false)))
}
& $builder
exit $LASTEXITCODE
