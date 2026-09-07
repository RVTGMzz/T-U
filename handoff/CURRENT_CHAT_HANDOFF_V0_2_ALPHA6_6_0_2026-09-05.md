# Team Up! Chat Handoff - v0.2.0-alpha.6.6.0

Date: 2026-09-05
Status: COMPILE VERIFIED / PACKAGE VERIFIED / DIRECT BUILDER VERIFIED / IN-GAME SMOKE PENDING

## Resume from here

Development branch:

`v0.2-alpha6-6-party-strategy-foundation`

Final direct-builder verified checkpoint:

`d103736c346bfc13939e6a660e3b5dba51edbfb9`

First materialized Alpha 6.6.0 source commit:

`9c5f3bd9d62905b792560a4d588da9e0621b9a2d`

Baseline inherited from:

`v0.2-alpha6-5-3-surge-validation-harness-handoff`

Alpha 6.5.3 handoff commit:

`7dfd45a25be9ebd1326c45a887821f68eba28c1d`

## Why Alpha 6.6.0 exists

Alpha 6.5.3 completed the Surge validation harness. No new in-game failure report was available when this milestone started, so Team Up did not create another fake hotfix or broaden unknown-monster cloning without evidence.

Alpha 6.6.0 instead begins the next broader gameplay layer from the Team Up roadmap: party-wide tactical strategy.

The strategy foundation is intentionally config-backed and does not alter PartySaveData schema.

## Party Strategy contract

New enum:

`src/TeamUp/Core/PartyStrategy.cs`

Strategies:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Default:

`Balanced`

Runtime strategy is provided to `CombatService` through a live `Func<PartyStrategy>` delegate, so changing strategy takes effect without rebuilding party save state.

## Runtime behavior

### Balanced

Baseline Team Up combat behavior.

- engagement radius multiplier: `1.00`
- attack cooldown multiplier: `1.00`

### Defensive

Tighter and more protective posture.

- engagement radius multiplier: `0.78`
- attack cooldown multiplier: `1.08`
- healer/support Farmer recovery threshold receives an extra `+0.10`, capped at `0.95`
- existing hard leash, threat, role targeting, rescue, and retreat systems remain active

### Aggressive

More proactive combat posture.

- engagement radius multiplier: `1.18`
- attack cooldown multiplier: `0.88`
- existing hard leash remains `12` tiles, so aggressive mode does not authorize infinite chasing

### Hold Position

Party can fight around the current position without chasing distant targets.

- engagement radius multiplier: `0.70`
- candidate targets are additionally constrained to within `4.5` tiles of the NPC
- if a selected target is outside that NPC's attack range, Team Up clears movement controllers and halts instead of calling `MoveTowardTarget`
- combat/heal/control behavior remains active once a valid target is in range
- lightweight `HOLD POSITION` feedback can appear when entering the hold behavior

### Boss Focus

Prioritizes the highest-health valid target in the existing candidate set.

- engagement radius multiplier: `1.00`
- attack cooldown multiplier: `0.96`
- candidate ordering first uses descending `Monster.MaxHealth`, then the existing role-aware target score as tie-break
- does not modify monster identity or use blind cloning

## Strategy command

SMAPI console:

`teamup_strategy status`

`teamup_strategy balanced`

`teamup_strategy defensive`

`teamup_strategy aggressive`

`teamup_strategy hold`

`teamup_strategy boss`

Changing strategy:

1. writes `Config.PartyStrategy`;
2. persists through `Helper.WriteConfig(Config)`;
3. calls `Combat.Clear()` so stale target/facing/runtime locks do not bleed into the new tactic;
4. displays lightweight `TEAM STRATEGY` feedback when a world is loaded.

## Save-safety lock

Alpha 6.6.0 does NOT add strategy fields to PartySaveData.

The selected strategy lives in Team Up `config.json` through `ModConfig.PartyStrategy`.

Invalid enum values are normalized back to `PartyStrategy.Balanced` at entry.

Do not migrate PartySaveData merely to store the global strategy unless a later design explicitly needs per-save/per-farm strategy ownership.

## Final build verification

Authoritative direct-builder GitHub Actions run:

`33901774383`

Head SHA:

`d103736c346bfc13939e6a660e3b5dba51edbfb9`

Result: SUCCESS

Verified steps:

- SMAPI build environment: PASS
- direct `BuildV0_2Alpha660.ps1`: PASS
- source acceptance: PASS
- packaged build verification: PASS
- materialization check: PASS, no source diff
- artifact upload: PASS

Compiler result:

- 0 warnings
- 0 errors

Package:

`TeamUp_v0.2.0-alpha.6.6.0_PARTY_STRATEGY_FOUNDATION_TEST.zip`

Package SHA256 from the final direct-builder run:

`d1a1e6ab8b3108de393a74c6ea7a77fd7b2ce21bad54eb5c68b63302d1238823`

GitHub Actions artifact:

- Name: `team-up-alpha6-6-party-strategy-foundation`
- Artifact ID: `9947921694`
- Artifact wrapper size: `197140` bytes
- Uploaded wrapper digest: `sha256:42c78e8cb50d6624afb38ce67796002bb5ddbc6ee310f3a67d2a66402285b378`

Important: the Actions artifact digest is for the wrapper ZIP. The Team Up mod ZIP inside uses the package SHA256 above.

## CI history worth remembering

Run `33901390675` failed before C# compile because the first builder expected the wrong indentation for the existing `AcquireTarget` radius line. The builder failed closed with `Patch anchor missing: strategy engagement radius`.

This was a build-materialization anchor issue, not a gameplay regression.

Run `33901514897` used a temporary bootstrap correction, then compiled and verified Alpha 6.6.0 successfully with 0 warnings / 0 errors. It materialized source commit `9c5f3bd9d62905b792560a4d588da9e0621b9a2d`.

The temporary bootstrap helper was then deleted and the workflow restored to call `BuildV0_2Alpha660.ps1` directly.

Final run `33901774383` proved the direct builder is self-sufficient and idempotent. Materialization reported `No materialized source diff.`

Do not restore `FixAndBuildV0_2Alpha660.ps1`.

## Regression locks retained

Do not regress:

- `HardLeashTiles = 12f`
- `TargetLockDurationTicks = 45`
- `FacingHoldDurationTicks = 10`
- Alpha 6.5.3 `teamup_test surge status|reapply|clear|board`
- non-stacking Surge reapply ownership guard
- Alpha 6.5.2 safe tile placement using `isTileOnMap`, `isTilePassable`, `IsTileBlockedBy(... CollisionMask.All)`
- never restore `isTileLocationTotallyClearAndPlaceable`
- Cardcha test arena Surge suppression
- no blind `Activator.CreateInstance`, `MemberwiseClone`, or custom monster cloning
- MiMi recruitment only after Cardcha friendship/social unlock
- no Cardcha private SaveData/service reflection coupling
- ChaCha remains Special/Farmer Companion, never Main Party
- Sudoku Control/Damage identity and `NINEFOLD SEAL`
- MiMi Support/Control identity and `BROOMTAIL SIGIL`
- Origin story Linus -> Marlon -> First Awakening -> Stronger Together
- 51 SVE/RSV expansion NPC identities/icons/balance
- equipment double-click
- controller focus
- Party Vault drag/drop
- existing role/threat/rescue/retreat/anti-spin behavior

## Build entry points

One-click Windows launcher:

`BUILD_V0_2_ALPHA6.bat`

Direct PowerShell builder:

`BuildV0_2Alpha660.ps1`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_0_PARTY_STRATEGY_FOUNDATION_VI.txt`

Workflow:

`.github/workflows/team-up-alpha6-6-party-strategy-foundation.yml`

## In-game smoke still required

CI confirms compile/source/package contracts only. Alpha 6.6.0 is not yet declared in-game verified.

Priority live tests:

1. `teamup_strategy balanced` and confirm baseline behavior.
2. `defensive`: verify shorter acquisition and earlier healer/support intervention.
3. `aggressive`: verify longer acquisition and slightly faster attack cadence while hard leash remains effective.
4. `hold`: verify NPCs do not chase targets outside attack range but still fight/heal/control nearby.
5. `boss`: use mixed-MaxHealth enemies and confirm the highest-health valid candidate is preferred.
6. switch strategies during combat and confirm target locks retarget cleanly rather than causing spin/stale chasing.
7. save/relaunch and confirm config strategy persists.
8. rerun Alpha 6.5.3 Surge commands and Cardcha sandbox suppression.
9. smoke MiMi/Sudoku/Origin/equipment/controller/Vault/expansion NPC regressions.

## Recommended next step

If Alpha 6.6.0 behaves correctly in-game, the clean next point release is likely **Alpha 6.6.1: Party Strategy UI Polish**:

- expose strategy selection in an existing Party/Codex UI rather than requiring console commands;
- show the active strategy clearly;
- keep the five-value strategy contract stable;
- do not introduce formations or per-member strategy overrides until the foundation is live-tested.

If live testing reveals a specific targeting/chase/healing issue, make Alpha 6.6.1 a focused behavior hotfix instead of adding UI first.
