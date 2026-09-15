# Team Up new-chat handoff - 2026-09-15

This is the session transfer checkpoint for **Team Up v0.2.0-alpha.6.7.44.17**.

## Read order in the next chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md`
5. this file for the exact 2026-09-15 session state

## Authoritative checkpoint

- Repository: `ronvotri/Team-Up`
- Branch: `v0.2-alpha6-7-44-17-lightweight-minions`
- Version: `0.2.0-alpha.6.7.44.17`
- CI-verified code/artifact source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- CI run: `34904471245`
- CI job: `104177799252`
- Artifact ID: `10371887676`
- Artifact name: `team-up-alpha6-7-44-17-lightweight-minions`
- Artifact wrapper SHA256: `ca2c4e2426c2c195c1f201030164d064244f6b639e87a4c349b05e766cbfa8f9`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- Inner ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- Build: PASS, 0 warnings, 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

The branch HEAD may be newer than the CI source SHA because this handoff is a docs-only synchronization after the verified build. Do not confuse the latest docs commit with the source SHA that produced the downloadable ZIP.

## CI audits that passed

- PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD
- PELIPPER VISIBLE MUTATION X2 CAP + RESTORE
- MUTATION 2-4 MINION WIDE-SPAWN CARRY-FORWARD
- MUTANT LEADER + NORMAL HOSTILE MINION POLICY
- PELIPPER LIGHTWEIGHT ONE-ACTOR MINION POLICY
- GLOBAL LEADER-ONLY MUTANT LOOT-X3
- C# BUILD
- ZIP CONTENT AUDIT

## Current Mutation design lock

A Mutation encounter is:

- exactly **1 Mutant leader**;
- plus **2-4 ordinary hostile minions**.

Mutant leader rules:

- HP x3;
- stat multiplier x2;
- Pelipper visible source is capped at x2 size;
- Mutation aura;
- global x3 native loot on final defeat;
- x3 loot applies to vanilla, compatible custom monsters and Pelipper Mutant leaders.

Minion rules:

- ordinary hostile combat units;
- no Mutation HP/stat/scale/aura bonus;
- no x3 loot bonus;
- `MutationExcluded`, so a minion cannot recursively mutate;
- valid Team Up combat targets;
- should attack player/party through normal hostile AI.

## Performance-first Pelipper follower decision

The user explicitly decided that capture semantics are not important for temporary Mutation followers. The priority is whichever implementation causes less lag.

Canonical choice for 6.7.44.17:

**Do not ask Pelipper Town to create 2-4 complete native wild Pokemon encounters for the Mutation wave.**

A native Pelipper wild encounter carries at least a visible `PelipperTown.PokemonNpc`, a hidden combat proxy, encounter identity, HP modData, source/proxy pairing and Pelipper runtime bookkeeping. Multiplying that by 2-4 temporary followers is unnecessary overhead.

Instead, `MonsterMutationMinionFactory` uses a **lightweight Team Up actor** for Pelipper followers. The current implementation is one temporary ordinary hostile `GreenSlime` combat actor per follower, normalized to the leader's base combat stats and tagged with the Pelipper leader species for diagnostics.

This lightweight follower does NOT create:

- a new Pelipper `PokemonNpc`;
- a new Pelipper `WildEncounterId`;
- a Pelipper source/proxy pair;
- Pelipper real-HP modData lifecycle;
- Pelipper capture/runtime bookkeeping.

Telemetry:

- `pelipperLightweight=...`
- `nativePelipperSpawnsAvoided=...`

Tradeoff: a lightweight Pelipper follower is not a native Pokemon actor and may visually look like the fallback monster. If live testing says the visual is unacceptable, the preferred next direction is to skin the lightweight actor with the leader Pokemon visual without spawning a full Pelipper encounter. Do not switch to full native Pelipper follower encounters unless the user explicitly prioritizes visuals over performance.

The abandoned branch `v0.2-alpha6-7-44-17-mutant-capture-lock` is NOT canonical. Do not resume it by mistake.

## Capture semantics

Captureability is NOT a 6.7.44.17 gate anymore.

- Natural Pelipper wild Pokemon keep Pelipper's normal capture behavior.
- Temporary Team Up Mutation followers do not need to be catchable or explicitly blocked from capture.
- Mutant leader capture behavior is also not required to close this checkpoint unless the user later chooses to revisit it.

