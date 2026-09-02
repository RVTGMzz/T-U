$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if ($text.Contains($old)) { return $text.Replace($old, $new) }
    throw "v0.2-alpha.1 patch failed: expected source block not found: $label"
}

function Replace-RegexRequired([string]$text, [string]$pattern, [string]$replacement, [string]$alreadyPattern, [string]$label) {
    $options = [System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::Singleline
    if ([regex]::IsMatch($text, $alreadyPattern, $options)) { return $text }
    if (-not [regex]::IsMatch($text, $pattern, $options)) {
        throw "v0.2-alpha.1 patch failed: expected source pattern not found: $label"
    }
    return [regex]::Replace($text, $pattern, $replacement, $options, [timespan]::FromSeconds(2))
}

# -----------------------------------------------------------------------------
# ModEntry: wire real combat into the main update loop.
# -----------------------------------------------------------------------------
$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$mod = Get-Content $modPath -Raw

$mod = Replace-Required $mod `
    'using Ronvotri.TeamUp.Core;' `
    "using Ronvotri.TeamUp.Combat;`r`nusing Ronvotri.TeamUp.Core;" `
    'combat using'

$mod = Replace-Required $mod `
    'private FollowService Follow { get; set; } = null!;' `
    "private FollowService Follow { get; set; } = null!;`r`n    private CombatService Combat { get; set; } = null!;" `
    'combat field'

$mod = Replace-Required $mod `
    'Follow = new FollowService(Monitor);' `
    "Follow = new FollowService(Monitor);`r`n        Combat = new CombatService(Monitor, Follow);" `
    'combat construction'

$mod = Replace-Required $mod `
    'Team Up! v0.1.0-alpha.5.3.7 native vault + special lifecycle loaded.' `
    'Team Up! v0.2.0-alpha.1 full vanilla Codex + real NPC combat loaded.' `
    'version log'

# Alpha.5.3.7 inserts special-companion migration immediately after Party.Load.
# Anchor only to Party.Load itself so carried-forward lifecycle additions cannot
# break combat initialization again.
$mod = Replace-RegexRequired $mod `
    'Party\.Load\(saveData\);' `
    "Party.Load(saveData);`r`n        Combat.Clear();" `
    'Party\.Load\(saveData\);\s*Combat\.Clear\(\);' `
    'clear combat on save load'

$mod = Replace-RegexRequired $mod `
    '(private void OnDayEnding\(object\? sender, DayEndingEventArgs e\).*?long recruiterId = Game1\.player\.UniqueMultiplayerID;)(\s*)(Follow\.ReleaseAll\(Party\.Members, Party\.CompanionUnits, recruiterId\);)' `
    '$1$2Combat.Clear();$2$3' `
    'private void OnDayEnding\(object\? sender, DayEndingEventArgs e\).*?Combat\.Clear\(\);\s*Follow\.ReleaseAll\(Party\.Members, Party\.CompanionUnits, recruiterId\);' `
    'clear combat on day ending'

$mod = Replace-RegexRequired $mod `
    '(private void OnReturnedToTitle\(object\? sender, ReturnedToTitleEventArgs e\).*?SocialCodexButtonBounds = Rectangle\.Empty;)(\s*)(Party\.Clear\(\);)' `
    '$1$2Combat.Clear();$2$3' `
    'private void OnReturnedToTitle\(object\? sender, ReturnedToTitleEventArgs e\).*?Combat\.Clear\(\);\s*Party\.Clear\(\);' `
    'clear combat on title'

$oldUpdateTail = @'
        if (!e.IsMultipleOf(2))
            return;

        Follow.Update(
            Party.Members,
            Party.CompanionUnits,
            Game1.player.UniqueMultiplayerID);
'@
$newUpdateTail = @'
        // Combat runs every update tick so attack cooldowns and target movement feel
        // responsive. Follow still runs every two ticks and skips combat-controlled NPCs.
        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);

        if (!e.IsMultipleOf(2))
            return;

        Follow.Update(
            Party.Members,
            Party.CompanionUnits,
            Game1.player.UniqueMultiplayerID);
