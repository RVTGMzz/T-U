# Team Up - Canonical Latest Handoff

This file is the canonical pointer for continuing Team Up in a new chat. Read this file first, then read the checkpoint-specific handoff if more detail is needed.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44`
- Branch: `v0.2-alpha6-7-44-lower-workings-interior-survey`
- Previous checkpoint branch head / exact base: `88094c40af824d9e61537e220c557c7a22b35e1b`
- 6.7.44 CI input: `644a4b28b1fbde3729d4c41b5117d69dcecbc8c7`
- CI-verified materialized source: `1c5355874aec8b53064b3a102489ed81ee4cb0fe`
- Successful CI run: `34616862316`
- Successful CI job: `103320840886`
- Artifact ID: `10270816070`
- Artifact name: `team-up-alpha6-7-44-lower-workings-interior-survey`
- Artifact wrapper SHA256: `0751aa4da65b119c16773f571ea8682a198593f95dfc21d8c65ba356fd067228`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44_LOWER_WORKINGS_INTERIOR_SURVEY_TEST.zip`
- Inner ZIP SHA256: `7cd840107a8998b09e27e83c3461262495818b58e79fb2309a432c921406ee0b`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared; live in-game verification still required

## 6.7.44 implemented
Alpha 6.7.44 is the first actual dedicated Lower Workings map/chamber checkpoint.

Location:
- `Ronvotri.TeamUp_LowerWorkings`
- Stardew 1.6 `Data/Locations` + vanilla `StardewValley.GameLocation`
- mod-owned `assets/LowerWorkings.tmx`, `32x24`, using vanilla `Mines/mine.png`
- dynamic return path; no static TMX Warp

Persistent 6.7.44 state:
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyStage`
- `Ronvotri.TeamUp/Story/LowerWorkingsBreachTile`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorEntered`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyComplete`
- `Ronvotri.TeamUp/Story/LowerWorkingsSafeReturnUsed`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyReported`

Route `0..6`:
1. Guild authorizes dedicated survey after first descent is complete.
2. Full adaptive formation returns to recorded breach; new saves already have a persisted tile anchor, legacy 6.7.43 saves calibrate it once.
3. Enter the real Lower Workings location.
4. Hold full formation for 120 ticks near directed cribbing `(8,9)`.
5. Hold 120 ticks near newer Mutation-linked residue over older blast scoring `(22,8)`.
6. Hold 120 ticks near deeper sealed-pressure edge `(23,16)`.
7. Return to entry `(15,21)`, use secured withdrawal to the exact recorded breach anchor, then report at Guild.

Early withdrawal preserves survey stage. Host owns story progression; farmhands can use the shared route after host activation without writing host flags.

Story evidence now proves the current Mutation/Surge activity is interacting with an older deliberately sealed system, but the historical worker and the source/entity beyond the seal remain unknown.

No boss, story monster, containment encounter, George reveal, or final seal opening is introduced in 6.7.44.

## Bundled reactions
Reaction catalog now supports windows `0..41` with exact EN/VI parity and `584` lines per language.

New windows:
- `36`: dedicated interior survey authorized
- `37`: real Lower Workings location entered
- `38`: directed cribbing surveyed
- `39`: Mutation-linked residue surveyed
- `40`: deeper sealed-pressure edge surveyed
- `41`: interior survey reported complete

Same curated 14 NPCs per new window:
Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

Resolver after first-descent completion:
- interior stage `<=0 -> 35`
- `1 -> 36`
- `2 -> 37`
- `3 -> 38`
- `4 -> 39`
- `5 -> 40`
- `>=6 -> 41`

