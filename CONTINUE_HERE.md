# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.11**

Status: **compile/package/direct-builder verified with 0 warnings, 0 errors and `No materialized source diff.`. Live validation pending for the no-companion-profile UX and the Pelipper flicker fix inherited from 6.6.10.**

Development branch:

`v0.2-alpha6-6-11-no-companion-profiles`

Final handoff branch:

`v0.2-alpha6-6-11-no-companion-profiles-handoff`

Read first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_11_2026-09-05.md`

## Alpha 6.6.11: no profiles for Pokemon / summons

Live testing showed Team Up drew the Profile hint for talkable Pokemon/summon actors and could open a blank `Special / Companion` placeholder such as `PelipperTown.Villager.Maru`.

New direct-profile eligibility rule in `ModEntry.cs`:

- recruited people remain valid profile owners;
- explicit custom recruits remain valid profile owners;
- actors registered in `Party.CompanionUnits` are not profile owners;
- `FarmerCompanion`, `FarmerSummon`, `SpecialCompanion`, and `LinkedCompanion` actor kinds are not profile owners;
- unprofiled `PelipperTown.*` runtime proxy actors are not profile owners;
- unknown non-villager actors without a catalog profile are not profile owners;
- catalogued real NPC profiles continue to work.

The guard is enforced at three levels:

1. dialogue Profile hint is not drawn for companion/summon actors;
2. ProfileKey/controller input does not open their profile;
3. `OpenProfileFromDialogue()` has a final guard, preventing the blank placeholder from opening through another direct path.

## Alpha 6.6.10 Pelipper movement-authority lock preserved

Pelipper source-controlled Pokemon are skipped in `FollowService.UpdateCompanionUnits` before actor resolution. Team Up therefore does not `PrepareForParty`, Hold, Follow, Warp, Halt, or replace movement controllers for Pelipper Pokemon. Pelipper remains sole movement/render authority. Soft Active/Standby deployment marker writes remain idempotent.

## Performance / land safety / health locks

- Alpha 6.6.7 severe river/bridge lag fix preserved;
- Combat path retry cooldown = 24 ticks;
- Combat movement pulse = 3 ticks;
- never restore `isTileLocationTotallyClearAndPlaceable` to Follow, Combat, or Surge;
- Alpha 6.6.8 humanoid bare-water rejection and true bridge allowance preserved;
- Alpha 6.6.9 thin HUD + overhead NPC health bars preserved;
- host health/downed/state snapshot sync preserved.

## Party / compatibility locks

- 6 total people across online Farmers + active Team Up NPCs;
- shared external combat companion cap = 2 across Farmer-owned + NPC-linked units;
- vanilla pet free;
- ChaCha free and never Main Party;
- strategies: Balanced, Defensive, Aggressive, HoldPosition, BossFocus;
- MiMi: `Ronvotri.Cardcha_MiMi`, `BROOMTAIL SIGIL`, no Cardcha private save/service coupling;
- Sudoku: `ronvotri.HeyYoureCursed_Sudoku`, `NINEFOLD SEAL`;
- Sudoku movement markers remain `Ronvotri.TeamUp/PartyControlled` and `Ronvotri.TeamUp/PartyControllerOwner`;
- Switch Action Button equip / Use Tool unequip;
- controller debounce 180 ms;
- virtual mouse echo suppression 260 ms;
- inventory mouse double-click 450 ms;
- Codex D-pad/left analog exactly one profile per input;
- detailed profile scale = 1.52f;
- ASCII-safe punctuation retained.

## Authoritative Alpha 6.6.11 checkpoint

First materialized source commit: `8dbcf93`

Authoritative input commit: `3a0db6d2c11af3576246fca5e5502171f1e9569a`

Authoritative CI run: `33979472213`

Result:

- `BuildV0_2Alpha6611.ps1` success;
- 0 warnings;
- 0 errors;
- companion/summon direct profile block PASS;
- dialogue Profile hint/input/final-open guards PASS;
- Alpha 6.6.10 Pelipper follow-authority regression PASS;
- Alpha 6.6.7 performance regression PASS;
- Alpha 6.6.8 land-safe regression PASS;
- Alpha 6.6.9 health UI regression PASS;
- package verification PASS;
- `No materialized source diff.`;
- artifact upload PASS.

Package:

`TeamUp_v0.2.0-alpha.6.6.11_NO_COMPANION_PROFILES_HOTFIX_TEST.zip`

Package SHA256:

`0cd4a7c9712b9367f4e5d03ab2298fadf56d2fa11cb0a5c7f931c6209a8ccb62`

Artifact ID: `9973321194`

Artifact wrapper digest:

`sha256:3ebd0f2d7adb28d7a622ae0da93b68523d53af2c1a811b4ce175810a26356272`

## Highest-priority live validation

1. Talk to Farmer Pokemon such as Rowlet: no Team Up `Ho so` hint; pressing Profile must not open a profile.
2. Talk to NPC-linked Pokemon: same behavior.
3. `PelipperTown.Villager.*` / `PelipperTown.Player.*` runtime proxies must not open `Special / Companion` placeholders.
4. Real NPCs, MiMi, Sudoku, and catalogued Codex entries must still open profiles normally.
5. Confirm Pokemon remain stable/no flicker under the 6.6.10 authority fix.
6. Recheck river/bridge FPS, humanoid land safety, NPC health bars, Switch equipment, and Codex one-profile navigation.
