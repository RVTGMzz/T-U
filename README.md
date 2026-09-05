# Team Up!

**Party & Combat Companions for Stardew Valley**

> Build an RPG-style party from Stardew Valley NPCs, bring creature companions, assign combat roles, share loot, and take the team into monster-heavy content.

## Current development checkpoint

Current verified source line: **`v0.2.0-alpha.6.6.3` - Live Test Hotfix**.

Status: **compile/package/direct-builder verified; the four reported runtime fixes now require in-game validation**.

If you are resuming development in a new chat/session, read [`CONTINUE_HERE.md`](CONTINUE_HERE.md) and the latest handoff first.

## Alpha 6.6.3 live-test hotfix

Alpha 6.6.3 is intentionally narrow. It addresses four issues found during the first real Alpha 6.6.2 playtest.

### 1. Controller equipment activation

The Equipment screen now keeps two controller interaction modes coherent:

- D-pad / left stick navigates Team Up's logical focus.
- Right stick can move the controller cursor over a specific item.
- Pressing `A` once equips the currently focused item or the item under the controller cursor, depending on the active interaction mode.
- Returning to D-pad / left-stick navigation clears stale pointer intent before `A` activation.
- `X` unequips, `Y` auto-equips, and mouse double-click remains available with the existing 450 ms window.

### 2. Codex analog navigation

The Codex no longer lets viewport scrolling and logical row selection drift apart.

- Right-stick scrolling moves the viewport by two rows and clamps the logical selection into the visible range.
- After scrolling deep into the list, touching the left stick no longer snaps the list back to an old NPC above the viewport.
- Left-stick Up/Down moves two rows per input for faster browsing.
- D-pad Up/Down remains one row per input for precise selection.
- Filter dropdowns retain one-option navigation.

### 3. Pelipper Town Pokemon quota integration

Alpha 6.6.3 adds optional compatibility for Pelipper Town (`Griff.PelipperTown`) so its live villager/player Pokemon can participate in Team Up's existing shared combat-companion quota.

- No hard Pelipper Town DLL dependency is added.
- Team Up first respects its generic `Ronvotri.TeamUp/*` companion metadata contract.
- If that contract is absent, the optional Pelipper adapter detects a live Pelipper companion from runtime identity/owner metadata and conservative same-location proximity.
- A recruited NPC with a detected Pelipper partner gets the normal three-way recruit flow: NPC only / NPC + companion / Cancel.
- Pelipper companions are registered as `ExternalCreature` units, so the same hard shared `2/2` pool applies.
- Existing Alpha 6.6.2 saves with several visible Pelipper partners are reconciled periodically. Only up to two can remain deployed; overflow is Standby/suppressed.
- Pelipper remains movement authority for its own Pokemon. Team Up counts/suppresses deployment state but does not run its follower pathfinding controller on those source-owned actors.
- Leaving the owner restores the Pelipper companion to source control.

Diagnostics:

```text
teamup_pelipper status
teamup_pelipper reconcile
```

If live detection reports zero partners while visible Pelipper Pokemon are present, use the diagnostic output plus a fresh SMAPI log for the next compatibility hotfix instead of weakening the 2/2 rule.

### 4. Water / bridge / narrow-path performance

The follower pathfinding loop was reduced in cost for maps where formation offsets repeatedly land on water, cliffs, bridges, or narrow walkways.

- Follow updates now run every 4 ticks instead of every 2 ticks.
- Fallback formation tile search uses a small deterministic set of candidate offsets instead of scanning square rings up to radius 3 for every follower.
- Follow target validation uses map/passable checks instead of `isTileLocationTotallyClearAndPlaceable`.
- Pelipper source-owned Pokemon are excluded from Team Up follower pathfinding, preventing two movement controllers from fighting over the same actor.
- Long-distance warp/catch-up behavior remains available.

This is a targeted performance hotfix, not a claim that every possible water-map slowdown is solved. The in-game bridge/water smoke test is authoritative.

## Party Tactics UI

Codex retains the dedicated `Tactics / Chiến thuật` footer entry added in Alpha 6.6.2.

The Tactics screen supports mouse, keyboard, and controller navigation and shows:

- current party strategy;
- all five strategy choices;
- live shared people capacity;
- live shared external combat-companion capacity;
- whether the current player is host authority or a farmhand requester.

The five-value strategy contract remains unchanged:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Multiplayer strategy remains host-authoritative. Farmhands request a strategy change; the host validates/applies it, clears local and remote combat runtime locks, and broadcasts the resulting state.

## Current party rules

### People capacity: 6 total

The six-person cap includes **online Farmers and active Team Up NPCs together**.

```text
Single-player: 1 Farmer + up to 5 active NPCs = 6/6
Two-player co-op: 2 Farmers + up to 4 active NPCs = 6/6
Four-player co-op: 4 Farmers + up to 2 active NPCs = 6/6
```

