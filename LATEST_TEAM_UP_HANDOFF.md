# Team Up latest handoff: 0.2.0-alpha.6.7.44.36

Repository: **`ronvotri/T-U`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_36_ELITE_CONTRACT_FINALIZATION_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-36-elite-contract-finalization`
- Version: `0.2.0-alpha.6.7.44.36`
- CI source SHA: `a20fc738743eabb88bd8f9c080ce0cb12af40ac6`
- Run: `35362286478`
- Job: `105656210575`
- ZIP SHA256: `f05ea922ea88de8a5e4f65204208da9f4bcd34951d1d519d9fef2614a3805ea9`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Current live truth

6.7.44.33 restored Pelipper Mutation source/proxy identity after the rollback cycle. Ron live-confirmed Mutant spawning again. The current live problems are presentation flicker on the Mutant leader, too-short pursuit range for leader/followers, and weak-feeling contact damage.

6.7.44.24-30 are not part of the active runtime because that stack caused load/crash regressions.

## 6.7.44.34

6.7.44.34 keeps the loadable 6.7.44.23-shaped runtime plus the 6.7.44.33 pairing fixes, then adds only a focused combat-presence patch:

- pre-render x2 visible-scale stabilization;
- x3 Mutation aggro arena, 18 tiles;
- Pelipper `WildCombatEngaged=true`;
- Pelipper `PassiveUntilAttacked=false`;
- ordinary follower raw damage floor 4;
- Mutant leader raw damage floor 8 while preserving higher intended x2 damage.

Immediate live gate is only flicker, aggro distance and damage feel. Do not call Runtime PASS until Ron confirms.

After 6.7.44.34 passes, continue with leader movement/reach and reintroduce the remaining elite contract in small isolated steps. Do not begin 6.7.45 unless remaining gates pass or Ron explicitly waives them.


## 6.7.44.35

Focused leader locomotion/reach pass on the safe runtime shape. Per-step leader Halt is removed; hold is edge-triggered at 128px; attack reach is 160px. Followers remain unchanged. Runtime PASS requires Ron live confirmation.


## 6.7.44.36

Elite contract finalization bundles capture guard + explicit 3 HP phases + final-only x3 native loot. The old 6.7.44.24-30 crash stack remains excluded. Runtime PASS still requires Ron live confirmation.
