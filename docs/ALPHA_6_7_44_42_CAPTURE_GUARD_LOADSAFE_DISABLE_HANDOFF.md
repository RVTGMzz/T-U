# Alpha 6.7.44.42 - Capture Guard Load-Safe Disable Handoff

Repository: `RVTGMzz/T-U`

GitHub write account: `lengochung28191@gmail.com`

Branch: `v0.2-alpha6-7-44-42-capture-guard-loadsafe-disable`

Version: `0.2.0-alpha.6.7.44.42`

## Latest runtime authority

Ron provided a confirmed 6.7.44.41 SMAPI run together with Cardcha 0696D3-L .76.

Cardcha D3-L successfully started in load-safe mode and installed no `GameLocation.isCollidingPosition` Harmony postfix.

Team Up 6.7.44.41 startup confirmed the intended branch/version, but its elite capture guard still installed **89 Pelipper capture/catch/pokeball Harmony prefixes**.

The save reached `Context: loaded save 'Vôtri_446407416'`. Cardcha completed its main SaveLoaded persistence audit and created its remaining runtime maps. The process then exited abruptly without a managed SMAPI exception or stack trace before the playable world appeared.

This does not prove Team Up capture guard is the final root cause, but it makes the remaining 89-hook capture scan the strongest isolated runtime suspect.

## 6.7.44.42 isolation change

`Alpha674436EliteCaptureGuardService` remains instantiated so existing diagnostics and SaveLoaded telemetry calls stay valid.

However its constructor does **not** call:

`PatchPelipperCaptureMethods(AppDomain.CurrentDomain.GetAssemblies())`

Therefore the broad capture/catch/pokeball Harmony scan installs **zero hooks**.

Expected startup:

```text
Team Up 6.7.44.42 load-safe capture guard: Pelipper capture-method Harmony patching disabled; hooks=0.
[TeamUpBuild] version=0.2.0-alpha.6.7.44.42 branch=v0.2-alpha6-7-44-42-capture-guard-loadsafe-disable
```

Other 6.7.44 Mutation, exact-source minion, pursuit/reach and Lower Workings systems remain carried forward.

## CI / package

CI run: `36443436095`  
Job: `108999507464`  
Source/package commit: `95bb3521360623907eb79a2acf4c05e7a9f7b9fb`  
Conclusion: **SUCCESS**

Audit highlights:
- capture guard load-safe quarantine / zero install call: PASS
- old 44.24-44.30 crash-stack exclusion: PASS
- Lower Workings v2 read-only/lazy wiring: PASS
- Nidoran exact spawn tokens: PASS
- unified preflight carry-forward: PASS
- C# build: PASS, 0 warnings / 0 errors
- ZIP content audit: PASS

Tag: `team-up-6.7.44.42-loadsafe-95bb3521`

Package:
`TeamUp_v0.2.0-alpha.6.7.44.42_CAPTURE_GUARD_LOADSAFE_DISABLE_TEST.zip`

SHA256:
`5279f3cf143417ddbdd35b0281324cfd140c638d4ba0f0baaedcf0f4af3d420d`

Direct package:
`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.42-loadsafe-95bb3521/TeamUp_v0.2.0-alpha.6.7.44.42_CAPTURE_GUARD_LOADSAFE_DISABLE_TEST.zip`

## First runtime gate

Use Cardcha D3-L .76 unchanged.

Replace Team Up with a clean 6.7.44.42 folder and load the same save.

The first question is only: **does the save reach the playable world?**

If yes, the 89-hook capture scan is strongly implicated and the next patch should reintroduce capture protection only through specific proven Pelipper targets.

If no, do not restore the broad capture hooks. Inspect the new SMAPI tail and continue binary isolation of the remaining runtime systems.

Do not call Runtime PASS until Ron confirms.
