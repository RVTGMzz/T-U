# Team Up - Canonical Latest Handoff

Repository: **`ronvotri/T-U`**

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_38_LOWER_WORKINGS_RUNTIME_GATE_V2_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.38`
- Branch: `v0.2-alpha6-7-44-38-lower-workings-runtime-gate-v2`
- CI source SHA: `f08e1a861ba90eacbde5905952615d77bb055b67`
- Run: `35370448488`
- Job: `105682900175`
- ZIP SHA256: `71cb8c0dc9f006d19882163b0abf0023d2bb844971cd2676e7f1377f157d61e8`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Mutation

6.7.44.34-37 remain carried forward:
- Pelipper source-ID pairing;
- stable x2 presentation;
- 18-tile aggro arena;
- stronger damage;
- continuous leader pursuit;
- 128px hold / 160px reach;
- leader no-capture;
- 3 HP phases;
- final-only x3 reward;
- Pelipper native followers;
- exact same-type vanilla/custom followers;
- unsupported sources fail closed;
- factory-level GreenSlime fallback removed.

## Lower Workings Runtime Gate v2

6.7.44.38 validates the dedicated Lower Workings route without the old crash-path design.

No new SaveLoaded, UpdateTicked, Harmony, reflection scan, warp action or story writes are introduced.

Runtime command:

`teamup_lower_runtime`

Expected map contract:
- `Ronvotri.TeamUp_LowerWorkings`;
- vanilla `GameLocation`;
- 32x24;
- Back / Buildings / Front;
- arrival 15,21;
- exact persisted breach return.

## Next

Live-confirm the route. If clean, begin 6.7.45 Containment Chamber Escalation.
