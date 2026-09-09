# Team Up Alpha 6.7.22 Development Handoff

## Checkpoint

- Branch: `v0.2-alpha6-7-22-pelipper-wild-density-surface-probe`
- Version: `0.2.0-alpha.6.7.22`
- Base: Alpha 6.7.21 Universal Monster Density
- Materialized source commit: `1783f41d1e9851d51bc92cf9465a88c72814f540`
- CI run: `34336648072`
- CI job: `102417491336`
- CI result: SUCCESS
- Artifact ID: `10097988372`
- Wrapper artifact digest: `sha256:effdb9be249b32dbc92640c17e62c9cfe86baa0c864436362d54f43420a564ae`
- Inner mod ZIP: `TeamUp_v0.2.0-alpha.6.7.22_PELIPPER_WILD_DENSITY_SURFACE_PROBE_TEST.zip`
- Inner SHA256: `0acf0189f0c6971ffb33971c43b4315e295fbde79029eb760ff8c458e14790f1`

## Purpose

Alpha 6.7.21 intentionally leaves Pelipper Town wild density source-owned. Existing repository metadata verifies Pelipper companion lifecycle surfaces, but does not verify the native wild population/spawn method or desired-count member. Older user logs show Pelipper has staged `Entry population` with a source-side `desired` count and that each wild spawn creates a visible NPC plus a separate Monster combat proxy. Alpha 6.7.22 therefore discovers the exact source surface read-only before any native adapter is attempted.

## New command

`teamup_pelipper_density_probe`

The command:
- locates Pelipper's live ModEntry through the existing runtime-root locator;
- scans Pelipper assembly method signatures for Wild/Population/Populate/Entry/Encounter/Spawn/Resident/Zone surfaces;
- reads only strongly named primitive runtime values for desired/target/population/count/max/cap/limit/resident candidates;
- counts current visible wild actors and Monster combat proxies using canonical Team Up Pelipper identity;
- classifies the discovery as `EXACT_CANDIDATE`, `AMBIGUOUS`, or `NONE`;
- writes `diagnostics/Pelipper_Wild_Density_Probe_latest.txt`.

## Authority boundary

6.7.22 does NOT:
- invoke unknown Pelipper spawn/population methods;
- write Pelipper fields/properties/config;
- construct Pelipper actors;
- add Pelipper actors to `location.characters`;
- reflection-clone Pelipper state.

6.7.21 fail-closed behavior remains: Pelipper wild/capture/proxy actors can be detected for telemetry, but Team Up does not fabricate them or multiply their population.

## CI gates

PASS:
- source acceptance;
- read-only metadata/primitive probe audit;
- no source invocation/write audit;
- wild/proxy identity audit;
- 6.7.21 Universal Density fail-closed carry-forward;
- locked party/Pelipper/capture/mutation/rank invariants;
- build: 0 warnings / 0 errors;
- binary acceptance.

## Live test required

Install to:
`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up`

Then visit a Pelipper-populated location such as Town, Forest or Beach and run:
`teamup_pelipper_density_probe`

Send this exact file:
`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\Pelipper_Wild_Density_Probe_latest.txt`

If SMAPI errors occur, exit the game immediately and also send:
`%appdata%\StardewValley\ErrorLogs\SMAPI-latest.txt`

## Next branch after evidence

If the live probe exposes one exact, unambiguous source-owned population target/spawn route, create an isolated Alpha 6.7.23 native Pelipper wild-density adapter. Prefer changing Pelipper's own desired entry population or calling its own population authority so Pelipper itself creates visible NPC + combat proxy pairs. Never duplicate the proxy externally.

CI success is not live verification.
