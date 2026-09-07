# Team Up! Chat Handoff — v0.2.0-alpha.6.5.1

Date: 2026-09-04
Status: COMPILE VERIFIED / PACKAGE VERIFIED / SOURCE MATERIALIZED

## Resume from here

Development branch:

`v0.2-alpha6-5-1-mimi-recruit-gate-hardening`

Materialized Alpha 6.5.1 source commit:

`cbfb439895577b22d71744740262c54f4722995a`

Baseline inherited from:

`v0.2-alpha6-5-origin-surge-custom-recruits-handoff`

Alpha 6.5.0 baseline commit:

`2f18ca3b8eb7cc9f107fa4981c63191be1d6079d`

## Build verification

GitHub Actions run:

`33856026384`

Result: SUCCESS

Verified steps:

- SMAPI build environment: PASS
- Build Alpha 6.5.1 end-to-end: PASS
- Source acceptance: PASS
- Packaged build verification: PASS
- Generated source materialization: PASS
- Artifact upload: PASS

Compiler result:

- 0 warnings
- 0 errors

Package:

`TeamUp_v0.2.0-alpha.6.5.1_MIMI_RECRUIT_GATE_HARDENING_TEST.zip`

Package SHA256:

`e1e96ff94ba4db53949dc2a1e559566af995de03a280ad5996737838b4ef2ffb`

GitHub Actions artifact:

- Name: `team-up-alpha6-5-1-mimi-recruit-gate-hardening`
- Artifact ID: `9930218497`
- Uploaded artifact digest: `sha256:34eacb0cac31d2b2ca54df937726c2b987f44a7037b91ff3f3a82d952835cb38`

## Why Alpha 6.5.1 exists

Alpha 6.5.0 introduced explicit MiMi support, but its first recruit gate inferred post-story availability mainly from visible name/location state.

Alpha 6.5.1 hardens that contract against Cardcha's real runtime implementation.

Cardcha source was checked directly on:

`ronvotri/Cardcha-Shardbound`

Relevant Cardcha handoff branch:

`cardcha-alpha28-0642-airship-interior-stardew-rework-handoff`

Confirmed Cardcha runtime facts:

- canonical NPC.Name is `Ronvotri.Cardcha_MiMi`;
- mystery displayName is `???`;
- known identity displayName is `MiMi`;
- Cardcha UniqueID is `Ronvotri.Cardcha`;
- Cardcha promotes MiMi into its social/friendship layer only after the Wizard meetup is complete;
- that promotion creates the canonical `friendshipData` entry for `Ronvotri.Cardcha_MiMi`.

## Alpha 6.5.1 MiMi recruit contract

Team Up now requires all of the following before MiMi can be recruited:

1. Cardcha is loaded.
2. NPC.Name is the canonical `Ronvotri.Cardcha_MiMi`.
3. NPC is visible and has a current location.
4. displayName is `MiMi`, not `???`.
5. `Game1.player.friendshipData` contains `Ronvotri.Cardcha_MiMi`.
6. no event, dialogue, or active clickable menu currently owns the presentation.
7. the promoted actor is talkable / a villager.

This is deliberately fail-closed before Cardcha's social unlock.

## Important architecture lock

Do NOT read or rewrite Cardcha private SaveData from Team Up.

Do NOT reflect into Cardcha internal services.

Do NOT hardcode Cardcha's merchant weekday/time/location schedule as the long-term recruitment contract.

After Cardcha promotes MiMi into a legitimate social NPC, Cardcha remains the source of truth for where/when MiMi exists. Team Up only checks the live social actor state.

This keeps compatibility working if Cardcha later changes MiMi's home, merchant schedule, social routine, or restored-community-center route.

## Regression locks retained from Alpha 6.5.0

The following must remain intact:

- Origin story: Linus -> Marlon -> First Awakening -> Stronger Together.
- The Surge x2 target foundation with bounded extra cap.
- Cardcha test arena exclusion.
- default Surge bonus loot suppression.
- MiMi Support / Control identity and BROOMTAIL SIGIL.
- Sudoku Control / Damage identity and NINEFOLD SEAL.
- ChaCha remains Special/Farmer Companion and never Main Party.
- 51 SVE/RSV expansion NPC completed identities/icons/balance.
- equipment double-click behavior.
- controller focus behavior.
- Party Vault drag/drop.
- combat target-lock / anti-spin fixes.

## Build entry points

One-click Windows launcher:

`BUILD_V0_2_ALPHA6.bat`

Direct PowerShell build:

`BuildV0_2Alpha651.ps1`

Smoke checklist:

`SMOKE_TEST_V0_2_ALPHA6_5_1_MIMI_RECRUIT_GATE_VI.txt`

Workflow:

`.github/workflows/team-up-alpha6-5-1-mimi-recruit-gate-hardening.yml`

## Recommended next build

Suggested next checkpoint: Alpha 6.5.2.

Best next target is Surge/runtime polish rather than adding a broad unknown-mod adapter prematurely:

- validate safer Surge spawn placement around blocked tiles / walls;
- add lightweight threat/surge presentation at Adventurer's Guild;
- add useful debug telemetry for original monster count, requested extra budget, actual spawned count, and suppression reason;
- keep unknown custom monsters fail-safe;
- only add Pokémon/Pelipper species-specific density support after a stable source API/runtime contract is identified.

Do not regress the Alpha 6.5.1 MiMi social-unlock gate while doing this.
