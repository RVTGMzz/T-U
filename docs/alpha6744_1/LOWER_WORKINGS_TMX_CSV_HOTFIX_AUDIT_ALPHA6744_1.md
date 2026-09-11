# Team Up Alpha 6.7.44.1 - Lower Workings TMX CSV Hotfix Audit

## Root cause
The original 6.7.44 TMX generator joined each visual CSV row with a newline only. TMXTile splits CSV data on commas, so the final tile ID of one row and the first ID of the next row were read as one invalid token such as `151\n151`, causing `UInt32.Parse` to throw `FormatException`.

## Fix
- Changed the generated TMX row boundary to comma + newline.
- Kept the map dimensions, tile IDs, layers, story logic, ingress/egress, survey route, reactions, and safety locks unchanged.
- Bumped the test version to 0.2.0-alpha.6.7.44.1 so the fixed artifact cannot be confused with the broken 6.7.44 ZIP.

## CI regression coverage
- XML parses successfully.
- Back, Buildings, and Front each contain exactly 768 comma-separated tile IDs.
- Every tile ID parses independently as UInt32.
- Explicitly rejects row-boundary merged-number tokens.
- Re-parses the staged TMX and the TMX read back from the final ZIP.
- C# build remains 0 warnings / 0 errors.
