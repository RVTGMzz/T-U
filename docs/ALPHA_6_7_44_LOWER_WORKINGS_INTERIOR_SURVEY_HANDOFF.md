# Team Up Alpha 6.7.44 - Dedicated Lower Workings Map / Interior Survey Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.44`
- Branch: `v0.2-alpha6-7-44-lower-workings-interior-survey`
- Previous 6.7.43 branch head / exact base: `88094c40af824d9e61537e220c557c7a22b35e1b`
- Materializer commit: `f7df8417e8fcc5a0d210b68032c3ad40006e0a13`
- Authority/compile fixups commit: `a99485b06198eb8c68b517512027c3a785343270`
- Builder commit: `246567b8dc1c48d60011b2d880d61510f35550ac`
- Workflow / CI input: `644a4b28b1fbde3729d4c41b5117d69dcecbc8c7`
- CI-verified materialized source: `1c5355874aec8b53064b3a102489ed81ee4cb0fe`
- Successful CI run: `34616862316`
- Successful CI job: `103320840886`
- Artifact ID: `10270816070`
- Artifact name: `team-up-alpha6-7-44-lower-workings-interior-survey`
- Artifact wrapper SHA256: `0751aa4da65b119c16773f571ea8682a198593f95dfc21d8c65ba356fd067228`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44_LOWER_WORKINGS_INTERIOR_SURVEY_TEST.zip`
- Inner ZIP SHA256: `7cd840107a8998b09e27e83c3461262495818b58e79fb2309a432c921406ee0b`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared; live in-game verification is still required

## Implemented
Alpha 6.7.44 replaces the old conceptual-only interior threshold with the first actual Lower Workings location layer.

### Real Lower Workings location
- New location name: `Ronvotri.TeamUp_LowerWorkings`.
- Registered through Stardew 1.6 `Data/Locations`.
- Uses serializable vanilla type `StardewValley.GameLocation`.
- `CreateOnLoad.MapPath` points to the mod-owned `assets/LowerWorkings.tmx`.
- TMX size: `32x24` tiles.
- TMX uses vanilla `Mines/mine.png`; package does not redistribute vanilla art.
- Required `Back`, `Buildings`, and `Front` layers are present.
- There is intentionally no static TMX Warp property because the return target is the save-specific recorded breach.
- Location is excluded from normal NPC pathfinding and is not always-active.

### Persistent ingress anchor
New key:
- `Ronvotri.TeamUp/Story/LowerWorkingsBreachTile`

For progression that crosses the 6.7.42 threshold while 6.7.44 is installed, the current real player tile is stored with the already-authoritative recorded breach MineShaft.

For a legacy 6.7.43 save that has first descent complete but no tile anchor, the first 6.7.44 entry action at the recorded breach calibrates the current tile once and persists it. This avoids inventing a fake universal mine coordinate.

### Interior survey state
New persistent keys:
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyStage`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorEntered`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyComplete`
- `Ronvotri.TeamUp/Story/LowerWorkingsSafeReturnUsed`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyReported`

Stage route is `0..6`:
1. `0`: wait for first-descent completion and receive dedicated interior-survey authorization at the Adventure Guild.
2. `1`: return full operational formation to the recorded breach and enter the dedicated Lower Workings location.
3. `2`: survey directed emergency cribbing near tile `(8,9)` for `120` continuous ticks.
4. `3`: survey newer Mutation-linked residue over older blast scoring near `(22,8)` for `120` continuous ticks.
5. `4`: survey the deeper sealed-pressure edge near `(23,16)` for `120` continuous ticks.
6. `5`: survey is complete; return to entry tile `(15,21)` and use the secured withdrawal route to the exact recorded breach anchor.
7. `6`: report at the Adventure Guild; dedicated interior survey complete.

Full operational formation is required for all three clue holds. The adaptive people/NPC requirements from Entry Protocol are reused; the hard formation ceiling remains 5 PEOPLE total.

### Safe return / lifecycle
- Entry tile is `(15,21)`.
- Action at the entry allows withdrawal back to the save-specific recorded breach anchor.
- Early withdrawal at stages `2..4` preserves the survey stage, allowing regroup/re-entry without losing progress.
- At stage `5`, using the return route persists `LowerWorkingsSafeReturnUsed`; the Guild report only completes after that verified return.
- Host is authoritative for story stages/flags.
- Farmhands can use the shared route after the host opens it, but the 6.7.44 SaveLoaded path does not write host story flags from a farmhand.

## Interior evidence / lore boundary
6.7.44 deliberately escalates evidence without solving the mystery:
- directed cribbing was arranged to steer a failure inward while preserving a withdrawal lane;
- newer Mutation-linked mineral-organic residue lies over older blast scoring, so the current Surge is touching an older sealed system rather than causing the original blast geometry;
- the trace becomes denser along fractures at a deeper pressure edge, proving an active present-day influence from beyond the old closure;
- the source/entity/reason remains unidentified;
- the historical worker remains unidentified.

There is NO boss, story monster, containment encounter, or forced seal opening in 6.7.44. Those belong to 6.7.45 escalation scope.

## Bundled reactions
Reaction catalog now supports windows `0..41` with exact EN/VI parity and `584` reaction lines per language.

New windows:
- `36`: dedicated interior survey authorized.
- `37`: dedicated Lower Workings map entered.
- `38`: directed cribbing surveyed.
- `39`: Mutation-linked residue surveyed.
- `40`: deeper sealed-pressure edge surveyed.
- `41`: dedicated interior survey reported complete.

Same curated 14 NPCs per window:
Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

Resolver behavior after first-descent completion:
- interior stage `<=0 -> 35`
- `1 -> 36`
- `2 -> 37`
- `3 -> 38`
- `4 -> 39`
- `5 -> 40`
- `>=6 -> 41`

Before first descent completes, `GetStoryReactionWindowAlpha6744()` falls back to the complete 6.7.43 resolver chain.

## Hard lore locks retained
George before reveal MUST remain:
- observed Rank D;
- Non-Combatant;
- unrecruitable;
- no meaningful combat signature;
- no `Rank S`;
- no `The Last Blaster`;
- no explicit identification as the miner who sealed the chamber.

Future George reveal remains planned for 6.7.46, not 6.7.44/45.

Evelyn remains ordinary low Rank D healer/support during main story. Her secret remains postgame only.

Do NOT introduce exact `SECTOR 17` unless explicitly designed later.

## Gameplay safety locks retained
- First guaranteed Mutation remains the 10th eligible natural normal-monster defeat.
- Mutation exclusions remain bosses, story monsters, summons, mutation minions, Pelipper capture-protected monsters, and test-harness monsters.
- Pelipper capture ceasefire / protected-target safety remains intact.
- Five-PEOPLE total party ceiling remains intact.
- Story NPC roster ceiling remains 4/4.
- Entry Protocol READY and SURGE HIGH remain prerequisites.
- Multiplayer formation requirements remain adaptive to online Farmer count and configured people cap.
- No legacy fake-hide writer.
- Do not merge `main` until the user explicitly requests it and runtime behavior has been verified.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- DEDICATED DATA/LOCATIONS LOWER WORKINGS MAP: PASS
- TMX BACK/BUILDINGS/FRONT + VANILLA MINE TILESET: PASS
- RECORDED BREACH LOCATION + PERSISTENT TILE ANCHOR: PASS
- LEGACY SAVE ONE-TIME ANCHOR CALIBRATION: PASS
- SAFE RETURN ROUTE + EARLY EMERGENCY WITHDRAWAL: PASS
- THREE-ZONE 120-TICK INTERIOR SURVEY: PASS
- HOST-AUTHORITATIVE STORY PROGRESSION: PASS
- REACTION WINDOWS 0..41: PASS
- LOWER WORKINGS INTERIOR WINDOWS 36..41: PASS (14 NPCs each)
- CURATED MILESTONE REACTIONS: PASS (584 lines/language)
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- PELIPPER / MUTATION SAFETY CARRY-FORWARD: PASS
- FIVE-PEOPLE TOTAL + STORY SLOT 4 CEILING: PASS
- NO 6.7.44 BOSS / STORY MONSTER / COMBAT ESCALATION: PASS
- BINARY ACCEPTANCE: PASS
- Compiler: `0 Warning(s)`, `0 Error(s)`

## Archived checkpoint files
- `tools/materialize_alpha6744.py`
- `tools/materialize_alpha6744_fixups.py`
- `tools/build_alpha6744.py`
- `.github/workflows/team-up-alpha6-7-44-lower-workings-interior-survey.yml`
- `docs/alpha6744/LOWER_WORKINGS_INTERIOR_SURVEY_AUDIT_ALPHA6744.md`
- `docs/alpha6744/SMOKE_TEST_V0_2_ALPHA6_7_44_LOWER_WORKINGS_INTERIOR_SURVEY_VI.txt`
- `docs/alpha6744/BUILD_LOG_ALPHA6744.txt`

## Live smoke test
Use `docs/alpha6744/SMOKE_TEST_V0_2_ALPHA6_7_44_LOWER_WORKINGS_INTERIOR_SURVEY_VI.txt`.

Most important checks:
1. The TMX actually renders in-game and has no void/collision trap.
2. Legacy 6.7.43 save can calibrate ingress once and return to the same breach anchor.
3. Fresh 6.7.44 threshold crossing stores the anchor automatically.
4. All three 120-tick clue holds require the full adaptive formation.
5. Emergency withdrawal at stage 2/3/4 preserves stage and allows re-entry.
6. Stage 5 secured withdrawal + Guild report reaches stage 6/6.
7. Save/reload works inside the custom location.
8. Host/farmhand shared access works without farmhand story writes.
9. Reaction windows 36..41 are one-shot and skipped stale windows do not replay.
10. George/Evelyn/Pelipper/Mutation locks remain intact.

Useful commands:
- `teamup_lower_interior status|reset|stage 0-6`
- `teamup_lower_descent status|reset|stage 0-5`
- `teamup_story_reactions reset|status`
- `teamup_entry_protocol status`
- `teamup_surge_high status`
- `teamup_roster_story status`

## Next target: Alpha 6.7.45
Containment Chamber Escalation Encounter.

Goals:
- use the dedicated Lower Workings location established in 6.7.44;
- first major lower-workings encounter / chamber escalation;
- stronger causal evidence linking current Mutations to the old containment event;
- preserve the verified safe retreat route;
- bundle immediate NPC reactions in the same checkpoint;
- preserve Pelipper / Mutation eligibility safeguards;
- George remains anonymous and unrevealed until 6.7.46;
- no final boss yet.

Do not begin 6.7.45 as a stable continuation until the 6.7.44 live map/warp/save behavior has been tested or explicitly waived by the user.

## New-chat instruction
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-lower-workings-interior-survey. Bắt đầu 6.7.45.`
