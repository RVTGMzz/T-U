# Team Up Alpha 6.7.23 Dev Handoff

Branch: `v0.2-alpha6-7-23-capture-ceasefire-hardening`
Version: `0.2.0-alpha.6.7.23`
Materialized source commit: `7028e28a26517a23c8da00e6cdfbf0c3566fb144`
CI run: `34393178346` SUCCESS
Artifact ID: `10120461164`
Artifact wrapper digest: `sha256:c988670a3533890ea7fa1d0d27adef7554ee98f8e966f075da10d767adfb8fa4`
Inner mod ZIP SHA256: `8d27a95535d067f659fe387028d86120992df825a352ca679c7140f805f997eb`

## Live bug that triggered this checkpoint
User confirmed: Pelipper wild Pokemon at/below the 10% capture floor were still being attacked by Team Up companions.

## Root cause found
`CombatService` already filtered `PelipperCaptureSafetyService.IsProtected` targets and rechecked immediately before generic swings. However two independent autonomous-offense layers built their own raw Monster lists outside CombatService:
- `Alpha6CombatPolishService`
- `CharacterSkillIdentityService`

This allowed upgraded/signature attacks to continue selecting a capture-protected Pelipper combat proxy. `SpecialRecruitCombatService` clamped damage but could still animate/stun a protected target.

## 6.7.23 changes
- Added `Combat/TeamUpOffensiveTargetPolicy.cs` as the canonical autonomous-offense target gate.
- Policy excludes dead targets, Cardcha harness actors, Pelipper source-controlled/excluded actors, and Pelipper capture-protected targets.
- `Alpha6CombatPolishService` now uses the canonical gate.
- `CharacterSkillIdentityService` now uses the canonical gate and additionally clamps direct damage through `PelipperCaptureSafetyService.ClampDamage` as a second backstop.
- `SpecialRecruitCombatService` now uses the canonical gate so it does not perform zero-damage attack/stun visuals on capture-floor targets.
- Added per-tick capture ceasefire watchdog which removes only Team Up's transient `CombatTargetOptInKey` from protected wild proxies. Durable `WildCombatProxyKey` remains untouched.
- Added `teamup_capture_ceasefire` diagnostic command.
- Diagnostic output: `<Team Up>/diagnostics/TeamUp_Capture_Ceasefire_latest.txt`.

## Expected live diagnostic at floor
- `protected=True`
- `eligible=False`
- `target=False`
- `proxy=True`
- `budget=0`

## Authority boundary preserved
Team Up does not take over Pelipper AI/render/controller/capture lifecycle. Durable wild-proxy identity remains source-aware. Do not suppress source Pokemon AI by fake hiding/controller takeover.

## Carry-forward
6.7.22 Pelipper density probe, 6.7.21 Universal Density 2.5x, Mutation Encounters, party cap 5, shared external companion cap 2/2, Rank S dark bronze, and all prior diagnostics remain carried forward.

## Live status
CI-verified only. The user's <=10% capture scenario still requires live validation. If it fails, capture `teamup_capture_ceasefire`, `teamup_diag_all`, and exit game immediately before collecting `%appdata%\StardewValley\ErrorLogs\SMAPI-latest.txt`.
