# Alpha 6.7.44.44 - Lower Route Test Harness

Repository: `RVTGMzz/T-U`
Branch: `v0.2-alpha6-7-44-44-lower-route-test-harness`
Version: `0.2.0-alpha.6.7.44.44`

## Runtime reason

All 6.7.44.43 live gates passed except Lower Workings route. Ron declined config editing or assembling four NPC allies only for route validation.

## Patch

`teamup_lower_route_test status|arm|off`

When armed:
- only `LowerWorkingsInteriorSurveyStoryService.HasFullOperationalFormation` may bypass the formation count;
- all story prerequisites remain required;
- normal Adventure Guild briefing, persisted breach Action entry, Lower Workings warp handler, and safe-return handler remain authoritative;
- no `config.json` mutation;
- no debug-stage mutation;
- no direct warp in the harness command;
- bypass auto-clears after a successful return to the persisted breach.

## Build

- commit: `19e2cf60369e37debff8b5be5eb61d7e2202e463`
- CI run: `36575202368`
- job: `109428916981`
- conclusion: SUCCESS
- package: `TeamUp_v0.2.0-alpha.6.7.44.44_LOWER_ROUTE_TEST_HARNESS_TEST.zip`
- SHA256: `f6a9dcfda9269bf887376dda99b47f852e0e7bd86537ccef177e8cca40b2b430`
- release tag: `team-up-6.7.44.44-lower-route-test-19e2cf60`

## Live gate

Use Ron's already-prepared UndergroundMine1 breach state. Arm harness, take the normal Guild -> breach -> Lower Workings -> breach round trip, then verify:

`entries=1 entryPass=1 entryMismatch=0 returns=1 returnPass=1 returnMismatch=0 mapFail=0 errors=0`

and unified preflight:

`lowerRoute=PASS`.

6.7.45 remains unopened until live confirmation.
