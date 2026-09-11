# Team Up Alpha 6.7.36 - Controlled Breach / First Entry Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.36`
- Branch: `v0.2-alpha6-7-36-controlled-breach-first-entry`
- Base handoff: `42682c94e8ae24545e7cd8f05ea1ca327f882373`
- Materializer commit: `73393ccb4bcaa52d98d3a651a77c8a47194d554c`
- Build-audit script commit: `28fc13f11e342350b2623b151cebb445c035aab8`
- Workflow input: `5cd4679f06cc52064d359911b6b0a3bb63aefad0`
- CI-verified materialized source: `56e19afdece1e6c8adaf0bd4a04d68a86e4aa6c0`
- CI source tree: `744a65b4996695b1a989a061d0d5007171f84bb3`
- Archived audit/smoke/build-log head before this handoff: `5ec54f04b903f564365d544a3bdf9619a2e7ff2a`
- CI run: `34563171981`
- CI job: `103149890357`
- CI conclusion: SUCCESS
- Artifact ID: `10185052982`
- Artifact name: `team-up-alpha6-7-36-controlled-breach-first-entry`
- Artifact size: `453378` bytes
- Artifact wrapper digest: `sha256:5d0cf1495b43724877519408483cca3cedfcc124adddab6151e28aa9bbb1c4fc`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.36_CONTROLLED_BREACH_FIRST_ENTRY_TEST.zip`
- Inner ZIP SHA256: `c82671d55d78873b9f4daa04f9a7e8d7aefd4032e77b4d0f3885eedad335f10c`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Live verification: still required

## Repository retention
Everything needed to recover this checkpoint is committed to GitHub:
- `tools/materialize_alpha6736.py`
- `tools/build_alpha6736.py`
- `.github/workflows/team-up-alpha6-7-36-controlled-breach-first-entry.yml`
- materialized `src/TeamUp/**` source at CI-verified commit above
- `docs/alpha6736/CONTROLLED_BREACH_FIRST_ENTRY_AUDIT_ALPHA6736.md`
- `docs/alpha6736/SMOKE_TEST_V0_2_ALPHA6_7_36_CONTROLLED_BREACH_FIRST_ENTRY_VI.txt`
- `docs/alpha6736/BUILD_LOG_ALPHA6736.txt`
- this handoff document

## Implemented gameplay
New persistent service:
- `Story/ControlledBreachFirstEntryStoryService.cs`
- `StageKey = Ronvotri.TeamUp/Story/ControlledBreachFirstEntryStage`
- `BreachLocationKey = Ronvotri.TeamUp/Story/ControlledBreachLocation`
- `CompleteStage = 5`
- `MinimumFieldPeople = 3`
- `MinimumActiveNpcAllies = 1`
- `BreachHoldTicksRequired = 180`
- `EntryProbeTicksRequired = 120`

Prerequisite:
- `SealedCorridorApproachStoryService.CompleteStage == 4`

The exact Alpha 6.7.34 `SurveyLocationKey` is reused as the breach target. The player cannot substitute an arbitrary MineShaft.

Route:
1. Stage 0 at AdventureGuild with a valid field team: Marlon gives the controlled-breach briefing. The recorded survey face is copied into `BreachLocationKey`. Stage -> 1.
2. Stage 1: return to the exact recorded MineShaft face with a valid field team. Wrong MineShaft is rejected. Stage -> 2.
3. Stage 2: hold a valid same-location field team continuously for 180 ticks. Team break, warp, menu, dialogue, or wrong location resets the runtime hold. Completion opens only a narrow braced gap. Stage -> 3.
4. Stage 3: after the opening dialogue closes, hold the valid field team continuously for another 120 ticks. Completion performs a short first-entry threshold probe only. Stage -> 4.
5. Stage 4: return to AdventureGuild with the valid field team. Marlon receives the report. Stage -> 5/5.

Command:
- `teamup_breach status`
- `teamup_breach reset`
- `teamup_breach stage 0-5`

Diagnostic:
- `diagnostics/TeamUp_Controlled_Breach_First_Entry_latest.txt`

## Story result
The controlled opening reveals only the immediate threshold behind the sealed face:
- an old maintenance throat rather than a natural cave;
- rusted rail brackets and cut timber sockets;
- the familiar inward-burn trace;
- fine black shard residue near the threshold that appears recently disturbed;
- a deeper pressure pulse after the seal is opened.

Nothing attacks and no creature is directly seen in this checkpoint. The party withdraws without widening the breach. Marlon concludes that the space cannot be treated as dead history and that the changed pressure response is the next problem.

## Explicit boundary
Alpha 6.7.36 deliberately does NOT claim a new custom lower-workings map. The first entry is a short threshold probe represented at the existing MineShaft survey face. A real explorable lower-workings map can be implemented later as its own content checkpoint rather than silently faked here.

Also unchanged:
- no boss;
- no Surge HIGH transition yet;
- story NPC slot 4 remains locked, roster stays 3/4;
- five-person total formation cap unchanged;
- George remains observed Rank D / Non-Combatant / unrecruitable;
- no George reveal flag write;
- no `The Last Blaster` or historical worker identity reveal;
- Evelyn postgame secret untouched;
- reaction windows remain 0..17 and exactly 248 reaction lines per language;
- first guaranteed mutation threshold remains 10;
- Pelipper capture ceasefire/provider safety remains intact;
- no legacy fake-hide writer;
- no combat/monster/capture/map-ownership changes.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- CONTROLLED BREACH / FIRST ENTRY ROUTE 0..5: PASS
- SEALED CORRIDOR COMPLETION PREREQUISITE: PASS
- RECORDED SURVEY FACE REUSED AS BREACH LOCATION: PASS
- THREE-PEOPLE FIELD TEAM + ACTIVE TEAM UP NPC REQUIREMENT: PASS
- 180-TICK CONTROLLED OPENING HOLD: PASS
- 120-TICK FIRST-ENTRY THRESHOLD PROBE HOLD: PASS
- TEAM-BREAK / WARP / MENU HOLD RESET: PASS
- REACTION WINDOWS 0..17 CARRY-FORWARD: PASS (248 lines/language)
- STORY NPC SLOT 4 REMAINS LOCKED: PASS
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- NO CUSTOM LOWER-WORKINGS MAP OR BOSS CLAIMED: PASS
- 6.7.23-6.7.35 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS
- BINARY ACCEPTANCE: PASS

## Live smoke test
Precondition:
- `teamup_corridor status` should be stage 4/4.

Test:
1. `teamup_breach reset`.
2. Enter Guild with fewer than 3 people -> must NOT advance.
3. Enter Guild with Farmer + 2 active Team Up NPCs -> stage 1 and recorded breach location should be the Alpha 6.7.34 survey face.
4. Enter a different MineShaft -> must NOT advance.
5. Enter the exact recorded MineShaft -> stage 2.
6. Hold the valid team there for about 180 ticks -> stage 3 and controlled-opening dialogue.
7. During stage 2, deliberately break team/warp/open menu and confirm the hold resets rather than completing early.
8. Close the opening dialogue; keep the valid field team together about 120 ticks -> stage 4 and first-entry threshold-probe dialogue.
9. Return to Guild with the valid field team -> stage 5/5.
10. `teamup_roster_story status` remains 3/4.
11. George remains Rank D / Non-Combatant / unrecruitable; Evelyn has no postgame reveal.

## Recommended next checkpoint
Alpha 6.7.37 should be a dialogue-only reaction layer for the new breach route, preserving the alternating gameplay/reaction cadence.

Recommended reaction mapping:
- existing stage 0 of breach continues to use reaction window 17;
- breach stage 1 -> window 18: controlled-breach briefing;
- breach stage 2 -> window 19: team reaches exact sealed face / braces ready;
- breach stage 3 -> window 20: narrow controlled opening achieved / first pressure pulse;
- breach stage 4 -> window 21: first-entry threshold probe complete;
- breach stage 5 -> window 22: Marlon receives report / deeper response confirmed.

With the same 14 curated NPCs this would add 70 lines per language, taking the exact reaction catalog from 248 to 318 lines per language.

After that, a later major gameplay checkpoint can escalate the pressure response toward the planned Surge HIGH milestone and only then consider unlocking story NPC slot 4. George's true combat reveal remains later still.
