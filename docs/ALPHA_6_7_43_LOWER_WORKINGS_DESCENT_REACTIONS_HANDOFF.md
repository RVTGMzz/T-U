# Team Up Alpha 6.7.43 - Lower Workings Descent Reactions Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.43`
- Branch: `v0.2-alpha6-7-43-lower-workings-descent-reactions`
- Exact base handoff: `abef203f179d8f73a04cabacd37360860a1437bb`
- Materializer commit: `e570f26d7ba2281ff0da045b895f0a178ab2f90b`
- Builder commit: `9d2b5892da1f56988a08f4543850b02c199319ed`
- Workflow / CI input: `bebd7eda2232caac0eab94ea0314f413c94d6451`
- CI-verified materialized source: `df836815e1fbe63c3ed8ede512c71a281b35b545`
- Successful CI run: `34613784319`
- Successful CI job: `103310534658`
- Artifact ID: `10270300653`
- Artifact name: `team-up-alpha6-7-43-lower-workings-descent-reactions`
- Artifact wrapper digest: `sha256:b503f5bb1c6e83b4651bf0303e8534bac7863b4ed72dcac72c10e48e7f5bfb6c`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.43_LOWER_WORKINGS_DESCENT_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `16ae5d354c9b84231a158240edf2f92f80d1279b1d500c05789a214e37e108f1`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: not merged
- Live verification: still required

## Implemented
Alpha 6.7.43 is the dialogue/reaction layer for Alpha 6.7.42 Lower Workings Descent / Threshold Crossing.

Reaction catalog now supports windows `0..35`.

New windows:
- `31`: first Lower Workings descent authorized at the Guild.
- `32`: full operational formation staged at the exact breach face and threshold line prepared.
- `33`: the no-pursuit threshold is crossed with formation intact.
- `34`: first interior threshold zone inspected; structural evidence supports deliberate emergency containment without identifying the historical worker.
- `35`: first descent is reported complete at the Guild.

The same curated 14 NPCs are supported in every new window:
Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

Added `70` new reaction lines per language. Total exact reaction catalog is now `500` lines per language with EN/VI parity.

## Resolver
`GetStoryReactionWindowAlpha6743()` preserves every previous reaction layer.

Until Entry Protocol is complete, it falls back to Alpha 6.7.41. Once Entry Protocol is complete, `LowerWorkingsDescentAlpha6742.Stage` maps as:
- `<=0 -> 30`
- `1 -> 31`
- `2 -> 32`
- `3 -> 33`
- `4 -> 34`
- `>=5 -> 35`

This prevents NPCs from reacting to a real descent before Alpha 6.7.42 has actually begun.

`ModEntry.Alpha6728.cs` now routes both NPC interaction and reaction diagnostics through the Alpha 6.7.43 resolver.

## Narrative boundaries
- Window 31 treats the operation as the first real descent, not another rehearsal.
- Window 32 focuses on staging, exact breach face, retreat lane, and full formation.
- Window 33 acknowledges the persisted threshold-crossed state but does not imply a dungeon or boss encounter.
- Window 34 reacts to directed cribbing, blast scoring, and collapse geometry that indicate deliberate emergency containment. It does not identify the worker responsible or the entity/problem behind the closure.
- Window 35 acknowledges a clean return and first-descent completion, preparing the project for a dedicated interior Lower Workings layer.

George remains only an experienced former miner in all new reactions. His dialogue never establishes hidden rank, unique combat identity, or his role in the old incident.

