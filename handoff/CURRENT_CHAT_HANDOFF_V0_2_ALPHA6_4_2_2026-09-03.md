# Team Up v0.2.0-alpha.6.4.2 handoff

Date: 2026-09-03
Branch: `v0.2-alpha6-4-2-ui-readability`
Status: **COMPILE-VERIFIED PASS**

## Verified build

GitHub Actions workflow: `Team Up v0.2.0-alpha.6.4.2 UI Readability`
Run: `33763026989`
Build-trigger SHA: `3d3a2cd7d37ccd40dbcf00a988c77f0af6d81ce5`
Materialized source SHA: `dca08decb04caa2051c5f4c28166313e9e3849bf`

CI result:
- Build Alpha 6.4.2 end-to-end: PASS
- Source acceptance: PASS
- Package verification: PASS
- Materialize generated source: PASS
- Artifact upload: PASS

`dotnet build`:
- Build succeeded.
- 0 Warning(s)
- 0 Error(s)
- Time Elapsed 00:00:07.09

Package:
`release/TeamUp_v0.2.0-alpha.6.4.2_UI_READABILITY_TEST.zip`

Inner mod ZIP SHA-256:
`1a9618ec52983ccb3b6f20f1dcab3ad3e15e48bab99d41a2801521dc63a5ee64`

Workflow artifact:
- ID: `9896308916`
- Name: `team-up-alpha6-4-2-ui-readability`
- Size: 166233 bytes
- Artifact digest: `sha256:9a655e620921ae90a43aa46cd61af90d42ea448c2e017620b0f4a025261d4789`

Expected SMAPI marker:
`Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.4.2`

## Alpha 6.4.2 scope

This is a contained Character Profile readability pass based directly on Alpha 6.4.1.

### Description text x2

`CharacterProfileMenu` now locks:
`DescriptionScale = BodyScale * 2f`

Only the descriptive body text for:
- `NỘI TẠI / PASSIVE`
- `KỸ NĂNG ĐẶC TRƯNG / SIGNATURE`

is doubled.

The following stay at the previous size so the layout does not inflate everywhere:
- profile headers
- role labels
- affinity rows
- source
- engagement
- relationship summary
- footer buttons

### Short-viewport scrolling

Because x2 description text can exceed the right panel at resolutions around the user's test screenshot (~1008x646), the Passive + Signature area is now a dedicated vertically scrollable region.

Inputs:
- mouse wheel
- keyboard Up / Down
- keyboard PageUp / PageDown
- gamepad D-pad Up / Down
- gamepad Left Stick Up / Down

A small scrollbar appears only when content is taller than the available region.

Rendering uses line-level vertical clipping guards so description text does not paint over the panel border/footer.

### Signature layout

Alpha 6.3.2 one-icon rule stays locked:
- Passive remains text-only
- Signature keeps exactly one bespoke/runtime icon

The Signature header/icon is separated from its enlarged body text so the x2 description can use the full right-panel width instead of wrapping in a narrow column beside the icon.

## Inherited locks retained

Alpha 6.4.1:
- 4 hearts Trusted progression benefit
- 8 hearts Close Companion role-aware AI timing
- 10 hearts Signature Affinity
- spouse Bond
- 14-heart Soulmate / expansion role fallback

Alpha 6.4.0:
- vanilla Character Skill Identity
- 70% Primary / 30% Secondary balance budget
- bounded temporary buffs

Alpha 6.3.x:
- one Signature icon per completed NPC kit
- rarity frame
- Role Score
- Combat Impact
- Auto Equip

Core:
- ChaCha never Main Party
- Alex/Tank approaches before TAUNT
- Party Vault layout fix
- Farmer pass-through Team Up NPCs
- SVE/RSV Wave 1 signatures retained

## Test checklist

Use:
`SMOKE_TEST_V0_2_ALPHA6_4_2_UI_READABILITY_VI.txt`

First test the exact screen that motivated this pass:
1. Open Emily Character Profile at the same/similar resolution as the screenshot.
2. Confirm Positive Energy description is approximately 2x the Alpha 6.4.1 text size.
3. Confirm Prismatic Aura description is approximately 2x the Alpha 6.4.1 text size.
4. Confirm affinity/title/relationship text did not double.
5. Scroll to the final Signature line and verify no footer/panel overlap.
6. Repeat on Sebastian, Caroline, Wizard or another NPC with different description lengths.

If anything clips, send a full Character Profile screenshot plus game resolution.
