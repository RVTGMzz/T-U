# Continue Team Up Here

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.42**

Development branch:

`v0.2-alpha6-7-44-42-capture-guard-loadsafe-disable`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_42_CAPTURE_GUARD_LOADSAFE_DISABLE_HANDOFF.md`
5. `NEXT_CHAT_PROMPT.md`

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.42`
- Branch: `v0.2-alpha6-7-44-42-capture-guard-loadsafe-disable`
- CI source SHA: `95bb3521360623907eb79a2acf4c05e7a9f7b9fb`
- CI run: `36443436095`
- CI job: `108999507464`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.42_CAPTURE_GUARD_LOADSAFE_DISABLE_TEST.zip`
- ZIP SHA256: `5279f3cf143417ddbdd35b0281324cfd140c638d4ba0f0baaedcf0f4af3d420d`
- Build: PASS, 0 warnings / 0 errors
- Capture guard broad Harmony scan: **DISABLED, hooks=0 by design**
- Runtime: RETEST REQUIRED

Direct package:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.42-loadsafe-95bb3521/TeamUp_v0.2.0-alpha.6.7.44.42_CAPTURE_GUARD_LOADSAFE_DISABLE_TEST.zip`

## Why .42 exists

A confirmed 6.7.44.41 runtime still hard-exits after SaveLoaded. Unlike .40, it no longer emits InvalidProgramException spam, but it still installs 89 generic Pelipper capture-method Harmony prefixes.

6.7.44.42 quarantines that whole capture scan. The service remains present and reports `hooks=0`, so existing telemetry/reset code remains valid.

## Immediate live gate

Keep Cardcha D3-L .76 installed unchanged.

Clean-replace only Team Up with .42, load the same save, and report whether the playable world appears.

Expected:

`[TeamUpBuild] version=0.2.0-alpha.6.7.44.42 branch=v0.2-alpha6-7-44-42-capture-guard-loadsafe-disable`

and:

`Team Up 6.7.44.42 load-safe capture guard: Pelipper capture-method Harmony patching disabled; hooks=0.`

If the save still hard-exits, inspect the new SMAPI tail before changing any additional gameplay feature.

Do not start 6.7.45 yet.
