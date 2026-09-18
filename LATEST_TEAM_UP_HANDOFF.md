# Team Up latest handoff: 0.2.0-alpha.6.7.44.39

Repository: **`ronvotri/T-U`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_39_RUNTIME_IDENTITY_NIDORAN_EXACT_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-39-runtime-identity-nidoran-exact`
- Version: `0.2.0-alpha.6.7.44.39`
- CI source SHA: `feee4184c7c991743b695e70dc071b13e56d9650`
- Run: `35373758996`
- Job: `105693541626`
- ZIP SHA256: `b49f1597b8a9316d521c81f0c732c5c5bdbd76ca3dabc9fb9554622dae008566`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Latest live authority

Ron proved normal Pelipper Mutation followers work for Alolan Meowth and Yamper. Nidoran♂ still caused Pelipper to create Nidoran♀ followers, which Team Up correctly refused to claim. `teamup_mutation_regression` was Unknown in Ron's runtime despite existing in 6.7.44.37/38 source.

6.7.44.39 addresses both diagnostic/runtime-identity uncertainty and the Nidoran command-token ambiguity.

## 6.7.44.39

- early command registration for `teamup_build`, `teamup_mutation_regression`, and `teamup_lower_runtime`;
- startup build stamp with exact version/branch;
- Nidoran♂ command token `nidoran-m`;
- Nidoran♀ command token `nidoran-f`;
- request telemetry reports display species + exact spawn token;
- wrong-gender followers are still rejected by exact matching.

All 6.7.44.34-38 safety/Mutation/Lower Workings work remains carried forward. Old 6.7.44.24-30 crash services remain absent.

## Next live gate

Install into a clean Team Up folder, run `teamup_build`, then `teamup_mutation_regression`. Confirm build identity first, then live-test Nidoran gender exactness. No Runtime PASS until Ron confirms.
