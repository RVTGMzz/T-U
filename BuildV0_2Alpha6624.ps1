$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha6613 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6613.cs'
$alpha6615 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6615.cs'
$alpha6618 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6618.cs'
$alpha6621 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6621.cs'
$deployment = Join-Path $root 'src\TeamUp\Core\PelipperDeploymentStateService.cs'
$threat = Join-Path $root 'src\TeamUp\Combat\ThreatService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$profile = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6624'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_24_SOURCE_AUTHORITY_HARD_TAUNT_AUDIT_VI.txt'
$version = '0.2.0-alpha.6.6.24'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.24_SOURCE_AUTHORITY_HARD_TAUNT_AUDIT_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.24_SOURCE_AUTHORITY_HARD_TAUNT_AUDIT_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function Write-Utf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}
function RequireReplace([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Missing 6.6.24 patch anchor: $label" }
    return $text.Replace($old, $new)
}
function RequireRemove([string]$text, [string]$old, [string]$label) {
    if (-not $text.Contains($old)) { return $text }
    return $text.Replace($old, '')
}
function RequireContains([string]$text, [string]$token, [string]$label) {
    if (-not $text.Contains($token)) { throw "6.6.24 required token missing: $label -> $token" }
}
function RequireAbsent([string]$text, [string]$token, [string]$label) {
    if ($text.Contains($token)) { throw "6.6.24 forbidden token still present: $label -> $token" }
}
function Log([string]$text) {
    $text | Tee-Object -FilePath $log -Append
}

# -----------------------------------------------------------------------------
# Version labels
# -----------------------------------------------------------------------------
$projectText = Read-Lf $project
$projectText = RequireReplace $projectText '<Version>0.2.0-alpha.6.6.23</Version>' '<Version>0.2.0-alpha.6.6.24</Version>' 'project version'
Write-Utf8 $project $projectText

$modText = Read-Lf $modEntry
$modText = RequireReplace $modText 'build: v0.2.0-alpha.6.6.23' 'build: v0.2.0-alpha.6.6.24' 'debug build label'
$modText = RequireReplace $modText 'Team Up! v0.2.0-alpha.6.6.23 Anti-Flicker + Hard Taunt Hotfix loaded. Codex 115% preserved.' 'Team Up! v0.2.0-alpha.6.6.24 Source Authority + Hard Taunt Audit loaded. Codex 115% preserved.' 'load label'
Write-Utf8 $modEntry $modText

# -----------------------------------------------------------------------------
# Alpha 6.6.13 cleanup: remove legacy render ownership of Pelipper actors.
# The source mod owns render/movement. Team Up keeps only the soft deployment marker.
# -----------------------------------------------------------------------------
$a13 = Read-Lf $alpha6613
$a13 = RequireReplace $a13 @'
    private readonly HashSet<NPC> PelipperRenderSuppressedAlpha6613 = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<NPC, bool> PelipperRenderRestoreAlpha6613 = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<NPC, bool> PelipperSourceDeploymentAlpha6613 = new(ReferenceEqualityComparer.Instance);
'@ @'
    private readonly Dictionary<NPC, bool> PelipperSourceDeploymentAlpha6613 = new(ReferenceEqualityComparer.Instance);
'@ 'remove legacy render suppression fields'

$a13 = RequireReplace $a13 @'
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6613UpdateTicked;
        Helper.Events.Display.RenderingWorld += OnAlpha6613RenderingWorld;
        Helper.Events.Display.RenderedWorld += OnAlpha6613RenderedWorld;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6613ReturnedToTitle;
'@ @'
        Helper.Events.GameLoop.UpdateTicked += OnAlpha6613UpdateTicked;
        Helper.Events.GameLoop.ReturnedToTitle += OnAlpha6613ReturnedToTitle;
'@ 'remove render event subscriptions'

$a13 = RequireRemove $a13 @'
        PelipperRenderSuppressedAlpha6613.Clear();
'@ 'remove quota render suppression clear'

$a13 = RequireReplace $a13 @'
            PelipperDeploymentStateService.SetDesiredDeployment(
                actor,
                unit.OwnerCharacterName ?? string.Empty,
                deployed);

            TrySetPelipperSourceDeploymentAlpha6613(actor, deployed);

            if (!deployed)
                PelipperRenderSuppressedAlpha6613.Add(actor);
'@ @'
            // Alpha 6.6.24 source authority: quota enforcement records intent only.
            // It never toggles source-owned render/controller/runtime state.
            PelipperDeploymentStateService.SetDesiredDeployment(
                actor,
                unit.OwnerCharacterName ?? string.Empty,
                deployed);
'@ 'quota soft-marker-only path'

$a13 = RequireRemove $a13 @'
    private void OnAlpha6613RenderingWorld(object? sender, RenderingWorldEventArgs e)
    {
        PelipperRenderRestoreAlpha6613.Clear();
        foreach (NPC actor in PelipperRenderSuppressedAlpha6613.ToList())
        {
            if (actor.currentLocation is null)
                continue;

            bool wasInvisible = actor.IsInvisible;
            PelipperRenderRestoreAlpha6613[actor] = wasInvisible;
            if (!wasInvisible)
                TrySetActorInvisibleAlpha6613(actor, true);
        }
    }

    private void OnAlpha6613RenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        foreach ((NPC actor, bool wasInvisible) in PelipperRenderRestoreAlpha6613.ToList())
        {
            if (!wasInvisible)
                TrySetActorInvisibleAlpha6613(actor, false);
        }
        PelipperRenderRestoreAlpha6613.Clear();
    }

