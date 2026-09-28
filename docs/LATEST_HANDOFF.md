# Team Up - Canonical Latest Handoff

Repository: **`RVTGMzz/T-U`**

Current detailed handoff: `ALPHA_6_7_44_42_CAPTURE_GUARD_LOADSAFE_DISABLE_HANDOFF.md`

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

## Runtime authority

Confirmed 6.7.44.41 live test:
- correct .41 build and branch loaded;
- capture guard still installed 89 Pelipper capture/catch/pokeball Harmony prefixes;
- save `Vôtri_446407416` reached SaveLoaded;
- Cardcha D3-L .76 had already removed its collision hot-path postfix and completed its SaveLoaded work;
- process exited abruptly before playable world;
- no managed SMAPI exception was logged.

This makes the 89-hook capture scan the strongest isolated suspect, but not yet a proven root cause.

## 6.7.44.42

The capture service remains constructed, so downstream telemetry calls remain safe, but the constructor no longer invokes the broad `PatchPelipperCaptureMethods(...)` scan.

CI proves the install call is absent and the rest of 6.7.44 carries forward.

Runtime gate: install .42 cleanly with Cardcha .76 unchanged and answer only whether the same save reaches the world.

Do not restore the broad capture scan even if another subsystem is later implicated.
