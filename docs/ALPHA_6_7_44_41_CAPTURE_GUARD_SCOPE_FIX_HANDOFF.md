# Alpha 6.7.44.41 - Capture Guard Scope Fix Handoff

Repository: `RVTGMzz/T-U`

Branch: `v0.2-alpha6-7-44-41-capture-guard-scope-fix`

Version: `0.2.0-alpha.6.7.44.41`

## Build

- CI run: `35455277486`
- CI job: `105929333037`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.41_CAPTURE_GUARD_SCOPE_FIX_TEST.zip`
- SHA256: `d923a7eb93caf76d8da9dbca19a67e6f273e01f001c8b703f2325f1bb3a7084e`
- Build: PASS, 0 warnings / 0 errors
- Capture guard scope audit: PASS

## Live crash evidence

6.7.44.40 loaded successfully and save initialization reached the active save. SMAPI then stopped abruptly with no managed exception. During Team Up startup the capture guard emitted many `InvalidProgramException: Common Language Runtime detected an invalid program` lines while trying to patch methods like `ToString`, `PrintMembers`, `GetHashCode`, `Equals`, `Deconstruct`, and `Dispose` on Pelipper types whose TYPE NAMES contained Capture/Pokeball.

The service still reported `hooks=172`.

## 6.7.44.41 fix

Old matcher concatenated declaring type name + method name. Therefore a type named `CaptureResult` caused every method on the type to look like a capture method.

New matcher:
- looks only at the method name;
- excludes `ToString`, `PrintMembers`, `GetHashCode`, `Equals`, `Deconstruct`, `Dispose`, and `Clone`;
- still recognizes real method names containing `capture`, `catch`, or `pokeball`;
- keeps the Mutant no-capture marker and follower-native contract.

## Live gate

Install 6.7.44.41 cleanly and launch/load the same save.

Expected:
- no repeated capture-guard InvalidProgramException spam;
- capture guard hook count sharply lower than 172;
- save stays loaded;
- `teamup_build` reports 6.7.44.41;
- `teamup_preflight` remains available.

Do not call Runtime PASS until Ron confirms.
