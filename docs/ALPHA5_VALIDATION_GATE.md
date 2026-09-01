# Team Up! alpha.5 validation gate

This checkpoint exists to validate `0.1.0-alpha.5` before starting v0.2 Combat Foundation.

## Branch

`alpha5-validation-gate-fixes`

## Pre-flight fixes added after the original alpha.5 handoff

1. Added root `BUILD_ALPHA5.bat` as a compatibility entry point that calls the existing `BUILD_TEAM_UP.bat` one-click build. This makes the command documented in `CONTINUE_HERE.md` real instead of pointing to a missing file.
2. Added an idempotent legacy-roster Role migration in `PartyManager.Load(...)`:
   - only Party Members whose saved Role is still `Unassigned` are touched;
   - only NPCs with an alpha.5 `NpcCombatProfile` are touched;
   - their Role becomes the profile Primary Role;
   - existing Engagement choice is preserved;
   - already assigned Roles are never overwritten.

## Validation status

These changes have been reviewed statically, but this branch is **not yet compile/runtime validated** in a Stardew/SMAPI environment.

Do not call alpha.5 build-clean until the user's PC passes the build and smoke test.

## Build

Run from the repository root:

```text
BUILD_ALPHA5.bat
```

Expected package:

```text
release/TeamUp_v0.1.0-alpha.5_SMOKE_TEST.zip
```

If build fails, use root `BUILD_LOG.txt` as the source of truth.

## Runtime smoke gate

Use `SMOKE_TEST_ALPHA5_VI.txt` and verify at minimum:

- clean SMAPI load;
- normal-dialogue recruitment hint placement;
- keyboard E recruitment;
- controller Right Shoulder recruitment;
- exhausted-dialogue recruitment fallback;
- follow/catch-up/map transitions;
- Party Member menu;
- Role and Engagement persistence;
- Party Vault persistence;
- Codex navigation and five vanilla profiles;
- next-day roster remains but auto-follow does not;
- no duplicate Party Member/Companion records;
- gifting remains vanilla.

Only after this gate passes should development move into v0.2 Combat Foundation.
