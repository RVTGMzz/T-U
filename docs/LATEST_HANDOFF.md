# Team Up - Canonical Latest Handoff

Read this file first when continuing Team Up. Detailed current notes: `docs/ALPHA_6_7_44_11_PELIPPER_PAIR_CACHE_DUAL_PROBE_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.11`
- Branch: `v0.2-alpha6-7-44-11-pelipper-pair-cache-dual-probe`
- Base: `v0.2-alpha6-7-44-10-pelipper-species-pairing-probe`
- CI input SHA: `027da2bc8291973f9fd1ded78fa0a91d9a09ad3e`
- Successful CI run: `34750488830`
- Successful CI job: `103706056038`
- Artifact ID: `10315770465`
- Artifact wrapper SHA256: `60b0713ea63d68fb2f6d1b8af57f2489fe586073d262098cf0f31262db4b01c8`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.11_PELIPPER_PAIR_CACHE_DUAL_PROBE_TEST.zip`
- Inner ZIP SHA256: `e8fe33602e64b9bd344b62f83def4df5a055968736813033c97f49dae4fb6afa`
- Compiler: 0 warnings / 0 errors
- Main: NOT merged
- Stable: NOT declared
- 6.7.45: NOT started

## Live truth from 6.7.44.10
Species fallback successfully paired many hidden Green Slime combat proxies to visible `PelipperTown.PokemonNpc` actors. This proves late source identity is recoverable.

The live output also showed repeated pairing recalculation/log spam for the same encounters. That is a performance regression and can contribute to the previously reported combat lag. Forced Mutation still rejected the selected Pokemon, so HP ownership remains unresolved.

## 6.7.44.11
`Alpha674410PelipperSpeciesPairingService` now caches successful pairings by proxy instance. Cache reuse requires the source to remain present, same-location, Pelipper-wild, non-proxy, and same normalized species. Invalid entries are dropped.

Per-pair `[PelipperSpeciesPairing]` logging is removed. Status now includes `cacheHits` and `cacheInvalidated`.

New `Alpha674411PelipperDualHpProbeService` is cold-path only. A rejected `teamup_mutation force` immediately probes both the visible PokemonNpc source and hidden proxy, including numeric/wrapped values, nested battle/combat/state/Pokemon data, relevant modData, and readable member names. Probe results are cached by source/proxy runtime type pair.

Mutation remains fail-closed until a real writable Pelipper Pokemon HP/state path is proven. Never scale or use hidden Green Slime sentinel HP as Pokemon HP.

## Carry-forward locks
- Current Shiny behavior remains frozen as acceptable unless a concrete regression appears.
- Shiny remains Mutation-excluded.
- Active Following/Waiting Team Up members cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz; performance is still a live gate.
- Capture bonus/chance/rate/multiplier settings are not Catch Mode or HP floors.
- Lower Workings remains unchanged and still gates 6.7.45.
- NPC base damage stays moderate with slow level growth to preserve future gear/skill/build progression.
- Story slots 4/4, formation cap 5 PEOPLE including Farmers, George/Evelyn lore locks unchanged, no exact SECTOR 17, no final boss.

## CI acceptance
Run `34750488830` passed:
- VERSION + BUILD ENVIRONMENT AUDIT: PASS
- PELIPPER PAIR CACHE + LOG-SPAM AUDIT: PASS
- PELIPPER DUAL SOURCE+PROXY HP PROBE AUDIT: PASS
- SOURCE MUTATION FAIL-CLOSED CARRY-FORWARD: PASS
- USER-FACING + GIFT GUARD + EN/VI CARRY-FORWARD: PASS
- C# BUILD: PASS
- ZIP CONTENT AUDIT: PASS
- 0 warnings / 0 errors

## Next live test
Fresh session, normal non-Shiny Pelipper wild Pokemon:

`teamup_mutation force`

then:

`teamup_mutation status`

Return full lines:
- `Pelipper species pairing: ...`
- `Pelipper SOURCE mutation: ...`
- `Pelipper SOURCE HP probe: ...`
- `Pelipper dual HP probe: ...`

Expected: no repeated pairing flood; pair cache hits rise; if force still rejects, dual probe `runs>=1` exposes the real source/proxy layout.

New-chat instruction:
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-11-pelipper-pair-cache-dual-probe. Live-test teamup_mutation force + status trên Pokemon hoang non-Shiny; đọc Pelipper species pairing, Pelipper SOURCE mutation, Pelipper SOURCE HP probe và Pelipper dual HP probe trước khi làm tiếp.`
