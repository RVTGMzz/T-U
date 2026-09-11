# Team Up - Canonical Latest Handoff

Read this file first when continuing Team Up. Detailed implementation notes are in `docs/ALPHA_6_7_44_3_PELIPPER_RUNTIME_HOTFIX_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.3`
- Branch: `v0.2-alpha6-7-44-3-pelipper-runtime-hotfix`
- Base: `v0.2-alpha6-7-44-2-pelipper-encounter-reactions`
- Successful CI input SHA: `983210f32262e8d783000435e5458c1ba3872815`
- CI-materialized source SHA: `2fe9ff859a2ea02383adecb35e8f85dcbecd461e`
- Successful CI run: `34656390802`
- Successful CI job: `103449634306`
- Artifact ID: `10286316141`
- Artifact name: `team-up-alpha6-7-44-3-pelipper-runtime-hotfix`
- Artifact wrapper SHA256: `2a629e649887b155965344490f51c6cbe14008210ac9bfe2c2832e01197196dc`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.3_PELIPPER_RUNTIME_HOTFIX_TEST.zip`
- Inner ZIP SHA256: `fbea4f399fa62910322c85dbd48523413ae2260671056410a812a8a094d23ad8`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared
- 6.7.45: NOT started

## Why 6.7.44.3 exists
Live 6.7.44.2 test exposed these regressions:
- normal Green Slime falsely triggered the `SHINY` Farmer prompt;
- the prompt could reopen repeatedly after the Farmer answered;
- normal Green Slime could repeatedly log as `EliteBoss` because raw HP was used as an elite heuristic;
- Pelipper wild low-HP capture protection was disabled because Team Up waited for a universal Catch Mode boolean Pelipper does not reliably expose;
- historical Mutation policy rejected every Pelipper wild combat proxy, so `teamup_mutation force` could find no target while Pelipper owned the combat actors.

## 6.7.44.3 runtime rules
### Pelipper capture safety
For a genuine Pelipper wild combat proxy:
- Pelipper presence is now the authority for Team Up mercy/capture protection.
- A source threshold is used when Team Up can resolve one; otherwise fallback is 10%.
- Existing damage clamps, per-tick floor repair and offensive ceasefire remain active.
- At the protected floor Team Up should report `budget=0`, `protected=True`, `target=False`, `proxy=True`.

A Team Up Mutant is deliberately excluded from this mercy floor so it cannot become stuck at 10% HP.

### Shiny detection
Shiny requires BOTH:
1. genuine Pelipper wild identity;
2. authoritative current Shiny state.

Capability/config signals such as `CanBeShiny`, Shiny chance/odds/rate/roll/eligibility are not accepted as live Shiny state.

6.7.44.2 false Team Up Shiny markers are repaired when strict evidence is absent.

### Shiny prompt/reaction spam
- Any Farmer answer makes the prompt sticky for that encounter, not only `Engage`.
- Prompt identity no longer depends on moving tile coordinates.
- Encounter reactions have a stable location/name/type/kind cooldown to suppress proxy-recreation log/bubble spam.

### Elite/Boss recognition
Raw `MaxHealth >= 300` was removed as an Elite/Boss criterion. Explicit identity/type/modData boss/elite/champion evidence is required instead.

### Pelipper wild Mutation
Normal non-Shiny Pelipper wild combat proxies are now eligible for Team Up Mutation.

Priority:
- confirmed natural Shiny -> excluded from Mutation;
- owned/source-controlled companions -> excluded;
- boss/scripted/protected/test/summon/minion/Surge exclusions remain;
- ordinary non-Shiny Pelipper wild -> eligible.

On transform, a Pelipper wild Mutant gets Team Up combat targeting restored and does not use the capture mercy floor.

Important limitation: 6.7.44.3 does NOT yet prove/intercept Pelipper's actual Poké Ball capture path for a Mutant. Do not claim Mutants are fully non-catchable until that exact path is implemented and live-tested.

## CI acceptance
Run `34656390802` passed:
- `PELIPPER WILD MERCY PRIORITY AUDIT: PASS`
- `SHINY FALSE-POSITIVE + PROMPT SPAM AUDIT: PASS`
- `PELIPPER WILD MUTATION ELIGIBILITY AUDIT: PASS`
- `6.7.44.1 LOWER WORKINGS TMX HOTFIX CARRY-FORWARD: PASS`
- `EN/VI PARITY CARRY-FORWARD: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT + LOWER WORKINGS TMX RE-PARSE: PASS`
- `0 Warning(s)`, `0 Error(s)`.

Run #1 `34656314841` failed only because the new static audit searched too broad a region for `target.Tile.X`; the runtime materializer itself had passed. The audit was corrected before successful run #2.

## Lower Workings carry-forward
6.7.44.1 TMX hotfix remains intact:
- location `Ronvotri.TeamUp_LowerWorkings`;
- 32x24 `assets/LowerWorkings.tmx`;
- Back / Buildings / Front;
- dynamic exact return to persisted breach;
- no static TMX Warp;
- three 120-tick clue holds at `(8,9)`, `(22,8)`, `(23,16)`;
- early withdrawal preserves stages 2..4;
- stage 5 secured withdrawal, then Guild report -> stage 6;
- host-authoritative story progression and shared farmhand route.

Story reaction windows remain 36..41 with EN/VI parity.

## Lore/gameplay locks
- SURGE HIGH.
- Entry Protocol READY.
- Story NPC slots 4/4.
- Hard formation cap = 5 PEOPLE including Farmers.
- George pre-reveal stays observed Rank D, Non-Combatant, unrecruitable, no Rank S, no `The Last Blaster`, no historical-miner confirmation. Reveal remains 6.7.46.
- Evelyn stays ordinary low Rank D healer/support in main story; secret remains postgame.
- Historical worker and source/entity beyond the seal remain unidentified.
- No exact `SECTOR 17` unless later explicitly designed.
- No final boss.
- Pelipper source ownership/render/controller authority preserved.
- No legacy fake-hide writer.
- Do not merge `main` until explicitly requested.

## Mandatory live test now
Install ONLY **6.7.44.3**, replacing the old Team Up folder.

### 1. Ordinary Green Slime
- no SHINY prompt;
- no Elite/Boss reaction solely due to HP;
- no repeated `kind=EliteBoss target=Green Slime` log flood.

### 2. Normal Pelipper wild capture floor
Use Farmer + Team Up NPC + player Pokémon against a normal wild Pokémon.
- Reduce it to the active capture threshold.
- Team Up allies must stop attacking it there.
- It must not be killed by Team Up friendly damage after entering the floor.

Commands:
- `teamup_capture`
- `teamup_capture_proxy`
- `teamup_preflight`
- `teamup_encounter status`

Expected protected state: `budget=0`, `protected=True`, `target=False`, `proxy=True`.

### 3. Forced Mutation with Pelipper
On a normal non-Shiny Pelipper wild target:
- `teamup_mutation force`
- `teamup_mutation list`
- `teamup_mutation status`

Expected:
- force finds/transforms the target instead of saying no eligible monster;
- Mutant remains attackable;
- Mutant does not stop at Pelipper's 10% mercy floor.

### 4. Real Shiny
- exactly one Shiny attention/prompt for that encounter;
- Hold -> Team Up stops;
- Ignore -> Team Up leaves it alone;
- Engage -> releases Team Up Shiny hold;
- no repeated prompt after answering;
- natural Shiny remains Mutation-exempt.

Test capture floor on a normal wild Pokémon before risking a real rare Shiny.

### 5. Lower Workings gate
Still verify custom map load, collision, exact ingress/egress, save/reload inside, all three clues, early withdrawal, stage 5 safe return, stage 6 Guild report, farmhand access, and reaction windows 36..41.

## Next target
### 6.7.45 - Containment Chamber Escalation Encounter
Do NOT begin until 6.7.44.3 Pelipper runtime regressions and the Lower Workings live gate pass, unless the user explicitly waives the gate.

Goals remain:
- first major Lower Workings chamber escalation encounter;
- stronger causal evidence linking current Mutations to the old containment event;
- secure retreat and host authority;
- immediate NPC reactions;
- George remains anonymous/unrevealed until 6.7.46;
- no final boss.

## Detailed handoff
`docs/ALPHA_6_7_44_3_PELIPPER_RUNTIME_HOTFIX_HANDOFF.md`

## New-chat instruction
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-3-pelipper-runtime-hotfix. Live-test Green Slime false Shiny/Elite, Pelipper 10% mercy, Pelipper wild Mutation và Lower Workings trước khi bắt đầu 6.7.45.`
