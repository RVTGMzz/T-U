# Continue Team Up Here

Repository: **`RVTGMzz/T-U`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.40**

Development branch:

`v0.2-alpha6-7-44-40-final-runtime-closure`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_40_FINAL_RUNTIME_CLOSURE_HANDOFF.md`

## Verified build checkpoint

- Repository: `RVTGMzz/T-U`
- Version: `0.2.0-alpha.6.7.44.40`
- Branch: `v0.2-alpha6-7-44-40-final-runtime-closure`
- CI source SHA: `f658913ed19b92491facddf3f98bc1ff3c9d1e05`
- CI run: `35419812620`
- CI job: `105835213977`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.40_FINAL_RUNTIME_CLOSURE_TEST.zip`
- ZIP SHA256: `5d5ad7a90577f0a02a7397003553463914104eec57931ef9209188bcd8cad7d8`
- Build: PASS, 0 warnings, 0 errors
- Package audit: PASS

## 6.7.44.40 purpose

This is the final technical closure build before 6.7.45 story work.

New command:

`teamup_preflight`

It unifies:
- exact loaded build identity;
- Nidoran♂/♀ Pelipper spawn-token self-check;
- current Mutation follower regression audit;
- current Pelipper Mutant elite markers (3 phases, loot x3 marker, no-capture marker);
- live Nidoran follower gender exactness when a Nidoran Mutant is currently active;
- Lower Workings map validity and entry/return telemetry.

Output state:
- `PASS`: all observed technical gates passed;
- `PENDING`: no failure, but one or more live paths have not yet been observed;
- `FAIL`: an observed technical contract failed.

## Immediate live test

Install 6.7.44.40 into a clean Team Up folder.

Run:

```text
teamup_build
teamup_preflight
```

Then force a Mutation and run `teamup_preflight` again.

For full closure, also run preflight after a Nidoran Mutation and after completing the Lower Workings entry/return route.

Do not call 6.7.44 Runtime PASS until Ron confirms the live output.

## Carry-forward locks

- old 6.7.44.24-30 crash stack remains excluded;
- 6.7.44.34 combat presence remains;
- 6.7.44.35 pursuit/reach remains;
- 6.7.44.36 elite contract remains;
- 6.7.44.37 fail-closed/no-GreenSlime remains;
- 6.7.44.38 Lower Workings v2 remains;
- 6.7.44.39 Nidoran exact spawn-token mapping remains.

## Next

If 6.7.44.40 preflight closes without a concrete runtime failure, begin **6.7.45 Containment Chamber Escalation Encounter**.
