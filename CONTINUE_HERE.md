# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.5.3**

Status: **compile/package/source verified; in-game smoke pending**.

Resume branch:

`v0.2-alpha6-5-3-surge-validation-harness-handoff`

Read this handoff first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_5_3_2026-09-04.md`

Alpha 6.5.3 adds the Surge validation harness on top of the Alpha 6.5.2 safe runtime foundation:

- `teamup_test surge status`
- `teamup_test surge reapply`
- `teamup_test surge clear`
- `teamup_test surge board`
- last `[SurgeTelemetry]` snapshot retained for instant inspection
- reapply clears only Team Up-owned Surge extras before one fresh apply, preventing debug accumulation
- clear never broadens to arbitrary source/custom monsters

Important: Alpha 6.5.3 does NOT change Surge balance or broaden custom monster cloning. Safe placement, Cardcha sandbox exclusion, loot suppression, MiMi/Sudoku/Origin, 51 expansion NPCs, equipment/controller/Vault, and combat anti-spin regressions remain locked.

Important: current Stardew 1.6 safe placement contract remains `isTileOnMap + isTilePassable + IsTileBlockedBy`. Do not restore `isTileLocationTotallyClearAndPlaceable`.

Important: CI is green, but live in-game smoke is still required before declaring The Surge runtime fully verified. For current implementation truth, trust this file + the latest handoff + materialized source + current smoke checklist, not older README roadmap text.
