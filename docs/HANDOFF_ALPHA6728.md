# Team Up Alpha 6.7.28 Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.28`
- Branch: `v0.2-alpha6-7-28-george-camouflage-milestone-reactions`
- CI run: `34462659173`
- CI job: `102823861988`
- CI-verified materialized source commit: `0bdc723978787aa4899e398dd4db1fa969158a92`
- Workflow artifact ID: `10146198996`
- Workflow artifact digest: `sha256:d614a744c01f01af01b0ac92466494a0e769f7dd79ff1f834c9fd0c7515b8ceb`
- Inner test ZIP: `TeamUp_v0.2.0-alpha.6.7.28_GEORGE_CAMOUFLAGE_MILESTONE_REACTIONS_TEST.zip`
- Inner test ZIP SHA256: `5a5339fa820cdeb79fbece55c06a1e20606013c2c8e27e86209c331c13f50e40`
- Status: **CI-VERIFIED ONLY. LIVE DIALOGUE/INPUT TEST REQUIRED.**

## George pre-reveal camouflage
- George remains observed `Rank D` in Codex before his future story reveal.
- Profile status is `Non-Combatant / Không tham chiến` and his observed signature remains no combat skill.
- There is no `???`, `Rank S`, or `The Last Blaster` spoiler in the pre-reveal presentation.
- Normal UI recruitment is blocked before the future reveal.
- Host-authoritative recruitment is blocked too, so multiplayer requests cannot bypass the story gate.
- Inactive George roster entries cannot be reactivated before reveal.
- Old saves with George active are hardened by forcing him inactive and releasing him back to vanilla schedule while unrevealed.
- Pressing the Team Up Recruit input while speaking to George deliberately gives the wheelchair/monster-hunting tease, but does not recruit him.
- A future `Ronvotri.TeamUp/Story/GeorgeCombatRevealed` flag contract exists, but Alpha 6.7.28 never sets it. The main-story finale must own the actual reveal.

## NPC reactions between milestones
Alpha 6.7.28 adds a one-time ambient reaction layer for the opening story windows:
1. Stage 0: first Mutant has appeared, before Linus.
2. Stage 1: after Linus, before Marlon.
3. Stage 2: after Marlon, when the first Team Up NPC slot has opened.

There are 38 curated reaction lines in English and 38 in Vietnamese across Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Maru, Pierre, Robin, Wizard, with Linus and Marlon joining the post-Marlon window.

Reaction state is persisted per Farmer under `Ronvotri.TeamUp/StoryReaction/...`. A matching reaction is consumed once when the player interacts empty-handed with that NPC in that specific window. If the story advances, missed older reactions are not replayed.

George's reactions are intentionally mundane so the Rank S twist remains clean. Evelyn's reactions stay caring/domestic and do not hint at her postgame Rank S story.

## Debug / diagnostic
Command:
- `teamup_story_reactions status`
- `teamup_story_reactions reset`

Diagnostic:
`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Milestone_Reactions_latest.txt`

## CI acceptance
- Source acceptance: PASS
- George observed Rank D / Non-Combatant camouflage: PASS
- George recruit + reactivation block: PASS
- Old-save active George hardening: PASS
- Secret Rank S / Last Blaster spoiler leak audit: PASS
- Three between-milestone reaction windows: PASS
- Curated milestone localization: PASS, 38 lines/language
- George + Evelyn mundane pre-reveal reactions: PASS
- Marlon first-companion reaction: PASS
- 6.7.25 through 6.7.27 story/roster carry-forward: PASS
- Pelipper/capture/party safety carry-forward: PASS
- Build: 0 warnings, 0 errors
- Binary acceptance: PASS

## Next story checkpoint
Do not jump to George reveal yet. The next logical chapter is Marlon's investigation / old-mine evidence and a meaningful gameplay objective with the first recruited ally. That chapter should unlock NPC slot 2 only at its payoff and add a new reaction window for villagers after that milestone.

Do not merge this checkpoint to stable `main` merely because CI passed.
