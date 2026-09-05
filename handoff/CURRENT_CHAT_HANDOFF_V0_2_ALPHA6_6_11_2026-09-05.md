# Team Up v0.2.0-alpha.6.6.11 Handoff

Date: 2026-09-05

## Resume point

Development branch:

`v0.2-alpha6-6-11-no-companion-profiles`

Final handoff branch:

`v0.2-alpha6-6-11-no-companion-profiles-handoff`

Version: `0.2.0-alpha.6.6.11`

Status: **compile/package/direct-builder verified; live validation pending**.

## Why this milestone exists

The user reported that Pokemon and summoned/external companion actors could incorrectly show Team Up's Profile hint during dialogue and open an empty `Special / Companion` profile. Screenshots included Rowlet with `L (Controller) / Q Ho so` and a blank `PelipperTown.Villager.Maru` placeholder profile.

Product rule now locked: **Pokemon, summons, external creatures, and special companion actors do not own Team Up character profiles. Character profiles are for recruitable/catalogued people.**

## Alpha 6.6.11 implementation

`ModEntry.cs` now has `CanOpenDirectProfile(NPC npc)`.

The eligibility rule allows:

- recruited Team Up people;
- explicit custom recruits;
- normal/catalogued real NPC profiles.

It rejects:

- actors already represented by `Party.CompanionUnits`;
- actor kinds `FarmerCompanion`, `FarmerSummon`, `SpecialCompanion`, `LinkedCompanion`;
- unprofiled `PelipperTown.*` runtime proxy actors;
- actors detected as Pelipper source actors without a real Team Up catalog profile;
- unknown non-villager actors without a catalog profile.

Three independent guards prevent profile leakage:

1. `DrawDialogueActions(...)` returns before drawing Team Up dialogue tags for companion/summon speakers.
2. the dialogue `ProfileKey` path checks `CanOpenDirectProfile(speaker)` before suppress/open.
3. `OpenProfileFromDialogue(NPC npc)` checks `CanOpenDirectProfile(npc)` again before creating a menu.

Do not remove the final guard just because the UI hint is hidden. The UI/input/open layers intentionally defend the same product rule independently.

## Alpha 6.6.10 Pelipper flicker authority lock

Pelipper source-controlled companions must be skipped in `FollowService.UpdateCompanionUnits` before `ResolveCharacter(unit.CharacterName)`.

Team Up must not Prepare/Hold/Follow/Warp/Halt/change controllers for Pelipper Pokemon. Pelipper Town is sole movement/render authority. Soft deployment marker writes stay idempotent.

## Previous live/performance locks

- Alpha 6.6.7 removed severe river/bridge lag and user confirmed it.
- Combat unreachable retry cooldown = 24 ticks.
- Combat movement pulse = 3 ticks.
- Pelipper decorative/source actors excluded from hostile Team Up targeting by default.
- Never restore `isTileLocationTotallyClearAndPlaceable` to Follow, Combat, or Surge.
- Alpha 6.6.8 humanoid party NPCs reject bare water, real Buildings-layer bridges remain valid, and stranded humanoids are rescued to land.
- Alpha 6.6.9 thin HUD and contextual overhead NPC health bars remain.
- health/downed/state multiplayer snapshot sync remains.

## Party rules

- people cap = 6 total including online Farmers + active Team Up NPCs;
- shared external combat companion cap = 2 across Farmer-owned + NPC-linked companions;
- Active/Waiting/ReturningHome reserve companion slots;
- Standby/Inactive do not;
- vanilla pet free;
- ChaCha free and never Main Party.

## Input / UI locks

- Switch semantic Action Button equip;
- Switch semantic Use Tool unequip;
- controller activation debounce 180 ms;
- virtual mouse echo suppression 260 ms;
- mouse inventory double-click 450 ms;
- Codex D-pad and left analog exactly one profile per input;
- profile detailed content scale = 1.52f;
- ASCII-safe punctuation retained.

## Character compatibility locks

MiMi:

- canonical `Ronvotri.Cardcha_MiMi`;
- source `Ronvotri.Cardcha`;
- signature `BROOMTAIL SIGIL`;
- requesting Farmer friendship gate;
- no Cardcha private SaveData/service access.

Sudoku:

- canonical `ronvotri.HeyYoureCursed_Sudoku`;
- signature `NINEFOLD SEAL`;
- movement contract while Team Up controls Sudoku:
  - `Ronvotri.TeamUp/PartyControlled = true`
  - `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`

## Build pipeline

Builder: `BuildV0_2Alpha6611.ps1`

One-click launcher: `BUILD_V0_2_ALPHA6.bat`

Smoke checklist: `SMOKE_TEST_V0_2_ALPHA6_6_11_NO_COMPANION_PROFILES_VI.txt`

First materialization CI: `33979366255`

Materialized source commit: `8dbcf93`

Final authoritative input commit: `3a0db6d2c11af3576246fca5e5502171f1e9569a`

Final authoritative CI: `33979472213`

Authoritative result:

- 0 warnings;
- 0 errors;
- companion/summon direct profile block PASS;
- dialogue Profile hint/input/final-open guards PASS;
- Alpha 6.6.10 follow-authority regression PASS;
- Alpha 6.6.7 performance regression PASS;
- Alpha 6.6.8 land-safe regression PASS;
- Alpha 6.6.9 health UI regression PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.11_NO_COMPANION_PROFILES_HOTFIX_TEST.zip`

Authoritative package SHA256:

`0cd4a7c9712b9367f4e5d03ab2298fadf56d2fa11cb0a5c7f931c6209a8ccb62`

Artifact ID: `9973321194`

Artifact wrapper digest:

`sha256:3ebd0f2d7adb28d7a622ae0da93b68523d53af2c1a811b4ce175810a26356272`

## Required live validation

1. Talk to Farmer Pokemon such as Rowlet: no Team Up Profile hint and ProfileKey must not open a profile.
2. Talk to NPC-linked Pokemon: same.
3. `PelipperTown.Villager.*` / `PelipperTown.Player.*` proxies without real catalog profiles must not open `Special / Companion` placeholders.
4. Real NPCs, MiMi, Sudoku, and Codex catalog entries must still open profiles normally.
5. Confirm 6.6.10 Pokemon flicker behavior in game.
6. Recheck river/bridge performance, humanoid land safety, NPC HP bars, Switch equipment, and Codex one-profile navigation.
