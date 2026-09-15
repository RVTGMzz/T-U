# Team Up handoff: 0.2.0-alpha.6.7.44.17

Branch: `v0.2-alpha6-7-44-17-lightweight-minions`

This is the detailed technical checkpoint after the 2026-09-15 session handoff synchronization.

## CI verified checkpoint

- Verified source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- Run: `34904471245`
- Job: `104177799252`
- Artifact ID: `10371887676`
- Artifact name: `team-up-alpha6-7-44-17-lightweight-minions`
- Artifact wrapper SHA256: `ca2c4e2426c2c195c1f201030164d064244f6b639e87a4c349b05e766cbfa8f9`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- Inner ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- Build: PASS, 0 warnings / 0 errors
- ZIP content audit: PASS
- `main`: NOT merged
- 6.7.45: NOT started

The artifact is tied to `c1df68...`. Later branch commits may only synchronize handoff docs and therefore do not require a rebuild.

## Passed CI audits

- `PELIPPER MODDATA HP + PAIR CACHE CARRY-FORWARD: PASS`
- `PELIPPER VISIBLE MUTATION X2 CAP + RESTORE AUDIT: PASS`
- `MUTATION 2-4 MINION WIDE-SPAWN CARRY-FORWARD: PASS`
- `MUTANT LEADER + NORMAL HOSTILE MINION POLICY AUDIT: PASS`
- `PELIPPER LIGHTWEIGHT ONE-ACTOR MINION POLICY: PASS`
- `GLOBAL LEADER-ONLY MUTANT LOOT-X3 AUDIT: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT AUDIT: PASS`

## Design lock: one leader + ordinary followers

Mutation encounter structure:

- one Mutant leader;
- two to four ordinary hostile followers.

Leader receives:

- HP x3;
- stat x2;
- Mutation aura;
- Pelipper visible source x2 cap;
- global x3 native loot on final defeat.

Followers receive:

- ordinary hostile stats/AI;
- no Mutation HP/stat/scale/aura bonus;
- no x3 Mutant loot bonus;
- `MutationExcluded` to block recursive Mutation;
- normal Team Up combat-target opt-in.

`Alpha674416MutationLeaderMinionPolicyService` strips accidental Mutant/reward state from followers and records leader/follower telemetry.

## Global x3 Mutant reward

`Alpha674414PelipperMutantRewardService` is global despite its historical class name.

It applies to all Team Up Mutant leaders that reach the native hooked `monsterDrop` path, including vanilla, compatible custom monsters and Pelipper leaders.

Implementation:

- original native drop pass runs once;
- Team Up invokes the same native drop method two extra times;
- total = three independent native reward passes;
- Team Up does not fabricate or clone one item stack.

Status line:

```text
Mutant reward: lootX3 | minions=2-4 | scope=all-mutants | marked=... | dropHooks=... | dropCalls=... | extraDropPasses=... | errors=... | last=...
```

Followers never receive the Mutant reward marker.

## Performance-first Pelipper follower architecture

The user explicitly stated that whether temporary followers are catchable is not important. The preferred implementation is whichever causes less lag.

Therefore 6.7.44.17 does NOT ask Pelipper Town to spawn complete native wild Pokemon for the two to four followers.

A complete Pelipper wild encounter would add, per follower:

- visible `PelipperTown.PokemonNpc` source actor;
- hidden Monster combat proxy;
- encounter identity / `WildEncounterId`;
- `WildCurrentHealth` / `WildMaxHealth` state;
- source/proxy pairing work;
- Pelipper lifecycle and capture bookkeeping.

Instead, `MonsterMutationMinionFactory` detects a Pelipper wild leader and creates one lightweight Team Up combat actor per follower. Current implementation uses a normal `GreenSlime` combat actor normalized to leader base health/damage/speed and tags it with:

- `PelipperLightweightMinionMarker`;
- `PelipperLeaderSpeciesMarker`.

This path increments:

- `PelipperLightweightSpawned`;
- `PelipperNativeSpawnAvoided`.

`teamup_mutation status` exposes them through the leader/minion policy line as:

- `pelipperLightweight=...`;
- `nativePelipperSpawnsAvoided=...`.

No new native Pelipper `PokemonNpc`, `WildEncounterId`, HP modData lifecycle or source/proxy pairing is created for these temporary followers.

### Visual tradeoff

A lightweight Pelipper follower is not a native Pokemon actor and may visually appear as the Team Up fallback monster. If the user dislikes the visual after live testing, the preferred optimization path is to skin/override the lightweight actor's presentation while preserving the one-actor architecture.

Do NOT switch to spawning full Pelipper encounters unless the user explicitly decides the visual benefit is worth the runtime cost.

## Capture semantics are not a current gate

A temporary branch `v0.2-alpha6-7-44-17-mutant-capture-lock` was briefly explored, then superseded by the performance-first decision.

