# Team Up v0.2.0-alpha.6.3.1 handoff

Branch: `v0.2-alpha6-3-1-equipment-rpg-polish`
Base branch: `v0.2-alpha6-3-npc-loadout-trait-icons`
Verified materialized source commit: `dcbceeba394782e08f8d96b9d2580d35d8a00e41`
Version: `0.2.0-alpha.6.3.1`

## Goal
Polish the Alpha 6.3 NPC equipment screen into a stronger RPG-style loadout experience without changing Stardew item data, save ownership, or Team Up combat architecture.

## Implemented

### 1. Team Up rarity presentation
- Added Team Up-only visual rarity: Common / Uncommon / Rare / Epic / Legendary.
- Rarity is derived from the same sale-price tier used by the existing bounded Team Up equipment snapshot math.
- Rarity does NOT modify the underlying Stardew/SVE/RSV item, QualifiedItemId, stats, assets, or save data.
- Farmer backpack items and equipped slot icons receive a subtle rarity frame.
- Equipped item names use the rarity palette.

### 2. Role-aware equipment comparison
- Added `Core/EquipmentRpgPolishService.cs`.
- Active role resolves from the member's selected role, with NPC primary role fallback.
- Candidate gear gets a role score:
  - Tank: DEF first, then Control/CDR.
  - Damage: ATK first, then CDR.
  - Healer: HEAL first, then CDR.
  - Control: CTRL first, then CDR.
  - Support: CDR + HEAL + CTRL.
- Hover/controller comparison now shows role fit and current -> candidate role score.
- Fit labels: Excellent / Good / Neutral / Poor, localized EN/VI.

### 3. Direct combat impact preview
- Preview clones `PartyMemberData`; it does not mutate live member/save state.
- Uses the real `ProgressionService` formulas for current/candidate comparison.
- Role-specific impact line:
  - Tank: total DEF.
  - Damage: damage multiplier.
  - Healer: healing multiplier.
  - Control: control multiplier.
  - Support: cooldown reduction.
- Signature cooldown preview is shown when the signature's base cooldown is known.
- Known cooldown table covers Abigail/Alex/Harvey/Maru/Emily and all 24 SVE/RSV Wave 1 signature NPCs.
- Expansion Tier 3 preview applies the existing `-90 ticks` cooldown rule used by `ExpansionSkillService`.

### 4. Safe Auto Equip
- Added on-screen `AUTO EQUIP / TỰ ĐỘNG TRANG BỊ` button.
- Keyboard/gamepad shortcut: `Y`.
- Evaluates Weapon / Armor / Trinket separately using the NPC's active-role score.
- Only equips a candidate if its role score is strictly better than the currently equipped item for that slot.
- Uses existing `EquipmentService.GetEligibleInventoryItems` and `EquipmentService.TryEquip`.
- Therefore exact Item-object storage, safe swap behavior, full-inventory cancellation, and save ownership semantics remain owned by `EquipmentService`.
- Does not auto-play combat and does not alter item stats.

## Retained Alpha 6.3.0 features
- NPC portrait + compact level/HP/role column.
- Three live slots: Weapon / Armor / Trinket.
- 6x6 Farmer backpack with real item icons.
- Mouse and controller focus comparison cards.
- Passive and Signature unique deterministic NPC icons.

## Retained architecture / regression locks
- Team Up UniqueID remains `Ronvotri.TeamUp`.
- ChaCha (`Ronvotri.Cardcha_ChaCha`, display name `ChaCha`) remains Special/Farmer Companion and must never enter Main Party.
- Main Party remains human NPC recruitment only.
- Cardcha remains optional test host, not gameplay dependency.
- Alex Tank approach-before-local-TAUNT remains intact.
- Party Vault category spacing fix remains intact; do not classify that historical issue as Vietnamese encoding corruption.
- Follow anti-thrash and Farmer-through-party collision remain intact.
- Alpha 6.2 SVE/RSV Wave 1 real signature skills remain active.
- `teamup_test preset fullparty` intentionally saves Alex + Abigail + Harvey + Maru; persistence after warp is expected.
- Team Up threat/guard gameplay logic is not a claim that all vanilla monster subclasses are globally Harmony-retargeted.

## Build verification
GitHub Actions run: `33713741905`
Artifact ID: `9877782163`
Result: **SUCCESS**

Compile result:
- `0 Warning(s)`
- `0 Error(s)`
- Source acceptance PASS
- Package verification PASS
- Artifact upload PASS

Expected SMAPI marker:
`Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.3.1`

Packaged mod ZIP:
`release/TeamUp_v0.2.0-alpha.6.3.1_EQUIPMENT_RPG_POLISH_TEST.zip`

Inner mod ZIP SHA256:
`6cbcce8e7477052dbeb14894fe82eb203ed86ce888c3cb7ced1ec3af2139f61f`

GitHub Actions outer artifact digest:
`sha256:00af917c6644a0c053b86019f4168a48df825773ae35cf6fbed49d75820c7aed`

## In-game smoke test order
1. Startup marker is exactly Alpha 6.3.1 with no Team Up red error.
2. Equipment screen shows subtle rarity frames on backpack gear and equipped gear.
3. Hover/focus a compatible item and verify rarity, base stat comparison, role fit, role score, and role-specific combat impact.
4. Test known-signature NPC and verify Signature CD current -> candidate when gear changes CDR.
5. Use mouse Auto Equip and gamepad/keyboard Y; verify only better role-fit gear is equipped.
6. Verify no item loss/duplication and gear persists through save/load.
7. Verify manual equip/unequip + controller comparison from Alpha 6.3.0.
8. Verify Passive/Signature icons still render.
9. Regression: ChaCha no Recruit, Alex approaches before TAUNT, Party Vault category unobscured, Farmer pass-through, SVE/RSV Wave 1 skills still work.

## Build entry point
`BUILD_V0_2_ALPHA6.bat` now targets Alpha 6.3.1 and calls `BuildV0_2Alpha631.ps1`.
