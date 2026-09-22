# Continue Team Up Here

Repository: **`RVTGMzz/T-U`**

GitHub write account for this project: **`lengochung28191@gmail.com`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.41**

Development branch:

`v0.2-alpha6-7-44-41-capture-guard-scope-fix`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_41_CAPTURE_GUARD_SCOPE_FIX_HANDOFF.md`
5. `docs/ALPHA_6_7_44_41_RUNTIME_GATE_INSPECTOR.md`
6. `NEXT_CHAT_PROMPT.md`

## Verified build checkpoint

- Repository: `RVTGMzz/T-U`
- Version: `0.2.0-alpha.6.7.44.41`
- Branch: `v0.2-alpha6-7-44-41-capture-guard-scope-fix`
- CI source SHA: `80c93472af35ecfe4f55ebbe4aef10d8bfe3ee1d`
- CI run: `35455277486`
- CI job: `105929333037`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.41_CAPTURE_GUARD_SCOPE_FIX_TEST.zip`
- ZIP SHA256: `d923a7eb93caf76d8da9dbca19a67e6f273e01f001c8b703f2325f1bb3a7084e`
- Build: PASS, 0 warnings, 0 errors
- Capture guard method-scope audit: PASS
- Unified preflight carry-forward: PASS

## Latest live authority

The newest Ron SMAPI log was expected to test 6.7.44.41, but it actually loaded the stale 6.7.44.40 package.

Confirmed in that log:

- Team Up `0.2.0-alpha.6.7.44.40`;
- `[TeamUpBuild] ... branch=v0.2-alpha6-7-44-40-final-runtime-closure`;
- 41 capture-guard `InvalidProgramException` lines;
- `hooks=172`;
- save `Vôtri_446407416` loaded;
- the old object/record plumbing patch targets are still present.

Therefore the newest failed launch is another 6.7.44.40 reproduction. It does **not** prove 6.7.44.41 fails.

6.7.44.41 remains runtime-untested on a confirmed clean install.

Ownership Marker remains outside the current crash suspect set unless new evidence points to it.

## 6.7.44.41 fix

`Alpha674436EliteCaptureGuardService` now:

- matches method name only;
- declaring type names no longer influence capture detection;
- explicitly excludes `ToString`, `PrintMembers`, `GetHashCode`, `Equals`, `Deconstruct`, `Dispose`, and `Clone`;
- still recognizes actual method names containing `capture`, `catch`, or `pokeball`;
- preserves Mutant leader no-capture;
- preserves ordinary follower native capture.

Do NOT restore the old broad type-name matcher.

## Runtime inspector

A tooling-only helper now exists:

`tools/analyze_alpha674441_log.py`

Run:

```bash
python tools/analyze_alpha674441_log.py SMAPI-latest.txt
```

It detects stale .40 installs, capture-guard failures, hook count, save-load evidence, and prints the final log tail.

It does not change TeamUp.dll, the ZIP, save data, config, or story state.

## Immediate live gate

Install 6.7.44.41 into a clean Team Up folder.

Before loading a save, confirm the console reports:

`Team Up! 0.2.0-alpha.6.7.44.41`

and:

`[TeamUpBuild] version=0.2.0-alpha.6.7.44.41 branch=v0.2-alpha6-7-44-41-capture-guard-scope-fix`

Then load the exact save that crashed.

Expected:

1. no repeated capture-guard `InvalidProgramException` spam;
2. capture guard `hooks=...` sharply lower than 172;
3. save stays loaded;
4. only after stability, run `teamup_build` and `teamup_preflight`.

If a confirmed 6.7.44.41 install still hard-crashes, request only the new `SMAPI-latest.txt` and inspect the tail directly.

## Carry-forward locks

6.7.44.41 still carries:

- 6.7.44.34 combat presence / anti-flicker / 18-tile aggro / stronger damage;
- 6.7.44.35 continuous leader pursuit / 128px hold / 160px reach;
- 6.7.44.36 no-capture / 3 HP phases / final-only x3 native loot;
- 6.7.44.37 factory-level no-GreenSlime fallback / fail closed;
- 6.7.44.38 read-only Lower Workings Runtime Gate v2;
- 6.7.44.39 runtime identity + Nidoran exact spawn token mapping;
- 6.7.44.40 unified `teamup_preflight`.

Old 6.7.44.24-30 crash-stack services remain excluded.

## Next

Do NOT start 6.7.45 yet.

First Ron must confirm a genuine 6.7.44.41 clean install can load the save without the previous hard crash. If stable, resume `teamup_build` / `teamup_preflight` and close the remaining 6.7.44 live gates. Then begin **6.7.45 Containment Chamber Escalation Encounter**.
