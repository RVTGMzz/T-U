# Team Up Session Handoff — Alpha 6.5.0

Date: 2026-09-04
Repository: `ronvotri/Team-Up`
Canonical branch: `v0.2-alpha6-5-origin-surge-custom-recruits`

## Canonical verified build

Version: `0.2.0-alpha.6.5.0`

GitHub Actions:
- Workflow: `Team Up v0.2.0-alpha.6.5.0 Origin Surge Custom Recruits`
- Successful run: `33849941486`
- Job: `100950217326`
- Result: SUCCESS
- Compile: `0 Error(s)`, `2 Warning(s)`
- Compile time: `00:00:06.51`

Materialized source commit:
- `2f18ca3b8eb7cc9f107fa4981c63191be1d6079d`

Build input commit:
- `2c2d554da3cbcf4b3b704725e336c62b5bee620a`

Inner mod ZIP:
- `release/TeamUp_v0.2.0-alpha.6.5.0_ORIGIN_SURGE_CUSTOM_RECRUITS_TEST.zip`
- SHA-256: `ad74332147df978cef8fd67e08d07231d626e96117aea01ff58ea36f44282a19`

GitHub Actions artifact:
- Artifact ID: `9927949567`
- Artifact name: `team-up-alpha6-5-origin-surge-custom-recruits`
- Artifact ZIP digest: `sha256:c8bb4636c0a151377a1e3d2e8aaedcfc9b1dde77081e927728669c5e01a6d953`

## Current implementation state

### Alpha 6.4.5 interaction / AI fixes retained

- Equipment supports double-click equip and double-click unequip behavior.
- Controller focus can reach Auto Equip and Unequip controls.
- Party Vault supports real mouse drag/drop in addition to prior interaction paths.
- Combat AI has target lock and facing hold to reduce idle facing jitter / up-down spinning.
- Sound helper calls are guarded against null/blank cue names in Team Up paths.

### Alpha 6.4.6 roster completion retained

- All previously-placeholder SVE / RSV recruits have complete combat identities.
- Expansion roster has real Primary/Secondary roles, affinities, engagement, passive, Signature, Tier 2/Tier 3 behavior.
- Completed expansion roster has bespoke semantic Signature icons.
- Zero-sum Power / Reach / Utility / Tempo tuning remains locked.
- Existing 70% Primary / 30% Secondary identity philosophy remains locked.

### Alpha 6.5.0 origin story

Narrative spine:

`The Surge -> people are forced to fight together -> Awakening appears under pressure -> Team Up becomes the practical response.`

NPC roles:
- Linus is the first observer of unusual changes in wildlife / Valley rhythm.
- Marlon is the main combat mentor and the NPC who recognizes The Surge as an abnormal monster threat.
- Wizard is intentionally NOT the primary Team Up lore explainer.

Story service:
- `src/TeamUp/Story/OriginStoryService.cs`
- Runtime progression stored through Farmer modData instead of changing PartySaveData.
- Four-beat flow: combat anomaly -> Linus -> Marlon/The Surge -> first Awakening -> Team Up.

### Alpha 6.5.0 Monster Surge

Service:
- `src/TeamUp/Combat/MonsterSurgeService.cs`

Config direction now implemented:
- `EnableMonsterSurge = true`
- `MonsterDensityMultiplier = 2.0f`
- `MonsterSurgeExtraCap = 18`
- `SurgeMonstersDropLoot = false` by default

Safety model:
- Target is approximately x2 density in eligible combat zones.
- Does NOT blindly clone arbitrary third-party monsters.
- Does NOT use `Activator.CreateInstance`, `MemberwiseClone`, or reflective custom-monster cloning.
- Cardcha test arena is excluded from Surge population changes.
- Surge extras are separately marked and reward-suppressed by default to avoid immediate x2 economy inflation.

Important limitation:
- Pokémon / Pelipper ecosystem does NOT yet have an explicit species-aware adapter.
- 6.5.0 establishes the safe Surge foundation first.
- Future adapter must distinguish ordinary/wild/repeatable encounters from story, boss, quest, named, unique, summon, or scripted entities.

