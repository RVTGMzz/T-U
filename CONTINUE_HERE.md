# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.3**

Status: **compile/package/direct-builder verified; four live-test fixes require in-game validation**.

Development branch:

`v0.2-alpha6-6-3-live-test-hotfix`

Final handoff branch:

`v0.2-alpha6-6-3-live-test-hotfix-handoff`

Read this handoff first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_3_2026-09-05.md`

## Why Alpha 6.6.3 exists

The first Alpha 6.6.2 in-game test found four concrete runtime problems:

1. Equipment could be equipped by mouse double-click, but controller `A` did not reliably equip.
2. Codex right-stick scrolling moved the viewport without keeping logical selection inside the visible rows; touching left stick then snapped back to an old NPC. Left-stick row navigation also felt too slow.
3. Pelipper Town villager Pokemon were not recognized by Team Up's generic companion contract, so the intended shared 2/2 cap was not enforced in actual gameplay.
4. Full parties produced severe slowdown around water, bridges and narrow walkways.

Alpha 6.6.3 is a focused hotfix for these four items. Do not expand formation/per-member strategy or unrelated features until this hotfix is live-smoked.

## Fix 1: Equipment controller activation

`UI/EquipmentMenu.cs` now distinguishes logical-focus controller navigation from controller-pointer navigation.

Locked behavior:

- D-pad / left stick sets logical-focus mode.
- Right-stick pointer movement sets pointer mode.
- `A` in logical-focus mode activates the focused slot/item.
- `A` in pointer mode equips/activates the item/button under `Game1.getMouseX/Y()`.
- Switching back to D-pad/left stick prevents stale pointer activation.
- `X` unequips and `Y` auto-equips.
- Mouse double-click remains 450 ms.

Key tokens:

- `_preferFocusedGamepadActivation`
- `TryActivateControllerPointer()`
- `EquipInventoryIndex(i)`
- `DoubleClickWindowMs = 450`

## Fix 2: Codex analog navigation

`UI/CodexBrowserMenu.cs` now keeps viewport and selected row synchronized.

Locked behavior:

- right-stick scroll step = 2 rows;
- after scrolling, `_selectedIndex` is clamped to `[firstVisible, lastVisible]`;
- left stick Up/Down moves two rows per input;
- D-pad Up/Down remains one row;
- dropdown option navigation remains precise and should not skip two entries.

Do not reintroduce viewport scrolling that leaves selection outside the visible window.

## Fix 3: Pelipper Town shared 2/2 companion quota

Provider compatibility ID:

`Griff.PelipperTown`

New optional adapter:

`Core/PelipperTownCompatibilityService.cs`

Important rules:

- no hard Pelipper DLL reference;
- no Pelipper private-save reading;
- generic Team Up companion `modData` contract is checked first;
- optional Pelipper fallback detects live actors using runtime identity/owner metadata and conservative proximity;
- villager partner can enter normal recruit flow: NPC only / NPC + Pokemon / Cancel;
- Pelipper Pokemon are stored as `ExternalCreature`, so existing `GetActiveCombatCompanionCount()` and hard max 2 are reused;
- existing 6.6.2 party members are reconciled every 30 ticks on host;
- only max two source Pokemon may remain deployed;
- overflow Pelipper actors become Standby and are visually suppressed while Team Up owns the active party choice;
- Pelipper remains movement authority for its Pokemon; FollowService must skip source-controlled Pelipper units;
- Leave restores source visibility/control and clears Team Up Pelipper opt-out state;
- SaveLoaded/DayEnding cleanup stale suppression.

Diagnostics:

```text
teamup_pelipper status
teamup_pelipper reconcile
```

If visible Pelipper partners produce `detectedPartners=0`, do not weaken the 2/2 limit and do not hard-code Pokemon species. Get a fresh SMAPI log + command output and then bind to the real Pelipper runtime/API shape in the next focused hotfix.

## Fix 4: water / bridge / narrow path performance

The previous follower loop was an actual Team Up CPU-risk area:

- follow runtime updated every 2 ticks;
- formation fallback could scan square rings up to radius 3 per follower;
- each candidate used `isTileLocationTotallyClearAndPlaceable`, which is much heavier and particularly noisy with crowds/narrow terrain.

Alpha 6.6.3 changes:

- `Follow.Update` cadence in Alpha661 coordinator: every 4 ticks;
- bounded deterministic `OpenSearchOffsets`;
- Follow `IsOpen`: `location.isTileOnMap(tile) && location.isTilePassable(tile)`;
- `isTileLocationTotallyClearAndPlaceable` must not exist in FollowService;
- Pelipper source-owned companions are skipped by Team Up follow movement so two pathfinding controllers do not fight.

Long-distance warp/catch-up remains. Surge safety rules are separate and remain unchanged.

## Product rules still locked

### Shared people capacity

- 6 total people across online Farmers + `Following`/`Waiting` NPCs.
- Single-player: 1 Farmer + at most 5 active NPCs.
- Two-player: 2 Farmers + at most 4 active NPCs.
- overflow becomes Inactive, never deleted from roster/progression/equipment.
- each NPC retains `RecruiterId`.
- same NPC cannot have two owners.

### Shared combat companion capacity

- hard max 2 deployed external creatures across the whole farm.
- Farmer-owned and NPC-linked external creatures share the same pool.
- `Active`, `Waiting`, `ReturningHome` reserve slots.
- `Standby`, `Inactive` do not.
- vanilla pet free.
- ChaCha free and never Main Party.
- detected Pelipper Town Pokemon now enter this same pool.

### Party Strategy

Five values remain unchanged:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Tactics UI remains in Codex. Multiplayer strategy remains host-authoritative via Alpha 6.6.2 request/state messages.

## Custom recruit locks

### MiMi

- canonical ID `Ronvotri.Cardcha_MiMi`;
- source Cardcha `Ronvotri.Cardcha`;
- requesting Farmer's live friendship is checked in multiplayer;
- Team Up does not read Cardcha private SaveData/services/schedule;
- signature `BROOMTAIL SIGIL`.

### Sudoku

- canonical ID `ronvotri.HeyYoureCursed_Sudoku`;
- signature `NINEFOLD SEAL`;
- Team Up runtime movement markers:
  - `Ronvotri.TeamUp/PartyControlled = true`
  - `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`
- source mod remains story/trust/roommate authority.

## Regression locks

- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- anti-spin;
- Hold Position no chase outside attack range;
- Aggressive never disables hard leash;
- Boss Focus only selects highest MaxHealth among valid candidates;
- Surge Cardcha arena exclusion;
- Surge safe placement uses `isTileOnMap`, `isTilePassable`, `IsTileBlockedBy`;
- never restore `isTileLocationTotallyClearAndPlaceable` in Surge;
- no arbitrary custom-monster cloning with `Activator.CreateInstance` or `MemberwiseClone`;
- 51 SVE/RSV profiles/icons/balance;
- Party Vault drag/drop and `releaseLeftClick`;
- equipment mouse double-click 450 ms;
- Origin story.

## CI checkpoint

Authoritative direct-builder run:

`33954805549`

Authoritative input commit:

`59cfa37002172f755c6345730027513d3f54c9be`

First Alpha 6.6.3 materialized source commit:

`c45c8626aaccecce4cb8893e66c0fd9f99d15015`

Final cleanup before authoritative run:

`4598ff51b888aba7fbd0586b16f7d2c5b8fb1a28`

Result:

- direct `BuildV0_2Alpha663.ps1`;
- build success;
- 0 warnings;
- 0 errors;
- controller equipment source acceptance PASS;
- Codex analog sync/speed acceptance PASS;
- Pelipper Town shared 2/2 adapter acceptance PASS;
- water/narrow follower performance acceptance PASS;
- Alpha 6.6.2 / 6.6.1 regressions PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.3_LIVE_TEST_HOTFIX_TEST.zip`

