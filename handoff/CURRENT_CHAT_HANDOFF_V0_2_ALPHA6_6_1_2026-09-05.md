# Team Up v0.2.0-alpha.6.6.1 Handoff

Date: 2026-09-05

Milestone: **Shared Party Capacity + Companion Choice + Multiplayer Foundation**

Status: **compile/package/direct-builder verified; in-game and real 2-client multiplayer smoke pending**.

## Resume point

Development branch:

`v0.2-alpha6-6-1-party-capacity-multiplayer`

Final handoff branch:

`v0.2-alpha6-6-1-party-capacity-multiplayer-handoff`

Start by reading:

- `CONTINUE_HERE.md`
- this file
- `SMOKE_TEST_V0_2_ALPHA6_6_1_SHARED_PARTY_MULTIPLAYER_VI.txt`

## Why Alpha 6.6.1 changed scope

Alpha 6.6.0 introduced the five-value Party Strategy foundation. Before moving to strategy UI polish, product rules were clarified for party capacity, Pokemon/summon capacity, linked-companion recruit UX, multiplayer ownership, and Sudoku source-mod movement compatibility. Alpha 6.6.1 implements that foundation instead of adding cosmetic strategy UI first.

## Locked people-cap rule

The farm has **6 total people slots**, shared across multiplayer.

Every online Farmer consumes one slot. Active Team Up NPCs consume the remainder.

Examples:

```text
1 Farmer online -> max 5 active NPCs
2 Farmers online -> max 4 active NPCs
3 Farmers online -> max 3 active NPCs
4 Farmers online -> max 2 active NPCs
```

States counting as active people:

- `Following`
- `Waiting`

If an additional Farmer joins and current active NPCs would exceed six total people, `PartyManager.EnforceSharedPeopleCapacity` moves overflow NPCs to `Inactive`. Runtime then releases those NPCs back to source/vanilla scheduling. Their roster data, recruiter ownership, progression, level, equipment and linked-companion registration are retained.

Same-NPC double ownership is blocked through global owner lookup (`GetAnyOwner`).

## Locked external combat-companion rule

The farm has **2 shared external combat-companion slots** across all online Farmers and recruited NPCs.

The pool includes external Pokemon-like creatures/summons owned by:

- a Farmer;
- a recruited NPC.

Slot-reserving states:

- `Active`
- `Waiting`
- `ReturningHome`

Non-reserving states:

- `Standby`
- `Inactive`

Exemptions:

- vanilla dog/cat does not consume the 2/2 pool;
- ChaCha is a free Special Companion and consumes neither people nor combat-companion slots;
- ChaCha remains forbidden from Main Party.

The legacy config property name `MaxActiveLinkedCompanions` is retained for compatibility, but Alpha 6.6.1 clamps it to 0..2 and applies it as the global combat-companion pool.

## Linked companion recruit UX

When `CompanionIntegrationService.FindLinkedCompanion(npc)` resolves a live linked companion, the top-level invite flow offers exactly three product choices:

1. NPC only
2. NPC + companion
3. Cancel

If the user chooses NPC + companion while the pool is full at 2/2, Team Up opens a second replacement prompt. The chosen currently active companion is set to `Standby`, then the new linked companion may activate.

Multiplayer authority rule:

- farmhand may replace their own companion;
- host may manage farm-wide companion replacement;
- farmhand cannot silently evict another farmhand's companion.

## Runtime companion provider contract

New file:

`src/TeamUp/Core/CompanionIntegrationService.cs`

This is intentionally a lightweight live-actor contract. Team Up does not require provider DLLs and does not read provider-private save data.

Provider modData keys:

```text
Ronvotri.TeamUp/CompanionKind
Ronvotri.TeamUp/CompanionOwnerCharacter
Ronvotri.TeamUp/CompanionOwnerFarmerId
Ronvotri.TeamUp/CompanionProviderId
Ronvotri.TeamUp/CompanionProviderUnitId
```

Relevant declared kinds already supported by the broader classification contract include:

- `FarmerSummon`
- `LinkedCompanion`
- `FarmerCompanion`
- `SpecialCompanion`

Important: do not turn this into a hard dependency on Pokemon/summon provider internals unless a later explicit API contract is designed.

## Multiplayer architecture

Alpha 6.6.1 is a **host-authoritative foundation**.

New message models:

`src/TeamUp/Core/MultiplayerMessages.cs`

Includes:

- `RecruitRequestMessage`
- `LeaveRequestMessage`
- `MemberCommandRequestMessage`
- `PartySnapshotMessage`
- `PartyActionResultMessage`

Coordinator:

`src/TeamUp/ModEntry.Alpha661.cs`

