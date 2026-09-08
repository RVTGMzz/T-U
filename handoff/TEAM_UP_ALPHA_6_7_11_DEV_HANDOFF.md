# Team Up! — Alpha 6.7.11 Development Handoff

## Checkpoint

- Version: `0.2.0-alpha.6.7.11`
- Branch: `v0.2-alpha6-7-11-elite-boss-coordination`
- Base checkpoint: `main` / `0.2.0-alpha.6.7.10`
- CI run: `34247888245`
- CI result: **PASS**
  - SOURCE ACCEPTANCE: PASS
  - ELITE/BOSS COORDINATION STATIC AUDIT: PASS
  - GUNTHER ROUTE FIX CARRIED FORWARD: PASS (live verification still required)
  - Build: **0 warnings / 0 errors**
  - BINARY ACCEPTANCE: PASS
- Artifact: `TeamUp_v0.2.0-alpha.6.7.11_ELITE_BOSS_COORDINATION_TEST.zip`
- Artifact SHA256: `82cb80bef29a0532892b13f7dded5f87ec1de916c6cf8b9d53bf5ff259c089fe`

## Why 6.7.11 is separate from main

`main` intentionally remains on Alpha 6.7.10 so the original Gunther `NPC.loadEndOfRouteBehavior` crash can be live-tested against a clean route-fix checkpoint.

Do **not** mark either 6.7.10 or 6.7.11 route behavior as live verified until the user reproduces the original Gunther circumstances in Stardew/SMAPI and confirms the error is gone.

## Alpha 6.7.11 combat changes

### 1. Major-threat coordination no longer bypasses anti-dogpile

Alpha 6.7.10 added a direct major-threat override for Damage/Control/aggressive members. That could bypass `assignedCounts` and make every eligible attacker choose the same boss while adds were alive.

6.7.11 adds a soft coordinated-attacker cap:

- Normal strategies: `2` coordinated attackers on one major threat before overflow prefers adds.
- `BossFocus`: `3` coordinated attackers before overflow prefers adds.
- If the boss is the only living target, the cap does **not** make party members idle. Normal acquisition remains the fallback.
- Tank and Healer retain their role-specific targeting path and are not forced through the major-threat override.

### 2. Large-hitbox movement is now hitbox-aware

Alpha 6.7.10 already used bounding-box distance to decide whether an NPC was in attack range, but movement still searched around `target.Tile`, which is effectively the monster center/anchor.

6.7.11 changes combat approach search to:

- inspect the monster's full `GetBoundingBox()` footprint;
- search walkable land/bridge tiles around that footprint;
- reject tiles inside the monster hitbox;
- accept approach tiles within the role's attack-range gap from the hitbox;
- prefer the reachable candidate closest to the NPC.

This should reduce giant-boss pathing that tries to walk toward the middle of the sprite/hitbox.

## Locked invariants carried forward

- Total people cap: **5**, including online Farmers.
- Single-player: Farmer + up to 4 NPCs.
- Shared external combat companion cap: **2/2**.
- Pelipper Town remains source authority for companion runtime ownership.
- No fake hide / render suppression for Pelipper companions.
- Human NPCs never path/warp onto bare water; real bridges remain walkable.
- Combat path retry: `24` ticks.
- Combat movement pulse: `3` ticks.
- Gus keeps vanilla 4-direction walk/facing animation.
- Original Alpha6 prototype signature authority remains single-owner.
- Rank remains separate from Special Recruit badges.

## First live-test order

### A. Gunther route regression, still priority #1

1. Recruit Gunther in the same museum/schedule circumstances that previously produced the crash.
2. Change maps and cross a schedule/end-of-route boundary.
3. Confirm no `NullReferenceException` at `NPC.loadEndOfRouteBehavior`.
4. Run `teamup_route_guard`; expected `routeGuard=True`.

### B. 6.7.11 elite/boss coordination

1. Fight an elite/boss with `MaxHealth >= 300` while adds are alive.
2. Damage/Control/aggressive actors should still put meaningful pressure on the major threat.
3. Outside Boss Focus, once two coordinated attackers are assigned to the major threat, later eligible attackers should be able to spread onto adds.
4. In Boss Focus, the soft coordinated cap is three before overflow prefers adds.
5. If only one boss remains, all useful members should keep attacking rather than idle.
6. Tank/Healer should retain their role-specific decisions.

### C. Large boss hitbox movement

1. Use a monster/boss with a visibly large hitbox.
2. Watch melee NPC approach behavior.
3. NPC should approach a nearby valid edge around the hitbox instead of repeatedly trying to path into its center.
4. Once bounding-box gap is inside attack range, the NPC should stop and attack.

### D. Regression checks

- Gus vanilla walk/facing animation.
- Farmer + 4 NPC = 5/5; next NPC blocked.
- Pelipper never exceeds 2/2.
- `teamup_roster_audit` and capture any WARNING.
- Rank UI: Abigail/Alex/Haley/Maru/Evelyn = A; MiMi/Marlon = S; Sudoku = A.

## Merge policy

Do not merge 6.7.11 into `main` merely because CI passed. Preferred sequence:

1. live-verify Gunther route fix;
2. live-test the 6.7.11 combat changes;
3. merge when both route safety and combat behavior are clean enough for the next checkpoint.
