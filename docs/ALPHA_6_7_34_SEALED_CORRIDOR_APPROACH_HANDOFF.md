# Team Up Alpha 6.7.34 - Sealed Corridor Approach Handoff

## Checkpoint identity
- Version: `0.2.0-alpha.6.7.34`
- Branch: `v0.2-alpha6-7-34-sealed-corridor-approach`
- Base handoff head: `66e30639154a6e96d34a15881710af4d51195a24`
- Workflow input: `f78d766b491d5ac7c4cb1eea08e57eaf8f6fbaea`
- CI-verified materialized source: `b8ff1ac2cf9ba7081ebaa322ddf8263fc96505d5`
- Source tree: `85a854bc51fe6b9373b5a5c30aa15e5a4ad632e3`
- CI run: `34558878549`
- CI job: `103137288283`
- Artifact ID: `10183594029`
- Artifact name: `team-up-alpha6-7-34-sealed-corridor-approach`
- Artifact wrapper digest: `sha256:a6d0c2a18c4b3ba6d18d8a78395a16c6e0aedec2b32a2dd851eb8c31d75c7fdd`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.34_SEALED_CORRIDOR_APPROACH_TEST.zip`
- Inner ZIP SHA256: `783c84fbbc3176f1169ae90c4ffe67988e7440e61841de8aa24c05bec88bf771`
- CI compiler: `0 Warning(s)` / `0 Error(s)`
- Not merged to `main`.
- Not live verified.

## New gameplay service
`SealedCorridorApproachStoryService`

Persistent keys:
- `Ronvotri.TeamUp/Story/SealedCorridorApproachStage`
- `Ronvotri.TeamUp/Story/SealedCorridorSurveyLocation`

Constants:
- Complete stage: `4`
- Minimum field people: `3`
- Minimum active Team Up NPC allies: `1`
- Stable survey hold: `240` update ticks

Prerequisite:
- `FieldTriangulationStoryService.CompleteStage` must already be reached.

Route:
1. Stage 0, Adventurer's Guild, valid field team: Marlon briefs a pressure survey. Stage -> 1.
2. Stage 1, any MineShaft, valid field team: that exact `NameOrUniqueName` is persisted as the survey face. Stage -> 2.
3. Stage 2: stay at that exact MineShaft with the valid field team for 240 continuous update ticks. Stage -> 3.
4. Stage 3, return to Adventurer's Guild with valid field team: Marlon confirms the buried sealed access face. Stage -> 4.

The runtime hold resets if the player leaves the marked MineShaft, loses the required field team, opens dialogue/menu/event state, or otherwise stops satisfying `Context.IsPlayerFree`.

## Narrative payoff
The team confirms:
- a thin lateral cold draft through newer mortar;
- an older timber scar under newer reinforcement;
- a hollow response behind the wall;
- lateral stress instead of normal active-shaft load;
- a faint inward-burn trace following an obsolete support seam;
- continuing pressure carried by the seal.

Marlon concludes that this is the buried access face leading toward the sealed lower workings. The wall is explicitly **not breached** in this checkpoint.

## Story locks retained
- Story roster remains `3/4`; slot 4 is still reserved for the future Surge HIGH / major-chapter milestone.
- George remains observed Rank D, Non-Combatant, unrecruitable.
- No `Rank S`, `The Last Blaster`, or George identity reveal.
- Evelyn postgame secret untouched.
- Reaction windows remain `0..13`, exactly `192` reaction lines per language.
- No boss/final framework added.
- No monster spawn/clone/replace/hide/damage/retarget or ownership behavior changed.
- Pelipper capture safety and provider boundaries carried forward.
- Five-person hard cap carried forward.
- No invented `Sector 17` lore.

## Debug command
`teamup_corridor status|reset|stage 0-4`

Diagnostic:
`diagnostics/TeamUp_Sealed_Corridor_Approach_latest.txt`

## Live test boundary
1. `teamup_triangulation status` should report `4/4`.
2. `teamup_corridor reset`.
3. Farmer + 1 NPC at Guild must not advance.
4. Farmer + 2 NPCs at Guild must advance to stage 1.
5. Enter a MineShaft with the valid team. Stage becomes 2 and `surveyLocation` persists.
6. Hold the valid team at that exact MineShaft for about four seconds. Stage becomes 3.
7. Repeat stage 2 but break the team / open a menu / warp away before completion. Hold progress must reset and stage must stay 2.
8. Enter another MineShaft at stage 2. It must not progress and should direct the player back to the marked survey face.
9. Return to Guild with the valid team at stage 3. Stage becomes 4.
10. `teamup_roster_story status` must still show `3/4`.
11. George must remain Rank D / Non-Combatant / unrecruitable.

Do not call this checkpoint stable until the live continuous-hold behavior is verified in-game.

## Recommended next checkpoint
**Alpha 6.7.35: Sealed Corridor Approach Reactions**

Dialogue-only mirror of 6.7.34. Suggested new reaction windows:
- 14: Marlon pressure-survey briefing / approach operation begins
- 15: survey face found
- 16: pressure survey completed
- 17: sealed access face confirmed at the Guild

Keep the same curated 14 NPCs unless a later design decision changes the cast. Add 56 lines per language, taking the reaction catalog from 192 to 248 per language. Keep George as an experienced old miner only, never reveal Last Blaster/Rank S yet. Slot 4 stays locked.
