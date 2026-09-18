# Alpha 6.7.44.38 - Lower Workings Runtime Gate v2 Handoff

Repository: `ronvotri/T-U`

Branch: `v0.2-alpha6-7-44-38-lower-workings-runtime-gate-v2`

Version: `0.2.0-alpha.6.7.44.38`

## Build

- CI source SHA: `f08e1a861ba90eacbde5905952615d77bb055b67`
- Run: `35370448488`
- Job: `105682900175`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.38_LOWER_WORKINGS_RUNTIME_GATE_V2_TEST.zip`
- SHA256: `71cb8c0dc9f006d19882163b0abf0023d2bb844971cd2676e7f1377f157d61e8`
- Build: PASS, 0 warnings / 0 errors
- ZIP audit: PASS

## Design

This is not a restore of `Alpha674428LowerWorkingsRuntimeGateService`.

New service:
`Alpha674438LowerWorkingsRuntimeGateV2Service`

Safety:
- no Harmony;
- no AppDomain/reflection scan;
- no new SaveLoaded subscription;
- no UpdateTicked subscription;
- no warp calls;
- no modData/story writes;
- only observes the existing local Warped event and explicit status command.

## Runtime checks

- dedicated location loaded;
- exact vanilla GameLocation runtime type;
- map dimensions 32x24;
- Back / Buildings / Front present and correctly sized;
- arrival tile 15,21;
- clue tiles in bounds;
- persisted breach location/tile readable;
- entry arrival exact;
- return location/tile exact;
- host/farmhand observations;
- stage/prerequisite summary.

Command:

`teamup_lower_runtime <status|reset>`

Reset only clears validator telemetry.

## Static TMX gate

CI validates:
- map 32x24;
- AmbientLight `45 50 60`;
- `Mines/mine.png`;
- Back / Buildings / Front;
- no static Warp property;
- no objectgroup static warp layer.

## Carry-forward

All 6.7.44.34-37 Mutation fixes remain present. Old 6.7.44.24-30 crash-stack services remain absent.

## Live gate

1. Load the save.
2. Run `teamup_lower_runtime`.
3. If eligible, enter Lower Workings normally.
4. Run status inside.
5. Use the safe return.
6. Run status after return.
7. Confirm no crash and exact entry/return telemetry.

After live pass, start 6.7.45 Containment Chamber Escalation.
