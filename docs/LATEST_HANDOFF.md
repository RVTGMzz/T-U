# Team Up - Canonical Latest Handoff

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `docs/ALPHA_6_7_44_18_NATIVE_MINIONS_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.18`
- Branch: `v0.2-alpha6-7-44-18-native-minions`
- CI-verified source SHA: `8c73eb93a3a7529e3d8232773e7c61c73ca9567d`
- Run: `34914882066`
- Job: `104210252539`
- Artifact ID: `10375867225`
- Artifact name: `team-up-alpha6-7-44-18-native-source-minions`
- Artifact wrapper SHA256: `9aaf524f071610f64c5e93dc05dc9ad4225f586e21ca832ef41b4059e7afeebd`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.18_NATIVE_SOURCE_MINIONS_TEST.zip`
- Inner ZIP SHA256: `81a0a16ec0890f2434fa32961dcf08242933377197348918a17f340eefa63957`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

Docs-only commits after CI may make branch HEAD newer. Keep `8c73eb9...` as the source SHA that produced the downloadable ZIP.

## Authoritative Mutation contract

A Mutation encounter is one Mutant leader plus 2-4 ordinary hostile followers.

Mutant leader:

- HP x3;
- stat x2;
- aura;
- Pelipper visible source x2 cap;
- global x3 native loot at final death.

Followers:

- must correspond to the creature before Mutation;
- ordinary hostile;
- no Mutation bonuses/aura;
- no x3 leader reward;
- mutation-excluded;
- no unrelated Slime fallback.

This replaces the 6.7.44.17 performance-first lightweight-Slime design. The user explicitly chose source fidelity and native behavior because Mutation is rare enough that the extra native spawn cost is acceptable.

## Pelipper source-native follower lock

For Pelipper Mutant leaders, Team Up now requests genuine native wild Pokemon of the same species through Pelipper's registered native spawn pipeline.

Expected result: a Nidoran♂ Mutant calls 2-4 ordinary Nidoran♂ wild followers. They should attack normally and remain normal Pelipper capture targets.

Native capture is still runtime-pending and must be tested with a Poké Ball before being called live-passed.

Telemetry:

- `pelipperCommands=...`
- `pelipperNative=...`
- `sourceFailures=...`
- `pending=...`
- `deferredResolved=...`
- `deferredExpired=...`

If Pelipper's `pokemon_spawn` callback is unavailable, fail closed and inspect the `Pokémon spawn commands` setting or replace the adapter with a direct native entry point. Do not return to Slime fallback.

## Vanilla/custom source-equivalent lock

For non-Pelipper leaders, Team Up accepts only a safely-created same runtime/source type.

If the generic factory would produce a GreenSlime fallback, 6.7.44.18 discards it before map insertion and increments `sourceFailures`.

Future source mods with opaque construction/capture systems may receive dedicated native adapters. Generic compatibility must never fabricate an unrelated follower merely to satisfy the requested count.

## CI gates passed

- PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD
- PELIPPER VISIBLE MUTATION X2 CAP + RESTORE
- MUTATION 2-4 MINION WIDE-SPAWN CARRY-FORWARD
- MUTANT LEADER + SOURCE-EQUIVALENT NORMAL HOSTILE MINION POLICY
- SOURCE-NATIVE MINIONS + PELIPPER NATIVE CAPTURE PIPELINE AUDIT
- GLOBAL LEADER-ONLY MUTANT LOOT-X3
- C# BUILD, 0 warnings / 0 errors
- ZIP CONTENT AUDIT

## Live-proven vs runtime-pending

Live-proven before this build:

- Pelipper forced Mutation succeeds;
- real Pokemon HP binding succeeds through `Griff.PelipperTown/WildCurrentHealth` and `WildMaxHealth`;
- natural Mutation rolls reach the engine;
- Pelipper source/proxy pairing cache works;
- visible Pelipper source x2 scaling works;
- wide placement can spawn the full requested wave with `safeRejected=0`;
- ordinary follower hostility works.

Runtime-pending in 6.7.44.18:

- same-species native Pelipper followers;
- native follower capture;
- native-wave performance;
- three HP phase completion;
- final global x3 loot;
- vanilla same-runtime regression;
- compatible custom-mod regression;
- Lower Workings runtime gate.

## Next runtime sequence

1. On a normal non-Shiny Pelipper wild Pokemon run `teamup_mutation force`.
2. Wait about one second and run `teamup_mutation status`.
3. Confirm 2-4 followers are the same species, no Slimes, hostility works, `pelipperCommands>0`, `pelipperNative>0`, `sourceFailures=0`, and `pending=0` after resolution.
4. Throw a Poké Ball at one follower and confirm Pelipper native capture.
5. Deplete the leader's first HP bar and expect `phaseGuards=1` plus a real-HP modData write.
6. Deplete the second bar and expect `phaseGuards=2`.
7. Deplete the third bar and expect final death plus `finalLethalPasses` increment.
8. Confirm global x3 reward: `dropCalls>=1`, `extraDropPasses+=2`, `errors=0`.
9. Test one vanilla Mutant and one compatible custom-mod Mutant.
10. Complete Lower Workings runtime gate.
11. Only then prepare 6.7.45 unless the user explicitly waives remaining gates.

## Pelipper HP architecture lock

Visible `PelipperTown.PokemonNpc` owns species identity, sprite/render, display name and Shiny evidence.

The hidden Monster proxy owns combat/controller behavior. Its ordinary `Health/MaxHealth = 1,000,000` is only a technical sentinel.

Real Pokemon HP remains:

- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

Never regress to using the sentinel as Pokemon HP.

## Carry-forward locks

- Current Shiny behavior remains frozen unless a concrete regression appears.
- Confirmed Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz and the pair cache stays enabled.
- High `cacheHits` means reuse, not full pairing scans.
- Pelipper retains controller/render/ownership/capture authority for genuine Pelipper actors.
- Performance remains a subjective live gate.
- Lower Workings remains unchanged and gates 6.7.45 unless explicitly waived.
- Runtime roster currently reports 111 profile rows; do not publish-lock the older manual ~110 count until reconciled.

## Planned 6.7.45

**Containment Chamber Escalation Encounter** in the real Lower Workings. Keep George ordinary/anonymous until 6.7.46, preserve host authority and secure retreat, do not introduce exact `SECTOR 17`, and do not create a final boss yet.
