# Team Up handoff: 0.2.0-alpha.6.7.44.18

Branch: `v0.2-alpha6-7-44-18-native-minions`

This checkpoint supersedes the 6.7.44.17 lightweight-Slime follower design.

## CI verified checkpoint

- Version: `0.2.0-alpha.6.7.44.18`
- CI source SHA: `8c73eb93a3a7529e3d8232773e7c61c73ca9567d`
- Run: `34914882066`
- Job: `104210252539`
- Artifact ID: `10375867225`
- Artifact: `team-up-alpha6-7-44-18-native-source-minions`
- Artifact wrapper SHA256: `9aaf524f071610f64c5e93dc05dc9ad4225f586e21ca832ef41b4059e7afeebd`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.18_NATIVE_SOURCE_MINIONS_TEST.zip`
- Inner ZIP SHA256: `81a0a16ec0890f2434fa32961dcf08242933377197348918a17f340eefa63957`
- Build: PASS, 0 warnings, 0 errors
- ZIP audit: PASS
- `main`: NOT merged
- 6.7.45: NOT started

Docs-only commits after this build may make branch HEAD newer. Keep `8c73eb9...` as the source SHA that produced the verified artifact.

## User-authoritative follower design

One Mutation encounter remains exactly:

- 1 Mutant leader;
- 2-4 ordinary hostile followers.

The follower rule changed after live testing 6.7.44.17. The user rejected unrelated Slime followers even though they were lightweight and functional.

Current lock:

- followers must correspond to the creature that existed before Mutation;
- Pelipper leader -> 2-4 genuine native wild Pokemon of the same species;
- vanilla leader -> same vanilla monster type when safe;
- custom-mod leader -> same runtime/source type when Team Up can instantiate it safely;
- followers attack normally;
- followers are `MutationExcluded`, never recursively mutate;
- followers receive no Mutation stat/HP/scale/aura bonus;
- followers receive no x3 Mutant leader reward;
- no unrelated GreenSlime fallback is allowed;
- unsupported custom sources fail closed and report telemetry instead of silently creating the wrong creature.

For source mods with their own capture system, native capture semantics require a native provider/API. Pelipper has such a provider in this checkpoint. Generic custom monsters can only inherit capture semantics when their own normal runtime type already provides them.

## Pelipper native follower provider

`Alpha674418NativeMutationMinionService` owns the 6.7.44.18 wave.

For Pelipper:

1. Resolve the Mutant leader's real species through `PelipperWildEncounterIdentityService`.
2. Reuse Team Up's safe-position resolver, including the radius 2-8 relaxed placement patch.
3. Resolve Pelipper Town's registered `pokemon_spawn` command callback through SMAPI's internal command manager.
4. Invoke Pelipper's own native spawn pipeline for the same species, optionally carrying the source level when it can be read safely.
5. Resolve the newly-created `PokemonNpc + hidden combat proxy` pair through Pelipper encounter identity.
6. Move source and proxy to the selected follower tile.
7. Tag the hidden combat proxy as an ordinary Team Up Mutation minion and `MutationExcluded`.
8. Preserve Pelipper ownership of render, source/proxy lifecycle, encounter identity and Poké Ball capture.

This means a Nidoran♂ Mutant should call ordinary wild Nidoran♂ followers, not Slimes and not skinned fake actors.

If `pokemon_spawn` is unavailable, the provider fails closed, increments `sourceFailures`, logs the failure and creates no Slime replacement.

## Vanilla/custom provider

Non-Pelipper leaders use the existing safe runtime-type constructor path, but 6.7.44.18 only accepts `mode=\"same-runtime-type\"`.

If the factory would fall back to GreenSlime, that temporary object is discarded before it is added to the map.

This is intentionally conservative for future custom mods. A dedicated native adapter/API may be added for a mod whose monster type cannot be safely reconstructed from its runtime type.

## Telemetry

`teamup_mutation status` now includes:

```text
Mutation native minions: source-equivalent-only | requested=... | waves=... | pelipperNative=... | pelipperCommands=... | sameRuntime=... | sourceFailures=... | safeRejected=... | pending=... | deferredResolved=... | deferredExpired=... | last=...
```

Native Pelipper resolution also logs:

```text
[MutationNativePelipper] species=... encounter=... tile=... capture=native
```

Wave summary:

```text
[MutationNativeMinions] source=... leader=... requested=... spawnedNow=... pending=... failed=... safeRejected=... provider=pelipper-native|same-runtime-type
```

## Live evidence inherited from 6.7.44.17

The user's Nidoran♂ test on BusStop proved:

```text
[MutationTelemetry] source=StardewValley.Monsters.Monster location=BusStop baseHP=66 mutantHP=198 baseDamage=1 damageX=2 baseResilience=0 speed=1->2 scaleX=2 scaleApplied=True minionsRequested=3 force=True storyDirective=NormalRoll
Đã cưỡng chế đột biến: Nidoran♂.
[MutationMinions] source=StardewValley.Monsters.Monster location=BusStop spawned=3/3 sameType=0 fallback=3 safeRejected=0
```

The user confirmed those three followers attacked normally.

