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
5. `NEXT_CHAT_PROMPT.md`

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

Ron supplied the crash log from 6.7.44.40.

Important evidence:

- Team Up 6.7.44.40 did load.
- The save reached `Context: loaded save 'Vôtri_446407416'`.
- During Team Up startup, the elite capture guard attempted Harmony patches across many Pelipper methods.
- The old matcher combined DECLARING TYPE NAME + METHOD NAME, so types such as `CaptureResult` caused unrelated methods on that type to be treated as capture methods.
- Repeated patch failures included `InvalidProgramException: Common Language Runtime detected an invalid program.`
- affected methods included object/record plumbing such as `ToString`, `PrintMembers`, `GetHashCode`, `Equals`, `Deconstruct`, and `Dispose`.
- capture guard still reported `hooks=172`.
- the process then ended abruptly after save load with no managed SMAPI crash stack.

This makes the capture guard over-patching the strongest current crash suspect.

Ownership Marker is NOT the current crash suspect. The separate warning about its non-public API type is unrelated to this Team Up hard-crash investigation.

## 6.7.44.41 fix

`Alpha674436EliteCaptureGuardService` now:

- matches **method name only**;
- declaring type names no longer influence capture detection;
- explicitly excludes:
  - `ToString`
  - `PrintMembers`
  - `GetHashCode`
  - `Equals`
  - `Deconstruct`
  - `Dispose`
  - `Clone`
- still recognizes real method names containing:
  - `capture`
  - `catch`
  - `pokeball`
- preserves Mutant leader no-capture;
- preserves ordinary follower native capture.

Do NOT restore the old broad type-name matcher.

## Immediate live gate

Install 6.7.44.41 into a **clean Team Up folder** and load the exact save that crashed.

The first goal is load stability, not feature testing.

Expected:

1. no repeated capture-guard `InvalidProgramException` spam;
2. capture guard `hooks=...` is sharply lower than 172;
3. save stays loaded instead of the game vanishing;
4. only after the save is stable, run:
   `teamup_build`
   `teamup_preflight`

If 6.7.44.41 still hard-crashes, request only the new `SMAPI-latest.txt` and inspect the tail directly. Do not ask Ron to repeat broad reproduction steps.

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

First Ron must confirm 6.7.44.41 can load the save without the previous hard crash. If stable, resume `teamup_build` / `teamup_preflight` and close the remaining 6.7.44 live gates. Then begin **6.7.45 Containment Chamber Escalation Encounter**.
