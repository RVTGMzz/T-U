# Team Up! - CONTINUE HERE alpha.5.2

Current checkpoint after the first alpha.5.1 gameplay UX review.

## Branch

`alpha5.2-ux-foundation`

## Current version to build/test

`0.1.0-alpha.5.2`

## What changed from alpha.5.1

### Special/Farmer Companion classification

ChaCha is no longer a normal Main Party recruitment target.

A new classification layer separates:

- Main Party NPCs;
- Farmer/special companions (ChaCha + future Farmer summons/familiars);
- NPC-linked companions;
- ineligible characters.

Compatibility modData contract:

`Ronvotri.TeamUp/CompanionKind`

Values:

- `FarmerCompanion`
- `FarmerSummon`
- `SpecialCompanion`
- `LinkedCompanion`

Config fallback:

`SpecialCompanionNpcNames`

Default contains `ChaCha`.

### Recruitment hint

The hint no longer depends only on `Game1.currentSpeaker`.
Team Up remembers the NPC the player manually interacted with before Stardew opens the dialogue and draws a dedicated bordered prompt above the DialogueBox.

Locked UX is still:

- E on keyboard;
- Right Shoulder on controller, displayed as `R (Controller)`;
- keyboard R is not the universal Team Up key;
- held-item gifting remains vanilla.

### Character Profile

Profiles no longer open as one wall of DialogueBox text.
`CharacterProfileMenu` now renders:

- portrait;
- name;
- Primary/Secondary Role + role glyphs;
- Recommended Engagement;
- five affinity bars;
- Passive;
- Signature Ability;
- Back/ESC/controller B.

### Party Vault

The persistence architecture is unchanged and still uses Stardew 1.6 native global inventory `Ronvotri.TeamUp/PartyVault`.

Alpha.5.2 adds a viewport-safe Team Up header with:

- Party Vault title;
- shared supplies description;
- x/36 occupied slots;
- Food / Healing / Utility / Loot category direction.

Those categories are presentation/future logistics direction only in this checkpoint.

## Build

Run:

`BUILD_ALPHA5_2.bat`

Expected package:

`release/TeamUp_v0.1.0-alpha.5.2_SMOKE_TEST.zip`

If build fails, send/read `BUILD_LOG.txt` and diagnose the first real compiler `error CS...`.

## Runtime smoke test

Use:

`SMOKE_TEST_ALPHA5_2_VI.txt`

Priority checks:

1. ChaCha has no Main Party invite hint and cannot be recruited.
2. Normal NPC dialogue visibly shows E / Right Shoulder invite hint.
3. Character Profile is a real dossier UI, not a text wall.
4. Party Vault header fits short viewports and item persistence remains intact.
5. Existing Follow / Stand / Role / Engagement / save-load / next-day lifecycle do not regress.

## Important combat gate

NPCs still only follow/stand in alpha.5.2. That is expected.

Do NOT start Combat Foundation until alpha.5.2 passes build + runtime smoke test.

After gate passes, combat order remains:

1. combat eligibility + safe locations;
2. target acquisition;
3. Engagement changes radius/behavior;
4. leash/return;
5. retreat threshold;
6. minimal per-role behavior;
7. threat/aggro;
8. Passive/Signature prototypes after the loop is stable.

For full context read:

1. `CONTINUE_HERE.md`
2. `CONTINUE_HERE_ALPHA5_1.md`
3. `CONTINUE_HERE_ALPHA5_2.md`
4. `docs/V0_1_IMPLEMENTATION_STATUS.md`
5. `docs/ALPHA5_PARTY_IDENTITY.md`
6. `docs/ALPHA5_2_UX_FOUNDATION.md`
