# Alpha 6.7.44.39 - Runtime Identity + Nidoran Exact Handoff

Repository: `RVTGMzz/T-U`

Branch: `v0.2-alpha6-7-44-39-runtime-identity-nidoran-exact`

Version: `0.2.0-alpha.6.7.44.39`

## Build

- CI source SHA: `feee4184c7c991743b695e70dc071b13e56d9650`
- Run: `35373758996`
- Job: `105693541626`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.39_RUNTIME_IDENTITY_NIDORAN_EXACT_TEST.zip`
- SHA256: `b49f1597b8a9316d521c81f0c732c5c5bdbd76ca3dabc9fb9554622dae008566`
- Build: PASS, 0 warnings / 0 errors
- ZIP audit: PASS

## Why this build exists

Ron live-tested Mutation and reported two concrete regressions.

First, `teamup_mutation_regression` was Unknown, even though 6.7.44.37/38 source registered it. That makes installed DLL identity uncertain.

Second, a Nidoran♂ Mutant caused Pelipper's native `pokemon_spawn` route to create Nidoran♀ followers. Team Up's exact matcher rejected those followers, so the safety layer worked but the requested spawn identity was wrong.

## Runtime identity

`teamup_mutation_regression` is registered in early core wiring instead of the later runtime-fix block.

New command:

`teamup_build`

It reports exact loaded version/branch and diagnostic-command registration.

Startup also logs:

`[TeamUpBuild] version=... branch=v0.2-alpha6-7-44-39-runtime-identity-nidoran-exact`

## Nidoran exact native spawn

Before Pelipper `pokemon_spawn`:

- `Nidoran♂` normalizes to `nidoranmale` and maps to `nidoran-m`;
- `Nidoran♀` normalizes to `nidoranfemale` and maps to `nidoran-f`;
- all other species retain the existing display token.

Telemetry:

`[MutationNativePelipperRequest] species=<display> spawnToken=<token> level=<n>`

Existing post-spawn matching still compares gender-aware normalized species and rejects a mismatch.

## Live test

Use a clean Team Up mod folder.

Run:

```text
teamup_build
teamup_mutation_regression
```

Expected: both commands exist and build reports 6.7.44.39.

Then force Mutation. For Nidoran, verify request token and actual Pelipper-created species are the same gender as the leader.

Do not call Runtime PASS until Ron confirms.

## Carry-forward

All 6.7.44.34-38 work remains present and old 6.7.44.24-30 crash services remain excluded.
