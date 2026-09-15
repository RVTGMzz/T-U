# Team Up - Canonical Latest Handoff

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md`

Current dated chat transfer: `handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_7_44_17_2026-09-15.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.17`
- Branch: `v0.2-alpha6-7-44-17-lightweight-minions`
- CI-verified source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- Run: `34904471245`
- Job: `104177799252`
- Artifact ID: `10371887676`
- Artifact name: `team-up-alpha6-7-44-17-lightweight-minions`
- Artifact wrapper SHA256: `ca2c4e2426c2c195c1f201030164d064244f6b639e87a4c349b05e766cbfa8f9`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- Inner ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

Docs-only commits after the CI run may make branch HEAD newer than the verified artifact source SHA. Keep `c1df68...` as the code SHA that produced this ZIP.

## Current Mutation contract

A Mutation encounter is one Mutant leader plus 2-4 ordinary hostile followers.

Mutant leader:

- HPx3;
- stat x2;
- aura;
- Pelipper visible source x2 cap;
- global x3 native loot at final death.

Followers:

- ordinary hostile;
- no Mutation bonuses/aura;
- no x3 leader reward;
- mutation-excluded so no recursive Mutation.

Global x3 loot is not Pelipper-specific. It applies to every successful Mutant leader using the hooked native `monsterDrop` path. The implementation runs the original native drop once plus two extra native drop passes.

## Pelipper lightweight follower lock

User decision: prioritize lower runtime cost over whether temporary Mutation followers are catchable.

For Pelipper Mutant leaders, Team Up must NOT request 2-4 full native Pelipper wild encounters. A full encounter would create extra `PokemonNpc`, hidden combat proxy, encounter identity, HP modData and pairing/runtime work for each temporary follower.

6.7.44.17 instead creates one lightweight Team Up combat actor per Pelipper follower. Current implementation uses a temporary normal hostile actor and records the leader species as metadata. It deliberately avoids native Pelipper encounter creation.

Expected telemetry:

- `pelipperLightweight=...`
- `nativePelipperSpawnsAvoided=...`

Follower captureability is not a runtime gate. Natural Pelipper wild Pokemon remain under Pelipper's native capture rules.

If follower visuals need improvement later, prefer a visual skin/override on the lightweight actor. Do not reintroduce full native Pelipper follower encounters unless the user explicitly accepts the performance tradeoff.

The experimental `v0.2-alpha6-7-44-17-mutant-capture-lock` branch is superseded and non-canonical.

## CI gates passed

- PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD
- PELIPPER VISIBLE MUTATION X2 CAP + RESTORE
- MUTATION 2-4 MINION WIDE-SPAWN CARRY-FORWARD
- MUTANT LEADER + NORMAL HOSTILE MINION POLICY
- PELIPPER LIGHTWEIGHT ONE-ACTOR MINION POLICY
- GLOBAL LEADER-ONLY MUTANT LOOT-X3
- C# BUILD: 0 warnings / 0 errors
- ZIP CONTENT AUDIT

## Live-proven vs CI-only

Live-proven from earlier builds:

- Pelipper forced Mutation succeeds.
- Real Pokemon HP binding through `Griff.PelipperTown/WildCurrentHealth` / `WildMaxHealth` succeeds.
- Natural non-force deaths reach Mutation rolls.
- Pelipper source/proxy species pairing cache works.
- Visible Pelipper source scaling works.
- x3 visible scale was too large, so current cap is x2.

Still CI-only / runtime-pending in 6.7.44.17:

- 2-4 lightweight Pelipper followers actually spawn;
- followers attack normally;
- no visible hitch when the wave appears;
- lightweight counters increment;
- three HP phases complete correctly;
- final global x3 loot works live;
- non-Pelipper regression test.

## Next runtime sequence

1. Force a normal non-Shiny Pelipper Mutation with `teamup_mutation force`.
2. Wait about one second and run `teamup_mutation status`.
3. Confirm 2-4 followers, normal hostility, no lag, `pelipperLightweight>0`, `nativePelipperSpawnsAvoided>0`, `minionsNormalized>0`, `hostileReady>0`.
4. Deplete first HP bar and expect `phaseGuards=1` plus a real-HP modData write.
5. Deplete second HP bar and expect `phaseGuards=2`.
6. Deplete third HP bar and expect final death plus `finalLethalPasses` increment.
7. Confirm `Mutant reward` has `dropCalls>=1`, two new `extraDropPasses`, and `errors=0`.
8. Test one non-Pelipper Mutant.
9. Complete Lower Workings runtime gate.
10. Only then prepare 6.7.45 unless the user explicitly waives remaining gates.

## Pelipper HP architecture lock

Visible `PelipperTown.PokemonNpc` owns species identity, sprite/render, display name and Shiny evidence.

The hidden `Monster` proxy is combat/controller authority. Its technical `Health/MaxHealth = 1,000,000` is a sentinel, NOT real Pokemon HP.

Real Pokemon combat HP is:

- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

Never regress to using the sentinel as Pokemon HP.

## Carry-forward locks

- Shiny behavior remains frozen as accepted unless a concrete regression appears.
- Confirmed Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Encounter discovery remains 20Hz and pair cache stays enabled.
- High pairing `cacheHits` are expected reuse, not map scans.
- Pelipper retains controller/render/ownership authority for real Pelipper actors.
- Performance remains a subjective live gate.
- Lower Workings remains unchanged and gates 6.7.45 unless explicitly waived.
- Runtime roster currently reports 111 profile rows; do not publish-lock the older manual ~110 count until a roster audit reconciles it.

## Planned 6.7.45

**Containment Chamber Escalation Encounter** in the real Lower Workings. Keep George ordinary/anonymous until 6.7.46, preserve host authority and secure retreat, do not introduce exact `SECTOR 17`, and do not create a final boss yet.
