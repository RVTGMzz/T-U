$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$version = '0.2.0-alpha.6.4.1'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

$projectPath = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$modPath = Join-Path $root 'src\TeamUp\ModEntry.cs'
$progressionPath = Join-Path $root 'src\TeamUp\Core\ProgressionService.cs'
$relationshipPath = Join-Path $root 'src\TeamUp\Core\RelationshipBondService.cs'
$combatPath = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$identityPath = Join-Path $root 'src\TeamUp\Combat\CharacterSkillIdentityService.cs'
$expansionPath = Join-Path $root 'src\TeamUp\Combat\ExpansionSkillService.cs'
$alpha6Path = Join-Path $root 'src\TeamUp\Combat\Alpha6CombatPolishService.cs'
$profilePath = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$defaultI18nPath = Join-Path $root 'src\TeamUp\i18n\default.json'
$viI18nPath = Join-Path $root 'src\TeamUp\i18n\vi.json'

function ReadText([string]$path) {
    if (-not (Test-Path $path)) { throw "Missing Alpha 6.4.1 source file: $path" }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function WriteText([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text.Replace("`r`n", "`n"), $utf8NoBom)
}
function ReplaceRequired([string]$text, [string]$needle, [string]$replacement, [string]$label) {
    if (-not $text.Contains($needle)) { throw "Could not locate $label" }
    return $text.Replace($needle, $replacement)
}
function PatchSignatureMultipliers([string]$text) {
    $text = $text.Replace('_progression.GetDamageMultiplier(member, role)', '_progression.GetDamageMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)')
    $text = $text.Replace('_progression.GetHealingMultiplier(member, role)', '_progression.GetHealingMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)')
    $text = $text.Replace('_progression.GetControlMultiplier(member, role)', '_progression.GetControlMultiplier(member, role) * _progression.GetSignatureEffectMultiplier(member, role)')
    return $text
}

# Version.
$project = ReadText $projectPath
$project = [regex]::Replace($project, '<Version>[^<]+</Version>', "<Version>$version</Version>", 1)
WriteText $projectPath $project

# Relationship service needs XNA Vector2 for pressure checks.
$relationship = ReadText $relationshipPath
if ($relationship -notmatch 'using Microsoft\.Xna\.Framework;') {
    $relationship = "using Microsoft.Xna.Framework;`n" + $relationship
}
WriteText $relationshipPath $relationship

# New vanilla identity runtime: 10-heart Signature Affinity applies to signature output/buff duration only.
$identity = ReadText $identityPath
if ($identity -notmatch 'GetSignatureEffectMultiplier\(member, role\)') {
    $identity = PatchSignatureMultipliers $identity
}
if ($identity -notmatch 'signatureAffinity = _progression\.GetSignatureEffectMultiplier\(owner') {
    $needle = '        int duration = tier >= 3 ? (int)Math.Round(identity.BuffDurationTicks * 1.25f) : identity.BuffDurationTicks;'
    $replacement = "        float signatureAffinity = _progression.GetSignatureEffectMultiplier(owner, ResolveRole(owner));`n        int duration = tier >= 3`n            ? (int)Math.Round(identity.BuffDurationTicks * 1.25f * signatureAffinity)`n            : (int)Math.Round(identity.BuffDurationTicks * signatureAffinity);"
    $identity = ReplaceRequired $identity $needle $replacement 'CharacterSkillIdentityService buff duration'
}
$identity = $identity.Replace('Alpha 6.4.0 runtime for the remaining vanilla NPC signature identities.', 'Alpha 6.4.1 runtime for vanilla NPC signature identities with relationship affinity.')
WriteText $identityPath $identity

# Expansion signatures are entirely signature runtime, so the same 10-heart effect multiplier is safe here.
$expansion = ReadText $expansionPath
if ($expansion -notmatch 'GetSignatureEffectMultiplier\(member, role\)') {
    $expansion = PatchSignatureMultipliers $expansion
}
WriteText $expansionPath $expansion

# Alpha 6 prototype Tier 2/3 service is also signature-only/rescue polish.
$alpha6 = ReadText $alpha6Path
if ($alpha6 -notmatch 'GetSignatureEffectMultiplier\(member, role\)') {
    $alpha6 = PatchSignatureMultipliers $alpha6
}
WriteText $alpha6Path $alpha6

# Base CombatService: 8-heart recovery timing + 10-heart affinity for inherited Tier 1 signature blocks.
$combat = ReadText $combatPath
if ($combat -notmatch 'GetRecoveryThresholdAdjustment\(member, role\)') {
    $needle = "        if (farmerPressure >= 2)`n            farmerThreshold = Math.Min(0.90f, farmerThreshold + 0.10f);"
    $replacement = "        if (farmerPressure >= 2)`n            farmerThreshold = Math.Min(0.90f, farmerThreshold + 0.10f);`n        farmerThreshold = Math.Clamp(`n            farmerThreshold + _progression.GetRecoveryThresholdAdjustment(member, role),`n            0.35f,`n            0.95f);"
    $combat = ReplaceRequired $combat $needle $replacement 'CombatService relationship recovery threshold'
}

# Only patch the legacy signature methods, never generic basic attacks/heals.
$recoveryStart = $combat.IndexOf('    private void TryTriggerRecoverySignature(')
$attackStart = $combat.IndexOf('    private void TryTriggerAttackSignature(')
$nextAfterAttack = $combat.IndexOf('    private ', $attackStart + 10)
if ($recoveryStart -lt 0 -or $attackStart -lt 0) { throw 'Could not locate legacy signature methods.' }
if ($nextAfterAttack -lt 0) { $nextAfterAttack = $combat.Length }
$beforeRecovery = $combat.Substring(0, $recoveryStart)
$recoveryBlock = $combat.Substring($recoveryStart, $attackStart - $recoveryStart)
$attackBlock = $combat.Substring($attackStart, $nextAfterAttack - $attackStart)
$afterAttack = $combat.Substring($nextAfterAttack)
if ($recoveryBlock -notmatch 'GetSignatureEffectMultiplier') { $recoveryBlock = PatchSignatureMultipliers $recoveryBlock }
if ($attackBlock -notmatch 'GetSignatureEffectMultiplier') { $attackBlock = PatchSignatureMultipliers $attackBlock }
$combat = $beforeRecovery + $recoveryBlock + $attackBlock + $afterAttack
WriteText $combatPath $combat

# Wire relationship service into lifecycle/update and Character Profile.
$mod = ReadText $modPath
$mod = $mod.Replace('0.2.0-alpha.6.4.0', $version)
$mod = $mod.Replace('Team Up! v0.2.0-alpha.6.4.1 character skill identity loaded.', 'Team Up! v0.2.0-alpha.6.4.1 friendship & bond loaded.')
if ($mod -notmatch 'RelationshipBondService Relationships') {
    $needle = "    private ProgressionService Progression { get; set; } = null!;`n    private EquipmentService Equipment { get; set; } = null!;"
    $replacement = "    private ProgressionService Progression { get; set; } = null!;`n    private RelationshipBondService Relationships { get; set; } = null!;`n    private EquipmentService Equipment { get; set; } = null!;"
    $mod = ReplaceRequired $mod $needle $replacement 'ModEntry relationship field'
}
if ($mod -notmatch 'Relationships = new RelationshipBondService') {
    $needle = "        Progression = new ProgressionService();`n        Equipment = new EquipmentService();"
    $replacement = "        Progression = new ProgressionService();`n        Relationships = new RelationshipBondService(Progression);`n        Equipment = new EquipmentService();"
    $mod = ReplaceRequired $mod $needle $replacement 'ModEntry relationship construction'
}
if ($mod -notmatch 'Relationships\.Update\(Party\.Members') {
    $needle = "        SkillIdentity.Update(Party.Members, Game1.player.UniqueMultiplayerID);`n        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);"
    $replacement = "        SkillIdentity.Update(Party.Members, Game1.player.UniqueMultiplayerID);`n        Relationships.Update(Party.Members, Game1.player.UniqueMultiplayerID);`n        Combat.Update(Party.Members, Game1.player.UniqueMultiplayerID);"
    $mod = ReplaceRequired $mod $needle $replacement 'ModEntry relationship update'
}
# Clear relationship runtime credits/cooldowns whenever Alpha6Polish is cleared in lifecycle methods.
if ($mod -notmatch 'Alpha6Polish\.Clear\(\);\n        Relationships\.Clear\(\);') {
    $mod = $mod.Replace("        Alpha6Polish.Clear();`n", "        Alpha6Polish.Clear();`n        Relationships.Clear();`n")
}

if ($mod -notmatch 'RelationshipBondState bond = Relationships\.GetState') {
    $needle = "        string signature = profile is null ? string.Empty : Helper.Translation.Get(profile.AbilityKey);`n`n        PartyMemberData? progressionMember"
    $replacement = @'
        string signature = profile is null ? string.Empty : Helper.Translation.Get(profile.AbilityKey);
        RelationshipBondState bond = Relationships.GetState(characterName);
        string relationship = Helper.Translation.Get("relationship.summary", new
        {
            hearts = bond.Hearts,
            stage = Helper.Translation.Get(Relationships.GetStageKey(bond.Stage))
        });
        if (bond.IsSpouse)
            relationship += "\n" + Helper.Translation.Get("relationship.spouse-bond");
        if (bond.Stage == RelationshipBondStage.Soulmate && !string.IsNullOrWhiteSpace(bond.SoulmateTraitName))
            relationship += "\n" + Helper.Translation.Get("relationship.soulmate", new { trait = bond.SoulmateTraitName });

        PartyMemberData? progressionMember
'@
    $mod = ReplaceRequired $mod $needle $replacement 'profile relationship summary'
}
$oldCtor = '            characterName, profile, displayName, status, source, engagement, passive, signature,'
$newCtor = '            characterName, profile, displayName, status, source, engagement, passive, signature, relationship,'
if ($mod.Contains($oldCtor)) { $mod = $mod.Replace($oldCtor, $newCtor) }
WriteText $modPath $mod

# Character profile gets a compact Relationship/Bond block in the left identity panel.
$profile = ReadText $profilePath
$profile = $profile.Replace('Alpha 6.3.2 keeps the passive readable as text and reserves the single character icon', 'Alpha 6.4.1 keeps the passive readable as text, adds relationship status, and reserves the single character icon')
if ($profile -notmatch 'private readonly string _relationshipText;') {
    $needle = "    private readonly string _signatureText;`n    private readonly Action _onBack;"
    $replacement = "    private readonly string _signatureText;`n    private readonly string _relationshipText;`n    private readonly Action _onBack;"
    $profile = ReplaceRequired $profile $needle $replacement 'CharacterProfile relationship field'
}
if ($profile -notmatch 'string relationshipText,') {
    $needle = "        string passiveText,`n        string signatureText,`n        Func<PartyRole, string> roleLabel,"
    $replacement = "        string passiveText,`n        string signatureText,`n        string relationshipText,`n        Func<PartyRole, string> roleLabel,"
    $profile = ReplaceRequired $profile $needle $replacement 'CharacterProfile relationship constructor parameter'
}
if ($profile -notmatch '_relationshipText = relationshipText;') {
    $needle = "        _signatureText = signatureText;`n        _roleLabel = roleLabel;"
    $replacement = "        _signatureText = signatureText;`n        _relationshipText = relationshipText;`n        _roleLabel = roleLabel;"
    $profile = ReplaceRequired $profile $needle $replacement 'CharacterProfile relationship assignment'
}
if ($profile -notmatch 'profile.relationship') {
    $needle = "        string wrapped = WrapScaled(_engagementLabel, panelWidth - SectionPadding * 2, BodyScale);`n        DrawScaledString(b, Game1.smallFont, wrapped, new Vector2(x + SectionPadding, cursorY), Game1.textColor, BodyScale);"
    $replacement = @'
        string wrapped = WrapScaled(_engagementLabel, panelWidth - SectionPadding * 2, BodyScale);
        DrawScaledString(b, Game1.smallFont, wrapped, new Vector2(x + SectionPadding, cursorY), Game1.textColor, BodyScale);
        cursorY += (int)(Game1.smallFont.MeasureString(wrapped).Y * BodyScale) + 14;

        DrawScaledString(b, Game1.smallFont, _i18n.Get("profile.relationship"), new Vector2(x + SectionPadding, cursorY), new Color(112, 73, 44), CaptionScale);
        cursorY += (int)(Game1.smallFont.LineSpacing * CaptionScale) + 2;
        string relationship = WrapScaled(_relationshipText, panelWidth - SectionPadding * 2, 1.02f);
        DrawScaledString(b, Game1.smallFont, relationship, new Vector2(x + SectionPadding, cursorY), Game1.textColor, 1.02f);
'@
    $profile = ReplaceRequired $profile $needle $replacement 'CharacterProfile relationship draw block'
}
WriteText $profilePath $profile

# i18n EN/VI. Insert before combat keys so JSON remains simple and portable across PS 5.1/7.
$default = ReadText $defaultI18nPath
if ($default -notmatch '"profile.relationship"') {
$keys = @'
  "profile.relationship": "RELATIONSHIP",
  "relationship.summary": "{{hearts}} hearts · {{stage}}",
  "relationship.stage.acquaintance": "Acquaintance",
  "relationship.stage.trusted": "Trusted",
  "relationship.stage.close": "Close Companion",
  "relationship.stage.max-friendship": "Max Friendship",
  "relationship.stage.spouse": "Spouse Bond",
  "relationship.stage.soulmate": "Soulmate",
  "relationship.spouse-bond": "Bond Trait active while fighting together.",
  "relationship.soulmate": "Soulmate Trait: {{trait}}",
'@
    $marker = '  "combat.enter":'
    $default = ReplaceRequired $default $marker ($keys + $marker) 'default i18n relationship marker'
}
WriteText $defaultI18nPath $default

$vi = ReadText $viI18nPath
if ($vi -notmatch '"profile.relationship"') {
$keys = @'
  "profile.relationship": "MỐI QUAN HỆ",
  "relationship.summary": "{{hearts}} tim · {{stage}}",
  "relationship.stage.acquaintance": "Quen biết",
  "relationship.stage.trusted": "Tin cậy",
  "relationship.stage.close": "Đồng hành thân thiết",
  "relationship.stage.max-friendship": "Tình bạn tối đa",
  "relationship.stage.spouse": "Gắn kết vợ/chồng",
  "relationship.stage.soulmate": "Tri kỷ",
  "relationship.spouse-bond": "Bond Trait được kích hoạt khi cùng chiến đấu.",
  "relationship.soulmate": "Soulmate Trait: {{trait}}",
'@
    $marker = '  "combat.enter":'
    $vi = ReplaceRequired $vi $marker ($keys + $marker) 'Vietnamese i18n relationship marker'
}
WriteText $viI18nPath $vi

# Source acceptance.
$project = ReadText $projectPath
$mod = ReadText $modPath
$progression = ReadText $progressionPath
$relationship = ReadText $relationshipPath
$combat = ReadText $combatPath
$identity = ReadText $identityPath
$expansion = ReadText $expansionPath
$alpha6 = ReadText $alpha6Path
$profile = ReadText $profilePath
$default = ReadText $defaultI18nPath
$vi = ReadText $viI18nPath

if ($project -notmatch '<Version>0\.2\.0-alpha\.6\.4\.1</Version>') { throw 'Alpha 6.4.1 version missing.' }
if ($mod -notmatch 'RelationshipBondService Relationships') { throw 'Relationship service not wired.' }
if ($mod -notmatch 'Relationships\.Update\(Party\.Members') { throw 'Relationship update missing.' }
if ($mod -notmatch 'build: v0\.2\.0-alpha\.6\.4\.1') { throw 'Alpha 6.4.1 debug marker missing.' }
if ($relationship -notmatch 'TrustedHearts = 4' -or $relationship -notmatch 'CloseCompanionHearts = 8' -or $relationship -notmatch 'MaxFriendshipHearts = 10' -or $relationship -notmatch 'SoulmateHearts = 14') { throw 'Friendship heart gates are incomplete.' }
if ($relationship -notmatch '\["Harvey"\].*I Won''t Lose You' -or $relationship -notmatch '\["Alex"\].*Always By Your Side') { throw 'Vanilla soulmate traits are incomplete.' }
if ($progression -notmatch 'ConfigureRelationshipHooks' -or $progression -notmatch 'GetSignatureEffectMultiplier') { throw 'Progression relationship hooks missing.' }
if ($combat -notmatch 'GetRecoveryThresholdAdjustment') { throw '8-heart recovery AI synergy missing.' }
if ($identity -notmatch 'GetSignatureEffectMultiplier' -or $expansion -notmatch 'GetSignatureEffectMultiplier' -or $alpha6 -notmatch 'GetSignatureEffectMultiplier') { throw '10-heart Signature Affinity missing from signature runtimes.' }
if ($profile -notmatch 'profile.relationship' -or $mod -notmatch 'RelationshipBondState bond') { throw 'Relationship profile UI missing.' }
if ($default -notmatch 'relationship.stage.soulmate' -or $vi -notmatch 'relationship.stage.soulmate') { throw 'Relationship i18n missing.' }

Write-Host 'Alpha 6.4.1 Friendship & Bond integrated.'
Write-Host '4 hearts Trusted: +10% progression via fractional credit; 8 hearts: role-aware AI timing; 10 hearts: +5% Signature Affinity.'
Write-Host 'Spouse Bond is conditional and role-aware; 14-heart Soulmate uses bespoke vanilla traits with safe role fallback for expansion spouses.'