'@ 'remove render suppression callbacks'

$a13 = RequireRemove $a13 @'
        PelipperRenderSuppressedAlpha6613.Clear();
        PelipperRenderRestoreAlpha6613.Clear();
'@ 'remove returned-to-title render caches'

$a13 = RequireRemove $a13 @'
    private static void TrySetActorInvisibleAlpha6613(NPC actor, bool invisible)
    {
        try
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase;
            PropertyInfo? property = actor.GetType().GetProperty("IsInvisible", flags)
                ?? typeof(NPC).GetProperty("IsInvisible", flags);
            if (property?.CanWrite == true)
            {
                property.SetValue(actor, invisible);
                return;
            }

            FieldInfo? field = actor.GetType().GetField("isInvisible", flags)
                ?? typeof(NPC).GetField("isInvisible", flags);
            if (field is null)
                return;

            if (field.FieldType == typeof(bool))
            {
                field.SetValue(actor, invisible);
                return;
            }

            object? netBool = field.GetValue(actor);
            PropertyInfo? valueProperty = netBool?.GetType().GetProperty("Value", flags);
            if (valueProperty?.CanWrite == true)
                valueProperty.SetValue(netBool, invisible);
        }
        catch
        {
        }
    }
'@ 'remove IsInvisible writer'
Write-Utf8 $alpha6613 $a13

# -----------------------------------------------------------------------------
# NPC-linked Pelipper source authority.
# When a native owner lifecycle request does not converge, do NOT fall through to
# legacy actor-level toggles. Preserve source-live slot truth and stop touching actor state.
# -----------------------------------------------------------------------------
$a15 = Read-Lf $alpha6615
$a15 = RequireReplace $a15 @'
                bool deployed = !optedOut && IsPelipperUnitDeployedAlpha669(linked);
                PelipperDeploymentStateService.SetDesiredDeployment(actor, owner.Name, deployed);
                TrySetPelipperSourceDeploymentAlpha6613(actor, deployed);
'@ @'
                bool deployed = !optedOut && IsPelipperUnitDeployedAlpha669(linked);
                PelipperDeploymentStateService.SetDesiredDeployment(actor, owner.Name, deployed);
'@ 'linked NPC record uses soft marker only'

