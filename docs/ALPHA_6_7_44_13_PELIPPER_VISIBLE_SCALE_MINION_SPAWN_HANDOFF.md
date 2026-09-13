# Team Up handoff: 0.2.0-alpha.6.7.44.13

Canonical latest: `docs/LATEST_HANDOFF.md`

## Source of truth
- Branch: `v0.2-alpha6-7-44-13-pelipper-visible-scale-minion-spawn`
- Version: `0.2.0-alpha.6.7.44.13`
- CI input SHA: `c989c6407ac70ad799ffc5b06cbaac90c9717272`
- Run: `34778989370`
- Job: `103782389584`
- Artifact ID: `10324089476`
- Artifact wrapper SHA256: `ac0369fb12e2d249d918eeae10f5252a9d1705157674b551b955b354d8e49562`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.13_PELIPPER_VISIBLE_SCALE_MINION_SPAWN_TEST.zip`
- Test ZIP SHA256: `9e3c2fb4975c2944f915d38fb39e5002311178b31dfc3e66b40b30779cdfbd64`
- Build: PASS, 0 warnings / 0 errors
- main: NOT merged
- 6.7.45: NOT started

## Live truth from 6.7.44.12
Pelipper Mutation core is now live-confirmed:
- forced Mutation succeeded on Seadra and Tauros;
- `Pelipper modData HP binding` resolved/wrote `WildCurrentHealth/WildMaxHealth` successfully;
- source-aware Mutation reported `hpUnresolved=0`, `transformBlocked=0`, `forceTransforms=1`;
- natural pre-lethal Mutation rolls are now firing (`rolls` increased without force counting toward that counter);
- current Shiny handling remains acceptable and must stay frozen unless a concrete regression appears.

Two live regressions remained:
1. `Scalex3 scaleApplied=True` only scaled the hidden Pelipper Monster proxy. The visible `PelipperTown.PokemonNpc` sprite stayed normal-sized.
2. Mutation requested minions but spawned none on both a custom map and Farm (`spawned=0/N`, `safeRejected=N`). The strict `CollisionMask.All` placement gate is too restrictive for these environments.

## 6.7.44.13 changes
### Visible Pelipper Mutation scale
New `Alpha674413PelipperVisibleMutationService` patches successful `MonsterMutationService.TryMutate` results. For Pelipper wild proxies it resolves the visible Pokemon source and applies the configured Mutation visual multiplier to the source actor, preferring `_visualScaleMultiplier` before `_drawScale` fallbacks.

The service stores the original source scale and restores it when the encounter stops being an active wild Mutant, including source role changes/capture-like transitions. It also reapplies the Mutation scale at the 20Hz runtime pulse if Pelipper overwrites presentation state.

Status line:
`Pelipper visible Mutation: tracked=... | applied=... | reapplied=... | restored=... | failed=... | last=...`

### Mutation minion relaxed safe-spawn fallback
New `Alpha674413MutationMinionSpawnService` Harmony-postfixes the existing strict `TryFindSafeSpawnPosition`. The original strict gate always runs first. Only when it returns false does the fallback search nearby offsets requiring:
- tile on map;
- tile passable;
- no placed object;
- no terrain feature;
- minimum distance from Farmer;
- minimum distance from all other characters.

This intentionally drops only the over-broad `CollisionMask.All` rejection on the fallback pass.

Status line:
`Mutation minion spawn fallback: attempts=... | resolved=... | rejected=... | last=...`

## CI acceptance
Run `34778989370` passed:
- PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD: PASS
- PELIPPER VISIBLE MUTATION SCALE + RESTORE AUDIT: PASS
- MUTATION MINION RELAXED SAFE-SPAWN AUDIT: PASS
- C# BUILD: PASS
- ZIP CONTENT AUDIT: PASS
- 0 warnings / 0 errors

## Next live test
Fresh session, normal non-Shiny Pelipper wild Pokemon:

1. `teamup_mutation force`
2. visually confirm the actual Pokemon sprite becomes roughly x3 larger;
3. wait for the minion wave and confirm at least one minion can spawn on Farm/custom map;
4. `teamup_mutation status`

Return these lines:
- `Pelipper visible Mutation: ...`
- `Mutation minion spawn fallback: ...`
- `Pelipper SOURCE mutation: ...`
- `Pelipper modData HP binding: ...`
- final `[MutationTelemetry] ... minionsSpawned=...`

Then continue the existing HPx3 phase test: deplete one real Pelipper HP bar and verify `phaseGuards` and HP-binding writes increase.

## Carry-forward locks
- Shiny remains Mutation-excluded and current live Shiny behavior is frozen as accepted.
- Active Following/Waiting Team Up members cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz; performance remains a live gate.
- Never use the hidden Green Slime 1,000,000 HP sentinel as Pokemon HP.
- Pelipper remains controller/render/ownership authority.
- Lower Workings remains unchanged and still gates 6.7.45.
- NPC base damage remains moderate to preserve future level/gear/skill/build progression.
- Do not claim Mutants are fully non-catchable until the actual Pelipper ball-capture path is intercepted and live-tested.
