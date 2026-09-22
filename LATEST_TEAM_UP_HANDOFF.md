# Team Up latest handoff: 0.2.0-alpha.6.7.44.41

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_41_CAPTURE_GUARD_SCOPE_FIX_HANDOFF.md`

Runtime inspector: `docs/ALPHA_6_7_44_41_RUNTIME_GATE_INSPECTOR.md`

New-chat prompt: `NEXT_CHAT_PROMPT.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-41-capture-guard-scope-fix`
- Version: `0.2.0-alpha.6.7.44.41`
- CI source SHA: `80c93472af35ecfe4f55ebbe4aef10d8bfe3ee1d`
- Run: `35455277486`
- Job: `105929333037`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.41_CAPTURE_GUARD_SCOPE_FIX_TEST.zip`
- ZIP SHA256: `d923a7eb93caf76d8da9dbca19a67e6f273e01f001c8b703f2325f1bb3a7084e`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Latest live authority

Ron supplied another SMAPI log after attempting to retest. That log is **not 6.7.44.41**.

It explicitly reports:

- Team Up `0.2.0-alpha.6.7.44.40`;
- branch `v0.2-alpha6-7-44-40-final-runtime-closure`;
- capture guard `hooks=172`;
- 41 Team Up capture-guard `InvalidProgramException` lines;
- 41 bad object/record plumbing targets;
- save `Vôtri_446407416` loaded.

Therefore this retest only reconfirms the known 6.7.44.40 crash signature. It is **not evidence that 6.7.44.41 failed**.

6.7.44.41 has still not received a clean live-load test.

## Tooling added without changing the mod package

- `tools/analyze_alpha674441_log.py`
- `docs/ALPHA_6_7_44_41_RUNTIME_GATE_INSPECTOR.md`

The inspector is read-only and classifies SMAPI logs as:

- `FAIL_WRONG_BUILD`
- `FAIL_CAPTURE_GUARD`
- `INCOMPLETE`
- `CANDIDATE_PASS_AWAITING_USER_CONFIRMATION`

It correctly classifies Ron's stale 6.7.44.40 log as `FAIL_WRONG_BUILD`.

The 6.7.44.41 DLL/ZIP was intentionally left unchanged while awaiting a clean runtime test.

## Live gate

Install 6.7.44.41 into a clean Team Up folder and confirm startup reports `0.2.0-alpha.6.7.44.41`.

Then load the same save and verify:

- no repeated capture-guard InvalidProgramException spam;
- capture hook count is sharply lower than 172;
- game remains loaded.

Only after that run `teamup_build` and `teamup_preflight`.

Do not call Runtime PASS before Ron confirms live stability.