Key runtime rules:

1. Farmhands can initiate interaction/recruit/member commands locally.
2. Farmhand writes do not directly mutate canonical party state.
3. Client sends request to host.
4. Host resolves requesting Farmer from `FromPlayerID`.
5. Host validates recruitability/ownership/capacity.
6. Host commits canonical state.
7. Host saves and broadcasts a party snapshot.
8. The same NPC cannot be concurrently recruited by multiple Farmers at party-state level.

### Per-Farmer ownership

Existing `RecruiterId` remains the NPC owner identity.

Follow and combat are now routed to the corresponding online Farmer rather than blindly using `Game1.player` for every party member.

`FollowService.Update(... recruiterId, Farmer owner)` routes following/warping to that Farmer's current location/tile.

`CombatService.SetFarmerContext(Farmer farmer)` gives each online Farmer an independent CombatService runtime context. The host creates `RemoteCombatServices` for remote recruiters.

This matters when Farmers split across maps: NPCs owned by Farmer A should not chase Farmer B's combat merely because B is host/local player.

### Disconnect behavior

When a peer disconnects:

- that recruiter's runtime followers are released;
- their party states are deactivated;
- remote CombatService runtime is cleared;
- canonical roster/progression remains saved;
- shared capacity is re-enforced and snapshots are broadcast.

### Known Alpha 6.6.1 multiplayer limitation

Remote farmhand equipment inventory mutation is intentionally fail-closed/host-authoritative. Alpha 6.6.1 does not pretend to safely move equipment items between remote client inventories. Live multiplayer equipment transfer should be designed/tested in a later focused milestone.

## Sudoku compatibility handshake

Canonical Sudoku NPC ID remains:

`ronvotri.HeyYoureCursed_Sudoku`

When Team Up owns NPC movement, `FollowService.PrepareForParty` writes:

```text
Ronvotri.TeamUp/PartyControlled = true
Ronvotri.TeamUp/PartyControllerOwner = <Farmer UniqueMultiplayerID>
```

When Team Up releases the actor, both markers are removed.

The intended Hey! You're Cursed! counterpart is simple:

- if Sudoku's live actor has `Ronvotri.TeamUp/PartyControlled=true`, pause/reset source roommate movement/path/glide ownership;
- when marker disappears, source roommate behavior can resume cleanly;
- Hey! You're Cursed! still owns Sudoku story, trust, materialization, dialogue and private save state;
- Team Up must not read/write Hey! You're Cursed! private story flags to implement this handshake.

## MiMi multiplayer gate

MiMi canonical NPC ID remains:

`Ronvotri.Cardcha_MiMi`

Cardcha UniqueID remains:

`Ronvotri.Cardcha`

Alpha 6.6.1 changes the live social unlock check to use the **Farmer who actually requested recruitment**:

`farmer.friendshipData.ContainsKey(MimiNpcId)`

Do not revert this to unconditional `Game1.player.friendshipData` or farmhands may fail/incorrectly pass the gate based on host progress.

Team Up still does not read Cardcha private SaveData/services or hard-code MiMi's merchant schedule.

## Party Strategy contract remains unchanged

Five values:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Command:

`teamup_strategy <status|balanced|defensive|aggressive|hold|boss>`

Strategy remains config-backed. Do not migrate PartySaveData just to store global strategy.

Strategy switch continues clearing combat runtime locks, including remote CombatService runtimes in Alpha 6.6.1.

## Combat regression locks

Preserve:

- `HardLeashTiles = 12f`
- `TargetLockDurationTicks = 45`
- `FacingHoldDurationTicks = 10`
- Hold Position no chase outside attack range
- Aggressive never disables hard leash
- Boss Focus selects highest-MaxHealth only among already-valid candidates
- no arbitrary world-wide monster targeting

## Surge regression locks

Preserve Alpha 6.5.2/6.5.3 contracts:

- Team Up-owned safe `GreenSlime` overlay only
- x2 is a target, not a guarantee
- no blind arbitrary custom/boss/story monster cloning
- Cardcha test arena excluded
- Cardcha harness monsters excluded from baseline
- loot suppressed by default for Team Up Surge extras
- marker-scoped `ClearOwnedSurgeMonsters`
- debug `reapply` clears owned extras before fresh ApplyOnce
- telemetry remains runtime-only
- AdventureGuild board is UI-safe
- safe placement uses `isTileOnMap`, `isTilePassable`, `IsTileBlockedBy`
- NEVER reintroduce `isTileLocationTotallyClearAndPlaceable`
- no `Activator.CreateInstance`
- no `MemberwiseClone`

