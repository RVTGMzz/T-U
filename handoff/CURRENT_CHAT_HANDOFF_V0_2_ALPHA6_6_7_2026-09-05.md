# Team Up v0.2.0-alpha.6.6.7 Handoff

Date: 2026-09-05

## Resume point

Development branch:

`v0.2-alpha6-6-7-water-combat-pathfinding-hotfix`

Final handoff branch:

`v0.2-alpha6-6-7-water-combat-pathfinding-hotfix-handoff`

Version:

`0.2.0-alpha.6.6.7`

Status: compile/package/direct-builder verified. Water/bridge performance still requires live in-game validation.

## Why Alpha 6.6.7 exists

Live testing showed severe lag at some river, bridge and narrow-path locations with a full Team Up party.

Inspection found a second expensive movement path outside FollowService: CombatService still scanned approach tiles with `isTileLocationTotallyClearAndPlaceable` and could repeatedly rebuild paths toward unreachable targets. Pelipper Town source-owned water/decorative actors represented as monsters could also be interpreted as hostile Team Up targets.

## Fixes

### Pelipper combat target filtering

`src/TeamUp/Core/PelipperTownCompatibilityService.cs`

New contract:

`Ronvotri.TeamUp/CombatTarget`

Pelipper actors are excluded from Team Up combat by default. A source/provider can explicitly opt an actor in with `Ronvotri.TeamUp/CombatTarget=true`.

Filtering applies to:

- main Team Up monster target list;
- nearby monster queries used by signature/AoE logic.

### Lightweight combat approach search

`src/TeamUp/Combat/CombatService.cs`

CombatService no longer contains `isTileLocationTotallyClearAndPlaceable`.

Approach candidate validation now uses:

`location.isTileOnMap(tile) && location.isTilePassable(tile)`

If no legal approach tile exists, movement fails closed rather than attempting to path directly to an unreachable water/blocked target.

### Retry and movement throttling

- `CombatPathRetryCooldownTicks = 24`
- `CombatMovementPulseTicks = 3`
- failed/unreachable combat path does not rebuild every frame;
- movement path creation is pulsed every 3 ticks;
- targeting, cooldowns and attacks continue normally between movement pulses.

## Follow performance retained

FollowService remains on the Alpha 6.6.3+ lightweight path:

- bounded `OpenSearchOffsets`;
- no `isTileLocationTotallyClearAndPlaceable`;
- `isTileOnMap + isTilePassable`;
- Follow.Update every 4 ticks;
- source-owned Pelipper companions are not controlled by Team Up follower pathfinding.

## Switch controller retained

Alpha 6.6.6 semantic controller bridge remains:

- `IsActionButton()` -> equip / activate;
- `IsUseToolButton()` -> unequip;
- controller debounce 180 ms;
- virtual mouse echo suppression 260 ms;
- inventory mouse double-click 450 ms;
- transactional equipment commit checks retained.

Equip was user-confirmed working before this milestone. Unequip still needs live confirmation.

## UI regressions retained

- Codex D-pad and left analog move exactly one profile per input;
- Character Profile detail scale = 1.52f;
- unsupported punctuation replaced with ASCII-safe characters, preventing hollow-star fallback glyphs.

## Product locks

- maximum 6 total people across online Farmers plus active NPCs;
- shared deployed external Pokemon/summon/creature companion cap = 2;
- Farmer-owned and NPC-linked creatures share the same 2/2 pool;
- vanilla pet free;
- ChaCha free and never Main Party;
- Party Strategy: Balanced, Defensive, Aggressive, HoldPosition, BossFocus;
- MiMi canonical `Ronvotri.Cardcha_MiMi`, signature `BROOMTAIL SIGIL`;
- Sudoku canonical `ronvotri.HeyYoureCursed_Sudoku`, signature `NINEFOLD SEAL`;
- Sudoku Team Up marker `Ronvotri.TeamUp/PartyControlled = true`;
- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- anti-spin retained;
- Surge Cardcha arena exclusion retained;
- Surge safe placement uses `isTileOnMap + isTilePassable + IsTileBlockedBy`;
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge, Follow or Combat;
- no arbitrary custom-monster cloning;
- 51 SVE/RSV profiles/icons/balance retained;
- Party Vault drag/drop retained;
- Origin story retained.

## Build pipeline

Builder:

`BuildV0_2Alpha667.ps1`

One-click launcher:

`BUILD_V0_2_ALPHA6.bat`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_7_WATER_COMBAT_PATHFINDING_HOTFIX_VI.txt`

### Failed first run

Run `33964658125` failed before gameplay compile because builder quoting inserted a literal backslash into a Pelipper constant. This was generator-only and was fixed with literal here-string insertion.

### Materialization run

Run `33964727567`

Materialized gameplay source commit:

`d97b361`

Result: 0 warnings, 0 errors, source/package acceptance PASS.

### Final authoritative run

Run:

`33964808233`

Authoritative input:

`a430575c20158624e3662679387f078722fafba8`

Result:

- direct `BuildV0_2Alpha667.ps1` success;
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

## Live validation priority

1. Revisit the exact river/bridge screenshot location with 4-6 people.
2. Cross it repeatedly and stand next to water for 10-15 seconds.
3. Verify Team Up does not chase or attack Pelipper water/decorative actors.
4. Enter real land combat and verify reachable monsters are still approached/attacked normally.
5. Verify unreachable targets no longer cause sustained frame-time spikes.
6. Verify Switch Action equip and Use Tool unequip.
7. Re-test Codex one-row navigation, Tactics, Pelipper 2/2, MiMi, Sudoku, Surge and Vault.

If severe lag remains while Team Up is not engaged in combat, collect a fresh `SMAPI-latest.txt` immediately after reproducing it. The next investigation should separate Pelipper runtime/world updates from Team Up Follow/Combat rather than simply reducing tick frequency again.
