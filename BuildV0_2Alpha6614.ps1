$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$captureSafety = Join-Path $root 'src\TeamUp\Core\PelipperCaptureSafetyService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$expansion = Join-Path $root 'src\TeamUp\Combat\ExpansionSkillService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6614'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.14_PELIPPER_CAPTURE_SAFETY_SYNC_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.14_PELIPPER_CAPTURE_SAFETY_SYNC_TEST.sha256.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_14_PELIPPER_CAPTURE_SAFETY_SYNC_VI.txt'
$version = '0.2.0-alpha.6.6.14'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Replace-Exact([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

try {
    foreach ($required in @($project,$manifest,$modEntry,$captureSafety,$combat,$expansion,$follow,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.14 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.14')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.14 Pelipper Capture Safety Sync loaded.')
    Write-Utf8 $modEntry $modText

    $combatText = Read-Lf $combat

    $oldMonsterList = @'
        List<Monster> monsters = FarmerContext.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            .ToList();
'@
    $newMonsterList = @'
        List<Monster> combatMonsters = FarmerContext.currentLocation.characters
            .OfType<Monster>()
            .Where(monster => monster.Health > 0)
            .Where(monster => !OptionalTestHostCompatibility.IsCardchaHarnessMonster(monster))
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            .ToList();

        // Capture-protected Pelipper targets stay in combat context for incoming damage,
        // guard/heal/support logic, but are removed from every offensive target list.
        List<Monster> monsters = combatMonsters
            .Where(monster => !PelipperCaptureSafetyService.IsProtected(monster))
            .ToList();
'@
    $combatText = Replace-Exact $combatText $oldMonsterList $newMonsterList 'combat/offensive monster split'

    $oldFrame = @'
        _threat.BeginFrame(monsters, validThreatActors);
        PulseAmbientThreat(activeMembers, monsters);
        ApplyTankGuardToFarmerDamage(activeMembers, monsters, validThreatActors);
        UpdateSurvivalStates(activeMembers, monsters, validThreatActors);
        _expansionSkills.Update(activeMembers, monsters);
'@
    $newFrame = @'
        _threat.BeginFrame(combatMonsters, validThreatActors);
        PulseAmbientThreat(activeMembers, monsters);
        ApplyTankGuardToFarmerDamage(activeMembers, combatMonsters, validThreatActors);
        UpdateSurvivalStates(activeMembers, combatMonsters, validThreatActors);
        _expansionSkills.Update(activeMembers, monsters);
'@
    $combatText = Replace-Exact $combatText $oldFrame $newFrame 'capture-safe frame lists'

    $oldRecovery = '            if (TryPerformRecovery(npc, member, role, affinity, activeMembers, monsters, validThreatActors))'
    $newRecovery = '            if (TryPerformRecovery(npc, member, role, affinity, activeMembers, combatMonsters, validThreatActors))'
    $combatText = Replace-Exact $combatText $oldRecovery $newRecovery 'healing keeps full combat context'

    $oldAttackGate = @'
            if (!attackReady)
                continue;

            PerformAttack(npc, target, member, role, affinity);
'@
    $newAttackGate = @'
            if (!attackReady)
                continue;

            // Another teammate may have pushed this target onto the capture threshold earlier in
            // the same frame. Re-check immediately before the weapon swing.
            if (PelipperCaptureSafetyService.IsProtected(target))
            {
                Disengage(member.CharacterName, npc);
                continue;
            }

            PerformAttack(npc, target, member, role, affinity);
'@
    $combatText = Replace-Exact $combatText $oldAttackGate $newAttackGate 'last-moment protected target guard'

    $oldPerformDamage = @'
        damage = (int)Math.Round(damage * (0.85f + affinity * 0.05f));
        damage = Math.Max(1, (int)Math.Round(damage * _progression.GetDamageMultiplier(member, role)));

        int healthBefore = target.Health;
        FarmerContext.currentLocation.damageMonster(
            target.GetBoundingBox(), damage, damage + 2, isBomb: false, knockback,
            100, 0.02f, 1.5f, triggerMonsterInvincibleTimer: false, FarmerContext);

        int dealt = Math.Max(0, healthBefore - Math.Max(0, target.Health));
        if (dealt > 0)
            _threat.AddThreat(target, member.CharacterName, GetAttackThreat(member, role, dealt));

        ApplyRoleCombatEffect(npc, target, member, role, affinity, dealt);
        if (target.Health > 0)
            TryTriggerAttackSignature(npc, target, member, role, affinity);
'@
    $newPerformDamage = @'
        damage = (int)Math.Round(damage * (0.85f + affinity * 0.05f));
        damage = Math.Max(1, (int)Math.Round(damage * _progression.GetDamageMultiplier(member, role)));

        bool captureLimited = PelipperCaptureSafetyService.TryGetDamageBudget(target, out int captureBudget);
        if (captureLimited)
        {
            if (captureBudget <= 0)
                return;
            damage = Math.Min(damage, captureBudget);
        }

        int healthBefore = target.Health;
        FarmerContext.currentLocation.damageMonster(
            target.GetBoundingBox(), damage, captureLimited ? damage : damage + 2, isBomb: false, knockback,
            100, captureLimited ? 0f : 0.02f, captureLimited ? 1f : 1.5f,
            triggerMonsterInvincibleTimer: false, FarmerContext);

        int dealt = Math.Max(0, healthBefore - Math.Max(0, target.Health));
        if (dealt > 0)
            _threat.AddThreat(target, member.CharacterName, GetAttackThreat(member, role, dealt));

        bool protectedAfterHit = PelipperCaptureSafetyService.IsProtected(target);
        if (!protectedAfterHit)
            ApplyRoleCombatEffect(npc, target, member, role, affinity, dealt);
        if (target.Health > 0 && !protectedAfterHit)
            TryTriggerAttackSignature(npc, target, member, role, affinity);
'@
    $combatText = Replace-Exact $combatText $oldPerformDamage $newPerformDamage 'main attack damage ceiling'

    $oldAbigail = @'
            foreach (Monster monster in GetLivingMonstersNear(target.Tile, 2.25f))
            {
                FarmerContext.currentLocation.damageMonster(monster.GetBoundingBox(), bonusDamage, bonusDamage + 2,
                    isBomb: false, 1.0f, 100, 0.02f, 1.5f, triggerMonsterInvincibleTimer: false, FarmerContext);
                _threat.AddThreat(monster, member.CharacterName, bonusDamage * 1.15f);
                SpawnBurst(FarmerContext.currentLocation, monster.Position, purple, 5, 28f);
            }
'@
    $newAbigail = @'
            foreach (Monster monster in GetLivingMonstersNear(target.Tile, 2.25f))
            {
                int appliedDamage = PelipperCaptureSafetyService.ClampDamage(monster, bonusDamage);
                if (appliedDamage <= 0)
                    continue;
                bool captureLimited = PelipperCaptureSafetyService.TryGetDamageBudget(monster, out _);
                FarmerContext.currentLocation.damageMonster(monster.GetBoundingBox(), appliedDamage, captureLimited ? appliedDamage : appliedDamage + 2,
                    isBomb: false, 1.0f, 100, captureLimited ? 0f : 0.02f, captureLimited ? 1f : 1.5f,
                    triggerMonsterInvincibleTimer: false, FarmerContext);
                _threat.AddThreat(monster, member.CharacterName, appliedDamage * 1.15f);
                SpawnBurst(FarmerContext.currentLocation, monster.Position, purple, 5, 28f);
            }
'@
    $combatText = Replace-Exact $combatText $oldAbigail $newAbigail 'Abigail signature damage ceiling'

    $oldAlex = @'
            foreach (Monster monster in GetLivingMonstersNear(FarmerContext.Tile, 2.75f))
            {
                FarmerContext.currentLocation.damageMonster(monster.GetBoundingBox(), guardDamage, guardDamage + 1,
                    isBomb: false, 2.4f, 100, 0f, 1.25f, triggerMonsterInvincibleTimer: false, FarmerContext);
                _threat.AddThreat(monster, member.CharacterName, 55f + guardDamage * 3f);
                SpawnBurst(FarmerContext.currentLocation, monster.Position, orange, 4, 30f);
            }
'@
    $newAlex = @'
            foreach (Monster monster in GetLivingMonstersNear(FarmerContext.Tile, 2.75f))
            {
                int appliedDamage = PelipperCaptureSafetyService.ClampDamage(monster, guardDamage);
                if (appliedDamage <= 0)
                    continue;
                bool captureLimited = PelipperCaptureSafetyService.TryGetDamageBudget(monster, out _);
                FarmerContext.currentLocation.damageMonster(monster.GetBoundingBox(), appliedDamage, captureLimited ? appliedDamage : appliedDamage + 1,
                    isBomb: false, 2.4f, 100, 0f, captureLimited ? 1f : 1.25f,
                    triggerMonsterInvincibleTimer: false, FarmerContext);
                _threat.AddThreat(monster, member.CharacterName, 55f + appliedDamage * 3f);
                SpawnBurst(FarmerContext.currentLocation, monster.Position, orange, 4, 30f);
            }
'@
    $combatText = Replace-Exact $combatText $oldAlex $newAlex 'Alex signature damage ceiling'

    $oldNearbyFilter = @'
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            .Where(monster => Vector2.Distance(monster.Tile, centerTile) <= radiusTiles)
'@
    $newNearbyFilter = @'
            .Where(monster => !PelipperTownCompatibilityService.ShouldExcludeFromTeamUpCombat(monster))
            .Where(monster => !PelipperCaptureSafetyService.IsProtected(monster))
            .Where(monster => Vector2.Distance(monster.Tile, centerTile) <= radiusTiles)
'@
    $combatText = Replace-Exact $combatText $oldNearbyFilter $newNearbyFilter 'attack signature protected filter'
    Write-Utf8 $combat $combatText

    $expansionText = Read-Lf $expansion
    $oldDamageTargets = @'
        foreach (Monster monster in targets.Where(monster => monster.Health > 0).ToList())
        {
            int before = monster.Health;
            Game1.currentLocation.damageMonster(monster.GetBoundingBox(), damage, damage + 2, isBomb: false, knockback * GetIdentityUtilityScale(member.CharacterName), 100, 0.02f, 1.25f, triggerMonsterInvincibleTimer: false, Game1.player);
            int dealt = Math.Max(0, before - Math.Max(0, monster.Health));
'@
    $newDamageTargets = @'
        foreach (Monster monster in targets.Where(monster => monster.Health > 0 && !PelipperCaptureSafetyService.IsProtected(monster)).ToList())
        {
            int appliedDamage = PelipperCaptureSafetyService.ClampDamage(monster, damage);
            if (appliedDamage <= 0)
                continue;
            bool captureLimited = PelipperCaptureSafetyService.TryGetDamageBudget(monster, out _);
            int before = monster.Health;
            Game1.currentLocation.damageMonster(monster.GetBoundingBox(), appliedDamage, captureLimited ? appliedDamage : appliedDamage + 2,
                isBomb: false, knockback * GetIdentityUtilityScale(member.CharacterName), 100,
                captureLimited ? 0f : 0.02f, captureLimited ? 1f : 1.25f,
                triggerMonsterInvincibleTimer: false, Game1.player);
            int dealt = Math.Max(0, before - Math.Max(0, monster.Health));
'@
    $expansionText = Replace-Exact $expansionText $oldDamageTargets $newDamageTargets 'expansion damage ceiling'

    $oldLivingNear = @'
        return monsters.Where(monster => monster.Health > 0).Where(monster => ReferenceEquals(monster.currentLocation, Game1.currentLocation))
            .Where(monster => Vector2.Distance(monster.Tile, centerTile) <= radius).ToList();
'@
    $newLivingNear = @'
        return monsters.Where(monster => monster.Health > 0 && !PelipperCaptureSafetyService.IsProtected(monster))
            .Where(monster => ReferenceEquals(monster.currentLocation, Game1.currentLocation))
            .Where(monster => Vector2.Distance(monster.Tile, centerTile) <= radius).ToList();
'@
    $expansionText = Replace-Exact $expansionText $oldLivingNear $newLivingNear 'expansion offensive protected filter'
    Write-Utf8 $expansion $expansionText

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $captureText = Read-Lf $captureSafety
    $combatText = Read-Lf $combat
    $expansionText = Read-Lf $expansion
    $followText = Read-Lf $follow

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.14</Version>')) { throw 'Version materialization failed.' }
    foreach ($token in @('build: v0.2.0-alpha.6.6.14','Pelipper Capture Safety Sync loaded.')) {
        if (-not $modText.Contains($token)) { throw "ModEntry 6.6.14 token missing: $token" }
    }
    foreach ($token in @('FallbackThreshold = 0.10f','TryGetDamageBudget','ClampDamage','CurrentThreshold','CurrentEnabled')) {
        if (-not $captureText.Contains($token)) { throw "Capture safety service token missing: $token" }
    }
    foreach ($token in @(
        'List<Monster> combatMonsters',
        'PelipperCaptureSafetyService.IsProtected(monster)',
        'TryPerformRecovery(npc, member, role, affinity, activeMembers, combatMonsters',
        'PelipperCaptureSafetyService.TryGetDamageBudget(target, out int captureBudget)',
        'captureLimited ? 0f : 0.02f',
        'protectedAfterHit'
    )) {
        if (-not $combatText.Contains($token)) { throw "Combat capture safety token missing: $token" }
    }
    foreach ($token in @('PelipperCaptureSafetyService.ClampDamage(monster, damage)','!PelipperCaptureSafetyService.IsProtected(monster)')) {
        if (-not $expansionText.Contains($token)) { throw "Expansion capture safety token missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService performance regression.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService performance regression.' }

    Log 'Building Alpha 6.6.14 Pelipper Capture Safety Sync...'
    Log 'CAPTURE: Team Up stops all offensive targeting at Pelipper low-HP capture threshold.'
    Log 'CEILING: Team Up damage is clamped so a hit cannot cross below the capture threshold.'
    Log 'SUPPORT: combat context remains active for guard/heal/support while offensive target list excludes protected Pokemon.'
    Log 'SOURCE: read-only Pelipper config reflection when discoverable; 10% compatibility fallback otherwise.'
    Log 'REGRESSION: 6.6.13 single-target combat, shared 2/2 quota, farewell, water/bridge performance and land safety preserved.'

    & dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
    & dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    $dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' | Select-Object -First 1
    if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

    if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
    if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
    New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
    Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force
    $manifestText = Read-Lf $manifest
    $manifestText = $manifestText.Replace('%ProjectVersion%', $version)
    Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
    Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.14 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $zipName`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.14'
    Log 'PELIPPER CAPTURE SAFETY SYNC: ENABLED'
    Log 'TEAM-WIDE OFFENSIVE STOP AT CAPTURE THRESHOLD: ENABLED'
    Log 'HEAL / SUPPORT DURING CAPTURE HOLD: ENABLED'
    Log 'CAPTURE DAMAGE CEILING: ENABLED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
