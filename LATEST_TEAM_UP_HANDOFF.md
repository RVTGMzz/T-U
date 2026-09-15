# Team Up latest handoff: 0.2.0-alpha.6.7.44.21

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_21_PELIPPER_MUTATION_HOSTILITY_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-21-pelipper-mutation-hostility`
- Version: `0.2.0-alpha.6.7.44.21`
- CI source SHA: `9bb3a88a98c51a3695ffa9e982f3da482c08632c`
- Run: `34976619122`
- Job: `104405839414`
- Artifact ID: `10398993101`
- Wrapper SHA256: `c27601d8160b76edaad383ed0be7a1dcd47f0b5e4b672101e71b8b38ae41d72e`
- Inner ZIP SHA256: `7e68d423593acf32efcfa1d618bb5d5f90cfa9a0f9679e976b309ee6aaa602e9`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Current live truth

6.7.44.19 proved native same-species Pelipper followers can spawn 4/4 with Spawn Commands OFF and no Slime fallback.

6.7.44.20 live Growlithe test proved generic native pursuit is not sufficient: Team Up successfully armed leader/minion proxy+source dozens of times with zero identity misses, but Pelipper wild Pokemon still did not attack Farmer. Treat Pelipper wilds as passive provider actors rather than native hostile Monster AI.

## 6.7.44.21

Adds `Alpha674421PelipperMutationHostilityService` only for Pelipper Mutation leaders/minions. The genuine visible Pokemon source is driven toward Farmer using a private `PathFindController`; its genuine hidden Monster proxy is synchronized to the same position; contact damage calls `Farmer.takeDamage(..., proxy)` with a cooldown. No replacement actor is created and capture identity is preserved architecturally. Natural non-Mutation wild Pokemon are untouched.

Immediate gate: force a Pelipper Mutation, do not attack first, verify leader + followers visibly chase and damage Farmer. `teamup_mutation status` should show `pathsBuilt>0`, `pathSteps>0`, `pairs>0`, leader/minion counts positive and, after contact, `contactDamageCalls>0`, ideally `identityMisses=0`.

Then explicitly recheck follower Poké Ball capture. After that continue 3 HP phases, x3 final loot, vanilla/custom regression and Lower Workings.

Do not begin 6.7.45 unless remaining runtime gates are passed or explicitly waived.
