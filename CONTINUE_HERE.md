# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.5**

Status: **compile/package verified with 0 warnings and 0 errors; Switch equipment, Codex navigation and Character Profile visual changes require in-game confirmation**.

Development branch:

`v0.2-alpha6-6-5-switch-input-codex-profile-polish`

Final handoff branch after closure:

`v0.2-alpha6-6-5-switch-input-codex-profile-polish-handoff`

Previous handoff:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_4_2026-09-05.md`

## Why Alpha 6.6.5 exists

Real Nintendo/Switch controller testing after Alpha 6.6.4 found three remaining UX/runtime problems:

1. Equipment still could not reliably equip through the physical Switch action button.
2. Codex vertical navigation could skip one profile, e.g. A -> C instead of A -> B -> C.
3. Character Profile typography was visually unbalanced because passive/signature descriptions were rendered at 2.28x while surrounding content stayed near 1.1x.

The same test also exposed hollow `☆` glyphs in equipment summaries. Those were font fallback for punctuation such as em dash, middle dot and arrow, not corrupted equipment data.

## Alpha 6.6.5 locked fixes

### 1. Switch/Nintendo equipment input

File:

`src/TeamUp/ModEntry.Alpha663.cs`

Team Up now captures the platform-configured SMAPI Action Button while `EquipmentMenu` is open:

- checks `e.Button.IsActionButton()` instead of assuming a physical XInput `Buttons.A` mapping;
- suppresses the original button through `Helper.Input.Suppress(e.Button)`;
- routes exactly one activation into the existing EquipmentMenu transactional controller path;
- keeps Alpha 6.6.4 debounce and virtual mouse echo suppression active.

Key tokens:

- `OnAlpha665EquipmentButtonPressed`
- `e.Button.IsActionButton()`
- `Helper.Input.Suppress(e.Button)`
- `menu.receiveGamePadButton(Buttons.A)`

Do not replace this with a hard-coded Nintendo/XInput button assumption.

### 2. Codex one-profile navigation

File:

`src/TeamUp/UI/CodexBrowserMenu.cs`

D-pad and left-stick Up/Down now both move exactly one profile per input.

Locked behavior:

- D-pad Up/Down: 1 row;
- left-stick Up/Down: 1 row;
- dropdown options: 1 option;
- right-stick/mouse-wheel viewport scrolling remains separate and keeps logical selection inside the visible range.

Do not restore `MoveVertical(2)` or `MoveVertical(-2)` in controller navigation.

### 3. Character Profile typography

File:

`src/TeamUp/UI/CharacterProfileMenu.cs`

The right-side scrollable profile panel now uses a common readable content scale:

`ProfileContentScale = 1.52f`

This is approximately two-thirds of the previous giant 2.28x description scale.

The scale is used for:

- section titles in the detailed right panel;
- affinity labels and scores;
- passive text;
- signature text;
- relationship text.

Affinity row spacing was increased to fit the larger type. Long passive/signature/relationship content should scroll rather than being shrunk aggressively.

### 4. Hollow-star font fallback

Files:

- `src/TeamUp/Core/ProgressionService.cs`
- `src/TeamUp/UI/EquipmentMenu.cs`

Stardew's UI font was rendering unsupported punctuation as hollow stars. Alpha 6.6.5 replaces UI punctuation with safe ASCII equivalents:

- empty gear: `-`
- separators: `|`
- comparison arrow: `->`

Examples now render as:

`Lv.1 | HP 90/90 | Healer M0`

`W: - | A: -`

`T: -`

The old hollow `☆` marks were a glyph fallback bug only. They did not represent a hidden equipment/stat mechanic.

## Alpha 6.6.4 equipment safety retained

- controller activation debounce remains 180 ms;
- virtual mouse echo suppression remains 260 ms;
- NPC slot cards do not double-click unequip;
- unequip remains explicit through X or the Unequip button;
- inventory mouse double-click equip remains 450 ms;
- equip HUD success requires committed PartyMember metadata and actual global equipment storage;
- unequip HUD success requires both stores to be empty.

## Product rules retained

### People capacity

- maximum 6 total people across online Farmers plus active `Following`/`Waiting` NPCs;
- single player therefore allows up to 5 active NPCs;
- overflow becomes Inactive without deleting roster/progression/equipment.

### Combat companion capacity

- hard shared max 2 deployed external Pokemon/summon/creature companions across the farm;
- Farmer-owned and NPC-linked external creatures share the pool;
- `Active`, `Waiting`, `ReturningHome` reserve slots;
- `Standby`, `Inactive` do not;
- vanilla pet is free;
- ChaCha is free and never Main Party.

### Party Strategy

Five values remain:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

### Custom recruits

MiMi:

- canonical `Ronvotri.Cardcha_MiMi`
- source `Ronvotri.Cardcha`
- signature `BROOMTAIL SIGIL`

Sudoku:

- canonical `ronvotri.HeyYoureCursed_Sudoku`
- signature `NINEFOLD SEAL`
- Team Up movement marker `Ronvotri.TeamUp/PartyControlled = true`
- source mod remains story/trust/roommate authority.

## Core regression locks

- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- anti-spin;
- Hold Position no chase outside attack range;
- Aggressive never disables hard leash;
- Boss Focus only prioritizes highest MaxHealth among valid targets;
- Pelipper Town shared 2/2 compatibility remains source-respecting;
- Follow water/bridge bounded open-tile search remains;
- Surge Cardcha arena exclusion and safe placement remain;
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge;
- no arbitrary custom monster cloning;
- 51 SVE/RSV profiles/icons/balance;
- Party Vault drag/drop;
- Origin story.

## Alpha 6.6.5 build checkpoint

Materialized source commit from the first successful build:

`7046dd5`

Successful materializing CI run:

`33961399608`

Result:

- direct `BuildV0_2Alpha665.ps1` success;
- 0 warnings;
- 0 errors;
- Switch SMAPI Action Button bridge acceptance PASS;
- Codex one-row controller navigation acceptance PASS;
- Character Profile 1.52 uniform typography acceptance PASS;
- Stardew-font-safe punctuation acceptance PASS;
- Alpha 6.6.4 / 6.6.3 regression acceptance PASS;
- package verification PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.5_SWITCH_CODEX_PROFILE_HOTFIX_TEST.zip`

Package SHA256:

`67f797a5858fc9bcdbe688040cbb1dc8017b07dcd2adfe7418fda4f6e3bacb51`

Artifact ID from the materializing run:

`9968051018`

Artifact wrapper digest:

`sha256:3ca2419de44eedce45e4ef8b31d35157e7d1e484ad2e3a641d71b06b64896349`

A final authoritative rerun from the materialized source should report `No materialized source diff.` before this checkpoint is handed off.

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_5_SWITCH_CODEX_PROFILE_POLISH_VI.txt`

Highest priority:

1. On Switch controller, focus a valid Farmer inventory item and press the physical Action Button once. Item must leave the bag and appear in the real NPC equipment slot.
2. Repeat using right-stick pointer mode.
3. Confirm no duplicate equip/unequip transaction or contradictory HUD pair.
4. In Codex, verify A -> B -> C -> D with both D-pad and short left-stick inputs, never A -> C.
5. Open Harvey or another profile with long passive/signature text. Right panel should be consistently large and readable at roughly 1.52x, with scrolling for overflow.
6. Confirm equipment summary no longer displays hollow `☆` glyphs.
7. Re-test Tactics, Pelipper 2/2, Sudoku, MiMi, Surge and Party Vault.

Do not call the Switch equipment behavior live-verified until the user confirms it in game.
