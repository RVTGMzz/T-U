# Team Up Alpha 6.7.27 - Story Roster Progression Handoff

Status: CI-verified only. Live gameplay verification still required. Do not merge to stable `main` yet.

## Branch
`v0.2-alpha6-7-27-story-roster-progression`

## CI-verified materialized source
`07029f1047c37bd3d2d2b06e7fcde6543ec55a56`

## CI
- Run: `34459492131`
- Job: `102813659463`
- Result: SUCCESS
- Build: 0 warnings, 0 errors
- Inner test ZIP SHA256: `f7371ac4dcc35479df6f060eef6e11f1661f32a4cfeb34979c1665f6b7818489`
- Artifact: `team-up-alpha6-7-27-story-roster-progression`
- Artifact ID: `10144910226`
- Artifact wrapper digest: `sha256:10aa29227c9fbf3e7381d48230818662690afa5cbcbbd4381b847db419c50ce0`

## Implemented
- Recruitment capacity is now gated by story progression.
- Before Marlon's first Surge bridge is complete, ordinary NPC recruitment is locked.
- Marlon bridge stage 2 unlocks exactly 1 NPC ally slot.
- Persistent story allowance supports 0..4 NPC ally slots.
- `UnlockTo(...)` is available for later chapters to open slots 2, 3, and 4.
- The formation cap is now normalized to 5 people total, including online Farmers.
- Multiplayer reduces effective NPC slots when more Farmers are online.
- Host-authoritative recruit and inactive-member reactivation both enforce story capacity.
- Codex/profile status distinguishes `Team Up Locked` and `Team Limit Reached`.
- Existing saves already at Marlon stage 2 silently receive slot 1 on load.

## Debug
Command:
`teamup_roster_story status|reset|setslots <0-4>|sync`

Diagnostic:
`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Roster_Progression_latest.txt`

## Important invariant
Single player maximum remains Farmer + 4 NPCs = 5 people total. This is not Farmer + 5 NPCs.

## Next recommended checkpoint
Alpha 6.7.28 should implement George's pre-reveal camouflage/lock before expanding deeper story chapters:
- George remains observed Rank D / non-combatant.
- No usable combat skill before reveal.
- Recruitment attempt should be rejected with character-specific dialogue rather than letting him join after Marlon unlocks slot 1.
- Do not reveal Rank S, `The Last Blaster`, mine conspiracy, or final-boss role yet.
- Evelyn stays recruitable with her deliberately modest pre-Awakening `Garden Remedy`; her Rank S Awakening remains postgame.

After that, continue Marlon's investigation and story milestones that unlock NPC slots 2..4.
