# Team Up - Canonical Latest Handoff

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_19_PELIPPER_COMMAND_GATE_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.19`
- Branch: `v0.2-alpha6-7-44-19-pelipper-command-gate`
- CI source SHA: `bdc566349eedd1e111226e4da049c6e00c5d58f7`
- Run: `34970728030`
- Job: `104386087201`
- Artifact ID: `10396698147`
- Wrapper SHA256: `40f1c3738b2709e1a1a5d7ef247c1deb747b932383dbd606dae3623bdabad71a`
- Inner ZIP SHA256: `116835726f684870792cfe357a73fe8722bd40263562779cd36119d20e13d003`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Current live truth

6.7.44.18 proved the source-native follower architecture reaches the follower request, but Pelipper blocked each `pokemon_spawn` callback when its user-facing Spawn Commands setting was disabled. Minccino successfully transformed at HP 87 -> 261, x2 scale, requested 4 followers, then received four disabled-command messages and four timeouts. Capsakid showed the same issue.

## 6.7.44.19 delta

`Alpha674419PelipperSpawnCommandGateService` patches Team Up's internal Pelipper follower request. It locates only a Pelipper-owned writable boolean whose semantic member name contains both `spawn` and `command`. If the player has it disabled, Team Up opens it only in memory for the native callback and restores the exact original value in a Harmony finalizer. It never persists Pelipper config.

Pelipper still owns wild enablement, host checks, species validation, source/proxy creation, HP metadata, AI and native capture.

## Frozen Mutation contract

One encounter is one Mutant leader plus 2-4 ordinary hostile source-equivalent followers.

No unrelated Slime fallback is allowed.

Pelipper followers must be genuine same-species wild encounters. Vanilla/custom followers must use the corresponding source/runtime type when safely constructible. Unsupported custom sources fail closed.

Leader remains HP x3, stat x2, visible Pelipper scale cap x2, aura and global native loot x3. Followers receive no Mutation bonus/aura/x3 leader reward and are Mutation-excluded.

## Next runtime sequence

Keep Pelipper Spawn Commands OFF.

1. Force a normal non-Shiny Pelipper Mutation with `teamup_mutation force`.
2. Wait about one second and run `teamup_mutation status`.
3. For N=2-4 followers, expect same-species visible hostile followers, `pelipperNative=N`, `pending=0`, `sourceFailures=0`.
4. Gate telemetry should show `attempts=N`, `gateFound=N`, `bypasses=N`, `restores=N`, `probeFailures=0`, `writeFailures=0`.
5. Throw a Poké Ball at one follower and verify native capture.
6. Test all three leader HP phases and final x3 leader loot.
7. Test one vanilla/non-Pelipper Mutation and a compatible custom monster when practical.
8. Complete Lower Workings runtime gate.
9. Only then prepare 6.7.45 unless the user explicitly waives remaining gates.

## Carry-forward locks

- Confirmed Shiny remains Mutation-excluded.
- Real Pelipper HP remains `Griff.PelipperTown/WildCurrentHealth` / `WildMaxHealth`; never use the proxy's 1,000,000 sentinel.
- Preserve 20Hz encounter discovery and species-pair cache.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Lower Workings remains unchanged and gates 6.7.45 unless explicitly waived.
