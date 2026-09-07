# Team Up v0.2.0-alpha.6.3.2 Handoff

Date: 2026-09-03
Branch: `v0.2-alpha6-3-2-signature-icons`
Baseline: Alpha 6.3.1 Equipment RPG Polish

## Status

COMPILE-VERIFIED PASS via GitHub Actions.

Workflow: `Team Up v0.2.0-alpha.6.3.2 Signature Icons`
Run: `33733852186`
Build-trigger SHA: `b5df4ce96c654995f031606495e2b54c0ee3cdc7`
Materialized source SHA: `1f1cbdd` (full SHA available from branch history)

Build result:
- Build succeeded
- 0 Warning(s)
- 0 Error(s)
- Time elapsed 00:00:04.17

Inner mod ZIP:
`release/TeamUp_v0.2.0-alpha.6.3.2_SIGNATURE_ICON_ART_PASS_TEST.zip`

Inner mod ZIP SHA-256:
`02a593e3cba30eef7fe087658586dbee68b3ddf623af49abbf7b9b0244fc381b`

Workflow artifact:
- ID: `9884938787`
- Name: `team-up-alpha6-3-2-signature-icons`
- Artifact digest: `sha256:196cc7ef0e9ee526c30c95683c83bb7d3eacd2ffb8654194864d1bf8841b99ea`

## Alpha 6.3.2 scope

### One icon only
Character Profile now reserves the character icon for the Signature skill only.
- Passive remains text-only.
- Signature has one icon.
- This avoids two competing trait icons in the same dossier.

### Bespoke signature icons
Hand-authored runtime 8x8 silhouettes were added for all currently completed signature kits:

Vanilla locked prototypes:
- Abigail
- Alex
- Harvey
- Maru
- Emily

SVE Wave 1:
- Alesia
- Andy
- Camilla
- Claire
- Isaac
- Jadu
- Lance
- Martin
- Morgan
- Olivia
- Sophia
- Victor

RSV Wave 1:
- Aguar
- Blair
- Carmen
- Daia
- Ian
- Jio
- June
- Kenneth
- Kiarra
- Maddie
- Shiro
- Ysabelle

Total bespoke icons: 29.

Icons are original runtime pixel patterns drawn with `Game1.staminaRect`. No vanilla/SVE/RSV PNG assets are copied.

### Safe fallback
NPCs without a completed bespoke signature kit continue to use the deterministic procedural signature renderer. This allows future Wave 2 NPCs to remain safe before their art pass.

### Files
- `src/TeamUp/UI/TraitIconRenderer.cs`
- `src/TeamUp/UI/CharacterProfileMenu.cs`
- `src/TeamUp/TeamUp.csproj`
- `_build_support/IntegrateAlpha632SignatureIcons.ps1`
- `BuildV0_2Alpha632.ps1`
- `BUILD_V0_2_ALPHA6.bat`
- `SMOKE_TEST_V0_2_ALPHA6_3_2_SIGNATURE_ICONS_VI.txt`
- `.github/workflows/team-up-alpha6-3-2-signature-icons.yml`

### Regression locks
Alpha 6.3.1 equipment RPG polish remains materialized:
- rarity frames
- role score
- combat impact
- signature cooldown preview
- Auto Equip

Earlier architecture locks remain unchanged:
- UniqueID `Ronvotri.TeamUp`
- ChaCha never enters Main Party
- no global monster AI retarget claim
- SVE/RSV optional compatibility
- no new save fields in this icon pass

## Next design direction

Do not add a second passive icon again unless the design is explicitly reopened.

Planned character design standard:
- class/role defines job
- each NPC may have different buffs, debuffs, AI behavior and Signature identity
- balance through class power budget rather than identical skills
- friendship and spouse Bond/Soulmate systems are planned as a separate gameplay pass, not part of 6.3.2

Suggested next development milestone after in-game smoke testing:
`Alpha 6.4.0` character identity / expansion skill continuation, or a contained relationship Bond foundation if prioritized.
