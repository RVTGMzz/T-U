# Team Up Alpha 6.7.26 Dev Handoff

## Status
CI-verified only. Live story presentation test still required. Do not merge to `main` automatically.

## Branch
`v0.2-alpha6-7-26-linus-marlon-surge-bridge`

## Version
`0.2.0-alpha.6.7.26`

## Purpose
Connect the deterministic tenth-kill first Mutant from Alpha 6.7.25 to the real main-story opening without exposing later George/Evelyn twists.

## Runtime flow
1. Before the first Mutant, the story bridge stays dormant.
2. Lethal eligible defeat #10 mutates through Alpha 6.7.25 and commits The Surge.
3. Team Up posts an objective directing the player to Linus.
4. Entering `Forest` plays Linus's first-Surge observation and redirects the player to Marlon.
5. Entering `AdventureGuild` plays Marlon's first investigation scene.
6. Marlon hints that an unnamed person outside the Guild stopped a similar incident recorded long ago.
7. The checkpoint stops there. No automatic Awakening and no instant story completion.

## Important changes
- Replaced obsolete `Ronvotri.TeamUp/OriginStage` prototype progression with fresh `Ronvotri.TeamUp/SurgeNarrativeStage` state.
- Retired the old trigger that advanced story merely because a monster existed in the current location.
- Retired the prototype automatic NPC Awakening and `origin.marlon.teamup` completion path.
- Story remains per-Farmer in `Farmer.modData` while first Surge activation remains host-authoritative through Alpha 6.7.25.
- Added `teamup_story_intro status|reset|stage <0-2>`.
- Diagnostic path: `E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Story_Intro_latest.txt`.
- `teamup_story_intro reset` resets only the short narrative bridge, not the 10-kill Surge state.

## CI
Successful workflow run: `34456733074`
Successful job: `102804769506`
Materialized source commit: `255e926`

Acceptance:
- FIRST MUTANT -> LINUS STORY GATE: PASS
- LINUS -> MARLON OBJECTIVE BRIDGE: PASS
- MARLON ANONYMOUS HERO HINT: PASS
- LEGACY MERE-COMBAT ORIGIN TRIGGER RETIRED: PASS
- LEGACY AUTO-AWAKENING / INSTANT COMPLETION RETIRED: PASS
- 6.7.25 TEN-KILL FIRST MUTANT CARRY-FORWARD: PASS
- 6.7.24 CODEX / SECRET-RANK FOUNDATION CARRY-FORWARD: PASS
- PELIPPER/CAPTURE/PARTY SAFETY CARRY-FORWARD: PASS
- Build: 0 warnings, 0 errors

Artifact ID: `10143793944`
Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.26_LINUS_MARLON_STORY_BRIDGE_TEST.zip`
Inner SHA256: `9636d4c2424c0edd9ce9c1333057e7ae52e15272e395bc80a4b3a13b45d16d72`

## Live test
Use:
- `teamup_surge_story reset`
- `teamup_story_intro reset`
- `teamup_surge_story setkills 9`
- kill one eligible normal monster
- confirm first Mutant and Linus objective
- enter Forest
- confirm Linus dialogue and Marlon objective
- enter AdventureGuild
- confirm Marlon dialogue and `stage=2/2`

If story state is wrong, run `teamup_story_intro status` and send:
`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Story_Intro_latest.txt`

If SMAPI errors/crashes, exit the game immediately and send:
`%appdata%\StardewValley\ErrorLogs\SMAPI-latest.txt`

## Next safe checkpoint
Alpha 6.7.27 should build the first progressive recruitment/story gate after Marlon's investigation, without implementing George's reveal or Evelyn's postgame Awakening yet.
