# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.7**

Status: **compile/package/direct-builder verified with 0 warnings, 0 errors and no materialized source diff; water/bridge performance still requires real in-game validation.**

Development branch:

`v0.2-alpha6-6-7-water-combat-pathfinding-hotfix`

Final handoff branch:

`v0.2-alpha6-6-7-water-combat-pathfinding-hotfix-handoff`

Read first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_7_2026-09-05.md`

## Why Alpha 6.6.7 exists

Live testing still showed severe lag at some river/bridge/narrow-path locations with a full Team Up party.

Inspection found a second expensive pathfinding path outside FollowService: `CombatService` was still scanning approach tiles with `isTileLocationTotallyClearAndPlaceable` and could rebuild unreachable paths repeatedly. Pelipper Town water/decorative actors represented as monsters could also be interpreted as combat targets even when they were source-owned/non-hostile.

## Alpha 6.6.7 fixes

### Pelipper combat target filter

`PelipperTownCompatibilityService` now exposes:

- `Ronvotri.TeamUp/CombatTarget`
- `ShouldExcludeFromTeamUpCombat(NPC actor)`

Pelipper actors are excluded from Team Up combat by default. A provider can explicitly opt a specific actor into Team Up combat by setting `Ronvotri.TeamUp/CombatTarget=true`.

This filter applies to the main target list and Team Up signature/AoE monster queries.

### Lightweight combat pathfinding

`CombatService` no longer contains `isTileLocationTotallyClearAndPlaceable`.

Approach tiles use:

`location.isTileOnMap(tile) && location.isTilePassable(tile)`

If no legal approach tile exists, movement fails closed and enters a retry cooldown instead of creating a path toward water/blocked terrain.

### Path retry throttling

- unreachable combat path retry cooldown: `24` ticks;
- combat movement path creation pulse: every `3` ticks;
- combat cooldowns, targeting and attacks continue normally between movement pulses.

This targets CPU spikes without changing the five Party Strategy values, hard leash, target lock or facing hold.

## Switch controller state retained

Alpha 6.6.6 semantic controller bridge remains:

- `IsActionButton()` -> equip/activate;
- `IsUseToolButton()` -> unequip;
- transactional equipment commit checks retained;
- controller debounce 180 ms;
- virtual mouse echo suppression 260 ms;
- inventory mouse double-click 450 ms.

Equip is already user-confirmed working. Unequip still needs user confirmation after installing a 6.6.6+ build.

## UI regressions retained

- Codex D-pad and left analog move exactly one profile per input;
- Character Profile detail scale = `1.52f`;
- ASCII-safe punctuation removes hollow-star fallback glyphs.

## Product rules retained

- 6 total people across online Farmers + active NPCs;
- shared external Pokemon/summon companion cap = 2;
- Farmer and NPC companions share the same 2/2 pool;
- vanilla pet and ChaCha are free;
- Party Strategy: Balanced, Defensive, Aggressive, HoldPosition, BossFocus;
- MiMi: `Ronvotri.Cardcha_MiMi`, `BROOMTAIL SIGIL`;
- Sudoku: `ronvotri.HeyYoureCursed_Sudoku`, `NINEFOLD SEAL`;
- Sudoku Team Up marker: `Ronvotri.TeamUp/PartyControlled = true`;
- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- Surge safe placement remains `isTileOnMap + isTilePassable + IsTileBlockedBy`;
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge/Follow/Combat;
- Cardcha arena Surge exclusion retained;
- 51 SVE/RSV profiles/icons/balance retained;
- Party Vault drag/drop retained;
- Origin story retained.

## Authoritative checkpoint

Materialized source commit:

`d97b361`

Authoritative input commit:

`a430575c20158624e3662679387f078722fafba8`

Authoritative CI run:

`33964808233`

Result:

- `BuildV0_2Alpha667.ps1` success;
- 0 warnings;
- 0 errors;
- Pelipper combat target filter PASS;
- lightweight combat approach scan PASS;
- unreachable path retry cooldown PASS;
- combat movement pulse PASS;
- Switch equip/unequip regression PASS;
- Codex/Profile/Surge regression PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.7_WATER_COMBAT_PATHFINDING_HOTFIX_TEST.zip`

Package SHA256:

`228ed7a54d077a3cbf944b856a334ab3f62dbe818bc71bc793b08597c77abde2`

Artifact ID:

`9969080935`

Artifact wrapper digest:

`sha256:1ee65498ece6abd844a1ed56adc22823fecb7c9ffeda92a25f5a0e111c4bd03e`

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_7_WATER_COMBAT_PATHFINDING_HOTFIX_VI.txt`

Highest priority:

1. Revisit the exact river/bridge location that lagged badly with a 4-6 person party.
2. Test while Pelipper water actors are visible and verify Team Up does not chase/attack them.
3. Enter real land combat and verify NPCs still approach and attack reachable monsters.
4. Verify unreachable targets no longer cause sustained frame-time spikes.
5. Re-test Switch equip and Use Tool unequip.
6. Re-test Codex one-row navigation, Tactics, Pelipper 2/2, MiMi, Sudoku, Surge and Vault.

Do not call the water-performance fix live-verified until the user confirms it in game.