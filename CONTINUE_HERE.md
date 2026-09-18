# Continue Team Up Here

Repository: **`ronvotri/T-U`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.35**

Development branch:

`v0.2-alpha6-7-44-35-leader-pursuit-reach`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_35_LEADER_PURSUIT_REACH_HANDOFF.md`

## Verified build checkpoint

- Repository: `ronvotri/T-U`
- Version: `0.2.0-alpha.6.7.44.35`
- Branch: `v0.2-alpha6-7-44-35-leader-pursuit-reach`
- CI source SHA: `0397307ce548e35256e6ee41d0fe2252578107ee`
- CI run: `35357490625`
- CI job: `105640304295`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.35_COMBAT_PRESENCE_FIX_TEST.zip`
- ZIP SHA256: `3a93d1de1890d19d23b5e53fb745b2db6e4e2edcf9a175e8ea8751a05c07202a`
- Build: PASS, 0 warnings, 0 errors
- Package audit: PASS

## Current live truth

- 6.7.44.19 previously live-proved genuine same-species Pelipper followers with Spawn Commands OFF.
- 6.7.44.23 previously live-proved Pelipper leader/follower steering and contact combat, but leader movement was still visually imperfect.
- 6.7.44.24 through 6.7.44.30 introduced a load/crash regression and are excluded from the current runtime.
- 6.7.44.31 restored the known-loadable 6.7.44.23 runtime shape, but Mutation force was not functionally reliable for all Pelipper species.
- 6.7.44.33 restored Pelipper source/proxy pairing using `PokemonNpcEncounter/v1` and preserves Nidoran♂/Nidoran♀ identity. Ron live-confirmed Mutation spawning again.
- Live feedback on 6.7.44.33: Mutant leader visibly flickered, leader/followers only aggroed at short distance, and damage felt too weak.

## 6.7.44.34 delta

6.7.44.34 is a focused combat-presence pass on top of the loadable 6.7.44.33 path:

- keeps the 6.7.44.33 Pelipper source-ID pairing fix;
- excludes all 6.7.44.24-30 crash-stack services;
- reasserts Pelipper Mutant visible scale immediately before world render to reduce x1/x2 flicker;
- Mutation aggro arena is 18 tiles, x3 the 6-tile baseline;
- Pelipper Mutation actors set `WildCombatEngaged=true` and `PassiveUntilAttacked=false` inside the aggro arena;
- ordinary Pelipper Mutation followers use a minimum raw contact-damage floor of 4;
- Mutant leader uses a minimum raw contact-damage floor of 8 and preserves higher intended x2 Mutation damage;
- no Lower Workings runtime validator is active in this build.

## Immediate runtime test

Install only 6.7.44.34 and run:

```text
teamup_mutation force
```

Verify only these three things first:

1. Mutant leader no longer visibly flickers between normal and x2 presentation.
2. Leader and followers start pursuing from a materially larger distance, approximately the new 18-tile arena.
3. Leader/follower contact damage feels materially stronger than 6.7.44.33.

Do not call Runtime PASS until Ron confirms these live.

## Frozen Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**.

Leader:
- HP x3;
- stat x2;
- visible Pelipper scale capped at x2;
- Mutation aura;
- final native loot x3;
- must not be catchable in the final intended design.

Followers:
- same/source-equivalent creature;
- ordinary hostile;
- no Mutation bonus/aura/x3 reward;
- Mutation-excluded;
- genuine native Pelipper encounters where supported;
- no unrelated GreenSlime visible fallback.

Real Pelipper HP remains:
- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

Never use the hidden technical proxy 1,000,000 HP as real Pokemon HP.

## Remaining gates before 6.7.45

- live-pass 6.7.44.34 flicker / aggro / damage;
- then repair leader movement/reach only on the current safe runtime shape;
- reintroduce capture-block, 3 HP phases and x3 loot one small change at a time;
- vanilla/non-Pelipper Mutation regression;
- Lower Workings runtime gate using a non-crashing implementation.

Do not start 6.7.45 unless Ron explicitly waives remaining gates.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trong repo ronvotri/T-U, branch v0.2-alpha6-7-44-35-leader-pursuit-reach. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_35_LEADER_PURSUIT_REACH_HANDOFF.md. Current CI source SHA 0397307ce548e35256e6ee41d0fe2252578107ee, run 35357490625. 6.7.44.33 đã khôi phục Mutation Pelipper source/proxy pairing và Ron live-confirmed spawn lại; feedback mới là elite flicker, aggro quá gần và damage yếu. 6.7.44.34 sửa pre-render scale stabilization, x3 aggro arena 18 tiles, Pelipper engaged/passive flags và damage floors. Các service 6.7.44.24-30 gây crash load vẫn bị loại hoàn toàn. Chỉ gọi Runtime PASS khi Ron test lại và xác nhận.`


## 6.7.44.35 delta

- leader chase no longer calls `source.Halt()` every chase step;
- leader only halts once when entering the 128px hold band;
- elite melee reach is 160px;
- follower steering is unchanged;
- 6.7.44.34 flicker/aggro/damage fixes are carried forward;
- no 6.7.44.24-30 crash-stack service is reintroduced.

Immediate live gate: force one Pelipper Mutation and judge only leader chase smoothness + 160px reach.