## Carried-forward story truth
- SURGE HIGH confirmed.
- Story NPC slots are 4/4.
- Hard formation cap remains 5 PEOPLE total including Farmers.
- HIGH Response Entry Protocol is READY.
- First descent / threshold crossing is complete.
- The team has now entered a real Lower Workings location and mapped its first chamber layer.
- Directed emergency cribbing confirms deliberate containment engineering.
- Newer Mutation-linked residue lies over old blast scoring, linking the present Surge to the older sealed system without implying the present Mutations caused the original blast work.
- A deeper sealed-pressure edge remains active and unidentified.
- Historical worker identity is still unknown to the player.
- Object/entity/source beyond the seal is still not definitively identified.

## Hard lore locks
George before reveal remains:
- observed Rank D;
- Non-Combatant;
- unrecruitable;
- no meaningful combat signature;
- no `Rank S`;
- no `The Last Blaster`;
- no explicit identification as the miner who sealed the chamber.

Future George reveal remains planned for 6.7.46.

Evelyn remains ordinary low Rank D healer/support during main story; her secret is postgame only.

Do NOT introduce exact `SECTOR 17` unless explicitly designed later.

## Gameplay safety locks
Preserve all of these:
- first guaranteed Mutation remains the 10th eligible natural normal-monster defeat;
- Mutation exclusions remain bosses, story monsters, summons, mutation minions, Pelipper capture-protected monsters, and test-harness monsters;
- Pelipper capture ceasefire / target safety remains intact;
- five-PEOPLE total party ceiling remains intact;
- story roster ceiling remains 4 NPC slots;
- multiplayer formation requirements remain adaptive to online Farmer count and configured people cap;
- no legacy fake-hide writer;
- do not merge `main` until the user explicitly requests it and runtime behavior has been verified.

## Archived checkpoint files
- `tools/materialize_alpha6744.py`
- `tools/materialize_alpha6744_fixups.py`
- `tools/build_alpha6744.py`
- `.github/workflows/team-up-alpha6-7-44-lower-workings-interior-survey.yml`
- `docs/alpha6744/LOWER_WORKINGS_INTERIOR_SURVEY_AUDIT_ALPHA6744.md`
- `docs/alpha6744/SMOKE_TEST_V0_2_ALPHA6_7_44_LOWER_WORKINGS_INTERIOR_SURVEY_VI.txt`
- `docs/alpha6744/BUILD_LOG_ALPHA6744.txt`
- `docs/ALPHA_6_7_44_LOWER_WORKINGS_INTERIOR_SURVEY_HANDOFF.md`

Previous gameplay/reaction handoff:
- `docs/ALPHA_6_7_43_LOWER_WORKINGS_DESCENT_REACTIONS_HANDOFF.md`

## Live test for 6.7.44
Install the 6.7.44 test ZIP and load as host.

Useful commands:
- `teamup_lower_interior status|reset|stage 0-6`
- `teamup_lower_descent status|reset|stage 0-5`
- `teamup_story_reactions reset|status`
- `teamup_entry_protocol status`
- `teamup_surge_high status`
- `teamup_roster_story status`

Critical runtime checks:
- map renders cleanly and collisions do not trap the player;
- legacy-save anchor calibration and fresh-save stored anchor both work;
- ingress and secured return resolve the same recorded MineShaft anchor;
- emergency withdrawal preserves stages 2..4;
- save/reload inside the custom location works;
- host/farmhand shared access is safe;
- reactions 36..41 remain one-shot and stale windows do not replay;
- Pelipper, Mutation and George/Evelyn locks remain intact.

## Next target
### 6.7.45 - Containment Chamber Escalation Encounter
Goal:
- build directly on the dedicated 6.7.44 Lower Workings location;
- add the first major lower-workings encounter / chamber escalation;
- strengthen the link between current Mutations and the old containment event;
- preserve the secured retreat route and host authority;
- bundle immediate NPC reactions;
- George remains anonymous and unrevealed until 6.7.46;
- no final boss yet.

Do not treat 6.7.44 as stable until live map/warp/save behavior is verified. Do not merge `main` without explicit user instruction.

## New-chat instruction
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-lower-workings-interior-survey. Bắt đầu 6.7.45.`
