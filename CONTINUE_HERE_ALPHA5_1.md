# Team Up! - CONTINUE HERE alpha.5.1

Current hotfix checkpoint after the 2026-09-01 alpha.5 build attempt.

## Current version to test

`0.1.0-alpha.5.1`

## What happened

The user's alpha.5 build restored successfully under .NET SDK 6.0.428, then failed with one compiler error:

```text
ModConfig.cs(12,29): error CS0117:
'SButton' does not contain a definition for 'ControllerRightShoulder'
```

`CS9057` was only a warning.

## Fix applied

Recruitment binding changed from the invalid enum member:

```csharp
SButton.ControllerRightShoulder
```

to the SMAPI-compatible XNA conversion:

```csharp
Buttons.RightShoulder.ToSButton()
```

This keeps the locked UX unchanged:

- keyboard E recruits while normal NPC dialogue is open;
- controller Right Shoulder is displayed as `R (Controller)`;
- keyboard R is not the Team Up all-purpose key.

## Build

Run:

```text
BUILD_ALPHA5_1.bat
```

Expected package after successful build:

```text
release/TeamUp_v0.1.0-alpha.5.1_SMOKE_TEST.zip
```

If the build fails, read/send `BUILD_LOG.txt` and diagnose the first real compiler `error CS...`, not the final PowerShell throw line.

## After build success

Continue the existing alpha.5 Party Identity/Codex smoke test. Do not start v0.2 combat until alpha.5.1 recruitment, following, Party Member menu, Role, Engagement, Party Vault, Codex, save/load, and next-day lifecycle all pass.

For full project context read, in order:

1. `CONTINUE_HERE.md`
2. `CONTINUE_HERE_ALPHA5_1.md`
3. `docs/V0_1_IMPLEMENTATION_STATUS.md`
4. `docs/ALPHA5_PARTY_IDENTITY.md`
5. `docs/ALPHA5_1_BUILD_FIX.md`
