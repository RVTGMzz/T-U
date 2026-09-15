# Team Up 0.2.0-alpha.6.7.44.21 - Pelipper Mutation Hostility Handoff

## Canonical checkpoint

- Branch: `v0.2-alpha6-7-44-21-pelipper-mutation-hostility`
- Version: `0.2.0-alpha.6.7.44.21`
- CI source SHA: `9bb3a88a98c51a3695ffa9e982f3da482c08632c`
- CI run: `34976619122`
- CI job: `104405839414`
- Artifact ID: `10398993101`
- Artifact: `team-up-alpha6-7-44-21-pelipper-mutation-hostility`
- Wrapper SHA256: `c27601d8160b76edaad383ed0be7a1dcd47f0b5e4b672101e71b8b38ae41d72e`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.21_PELIPPER_MUTATION_HOSTILITY_TEST.zip`
- Inner ZIP SHA256: `7e68d423593acf32efcfa1d618bb5d5f90cfa9a0f9679e976b309ee6aaa602e9`
- Build: PASS, 0 warnings, 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Live finding that supersedes 6.7.44.20

6.7.44.20 was tested live on Growlithe. Native source/minion spawning still passed: four genuine Growlithe followers were created, `spawnedNow=4`, `pending=0`, `failed=0`, `safeRejected=0`, native capture identity retained and no Slime fallback.

The native aggro layer also definitely armed the actors:

- `leaderArms=8`
- `minionArms=67`
- `pelipperProxyArms=75`
- `pelipperSourceArms=75`
- `damageFloors=4`
- `identityMisses=0`

Despite that, neither Mutant nor followers attacked Farmer. This proves Pelipper wild encounters do not consume Stardew's generic `Monster.focusedOnFarmers` / `moveTowardPlayer()` flags as hostile AI. Do not return to repeatedly setting those flags as the main fix.

## 6.7.44.21 architecture

`Alpha674421PelipperMutationHostilityService` adds a Team Up-owned hostility layer only for Pelipper Mutation leaders and Mutation minions.

It preserves the genuine Pelipper source/proxy pair and capture identity:

1. Resolve the real `PelipperWildEncounterIdentity`.
2. Move the visible `PokemonNpc` toward a passable tile adjacent to Farmer using a private `PathFindController`.
3. Repath every 12 ticks or when the target tile changes.
4. Synchronize the genuine hidden Monster combat proxy to the visible source position.
5. When source/proxy bounding box touches Farmer, call `Farmer.takeDamage(..., proxy)` with a 45-tick per-proxy cooldown.
6. Natural non-Mutation Pelipper wild Pokemon are not touched.

This is not a new fake actor, not a Slime fallback, and not a respawn. Native Pelipper source/proxy/capture identity stays intact.

Telemetry line:

`Pelipper Mutation hostility: teamup-pathing | ticks=... | pairs=... | leaders=... | minions=... | pathsBuilt=... | pathFailures=... | pathSteps=... | proxySyncs=... | contactDamageCalls=... | identityMisses=... | last=...`

## Frozen Mutation contract

One encounter is exactly one Mutant leader plus 2-4 ordinary source-equivalent followers.

Leader:
- HP x3
- stats x2
- Pelipper visible scale cap x2
- Mutation aura
- global native loot x3 on final defeat

Followers:
- same source/species as the pre-Mutation creature
- ordinary, no Mutation bonus/aura
- MutationExcluded
- no x3 leader loot bonus
- valid Team Up combat targets
- no unrelated Slime fallback
- genuine Pelipper followers remain natively catchable in architecture

## Proven live gates

- Pelipper native same-species follower spawn: PASS
- 4/4 follower wave observed: PASS
- Spawn Commands player setting may stay OFF: PASS
- internal gate bypass + exact restore: PASS
- no Slime fallback: PASS
- source/proxy identity pairing: PASS
- real Pelipper HP binding from `WildCurrentHealth` / `WildMaxHealth`: previously PASS
- visible leader x2 scale: PASS
- 6.7.44.20 generic native pursuit flags: proven ineffective for Pelipper hostility

## Immediate runtime gate for 6.7.44.21

1. Keep Pelipper Spawn Commands OFF.
2. Find a normal non-Shiny wild Pokemon.
3. Run `teamup_mutation force`.
4. Do not attack first. Stand/move nearby.
5. Verify visible leader + 2-4 visible followers path toward Farmer.
6. Let one touch Farmer and verify HP decreases.
7. Run `teamup_mutation status`.

Expected new telemetry:
- `pairs > 0`
- `leaders > 0`
- `minions > 0`
- `pathsBuilt > 0`
- `pathSteps > 0`
- usually `proxySyncs > 0`
- after contact, `contactDamageCalls > 0`
- ideally `identityMisses = 0`

Also confirm source-native spawn telemetry remains clean: `pelipperNative=N`, `pending=0`, `sourceFailures=0`.

Then throw a Poke Ball at one follower to explicitly live-confirm capture after the new movement layer.

## Remaining gates before 6.7.45

After hostility/capture:
- test all three Pelipper leader HP phases;
- test final global x3 leader loot;
- test one vanilla/non-Pelipper Mutation;
- test a compatible custom source when practical;
- complete Lower Workings runtime gate.

Do not begin 6.7.45 unless these gates pass or the user explicitly waives them.

## Carry-forward locks

- Confirmed Shiny stays Mutation-excluded.
- Never use Pelipper proxy technical 1,000,000 HP sentinel as Pokemon HP.
- Preserve 20Hz Pelipper discovery and species pair cache.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Lower Workings remains unchanged and still gates 6.7.45 unless waived.
- Planned 6.7.45 is Containment Chamber Escalation Encounter; George remains ordinary/anonymous until 6.7.46.
