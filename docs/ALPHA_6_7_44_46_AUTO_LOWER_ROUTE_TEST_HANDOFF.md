# Team Up 6.7.44.46 Auto Lower Route Test - Session Handoff

Repository: **RVTGMzz/T-U**

GitHub write account: **lengochung28191@gmail.com**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.46**

Development branch:

`v0.2-alpha6-7-44-46-auto-route-test`

`main` is NOT merged.

**6.7.44.46 is still part of the 6.7.44 stabilization line. Alpha 6.7.45 Containment Chamber Escalation Encounter has NOT started.**

## Why .44.46 exists

Ron found the manual Lower Workings validation route too confusing. The old .44.44 harness required several manual warps/actions and multiple console commands.

.44.46 adds a one-command automatic route test while preserving the real production entry/return handlers.

## Verified package

- package/source commit: `3bb586db18b438a1aa04ec2732d1a6ad52a34189`
- CI run/job: `37167414621` / `111333153154`
- CI: **SUCCESS**
- release id: `402767033`
- tag: `team-up-6.7.44.46-auto-route-3bb586db`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.46_AUTO_ROUTE_TEST.zip`
- ZIP size: `626141` bytes
- SHA256: `928f8a2b8d939037821eed04127baca56fb52e65fdd01347d97322340a738c57`

Direct ZIP:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.46-auto-route-3bb586db/TeamUp_v0.2.0-alpha.6.7.44.46_AUTO_ROUTE_TEST.zip`

## One-command test

After clean-replacing Team Up with .44.46, load the already-prepared host save and run:

`teamup_lower_route_auto`

No `arm`, Guild visit, manual breach movement, Action press, `teamup_lower_runtime status`, or separate `teamup_preflight` is required for this route gate.

The command:
- validates first descent / Entry Protocol / SURGE HIGH / 4 story slots;
- reads the existing persisted breach location and breach tile;
- snapshots current Lower Workings Interior Survey story keys and the player's original location/tile;
- resets only Lower Route telemetry;
- temporarily enables the existing formation-count test bypass;
- temporarily sets the Interior Survey to the route-entry test stage;
- teleports to the persisted breach;
- invokes the real `LowerWorkingsInteriorSurveyStoryService.TryHandleLocalAction()` entry handler;
- validates the real Lower Workings arrival tile;
- invokes the real production safe-return handler;
- checks exact runtime-gate counters;
- returns the player to the original location;
- restores the original Interior Survey story keys and bypass state.

Expected final line contains:

`[LowerRouteAuto] AUTO TEST PASS`

and:

`entries=1 entryPass=1 entryMismatch=0 returns=1 returnPass=1 returnMismatch=0 mapFail=0 errors=0 lowerRoute=PASS saveStateRestored=true`

If it prints AUTO TEST FAIL, preserve the whole final line and SMAPI log.

Optional:
- `teamup_lower_route_auto status`
- `teamup_lower_route_auto cancel`

Do not move the farmer while the auto test is running.

## Remaining runtime gate after Lower Route

The .44.45 uncatalogued NPC runtime-profile fallback is carried forward unchanged into .44.46.

After Lower Route is confirmed PASS:
1. open Brianna or another previously pending adult-human modded NPC dossier;
2. verify source = **Team Up Runtime Profile**;
3. verify Primary/Secondary role, affinities, passive, and signature are populated;
4. fallback stays Rank D;
5. confirm Pelipper/Pokémon/pets/summons/special-linked companions/children remain excluded.

Do not call NPC fallback Runtime PASS until Ron confirms it in-game.

## Stability locks

- broad Pelipper capture-method Harmony scan stays disabled, hooks=0;
- do not restore declaring-type capture matching;
- `teamup_preflight` remains primary;
- `teamup_preflight_legacy` remains legacy diagnostic;
- curated NPC profiles override runtime fallback;
- `main` remains unmerged;
- 6.7.45 remains unopened.
