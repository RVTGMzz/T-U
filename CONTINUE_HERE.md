# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.8**

Status: **compile/package/direct-builder verified with 0 warnings, 0 errors and no materialized source diff. Alpha 6.6.7 water/bridge lag fix is user-confirmed; Alpha 6.6.8 now needs in-game confirmation that humanoid followers stay out of bare water without reintroducing lag.**

Development branch:

`v0.2-alpha6-6-8-land-safe-follow-targets`

Final handoff branch:

`v0.2-alpha6-6-8-land-safe-follow-targets-handoff`

Read first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_8_2026-09-05.md`

## Why Alpha 6.6.8 exists

Live testing confirmed Alpha 6.6.7 removed the severe river/bridge lag, but the simplified lightweight `isTileOnMap + isTilePassable` target test allowed humanoid party NPCs to accept some water tiles as formation/combat destinations. The user supplied a screenshot showing multiple recruited NPCs standing in the river after the performance fix.

## Alpha 6.6.8 fix

New helper:

`src/TeamUp/Core/PartyTileSafety.cs`

`PartyTileSafety.IsWalkableLandOrBridge(...)` keeps the cheap checks from 6.6.7, rejects bare water through `isWaterTile(x,y)`, and allows true bridge/walkway tiles when a Buildings-layer tile overlays the water coordinate.

### Humanoid follow behavior

- Main Party NPC formation uses land-safe targets only.
- No fallback to a known-invalid preferred formation tile.
- If no nearby land/bridge tile is valid, humanoid followers wait instead of following a mounted/traversing Farmer into water.
- If a party NPC is already stranded on bare water from a previous runtime/save state, Team Up immediately warps that NPC to the resolved safe formation tile.
- The rule is intentionally applied to humanoid party members, not globally to all external Pokemon/summons.

### Combat behavior

`CombatService.IsLightweightCombatTile(...)` now delegates to the same land/bridge safety helper, so reachable land combat remains lightweight but approach targets cannot be bare water.

### Performance locks preserved

- no `isTileLocationTotallyClearAndPlaceable` in FollowService or CombatService;
- Pelipper source-owned water/decorative actors remain excluded from hostile Team Up targeting by default;
- unreachable combat path retry cooldown remains 24 ticks;
- combat movement path pulse remains every 3 ticks;
- FollowService lightweight pathing from 6.6.7 remains.

## Other regression locks preserved

- Switch semantic Action Button equip;
- Switch semantic Use Tool unequip;
- controller debounce 180 ms;
- virtual mouse echo suppression 260 ms;
- inventory mouse double-click 450 ms;
- Codex D-pad/left analog = exactly one profile per input;
- profile content scale 1.52f;
- ASCII-safe punctuation, no hollow-star fallback glyphs;
- 6 total people across online Farmers + active NPCs;
- 2 shared external Pokemon/summon slots;
- vanilla pet and ChaCha free;
- five Party Strategy values unchanged;
- MiMi `Ronvotri.Cardcha_MiMi` / `BROOMTAIL SIGIL`;
- Sudoku `ronvotri.HeyYoureCursed_Sudoku` / `NINEFOLD SEAL`;
- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- Surge safe-placement and Cardcha sandbox locks;
- 51 SVE/RSV profiles/icons/balance;
- Party Vault drag/drop;
- Origin story.

## Authoritative checkpoint

First materialized source commit:

`b93fc29`

Authoritative input commit:

`b4b97f6c7955b7872b86dc145e628789bcca1954`

Authoritative CI run:

`33971140085`

Result:

- `BuildV0_2Alpha668.ps1` success;
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

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_8_LAND_SAFE_FOLLOW_TARGETS_VI.txt`

Highest priority:

1. Return to the exact river screenshot location with a 4-6 person party.
2. NPC people must remain on bank/real bridge and must not stand in the river.
3. FPS must remain as smooth as user-confirmed Alpha 6.6.7.
4. Test a true bridge to ensure the water guard does not block bridge traversal.
5. If loading with NPCs already in water, verify they are rescued to land once and do not teleport-loop.
6. Test real land combat near water.
7. Re-test Switch equip/unequip and Codex one-row navigation.

Do not call Alpha 6.6.8 live-verified until the user confirms the land-safe behavior in game.
