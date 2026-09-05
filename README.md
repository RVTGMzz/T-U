# Team Up!

**Party & Combat Companions for Stardew Valley**

> Build an RPG-style party from Stardew Valley NPCs, bring creature companions, assign combat roles, share loot, and take the team into monster-heavy content.

## Current development checkpoint

Current verified source line: **`v0.2.0-alpha.6.6.1` - Shared Party Capacity + Companion Choice + Multiplayer Foundation**.

Status: **compile/package/direct-builder verified; in-game and 2-client multiplayer smoke pending**.

If you are resuming development in a new chat/session, read [`CONTINUE_HERE.md`](CONTINUE_HERE.md) and the latest handoff first.

## Current party rules

Team Up now uses one shared party budget for the multiplayer farm.

### People capacity: 6 total

The six-person cap includes **online Farmers and active Team Up NPCs together**.

Examples:

```text
Single-player
1 Farmer + up to 5 active NPCs = 6/6

Two-player co-op
2 Farmers + up to 4 active NPCs = 6/6

Four-player co-op
4 Farmers + up to 2 active NPCs = 6/6
```

- `Following` and `Waiting` NPCs consume a people slot.
- If another Farmer joins and the active party would exceed six, overflow NPCs are moved to `Inactive` and returned to their source/vanilla schedule.
- Overflow never deletes roster ownership, level, equipment, progression, or linked-companion registration.
- Each recruited NPC keeps the `RecruiterId` of the Farmer who invited them.
- The same NPC cannot be owned by two Farmers at once.

### Combat companion capacity: 2 shared

The farm has a shared pool of **two deployed external combat companions**, including Pokemon-like creatures and summons owned by either a Farmer or a recruited NPC.

- `Active`, `Waiting`, and `ReturningHome` external creatures reserve a slot.
- `Standby` and `Inactive` do not reserve a slot.
- Vanilla dog/cat pets are free and do not consume the 2/2 pool.
- **ChaCha is a free Special Companion** and consumes neither a people slot nor a combat-companion slot.
- External creature/summon providers can use Team Up's lightweight runtime `modData` contract without a hard DLL dependency.

Recommended maximum combat footprint with one Farmer:

```text
Farmer
├─ NPC 1
├─ NPC 2
├─ NPC 3
├─ NPC 4
├─ NPC 5
├─ External Companion 1
├─ External Companion 2
└─ ChaCha (optional free Special Companion)
```

## NPC + companion recruitment

When Team Up detects that an invited NPC currently has a linked companion, recruitment uses three top-level choices:

```text
Invite Abigail to Team Up?

> Abigail only
  Abigail + Pikachu
  Cancel
```

If the player chooses `NPC + companion` while the shared companion pool is already 2/2, Team Up opens a replacement choice. The selected active companion is moved to Standby before the new companion is deployed.

In multiplayer, a farmhand may replace their own active companion. The host retains farm-wide authority. Team Up never silently steals another farmhand's companion slot.

## Multiplayer foundation

Alpha 6.6.1 introduces a **host-authoritative shared-party model**.

- Farmhands can interact with NPCs and request recruit/leave/member actions.
- The host validates and commits shared party state.
- The host broadcasts party snapshots back to clients.
- Recruit requests are race-safe at the party-state level: once an NPC has an owner, a second Farmer cannot recruit the same NPC.
- Each online Farmer gets their own follow/combat owner context based on `RecruiterId`.
- NPCs follow and fight around the Farmer who recruited them, including when Farmers split across different maps.
- On disconnect, that Farmer's NPCs are deactivated and their companions leave active deployment, while roster/progression remains saved for later.

### Alpha 6.6.1 multiplayer limitation

Remote farmhand inventory mutation is intentionally fail-closed in this foundation milestone. Equipment management that would transfer items between remote inventories remains host-authoritative until a later multiplayer inventory-safe implementation is live-tested.

## Creature integration runtime contract

External mods can identify live creature actors using `NPC.modData` instead of requiring Team Up to reference their DLL or private save model.

Supported Alpha 6.6.1 keys include:

```text
Ronvotri.TeamUp/CompanionKind
Ronvotri.TeamUp/CompanionOwnerCharacter
Ronvotri.TeamUp/CompanionOwnerFarmerId
Ronvotri.TeamUp/CompanionProviderId
Ronvotri.TeamUp/CompanionProviderUnitId
```

Important companion kinds include `FarmerSummon`, `LinkedCompanion`, and the existing special-companion classifications.

Provider mods remain responsible for their own story, unlock, spawning, identity, and private save data.

## Sudoku compatibility handshake

Team Up can recruit the Hey! You're Cursed! NPC with canonical ID:

`ronvotri.HeyYoureCursed_Sudoku`

When Team Up owns an NPC's follow/combat movement, it writes runtime markers on that actor:

```text
Ronvotri.TeamUp/PartyControlled = true
Ronvotri.TeamUp/PartyControllerOwner = <Farmer UniqueMultiplayerID>
```

The markers are removed when Team Up releases the NPC. Hey! You're Cursed! can use `PartyControlled` to pause Sudoku's roommate movement while Team Up owns her, without either mod reading the other's private save/service state.

## MiMi compatibility

MiMi remains a source-owned custom recruit from Cardcha:

- Cardcha UniqueID: `Ronvotri.Cardcha`
- canonical MiMi NPC ID: `Ronvotri.Cardcha_MiMi`
- Team Up uses the live Stardew social/friendship layer as its recruit gate.
- In multiplayer, the friendship gate is checked against the **Farmer who actually sent the recruit request**, not automatically against the host.
- Team Up does not read Cardcha private SaveData/services or hard-code MiMi's work schedule.

ChaCha remains a Special/Farmer Companion and never becomes a Main Party member.

## Party Strategy

Alpha 6.6.0 introduced five party-wide tactical strategies, retained unchanged in Alpha 6.6.1:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Console command:

```text
teamup_strategy <status|balanced|defensive|aggressive|hold|boss>
```

Strategy is config-backed rather than stored in PartySaveData. Switching strategy clears combat runtime locks for clean retargeting.

Key behavior locks:

- hard leash remains 12 tiles;
- Aggressive does not disable the leash;
- Hold Position does not chase targets outside attack range;
- Boss Focus prefers the highest-MaxHealth target only among already-valid candidates;
- target lock, facing hold, and anti-spin protections remain active.

## Party roles

| Role | Purpose |
| --- | --- |
| Tank | Hold threat, protect allies, intercept enemies, survive pressure. |
| DPS | Primary damage dealer. |
| Support | Buff allies, debuff enemies, improve party performance. |
| Healer | Restore HP, shield allies, emergency recovery. |
| Control | Stun, slow, root, knock back, interrupt, or manipulate enemy positioning. |

NPCs use affinity profiles rather than hard class locking. Team Up also retains Engagement Styles, equipment/loadout progression, character skill identities, friendship bonds, and expansion NPC profiles.

## Custom recruit identities

Current explicit custom recruits include:

- MiMi: Support / Control, signature `BROOMTAIL SIGIL`.
- Sudoku: Control / Damage, signature `NINEFOLD SEAL`.

Team Up respects each source mod's own unlock/materialization progression.

## Monster Surge

The Surge remains regression-locked from Alpha 6.5.2/6.5.3:

- target monster density around x2 in eligible combat zones;
- Team Up-owned safe `GreenSlime` overlay only;
- no blind cloning of arbitrary custom/boss/story monsters;
- Cardcha test arena excluded;
- Team Up Surge extras have loot suppressed by default;
- safe placement uses `isTileOnMap`, `isTilePassable`, and `IsTileBlockedBy`;
- never reintroduce `isTileLocationTotallyClearAndPlaceable` for this system;
- debug commands retain status/reapply/clear/board behavior.

## Party Vault and UI

Party Vault remains a permanent shared-party inventory feature. Equipment double-click, controller focus/navigation, Codex/profile UI, Party Vault drag/drop, and the SVE/RSV expansion profile/icon work remain regression-locked.

## Build and smoke test

One-click local build:

`BUILD_V0_2_ALPHA6.bat`

Direct builder:

`BuildV0_2Alpha661.ps1`

Live smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_1_SHARED_PARTY_MULTIPLAYER_VI.txt`

CI verifies compilation, source contracts, package output, and builder idempotency. Multiplayer behavior still requires a real two-client in-game smoke before Alpha 6.6.1 can be called live-verified.

## Independent development / clean-room rule

Team Up! is a new independent codebase. It is not intended to be a fork, modification, or redistribution of The Stardew Squad.

Project rules:

- Do not copy or redistribute The Stardew Squad code, DLLs, assets, translations, content packs, UI assets, or dialogue.
- Do not port its internal classes or implementation into Team Up!.
- Build Team Up! systems from a fresh architecture using Stardew Valley, SMAPI, and permitted dependencies/APIs.
- Compatibility research with other mods is allowed, but compatibility research must not become code/asset copying.

## Naming

- **Display name:** Team Up!
- **Repository:** `ronvotri/Team-Up`
- **SMAPI UniqueID:** `Ronvotri.TeamUp`
- **Working subtitle:** *Party & Combat Companions for Stardew Valley*

## Design compass

> Don't ask only, "What work can this NPC do for the player?" Ask, **"What role does this companion play in the party?"**

Team Up's identity is an RPG-style shared party framework for Stardew Valley combat, exploration, NPCs, creature companions, shared loot, and source-respecting mod integrations.