### MiMi recruit integration

Source:
- Cardcha
- Cardcha mod ID: `Ronvotri.Cardcha`
- NPC ID: `Ronvotri.Cardcha_MiMi`

Decision:
- MiMi is a normal Main Party recruit when her source story legitimately exposes her as MiMi.
- Team Up must not bypass Cardcha mystery/story phases.
- ChaCha remains Special/Farmer Companion and NEVER Main Party.

MiMi combat identity:
- Primary: Support
- Secondary: Control
- Affinities: Tank 1 / Damage 2 / Support 5 / Healer 2 / Control 4
- Recommended engagement: Balanced
- Signature: `BROOMTAIL SIGIL`
- Archetype: Sweep
- Damage intentionally modest, with wider control/tempo utility.
- Bespoke Signature icon is included.

### Sudoku recruit integration

Source:
- Hey! You're Cursed!
- Canonical NPC ID: `ronvotri.HeyYoureCursed_Sudoku`
- Runtime alias `Sudoku` is supported for source builds that expose that actor name.

Decision:
- Sudoku becomes a Main Party recruit only when her source mod actually exposes her as a valid live NPC.
- Team Up must not bypass materialization / roommate / trust / puzzle progression.
- Team Up reads runtime availability and owns only Team Up combat state.

Sudoku combat identity:
- Primary: Control
- Secondary: Damage
- Affinities: Tank 1 / Damage 4 / Support 2 / Healer 1 / Control 5
- Recommended engagement: Cautious
- Signature: `NINEFOLD SEAL`
- Archetype: Control
- Strong disable, deliberately low damage, no party-wide buff, slower cooldown than aggressive DPS signatures.
- Bespoke 3x3-grid style Signature icon is included.

## Current warnings

The successful build has exactly 2 SMAPI analyzer warnings in:
- `src/TeamUp/Core/CustomNpcCompatibilityService.cs`

Reason:
- direct access to `npc.isInvisible` NetBool

Recommended cleanup:
- replace with public `npc.IsInvisible` property if available in the target reference assemblies.

These are warnings only. The verified Alpha 6.5.0 build has 0 compile errors.

## User-facing test priorities for the next session

1. Install Alpha 6.5.0 and test ordinary Mines/combat areas first.
2. Confirm The Surge feels meaningfully denser without becoming chaotic or causing performance spikes.
3. Verify Surge monsters do not leak into non-combat maps or persist unexpectedly.
4. Verify loot/economy behavior is acceptable with Surge bonus loot suppressed.
5. Test MiMi only after Cardcha has legitimately introduced/unlocked her.
6. Test Sudoku only after Hey! You're Cursed! has legitimately materialized/exposed her.
7. Re-test equipment double-click equip/unequip, controller focus, Party Vault drag/drop, and combat facing stability as regression checks.
8. If the prior `[game] Error playing sound` recurs, capture 20–30 SMAPI lines BEFORE the error to identify the actual caller.

## Recommended next implementation step

Highest-value next branch:

`v0.2-alpha6-5-1-surge-compat-polish`

Suggested scope:
- clean the two `IsInvisible` analyzer warnings;
- add a real Pokémon/Pelipper compatibility adapter after inspecting the source mod's runtime entity/encounter contracts;
- add stronger protected-entity rules for known mod ecosystems;
- expose Threat/Surge status more clearly in Adventurer's Guild;
- tune Surge density/economy using live test feedback before adding more large systems.

Do NOT replace the safe spawn-budget foundation with generic entity cloning.

## Design source of truth

See:
- `docs/TEAM_UP_ORIGIN_SURGE_CUSTOM_NPC_DESIGN.md`
- GitHub Issue #2: `Roadmap: Origin Story, The Surge, MiMi & Sudoku recruitment`

The current branch is the source of truth for continuing work in a new chat/session.