## Safety boundaries retained
- Alpha 6.7.42 gameplay remains unchanged.
- `LowerWorkingsDescentStage` remains `0..5`.
- Persistent `LowerWorkingsThresholdCrossed` and `LowerWorkingsFirstDescentComplete` flags remain authoritative.
- 120-tick threshold crossing and 180-tick first-interior inspection remain unchanged.
- Entry Protocol READY and adaptive full operational formation remain unchanged.
- Story NPC slots remain 4/4 and five PEOPLE total remains the hard formation cap.
- George remains observed Rank D / Non-Combatant / unrecruitable.
- No George combat reveal or `The Last Blaster` reveal.
- Evelyn postgame secret remains untouched.
- No custom Lower Workings map, final boss, monster ownership rewrite, capture rewrite, or roster unlock is introduced by 6.7.43.
- No `Sector 17` identifier.
- Pelipper capture safety remains intact.
- First guaranteed Mutation remains the 10th eligible natural normal-monster defeat.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- REACTION WINDOWS 0..35: PASS
- LOWER WORKINGS DESCENT WINDOWS 31..35: PASS, 14 NPCs each
- CURATED MILESTONE REACTIONS: PASS, 500 lines/language
- NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.43 WINDOW RESOLVER: PASS
- LOWER WORKINGS DESCENT 6.7.42 CARRY-FORWARD: PASS
- 120-TICK THRESHOLD CROSSING + PERSISTENT CROSSED STATE: PASS
- 180-TICK FIRST INTERIOR INSPECTION + PERSISTENT COMPLETION: PASS
- ENTRY PROTOCOL READY + ADAPTIVE FULL FORMATION CARRY-FORWARD: PASS
- SURGE HIGH + STORY SLOT 4 CARRY-FORWARD: PASS
- FIVE-PEOPLE TOTAL FORMATION CAP: PASS
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- PELIPPER / MUTATION SAFETY CARRY-FORWARD: PASS
- NO COMBAT, MAP, BOSS, MONSTER, CAPTURE OR ROSTER MECHANICS CHANGED: PASS
- BINARY ACCEPTANCE: PASS

Only GitHub Actions infrastructure warnings were present: Node 20 deprecation / forced Node 24, `punycode` deprecation, and `url.parse()` deprecation. C# compiler warnings remain zero.

## Archived in repo
- `tools/materialize_alpha6743.py`
- `tools/build_alpha6743.py`
- `.github/workflows/team-up-alpha6-7-43-lower-workings-descent-reactions.yml`
- `docs/alpha6743/LOWER_WORKINGS_DESCENT_REACTIONS_AUDIT_ALPHA6743.md`
- `docs/alpha6743/SMOKE_TEST_V0_2_ALPHA6_7_43_LOWER_WORKINGS_DESCENT_REACTIONS_VI.txt`
- `docs/alpha6743/BUILD_LOG_ALPHA6743.txt`

## Live smoke test
1. Install the 6.7.43 test ZIP and load as host.
2. `teamup_story_reactions reset`.
3. `teamup_lower_descent stage 1` -> reaction window `31`.
4. Talk to a supported NPC twice. First interaction should consume the special reaction; second should return to normal dialogue.
5. `teamup_lower_descent stage 2` -> window `32`.
6. `teamup_lower_descent stage 3` -> window `33`; reaction must not modify `ThresholdCrossed`.
7. `teamup_lower_descent stage 4` -> window `34`; dialogue may conclude deliberate containment but must not identify the historical worker.
8. `teamup_lower_descent stage 5` -> window `35`; reaction must not modify `FirstDescentComplete`.
9. Skip one reaction window and advance. The stale missed window must not replay.
10. George remains Rank D / Non-Combatant / unrecruitable and never reveals The Last Blaster.
11. Evelyn does not reveal her postgame secret.
12. `teamup_story_reactions status` should report the active window in catalog `0..35`.

Useful commands:
- `teamup_story_reactions reset|status`
- `teamup_lower_descent status|reset|stage 0-5`
- `teamup_entry_protocol status`
- `teamup_surge_high status`
- `teamup_roster_story status`

## Compressed roadmap from here
To avoid endless gameplay/reaction ping-pong, future checkpoints should bundle reactions into major gameplay checkpoints when practical.

Recommended remaining sequence:
1. `Alpha 6.7.44`: Dedicated Lower Workings chamber/map foundation + first interior survey + its immediate reactions in the same checkpoint.
2. `Alpha 6.7.45`: Containment-chamber escalation encounter + deeper evidence + bundled NPC reactions; still no George reveal.
3. `Alpha 6.7.46`: Old Coal Mine truth / George reveal as `The Last Blaster`, including Codex/profile transition and story reactions.
4. `Alpha 6.7.47`: Final containment boss / resolution framework + ending story beats.
5. `Alpha 6.7.48`: stabilization, compatibility regression pass, live-test fixes, packaging and stable-candidate preparation.

This is a planning target, not a promise that no extra hotfix checkpoint will ever be needed. If live testing exposes a significant runtime bug, a focused hotfix may still be required.
