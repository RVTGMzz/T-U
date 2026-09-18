# Team Up - Canonical Latest Handoff

Repository: **`ronvotri/T-U`**

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_37_REGRESSION_STABILITY_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.37`
- Branch: `v0.2-alpha6-7-44-37-regression-stability`
- CI source SHA: `561909f1849d06c0d9750deac2e523ff30009995`
- Run: `35363771773`
- Job: `105661138777`
- ZIP SHA256: `7ebca7fe3f75bafbce33086c2d0b3680d01dc19b669c79809e35ccdf21eb59e7`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Active architecture

Pelipper Mutation:
- visible source: `PelipperTown.PokemonNpc`;
- hidden combat proxy: `StardewValley.Monsters.Monster`;
- real HP: `WildCurrentHealth` / `WildMaxHealth`;
- source/proxy identity recognizes `PokemonNpcEncounter/v1`;
- Nidoran♂/♀ remain distinct;
- x3 aggro arena;
- leader/follower combat engaged;
- leader continuous pursuit, hold 128px, reach 160px;
- leader no-capture;
- 3 HP phases;
- final-only x3 native reward.

Minion stability:
- Pelipper: genuine native followers;
- vanilla/custom: exact same runtime type only;
- unsupported source: fail closed;
- no unrelated GreenSlime fallback at factory level.

## Runtime audit

After a Mutation wave:

`teamup_mutation_regression`

PASS requires zero type mismatch, recursive Mutants, missing MutationExcluded, factory fallback, Pelipper identity miss and duplicate Pelipper encounter IDs.

## Safety boundary

Do not restore the old 6.7.44.24-30 service stack wholesale. It caused live load crashes.

## Next sequence

1. Live-confirm 6.7.44.36 elite contract and 6.7.44.37 regression.
2. Build Lower Workings Runtime Gate v2 with no previous crash-path reuse.
3. Then prepare 6.7.45 Containment Chamber Escalation.
