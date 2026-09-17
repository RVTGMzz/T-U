# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.27**

Development branch:

`v0.2-alpha6-7-44-27-mutation-regression-guard`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.27`
- CI source SHA: `7ea3e52aa55ef4dc0895985c614d16f425d03c9b`
- CI run: `35179652233`
- CI job: `105068859002`
- Private prerelease asset ID: `569397151`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.27_MUTATION_REGRESSION_GUARD_TEST.zip`
- ZIP SHA256: `9609d82c02917f63cf08d450804cb7ddaaa51ad5e21b1f91f49b7810d1aa38b5`
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
- 6.7.44.26: explicit Pelipper Mutant phase lifecycle + premature loot guard. Not live-tested yet.
- 6.7.44.27: non-Pelipper Mutation regression guard. Build/audit PASS; runtime live validation still pending.

## 6.7.44.27 regression guard

`Alpha674427MutationRegressionGuardService` is chained through `Alpha674424EliteReachOverlayService`, alongside 6.7.44.25 continuous chase and 6.7.44.26 phase lifecycle, so existing status/reset wiring remains stable.

The guard takes authority at `MonsterMutationMinionFactory.Create` with a Priority.First Harmony prefix:

- Pelipper cannot use the generic factory; it must stay on the provider-native `pokemon_spawn` path.
- Vanilla/custom sources with a safe source-equivalent runtime constructor use that exact runtime type.
- Supported constructor shapes are `(Vector2)` and `(Vector2, int)` where the second parameter is level/mine/difficulty/depth-like.
- Resulting ordinary followers are normalized to normal HP/damage/speed, not Mutation stats.
- Unsupported sources return `unsupported-fail-closed` and spawn no unrelated replacement.
- The legacy factory body is skipped while the guard is active, so the historical unrelated `green-slime-fallback` branch is never executed at runtime.
- A real GreenSlime remains valid when the source itself is a GreenSlime because that is source-equivalent same-runtime behavior, not fallback substitution.

Expected status line:

```text
Mutation regression guard: source-equivalent-only
```

Useful fields:

```text
factoryCalls
sameRuntimeAllowed
unsupportedBlocked
pelipperBlocked
greenSlimeFallbackPrevented
constructorFailures
legacyFactoryOriginalRuns=0
```

## Combined runtime test

Install the latest 6.7.44.27 build. A Pelipper encounter should still validate all carry-forward behavior from 6.7.44.26:

1. 2-4 same-species native followers.
2. Followers attack and remain catchable.
3. Mutant leader continuous chase, 160px reach, x2 intended damage and no capture.
4. Explicit 1/3 -> 2/3 -> 3/3 HP lifecycle.
5. No early loot, final native loot x3.

For non-Pelipper regression, force/observe Mutation on vanilla monsters where practical. Safe sources should report same-runtime followers; unsupported/custom sources should fail closed and must never turn into an unrelated GreenSlime.

Then run:

```text
teamup_mutation status
```

Inspect source Mutation, follower provider, continuous chase, elite finalization, phase lifecycle, reward and regression guard together.

## Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**.

Leader keeps HP x3, stat x2, visible Pelipper x2 cap, aura, no capture and final native loot x3. Followers match the original creature, remain ordinary and Mutation-excluded, receive no x3 leader reward, and retain provider-native capture semantics when Pelipper is the source.

Unsupported source providers fail closed. No unrelated Slime fallback.

## Remaining gates before 6.7.45

- live-pass latest combined Pelipper movement/reach/damage/capture behavior;
- live-pass all three Pelipper HP phases;
- live-pass final x3 leader loot with no phase-early reward;
- live-pass vanilla/non-Pelipper regression when practical;
- compatible custom-source regression when practical;
- Lower Workings runtime gate.

Next code/build focus: **Lower Workings runtime gate**. Do not start 6.7.45 story content unless the user explicitly waives remaining runtime validation.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-27-mutation-regression-guard. Current verified code SHA 7ea3e52aa55ef4dc0895985c614d16f425d03c9b, run 35179652233, ZIP SHA256 9609d82c02917f63cf08d450804cb7ddaaa51ad5e21b1f91f49b7810d1aa38b5. 6.7.44.25 bỏ per-step Halt cho leader, 6.7.44.26 harden explicit 3-phase HP + early-loot guard, 6.7.44.27 khóa generic Mutation follower factory thành source-equivalent-only và fail-closed unsupported sources, không cho legacy GreenSlime fallback chạy. Tiếp theo build Lower Workings runtime gate; giữ 6.7.45 story chưa bắt đầu cho tới khi runtime gates được live-test hoặc tôi chủ động waive.`
