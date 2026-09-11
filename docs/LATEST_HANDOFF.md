# Team Up - Canonical Latest Handoff

This file is the canonical pointer for continuing Team Up in a new chat. Read this file first, then read the checkpoint-specific handoff if more detail is needed.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.1`
- Branch: `v0.2-alpha6-7-44-1-tmx-csv-hotfix`
- Base 6.7.44 branch handoff commit: `86890c8df8a41e556f980cf39485fbb8c138ed6a`
- Hotfix workflow / CI input: `d1dc98593a4e95a329a732bcd8c55bdde3fc65a3`
- CI-verified hotfix source: `e4b00174a0dc058c3ac997f321ba8eec60094f68`
- Successful CI run: `34623512699`
- Successful CI job: `103342934534`
- Artifact ID: `10272739543`
- Artifact name: `team-up-alpha6-7-44-1-tmx-csv-hotfix`
- Artifact wrapper SHA256: `195e1c5ee178c1d50b8a8ea7f7c21bc3bf510ebccdf6a709fdd3ffd8410e5b26`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.1_LOWER_WORKINGS_TMX_CSV_HOTFIX_TEST.zip`
- Inner ZIP SHA256: `b7371fb9d8f681708e1b6274c203e905d2ba2526285211b9f99a464d6156ea0c`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared; live runtime verification still required

## Why 6.7.44.1 exists
The original 6.7.44 test build failed while Stardew created `Ronvotri.TeamUp_LowerWorkings`.

Runtime stack:
- `InvalidOperationException: There is an error in XML document (40, 5)`
- `FormatException`
- `System.UInt32.Parse`
- `TMXTile.TMXData.decode`

Root cause: the 6.7.44 generated TMX joined visual CSV rows with a bare newline. TMXTile splits CSV on commas, so a row boundary produced an invalid numeric token such as `151\n151`.

6.7.44.1 changes row boundaries to comma + newline. The map layout and all gameplay/story behavior are unchanged.

## Hotfix regression coverage
The new CI explicitly verifies:
- TMX XML parses successfully;
- map remains `32x24`;
- `Back`, `Buildings`, and `Front` each contain exactly `768` comma-separated tile IDs;
- every tile ID parses independently within UInt32 range;
- merged row-boundary tokens such as `151\n151` are rejected;
- staged TMX re-parses successfully;
- the TMX read back from the final release ZIP re-parses successfully;
- ZIP contents are complete;
- C# build remains `0 Warning(s)`, `0 Error(s)`.

CI results:
- TMX XML PARSE: PASS
- TMX CSV TOKEN COUNT: PASS (768 IDs per layer)
- TMX CSV UINT32 PARSE: PASS
- TMX ROW-BOUNDARY COMMA REGRESSION: PASS
- 6.7.44 STORY / INGRESS / EGRESS CARRY-FORWARD: PASS
- EN/VI PARITY CARRY-FORWARD: PASS
- C# BUILD: PASS
- PACKAGED TMX RE-PARSE: PASS
- ZIP CONTENT + TMX RE-PARSE: PASS

## 6.7.44 gameplay carried forward unchanged
Alpha 6.7.44 remains the first actual dedicated Lower Workings map/chamber checkpoint.

Location:
- `Ronvotri.TeamUp_LowerWorkings`
- Stardew 1.6 `Data/Locations` + vanilla `StardewValley.GameLocation`
- `assets/LowerWorkings.tmx`, `32x24`, using vanilla `Mines/mine.png`
- dynamic return path; no static TMX Warp