Canonical branch is `v0.2-alpha6-7-44-17-lightweight-minions`.

Current rule:

- natural Pelipper wild Pokemon keep normal Pelipper capture behavior;
- Mutation followers do not require capture support or an explicit capture block;
- Mutant leader capture behavior is not required to close 6.7.44.17 unless the user later reopens it.

## Live evidence: Seadra 6.7.44.12

Force test on `Custom_ShearwaterBridge`:

```text
[MutationTelemetry] source=StardewValley.Monsters.Monster location=Custom_ShearwaterBridge baseHP=160 mutantHP=480 baseDamage=1 damageX=2 baseResilience=0 speed=1->2 scaleX=3 scaleApplied=True minionsRequested=3 force=True storyDirective=NormalRoll
Đã cưỡng chế đột biến: Seadra.
[MutationMinions] source=StardewValley.Monsters.Monster location=Custom_ShearwaterBridge spawned=0/3 sameType=0 fallback=0 safeRejected=3
```

Important status evidence:

```text
Mutation: Enabled=True | Chance=5% | HPx3 | Statx2 | Scalex3 | Minions=2-4 | deathHooks=1 | rolls=2 | mutations=1 | excluded=0 | active=1
Pelipper species pairing: attempts=4 | resolved=4 | cacheHits=901 | cacheInvalidated=0 | ambiguous=0 | noMatch=0
Pelipper SOURCE mutation: sourceDamageCalls=16 | hpResolved=16 | hpUnresolved=0 | sourceLethalCandidates=2 | mutationAttempts=2 | mutationIntercepts=0 | duplicateSuppressed=2 | phaseGuards=0 | finalLethalPasses=0 | transformBlocked=0 | forceTransforms=1 | auraDraws=873 | damageHooks=29 | last=mutated source=Seadra hp=160/160 logicalHP=480 bars=3 extraLives=2 via=PelipperProxyModData.WildCurrentHealth/PelipperProxyModData.WildMaxHealth force=True
Pelipper modData HP binding: resolved=19 | writes=1 | invalid=0 | fallbacks=0 | last=write source=Seadra Griff.PelipperTown/WildCurrentHealth=160
```

This proves:

1. forced Pelipper Mutation succeeds live;
2. Pelipper real HP binding succeeds live;
3. `WildCurrentHealth` / `WildMaxHealth` are the correct Pokemon HP authority;
4. natural non-force Mutation rolls reach the engine;
5. species pairing/cache works live.

### Why `rolls=2` proves natural roll reachability

In `MonsterMutationService.TryMutate(Monster monster, bool force)`, `_rolls++` occurs only inside `if (!force)`.

`ForceNearestEligible()` calls `TryMutate(target, force: true)`.

Therefore the two recorded rolls came from natural lethal paths, not from the force command. At 5% chance, missing both is normal and does not indicate failure.

## Live evidence: Fidough 6.7.44.13

Force test on Farm:

```text
[MutationTelemetry] source=StardewValley.Monsters.Monster location=Farm baseHP=59 mutantHP=177 baseDamage=1 damageX=2 baseResilience=0 speed=1->2 scaleX=3 scaleApplied=True minionsRequested=4 force=True storyDirective=NormalRoll
Đã cưỡng chế đột biến: Fidough.
[MutationMinions] source=StardewValley.Monsters.Monster location=Farm spawned=0/4 sameType=0 fallback=0 safeRejected=4
```

Visible source telemetry:

```text
Pelipper visible Mutation: tracked=1 | applied=1 | reapplied=0 | restored=0 | failed=0 | last=scaled source=Fidough member=_visualScaleMultiplier 1->3 force=True
```

This proves visible `PokemonNpc` source scaling works, rather than only scaling the hidden proxy.

User feedback: x3 is too large. Current visible Pelipper Mutation cap is x2.

The same test proved the older minion placement was still rejecting all requested positions. 6.7.44.15 widened placement, 6.7.44.16 normalized follower semantics and 6.7.44.17 changed Pelipper followers to the lightweight one-actor factory. Those later changes still need live validation.

## Pelipper HP architecture lock

Correct architecture:

### Visible source

`PelipperTown.PokemonNpc`

Owns:

- species identity;
- sprite/render;
- display name;
- Shiny identity/evidence.

### Hidden combat proxy

`StardewValley.Monsters.Monster` / Green Slime style proxy

Owns combat/controller behavior. Its ordinary `Health/MaxHealth = 1,000,000` is a technical sentinel.

**Never use that 1,000,000 sentinel as Pokemon HP.**

Real Pokemon combat HP is stored in proxy modData:

- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

6.7.44.12 binds those values into the source-aware Mutation HP engine.

## Pair-cache/performance lock

Pair cache is live-proven. Example Seadra telemetry showed `attempts=4`, `resolved=4`, `cacheHits=901`, `cacheInvalidated=0`.

High `cacheHits` means the cached pair is being reused. It does NOT mean hundreds of full world scans.

