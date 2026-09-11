# Team Up Alpha 6.7.44.1 - Lower Workings TMX CSV Hotfix Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.44.1`
- Branch: `v0.2-alpha6-7-44-1-tmx-csv-hotfix`
- Base 6.7.44 handoff commit: `86890c8df8a41e556f980cf39485fbb8c138ed6a`
- Hotfix materializer commit: `f79952c12e982a9f9d529ddc86cfa23a69006cc6`
- Hotfix builder commit: `b18dd61436fe58df2f11083b21c1ab78b98862e6`
- Workflow / CI input: `d1dc98593a4e95a329a732bcd8c55bdde3fc65a3`
- CI-verified materialized source: `e4b00174a0dc058c3ac997f321ba8eec60094f68`
- Successful CI run: `34623512699`
- Successful CI job: `103342934534`
- Artifact ID: `10272739543`
- Artifact name: `team-up-alpha6-7-44-1-tmx-csv-hotfix`
- Artifact wrapper SHA256: `195e1c5ee178c1d50b8a8ea7f7c21bc3bf510ebccdf6a709fdd3ffd8410e5b26`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.1_LOWER_WORKINGS_TMX_CSV_HOTFIX_TEST.zip`
- Inner ZIP SHA256: `b7371fb9d8f681708e1b6274c203e905d2ba2526285211b9f99a464d6156ea0c`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared; live runtime test is still required

## Reported runtime failure
6.7.44 failed while Stardew tried to create `Ronvotri.TeamUp_LowerWorkings`.

SMAPI/xTile/TMXTile stack ended in:
- `InvalidOperationException: There is an error in XML document (40, 5)`
- `FormatException: Input string was not in a correct format`
- `System.UInt32.Parse`
- `TMXTile.TMXData.decode`

## Root cause
The 6.7.44 Python TMX generator wrote each visual map row as a comma-separated row, but joined visual rows with a bare newline:

`rowA\nrowB`

TMXTile parses CSV by commas. Therefore the final tile ID of row A and the first tile ID of row B were presented as one invalid numeric token, e.g.:

`151\n151`

XML itself was structurally valid, so the previous static audit did not catch this parser-specific CSV rule.

## Fix
6.7.44.1 changes every tile-layer row boundary to:

`rowA,\nrowB`

No tile coordinates, layout, story state, survey logic, dialogue, reaction windows, ingress/egress logic, Mutation rules, Pelipper safety, roster caps, or lore locks were changed.

Version was bumped to `0.2.0-alpha.6.7.44.1` so the fixed ZIP cannot be confused with the broken 6.7.44 test artifact.

## New regression coverage
The 6.7.44.1 builder now verifies the exact failure mode before packaging:
- TMX XML parses successfully.
- `Back`, `Buildings`, and `Front` are all present.
- each `32x24` layer contains exactly `768` comma-separated tile IDs.
- every tile ID parses independently as an integer within UInt32 range.
- merged row-boundary patterns such as `151\n151` are rejected.
- the staged TMX is parsed again.
- the TMX is read back from the final release ZIP and parsed again.
- package contents are verified.
- C# build remains `0 Warning(s)`, `0 Error(s)`.

CI acceptance:
- TMX XML PARSE: PASS
- TMX CSV TOKEN COUNT: PASS (768 IDs per layer)
- TMX CSV UINT32 PARSE: PASS
- TMX ROW-BOUNDARY COMMA REGRESSION: PASS
- 6.7.44 STORY / INGRESS / EGRESS CARRY-FORWARD: PASS
- EN/VI PARITY CARRY-FORWARD: PASS
- C# BUILD: PASS
- PACKAGED TMX RE-PARSE: PASS
- ZIP CONTENT + TMX RE-PARSE: PASS

## Carry-forward from 6.7.44
Everything in `docs/ALPHA_6_7_44_LOWER_WORKINGS_INTERIOR_SURVEY_HANDOFF.md` remains authoritative except the broken 6.7.44 artifact itself.

Important unchanged facts:
- dedicated location: `Ronvotri.TeamUp_LowerWorkings`;
- map: `assets/LowerWorkings.tmx`, 32x24;
- dynamic recorded-breach ingress/egress;
- interior survey stage `0..6`;
- three 120-tick survey zones;
- early emergency withdrawal preserves survey progress;
- reaction catalog remains windows `0..41`, 584 lines/language;
- George remains unrevealed Rank D / Non-Combatant / unrecruitable;
- Evelyn secret remains postgame only;
- first guaranteed Mutation remains the 10th eligible natural normal-monster defeat;
- Pelipper capture safety remains intact;
- hard cap remains five PEOPLE total and story NPC slots remain 4/4;
- no boss or containment encounter yet.

## Live test now
Delete/replace the old 6.7.44 Team Up folder with the 6.7.44.1 ZIP. Do not keep both versions installed.

First required check:
1. Launch the save.
2. Confirm the previous `Couldn't create the 'Ronvotri.TeamUp_LowerWorkings' location` error is gone.
3. Enter the dedicated Lower Workings route and verify the map actually renders.
4. Then continue the original 6.7.44 smoke test for collision, ingress/egress, save/reload, multiplayer, three survey zones, and reactions.

If a new map error appears, capture the full SMAPI stack and do not advance to 6.7.45 yet.

## Next target after live verification
`6.7.45 - Containment Chamber Escalation Encounter`.

Do not merge `main` until explicitly requested.

## New-chat instruction
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-1-tmx-csv-hotfix. Xác nhận live test 6.7.44.1 rồi mới bắt đầu 6.7.45.`
