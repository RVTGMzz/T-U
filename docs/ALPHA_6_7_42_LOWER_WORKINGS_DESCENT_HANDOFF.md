# Team Up Alpha 6.7.42 - Lower Workings Descent / Threshold Crossing Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.42`
- Branch: `v0.2-alpha6-7-42-lower-workings-descent`
- Exact base handoff: `3583609053073a790fcc51dbff4144d685081d2f`
- Materializer commit: `fc7625d144430e8c9605cc4850a392d836d58f60`
- Builder commit: `712b2132141d1e4ba6422cebf4f1e14e9ed68c50`
- Workflow / CI input: `1efd51346b6cc6f0f9b8a7c70154f1a996938c01`
- CI-verified materialized source: `6b7c3ab61fc2c046bf62530fd5e555f8abf63e45`
- Successful CI run: `34609004111`
- Successful CI job: `103294463589`
- Artifact ID: `10267751704`
- Artifact name: `team-up-alpha6-7-42-lower-workings-descent`
- Artifact wrapper digest: `sha256:f48e407b5edab38a50728e7345df216a97a7cfaeb4568e7a2b87837a83f87f24`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.42_LOWER_WORKINGS_DESCENT_TEST.zip`
- Inner ZIP SHA256: `ca4d2555db8563a5fe0794b3fbe047ee5e42f572d888187fe384447f0c254e62`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: not merged
- Live verification: still required

## Implemented
Alpha 6.7.42 is the first gameplay checkpoint where the HIGH-response team deliberately crosses the old controlled-breach threshold after the Entry Protocol is READY.

New service:
- `src/TeamUp/Story/LowerWorkingsDescentStoryService.cs`
- `StageKey = Ronvotri.TeamUp/Story/LowerWorkingsDescentStage`
- `ThresholdCrossedFlagKey = Ronvotri.TeamUp/Story/LowerWorkingsThresholdCrossed`
- `FirstDescentCompleteFlagKey = Ronvotri.TeamUp/Story/LowerWorkingsFirstDescentComplete`
- `CompleteStage = 5`
- `ThresholdCrossingTicksRequired = 120`
- `ThresholdInspectionTicksRequired = 180`

New wiring:
- `src/TeamUp/ModEntry.Alpha6742.cs`
- command: `teamup_lower_descent status|reset|stage 0-5`
- diagnostic: `diagnostics/TeamUp_Lower_Workings_Descent_latest.txt`

## Route
Prerequisites:
- Alpha 6.7.40 Entry Protocol stage must be complete, `4/4`.
- persistent `LowerWorkingsEntryProtocolReady` must be true.
- persistent SURGE HIGH must still be true.
- story NPC slots remain authorized to `4/4`.
- exact recorded controlled-breach MineShaft must still exist.

Stages:
1. Stage 0 at AdventureGuild -> Marlon authorizes the first real Lower Workings descent -> stage 1.
2. Stage 1 at the exact recorded breach face with the full operational formation -> crossing line established -> stage 2.
3. Stage 2 requires the full formation to remain intact for `120` continuous ticks. Completion persists `LowerWorkingsThresholdCrossed = 1` -> stage 3.
4. Stage 3 requires the same full formation to hold the first interior threshold zone for `180` continuous ticks. Completion reveals deliberate-containment evidence -> stage 4.
5. Stage 4 at AdventureGuild -> Marlon receives the report and persists `LowerWorkingsFirstDescentComplete = 1` -> stage 5/5.

Breaking formation, leaving the exact recorded MineShaft, warping, opening menu/dialogue/event presentation, or losing player-free state resets the active hold timer.

## Threshold representation
Alpha 6.7.42 deliberately does not pretend a dedicated Lower Workings dungeon map already exists.

The team crosses the narrow controlled opening in story/gameplay state while remaining anchored to the exact recorded breach MineShaft. The two persistent flags are designed so a later checkpoint can add a dedicated chamber/map layer and inherit first-entry progress cleanly.

This gives the project a real threshold-crossing state without baking in a fake or disposable map implementation.

## Physical evidence
The first interior threshold inspection now establishes stronger evidence that the historical collapse was deliberate emergency containment rather than an ordinary random cave-in:
- old timber cribbing was wedged to steer the collapse across the passage;
- blast cups form a deliberate fan rather than a random industrial pattern;
- charred support seams follow the closing line;
- the passage was shaped into a barrier under emergency pressure.

The evidence still does not identify the historical worker and does not identify what was being contained.

## Formation / safety carry-forward
The full operational formation continues to reuse Alpha 6.7.40 adaptive requirements rather than assuming four NPCs in multiplayer.

Normal five-person-cap examples:
- solo: `1 Farmer + 4 NPC`;
- 2-player co-op: `2 Farmers + 3 NPC`;
- 3-player co-op: `3 Farmers + 2 NPC`;
- 4-player co-op: `4 Farmers + 1 NPC`.

Five PEOPLE total remains the hard formation ceiling.

## Scope / spoiler boundaries retained
- No dedicated Lower Workings dungeon map yet.
- No fake warp into a nonexistent map.
- No final boss.
- No boss framework change.
- No monster spawn or monster ownership changes.
- No mutation/capture rewrite.
- No roster unlock or expansion.
- Story roster remains 4/4.
- George remains observed Rank D / Non-Combatant / unrecruitable.
- No George combat reveal and no `The Last Blaster` reveal.
- Historical worker remains unnamed.
- Evelyn postgame reveal remains untouched.
- No `Sector 17` identifier.
- Pelipper capture safety remains intact.
- First guaranteed Mutation remains the 10th eligible natural normal-monster defeat.
- Reaction catalog remains windows `0..30`, exactly `430` reaction lines per language.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- LOWER WORKINGS DESCENT ROUTE 0..5: PASS
- ENTRY PROTOCOL READY + SURGE HIGH + STORY SLOT4 PREREQUISITES: PASS
- EXACT RECORDED BREACH FACE REUSED: PASS
- ADAPTIVE FULL OPERATIONAL FORMATION REUSED: PASS
- 120-TICK THRESHOLD CROSSING: PASS
- PERSISTENT THRESHOLD-CROSSED STATE: PASS
- 180-TICK FIRST INTERIOR INSPECTION: PASS
- PERSISTENT FIRST-DESCENT-COMPLETE STATE: PASS
- FORMATION / WARP / FREE-STATE / MENU RESET: PASS
- DELIBERATE-CONTAINMENT EVIDENCE WITHOUT HISTORICAL-WORKER IDENTITY: PASS
- REACTION WINDOWS 0..30 CARRY-FORWARD: PASS, 430 lines/language
- FIVE-PEOPLE TOTAL FORMATION CAP: PASS
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- NO CUSTOM LOWER-WORKINGS MAP, FINAL BOSS OR ROSTER EXPANSION: PASS
- PELIPPER / MUTATION SAFETY CARRY-FORWARD: PASS
- BINARY ACCEPTANCE: PASS

Only GitHub Actions infrastructure warnings were present: Node 20 deprecation / forced Node 24, `punycode` deprecation, and `url.parse()` deprecation. C# compiler warnings remain zero.

## Archived in repo
- `tools/materialize_alpha6742.py`
- `tools/build_alpha6742.py`
- `.github/workflows/team-up-alpha6-7-42-lower-workings-descent.yml`
- `docs/alpha6742/LOWER_WORKINGS_DESCENT_AUDIT_ALPHA6742.md`
- `docs/alpha6742/SMOKE_TEST_V0_2_ALPHA6_7_42_LOWER_WORKINGS_DESCENT_VI.txt`
- `docs/alpha6742/BUILD_LOG_ALPHA6742.txt`

## Live smoke test
1. Install the 6.7.42 test ZIP and load as host.
2. `teamup_entry_protocol status` -> expect stage `4/4`, ready `True`.
3. `teamup_surge_high status` -> expect HIGH true.
4. `teamup_roster_story status` -> expect story slots `4/4`.
5. `teamup_lower_descent reset`.
6. Enter AdventureGuild -> stage `1`, receive Marlon first-descent order.
7. Bring the full operational formation to the exact recorded breach MineShaft -> stage `2`.
8. Hold formation for `120` continuous ticks -> stage `3`, `thresholdCrossed=True`.
9. Before 120 ticks, test breaking formation, warping, menu/dialogue, or leaving the correct shaft. Crossing hold must reset.
10. At stage `3`, hold formation for another `180` continuous ticks -> stage `4`, deliberate-containment evidence is presented.
11. Before 180 ticks, repeat break/reset tests. Inspection hold must reset.
12. Return to AdventureGuild -> stage `5/5`, `firstDescentComplete=True`.
13. Save/reload -> both threshold-crossed and first-descent-complete flags remain true.
14. Confirm no custom-map warp, boss, roster change, George reveal, or Evelyn reveal occurs.
15. `teamup_story_reactions status` remains on the 0..30 reaction catalog.

Useful commands:
- `teamup_lower_descent status|reset|stage 0-5`
- `teamup_entry_protocol status`
- `teamup_surge_high status`
- `teamup_roster_story status`
- `teamup_story_reactions status`

## Recommended next checkpoint
Recommended next checkpoint: `Alpha 6.7.43: Lower Workings Descent Reactions`.

Suggested reaction windows:
- `31`: first-descent order issued at the Guild.
- `32`: full formation staged at the exact breach face.
- `33`: threshold crossed for the first time.
- `34`: first interior threshold inspection reveals deliberate-containment evidence.
- `35`: first descent reported complete at the Guild.

Using the same curated 14 NPCs would add `70` new lines per language and grow the reaction catalog from `430` to `500` lines per language.

After those reactions, a later gameplay checkpoint can add the first dedicated Lower Workings chamber/map layer or a contained encounter without forcing the George reveal immediately.
