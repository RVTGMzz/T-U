# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.5**

Status: **compile/package/direct-builder verified with 0 warnings, 0 errors and no materialized source diff. Switch equipment and UI behavior still require real in-game confirmation.**

Development branch:

`v0.2-alpha6-6-5-switch-input-codex-profile-polish`

Final handoff branch:

`v0.2-alpha6-6-5-switch-input-codex-profile-polish-handoff`

Read first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_5_2026-09-05.md`

Previous checkpoint:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_4_2026-09-05.md`

## Alpha 6.6.5 fixes

### Switch / Nintendo equipment input

`src/TeamUp/ModEntry.Alpha663.cs`

When `EquipmentMenu` is open, Team Up now captures SMAPI's configured Action Button instead of assuming XInput `Buttons.A` is the physical Nintendo input.

Locked tokens:

- `OnAlpha665EquipmentButtonPressed`
- `e.Button.IsActionButton()`
- `Helper.Input.Suppress(e.Button)`
- `menu.receiveGamePadButton(Buttons.A)`

The routed activation reuses Alpha 6.6.4 transactional equipment confirmation, 180 ms controller debounce and 260 ms virtual-mouse echo suppression.

### Codex one-row navigation

`src/TeamUp/UI/CodexBrowserMenu.cs`

D-pad and left-stick Up/Down both move exactly one profile per input. Do not restore `MoveVertical(2)` or `MoveVertical(-2)` in controller navigation. Right-stick/mouse-wheel viewport scrolling remains separate and selection remains clamped to the visible range.

### Character Profile typography

`src/TeamUp/UI/CharacterProfileMenu.cs`

Right-side scrollable detail content uses:

`ProfileContentScale = 1.52f`

This replaces the previous giant 2.28x passive/signature description scale. Affinity labels/scores, section labels, passive, signature and relationship content use the larger balanced scale with extra vertical spacing and scrolling for overflow.

### Hollow-star glyph bug

The `☆` marks seen in equipment summary were font fallback, not hidden stats or broken equipment state.

Unsafe punctuation was replaced in:

- `src/TeamUp/Core/ProgressionService.cs`
- `src/TeamUp/UI/EquipmentMenu.cs`

Safe UI output now uses:

- empty gear `-`
- separator `|`
- comparison arrow `->`

Examples:

`Lv.1 | HP 90/90 | Healer M0`

`W: - | A: -`

`T: -`

## Alpha 6.6.4 equipment locks retained

- inventory mouse double-click = 450 ms;
- NPC slot-card double-click unequip remains removed;
- X / explicit Unequip button handles unequip;
- successful equip HUD requires both PartyMember metadata and real FarmerTeam global equipment storage to contain the requested item;
- successful unequip HUD requires both stores to be empty;
- controller debounce = 180 ms;
- gamepad virtual-click suppression = 260 ms.

## Product rules retained

- maximum 6 total people across online Farmers plus active Team Up NPCs;
- shared deployed external Pokemon/summon/creature companion cap = 2;
- Farmer-owned and NPC-linked external creatures share the 2/2 pool;
- vanilla pet free;
- ChaCha free and never Main Party;
- Party Strategy values: Balanced, Defensive, Aggressive, HoldPosition, BossFocus;
- MiMi canonical `Ronvotri.Cardcha_MiMi`, signature `BROOMTAIL SIGIL`;
- Sudoku canonical `ronvotri.HeyYoureCursed_Sudoku`, signature `NINEFOLD SEAL`;
- Sudoku Team Up control marker remains `Ronvotri.TeamUp/PartyControlled = true`;
- Pelipper Town shared 2/2 compatibility remains source-respecting;
- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- Surge Cardcha sandbox and safe-placement rules retained;
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge;
- 51 SVE/RSV profiles/icons/balance retained;
- Party Vault drag/drop retained;
- Origin story retained.

## Authoritative Alpha 6.6.5 checkpoint

Materialized gameplay source commit:

`7046dd568ca71c0c78a51f7ee12e3ab691241c03`

Final authoritative input/docs commit:

`000adad903c838419e308d9c0c98c5e00605d45e`

Final authoritative CI run:

`33961524993`

Result:

- `BuildV0_2Alpha665.ps1` success;
- 0 warnings;
- 0 errors;
- Switch SMAPI Action Button bridge PASS;
- Codex one-row controller navigation PASS;
- Character Profile uniform 1.52 typography PASS;
- Stardew-font-safe punctuation PASS;
- Alpha 6.6.4 / 6.6.3 regression tokens PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Authoritative package:

`TeamUp_v0.2.0-alpha.6.6.5_SWITCH_CODEX_PROFILE_HOTFIX_TEST.zip`

Package SHA256:

`38706ecabac5f3e34d64fc2958e4c961252c88897f7dd5b3fe3117db16020e36`

Artifact ID:

`9968093241`

Artifact wrapper digest:

`sha256:f4dd7737ed0362208cef513220be42023807c3b3752c945a30794d41af9ce5df`

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_5_SWITCH_CODEX_PROFILE_POLISH_VI.txt`

Highest priority:

1. Switch controller: focus a valid inventory weapon/boots/ring and press the physical Action Button once. The item must leave the bag and visibly occupy the NPC slot.
2. Repeat in controller-pointer mode using right stick.
3. No contradictory `equipped` then `unequipped` HUD pair may occur for one action.
4. Codex D-pad and left-stick should traverse A -> B -> C -> D, one profile per input.
5. Open Harvey or another long profile and verify all detailed right-panel text is balanced at roughly 1.52x and scrolls cleanly.
6. Equipment summary must no longer show hollow `☆` glyphs.
7. Re-test Tactics, Pelipper 2/2, Sudoku, MiMi, Surge and Party Vault.

Do not call the Switch equipment behavior live-verified until the user confirms it in game.
