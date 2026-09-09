# Team Up Alpha 6.7.20 Development Handoff

Branch: `v0.2-alpha6-7-20-mutant-footprint-same-type-minions`

Version: `0.2.0-alpha.6.7.20`

Materialized source commit: `879810bf44dc2ac5ae99321033db58a3adedb391`

CI run: `34325738779` SUCCESS

Artifact ID: `10093678181`

Inner test ZIP: `TeamUp_v0.2.0-alpha.6.7.20_MUTANT_FOOTPRINT_SAME_TYPE_MINIONS_TEST.zip`

Inner SHA256: `6d5f28d7a1e1a004e9100c7673f1f028e38c33a1ff5148968d1f82ef05e1e215`

## Added in 6.7.20

- Mutant combat/collision footprint now follows the visual mutation scale that actually applied.
- Default requested mutant visual/footprint scale remains x3.
- Bounding box size has a 768 px safety cap for unusually large custom monsters.
- Runtime `Character.GetBoundingBox` + Monster overrides are patched with a thread-local nesting guard so override->base call chains cannot multiply the footprint twice.
- Mutation minions first attempt to spawn the same runtime Monster type as the mutant.
- Safe same-type constructor shapes only:
  - `(Vector2 position)`
  - `(Vector2 position, int level)` only when reflection metadata names the int as level/mine/difficulty/depth.
- Unsupported custom constructor shapes fail closed to GreenSlime fallback.
- No `MemberwiseClone`, FormatterServices, NetField copying, or fabricated opaque constructor arguments.
- Same-type/fallback telemetry added to `teamup_mutation status` and new command `teamup_mutation_detail`.

## Preserved mutation policy

- Default mutation chance 5%.
- Mutant HP x3.
- Main combat stats x2, with existing speed safety cap.
- Requested visual scale x3.
- 2-4 normal minions.
- Normal vanilla and normal Cardcha/custom monsters remain eligible.
- Boss/script/quest-protected actors excluded.
- Pelipper wild/capture/companion/proxy actors excluded.
- Cardcha test arena/dummy/kill-target actors excluded.
- Surge monsters, mutation minions and mutants cannot recursively mutate.

## Live verification still required

CI proves compilation and static invariants only. Live Stardew testing still needs to verify:

1. death interception really replaces a normal death with mutation at 5%/forced 100% test;
2. x3 footprint feels correct and does not cause unacceptable narrow-corridor trapping;
3. player and Team Up attacks register across the enlarged body;
4. same-type minion constructors behave correctly for the monster classes encountered in vanilla/Cardcha;
5. unsupported custom monsters fall back safely;
6. Pelipper capture 10% and Gunther/Aerodactyl regressions remain unaffected.

Useful commands:

- `teamup_mutation status`
- `teamup_mutation list`
- `teamup_mutation force`
- `teamup_mutation_detail`
- `teamup_diag_all`
- `teamup_capture`
- `teamup_capture_proxy`

If a live bug occurs, exit the game immediately and collect:

`%appdata%\StardewValley\ErrorLogs\SMAPI-latest.txt`

Team Up diagnostics:

`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Diagnostic_bundle_latest.txt`

Do not merge to `main` until live smoke is acceptable.
