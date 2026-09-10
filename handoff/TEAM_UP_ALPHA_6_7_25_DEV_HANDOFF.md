# Team Up Alpha 6.7.25 Developer Handoff

Branch: `v0.2-alpha6-7-25-first-surge-trigger`

Version: `0.2.0-alpha.6.7.25`

CI-verified materialized source commit: `dba880e38d1baf282b1cce6794e4c93f1d5d158a`

Workflow run: `34452192020`

Job: `102790231239`

Artifact ID: `10141996290`

Inner mod ZIP: `TeamUp_v0.2.0-alpha.6.7.25_FIRST_SURGE_TRIGGER_TEST.zip`

Inner SHA256: `e78fdf42e6c66bb3c99eedeb2ef153486868890cff9132cef89cb4b40f689933`

## Purpose

This checkpoint makes the first Mutation a deterministic story trigger for The Surge instead of allowing the existing 5% random mutation system and 2.5x density overlay to be narratively active from the beginning.

## Runtime rules

- Before activation, eligible lethal monster defeats are persisted per host Farmer.
- Defeats 1 through 9 cannot randomly mutate.
- Eligible lethal defeat 10 is forced to transform that same dying monster into the first Mutant.
- The Surge activation flag is committed only after the mutation transformation succeeds.
- After activation, the existing configured random mutation chance resumes.
- The existing MonsterSurgeService density overlay is gated until activation.
- A per-monster modData marker prevents repeated deathAnimation calls from double-counting one actor.
- Debug `teamup_mutation force` does not advance or activate the story gate.

## Test command

`teamup_surge_story status|reset|setkills <0-9>`

Diagnostic file:

`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Surge_Story_latest.txt`

Fast live test:

1. `teamup_surge_story reset`
2. `teamup_surge_story setkills 9`
3. `teamup_surge_story status`
4. Kill one ordinary eligible monster.
5. It must revive/transform into the first Mutant.
6. `teamup_surge_story status`
7. Expect `activated=True`, `kills=10/10`, `randomMutationUnlocked=True`, `densityUnlocked=True`.

## CI result

PASS with 0 warnings and 0 errors.

CI confirms source/build invariants only. Live ten-defeat behavior is not yet verified.

## Next narrative checkpoint

After live confirmation, the safe next story layer is Linus reacting to the first Surge incident and directing the player toward Marlon. Do not merge this branch to stable main solely because CI passed.
