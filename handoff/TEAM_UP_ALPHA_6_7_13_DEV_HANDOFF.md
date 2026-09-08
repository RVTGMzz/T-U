# Team Up! — Alpha 6.7.13 Development Handoff

## Checkpoint

- Version: `0.2.0-alpha.6.7.13`
- Branch: `v0.2-alpha6-7-13-capture-proxy-recruit-intent`
- Base: Alpha 6.7.12
- CI run: `34271123585`
- CI result: **PASS**
  - SOURCE ACCEPTANCE: PASS
  - LIVE CAPTURE-PROXY REGRESSION AUDIT: PASS
  - RECRUITMENT INTENT REGRESSION AUDIT: PASS
  - DEDICATED WATCHDOG EVENT AUDIT: PASS
  - Build: **0 warnings / 0 errors**
  - BINARY ACCEPTANCE: PASS
- Artifact: `TeamUp_v0.2.0-alpha.6.7.13_CAPTURE_PROXY_RECRUIT_INTENT_TEST.zip`
- Artifact SHA256: `eecd2be0f175ecc9f0d2604ef3098a1f85948fad86f6151db471295298d2b98a`

## Why 6.7.12 failed live

### Capture floor

Pelipper Town constructs each wild encounter as two layers: a visible Wild NPC and a separate Monster combat proxy. Team Up's 6.7.12 capture policy still relied too heavily on the proxy itself exposing direct Wild identity.

A second concrete bug existed in event wiring: the 6.7.12 last-resort capture repair was placed in `OnAlpha6615UpdateTicked`, while Alpha 6.6.26's single companion authority intentionally unsubscribes that legacy update handler. The repair therefore never ran in the current architecture.

### NPC + Pokemon recruitment

Pelipper's configured species and its `Companion enabled` state are intentionally separate. Pelipper keeps the selected species when the companion is disabled. Team Up 6.7.12 incorrectly returned no configured descriptor when `Companion enabled=false`, which collapsed the recruitment UI to a single NPC-only Invite option.

## 6.7.13 fixes

1. Adds explicit `PelipperWildCombatProxy` identity to the same unowned Pelipper Monster proxies Team Up already opts into combat.
2. Capture policy accepts direct Wild identity, the explicit proxy marker, and the combat-target marker.
3. Keeps the 6.7.12 global `Monster.takeDamage` and `GameLocation.damageMonster` clamps.
4. Moves the last-resort capture repair to a dedicated `OnAlpha6713CaptureFloorUpdateTicked` event that Alpha 6.6.26 does not unsubscribe.
5. Configured villager species lookup no longer disappears merely because `Companion enabled=false`.
6. Recruitment checks live companion first, then configured species intent.
7. Adds internal-name/display-name Pelipper config-key resolution for expansion NPCs while canonical Team Up ownership stays bound to `NPC.Name`.
8. Initial NPC-only / NPC+Pokemon source writes use the resolved Pelipper key.
9. Native source convergence now considers Pelipper's configured enabled bit in addition to live-runtime detection.

## New diagnostics

- `teamup_recruit_probe <NPC name>`
  - internal/display name
  - resolved Pelipper config key
  - configured enabled state
  - configured species
  - live species
  - species Team Up will offer in recruitment UI
- `teamup_capture_proxy`
  - every Pelipper Monster proxy in current location
  - HP/max HP
  - runtime type
  - Team Up combat marker
  - wild proxy marker
  - final capture classification
  - current damage budget
- Existing `teamup_capture` remains available.

## Mandatory live tests

1. Re-test the reported Luther/Gunther + Aerodactyl case. Recruitment must show NPC-only and NPC+Pokemon choices.
2. NPC-only must prevent Aerodactyl from joining.
3. NPC+Pokemon must call Aerodactyl only when capacity permits.
4. With Farmer + Team Up NPC + player Pokemon, wild Pokemon must stop receiving friendly damage at the active Pelipper capture floor.
5. Test more than one wild species.
6. If either issue remains, capture `teamup_recruit_probe <NPC>` and/or both `teamup_capture` + `teamup_capture_proxy` while the target is still alive.

## Carry-forward status

- 5-person people cap retained.
- Shared external combat companion cap 2/2 retained.
- 6.7.11 boss/add coordination retained.
- 6.7.12 Rank S contrast retained.
- Gunther route guard retained but is still **not live-verified** until the original crash conditions are reproduced successfully.

Do not merge this checkpoint merely because CI passed. The two reported live regressions are the acceptance gate.
