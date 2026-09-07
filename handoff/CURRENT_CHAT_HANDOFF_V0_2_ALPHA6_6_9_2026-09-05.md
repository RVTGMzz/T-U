# Team Up v0.2.0-alpha.6.6.9 Handoff

Date: 2026-09-05

## Resume point

Development branch:

`v0.2-alpha6-6-9-companion-flicker-health-bars`

Final handoff branch:

`v0.2-alpha6-6-9-companion-flicker-health-bars-handoff`

Version:

`0.2.0-alpha.6.6.9`

Status: **compile/package/direct-builder verified; live flicker and health-bar validation pending**.

## Why this milestone exists

The user confirmed the Alpha 6.6.7 water/bridge performance fix removed the severe lag. Alpha 6.6.8 then prevented humanoid Team Up NPCs from accepting bare-water destinations. After that, live testing revealed two issues:

1. Pelipper Pokemon flickered. This affected both Alex/NPC-linked Pokemon and the Farmer's own Pokemon, proving it was a provider-wide compatibility issue rather than one NPC owner.
2. Team Up NPC health existed internally but was not visible enough to manage the party during combat.

## Pelipper flicker fix

New service:

`src/TeamUp/Core/PelipperDeploymentStateService.cs`

Soft deployment contract:

```text
Ronvotri.TeamUp/PelipperDeployment = Active|Standby
Ronvotri.TeamUp/PelipperDeploymentOwner = <owner>
```

### Authority rule

Pelipper Town is now the render and movement authority for Pelipper-owned Pokemon.

Alpha 6.6.9 runtime must NOT use Team Up to continuously:

- set `IsInvisible`;
- call `Halt()`;
- clear `controller`;
- clear `temporaryController`.

Team Up keeps party/quota state and writes only soft desired deployment markers.

### Legacy repair

`PelipperDeploymentStateService.ReleaseLegacySuppression(...)` may call old:

`PelipperTownCompatibilityService.SetSuppressed(actor, owner, false)`

This is one-time migration cleanup for Team Up suppression written by builds before 6.6.9. New runtime must never call `SetSuppressed(..., true)`.

### Integration paths updated

`ModEntry.Alpha663.cs`:

- reconcile source-controlled linked unit -> `SetDesiredDeployment(...)`;
- NPC-only / no slot -> soft Standby;
- recruit NPC + Pokemon -> soft Active;
- leave owner -> `ClearDesiredDeployment(...)`;
- helper `IsPelipperUnitDeployedAlpha669(...)`.

`ModEntry.Alpha661.cs` replacement flow:

- old direct suppression replaced with `PelipperDeploymentStateService.SetDesiredDeployment(..., false)`.

Important: if Pelipper Town does not consume the soft Standby marker, a Team Up Standby Pokemon may remain visually present. Do not restore `IsInvisible` hacks. Implement the counterpart handshake in Pelipper Town instead.

## NPC health UI

New service:

`src/TeamUp/UI/PartyHealthOverlayService.cs`

Runtime coordinator:

`src/TeamUp/ModEntry.Alpha669.cs`

### HUD

- up to 5 active Following/Waiting NPCs;
- small names on left side;
- `HudBarWidth = 86`;
- `HudBarHeight = 5`;
- `HudRowHeight = 17`;
- health ratio from real `member.CurrentHealth / Progression.GetMaxHealth(member)`;
- green / caution / danger / downed colors.

### Overhead bars

- `WorldBarWidth = 42`;
- `WorldBarHeight = 4`;
- contextual only;
- appears if downed, wounded, or engaged with a valid hostile target;
- full HP outside combat hides overhead bar;
- no numeric `100/100` or verbose text over NPCs.

### Multiplayer health sync

Host Alpha669 coordinator checks a compact signature every 12 ticks containing:

- RecruiterId;
- CharacterName;
- CurrentHealth;
- IsDowned;
- IsWithdrawn;
- State.

Only when signature changes does host call `BroadcastPartySnapshot()`. This avoids per-frame network health spam while allowing farmhands to receive health/downed/state changes.

## Performance + land safety preserved

### Alpha 6.6.7

- `CombatPathRetryCooldownTicks = 24`;
- `CombatMovementPulseTicks = 3`;
- Pelipper source/decorative actors excluded from hostile Team Up combat target list by default;
- no `isTileLocationTotallyClearAndPlaceable` in CombatService.

### Alpha 6.6.8

- `PartyTileSafety.IsWalkableLandOrBridge(...)`;
- bare water rejected for humanoid party NPCs;
- real Buildings-layer bridge/walkway over water allowed;
- stranded humanoid NPC rescue retained;
- FollowService uses `FindLandOpenNear`;
- no `isTileLocationTotallyClearAndPlaceable` in FollowService;
- combat approach uses same land-safe helper.

## Party model locked

### People capacity