## Live truth already proven

### Pelipper Mutation and real HP binding

6.7.44.12 live Seadra test proved:

- forced Pelipper Mutation works;
- source transform is no longer blocked;
- real Pokemon HP resolves from Pelipper modData;
- `WildCurrentHealth` / `WildMaxHealth` are the real Pokemon HP path;
- natural non-force Mutation rolls are reached;
- source/proxy species pairing cache works.

Important telemetry from that live test included:

- `rolls=2`
- `forceTransforms=1`
- `transformBlocked=0`
- `hpResolved=16`
- `hpUnresolved=0`
- modData binding `resolved=19`, `writes=1`, `invalid=0`, `fallbacks=0`
- source `Seadra hp=160/160 logicalHP=480 bars=3 extraLives=2`

`rolls=2` was natural-roll evidence, not the force command. In `MonsterMutationService.TryMutate`, `_rolls++` only occurs under `!force`, while `ForceNearestEligible()` calls `TryMutate(..., force: true)`.

### Visible Pelipper scaling

6.7.44.13 live Fidough test proved Team Up now scales the visible `PokemonNpc` source rather than only the hidden proxy:

`last=scaled source=Fidough member=_visualScaleMultiplier 1->3 force=True`

The user judged x3 too large. Canonical visible Pelipper Mutation scale is now **x2**.

### What old live tests did NOT prove

Earlier waves still showed `spawned=0/N` with `safeRejected=N`. 6.7.44.15 widened spawn placement, 6.7.44.16 locked follower semantics, and 6.7.44.17 changed Pelipper followers to lightweight actors. Those three later behaviors still require live confirmation.

## Next live gates, in order

### 1. Pelipper 6.7.44.17 lightweight wave

Install the verified 6.7.44.17 ZIP cleanly, then use a normal non-Shiny Pelipper wild Pokemon:

```text
teamup_mutation force
```

Wait about one second, then:

```text
teamup_mutation status
```

Confirm:

- leader visible size is about x2;
- 2-4 followers actually appear;
- followers are ordinary, not Mutants;
- followers attack player/party;
- wave appearance causes no noticeable hitch/lag;
- `pelipperLightweight > 0`;
- `nativePelipperSpawnsAvoided > 0`;
- `minionsNormalized > 0` and `hostileReady > 0` when followers spawned.

If followers are still `0/N`, inspect the current `TryFindSafeSpawnPosition` / safe placement path before changing the factory architecture.

### 2. Pelipper HP x3 phase behavior

Attack the active Mutant through all three health bars.

Expected after first depleted bar:

- `phaseGuards=1`;
- Pelipper modData HP `writes` increases;
- source real HP restores for the next bar.

Expected after second depleted bar:

- `phaseGuards=2`.

Expected after third depleted bar:

- real final death;
- `finalLethalPasses` increments.

Do not call the 3-phase Pelipper HP loop fully live-passed until this is seen.

### 3. Global Mutant loot x3

On the final leader death, verify the reward status line:

`Mutant reward: lootX3 | minions=2-4 | scope=all-mutants | ...`

A successful x3 reward should produce:

- `dropCalls >= 1`;
- `extraDropPasses` increases by 2 for one final reward call;
- `errors=0`.

Implementation is three independent native drop passes, not cloning one item stack.

### 4. Non-Pelipper Mutation

Force/test one vanilla or compatible custom monster. Confirm same-runtime-type follower creation still works when safe, followers remain ordinary hostile, and only the leader receives x3 loot.

### 5. Lower Workings runtime gate

Lower Workings still requires live validation before 6.7.45 unless the user explicitly waives the gate.

### 6. Only then prepare 6.7.45

Planned 6.7.45 remains **Containment Chamber Escalation Encounter**. Do not start it early unless the user explicitly waives remaining runtime gates.

## Pelipper architecture lock

Visible actor: `PelipperTown.PokemonNpc`

- species identity;
- sprite/render;
- display name;
- Shiny evidence.

Hidden combat proxy: `StardewValley.Monsters.Monster` / Green Slime style proxy

