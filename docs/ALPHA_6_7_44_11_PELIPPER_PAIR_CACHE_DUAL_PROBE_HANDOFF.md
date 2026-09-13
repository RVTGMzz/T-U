# Team Up 0.2.0-alpha.6.7.44.11

Branch: `v0.2-alpha6-7-44-11-pelipper-pair-cache-dual-probe`
CI SHA: `027da2bc8291973f9fd1ded78fa0a91d9a09ad3e`
Run: `34750488830`
Job: `103706056038`
Artifact ID: `10315770465`
ZIP: `TeamUp_v0.2.0-alpha.6.7.44.11_PELIPPER_PAIR_CACHE_DUAL_PROBE_TEST.zip`
ZIP SHA256: `e8fe33602e64b9bd344b62f83def4df5a055968736813033c97f49dae4fb6afa`
Build: PASS, 0 warnings / 0 errors.

Live 6.7.44.10 proved species pairing works between hidden Green Slime proxies and visible `PelipperTown.PokemonNpc` actors, but the same pairings were recalculated/logged repeatedly and forced Mutation still rejected the target.

6.7.44.11 therefore:
- caches successful species pairs by proxy instance with validation before reuse;
- removes per-pair `[PelipperSpeciesPairing]` log spam;
- adds aggregate `cacheHits/cacheInvalidated` telemetry;
- adds a cold-path dual HP probe that inspects both visible PokemonNpc source and hidden proxy after a rejected force;
- keeps source Mutation fail-closed until real writable HP/state ownership is proven.

Shiny behavior remains frozen as currently acceptable. Active teammate gift guard, 20Hz encounter discovery, Lower Workings and story locks are carried forward. `main` is not merged and 6.7.45 is not started.

Next live test on a normal non-Shiny wild Pokemon:
1. `teamup_mutation force`
2. `teamup_mutation status`
3. Return `Pelipper species pairing`, `Pelipper SOURCE mutation`, `Pelipper SOURCE HP probe`, and `Pelipper dual HP probe` lines.

Expected: no repeated pairing flood; pair cache hits rise; if force still rejects, dual probe runs at least once and exposes source/proxy member data for the next exact HP binding.