Preserve:

- encounter discovery every 3 ticks / roughly 20Hz;
- weak source/proxy identity cache;
- species pairing cache;
- cached Shiny reflection evidence;
- Elite proxy guard.

Performance still requires subjective live confirmation. Healthy counters alone do not prove there is no hitch.

## Current runtime gates, exact order

### Gate 1: lightweight Pelipper follower wave

On a normal non-Shiny Pelipper wild Pokemon:

```text
teamup_mutation force
```

Wait roughly one second, then:

```text
teamup_mutation status
```

Expected:

- leader is visibly around x2;
- 2-4 followers appear;
- followers are normal, not Mutants;
- followers attack player/party;
- no noticeable hitch when the wave appears;
- `pelipperLightweight > 0`;
- `nativePelipperSpawnsAvoided > 0`;
- `minionsNormalized > 0`;
- `hostileReady > 0`.

If `spawned=0/N` still occurs, inspect/fix current safe-position selection before abandoning the lightweight architecture.

### Gate 2: HP x3 phases

Deplete first HP bar. Expected:

- `phaseGuards=1`;
- modData HP `writes` increases;
- source real HP is restored for the next phase.

Deplete second HP bar. Expected:

- `phaseGuards=2`.

Deplete third HP bar. Expected:

- final real death;
- `finalLethalPasses` increases.

Do not mark three-phase HP live-passed before seeing these transitions.

### Gate 3: global x3 leader loot

On final leader death, inspect:

```text
Mutant reward: lootX3 | minions=2-4 | scope=all-mutants | ...
```

Expected for one successful reward:

- `dropCalls >= 1`;
- `extraDropPasses` gains two;
- `errors=0`;
- `last=rewarded ... nativeDropPasses=3`.

### Gate 4: non-Pelipper regression

Force one vanilla or compatible custom Mutant and verify:

- 2-4 followers;
- same-runtime-type preference when safe;
- normal follower state/AI;
- only leader gets x3 loot.

### Gate 5: Lower Workings runtime

Lower Workings must still pass its runtime gate before 6.7.45 unless the user explicitly waives it.

### Gate 6: 6.7.45

Only after the above, prepare **Containment Chamber Escalation Encounter**, unless the user explicitly waives remaining gates.

## Shiny lock

Current Shiny behavior was accepted and should remain frozen unless a concrete regression appears.

Confirmed natural Shiny wins over Mutation and remains Mutation-excluded.

Do not casually alter Shiny logic while fixing Mutation/minions.

## Gift guard lock

Active Team Up NPCs in `Following` or `Waiting` cannot receive held-item vanilla gifts. Inactive roster members keep normal vanilla gifting.

Carry this forward unchanged.

## Lower Workings carry-forward

Location:

`Ronvotri.TeamUp_LowerWorkings`

Current architecture:

- `Data/Locations` registration;
- vanilla `StardewValley.GameLocation`;
- map `assets/LowerWorkings.tmx`;
- 32x24;
- vanilla `Mines/mine.png`;
- Back/Buildings/Front;
- no static Warp;
- exact persisted breach return;
- AmbientLight `45 50 60`;
- excluded from NPC pathfinding;
- not `AlwaysActive`.

Interior survey stages remain 0-6. Commands:

```text
teamup_lower_interior status|reset|stage 0-6
teamup_lower_descent status|reset|stage 0-5
teamup_story_reactions reset|status
teamup_entry_protocol status
teamup_surge_high status
teamup_roster_story status
```

## Story/lore locks

- George before 6.7.46: observed Rank D, Non-Combatant, unrecruitable.
- No Rank S George.
- No `The Last Blaster` reveal yet.
- No explicit historical-miner identity yet.
- Evelyn main story: ordinary low Rank D healer/support; secret material only postgame.
- Do not introduce exact `SECTOR 17` unless explicitly designed later.
- Story NPC slots: 4/4.
- Formation hard cap: 5 PEOPLE including Farmers.
- Entry Protocol READY + SURGE HIGH remain prerequisites.
- Multiplayer formation remains adaptive.
- No legacy fake-hide writer.
- No final boss yet.
- Pelipper source ownership/render/controller authority stays with Pelipper.

## Planned 6.7.45

**Containment Chamber Escalation Encounter**

- real Lower Workings;
- first major chamber escalation;
- stronger causal evidence that current Mutations relate to the old containment event;
- secure retreat;
- host authority;
- immediate reactions;
- Pelipper/Mutation safety;
- George remains ordinary/anonymous until 6.7.46;
- no final boss.

## Roster caution

Runtime startup reports 111 profile rows. Older manual breakdowns were around 110. Do not publish-lock 110 until a dedicated roster audit reconciles the extra row.

## Important code files

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
- `tools/build_alpha6744_11.py`

The build script filename is historical but the branch version of the script is retargeted to the current 6.7.44.17 build/audit/package gate.
