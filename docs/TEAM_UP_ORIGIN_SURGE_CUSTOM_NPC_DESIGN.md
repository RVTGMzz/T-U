# Team Up Origin, The Surge, and Custom NPC Integration

Status: DESIGN LOCK / PRE-BUILD ROADMAP
Baseline: Team Up v0.2.0-alpha.6.4.6
Updated: 2026-09-04

## 1. Narrative spine

Team Up should have a light origin story, not a heavy questline.

Core chain:

**The Surge -> people are forced to fight together -> Awakening appears under pressure -> Team Up becomes the practical response.**

The story exists to explain gameplay systems, not to replace Stardew Valley with a main-quest RPG.

### The Surge

Monster populations begin rising and creatures appear outside the patterns Marlon expects. The Valley is becoming more dangerous, and solo combat is no longer the sensible default.

The source of The Surge is intentionally unresolved at first. No single lore NPC knows everything.

### Awakening

Awakening is not a class assignment and not a magical gift handed out by Farmer.

Combat pressure reveals abilities that reflect who each NPC already is:

- Primary Role = strongest combat instinct.
- Secondary Role = secondary side of their personality/combat identity.
- Affinity = natural leaning, not a forced class restriction.
- Signature = the unique way that NPC's Awakening manifests.
- Friendship/Bond/Soulmate = better synchronization, not a new source of raw power.
- Equipment = build customization and focus, not the origin of Awakening.

Design rule: power should feel like something that character *would* awaken, not a generic RPG class pasted onto them.

## 2. NPCs used by the origin story

### Linus

Role in story: first observer.

He notices that wildlife and the Valley's normal rhythm have changed. He hints that creatures are moving in unusual ways, but he does not explain the combat system.

### Marlon

Role in story: combat mentor and Team Up system guide.

Marlon recognizes that monster activity is abnormal and that some people display unusual combat instincts under pressure. He does not know the ultimate supernatural cause.

Marlon should be the main Team Up mentor because Adventurer's Guild is a natural home for:

- Role explanations;
- Affinity explanations;
- combat tutorials;
- Threat / Surge status;
- optional future training access.

### Gil

Optional flavor only. Short dry comments are welcome, but Gil should not become a system narrator.

### Wizard

Do **not** use Wizard as the primary explanation NPC. He is already involved in too many Stardew/mod story events. Team Up origin should stand on Linus + Marlon and remain independent from Wizard-heavy lore.

## 3. Light story structure

Keep the origin to roughly four short events.

### Act I: Something Is Moving

Linus notices wildlife and monster movement changing.

### Act II: The Surge

Marlon confirms monster activity is increasing beyond normal patterns. Adventurer's Guild begins treating the situation as a real threat.

### Act III: The First Awakening

During a dangerous fight, a valid recruit uses a Signature/Awakening ability for the first time. The NPC should react as if they did not know they could do it.

### Act IV: Stronger Together

Marlon concludes that fighting in groups is now the practical answer. Farmer is not presented as the person granting powers, but may act as a catalyst for people pushing beyond their normal limits.

Team Up systems unlock / are formally introduced here.

Suggested theme line:

> The Valley did not give them a new identity. Danger only revealed what had already been there.

## 4. Monster Surge gameplay direction

### Goal

Installing Team Up should make combat zones feel meaningfully more dangerous so a party has a reason to exist.

Target default density: **up to approximately x2 normal monster population** in eligible combat areas.

This must NOT be implemented as `clone every monster entity`.

### Safe spawn-budget model

Team Up should increase a location's eligible monster spawn budget instead of blindly duplicating existing monsters.

Do not duplicate or interfere with:

- bosses;
- story encounters;
- quest monsters;
- named/unique monsters;
- summons;
- scripted event entities;
- Cardcha test dummies / test targets;
- other explicitly protected entities.

### Configuration direction

Planned config surface:

```json
{
  "EnableMonsterSurge": true,
  "MonsterDensityMultiplier": 2.0
}
```

Exact economy/reward tuning remains a balance-test item. Density is the priority; loot inflation must be measured before release.

### Combat-zone scope

The Surge should affect eligible combat locations, not ordinary social/town maps by default.

Leaving a Surge combat area must not leak spawned monsters into unrelated locations or save-state scripts.

## 5. Compatibility policy for monster mods

### Vanilla

Full support. Use Team Up's safe spawn-budget rules.

