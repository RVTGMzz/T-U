$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$deployment = Join-Path $root 'src\TeamUp\Core\PelipperDeploymentStateService.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$alpha669 = Join-Path $root 'src\TeamUp\ModEntry.Alpha669.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$tileSafety = Join-Path $root 'src\TeamUp\Core\PartyTileSafety.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6611'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.11_NO_COMPANION_PROFILES_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.11_NO_COMPANION_PROFILES_HOTFIX_TEST.sha256.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_11_NO_COMPANION_PROFILES_VI.txt'
$version = '0.2.0-alpha.6.6.11'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

if (Test-Path $log) { Remove-Item $log -Force }
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }
function Read-Lf([string]$path) { return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n") }
function Write-Utf8([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8NoBom) }
function Replace-Required([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Patch anchor missing: $label" }
    return $text.Replace($old, $new)
}

try {
    foreach ($required in @($project,$manifest,$modEntry,$follow,$deployment,$alpha663,$alpha669,$combat,$tileSafety,$smoke)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.11 input: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.11')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.11 No Companion Profiles Hotfix loaded.')

    $oldProfileKey = @'
            if (Config.ProfileKey.JustPressed())
            {
                Helper.Input.Suppress(e.Button);
                OpenProfileFromDialogue(speaker);
                return;
            }
'@
    $newProfileKey = @'
            if (Config.ProfileKey.JustPressed())
            {
                if (!CanOpenDirectProfile(speaker))
                    return;

                Helper.Input.Suppress(e.Button);
                OpenProfileFromDialogue(speaker);
                return;
            }
'@
    $modText = Replace-Required $modText $oldProfileKey $newProfileKey 'dialogue profile input eligibility guard'

    $oldOpenProfile = @'
    private void OpenProfileFromDialogue(NPC npc)
    {
        IClickableMenu? dialogueMenu = Game1.activeClickableMenu;
'@
    $newOpenProfile = @'
    private void OpenProfileFromDialogue(NPC npc)
    {
        if (!CanOpenDirectProfile(npc))
            return;

        IClickableMenu? dialogueMenu = Game1.activeClickableMenu;
'@
    $modText = Replace-Required $modText $oldOpenProfile $newOpenProfile 'OpenProfileFromDialogue eligibility guard'

    $oldDrawActions = @'
    private void DrawDialogueActions(RenderedActiveMenuEventArgs e, DialogueBox dialogueBox, NPC speaker)
    {
        long recruiterId = Game1.player.UniqueMultiplayerID;
'@
    $newDrawActions = @'
    private void DrawDialogueActions(RenderedActiveMenuEventArgs e, DialogueBox dialogueBox, NPC speaker)
    {
        // Alpha 6.6.11: creature/summon actors do not own Team Up character profiles.
        // Do not advertise profile/recruit tags over source-owned companion dialogue.
        if (!CanOpenDirectProfile(speaker))
            return;

        long recruiterId = Game1.player.UniqueMultiplayerID;
'@
    $modText = Replace-Required $modText $oldDrawActions $newDrawActions 'dialogue action hint eligibility guard'

    if (-not $modText.Contains('    private bool CanOpenDirectProfile(NPC npc)')) {
        $profileHelper = @'
    private bool CanOpenDirectProfile(NPC npc)
    {
        // Recruited people and explicit custom recruits remain valid profile owners.
        if (Party.GetAnyOwner(npc.Name) is not null
            || CustomNpcCompatibilityService.IsExplicitCustomRecruit(npc))
        {
            return true;
        }

        // Any actor already registered in the shared companion pool is a creature/summon unit,
        // not a character-profile entry, regardless of who owns it.
        if (Party.CompanionUnits.Any(unit =>
            unit.CharacterName.Equals(npc.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        TeamUpCharacterKind kind = CompanionClassificationService.Classify(npc, Config.SpecialCompanionNpcNames);
        if (kind is TeamUpCharacterKind.FarmerOrSpecialCompanion or TeamUpCharacterKind.NpcLinkedCompanion)
            return false;

        NpcCombatProfile? profile = NpcProfileCatalog.Get(npc.Name);

        // Pelipper Town uses runtime proxy actors such as PelipperTown.Player.* and
        // PelipperTown.Villager.* for Pokemon/summoned partners. They should never open the
        // generic "Special / Companion" placeholder. A deliberately catalogued human profile
        // can still opt in by having a real Team Up profile.
        if (npc.Name.StartsWith("PelipperTown.", StringComparison.OrdinalIgnoreCase)
            && profile is null)
        {
            return false;
        }

        if (PelipperTownCompatibilityService.LooksLikePelipperActor(npc)
            && profile is null)
        {
            return false;
        }

        // Generic summoned NPC-like objects from other providers normally aren't villagers.
        // Keep known/catalogued characters intact, but reject unknown non-villager actors.
        if (!npc.IsVillager && profile is null)
            return false;

        return true;
    }

'@
        $anchor = '    private string GetProfileStatus(string characterName)'
        if (-not $modText.Contains($anchor)) { throw 'Patch anchor missing: CanOpenDirectProfile insertion' }
        $modText = $modText.Replace($anchor, $profileHelper + $anchor)
    }

    Write-Utf8 $modEntry $modText

    $projectText = Read-Lf $project
    $modText = Read-Lf $modEntry
    $followText = Read-Lf $follow
    $deploymentText = Read-Lf $deployment
    $alpha663Text = Read-Lf $alpha663
    $alpha669Text = Read-Lf $alpha669
    $combatText = Read-Lf $combat
    $tileText = Read-Lf $tileSafety

    if (-not $projectText.Contains('<Version>0.2.0-alpha.6.6.11</Version>')) { throw 'Version materialization failed.' }
    foreach ($token in @('build: v0.2.0-alpha.6.6.11','No Companion Profiles Hotfix loaded.','private bool CanOpenDirectProfile(NPC npc)','Party.CompanionUnits.Any','TeamUpCharacterKind.FarmerOrSpecialCompanion','TeamUpCharacterKind.NpcLinkedCompanion','npc.Name.StartsWith("PelipperTown."','!npc.IsVillager && profile is null')) {
        if (-not $modText.Contains($token)) { throw "6.6.11 profile guard token missing: $token" }
    }

    $profileKeyStart = $modText.IndexOf('            if (Config.ProfileKey.JustPressed())')
    $profileKeyEnd = $modText.IndexOf('            if (Config.RecruitKey.JustPressed())', $profileKeyStart)
    if ($profileKeyStart -lt 0 -or $profileKeyEnd -lt 0) { throw 'Unable to inspect dialogue profile input path.' }
    $profileKeyBody = $modText.Substring($profileKeyStart, $profileKeyEnd - $profileKeyStart)
    if (-not $profileKeyBody.Contains('CanOpenDirectProfile(speaker)')) { throw 'Dialogue profile input does not guard companion actors.' }

    $drawStart = $modText.IndexOf('    private void DrawDialogueActions(')
    $drawEnd = $modText.IndexOf('        long recruiterId = Game1.player.UniqueMultiplayerID;', $drawStart)
    if ($drawStart -lt 0 -or $drawEnd -lt 0) { throw 'Unable to inspect DrawDialogueActions.' }
    $drawPrefix = $modText.Substring($drawStart, $drawEnd - $drawStart)
    if (-not $drawPrefix.Contains('CanOpenDirectProfile(speaker)')) { throw 'Dialogue profile hint guard missing.' }

    $openStart = $modText.IndexOf('    private void OpenProfileFromDialogue(NPC npc)')
    $openEnd = $modText.IndexOf('        IClickableMenu? dialogueMenu', $openStart)
    if ($openStart -lt 0 -or $openEnd -lt 0) { throw 'Unable to inspect OpenProfileFromDialogue.' }
    $openPrefix = $modText.Substring($openStart, $openEnd - $openStart)
    if (-not $openPrefix.Contains('CanOpenDirectProfile(npc)')) { throw 'OpenProfileFromDialogue final guard missing.' }

    # Keep the live-confirmed 6.6.10 follow authority lock.
    $companionStart = $followText.IndexOf('    private void UpdateCompanionUnits(')
    $companionEnd = $followText.IndexOf('    private void FollowTarget(', $companionStart)
    if ($companionStart -lt 0 -or $companionEnd -lt 0) { throw 'Unable to inspect UpdateCompanionUnits.' }
    $companionBody = $followText.Substring($companionStart, $companionEnd - $companionStart)
    $skipIndex = $companionBody.IndexOf('PelipperTownCompatibilityService.IsSourceControlled(unit)')
    $resolveIndex = $companionBody.IndexOf('ResolveCharacter(unit.CharacterName)')
    if ($skipIndex -lt 0 -or $resolveIndex -lt 0 -or $skipIndex -gt $resolveIndex) { throw '6.6.10 Pelipper early-skip regression.' }

    foreach ($token in @('desiredState = deployed ? ActiveValue : StandbyValue','currentState.Equals(desiredState','currentOwner.Equals(ownerName')) {
        if (-not $deploymentText.Contains($token)) { throw "6.6.10 deployment regression missing: $token" }
    }
    if ($deploymentText.Contains('SetSuppressed(actor, owner, true)')) { throw 'Pelipper suppression=true regression detected.' }
    if ($alpha663Text.Contains('PelipperTownCompatibilityService.SetSuppressed(')) { throw 'Alpha663 direct Pelipper suppression regression detected.' }

    foreach ($token in @('RenderedWorld','RenderedHud','BuildHealthSnapshotSignatureAlpha669')) {
        if (-not $alpha669Text.Contains($token)) { throw "6.6.9 health regression missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService regressed to expensive placement query.' }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService regressed to expensive placement query.' }
    foreach ($token in @('CombatPathRetryCooldownTicks = 24','CombatMovementPulseTicks = 3','PartyTileSafety.IsWalkableLandOrBridge(location, tile)')) {
        if (-not $combatText.Contains($token)) { throw "Combat regression missing: $token" }
    }
    foreach ($token in @('isWaterTile(x, y)','GetLayer("Buildings")','isTilePassable(tile)')) {
        if (-not $tileText.Contains($token)) { throw "Land-safe regression missing: $token" }
    }

    Log 'Building Alpha 6.6.11 No Companion Profiles Hotfix...'
    Log 'ROOT FIX: Pokemon, summons, special companions, and registered companion units cannot open direct Team Up character profiles.'
    Log 'UI FIX: dialogue Profile hint is hidden for companion/summon actors.'
    Log 'INPUT FIX: profile key and OpenProfileFromDialogue are both guarded.'
    Log 'REGRESSION: Alpha 6.6.10 Pelipper source movement authority remains locked.'
    Log 'REGRESSION: 6.6.7 performance, 6.6.8 land-safe, and 6.6.9 health UI remain locked.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.11 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.11'
    Log 'COMPANION / SUMMON DIRECT PROFILES: BLOCKED'
    Log 'DIALOGUE PROFILE HINT FOR COMPANIONS: HIDDEN'
    Log 'PROFILE INPUT + OPEN FLOW GUARDS: ENABLED'
    Log '6.6.10 PELIPPER FOLLOW AUTHORITY: PRESERVED'
    Log '6.6.7 PERFORMANCE + 6.6.8 LAND-SAFE + 6.6.9 HEALTH: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
