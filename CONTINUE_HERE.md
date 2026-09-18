# Continue Team Up Here

Repository: **`ronvotri/T-U`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.37**

Development branch:

`v0.2-alpha6-7-44-37-regression-stability`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_37_REGRESSION_STABILITY_HANDOFF.md`

## Verified build checkpoint

- Repository: `ronvotri/T-U`
- Version: `0.2.0-alpha.6.7.44.37`
- Branch: `v0.2-alpha6-7-44-37-regression-stability`
- CI source SHA: `561909f1849d06c0d9750deac2e523ff30009995`
- CI run: `35363771773`
- CI job: `105661138777`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.37_REGRESSION_STABILITY_TEST.zip`
- ZIP SHA256: `7ebca7fe3f75bafbce33086c2d0b3680d01dc19b669c79809e35ccdf21eb59e7`
- Build: PASS, 0 warnings, 0 errors
- Package audit: PASS

## Current live truth

- 6.7.44.33 restored Pelipper Mutation source/proxy pairing and Ron live-confirmed Mutant spawning again.
- 6.7.44.34 addressed flicker, x3 aggro arena and stronger damage.
- 6.7.44.35 addressed leader pursuit jitter, hold 128px and reach 160px. Ron confirmed it was OK.
- 6.7.44.36 bundles leader no-capture, explicit 3 HP phases and final-only x3 native loot. CI PASS; live contract still needs confirmation.
- 6.7.44.24-30 remain excluded because the combined stack caused load crashes.

## 6.7.44.37 delta

Regression + Stability hardening:

- removed GreenSlime fallback from `MonsterMutationMinionFactory` itself;
- Pelipper sources must use Pelipper native spawn path;
- vanilla/custom followers must be exact same runtime type;
- unsupported custom sources fail closed and spawn nothing;
- legacy wave also handles nullable factory result and cannot create fallback;
- new command `teamup_mutation_regression` audits type mismatch, recursive Mutation, missing exclusion markers, factory fallback, Pelipper identity and duplicate encounter IDs;
- 6.7.44.34-36 combat and elite contract remain carried forward.

## Immediate runtime test

1. Install 6.7.44.37.
2. Force one normal Mutation: `teamup_mutation force`
3. Let the follower wave finish spawning.
4. Run: `teamup_mutation_regression`

Expected core result:

`Mutation regression audit: PASS`

For a Pelipper encounter, `factoryFallback=0`, `typeMismatch=0`, `recursiveMutants=0`, `missingExcluded=0`, `pelipperIdentityMiss=0`, `pelipperDuplicateEncounter=0`.

Also keep the 6.7.44.36 elite contract test in mind: leader not catchable, follower native-catchable, two HP restores without loot, third death real with x3 native reward.

## Frozen Mutation contract

One encounter: **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**.

Leader:
- HP x3 as 3 source-aware phases;
- stats x2;
- visible Pelipper scale cap x2;
- Mutation aura;
- cannot be captured;
- final native reward x3 only after phase 3.

Followers:
- same/source-equivalent creature;
- ordinary hostile;
- Mutation-excluded;
- no Mutation aura/bonus/x3 reward;
- Pelipper followers are genuine native wild encounters;
- unsupported custom sources fail closed;
- no unrelated GreenSlime fallback.

Real Pelipper HP:
- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

Never use the hidden proxy 1,000,000 HP as real Pokemon HP.

## Remaining gates before 6.7.45

1. Live-confirm 6.7.44.36 elite contract + 6.7.44.37 regression audit.
2. Build Lower Workings Runtime Gate v2 without the previous crash path.
3. Then start 6.7.45 Containment Chamber Escalation unless Ron explicitly waives a remaining gate.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trong repo ronvotri/T-U, branch v0.2-alpha6-7-44-37-regression-stability. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_37_REGRESSION_STABILITY_HANDOFF.md. Current CI source SHA 561909f1849d06c0d9750deac2e523ff30009995, run 35363771773. 6.7.44.35 chase/reach đã được Ron xác nhận OK. 6.7.44.36 bundles no-capture + 3 HP phases + final-only x3 loot. 6.7.44.37 removes GreenSlime fallback at factory level, requires exact same-type vanilla/custom followers, keeps Pelipper native followers, adds teamup_mutation_regression audit. Old 6.7.44.24-30 crash stack remains excluded. Next gate after live regression is Lower Workings Runtime Gate v2, then 6.7.45 story.`
