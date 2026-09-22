# Alpha 6.7.44.41 Runtime Gate Inspector

This is a read-only helper for the 6.7.44.41 live-load gate.

Run:

```bash
python tools/analyze_alpha674441_log.py SMAPI-latest.txt
```

It reports:

- detected Team Up version and branch;
- capture-guard hook count;
- capture-guard `InvalidProgramException` count;
- record/object plumbing targets such as `ToString`, `PrintMembers`, `GetHashCode`, `Equals`, `Deconstruct`, `Dispose`, and `Clone`;
- loaded-save marker;
- the final log tail.

Statuses:

- `FAIL_WRONG_BUILD`: the log is not from 6.7.44.41. This specifically catches the known 6.7.44.40 stale-install case.
- `FAIL_CAPTURE_GUARD`: 6.7.44.41 is present but the old capture-guard failure signature remains.
- `INCOMPLETE`: there is not enough evidence to close the load gate.
- `CANDIDATE_PASS_AWAITING_USER_CONFIRMATION`: static log evidence meets the load-gate criteria, but Runtime PASS still requires Ron to confirm the save remained stable.

The tool never mutates save data, story state, config, or TeamUp.dll.

## Regression fixture from Ron's 6.7.44.40 log

The 2026-09-20 log correctly classifies as `FAIL_WRONG_BUILD` with:

- version `0.2.0-alpha.6.7.44.40`;
- branch `v0.2-alpha6-7-44-40-final-runtime-closure`;
- `hooks=172`;
- 41 Team Up capture-guard `InvalidProgramException` lines;
- 41 object/record plumbing bad targets;
- save `Vôtri_446407416` loaded.

This is intentionally tooling-only. It does not change the 6.7.44.41 mod package while that package is awaiting a clean live test.
