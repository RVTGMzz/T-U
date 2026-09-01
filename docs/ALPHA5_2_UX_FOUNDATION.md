# Team Up! alpha.5.2 - UX Foundation

Version: `0.1.0-alpha.5.2`

Branch: `alpha5.2-ux-foundation`

This checkpoint is a pre-combat UX/foundation pass based on the alpha.5.1 gameplay smoke test.

## Why alpha.5.2 exists

The alpha.5.1 test exposed three important gaps:

1. ChaCha was being treated as a normal recruitable villager even though ChaCha belongs to the Farmer/special companion layer.
2. The recruitment shortcut worked when the player already knew it, but the on-dialogue hint was not reliably visible.
3. Character profiles and Party Vault contained the right foundation data, but still looked like debug/native placeholder UI instead of Team Up features.

NPCs following without fighting is NOT an alpha.5.2 bug. Combat AI remains gated until this UX checkpoint passes.

## 1. Character classification contract

Alpha.5.2 introduces `CompanionClassificationService`.

Character layers are now explicitly separated:

- Main Party Candidate
  - normal recruitable NPC;
  - requires Team Up invitation;
  - consumes Main Party slot.

- Farmer / Special Companion
  - includes ChaCha and future Farmer summons/familiars;
  - never receives the Main Party invite prompt;
  - does not consume Main Party slot;
  - future behavior belongs to a separate companion subsystem.

- NPC-linked Companion
  - pet/Pokemon/creature linked to a recruited NPC;
  - follows the NPC owner, not the Farmer;
  - does not consume Main Party slot.

- Ineligible
  - children or characters which don't satisfy party recruitment rules.

### Compatibility contract

Adapters can declare a character using modData key:

`Ronvotri.TeamUp/CompanionKind`

Supported alpha.5.2 values:

- `FarmerCompanion`
- `FarmerSummon`
- `SpecialCompanion`
- `LinkedCompanion`

`ModConfig.SpecialCompanionNpcNames` exists as a compatibility fallback for third-party characters which cannot yet provide the modData contract. Its default contains `ChaCha`.

This avoids building Main Party recruitment around an endless hard-coded summon blacklist.

## 2. Recruitment hint reliability

Alpha.5.1 rendering depended on `Game1.currentSpeaker` being available at render time.

Alpha.5.2 additionally remembers the NPC the player manually interacted with before Stardew opens the DialogueBox. The hint renderer resolves:

1. current Stardew speaker when available;
2. otherwise the remembered manually-interacted NPC.

The hint is now drawn in a dedicated dark/gold bordered panel above the DialogueBox.

Locked controls remain unchanged:

- keyboard `E` = recruit while normal dialogue is open;
- controller Right Shoulder = displayed as `R (Controller)`;
- keyboard `R` is NOT a Team Up all-purpose interaction key;
- gifting remains vanilla when holding an item.

Special/Farmer companions never receive this hint.

## 3. Character Profile UI

`CharacterProfileMenu` replaces the old wall-of-text profile DialogueBox.

The alpha.5.2 dossier includes:

- NPC portrait;
- NPC name;
- Primary Role with Team Up role glyph;
- Secondary Role with glyph;
- Recommended Engagement;
- five affinity bars: Tank / DPS / Support / Healer / Control;
- Passive block;
- Signature Ability block;
- mouse Back button;
- keyboard Escape;
- controller B/Back.

The data source remains `NpcProfileCatalog`; the UI was replaced without changing the profile model.

## 4. Party Vault identity pass

Party Vault still uses the safe Stardew 1.6 native global inventory:

`Ronvotri.TeamUp/PartyVault`

No storage serialization architecture was replaced.

Alpha.5.2 adds a viewport-safe Team Up header showing:

- Party Vault title;
- shared-party-supplies description;
- used slots out of 36;
- future-facing supply categories: Food / Healing / Utility / Loot.

The categories are informational in alpha.5.2. Auto-consumption, auto-loot, combat supply rules and role-specific supply use remain future combat/logistics work.

## Explicitly NOT implemented in alpha.5.2

- combat target acquisition;
- NPC attacks;
- threat / aggro;
- Tank taunt/interception runtime;
- Healer healing runtime;
- Support buffs/debuffs;
- Control crowd control;
- DPS combat rotation;
- Engagement combat radius/behavior;
- retreat thresholds;
- Passive runtime;
- Signature runtime;
- Farmer summon combat management.

## Build

Run:

`BUILD_ALPHA5_2.bat`

Expected package:

`release/TeamUp_v0.1.0-alpha.5.2_SMOKE_TEST.zip`

Smoke checklist:

`SMOKE_TEST_ALPHA5_2_VI.txt`

Only after this checkpoint passes should development enter the Combat Foundation sequence.
