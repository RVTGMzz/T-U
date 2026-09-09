# Team Up Alpha 6.7.21 Development Handoff

Branch: `v0.2-alpha6-7-21-universal-monster-density`
Version: `0.2.0-alpha.6.7.21`
Base: Alpha 6.7.20

## Goal
Replace the legacy map-name-gated Surge overlay with actor-driven Universal Monster Density for normal hostile monsters while preserving source ownership boundaries for Pelipper and excluding bosses/scripted actors.

## Runtime changes
- Default total density target: x2.5 normal eligible monsters.
- Default extra cap: 36; hard clamp: 60.
- Removed the `LooksLikeCombatZone(location)` call from runtime density eligibility. Real eligible `Monster` actors now define a combat-capable location.
- Initial discovery waits 90 ticks, then retries every 60 ticks for up to five attempts so late-spawning mod monsters are not missed.
- Bosses are excluded from the baseline and never duplicated.
- Scripted, quest-protected, explicit density/surge/mutation-excluded actors are excluded.
- Cardcha normal monsters remain eligible. Cardcha test arena/harness stays excluded.
- Existing Surge actors, Mutants and Mutation Minions are excluded from recursive baseline calculation.

## Custom/Cardcha spawning
New `UniversalMonsterDensitySpawnFactory`:
- tries same runtime type with safe constructors only: `(Vector2)` or `(Vector2,int level/difficulty)`;
- vanilla constructor failures may use GreenSlime fallback;
- third-party/custom constructor failures are skipped, never reflection-cloned and never replaced by unrelated slime;
- telemetry records same-type, vanilla fallback and custom rejection counts.

## Pelipper boundary
Pelipper wild/capture/companion/proxy actors are classified as `PELIPPER` and surfaced in density telemetry, but Team Up does not fabricate duplicate Pokemon/proxies. A Pelipper-only location can report `pelipper-only-source-owned-by-pelipper`. This is intentional fail-safe behavior until Pelipper's own spawn authority is proven and adapted.

## Diagnostics
New command:
- `teamup_density status`
- `teamup_density sources`
- `teamup_density reapply`

Important telemetry fields:
- `Raw`
- `Eligible`
- `BossExcluded`
- `ProtectedExcluded`
- `PelipperSignals`
- `Wanted`
- `Spawned`
- `FactoryRejected`
- `UnsafeRejected`
- `SameType`
- `VanillaFallback`
- `CustomRejected`
- `Suppress`

## CI
Run: `34335298611`
Job: `102413165643`
Result: SUCCESS
Artifact ID: `10097464317`
Artifact wrapper digest: `sha256:25897283347dad08b53816646c350d09ee9ceb60be5d0c3b1507a1b65eb41749`
Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.21_UNIVERSAL_MONSTER_DENSITY_TEST.zip`
Inner SHA256: `9da7d8a581c16fd692cfaa827a9e47ea876c5eee5fa9b1759db2caf0825cf80a`
Materialized source commit: `73701a1`

CI acceptance:
- SOURCE ACCEPTANCE: PASS
- UNIVERSAL DENSITY STATIC AUDIT: PASS
- BOSS/SCRIPT POLICY STATIC AUDIT: PASS
- CUSTOM/CARDCHA SPAWN STATIC AUDIT: PASS
- PELIPPER OWNERSHIP STATIC AUDIT: PASS
- LATE-SPAWN RETRY STATIC AUDIT: PASS
- 6.7.13-6.7.20 CARRY-FORWARD: PASS
- Build succeeded, 0 warnings, 0 errors
- BINARY ACCEPTANCE: PASS

## Live verification still required
1. Vanilla combat map: verify total normal monster count trends toward x2.5 within safe tile/cap limits.
2. Cardcha real combat map: verify normal monsters are classified `ELIGIBLE`, same-type extras spawn when safe constructors exist, boss/test actors remain excluded.
3. Modded custom map whose name contains no legacy combat keyword: verify density still applies.
4. Late-spawn map: verify retries catch monsters that appear after initial warp.
5. Boss + normal adds: boss must not increase baseline; only normal adds should drive density.
6. Pelipper-only area: verify `PELIPPER` classification and no fabricated duplicate Pokemon/proxies.
7. Recheck Pelipper capture 10% and NPC-only companion invariants.

Do not merge to `main` until live smoke tests pass. CI success is not live gameplay verification.
