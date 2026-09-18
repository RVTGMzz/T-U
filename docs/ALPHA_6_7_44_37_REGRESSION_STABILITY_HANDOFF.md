# Alpha 6.7.44.37 - Regression + Stability Handoff

Repository: `ronvotri/T-U`

Branch: `v0.2-alpha6-7-44-37-regression-stability`

Version: `0.2.0-alpha.6.7.44.37`

## Build

- CI source SHA: `561909f1849d06c0d9750deac2e523ff30009995`
- Run: `35363771773`
- Job: `105661138777`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.37_REGRESSION_STABILITY_TEST.zip`
- SHA256: `7ebca7fe3f75bafbce33086c2d0b3680d01dc19b669c79809e35ccdf21eb59e7`
- Build: PASS, 0 warnings / 0 errors
- ZIP audit: PASS

## Why this build exists

6.7.44.36 completes the elite lifecycle. 6.7.44.37 hardens non-Pelipper/custom compatibility and removes the last dormant unrelated-creature fallback from core code.

## Factory hardening

`MonsterMutationMinionFactory.Create` now returns nullable `Monster?`.

Pelipper source:
- returns null with `pelipper-native-required`;
- native follower service owns the only valid Pelipper path.

Vanilla/custom source:
- exact same runtime type is constructed only through conservative recognized constructors;
- unsupported constructor shapes return null with `unsupported-fail-closed`;
- no GreenSlime is constructed.

Legacy `MonsterMutationService.SpawnMinionWave` also handles null safely, so even if the native prefix path is unavailable the core fallback remains fail-closed.

## Runtime audit

Command:

`teamup_mutation_regression`

It checks:
- minion runtime type mismatch;
- recursive Mutant minions;
- missing MutationExcluded markers;
- factory fallback count;
- fail-closed count;
- Pelipper native identity;
- duplicate Pelipper encounter IDs.

## Carry-forward

6.7.44.34-36 remain intact:
- source-ID pairing;
- pre-render scale stabilization;
- 18-tile aggro arena;
- stronger damage;
- continuous leader pursuit;
- 128px hold / 160px reach;
- Mutant leader capture blocked;
- 3 HP phases;
- final-only x3 native loot.

Old 6.7.44.24-30 crash-stack services remain absent.

## Live test

1. Install 6.7.44.37.
2. Run `teamup_mutation force`.
3. Wait until the 2-4 follower wave finishes.
4. Run `teamup_mutation_regression`.
5. Expected result starts with `Mutation regression audit: PASS`.
6. Also confirm the 6.7.44.36 elite contract during the same encounter if practical.

After live pass, next build is Lower Workings Runtime Gate v2.
