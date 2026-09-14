# Team Up handoff: 0.2.0-alpha.6.7.44.16

Canonical latest: `docs/LATEST_HANDOFF.md`
Detailed notes: `docs/ALPHA_6_7_44_16_MUTANT_LEADER_NORMAL_MINIONS_HANDOFF.md`

- Branch: `v0.2-alpha6-7-44-16-mutant-leader-minion-hostility`
- CI SHA: `8ab6f7105e505ec47a63f6dd93f77251e7c4fdc5`
- Run: `34901111167`
- Job: `104167024063`
- Artifact ID: `10370307939`
- Artifact wrapper SHA256: `bf313a16b2496c56c5ef2c820e506b9b6439eb8017585993558148cf030c24c4`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.16_MUTANT_LEADER_NORMAL_MINIONS_TEST.zip`
- ZIP SHA256: `5074ee09ee439a0b94ec30bc1ae95d997c6da9236cbcf820eaac6941b3e46dbc`
- Build: PASS, 0 warnings / 0 errors
- main not merged
- 6.7.45 not started

## Current Mutation design lock
- One Mutant leader + 2-4 ordinary hostile minions.
- Leader keeps HPx3, stat x2, Pelipper visible scale x2, aura and global loot x3.
- Minions are normal monsters: no Mutation bonuses, no Mutation aura, no x3 loot bonus, and they are mutation-excluded so they cannot recursively mutate.
- Minion factory prefers the same runtime type when safely constructible; otherwise it uses the ordinary hostile fallback.
- Both leader and minions remain Team Up combat targets and are expected to attack the player using their normal hostile AI.

## Live truth carried forward
Pelipper Mutation core, real proxy-modData HP binding, natural Mutation rolls and visible source scaling are live-confirmed. 6.7.44.15 widened the 2-4 minion placement search after earlier live waves spawned 0/N; this still needs live validation. Global x3 leader loot also still needs final-death live validation.

## Next live test
Force a normal non-Shiny Mutation. Confirm x2 leader size for Pelipper, 2-4 followers actually spawn, leader + followers attack the player/party, and followers remain ordinary. Run `teamup_mutation status`; expected new policy telemetry has `leadersMarked>=1`, `minionsNormalized>0`, `hostileReady>0`, and no follower loot bonus. Kill the leader through all HP phases and verify global x3 leader reward. Also test a non-Pelipper Mutant.

Carry-forward locks:
- current Shiny behavior remains frozen as accepted unless a concrete regression appears;
- Shiny remains Mutation-excluded;
- active Following/Waiting teammates cannot receive held-item vanilla gifts;
- pair cache/performance safeguards remain enabled;
- never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP;
- Pelipper retains controller/render/ownership authority;
- Lower Workings remains unchanged and still gates 6.7.45;
- do not claim Mutants fully non-catchable until the actual Pelipper ball-capture path is intercepted and live-tested.
