from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TeamUp"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")


def replace_once(path: Path, old: str, new: str, label: str) -> None:
    text = read(path)
    if new in text:
        return
    if old not in text:
        raise RuntimeError(f"Missing patch anchor: {label} ({path})")
    write(path, text.replace(old, new, 1))


project = SRC / "TeamUp.csproj"
banter = SRC / "Core" / "PartyBanterService.cs"
alpha661 = SRC / "ModEntry.Alpha661.cs"
alpha663 = SRC / "ModEntry.Alpha663.cs"
alpha6626 = SRC / "ModEntry.Alpha6626.cs"
native_bridge = SRC / "Core" / "PelipperTown119NativeBridge.cs"
profiles = SRC / "Core" / "NpcProfileCatalog.cs"
identities = SRC / "Core" / "CharacterSkillIdentityCatalog.cs"
combat = SRC / "Combat" / "CombatService.cs"
default_i18n = SRC / "i18n" / "default.json"
vi_i18n = SRC / "i18n" / "vi.json"

replace_once(
    project,
    '<Version>0.2.0-alpha.6.7.0</Version>',
    '<Version>0.2.0-alpha.6.7.1</Version>',
    'project version',
)

replace_once(
    banter,
    'new Color(245, 235, 205)',
    'new Color(72, 42, 28)',
    'banter readable text color',
)

replace_once(
    alpha661,
    'LiveCompanionDescriptor? detectedCompanion = CompanionIntegrationService.FindLinkedCompanion(npc);',
    'LiveCompanionDescriptor? detectedCompanion = FindRecruitCandidateCompanionAlpha671(npc);',
    'authoritative recruit candidate lookup',
)
replace_once(
    alpha661,
    'LiveCompanionDescriptor? companion = CompanionIntegrationService.FindLinkedCompanion(npc);',
    'LiveCompanionDescriptor? companion = FindRecruitCandidateCompanionAlpha671(npc);',
    'recruit menu dormant lookup',
)
replace_once(
    alpha661,
    'SendActionResult(responsePlayerId, false, "TEAM UP PARTY FULL • 6/6 people");',
    'SendActionResult(responsePlayerId, false, Helper.Translation.Get("party.full-hud", new { max = Config.MaxPartyMembers }));',
    '5/5 party full HUD',
)
replace_once(
    alpha661,
    'new("InviteOnly", $"{npc.displayName} only"),',
    'new("InviteOnly", Helper.Translation.Get("recruit.invite-only", new { name = npc.displayName })),',
    'localized invite-only label',
)
replace_once(
    alpha661,
    'new("InviteTogether", $"{npc.displayName} + {companion.DisplayName}"),',
    'new("InviteTogether", Helper.Translation.Get("recruit.invite-together", new { name = npc.displayName, companion = companion.DisplayName })),',
    'localized invite-together label',
)
replace_once(
    alpha661,
    '$"Invite {npc.displayName} to Team Up?",',
    'Helper.Translation.Get("recruit.question", new { name = npc.displayName }),',
    'localized together recruit title',
)

replace_once(
    alpha663,
    '''        PelipperTownCompatibilityService.SetOwnerOptOut(owner, !includeCompanion);\n''',
    '''        PelipperTownCompatibilityService.SetOwnerOptOut(owner, !includeCompanion);\n\n        // Alpha 6.7.1: recruitment controls Pelipper at the source even while the configured\n        // partner actor is asleep/dormant. NPC-only therefore cannot auto-spawn later.\n        if (PelipperTown119NativeBridge.HasVillagerLifecycle)\n            PelipperTown119NativeBridge.TrySetVillagerCompanionEnabled(owner.Name, includeCompanion, out _);\n''',
    'native dormant NPC recruit choice',
)

