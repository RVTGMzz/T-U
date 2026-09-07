# Team Up! Chat Handoff - v0.2.0-alpha.6.5.3

Date: 2026-09-04
Status: COMPILE VERIFIED / PACKAGE VERIFIED / SOURCE MATERIALIZED / IN-GAME SMOKE PENDING

## Resume from here

Development branch:

`v0.2-alpha6-5-3-surge-validation-harness`

Materialized Alpha 6.5.3 source commit:

`2330d2f99764a1bc9fa723fa7416b2c230a64dcd`

Baseline inherited from:

`v0.2-alpha6-5-2-surge-runtime-polish-handoff`

Alpha 6.5.2 handoff commit:

`faf92d6027e7579dcda15f9e7df8f2aa6fee8643`

## Build verification

Authoritative successful GitHub Actions run:

`33861977814`

Result: SUCCESS

Verified steps:

- SMAPI build environment: PASS
- Build Alpha 6.5.3 end-to-end: PASS
- Source acceptance: PASS
- Packaged build verification: PASS
- Generated source materialization: PASS
- Artifact upload: PASS

Compiler result:

- 0 warnings
- 0 errors

Package:

`TeamUp_v0.2.0-alpha.6.5.3_SURGE_VALIDATION_HARNESS_TEST.zip`

Package SHA256:

`7df2073a96ebc8950ccf56ba8cfe6a5cae2605d2cfd2f8ceed425444d7b4c823`

GitHub Actions artifact:

- Name: `team-up-alpha6-5-3-surge-validation-harness`
- Artifact ID: `9932475908`
- Uploaded artifact digest: `sha256:bd20ed019ed22a3d970e6c3b64675ca3b82b9336c49f6ad57dc205b6754ef9fa`
- Artifact wrapper size: 195878 bytes

Important: the uploaded Actions artifact digest is the wrapper ZIP digest. The Team Up mod package inside it uses the package SHA256 above.

## Why Alpha 6.5.3 exists

Alpha 6.5.2 made The Surge placement conservative and observable, but it still required normal warps/encounters to repeat tests. Because Alpha 6.5.2 had not yet been smoke-tested in game, Alpha 6.5.3 deliberately avoids changing gameplay balance or broadening third-party monster support.

Alpha 6.5.3 adds a developer validation harness so the same combat location can be inspected, cleared, and reapplied safely without accumulating Team Up-owned extras.

## New debug commands

`teamup_test surge status`

- prints the current Surge Describe snapshot;
- prints the most recent compact `[SurgeTelemetry]` line through `LastTelemetryLine`;
- prints the number of Team Up-owned Surge monsters currently in the active location.

`teamup_test surge reapply`

- refuses to run while an event, dialogue, or clickable menu owns presentation;
- clears only monsters marked with `Ronvotri.TeamUp/SurgeSpawn` in the current location;
- resets the current visit telemetry;
- applies exactly one fresh Surge budget to the current location;
- therefore repeated debug tests do not stack x2 -> x3 -> x4 merely because the command was repeated.

`teamup_test surge clear`

- removes only Team Up-owned monsters carrying the Surge marker;
- preserves vanilla monsters, custom/mod monsters, Cardcha harness targets, bosses, and other source actors.

`teamup_test surge board`

- debug-displays the latest Marlon Threat Board snapshot only while inside `AdventureGuild`;
- requires a recorded encounter;
- refuses to interrupt event/dialogue/menu presentation;
- does not create or change Origin story progression.

## Runtime-only debug bridge

`MonsterSurgeService.ActiveInstance` exposes the active Surge service only to Team Up's developer harness.

It is runtime-only and is not serialized. No PartySaveData schema change was introduced.

`LastTelemetryLine` retains the last emitted telemetry line so `teamup_test surge status` can show the exact latest snapshot without requiring the tester to search backward through a large SMAPI log.

## Non-stacking ownership lock

The 6.5.3 source acceptance explicitly verifies that `DebugReapplyCurrentLocation` calls:

`ClearOwnedSurgeMonsters(location)`

before:

`ApplyOnce(location)`

`ClearOwnedSurgeMonsters` filters with `.Where(IsSurgeMonster)`.

Do not broaden this clear path to arbitrary `Monster` actors.

## Alpha 6.5.2 safe placement contract remains locked

Still required:

