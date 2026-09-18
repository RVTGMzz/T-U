# Continue Team Up Here

Repository: **`ronvotri/T-U`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.38**

Development branch:

`v0.2-alpha6-7-44-38-lower-workings-runtime-gate-v2`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_38_LOWER_WORKINGS_RUNTIME_GATE_V2_HANDOFF.md`

## Verified build checkpoint

- Repository: `ronvotri/T-U`
- Version: `0.2.0-alpha.6.7.44.38`
- Branch: `v0.2-alpha6-7-44-38-lower-workings-runtime-gate-v2`
- CI source SHA: `f08e1a861ba90eacbde5905952615d77bb055b67`
- CI run: `35370448488`
- CI job: `105682900175`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.38_LOWER_WORKINGS_RUNTIME_GATE_V2_TEST.zip`
- ZIP SHA256: `71cb8c0dc9f006d19882163b0abf0023d2bb844971cd2676e7f1377f157d61e8`
- Build: PASS, 0 warnings, 0 errors
- Package audit: PASS

## Current authority

- 6.7.44.35 leader chase/reach was live-confirmed OK by Ron.
- 6.7.44.36 bundles leader no-capture, 3 HP phases and final-only x3 native loot.
- 6.7.44.37 removes GreenSlime fallback at factory level and adds `teamup_mutation_regression`.
- 6.7.44.38 adds a read-only Lower Workings Runtime Gate v2.
- Old 6.7.44.24-30 crash-stack services remain excluded.

## 6.7.44.38 delta

Lower Workings Runtime Gate v2 is intentionally minimal:

- no Harmony;
- no reflection/assembly scan;
- no new SaveLoaded subscription;
- no UpdateTicked subscription;
- no warp call;
- no story/modData writes;
- observes only the already-existing local Warped event;
- command: `teamup_lower_runtime status|reset`.

It validates:
- runtime location is vanilla `StardewValley.GameLocation`;
- map is 32x24;
- layers Back / Buildings / Front exist and are 32x24;
- arrival tile is `15,21`;
- story clue tiles are in bounds;
- persisted breach location/tile can be read;
- entry arrives at `15,21`;
- safe return lands on the exact persisted breach tile;
- host/farmhand observation telemetry;
- prerequisites and survey stage are reported read-only.

Static CI also validates:
- `AmbientLight=45 50 60`;
- vanilla `Mines/mine.png` tilesheet;
- no static Warp property/object layer.

## Immediate runtime test

After installing 6.7.44.38, load the save and first run:

`teamup_lower_runtime`

This command must exist and must not crash the save.

If the story state already allows entry, enter Lower Workings normally, then run the command again. On return to the breach, run it once more.

Do not call Runtime PASS until Ron confirms the live route.

## Next

If 6.7.44.38 loads and the Lower Workings route validates, technical gates for 6.7.44 are closed enough to begin **6.7.45 Containment Chamber Escalation Encounter**.

Do not restore the old 6.7.44.24-30 service stack.
