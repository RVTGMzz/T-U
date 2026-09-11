# Team Up Alpha 6.7.38 - Surge HIGH Escalation / Story Slot 4 Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.38`
- Branch: `v0.2-alpha6-7-38-surge-high-slot4`
- Base handoff: `0af125e9fa7476a9c5428f733a05c95072192d19`
- Workflow input: `9b7e1166d5c501a7520f92ef6adef3e03ac6e487`
- CI-verified materialized source: `14a852b22b54dd1708c4ef1b8dc67abd02f0cfc9`
- CI run: `34566230413`
- CI job: `103158808778`
- Artifact ID: `10186107363`
- Artifact name: `team-up-alpha6-7-38-surge-high-slot4`
- Artifact wrapper digest: `sha256:ccbde4e94de8ec3a25832d5ad28d6aa90408362d7829a1b9c0e5ee070cfe03d5`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.38_SURGE_HIGH_SLOT4_TEST.zip`
- Inner ZIP SHA256: `69f94a8491b42f4066e4688d553d91caa99fe8a00a05e4f5d20b56af4c7f59f3`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: not merged
- Live verification: still required

## Implemented
Alpha 6.7.38 is the next gameplay checkpoint after Controlled Breach / First Entry and its reaction layer.

New persistent service:
- `Story/SurgeHighEscalationStoryService.cs`
- stage key: `Ronvotri.TeamUp/Story/SurgeHighEscalationStage`
- persistent HIGH flag: `Ronvotri.TeamUp/Story/SurgeHighConfirmed`
- complete stage: `4`
- field-team rule: at least `3` people physically in the same location, including at least `1` active Team Up NPC ally.
- stable HIGH confirmation hold: `180` ticks.

## Route
Prerequisite: `ControlledBreachAlpha6736.Stage >= 5`.

- Stage `0`: at Adventurer's Guild with a valid field team, Marlon briefs the team to determine whether the first-entry pressure pulse was transient or sustained. The recorded breach location must exist.
- Stage `1`: return to the exact saved MineShaft breach face from Alpha 6.7.36. Wrong MineShafts do not advance.
- Stage `2`: hold the valid field team continuously at that exact breach face for `180` ticks. Leaving the face, changing location, losing the required team, dialogue/menu ownership, or losing player-free state resets the runtime hold.
- Stage `3`: the stable interval confirms persistent `SURGE HIGH`; `SurgeHighConfirmed=1` is saved. Story roster is still expected to remain `3/4` at this point.
- Stage `4`: return to Marlon at the Guild with a valid field team. Marlon authorizes the stronger formation and `RosterProgressionAlpha6727.UnlockTo(..., 4, "surge-high-confirmed")` opens the final story NPC slot. Five PEOPLE total remains the formation hard cap.

## Narrative state at completion
Known after 6.7.38:
- the first-entry pressure response was not a one-time release;
- repeated pressure and shard-residue movement confirm a sustained escalation;
- Team Up now classifies the chapter state as `SURGE HIGH`;
- Marlon explicitly expands the story NPC allowance to `4/4` to prepare for the next phase;
- the deeper lower workings are still not a custom playable dungeon in this checkpoint;
- no final boss has spawned;
- the historical worker remains unnamed.

## Debug / diagnostics
Command:
- `teamup_surge_high status`
- `teamup_surge_high reset`
- `teamup_surge_high stage 0-4`

Diagnostic:
- `diagnostics/TeamUp_Surge_HIGH_Escalation_latest.txt`

Reset behavior:
- resets only Alpha 6.7.38 stage and HIGH flag;
- does not reduce Controlled Breach progress;
- does not reduce a story roster slot that has already been unlocked.

For a clean slot-unlock retest, use `teamup_roster_story setslots 3` before replaying the final report.

## Carry-forward / safety boundaries
- Controlled Breach 6.7.36 remains unchanged: exact face reuse, 180-tick opening, 120-tick threshold probe.
- Reaction windows remain exactly `0..22` with `318` curated reaction lines per language. Alpha 6.7.38 adds no new reaction windows yet.
- George remains observed Rank D, Non-Combatant, unrecruitable.
- No `GeorgeCombatRevealed` write.
- No `Rank S`, `The Last Blaster`, `George Mullner`, or `Keeper` reveal.
- Evelyn postgame secret remains untouched.
- Five-person total formation cap remains authoritative, including online Farmers.
- Pelipper capture protection remains intact.
- First guaranteed Mutation remains at the 10th eligible normal monster defeat.
- No final boss, lower-workings dungeon map, mutation ownership rewrite, or capture rewrite was introduced.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- SURGE HIGH ROUTE 0..4: PASS
- CONTROLLED BREACH COMPLETION PREREQUISITE: PASS
- EXACT RECORDED BREACH FACE REUSED: PASS
- THREE-PEOPLE FIELD TEAM + ACTIVE TEAM UP NPC REQUIREMENT: PASS
- 180-TICK SURGE HIGH CONFIRMATION HOLD: PASS
- TEAM-BREAK / WARP / MENU HOLD RESET: PASS
- PERSISTENT SURGE HIGH FLAG: PASS
- STORY NPC SLOT 4 UNLOCKED ONLY AFTER HIGH REPORT: PASS
- FIVE-PEOPLE TOTAL FORMATION CAP: PASS
- REACTION WINDOWS 0..22 CARRY-FORWARD: PASS, 318 lines/language
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- NO FINAL BOSS OR CUSTOM LOWER-WORKINGS MAP ADDED: PASS
- 6.7.23-6.7.37 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS
- BINARY ACCEPTANCE: PASS

## Archived in repo
- `docs/alpha6738/SURGE_HIGH_SLOT4_AUDIT_ALPHA6738.md`
- `docs/alpha6738/SMOKE_TEST_V0_2_ALPHA6_7_38_SURGE_HIGH_SLOT4_VI.txt`
- `docs/alpha6738/BUILD_LOG_ALPHA6738.txt`

## Live smoke test
1. Install the 6.7.38 test ZIP and load as host.
2. `teamup_breach status` should show `5/5`.
3. For a clean unlock test: `teamup_roster_story setslots 3`, then `teamup_surge_high reset`.
4. Enter AdventureGuild with a valid field team -> Surge HIGH stage `1`.
5. Enter the exact recorded breach MineShaft -> stage `2`.
6. Break formation or open a menu before the hold finishes -> runtime HIGH hold must reset.
7. Hold the valid team continuously for 180 ticks -> stage `3`, HIGH flag true.
8. At stage `3`, `teamup_roster_story status` must still report `3/4`.
9. Return to AdventureGuild with a valid field team -> stage `4/4`, story roster `4/4`.
10. Save and reload -> HIGH remains true and stage remains `4/4`.
11. George remains Rank D / Non-Combatant / unrecruitable.
12. In multiplayer, story allowance may read `4/4`, but effective NPC capacity must still respect the five-people total formation cap.

## Recommended next checkpoint
`Alpha 6.7.39: Surge HIGH Reactions` should be the next reaction-only checkpoint.

Recommended reaction mapping:
- current Surge HIGH stage `<=0` -> keep window `22`;
- stage `1` -> window `23`: HIGH-check briefing;
- stage `2` -> window `24`: breach-face HIGH reading started;
- stage `3` -> window `25`: SURGE HIGH confirmed;
- stage `4` -> window `26`: slot 4 authorized / major-chapter escalation acknowledged.

Using the same curated 14 NPCs would add `56` new lines per language and raise the exact reaction catalog from `318` to `374` lines per language.

The next gameplay checkpoint after that should build on SURGE HIGH and the full story roster without revealing George or spawning the final boss until the narrative ledger deliberately reaches those beats.
