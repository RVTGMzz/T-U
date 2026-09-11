# Team Up Alpha 6.7.39 - Surge HIGH Reactions Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.39`
- Branch: `v0.2-alpha6-7-39-surge-high-reactions`
- Exact base handoff: `13dc62426f6c3778fef9cc949cfe5735a258a290`
- Initial workflow input: `bb716318a4c01558805e9406bc364171faf0f9f4`
- Initial CI run: `34569188177`
- Initial CI job: `103167444616`
- Initial result: failed before source materialization because the startup-message preflight expected `Surge HIGH escalation / story slot 4 layer active.` while the real 6.7.38 source used `Surge HIGH escalation and story slot 4 layer active.` No materialized source commit or artifact was produced by that failed run.
- Corrected workflow input: `4d756afff32420f7aa4653d5da8d3f6758567af9`
- CI-verified materialized source: `fccc160d650b1cd7deb46c32613a5f34b687b29f`
- Successful CI run: `34569313137`
- Successful CI job: `103167817613`
- Artifact ID: `10187171169`
- Artifact name: `team-up-alpha6-7-39-surge-high-reactions`
- Artifact wrapper digest: `sha256:4d389ddbabe3ac9ae2a9e21341cb70074be184c572d581386bfd954ff4514a3e`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.39_SURGE_HIGH_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `64e4ac3869619885bf3d4d5a1445dc21bb9765ddbbc814cb238b1a7264b92b08`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: not merged
- Live verification: still required

## Implemented
Alpha 6.7.39 is a dialogue-only checkpoint layered on Alpha 6.7.38 Surge HIGH Escalation / Story Slot 4.

Reaction catalog now supports windows `0..26`.

New windows:
- `23`: HIGH-check briefing from Marlon after the completed first-entry probe.
- `24`: the field team reaches the exact recorded breach face and begins the stable HIGH reading.
- `25`: the 180-tick controlled interval confirms persistent `SURGE HIGH`.
- `26`: the HIGH report is acknowledged at the Guild and story NPC slot 4 authorization is recognized.

The same curated 14 NPCs are supported in each new window:
Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

Added `56` new reaction lines per language. Total exact reaction catalog is now `374` lines per language with EN/VI parity.

## Resolver
`GetStoryReactionWindowAlpha6739()` preserves all previous routing.

If Controlled Breach is not complete, it falls back to Alpha 6.7.37. Once Controlled Breach is complete, `SurgeHighAlpha6738.Stage` maps as:
- `<=0 -> 22`
- `1 -> 23`
- `2 -> 24`
- `3 -> 25`
- `>=4 -> 26`

This prevents characters from reacting to HIGH before the 6.7.38 HIGH operation actually starts.

`ModEntry.Alpha6728.cs` now uses the 6.7.39 resolver for NPC interaction and reaction diagnostics.

## Narrative boundaries
- Window 23 treats the first-entry pressure pulse as something that must be re-measured, not automatically a major threat.
- Window 24 reflects a stable measurement operation at the known breach face.
- Window 25 acknowledges `SURGE HIGH` only as an operational risk classification. It does not identify the cause, boss, entity, or historical worker.
- Window 26 acknowledges the stronger formation and story slot 4 authorization without claiming the next deeper operation has begun.

George remains an ordinary experienced former miner in all four new windows. No secret-hero wording or historical identity reveal is present.

