# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.20**

Development branch:

`v0.2-alpha6-7-44-20-mutation-aggro`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_20_MUTATION_AGGRO_HANDOFF.md`

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.20`
- CI source SHA: `712bc6a1bacb74fa0bd6273246f9f42b08927353`
- CI run: `34974189152`
- CI job: `104397637635`
- Artifact ID: `10398603342`
- Wrapper SHA256: `db583c3f1fec2560bc38d522c82b652c690b625638b09cee91d5f4cf4c17b544`
- Inner ZIP SHA256: `e18b146d28d539f0f6c87634ccb03dbe9305f151b1205d8742d2d4a74bb79efd`
- Build: PASS, 0 warnings, 0 errors

Docs-only commits after the CI SHA are expected.

## Live truth

6.7.44.19 live Rockruff test proved source-native follower spawning works with Pelipper Spawn Commands still OFF: leader HP 78 -> 234, x2 scale, 4 requested, four genuine Rockruff Lv.5 created, `spawnedNow=4`, `pending=0`, `failed=0`, `safeRejected=0`, capture identity native, no Slimes.

The remaining regression was that both the Mutant leader and its followers stopped attacking the player.

## 6.7.44.20 fix

`Alpha674420MutationAggroService` re-arms Stardew's native pursuit flags for Mutation leaders/minions. Pelipper proxy and visible source are armed together. Team Up does not teleport or replace provider controllers.

Immediate live gate: force one Pelipper Mutation, confirm same-species followers still spawn, then stand nearby and verify leader + followers actively pursue/attack. Run `teamup_mutation status` and expect `leaderArms>0`, `minionArms>0`, `pelipperProxyArms>0`, `pelipperSourceArms>0`, ideally `identityMisses=0`.

Then verify a follower remains catchable.

## Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**. No unrelated Slime fallback.

Leader keeps HP x3, stat x2, visible Pelipper x2 cap, aura and global native loot x3. Followers match the original creature, stay ordinary, receive no Mutation bonus/aura/x3 reward, and cannot recursively mutate.

## Remaining gates before 6.7.45

- Mutation hostility live-pass;
- follower native capture recheck after aggro;
- all three Pelipper HP phases;
- final global x3 leader loot;
- vanilla/non-Pelipper Mutation regression;
- compatible custom-source regression when practical;
- Lower Workings runtime gate.

Do not start 6.7.45 unless the user explicitly waives remaining gates.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-20-mutation-aggro. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_20_MUTATION_AGGRO_HANDOFF.md. Current verified code SHA là 712bc6a1bacb74fa0bd6273246f9f42b08927353, run 34974189152. 6.7.44.19 live-pass native same-species Rockruff followers 4/4 nhưng leader + đệ không tấn công Farmer. 6.7.44.20 re-arms native Stardew pursuit flags cho Mutation proxy/source. Ưu tiên live-test hostility + capture, sau đó 3 HP phases, x3 loot, vanilla/custom regression và Lower Workings. Không bắt đầu 6.7.45 trừ khi tôi chủ động waive.`
