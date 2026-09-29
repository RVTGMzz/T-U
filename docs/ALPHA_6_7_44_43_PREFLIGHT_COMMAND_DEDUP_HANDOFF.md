# Alpha 6.7.44.43 - Preflight Command Dedup Handoff

Repository: `RVTGMzz/T-U`

GitHub write account: `lengochung28191@gmail.com`

Branch: `v0.2-alpha6-7-44-43-preflight-command-dedup`

Version: `0.2.0-alpha.6.7.44.43`

## Latest runtime authority

Ron tested 6.7.44.42 and SMAPI surfaced a managed UpdateTicked exception before normal gameplay:

`ArgumentException: Can't register the 'teamup_preflight' command because there's already a command with that name.`

Call path:

`EnsureAlpha6715Registered -> EnsureAlpha6714Registered -> EnsureAlpha6619EventsRegistered -> UpdateDialogueCompanionInputAlpha6616 -> OnUpdateTicked`

Root cause: the modern 6.7.44 layer registers `teamup_preflight` early, while the older Alpha6715 diagnostic layer attempted to register the same command later during UpdateTicked.

This is a separate blocker from the capture-hook isolation. The 6.7.44.42 capture quarantine itself remained active with zero broad Pelipper capture-method Harmony hooks.

## 6.7.44.43 fix

- `teamup_preflight` remains the authoritative unified 6.7.44 runtime preflight.
- Alpha6715's older diagnostic command is renamed to `teamup_preflight_legacy`.
- CI asserts the legacy file no longer registers the primary command.
- CI asserts the unified layer owns exactly one primary `teamup_preflight` registration.
- Capture-method Harmony quarantine remains enabled with `hooks=0`.
- No Cardcha code is changed.

Expected startup:

```text
[TeamUpBuild] version=0.2.0-alpha.6.7.44.43 branch=v0.2-alpha6-7-44-43-preflight-command-dedup
Team Up 6.7.44.43 load-safe capture guard: Pelipper capture-method Harmony patching disabled; hooks=0.
Team Up Alpha 6.7.15 legacy preflight diagnostics enabled as teamup_preflight_legacy.
```

## CI / package

- CI run: `36565769771`
- CI job: `109397174239`
- Source/package commit: `886191b10b3f53577bd1d4f5b86927bcfa29a136`
- Conclusion: **SUCCESS**
- Package: `TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`
- ZIP SHA256: `b5d2d07f82992c92a835eb15cfb39d782c996d42ea6a83fbd078dea0ed06d1ce`

Direct package:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.43-preflight-dedup-886191b1/TeamUp_v0.2.0-alpha.6.7.44.43_PREFLIGHT_COMMAND_DEDUP_TEST.zip`

Release audit confirms:
- preflight command dedup / single primary owner: PASS;
- capture guard load-safe quarantine / zero install call: PASS;
- existing Lower Workings / Mutation / Nidoran carry-forward audits: PASS;
- C# build and package publish: SUCCESS.

## Immediate runtime gate

Keep Cardcha D3-L .76 unchanged.

Clean-replace Team Up with 6.7.44.43 and load the same save.

First check only:
1. the duplicate `teamup_preflight` ArgumentException does not return;
2. capture guard still reports `hooks=0`;
3. determine whether the save reaches the playable world.

Do not start 6.7.45 and do not call Runtime PASS until Ron confirms.
