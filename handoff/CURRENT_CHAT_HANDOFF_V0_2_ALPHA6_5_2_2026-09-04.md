# Team Up! Chat Handoff - v0.2.0-alpha.6.5.2

Date: 2026-09-04
Status: COMPILE VERIFIED / PACKAGE VERIFIED / SOURCE MATERIALIZED / IN-GAME SMOKE PENDING

## Resume from here

Development branch:

`v0.2-alpha6-5-2-surge-runtime-polish`

Materialized Alpha 6.5.2 source commit:

`464c93197fc49dbbcf9298becf917cfd43f9bd34`

Baseline inherited from:

`v0.2-alpha6-5-1-mimi-recruit-gate-hardening-handoff`

Alpha 6.5.1 handoff commit:

`5d0523a6ba9c1d846f03fb112e6d0325724d6ad1`

## Build verification

GitHub Actions successful run:

`33861103138`

Result: SUCCESS

Verified steps:

- SMAPI build environment: PASS
- Build Alpha 6.5.2 end-to-end: PASS
- Source acceptance: PASS
- Packaged build verification: PASS
- Generated source materialization: PASS
- Artifact upload: PASS

Compiler result:

- 0 warnings
- 0 errors

Package:

`TeamUp_v0.2.0-alpha.6.5.2_SURGE_RUNTIME_POLISH_TEST.zip`

Package SHA256:

`7a63bb7dbab81e6efb34ee435c63daf43da86d2f3b33157ab27c4018a7ec52a9`

GitHub Actions artifact:

- Name: `team-up-alpha6-5-2-surge-runtime-polish`
- Artifact ID: `9932156625`
- Uploaded artifact digest: `sha256:e8efc398395c5e1d2e122fcc04dbbfccd71a752a3d5b2f666beacbeb0553035f`
- Artifact wrapper size: 194401 bytes

Important: the uploaded Actions artifact digest is the digest of the Actions wrapper ZIP. The Team Up mod package inside it uses the package SHA256 listed above.

## Why Alpha 6.5.2 exists

Alpha 6.5.0 established The Surge as a safe density overlay, but extra monsters were positioned using small pixel offsets around baseline monsters. That could place an extra actor too near blocked geometry on cramped or heavily modded combat maps.

Alpha 6.5.2 makes runtime placement conservative and observable before Team Up attempts broader third-party monster adapters.

## Alpha 6.5.2 safe spawn contract

The Surge still does NOT clone arbitrary source monsters. Extra runtime enemies remain Team Up-owned safe `GreenSlime` overlays.

For each requested extra spawn, Team Up now searches a deterministic tile ring around a baseline monster and accepts a candidate only when all current Stardew 1.6 collision checks pass:

1. `location.isTileOnMap(tile)`
2. `location.isTilePassable(tile)`
3. `!location.IsTileBlockedBy(tile, ignorePassables: CollisionMask.All)`
4. candidate is at least 128 px from the Farmer
5. candidate is not stacked within 56 px of another live monster

Tile validation is wrapped fail-closed. If a custom map throws or no safe candidate exists, Team Up skips that requested spawn instead of forcing an actor into invalid geometry.

This means x2 is a TARGET, not an unconditional promise. On cramped maps, spawning fewer monsters than the requested budget is correct behavior.

## Important Stardew 1.6 API lock

Do NOT restore `GameLocation.isTileLocationTotallyClearAndPlaceable` in Team Up Alpha 6.5.2+.

The first Alpha 6.5.2 CI attempt, run `33860866288`, failed compile because current Stardew 1.6 reference assemblies do not expose that helper on `GameLocation`.

The implementation was corrected to the current compatible contract:

- `isTileOnMap`
- `isTilePassable`
- `IsTileBlockedBy(... CollisionMask.All)`

The corrected run `33861103138` then compiled with 0 warnings and 0 errors.

## Surge telemetry contract

Every applied or suppressed Surge visit records one compact SMAPI Trace line beginning with:

`[SurgeTelemetry]`

Fields:

- `location=`
- `baseline=`
- `wanted=`
- `spawned=`
- `unsafeRejected=`
- `total=`
- `multiplier=`
- `threat=`
- `suppression=`

Known suppression values:

- `pending`
- `not-combat-zone`
- `cardcha-sandbox`
- `no-baseline-monsters`
- `spawn-budget-zero`
- `no-safe-spawn-tile`
- `partial-safe-placement`
- `none`

This telemetry is the first place to inspect when the user reports that a map did or did not receive the expected number of Surge monsters.

## Threat presentation

Runtime threat tiers:

- `LOW`
- `ELEVATED`
- `HIGH`
- `SURGE`

Successful Surge combat message:

`THE SURGE • <TIER> • +N MONSTERS`

After a recorded combat encounter, returning to `AdventureGuild` can show one lightweight runtime board message for that encounter:

`MARLON'S THREAT BOARD • <TIER> • <LOCATION> • baseline->total`

The board refuses to interrupt an active event, dialogue, or clickable menu. It is runtime flavor/feedback, not a new forced quest stage and does not modify the existing Origin story schema.

## Cardcha sandbox and economy locks

Must remain true:

- `Cardcha_CardTestArena` never receives Surge monsters.
- Cardcha harness monsters are excluded from baseline counting.
- Surge extras keep `Ronvotri.TeamUp/SurgeSpawn` marker.
- Surge extras keep `Ronvotri.TeamUp/SurgeSource` marker.
- `SurgeMonstersDropLoot=false` remains the default.
- known loot collections on Team Up-owned Surge monsters remain suppressed when full loot is off.
- no boss/story/quest/custom monster cloning.

## Regression locks retained from Alpha 6.5.1 / 6.5.0

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

`BuildV0_2Alpha652.ps1`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_5_2_SURGE_RUNTIME_POLISH_VI.txt`

Workflow:

`.github/workflows/team-up-alpha6-5-2-surge-runtime-polish.yml`

## In-game validation still required

CI verifies compile/source/package correctness only. Alpha 6.5.2 has NOT yet been declared in-game verified in this handoff.

Priority live checks:

1. Enter several vanilla Mine/Cave floors with monsters and inspect actual placement.
2. Confirm cramped layouts reduce spawn count safely instead of spawning inside walls/rocks.
3. Inspect `[SurgeTelemetry]` in SMAPI log and compare baseline/wanted/spawned/unsafeRejected.
4. Confirm `Cardcha_CardTestArena` reports `suppression=cardcha-sandbox` and receives no Surge extra.
5. Return to AdventureGuild after a real Surge encounter and verify Marlon's Threat Board appears once without interrupting Origin dialogue.
6. Run MiMi/Sudoku/Origin/equipment/controller/Vault regression smoke checks.

## Recommended next step

Do not immediately expand unknown custom-monster cloning.

First use the Alpha 6.5.2 in-game smoke results to decide between:

- Alpha 6.5.3 hotfix/polish if runtime placement, telemetry, or Guild presentation needs adjustment; or
- the next broader milestone if 6.5.2 is clean.

Pokémon/Pelipper species-specific density support should still wait for a clear source API/runtime identity contract rather than relying on blind cloning.
