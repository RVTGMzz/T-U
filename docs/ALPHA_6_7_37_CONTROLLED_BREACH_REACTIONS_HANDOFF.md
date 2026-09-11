# Team Up Alpha 6.7.37 - Controlled Breach Reactions Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.37`
- Branch: `v0.2-alpha6-7-37-controlled-breach-reactions`
- Base handoff: `414f119924ad73219a4ee147d839efdcad87eb64`
- Workflow input: `52fd5b377eca1b0b642cd551534dd8a1db84f119`
- CI-verified materialized source: `4751ad2e97c29abf2e01da2a8eed820f4448ec3f`
- CI run: `34564218446`
- CI job: `103152908896`
- Artifact ID: `10185414901`
- Artifact name: `team-up-alpha6-7-37-controlled-breach-reactions`
- Artifact wrapper digest: `sha256:1e6e85a7f74c884e9bb4e89a4dff0723f096a0110e4b5e9dbcd571e32c51c957`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.37_CONTROLLED_BREACH_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `224746584101c9ce5a940ff0fed7b9f953346fb020ac44ebdff85ea4a58f7f88`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: not merged
- Live verification: still required

## Implemented
Alpha 6.7.37 is a dialogue-only checkpoint layered on Alpha 6.7.36 Controlled Breach / First Entry.

Reaction catalog now supports windows `0..22`.

New windows:
- `18`: controlled-breach briefing after Marlon authorizes the narrow opening.
- `19`: exact saved breach face reached and prepared.
- `20`: 180-tick controlled opening stabilized.
- `21`: 120-tick first-entry threshold probe completed.
- `22`: first-entry probe reported back to Marlon.

The same curated 14 NPCs are supported in each new window:
Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

Added `70` new reaction lines per language. Total exact reaction catalog is now `318` lines per language with EN/VI parity.

## Resolver
`GetStoryReactionWindowAlpha6737()` preserves all previous routing.

If the sealed-corridor approach is not complete, it falls back to Alpha 6.7.35. Once that prerequisite is complete, `ControlledBreachAlpha6736.Stage` maps as:
- `<=0 -> 17`
- `1 -> 18`
- `2 -> 19`
- `3 -> 20`
- `4 -> 21`
- `>=5 -> 22`

`ModEntry.Alpha6728.cs` now uses the 6.7.37 resolver for NPC interaction and reaction diagnostics.

## Safety boundaries retained
- George remains observed Rank D, Non-Combatant, unrecruitable.
- No `GeorgeCombatRevealed` write.
- No `Rank S`, `The Last Blaster`, `George Mullner`, or `Keeper` leak in new dialogue.
- Evelyn postgame secret remains untouched.
- Story NPC slot 4 remains locked.
- Five-person total party cap unchanged.
- Alpha 6.7.36 breach gameplay is unchanged, including exact survey-face reuse, 180-tick controlled-opening hold, 120-tick threshold-probe hold, team-break/warp/menu reset, and stage 0..5 persistence.
- No combat, monster, mutation, capture, custom map, boss, breach mechanics, or roster behavior changed.

## CI acceptance
- SOURCE ACCEPTANCE: PASS
- REACTION WINDOWS 0..22: PASS
- CONTROLLED BREACH WINDOWS 18..22: PASS, 14 NPCs each
- CURATED MILESTONE REACTIONS: PASS, 318 lines/language
- NPC INTERACTION + DIAGNOSTIC ROUTED TO 6.7.37 WINDOW RESOLVER: PASS
- CONTROLLED BREACH 6.7.36 CARRY-FORWARD: PASS
- 180-TICK OPENING + 120-TICK PROBE HOLDS: PASS
- STORY NPC SLOT 4 REMAINS LOCKED: PASS
- GEORGE / LAST BLASTER SPOILER BOUNDARY: PASS
- EVELYN POSTGAME SPOILER BOUNDARY: PASS
- 6.7.23-6.7.36 STORY/ROSTER/CAPTURE SAFETY CARRY-FORWARD: PASS
- BINARY ACCEPTANCE: PASS

## Archived in repo
- `docs/alpha6737/CONTROLLED_BREACH_REACTIONS_AUDIT_ALPHA6737.md`
- `docs/alpha6737/SMOKE_TEST_V0_2_ALPHA6_7_37_CONTROLLED_BREACH_REACTIONS_VI.txt`
- `docs/alpha6737/BUILD_LOG_ALPHA6737.txt`

## Live smoke test
1. `teamup_story_reactions reset`.
2. `teamup_breach stage 1` -> reaction window 18.
3. Talk to a supported NPC -> one special reaction only; second talk returns to normal interaction.
4. `teamup_breach stage 2` -> window 19.
5. `teamup_breach stage 3` -> window 20.
6. `teamup_breach stage 4` -> window 21.
7. `teamup_breach stage 5` -> window 22.
8. Skip a window and advance -> stale missed reaction must not replay later.
9. Check George remains Rank D / Non-Combatant / unrecruitable.
10. Check Evelyn does not reveal postgame secret.
11. `teamup_roster_story status` remains `3/4`.

Useful command:
- `teamup_story_reactions status`
- diagnostic: `diagnostics/TeamUp_Milestone_Reactions_latest.txt`

## Recommended next checkpoint
`Alpha 6.7.38: Surge HIGH Escalation / Story Slot 4` is the next logical gameplay checkpoint after live validation.

Design intent for that future checkpoint:
- Treat the pressure pulse discovered by the first-entry probe as the trigger for a real major-chapter escalation.
- Escalate the Surge state to HIGH through a persistent story service rather than dialogue-only flavor.
- Unlock story NPC slot 4 only when that escalation is genuinely reached.
- Keep George pre-reveal locked and unnamed in the historical incident context.
- Do not spawn the final boss or reveal The Last Blaster yet.
- Preserve five-person total party cap, Pelipper capture safety, and all mutation exclusions.
