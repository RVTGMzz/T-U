$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root 'src\TeamUp\TeamUp.csproj'
$manifest = Join-Path $root 'src\TeamUp\manifest.json'
$modEntry = Join-Path $root 'src\TeamUp\ModEntry.cs'
$alpha6618 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6618.cs'
$alpha6621 = Join-Path $root 'src\TeamUp\ModEntry.Alpha6621.cs'
$threat = Join-Path $root 'src\TeamUp\Combat\ThreatService.cs'
$combat = Join-Path $root 'src\TeamUp\Combat\CombatService.cs'
$follow = Join-Path $root 'src\TeamUp\Following\FollowService.cs'
$codex = Join-Path $root 'src\TeamUp\UI\CodexBrowserMenu.cs'
$profile = Join-Path $root 'src\TeamUp\UI\CharacterProfileMenu.cs'
$releaseDir = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha6623'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG.txt'
$smoke = Join-Path $root 'SMOKE_TEST_V0_2_ALPHA6_6_23_ANTI_FLICKER_HARD_TAUNT_VI.txt'
$version = '0.2.0-alpha.6.6.23'
$zipName = 'TeamUp_v0.2.0-alpha.6.6.23_ANTI_FLICKER_HARD_TAUNT_TEST.zip'
$zip = Join-Path $releaseDir $zipName
$shaPath = Join-Path $releaseDir 'TeamUp_v0.2.0-alpha.6.6.23_ANTI_FLICKER_HARD_TAUNT_TEST.sha256.txt'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Read-Lf([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8).Replace("`r`n", "`n")
}
function Write-Utf8([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, $utf8NoBom)
}
function RequireReplace([string]$text, [string]$old, [string]$new, [string]$label) {
    if ($text.Contains($new)) { return $text }
    if (-not $text.Contains($old)) { throw "Missing 6.6.23 patch anchor: $label" }
    return $text.Replace($old, $new)
}
function Log([string]$text) {
    $text | Tee-Object -FilePath $log -Append
}

# -----------------------------------------------------------------------------
# Version labels
# -----------------------------------------------------------------------------
$projectText = Read-Lf $project
$projectText = RequireReplace $projectText '<Version>0.2.0-alpha.6.6.22</Version>' '<Version>0.2.0-alpha.6.6.23</Version>' 'project version'
Write-Utf8 $project $projectText

$modText = Read-Lf $modEntry
$modText = RequireReplace $modText 'build: v0.2.0-alpha.6.6.22' 'build: v0.2.0-alpha.6.6.23' 'debug build label'
$modText = RequireReplace $modText 'Team Up! v0.2.0-alpha.6.6.22 Codex 115% UI Polish loaded. Pelipper 6.6.21 lifecycle test path preserved.' 'Team Up! v0.2.0-alpha.6.6.23 Anti-Flicker + Hard Taunt Hotfix loaded. Codex 115% preserved.' 'load label'
Write-Utf8 $modEntry $modText

# -----------------------------------------------------------------------------
# Pelipper NPC-only anti-flicker latch.
# One non-converged source request is allowed. If Pelipper keeps the actor live,
# Team Up keeps the slot occupied but does not hammer the lifecycle every 10 ticks.
# -----------------------------------------------------------------------------
$a21 = Read-Lf $alpha6621
$a21 = RequireReplace $a21 @'
    private bool Alpha6621Registered;
'@ @'
    private bool Alpha6621Registered;
    private readonly HashSet<string> PelipperNonConvergedSourceRequestsAlpha6623 = new(StringComparer.OrdinalIgnoreCase);
'@ 'anti-flicker request latch field'

