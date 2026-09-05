# Team Up!

**Party & Combat Companions for Stardew Valley**

> Build an RPG-style party from Stardew Valley NPCs, bring creature companions, assign combat roles, share loot, and take the team into monster-heavy content.

## Current development checkpoint

Current verified source line: **`v0.2.0-alpha.6.6.7` - Water Combat Pathfinding Hotfix**.

Status: **compile/package/direct-builder verified with 0 warnings, 0 errors and no materialized source diff. Water/bridge performance still requires live in-game validation.**

Resume development from:

- `CONTINUE_HERE.md`
- `handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_7_2026-09-05.md`

## Alpha 6.6.7

This hotfix targets severe frame-time spikes reported around rivers, bridges and narrow routes with a full party.

### Combat pathfinding

- Removed `isTileLocationTotallyClearAndPlaceable` from Team Up combat approach searching.
- Approach tiles now use lightweight `isTileOnMap + isTilePassable` checks.
- Unreachable/no-path targets enter a 24-tick retry cooldown instead of rebuilding paths every frame.
- Combat movement path creation is pulsed every 3 ticks while targeting, cooldown and attack logic continues normally.

### Pelipper Town water/decorative actors

Pelipper source-owned actors are excluded from Team Up hostile combat targeting by default. This prevents party NPCs from repeatedly trying to reach non-hostile or unreachable Pokemon/world actors in water.

Optional provider opt-in:

`Ronvotri.TeamUp/CombatTarget=true`

The filter applies to both the main combat target list and Team Up signature/AoE monster queries.

## Party Tactics

The five strategy values remain:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Tactics UI remains in Codex and multiplayer strategy remains host-authoritative.

## Current party rules

### People capacity: 6 total

The six-person cap includes online Farmers and active Team Up NPCs together.

```text
Single-player: 1 Farmer + up to 5 active NPCs = 6/6
Two-player co-op: 2 Farmers + up to 4 active NPCs = 6/6
Four-player co-op: 4 Farmers + up to 2 active NPCs = 6/6
```

### Combat companion capacity: 2 shared

The farm has one shared pool of two deployed external Pokemon/summon/creature companions across all Farmers and NPCs.

- `Active`, `Waiting`, and `ReturningHome` reserve a slot.
- `Standby` and `Inactive` do not.
- Farmer-owned and NPC-linked creatures share the pool.
- Vanilla dog/cat pets are free.
- ChaCha is free and never enters Main Party.
- Pelipper Town companions use this same shared quota when detected.

## Switch controller

Semantic input from Alpha 6.6.6 remains:

- Stardew/SMAPI Action Button activates/equips.
- Stardew/SMAPI Use Tool Button unequips.
- controller activation debounce: 180 ms;
- virtual mouse echo suppression: 260 ms;
- inventory mouse double-click equip: 450 ms;
- transactional equipment state validation retained.

Equip has been user-confirmed working. Unequip still requires live confirmation on the user's Switch controller.

## Codex / Profile UI

- D-pad and left analog move one Codex profile per input.
- Character Profile detailed content scale is `1.52f` with scrolling.
- ASCII-safe punctuation avoids hollow-star fallback glyphs.

## Compatibility

### MiMi

- Cardcha source: `Ronvotri.Cardcha`
- canonical NPC: `Ronvotri.Cardcha_MiMi`
- signature: `BROOMTAIL SIGIL`
- Team Up does not read Cardcha private SaveData/services.

### Sudoku

- canonical NPC: `ronvotri.HeyYoureCursed_Sudoku`
- signature: `NINEFOLD SEAL`
- movement marker while Team Up controls Sudoku:
  - `Ronvotri.TeamUp/PartyControlled = true`
  - `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`

## Regression locks

- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- anti-spin;
- Hold Position no chase outside attack range;
- Aggressive never disables hard leash;
- Boss Focus prioritizes highest MaxHealth only among valid candidates;
- Surge Cardcha arena exclusion;
- Surge safe placement uses `isTileOnMap + isTilePassable + IsTileBlockedBy`;
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge, Follow or Combat;
- no arbitrary custom-monster cloning;
- 51 SVE/RSV profiles/icons/balance;
- Party Vault drag/drop;
- Origin story retained.

## Build

One-click local build:

`BUILD_V0_2_ALPHA6.bat`

Direct builder:

`BuildV0_2Alpha667.ps1`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_7_WATER_COMBAT_PATHFINDING_HOTFIX_VI.txt`

Authoritative CI run:

`33964808233`

Authoritative input commit:

`a430575c20158624e3662679387f078722fafba8`

Materialized gameplay source:

`d97b361`

Package:

`TeamUp_v0.2.0-alpha.6.6.7_WATER_COMBAT_PATHFINDING_HOTFIX_TEST.zip`

Package SHA256:

`228ed7a54d077a3cbf944b856a334ab3f62dbe818bc71bc793b08597c77abde2`

Artifact ID:

`9969080935`

Artifact wrapper digest:

`sha256:1ee65498ece6abd844a1ed56adc22823fecb7c9ffeda92a25f5a0e111c4bd03e`

Authoritative builder result: `No materialized source diff.`

## Independent development / clean-room rule

Team Up! is an independent codebase. Do not copy or redistribute code, DLLs, assets, translations, UI assets, or dialogue from unrelated closed implementations. Compatibility work should stay source-respecting and prefer documented/runtime contracts over private save coupling.

## Naming

- **Display name:** Team Up!
- **Repository:** `ronvotri/Team-Up`
- **SMAPI UniqueID:** `Ronvotri.TeamUp`
- **Working subtitle:** *Party & Combat Companions for Stardew Valley*
