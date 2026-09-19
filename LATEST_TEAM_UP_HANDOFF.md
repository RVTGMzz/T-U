# Team Up latest handoff: 0.2.0-alpha.6.7.44.41

Repository: **`RVTGMzz/T-U`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_41_CAPTURE_GUARD_SCOPE_FIX_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-41-capture-guard-scope-fix`
- Version: `0.2.0-alpha.6.7.44.41`
- CI source SHA: `f658913ed19b92491facddf3f98bc1ff3c9d1e05`
- Run: `35455277486`
- Job: `105929333037`
- ZIP SHA256: `d923a7eb93caf76d8da9dbca19a67e6f273e01f001c8b703f2325f1bb3a7084e`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## 6.7.44.40

Adds one unified runtime command: `teamup_preflight`.

It reports PASS / PENDING / FAIL across build identity, Nidoran token mapping, Mutation regression, elite markers, live Nidoran exactness and Lower Workings map/route telemetry.

All 6.7.44.34-39 safety and gameplay fixes remain carried forward. Old 6.7.44.24-30 crash services remain absent.

## Next live gate

Install into a clean Team Up folder, run `teamup_build` and `teamup_preflight`. After one Mutation, run preflight again. Full closure also needs one Nidoran live observation and Lower Workings entry/return observation.


## 6.7.44.41 crash-response delta

Ron supplied a live SMAPI log where 6.7.44.40 loaded, then the process terminated abruptly after save initialization with no managed crash stack. The strongest abnormal signal was the 6.7.44.36 capture guard attempting to patch record/object methods such as ToString, Equals, GetHashCode, PrintMembers and Deconstruct across Pelipper capture-related types, producing repeated InvalidProgramException messages while still reporting 172 installed hooks.

6.7.44.41 narrows matching to method names only and explicitly excludes record/object plumbing. Declaring type names no longer cause unrelated methods to be patched. Runtime live validation must confirm load stability and a sharply reduced hook count before moving to 6.7.45.
