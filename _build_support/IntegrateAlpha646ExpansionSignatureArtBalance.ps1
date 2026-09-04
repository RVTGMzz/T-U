$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$projectPath = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$modEntryPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$expansionPath = Join-Path $root 'src\TeamUp\Combat\ExpansionSkillService.cs'
$tuningPath = Join-Path $root 'src\TeamUp\Combat\ExpansionSkillService.IdentityBalance.cs'
$rendererPath = Join-Path $root 'src\TeamUp\UI\TraitIconRenderer.cs'
$iconCatalogPath = Join-Path $root 'src\TeamUp\UI\ExpansionSignatureIconCatalog.cs'
$completionPath = Join-Path $root 'src\TeamUp\Core\ExpansionRosterCompletion.cs'
$version = '0.2.0-alpha.6.4.6'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing Alpha 6.4.6 source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}

function WriteText([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom)
}

# Version + startup marker.
$project = ReadText $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteText $projectPath $project

$mod = ReadText $modEntryPath
$mod = $mod.Replace('0.2.0-alpha.6.4.5', $version)
$mod = $mod.Replace('interaction + AI + full expansion roster loaded.', 'expansion signature art + zero-sum identity balance loaded.')
WriteText $modEntryPath $mod

# Apply identity budget axes to the actual expansion runtime.
$expansion = ReadText $expansionPath
if ($expansion -notmatch 'GetIdentityRadius\(SkillSpec spec') {
    $oldCooldown = '_cooldowns[member.CharacterName] = ScaleCooldown(member, role, spec.BaseCooldownTicks - (tier >= 3 ? 90 : 0));'
    $newCooldown = '_cooldowns[member.CharacterName] = ScaleCooldown(member, role, spec.BaseCooldownTicks - (tier >= 3 ? 90 : 0) + GetIdentityCooldownDelta(member.CharacterName));'
    if (-not $expansion.Contains($oldCooldown)) { throw 'Could not locate expansion cooldown integration point.' }
    $expansion = $expansion.Replace($oldCooldown, $newCooldown)

    # Reach axis. This intentionally changes every SkillSpec radius read in runtime methods,
    # while the helper itself is inserted after this global replacement.
    $expansion = $expansion.Replace('spec.Radius', 'GetIdentityRadius(spec, member.CharacterName)')

    # Utility axis strengthens or weakens control windows without affecting raw class power.
    $expansion = $expansion.Replace(
        '_progression.GetSignatureEffectMultiplier(member, role));',
        '_progression.GetSignatureEffectMultiplier(member, role) * GetIdentityUtilityScale(member.CharacterName));')

    $oldDamage = 'private int ScaleDamage(PartyMemberData member, PartyRole role, int raw) => Math.Max(1, (int)Math.Round(raw * _progression.GetDamageMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)));'
    $newDamage = 'private int ScaleDamage(PartyMemberData member, PartyRole role, int raw) => Math.Max(1, (int)Math.Round(raw * _progression.GetDamageMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityPowerScale(member.CharacterName)));'
    if (-not $expansion.Contains($oldDamage)) { throw 'Could not locate expansion ScaleDamage integration point.' }
    $expansion = $expansion.Replace($oldDamage, $newDamage)

    $oldHeal = 'private int ScaleHeal(PartyMemberData member, PartyRole role, int raw) => Math.Max(1, (int)Math.Round(raw * _progression.GetHealingMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)));'
    $newHeal = 'private int ScaleHeal(PartyMemberData member, PartyRole role, int raw) => Math.Max(1, (int)Math.Round(raw * _progression.GetHealingMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role) * GetIdentityPowerScale(member.CharacterName)));'
    if (-not $expansion.Contains($oldHeal)) { throw 'Could not locate expansion ScaleHeal integration point.' }
    $expansion = $expansion.Replace($oldHeal, $newHeal)

    # Knockback is part of utility identity. Keep it bounded to the same +/-12% envelope.
    $oldDamageMonster = 'Game1.currentLocation.damageMonster(monster.GetBoundingBox(), damage, damage + 2, isBomb: false, knockback, 100, 0.02f, 1.25f, triggerMonsterInvincibleTimer: false, Game1.player);'
    $newDamageMonster = 'Game1.currentLocation.damageMonster(monster.GetBoundingBox(), damage, damage + 2, isBomb: false, knockback * GetIdentityUtilityScale(member.CharacterName), 100, 0.02f, 1.25f, triggerMonsterInvincibleTimer: false, Game1.player);'
    if (-not $expansion.Contains($oldDamageMonster)) { throw 'Could not locate expansion knockback integration point.' }
    $expansion = $expansion.Replace($oldDamageMonster, $newDamageMonster)

    $helperMarker = '    private static List<Monster> LivingNear(Vector2 centerTile, float radius, IReadOnlyList<Monster> monsters)'
    $helper = @'
    private static float GetIdentityRadius(SkillSpec spec, string characterName)
        => Math.Max(0.8f, spec.Radius + GetIdentityRadiusBonus(characterName));

'@
    if (-not $expansion.Contains($helperMarker)) { throw 'Could not locate expansion radius helper insertion point.' }
    $expansion = $expansion.Replace($helperMarker, $helper + $helperMarker)
}
WriteText $expansionPath $expansion