$a21 = RequireReplace $a21 @'
        Alpha6621Registered = true;
        Helper.ConsoleCommands.Add(
'@ @'
        Alpha6621Registered = true;
        Helper.Events.GameLoop.DayEnding += (_, _) => PelipperNonConvergedSourceRequestsAlpha6623.Clear();
        Helper.Events.GameLoop.ReturnedToTitle += (_, _) => PelipperNonConvergedSourceRequestsAlpha6623.Clear();
        Helper.ConsoleCommands.Add(
'@ 'anti-flicker lifecycle reset events'

$a21 = RequireReplace $a21 @'
        ConfigurePelipperApiBridgeAlpha6619();

        bool routed = PelipperVillagerLifecycleBridge.TrySetEnabled(owner.Name, owner, enabled, out string route);
'@ @'
        ConfigurePelipperApiBridgeAlpha6619();

        string requestKey = $"{owner.Name}|{enabled}";
        bool sourceLiveBefore = IsNpcPelipperSourceLiveAlpha6619(owner);
        bool alreadyConverged = enabled ? sourceLiveBefore : !sourceLiveBefore;
        if (alreadyConverged)
        {
            PelipperNonConvergedSourceRequestsAlpha6623.Remove(requestKey);
            return true;
        }

        // Alpha 6.6.23: a non-converged Pelipper route must not be hammered every 10 ticks.
        // The source-live actor remains authoritative and keeps consuming its real slot, but Team
        // Up waits for an intent change / restore instead of causing a visible spawn-hide loop.
        if (PelipperNonConvergedSourceRequestsAlpha6623.Contains(requestKey))
            return false;

        bool routed = PelipperVillagerLifecycleBridge.TrySetEnabled(owner.Name, owner, enabled, out string route);
'@ 'anti-flicker early convergence/latch gate'

$a21 = RequireReplace $a21 @'
                PelipperNpcNativeControlWarningsAlpha6618.Remove(owner.Name);
                PelipperNonConvergedRoutesAlpha6619.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));
                Monitor.Log(
'@ @'
                PelipperNpcNativeControlWarningsAlpha6618.Remove(owner.Name);
                PelipperNonConvergedRoutesAlpha6619.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));
                PelipperNonConvergedSourceRequestsAlpha6623.Remove(requestKey);
                Monitor.Log(
'@ 'clear anti-flicker latch on convergence'

$a21 = RequireReplace $a21 @'
            string convergenceKey = $"{owner.Name}|{enabled}|{route}";
            if (PelipperNonConvergedRoutesAlpha6619.Add(convergenceKey))
            {
                Monitor.Log(
                    $"Alpha 6.6.21 routed {(enabled ? "deploy" : "recall")} for {owner.Name} via {route}, but Pelipper source is still live={sourceLive}. The real slot remains occupied; Team Up will retry. If this persists, run teamup_pelipper_probe {owner.Name}.",
                    LogLevel.Warn);
            }
            return false;
'@ @'
            string convergenceKey = $"{owner.Name}|{enabled}|{route}";
            PelipperNonConvergedSourceRequestsAlpha6623.Add(requestKey);
            if (PelipperNonConvergedRoutesAlpha6619.Add(convergenceKey))
            {
                Monitor.Log(
                    $"Alpha 6.6.23 routed {(enabled ? "deploy" : "recall")} for {owner.Name} via {route}, but Pelipper source is still live={sourceLive}. Team Up keeps the real slot occupied and suppresses repeated lifecycle retries to prevent flicker. Run teamup_pelipper_probe {owner.Name} for the native route.",
                    LogLevel.Warn);
            }
            return false;
'@ 'latch non-converged Pelipper route'

$a21 = RequireReplace $a21 @'
        if (PelipperNpcNativeControlWarningsAlpha6618.Add(owner.Name))
        {
            Monitor.Log(
                $"Alpha 6.6.21 found no Pelipper villager lifecycle route for {owner.Name}. runtimeRoot={PelipperVillagerLifecycleBridge.RootTypeName}. The source-live Pokemon stays counted. Run teamup_pelipper_probe {owner.Name} for exact candidates.",
                LogLevel.Warn);
        }
        return false;
'@ @'
        PelipperNonConvergedSourceRequestsAlpha6623.Add(requestKey);
        if (PelipperNpcNativeControlWarningsAlpha6618.Add(owner.Name))
        {
            Monitor.Log(
                $"Alpha 6.6.23 found no Pelipper villager lifecycle route for {owner.Name}. runtimeRoot={PelipperVillagerLifecycleBridge.RootTypeName}. The source-live Pokemon stays counted and repeated retries are suppressed to prevent flicker. Run teamup_pelipper_probe {owner.Name} for exact candidates.",
                LogLevel.Warn);
        }
        return false;
'@ 'latch missing Pelipper route'

$a21 = RequireReplace $a21 @'
        ConfigurePelipperApiBridgeAlpha6619();

        bool restored = PelipperVillagerLifecycleBridge.Restore(owner.Name, owner, out string route);
'@ @'
        ConfigurePelipperApiBridgeAlpha6619();
        PelipperNonConvergedSourceRequestsAlpha6623.RemoveWhere(key => key.StartsWith(owner.Name + "|", StringComparison.OrdinalIgnoreCase));

        bool restored = PelipperVillagerLifecycleBridge.Restore(owner.Name, owner, out string route);
'@ 'clear anti-flicker latch on restore'
Write-Utf8 $alpha6621 $a21

# -----------------------------------------------------------------------------
# ThreatService hard aggro lock.
# This is Team Up gameplay authority only; it does not seize vanilla monster controllers.
# -----------------------------------------------------------------------------
$threatText = Read-Lf $threat
$threatText = RequireReplace $threatText @'
    private readonly Dictionary<Monster, Dictionary<string, float>> _tables = new();
'@ @'
    private sealed class ForcedAggroState
    {
        public string ActorId { get; init; } = string.Empty;
        public int Ticks { get; set; }
    }

    private readonly Dictionary<Monster, Dictionary<string, float>> _tables = new();
    private readonly Dictionary<Monster, ForcedAggroState> _forcedAggro = new();
'@ 'forced aggro state'

$threatText = RequireReplace $threatText @'
    public void Clear()
    {
        _tables.Clear();
    }
'@ @'
    public void Clear()
    {
        _tables.Clear();
        _forcedAggro.Clear();
    }
'@ 'forced aggro clear'

$threatText = RequireReplace $threatText @'
        HashSet<Monster> live = monsters.Where(monster => monster.Health > 0).ToHashSet();
        foreach (Monster stale in _tables.Keys.Where(monster => !live.Contains(monster)).ToList())
            _tables.Remove(stale);

        foreach (Monster monster in live)
        {
            Dictionary<string, float> table = GetTable(monster);
'@ @'
        HashSet<Monster> live = monsters.Where(monster => monster.Health > 0).ToHashSet();
        foreach (Monster stale in _tables.Keys.Where(monster => !live.Contains(monster)).ToList())
            _tables.Remove(stale);
        foreach (Monster stale in _forcedAggro.Keys.Where(monster => !live.Contains(monster)).ToList())
            _forcedAggro.Remove(stale);

        foreach (Monster monster in live)
        {
            if (_forcedAggro.TryGetValue(monster, out ForcedAggroState? forced))
            {
                forced.Ticks--;
                if (forced.Ticks <= 0
                    || forced.ActorId == FarmerActorId
                    || !validPartyActors.Contains(forced.ActorId))
                {
                    _forcedAggro.Remove(monster);
                }
            }

            Dictionary<string, float> table = GetTable(monster);
'@ 'forced aggro tick and stale cleanup'

$threatText = RequireReplace $threatText @'
    public void AddThreat(IEnumerable<Monster> monsters, string actorId, float amount)
    {
        foreach (Monster monster in monsters)
            AddThreat(monster, actorId, amount);
    }

    public void ScaleActor(string actorId, float multiplier)
'@ @'
    public void AddThreat(IEnumerable<Monster> monsters, string actorId, float amount)
    {
        foreach (Monster monster in monsters)
            AddThreat(monster, actorId, amount);
    }

    public void ForceAggro(Monster monster, string actorId, int ticks)
    {
        if (monster.Health <= 0 || string.IsNullOrWhiteSpace(actorId) || actorId == FarmerActorId || ticks <= 0)
            return;

        _forcedAggro[monster] = new ForcedAggroState
        {
            ActorId = actorId,
            Ticks = ticks
        };
    }

    public bool IsForcedAggro(Monster monster, string actorId)
        => _forcedAggro.TryGetValue(monster, out ForcedAggroState? forced)
            && forced.Ticks > 0
            && forced.ActorId.Equals(actorId, StringComparison.OrdinalIgnoreCase);

    public void ScaleActor(string actorId, float multiplier)
'@ 'forced aggro API'

$threatText = RequireReplace $threatText @'
    public string GetAggroActor(Monster monster, IReadOnlyCollection<string> validPartyActors)
    {
        Dictionary<string, float> table = GetTable(monster);
'@ @'
    public string GetAggroActor(Monster monster, IReadOnlyCollection<string> validPartyActors)
    {
        if (_forcedAggro.TryGetValue(monster, out ForcedAggroState? forced)
            && forced.Ticks > 0
            && validPartyActors.Contains(forced.ActorId))
        {
            return forced.ActorId;
        }

        Dictionary<string, float> table = GetTable(monster);
'@ 'forced aggro priority'
Write-Utf8 $threat $threatText

# -----------------------------------------------------------------------------
# Combat hard taunt: force internal aggro for a short window and fully redirect
# Farmer HP loss caused while that nearby monster is hard-taunted to the Tank.
# -----------------------------------------------------------------------------
$combatText = Read-Lf $combat
$combatText = RequireReplace $combatText @'
        bool ownsPressure = nearbyThreats.Any(monster =>
            _threat.GetAggroActor(monster, validThreatActors).Equals(guard.CharacterName, StringComparison.OrdinalIgnoreCase)
            || _threat.GetThreat(monster, guard.CharacterName) >= _threat.GetThreat(monster, ThreatService.FarmerActorId) * 0.85f);
        if (!ownsPressure)
            return;
'@ @'
        bool hardTauntOwnsPressure = nearbyThreats.Any(monster => _threat.IsForcedAggro(monster, guard.CharacterName));
        bool ownsPressure = hardTauntOwnsPressure || nearbyThreats.Any(monster =>
            _threat.GetAggroActor(monster, validThreatActors).Equals(guard.CharacterName, StringComparison.OrdinalIgnoreCase)
            || _threat.GetThreat(monster, guard.CharacterName) >= _threat.GetThreat(monster, ThreatService.FarmerActorId) * 0.85f);
        if (!ownsPressure)
            return;
'@ 'hard taunt pressure ownership'

$combatText = RequireReplace $combatText @'
        int mastery = _progression.GetMasteryLevel(guard, PartyRole.Tank);
        float guardRatio = Math.Min(0.55f, 0.35f + mastery * 0.02f);
        int absorbed = Math.Clamp((int)Math.Round(lost * guardRatio), 1, lost);
        FarmerContext.health = Math.Min(FarmerContext.maxHealth, FarmerContext.health + absorbed);

        int redirected = Math.Max(1, absorbed - _progression.GetDefense(guard) / 3);
        guard.CurrentHealth = Math.Max(0, guard.CurrentHealth - redirected);
        guardNpc.showTextAboveHead($"GUARD -{redirected}", new Color(255, 165, 80), 2, 900, 0);
'@ @'
        int mastery = _progression.GetMasteryLevel(guard, PartyRole.Tank);
        float guardRatio = hardTauntOwnsPressure ? 1f : Math.Min(0.55f, 0.35f + mastery * 0.02f);
        int absorbed = Math.Clamp((int)Math.Round(lost * guardRatio), 1, lost);
        FarmerContext.health = Math.Min(FarmerContext.maxHealth, FarmerContext.health + absorbed);

        int redirected = Math.Max(1, absorbed - _progression.GetDefense(guard) / 3);
        guard.CurrentHealth = Math.Max(0, guard.CurrentHealth - redirected);
        if (hardTauntOwnsPressure)
            _incomingDamageCooldowns[guard.CharacterName] = Math.Max(GetCooldown(_incomingDamageCooldowns, guard.CharacterName), 18);
        guardNpc.showTextAboveHead(hardTauntOwnsPressure ? $"TAUNT -{redirected}" : $"GUARD -{redirected}", new Color(255, 165, 80), 2, 900, 0);
'@ 'hard taunt full Farmer damage redirect'

$combatText = RequireReplace $combatText @'
        int mastery = _progression.GetMasteryLevel(member, PartyRole.Tank);
        float amount = (34f + mastery * 5f) * GetEngagementThreatMultiplier(member.Engagement);
        foreach (Monster monster in candidates)
            _threat.AddThreat(monster, member.CharacterName, amount);

        npc.showTextAboveHead("TAUNT", new Color(255, 165, 80), 2, 900, 0);
'@ @'
        int mastery = _progression.GetMasteryLevel(member, PartyRole.Tank);
        float amount = (48f + mastery * 7f) * GetEngagementThreatMultiplier(member.Engagement);
        int hardTauntTicks = Math.Clamp(150 + mastery * 12, 150, 240);
        foreach (Monster monster in candidates)
        {
            _threat.AddThreat(monster, member.CharacterName, amount);
            _threat.ForceAggro(monster, member.CharacterName, hardTauntTicks);
        }

        npc.showTextAboveHead("HARD TAUNT", new Color(255, 165, 80), 2, 1050, 0);
'@ 'tank hard taunt lock'
Write-Utf8 $combat $combatText

# -----------------------------------------------------------------------------
# Acceptance
# -----------------------------------------------------------------------------
if (Test-Path $log) { Remove-Item $log -Force }
$projectText = Read-Lf $project
$modText = Read-Lf $modEntry
$a18 = Read-Lf $alpha6618
$a21 = Read-Lf $alpha6621
$threatText = Read-Lf $threat
$combatText = Read-Lf $combat
$followText = Read-Lf $follow
$codexText = Read-Lf $codex
$profileText = Read-Lf $profile

foreach ($token in @(
    '<Version>0.2.0-alpha.6.6.23</Version>',
    'build: v0.2.0-alpha.6.6.23'
)) {
    if (-not ($projectText + $modText).Contains($token)) { throw "6.6.23 version token missing: $token" }
}
foreach ($token in @(
    'PelipperNonConvergedSourceRequestsAlpha6623',
    'suppress repeated lifecycle retries to prevent flicker',
    'teamup_pelipper_probe'
)) {
    if (-not $a21.Contains($token)) { throw "6.6.23 anti-flicker token missing: $token" }
}
foreach ($token in @(
    'ForcedAggroState',
    'ForceAggro(',
    'IsForcedAggro(',
    'forced.Ticks--'
)) {
    if (-not $threatText.Contains($token)) { throw "6.6.23 forced-aggro token missing: $token" }
}
foreach ($token in @(
    'hardTauntOwnsPressure',
    'hardTauntTicks',
    '_threat.ForceAggro(',
    'HARD TAUNT',
    'guardRatio = hardTauntOwnsPressure ? 1f'
)) {
    if (-not $combatText.Contains($token)) { throw "6.6.23 hard-taunt token missing: $token" }
}
foreach ($token in @(
    'GetEffectiveCombatCompanionCountAlpha6618',
    'PrepareNpcCompanionRecruitCapacityAlpha6618',
    'physically deployed and must block a third companion'
)) {
    if (-not $a18.Contains($token)) { throw "2/2 source-live hard-cap regression token missing: $token" }
}
if (-not $codexText.Contains('Math.Min(1739, Game1.uiViewport.Width - 12)')) { throw '6.6.22 Codex 115% regression.' }
if (-not $profileText.Contains('Math.Min(1518, Game1.uiViewport.Width - 16)')) { throw '6.6.22 Profile 115% regression.' }
if ($followText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'FollowService water/pathfinding regression.' }
if ($combatText.Contains('isTileLocationTotallyClearAndPlaceable')) { throw 'CombatService water/pathfinding regression.' }
if (-not $combatText.Contains('private const int CombatPathRetryCooldownTicks = 24;')) { throw 'Combat retry 24 regression.' }
if (-not $combatText.Contains('private const int CombatMovementPulseTicks = 3;')) { throw 'Combat movement pulse 3 regression.' }

Log 'Building Alpha 6.6.23 Anti-Flicker + Hard Taunt Hotfix...'
Log 'PELIPPER NPC-ONLY: one non-converged lifecycle attempt is latched instead of retried every 10 ticks.'
Log 'PELIPPER SOURCE TRUTH: live Pokemon remains counted until sourceLive becomes false.'
Log 'PELIPPER PROBE: teamup_pelipper_probe remains available for exact native recall mapping.'
Log 'HARD TAUNT: Tank forces Team Up aggro for 150-240 ticks depending on mastery.'
Log 'HARD TAUNT REDIRECT: nearby hard-taunted monster pressure redirects 100% detected Farmer HP loss to Tank.'
Log 'DOUBLE-HIT GUARD: short incoming-damage gate prevents same-pulse virtual Tank double damage.'
Log 'VANILLA MONSTER AUTHORITY: no long-lived monster controller takeover added.'
Log 'CODEX 115%: preserved from 6.6.22.'
Log 'SOURCE-LIVE 2/2 HARD CAP: preserved.'

& dotnet restore $project 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }
& dotnet build $project -c Release --no-restore -p:EnableModDeploy=false -p:EnableModZip=false 2>&1 | Tee-Object -FilePath $log -Append
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

$dll = Get-ChildItem (Join-Path $root 'src\TeamUp\bin\Release') -Recurse -Filter 'TeamUp.dll' |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $dll -or -not (Test-Path $dll.FullName)) { throw 'Compiled TeamUp.dll was not found.' }

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
if (-not (Test-Path $zip)) { throw 'Alpha 6.6.23 ZIP was not created.' }

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Utf8 $shaPath ("$hash  $zipName`r`n")
Copy-Item $smoke (Join-Path $releaseDir (Split-Path $smoke -Leaf)) -Force

Log 'PELIPPER NPC-ONLY RETRY LATCH: ENABLED'
Log 'PELIPPER SOURCE-LIVE SLOT TRUTH: PRESERVED'
Log 'HARD TAUNT FORCED AGGRO WINDOW: ENABLED'
Log 'HARD TAUNT 100% FARMER DAMAGE REDIRECT: ENABLED'
Log 'HARD TAUNT DOUBLE-HIT GUARD: ENABLED'
Log 'CODEX 115% UI: PRESERVED'
Log 'SOURCE-LIVE 2/2 HARD CAP: PRESERVED'
Log 'BUILD SUCCESS - ALPHA 6.6.23'
Log "ZIP: $zipName"
Log "SHA256: $hash"
