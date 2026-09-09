# Team Up! — Alpha 6.7.15 Development Handoff

## Checkpoint

- Version: `0.2.0-alpha.6.7.15`
- Branch: `v0.2-alpha6-7-15-preflight-diagnostics`
- Base: Alpha 6.7.14
- CI run: `34306801917`
- CI result: **PASS**
  - SOURCE ACCEPTANCE: PASS
  - READ-ONLY PREFLIGHT DIAGNOSTICS AUDIT: PASS
  - 6.7.13/6.7.14 LIVE-GUARD CARRY-FORWARD: PASS
  - LOCKED PARTY/PELIPPER/BOSS/RANK INVARIANTS: PASS
  - Build: **0 warnings / 0 errors**
  - BINARY ACCEPTANCE: PASS
- Artifact: `TeamUp_v0.2.0-alpha.6.7.15_PREFLIGHT_DIAGNOSTICS_TEST.zip`
- Inner mod ZIP SHA256: `2d9d85d5b4540115df7dd4e96f47c3f81f4fe9776f5316d68733541f35bfc8ee`
- Actions wrapper artifact ID: `10086936014`

## Purpose

Alpha 6.7.15 deliberately adds no new gameplay behavior. It exists so live testing after work can be much faster and less error-prone.

New command:

`teamup_preflight`

It creates one read-only snapshot covering:

- Team Up version and location;
- Gunther/NPC route guard status;
- capture-safety enabled state and threshold;
- Pelipper 1.1.9 native bridge capabilities/status;
- total people usage vs the 5-person cap;
- reserved/effective external combat companion usage vs 2/2;
- active NPC roster rows with rank, NPC-only intent, Pelipper config key, source enabled/live state, linked Pokemon;
- every Pelipper Monster combat proxy in the current location with HP, wild/proxy/target classification and current damage budget;
- automatic PASS/WARN summary for obvious violations.

The command also exports:

`<Team Up mod folder>/diagnostics/TeamUp_Diagnostic_latest.txt`

SMAPI prints the exact absolute path after export, so the tester does not need to remember where the file lives.

## Read-only guarantee

`ModEntry.Alpha6715.cs` is audited to contain no calls to Team Up/Pelipper gameplay mutation routes such as:

- SetOwnerOptOut
- TrySetVillagerCompanionEnabled
- TrySetPelipperNpcSourceEnabledAlpha6621
- SetCompanionState
- TryAddPlayerCompanion
- TryLinkCompanion
- Health assignment
- PerformAttack
- SetDesiredDeployment

The only bridge setup call is `ConfigurePelipperApiBridgeAlpha6619`, used to discover/read the existing runtime.

## Mandatory live use later

### NPC-only / Aerodactyl

1. Invite the affected NPC as NPC-only.
2. Wait / change map.
3. Run `teamup_preflight`.
4. Expected row: `npcOnly=True`, `configuredEnabled=False`, `sourceLive=False`.
5. Aerodactyl must not materialize.

### Capture floor

1. Use Farmer + Team Up NPC + player Pokemon.
2. Bring a wild Pokemon to the active capture floor.
3. Run `teamup_preflight` while the target is still alive.
4. Expected proxy at floor: `budget=0`, `protected=True`, and `target=False` after ceasefire convergence.
5. Friendly NPC/Pokemon must not kill the wild target.

If WARN appears, upload `TeamUp_Diagnostic_latest.txt`.

## Carry-forward status

- Alpha 6.7.13 capture-proxy identity retained.
- Alpha 6.7.14 NPC-only source lock + capture ceasefire retained.
- People cap 5 retained.
- Shared external combat companion cap 2/2 retained.
- Alpha 6.7.11 boss/add coordination retained.
- Alpha 6.7.12 Rank S contrast retained.
- Gunther route guard retained but still **not live-verified** under the original crash conditions.
- Main remains at the clean Alpha 6.7.10 checkpoint. Do not merge 6.7.15 merely because CI passed; live acceptance of the outstanding regressions is still required.
