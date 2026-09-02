# Team Up! alpha.5.3.1 checkpoint

Branch: `alpha5.3.1-ui-controller-hotfix`
Version: `0.1.0-alpha.5.3.1`

## Why this hotfix exists
Alpha.5.3 smoke screenshots showed four UX problems:
1. Character Profile was too small for the available screen and text was harder to read than necessary.
2. The default mouse cursor stayed visibly parked on custom menus during controller play.
3. Codex filters were not controller-navigable as UI controls and LB/RB cycling was too opaque.
4. Dialogue helper tags overlapped the dialogue frame and were too small.

## alpha.5.3.1 changes

### Character Profile
- Uses up to 1320x780 or almost the full viewport on smaller screens.
- Larger portrait and larger body/caption text.
- Affinity bars enlarged.
- Long source/status/body strings are fit/wrapped instead of being allowed to clip.
- Mouse cursor is hidden during controller navigation and comes back after actual mouse movement.

### Codex Browser
- Uses more viewport space and adapts visible NPC row count to screen height.
- Role / Status / Source are true drop-down selectors.
- Controller flow:
  - D-pad left/right: move between filter controls.
  - A: open selected filter drop-down.
  - D-pad up/down: choose drop-down item.
  - A: confirm.
  - B: close only the drop-down when one is open.
  - D-pad down from filters: enter NPC list.
  - D-pad up from first NPC: return to filters.
- LB/RB no longer changes the role filter automatically.
- Row/filter text is fit to available width to avoid clipping.
- Mouse cursor is suppressed during gamepad navigation.

### Dialogue helper tags
- Still fixed left/right around NPC dialogue:
  - Left: `L (Controller) / Q  Hồ sơ`
  - Right before recruitment: `Thu nạp  R (Controller) / E`
  - Right after recruitment: `Rời đội  R (Controller) / E`
- Tags are now fully outside the top edge of the dialogue frame.
- Hint text scale is 1.5x.
- Recruit and Leave still require confirmation.

### Social tab Codex entry
- Wider button and fit-to-width text so `X (Controller) / P TEAM UP CODEX` is not clipped.

## Not changed
- Only five finalized combat profiles still exist: Abigail, Alex, Emily, Harvey, Maru.
- Unknown NPCs can still open profile shells.
- Search-by-name is still a later Codex feature.
- Combat AI is still NOT enabled.
- Party Vault persistence/follow/role/engagement architecture is unchanged.

## Build
Run only:
`BUILD_ALPHA5_3_1.bat`

Expected output:
`release/TeamUp_v0.1.0-alpha.5.3.1_SMOKE_TEST.zip`

If build fails, send `BUILD_LOG.txt` and fix the first fatal `CS...` compiler error before runtime testing.

Smoke checklist:
`SMOKE_TEST_ALPHA5_3_1_VI.txt`
