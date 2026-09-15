# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.17**

Development branch:

`v0.2-alpha6-7-44-17-lightweight-minions`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md`
5. `handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_7_44_17_2026-09-15.md`

The dated handoff contains the fullest restart context, live evidence, remaining gates, Lower Workings locks and the exact resume prompt.

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.17`
- CI-verified source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- CI run: `34904471245`
- CI job: `104177799252`
- Artifact ID: `10371887676`
- Artifact: `team-up-alpha6-7-44-17-lightweight-minions`
- Artifact wrapper SHA256: `ca2c4e2426c2c195c1f201030164d064244f6b639e87a4c349b05e766cbfa8f9`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- Inner ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- Build: PASS, 0 warnings, 0 errors

The branch HEAD can be newer than the CI source SHA because handoff documents were synchronized after the build. The ZIP above was produced from `c1df68...`.

## Current Mutation design

One Mutation encounter is **1 Mutant leader + 2-4 ordinary hostile minions**.

Leader:

- HP x3;
- stat x2;
- Pelipper visible scale capped at x2;
- Mutation aura;
- x3 native loot on final defeat;
- x3 loot applies to all Mutant leaders, including vanilla/custom/Pelipper.

Minions:

- ordinary hostile;
- no Mutation bonus;
- no aura;
- no x3 loot;
- `MutationExcluded` so no recursive Mutation;
- valid Team Up combat targets.

## Pelipper follower performance lock

For a Pelipper Mutant leader, **do not spawn 2-4 full native Pelipper wild encounters**.

6.7.44.17 chooses the lightweight Team Up path because performance is more important than whether temporary followers are catchable. Each Pelipper follower is currently one lightweight temporary combat actor instead of a full `PokemonNpc + hidden proxy + WildEncounterId + HP modData + pairing/cache` bundle.

Status telemetry:

- `pelipperLightweight=...`
- `nativePelipperSpawnsAvoided=...`

Captureability of Mutation followers is NOT a current gate. Natural Pelipper wild Pokemon keep their native capture behavior.

The temporary branch `v0.2-alpha6-7-44-17-mutant-capture-lock` is superseded. Do not use it as the continuation branch.

## Live-proven truth

Pelipper Mutation core is live-proven. The real Pokemon HP path is also live-proven:

- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

Do not use the hidden Green Slime proxy's technical `Health/MaxHealth = 1,000,000` as Pokemon HP.

A 6.7.44.12 Seadra test proved forced Mutation, source HP binding, pair-cache reuse and natural Mutation rolls. A 6.7.44.13 Fidough test proved visible source scaling. x3 was visually too large, so the current visible Pelipper scale is x2.

## Not live-proven yet

6.7.44.17 itself still needs runtime confirmation for:

- 2-4 lightweight Pelipper followers actually spawning;
- followers attacking normally;
- no noticeable spawn hitch/lag;
- `pelipperLightweight > 0` and `nativePelipperSpawnsAvoided > 0`;
- full 3-bar Pelipper HP behavior;
- global x3 leader loot on final death;
- one non-Pelipper Mutation regression test.

Lower Workings also still has a runtime gate before 6.7.45 unless explicitly waived by the user.

## Next test sequence

Use a normal non-Shiny Pelipper wild Pokemon:

```text
teamup_mutation force
```

Wait roughly one second, then:

```text
teamup_mutation status
```

Check leader x2 size, 2-4 followers, hostility, smooth performance and the lightweight counters. If followers remain `0/N`, inspect safe spawn placement before changing the lightweight factory architecture.

Then deplete the Mutant through all three HP bars. Expected phase telemetry is `phaseGuards=1`, then `phaseGuards=2`, then a true final death with `finalLethalPasses` increasing.

On final death verify:

`Mutant reward: lootX3 | minions=2-4 | scope=all-mutants | ...`

A successful reward should have `dropCalls>=1`, two additional `extraDropPasses` for that reward, and `errors=0`.

After that, force/test one non-Pelipper Mutant.

## Frozen safety locks

- Confirmed Shiny remains Mutation-excluded.
- Current Shiny behavior stays frozen unless a concrete regression appears.
- Active Following/Waiting Team Up NPCs cannot receive held-item vanilla gifts.
- Preserve 20Hz Pelipper encounter discovery and species-pair cache.
- High `cacheHits` means pair reuse, not repeated full scans.
- Pelipper retains controller/render/ownership authority for real Pelipper actors.
- Performance is still a subjective live gate even when telemetry is healthy.

## Lower Workings and story gate

Lower Workings remains `Ronvotri.TeamUp_LowerWorkings`, using `assets/LowerWorkings.tmx`, 32x24, no static Warp, exact persisted breach return and stages 0-6.

Do not start Alpha 6.7.45 until current Mutation runtime gates and Lower Workings are live-passed unless the user explicitly waives them.

Planned 6.7.45 is **Containment Chamber Escalation Encounter**. George remains ordinary/anonymous until 6.7.46, no exact `SECTOR 17`, and no final boss.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-17-lightweight-minions. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md, docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md và handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_7_44_17_2026-09-15.md. Current verified artifact source SHA là c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e, run 34904471245. Ưu tiên live-test 6.7.44.17 lightweight Pelipper Mutation minions, 3 HP phases và global x3 leader loot. Không bắt đầu 6.7.45 cho tới khi các runtime gate hiện tại và Lower Workings pass, trừ khi tôi chủ động waive.`
