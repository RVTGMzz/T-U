# Team Up 0.2.0-alpha.6.7.44.19 handoff

## Checkpoint

- Branch: `v0.2-alpha6-7-44-19-pelipper-command-gate`
- Version: `0.2.0-alpha.6.7.44.19`
- CI source SHA: `bdc566349eedd1e111226e4da049c6e00c5d58f7`
- CI run: `34970728030`
- CI job: `104386087201`
- Artifact ID: `10396698147`
- Artifact: `team-up-alpha6-7-44-19-pelipper-native-minion-gate-fix`
- Artifact wrapper SHA256: `40f1c3738b2709e1a1a5d7ef247c1deb747b932383dbd606dae3623bdabad71a`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.19_PELIPPER_NATIVE_MINION_GATE_FIX_TEST.zip`
- Inner ZIP SHA256: `116835726f684870792cfe357a73fe8722bd40263562779cd36119d20e13d003`
- Build: PASS, 0 warnings, 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Why 6.7.44.19 exists

6.7.44.18 live testing proved Mutation itself worked, but Pelipper followers failed whenever Pelipper's player-facing `Spawn Commands` option was disabled.

Live evidence:

- Minccino: base HP 87 -> Mutant HP 261, x2 scale, 4 followers requested.
- Pelipper printed `Lệnh spawn Pokémon đã bị vô hiệu hóa trong Tùy chọn Mod.` four times.
- Team Up reported `spawnedNow=0 pending=4`, then all four requests timed out.
- Earlier Capsakid requests failed the same way.

Therefore the failure was not Mutation RNG, safe placement or follower count. It was exclusively the debug/cheat command gate.

## 6.7.44.19 fix

`Alpha674419PelipperSpawnCommandGateService` Harmony-patches Team Up's internal `RequestPelipperNativeSpawn`.

For each Team Up internal native follower request:

1. resolve the registered Pelipper `pokemon_spawn` callback;
2. inspect only Pelipper-owned runtime objects;
3. locate a writable boolean whose member name semantically contains both `spawn` and `command`;
4. if the player's value is false, temporarily set that in-memory value to true;
5. let 6.7.44.18 invoke Pelipper's own native spawn callback;
6. restore the exact original value in a Harmony finalizer even if the request throws.

The patch does NOT persist Pelipper config and does NOT bypass wild-Pokémon enablement, host checks, species validation or Pelipper's native source/proxy/capture pipeline.

## Mutation contract remains frozen

One encounter is 1 Mutant leader + 2-4 ordinary hostile followers matching the creature before Mutation.

No unrelated Slime fallback is allowed.

Pelipper followers must be genuine same-species Pelipper wild encounters and remain natively catchable. Vanilla/custom followers must be source-equivalent when Team Up can construct them safely. Unsupported custom sources fail closed and report telemetry.

Leader keeps HP x3, stats x2, visible Pelipper scale cap x2, Mutation aura and global native loot x3. Followers receive no Mutation bonus/aura/x3 leader reward and are marked Mutation-excluded.

## New telemetry gate

After forcing a Pelipper Mutation and waiting about one second, run:

`teamup_mutation status`

Expected for a 4-follower wave while Pelipper Spawn Commands remains disabled by the player:

- `Pelipper Mutation spawn-command gate: attempts=4`
- `gateFound=4`
- `bypasses=4`
- `restores=4`
- `probeFailures=0`
- `writeFailures=0`
- native minions: `pelipperNative=4`, `pending=0`, `sourceFailures=0`

The exact count can be 2-4 according to the wave roll, but `bypasses` and `restores` must match when the player setting is off.

Then throw a Poké Ball at one follower. Capture is still a live gate until proven.

## Remaining gates before 6.7.45

- 6.7.44.19 same-species Pelipper follower spawn with player Spawn Commands option OFF.
- Spawn-command gate restore telemetry balanced.
- Native Poké Ball capture of one Mutation follower.
- Three Pelipper leader HP phases.
- Final global x3 leader loot.
- One vanilla/non-Pelipper Mutation regression.
- One compatible custom-monster regression when practical.
- Lower Workings runtime gate unless explicitly waived by the user.
