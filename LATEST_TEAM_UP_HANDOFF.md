# Team Up latest handoff: 0.2.0-alpha.6.7.44.19

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_19_PELIPPER_COMMAND_GATE_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-19-pelipper-command-gate`
- Version: `0.2.0-alpha.6.7.44.19`
- CI source SHA: `bdc566349eedd1e111226e4da049c6e00c5d58f7`
- Run: `34970728030`
- Job: `104386087201`
- Artifact ID: `10396698147`
- Inner ZIP SHA256: `116835726f684870792cfe357a73fe8722bd40263562779cd36119d20e13d003`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Live result from 6.7.44.18

Mutation leader creation worked. Minccino transformed with HP 87 -> 261, visible x2 scale and 4 requested followers. All four followers failed only because Pelipper printed that Pokémon spawn commands were disabled in Mod Options, after which Team Up timed them out. Capsakid showed the same failure.

## 6.7.44.19 fix

Team Up preserves Pelipper's native `pokemon_spawn` pipeline, but an internal Mutation follower request no longer requires the user to enable the debug/cheat Spawn Commands option. Team Up temporarily opens only the matching in-memory Pelipper spawn+command boolean, executes the native request, and restores the player's original value in a Harmony finalizer. No config persistence is performed.

## Current Mutation contract

One Mutant leader + 2-4 ordinary hostile followers matching the original creature. No unrelated Slime fallback.

Pelipper followers are genuine same-species wild encounters and should remain natively catchable. Vanilla/custom followers must remain source-equivalent where safely constructible. Unsupported sources fail closed.

## Immediate live gate

Keep Pelipper Spawn Commands OFF, run `teamup_mutation force`, then after about one second run `teamup_mutation status`.

For N requested followers, expect `pelipperNative=N`, `pending=0`, `sourceFailures=0`, and gate telemetry `attempts=N`, `gateFound=N`, `bypasses=N`, `restores=N`, `probeFailures=0`, `writeFailures=0`.

Then catch one follower with a Poké Ball. After capture, test 3 HP phases, x3 final leader loot, one vanilla/non-Pelipper Mutation, a compatible custom monster when practical, and Lower Workings.

Do not begin 6.7.45 unless the remaining runtime gates are passed or explicitly waived by the user.
