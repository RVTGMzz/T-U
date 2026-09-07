# Team Up! current chat handoff — v0.2 Alpha 6.2.0

Date: 2026-09-03
Branch: `v0.2-alpha6-2-expansion-skills-wave1`
Base checkpoint: `v0.2-alpha6-1-cardcha-test-bridge-debug-presets` / Alpha 6.1.3

## Current checkpoint

Target build: `0.2.0-alpha.6.2.0`
Expected ZIP:
`release/TeamUp_v0.2.0-alpha.6.2.0_EXPANSION_SKILLS_WAVE1_TEST.zip`

Expected SMAPI marker:
`Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.2.0`

This checkpoint is NOT compile-verified yet. User must download a fresh source ZIP from this branch and run `BUILD_V0_2_ALPHA6.bat`. Never claim compile-clean until the user shows a successful build/log.

## Goal of Alpha 6.2.0

Port the previously approved expansion-NPC Codex work from Alpha 3.5/3.6 onto the current Alpha 6.1.3 gameplay baseline, then make the first curated SVE/RSV signatures real runtime skills rather than Codex-only descriptions.

Alpha 6.1.3 fixes remain the inherited foundation:
- ChaCha hard exclusion from Main Party
- Alex Tank approach-before-local-TAUNT
- dedicated EquipmentMenu
- Party Vault category/layout overlap fix
- Codex focus memory and dialogue hint restoration
- Follow anti-thrash
- Farmer-through-party collision
- Cardcha optional debug arena bridge + `teamup_test`

## Expansion source provenance

`NpcProfileCatalog` now merges vanilla profiles with `ExpansionNpcProfileCatalog`.

Source labels:
- `Stardew Valley`
- `Stardew Valley Expanded`
- `Ridgeside Village`

Mod IDs retained from the previously approved Alpha 3.5 implementation:
- SVE CP: `FlashShifter.StardewValleyExpandedCP`
- SVE code: `FlashShifter.SVECode`
- RSV CP: `Rafseazz.RSVCP`

Codex uses `NpcProfileCatalog.GetAvailableProfiles(Helper.ModRegistry)` after the Alpha 6.2 integration pass.
Expansion rows appear only when the corresponding mod is loaded AND Stardew has materialized that NPC in the current save. This avoids ghost Codex rows.

Expansion NPCs that have not received a curated combat pass remain `Unassigned` shells. Recruiting those shells must NOT auto-force an Unassigned role; the player can choose a role manually.

## Wave 1 curated profiles + real skills

### Stardew Valley Expanded — 12

1. Alesia — Damage / Tank — `METEOR BREAK`
   - AoE burst around a wounded/clustered target.
2. Andy — Tank / Support — `FENCE-LINE CHARGE`
   - local damage + knockback + strong threat; Tier 3 self recovery.
3. Camilla — Control / Damage — `HEX BLOOM`
   - clustered magical damage + stun.
4. Claire — Support / Healer — `SECOND TAKE`
   - focused emergency recovery; stronger/secondary recovery at Tier 3.
5. Isaac — Damage / Tank — `EXECUTION ARC`
   - finishing strike against a low-health nearby monster.
6. Jadu — Control / Support — `RUNIC BIND`
   - clustered control field and stun.
7. Lance — Damage / Control — `HIGHLAND BURST`
   - damage + displacement + short control.
8. Martin — Support / Damage — `QUICK ASSIST`
   - heals an injured ally and/or disrupts pressure near Farmer.
9. Morgan — Control / Support — `ASTRAL SNARE`
   - clustered control field.
10. Olivia — Support / Control — `VINTAGE RALLY`
    - party recovery rally.
11. Sophia — Support / Damage — `HEROIC SCENE`
    - hybrid heal + light area damage.
12. Victor — Control / Support — `CALCULATED FIELD`
    - tactical clustered stun/control field.

### Ridgeside Village — 12

1. Aguar — Control / Support — `SPIRIT SEAL`
2. Blair — Damage / Support — `RISING STRIKE`
3. Carmen — Support / Healer — `WARM SHELTER`
4. Daia — Damage / Control — `PREDATOR STEP`
5. Ian — Tank / Damage — `SHOULDER THROUGH`
6. Jio — Damage / Control — `SHADOW CUT`
7. June — Support / Control — `RESONANT CHORD`
   - party recovery + nearby enemy stun.
8. Kenneth — Control / Support — `STATIC LOCK`
9. Kiarra — Damage / Support — `BRIGHT RUSH`
10. Maddie — Healer / Support — `SAFE HAVEN`
11. Shiro — Tank / Damage — `GUARDIAN BREAK`
    - local damage + knockback + high threat; Tier 3 self recovery.
12. Ysabelle — Support / Control — `SPOTLIGHT TEMPO`
    - ally recovery + nearby control.

## Signature tier rules