1. `location.isTileOnMap(tile)`
2. `location.isTilePassable(tile)`
3. `!location.IsTileBlockedBy(tile, ignorePassables: CollisionMask.All)`
4. minimum Farmer spacing
5. minimum live-monster spacing
6. fail closed on unknown/custom map exceptions

Do NOT restore `GameLocation.isTileLocationTotallyClearAndPlaceable` in Team Up 6.5.2+ because current Stardew 1.6 reference assemblies do not expose it on `GameLocation`.

## Surge safety/economy locks

Must remain true:

- extra enemies are still Team Up-owned safe `GreenSlime` overlays;
- no arbitrary source monster cloning;
- no `Activator.CreateInstance`, `MemberwiseClone`, or constructor reflection for custom monster duplication;
- `Cardcha_CardTestArena` remains Surge-suppressed;
- Cardcha harness monsters remain excluded from baseline counting;
- Surge extras retain `Ronvotri.TeamUp/SurgeSpawn` and `Ronvotri.TeamUp/SurgeSource` markers;
- `SurgeMonstersDropLoot=false` remains default;
- known loot collections remain suppressed when full loot is off.

## Threat / telemetry regression lock

Threat tiers remain:

- LOW
- ELEVATED
- HIGH
- SURGE

Telemetry remains:

`[SurgeTelemetry] location=... baseline=... wanted=... spawned=... unsafeRejected=... total=... multiplier=... threat=... suppression=...`

Normal Marlon Threat Board remains one lightweight post-encounter presentation on return to AdventureGuild and does not interrupt active UI/story presentation.

## Prior regression locks

Do not regress:

- MiMi only recruits after Cardcha social/friendship unlock.
- Team Up does not read Cardcha private SaveData or reflect into Cardcha services.
- ChaCha remains Special/Farmer Companion and never Main Party.
- Sudoku remains Control/Damage with NINEFOLD SEAL.
- MiMi remains Support/Control with BROOMTAIL SIGIL.
- Origin story remains Linus -> Marlon -> First Awakening -> Stronger Together.
- 51 SVE/RSV expansion NPC identities/icons/balance remain intact.
- equipment double-click behavior remains intact.
- controller focus behavior remains intact.
- Party Vault drag/drop remains intact.
- combat target-lock / anti-spin fixes remain intact.

## Build entry points

One-click Windows launcher:

`BUILD_V0_2_ALPHA6.bat`

Direct PowerShell build:

`BuildV0_2Alpha653.ps1`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_5_3_SURGE_VALIDATION_HARNESS_VI.txt`

Workflow:

`.github/workflows/team-up-alpha6-5-3-surge-validation-harness.yml`

## CI history note

Do not treat the first two Alpha 6.5.3 runs as gameplay failures.

- run `33861794963`: early workflow/build setup run from an earlier branch SHA;
- run `33861808755`: builder stopped at PowerShell parsing because C-style quote escaping was used in two injection replacement strings;
- builder quoting was fixed using PowerShell here-strings;
- authoritative run `33861977814`: SUCCESS with 0 warnings / 0 errors and all acceptance/package/materialization checks passing.

## In-game validation still required

CI verifies compile/source/package correctness only. Alpha 6.5.3 is NOT yet declared in-game verified.

Priority live checks:

1. `teamup_test surge status` on a real Mine/Cave floor.
2. Repeated `teamup_test surge reapply` and confirm extras do not accumulate across repetitions.
3. `teamup_test surge clear` and confirm source/custom monsters remain.
4. Compare `LastTelemetryLine` against actual visible monster counts.
5. Test cramped floors and confirm `unsafeRejected` / partial placement rather than wall spawns.
6. In Cardcha Test Arena, reapply must report `cardcha-sandbox` and inject nothing.
7. After a recorded encounter, test `teamup_test surge board` in AdventureGuild.
8. Run MiMi/Sudoku/Origin/equipment/controller/Vault/expansion regression checks.

## Recommended next step

Use the Alpha 6.5.3 in-game smoke results before broadening The Surge.

If live testing finds a runtime issue, use Alpha 6.5.4 as a focused hotfix.

If live testing is clean, move to the next broader milestone rather than adding another validation-only point release. Pokémon/Pelipper or other species-specific density support should still require a clear runtime identity/API contract and must never be implemented by blind cloning.
