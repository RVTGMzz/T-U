# Team Up Alpha 6.7.40 - HIGH Response Preparation / Lower Workings Entry Protocol Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.40`
- Branch: `v0.2-alpha6-7-40-high-response-preparation`
- Exact base handoff: `ce58f27b48b675bee25e741259bdf2884a596376`
- Materializer commit: `b539336f7460d09ac7bbb913efcf277daaf09e07`
- Builder commit: `f0b1e4c298ed1685cebc2d69ce65b6745c1abb3f`
- Workflow / CI input: `ed08d1ed76d9e3b89e87b8855de7be3ce74e8d63`
- CI-verified materialized source: `463d6027f73797f508abaf4ae5cc206e2d390c57`
- Successful CI run: `34570826790`
- Successful CI job: `103172354026`
- Artifact ID: `10187702818`
- Artifact name: `team-up-alpha6-7-40-high-response-entry-protocol`
- Artifact wrapper digest: `sha256:8605db0741ba146935e0a7f7e689625ee486e444323c9982de25f2cd3f4e8e4b`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.40_HIGH_RESPONSE_ENTRY_PROTOCOL_TEST.zip`
- Inner ZIP SHA256: `0dfaaf60da00c50a32ee514f62a27ee40d944b5a9e484173f786b9eeca216c69`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: not merged
- Live verification: still required

## Implemented
Alpha 6.7.40 turns the completed SURGE HIGH escalation and story slot 4 authorization into a concrete preparation protocol for a later lower-workings descent.

New service:
- `src/TeamUp/Story/LowerWorkingsEntryProtocolStoryService.cs`
- `StageKey = Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolStage`
- `ProtocolReadyFlagKey = Ronvotri.TeamUp/Story/LowerWorkingsEntryProtocolReady`
- `CompleteStage = 4`
- `ReadinessHoldTicksRequired = 240`

New wiring:
- `src/TeamUp/ModEntry.Alpha6740.cs`
- command: `teamup_entry_protocol status|reset|stage 0-4`
- diagnostic: `diagnostics/TeamUp_Lower_Workings_Entry_Protocol_latest.txt`

## Route
Prerequisites:
- Alpha 6.7.38 Surge HIGH stage must be complete, `4/4`.
- persistent `SurgeHighConfirmed` must still be true.
- story NPC slots must be authorized to `4/4`.
- exact recorded controlled-breach MineShaft must still exist.

Stages:
1. Stage 0 at AdventureGuild -> Marlon issues the HIGH Response Entry Protocol. No descent occurs.
2. Stage 1 at the exact recorded breach MineShaft with the full operational formation -> staging line, withdrawal order, rear anchor, and no-pursuit threshold are established -> stage 2.
3. Stage 2 requires the full formation to remain intact for `240` continuous ticks while the fallback line is validated. Breaking formation, leaving the breach face, warping, losing player-free state, or presentation/menu ownership resets the timer. Completion -> stage 3.
4. Stage 3 at AdventureGuild -> Marlon approves the drill and persists `LowerWorkingsEntryProtocolReady = 1` -> stage 4/4.

This checkpoint explicitly validates preparation only. It does not move the player into a new lower-workings map.

## Adaptive full operational formation
The protocol does not blindly require four NPCs in multiplayer.

Required field people:
`min(configured people cap, online Farmers + unlocked story NPC slots)`

Required active NPC allies:
`min(unlocked story NPC slots, max(0, configured people cap - online Farmers))`

With the normal five-person cap and story slots at 4/4:
- Solo: `1 Farmer + 4 NPC = 5 people`.
- Two-player co-op: `2 Farmers + 3 NPC = 5 people`.
- Three-player co-op: `3 Farmers + 2 NPC = 5 people`.
- Four-player co-op: `4 Farmers + 1 NPC = 5 people`.

If the configured people cap is lower than 5, the protocol respects that cap. The global five-people hard ceiling remains authoritative.

## Narrative / operational rules
Marlon's protocol now establishes four explicit safety concepts before a real descent:
- staging line on the modern side of the breach;
- clear withdrawal order and route;
- rear anchor watching the structural supports;
- no-pursuit threshold and immediate abort conditions.

Abort logic represented by gameplay includes:
- broken full formation;
- wrong MineShaft / leaving the recorded breach face;
- warp;
- menu/dialogue/event ownership;
- player no longer being free during the readiness hold.

SURGE HIGH remains active, but the 240-tick drill confirms only that the formation and fallback plan are viable. It does not identify the source of HIGH.

## Scope / spoiler boundaries retained
- No final boss.
- No custom lower-workings dungeon map.
- No monster spawn or monster ownership changes.
- No mutation/capture rewrite.
- No roster unlock is performed by Alpha 6.7.40.
- Story roster remains 4/4 from Alpha 6.7.38.
- Five PEOPLE total remains the hard formation cap.
- George remains observed Rank D / Non-Combatant / unrecruitable.
- No George combat reveal and no `The Last Blaster` reveal.
- Historical worker remains unnamed.
- Evelyn postgame reveal remains untouched.
- No `Sector 17` identifier.
- Pelipper capture safety remains intact.
- First guaranteed Mutation remains the 10th eligible natural normal-monster defeat.
- Reaction catalog remains windows `0..26`, exactly `374` reaction lines per language.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- HIGH RESPONSE ENTRY PROTOCOL ROUTE 0..4: PASS
- SURGE HIGH + SLOT4 PREREQUISITES: PASS
- EXACT RECORDED BREACH FACE REUSED: PASS
- ADAPTIVE FULL FORMATION RULE: PASS
- SOLO 1 FARMER + 4 NPC / COOP 2 FARMERS + 3 NPC MODEL: PASS
- 240-TICK READINESS HOLD: PASS
- FORMATION / WARP / FREE-STATE / MENU RESET: PASS
- PERSISTENT ENTRY PROTOCOL READY FLAG: PASS
- NO ROSTER UNLOCK SIDE EFFECT: PASS
- FIVE-PEOPLE TOTAL FORMATION CAP: PASS
- REACTION WINDOWS 0..26 CARRY-FORWARD: PASS, 374 lines/language
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- NO FINAL BOSS OR CUSTOM LOWER-WORKINGS MAP ADDED: PASS
- 6.7.23-6.7.39 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS
- BINARY ACCEPTANCE: PASS

Only GitHub Actions infrastructure warnings were present: Node 20 deprecation/forced Node 24, `punycode` deprecation, and `url.parse()` deprecation. C# compiler warnings remain zero.

## Archived in repo
- `tools/materialize_alpha6740.py`
- `tools/build_alpha6740.py`
- `.github/workflows/team-up-alpha6-7-40-high-response-entry-protocol.yml`
- `docs/alpha6740/HIGH_RESPONSE_ENTRY_PROTOCOL_AUDIT_ALPHA6740.md`
- `docs/alpha6740/SMOKE_TEST_V0_2_ALPHA6_7_40_HIGH_RESPONSE_ENTRY_PROTOCOL_VI.txt`
- `docs/alpha6740/BUILD_LOG_ALPHA6740.txt`

## Live smoke test
1. Install the 6.7.40 test ZIP and load as host.
2. `teamup_surge_high status` -> expect stage `4/4`, `high=True`.
3. `teamup_roster_story status` -> expect story slots `4/4`.
4. `teamup_entry_protocol reset`.
5. Enter AdventureGuild -> stage `1`, receive Marlon protocol briefing.
6. Bring the full operational formation to the exact recorded breach MineShaft. With normal solo cap this means Farmer + 4 active NPC allies. In two-player co-op it means 2 Farmers + 3 active NPC allies.
7. Arrival with the correct full formation -> stage `2`.
8. Hold the formation for `240` continuous ticks -> stage `3`.
9. Before 240 ticks, test breaking formation, warping, opening a menu/dialogue, or leaving the correct shaft. The hold must restart.
10. Return to AdventureGuild -> stage `4/4`, `ProtocolReady=True`.
11. Save and reload -> READY must remain true.
12. Confirm no new roster slot, no boss, and no custom map was introduced.
13. George remains Rank D / Non-Combatant / unrecruitable. Evelyn remains unrevealed.
14. `teamup_story_reactions status` remains on the 0..26 catalog.

Useful commands:
- `teamup_entry_protocol status|reset|stage 0-4`
- `teamup_surge_high status`
- `teamup_roster_story status`
- `teamup_story_reactions status`

## Recommended next checkpoint
Recommended next checkpoint: `Alpha 6.7.41: Entry Protocol Reactions`.

Suggested reaction windows:
- `27`: HIGH response / entry-protocol briefing.
- `28`: full formation assembled and staging line established.
- `29`: 240-tick readiness / withdrawal-line drill validated.
- `30`: Lower Workings Entry Protocol marked READY.

Using the same curated 14 NPCs would add `56` new lines per language and grow the reaction catalog from `374` to `430` lines per language.

After that, a gameplay checkpoint can begin the first real lower-workings descent without forcing the George reveal immediately.
