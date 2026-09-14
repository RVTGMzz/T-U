# Team Up - Canonical Latest Handoff

Current detailed handoff: `docs/ALPHA_6_7_44_16_MUTANT_LEADER_NORMAL_MINIONS_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.16`
- Branch: `v0.2-alpha6-7-44-16-mutant-leader-minion-hostility`
- CI SHA: `8ab6f7105e505ec47a63f6dd93f77251e7c4fdc5`
- Run: `34901111167`
- Job: `104167024063`
- Artifact ID: `10370307939`
- Artifact wrapper SHA256: `bf313a16b2496c56c5ef2c820e506b9b6439eb8017585993558148cf030c24c4`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.16_MUTANT_LEADER_NORMAL_MINIONS_TEST.zip`
- Inner ZIP SHA256: `5074ee09ee439a0b94ec30bc1ae95d997c6da9236cbcf820eaac6941b3e46dbc`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Mutation design
A Mutation encounter is one Mutant leader plus 2-4 ordinary hostile minions. Only the leader receives Mutation combat bonuses and x3 loot. Followers are explicitly kept non-Mutant, mutation-excluded, and stripped of any accidental x3 reward marker. The minion factory prefers the original runtime monster type when it can safely recreate it, otherwise it uses the ordinary hostile fallback.

Pelipper visible Mutants remain capped at x2 size. Global x3 loot applies to Mutant leaders regardless of vanilla/custom/Pelipper origin. Minions retain their existing loot policy and never receive the Mutant x3 bonus.

## CI gates passed
- PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD
- PELIPPER VISIBLE MUTATION X2 CAP + RESTORE
- MUTATION 2-4 MINION WIDE-SPAWN CARRY-FORWARD
- MUTANT LEADER + NORMAL HOSTILE MINION POLICY
- GLOBAL LEADER-ONLY MUTANT LOOT-X3
- C# BUILD: 0 warnings / 0 errors
- ZIP CONTENT AUDIT

## Next live test
Force a normal non-Shiny Mutation. Confirm 2-4 followers spawn, leader and followers attack the player/party, followers remain normal size/state, and `teamup_mutation status` reports `leadersMarked>=1`, `minionsNormalized>0`, and `hostileReady>0`. Kill the leader through all HP phases and verify x3 leader reward telemetry. Test both Pelipper and one non-Pelipper Mutant.

## Carry-forward locks
- Current Shiny behavior stays frozen as accepted unless a concrete regression appears.
- Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz and pair cache stays enabled.
- Never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP.
- Pelipper remains controller/render/ownership authority.
- Lower Workings remains unchanged and still gates 6.7.45.
- Mutants are NOT yet proven non-catchable through Pelipper's real ball-capture path.
