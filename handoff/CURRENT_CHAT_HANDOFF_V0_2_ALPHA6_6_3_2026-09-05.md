# Team Up v0.2.0-alpha.6.6.3 Handoff

Date: 2026-09-05

## Resume point

Development branch:

`v0.2-alpha6-6-3-live-test-hotfix`

Final handoff branch:

`v0.2-alpha6-6-3-live-test-hotfix-handoff`

Version:

`0.2.0-alpha.6.6.3`

This milestone is **compile/package/direct-builder verified**. It is **not yet live-verified** for the four runtime fixes below.

## Why this hotfix exists

Alpha 6.6.2 was tested in game and four concrete issues were reported:

1. Equipment worked with mouse double-click but controller could not reliably equip.
2. Codex right-stick scrolling and logical NPC selection diverged, causing the list to jump back when left stick was used. Left-stick list movement also felt too slow.
3. Pelipper Town villager Pokemon were not included in Team Up's shared 2/2 companion quota, allowing e.g. four NPCs + four Pokemon.
4. Full parties caused strong lag around water, bridges and narrow routes.

Alpha 6.6.3 intentionally fixes only these runtime issues plus required diagnostics. Do not expand into formation/per-member strategy before live validation unless explicitly requested.

## Fix 1: controller equipment

File:

`src/TeamUp/UI/EquipmentMenu.cs`

Behavior:

- D-pad / left stick uses logical focus.
- Right stick uses controller-pointer intent.
- One `A` equips the focused item in logical-focus mode.
- One `A` equips the item under controller cursor in pointer mode.
- Switching from pointer back to D-pad/left stick prevents stale cursor activation.
- `X` unequip and `Y` auto-equip remain.
- mouse double-click remains `450 ms`.

Important tokens:

- `_preferFocusedGamepadActivation`
- `_lastHoverPoint`
- `TryActivateControllerPointer()`
- `EquipInventoryIndex(i)`
- `DoubleClickWindowMs = 450`

## Fix 2: Codex analog navigation

File:

`src/TeamUp/UI/CodexBrowserMenu.cs`

Behavior:

- right-stick scroll step is 2 rows;
- scroll clamps `_selectedIndex` into visible `[firstVisible,lastVisible]`;
- left-stick Up/Down moves two rows;
- D-pad Up/Down remains one row;
- dropdown navigation remains one option at a time;
- touching left stick after deep right-stick scroll should no longer jump to stale NPC selection above the viewport.

Important tokens:

- `int scrollStep = direction < 0 ? 2 : -2`
- `firstVisible`
- `lastVisible`
- `MoveVertical(-2)`
- `MoveVertical(2)`

## Fix 3: Pelipper Town Pokemon enter shared 2/2 quota

Provider:

`Griff.PelipperTown`

Files:

- `src/TeamUp/Core/PelipperTownCompatibilityService.cs`
- `src/TeamUp/Core/CompanionIntegrationService.cs`
- `src/TeamUp/ModEntry.Alpha663.cs`
- integration hooks in `src/TeamUp/ModEntry.Alpha661.cs`

Design:

- optional integration, no hard Pelipper DLL reference;
- does not read Pelipper private save data;
- generic Team Up `Ronvotri.TeamUp/*` companion contract remains first priority;
- fallback detects live Pelipper actors using runtime type/assembly/modData/sprite identity, owner metadata, and conservative same-location proximity for villager partners;
- obvious wild actors are excluded;
- detected Pelipper Pokemon are registered as `ExternalCreature` and therefore use the same existing hard max 2 shared quota;
- every 30 ticks on host, previous 6.6.2 party members are reconciled into the quota;
- max two deployed; overflow becomes Standby/suppressed;
- Pelipper remains movement authority for its Pokemon; Team Up FollowService skips source-controlled Pelipper units;
- leaving the owning NPC clears Team Up opt-out/suppression and restores source actor visibility/control;
- SaveLoaded and DayEnding clean stale suppression.

Runtime Team Up Pelipper markers:

- `Ronvotri.TeamUp/PelipperCompanionOptOut`
- `Ronvotri.TeamUp/PelipperSuppressed`
- `Ronvotri.TeamUp/PelipperSuppressedOwner`

Diagnostics:

```text
teamup_pelipper status
teamup_pelipper reconcile
```

If visible Pelipper partners return `detectedPartners=0`, do not weaken quota and do not hard-code Pokemon species. Get a fresh SMAPI log + diagnostic output and bind to actual Pelipper runtime/API shape in a focused follow-up.

## Fix 4: water/bridge/narrow performance

File:

`src/TeamUp/Following/FollowService.cs`

Coordinator cadence:

`src/TeamUp/ModEntry.Alpha661.cs`

Before hotfix, Team Up follower formation did a radius-square fallback up to radius 3 and called `isTileLocationTotallyClearAndPlaceable` repeatedly, with Follow.Update every 2 ticks. This can become expensive when several formation offsets repeatedly fail around water, cliffs, bridge tiles or narrow passages.

Alpha 6.6.3:

