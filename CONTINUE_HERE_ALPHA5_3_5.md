# Team Up! alpha.5.3.5 checkpoint

Branch: `alpha5.3.5-stability-hotfix`
Version: `0.1.0-alpha.5.3.5`
Build entry: `BUILD_ALPHA5_3_5.bat`
Expected release: `release/TeamUp_v0.1.0-alpha.5.3.5_SMOKE_TEST.zip`

## User-tested issues addressed

1. Character Profile controller navigation
   - Profile opened from NPC dialogue now has explicit controller focus for `All Characters` and `Back`.
   - D-pad left/right changes footer focus, A activates, B goes back, Y remains a quick All Characters shortcut.

2. Cursor behavior
   - Profile and Codex track the physical mouse using `Mouse.GetState()`.
   - Controller/snappy-menu cursor movement no longer makes the mouse cursor reappear.
   - Cursor returns only after real mouse movement/click/scroll.

3. Codex filters
   - Codex was rebuilt with native gamepad handling.
   - A on Role/Status/Source opens a real dropdown and no longer falls through to the NPC row under a stale cursor position.
   - D-pad up/down navigates options, A confirms, B closes dropdown.

4. Party member contextual hints
   - Carried forward root-menu hint pinning so party members retain `L/Q Profile` and `R/E Leave Team` context.
   - Dialogue speaker resolution prefers the Team Up pinned NPC over vanilla `currentSpeaker`, which may be null/stale in question dialogues.
   - Leave Team still requires confirmation.
   - The vanilla question-mark dialogue icon is suppressed only on the Team Up member root command menu.

5. Party Vault safety rewrite
   - Removed ItemGrabMenu reward-style behavior entirely.
   - Vault uses native global inventory persistence plus a StorageContainer-derived transfer menu with Team Up-owned click behavior.
   - Items are moved only between player inventory and Party Vault. No recipe/Stardrop/artifact reward consumption paths are executed by Team Up.
   - Closing while holding an item is blocked by the normal MenuWithInventory held-item safety rule.

6. Follow polish carried forward
   - update every 2 ticks
   - stop distance 1.45 tiles
   - repath threshold 0.90 tile
   - warp threshold 11 tiles
   - still uses vanilla PathFindController

## Important

- Combat AI is still NOT implemented.
- Full Codex roster is still not authored; current five full profiles remain Abigail/Alex/Emily/Harvey/Maru, while encountered unknown NPCs can use profile shell behavior.
- ChaCha and Farmer/Special Companions remain outside Main Party recruitment.
- `ApplyAlpha5_3_5Patches.ps1` only patches ModEntry + Follow integration. Codex, Profile, and Party Vault alpha.5.3.5 changes are written directly in source.

See `SMOKE_TEST_ALPHA5_3_5_VI.txt` for validation.
