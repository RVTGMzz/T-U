# Team Up handoff: 0.2.0-alpha.6.7.44.17

Start here: `CONTINUE_HERE.md`

Canonical latest: `docs/LATEST_HANDOFF.md`

Detailed technical handoff: `docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md`

Session transfer: `handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_7_44_17_2026-09-15.md`

## Checkpoint

- Branch: `v0.2-alpha6-7-44-17-lightweight-minions`
- Version: `0.2.0-alpha.6.7.44.17`
- CI-verified source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- Run: `34904471245`
- Job: `104177799252`
- Artifact ID: `10371887676`
- Artifact: `team-up-alpha6-7-44-17-lightweight-minions`
- Artifact wrapper SHA256: `ca2c4e2426c2c195c1f201030164d064244f6b639e87a4c349b05e766cbfa8f9`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

The branch HEAD is allowed to be newer than the CI source SHA because handoff docs were synchronized after the verified build. The artifact above is tied to `c1df68...`.

## CI gates passed

- Pelipper modData HP + pair-cache carry-forward
- Pelipper visible Mutation x2 cap + restore
- Mutation 2-4 minion wide-spawn carry-forward
- Mutant leader + normal hostile minion policy
- Pelipper lightweight one-actor minion policy
- Global leader-only Mutant loot x3
- C# build
- ZIP content audit

## Mutation design lock

One encounter is **1 Mutant leader + 2-4 ordinary hostile minions**.

Leader keeps HPx3, stat x2, Mutation aura, global native loot x3 and, for Pelipper, visible source scale capped at x2.

Followers stay ordinary hostile units: no Mutation bonuses, no aura, no x3 reward, and `MutationExcluded` prevents recursive Mutation.

For non-Pelipper leaders, the factory still prefers same-runtime-type followers when safe.

For Pelipper leaders, followers use Team Up's **lightweight temporary one-actor path** rather than asking Pelipper Town to create complete native wild encounters. This is the user's explicit performance-first choice. Current lightweight Pelipper followers are temporary Team Up combat actors and do not create extra `PokemonNpc`, `WildEncounterId`, source/proxy pairing, real-HP modData lifecycle or Pelipper capture bookkeeping.

Captureability of temporary Mutation followers is NOT a current gate. Natural Pelipper wild Pokemon keep native capture behavior.

The branch `v0.2-alpha6-7-44-17-mutant-capture-lock` was an abandoned intermediate direction and must not replace the canonical lightweight-minions branch.

## Live truth already proven

6.7.44.12 live Seadra testing proved:

- forced Pelipper Mutation works;
- real Pokemon HP resolves from `Griff.PelipperTown/WildCurrentHealth` / `WildMaxHealth`;
- source HP binding writes successfully;
- natural Mutation rolls reach the Mutation engine;
- source/proxy pairing cache works.

6.7.44.13 live Fidough testing proved visible `PokemonNpc` scaling works. The user found x3 too large, so visible Pelipper Mutation scale is now x2.

Never use the hidden proxy's technical `Health/MaxHealth = 1,000,000` sentinel as Pokemon HP.

## Still needs live proof

- 6.7.44.17 lightweight Pelipper followers spawn 2-4 successfully;
- followers attack normally;
- no noticeable wave-spawn hitch/lag;
- telemetry shows `pelipperLightweight>0` and `nativePelipperSpawnsAvoided>0`;
- first/second HP depletion produce `phaseGuards=1` then `phaseGuards=2`;
- final third depletion produces real death and increments `finalLethalPasses`;
- global x3 leader reward produces `dropCalls>=1`, `extraDropPasses+=2`, `errors=0`;
- one non-Pelipper Mutation regression test;
- Lower Workings runtime gate.

## Immediate next test

On a normal non-Shiny Pelipper wild Pokemon:

```text
teamup_mutation force
```

Wait about one second, then:

```text
teamup_mutation status
```

Confirm x2 leader size, 2-4 followers, ordinary follower state, hostility and smooth performance. If the wave is still `0/N`, inspect safe placement before replacing the lightweight architecture.

Then fight through all three leader HP phases and verify final x3 reward telemetry. After that test one non-Pelipper Mutant.

## Carry-forward locks

- Current Shiny behavior stays frozen unless a concrete regression appears.
- Confirmed Shiny remains Mutation-excluded.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Preserve 20Hz encounter discovery and Pelipper pair cache.
- High `cacheHits` means reuse, not full pairing scans.
- Pelipper retains controller/render/ownership authority for real Pelipper actors.
- Lightweight follower visuals may be improved later by skinning the Team Up actor, not by spawning full Pelipper encounters, unless the user explicitly chooses otherwise.
- Performance remains a subjective live gate.
- Lower Workings remains unchanged and still gates 6.7.45 unless explicitly waived.

## 6.7.45 lock

Planned 6.7.45 is **Containment Chamber Escalation Encounter** in the real Lower Workings. Do not start it until current runtime gates and Lower Workings pass unless the user explicitly waives the gate. George stays ordinary/anonymous until 6.7.46. No exact `SECTOR 17` and no final boss.
