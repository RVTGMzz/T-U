# Team Up! — Alpha 6.7.14 Development Handoff

## Checkpoint

- Version: `0.2.0-alpha.6.7.14`
- Branch: `v0.2-alpha6-7-14-npc-only-lock-capture-ceasefire`
- Base: Alpha 6.7.13
- CI run: `34303002148`
- CI result: **PASS**
  - SOURCE ACCEPTANCE: PASS
  - CAPTURE CEASEFIRE STATIC AUDIT: PASS
  - NPC-ONLY SOURCE LOCK STATIC AUDIT: PASS
  - 6.7.13 LIVE-FIX CARRY-FORWARD: PASS
  - LOCKED PARTY/PELIPPER/BOSS/RANK INVARIANTS: PASS
  - Build: **0 warnings / 0 errors**
  - BINARY ACCEPTANCE: PASS
- Materialized source commit: `5baf1d1`
- Artifact: `TeamUp_v0.2.0-alpha.6.7.14_NPC_ONLY_LOCK_CAPTURE_CEASEFIRE_TEST.zip`
- Artifact SHA256: `62034625950af0b3befe88a9f4d6848edf01dfae846f4e5ed298709c33b7d5fe`
- Actions artifact ID: `10085609998`

## 6.7.14 changes

### Durable NPC-only Pelipper source lock

The player's explicit NPC-only choice is represented by `Ronvotri.TeamUp/PelipperCompanionOptOut=true` on the owner NPC. Older reconcile paths generally enforced that choice only after a linked Team Up companion row existed or after Pelipper exposed a live actor.

6.7.14 adds a host-side 10-tick lock that scans active Team Up NPC owners with the durable opt-out marker and enforces Pelipper source enabled=false even when Team Up has **no linked companion row yet**. This closes the late-materialization hole where a configured Aerodactyl/Pokemon could appear after recruitment/map refresh.

Diagnostic command:

`teamup_npc_only_lock`

Expected converged NPC-only row: `enabled=False`, `sourceLive=False`.

### Capture-floor ceasefire marker

6.7.13 gave unowned Pelipper Monster combat proxies a durable `PelipperWildCombatProxy` identity so capture safety could recognize the actual HP-bearing proxy.

6.7.14 separates identity from offensive opt-in:

- `PelipperWildCombatProxy=true` remains on the unowned proxy at all HP levels;
- when `PelipperCaptureSafetyService.IsProtected(monster)` is true, Team Up removes `Ronvotri.TeamUp/CombatTarget`;
- the durable wild-proxy marker remains, so `takeDamage`, `damageMonster`, and the dedicated per-tick capture watchdog continue to enforce the floor;
- if HP later rises above the floor, the normal combat probe may restore CombatTarget.

This makes the <=10% state a real Team Up ceasefire rather than only a damage clamp.

## Mandatory live acceptance

1. Invite the affected NPC with configured Aerodactyl/Pokemon and choose **NPC only**.
2. Change maps / wait / enter combat. The configured Pokemon must not materialize later.
3. Run `teamup_npc_only_lock`; expected `enabled=False`, `sourceLive=False` for that NPC.
4. With Farmer + 1 Team Up NPC + Farmer-owned Pokemon, fight a wild Pelipper Pokemon down to the capture floor.
5. Team Up NPC must disengage and the wild Pokemon must not lose HP below the floor.
6. Run `teamup_capture_proxy` while target is at floor; expected proxy row should show `target=False`, `proxy=True`, and damage budget 0.
7. Test at least two wild species.

If recruitment still shows only one choice, run:

`teamup_recruit_probe <NPC name>`

If capture floor still fails, run while the target is still alive:

`teamup_capture`
`teamup_capture_proxy`

## Carry-forward invariants

- Total people cap remains 5 including online Farmers.
- Shared external combat companion cap remains 2/2 farm-wide.
- Pelipper Town remains source authority for companion spawn/despawn/render/controller/AI.
- Team Up does not reintroduce Pelipper render/visibility suppression.
- 6.7.11 boss/add coordination and large-hitbox approach are retained.
- 6.7.12 Rank S contrast is retained.
- 6.7.13 capture-proxy and recruitment-intent fixes are retained.
- Gunther route guard remains present but is still **not live-verified** under the original crash conditions.

## Merge policy

Do **not** merge to `main` merely because CI passed. Live acceptance of the Aerodactyl NPC-only path and the <=10% capture-floor party path remains mandatory.