bridge_anchor = '''    public static bool TryIsVillagerCompanionConfiguredEnabled(string npcName, out bool enabled)\n'''
bridge_method = '''    /// <summary>\n    /// Alpha 6.7.1: asks Pelipper 1.1.9 for the configured villager partner even when its runtime\n    /// actor is dormant. Verified DLL signature:\n    /// TryGetConfiguredCompanion(string, out SpeciesDefinition, out bool) -> bool.\n    /// This descriptor is recruitment intent only and must never be treated as source-live truth.\n    /// </summary>\n    public static bool TryGetConfiguredVillagerCompanionDescriptor(string npcName, out LiveCompanionDescriptor? descriptor)\n    {\n        descriptor = null;\n        object? manager = VillagerManager ?? (RuntimeRoot is null ? null : ResolveVillagerManager(RuntimeRoot));\n        if (manager?.GetType().FullName != "PelipperTown.VillagerCompanionManager"\n            || string.IsNullOrWhiteSpace(npcName))\n        {\n            return false;\n        }\n\n        if (TryIsVillagerCompanionConfiguredEnabled(npcName, out bool enabled) && !enabled)\n            return true;\n\n        MethodInfo? method = manager.GetType().GetMethods(InstanceFlags)\n            .FirstOrDefault(candidate =>\n            {\n                if (!candidate.Name.Equals("TryGetConfiguredCompanion", StringComparison.Ordinal))\n                    return false;\n                ParameterInfo[] parameters = candidate.GetParameters();\n                return candidate.ReturnType == typeof(bool)\n                    && parameters.Length == 3\n                    && parameters[0].ParameterType == typeof(string)\n                    && parameters[1].ParameterType.IsByRef\n                    && parameters[2].ParameterType == typeof(bool).MakeByRefType();\n            });\n        if (method is null)\n            return false;\n\n        try\n        {\n            object?[] args = { npcName, null, false };\n            if (method.Invoke(manager, args) is not bool found || !found || args[1] is null)\n                return true;\n\n            object species = args[1]!;\n            Type speciesType = species.GetType();\n            string speciesId = SafeGet(() => speciesType.GetProperty("Id", InstanceFlags)?.GetValue(species)?.ToString()) ?? string.Empty;\n            string displayName = SafeGet(() => speciesType.GetProperty("DisplayName", InstanceFlags)?.GetValue(species)?.ToString()) ?? speciesId;\n            if (string.IsNullOrWhiteSpace(speciesId))\n                return false;\n            if (string.IsNullOrWhiteSpace(displayName))\n                displayName = speciesId;\n\n            string providerUnitId = $"configured:npc:{npcName}:{speciesId}";\n            descriptor = new LiveCompanionDescriptor\n            {\n                UnitId = $"{PelipperTownCompatibilityService.ProviderId}:{providerUnitId}",\n                CharacterName = speciesId,\n                DisplayName = displayName,\n                OwnerKind = CompanionOwnerKind.PartyMember,\n                OwnerCharacterName = npcName,\n                ProviderId = PelipperTownCompatibilityService.ProviderId,\n                ProviderUnitId = providerUnitId\n            };\n            return true;\n        }\n        catch\n        {\n            return false;\n        }\n    }\n\n'''
text = read(native_bridge)
if 'TryGetConfiguredVillagerCompanionDescriptor' not in text:
    if bridge_anchor not in text:
        raise RuntimeError('Missing configured-companion bridge anchor')
    write(native_bridge, text.replace(bridge_anchor, bridge_method + bridge_anchor, 1))

replace_once(
    alpha6626,
    '''                if (liveKeys.Contains(key))\n                    continue;\n\n                if (IsSlotReservedAlpha6617(unit.State))\n''',
    '''                if (liveKeys.Contains(key) || IsDormantConfiguredReservationAlpha671(unit, online))\n                    continue;\n\n                if (IsSlotReservedAlpha6617(unit.State))\n''',
    'preserve dormant configured slot reservation',
)
replace_once(
    alpha6626,
    '''            int availablePelipperSlots = Math.Max(0, max - Math.Min(max, nonPelipper.Count));\n''',
    '''            // Alpha 6.7.1: an explicitly selected NPC + Pokemon reserves capacity even while\n            // Pelipper keeps that Pokemon asleep/dormant. Trim impossible old-save overflow first.\n            int dormantBudget = Math.Max(0, max - Math.Min(max, nonPelipper.Count));\n            List<CompanionUnitData> dormantPelipper = Party.CompanionUnits\n                .Where(unit => IsDormantConfiguredReservationAlpha671(unit, online))\n                .ToList();\n            for (int i = dormantBudget; i < dormantPelipper.Count; i++)\n            {\n                CompanionUnitData overflow = dormantPelipper[i];\n                changed |= Party.SetCompanionState(overflow.UnitId, overflow.RecruiterId, CompanionDeploymentState.Standby);\n                if (repairOverflow)\n                    RequestAuthorityReturnAlpha6626(overflow, "dormant configured 2/2 overflow");\n            }\n\n            int activeDormantReservations = dormantPelipper\n                .Take(dormantBudget)\n                .Count(unit => IsSlotReservedAlpha6617(unit.State));\n            int availablePelipperSlots = Math.Max(0,\n                max - Math.Min(max, nonPelipper.Count) - activeDormantReservations);\n''',
    'dormant slot budget',
)

