# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.9**

Status: **compile/package/direct-builder verified with 0 warnings, 0 errors and no materialized source diff. Alpha 6.6.9 now requires live validation for Pelipper flicker removal and NPC health presentation.**

Development branch:

`v0.2-alpha6-6-9-companion-flicker-health-bars`

Final handoff branch:

`v0.2-alpha6-6-9-companion-flicker-health-bars-handoff`

Read first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_9_2026-09-05.md`

## Why Alpha 6.6.9 exists

After Alpha 6.6.8, the user reported that not only Alex's Pokemon but also the Farmer's own Pelipper Pokemon flickered. This made the issue clearly provider-wide rather than owner-specific. The same live test also showed Team Up NPCs had real HP internally but no useful visible health feedback.

## Pelipper source authority hotfix

New helper:

`src/TeamUp/Core/PelipperDeploymentStateService.cs`

Team Up now uses soft deployment markers:

```text
Ronvotri.TeamUp/PelipperDeployment = Active|Standby
Ronvotri.TeamUp/PelipperDeploymentOwner = <owner>
```

Rules:

- Pelipper Town remains visibility/render/movement authority for source-owned Pokemon.
- Team Up no longer uses `IsInvisible`, `Halt()`, `controller` or `temporaryController` as a live deployment mechanism.
- Team Up still tracks shared combat companion state and quota internally.
- Old Team Up suppression from pre-6.6.9 builds is restored once on load through `SetSuppressed(..., false)` and never re-applied as true.
- Alpha661 replacement flow and Alpha663 reconcile flow both use `PelipperDeploymentStateService.SetDesiredDeployment(...)`.
- Leave clears desired deployment marker and returns full source authority.

Important follow-up rule: if Pelipper Town itself does not consume soft `Standby`, a standby Pokemon may still be visible. Do NOT reintroduce Team Up `IsInvisible` hacks. Add a small counterpart handshake to Pelipper Town instead.

## NPC health presentation

New UI service:

`src/TeamUp/UI/PartyHealthOverlayService.cs`

Runtime coordinator:

`src/TeamUp/ModEntry.Alpha669.cs`

Behavior:

- thin left-side party HUD for up to 5 active NPCs;
- HUD bar height = 5 px;
- contextual overhead bar height = 4 px;
- overhead appears when wounded, downed, or near a valid hostile target;
- full HP outside combat hides overhead bar;
- HP uses real `PartyMemberData.CurrentHealth` and `Progression.GetMaxHealth(member)`;
- no verbose numeric text above NPCs;
- host checks health/downed/state signature every 12 ticks and broadcasts party snapshot only when it changes, enabling farmhand health updates without per-frame network traffic.

## 6.6.7 + 6.6.8 locks preserved

- severe water/bridge lag fix remains;
- Combat path retry cooldown = 24 ticks;
- Combat movement pulse = 3 ticks;
- no `isTileLocationTotallyClearAndPlaceable` in FollowService or CombatService;
- Pelipper source actors excluded from hostile Team Up target lists by default;
- humanoid party NPCs reject bare water;
- true bridge/walkway tiles remain valid;
- stranded humanoid NPC rescue remains;
- combat approach targets use `PartyTileSafety.IsWalkableLandOrBridge`.

## Party model locked

### People

- 6 total across online Farmers + Following/Waiting NPCs;
- Farmer counts;
- overflow NPC becomes Inactive without losing roster/progression/equipment;
- each NPC retains RecruiterId;
- one NPC cannot have two recruiters.

### External combat companions

- hard Team Up state maximum 2 shared across whole farm;
- Farmer-owned + NPC-linked share pool;
- Active/Waiting/ReturningHome reserve slots;
- Standby/Inactive do not;
- vanilla pet free;
- ChaCha free and never Main Party.

## Input / Codex locks

- Switch semantic Action Button equip;
- Switch semantic Use Tool unequip;
- controller debounce 180 ms;
- virtual mouse echo suppression 260 ms;
- inventory mouse double-click 450 ms;
- Codex D-pad/left analog exactly one profile per input;
- profile content scale 1.52f;
- ASCII-safe punctuation, no hollow-star fallback glyphs.

## Strategy / custom NPC locks

Five strategies unchanged:

- Balanced
- Defensive
- Aggressive
- HoldPosition
- BossFocus

MiMi:

- `Ronvotri.Cardcha_MiMi`
- `BROOMTAIL SIGIL`
- requesting Farmer live friendship gate
- no Cardcha private save/service access

Sudoku:

- `ronvotri.HeyYoureCursed_Sudoku`
- `NINEFOLD SEAL`
- `Ronvotri.TeamUp/PartyControlled = true`
- `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`

## Authoritative checkpoint

First 6.6.9 materialized source commit:

`b1ce92b`

Authoritative input commit:

`ad214367f06fee1612818dc7d9e340f577a0cd90`

Authoritative CI run:

`33974465552`

Result:

- `BuildV0_2Alpha669.ps1` success;
- 0 warnings;
- 0 errors;
- Pelipper source render/movement authority PASS;
- soft Active/Standby deployment contract PASS;
- one-time legacy visibility repair PASS;
- thin world + HUD health bars PASS;
- multiplayer health snapshot sync PASS;
- Alpha 6.6.7 performance regression PASS;
- Alpha 6.6.8 land-safe regression PASS;
- Switch input/Codex one-row regression PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.9_COMPANION_FLICKER_HEALTH_BARS_HOTFIX_TEST.zip`

Package SHA256:

`efde24f02f12a7fbc23378cba4af2d7cdb8da2a6675ebd7a45cbaebd8e1e2949`

Artifact ID:

`9971897681`

Artifact wrapper digest:

`sha256:729faece12dffef01beb3d22e2e1855927c77662ba461a6fd54da6c061a76741`

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_9_COMPANION_FLICKER_HEALTH_BARS_VI.txt`

Highest priority:

1. Farmer Pokemon: stand 20-30 seconds, move around, warp maps, verify no blinking.
2. Alex/NPC-linked Pokemon: same test, verify no blinking.
3. Farmer + NPC Pokemon together, especially near water/bridge.
4. Damage an NPC and verify HUD + contextual overhead HP move with real health.
5. Heal NPC and verify bar rises; full HP outside combat should hide overhead bar.
6. Re-test the exact river/bridge scenes: performance must remain smooth and humanoid NPCs must remain on land/bridge.
7. Test Switch equip + unequip and Codex one-profile-per-input.

Do not call Alpha 6.6.9 live-verified until the user confirms these points.