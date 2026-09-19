# Alpha 6.7.44.41 - Capture Guard Scope Fix Handoff

Repository: `RVTGMzz/T-U`

GitHub write account: `lengochung28191@gmail.com`

Branch: `v0.2-alpha6-7-44-41-capture-guard-scope-fix`

Version: `0.2.0-alpha.6.7.44.41`

## Build

- CI source SHA: `80c93472af35ecfe4f55ebbe4aef10d8bfe3ee1d`
- CI run: `35455277486`
- CI job: `105929333037`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.41_CAPTURE_GUARD_SCOPE_FIX_TEST.zip`
- SHA256: `d923a7eb93caf76d8da9dbca19a67e6f273e01f001c8b703f2325f1bb3a7084e`
- Build: PASS, 0 warnings / 0 errors
- ZIP audit: PASS
- Capture guard method-name scope / record exclusion audit: PASS
- Unified preflight carry-forward: PASS

## Live crash evidence from 6.7.44.40

The save reached `Context: loaded save 'Vôtri_446407416'`, so the game was not failing before save initialization.

The standout abnormal behavior was the 6.7.44.36 elite capture guard attempting Harmony patches across a large number of Pelipper methods and producing repeated:

`InvalidProgramException: Common Language Runtime detected an invalid program.`

The old matcher combined normalized declaring type name with normalized method name. Therefore any type whose name contained `capture`, `catch`, or `pokeball` caused every declared method on that type to look capture-related.

Observed bad patch targets included:
- `ToString`
- `PrintMembers`
- `GetHashCode`
- `Equals`
- `Deconstruct`
- `Dispose`

The service still reported `hooks=172`.

The log then stopped abruptly after save load without a managed SMAPI exception/stack trace. This is consistent with the capture guard over-patching being the strongest current hard-crash suspect.

Ownership Marker's separate non-public API warning is unrelated to this crash investigation.

## 6.7.44.41 implementation

`Alpha674436EliteCaptureGuardService.LooksLikeCaptureMethod` now accepts `MethodInfo` and examines the METHOD NAME only.

Explicitly excluded:
- `ToString`
- `PrintMembers`
- `GetHashCode`
- `Equals`
- `Deconstruct`
- `Dispose`
- `Clone`

Still accepted when the actual method name contains:
- `capture`
- `catch`
- `pokeball`

The old declaring-type + method-name concatenation must not return.

The Mutant leader no-capture marker remains, and ordinary followers are still intended to retain native capture.

## First live test

Use a clean Team Up install.

Load the same save that failed on 6.7.44.40.

Success criteria:
1. no repeated capture guard InvalidProgramException spam;
2. capture hook count sharply lower than 172;
3. save remains loaded;
4. `teamup_build` reports 6.7.44.41;
5. `teamup_preflight` is available.

Do not call Runtime PASS until Ron confirms.

If the process still disappears, request the new `SMAPI-latest.txt` only and inspect the tail/fatal signatures directly.

## Carry-forward

6.7.44.34 through 6.7.44.40 remain carried forward, including Mutation combat, pursuit/reach, elite lifecycle, fail-closed follower creation, Lower Workings v2, Nidoran exact tokens, and unified preflight.

Old 6.7.44.24-30 crash-stack services remain excluded.

## Next

After load stability is proven, close the remaining 6.7.44 live gates with `teamup_preflight`. Then begin 6.7.45 Containment Chamber Escalation.
