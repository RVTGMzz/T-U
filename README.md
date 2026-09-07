# Team Up!

**Party & Combat Companions for Stardew Valley**

> Build an RPG-style party from Stardew Valley NPCs, bring creature companions, assign combat roles, share loot, and take the team into monster-heavy content.

## Current development checkpoint

Current verified source line: **`v0.2.0-alpha.6.6.9` - Companion Flicker + Thin Health Bars Hotfix**.

Status: **compile/package/direct-builder verified with 0 warnings, 0 errors and no materialized source diff. Alpha 6.6.7 water/bridge performance is user-confirmed; Alpha 6.6.8 land-safe behavior and Alpha 6.6.9 companion flicker/health UI require final live validation.**

Resume development from:

- `CONTINUE_HERE.md`
- `handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_9_2026-09-05.md`

## Alpha 6.6.9

### Pelipper source render/movement authority

Live testing showed flicker on both Farmer-owned Pokemon and NPC-linked Pokemon. The common cause was Team Up and Pelipper Town potentially competing over source-owned actor visibility/movement state.

Alpha 6.6.9 changes the contract:

- Team Up no longer continuously sets Pelipper Pokemon `IsInvisible`.
- Team Up no longer calls `Halt()` or clears Pelipper `controller` / `temporaryController` during deployment reconciliation.
- Pelipper Town remains render and movement authority for its source-owned Pokemon.
- Team Up keeps quota/deployment bookkeeping through soft markers only:
  - `Ronvotri.TeamUp/PelipperDeployment = Active|Standby`
  - `Ronvotri.TeamUp/PelipperDeploymentOwner = <owner>`
- Legacy visibility suppression written by pre-6.6.9 Team Up builds is restored once on load and is never re-applied by the new runtime.

If Pelipper Town does not yet consume the soft `Standby` marker, a standby actor may remain visually present. The correct follow-up is a small Pelipper-side handshake, not restoring Team Up visibility hacks.

### NPC health presentation

Team Up already had real persistent NPC health. Alpha 6.6.9 exposes it in-game without adding a second fake HP system.

- compact party HUD on the left for up to 5 active NPCs;
- HUD health bar height: **5 px**;
- contextual overhead health bar height: **4 px**;
- overhead bars appear when the NPC is wounded, downed, or near a valid combat target;
- full-health NPCs outside combat do not carry a permanent overhead bar;
- colors communicate healthy / caution / danger / downed state;
- no verbose `100/100` text over NPC heads;
- host broadcasts a party snapshot only when health/downed/state signature changes, so farmhands can receive health changes without per-frame network spam.

## Performance + land safety preserved

Alpha 6.6.7 and 6.6.8 remain locked:

- no `isTileLocationTotallyClearAndPlaceable` in FollowService or CombatService;
- combat unreachable-path retry cooldown = 24 ticks;
- combat movement pulse = 3 ticks;
- Pelipper decorative/source actors excluded from hostile Team Up targeting by default;
- humanoid Team Up NPCs reject bare-water destinations;
- real bridge/walkway tiles remain allowed;
- stranded humanoid NPCs can be rescued to safe land/bridge positions.

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

Semantic controller input remains locked:

- Stardew/SMAPI Action Button activates/equips;
- Stardew/SMAPI Use Tool Button unequips;
- controller activation debounce: 180 ms;
- virtual mouse echo suppression: 260 ms;
- inventory mouse double-click equip: 450 ms;
- transactional equipment validation retained.

## Codex / Profile UI

- D-pad and left analog move exactly one Codex profile per input.
- Character Profile detailed content scale remains `1.52f` with scrolling.
- ASCII-safe punctuation avoids hollow-star fallback glyphs.

## Compatibility

### MiMi

- Cardcha source: `Ronvotri.Cardcha`
- canonical NPC: `Ronvotri.Cardcha_MiMi`
- signature: `BROOMTAIL SIGIL`
- requesting Farmer live friendship gate in multiplayer
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

`BuildV0_2Alpha669.ps1`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_9_COMPANION_FLICKER_HEALTH_BARS_VI.txt`

Authoritative CI run:

`33974465552`

Authoritative input commit:

`ad214367f06fee1612818dc7d9e340f577a0cd90`

First materialized 6.6.9 source commit:

`b1ce92b`

Package:

`TeamUp_v0.2.0-alpha.6.6.9_COMPANION_FLICKER_HEALTH_BARS_HOTFIX_TEST.zip`

Package SHA256:

`efde24f02f12a7fbc23378cba4af2d7cdb8da2a6675ebd7a45cbaebd8e1e2949`

Artifact ID:

`9971897681`

Artifact wrapper digest:

`sha256:729faece12dffef01beb3d22e2e1855927c77662ba461a6fd54da6c061a76741`

Authoritative builder result: `No materialized source diff.`

## Independent development / clean-room rule

Team Up! is an independent codebase. Do not copy or redistribute code, DLLs, assets, translations, UI assets, or dialogue from unrelated closed implementations. Compatibility work should stay source-respecting and prefer documented/runtime contracts over private save coupling.

## Naming

- **Display name:** Team Up!
- **Repository:** `ronvotri/Team-Up`
- **SMAPI UniqueID:** `Ronvotri.TeamUp`
- **Working subtitle:** *Party & Combat Companions for Stardew Valley*