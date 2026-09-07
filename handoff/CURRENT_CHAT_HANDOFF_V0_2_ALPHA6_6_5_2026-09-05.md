# Team Up v0.2.0-alpha.6.6.5 Final Handoff

Date: 2026-09-05

## Resume point

Final handoff branch:

`v0.2-alpha6-6-5-switch-input-codex-profile-polish-handoff`

Development branch:

`v0.2-alpha6-6-5-switch-input-codex-profile-polish`

Materialized gameplay source commit:

`7046dd568ca71c0c78a51f7ee12e3ab691241c03`

Final authoritative input/docs commit on development branch:

`000adad903c838419e308d9c0c98c5e00605d45e`

Final handoff docs commit before this file:

`1b838fdfc21ffb02792348270f0ca74c5ef8847b`

Version:

`0.2.0-alpha.6.6.5`

## Why this hotfix exists

The user tested Alpha 6.6.4 with a Nintendo/Switch controller and reported:

1. Equipment still would not equip reliably with controller input.
2. Codex Down could skip the next NPC profile, e.g. A -> C instead of A -> B -> C.
3. Character Profile right-side content was visually unbalanced because passive/signature text was enormous while affinity/section text was small.
4. Hollow `☆` characters appeared in compact equipment summary text.

## Root causes and fixes

### Switch equipment input

Alpha 6.6.4 still assumed `IClickableMenu.receiveGamePadButton(Buttons.A)` would receive the platform's physical action input. That is not a safe assumption for Nintendo/Switch layouts.

Alpha 6.6.5 adds a SMAPI input bridge in `ModEntry.Alpha663.cs`:

```csharp
private void OnAlpha665EquipmentButtonPressed(object? sender, ButtonPressedEventArgs e)
{
    if (!Context.IsWorldReady || Game1.activeClickableMenu is not EquipmentMenu menu)
        return;

    if (!e.Button.IsActionButton())
        return;

    Helper.Input.Suppress(e.Button);
    menu.receiveGamePadButton(Buttons.A);
}
```

Registration is added through the already-active `RegisterAlpha663HotfixEvents()` path.

Contract:

- detect SMAPI's configured Action Button;
- suppress the original button before Stardew translates it further;
- route exactly one logical activation into EquipmentMenu;
- reuse 6.6.4 debounce, pointer/focus behavior and transactional commit verification.

Do not replace this bridge with a hard-coded Nintendo/XInput button mapping unless a future platform-specific test proves it necessary.

### Codex profile skipping

`CodexBrowserMenu.receiveGamePadButton()` previously intentionally used two-row movement for left stick:

`MoveVertical(-2)` / `MoveVertical(2)`

This caused the user's A -> C behavior.

Alpha 6.6.5 changes left-stick vertical input to one row, matching D-pad precision.

Locked behavior:

- D-pad Up/Down = one row;
- left-stick Up/Down = one row;
- dropdown Up/Down = one option;
- right-stick/mouse-wheel viewport scrolling remains separate and selection stays synchronized to the visible range.

### Character Profile typography

Old constants included:

`DescriptionScale = BodyScale * 2f`

With `BodyScale = 1.14f`, long passive/signature descriptions rendered at 2.28x.

Alpha 6.6.5 replaces the giant description scale with:

`ProfileContentScale = 1.52f`

This is roughly two-thirds of 2.28, matching the user's requested visual target.

The right-side scrollable profile content now uses the common larger scale for:

- affinity labels and scores;
- section labels;
- passive description;
- signature description;
- relationship content.

Affinity vertical spacing is increased. Overflow should use scrolling rather than aggressive shrinkage.

### Hollow star glyphs

The `☆` characters were a font fallback bug, not equipment state corruption.

`ProgressionService` used unsupported punctuation such as:

- em dash for empty gear;
- middle dot separators.

`EquipmentMenu` also used middle-dot and arrow glyphs.

Alpha 6.6.5 replaces them with Stardew-font-safe ASCII:

- `-`
- `|`
- `->`

Expected compact equipment summary:

```text
W: - | A: -
T: -
```

## Alpha 6.6.4 safety retained

Do not regress these:

- inventory item mouse double-click window = 450 ms;
- controller activation debounce = 180 ms;
- virtual mouse echo suppression = 260 ms;
- NPC equipment slot cards do not double-click unequip;
- explicit X / Unequip button still unequips;
- equip success HUD only after requested item is verified in metadata plus actual global equipment storage;
- unequip success HUD only after both stores are empty.

## Product rules retained

### People

Maximum 6 total people across online Farmers and active `Following`/`Waiting` NPCs.

Single-player therefore supports 1 Farmer + up to 5 active NPCs.

### External combat companions

Hard shared deployed cap = 2 farm-wide.

- Farmer-owned and NPC-linked external creatures share the pool;
- Active / Waiting / ReturningHome reserve a slot;
- Standby / Inactive do not;
- vanilla pet is free;
- ChaCha is free and never Main Party.

### Strategy

Keep all five:

- Balanced
- Defensive
- Aggressive
- HoldPosition
- BossFocus

### MiMi

- canonical `Ronvotri.Cardcha_MiMi`
- source `Ronvotri.Cardcha`
- signature `BROOMTAIL SIGIL`
- no Cardcha private save/service coupling.

### Sudoku

- canonical `ronvotri.HeyYoureCursed_Sudoku`
- source `ronvotri.HeyYoureCursed`
- signature `NINEFOLD SEAL`
- Team Up control marker `Ronvotri.TeamUp/PartyControlled = true`
- source mod owns story/trust/roommate logic.

## Other regression locks

- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- anti-spin;
- Hold Position no chase outside attack range;
- Aggressive never disables hard leash;
- Boss Focus highest MaxHealth only among valid candidates;
- Pelipper Town compatibility remains optional/source-respecting and shares 2/2 external pool;
- Follow bounded open-tile fallback for water/bridges/narrow maps remains;
- Surge Cardcha arena exclusion remains;
- Surge safe placement uses `isTileOnMap`, `isTilePassable`, `IsTileBlockedBy`;
- do not restore `isTileLocationTotallyClearAndPlaceable` to Surge;
- no arbitrary custom-monster cloning;
- 51 SVE/RSV profiles/icons/balance retained;
- Party Vault drag/drop retained;
- Origin story retained.

## Build and CI

Builder:

`BuildV0_2Alpha665.ps1`

One-click Windows launcher:

`BUILD_V0_2_ALPHA6.bat`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_5_SWITCH_CODEX_PROFILE_POLISH_VI.txt`

First successful materializing run:

`33961399608`

Final authoritative run from materialized source:

`33961524993`

Final authoritative run result:

- build success;
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

## Live smoke still pending

Highest priority next test:

1. Open NPC Equipment on Switch controller.
2. Navigate to a valid weapon with D-pad/left stick and press the physical Action Button once.
3. Confirm item leaves Farmer bag, appears in NPC slot and only one success transaction occurs.
4. Repeat with controller pointer/right stick.
5. Open Codex and verify A -> B -> C with D-pad and left stick, one row each input.
6. Open Harvey profile and visually check the 1.52 scale + scrolling.
7. Confirm the old hollow `☆` equipment-summary glyphs are gone.
8. Regression smoke Tactics, Pelipper 2/2, Sudoku, MiMi, Surge and Vault.

If Switch Action Button still cannot equip after this build, collect a fresh SMAPI log from the exact button press. Because 6.6.5 captures SMAPI `IsActionButton()` directly, that log/testing will tell us whether the remaining issue is platform button classification, menu focus state, or inventory mutation rather than the old XInput mapping path.