- controller/combat authority;
- technical `Health/MaxHealth = 1,000,000` sentinel;
- real Pokemon HP is stored in:
  - `Griff.PelipperTown/WildCurrentHealth`
  - `Griff.PelipperTown/WildMaxHealth`

**Never treat the 1,000,000 proxy sentinel as Pokemon combat HP.**

Pelipper retains controller/render/ownership authority for real Pelipper actors.

## Shiny and gift locks

- Current Shiny behavior is accepted and frozen unless a concrete regression appears.
- Confirmed Shiny remains Mutation-excluded.
- Active Team Up NPCs in `Following` or `Waiting` cannot receive held-item vanilla gifts.
- Do not casually rewrite Shiny behavior while fixing Mutation.

## Pair cache / performance lock

Pelipper species pairing cache is live-proven. High `cacheHits` means successful pair reuse, not full map scans. Preserve the 20Hz encounter discovery and pairing cache safeguards.

Performance remains a subjective live gate. Telemetry can show cache health, but the user should still confirm combat feels smooth.

## Lower Workings carry-forward

Location: `Ronvotri.TeamUp_LowerWorkings`

- registered in `Data/Locations`;
- vanilla `StardewValley.GameLocation`;
- map `assets/LowerWorkings.tmx`;
- 32x24;
- no static Warp;
- exact persisted breach return;
- AmbientLight `45 50 60`;
- excluded from NPC pathfinding;
- not `AlwaysActive`.

Interior survey stages remain 0-6. Useful commands:

```text
teamup_lower_interior status
teamup_lower_descent status
teamup_story_reactions status
teamup_entry_protocol status
teamup_surge_high status
teamup_roster_story status
```

## Story/lore locks

- George before 6.7.46 remains observed Rank D, Non-Combatant and unrecruitable.
- No Rank S George, no `The Last Blaster`, no explicit historical-miner reveal before the planned point.
- Evelyn stays ordinary low Rank D healer/support in the main story; secret material is postgame only.
- Do not introduce exact `SECTOR 17` unless explicitly designed later.
- Story NPC slots remain 4/4.
- Formation hard cap remains 5 PEOPLE including Farmers.
- Entry Protocol READY + SURGE HIGH remain prerequisites.
- Multiplayer formation remains adaptive.
- No legacy fake-hide writer.
- No final boss yet.

## Planned 6.7.45

Containment Chamber Escalation Encounter:

- real Lower Workings;
- first major chamber escalation;
- stronger causal evidence that current Mutations relate to the old containment event;
- secure retreat;
- host authority;
- immediate reactions;
- Pelipper/Mutation safety;
- George remains anonymous/ordinary until 6.7.46;
- no final boss.

## Roster caution

Runtime startup currently reports 111 profile rows while the older manual breakdown was about 110. Do not publish-lock the roster at 110 until a dedicated roster audit reconciles the extra row.

## Key code files for the next chat

- `src/TeamUp/Combat/MonsterMutationService.cs`
- `src/TeamUp/Combat/MonsterMutationMinionFactory.cs`
- `src/TeamUp/Core/Alpha674410PelipperSpeciesPairingService.cs`
- `src/TeamUp/Core/Alpha674412PelipperModDataHpBindingService.cs`
- `src/TeamUp/Core/Alpha674413PelipperVisibleMutationService.cs`
- `src/TeamUp/Core/Alpha674413MutationMinionSpawnService.cs`
- `src/TeamUp/Core/Alpha674414PelipperMutantRewardService.cs`
- `src/TeamUp/Core/Alpha674416MutationLeaderMinionPolicyService.cs`
- `src/TeamUp/Core/Alpha67448PelipperSourceMutationService.cs`
- `src/TeamUp/ModEntry.Alpha6719.cs`
- `src/TeamUp/ModEntry.Alpha67446.cs`
- `tools/build_alpha6744_11.py` (historical filename, currently retargeted for the 6.7.44.17 gate)

## Resume sentence for a fresh chat

Use:

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-17-lightweight-minions. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md. Current verified artifact source SHA là c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e, run 34904471245. Ưu tiên live-test lightweight Pelipper Mutation minions, 3 HP phases và x3 leader loot. Không bắt đầu 6.7.45 cho tới khi các runtime gate hiện tại và Lower Workings pass, trừ khi tôi chủ động waive.`