Therefore already live-proven before 6.7.44.18:

- forced Pelipper Mutation works;
- visible x2 scale works;
- requested 2-4 wave scheduling works;
- wide safe placement can spawn all requested followers with `safeRejected=0`;
- ordinary follower hostility works;
- Pelipper real HP binding and pair cache were already proven by earlier Seadra/Fidough tests.

The only rejected part was follower identity: the old lightweight actors appeared as Slimes.

## 6.7.44.18 runtime gates

### Gate 1: same-species native Pelipper followers

On a normal non-Shiny Pelipper wild Pokemon:

```text
teamup_mutation force
```

Wait about one second, then:

```text
teamup_mutation status
```

Expected:

- Mutant leader remains visibly about x2;
- 2-4 followers are the same Pokemon species as the pre-Mutation leader;
- no Slime followers;
- followers attack player/party normally;
- `pelipperCommands > 0`;
- `pelipperNative > 0`;
- `sourceFailures = 0`;
- `safeRejected = 0` in an ordinary open location;
- `pending` returns to 0 after native pairing resolves;
- no unacceptable hitch/lag.

If the command callback is unavailable, inspect Pelipper Town's `Pokémon spawn commands` setting or replace the adapter with a direct native API/internal spawn entry point. Do not restore Slime fallback.

### Gate 2: native capture

Throw a normal Poké Ball at one Mutation follower. Expected: Pelipper handles it as an ordinary wild encounter and capture works according to Pelipper's own rules.

This is NOT considered live-passed merely because the code uses Pelipper's native spawn pipeline.

### Gate 3: three HP phases

Deplete the Mutant leader through all three bars:

- first depletion: `phaseGuards=1` and real HP modData write increases;
- second depletion: `phaseGuards=2`;
- third depletion: real final death and `finalLethalPasses` increases.

Real Pelipper HP remains:

- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

Never use the hidden proxy's technical `1,000,000` sentinel as Pokemon HP.

### Gate 4: global x3 leader reward

On final leader death:

- `dropCalls >= 1`;
- `extraDropPasses` increases by 2;
- `errors=0`;
- only leader receives x3 reward semantics.

### Gate 5: vanilla / non-Pelipper regression

Force one vanilla monster. Expected:

- 2-4 same runtime-type followers when the type exposes a safe constructor;
- ordinary hostile behavior;
- followers never Mutation-transform;
- no unrelated fallback monster;
- only leader gets x3 loot.

### Gate 6: custom-mod regression

Test at least one compatible custom monster. If same-runtime construction is supported, followers should match. If unsupported, `sourceFailures` should increase and Team Up should fail closed rather than spawn the wrong creature.

### Gate 7: Lower Workings

Lower Workings still gates 6.7.45 unless the user explicitly waives it.

## Frozen carry-forward locks

- Confirmed Shiny remains Mutation-excluded.
- Current Shiny behavior stays frozen unless a concrete regression appears.
- Active Following/Waiting Team Up NPCs cannot receive held-item vanilla gifts.
- Preserve 20Hz Pelipper discovery and species/source pairing cache.
- High cache hits mean reuse, not repeated full scans.
- Pelipper retains controller/render/ownership/capture authority for genuine Pelipper actors.
- Mutation follower rewards remain ordinary; global x3 is leader-only.
- Performance remains a subjective live gate.

## Lower Workings / story locks

Lower Workings remains `Ronvotri.TeamUp_LowerWorkings`, map `assets/LowerWorkings.tmx`, 32x24, no static Warp, persisted breach return, survey stages 0-6.

Do not start 6.7.45 until the current Mutation gates and Lower Workings are live-passed unless the user explicitly waives them.

Planned 6.7.45 remains **Containment Chamber Escalation Encounter**. George stays ordinary/anonymous until 6.7.46. No exact `SECTOR 17`. No final boss.

## Key files

- `src/TeamUp/Core/Alpha674418NativeMutationMinionService.cs`
- `src/TeamUp/Core/Alpha674416MutationLeaderMinionPolicyService.cs`
- `src/TeamUp/Core/Alpha674413MutationMinionSpawnService.cs`
- `src/TeamUp/Core/PelipperWildEncounterIdentityService.cs`
- `src/TeamUp/Combat/MonsterMutationService.cs`
- `src/TeamUp/Combat/MonsterMutationMinionFactory.cs`
- `src/TeamUp/ModEntry.Alpha6719.cs`
- `src/TeamUp/ModEntry.Alpha67446.cs`
- `tools/build_alpha6744_11.py`

## Next-chat resume sentence

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-18-native-minions. Current verified code SHA là 8c73eb93a3a7529e3d8232773e7c61c73ca9567d, run 34914882066. 6.7.44.18 supersedes lightweight Slime minions: Mutation followers must match the original creature; Pelipper uses genuine native same-species wild encounters and should remain catchable. Ưu tiên live-test same-species spawn, native capture, three HP phases, global x3 leader loot, vanilla/custom regression và Lower Workings. Không bắt đầu 6.7.45 trừ khi các gate pass hoặc tôi chủ động waive.`
