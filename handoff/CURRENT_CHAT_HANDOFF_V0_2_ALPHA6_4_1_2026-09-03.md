# Team Up v0.2.0-alpha.6.4.1 handoff

Date: 2026-09-03
Branch: `v0.2-alpha6-4-1-friendship-bond`
Status: **COMPILE-VERIFIED PASS**

## Verified build

GitHub Actions workflow: `Team Up v0.2.0-alpha.6.4.1 Friendship Bond`
Run: `33741430561`
Build-trigger SHA: `6766650245e3f894c3822315d5a1aaa9774d4f7c`
Materialized source SHA: `3053ccc2d8bd04313c267f63324402d5428a2dba`

CI result:
- Build Alpha 6.4.1 end-to-end: PASS
- Source acceptance: PASS
- Package verification: PASS
- Materialize generated source: PASS
- Artifact upload: PASS

`dotnet build`:
- Build succeeded.
- 0 Warning(s)
- 0 Error(s)
- Time Elapsed 00:00:05.29

Package:
`release/TeamUp_v0.2.0-alpha.6.4.1_FRIENDSHIP_BOND_TEST.zip`

Inner mod ZIP SHA-256:
`c3d0e5de50b24bc25d0cbd0ee0a6d67c8f9a1cbca75ca4c3e7bee141f1cd5f16`

Workflow artifact:
- ID: `9887880212`
- Name: `team-up-alpha6-4-1-friendship-bond`
- Artifact digest: `sha256:12b4837e2e9fcdca93130b1bfba5bdbfdba1cec10fe62cd30089fda6277d6f85`

Expected SMAPI marker:
`Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.4.1`

## Relationship progression

Alpha 6.4.1 reads Stardew friendship state at runtime. It never edits friendship points/hearts.

Milestones:
- 0-3 hearts: Acquaintance
- 4 hearts: Trusted
- 8 hearts: Close Companion
- 10 hearts: Max Friendship / Signature Affinity
- current spouse: Spouse Bond
- current spouse at 14 hearts: Soulmate

## 4 hearts - Trusted

Trusted grants approximately +10% Character XP and Role Mastery XP.

Implementation uses fractional runtime credits instead of blindly adding +1 per hit/skill. This avoids making small XP awards accidentally 25-50% stronger.

The awarded XP itself remains normal Team Up progression and saves normally. Fractional credit is runtime-only.

## 8 hearts - Close Companion

This milestone is primarily AI timing, not raw stats.

Role-aware adjustments:
- Tank: retreats slightly later (-0.03 threshold)
- Damage: retreats slightly later (-0.02)
- Healer: retreats slightly earlier when necessary (+0.02) and reacts to Farmer healing need earlier (+0.06 recovery threshold)
- Support: small safety adjustment (+0.015) and earlier Farmer support (+0.04 recovery threshold)
- Control: small safety adjustment (+0.01)

This preserves role identity instead of simply adding universal damage.

## 10 hearts - Signature Affinity

At 10+ hearts, Signature effectiveness gets a bounded x1.05 relationship multiplier.

The multiplier is connected to:
- Alpha 6.4.0 vanilla CharacterSkillIdentityService
- ExpansionSkillService for SVE/RSV Wave 1
- Alpha6CombatPolishService Tier 2/3 prototype signatures
- legacy Tier 1 signature blocks in CombatService

It affects Signature output/CC and vanilla identity Signature buff duration where applicable.
It does not globally add +5% to generic basic attacks.

## Spouse Bond

Only the Farmer's current spouse receives Bond utility, and only while that NPC is an active Following party member in the same location.

Role-aware Bond fallback:
- Tank: when Farmer is wounded, +2 runtime DEF
- Damage: while enemies pressure the Farmer, +5% runtime damage
- Support: under party pressure, +3 cooldown reduction points
- Healer: when Farmer is wounded, +6% runtime healing
- Control: with 2+ nearby enemies, +7% runtime control

These are conditional runtime modifiers. They refresh while the condition is true and naturally expire afterward.

## 14 hearts - Soulmate Traits

Vanilla romance candidates have bespoke trait identities:
- Abigail: `Dungeon Pact`
- Alex: `Always By Your Side`
- Elliott: `Rousing Devotion`
- Emily: `Two Hearts, One Aura`
- Haley: `Perfect Focus`
- Harvey: `I Won't Lose You`
- Leah: `Rooted Together`
- Maru: `Linked Systems`
- Penny: `Second Wind Together`
- Sam: `Shared Tempo`
- Sebastian: `Shadow Sync`
- Shane: `Stay Standing`

Harvey's Soulmate trait includes an emergency Farmer heal below 25% HP with a long 1800-tick cooldown.

Expansion spouses without a bespoke Alpha 6.4.1 trait use a safe role-based fallback:
- Tank: `Unbreakable Bond`
- Healer: `Heartkeeper`
- Support: `Shared Rhythm`
- Control: `Perfect Understanding`
- Damage/default: `Fight As One`

This avoids hard-failing or giving no relationship benefit to compatible modded spouses while bespoke expansion traits are not yet authored.

## Balance locks

Relationship is synergy, not a spouse meta requirement.

Inherited runtime caps still apply:
- per-source damage/heal/control buff <= 12%
- per-source defense <= +3
- per-source CDR <= 5 points
- aggregate runtime damage/heal/control <= 25%
- aggregate runtime defense <= +6
- aggregate runtime CDR <= 10 points
- total CDR <= 45%
- runtime modifier duration <= 900 ticks

No `RelationshipBond`, `Soulmate`, or temporary modifier fields were added to `PartyMemberData`.

## Character Profile UI

Character Profile now shows a compact `RELATIONSHIP` / `MỐI QUAN HỆ` section in the left identity panel:
- current hearts
- relationship stage
- spouse Bond notice when applicable
- Soulmate Trait name at 14 hearts

The Alpha 6.3.2 rule remains:
- Passive text-only
- exactly one Signature icon

## Regression locks retained

- Alpha 6.4.0 all vanilla Character Skill Identities remain active.
- Alpha 6.3.2 bespoke one-Signature-icon system remains.
- Alpha 6.3.1 rarity / role score / combat impact / Auto Equip remain.
- SVE/RSV Wave 1 signatures remain.
- ChaCha never enters Main Party.
- Alex/Tank approaches before TAUNT.
- Party Vault label fix remains.
- Farmer passes through Team Up party members.
- Team Up threat does not claim universal vanilla monster retargeting.

## Test checklist

Use:
`SMOKE_TEST_V0_2_ALPHA6_4_1_FRIENDSHIP_BOND_VI.txt`

First test priorities:
1. Relationship block layout in Character Profile at actual game resolution.
2. 4/8/10 heart behavior using NPCs with known friendship levels.
3. Current spouse in active party.
4. 14-heart vanilla spouse Soulmate Trait.
5. Buff expiry/non-stacking.
6. Alpha 6.3.1-6.4.0 regression.

## Next safe checkpoint

Do not stack another relationship layer before in-game testing Alpha 6.4.1.
Safe independent candidates after this checkpoint:
- Party HUD with HP/Role/Signature cooldown
- Expansion Skills Wave 2
- Buff/Debuff status visualization
