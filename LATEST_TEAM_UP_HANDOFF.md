# Team Up latest handoff: 0.2.0-alpha.6.7.44.37

Repository: **`ronvotri/T-U`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_37_REGRESSION_STABILITY_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-37-regression-stability`
- Version: `0.2.0-alpha.6.7.44.37`
- CI source SHA: `561909f1849d06c0d9750deac2e523ff30009995`
- Run: `35363771773`
- Job: `105661138777`
- ZIP SHA256: `7ebca7fe3f75bafbce33086c2d0b3680d01dc19b669c79809e35ccdf21eb59e7`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Runtime authority

Ron confirmed 6.7.44.35 leader chase/reach is OK.

6.7.44.36 completes the elite contract in one bundle:
- Mutant leader no-capture;
- 3 source-aware HP phases;
- no phase 1/2 loot;
- final phase native reward x3.

6.7.44.37 is the regression/stability pass:
- no GreenSlime fallback remains in the factory;
- Pelipper uses only native spawn;
- vanilla/custom followers require exact same runtime type;
- unsupported sources fail closed;
- runtime audit command: `teamup_mutation_regression`.

The 6.7.44.24-30 combined crash stack remains excluded.

## Next

Live test 6.7.44.37 and the 6.7.44.36 elite contract. If clean, move to Lower Workings Runtime Gate v2. Only after that start 6.7.45 unless Ron explicitly waives the gate.
