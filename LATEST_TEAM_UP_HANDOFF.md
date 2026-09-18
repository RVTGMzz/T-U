# Team Up latest handoff: 0.2.0-alpha.6.7.44.34

Repository: **`ronvotri/T-U`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_34_COMBAT_PRESENCE_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-34-combat-presence-fix`
- Version: `0.2.0-alpha.6.7.44.34`
- CI source SHA: `9c75bd4790be3eb747f1fd869621540efc86dae5`
- Run: `35289471845`
- Job: `105428979340`
- ZIP SHA256: `177036a76f6164bedf5fd15dd78ba8281eba1df95f2b0322df9efac5ee284d2a`
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
