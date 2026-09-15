# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.23**

Development branch:

`v0.2-alpha6-7-44-23-mutant-leader-smoothing`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_23_MUTANT_LEADER_SMOOTHING_HANDOFF.md`

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.23`
- CI source SHA: `2c8f132527cf06aa2d17a82875d7b6f4f46750b4`
- CI run: `34996106050`
- CI job: `104472621463`
- Artifact ID: `10407443067`
- Wrapper SHA256: `f27e09e980708d06a91c1e74bbd7a5094b92a2637aa405916fd5df8b8a283080`
- Inner ZIP SHA256: `d95ed6f2a48447ec967032b38b08c2b30632547ecc3fc02351278981fec1efac`
- Build: PASS, 0 warnings, 0 errors

Docs-only commits after the CI source SHA are expected.

## Live truth

6.7.44.19 live-proved genuine same-species Pelipper followers with Spawn Commands OFF and no Slime fallback.

6.7.44.20 proved generic Monster pursuit flags are insufficient for Pelipper wild Pokemon.

6.7.44.21 proved Team Up-owned chase works but tile pathing was visually bad.

6.7.44.22 live test proved the followers now attack Farmer. The remaining failure is specifically the Mutant leader: it only attacks at very close range and visibly jitters / moves unlike a normal Pokemon.

## 6.7.44.23 fix

`Alpha674423PelipperMutantLeaderSmoothingService` supersedes the 6.7.44.22 runtime steering instance while preserving its successful follower pack behavior.

Leader-specific changes:

- no follower separation is applied to the leader;
- `source.Halt()` clears Pelipper passive movement/velocity before each leader chase step;
- short direction hysteresis reduces rapid cardinal axis flips;
- leader holds a stable melee band instead of trying to overlap Farmer;
- leader melee reach is extended to visually match the x2 Mutant scale;
- followers retain pack separation, leader clearance and blocked sidesteps;
- real source/proxy pair and native capture identity remain intact.

6.7.44.22 remains in source history but is NOT instantiated at runtime in 6.7.44.23.

## Immediate runtime test

Keep Pelipper Spawn Commands OFF and run:

```text
teamup_mutation force
```

Do not attack first. Verify:

- followers still attack normally;
- Mutant leader approaches smoothly without the previous jitter;
- leader stops near Farmer instead of trying to occupy the same pixels;
- leader can damage Farmer from noticeably farther away;
- native spawn/capture behavior remains intact.

Then:

```text
teamup_mutation status
```

Expected new steering line: `Pelipper Mutation steering: leader-smooth-reach`.

Inspect `leaderMoves`, `minionMoves`, `leaderReachHits`, `leaderRangeHolds`, `leaderHaltResets`, `leaderDirectionChanges`, `leaderDirectionLocks`, `proxySyncs`, `contactDamageCalls`, and ideally `identityMisses=0`.

## Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**. No unrelated Slime fallback.

Leader keeps HP x3, stat x2, visible Pelipper x2 cap, aura and global native loot x3. Followers match the original creature, remain ordinary and Mutation-excluded, receive no x3 leader reward, and remain genuine Pelipper wild encounters when Pelipper is the source.

## Remaining gates before 6.7.45

- 6.7.44.23 leader smoothing/reach live-pass;
- follower native capture recheck;
- all three Pelipper HP phases;
- final global x3 leader loot;
- vanilla/non-Pelipper Mutation regression;
- compatible custom-source regression when practical;
- Lower Workings runtime gate.

Do not start 6.7.45 unless the user explicitly waives remaining gates.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-23-mutant-leader-smoothing. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_23_MUTANT_LEADER_SMOOTHING_HANDOFF.md. Current verified code SHA là 2c8f132527cf06aa2d17a82875d7b6f4f46750b4, run 34996106050. 6.7.44.22 live-proved follower Pelipper Mutation đã tấn công được, nhưng Mutant leader chỉ đánh khi rất gần và movement bị giật. 6.7.44.23 giữ follower pack steering, bỏ separation trên leader, Halt() movement state trước chase, thêm direction hysteresis, stable melee band và extended x2 melee reach. Ưu tiên live-test leader smooth/reach + capture, sau đó 3 HP phases, x3 loot, vanilla/custom regression và Lower Workings. Không bắt đầu 6.7.45 trừ khi tôi chủ động waive.`