## Safety boundaries retained
- Alpha 6.7.38 gameplay is unchanged.
- `SurgeHighEscalationStage` remains 0..4.
- `Ronvotri.TeamUp/Story/SurgeHighConfirmed` remains the persistent HIGH flag.
- 180-tick HIGH confirmation hold remains unchanged.
- HIGH route still requires at least 3 people at the same location including at least 1 active Team Up NPC ally.
- Exact recorded breach face from 6.7.36 is still reused.
- Story slot 4 remains an Alpha 6.7.38 gameplay unlock, not a reaction side effect.
- Five PEOPLE total party cap remains authoritative.
- George remains observed Rank D / Non-Combatant / unrecruitable.
- No `GeorgeCombatRevealed` write.
- No `Rank S`, `The Last Blaster`, `George Mullner`, `Keeper`, or `Sector 17` leak in new dialogue.
- Evelyn postgame secret remains untouched.
- Pelipper capture safety remains intact.
- First guaranteed Mutation remains the 10th eligible natural normal-monster defeat.
- No combat, monster, mutation, capture, map, boss, or roster mechanics were added or changed by 6.7.39.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- REACTION WINDOWS 0..26: PASS
- SURGE HIGH WINDOWS 23..26: PASS, 14 NPCs each
- CURATED MILESTONE REACTIONS: PASS, 374 lines/language
- NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.39 WINDOW RESOLVER: PASS
- SURGE HIGH 6.7.38 CARRY-FORWARD: PASS
- PERSISTENT HIGH + 180-TICK CONFIRMATION HOLD: PASS
- STORY NPC SLOT 4 AUTHORIZATION CARRY-FORWARD: PASS
- FIVE-PEOPLE TOTAL FORMATION CAP: PASS
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- 6.7.23-6.7.38 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS
- BINARY ACCEPTANCE: PASS

## Rebuild / replay note
The first materializer run exposed a harmless startup-message literal mismatch. The successful workflow contains a preflight replacement before invoking `tools/materialize_alpha6739.py`.

A standalone replay wrapper is also preserved at:
- `tools/replay_alpha6739_from_6738.py`

When reconstructing from the exact 6.7.38 handoff base, use the workflow or the replay wrapper rather than invoking the raw materializer without the preflight fix.

## Archived in repo
- `docs/alpha6739/SURGE_HIGH_REACTIONS_AUDIT_ALPHA6739.md`
- `docs/alpha6739/SMOKE_TEST_V0_2_ALPHA6_7_39_SURGE_HIGH_REACTIONS_VI.txt`
- `docs/alpha6739/BUILD_LOG_ALPHA6739.txt`

## Live smoke test
1. Install the 6.7.39 test ZIP and load as host.
2. `teamup_story_reactions reset`.
3. `teamup_surge_high stage 1` -> reaction window `23`.
4. Talk to a supported NPC -> one special reaction only; second talk returns to normal interaction.
5. `teamup_surge_high stage 2` -> window `24`.
6. `teamup_surge_high stage 3` -> window `25`.
7. Confirm HIGH remains true. Reactions must not alter HIGH stage or roster slots.
8. `teamup_surge_high stage 4` -> window `26`.
9. Skip a window and advance -> stale missed reaction must not replay later.
10. George remains Rank D / Non-Combatant / unrecruitable.
11. Evelyn does not reveal postgame secret.
12. With normal story flow, stage 4 still corresponds to story allowance `4/4`, while multiplayer effective NPC capacity continues to respect the five-people hard cap.

Useful commands:
- `teamup_story_reactions reset|status`
- `teamup_surge_high status|reset|stage 0-4`
- `teamup_roster_story status`
- diagnostic: `diagnostics/TeamUp_Milestone_Reactions_latest.txt`

## Recommended next checkpoint
The next gameplay checkpoint should build on `SURGE HIGH` and the full story roster without revealing George or spawning the final boss yet.

Recommended direction: `Alpha 6.7.40: HIGH Response Preparation / Lower Workings Entry Protocol`.

Suggested intent:
- Marlon converts the HIGH classification into a concrete deeper-operation preparation plan.
- Require a full operational field formation appropriate to the five-person cap rather than silently assuming four NPCs in multiplayer.
- Establish containment / withdrawal criteria before a deeper descent.
- Keep the historical worker unnamed and George pre-reveal locked.
- Do not reveal the final boss, The Last Blaster, or Evelyn's postgame identity yet.
- Preserve Pelipper capture safety, mutation exclusions, and existing Surge ownership rules.
