# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.4**

Status: **compile/package/direct-builder verified with 0 warnings and 0 errors; controller equipment fix requires in-game confirmation**.

Development branch:

`v0.2-alpha6-6-4-controller-equipment-transaction-hotfix`

Final handoff branch:

`v0.2-alpha6-6-4-controller-equipment-transaction-hotfix-handoff`

Read this handoff first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_4_2026-09-05.md`

For the previous Pelipper/Codex/water hotfix details, see:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_3_2026-09-05.md`

## Why Alpha 6.6.4 exists

A real controller test on the Equipment screen showed contradictory HUD messages such as `Đã trang bị Liềm` followed by `Đã tháo Liềm về túi`, while the NPC slot still displayed `Chưa trang bị`.

The important runtime finding was that controller activation and mouse-style activation were not sufficiently isolated. Stardew can echo controller `A` through mouse/cursor behavior, while Team Up also had a slot-card double-click unequip path. One physical action could therefore cause multiple state transitions or misleading success feedback.

## Alpha 6.6.4 equipment locks

File:

`src/TeamUp/UI/EquipmentMenu.cs`

Behavior:

- Controller `A`, `X`, and `Y` activation is debounced.
- A controller activation suppresses its short-lived virtual left-click echo.
- NPC equipment slot cards now only select the slot. They no longer support double-click unequip.
- Unequip remains explicit through controller `X` or the `Tháo trang bị` button.
- Mouse double-click on an eligible item in the Farmer inventory remains supported with the existing 450 ms window.
- Equip success HUD is transactional: Team Up verifies both PartyMember equipment metadata and the actual FarmerTeam global equipment inventory slot before showing success.
- Unequip success HUD is transactional: both metadata and the actual global equipment slot must be empty before success is shown.
- If commit verification fails, an error is shown instead of a fake success toast.

Important constants/tokens:

- `DoubleClickWindowMs = 450`
- `ControllerActivationDebounceMs = 180`
- `ControllerMouseEchoSuppressionMs = 260`
- `TryBeginControllerActivation()`
- `_suppressMouseClickUntilMs`
- `TryActivateControllerPointer()`
- `GetActualEquippedItem(_selectedSlot)`

Do not restore slot-card double-click unequip. It is intentionally removed to keep one controller action equal to one state transition.

## Alpha 6.6.3 behavior retained

### Codex navigation

- right-stick scroll keeps selection inside the visible viewport;
- right-stick scroll step 2 rows;
- left-stick Up/Down moves 2 rows;
- D-pad remains 1 row for precision.

### Pelipper Town

Provider compatibility ID:

`Griff.PelipperTown`

- optional adapter, no hard Pelipper DLL dependency;
- no Pelipper private-save reading;
- detected Farmer/NPC Pokemon enter the shared external companion pool;
- shared hard max remains 2/2;
- Pelipper remains movement authority for its own source actors;
- diagnostics remain:
  - `teamup_pelipper status`
  - `teamup_pelipper reconcile`

### Water / bridge performance

- Team Up Follow update cadence remains every 4 ticks;
- bounded `OpenSearchOffsets` fallback remains;
- Follow tile validation uses `isTileOnMap + isTilePassable`;
- Pelipper source-controlled Pokemon are skipped by Team Up follower movement.

## Product rules still locked

### People capacity

- maximum 6 total people across online Farmers + `Following` / `Waiting` Team Up NPCs;
- a Farmer consumes a people slot;
- single player therefore allows up to 5 active NPCs;
- overflow becomes Inactive without deleting roster/progression/equipment;
- ownership remains `RecruiterId` based.

### Combat companion capacity

- hard shared max 2 deployed external Pokemon/summon/creature companions across the whole farm;
- Farmer-owned and NPC-linked creatures share the pool;
- `Active`, `Waiting`, `ReturningHome` reserve slots;
- `Standby`, `Inactive` do not;
- vanilla pet is free;
- ChaCha is free and never Main Party.

### Party Strategy

Five values remain unchanged:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Tactics UI and host-authoritative multiplayer strategy sync remain unchanged from Alpha 6.6.2.

## Custom recruit locks

### MiMi

- Cardcha UniqueID `Ronvotri.Cardcha`
- canonical NPC `Ronvotri.Cardcha_MiMi`
- requesting Farmer live friendship gate in multiplayer
- no Cardcha private save/service access
- signature `BROOMTAIL SIGIL`

### Sudoku

- canonical NPC `ronvotri.HeyYoureCursed_Sudoku`
- signature `NINEFOLD SEAL`
- Team Up movement markers:
  - `Ronvotri.TeamUp/PartyControlled = true`
  - `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`
- source mod remains story/trust/roommate authority.

## Core regression locks

- hard leash 12 tiles
- target lock 45 ticks
- facing hold 10 ticks
- anti-spin
- Hold Position no chase outside attack range
- Aggressive never disables hard leash
- Boss Focus only picks highest MaxHealth among already-valid candidates
- Surge safe GreenSlime overlay
- Cardcha arena Surge exclusion
- Surge safe placement uses `isTileOnMap`, `isTilePassable`, `IsTileBlockedBy`
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge
- no arbitrary custom-monster cloning through `Activator.CreateInstance` / `MemberwiseClone`
- 51 SVE/RSV profiles/icons/balance
- Party Vault drag/drop / `releaseLeftClick`
- Origin story

## Authoritative Alpha 6.6.4 CI

Authoritative run:

`33958778850`

Authoritative input commit:

`2aed2ca2a7854ad22f1290f25abeabfd57ac3e57`

Warning-clean materialization commit before authoritative run:

`dc0cd99`

Result:

- direct `BuildV0_2Alpha664.ps1`
- build success
- 0 warnings
- 0 errors
- controller input echo guard acceptance PASS
- transactional equip/unequip HUD confirmation PASS
- inventory double-click 450 ms regression PASS
- Alpha 6.6.3 regression acceptance PASS
- package verification PASS
- `No materialized source diff.`
- artifact upload PASS

Package:

`TeamUp_v0.2.0-alpha.6.6.4_CONTROLLER_EQUIPMENT_TRANSACTION_HOTFIX_TEST.zip`

Package SHA256:

`5459cad0a9c4fa72e00a55fa8ff71ef0876a18f22656e1cbb9ed4fed0fc35911`

Artifact ID:

`9967245555`

Artifact wrapper digest:

`sha256:be4a75914af8d5d0277060b5b13c4790052fa4676112ca309e6bcf899cc690d9`

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_4_CONTROLLER_EQUIPMENT_TRANSACTION_HOTFIX_VI.txt`

Highest priority:

1. Select an eligible weapon with D-pad/left stick and press `A` once. Exactly one equip transition should occur and the NPC slot must visibly contain the item.
2. Use right stick to place controller cursor over an eligible item, press `A` once, and verify the correct item is committed.
3. Hold or quickly repeat `A`. It must not create a rapid equip/unequip pair.
4. Confirm the old paired HUD messages `Đã trang bị` then `Đã tháo` no longer appear for one controller action.
5. Controller `X` and the explicit Unequip button must still work.
6. Mouse double-click on Farmer inventory items must still equip within 450 ms.
7. Re-test Codex analog, Pelipper 2/2, water/bridge performance, Tactics, Sudoku, MiMi, Surge, and Vault.

Do **not** call the controller equipment bug live-verified until the user confirms these in game.
