# Team Up v0.2.0-alpha.6.3.0 handoff

Branch: `v0.2-alpha6-3-npc-loadout-trait-icons`
Base: `v0.2-alpha6-2-expansion-skills-wave1`

## Goal
Add a compact Farmer-like NPC equipment screen, unique icons for NPC passive/signature traits, and readable gear stat comparison before equipping.

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

### Gear stat comparison
- Added `Core/EquipmentPreviewService.cs` using the same stat formula and early NPC gear synergies as `EquipmentService.BuildSnapshot`.
- Hovering a Farmer backpack item shows a comparison card for the selected NPC slot.
- Controller/keyboard inventory focus shows the same comparison card without requiring a physical mouse.
- Card compares ATK / DEF / HEAL / CTRL / CDR as current -> candidate and shows the delta.
- Positive deltas are visually positive, negative deltas visually negative, unchanged values neutral.
- Incompatible items show an explicit incompatibility message instead of misleading stats.

### Trait icons
- Added `UI/TraitIconRenderer.cs`.
- Every `characterName + Passive/Signature` pair receives a deterministic runtime-generated pixel glyph.
- Icons are stable across sessions and do not depend on SVE/RSV art assets.
- `CharacterProfileMenu` now shows icon cards beside Passive and Signature text.
- Role color is used as the palette family while the glyph remains unique to the NPC and trait kind.

### Build/package
- Added `BuildV0_2Alpha63.ps1`.
- `BUILD_V0_2_ALPHA6.bat` targets Alpha 6.3.0.
- Build chain now layers:
  - stable Alpha 6.1.3 regression fixes,
  - Alpha 6.2 expansion skill Wave 1,
  - Alpha 6.3 NPC loadout + trait icons,
  - Alpha 6.3 controller stat-preview polish.
- Added `_build_support/IntegrateNpcLoadoutTraitIcons.ps1`.
- Added `_build_support/FixAlpha63LoadoutPolish.ps1`.
- Added Vietnamese smoke test: `SMOKE_TEST_V0_2_ALPHA6_3_NPC_LOADOUT_VI.txt`.
- Expected ZIP: `release/TeamUp_v0.2.0-alpha.6.3.0_NPC_LOADOUT_TRAIT_ICONS_TEST.zip`.
- Expected SMAPI marker: `Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.3.0`.

## CI VERIFIED BUILD
GitHub Actions workflow:
`Team Up v0.2.0-alpha.6.3.0 NPC Loadout + Trait Icons`

Successful final run:
`33712076153`

Build result:
- Build Alpha 6.3 end-to-end: PASS
- Source acceptance: PASS
- Package verification: PASS
- Artifact upload: PASS
- dotnet build: PASS
- Warnings: 0
- Errors: 0

Final mod ZIP SHA-256:
`30e5cf896c2704fc02797b212f9061678ba173b8098accc55923d968d847a3d3`

Workflow artifact ID:
`9877257342`

Materialized source commit produced by CI:
`4899e7b` (`chore: materialize Team Up v0.2.0-alpha.6.3.0 build source [skip ci]`)

## Important regression expectations
- ChaCha remains excluded from Main Party.
- Tank approach-before-local-TAUNT remains intact.
- Equipment uses exact item objects and safe swap/unequip behavior from `EquipmentService`.
- Party Vault spacing/UTF-8 fixes remain intact.
- Follow anti-thrash + party pass-through remain intact.
- Alpha 6.2 SVE/RSV signature skill Wave 1 remains active.

## Highest-priority in-game test order
1. Open a recruited NPC -> Equipment.
2. Confirm portrait + 3 live slots + 6x6 Farmer backpack grid render cleanly.
3. Select Weapon, Armor, Trinket and confirm compatible gear stays bright while incompatible gear dims.
4. Mouse-hover a compatible item and confirm comparison card shows current -> candidate stats.
5. Navigate the backpack with controller and confirm focused item shows the same comparison card.
6. Equip/swap/unequip and verify no item loss or duplication.
7. Open Character Profile and confirm Passive and Signature each have distinct icons.
8. Test one SVE and one RSV Wave 1 NPC so expansion source/profile/skill regressions are covered.
9. Confirm ChaCha still has no Recruit option.
10. Confirm Alex Tank approaches before local TAUNT.

## Next work after user test
- Fix any visual spacing/scale issues seen at the user's actual resolution.
- If loadout UX is accepted, extend hover/controller comparison with richer item description and whole-loadout totals rather than only selected-slot stats.
- Continue SVE/RSV skill/profile Wave 2 after UI regression is stable.
