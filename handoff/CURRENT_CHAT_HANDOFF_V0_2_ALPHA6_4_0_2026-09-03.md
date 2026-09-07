# Team Up v0.2.0-alpha.6.4.0 handoff

Date: 2026-09-03
Branch: `v0.2-alpha6-4-character-skill-identity`
Status: **COMPILE-VERIFIED PASS**

## Verified build

GitHub Actions workflow: `Team Up v0.2.0-alpha.6.4.0 Character Skill Identity`
Run: `33736194047`
Build-trigger SHA: `62ccd5fb88f728e90201687618898bf6465bc793`
Materialized source SHA: `a4c2a19` (full SHA available from branch history)

CI result:
- Build Alpha 6.4.0 end-to-end: PASS
- Source acceptance: PASS
- Package verification: PASS
- Materialize generated source: PASS
- Artifact upload: PASS

`dotnet build`:
- Build succeeded.
- 0 Warning(s)
- 0 Error(s)
- Time Elapsed 00:00:03.83

Package:
`release/TeamUp_v0.2.0-alpha.6.4.0_CHARACTER_SKILL_IDENTITY_TEST.zip`

Inner mod ZIP SHA-256:
`0246f229176ac19956aa21d28440f06c33abbe7bda7d0ba6c32c6ce7457c66d7`

Workflow artifact:
- ID: `9885834331`
- Name: `team-up-alpha6-4-character-skill-identity`
- Artifact digest: `sha256:aee980ac9facdc42e1c260ea6ecd54888170b0118a390c343f68a83bcc8acc50`

Expected SMAPI marker:
`Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.4.0`

## Alpha 6.4.0 scope

Alpha 6.4.0 finishes runtime skill identity for the 24 vanilla NPC profiles that were not part of the five Alpha 6 signature prototypes.

Existing prototype owners remain unchanged:
- Abigail
- Alex
- Harvey
- Maru
- Emily

New completed vanilla identities:
- Caroline
- Clint
- Demetrius
- Elliott
- Evelyn
- George
- Gus
- Haley
- Jodi
- Kent
- Leah
- Lewis
- Linus
- Marnie
- Pam
- Penny
- Pierre
- Robin
- Sam
- Sandy
- Sebastian
- Shane
- Willy
- Wizard

## Locked design rules

### Role budget
Every new identity uses the locked design budget:
- 70% Primary Role
- 30% Secondary Role

The catalog validates this rule at source/runtime initialization.

### Signature archetypes
Runtime supports seven reusable mechanic families while keeping per-character tuning unique:
- Recovery
- Guard
- Control
- Rally
- Burst
- Sweep
- Hybrid

Each NPC owns an individual signature name, cooldown, radius, target cap, damage/heal/control tuning, knockback, trigger condition, and optional temporary buff package.

### Temporary buffs
`ProgressionService` now owns runtime-only temporary combat modifiers:
- damage bonus
- defense bonus
- healing bonus
- control bonus
- cooldown reduction

Safety caps:
- single configured damage/heal/control buff <= 12%
- single defense buff <= +3
- single cooldown buff <= 5 percentage points
- combined runtime damage/heal/control bonus capped at 25%
- runtime defense aggregation capped at +6
- runtime cooldown aggregation capped at 10 points
- total cooldown reduction capped at 45%
- modifier duration capped at 900 ticks

No new `PartyMemberData` save fields were added. Buffs clear on lifecycle reset and are intentionally non-persistent.

## Skill identity examples

- Caroline: `TEA BREAK` - Support/Healer hybrid recovery + light sustain/CDR utility.
- Clint: `FORGE HAMMER` - Tank/Control front-line smash with displacement and stun.
- Demetrius: `SPECIMEN TRAP` - Control/Support area lockdown.
- Elliott: `ROUSING VERSE` - Support/Damage rally with light party tempo.
- Evelyn: `GARDEN REMEDY` - Healer/Support recovery specialist.
- Haley: `FLASH SHOT` - Damage/Support fast focused burst.
- Kent: `COVERING STRIKE` - Tank/Damage threat punishment + brace.
- Leah: `WOODLAND SWEEP` - Damage/Control multi-target sweep.
- Linus: `WILD SNARE` - Support/Control area snare + cooldown utility.
- Pam: `ROADHOUSE RUSH` - Tank/Damage aggressive sweep.
- Penny: `SECOND WIND` - Healer/Support emergency recovery + tempo.
- Robin: `HAMMER BRACE` - Tank/Support party brace + displacement.
- Sam: `POWER CHORD` - Damage/Support quick area burst + cooldown rhythm.
- Sebastian: `SHADOW PIN` - Control/Damage focused single-target disable.
- Shane: `HAYMAKER` - Damage/Tank heavy single-target hit + brief brace.
- Wizard: `ARCANE BURST` - Control/Damage area magical lockdown.

## Signature icons

Alpha 6.3.2 one-icon rule remains locked:
- Passive: text-only
- Signature: exactly one icon

Alpha 6.4.0 adds bespoke 8x8 runtime signature silhouettes for all 24 newly completed vanilla kits.
Existing bespoke icons remain for:
- five vanilla Alpha 6 prototypes
- 12 SVE Wave 1 NPCs
- 12 RSV Wave 1 NPCs

Procedural fallback remains for NPCs whose official Team Up kit is not complete.

## Equipment integration

`EquipmentRpgPolishService` now knows base signature cooldowns for all newly completed vanilla identities, so Alpha 6.3.1 equipment hover can preview Signature CD and CDR impact for them.

Inherited Alpha 6.3.1 features remain:
- rarity frames
- role score
- direct combat impact
- Auto Equip / Best Gear

## Regression locks retained

- UniqueID remains `Ronvotri.TeamUp`.
- Cardcha is optional test integration only.
- ChaCha never enters Main Party.
- Alex/Tank approaches before TAUNT.
- Team Up threat does not claim universal vanilla monster retargeting.
- Party Vault category layout fix remains.
- Farmer passes through Team Up party members.
- SVE/RSV Wave 1 runtime signatures remain active.
- Alpha 6.3.2 Signature icon UI remains one-icon only.

## Test checklist

Use:
`SMOKE_TEST_V0_2_ALPHA6_4_0_CHARACTER_SKILL_IDENTITY_VI.txt`

Focus first on:
1. Tier 2 and Tier 3 activation for new vanilla NPCs.
2. Tank vs DPS vs Heal/Support vs Control class feel.
3. Temporary buff expiry and non-stacking safety.
4. Signature icon differences in Character Profile.
5. Equipment Signature CD preview.
6. Alpha 6.2 / 6.3.1 / 6.3.2 regression.

## Next planned checkpoint

`Alpha 6.4.1 - Friendship & Bond`

Planned relationship rules already agreed but **not implemented in 6.4.0**:
- 4 hearts: Trusted, light Character XP / Mastery benefit
- 8 hearts: Close Companion, AI synergy improvement
- 10 hearts: Signature Affinity bonus around 5% by role
- spouse: role-aware Bond Trait
- 14 hearts: NPC-specific Soulmate Trait

Marriage must add utility/synergy, not make a spouse universally stronger than non-spouse party choices.
