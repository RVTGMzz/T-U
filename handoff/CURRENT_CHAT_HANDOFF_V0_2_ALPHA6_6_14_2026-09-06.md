# Team Up v0.2.0-alpha.6.6.14 handoff

## User request
When Pelipper Town uses its low-HP capture/mercy behavior around 10% HP, the entire Team Up party should stop offensive actions against that wild/battle Pokemon. Healing, revive, guard and ally support must continue.

## Alpha 6.6.14 behavior
- New `Core/PelipperCaptureSafetyService.cs` provides a read-only Pelipper compatibility policy.
- Best-effort reflection probes obvious Pelipper config/settings for capture/mercy mode and threshold.
- Compatibility fallback is enabled at 10% when no stable source setting is discoverable.
- Capture safety applies only to unowned Pelipper wild/battle combat proxies, never owned companions.
- Combat is split into full `combatMonsters` context and offensive `monsters` targets.
- Protected Pelipper Pokemon remain in full combat context for incoming pressure, tank guard, healer/revive/support logic.
- Protected Pokemon are removed from offensive acquisition and offensive expansion skills.
- If another valid enemy exists, Team Up switches to it.
- If the protected Pokemon is the last enemy, offensive actors disengage while healing/support remains available.
- A last-moment `IsProtected` guard runs before weapon damage.
- `TryGetDamageBudget` / `ClampDamage` implement a damage ceiling, so a Team Up hit cannot cross below the capture threshold.
- Capture-limited hit disables crit variance to prevent threshold overshoot.
- Abigail/Alex signature AoE and SVE/RSV ExpansionSkillService damage use capture-safe clamping/filtering.
- Existing 6.6.13 single-target combat, hard shared 2/2 Pelipper quota, personality curfew farewell, water/bridge performance and land safety are preserved.

## Authoritative CI
- Run: `34011543974`
- Input commit: `244380287d5e52f6cf0c77eb4733fac896af396b`
- Result: SUCCESS
- Build: 0 warnings, 0 errors
- Source acceptance: PASS
- Package verification: PASS
- Materialization check: `No materialized source diff.`
- Package: `TeamUp_v0.2.0-alpha.6.6.14_PELIPPER_CAPTURE_SAFETY_SYNC_TEST.zip`
- Package SHA256: `31ef8f72152f89920e471f212b3c689472eb40e1d316d415a35ce8d09eddc70e`
- Artifact ID: `9982616111`
- Artifact wrapper SHA256: `0af764f873dde134f1219c3dd4bad84d47801dfcc0bd391c60225a9b7b8ac3e2`

## Live test priority
1. One Pelipper wild Pokemon above capture threshold: Team Up must attack normally.
2. Bring it to threshold: all Team Up offensive damage must stop.
3. High-damage hit from roughly 11-15% must clamp at the threshold rather than KO/cross below it.
4. Injure Farmer/NPC while protected Pokemon is the only enemy: healer/revive/guard/support must still work.
5. Put a second enemy nearby: Team Up should switch to that enemy and must not splash AoE damage into the protected Pokemon.
6. Regression: 2/2 Pelipper companion quota, active Pokemon flicker, single-target combat, curfew bye, water/bridge FPS, land safety, HP bars, Switch equipment, Codex.

## Compatibility caveat
Pelipper Town has no hard DLL/API dependency in Team Up. Its config discovery is reflection-only and best-effort. If the selected Pelipper capture mode/threshold is not discoverable at runtime, Team Up intentionally falls back to the requested 10% safety threshold. If live behavior differs when the Pelipper setting is changed, obtain a fresh SMAPI log and the exact Pelipper setting shown in-game before changing the fallback or hard-coding source-private names.