'@
$mod = Replace-Required $mod $oldUpdateTail $newUpdateTail 'combat update loop'

Set-Content -Path $modPath -Value $mod -Encoding UTF8

# -----------------------------------------------------------------------------
# FollowService: combat and formation movement must never fight over npc.controller.
# -----------------------------------------------------------------------------
$followPath = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$follow = Get-Content $followPath -Raw

$follow = Replace-Required $follow `
    'private readonly HashSet<string> _releasedCharacters = new(StringComparer.OrdinalIgnoreCase);' `
    "private readonly HashSet<string> _releasedCharacters = new(StringComparer.OrdinalIgnoreCase);`r`n    private readonly HashSet<string> _combatControlled = new(StringComparer.OrdinalIgnoreCase);" `
    'combat control set'

$follow = Replace-Required $follow `
    "_releasedCharacters.Remove(npc.Name);`r`n        PrepareForParty(npc);" `
    "_releasedCharacters.Remove(npc.Name);`r`n        _combatControlled.Remove(npc.Name);`r`n        PrepareForParty(npc);" `
    'take party control resets combat flag'

$follow = Replace-Required $follow `
    "_releasedCharacters.Add(npc.Name);`r`n        ClearPath(npc);" `
    "_releasedCharacters.Add(npc.Name);`r`n        _combatControlled.Remove(npc.Name);`r`n        ClearPath(npc);" `
    'release clears combat flag'

$combatMethod = @'
    public void SetCombatControl(NPC npc, bool active)
    {
        if (active)
        {
            _releasedCharacters.Remove(npc.Name);
            if (!_combatControlled.Add(npc.Name))
                return;

            PrepareForParty(npc);
            ClearPath(npc);
            npc.Halt();
            return;
        }

        if (!_combatControlled.Remove(npc.Name))
            return;

        ClearPath(npc);
        RestoreBaseSpeed(npc, keepTracked: true);
        npc.Halt();
    }

'@
$follow = Replace-RegexRequired $follow `
    '    public void HoldPosition\(NPC npc\)' `
    ($combatMethod + '    public void HoldPosition(NPC npc)') `
    'public void SetCombatControl\(NPC npc, bool active\)' `
    'combat control method'

$follow = Replace-RegexRequired $follow `
    'PartyMemberData member = ownedMembers\[index\];\s*if \(_releasedCharacters\.Contains\(member\.CharacterName\)\)\s*continue;' `
    "PartyMemberData member = ownedMembers[index];`r`n            if (_releasedCharacters.Contains(member.CharacterName)`r`n                || _combatControlled.Contains(member.CharacterName))`r`n            {`r`n                continue;`r`n            }" `
    '_combatControlled\.Contains\(member\.CharacterName\)' `
    'skip formation while fighting'

Set-Content -Path $followPath -Value $follow -Encoding UTF8

# -----------------------------------------------------------------------------
# CombatService: recovery must not keep a dead/stale target engaged by itself.
# -----------------------------------------------------------------------------
$combatPath = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$combat = Get-Content $combatPath -Raw
$oldCombatFlow = @'
            if (TryPerformRecovery(npc, member, role, affinity, monsters.Count > 0))
                stillEngaged.Add(member.CharacterName);

            Monster? target = AcquireTarget(npc, member, role, monsters);
            if (target is null)
            {
                if (!stillEngaged.Contains(member.CharacterName))
                    Disengage(member.CharacterName, npc);
                continue;
            }

            stillEngaged.Add(member.CharacterName);
'@
$newCombatFlow = @'
            Monster? target = AcquireTarget(npc, member, role, monsters);
            if (target is null)
            {
                Disengage(member.CharacterName, npc);
                continue;
            }

            TryPerformRecovery(npc, member, role, affinity, combatPresent: true);
            stillEngaged.Add(member.CharacterName);
'@
$combat = Replace-Required $combat $oldCombatFlow $newCombatFlow 'recovery engagement lifecycle'
Set-Content -Path $combatPath -Value $combat -Encoding UTF8

Write-Host 'v0.2-alpha.1 combat integration patches applied.'
