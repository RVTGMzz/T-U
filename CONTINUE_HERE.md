# Continue Team Up Here

Current verified checkpoint: **Team Up v0.2.0-alpha.6.6.0**

Status: **compile/package/direct-builder verified; in-game smoke pending**.

Resume branch:

`v0.2-alpha6-6-party-strategy-foundation-handoff`

Read this handoff first:

`handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_6_0_2026-09-05.md`

Alpha 6.6.0 starts the Party Strategy foundation on top of the verified Alpha 6.5.3 Surge harness.

New party-wide strategies:

- `Balanced`
- `Defensive`
- `Aggressive`
- `HoldPosition`
- `BossFocus`

Test command:

`teamup_strategy <status|balanced|defensive|aggressive|hold|boss>`

The selected strategy is stored in Team Up config, not PartySaveData. Switching strategy clears combat runtime locks for clean retargeting.

Important: Alpha 6.5.3 Surge status/reapply/clear/board, Alpha 6.5.2 safe placement, Cardcha sandbox exclusion, MiMi/Sudoku/Origin, 51 expansion NPCs, equipment/controller/Vault, hard leash, target lock and anti-spin behavior remain regression-locked.

Important: Alpha 6.6.0 is CI verified but still needs in-game strategy smoke before being declared live verified. For current implementation truth, trust this file + the latest handoff + verified source + current smoke checklist, not older README roadmap text.
