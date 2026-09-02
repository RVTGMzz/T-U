$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$modPath = Join-Path $repoRoot 'src\TeamUp\ModEntry.cs'
$mod = [System.IO.File]::ReadAllText($modPath, [System.Text.Encoding]::UTF8)
$mod = $mod.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Alpha 6.1.2 debug integration could not locate $label." }
    return $text.Replace($old, $new)
}

if (-not $mod.Contains('using Ronvotri.TeamUp.Debugging;')) {
    $mod = Replace-Required $mod `
        'using Ronvotri.TeamUp.Following;' `
        "using Ronvotri.TeamUp.Debugging;`r`nusing Ronvotri.TeamUp.Following;" `
        'debug namespace insertion point'
}

if (-not $mod.Contains('private TeamUpDebugService DebugTools { get; set; }')) {
    $mod = Replace-Required $mod `
        '    private Alpha6CombatPolishService Alpha6Polish { get; set; } = null!;' `
        "    private Alpha6CombatPolishService Alpha6Polish { get; set; } = null!;`r`n    private TeamUpDebugService DebugTools { get; set; } = null!;" `
        'debug service field'
}

$marker = 'Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.2'

if (-not $mod.Contains('DebugTools.RegisterCommands();')) {
    $old = '        Alpha6Polish = new Alpha6CombatPolishService(Monitor, Progression);'
    $new = @'
        Alpha6Polish = new Alpha6CombatPolishService(Monitor, Progression);
        DebugTools = new TeamUpDebugService(
            Helper,
            Monitor,
            Party,
            Progression,
            Follow,
            Combat,
            Alpha6Polish,
            SavePartyNow);
        DebugTools.RegisterCommands();
        Monitor.Log("Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.2", LogLevel.Info);
'@
    $mod = Replace-Required $mod $old $new.TrimEnd() 'debug service construction'
}
else {
    $mod = [regex]::Replace(
        $mod,
        'Team Up DEBUG HARNESS READY \| command: teamup_test \| build: v0\.2\.0-alpha\.6\.1(?:\.1)?',
        $marker,
        1)

    if (-not $mod.Contains($marker)) {
        $oldMarker = '        DebugTools.RegisterCommands();'
        $newMarker = @'
        DebugTools.RegisterCommands();
        Monitor.Log("Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.2", LogLevel.Info);
'@
        $mod = Replace-Required $mod $oldMarker $newMarker.TrimEnd() 'debug ready marker'
    }
}

[System.IO.File]::WriteAllText($modPath, $mod, $utf8NoBom)
Write-Host 'Alpha 6.1.2 Team Up debug harness integrated with startup marker.'
