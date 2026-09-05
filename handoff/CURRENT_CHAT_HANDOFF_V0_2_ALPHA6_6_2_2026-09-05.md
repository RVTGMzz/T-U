# Team Up v0.2.0-alpha.6.6.2 Handoff

Date: 2026-09-05
Milestone: **Party Tactics + Shared Capacity UI**
Status: **compile/package/direct-builder verified; in-game UI + real 2-client multiplayer smoke pending**

## Resume point

Development branch:

`v0.2-alpha6-6-2-party-tactics-capacity-ui`

Final handoff branch:

`v0.2-alpha6-6-2-party-tactics-capacity-ui-handoff`

Read this file and `CONTINUE_HERE.md` before changing behavior.

## Why Alpha 6.6.2 exists

Alpha 6.6.0 established the five party-wide strategies through config/console.
Alpha 6.6.1 established the shared six-person party budget, shared two-companion budget, companion-choice UX, host-authoritative party state, per-Farmer runtime context, and Sudoku movement handshake.

Alpha 6.6.2 exposes the strategy foundation as normal controller-friendly UI and extends multiplayer authority to strategy changes without changing the existing five strategy behaviors or the 6/2 capacity contracts.

## New Alpha 6.6.2 implementation

### Party Tactics menu

New source:

`src/TeamUp/UI/PartyTacticsMenu.cs`

Opened from the Codex footer through `Tactics / Chiến thuật`.

The menu shows:

- current Party Strategy;
- five strategy choices;
- shared People used/max;
- online Farmer count + active NPC count;
- shared external Combat Companion used/max;
- host/farmhand authority hint.

Input support:

- mouse click/hover;
- keyboard Up/Down + Enter/Space + Esc;
- controller D-pad/left-stick + A + B/Back.

Back returns to the Codex callback rather than deliberately creating a second independent navigation stack.

### Codex integration

`src/TeamUp/UI/CodexBrowserMenu.cs` now has:

- `FocusArea.Tactics`;
- `_tacticsButton`;
- `Action<CodexBrowserMenu> _openTactics`;
- mouse click/hover support;
- controller footer navigation between Tactics and Close;
- activation that opens the separate Tactics menu.

Do not duplicate strategy behavior inside Codex or `PartyTacticsMenu`. The UI delegates requests to ModEntry.

### Strategy multiplayer authority

New source:

`src/TeamUp/ModEntry.Alpha662.cs`

Network message classes live in:

`src/TeamUp/Core/MultiplayerMessages.cs`

Message types:

- `Alpha662/StrategyRequest`
- `Alpha662/StrategyState`
- `StrategyRequestMessage`
- `StrategyStateMessage`

Contract:

1. Host UI / `teamup_strategy` request -> host validates and applies.
2. Farmhand UI / console request -> sends request to host rather than mutating live multiplayer strategy independently.
3. Host writes `Config.PartyStrategy`.
4. Host calls `Combat.Clear()` and `ClearRemoteCombatServices()`.
5. Host broadcasts `StrategyStateMessage`.
6. Farmhand receives host strategy, writes local config, clears local Combat runtime locks, and reflects host state in UI.
7. On `PeerConnected`, host sends current strategy to the new peer.
8. Outside an active world, strategy changes still update local config directly.

The five strategy values remain exactly:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

No new PartySaveData field was added for strategy.

## Shared capacity UI contract

The Tactics menu reads existing Alpha 6.6.1 capacity services instead of implementing a second quota model.

People display:

`Party.GetSharedPeopleCount(GetOnlineFarmerIds())`

Max people display:

`Math.Clamp(Config.MaxPartyMembers, 1, 6)`

Companion display:

`Party.GetActiveCombatCompanionCount()`

Max external companion display:

`Config.AllowLinkedCompanions ? Math.Clamp(Config.MaxActiveLinkedCompanions, 0, 2) : 0`

This is display/interaction polish only. The authority and enforcement continue to live in PartyManager/Alpha661 multiplayer code.

## Alpha 6.6.1 capacity and multiplayer locks remain mandatory

### Six total people

- online Farmers + active `Following`/`Waiting` Team Up NPCs share one six-person cap;
- 1 Farmer allows at most 5 active NPCs;
- 2 Farmers allow at most 4 active NPCs;
- Farmer join overflow moves NPCs to `Inactive`, never deleting roster/progression/equipment;
- NPC owner remains `RecruiterId`;
- same NPC cannot belong to two Farmers.

### Two shared external combat companions

- Farmer summons and NPC linked external creatures share the same 2/2 pool;
- `Active`, `Waiting`, `ReturningHome` reserve a slot;
- `Standby`, `Inactive` do not;
- vanilla dog/cat is free;
- ChaCha is free and never Main Party.

### NPC linked companion recruit flow

Top-level choices remain:

1. NPC only
2. NPC + linked companion
3. Cancel

At 2/2, use replacement flow. Farmhands must not evict another farmhand's owned companion.

### Per-Farmer runtime

Follow/Combat must remain routed by `RecruiterId` and correct Farmer context. Split-map multiplayer is a required live smoke case.

## Source-mod compatibility locks

### MiMi

- Cardcha UniqueID `Ronvotri.Cardcha`;
- canonical ID `Ronvotri.Cardcha_MiMi`;
- Team Up friendship gate uses the requesting Farmer in multiplayer;
- do not read Cardcha private SaveData/services or hard-code merchant schedule/location;
- `BROOMTAIL SIGIL` retained.

### Sudoku