Persistent state:
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyStage`
- `Ronvotri.TeamUp/Story/LowerWorkingsBreachTile`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorEntered`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyComplete`
- `Ronvotri.TeamUp/Story/LowerWorkingsSafeReturnUsed`
- `Ronvotri.TeamUp/Story/LowerWorkingsInteriorSurveyReported`

Route `0..6`:
1. Guild authorizes dedicated survey after first descent is complete.
2. Full adaptive formation returns to recorded breach; legacy 6.7.43 saves may calibrate the tile anchor once.
3. Enter the real Lower Workings location.
4. Hold full formation for 120 ticks near directed cribbing `(8,9)`.
5. Hold 120 ticks near newer Mutation-linked residue over old blast scoring `(22,8)`.
6. Hold 120 ticks near deeper sealed-pressure edge `(23,16)`.
7. Return to entry `(15,21)`, use secured withdrawal to the exact recorded breach anchor, then report at Guild.

Early withdrawal preserves survey stage. Host owns story progression; farmhands may use the shared route after host activation without writing host story flags.

## Bundled reactions
Reaction catalog remains windows `0..41`, exact EN/VI parity, `584` reaction lines per language.

6.7.44 windows remain:
- `36`: dedicated interior survey authorized
- `37`: real Lower Workings location entered
- `38`: directed cribbing surveyed
- `39`: Mutation-linked residue surveyed
- `40`: deeper sealed-pressure edge surveyed
- `41`: interior survey reported complete

Supported NPCs remain Abigail, Alex, Clint, Demetrius, Evelyn, George, Gus, Lewis, Linus, Marlon, Maru, Pierre, Robin, Wizard.

## Story truth / lore locks
- SURGE HIGH confirmed.
- Story NPC slots are 4/4.
- Hard formation cap remains 5 PEOPLE total including Farmers.
- Entry Protocol is READY.
- First descent is complete.
- Present Mutation/Surge activity is interacting with an older deliberately sealed system.
- Historical worker identity remains unknown to the player.
- Source/entity beyond the deeper seal remains unidentified.
- No boss or containment encounter has occurred yet.

George before reveal remains observed Rank D, Non-Combatant, unrecruitable, with no `Rank S`, no `The Last Blaster`, and no explicit identification as the historical miner. Reveal remains planned for 6.7.46.

Evelyn remains ordinary low Rank D healer/support during main story; her secret remains postgame only.

Do NOT introduce exact `SECTOR 17` unless explicitly designed later.

## Gameplay safety locks
Preserve all of these:
- first guaranteed Mutation remains the 10th eligible natural normal-monster defeat;
- Mutation exclusions remain bosses, story monsters, summons, mutation minions, Pelipper capture-protected monsters, and test-harness monsters;
- Pelipper capture ceasefire / target safety remains intact;
- five-PEOPLE total party ceiling remains intact;
- story roster ceiling remains 4 NPC slots;
- multiplayer formation requirements remain adaptive;
- no legacy fake-hide writer;
- do not merge `main` until explicitly requested and runtime behavior has been verified.

## Archived checkpoint files
6.7.44:
- `docs/ALPHA_6_7_44_LOWER_WORKINGS_INTERIOR_SURVEY_HANDOFF.md`
- `docs/alpha6744/*`

6.7.44.1 hotfix:
- `tools/hotfix_alpha6744_1_tmx_csv.py`
- `tools/build_alpha6744_1.py`
- `.github/workflows/team-up-alpha6-7-44-1-tmx-csv-hotfix.yml`
- `docs/alpha6744_1/LOWER_WORKINGS_TMX_CSV_HOTFIX_AUDIT_ALPHA6744_1.md`
- `docs/alpha6744_1/BUILD_LOG_ALPHA6744_1.txt`
- `docs/ALPHA_6_7_44_1_TMX_CSV_HOTFIX_HANDOFF.md`

## Live test now
Install **6.7.44.1**, not the original 6.7.44 ZIP. Delete/replace the old Team Up folder; do not keep both versions installed.

First checks:
1. Load the same save that produced the TMX error.
2. Confirm `Couldn't create the 'Ronvotri.TeamUp_LowerWorkings' location` is gone.
3. Enter Lower Workings and confirm the map renders instead of failing during save load.
4. Then continue collision, ingress/egress, emergency withdrawal, save/reload, host/farmhand, three survey-zone, and reaction tests from the 6.7.44 handoff.

Useful commands:
- `teamup_lower_interior status|reset|stage 0-6`
- `teamup_lower_descent status|reset|stage 0-5`
- `teamup_story_reactions reset|status`
- `teamup_entry_protocol status`
- `teamup_surge_high status`
- `teamup_roster_story status`

## Next target
### 6.7.45 - Containment Chamber Escalation Encounter
Only begin after the 6.7.44.1 map/warp/save live test passes or the user explicitly waives it.

Goals remain:
- first major Lower Workings encounter / chamber escalation;
- stronger causal evidence linking current Mutations to the old containment event;
- preserve secured retreat and host authority;
- bundle immediate NPC reactions;
- George remains anonymous/unrevealed until 6.7.46;
- no final boss yet.

## New-chat instruction
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-1-tmx-csv-hotfix. Xác nhận live test 6.7.44.1 rồi mới bắt đầu 6.7.45.`
