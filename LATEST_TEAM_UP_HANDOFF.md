# Team Up handoff: 0.2.0-alpha.6.7.44.11

Canonical latest: `docs/LATEST_HANDOFF.md`
Detailed current notes: `docs/ALPHA_6_7_44_11_PELIPPER_PAIR_CACHE_DUAL_PROBE_HANDOFF.md`

## Source of truth
- Branch: `v0.2-alpha6-7-44-11-pelipper-pair-cache-dual-probe`
- Version: `0.2.0-alpha.6.7.44.11`
- CI input SHA: `027da2bc8291973f9fd1ded78fa0a91d9a09ad3e`
- Run: `34750488830`
- Job: `103706056038`
- Artifact ID: `10315770465`
- Artifact wrapper SHA256: `60b0713ea63d68fb2f6d1b8af57f2489fe586073d262098cf0f31262db4b01c8`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.11_PELIPPER_PAIR_CACHE_DUAL_PROBE_TEST.zip`
- Test ZIP SHA256: `e8fe33602e64b9bd344b62f83def4df5a055968736813033c97f49dae4fb6afa`
- Build: PASS, 0 warnings / 0 errors
- `main` not merged
- 6.7.45 not started

## Live truth
6.7.44.10 successfully paired many Pelipper combat proxies to visible `PelipperTown.PokemonNpc` sources by exact species, proving the source actor exists. It also recalculated/logged the same pairings repeatedly and still rejected forced Mutation.

## 6.7.44.11
- successful species pairs are cached by proxy instance and validated before reuse;
- per-pair log spam is removed;
- pairing status adds `cacheHits` and `cacheInvalidated`;
- a rejected force deterministically probes both visible source and hidden proxy, including numeric candidates, nested state, relevant modData and member names;
- dual probe is cached by source/proxy runtime type pair and remains cold-path only;
- Mutation remains fail-closed until real writable Pokemon HP/state ownership is proven.

## Locked carry-forward
- Shiny behavior stays as currently accepted; Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery stays 20Hz; performance remains a live gate.
- Never treat Green Slime sentinel HP as Pokemon HP.
- Lower Workings remains unchanged and still gates 6.7.45.
- NPC base damage remains moderate with slow level scaling to preserve future gear/skill/build progression.

## Next live test
Run on a normal non-Shiny Pelipper wild Pokemon:

`teamup_mutation force`

then:

`teamup_mutation status`

Return:
- `Pelipper species pairing: ...`
- `Pelipper SOURCE mutation: ...`
- `Pelipper SOURCE HP probe: ...`
- `Pelipper dual HP probe: ...`

Expected: no repeated pairing flood; `cacheHits` increases; if force still rejects, `Pelipper dual HP probe: runs>=1` exposes the exact source/proxy layout for the next binding.
