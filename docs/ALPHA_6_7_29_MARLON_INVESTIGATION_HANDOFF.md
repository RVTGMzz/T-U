# Team Up Alpha 6.7.29 - Marlon Investigation + Milestone Reactions Handoff

## Status

- Version: `0.2.0-alpha.6.7.29`
- Development branch: `v0.2-alpha6-7-29-marlon-investigation-reactions`
- CI-verified materialized source commit: `cc08f36117bd3c8ebcb88e911123d5b44a2d0902`
- CI run: `34465429866`
- CI job: `102832768122`
- CI result: **SUCCESS**
- Compiler: **0 warnings, 0 errors**
- Live gameplay verification: **still required**
- Stable `main`: intentionally **not merged**.

## Build artifact

- Artifact ID: `10147311051`
- Artifact name: `team-up-alpha6-7-29-marlon-investigation-reactions`
- Artifact wrapper SHA256: `337e1995e1141601b0fdb66c512908f6b3774e7f4bdbc2822360e08881b59d1c`
- Inner test ZIP: `TeamUp_v0.2.0-alpha.6.7.29_MARLON_INVESTIGATION_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `27952892d7ba1f61d6440ef5922c76f0bf56147372b64436e263803e1eff41c0`

## Implemented story route

1. The Surge and the Linus/Marlon opening bridge must already be complete (`Origin.Stage >= 2`).
2. The host must bring a real active Team Up NPC ally to the Adventurer's Guild.
3. Marlon briefs the team using an old Guild survey mark that resembles the Mutant anomaly.
4. Entering a real `MineShaft` with the ally reveals stone scorched from the inside and the hooked mark from the old report.
5. The party must defeat an already-mutated, naturally occurring Team Up Mutant inside a `MineShaft` while an active NPC ally is physically present.
6. The Mutant death yields story evidence only. Team Up does not create a synthetic quest monster or seize another provider's spawn/controller lifecycle.
7. Returning to the Adventurer's Guild with the ally completes Marlon's debrief and calls `UnlockTo(..., 2, "marlon-investigation-debrief")`, unlocking story NPC ally slot 2/4.
8. The global five-person formation cap, including online Farmers, remains authoritative.

## NPC milestone reactions

Alpha 6.7.29 extends the one-shot reaction system from windows `0..2` to `0..6`.

- `0`: first Mutant, before Linus
- `1`: after Linus, before Marlon
- `2`: after Marlon opens the first ally slot
- `3`: Marlon assigns the first field investigation
- `4`: mine trail found
- `5`: Mutant evidence secured
- `6`: Marlon debrief complete / story ally slot 2 unlocked

Windows 3 through 6 have curated reactions for Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, and Wizard. The full reaction catalog now contains **94 lines per language**, with EN/VI key parity checked by CI.

George remains a deliberate false-negative before the future reveal: observed Rank D, Non-Combatant, no combat skill, and recruitment blocked. His new lines only read as ordinary mining experience. No reaction may reveal Rank S or `The Last Blaster`. Evelyn likewise remains spoiler-safe.

## New runtime files

- `src/TeamUp/Story/MarlonInvestigationStoryService.cs`
- `src/TeamUp/ModEntry.Alpha6729.cs`
- extended `src/TeamUp/Story/StoryMilestoneReactionService.cs`
- observation hook in `src/TeamUp/Combat/MonsterMutationService.cs`
- EN/VI localization additions

## Commands

- `teamup_marlon_case status`
- `teamup_marlon_case reset`
- `teamup_marlon_case stage 0`
- `teamup_marlon_case stage 1`
- `teamup_marlon_case stage 2`
- `teamup_marlon_case stage 3`
- `teamup_marlon_case stage 4`
- `teamup_story_reactions status`
- `teamup_story_reactions reset`
- `teamup_roster_story status`

Diagnostic output:

`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Marlon_Investigation_latest.txt`

## CI acceptance

The successful run verified:

- persistent Marlon field case route 0..4
- active NPC ally required at story gates
- MineShaft trail gate
- natural Mutant death evidence gate
- duplicate evidence guard
- Marlon debrief unlocks story slot 2
- reaction windows 0..6
- 94 localized reaction lines per language
- George/Evelyn/Marlon milestone coverage
- George Rank S / Last Blaster spoiler boundary
- 6.7.25 through 6.7.28 story/roster carry-forward
- Pelipper/capture/party safety carry-forward

## Live-test boundary

CI does not prove live dialogue pacing, party-follow warp timing, the actual Mutant death callback in a real save, or multiplayer physical-ally presence. Do not promote this checkpoint to stable `main` until those are exercised in game.
