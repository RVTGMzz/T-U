# Team Up latest handoff: 0.2.0-alpha.6.7.44.42

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_42_CAPTURE_GUARD_LOADSAFE_DISABLE_HANDOFF.md`

## Current checkpoint

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

## Latest live authority

Ron has now completed a genuine 6.7.44.41 launch attempt. It did **not** reach the playable world.

The same run also used Cardcha D3-L .76, whose collision Harmony postfix was already disabled. Cardcha completed its main SaveLoaded audit and map creation.

Team Up 6.7.44.41 still installed **89 Pelipper capture/catch/pokeball Harmony prefixes**. The process then hard-exited after save load without a managed SMAPI exception or stack trace.

Therefore 6.7.44.41 is now **Runtime FAIL / hard-exit remains**.

6.7.44.42 is the isolation build: the capture guard service stays alive, but its broad Pelipper Harmony scan installs zero hooks.

## Immediate runtime gate

Keep Cardcha D3-L .76 unchanged.

Clean-replace Team Up with 6.7.44.42 and load the same save.

Expected startup line:

```text
Team Up 6.7.44.42 load-safe capture guard: Pelipper capture-method Harmony patching disabled; hooks=0.
```

First question only: does the save reach the playable world?

Do not start 6.7.45 and do not call Runtime PASS before Ron confirms.
