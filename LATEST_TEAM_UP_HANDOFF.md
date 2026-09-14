# Team Up handoff: 0.2.0-alpha.6.7.44.17

Canonical latest: `docs/LATEST_HANDOFF.md`
Detailed notes: `docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md`

- Branch: `v0.2-alpha6-7-44-17-lightweight-minions`
- CI verified source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- Run: `34904471245`
- Job: `104177799252`
- Artifact ID: `10371887676`
- Artifact wrapper SHA256: `ca2c4e2426c2c195c1f201030164d064244f6b639e87a4c349b05e766cbfa8f9`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- Build: PASS, 0 warnings / 0 errors
- main not merged
- 6.7.45 not started

## Current Mutation design lock
- One Mutant leader + 2-4 ordinary hostile minions.
- Leader keeps HPx3, stat x2, Pelipper visible scale x2, aura and global loot x3.
- Minions are normal monsters: no Mutation bonuses, no Mutation aura, no x3 loot bonus, and they are mutation-excluded so they cannot recursively mutate.
- For non-Pelipper leaders, the factory still prefers the same runtime type when safely constructible.
- For Pelipper leaders, followers use Team Up's lightweight one-actor path instead of spawning complete Pelipper PokemonNpc + combat-proxy encounter pairs. This is the performance-first choice.
- Captureability of Mutation followers is no longer a 6.7.44.17 gate. Natural Pelipper wild Pokemon keep native capture behavior.

## Live truth carried forward
Pelipper Mutation core, real proxy-modData HP binding, natural Mutation rolls and visible source scaling are live-confirmed. 6.7.44.15 widened the 2-4 minion placement search after earlier live waves spawned 0/N. 6.7.44.17 now avoids native Pelipper follower encounter creation entirely to reduce runtime overhead. Minion spawn, hostility, three HP phases and final x3 reward still need live confirmation together.

## Next live test
Force a normal non-Shiny Pelipper Mutation. Confirm x2 leader size, 2-4 followers actually spawn, followers attack normally, and there is no noticeable hitch when the wave appears. Run `teamup_mutation status`; expected policy telemetry includes `pelipperLightweight>0` and `nativePelipperSpawnsAvoided>0`. Kill the leader through all HP phases and verify x3 leader reward. Also test one non-Pelipper Mutant.

Carry-forward locks:
- current Shiny behavior remains frozen as accepted unless a concrete regression appears;
- Shiny remains Mutation-excluded;
- active Following/Waiting teammates cannot receive held-item vanilla gifts;
- pair cache/performance safeguards remain enabled;
- never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP;
- Pelipper retains controller/render/ownership authority for the real wild leader encounter;
- Lower Workings remains unchanged and still gates 6.7.45.
