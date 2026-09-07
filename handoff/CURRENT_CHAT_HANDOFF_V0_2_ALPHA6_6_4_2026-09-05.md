# Team Up v0.2.0-alpha.6.6.4 Handoff

Date: 2026-09-05

## Resume point

Development branch:

`v0.2-alpha6-6-4-controller-equipment-transaction-hotfix`

Final handoff branch:

`v0.2-alpha6-6-4-controller-equipment-transaction-hotfix-handoff`

Version:

`0.2.0-alpha.6.6.4`

This milestone is **compile/package/direct-builder verified with 0 warnings and 0 errors**. Controller Equipment behavior still requires real in-game confirmation.

## Triggering live bug

User tested Alpha 6.6.3 with controller on George's Equipment screen. Repeated controller equip attempts produced contradictory HUD messages:

- `Đã trang bị Liềm.`
- `Đã tháo Liềm về túi.`

while the NPC Weapon slot still displayed `Chưa trang bị`.

The code path allowed controller activation plus mouse-style activation to overlap. Stardew can echo controller `A` through cursor/mouse behavior, and Team Up also allowed double-click on an NPC equipment slot to unequip. This made a ghost second state transition plausible and made success HUD messages untrustworthy.

## Alpha 6.6.4 fix

File:

`src/TeamUp/UI/EquipmentMenu.cs`

### Input isolation

- `ControllerActivationDebounceMs = 180`
- `ControllerMouseEchoSuppressionMs = 260`
- `TryBeginControllerActivation()` gates `A`, `X`, and `Y`.
- Each controller activation temporarily suppresses its virtual left-click echo.
- Mouse double-click state is reset when controller activation begins.

### Slot-card behavior

NPC equipment slot cards now **only select** a slot.

Removed behavior:

`IsDoubleClick(2000 + i) -> UnequipSelected()`

This must not be restored. Unequip is now explicit through:

- controller `X`, or
- the `Tháo trang bị` button.

### Mouse regression lock

Inventory double-click equip remains:

- `DoubleClickWindowMs = 450`
- `IsDoubleClick(1000 + i)`

### Transactional success HUD

Equip success is shown only after Team Up verifies:

1. PartyMember equipment metadata exists for the target slot.
2. The actual FarmerTeam global equipment inventory slot contains an item.
3. Metadata QualifiedItemId matches the actual stored item QualifiedItemId.
4. The requested item's QualifiedItemId matches the actual stored item QualifiedItemId.

Unequip success is shown only after:

1. PartyMember equipment metadata is null for the slot.
2. The actual FarmerTeam global equipment inventory slot is empty.

If verification fails, Team Up shows an error rather than a fake success toast.

## Alpha 6.6.3 retained

Do not regress:

- Codex right-stick viewport/selection synchronization.
- Left-stick 2-row list movement and D-pad 1-row precision.
- Pelipper Town optional adapter and shared 2/2 external companion quota.
- `teamup_pelipper status|reconcile` diagnostics.
- Follow update every 4 ticks.
- bounded `OpenSearchOffsets`.
- Follow tile validation using `isTileOnMap + isTilePassable`.
- Pelipper source actors skipped by Team Up Follow movement.

## Capacity locks

People:

- max 6 total online Farmers + Following/Waiting NPCs.
- single player: Farmer + up to 5 active NPCs.

Combat companions:

- max 2 deployed external Pokemon/summons/creatures farm-wide.
- Farmer-owned and NPC-linked share same pool.
- Active/Waiting/ReturningHome reserve slots.
- Standby/Inactive do not.
- vanilla farm pet free.
- ChaCha free and never Main Party.

## Strategy locks

- Balanced
- Defensive
- Aggressive
- HoldPosition
- BossFocus

Hard leash remains 12 tiles. Target lock 45 ticks. Facing hold 10 ticks. Anti-spin retained.

## Custom recruit locks

MiMi:

- `Ronvotri.Cardcha_MiMi`
- source `Ronvotri.Cardcha`
- requesting Farmer live friendship gate
- no Cardcha private SaveData/service coupling
- `BROOMTAIL SIGIL`

Sudoku:

- `ronvotri.HeyYoureCursed_Sudoku`
- `NINEFOLD SEAL`
- runtime movement marker `Ronvotri.TeamUp/PartyControlled = true`
- source mod remains story/trust/roommate authority.

## Surge locks

- safe GreenSlime overlay only.
- Cardcha arena excluded.
- placement uses `isTileOnMap`, `isTilePassable`, `IsTileBlockedBy`.
- never restore `isTileLocationTotallyClearAndPlaceable` in Surge.
- no arbitrary monster cloning via `Activator.CreateInstance` / `MemberwiseClone`.

## Build pipeline

Builder:

`BuildV0_2Alpha664.ps1`

Launcher:

`BUILD_V0_2_ALPHA6.bat`

Smoke test:

`SMOKE_TEST_V0_2_ALPHA6_6_4_CONTROLLER_EQUIPMENT_TRANSACTION_HOTFIX_VI.txt`

## CI history

First Alpha 6.6.4 run:

`33958578254`

- functional source/package PASS
- compile had one nullable warning CS8602
- not authoritative.

Warning-clean materialization run:

`33958718107`

- 0 warnings
- 0 errors
- materialized final nullable cleanup commit `dc0cd99`.

Authoritative run:

`33958778850`

Authoritative input commit:

`2aed2ca2a7854ad22f1290f25abeabfd57ac3e57`

Result:

- build success
- 0 warnings
- 0 errors
- controller input echo guard PASS
- transactional equip/unequip HUD confirmation PASS
- inventory double-click 450 ms regression PASS
- Alpha 6.6.3 regression tokens PASS
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

## Live validation priority

1. D-pad/left stick select eligible weapon, `A` once. One equip transition only. Slot must visibly show the item.
2. Right-stick cursor over item, `A` once. Correct item must be committed.
3. Hold or quickly repeat `A`. No rapid equip/unequip pair.
4. No paired `Đã trang bị` then `Đã tháo` for a single controller action.
5. `X` unequip works once.
6. Explicit Unequip button works once.
7. Mouse inventory double-click 450 ms still equips.
8. Re-test Codex analog, Pelipper 2/2, water/bridge performance, Tactics, MiMi, Sudoku, Surge, Vault.

Do not call the controller Equipment fix live-verified until the user confirms it in game.
