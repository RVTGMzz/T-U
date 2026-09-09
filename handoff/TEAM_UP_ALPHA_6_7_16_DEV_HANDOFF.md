# Team Up! — Alpha 6.7.16 Development Handoff

## Checkpoint

- Version: `0.2.0-alpha.6.7.16`
- Branch: `v0.2-alpha6-7-16-combat-telemetry-compat-audit`
- Base: Alpha 6.7.15
- CI run: `34310526172`
- CI result: **PASS**
  - SOURCE ACCEPTANCE: PASS
  - READ-ONLY COMBAT TELEMETRY AUDIT: PASS
  - RUNTIME COMPATIBILITY MATRIX AUDIT: PASS
  - 6.7.13/14/15 LIVE-GUARD CARRY-FORWARD: PASS
  - LOCKED PARTY/PELIPPER/BOSS/RANK INVARIANTS: PASS
  - Build: **0 warnings / 0 errors**
  - BINARY ACCEPTANCE: PASS
- Artifact: `TeamUp_v0.2.0-alpha.6.7.16_COMBAT_TELEMETRY_COMPAT_AUDIT_TEST.zip`
- Artifact SHA256: `acb12d4773be265f1178424b882a70710833110d364b0464c8318a36bea63438`
- Actions artifact ID: `10088193301`
- Actions wrapper digest: `sha256:30d48ae08803e8f07f8644cf37eb9a1eb1e57b16f7993b5baeebaa49d3da5115`

## Scope

Alpha 6.7.16 is diagnostics-only. It deliberately does not change Pelipper ownership, NPC recruitment, capture-floor behavior, combat targeting, party saves, schedules, or source-mod state.

## New diagnostics

### `teamup_combat_report`
Exports `diagnostics/TeamUp_Combat_latest.txt` and captures, per online Farmer:
- current strategy;
- active Team Up NPC state, role, engagement, rank and HP;
- current CombatService target;
- attack/signature cooldowns;
- target-lock and combat path-retry counters;
- target HP, bounding-box gap distance and role attack range;
- capture-protected target state;
- current-location alive threats, major targets (`MaxHealth >= 300`) and capture-protected count.

Warnings include stale dead targets and any capture-protected target that remains in Team Up's CombatService target table.

### `teamup_compat_audit`
Exports `diagnostics/TeamUp_Compatibility_latest.txt` and captures:
- static `CombatRosterIntegrityService` result;
- unique runtime NPC matrix;
- recruitment-candidate classification;
- profile/source/primary+secondary role/rank/kit coverage;
- warnings for runtime recruit candidates missing a combat profile or kit;
- available profile catalog counts by source;
- authored banter, context-banter and chemistry coverage totals.

### `teamup_diag_all`
Exports `diagnostics/TeamUp_Diagnostic_bundle_latest.txt` and combines:
1. Alpha 6.7.15 preflight snapshot;
2. Alpha 6.7.16 combat telemetry;
3. Alpha 6.7.16 compatibility audit;
4. one combined PASS/WARN result.

For the user's current Windows install, the combined file will be:
`E:\SteamLibrary\steamapps\common\Stardew Valley\Mods\Team Up\diagnostics\TeamUp_Diagnostic_bundle_latest.txt`

## Read-only guard

CI rejects Alpha6716 if it contains mutation calls for:
- Pelipper opt-out / source enabled state;
- companion state/add/link;
- monster health;
- attacks;
- desired deployment;
- Team Up control/release;
- party save writes.

This keeps diagnostics from becoming a second authority layer.

## Carry-forward status

- Alpha 6.7.13 explicit Pelipper wild combat-proxy identity retained.
- Alpha 6.7.14 NPC-only source lock + capture ceasefire retained.
- Alpha 6.7.15 preflight retained.
- 5-person people cap retained.
- Shared external combat companion cap 2/2 retained.
- 6.7.11 boss/add coordination retained.
- 6.7.12 Rank S contrast retained.
- Gunther route guard retained but **still not live-verified**.
- Reported Luther/Gunther + Aerodactyl and <=10% capture-floor regressions remain live acceptance gates.

## Next safe work while live testing is unavailable

Continue with content/data-only Banter + Chemistry expansion on a new branch. Do not modify Pelipper, Gunther route, capture-floor mechanics or CombatService gameplay until the current live gates have been tested.

Do not merge this checkpoint to `main` merely because CI passed.
