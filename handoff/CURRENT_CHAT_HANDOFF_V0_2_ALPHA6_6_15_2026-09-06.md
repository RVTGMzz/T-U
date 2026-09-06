# Team Up v0.2.0-alpha.6.6.15 handoff

Date: 2026-09-06

## Branches
- Development: `v0.2-alpha6-6-15-companion-intent-recall-capture-floor`
- Handoff: `v0.2-alpha6-6-15-companion-intent-recall-capture-floor-handoff`
- Authoritative input/source HEAD: `cc17a5a1af1fa74fa55f7a1bbd23e0e4a30672b3`

## Authoritative CI
- Run: `34042370048`
- Job: `101511322177`
- Result: success
- Build: 0 warnings / 0 errors
- Source acceptance: PASS
- Package verification: PASS
- Materialization: `No materialized source diff.`
- Artifact: `9992070274`
- Artifact wrapper digest: `sha256:dd1683cda952be44dfaaafd71fd2b0ed6f10302d83429ebc8f444291bd586383`

## Package
- `TeamUp_v0.2.0-alpha.6.6.15_COMPANION_INTENT_RECALL_CAPTURE_FLOOR_TEST.zip`
- SHA256: `2dfd1e270f39f0a6cfed2f291deee987262afcbe884ba09b18fcac63def94051`
- Contains `Team Up/manifest.json`, `Team Up/TeamUp.dll`, `Team Up/i18n/default.json`, `Team Up/i18n/vi.json`.

## Alpha 6.6.15 fixes

### Durable NPC companion intent
`ApplyPelipperRecruitChoiceAlpha663` now stores owner opt-out even if Pelipper companion actor/descriptor is not detectable at recruitment time. Choosing NPC-only is durable and reconcile must not auto-activate the linked Pokemon later.

When a Pelipper linked actor appears later, reconcile may register a durable linked record, but opted-out owners register it as Standby. This gives a future explicit Call path without stealing a combat companion slot.

### Linked companion Call / Return / Replace
New `ModEntry.Alpha6615.cs` adds companion management while talking to a recruited NPC. Party Menu key opens the linked companion control. Active linked Pokemon can Return to Standby. Standby linked Pokemon can Call into an open slot. At 2/2, Call opens an explicit replacement question and does not silently evict a companion.

Manual Return sets the NPC owner opt-out marker so 30-tick reconcile cannot immediately reactivate it. Explicit Call clears opt-out and activates the linked unit.

### Player-owned Standby recall
Alpha 6.6.15 detects a registered player Pelipper companion that is Standby but was manually re-summoned by the source mod. If the shared pool has room, it is promoted Active. If the pool is 2/2, Team Up queues a Replace/Cancel question instead of silently leaving it permanently retracted.

This detection is reflection-only and looks for source runtime booleans such as CompanionEnabled, FollowingOwner, Deployed, Summoned. Live testing is required because Pelipper may expose different runtime members.

### Global Pelipper capture floor
Alpha 6.6.14 only clamped Team Up CombatService damage. Alpha 6.6.15 enables Harmony and adds `PelipperCaptureDamagePatch`, which patches loaded Monster subclasses' `takeDamage(int, ...)` implementations and clamps the first damage argument through `PelipperCaptureSafetyService.ClampDamage`.

Goal: source-owned Pelipper companion attacks cannot push a wild Pelipper combat proxy below the capture threshold. Team Up NPC offensive targeting remains stopped at the protected threshold while heal/revive/buff/guard support continues.

Capture threshold adapter remains read-only, probing Pelipper config/settings when discoverable and falling back to 10%.

## Regression locks
- People cap 6 total including online Farmers.
- Shared combat companion cap 2.
- Pelipper source movement authority remains source-owned; Team Up Follow does not control it.
- Water/bridge pathfinding optimizations and land-safe humanoid NPC targets retained.
- Contextual NPC HP bars retained.
- Switch controller equipment fixes retained.
- Codex one-row navigation and no companion/summon profiles retained.
- Friendship curfew and personality farewell retained.
- Single Pelipper wild target combat retained above capture threshold.

## Priority live smoke
1. Recruit NPC with linked Pelipper Pokemon, choose NPC-only while Farmer already has companion slots in use. Wait several seconds. Linked Pokemon must not activate or evict Farmer Pokemon.
2. Talk to that NPC and use Party Menu key. Call linked Pokemon with an open slot, then Return it. Return must remain Standby after reconcile.
3. Fill 2/2, Call a linked NPC Pokemon. Replace/Cancel prompt must appear. Cancel preserves current slots. Replace returns exactly selected victim and activates requested companion.
4. Re-summon a registered player-owned Standby Pelipper Pokemon. Open slot should promote it; full pool should request replacement if the Pelipper runtime deployment member is detectable.
5. Enable Pelipper 10% mode and let both Team Up NPCs and Pelipper companions attack one wild Pokemon. HP must not cross the capture floor. Heal/buff/revive/guard should continue.
6. Re-test water bridge performance, NPC land safety, no Pokemon flicker, HP bars, Switch equipment, Codex, curfew/farewell.

## Caveats
- Global capture floor uses Harmony patching of `Monster.takeDamage`. If Pelipper applies companion damage by directly mutating health or using a completely separate non-Monster damage path, live test may still expose a bypass. Get a fresh SMAPI log immediately if that happens.
- Player Standby recall replacement prompt requires Team Up to observe a source deployment flag. Linked NPC companion Call/Return is directly managed by Team Up state and should not depend on that player-summon observation path.
