# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.26**

Development branch:

`v0.2-alpha6-7-44-26-pelipper-phase-lifecycle`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.26`
- CI source SHA: `aa4bc43d41f3755728bf76285c4a40bd2f7e98d6`
- CI run: `35178910723`
- CI job: `105066633179`
- Private prerelease asset ID: `569378688`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.26_PELIPPER_PHASE_LIFECYCLE_TEST.zip`
- ZIP SHA256: `6aeb3b44c1fe03a2de3b9efc908b02d006056e5ad039b005341364c8ff58d170`
- Build: PASS, 0 warnings, 0 errors

Docs-only commits after the CI source SHA are expected.

## Live truth so far

- 6.7.44.19: genuine same-species Pelipper followers spawn with Spawn Commands OFF; no visible Slime fallback.
- 6.7.44.20: generic Monster pursuit flags do not make Pelipper wild Pokemon actively attack Farmer.
- 6.7.44.21: Team Up-owned chase works, but tile-path locomotion was visually poor.
- 6.7.44.22: followers live-proved able to attack Farmer; leader still had short reach and jitter.
- 6.7.44.23: leader smoothing/reach attempt; live log showed per-step `Halt()` was effectively 1:1 with leader movement and therefore a likely jitter source.
- 6.7.44.24: adds 160px elite reach, 128px hold band, intended Mutation x2 damage preservation, requested-vs-actual damage telemetry, and Mutant-leader-only capture blocking. Not live-tested yet.
- 6.7.44.25: continuous leader chase bypasses the legacy per-step Halt outside the 128px hold band. Not live-tested yet.
- 6.7.44.26: explicit Pelipper Mutant phase lifecycle overlay. Not live-tested yet.

## 6.7.44.26 phase lifecycle

`Alpha674426PelipperMutantPhaseLifecycleService` is chained through `Alpha674424EliteReachOverlayService`, so existing ModEntry registration/status/reset wiring stays stable.

It does not replace the existing source-aware HP engine. It observes and hardens it:

- records explicit phase markers for `1/3 -> 2/3 -> 3/3`;
- observes `ExtraLifeMarker` transitions produced by the proven source-aware damage hook;
- verifies a guarded lethal transition restored authoritative Pelipper HP to full;
- records the final lethal candidate separately;
- patches native `monsterDrop` entry points and blocks premature drop calls during non-final or just-guarded phases;
- final-phase native death/drop remains authoritative;
- global Mutant native loot x3 remains the existing reward implementation;
- followers are not treated as Mutant leaders and remain catchable;
- no movement, teleport, controller replacement, fake capture, or fallback monster creation is added by 6.7.44.26.

Expected `teamup_mutation status` line:

```text
Pelipper Mutation phases: explicit-3-phase
```

Useful fields:

```text
tracked
transitions
phase2
phase3
restoreVerified
restoreMismatch
finalLethalArmed
prematureDropBlocks
finalDropPasses
unverifiedFinalDrops
invalidState
```

Healthy three-phase runtime should ultimately show:

```text
transitions=2
phase2=1
phase3=1
restoreVerified=2
restoreMismatch=0
finalLethalArmed>=1
invalidState=0
```

For reward, final native `monsterDrop` may be invoked three times because the existing reward service repeats the native drop pass twice to achieve x3. Those final calls must not be blocked by the phase lifecycle guard.

## Immediate runtime test

Install 6.7.44.26 and keep Pelipper Spawn Commands OFF.

Run:

```text
teamup_mutation force
```

Validate in one encounter if possible:

1. Followers still attack and retain native capture.
2. Mutant leader chase is visually smoother than 6.7.44.23/24.
3. Leader reaches/attacks from the 160px band instead of requiring pixel contact.
4. Leader requested damage preserves Mutation Stat x2; compare requested vs actual damage if Farmer defense reduces it.
5. Poké Ball cannot capture the Mutant leader, while followers remain catchable.
6. First lethal bar transition restores full HP and enters phase 2/3.
7. Second lethal bar transition restores full HP and enters phase 3/3.
8. Third lethal bar kills the leader for real.
9. No loot appears on phase 1->2 or 2->3; final death receives native loot x3.

Then run:

```text
teamup_mutation status
```

Inspect the continuous-chase, elite-finalization, source mutation, phase lifecycle, and Mutant reward lines together.

## Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**.

Leader keeps HP x3, stat x2, visible Pelipper x2 cap, aura, no capture, and final native loot x3. Followers match the original creature, remain ordinary and Mutation-excluded, receive no x3 leader reward, and retain genuine Pelipper wild capture semantics.

No unrelated Slime fallback.

## Remaining gates before 6.7.45

- live-pass 6.7.44.26 combined leader movement/reach/damage/capture behavior;
- live-pass all three Pelipper HP phases;
- live-pass final x3 leader loot with no phase-early reward;
- vanilla/non-Pelipper Mutation regression;
- compatible custom-source regression when practical;
- Lower Workings runtime gate.

Do not start 6.7.45 unless the user explicitly waives the remaining runtime gates.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-26-pelipper-phase-lifecycle. Current verified code SHA aa4bc43d41f3755728bf76285c4a40bd2f7e98d6, run 35178910723, ZIP SHA256 6aeb3b44c1fe03a2de3b9efc908b02d006056e5ad039b005341364c8ff58d170. 6.7.44.25 đã build continuous leader chase để bỏ per-step Halt gây jitter; 6.7.44.26 thêm explicit 1/3->2/3->3/3 phase lifecycle + premature loot guard mà không thay source-aware HP engine. Ưu tiên live-test một encounter hoàn chỉnh: follower attack/capture, leader smooth/reach/damage/capture lock, 3 HP phases, final loot x3; sau đó vanilla/custom regression và Lower Workings. Không bắt đầu 6.7.45 trừ khi tôi waive.`
