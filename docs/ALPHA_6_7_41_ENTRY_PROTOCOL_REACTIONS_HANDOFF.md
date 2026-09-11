# Team Up Alpha 6.7.41 - Entry Protocol Reactions Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.41`
- Branch: `v0.2-alpha6-7-41-entry-protocol-reactions`
- Exact base handoff: `45b66c0243ee1cda881d060873d52f04175f9a3f`
- Materializer commit: `32ec8485e015ed33badcf1e7c68768af8636dc4e`
- Builder commit: `bb2497695f2f0df24820650ea71fc01fe68a96b7`
- Workflow / CI input: `411ce9261f47981beebcc591655cbb0cdd472e2d`
- CI-verified materialized source: `1b2b6820784ea1ac9f3e2da8d70776d9efd6a810`
- Successful CI run: `34587730084`
- Successful CI job: `103225694308`
- Artifact ID: `10194306196`
- Artifact name: `team-up-alpha6-7-41-entry-protocol-reactions`
- Artifact wrapper digest: `sha256:601098fb559b84f402c83ed14af98f7afd80b5d9e5b507cd3ee445a2c29f56ea`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.41_ENTRY_PROTOCOL_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `53944e7edc0441de48502d7774900c69092b21c7118fabdde724aa6e98de91f2`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: not merged
- Live verification: still required

## Implemented
Alpha 6.7.41 is a dialogue-only reaction checkpoint layered on Alpha 6.7.40 HIGH Response Preparation / Lower Workings Entry Protocol.

Reaction catalog now supports windows `0..30`.

New windows:
- `27`: HIGH response / Entry Protocol briefing.
- `28`: full operational formation assembled and staging line established at the recorded breach face.
- `29`: 240-tick readiness / withdrawal-line drill validated.
- `30`: Lower Workings Entry Protocol marked READY at the Guild.

The same curated 14 NPCs are supported in every new window:
Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

Added `56` new reaction lines per language. Total exact reaction catalog is now `430` lines per language with EN/VI parity.

## Resolver
`GetStoryReactionWindowAlpha6741()` preserves all previous routing.

If Surge HIGH is not complete, it falls back to Alpha 6.7.39. Once Surge HIGH is complete, `EntryProtocolAlpha6740.Stage` maps as:
- `<=0 -> 26`
- `1 -> 27`
- `2 -> 28`
- `3 -> 29`
- `>=4 -> 30`

This prevents characters from reacting to the entry protocol before Alpha 6.7.40 actually begins.

`ModEntry.Alpha6728.cs` now uses the 6.7.41 resolver for NPC interaction and reaction diagnostics.

## Narrative boundaries
- Window 27 reacts to Marlon formalizing the HIGH response around staging, withdrawal order, rear anchor, abort conditions, and a no-pursuit threshold.
- Window 28 reflects the full operational formation being assembled at the exact breach face and the staging line being established.
- Window 29 acknowledges only that the team held the readiness drill for the full interval and proved the fallback line workable. It does not claim the lower workings are safe.
- Window 30 acknowledges that the Entry Protocol is READY. It explicitly does not claim that a real descent has begun.

George remains only an experienced former miner in these reactions. He gives ordinary mine-safety observations without revealing hidden rank, combat identity, or the historical incident role.

## Safety boundaries retained
- Alpha 6.7.40 gameplay is unchanged.
- `LowerWorkingsEntryProtocolStage` remains `0..4`.
- `Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady` remains the persistent READY flag.
- 240-tick readiness hold remains unchanged.
- Adaptive full operational formation remains unchanged:
  - solo normal cap: 1 Farmer + 4 NPC;
  - two-player co-op: 2 Farmers + 3 NPC;
  - three-player co-op: 3 Farmers + 2 NPC;
  - four-player co-op: 4 Farmers + 1 NPC.