- canonical ID `ronvotri.HeyYoureCursed_Sudoku`;
- `NINEFOLD SEAL` retained;
- Team Up movement ownership markers:
  - `Ronvotri.TeamUp/PartyControlled = true`
  - `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`
- remove markers when Team Up releases control;
- do not take ownership of Hey! You're Cursed! story/trust/roommate progression.

### ChaCha

- free Special/Farmer Companion;
- never Main Party;
- consumes neither people nor external combat-companion slot.

## Party Strategy behavior locks

Do not change these as part of UI fixes without explicit reason:

- Balanced: baseline.
- Defensive: shorter acquisition, support/healer earlier recovery, attack slightly slower.
- Aggressive: longer acquisition, faster attack, hard leash still active.
- HoldPosition: no chase outside attack range; local fight/heal/control still works.
- BossFocus: highest MaxHealth only among already-valid candidate monsters.

Combat safety locks retained:

- `HardLeashTiles = 12f`
- `TargetLockDurationTicks = 45`
- `FacingHoldDurationTicks = 10`
- anti-spin/facing hold behavior.

## Surge locks retained

- Team Up-owned safe GreenSlime overlay only;
- x2 is target, safety wins;
- Cardcha test arena excluded;
- Cardcha harness monsters excluded from baseline;
- loot suppressed by default for Team Up extras;
- placement contract remains `isTileOnMap + isTilePassable + IsTileBlockedBy`;
- NEVER restore `isTileLocationTotallyClearAndPlaceable`;
- no `Activator.CreateInstance` / `MemberwiseClone` arbitrary monster cloning;
- `ClearOwnedSurgeMonsters` remains marker-scoped;
- debug status/reapply/clear/board retained.

## Other regression locks

- 51 SVE/RSV expansion profiles/icons/balance;
- Equipment double click window 450 ms;
- controller focus/navigation;
- Party Vault drag/drop and `releaseLeftClick`;
- Origin story progression;
- MiMi/Sudoku identity art and signatures.

## Build files

- `BuildV0_2Alpha662.ps1`
- `BUILD_V0_2_ALPHA6.bat`
- `.github/workflows/team-up-alpha6-6-2-party-tactics-capacity-ui.yml`
- `SMOKE_TEST_V0_2_ALPHA6_6_2_PARTY_TACTICS_CAPACITY_UI_VI.txt`

Direct builder is the authoritative build path. No temporary bootstrap is required for Alpha 6.6.2.

## CI history

### Run 1

`33944305015`

Failed at C# compile because `ModEntry.Alpha662.cs` lacked `using StardewModdingAPI;`, so `Context` was unresolved. No materialization occurred. This was an import-only compile issue, not a Tactics design failure.

### Run 2

`33944382272`

First full green run:

- build PASS;
- 0 warnings / 0 errors;
- source acceptance PASS;
- package verification PASS;
- materialization PASS;
- artifact upload PASS.

Materialized source commit:

`7905fa8fdea8caf25417ed4bf2c7b7656908fa58`

### Authoritative direct-builder run

`33944448649`

Input commit:

`dd1f3fde561dee86ee211809079a415ecb42511f`

Result:

- direct `BuildV0_2Alpha662.ps1` PASS;
- 0 warnings;
- 0 errors;
- Party Tactics UI/controller source acceptance PASS;
- host-authoritative strategy sync acceptance PASS;
- shared capacity visibility acceptance PASS;
- Alpha 6.6.1 capacity/multiplayer regression acceptance PASS;
- Strategy/Surge/MiMi/Sudoku/equipment/vault regression acceptance PASS;
- package verification PASS;
- materialization reports exactly `No materialized source diff.`;
- artifact upload PASS.

## Authoritative package

`TeamUp_v0.2.0-alpha.6.6.2_PARTY_TACTICS_CAPACITY_UI_TEST.zip`

SHA256:

`92519cc0367563c051c931174fc7bca0da9ab73f552d137c024f363cb47b1cf7`

GitHub Actions artifact:

- ID `9962877914`
- size `216237` bytes
- wrapper digest `sha256:bcf3d4aa185e8552e0b9986e11e7bb75d07fd1340667a6581256c54d813def77`
- expires `2026-12-04T04:24:05Z`

Important: package SHA above is the Team Up mod ZIP itself. Artifact digest is the GitHub Actions wrapper ZIP.

## Live smoke still pending

Do not call Alpha 6.6.2 live verified yet.

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_2_PARTY_TACTICS_CAPACITY_UI_VI.txt`

Highest-priority live tests:

1. Open Codex -> Tactics with mouse and controller.
2. Return from Tactics to Codex using Esc/B/Back.
3. Select all five strategies and verify highlight + clean retarget.
4. Verify People card for 1+0, 1+5 and 2+4 scenarios.
5. Verify Companion card 0/2, 1/2, 2/2 and that vanilla pet + ChaCha stay free.
6. Real 2-client: host changes strategy and farmhand receives it.
7. Real 2-client: farmhand requests strategy; host applies and broadcasts it.
8. New farmhand join receives current host strategy.
9. Split-map RecruiterId Follow/Combat still works.
10. Re-test linked-companion replacement, MiMi friendship gate, Sudoku movement markers, Surge, Equipment, Vault and expansion profiles.

## Recommended next step

If no live bug is reported, do **not** automatically add a fake validation-only patch.

Good next options after live smoke:

- a focused Alpha 6.6.3 hotfix if Tactics/controller/strategy sync exposes a real issue; or
- a broader milestone only after this foundation is exercised in game.

Do not jump straight into per-member strategy overrides or formations before Alpha 6.6.2 multiplayer/UI smoke unless the user explicitly requests that direction.
