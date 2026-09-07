$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $root 'src/TeamUp'
$release = Join-Path $root 'release'
$stageRoot = Join-Path $root '_stage_alpha671'
$stageMod = Join-Path $stageRoot 'Team Up'
$log = Join-Path $root 'BUILD_LOG_ALPHA671.txt'
$version = '0.2.0-alpha.6.7.1'
$zipName = 'TeamUp_v0.2.0-alpha.6.7.1_LIVE_POLISH_DORMANT_COMPANION_TEST.zip'
$zip = Join-Path $release $zipName
$shaPath = Join-Path $release 'TeamUp_v0.2.0-alpha.6.7.1_LIVE_POLISH_DORMANT_COMPANION_TEST.sha256.txt'
$utf8 = New-Object System.Text.UTF8Encoding($false)

function ReadText([string]$path) { [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) }
function WriteText([string]$path, [string]$text) { [System.IO.File]::WriteAllText($path, $text, $utf8) }
function Require([bool]$ok, [string]$message) { if (-not $ok) { throw $message } }
function ReplaceOnce([string]$path, [string]$old, [string]$new, [string]$label) {
    $text = ReadText $path
    if ($text.Contains($new)) { return }
    Require ($text.Contains($old)) "Missing patch anchor: $label"
    WriteText $path ($text.Replace($old, $new))
}
function Log([string]$text) { $text | Tee-Object -FilePath $log -Append }

if (Test-Path $log) { Remove-Item $log -Force }
if (Test-Path $stageRoot) { Remove-Item $stageRoot -Recurse -Force }
New-Item -ItemType Directory -Force -Path $release | Out-Null

$project = Join-Path $src 'TeamUp.csproj'
$banter = Join-Path $src 'Core/PartyBanterService.cs'
$alpha661 = Join-Path $src 'ModEntry.Alpha661.cs'
$alpha663 = Join-Path $src 'ModEntry.Alpha663.cs'
$alpha6626 = Join-Path $src 'ModEntry.Alpha6626.cs'
$nativeBridge = Join-Path $src 'Core/PelipperTown119NativeBridge.cs'
$profiles = Join-Path $src 'Core/NpcProfileCatalog.cs'
$identities = Join-Path $src 'Core/CharacterSkillIdentityCatalog.cs'
$combat = Join-Path $src 'Combat/CombatService.cs'
$defaultI18n = Join-Path $src 'i18n/default.json'
$viI18n = Join-Path $src 'i18n/vi.json'

ReplaceOnce $project '<Version>0.2.0-alpha.6.7.0</Version>' '<Version>0.2.0-alpha.6.7.1</Version>' 'project version'
ReplaceOnce $banter 'new Color(245, 235, 205)' 'new Color(72, 42, 28)' 'banter readable text color'

ReplaceOnce $alpha661 'LiveCompanionDescriptor? detectedCompanion = CompanionIntegrationService.FindLinkedCompanion(npc);' 'LiveCompanionDescriptor? detectedCompanion = FindRecruitCandidateCompanionAlpha671(npc);' 'authoritative recruit candidate lookup'
ReplaceOnce $alpha661 'LiveCompanionDescriptor? companion = CompanionIntegrationService.FindLinkedCompanion(npc);' 'LiveCompanionDescriptor? companion = FindRecruitCandidateCompanionAlpha671(npc);' 'recruit menu dormant lookup'
ReplaceOnce $alpha661 'SendActionResult(responsePlayerId, false, "TEAM UP PARTY FULL • 6/6 people");' 'SendActionResult(responsePlayerId, false, Helper.Translation.Get("party.full-hud", new { max = Config.MaxPartyMembers }));' '5/5 party full HUD'
ReplaceOnce $alpha661 'new("InviteOnly", $"{npc.displayName} only"),' 'new("InviteOnly", Helper.Translation.Get("recruit.invite-only", new { name = npc.displayName })),' 'localized invite-only label'
ReplaceOnce $alpha661 'new("InviteTogether", $"{npc.displayName} + {companion.DisplayName}"),' 'new("InviteTogether", Helper.Translation.Get("recruit.invite-together", new { name = npc.displayName, companion = companion.DisplayName })),' 'localized invite-together label'
ReplaceOnce $alpha661 '$"Invite {npc.displayName} to Team Up?",' 'Helper.Translation.Get("recruit.question", new { name = npc.displayName }),' 'localized together recruit title'

