# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.6**

Status: **compile/package verified with 0 warnings and 0 errors; Switch equip is user-confirmed working, Switch unequip semantic-input fix now requires in-game confirmation.**

Development branch:

`v0.2-alpha6-6-6-switch-unequip-input-hotfix`

Planned handoff branch:

`v0.2-alpha6-6-6-switch-unequip-input-hotfix-handoff`

Previous handoff:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_5_2026-09-05.md`

## Why Alpha 6.6.6 exists

Live Switch/Nintendo controller testing confirmed Alpha 6.6.5 fixed **equip**, but the dedicated **unequip** shortcut still did nothing.

Root cause: Alpha 6.6.5 moved equip onto SMAPI's semantic `IsActionButton()` path, while unequip still depended on the menu receiving a hard-coded XInput `Buttons.X` event. Nintendo-labelled controllers do not reliably expose that physical face button through the same XInput label.

## Alpha 6.6.6 input contract

File:

`src/TeamUp/ModEntry.Alpha663.cs`

When `EquipmentMenu` is open:

- `e.Button.IsActionButton()` routes to `Buttons.A` and activates/equips using the already verified 6.6.5 path;
- `e.Button.IsUseToolButton()` routes to `Buttons.X` internally and explicitly unequips the currently selected NPC equipment slot;
- the original SMAPI button is suppressed before forwarding so one physical press cannot also echo into Stardew's menu handling;
- the existing EquipmentMenu transactional checks, controller debounce and virtual-mouse suppression remain authoritative.

Key tokens:

- `e.Button.IsActionButton()`
- `e.Button.IsUseToolButton()`
- `routedButton = Buttons.A`
- `routedButton = Buttons.X`
- `Helper.Input.Suppress(e.Button)`
- `menu.receiveGamePadButton(routedButton.Value)`

Do not replace semantic Action/Use Tool mapping with assumptions about Nintendo/Xbox face-button labels.

## Equipment safety retained

- controller activation debounce: 180 ms;
- virtual mouse echo suppression: 260 ms;
- inventory mouse double-click equip: 450 ms;
- NPC slot-card double-click unequip remains removed;
- explicit Unequip button remains available through normal UI focus/pointer activation;
- equip success HUD requires both PartyMember metadata and actual shared equipment storage commit;
- unequip success HUD requires both stores to be empty before success is reported.

## Alpha 6.6.5 UX fixes retained

- Codex D-pad and left stick move exactly 1 profile per input;
- Character Profile detailed right panel uses `ProfileContentScale = 1.52f` with scrolling;
- unsupported UI punctuation was replaced with ASCII-safe `-`, `|`, `->`, removing hollow-star fallback glyphs.

## Product and regression locks retained

- maximum 6 total people across online Farmers plus active Team Up NPCs;
- shared deployed external Pokemon/summon/creature companion cap = 2;
- Farmer-owned and NPC-linked external creatures share the 2/2 pool;
- vanilla pet free;
- ChaCha free and never Main Party;
- Party Strategy: Balanced, Defensive, Aggressive, HoldPosition, BossFocus;
- MiMi canonical `Ronvotri.Cardcha_MiMi`, signature `BROOMTAIL SIGIL`;
- Sudoku canonical `ronvotri.HeyYoureCursed_Sudoku`, signature `NINEFOLD SEAL`;
- Sudoku Team Up control marker `Ronvotri.TeamUp/PartyControlled = true`;
- Pelipper Town shared 2/2 compatibility remains source-respecting;
- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- Surge Cardcha sandbox and safe-placement rules retained;
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge;
- 51 SVE/RSV profiles/icons/balance retained;
- Party Vault drag/drop retained;
- Origin story retained.

## Alpha 6.6.6 build checkpoint

First successful materializing CI run:

`33962414122`

Materialized source commit:

`75a76d0`

First package SHA256:

`2c600137cea4456bc3f5dd17feaa2e7cb101515025735f12bb1a79708359b049`

First artifact ID:

`9968347630`

Result:

- `BuildV0_2Alpha666.ps1` success;
- 0 warnings;
- 0 errors;
- Switch semantic Action equip acceptance PASS;
- Switch semantic Use Tool unequip acceptance PASS;
- transactional equipment regression PASS;
- Alpha 6.6.5 Codex/Profile/font regression PASS;
- package verification PASS.

A final authoritative rerun from materialized source must report `No materialized source diff.` before handoff closure.

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_6_SWITCH_UNEQUIP_INPUT_HOTFIX_VI.txt`

Highest priority:

1. Equip an item using the Action Button. This is already confirmed working on the user's Switch controller and must remain working.
2. Select the occupied Weapon/Armor/Trinket slot and press the controller button configured by Stardew as **Use Tool** once. The item must leave the NPC slot and return to the Farmer inventory exactly once.
3. Move focus to the on-screen `Tháo trang bị` button and press Action. It must also unequip.
4. In controller-pointer mode, place the cursor on `Tháo trang bị` and press Action. It must also unequip.
5. Confirm no contradictory HUD messages and no duplicate transaction.
6. Re-test Codex one-row navigation, profile 1.52 typography, no hollow-star glyphs, Tactics, Pelipper 2/2, Sudoku, MiMi, Surge and Party Vault.

Do not call Switch unequip live-verified until the user confirms it in game.