- 6 total people across online Farmers + Following/Waiting Team Up NPCs;
- Farmer counts toward 6;
- single player max 5 active NPCs;
- 2-player max 4 active NPCs;
- overflow NPC becomes Inactive without deleting roster/progression/equipment;
- RecruiterId ownership retained;
- same NPC cannot have two recruiters.

### External combat companion capacity

- hard Team Up state maximum 2 shared across whole farm;
- Farmer-owned + NPC-linked share one pool;
- Active/Waiting/ReturningHome reserve a slot;
- Standby/Inactive do not;
- vanilla pet free;
- ChaCha free and never Main Party.

## Switch / Equipment locks

- SMAPI `IsActionButton()` routes equip;
- SMAPI `IsUseToolButton()` routes unequip;
- `ControllerActivationDebounceMs = 180`;
- `ControllerMouseEchoSuppressionMs = 260`;
- `DoubleClickWindowMs = 450` for mouse inventory equip;
- transactional equipment state validation preserved.

## Codex/Profile locks

- D-pad and left analog exactly one profile per input;
- do not restore `MoveVertical(2)` or `MoveVertical(-2)`;
- profile detail content scale 1.52f;
- ASCII-safe punctuation avoids hollow-star fallback glyphs.

## Strategy / custom NPC locks

Strategies:

- Balanced
- Defensive
- Aggressive
- HoldPosition
- BossFocus

MiMi:

- source `Ronvotri.Cardcha`;
- canonical `Ronvotri.Cardcha_MiMi`;
- requesting Farmer's friendship used in multiplayer;
- no private Cardcha SaveData/service access;
- signature `BROOMTAIL SIGIL`.

Sudoku:

- canonical `ronvotri.HeyYoureCursed_Sudoku`;
- signature `NINEFOLD SEAL`;
- while Team Up controls movement:
  - `Ronvotri.TeamUp/PartyControlled = true`
  - `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`

## Core combat regression locks

- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- anti-spin;
- Hold Position no chase outside attack range;
- Aggressive never disables hard leash;
- Boss Focus highest MaxHealth only among already-valid targets;
- Surge Cardcha arena exclusion;
- Surge safe placement `isTileOnMap + isTilePassable + IsTileBlockedBy`;
- never restore `isTileLocationTotallyClearAndPlaceable` to Surge, Follow or Combat;
- no arbitrary custom monster cloning via `Activator.CreateInstance` or `MemberwiseClone`;
- 51 SVE/RSV profile/icon/balance line;
- Party Vault drag/drop;
- Origin story.

## Build pipeline

Builder:

`BuildV0_2Alpha669.ps1`

One-click launcher:

`BUILD_V0_2_ALPHA6.bat`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_9_COMPANION_FLICKER_HEALTH_BARS_VI.txt`

### Materialization run

Run:

`33974363369`

Materialized source commit:

`b1ce92b`

Materialization package SHA256:

`90ef9dd211eb196a5448488a28dfaa21359c7db375b3c845c7bf3f1c6797eb56`

### Final authoritative direct-builder run

Run:

`33974465552`

Authoritative input commit:

`ad214367f06fee1612818dc7d9e340f577a0cd90`

Results:

- direct `BuildV0_2Alpha669.ps1` success;
- 0 warnings;
- 0 errors;
- Pelipper source render/movement authority PASS;
- soft Active/Standby deployment PASS;
- legacy visibility one-time repair PASS;
- thin NPC world + HUD health bars PASS;
- multiplayer health snapshot sync PASS;
- 6.6.7 performance regression PASS;
- 6.6.8 land-safe regression PASS;
- Switch input + Codex one-row regression PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.9_COMPANION_FLICKER_HEALTH_BARS_HOTFIX_TEST.zip`

Authoritative package SHA256:

`efde24f02f12a7fbc23378cba4af2d7cdb8da2a6675ebd7a45cbaebd8e1e2949`

Artifact ID:

`9971897681`

Artifact wrapper digest:

`sha256:729faece12dffef01beb3d22e2e1855927c77662ba461a6fd54da6c061a76741`

## Required live validation

Priority:

1. Farmer Pokemon stays visible and stable for 20-30 seconds, while moving, and through map warps.
2. Alex/NPC-linked Pokemon same test.
3. Farmer + NPC Pokemon simultaneously, especially near water/bridge.
4. No periodic blink caused by Team Up reconcile.
5. Wound a Team Up NPC: left HUD bar and contextual overhead bar decrease.
6. Heal the NPC: bars increase; full HP outside combat hides overhead bar.
7. Downed state visibly reads through bar color/state behavior.
8. Re-run exact river/bridge scenes: no severe lag and humanoid NPCs stay on land/bridge.
9. Switch equip and unequip.
10. Codex exactly one profile per input.

Do not call Alpha 6.6.9 live-verified until user confirms these behaviors.