- Story NPC slots remain 4/4 from Alpha 6.7.38; Alpha 6.7.41 performs no roster unlock.
- Five PEOPLE total remains the hard formation ceiling.
- George remains observed Rank D / Non-Combatant / unrecruitable.
- No `GeorgeCombatRevealed` write.
- No `Rank S`, `The Last Blaster`, `George Mullner`, `Keeper`, or `Sector 17` leak in new dialogue.
- Evelyn postgame secret remains untouched.
- Pelipper capture safety remains intact.
- First guaranteed Mutation remains the 10th eligible natural normal-monster defeat.
- No combat, map, boss, monster, mutation, capture, ownership, or roster mechanics were changed by 6.7.41.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- REACTION WINDOWS 0..30: PASS
- ENTRY PROTOCOL WINDOWS 27..30: PASS, 14 NPCs each
- CURATED MILESTONE REACTIONS: PASS, 430 lines/language
- NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.41 WINDOW RESOLVER: PASS
- ENTRY PROTOCOL 6.7.40 CARRY-FORWARD: PASS
- ADAPTIVE FULL FORMATION CARRY-FORWARD: PASS
- 240-TICK READINESS HOLD + PERSISTENT READY FLAG: PASS
- SURGE HIGH + STORY SLOT 4 CARRY-FORWARD: PASS
- FIVE-PEOPLE TOTAL FORMATION CAP: PASS
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- PELIPPER / MUTATION SAFETY CARRY-FORWARD: PASS
- NO COMBAT, MAP, BOSS, MONSTER, CAPTURE OR ROSTER MECHANICS CHANGED: PASS
- BINARY ACCEPTANCE: PASS

Only GitHub Actions infrastructure warnings were present: Node 20 deprecation / forced Node 24, `punycode` deprecation, and `url.parse()` deprecation. C# compiler warnings remain zero.

## Archived in repo
- `tools/materialize_alpha6741.py`
- `tools/build_alpha6741.py`
- `.github/workflows/team-up-alpha6-7-41-entry-protocol-reactions.yml`
- `docs/alpha6741/ENTRY_PROTOCOL_REACTIONS_AUDIT_ALPHA6741.md`
- `docs/alpha6741/SMOKE_TEST_V0_2_ALPHA6_7_41_ENTRY_PROTOCOL_REACTIONS_VI.txt`
- `docs/alpha6741/BUILD_LOG_ALPHA6741.txt`

## Live smoke test
1. Install the 6.7.41 test ZIP and load as host.
2. `teamup_story_reactions reset`.
3. `teamup_entry_protocol stage 1` -> reaction window `27`.
4. Talk to a supported NPC twice. First interaction should consume the special reaction; the second should return to normal interaction.
5. `teamup_entry_protocol stage 2` -> window `28`.
6. `teamup_entry_protocol stage 3` -> window `29`.
7. Confirm a reaction does not set READY or mutate the Entry Protocol stage.
8. `teamup_entry_protocol stage 4` -> window `30`.
9. Skip one window and advance. A stale missed reaction must not replay later.
10. George remains Rank D / Non-Combatant / unrecruitable and never reveals The Last Blaster.
11. Evelyn does not reveal her postgame secret.
12. `teamup_story_reactions status` should report the active window within the `0..30` catalog.
13. `teamup_entry_protocol status` remains authoritative for stage and READY state.

Useful commands:
- `teamup_story_reactions reset|status`
- `teamup_entry_protocol status|reset|stage 0-4`
- `teamup_surge_high status`
- `teamup_roster_story status`
- diagnostic: `diagnostics/TeamUp_Milestone_Reactions_latest.txt`

## Recommended next checkpoint
Recommended next gameplay checkpoint: `Alpha 6.7.42: Lower Workings Descent / Threshold Crossing`.

Suggested intent:
- Require completed Entry Protocol with persistent READY flag.
- Require the full operational formation appropriate to the current Farmer count and five-person hard cap.
- Return to the exact recorded breach face and deliberately cross the no-pursuit threshold for the first real lower-workings operation.
- Persist a threshold-crossed / first-descent state so the next checkpoint can build a dedicated lower-workings chamber or map layer cleanly.
- Introduce stronger physical evidence that the old collapse was deliberate containment, without identifying the historical worker.
- Do not reveal George, do not spawn the final boss, and do not introduce `Sector 17`.
- Preserve Pelipper capture safety, mutation exclusions, Surge HIGH ownership rules, and the current 4/4 story roster ceiling.
