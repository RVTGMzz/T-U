# Team Up Alpha 6.7.20 plan

Base: `v0.2-alpha6-7-19-mutation-encounters`.

Goal: deepen Mutation Encounters without touching Pelipper/Gunther/capture ownership paths.

Planned work:
- Mutant combat footprint/hitbox follows the visual mutation scale through Harmony-safe bounding-box expansion.
- Prevent double expansion when an override calls a patched base `GetBoundingBox`.
- Minion wave first attempts a safe same-runtime-type sibling constructor (`Vector2` or a recognized `Vector2,int level` shape); falls back to GreenSlime when the runtime type cannot be safely constructed.
- Keep all boss/script/quest/Pelipper/test exclusions and no-recursion/no-economy guards.
- Add telemetry showing same-type vs fallback minion counts and hitbox patch coverage.
- CI/static audits only. Live verification remains required.