Expansion signatures intentionally use the same Alpha 6 progression thresholds:
- Tier 1: base role combat only
- Tier 2 signature: Character Lv10 OR active-role Mastery 4
- Tier 3 signature: Character Lv20 OR active-role Mastery 8

Signature only fires when the NPC is currently assigned to the profile's Primary or Secondary role.
Cooldown is scaled by existing Role Mastery + equipment cooldown math.
Successful signature use awards normal Character XP + Role Mastery XP.
No new save fields are added.

## Runtime architecture

New source:
- `src/TeamUp/Combat/ExpansionSkillService.cs`
- `src/TeamUp/Core/ExpansionNpcProfileCatalog.cs`

Updated source:
- `src/TeamUp/Core/NpcProfileCatalog.cs`

Build-time integration helper:
- `_build_support/IntegrateExpansionSkillsWave1.ps1`
- `_build_support/ExpansionSkillsWave1Translations.json`

The expansion skill service is owned by `CombatService` and reuses the existing:
- `ProgressionService`
- `ThreatService`
- active party roster
- live Monster list

The integration helper runs AFTER `FixAlpha613PartyUxCombat.ps1`, so the Alpha 6.1.3 Tank/ChaCha/Equipment/Vault corrections remain authoritative.

For offensive/Tank/Control expansion signatures, Team Up's internal threat table receives real threat values. This still does NOT mean every vanilla monster subclass is globally Harmony-retargeted. Preserve the existing architecture statement about threat/guard logic.

## Build chain for Alpha 6.2.0

User still launches:
`BUILD_V0_2_ALPHA6.bat`

The BAT now calls:
`BuildV0_2Alpha62.ps1`

The wrapper preserves the existing `BuildV0_2Alpha6.ps1` chain, generates an Alpha 6.2 build script in memory/on disk temporarily, then inserts:
`IntegrateExpansionSkillsWave1.ps1`
AFTER the Alpha 6.1.3 consolidated fixer and BEFORE source verification / dotnet build.

The wrapper also advances:
- build/package version -> `0.2.0-alpha.6.2.0`
- ZIP/SHA filename -> `EXPANSION_SKILLS_WAVE1_TEST`
- debug marker -> 6.2.0
- packaged smoke checklist -> `SMOKE_TEST_V0_2_ALPHA6_2_EXPANSION_SKILLS_VI.txt`

Important Unicode safety:
The expansion integration does NOT deserialize/reserialize the existing `vi.json`, because doing so would destroy the ASCII `\uXXXX` spelling required by the inherited Alpha 6.1.3 verification gates. It inserts only new expansion keys before `common.back`, escaping non-ASCII characters to JSON `\uXXXX` sequences.

## Highest-priority next action

1. Download a fresh source ZIP from branch `v0.2-alpha6-2-expansion-skills-wave1`.
2. Run `BUILD_V0_2_ALPHA6.bat`.
3. If it fails, send the COMPLETE `BUILD_LOG.txt` and fix all compiler/helper errors in one pass.
4. Do not claim build success until the user provides the successful build output/log.

## Recommended smoke order after successful build

1. Regression safety first:
   - ChaCha no Recruit
   - Alex approaches monster before local TAUNT
   - Equipment dedicated panel
   - Party Vault category line visible
2. Codex Source:
   - SVE filter appears only with SVE installed/live NPCs
   - RSV filter appears only with RSV installed/live NPCs
3. SVE quick skill test:
   - Claire Lv20 Support M8 -> `SECOND TAKE`
   - Camilla Lv20 Control M8 -> `HEX BLOOM`
   - Lance Lv20 Damage M8 -> `HIGHLAND BURST`
4. RSV quick skill test:
   - June Lv20 Support M8 -> `RESONANT CHORD`
   - Shiro Lv20 Tank M8 -> `GUARDIAN BREAK`
   - Maddie Lv20 Healer M8 -> `SAFE HAVEN`
5. Use `SMOKE_TEST_V0_2_ALPHA6_2_EXPANSION_SKILLS_VI.txt` for the full checklist.

Useful debug examples:
`teamup_test add Claire`
`teamup_test level Claire 20`
`teamup_test mastery Claire support 8`

`teamup_test add Shiro`
`teamup_test level Shiro 20`
`teamup_test mastery Shiro tank 8`

## Architecture locks

- Team Up remains standalone UniqueID `Ronvotri.TeamUp`.
- SVE and RSV are optional NPC sources, not hard dependencies.
- Cardcha remains optional test host only; no Cardcha gameplay dependency.
- ChaCha remains Special/Farmer Companion and cannot enter Main Party.
- Main Party remains normal human/villager recruitment under `CompanionClassificationService`.
- Expansion NPCs must exist in the live save before their Codex row is offered.
- Do not hard-code broad Pelipper Town/Pokemon behavior into Team Up core.
- Do not claim universal monster visual AI redirection from Team Up threat tables.
