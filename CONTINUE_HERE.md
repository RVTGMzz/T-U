# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.2**

Status: **compile/package/direct-builder verified; in-game Tactics UI and real 2-client multiplayer smoke pending**.

Development source branch:

`v0.2-alpha6-6-2-party-tactics-capacity-ui`

Final handoff branch:

`v0.2-alpha6-6-2-party-tactics-capacity-ui-handoff`

Read this handoff first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_2_2026-09-05.md`

## Alpha 6.6.2 new surface: Party Tactics

Codex now has a `Tactics / Chiến thuật` footer entry that opens `PartyTacticsMenu`.

The Tactics screen:

- supports mouse, keyboard, and controller;
- shows the current farm-wide Party Strategy;
- exposes the existing five strategies without changing their contract;
- shows live shared People used/max;
- shows live shared external Combat Companion used/max;
- shows whether the current player is host authority or farmhand requester.

Five strategy values remain locked:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Do not add formations or per-member strategy overrides before Alpha 6.6.2 is live-smoked unless the user explicitly requests it.

## Multiplayer strategy authority

While a world is active:

- Host is authoritative for party-wide strategy.
- Host UI or `teamup_strategy` changes apply on host, clear local + `RemoteCombatServices` runtime locks, then broadcast the resulting strategy state.
- Farmhand UI or console changes send `StrategyRequestMessage` to host.
- Host validates the enum and requesting online Farmer before applying.
- Farmhands receive `StrategyStateMessage` and update local config/UI to the host state.
- New peers receive current strategy from host on `PeerConnected`.
- Outside an active world, strategy console changes still update local config directly.

Network types:

- `Alpha662/StrategyRequest`
- `Alpha662/StrategyState`
- `StrategyRequestMessage`
- `StrategyStateMessage`

## Alpha 6.6.1 locked product rules retained

### Shared people capacity

- The farm has **6 total people slots**.
- Every online Farmer consumes one people slot.
- `Following` / `Waiting` Team Up NPCs consume the remaining slots.
- Single-player: 1 Farmer + at most 5 active NPCs.
- Two-player co-op: 2 Farmers + at most 4 active NPCs.
- Overflow caused by a Farmer joining is moved to `Inactive` and released back to source/vanilla schedule, never deleted from roster/progression.
- Each NPC retains the `RecruiterId` of the Farmer who invited them.
- The same NPC cannot be recruited by two Farmers.

### Shared combat-companion capacity

- Maximum **2 deployed external Pokemon/summon/creature companions** across the whole farm.
- Farmer-owned and NPC-linked external creatures share the same 2/2 pool.
- `Active`, `Waiting`, and `ReturningHome` reserve a slot.
- `Standby` and `Inactive` do not.
- Vanilla dog/cat does not consume the pool.
- ChaCha consumes neither people nor combat-companion slots and never enters Main Party.

### NPC + companion invite UX

If a recruitable NPC has a live linked companion, top-level recruitment offers:

1. NPC only
2. NPC + companion
3. Cancel

If the player chooses NPC + companion at 2/2, show a replacement selection. Farmhands may replace their own active companion; host retains farm-wide authority.

### Shared-party multiplayer foundation

- Shared party state is host-authoritative.
- Farmhands send recruit/leave/member requests to host.
- Host validates, commits, and broadcasts party snapshots.
- NPC Follow/Combat is routed by `RecruiterId` and the corresponding online Farmer context.
- Remote farmhand equipment inventory mutation remains intentionally fail-closed/host-authoritative.

### Companion runtime contract

External provider actors can declare Team Up metadata through live `modData`:

- `Ronvotri.TeamUp/CompanionKind`
- `Ronvotri.TeamUp/CompanionOwnerCharacter`
- `Ronvotri.TeamUp/CompanionOwnerFarmerId`
- `Ronvotri.TeamUp/CompanionProviderId`
- `Ronvotri.TeamUp/CompanionProviderUnitId`

Team Up does not require provider DLLs or read provider-private save data for this contract.

### Sudoku control handshake

While Team Up owns NPC movement:

- `Ronvotri.TeamUp/PartyControlled = true`
- `Ronvotri.TeamUp/PartyControllerOwner = <Farmer UniqueMultiplayerID>`

Markers are removed on release. Hey! You're Cursed! should use `PartyControlled` only as a runtime movement ownership guard and continue owning Sudoku story/trust/roommate progression.

## CI checkpoint

Authoritative direct-builder run:

`33944448649`

Authoritative input commit:

`dd1f3fde561dee86ee211809079a415ecb42511f`

Result:

- direct `BuildV0_2Alpha662.ps1`
- build success
- 0 warnings
- 0 errors
- Party Tactics UI/controller source acceptance PASS
- host-authoritative strategy sync source acceptance PASS
- shared capacity visibility source acceptance PASS
- Alpha 6.6.1 capacity/multiplayer regression acceptance PASS
- Strategy/Surge/MiMi/Sudoku/equipment/vault regressions PASS
- package verification PASS
- `No materialized source diff.`
- artifact upload PASS

Package:

`TeamUp_v0.2.0-alpha.6.6.2_PARTY_TACTICS_CAPACITY_UI_TEST.zip`

Authoritative package SHA256:

`92519cc0367563c051c931174fc7bca0da9ab73f552d137c024f363cb47b1cf7`

Artifact ID:

`9962877914`

Artifact wrapper digest:

`sha256:bcf3d4aa185e8552e0b9986e11e7bb75d07fd1340667a6581256c54d813def77`

Artifact expiry:

`2026-12-04T04:24:05Z`

First successful materialized Alpha 6.6.2 source commit:

`7905fa8fdea8caf25417ed4bf2c7b7656908fa58`

## Regression locks retained

- Party Strategy: `Balanced`, `Defensive`, `Aggressive`, `HoldPosition`, `BossFocus`.
- hard leash 12 tiles.
- target lock 45 ticks.
- facing hold 10 ticks.
- Hold Position no chase outside attack range.
- Aggressive never disables hard leash.
- Boss Focus only prioritizes MaxHealth among already-valid candidates.
- Strategy changes must keep clearing stale combat runtime locks.
- Surge safe GreenSlime overlay, Cardcha sandbox exclusion, loot suppression, marker-scoped clear/reapply.
- Never reintroduce `isTileLocationTotallyClearAndPlaceable` to Surge placement.
- No `Activator.CreateInstance` / `MemberwiseClone` arbitrary monster cloning.
- MiMi live social/friendship gate, with requesting Farmer checked in multiplayer.
- Sudoku canonical ID `ronvotri.HeyYoureCursed_Sudoku` and `NINEFOLD SEAL`.
- MiMi `BROOMTAIL SIGIL`.
- 51 SVE/RSV expansion profiles/icons/balance.
- equipment double-click 450 ms.
- controller focus.
- Party Vault drag/drop / `releaseLeftClick`.
- Origin story.

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_2_PARTY_TACTICS_CAPACITY_UI_VI.txt`

Priority tests:

1. Codex footer Tactics entry, mouse and controller focus/navigation.
2. Tactics displays all five strategies and highlights the active one.
3. People card: 1 Farmer + 0 NPC = 1/6; 1 Farmer + 5 NPC = 6/6; 2 Farmers + 4 NPC = 6/6.
4. Companion card correctly shows 0/2, 1/2, 2/2 and ignores vanilla pet + ChaCha.
5. Host changes strategy and farmhand receives matching state.
6. Farmhand changes strategy and host applies/broadcasts it; farmhand does not diverge locally.
7. Newly joining farmhand receives current host strategy.
8. Host and farmhand split maps; existing RecruiterId Follow/Combat routing still works.
9. Re-test shared 6-person cap, shared 2-companion cap, NPC+companion replacement, MiMi gate, Sudoku markers.
10. Re-test Surge, equipment/controller/Vault and expansion NPC regressions.

Do not call Alpha 6.6.2 UI/multiplayer live-verified until a real in-game smoke, especially a two-client strategy sync test, passes.

If a real strategy-sync/UI bug appears, prefer a focused Alpha 6.6.3 hotfix before adding formation or per-member strategy systems.