### Known compatible monster/ecosystem mods

Use explicit adapters where needed.

Pokémon/Pelipper-related encounters are a planned compatibility target. Only normal/repeatable/wild encounters may be density-scaled. Story, boss, named, quest, and unique Pokémon must be excluded.

### Unknown monster mods

Do not blindly clone third-party monster objects.

Fallback behavior should be conservative: either add known-safe Team Up/vanilla-compatible encounters to eligible combat zones, or leave the third-party encounter untouched if its safety cannot be determined.

Compatibility is more important than forcing x2 on every unknown scripted entity.

## 6. Adventurer's Guild Threat presentation

Optional lightweight worldbuilding:

- LOW
- ELEVATED
- HIGH
- SURGE

This can later become a board/status line in Adventurer's Guild and may map to spawn density. It is not required for the first implementation.

## 7. Custom Ronvotri NPCs as Team Up recruits

### MiMi

Canonical source identity already known:

- NPC: **MiMi**
- Source UniqueID: `Ronvotri.Cardcha_MiMi`
- Source mod: Cardcha

Decision: **MiMi should be a full Team Up combat recruit**, not a Special/Farmer Companion like ChaCha.

Rules:

- Team Up must respect MiMi's source-story availability and must not bypass her Cardcha progression/gates.
- Once MiMi is legitimately available as a social NPC, she may join Main Party.
- She should receive a real Primary Role, Secondary Role, five Affinities, Engagement style, unique Passive text, unique Signature, Tier 2/Tier 3 behavior, and one bespoke Signature icon.
- Her final role and Signature should be designed from MiMi's established personality/lore rather than assigned generically.

Important distinction:

- `Ronvotri.Cardcha_ChaCha` remains Special/Farmer Companion and **never Main Party**.
- MiMi is a normal character recruit path once her own story allows it.

### Sudoku

Canonical source identity already known:

- NPC: **Sudoku**
- Source mod: **Hey! You're Cursed!**
- Known source identifier/assets: `ronvotri.HeyYoureCursed_Sudoku`
- Sudoku is implemented by her source mod as a roommate with her own trust/stage/puzzle progression.

Decision: **Sudoku should also be a full Team Up combat recruit** when her source mod has legitimately made her available.

Rules:

- Team Up must not bypass Sudoku's source progression, materialization state, roommate lifecycle, or story gates.
- Recruitment eligibility should be driven by her valid runtime/source state, not merely by detecting that the mod is installed.
- She should receive the same full Team Up identity standard as other completed recruits: Primary/Secondary Role, five Affinities, Engagement, unique Passive/Signature, Tier 2/Tier 3, and one bespoke Signature icon.
- Final combat identity must be based on Sudoku's actual character/lore rather than a generic filler profile.

## 8. Custom NPC integration architecture

MiMi and Sudoku should use explicit optional-source adapters/profiles rather than broad hardcoded behavior that could affect unrelated mod NPCs.

Requirements:

- Team Up remains standalone if Cardcha or Hey! You're Cursed! is absent.
- Missing source mods must never produce errors.
- No source mod save data should be rewritten by Team Up unless an explicit supported API/contract exists.
- Team Up should read enough runtime state to respect story availability, then own only Team Up combat state.
- Do not classify MiMi/Sudoku as generic fallback recruits if their explicit profile exists.

## 9. Balance locks carried forward

All new custom NPCs and future story unlocks must obey existing Team Up balance philosophy:

- 70% Primary / 30% Secondary identity.
- No all-in-one maxed skill.
- Multiple effects require weaker individual components.
- Friendship/marriage must not create mandatory meta choices.
- One NPC = one Signature icon; Passive remains text-only.
- Completed characters should have distinct gameplay fingerprints.
- A character's strength must come with a meaningful weakness/tradeoff.

## 10. Proposed future build sequence

Not yet implemented by this document.

1. Origin Story framework: Linus -> Marlon -> First Awakening -> Team Up introduction.
2. Monster Surge spawn-budget foundation with safe vanilla x2 target.
3. Known-mod adapters, beginning with Pokémon/Pelipper ecosystem compatibility where practical.
4. MiMi explicit recruit profile + Signature/icon.
5. Sudoku explicit recruit profile + Signature/icon.
6. Threat presentation / Adventurer's Guild polish.
7. Balance/economy test pass before Beta.

This document records design decisions only. Runtime implementation requires a separately compile-verified build.
