$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$followPath = Join-Path $repoRoot 'src\TeamUp\Following\FollowService.cs'
$follow = [System.IO.File]::ReadAllText($followPath, [System.Text.Encoding]::UTF8)
$follow = $follow.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`n", "`r`n")

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Alpha 6.1.2 party ghosting fix could not locate $label." }
    return $text.Replace($old, $new)
}

function Replace-Method([string]$text, [string]$methodName, [string]$nextMethodName, [string]$replacement) {
    $method = [regex]::Escape($methodName)
    $next = [regex]::Escape($nextMethodName)
    $pattern = "(?ms)^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$method\([^\r\n]*\)\s*\{.*?(?=^    (?:private|public|internal|protected) (?:static )?[^\r\n]+\s+$next\()"
    if (-not [regex]::IsMatch($text, $pattern)) {
        throw "Alpha 6.1.2 party ghosting fix could not locate method $methodName before $nextMethodName."
    }
    return [regex]::Replace($text, $pattern, $replacement + "`r`n`r`n", 1)
}

if (-not $follow.Contains('_baseFarmerPassesThrough')) {
    $follow = Replace-Required $follow `
        '    private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();' `
        "    private readonly Dictionary<NPC, float> _baseAddedSpeeds = new();`r`n    private readonly Dictionary<NPC, bool> _baseFarmerPassesThrough = new();" `
        'party collision state field'
}

$prepare = @'
    public void PrepareForParty(NPC npc)
    {
        RememberBaseSpeed(npc);
        EnableFarmerPassThrough(npc);
        npc.followSchedule = false;
        npc.ignoreScheduleToday = true;
    }
'@
$follow = Replace-Method $follow 'PrepareForParty' 'TakePartyControl' $prepare

$release = @'
    public void ReleaseToVanilla(NPC npc)
    {
        _releasedCharacters.Add(npc.Name);
        _combatControlled.Remove(npc.Name);
        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: false);
        RestoreFarmerPassThrough(npc);
        npc.Halt();
        npc.followSchedule = true;
        npc.ignoreScheduleToday = false;
    }
'@
$follow = Replace-Method $follow 'ReleaseToVanilla' 'ReleaseToVanillaAndResumeSchedule' $release

if (-not $follow.Contains('private void EnableFarmerPassThrough(NPC npc)')) {
$ghostMethods = @'
    private void EnableFarmerPassThrough(NPC npc)
    {
        if (!_baseFarmerPassesThrough.ContainsKey(npc))
            _baseFarmerPassesThrough[npc] = npc.farmerPassesThrough;

        npc.farmerPassesThrough = true;
    }

    private void RestoreFarmerPassThrough(NPC npc)
    {
        if (!_baseFarmerPassesThrough.TryGetValue(npc, out bool original))
            return;

        npc.farmerPassesThrough = original;
        _baseFarmerPassesThrough.Remove(npc);
    }

'@
    $needle = '    private void RememberBaseSpeed(NPC npc)'
    if (-not $follow.Contains($needle)) {
        throw 'Alpha 6.1.2 party ghosting fix could not locate RememberBaseSpeed insertion point.'
    }
    $follow = $follow.Replace($needle, $ghostMethods + $needle)
}

[System.IO.File]::WriteAllText($followPath, $follow, $utf8NoBom)
Write-Host 'Alpha 6.1.2 party ghosting applied: Farmer can pass through active Team Up members; vanilla collision restores on release.'
