# Team Up v0.2.0-alpha.6.6.10 Handoff

Date: 2026-09-05

## Resume point

Development branch:

`v0.2-alpha6-6-10-pelipper-follow-authority`

Final handoff branch:

`v0.2-alpha6-6-10-pelipper-follow-authority-handoff`

Authoritative verified source commit:

`5980d71a4445f445c47cd58bdde0d0b92903afc9`

Version:

`0.2.0-alpha.6.6.10`

Status: **compile/package/direct-builder verified; live Pelipper flicker validation pending**.

## Why Alpha 6.6.10 exists

Live test of Alpha 6.6.9 confirmed Pelipper Pokemon could still flicker after Team Up stopped continuously changing `IsInvisible`, `Halt`, `controller`, and `temporaryController` in the Pelipper compatibility layer.

Inspection found the remaining authority collision in `FollowService.UpdateCompanionUnits(...)`: every four ticks Team Up could still resolve source-controlled Pelipper Pokemon and run `PrepareForParty`, `HoldPosition`, `FollowTarget`, warp, `Halt`, or controller replacement on them. Pelipper Town was controlling the same actor at the same time.

## Root fix

`FollowService.UpdateCompanionUnits(...)` now checks:

```csharp
if (PelipperTownCompatibilityService.IsSourceControlled(unit))
    continue;
```

The skip occurs before `ResolveCharacter(unit.CharacterName)`.

Therefore Team Up cannot enter its movement pipeline for a registered Pelipper source-controlled companion.

Pelipper Town is the sole actor movement/render authority for its Pokemon. Team Up keeps only roster/quota/deployment bookkeeping.

## Idempotent deployment markers

`PelipperDeploymentStateService.SetDesiredDeployment(...)` now writes soft deployment modData only when the desired value actually changes.

Markers remain:

```text
Ronvotri.TeamUp/PelipperDeployment = Active|Standby
Ronvotri.TeamUp/PelipperDeploymentOwner = <owner>
```

This prevents the 30-tick reconciliation loop from repeatedly rewriting unchanged Pelipper marker values.

Legacy pre-6.6.9 suppression cleanup may still restore old state once. New runtime must never set Pelipper suppression to true.

## Preserved regressions

- Alpha 6.6.7 river/bridge performance fix retained.
- `CombatPathRetryCooldownTicks = 24` retained.
- `CombatMovementPulseTicks = 3` retained.
- no `isTileLocationTotallyClearAndPlaceable` in Follow or Combat.
- Alpha 6.6.8 `PartyTileSafety.IsWalkableLandOrBridge(...)` retained.
- humanoid party NPCs reject bare water and allow real bridge overlays.
- Alpha 6.6.9 HUD and contextual overhead health bars retained.
- multiplayer health/downed/state snapshot sync retained.
- Switch semantic equip/unequip input retained.
- Codex one-profile-per-input retained.
- six-person shared people capacity retained.
- two shared external Pokemon/summon slots retained.
- vanilla pet and ChaCha remain free.
- MiMi, Sudoku, Surge, Cardcha sandbox, 51 SVE/RSV profiles, Party Vault, and Origin story retained.

## Build pipeline

Builder:

`BuildV0_2Alpha6610.ps1`

One-click launcher:

`BUILD_V0_2_ALPHA6.bat`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_10_PELIPPER_FOLLOW_AUTHORITY_VI.txt`

### First materialization run

Run:

`33978790962`

Materialized source commit:

`fdde2a5`

### Final authoritative run

Run:

`33978859801`

Authoritative input commit:

`5980d71a4445f445c47cd58bdde0d0b92903afc9`

Results:

- build success;
- 0 warnings;
- 0 errors;
- Pelipper early skip before actor resolution PASS;
- Pelipper source-only movement authority PASS;
- idempotent deployment markers PASS;
- Alpha 6.6.7 performance regression PASS;
- Alpha 6.6.8 land-safe regression PASS;
- Alpha 6.6.9 health UI regression PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.10_PELIPPER_FOLLOW_AUTHORITY_HOTFIX_TEST.zip`

Package SHA256:

`3172f7ac33551ad204977a49c5274588463fbae96e417d84e53dcec2cc7a1ca3`

Artifact ID:

`9973140268`

Artifact wrapper digest:

`sha256:3f0f984daebe8846df2b26166f6497813cdef5c59e12a25a91fe6fe265e210a9`

## Required live validation

1. Summon Farmer Pokemon and stand still for 30 seconds.
2. Move continuously for 30 seconds and warp maps.
3. Recruit an NPC with a Pelipper partner and repeat.
4. Show Farmer Pokemon and NPC-linked Pokemon simultaneously.
5. Verify no flicker and no Team Up formation tug on Pokemon.
6. Verify two-companion quota still behaves correctly.
7. Re-test humanoid NPC follow near river/bridge for smooth FPS and land safety.
8. Re-test health HUD/overhead bars.
9. Re-test Switch equip/unequip and Codex one-profile navigation.

Do not call the Pelipper flicker issue live-fixed until the user confirms Alpha 6.6.10 in game.
