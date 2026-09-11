# Team Up 0.2.0-alpha.6.7.44.3 - Pelipper Runtime Hotfix Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.44.3`
- Branch: `v0.2-alpha6-7-44-3-pelipper-runtime-hotfix`
- Base: `v0.2-alpha6-7-44-2-pelipper-encounter-reactions`
- Successful CI input SHA: `983210f32262e8d783000435e5458c1ba3872815`
- Materialized source SHA: `2fe9ff859a2ea02383adecb35e8f85dcbecd461e`
- Successful CI run: `34656390802`
- Successful CI job: `103449634306`
- Artifact ID: `10286316141`
- Artifact: `team-up-alpha6-7-44-3-pelipper-runtime-hotfix`
- Artifact wrapper SHA256: `2a629e649887b155965344490f51c6cbe14008210ac9bfe2c2832e01197196dc`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.3_PELIPPER_RUNTIME_HOTFIX_TEST.zip`
- Inner ZIP SHA256: `fbea4f399fa62910322c85dbd48523413ae2260671056410a812a8a094d23ad8`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- `main`: NOT merged
- Stable: NOT declared
- 6.7.45: NOT started

## Why this hotfix exists
Live 6.7.44.2 testing found four connected regressions:

1. An ordinary Green Slime was falsely announced as `SHINY` and opened the Shiny tactical dialogue.
2. The Shiny dialogue reopened repeatedly after the Farmer answered it.
3. Pelipper low-HP capture mercy was not active, so Team Up allies could continue attacking below the intended capture threshold.
4. `teamup_mutation force` could not find a usable target when Pelipper wild combat proxies were the active combat actors.

The live log also repeatedly emitted:
`[EncounterReaction] kind=EliteBoss target=Green Slime ...`

## Root causes
### False Shiny
6.7.44.2 reflection treated any member containing `shiny` as current Shiny state. Capability/config members such as `CanBeShiny`, `ShinyChance`, odds/rate/roll fields could therefore be mistaken for a live Shiny flag.

### Elite/Boss false positive
6.7.44.2 treated `MaxHealth >= 300` as an Elite/Boss signal. Pelipper/custom combat scaling can legitimately push ordinary monsters above that value.

### Capture mercy disabled
6.7.44.2 required Team Up to positively resolve a universal Pelipper `Catch Mode` boolean before enabling the capture floor. Current Pelipper runtime exposes capture settings but not one stable universal mode flag for Team Up to depend on, so `_modeConfirmed` stayed false and mercy protection was disabled.

### Mutation blocked by Pelipper
The historical 6.7.19 Mutation policy explicitly excluded every `PelipperTownCompatibilityService.IsWildCombatActor(monster)`, so when Pelipper owned the mine encounter proxy there could be no eligible forced Mutation target.

## 6.7.44.3 behavior
### Pelipper capture priority
For a genuine Pelipper wild combat proxy:
- Pelipper installed/present -> Team Up low-HP mercy protection is enabled.
- Team Up still prefers a reflected Pelipper threshold if one is available.
- If no threshold is exposed, fallback remains 10%.
- Team Up removes capture-protected targets from offensive targeting at the floor.
- Existing takeDamage / area-damage clamp and floor watchdog remain in force.

A Team Up Mutation is explicitly excluded from this mercy floor so a Mutant cannot become stuck as an immortal 10%-HP combat target.

### Shiny detection
A Shiny now requires:
- genuine Pelipper wild actor identity; and
- authoritative current-state evidence such as `Shiny`, `IsShiny`, `ShinyFlag`, `ShinyForm`, or equivalent accepted state key / explicit Shiny form evidence.

Rejected as Shiny evidence:
- chance
- odds
- rate
- weight
- roll
- eligibility
- `CanBeShiny`
- `AllowShiny`
- other capability/config-like members

6.7.44.2 Team Up false-Shiny markers are repaired before classification when the strict evidence is absent.

### Shiny prompt spam
- Any Farmer answer now makes the prompt sticky for that encounter, not only `Engage`.
- Prompt identity no longer includes moving tile coordinates.
- Encounter reactions also have a stable location/name/type/kind cooldown so transient proxy recreation cannot flood the same reaction/log continuously.

### Elite/Boss detection
Raw HP is no longer an Elite/Boss classifier.
Elite/Boss recognition now requires explicit identity/type/modData signals such as boss/elite/champion semantics.

### Pelipper wild Mutation eligibility
Normal Pelipper wild combat proxies are now eligible for Team Up Mutation.

Priority:
1. Confirmed natural Shiny -> Mutation excluded.
2. Owned/source-controlled Pelipper companion -> excluded.
3. Boss/scripted/protected/test/summon/Mutation-minion/Surge exclusions remain.
4. Ordinary non-Shiny Pelipper wild proxy -> eligible.

When a Pelipper wild proxy transforms into a Team Up Mutant:
- Team Up restores `CombatTarget=true`;
- the Mutant does not receive Pelipper low-HP mercy protection;
- combat can continue normally.

Important: this checkpoint does NOT yet prove that Pelipper Poké Ball capture itself cannot capture a Mutant. Do not claim full Mutant non-catchability until that exact source capture path is intercepted/tested.

## CI gates passed
Run `34656390802`:
- `PELIPPER WILD MERCY PRIORITY AUDIT: PASS`
- `SHINY FALSE-POSITIVE + PROMPT SPAM AUDIT: PASS`
- `PELIPPER WILD MUTATION ELIGIBILITY AUDIT: PASS`
- `6.7.44.1 LOWER WORKINGS TMX HOTFIX CARRY-FORWARD: PASS`
- `EN/VI PARITY CARRY-FORWARD: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT + LOWER WORKINGS TMX RE-PARSE: PASS`
- `0 Warning(s)` / `0 Error(s)`

Run #1 `34656314841` failed only because the new static audit searched too broad a region for `target.Tile.X`; the runtime materializer itself had passed. The audit was corrected, then run #2 passed completely.

## Mandatory live test
Replace the entire old Team Up folder with 6.7.44.3. Do not keep 6.7.44.2 beside it.

### A. Ordinary Green Slime
- Green Slime must NOT show a SHINY tactical dialogue.
- Green Slime must NOT become Elite/Boss merely from HP scaling.
- No repeated `[EncounterReaction] kind=EliteBoss target=Green Slime` flood.

### B. Pelipper normal wild capture floor
With Farmer + Team Up NPC + player Pokémon attacking a normal wild Pokémon:
- reduce it to the capture threshold;
- Team Up allies must stop offensive targeting there;
- it must not be killed by Team Up friendly damage after entering the floor.

Commands:
- `teamup_capture`
- `teamup_capture_proxy`
- `teamup_preflight`
- `teamup_encounter status`

Expected around the protected floor:
- `budget=0`
- `protected=True`
- `target=False`
- `proxy=True`

### C. Forced Mutation under Pelipper
On a normal non-Shiny Pelipper wild combat target:
- run `teamup_mutation force`;
- target should transform rather than report no eligible normal hostile monster;
- run `teamup_mutation list` and `teamup_mutation status`;
- Mutant should remain attackable and should not stop at Pelipper's 10% mercy floor.

### D. Real Shiny
- a real Pelipper Shiny should trigger exactly one Shiny reaction/prompt for that encounter;
- `Hold` keeps Team Up from attacking;
- `Ignore` leaves it alone;
- `Engage` releases Team Up hold;
- no repeated prompt after answering;
- confirmed natural Shiny must remain Mutation-exempt.

Do not use a rare Shiny as the first capture-floor test. Verify the normal wild floor first.

### E. Lower Workings carry-forward
Still verify the 6.7.44.1/6.7.44 map/warp/save gate before starting 6.7.45:
- custom location loads;
- collision/ingress/egress works;
- exact breach return works;
- save/reload in Lower Workings works;
- three 120-tick clues work;
- early withdrawal preserves stage;
- stage 5 -> safe withdrawal -> Guild report reaches stage 6;
- multiplayer shared route works;
- story reactions 36..41 remain correct.

## Story/lore locks unchanged
- SURGE HIGH.
- Entry Protocol READY.
- Story NPC slots 4/4.
- Hard formation cap 5 PEOPLE total including Farmers.
- George remains Rank D observed, Non-Combatant, unrecruitable and unrevealed until 6.7.46.
- Evelyn postgame secret untouched.
- Historical worker and source/entity behind seal remain unidentified.
- No exact `SECTOR 17`.
- No final boss.

## Next target
`6.7.45 - Containment Chamber Escalation Encounter`

Do not begin until 6.7.44.3 Pelipper runtime regression tests and the Lower Workings live gate pass, unless the user explicitly waives that gate.
