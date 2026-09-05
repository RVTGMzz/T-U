# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.1**

Status: **compile/package/direct-builder verified; in-game and 2-client multiplayer smoke pending**.

Development source branch:

`v0.2-alpha6-6-1-party-capacity-multiplayer`

Final handoff branch:

`v0.2-alpha6-6-1-party-capacity-multiplayer-handoff`

Read this handoff first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_1_2026-09-05.md`

## Alpha 6.6.1 locked product rules

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

### Multiplayer foundation

- Shared party state is host-authoritative.
- Farmhands send recruit/leave/member requests to host.
- Host validates, commits, and broadcasts party snapshots.
- NPC Follow/Combat is routed by `RecruiterId` and the corresponding online Farmer context.
- Remote farmhand equipment inventory mutation remains intentionally fail-closed/host-authoritative in Alpha 6.6.1.

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

`33942539462`

Result:

- direct `BuildV0_2Alpha661.ps1`
- build success
- 0 warnings
- 0 errors
- source acceptance PASS
- package verification PASS
- `No materialized source diff.`
- artifact upload PASS

Package:

`TeamUp_v0.2.0-alpha.6.6.1_SHARED_PARTY_MULTIPLAYER_TEST.zip`

Package SHA256:

`7140af72b24b6e2eba06ebca26b0381bfc082305f1e9f18ac013e353bdd75655`

Artifact ID:

`9962299726`

Artifact wrapper digest:

`sha256:26eb75d50328ba72bb96f7b4e70593aa87a8987624b79b6a97bafe1b9bb2d67b`

First successful materialized Alpha 6.6.1 source commit:

`a6a32b11e565e51eaf598f9a97f18a254db6bc92`

Direct-builder authoritative source/workflow checkpoint:

`9f0a4d00b76af620e9be6f2abd28341e4a3cc42b`

## Regression locks retained

- Party Strategy: `Balanced`, `Defensive`, `Aggressive`, `HoldPosition`, `BossFocus`.
- hard leash 12 tiles.
- target lock 45 ticks.
- facing hold 10 ticks.
- Hold Position no chase outside attack range.
- Aggressive never disables hard leash.
- Boss Focus only prioritizes MaxHealth among already-valid candidates.
- Surge safe GreenSlime overlay, Cardcha sandbox exclusion, loot suppression, marker-scoped clear/reapply.
- Never reintroduce `isTileLocationTotallyClearAndPlaceable` to Surge placement.
- MiMi live social/friendship gate, with requesting Farmer checked in multiplayer.
- Sudoku canonical ID `ronvotri.HeyYoureCursed_Sudoku` and `NINEFOLD SEAL`.
- MiMi `BROOMTAIL SIGIL`.
- 51 SVE/RSV expansion profiles/icons/balance.
- equipment double-click.
- controller focus.
- Party Vault drag/drop.
- Origin story.

## Required live validation

Use:

`SMOKE_TEST_V0_2_ALPHA6_6_1_SHARED_PARTY_MULTIPLAYER_VI.txt`

Priority tests:

1. Single-player Farmer + 5 NPC = 6/6; sixth NPC blocked.
2. Two Farmers online = at most 4 active NPCs.
3. Host and farmhand split maps; each NPC follows/fights around its recruiter.
4. Shared external companion pool stays 2/2 across all Farmers/NPCs.
5. NPC linked-companion 3-option recruit flow and replacement flow.
6. ChaCha and vanilla pet remain free.
7. Sudoku PartyControlled marker pauses source roommate movement after HeyYoureCursed compatibility patch; marker disappears on Leave.
8. Disconnect/rejoin preserves ownership/progression and deactivates safely.
9. MiMi gate uses requesting Farmer's friendship entry.
10. Re-test Party Strategy, Surge, equipment/controller/Vault and expansion NPC regressions.

Do not call Alpha 6.6.1 multiplayer live-verified until a real 2-client smoke passes.
