# Team Up latest handoff: 0.2.0-alpha.6.7.44.41

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_41_CAPTURE_GUARD_SCOPE_FIX_HANDOFF.md`

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

## Crash-response authority

6.7.44.40 reached save load, but the elite capture guard emitted repeated CLR `InvalidProgramException` failures while trying to Harmony-patch unrelated record/object methods and still reported 172 hooks. The process then ended abruptly without a managed SMAPI stack.

6.7.44.41 narrows the capture matcher to method names only and explicitly excludes object/record plumbing. This is the current load-stability build.

## Live gate

Install 6.7.44.41 cleanly and load the same save.

First verify:
- no repeated capture-guard InvalidProgramException spam;
- capture hook count is sharply lower than 172;
- game remains loaded.

Only after that run `teamup_build` and `teamup_preflight`.

Do not call Runtime PASS before Ron confirms live stability.
