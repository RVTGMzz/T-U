# Team Up Alpha 6.7.35 - Sealed Corridor Approach Reactions Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.35`
- Branch: `v0.2-alpha6-7-35-sealed-corridor-reactions`
- Base handoff: `1df97ad5bf1bbf5a0cb5a87ae663559b4a9f738d`
- Workflow input: `6404e0bcac9df011906c0d3262c006605cd54703`
- CI-verified materialized source: `be5badc`
- CI run: `34562081480`
- CI job: `103146733610`
- Artifact ID: `10184678246`
- Artifact name: `team-up-alpha6-7-35-sealed-corridor-reactions`
- Artifact wrapper digest: `sha256:2d4a5b8abcff606b4019906a9dffd41be4655d4c95671d6c704c3ac65a6c7b84`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.35_SEALED_CORRIDOR_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `244b42ebd339bce1d1fc6905e137578b373058b344401e196d52afdeedb43ab5`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: not merged
- Live verification: still required

## Implemented
Alpha 6.7.35 is a dialogue-only checkpoint layered on Alpha 6.7.34 Sealed Corridor Approach.

Reaction catalog now supports windows `0..17`.

New windows:
- `14`: pressure-survey briefing after Marlon starts the sealed-corridor approach.
- `15`: survey face marked after the team reaches the candidate MineShaft face.
- `16`: continuous pressure survey completed after the 240-tick valid field-team hold.
- `17`: sealed access face confirmed and mapped after reporting back to Marlon.

The same curated 14 NPCs are supported in each new window:
Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

Added `56` new reaction lines per language. Total exact reaction catalog is now `248` lines per language with EN/VI parity.

## Resolver
`GetStoryReactionWindowAlpha6735()` preserves all previous story routing. If Field Triangulation is incomplete, it falls back to Alpha 6.7.33. Once triangulation is complete, `CorridorApproachAlpha6734.Stage` maps as:
- `<=0 -> 13`
- `1 -> 14`
- `2 -> 15`
- `3 -> 16`
- `>=4 -> 17`

`ModEntry.Alpha6728.cs` now uses the 6.7.35 resolver for interaction and reaction diagnostics.

## Safety boundaries retained
- George remains observed Rank D, Non-Combatant, unrecruitable.
- No `GeorgeCombatRevealed` write.
- No `Rank S`, `The Last Blaster`, `George Mullner`, or `Keeper` leak in new dialogue.
- Evelyn postgame secret remains untouched.
- Story NPC slot 4 remains locked at this checkpoint.
- Five-person total party cap unchanged.
- Alpha 6.7.34 sealed-corridor pressure survey remains unchanged, including 240-tick continuous hold, survey-location persistence, team-break reset, and no breach.
- No combat, monster, mutation, capture, map ownership, boss, breach, or roster-cap behavior was changed.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- REACTION WINDOWS 0..17: PASS
- SEALED CORRIDOR WINDOWS 14/15/16/17: PASS, 14 NPCs each
- CURATED MILESTONE REACTIONS: PASS, 248 lines/language
- NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.35 WINDOW RESOLVER: PASS
- SEALED CORRIDOR APPROACH 6.7.34 CARRY-FORWARD: PASS
- 240-TICK CONTINUOUS PRESSURE SURVEY CARRY-FORWARD: PASS
- STORY NPC SLOT 4 REMAINS LOCKED: PASS
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- 6.7.23-6.7.34 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS
- BINARY ACCEPTANCE: PASS

## Live smoke test
1. `teamup_story_reactions reset`.
2. `teamup_corridor stage 1` -> current reaction window should be 14.
3. Talk to a supported NPC -> one special reaction only; second talk returns to normal interaction.
4. `teamup_corridor stage 2` -> window 15.
5. `teamup_corridor stage 3` -> window 16.
6. `teamup_corridor stage 4` -> window 17.
7. Skip a window and advance -> stale missed reaction must not replay later.
8. Check George remains Rank D / Non-Combatant / unrecruitable.
9. Check Evelyn does not reveal postgame secret.
10. `teamup_roster_story status` remains `3/4`.

Useful command:
- `teamup_story_reactions status`
- diagnostic: `diagnostics/TeamUp_Milestone_Reactions_latest.txt`

## Recommended next checkpoint
After live validation, the next gameplay checkpoint can begin preparing a controlled breach / first entry into the sealed lower workings. It should still avoid revealing George until the story ledger deliberately reaches that reveal, and slot 4 should only unlock at the planned Surge HIGH / major-chapter milestone.
