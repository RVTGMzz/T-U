# CONTINUE HERE - Team Up! alpha.5.3.2

Branch: `alpha5.3.2-codex-focus-size-hotfix`
Version: `0.1.0-alpha.5.3.2`

## Why this checkpoint exists
User test of alpha.5.3.1 showed that controller focus could move onto Codex filter boxes, but pressing controller A did not reliably open the selected dropdown. The Codex also still felt too small on larger screens.

## Changes
- Codex browser enlarged by roughly 20% when viewport space allows.
- Filter boxes, rows, dropdown rows and footer controls scaled up with it.
- Controller A activation now has a direct gamepad-state rising-edge fallback inside `CodexBrowserMenu.update()`.
- `receiveGamePadButton(Buttons.A)` no longer performs a second activation, avoiding double-trigger if Stardew delivers both paths.
- A opens the focused filter dropdown.
- A confirms a dropdown option.
- A on the focused NPC row opens that profile.
- Existing mouse/controller cursor suppression remains.

## Build
Run only:
`BUILD_ALPHA5_3_2.bat`

Expected output:
`release/TeamUp_v0.1.0-alpha.5.3.2_SMOKE_TEST.zip`

## Test first
Read `SMOKE_TEST_ALPHA5_3_2_VI.txt`.
Priority test: controller focus a filter -> press A -> dropdown must open -> choose option -> press A -> selection must apply.

## Still not included
Combat AI is still not enabled. This is a Codex/controller UI hotfix only.