replace_once(
    profiles,
    '''            ["Gus"] = P("Gus", PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 3, 2, 5, 4, 2),\n''',
    '''            ["Gus"] = P("Gus", PartyRole.Support, PartyRole.Healer, EngagementStyle.Balanced, 3, 2, 5, 4, 2),\n            ["Gunther"] = P("Gunther", PartyRole.Control, PartyRole.Support, EngagementStyle.Cautious, 2, 2, 4, 1, 5),\n''',
    'Gunther completed combat profile',
)
replace_once(
    identities,
    '''            ["Gus"] = I("Gus", "HOT PLATE", CharacterSignatureArchetype.Hybrid, 780, 690,\n                damage: 2, heal: 6, radius: 6.0f, maxTargets: 4, knockback: 0.8f, triggerHp: 0.76f,\n                buffTicks: 360, healBuff: 0.06f, cdrBuff: 3, partyWide: true),\n''',
    '''            ["Gus"] = I("Gus", "HOT PLATE", CharacterSignatureArchetype.Hybrid, 780, 690,\n                damage: 2, heal: 6, radius: 6.0f, maxTargets: 4, knockback: 0.8f, triggerHp: 0.76f,\n                buffTicks: 360, healBuff: 0.06f, cdrBuff: 3, partyWide: true),\n\n            // Control / Support. Museum knowledge becomes a safe crowd-control seal.\n            ["Gunther"] = I("Gunther", "RELIC SEAL", CharacterSignatureArchetype.Control, 720, 630,\n                damage: 4, radius: 5.5f, maxTargets: 4, stun2: 620, stun3: 960, knockback: 0.8f,\n                buffTicks: 300, controlBuff: 0.08f, cdrBuff: 3, partyWide: true),\n''',
    'Gunther signature identity',
)
replace_once(
    combat,
    '''        int healthBefore = target.Health;\n''',
    '''        // Alpha 6.7.1: support/healer Gus used to deal invisible generic damage while standing\n        // perfectly still at range. Give Hot Plate a lightweight animated combat tell without\n        // changing his damage budget or stealing sprite/controller authority.\n        if (npc.Name.Equals("Gus", StringComparison.OrdinalIgnoreCase))\n        {\n            Color hotPlateColor = new(255, 190, 90);\n            SpawnBurst(FarmerContext.currentLocation, npc.Position + new Vector2(16f, -8f), hotPlateColor, 5, 22f);\n            if (GetCooldown(_signatureCooldowns, npc.Name) <= 0)\n            {\n                npc.showTextAboveHead("HOT PLATE", hotPlateColor, 2, 700, 0);\n                FarmerContext.currentLocation.playSound("yoba");\n                _signatureCooldowns[npc.Name] = 180;\n            }\n        }\n\n        int healthBefore = target.Health;\n''',
    'Gus visible combat tell',
)

replace_once(
    default_i18n,
    '  "recruit.invite": "Recruit to Party",\n',
    '  "recruit.invite": "Recruit to Party",\n  "recruit.invite-only": "{{name}} only",\n  "recruit.invite-together": "{{name}} + {{companion}}",\n  "party.full-hud": "TEAM UP PARTY FULL • {{max}}/{{max}} people",\n',
    'English recruit localization',
)
replace_once(
    vi_i18n,
    '  "recruit.invite": "Thu nạp vào Party",\n',
    '  "recruit.invite": "Thu nạp vào Party",\n  "recruit.invite-only": "Chỉ {{name}}",\n  "recruit.invite-together": "{{name}} + {{companion}}",\n  "party.full-hud": "TEAM UP ĐÃ ĐẦY • {{max}}/{{max}} người",\n',
    'Vietnamese recruit localization',
)
replace_once(
    default_i18n,
    '  "codex.gus.ability": "Hot Plate: small recovery paired with a mid-range counterattack.",\n',
    '  "codex.gus.ability": "Hot Plate: small recovery paired with a mid-range counterattack.",\n  "codex.gunther.passive": "Curator\'s Eye: improves control value when enemies gather into a cluster.",\n  "codex.gunther.ability": "Relic Seal: seals a group of enemies with light damage and a reliable stun.",\n',
    'English Gunther profile text',
)
replace_once(
    vi_i18n,
    '  "codex.gus.ability": "Hot Plate: hồi phục nhỏ kèm một đòn phản công tầm trung.",\n',
    '  "codex.gus.ability": "Hot Plate: hồi phục nhỏ kèm một đòn phản công tầm trung.",\n  "codex.gunther.passive": "Curator\'s Eye: tăng hiệu quả Control khi kẻ địch tụ thành nhóm.",\n  "codex.gunther.ability": "Relic Seal: phong ấn một cụm quái, gây sát thương nhẹ và làm choáng ổn định.",\n',
    'Vietnamese Gunther profile text',
)

print('Alpha 6.7.1 source materialized successfully.')