$a15 = RequireReplace $a15 @'
        if (unit.OwnerKind == CompanionOwnerKind.PartyMember
            && !string.IsNullOrWhiteSpace(unit.OwnerCharacterName))
        {
            NPC? owner = Game1.getCharacterFromName(unit.OwnerCharacterName);
            if (owner is not null && TrySetPelipperNpcSourceEnabledAlpha6618(
                owner,
                deployed,
                deployed ? "explicit Call" : "explicit Return/Standby"))
            {
                NPC? sourceActor = PelipperTownCompatibilityService.ResolveActor(unit);
                if (sourceActor is not null)
                {
                    PelipperDeploymentStateService.SetDesiredDeployment(
                        sourceActor,
                        unit.OwnerCharacterName,
                        deployed);
                }
                return;
            }
        }

        NPC? actor = PelipperTownCompatibilityService.ResolveActor(unit);
        if (actor is null)
            return;

        PelipperDeploymentStateService.SetDesiredDeployment(
            actor,
            unit.OwnerCharacterName ?? string.Empty,
            deployed);
        TrySetPelipperSourceDeploymentAlpha6613(actor, deployed);
'@ @'
        if (unit.OwnerKind == CompanionOwnerKind.PartyMember
            && !string.IsNullOrWhiteSpace(unit.OwnerCharacterName))
        {
            NPC? sourceActor = PelipperTownCompatibilityService.ResolveActor(unit);
            if (sourceActor is not null)
            {
                PelipperDeploymentStateService.SetDesiredDeployment(
                    sourceActor,
                    unit.OwnerCharacterName,
                    deployed);
            }

            NPC? owner = Game1.getCharacterFromName(unit.OwnerCharacterName);
            if (owner is not null)
            {
                // One source-native request per intent. Alpha 6.6.23 latches non-convergence;
                // Alpha 6.6.24 never falls through to actor-level render/runtime manipulation.
                TrySetPelipperNpcSourceEnabledAlpha6618(
                    owner,
                    deployed,
                    deployed ? "explicit Call" : "explicit Return/Standby");
            }
            return;
        }

        // Player-owned / non-NPC-linked Pelipper units keep the existing actor handshake for now.
        // This path does not participate in the NPC-only flicker bug.
        NPC? actor = PelipperTownCompatibilityService.ResolveActor(unit);
        if (actor is null)
            return;

        PelipperDeploymentStateService.SetDesiredDeployment(
            actor,
            unit.OwnerCharacterName ?? string.Empty,
            deployed);
        TrySetPelipperSourceDeploymentAlpha6613(actor, deployed);
'@ 'NPC-linked path never falls through to actor fallback'
Write-Utf8 $alpha6615 $a15

# -----------------------------------------------------------------------------
# Hard Taunt geometry audit.
# Normal Guard remains 4 tiles. A Tank who owns forced aggro may redirect from 7 tiles,
# closing the 6.6.23 hole where Alex could taunt at range but fail the guard-owner filter.
# -----------------------------------------------------------------------------
$combatText = Read-Lf $combat
$combatText = RequireReplace $combatText @'
    private const float HardLeashTiles = 12f;
    private const float RepathThresholdTiles = 1.35f;
'@ @'
    private const float HardLeashTiles = 12f;
    private const float NormalGuardRangeTiles = 4f;
    private const float HardTauntGuardRangeTiles = 7f;
    private const float RepathThresholdTiles = 1.35f;
'@ 'guard range constants'