# Route Signature rendering through the hand-authored expansion motif catalog before procedural fallback.
$renderer = ReadText $rendererPath
if ($renderer -notmatch 'ExpansionSignatureIconCatalog\.TryGetPattern') {
$oldHas = @'
    public static bool HasBespokeSignature(string characterName)
    {
        return BespokeSignaturePatterns.ContainsKey(characterName);
    }
'@
$newHas = @'
    public static bool HasBespokeSignature(string characterName)
    {
        return BespokeSignaturePatterns.ContainsKey(characterName)
            || ExpansionSignatureIconCatalog.Has(characterName);
    }
'@
    if (-not $renderer.Contains($oldHas)) { throw 'Could not locate HasBespokeSignature integration point.' }
    $renderer = $renderer.Replace($oldHas, $newHas)

$oldPattern = @'
        bool[,] pattern = kind == TraitIconKind.Signature && BespokeSignaturePatterns.TryGetValue(characterName, out bool[,]? bespoke)
            ? bespoke
            : BuildProceduralPattern(characterName, kind);
'@
$newPattern = @'
        bool[,] pattern = kind == TraitIconKind.Signature && TryGetBespokeSignaturePattern(characterName, out bool[,] bespoke)
            ? bespoke
            : BuildProceduralPattern(characterName, kind);
'@
    if (-not $renderer.Contains($oldPattern)) { throw 'Could not locate TraitIconRenderer pattern selection.' }
    $renderer = $renderer.Replace($oldPattern, $newPattern)
    $renderer = $renderer.Replace(
        'if (kind == TraitIconKind.Signature && BespokeSignaturePatterns.ContainsKey(characterName))',
        'if (kind == TraitIconKind.Signature && HasBespokeSignature(characterName))')

    $methodMarker = '    private static IReadOnlyDictionary<string, bool[,]> BuildBespokeSignaturePatterns()'
$helper = @'
    private static bool TryGetBespokeSignaturePattern(string characterName, out bool[,] pattern)
    {
        if (BespokeSignaturePatterns.TryGetValue(characterName, out bool[,]? existing) && existing is not null)
        {
            pattern = existing;
            return true;
        }

        return ExpansionSignatureIconCatalog.TryGetPattern(characterName, out pattern);
    }

'@
    if (-not $renderer.Contains($methodMarker)) { throw 'Could not locate TraitIconRenderer helper insertion point.' }
    $renderer = $renderer.Replace($methodMarker, $helper + $methodMarker)
}
WriteText $rendererPath $renderer

# Source-level acceptance before dotnet compilation.
$project = ReadText $projectPath
$mod = ReadText $modEntryPath
$expansion = ReadText $expansionPath
$tuning = ReadText $tuningPath
$renderer = ReadText $rendererPath
$icons = ReadText $iconCatalogPath
$completion = ReadText $completionPath

if ($project -notmatch '<Version>0\.2\.0-alpha\.6\.4\.6</Version>') { throw 'Alpha 6.4.6 version was not materialized.' }
if ($mod -notmatch 'build: v0\.2\.0-alpha\.6\.4\.6') { throw 'Alpha 6.4.6 debug marker missing.' }
if ($expansion -notmatch 'GetIdentityPowerScale\(member\.CharacterName\)') { throw 'Expansion identity power axis not wired.' }
if ($expansion -notmatch 'GetIdentityRadius\(spec, member\.CharacterName\)') { throw 'Expansion identity reach axis not wired.' }
if ($expansion -notmatch 'GetIdentityUtilityScale\(member\.CharacterName\)') { throw 'Expansion identity utility axis not wired.' }
if ($expansion -notmatch 'GetIdentityCooldownDelta\(member\.CharacterName\)') { throw 'Expansion identity tempo axis not wired.' }
if ($tuning -notmatch 'IdentityTunings' -or $tuning -notmatch 'tuning.Total != 0') { throw 'Zero-sum identity budget catalog missing.' }
if ($icons -notmatch 'ExpansionSignatureIconCatalog' -or $icons -notmatch '\["Ariah"\]' -or $icons -notmatch '\["Zayne"\]') { throw 'Expansion bespoke icon catalog incomplete.' }
if ($renderer -notmatch 'ExpansionSignatureIconCatalog\.TryGetPattern') { throw 'Expansion bespoke icons are not wired into TraitIconRenderer.' }

Write-Host 'Alpha 6.4.6 Expansion Signature Art + Balance integrated.'
Write-Host 'All 51 formerly-placeholder expansion NPCs keep their real signatures from 6.4.5 and now gain zero-sum per-character tuning plus bespoke semantic icon motifs.'
Write-Host 'Balance rule: every character Strength axis is paid for by Weakness axis; each axis remains within -2..+2.'
