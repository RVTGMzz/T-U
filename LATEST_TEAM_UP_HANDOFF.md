# Team Up handoff: 0.2.0-alpha.6.7.44.5

## Source of truth

- Branch: `v0.2-alpha6-7-44-5-pelipper-catchmode-gate`
- Version: `0.2.0-alpha.6.7.44.5`
- Verified source/build commit: `f420e025f00b56a1cc94fb1ea1c93459038228fa`
- `main` is not merged. This build is a live-test candidate, not stable.

## Implemented in 6.7.44.5

### Strict Pelipper Catch Mode gate

`PelipperCaptureSafetyService` now fails closed:

- Pelipper absent -> capture safety OFF.
- Pelipper + Catch Mode false -> OFF.
- Pelipper + mode unknown/unresolved -> OFF.
- Pelipper + Catch Mode positively confirmed true -> ON.
- 10% fallback threshold is allowed only after mode is confirmed ON.
- Every policy refresh begins disabled, preventing stale true state.
- Mutants never inherit Pelipper mercy/capture-floor protection.

### Shiny Emergency Hold remains independent

A confirmed Shiny can still trigger Team Up HOLD FIRE regardless of Catch Mode state. Shiny Hold blocks Team Up friendly damage but does not create or repair an artificial 10% capture floor.

### Diagnostics

`teamup_encounter status` now reports encounter state plus:

- Pelipper presence
- Catch Mode detected
- Catch Mode enabled
- capture safety enabled
- mode source
- threshold
- threshold source

### Build hygiene

- Harmony reference enabled through `Pathoschild.Stardew.ModBuildConfig` via `<EnableHarmony>true</EnableHarmony>`.
- C# language pinned to 13.0 so compiler keyword changes do not silently break legacy bridge source.
- Manifest materialized at `0.2.0-alpha.6.7.44.5`.

## CI / package verification

- Workflow: `Team Up v0.2.0-alpha.6.7.44.5 Pelipper Catch Mode Gate`
- Run: `34662578869`
- Job: `103467948066`
- Head: `f420e025f00b56a1cc94fb1ea1c93459038228fa`
- Result: PASS
- C# build: 0 warnings / 0 errors
- Artifact ID: `10288091017`
- Artifact: `team-up-alpha6-7-44-5-pelipper-catchmode-gate`
- Wrapper SHA256: `4d7db6c74fb57a3d3da86a28a753a8fa375c50b81f22dd3b1bc629b9cb3981e1`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.5_PELIPPER_CATCHMODE_GATE_TEST.zip`
- Test ZIP SHA256: `721220d57063d66f8444527a2a2e1985be275ee2d741c9daaf0c16e49bbdf23f`

Audits PASS:

1. version
2. strict Catch Mode gate
3. Shiny/Mutant separation
4. runtime diagnostic wiring
5. source-aware Shiny carry-forward
6. Lower Workings + EN/VI carry-forward
7. build
8. ZIP/TMX re-parse

## Runtime acceptance still required

Do NOT call this stable or runtime-proven until live Stardew/Pelipper tests pass.

Catch Mode live matrix:

1. Pelipper absent -> `captureSafetyEnabled=False`; normal monsters die normally.
2. Pelipper installed + Catch Mode OFF -> `catchModeDetected=True`, `catchModeEnabled=False`, `captureSafetyEnabled=False`; wild Pokemon can reach 0 HP normally.
3. Pelipper installed + Catch Mode ON -> `catchModeDetected=True`, `catchModeEnabled=True`, `captureSafetyEnabled=True`; Team Up stops at Pelipper threshold.
4. Toggle ON -> OFF, wait >2 seconds -> policy refreshes OFF; no stale capture floor.
5. Mode unresolved/unknown -> `catchModeDetected=False`, `captureSafetyEnabled=False`.
6. With Catch Mode OFF, natural Shiny still triggers Shiny Emergency Hold.
7. Normal non-Shiny wild with Catch Mode OFF gets normal combat behavior.
8. Mutated Pokemon never receives Pelipper capture-floor protection.
9. Owned/companion Pokemon remain excluded from mutation/capture-target behavior.
10. No duplicate Pelipper source/proxy/render/controller state.

Use `teamup_encounter status` during each live test and preserve the SMAPI log if any state is wrong.

## Lower Workings gate

6.7.44.1 map runtime gate is still separate and still needs the agreed live checks. Do not start 6.7.45 until Lower Workings runtime acceptance passes or the user explicitly waives it.

## Story locks carried forward

- George remains observed Rank D / Non-Combatant before the planned 6.7.46 reveal. No Rank S, no `The Last Blaster`, no explicit historical miner identity.
- Evelyn's postgame secret remains untouched; main-story presentation stays ordinary low Rank D healer/support.
- No exact `SECTOR 17` unless explicitly designed later.
- Story NPC slots remain 4/4. Hard formation cap remains 5 people including Farmers.
- Entry Protocol READY + SURGE HIGH prerequisites remain unchanged.
- No final boss.
- Pelipper source ownership/render/controller authority is preserved.
- 6.7.45 remains reserved for the Containment Chamber Escalation Encounter.
