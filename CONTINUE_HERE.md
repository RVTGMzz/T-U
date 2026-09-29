# Continue Team Up Here

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.44**

Development branch:

`v0.2-alpha6-7-44-44-lower-route-test-harness`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_44_LOWER_ROUTE_TEST_HARNESS.md`
5. `NEXT_CHAT_PROMPT.md`

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.44`
- Branch: `v0.2-alpha6-7-44-44-lower-route-test-harness`
- Source/package commit: `19e2cf60369e37debff8b5be5eb61d7e2202e463`
- CI run: `36575202368`
- CI job: `109428916981`
- CI: **SUCCESS**
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.44_LOWER_ROUTE_TEST_HARNESS_TEST.zip`
- ZIP SHA256: `f6a9dcfda9269bf887376dda99b47f852e0e7bd86537ccef177e8cca40b2b430`
- Capture guard broad Harmony scan: **DISABLED, hooks=0 by design**
- Load stability: **PASS**
- Mutation wave: **PASS**
- Elite markers: **PASS**
- Nidoran live: **PASS**
- Lower Workings map: **PASS**
- Lower route: **RUNTIME RETEST REQUIRED**

Direct package:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.44-lower-route-test-19e2cf60/TeamUp_v0.2.0-alpha.6.7.44.44_LOWER_ROUTE_TEST_HARNESS_TEST.zip`

## Why .44 exists

Ron does not want to edit Team Up config or drag four NPC allies into the mine solely to validate the Lower Workings route.

6.7.44.44 adds `teamup_lower_route_test status|arm|off`.

The harness:
- is runtime-only;
- bypasses only the Interior Survey formation-count check while armed;
- does not write `config.json`;
- does not set debug story stages;
- does not direct-warp the player;
- auto-clears after the normal Lower Workings safe-return path reaches the persisted breach.

CI explicitly audits these restrictions.

## Current save authority

Ron already debug-prepared prerequisites in UndergroundMine1:
- Controlled Breach 5/5, breachLocation=UndergroundMine1;
- Surge HIGH 4/4, high=True;
- roster slots=4/4;
- Entry Protocol 4/4, ready=True;
- Lower Descent 5/5, firstDescentComplete=True;
- Interior Survey reset to 0/6;
- Lower map valid 32x24.

Do NOT redo Mutation/Ponyta/Nidoran tests.

## Next live test

Clean-replace Team Up with .44.

Then:
1. load the same save;
2. run `teamup_build`;
3. run `teamup_lower_route_test arm`;
4. visit Adventure Guild so Interior Survey advances to stage 1;
5. return to persisted breach `UndergroundMine1`;
6. press Action to enter Lower Workings through the normal story handler;
7. at the Lower Workings arrival tile, press Action again to use the normal safe-return handler;
8. run `teamup_lower_runtime status`;
9. run `teamup_preflight`.

Pass target:
- entries=1, entryPass=1, entryMismatch=0;
- returns=1, returnPass=1, returnMismatch=0;
- mapFail=0, errors=0;
- `lowerRoute=PASS`.

Do not start 6.7.45 until Ron confirms this route.
