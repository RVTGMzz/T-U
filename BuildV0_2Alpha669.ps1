$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha663 = Join-Path $root 'src\TeamUp\ModEntry.Alpha663.cs'
$alpha669 = Join-Path $root 'src\TeamUp\ModEntry.Alpha669.cs'
$pelipper = Join-Path $root 'src\TeamUp\Core\PelipperTownCompatibilityService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$tileSafety = Join-Path $root 'src\TeamUp\Core\PartyTileSafety.cs'
$equipment = Join-Path $root 'src\TeamUp\UI\EquipmentMenu.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha669'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$zip = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.9_COMPANION_FLICKER_HEALTH_BARS_HOTFIX_TEST.zip'
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.9_COMPANION_FLICKER_HEALTH_BARS_HOTFIX_TEST.sha256.txt'
$version = '0.2.0-alpha.6.6.9'
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
    foreach ($required in @($project,$manifest,$modEntry,$alpha663,$alpha669,$pelipper,$follow,$combat,$tileSafety,$equipment,$codex)) {
        if (-not (Test-Path $required)) { throw "Missing Alpha 6.6.9 source: $required" }
    }

    $projectText = Read-Lf $project
    $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', "<Version>$version</Version>")
    Write-Utf8 $project $projectText

    $modText = Read-Lf $modEntry
    if (-not $modText.Contains('RegisterAlpha669HotfixEvents();')) {
        $modText = Replace-Required $modText `
            '        RegisterAlpha663HotfixEvents();' `
            "        RegisterAlpha663HotfixEvents();`n        RegisterAlpha669HotfixEvents();" `
            'register Alpha 6.6.9 hotfix events'
    }
    $modText = [regex]::Replace($modText, 'build: v0\.2\.0-alpha\.6\.6\.\d+', 'build: v0.2.0-alpha.6.6.9')
    $modText = [regex]::Replace(
        $modText,
        'Team Up! v0\.2\.0-alpha\.6\.6\.\d+ [^\r\n"]+ loaded\.',
        'Team Up! v0.2.0-alpha.6.6.9 Companion Flicker + NPC Health Bars Hotfix loaded.')
    Write-Utf8 $modEntry $modText

    $pelipperText = Read-Lf $pelipper
    if (-not $pelipperText.Contains('DeploymentStateKey = "Ronvotri.TeamUp/DeploymentState"')) {
        $oldConstants = '    public const string CombatTargetOptInKey = "Ronvotri.TeamUp/CombatTarget";'
        $newConstants = @'
    public const string CombatTargetOptInKey = "Ronvotri.TeamUp/CombatTarget";
    public const string DeploymentStateKey = "Ronvotri.TeamUp/DeploymentState";
    public const string DeploymentOwnerKey = "Ronvotri.TeamUp/DeploymentOwner";
'@
        $pelipperText = Replace-Required $pelipperText $oldConstants $newConstants.TrimEnd() 'Pelipper soft deployment contract constants'
    }

    if (-not $pelipperText.Contains('private static void RestoreLegacySuppression')) {
        $setPattern = '(?s)    public static void SetSuppressed\(NPC actor, string ownerName, bool suppressed\)\n    \{.*?\n    \}\n\n    public static void CleanupOrphanedSuppression'
        $setReplacement = @'
    public static void SetSuppressed(NPC actor, string ownerName, bool suppressed)
    {
        // Alpha 6.6.9: source-owned Pelipper actors keep render/movement authority.
        // Team Up records desired deployment state only. Do not toggle IsInvisible, Halt,
        // controller, or temporaryController here, since Pelipper may update those itself.
        RestoreLegacySuppression(actor);
        actor.modData[DeploymentStateKey] = suppressed ? "Standby" : "Active";
        if (!string.IsNullOrWhiteSpace(ownerName))
            actor.modData[DeploymentOwnerKey] = ownerName;
        else
            actor.modData.Remove(DeploymentOwnerKey);
    }

    private static void RestoreLegacySuppression(NPC actor)
    {
        bool hadLegacySuppression = actor.modData.TryGetValue(SuppressedKey, out string? rawSuppressed)
            && rawSuppressed.Equals("true", StringComparison.OrdinalIgnoreCase);
        if (hadLegacySuppression)
        {
            bool originalInvisible = actor.modData.TryGetValue(OriginalInvisibleKey, out string? rawOriginal)
                && bool.TryParse(rawOriginal, out bool parsed)
                && parsed;
            TrySetInvisible(actor, originalInvisible);
        }

        actor.modData.Remove(SuppressedKey);
        actor.modData.Remove(SuppressedOwnerKey);
        actor.modData.Remove(OriginalInvisibleKey);
    }

    public static void CleanupOrphanedSuppression
'@
        $patched = [regex]::Replace($pelipperText, $setPattern, $setReplacement, 1)
        if ($patched -eq $pelipperText) { throw 'Pelipper SetSuppressed patch anchor missing.' }
        $pelipperText = $patched
    }

    if (-not $pelipperText.Contains('bool clearAllDeployment = active.Count == 0;')) {
        $cleanupPattern = '(?s)    public static void CleanupOrphanedSuppression\(IReadOnlyCollection<string> activeTeamUpOwnerNames\)\n    \{.*?\n    \}\n\n    public static bool ShouldExcludeFromTeamUpCombat'
        $cleanupReplacement = @'
    public static void CleanupOrphanedSuppression(IReadOnlyCollection<string> activeTeamUpOwnerNames)
    {
        HashSet<string> active = activeTeamUpOwnerNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool clearAllDeployment = active.Count == 0;
        foreach (GameLocation location in Game1.locations)
        {
            foreach (NPC actor in location.characters.OfType<NPC>())
            {
                if (!LooksLikePelipperActor(actor))
                    continue;

                // One-time repair for saves produced by pre-6.6.9 visibility suppression.
                RestoreLegacySuppression(actor);

                if (clearAllDeployment)
                {
                    actor.modData.Remove(DeploymentStateKey);
                    actor.modData.Remove(DeploymentOwnerKey);
                    continue;
                }

                if (!actor.modData.TryGetValue(DeploymentOwnerKey, out string? owner)
                    || string.IsNullOrWhiteSpace(owner)
                    || active.Contains(owner))
                {
                    continue;
                }

                actor.modData.Remove(DeploymentStateKey);
                actor.modData.Remove(DeploymentOwnerKey);
            }
        }
    }

    public static bool ShouldExcludeFromTeamUpCombat
'@
        $patched = [regex]::Replace($pelipperText, $cleanupPattern, $cleanupReplacement, 1)
        if ($patched -eq $pelipperText) { throw 'Pelipper cleanup patch anchor missing.' }
        $pelipperText = $patched
    }
    Write-Utf8 $pelipper $pelipperText

    $modText = Read-Lf $modEntry
    $alphaText = Read-Lf $alpha669
    $pelipperText = Read-Lf $pelipper
    $followText = Read-Lf $follow
    $combatText = Read-Lf $combat
    $tileSafetyText = Read-Lf $tileSafety
    $alpha663Text = Read-Lf $alpha663
    $equipmentText = Read-Lf $equipment
    $codexText = Read-Lf $codex

    foreach ($token in @('RegisterAlpha669HotfixEvents();','build: v0.2.0-alpha.6.6.9','Companion Flicker + NPC Health Bars Hotfix loaded.')) {
        if (-not $modText.Contains($token)) { throw "ModEntry 6.6.9 token missing: $token" }
    }
    foreach ($token in @('RenderedWorld','PartyHealthBarWidthAlpha669 = 52','Progression.GetMaxHealth(member)','member.CurrentHealth','member.IsDowned','PartyHealthBarCombatRadiusAlpha669 = 10f','hpText = isDowned ? "DOWN"')) {
        if (-not $alphaText.Contains($token)) { throw "Health bar token missing: $token" }
    }
    foreach ($token in @('DeploymentStateKey = "Ronvotri.TeamUp/DeploymentState"','DeploymentOwnerKey = "Ronvotri.TeamUp/DeploymentOwner"','RestoreLegacySuppression(actor)','actor.modData[DeploymentStateKey] = suppressed ? "Standby" : "Active"','bool clearAllDeployment = active.Count == 0;')) {
        if (-not $pelipperText.Contains($token)) { throw "Pelipper soft deployment token missing: $token" }
    }

    $setStart = $pelipperText.IndexOf('    public static void SetSuppressed')
    $setEnd = $pelipperText.IndexOf('    private static void RestoreLegacySuppression', $setStart)
    if ($setStart -lt 0 -or $setEnd -lt 0) { throw 'Unable to inspect Pelipper SetSuppressed body.' }
    $setBody = $pelipperText.Substring($setStart, $setEnd - $setStart)
    foreach ($forbidden in @('TrySetInvisible','actor.Halt()','actor.controller','actor.temporaryController')) {
        if ($setBody.Contains($forbidden)) { throw "Pelipper source-authority regression inside SetSuppressed: $forbidden" }
    }

    foreach ($token in @('FindLandOpenNear','PartyTileSafety.IsWalkableLandOrBridge(owner.currentLocation, npc.Tile)')) {
        if (-not $followText.Contains($token)) { throw "6.6.8 follow regression missing: $token" }
    }
    if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService regressed to expensive placement query.' }
    foreach ($token in @('CombatPathRetryCooldownTicks = 24','CombatMovementPulseTicks = 3','ShouldExcludeFromTeamUpCombat(monster)','PartyTileSafety.IsWalkableLandOrBridge(location, tile)')) {
        if (-not $combatText.Contains($token)) { throw "6.6.7/6.6.8 combat regression missing: $token" }
    }
    if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService regressed to expensive placement query.' }
    foreach ($token in @('isWaterTile(x, y)','GetLayer("Buildings")','isTilePassable(tile)')) {
        if (-not $tileSafetyText.Contains($token)) { throw "Land-safe tile regression missing: $token" }
    }
    foreach ($token in @('e.Button.IsActionButton()','e.Button.IsUseToolButton()','routedButton = Buttons.A','routedButton = Buttons.X')) {
        if (-not $alpha663Text.Contains($token)) { throw "Switch equip/unequip regression missing: $token" }
    }
    foreach ($token in @('ControllerActivationDebounceMs = 180','ControllerMouseEchoSuppressionMs = 260','DoubleClickWindowMs = 450')) {
        if (-not $equipmentText.Contains($token)) { throw "Equipment regression missing: $token" }
    }
    if ($codexText.Contains('MoveVertical(2)') -or $codexText.Contains('MoveVertical(-2)')) { throw 'Codex one-row navigation regressed.' }

    Log 'Building Alpha 6.6.9 Companion Flicker + NPC Health Bars Hotfix...'
    Log 'FIX: Team Up no longer toggles Pelipper IsInvisible or movement controllers during deployment reconciliation.'
    Log 'FIX: legacy Team Up Pelipper visibility suppression is restored once, then removed.'
    Log 'FEATURE: NPC health bars render above active Team Up members while wounded/downed or near valid combat targets.'
    Log 'REGRESSION: 6.6.7 performance and 6.6.8 land-safe follow/combat remain locked.'

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
    if (-not (Test-Path $zip)) { throw 'Alpha 6.6.9 ZIP was not created.' }

    $hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    Write-Utf8 $shaPath ("$hash  $(Split-Path $zip -Leaf)`r`n")
    $smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_9_COMPANION_FLICKER_HEALTH_BARS_VI.txt'
    if (Test-Path $smoke) { Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force }
    Remove-Item $stageRoot -Recurse -Force -ErrorAction SilentlyContinue

    Log ''
    Log '========================================================='
    Log 'BUILD SUCCESS - ALPHA 6.6.9'
    Log 'PELIPPER SOURCE RENDER AUTHORITY: ENABLED'
    Log 'LEGACY VISIBILITY REPAIR: ENABLED'
    Log 'NPC WORLD HEALTH BARS: ENABLED'
    Log '6.6.7 PERFORMANCE REGRESSION: PRESERVED'
    Log '6.6.8 LAND-SAFE REGRESSION: PRESERVED'
    Log "ZIP: $zip"
    Log "SHA256: $hash"
    Log '========================================================='
}
catch {
    Log ''
    Log ('BUILD FAILED: ' + $_.Exception.Message)
    throw
}
