# Team Up v0.2.0-alpha.6.6.8 Handoff

Date: 2026-09-05

## Resume point

Development branch:

`v0.2-alpha6-6-8-land-safe-follow-targets`

Final handoff branch:

`v0.2-alpha6-6-8-land-safe-follow-targets-handoff`

Version:

`0.2.0-alpha.6.6.8`

Status: compile/package/direct-builder verified. Live test pending.

## User-confirmed state before this hotfix

Alpha 6.6.7 successfully removed the severe lag at the reported river/bridge area. The new regression was visual/movement correctness: multiple recruited humanoid NPCs could stand in the river because the lightweight map/passable check accepted water tiles on that map.

## Root cause

Alpha 6.6.7 deliberately removed expensive `isTileLocationTotallyClearAndPlaceable` calls. `FollowService.IsOpen()` and combat approach validation then relied on lightweight map/passable checks. Some water tiles can satisfy those checks, and `FindOpenNear()` could also fall back to an invalid preferred formation tile.

## Alpha 6.6.8 implementation

### PartyTileSafety

New file:

`src/TeamUp/Core/PartyTileSafety.cs`

`IsWalkableLandOrBridge(GameLocation, Vector2)`:

1. validates coordinates;
2. requires `isTileOnMap`;
3. requires `isTilePassable`;
4. accepts ordinary non-water tiles;
5. if `isWaterTile(x,y)` is true, only accepts the coordinate when the Buildings layer has a real tile there, preserving bridges/walkways over water.

The helper compiled successfully against the current Stardew reference assemblies.

### Human party formation

`FollowService` now uses nullable `FindPlayerFollowTile()` and `FindLandOpenNear()` for recruited humanoid party members.

- formation candidates use `PartyTileSafety`;
- if no safe land/bridge tile exists, no invalid fallback is returned;
- a Farmer using modded traversal/mount movement over water no longer drags humanoid NPCs into the river;
- companions/Pokemon still use their existing provider/source movement semantics rather than a blanket land-only rule.

### Stranded-NPC rescue

Before normal follow movement, if a recruited humanoid NPC is already standing on an unsafe bare-water tile and a safe formation target exists, Team Up warps that NPC once to the safe formation target and applies the normal repath cooldown.

This repairs screenshots/save/runtime states produced by 6.6.7 instead of waiting for NPC pathfinding to escape water.

### Combat

`CombatService.IsLightweightCombatTile()` delegates to `PartyTileSafety.IsWalkableLandOrBridge()`.

NPCs can still approach real land monsters with lightweight pathfinding, but combat approach coordinates can no longer be bare water.

## Performance safeguards retained

- no `isTileLocationTotallyClearAndPlaceable` in FollowService;
- no `isTileLocationTotallyClearAndPlaceable` in CombatService;
- no `isTileLocationTotallyClearAndPlaceable` in Surge;
- Pelipper water/decorative actors remain excluded from Team Up combat unless explicitly opted in;
- unreachable combat path retry = 24 ticks;
- combat path movement pulse = every 3 ticks;
- FollowService bounded fallback offsets retained.

## Other locked regressions

- Switch semantic Action equip retained;
- Switch semantic Use Tool unequip retained;
- equipment debounce 180ms;
- virtual mouse echo suppression 260ms;
- mouse inventory double-click 450ms;
- Codex one profile per D-pad/left-stick input;
- Character Profile content scale 1.52f;
- ASCII-safe punctuation, no hollow-star fallback glyph;
- 6 total people including online Farmers;
- 2 shared external Pokemon/summon slots;
- vanilla pet free;
- ChaCha free and never Main Party;
- five Party Strategy values unchanged;
- MiMi `Ronvotri.Cardcha_MiMi` / `BROOMTAIL SIGIL`;
- Sudoku `ronvotri.HeyYoureCursed_Sudoku` / `NINEFOLD SEAL`;
- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- Surge safe GreenSlime overlay and Cardcha arena exclusion;
- no arbitrary custom monster cloning;
- 51 SVE/RSV profiles/icons/balance;
- Party Vault drag/drop;
- Origin story.

## Build checkpoint

Materialized source commit:

`b93fc29`

Authoritative input commit:

`b4b97f6c7955b7872b86dc145e628789bcca1954`

Authoritative CI run:

`33971140085`

Result:

- Build success;
- 0 warnings;
- 0 errors;
- humanoid bare-water rejection PASS;
- bridge overlay allowance PASS;
- stranded NPC water rescue PASS;
- combat land-safe approach PASS;
- Alpha 6.6.7 performance regression PASS;
- Switch/Codex/Surge regression PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.8_LAND_SAFE_FOLLOW_TARGETS_HOTFIX_TEST.zip`

Package SHA256:

`923966725437a3bcc55c71ce9cf02b09d41b1b58c568891035bcf8ef3eb6ec43`

Artifact ID:

`9970958963`

Artifact wrapper digest:

`sha256:b854f8456a69984cc4a5abb3c99e4aec455d89630aa2ef77246d865d6d787e43`

## Live validation priority

1. Test the exact river location from the user's screenshot with a 4-6 person party.
2. Humanoid NPCs must remain on land or real bridge tiles.
3. FPS must remain smooth like Alpha 6.6.7, which the user already confirmed fixed the lag.
4. Cross a real bridge and verify NPCs are not blocked by the water guard.
5. Load/enter the area with an NPC already stranded in water and verify one-time rescue to safe land.
6. Test land combat beside water and confirm NPCs do not path into the river.
7. Re-test Switch equip/unequip and Codex one-row navigation.

If a specific modded bridge is falsely rejected, inspect that map's layer/property structure and extend bridge recognition narrowly. Do not weaken the bare-water guard globally and do not restore the expensive placeable query.