`Following` and `Waiting` NPCs consume people slots. Overflow caused by another Farmer joining becomes `Inactive` without deleting roster ownership, progression, level, equipment, or companion registration.

### Combat companion capacity: 2 shared

The farm has one shared pool of **two deployed external Pokemon/summon/creature companions** across all Farmers and NPCs.

- `Active`, `Waiting`, and `ReturningHome` reserve a slot.
- `Standby` and `Inactive` do not.
- Farmer-owned and NPC-linked creatures share the same pool.
- Vanilla dog/cat pets are free.
- **ChaCha is free**, consumes neither people nor combat-companion slots, and never enters Main Party.
- Alpha 6.6.3 extends live quota accounting to detected Pelipper Town Pokemon.

## NPC + companion recruitment

When Team Up detects a linked companion, recruitment offers:

1. NPC only
2. NPC + companion
3. Cancel

If the player chooses NPC + companion at `2/2`, Team Up opens the existing replacement flow. Farmhands may replace companions they are allowed to control; the host remains farm-wide authority.

## Multiplayer foundation

Shared party state remains host-authoritative:

- farmhands send recruit/leave/member requests;
- host validates and commits party state;
- host broadcasts party snapshots;
- the same NPC cannot be owned by two Farmers;
- NPC follow/combat routing uses `RecruiterId` and the matching online Farmer;
- disconnect/rejoin preserves roster/progression;
- remote farmhand equipment inventory mutation remains intentionally fail-closed/host-authoritative until a later inventory-safe multiplayer implementation is live-tested.

## Creature integration runtime contract

External mods may identify live companion actors through `NPC.modData` without requiring Team Up to reference their DLL/private save model:

```text
Ronvotri.TeamUp/CompanionKind
Ronvotri.TeamUp/CompanionOwnerCharacter
Ronvotri.TeamUp/CompanionOwnerFarmerId
Ronvotri.TeamUp/CompanionProviderId
Ronvotri.TeamUp/CompanionProviderUnitId
```

Provider mods remain responsible for story, unlock, spawning, identity, movement, and private save state unless an explicit compatibility contract says otherwise.

## Sudoku and MiMi compatibility

Sudoku canonical ID remains:

`ronvotri.HeyYoureCursed_Sudoku`

While Team Up owns Sudoku movement it writes:

```text
Ronvotri.TeamUp/PartyControlled = true
Ronvotri.TeamUp/PartyControllerOwner = <Farmer UniqueMultiplayerID>
```

MiMi remains source-owned by Cardcha with canonical ID `Ronvotri.Cardcha_MiMi`. In multiplayer, MiMi recruitment checks the requesting Farmer's live friendship entry. Team Up does not read Cardcha private save/services.

Signature identities remain:

- MiMi: `BROOMTAIL SIGIL`
- Sudoku: `NINEFOLD SEAL`

## Regression locks

- hard leash 12 tiles;
- target lock 45 ticks;
- facing hold 10 ticks;
- anti-spin;
- Hold Position no chase outside attack range;
- Aggressive never disables the leash;
- Boss Focus only prioritizes highest MaxHealth among already-valid candidates;
- Surge safe GreenSlime overlay, Cardcha arena exclusion, loot suppression and validation harness;
- Surge must not use `isTileLocationTotallyClearAndPlaceable`, `Activator.CreateInstance`, or `MemberwiseClone`;
- 51 SVE/RSV profiles/icons/balance;
- Party Vault drag/drop and `releaseLeftClick`;
- equipment mouse double-click 450 ms;
- Origin story.

## Build and smoke test

One-click local build:

`BUILD_V0_2_ALPHA6.bat`

Direct builder:

`BuildV0_2Alpha663.ps1`

Live smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_6_3_LIVE_TEST_HOTFIX_VI.txt`

Authoritative CI run:

`33954805549`

Authoritative input commit:

`59cfa37002172f755c6345730027513d3f54c9be`

Package:

`TeamUp_v0.2.0-alpha.6.6.3_LIVE_TEST_HOTFIX_TEST.zip`

Package SHA256:

`266e4226c09a4c39710f46a1083f90f32a11c392f35589a6c0e3aa5161b7c092`

Artifact ID:

`9965997611`

Artifact wrapper digest:

`sha256:1a3164bd486d3af23008075ad99310f95282dbe0b8fb767a8c7d88cd3fdf43cd`

CI compiled with 0 warnings and 0 errors, all Alpha 6.6.3 source/package acceptance checks passed, and the authoritative direct-builder run reported `No materialized source diff.`.

## Independent development / clean-room rule

Team Up! is an independent codebase. Do not copy or redistribute code, DLLs, assets, translations, UI assets, or dialogue from unrelated closed implementations. Compatibility work must stay source-respecting and should prefer documented/runtime contracts over private save coupling.

## Naming

- **Display name:** Team Up!
- **Repository:** `ronvotri/Team-Up`
- **SMAPI UniqueID:** `Ronvotri.TeamUp`
- **Working subtitle:** *Party & Combat Companions for Stardew Valley*
