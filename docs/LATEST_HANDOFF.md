# Team Up - Canonical Latest Handoff

Repository: **`RVTGMzz/T-U`**

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_41_CAPTURE_GUARD_SCOPE_FIX_HANDOFF.md`

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

The 6.7.44.40 capture guard over-patched Pelipper record/object methods because capture detection used declaring type names. The observed result was repeated CLR InvalidProgramException failures, 172 capture hooks, then abrupt process termination after save load.

6.7.44.41 makes the matcher method-name-only and excludes record/object plumbing.

## Runtime gate

Load the same save on 6.7.44.41. Runtime PASS requires Ron confirmation.

If stable:
- run `teamup_build`;
- run `teamup_preflight`;
- resume remaining Mutation / Nidoran / Lower Workings gates.

If it still hard-crashes, inspect the new SMAPI log directly before any more feature work.