## Custom NPC / content regression locks

Preserve:

- MiMi `BROOMTAIL SIGIL`
- Sudoku `NINEFOLD SEAL`
- ChaCha never Main Party
- 51 SVE/RSV profiles/icons/balance
- Origin story
- equipment double-click window 450 ms
- controller focus/navigation
- Party Vault drag/drop / `releaseLeftClick`
- Cardcha combat sandbox exclusion

## Build pipeline

One-click launcher:

`BUILD_V0_2_ALPHA6.bat`

Direct builder:

`BuildV0_2Alpha661.ps1`

Temporary bootstrap `FixAndBuildV0_2Alpha661.ps1` was used only to normalize/repair initial materialization and has been DELETED. Do not restore it.

### Bootstrap materialization run

Run:

`33942454237`

Result:

- build 0 warnings / 0 errors
- source acceptance PASS
- package verification PASS
- materialization PASS

First successful materialized gameplay/build source commit:

`a6a32b11e565e51eaf598f9a97f18a254db6bc92`

### Authoritative direct-builder run

Run:

`33942539462`

Input source/workflow checkpoint:

`9f0a4d00b76af620e9be6f2abd28341e4a3cc42b`

Result:

- direct `./BuildV0_2Alpha661.ps1`
- build success
- **0 warnings**
- **0 errors**
- source acceptance PASS
- shared 6-person acceptance PASS
- shared 2-companion acceptance PASS
- linked companion recruit/replacement acceptance PASS
- host-authoritative/per-Farmer context acceptance PASS
- Sudoku handshake acceptance PASS
- Strategy/Surge/MiMi/Sudoku/core regression acceptance PASS
- package verification PASS
- materialization reports **`No materialized source diff.`**
- artifact upload PASS

This run is the authoritative Alpha 6.6.1 CI checkpoint.

## Package

File:

`TeamUp_v0.2.0-alpha.6.6.1_SHARED_PARTY_MULTIPLAYER_TEST.zip`

Inner Team Up mod ZIP SHA256:

`7140af72b24b6e2eba06ebca26b0381bfc082305f1e9f18ac013e353bdd75655`

GitHub Actions artifact:

- name: `team-up-alpha6-6-1-shared-party-multiplayer`
- artifact ID: `9962299726`
- wrapper digest: `sha256:26eb75d50328ba72bb96f7b4e70593aa87a8987624b79b6a97bafe1b9bb2d67b`
- wrapper size from authoritative run: 209696 bytes

Important distinction: package SHA above is the inner installable Team Up ZIP. Artifact digest is GitHub's outer Actions artifact wrapper.

## Required live smoke before calling Alpha 6.6.1 live-verified

Checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_1_SHARED_PARTY_MULTIPLAYER_VI.txt`

Priority:

1. Single-player 1 Farmer + 5 NPC = 6/6; sixth NPC blocked.
2. Wait still consumes people slot.
3. Two online Farmers leave only four NPC slots.
4. Host recruits NPC A, farmhand recruits NPC B, each follows correct owner.
5. Host/farmhand split maps and combat independently.
6. Same NPC simultaneous invite race produces one owner only.
7. Shared external companion pool is exactly 2/2 across all owners.
8. NPC-only / NPC+companion / Cancel top-level flow.
9. Full-pool replacement flow and ownership restrictions.
10. ChaCha and vanilla pet remain free.
11. Farmer join overflow deactivates NPC without data loss.
12. Disconnect/rejoin preserves RecruiterId/progression.
13. Sudoku PartyControlled handshake with updated Hey! You're Cursed! source.
14. MiMi farmhand recruit checks requesting Farmer friendship.
15. Re-test all five Party Strategies.
16. Re-test Surge status/reapply/clear/board and Cardcha sandbox.
17. Re-test equipment/controller/Vault/SVE-RSV core regressions.

## What NOT to claim yet

CI proves compile/source/package/direct-builder contracts, not live co-op behavior.

Do NOT call the following verified until tested in Stardew with at least two clients:

- remote farmhand recruit UX end-to-end;
- per-Farmer cross-map follow/combat behavior;
- join/disconnect capacity rebalancing;
- companion replacement ownership behavior;
- snapshot timing under latency;
- Sudoku source-mod runtime handoff after Hey! You're Cursed! counterpart patch.

## Recommended next step

Do not immediately expand formations/per-member strategies or increase caps.

First run the Alpha 6.6.1 single-player + 2-client smoke. If a live bug appears, make Alpha 6.6.2 a focused multiplayer/capacity hotfix. If smoke passes, the next broader milestone can return to Party Strategy UI polish and multiplayer-safe companion management UI.