Authoritative package SHA256:

`266e4226c09a4c39710f46a1083f90f32a11c392f35589a6c0e3aa5161b7c092`

Artifact ID:

`9965997611`

Artifact wrapper digest:

`sha256:1a3164bd486d3af23008075ad99310f95282dbe0b8fb767a8c7d88cd3fdf43cd`

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_3_LIVE_TEST_HOTFIX_VI.txt`

Highest priority:

1. Equipment: logical focus + one `A` equips; right-stick cursor + one `A` equips correct item; switching modes never activates stale item.
2. Codex: right-stick deep scroll then left-stick movement must not jump back upward; left stick should feel faster, D-pad precise.
3. Pelipper: recruit NPC with visible partner and verify 3 choices. Build to 2/2, then third companion must trigger replacement/Standby rather than 3/2.
4. Old 6.6.2 save with 4 NPC + 4 Pokemon: wait for reconcile or run `teamup_pelipper reconcile`; no more than two source Pokemon should stay deployed.
5. Water/bridge/narrow route: compare frame-time/FPS against 6.6.2 with a full party.
6. Leave a Pelipper-backed NPC and verify its source Pokemon returns normally.
7. Re-test Tactics, shared people cap, multiplayer authority, MiMi, Sudoku, Surge, Vault.

Do **not** call the Pelipper integration or performance fix live-verified until this real in-game smoke passes.
