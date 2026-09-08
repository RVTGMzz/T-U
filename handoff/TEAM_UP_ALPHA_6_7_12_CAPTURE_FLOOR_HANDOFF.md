# Team Up! — Alpha 6.7.12 Capture Floor Handoff

## Checkpoint

- Version: `0.2.0-alpha.6.7.12`
- Branch: `v0.2-alpha6-7-12-capture-floor-rank-s`
- Base: `v0.2-alpha6-7-11-elite-boss-coordination`
- CI run: `34255632091`
- CI result: **PASS**
  - SOURCE ACCEPTANCE: PASS
  - CAPTURE FLOOR MULTI-LAYER STATIC AUDIT: PASS
  - S-RANK CONTRAST STATIC AUDIT: PASS
  - 6.7.11 COMBAT COORDINATION CARRIED FORWARD: PASS
  - GUNTHER ROUTE GUARD CARRIED FORWARD: PASS (live verification still required)
  - Build: **0 warnings / 0 errors**
  - BINARY ACCEPTANCE: PASS
- Artifact: `TeamUp_v0.2.0-alpha.6.7.12_CAPTURE_FLOOR_RANK_S_TEST.zip`
- Mod ZIP SHA256: `0f8ead2ff0ba1a06c72f017ef5e0a878e4408dcbad030d149adceee52a7d7311`

## Live regression that triggered 6.7.12

The user confirmed Pelipper's 10% mercy/capture behavior works with Farmer + player Pokemon only, but immediately stops being reliable after inviting one NPC into Team Up. In that state NPC damage and even the player's own Pokemon could continue until the wild Pokemon was KO'd.

An earlier SMAPI log proved Team Up's old hard-floor patch was loaded and had patched 29 `Monster.takeDamage` implementations, so this was not simply a missing registration problem.

## 6.7.12 capture-floor changes

### 1. takeDamage scan includes abstract declaring types

The old scanner skipped every abstract `Monster`-derived type. A concrete custom `takeDamage` implementation declared on an abstract custom base could therefore be inherited by the live wild proxy without ever receiving Team Up's floor prefix.

6.7.12 scans all `Monster`-derived declaring types and skips only methods that are themselves abstract.

### 2. GameLocation.damageMonster guard

6.7.12 also patches compatible `GameLocation.damageMonster` entry points discovered by reflection. If an area damage rectangle overlaps a capture-limited wild Pelipper monster, named `damage` / `minDamage` / `maxDamage` integer arguments are clamped to the smallest active capture budget before fan-out.

This is intentionally safety-first. It never changes movement, rendering, ownership, capture data, trajectory, precision or knockback parameters.

### 3. Per-tick last-resort live floor repair

`PelipperCaptureSafetyService.RepairCurrentLocationFloors` runs every host update tick. If a still-live wild Pelipper proxy has somehow been pushed below the active mercy floor through a custom direct-health path, Team Up restores it to the floor.

It deliberately does **not** resurrect `Health <= 0` actors, to avoid duplicating death/loot/removal side effects.

### 4. Diagnostics

New SMAPI console command:

`teamup_capture`

It reports:

- capture safety enabled state;
- active threshold;
- number of patched `takeDamage` methods;
- number of patched area-damage methods;
- current wild target HP / max HP / floor / remaining damage budget.

If the live bug survives 6.7.12, run this command while the target is still present and capture its output before leaving the session.

## Rank S readability

Rank identity is unchanged. The roster color for S changed from pale gold `(218,155,48)` to dark bronze `(132,70,12)` so `[S] Marlon` and `[S] MiMi` remain readable on the orange/gold Codex background.

## Locked regressions carried forward

- Total people cap: 5 including online Farmers.
- Single player: Farmer + up to 4 NPCs.
- External combat companions: 2/2.
- Pelipper remains source authority for Pokemon runtime ownership/render/control.
- No legacy fake-hide/render suppression.
- Human NPC pathing remains land/real-bridge safe.
- Gus vanilla walk/facing retained.
- Alpha 6.7.11 boss coordination + hitbox-aware approach retained.
- Alpha 6.7.10 Gunther route guard retained.

## Live test priority

1. Baseline: Farmer + own Pokemon versus wild Pokemon, 10% mercy enabled.
2. Add exactly one NPC to Team Up and repeat. This is the critical regression reproduction.
3. Add an NPC-linked Pokemon if available and repeat with up to 2/2 companions.
4. Test a high-damage hit from ~11-15% HP and confirm it clamps to the floor.
5. Test an AoE overlapping the capture target and another enemy.
6. Inspect Rank S readability.
7. Gunther/Aerodactyl remains a separate unresolved live issue. Do not mark it fixed because 6.7.12 CI is green.

If capture still fails, run `teamup_capture`, then immediately exit and provide:

`%appdata%\StardewValley\ErrorLogs\SMAPI-latest.txt`

On the user's current Windows install, the Team Up mod folder is:

`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up`

## Merge policy

Keep `main` unchanged until live verification. Do not merge 6.7.12 solely from CI results.
