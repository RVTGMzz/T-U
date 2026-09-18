# Continue Team Up Here

Repository: **`RVTGMzz/T-U`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.39**

Development branch:

`v0.2-alpha6-7-44-39-runtime-identity-nidoran-exact`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_39_RUNTIME_IDENTITY_NIDORAN_EXACT_HANDOFF.md`

## Verified build checkpoint

- Repository: `RVTGMzz/T-U`
- Version: `0.2.0-alpha.6.7.44.39`
- Branch: `v0.2-alpha6-7-44-39-runtime-identity-nidoran-exact`
- CI source SHA: `feee4184c7c991743b695e70dc071b13e56d9650`
- CI run: `35373758996`
- CI job: `105693541626`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.39_RUNTIME_IDENTITY_NIDORAN_EXACT_TEST.zip`
- ZIP SHA256: `b49f1597b8a9316d521c81f0c732c5c5bdbd76ca3dabc9fb9554622dae008566`
- Build: PASS, 0 warnings, 0 errors
- Package audit: PASS

## Latest live authority from Ron

User runtime showed:

- `teamup_mutation force` works for Alolan Meowth and Yamper with genuine native same-species Pelipper followers.
- Mutation combat telemetry shows the 6.7.44.34 damage path active: raw proxy damage 1 -> base floor 4 -> intended Mutant damage 8.
- `teamup_mutation_regression` returned Unknown command.
- Nidoran♂ Mutant requested followers, but Pelipper created Nidoran♀. Team Up refused to claim them, leaving `spawnedNow=0 pending=2`.

Therefore:
- native follower architecture is working generally;
- Nidoran gender exactness is still a live gate;
- the Unknown command strongly indicates the installed DLL was not the expected 6.7.44.37/38 runtime, so 6.7.44.39 adds explicit build identity.

## 6.7.44.39 delta

### Runtime identity

Commands are registered early in the core 6.7.44 wiring, before runtime-fix initialization:

- `teamup_build`
- `teamup_mutation_regression`
- `teamup_lower_runtime`

Startup prints:

`[TeamUpBuild] version=0.2.0-alpha.6.7.44.39 branch=v0.2-alpha6-7-44-39-runtime-identity-nidoran-exact`

`teamup_build` prints the exact loaded build and confirms the diagnostic commands are registered.

### Nidoran exact spawn request

Before calling Pelipper `pokemon_spawn`:

- Nidoran♂ -> `nidoran-m`
- Nidoran♀ -> `nidoran-f`
- all other species keep their existing display token.

New debug telemetry:

`[MutationNativePelipperRequest] species=<display> spawnToken=<token> level=<n>`

The existing exact species matcher still refuses a wrong-gender spawned actor.

## Immediate live test

Install 6.7.44.39 into a clean Team Up mod folder.

Run:

```text
teamup_build
teamup_mutation_regression
```

Both commands must exist. `teamup_build` must report 6.7.44.39 and branch `v0.2-alpha6-7-44-39-runtime-identity-nidoran-exact`.

Then force Mutation until Nidoran♂ or Nidoran♀ is selected. The request telemetry should show the matching `nidoran-m` or `nidoran-f` token, and native followers must match the leader gender.

Do not call Nidoran Runtime PASS until Ron confirms.

## Carry-forward locks

- old 6.7.44.24-30 crash stack remains excluded;
- 6.7.44.34 combat presence remains;
- 6.7.44.35 leader pursuit/reach remains;
- 6.7.44.36 elite contract remains;
- 6.7.44.37 fail-closed/no-GreenSlime architecture remains;
- 6.7.44.38 Lower Workings Runtime Gate v2 remains.

## Next

After 6.7.44.39 confirms:
1. regression command is actually available on Ron's installed build;
2. Nidoran gender-native follower spawn is exact;
3. Lower Workings v2 can be queried without load/runtime failure;

then proceed toward 6.7.45 Containment Chamber Escalation, unless another concrete runtime regression appears.
