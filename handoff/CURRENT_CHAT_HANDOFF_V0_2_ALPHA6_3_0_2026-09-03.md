# Team Up v0.2.0-alpha.6.3.0 handoff

Branch: `v0.2-alpha6-3-npc-loadout-trait-icons`
Base: `v0.2-alpha6-2-expansion-skills-wave1`

## Goal
Add a compact Farmer-like NPC equipment screen and unique icons for NPC passive/signature traits.

## Implemented

### NPC loadout UI
- Reworked `UI/EquipmentMenu.cs` into a 3-column RPG layout:
  - NPC portrait + compact Level/HP/Role summary.
  - Three live equipment slots: Weapon / Armor / Trinket.
  - 6x6 Farmer backpack grid with real item icons.
- Selecting a slot makes compatible inventory items stay bright and incompatible items dim.
- Mouse/controller can equip directly from the grid.
- X unequips the current slot and returns the exact item through the existing `EquipmentService` backend.
- Existing per-slot global inventory IDs are reused, so save/equipment ownership semantics remain unchanged.

### Trait icons
- Added `UI/TraitIconRenderer.cs`.
- Every `characterName + Passive/Signature` pair receives a deterministic runtime-generated pixel glyph.
- Icons are stable across sessions and do not depend on SVE/RSV art assets.
- `CharacterProfileMenu` now shows icon cards beside Passive and Signature text.
- Role color is used as the palette family while the glyph remains unique to the NPC and trait kind.

### Build/package
- Added `BuildV0_2Alpha63.ps1`.
- `BUILD_V0_2_ALPHA6.bat` now targets Alpha 6.3.0.
- Added `_build_support/IntegrateNpcLoadoutTraitIcons.ps1` to preserve Alpha 6.1.3 fixes + Alpha 6.2 expansion skills while adding new i18n/version checks.
- Added Vietnamese smoke test: `SMOKE_TEST_V0_2_ALPHA6_3_NPC_LOADOUT_VI.txt`.
- Expected ZIP: `release/TeamUp_v0.2.0-alpha.6.3.0_NPC_LOADOUT_TRAIT_ICONS_TEST.zip`.
- Expected SMAPI marker: `Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.3.0`.

## Important regression expectations
- ChaCha remains excluded from Main Party.
- Tank approach-before-local-TAUNT remains intact.
- Equipment uses exact item objects and safe swap/unequip behavior from `EquipmentService`.
- Party Vault spacing/UTF-8 fixes remain intact.
- Follow anti-thrash + party pass-through remain intact.
- Alpha 6.2 SVE/RSV signature skill Wave 1 remains active.

## Build status
Source integration is complete, but this chat environment does not have a Stardew Valley install/runtime to run the final local compile. Run `BUILD_V0_2_ALPHA6.bat` on the development machine. If it fails, use the generated `BUILD_LOG.txt` for the next repair pass.
