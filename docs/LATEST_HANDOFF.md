# Team Up - Canonical Latest Handoff

Current detailed handoff: `docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.17`
- Branch: `v0.2-alpha6-7-44-17-lightweight-minions`
- CI verified source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- Run: `34904471245`
- Job: `104177799252`
- Artifact ID: `10371887676`
- Artifact wrapper SHA256: `ca2c4e2426c2c195c1f201030164d064244f6b639e87a4c349b05e766cbfa8f9`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- Inner ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Mutation design
A Mutation encounter is one Mutant leader plus 2-4 ordinary hostile minions. Only the leader receives Mutation combat bonuses and x3 loot. Followers are explicitly kept non-Mutant, mutation-excluded, and stripped of any accidental x3 reward marker.

For normal Stardew/custom monsters, the minion factory still prefers the original runtime monster type when it can safely recreate it. For Pelipper leaders, 6.7.44.17 deliberately takes a different performance-first path: each follower is one temporary Team Up combat actor, not a complete Pelipper wild encounter. Team Up therefore does not create an extra PokemonNpc, hidden Pelipper combat proxy, WildEncounterId, HP modData pair or Pelipper pairing/cache workload for those followers.

Pelipper visible Mutants remain capped at x2 size. Global x3 loot applies to Mutant leaders regardless of vanilla/custom/Pelipper origin. Minions never receive the Mutant x3 bonus. Captureability of Mutation followers is not a 6.7.44.17 gate; natural Pelipper wild Pokemon retain the source mod's normal capture behavior.

## CI gates passed
- PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD
- PELIPPER VISIBLE MUTATION X2 CAP + RESTORE
- MUTATION 2-4 MINION WIDE-SPAWN CARRY-FORWARD
- MUTANT LEADER + NORMAL HOSTILE MINION POLICY
- PELIPPER LIGHTWEIGHT ONE-ACTOR MINION POLICY
- GLOBAL LEADER-ONLY MUTANT LOOT-X3
- C# BUILD: 0 warnings / 0 errors
- ZIP CONTENT AUDIT

## Next live test
Force a normal non-Shiny Pelipper Mutation. Confirm 2-4 followers spawn, followers attack normally, there is no noticeable hitch when the wave appears, and `teamup_mutation status` reports `pelipperLightweight>0` plus `nativePelipperSpawnsAvoided>0`. Kill the leader through all HP phases and verify x3 leader reward telemetry. Also test one non-Pelipper Mutant to confirm same-runtime-type followers still work there.

## Carry-forward locks
- Current Shiny behavior stays frozen as accepted unless a concrete regression appears.
- Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz and pair cache stays enabled.
- Never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP.
- Pelipper remains controller/render/ownership authority for the real wild leader encounter.
- Lower Workings remains unchanged and still gates 6.7.45.
