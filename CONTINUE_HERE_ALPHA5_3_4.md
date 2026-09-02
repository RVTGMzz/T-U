# Team Up! alpha.5.3.4 checkpoint

Branch: `alpha5.3.4-polish-hotfix`
Version: `0.1.0-alpha.5.3.4`
Build entry: `BUILD_ALPHA5_3_4.bat`
Expected release: `release/TeamUp_v0.1.0-alpha.5.3.4_SMOKE_TEST.zip`

## User-tested issues addressed

1. Party-member contextual actions
   - Party member root dialogue/menu keeps the left Profile hint.
   - Right action changes to Leave Team.
   - Leave Team always asks for confirmation before removal.
   - Role/Engagement submenus intentionally do not keep the contextual hints.

2. Dialogue hint anchor
   - alpha.5.3.3 attempted to use `DialogueBox.x/y`, which may not compile against the user's reference assemblies.
   - alpha.5.3.4 uses public DialogueBox width/height and Stardew's vanilla 64 px bottom anchor instead.
   - Goal: hints stay fully above the dialogue frame without compiler dependency on private/internal members.

3. Codex controller
   - Codex declares native gamepad handling so Stardew does not translate controller A into a stale mouse click.
   - A on Role/Status/Source opens the dropdown.
   - D-pad changes dropdown selection; A accepts; B closes.
   - Controller input hides the mouse cursor; mouse use shows it again.

4. Party Vault
   - Removed the custom `StorageContainer` overlay that produced floating title text and broken borders.
   - Vault now uses Stardew's native chest-style `ItemGrabMenu` with Organize support.
   - Persistence remains `FarmerTeam.GetOrCreateGlobalInventory("Ronvotri.TeamUp/PartyVault")`.
   - No Squad code/assets were copied. Squad is only a UX/feature reference.

5. Follow polish
   - Follow update cadence raised from every 4 ticks to every 2 ticks.
   - Repath threshold reduced from 1.35 to 0.90 tile.
   - Warp distance increased from 10 to 11 tiles.
   - Stop distance adjusted to 1.45 tiles.
   - Still uses vanilla `PathFindController`; test around pots/fences/narrow paths for clipping and jitter.

## Important

- Combat AI is still NOT implemented in alpha.5.3.4.
- Full Codex roster is still not populated; the five alpha profiles remain the complete authored profile set for now.
- ChaCha and Farmer/Special Companions remain outside Main Party recruitment.

## Build implementation note

`BuildAlpha5_3_4.ps1` runs `ApplyAlpha5_3_4Patches.ps1` before restore/build. The patcher is idempotent so the one-click build can be rerun from the same extracted source folder.

See `SMOKE_TEST_ALPHA5_3_4_VI.txt` for the current validation gate.