- Follow.Update every 4 ticks;
- bounded deterministic `OpenSearchOffsets`;
- Follow `IsOpen` uses `location.isTileOnMap(tile) && location.isTilePassable(tile)`;
- `isTileLocationTotallyClearAndPlaceable` is removed from FollowService;
- Pelipper source-owned Pokemon are excluded from Team Up follower pathfinding to avoid dual movement controllers;
- hard warp/catch-up behavior remains.

Do not confuse this with Surge placement. Surge remains independently regression-locked.

## Locked party model

### People

- total 6 across online Farmers + Following/Waiting NPCs;
- Farmer counts as a person slot;
- single player max 5 active NPCs;
- 2-player max 4 active NPCs;
- overflow becomes Inactive, preserving roster/progression/equipment;
- ownership via RecruiterId;
- one NPC cannot have two recruiters.

### Combat companions

- hard shared maximum 2 external creatures/summons/Pokemon across whole farm;
- Farmer-owned + NPC-linked share same pool;
- Active/Waiting/ReturningHome reserve slots;
- Standby/Inactive do not;
- vanilla pet free;
- ChaCha free and never Main Party;
- Pelipper Town Pokemon must now use this same pool.

### Recruit choice

If linked companion detected:

1. NPC only
2. NPC + companion
3. Cancel

At 2/2, existing replacement flow is used.

## Party Strategy retained

- Balanced
- Defensive
- Aggressive
- HoldPosition
- BossFocus

Tactics UI remains in Codex. Multiplayer strategy remains host-authoritative through Alpha 6.6.2 request/state network messages.

## Custom compatibility locks

### MiMi

- Cardcha UniqueID `Ronvotri.Cardcha`
- canonical NPC `Ronvotri.Cardcha_MiMi`
- requesting Farmer live friendship gate in multiplayer
- no Cardcha private save/service access
- `BROOMTAIL SIGIL`

### Sudoku

- canonical NPC `ronvotri.HeyYoureCursed_Sudoku`
- `NINEFOLD SEAL`
- Team Up movement markers:
  - `Ronvotri.TeamUp/PartyControlled = true`
  - `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`
- source mod retains story/trust/roommate authority.

## Core regression locks

- hard leash 12 tiles
- target lock 45 ticks
- facing hold 10 ticks
- anti-spin
- Aggressive never disables hard leash
- Hold Position no chase outside attack range
- Boss Focus only highest MaxHealth among already-valid candidates
- Surge safe GreenSlime overlay
- Cardcha arena Surge exclusion
- Surge safe placement `isTileOnMap + isTilePassable + IsTileBlockedBy`
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge
- no arbitrary custom monster cloning via `Activator.CreateInstance` / `MemberwiseClone`
- 51 SVE/RSV profiles/icons/balance
- Party Vault drag/drop + `releaseLeftClick`
- Equipment double-click 450 ms
- Origin story

## Build pipeline

Builder:

`BuildV0_2Alpha663.ps1`

One-click launcher:

`BUILD_V0_2_ALPHA6.bat`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_3_LIVE_TEST_HOTFIX_VI.txt`

### First materialization run

Run:

`33954651182`

First materialized source commit:

`c45c8626aaccecce4cb8893e66c0fd9f99d15015`

### Final authoritative run

Run:

`33954805549`

Authoritative input commit:

`59cfa37002172f755c6345730027513d3f54c9be`

Cleanup commit included immediately before authoritative input:

`4598ff51b888aba7fbd0586b16f7d2c5b8fb1a28`

Result:

- direct `BuildV0_2Alpha663.ps1`
- build success
- 0 warnings
- 0 errors
- source acceptance PASS
- controller equipment pointer/focus bridge PASS
- Codex analog viewport sync/speed PASS
- Pelipper Town shared 2/2 adapter PASS
- water/narrow follower performance guards PASS
- Alpha 6.6.2/6.6.1 regressions PASS
- package verification PASS
- `No materialized source diff.`
- artifact upload PASS

Package:

`TeamUp_v0.2.0-alpha.6.6.3_LIVE_TEST_HOTFIX_TEST.zip`

Authoritative package SHA256:

`266e4226c09a4c39710f46a1083f90f32a11c392f35589a6c0e3aa5161b7c092`

Artifact ID:

`9965997611`

Artifact wrapper digest:

`sha256:1a3164bd486d3af23008075ad99310f95282dbe0b8fb767a8c7d88cd3fdf43cd`

## Live validation priority

1. Equipment one-A equip using D-pad/left-stick focus.
2. Equipment one-A equip using right-stick controller cursor.
3. Switch pointer -> D-pad and verify no stale cursor activation.
4. Codex deep-scroll right stick then left-stick movement, no snap-back.
5. Left stick feels faster while D-pad stays precise.
6. Recruit Pelipper NPC + Pokemon to 1/2 then 2/2; third Pokemon must replacement/Standby, never 3/2.
7. On old 6.6.2 save with 4 NPC + 4 Pokemon, wait ~1 second or run `teamup_pelipper reconcile`; max two should remain deployed.
8. Leave Pelipper-backed NPC and verify Pokemon returns normally.
9. Run water/bridge/narrow route with full party and compare lag to 6.6.2.
10. Re-test Tactics, People 6 cap, multiplayer authority, MiMi, Sudoku, Surge, Vault.

Do not call Pelipper compatibility or performance live-verified until the user confirms this smoke in game.