$nativeChoiceOld = @'
        PelipperTownCompatibilityService.SetOwnerOptOut(owner, !includeCompanion);
'@
$nativeChoiceNew = @'
        PelipperTownCompatibilityService.SetOwnerOptOut(owner, !includeCompanion);

        // Alpha 6.7.1: recruitment controls Pelipper at the source even while the configured
        // partner actor is asleep/dormant. NPC-only therefore cannot auto-spawn later.
        if (PelipperTown119NativeBridge.HasVillagerLifecycle)
            PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled(owner.Name, includeCompanion, out _);
'@
ReplaceOnce $alpha663 $nativeChoiceOld $nativeChoiceNew 'native dormant NPC recruit choice'

$bridgeAnchor = @'
    public static bool TryIsVillagerCompanionConfiguredEnabled(string npcName, out bool enabled)
'@
$bridgeMethod = @'
    /// <summary>
    /// Alpha 6.7.1: asks Pelipper 1.1.9 for the configured villager partner even when its runtime
    /// actor is dormant. Verified DLL signature:
    /// TryGetConfiguredCompanion(string, out SpeciesDefinition, out bool) -> bool.
    /// This descriptor is recruitment intent only and must never be treated as source-live truth.
    /// </summary>
    public static bool TryGetConfiguredVillagerCompanionDescriptor(string npcName, out LiveCompanionDescriptor? descriptor)
    {
        descriptor = null;
        object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));
        if (manager?.GetType().FullName != "PelipperTown.VillagerCompanionManager"
            || string.IsNullOrWhiteSpace(npcName))
        {
            return false;
        }

        if (TryIsVillagerCompanionConfiguredEnabled(npcName, out bool enabled) && !enabled)
            return true;

        MethodInfo? method = manager.GetType().GetMethods(InstanceFlags)
            .FirstOrDefault(candidate =>
            {
                if (!candidate.Name.Equals("TryGetConfiguredCompanion", StringComparison.Ordinal))
                    return false;
                ParameterInfo[] parameters = candidate.GetParameters();
                return candidate.ReturnType == typeof(bool)
                    && parameters.Length == 3
                    && parameters[0].ParameterType == typeof(string)
                    && parameters[1].ParameterType.IsByRef
                    && parameters[2].ParameterType == typeof(bool).MakeByRefType();
            });
        if (method is null)
            return false;

        try
        {
            object?[] args = { npcName, null, false };
            if (method.Invoke(manager, args) is not bool found || !found || args[1] is null)
                return true;

            object species = args[1]!;
            Type speciesType = species.GetType();
            string speciesId = SafeGet(() => speciesType.GetProperty("Id", InstanceFlags)?.GetValue(species)?.ToString()) ?? string.Empty;
            string displayName = SafeGet(() => speciesType.GetProperty("DisplayName", InstanceFlags)?.GetValue(species)?.ToString()) ?? speciesId;
            if (string.IsNullOrWhiteSpace(speciesId))
                return false;
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = speciesId;

            string providerUnitId = $"configured:npc:{npcName}:{speciesId}";
            descriptor = new LiveCompanionDescriptor
            {
                UnitId = $"{PelipperTownCompatibilityService.ProviderId}:{providerUnitId}",
                CharacterName = speciesId,
                DisplayName = displayName,
                OwnerKind = CompanionOwnerKind.PartyMember,
                OwnerCharacterName = npcName,
                ProviderId = PelipperTownCompatibilityService.ProviderId,
                ProviderUnitId = providerUnitId
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

'@
$text = ReadText $nativeBridge
if (-not $text.Contains('TryGetConfiguredVillagerCompanionDescriptor')) {
    Require ($text.Contains($bridgeAnchor)) 'Missing configured-companion bridge anchor.'
    WriteText $nativeBridge ($text.Replace($bridgeAnchor, $bridgeMethod + $bridgeAnchor))
}

$authorityOld = @'
                if (liveKeys.Contains(key))
                    continue;

                if (IsSlotReservedAlpha6617(unit.State))
'@
$authorityNew = @'
                if (liveKeys.Contains(key) || IsDormantConfiguredReservationAlpha671(unit, online))
                    continue;

                if (IsSlotReservedAlpha6617(unit.State))
'@
ReplaceOnce $alpha6626 $authorityOld $authorityNew 'preserve dormant configured slot reservation'

$availableOld = @'
            int availablePelipperSlots = Math.Max(0, max - Math.Min(max, nonPelipper.Count));
'@
$availableNew = @'
            // Alpha 6.7.1: an explicitly selected NPC + Pokemon reserves capacity even while
            // Pelipper keeps that Pokemon asleep/dormant. Trim impossible old-save overflow first.
            int dormantBudget = Math.Max(0, max - Math.Min(max, nonPelipper.Count));
            List<CompanionUnitData> dormantPelipper = Party.CompanionUnits
                .Where(unit => IsDormantConfiguredReservationAlpha671(unit, online))
                .ToList();
            for (int i = dormantBudget; i < dormantPelipper.Count; i++)
            {
                CompanionUnitData overflow = dormantPelipper[i];
                changed |= Party.SetCompanionState(overflow.UnitId, overflow.RecruiterId, CompanionDeploymentState.Standby);
                if (repairOverflow)
                    RequestAuthorityReturnAlpha6626(overflow, "dormant configured 2/2 overflow");
            }

            int activeDormantReservations = dormantPelipper
                .Take(dormantBudget)
                .Count(unit => IsSlotReservedAlpha6617(unit.State));
            int availablePelipperSlots = Math.Max(0,
                max - Math.Min(max, nonPelipper.Count) - activeDormantReservations);
'@
ReplaceOnce $alpha6626 $availableOld $availableNew 'dormant slot budget'

$gusProfile = @'
            ["Gus"] = P("Gus", PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 3, 2, 5, 4, 2),
'@
$guntherProfile = @'
            ["Gus"] = P("Gus", PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 3, 2, 5, 4, 2),
            ["Gunther"] = P("Gunther", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 1, 5),
'@
ReplaceOnce $profiles $gusProfile $guntherProfile 'Gunther completed combat profile'

$gusIdentity = @'
            ["Gus"] = I("Gus", "HOT PLATE", CharacterSignatureArchetype.Hybrid, 780, 690,
                damage: 2, heal: 6, radius: 6.0f, maxTargets: 4, knockback: 0.8f, triggerHp: 0.76f,
                buffTicks: 360, healBuff: 0.06f, cdrBuff: 3, partyWide: true),
'@
$guntherIdentity = @'
            ["Gus"] = I("Gus", "HOT PLATE", CharacterSignatureArchetype.Hybrid, 780, 690,
                damage: 2, heal: 6, radius: 6.0f, maxTargets: 4, knockback: 0.8f, triggerHp: 0.76f,
                buffTicks: 360, healBuff: 0.06f, cdrBuff: 3, partyWide: true),

            // Control / Support. Museum knowledge becomes a safe crowd-control seal.
            ["Gunther"] = I("Gunther", "RELIC SEAL", CharacterSignatureArchetype.Control, 720, 630,
                damage: 4, radius: 5.5f, maxTargets: 4, stun2: 620, stun3: 960, knockback: 0.8f,
                buffTicks: 300, controlBuff: 0.08f, cdrBuff: 3, partyWide: true),
'@
ReplaceOnce $identities $gusIdentity $guntherIdentity 'Gunther signature identity'

$gusVisualAnchor = @'
        int healthBefore = target.Health;
'@
$gusVisual = @'
        // Alpha 6.7.1: support/healer Gus used to deal invisible generic damage while standing
        // perfectly still at range. Give Hot Plate a lightweight animated combat tell without
        // changing his damage budget or stealing sprite/controller authority.
        if (npc.Name.Equals("Gus", StringComparison.OrdinalIgnoreCase))
        {
            Color hotPlateColor = new(255, 190, 90);
            SpawnBurst(FarmerContext.currentLocation, npc.Position + new Vector2(16f, -8f), hotPlateColor, 5, 22f);
            if (GetCooldown(_signatureCooldowns, npc.Name) <= 0)
            {
                npc.showTextAboveHead("HOT PLATE", hotPlateColor, 2, 700, 0);
                FarmerContext.currentLocation.playSound("yoba");
                _signatureCooldowns[npc.Name] = 180;
            }
        }

        int healthBefore = target.Health;
'@
ReplaceOnce $combat $gusVisualAnchor $gusVisual 'Gus visible combat tell'

$defaultRecruitAnchor = @'
  "recruit.invite": "Recruit to Party",
'@
$defaultRecruitNew = @'
  "recruit.invite": "Recruit to Party",
  "recruit.invite-only": "{{name}} only",
  "recruit.invite-together": "{{name}} + {{companion}}",
  "party.full-hud": "TEAM UP PARTY FULL • {{max}}/{{max}} people",
'@
ReplaceOnce $defaultI18n $defaultRecruitAnchor $defaultRecruitNew 'English recruit localization'

$viRecruitAnchor = @'
  "recruit.invite": "Thu nạp vào Party",
'@
$viRecruitNew = @'
  "recruit.invite": "Thu nạp vào Party",
  "recruit.invite-only": "Chỉ {{name}}",
  "recruit.invite-together": "{{name}} + {{companion}}",
  "party.full-hud": "TEAM UP ĐÃ ĐẦY • {{max}}/{{max}} người",
'@
ReplaceOnce $viI18n $viRecruitAnchor $viRecruitNew 'Vietnamese recruit localization'

$defaultGusAnchor = @'
  "codex.gus.ability": "Hot Plate: small recovery paired with a mid-range counterattack.",
'@
$defaultGunther = @'
  "codex.gus.ability": "Hot Plate: small recovery paired with a mid-range counterattack.",
  "codex.gunther.passive": "Curator's Eye: improves control value when enemies gather into a cluster.",
  "codex.gunther.ability": "Relic Seal: seals a group of enemies with light damage and a reliable stun.",
'@
ReplaceOnce $defaultI18n $defaultGusAnchor $defaultGunther 'English Gunther profile text'

$viGusAnchor = @'
  "codex.gus.ability": "Hot Plate: hồi phục nhỏ kèm một đòn phản công tầm trung.",
'@
$viGunther = @'
  "codex.gus.ability": "Hot Plate: hồi phục nhỏ kèm một đòn phản công tầm trung.",
  "codex.gunther.passive": "Curator's Eye: tăng hiệu quả Control khi kẻ địch tụ thành nhóm.",
  "codex.gunther.ability": "Relic Seal: phong ấn một cụm quái, gây sát thương nhẹ và làm choáng ổn định.",
'@
ReplaceOnce $viI18n $viGusAnchor $viGunther 'Vietnamese Gunther profile text'

# The strings above are single-quoted PowerShell here-strings. Remove only the JSON transport
# escape artifact if this file was created through an API that preserved backslashes before quotes.
foreach ($path in @($alpha661, $alpha663, $alpha6626, $nativeBridge, $profiles, $identities, $combat, $defaultI18n, $viI18n)) {
    $text = ReadText $path
    if ($text.Contains('\"'))
        WriteText $path ($text.Replace('\"', '"'))
}

$projectText = ReadText $project
$banterText = ReadText $banter
$alpha661Text = ReadText $alpha661
$alpha663Text = ReadText $alpha663
$alpha6626Text = ReadText $alpha6626
$bridgeText = ReadText $nativeBridge
$profilesText = ReadText $profiles
$identitiesText = ReadText $identities
$combatText = ReadText $combat
$viText = ReadText $viI18n
$allSource = (Get-ChildItem $src -Recurse -Filter '*.cs' | ForEach-Object { ReadText $_.FullName }) -join "`n"

Require ($projectText.Contains('<Version>0.2.0-alpha.6.7.1</Version>')) '6.7.1 version missing.'
Require ($banterText.Contains('new Color(72, 42, 28)')) 'Readable banter color missing.'
Require (-not $alpha661Text.Contains('6/6 people')) 'Legacy 6/6 party full text remains.'
Require ($alpha661Text.Contains('FindRecruitCandidateCompanionAlpha671')) 'Dormant recruit lookup not wired.'
Require ($alpha661Text.Contains('recruit.invite-only')) 'Localized recruit menu not wired.'
Require ($alpha663Text.Contains('TrySetVillagerCompanionEnabled(owner.Name, includeCompanion')) 'Native recruit choice control missing.'
Require ($bridgeText.Contains('TryGetConfiguredVillagerCompanionDescriptor')) 'Configured dormant companion bridge missing.'
Require ($bridgeText.Contains('TryGetConfiguredCompanion')) 'Verified Pelipper configured method missing.'
Require ($alpha6626Text.Contains('IsDormantConfiguredReservationAlpha671')) 'Dormant authority reservation missing.'
Require ($profilesText.Contains('["Gunther"] = P(')) 'Gunther profile missing.'
Require ($identitiesText.Contains('["Gunther"] = I(')) 'Gunther identity missing.'
Require ($combatText.Contains('HOT PLATE')) 'Gus visible combat feedback missing.'
Require ($viText.Contains('"recruit.invite-only": "Chỉ {{name}}"')) 'Vietnamese recruit UI fix missing.'
Require ($viText.Contains('codex.gunther.passive')) 'Vietnamese Gunther profile text missing.'
Require ($allSource.IndexOf('PelipperRenderSuppressedAlpha6613') -lt 0) 'Legacy Pelipper render suppression returned.'
Require ($allSource.IndexOf('TrySetActorInvisibleAlpha6613') -lt 0) 'Legacy Pelipper visibility writer returned.'

Log 'Building Alpha 6.7.1 live polish...'
Log 'UI: dark readable banter text + localized recruit menu.'
Log 'PARTY: hard messaging 5/5 people; companion pool remains 2/2.'
Log 'PELIPPER: configured dormant villager companion detection + reservation + native NPC-only disable.'
Log 'GUNTHER: completed Control/Support combat profile + Relic Seal identity.'
Log 'GUS: visible Hot Plate combat tell without changing damage budget.'

dotnet build (Join-Path $src 'TeamUp.csproj') -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

$dll = Join-Path $src 'bin/Release/net6.0/TeamUp.dll'
Require (Test-Path $dll) 'TeamUp.dll missing.'
$dllAscii = (& strings $dll) -join "`n"
$dllUtf16 = (& strings -el $dll) -join "`n"
$dllStrings = $dllAscii + "`n" + $dllUtf16
Require ($dllStrings.Contains('TryGetConfiguredVillagerCompanionDescriptor')) 'Dormant configured bridge absent from DLL.'
Require ($dllStrings.Contains('TryGetConfiguredCompanion')) 'Pelipper configured method token absent from DLL.'
Require ($dllStrings.Contains('IsDormantConfiguredReservationAlpha671')) 'Dormant reservation absent from DLL.'
Require ($dllStrings.Contains('Gunther')) 'Gunther profile absent from DLL.'
Require ($dllStrings.Contains('RELIC SEAL')) 'Gunther Relic Seal absent from DLL.'
Require ($dllStrings.Contains('HOT PLATE')) 'Gus Hot Plate feedback absent from DLL.'
Require (-not $dllStrings.Contains('TEAM UP PARTY FULL • 6/6 people')) 'Legacy 6/6 message remains in DLL.'
Require (-not $dllStrings.Contains('PelipperRenderSuppressedAlpha6613')) 'Legacy render suppression returned in DLL.'

New-Item -ItemType Directory -Force -Path $stageMod | Out-Null
Copy-Item $dll (Join-Path $stageMod 'TeamUp.dll') -Force
$manifestText = (ReadText (Join-Path $src 'manifest.json')).Replace('%ProjectVersion%', $version)
WriteText (Join-Path $stageMod 'manifest.json') $manifestText
Copy-Item (Join-Path $src 'i18n') (Join-Path $stageMod 'i18n') -Recurse -Force

if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $stageMod -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
WriteText $shaPath "$hash  $zipName`n"
Log 'SOURCE ACCEPTANCE: PASS'
Log 'BINARY ACCEPTANCE: PASS'
Log 'BUILD SUCCESS - ALPHA 6.7.1'
Log "ZIP: $zipName"
Log "SHA256: $hash"
Write-Host 'BUILD SUCCESS - ALPHA 6.7.1'
Write-Host "ZIP: $zip"
Write-Host "SHA256: $hash"