$combatText = RequireReplace $combatText @'
            .Where(member =>
            {
                NPC? npc = Game1.getCharacterFromName(member.CharacterName);
                return npc is not null
                    && ReferenceEquals(npc.currentLocation, FarmerContext.currentLocation)
                    && Vector2.Distance(npc.Tile, FarmerContext.Tile) <= 4f;
            })
            .OrderByDescending(member => nearbyThreats.Count(monster =>
                _threat.GetAggroActor(monster, validThreatActors).Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(member => _threat.GetTotalThreat(member.CharacterName, nearbyThreats))
            .FirstOrDefault();
'@ @'
            .Where(member =>
            {
                NPC? npc = Game1.getCharacterFromName(member.CharacterName);
                if (npc is null || !ReferenceEquals(npc.currentLocation, FarmerContext.currentLocation))
                    return false;

                bool ownsHardTaunt = nearbyThreats.Any(monster =>
                    _threat.IsForcedAggro(monster, member.CharacterName));
                float guardRange = ownsHardTaunt ? HardTauntGuardRangeTiles : NormalGuardRangeTiles;
                return Vector2.Distance(npc.Tile, FarmerContext.Tile) <= guardRange;
            })
            .OrderByDescending(member => nearbyThreats.Count(monster =>
                _threat.IsForcedAggro(monster, member.CharacterName)))
            .ThenByDescending(member => nearbyThreats.Count(monster =>
                _threat.GetAggroActor(monster, validThreatActors).Equals(member.CharacterName, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(member => _threat.GetTotalThreat(member.CharacterName, nearbyThreats))
            .FirstOrDefault();
'@ 'hard taunt guard-owner range and priority'
Write-Utf8 $combat $combatText

# -----------------------------------------------------------------------------
# Source audit before compiling.
# -----------------------------------------------------------------------------
if (Test-Path $log) { Remove-Item $log -Force }

$projectText = Read-Lf $project
$modText = Read-Lf $modEntry
$a13 = Read-Lf $alpha6613
$a15 = Read-Lf $alpha6615
$a18 = Read-Lf $alpha6618
$a21 = Read-Lf $alpha6621
$deploymentText = Read-Lf $deployment
$threatText = Read-Lf $threat
$combatText = Read-Lf $combat
$followText = Read-Lf $follow
$codexText = Read-Lf $codex
$profileText = Read-Lf $profile

RequireContains $projectText '<Version>0.2.0-alpha.6.6.24</Version>' 'version'
RequireContains $modText 'build: v0.2.0-alpha.6.6.24' 'debug build version'
RequireContains $modText 'Math.Clamp(Config.MaxPartyMembers, 1, 6)' 'party cap 6'
RequireContains $modText 'Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2)' 'companion cap 2'

foreach ($token in @(
    'PelipperRenderSuppressedAlpha6613',
    'PelipperRenderRestoreAlpha6613',
    'TrySetActorInvisibleAlpha6613',
    'RenderingWorld += OnAlpha6613RenderingWorld',
    'RenderedWorld += OnAlpha6613RenderedWorld'
)) {
    RequireAbsent $a13 $token 'legacy Pelipper render ownership'
}
RequireContains $a13 'PelipperDeploymentStateService.SetDesiredDeployment' 'soft marker quota authority'

RequireContains $a15 'Alpha 6.6.24 never falls through to actor-level render/runtime manipulation.' 'NPC source authority return gate'
RequireContains $a15 'TrySetPelipperNpcSourceEnabledAlpha6618' 'native NPC lifecycle route preserved'
RequireContains $a15 'return;' 'NPC path terminates'

foreach ($token in @(
    'GetEffectiveCombatCompanionCountAlpha6618',
    'PrepareNpcCompanionRecruitCapacityAlpha6618',
    'physically deployed and must block a third companion'
)) {
    RequireContains $a18 $token '6.6.18 source-live hard-cap regression'
}
RequireContains $a21 'PelipperNonConvergedSourceRequestsAlpha6623' 'non-converged lifecycle latch'
RequireContains $deploymentText 'Ronvotri.TeamUp/PelipperDeployment' 'soft deployment marker key'

RequireContains $threatText 'ForceAggro' 'forced aggro service'
RequireContains $threatText 'IsForcedAggro' 'forced aggro query'
RequireContains $combatText 'private const int CombatPathRetryCooldownTicks = 24;' 'combat path retry 24'
RequireContains $combatText 'private const int CombatMovementPulseTicks = 3;' 'combat movement pulse 3'
RequireContains $combatText 'private const float NormalGuardRangeTiles = 4f;' 'normal guard range 4'
RequireContains $combatText 'private const float HardTauntGuardRangeTiles = 7f;' 'hard taunt guard range 7'
RequireContains $combatText 'hardTauntOwnsPressure ? 1f' '100 percent hard taunt redirect'
RequireContains $combatText 'HARD TAUNT' 'hard taunt activation label'
RequireContains $combatText '_incomingDamageCooldowns[guard.CharacterName]' 'redirect double-hit gate'

RequireAbsent $followText 'isTileLocationTotallyClearAndPlaceable' 'follow water/pathfinding regression'
RequireAbsent $combatText 'isTileLocationTotallyClearAndPlaceable' 'combat water/pathfinding regression'
RequireContains $codexText 'Math.Min(1739, Game1.uiViewport.Width - 12)' 'Codex 115 percent width'
RequireContains $codexText 'Math.Min(1049, Game1.uiViewport.Height - 12)' 'Codex 115 percent height'
RequireContains $profileText 'Math.Min(1518, Game1.uiViewport.Width - 16)' 'Profile 115 percent width'
RequireContains $profileText 'Math.Min(897, Game1.uiViewport.Height - 16)' 'Profile 115 percent height'

Log 'Building Alpha 6.6.24 Source Authority + Hard Taunt Audit...'
Log 'PELIPPER: legacy RenderingWorld/RenderedWorld suppression removed.'
Log 'PELIPPER: Team Up no longer writes IsInvisible for source-owned Pokemon.'
Log 'PELIPPER NPC: native lifecycle non-convergence never falls through to actor-level toggles.'
Log 'PELIPPER SLOT TRUTH: source-live Pokemon still occupies a real 2/2 slot.'
Log 'HARD TAUNT: forced aggro preserved; guard-owner range 4 -> 7 tiles only while forced aggro is owned.'
Log 'HARD TAUNT: 100% Farmer HP redirect during forced pressure preserved with anti-double-hit gate.'
Log 'REGRESSION: party cap 6, companion cap 2/2, combat retry 24, movement pulse 3 preserved.'
Log 'REGRESSION: Codex/Profile 115% and water-pathfinding safety preserved.'

& dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
& dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

$dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

# Binary audit: compile success is not enough. The old render-owner symbols must be gone.
$dllStrings = (& strings $dll.FullName | Out-String)
foreach ($token in @('PelipperRenderSuppressedAlpha6613', 'PelipperRenderRestoreAlpha6613', 'TrySetActorInvisibleAlpha6613')) {
    if ($dllStrings.Contains($token)) { throw "DLL AUDIT FAILED: legacy render symbol still present: $token" }
}
foreach ($token in @('ForceAggro', 'PelipperNonConvergedSourceRequestsAlpha6623', 'HardTauntGuardRangeTiles')) {
    if (-not $dllStrings.Contains($token)) { throw "DLL AUDIT FAILED: expected 6.6.24/runtime token missing: $token" }
}

if (-not (Test-Path $releaseDir)) { New-Item -ItemType Directory -Path $releaseDir | Out-Null }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $stageMod -Force | Out-Null
Copy-Item $dll.FullName (Join-Path $stageMod 'TeamUp.dll') -Force

$manifestText = Read-Lf $manifest
$manifestText = $manifestText.Replace('%ProjectVersion%', $version)
Write-Utf8 (Join-Path $stageMod 'manifest.json') $manifestText
Copy-Item (Join-Path $root 'src\TeamUp\i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal -Force
if (-not (Test-Path $zip)) { throw 'Alpha 6.6.24 ZIP was not created.' }

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Utf8 $shaPath ("$hash  $zipName`r`n")
Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

Log 'SOURCE AUTHORITY LEGACY RENDER SYMBOLS: ABSENT IN DLL'
Log 'NPC NATIVE-LIFECYCLE FALLTHROUGH: BLOCKED'
Log 'SOURCE-LIVE 2/2 HARD CAP: PRESERVED'
Log 'HARD TAUNT FORCED AGGRO: PRESENT IN DLL'
Log 'HARD TAUNT RANGE AUDIT: 7 TILES WHILE FORCED / 4 TILES NORMAL'
Log 'CODEX/PROFILE 115%: PRESERVED'
Log 'WATER PATHFINDING REGRESSION GATE: PASS'
Log 'BUILD SUCCESS - ALPHA 6.6.24'
Log "ZIP: $zipName"
Log "SHA256: $hash"
