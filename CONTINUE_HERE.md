# Continue Team Up Here

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Current development checkpoint: **Team Up v0.2.0-alpha.6.7.44.46**

Development branch:

`v0.2-alpha6-7-44-46-auto-route-test`

`main` is NOT merged.

**Important:** 6.7.44.46 is still inside the 6.7.44 stabilization line. **6.7.45 Containment Chamber Escalation Encounter has NOT started.**

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_46_AUTO_LOWER_ROUTE_TEST_HANDOFF.md`
5. `docs/ALPHA_6_7_44_45_RUNTIME_PROFILE_FALLBACK_HANDOFF.md`
6. `NEXT_CHAT_PROMPT.md`

## Active runtime gate 1: Lower Workings route

Manual .44.44 procedure is superseded for Ron by the .44.46 automatic harness.

Verified .44.46 package:
- commit: `3bb586db18b438a1aa04ec2732d1a6ad52a34189`
- CI run/job: `37167414621` / `111333153154`
- release tag: `team-up-6.7.44.46-auto-route-3bb586db`
- ZIP SHA256: `928f8a2b8d939037821eed04127baca56fb52e65fdd01347d97322340a738c57`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.46_AUTO_ROUTE_TEST.zip`

Direct package:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.46-auto-route-3bb586db/TeamUp_v0.2.0-alpha.6.7.44.46_AUTO_ROUTE_TEST.zip`

Ron now only needs to load the prepared host save and run:

`teamup_lower_route_auto`

PASS target:

`AUTO TEST PASS | entries=1 entryPass=1 entryMismatch=0 returns=1 returnPass=1 returnMismatch=0 mapFail=0 errors=0 | lowerRoute=PASS | saveStateRestored=true`

The auto harness snapshots/restores Interior Survey story state and player position. It drives the real production entry and return handlers. Do not ask Ron to repeat the old Guild -> breach -> Action -> Lower Workings -> Action procedure unless the automatic harness itself fails and manual isolation becomes necessary.

## Active runtime gate 2: Uncatalogued NPC profiles

The .44.45 runtime profile fallback is carried forward unchanged.

After Lower Route PASS, open Brianna or another previously pending eligible modded NPC dossier and confirm:
- source = Team Up Runtime Profile;
- Primary/Secondary role populated;
- affinities populated;
- passive populated;
- real signature populated;
- fallback Rank D;
- excluded companion/Pelipper categories remain excluded.

Do not call this gate Runtime PASS before in-game evidence.

## Locks

- Capture broad Harmony scan stays disabled, `hooks=0`.
- Do not restore declaring-type capture matching.
- `teamup_preflight` stays primary.
- `teamup_preflight_legacy` stays legacy.
- `main` stays unmerged.
- Do not start 6.7.45 until both active 6.7.44 runtime gates are closed.
