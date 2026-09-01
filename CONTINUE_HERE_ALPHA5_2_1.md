# Team Up! alpha.5.2.1 checkpoint

Version: `0.1.0-alpha.5.2.1`
Branch: `alpha5.2.1-recruit-vault-hotfix`

## Why this hotfix exists

User smoke testing of alpha.5.2 found three issues:

1. Recruitment hint appeared near the top-left/minimap instead of attached to the dialogue frame.
2. Pressing E / Right Shoulder recruited immediately instead of asking for confirmation.
3. Party Vault custom header floated above the storage UI and clipped/overlapped text.

## Fixes

### Recruitment hint

- Hint position is derived from viewport + DialogueBox dimensions instead of unreliable DialogueBox x/y fields.
- Hint is attached directly above the visible chat frame.

### Recruitment confirmation

- E / Right Shoulder no longer recruits immediately.
- It opens a confirmation question first.
- Vietnamese wording: `Bạn muốn thu nạp <NPC> vào Team Up?`
- Only the Recruit/Thu nap choice adds the NPC.
- A confirmation guard prevents the recruitment hotkey from recursively reopening the question.

### Party Vault

- Removed the floating extra header box.
- Vault title/status/subtitle are rendered inside the native StorageContainer panel.
- Native FarmerTeam global inventory persistence remains unchanged.

## Important

Combat AI is still NOT implemented in this build.
Party members can follow/wait/manage roles, but they do not yet attack enemies.
Combat Foundation starts only after this hotfix passes smoke testing.

## Build

Run the only build entry point in this package:

`BUILD_ALPHA5_2_1.bat`

Expected output:

`release/TeamUp_v0.1.0-alpha.5.2.1_SMOKE_TEST.zip`

If build fails, send `BUILD_LOG.txt`.
