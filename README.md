# Team Up!

**Party & Combat Companions for Stardew Valley**

> Build an RPG-style party from Stardew Valley NPCs, bring creature companions, assign combat roles, share loot, and take the team into monster-heavy content.

## Current development checkpoint

Current version: **`0.2.0-alpha.6.7.44.40`**

Current branch:

`v0.2-alpha6-7-44-40-final-runtime-closure`

Current state:

- CI-verified build: PASS, 0 warnings / 0 errors;
- `main`: not merged;
- Alpha 6.7.45: not started;
- current work is still in live-runtime validation before the next story build.

Resume development from:

- `CONTINUE_HERE.md`
- `LATEST_TEAM_UP_HANDOFF.md`
- `docs/LATEST_HANDOFF.md`
- `docs/ALPHA_6_7_44_40_FINAL_RUNTIME_CLOSURE_HANDOFF.md`

## Current verified artifact

- CI source SHA: `f658913ed19b92491facddf3f98bc1ff3c9d1e05`
- CI run: `35419812620`
- CI job: `105835213977`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.40_FINAL_RUNTIME_CLOSURE_TEST.zip`
- ZIP SHA256: `5d5ad7a90577f0a02a7397003553463914104eec57931ef9209188bcd8cad7d8`

Current live gate: run `teamup_preflight` to close the remaining 6.7.44 runtime evidence. The 6.7.44.24-30 crash stack remains excluded.

## Current Mutation system

A Mutation encounter is currently designed as:

- **1 Mutant leader**;
- **2-4 ordinary hostile minions**.

Mutant leader:

- HP x3;
- stat x2;
- Mutation aura;
- Pelipper visible Pokemon scale capped at x2;
- x3 native loot on final defeat.

Minions:

- ordinary hostile units;
- no Mutation bonuses;
- no Mutation aura;
- no x3 leader loot;
- mutation-excluded to prevent recursive Mutation.

Global x3 loot is intended for every Mutant leader supported by the native drop hook, not only Pelipper Town monsters.

### Pelipper lightweight minions

For a Pelipper Mutant leader, Team Up does **not** create 2-4 full native Pelipper wild encounters for temporary followers.

6.7.44.17 uses a performance-first one-actor follower path instead, avoiding extra native `PokemonNpc`, hidden combat proxy, `WildEncounterId`, Pelipper HP modData and source/proxy pairing work for each follower.

Captureability of these temporary followers is not a current requirement. Natural Pelipper wild Pokemon keep Pelipper's normal capture behavior.

If the lightweight follower visual needs improvement later, the preferred direction is a visual skin/override while preserving the lightweight one-actor architecture.

## Pelipper compatibility contract

Pelipper remains authoritative for its real Pokemon actors.

Visible source actor:

`PelipperTown.PokemonNpc`

This owns species identity, render/sprite, display name and Shiny evidence.

Hidden combat proxy:

`StardewValley.Monsters.Monster`

The proxy can expose a technical `Health/MaxHealth = 1,000,000` sentinel. That is **not** Pokemon HP.

Real Pelipper Pokemon combat HP is read from:

- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

Team Up preserves Pelipper controller/render/ownership authority and uses cached source/proxy pairing for compatibility.

## Current live-validation priorities

The current artifact still needs live confirmation for:

1. 2-4 lightweight Pelipper Mutation followers spawning successfully;
2. followers attacking the player/party normally;
3. no noticeable spawn hitch/lag;
4. Pelipper Mutant HP x3 across all three phases;
5. final global x3 leader reward;
6. one non-Pelipper Mutant regression test;
7. Lower Workings runtime gate.

Only after these runtime gates should Alpha 6.7.45 start, unless the user explicitly waives them.

## Lower Workings

Current story location:

`Ronvotri.TeamUp_LowerWorkings`

It uses `assets/LowerWorkings.tmx`, is registered through `Data/Locations`, has no static Warp and returns the party through the persisted breach route.

Planned next story build after runtime validation:

**Alpha 6.7.45 - Containment Chamber Escalation Encounter**

No final boss yet. George remains ordinary/anonymous until the planned later reveal.

## Party / combat direction

Team Up is building toward a party-RPG layer for Stardew Valley with:

- recruitable NPC party members;
- roles, tactics and combat behavior;
- companion/creature compatibility;
- shared Party Vault storage;
- NPC health/downed state;
- story progression and combat encounters;
- compatibility with major NPC/content expansions;
- host-authoritative multiplayer behavior.

Current story formation hard cap is **5 people including Farmers**. Multiplayer formation adapts to the number of connected Farmers.

## Party tactics

The five strategy values remain:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Tactics UI remains in Codex and multiplayer strategy remains host-authoritative.

## Controller and UI locks

Semantic controller behavior remains:

- Stardew/SMAPI Action Button activates/equips;
- Stardew/SMAPI Use Tool Button unequips;
- controller activation debounce: 180 ms;
- virtual mouse echo suppression: 260 ms;
- inventory mouse double-click equip: 450 ms;
- Codex D-pad / left analog moves one profile per input;
- detailed Character Profile scale remains `1.52f` unless a later UI pass explicitly changes it.

Active Team Up NPCs in `Following` or `Waiting` cannot receive held-item vanilla gifts. Inactive roster members keep normal gifting behavior.

## Performance / movement locks

Carry-forward safeguards include:

- combat unreachable-path retry cooldown = 24 ticks;
- combat movement pulse = 3 ticks;
- no `isTileLocationTotallyClearAndPlaceable` in Follow, Combat or Surge hot paths;
- humanoid followers reject bare-water destinations while real bridge/walkway tiles remain allowed;
- Pelipper encounter discovery is throttled and source/proxy pairing is cached;
- high Pelipper pair-cache `cacheHits` represent reuse, not repeated full scans.

## Shiny lock

Confirmed natural Shiny Pokemon remain Mutation-excluded. Current accepted Shiny behavior should not be rewritten without a concrete regression.

## Compatibility

### MiMi

- Cardcha source: `Ronvotri.Cardcha`
- canonical NPC: `Ronvotri.Cardcha_MiMi`
- signature: `BROOMTAIL SIGIL`
- Team Up does not depend on Cardcha private save/services.

### Sudoku

- canonical NPC: `ronvotri.HeyYoureCursed_Sudoku`
- signature: `NINEFOLD SEAL`
- Team Up party-control markers remain:
  - `Ronvotri.TeamUp/PartyControlled = true`
  - `Ronvotri.TeamUp/PartyControllerOwner = <Farmer ID>`

### Pelipper Town

Pelipper remains source/render/controller authority for native Pokemon actors. Team Up compatibility code should remain source-respecting and avoid taking ownership of Pelipper's native lifecycle.

## Build

Current CI build/audit/package script on the 6.7.44.40 branch:

`tools/build_alpha6744_40.py`

The workflow filename is historical; the current branch targets the 6.7.44.40 gate.

Current CI workflow:

`.github/workflows/team-up-alpha6-7-44-11-pelipper-pair-cache-dual-probe.yml`

The workflow filename is also historical; its current branch content targets 6.7.44.40.

## Independent development / clean-room rule

Team Up! is an independent codebase. Do not copy or redistribute code, DLLs, assets, translations, UI assets, or dialogue from unrelated closed implementations. Compatibility work should stay source-respecting and prefer documented/runtime contracts over private save coupling.

## Naming

- **Display name:** Team Up!
- **Repository:** `RVTGMzz/T-U`
- **SMAPI UniqueID:** `Ronvotri.TeamUp`
- **Working subtitle:** *Party & Combat Companions for Stardew Valley*
