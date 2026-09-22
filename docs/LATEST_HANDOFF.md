# Team Up - Canonical Latest Handoff

Repository: **`RVTGMzz/T-U`**

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_41_CAPTURE_GUARD_SCOPE_FIX_HANDOFF.md`

Runtime inspector notes: `ALPHA_6_7_44_41_RUNTIME_GATE_INSPECTOR.md`

Current new-chat prompt: `../NEXT_CHAT_PROMPT.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.41`
- Branch: `v0.2-alpha6-7-44-41-capture-guard-scope-fix`
- CI source SHA: `80c93472af35ecfe4f55ebbe4aef10d8bfe3ee1d`
- Run: `35455277486`
- Job: `105929333037`
- ZIP SHA256: `d923a7eb93caf76d8da9dbca19a67e6f273e01f001c8b703f2325f1bb3a7084e`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Current priority

Load stability before all other testing.

6.7.44.40 over-patched Pelipper record/object methods and produced repeated CLR InvalidProgramException failures with 172 capture hooks.

6.7.44.41 narrows the matcher to method names only and excludes record/object plumbing.

## Latest retest evidence

The latest Ron log still loaded **6.7.44.40**, not 6.7.44.41.

The log contains:

- `[TeamUpBuild] version=0.2.0-alpha.6.7.44.40 branch=v0.2-alpha6-7-44-40-final-runtime-closure`;
- `hooks=172`;
- 41 capture-guard `InvalidProgramException` lines;
- save `Vôtri_446407416` loaded.

This retest does not evaluate 6.7.44.41. A clean 6.7.44.41 runtime test is still pending.

## Read-only runtime inspector

Use:

```bash
python tools/analyze_alpha674441_log.py SMAPI-latest.txt
```

The inspector does not alter TeamUp.dll or save state. The current mod ZIP remains unchanged.

## Runtime gate

Load the same save on a confirmed 6.7.44.41 install. Runtime PASS requires Ron confirmation.

If stable:
- run `teamup_build`;
- run `teamup_preflight`;
- resume remaining Mutation / Nidoran / Lower Workings gates.

If it still hard-crashes on a confirmed 6.7.44.41 build, inspect the new SMAPI log directly before any more feature work.
