# Team Up! - Wiki / Codex Structure

## Goal

The Team Up! Codex should help players answer three questions:

1. Who should I bring?
2. What role should they play?
3. Which companion or expansion options fit this team?

The Codex is guidance, not a hard class lock.

## 1. Source hierarchy

The Wiki should separate content by source so the player can immediately tell what belongs to base Team Up! and what comes from compatibility support.

### Base Game

Vanilla Stardew Valley NPCs.

This is the canonical balance roster and should always be documented first.

### Expansion Rosters

Optional NPC packs such as:

- Stardew Valley Expanded;
- Ridgeside Village;
- future NPC expansion mods.

Expansion entries should never replace or redefine the vanilla balance baseline.

### Companion Providers

Creature-focused integrations such as Pelipper Town can appear primarily under Companion Units / Linked Companions where appropriate.

Exact owner-to-creature mappings must be resolved from the actual provider mod or integration data rather than guessed by Team Up!.

## 2. Proposed top-level Codex layout

```text
TEAM UP CODEX

Party Members
├─ Base Game
│  ├─ Marriage Candidates
│  ├─ Town & Special Adults
│  └─ Magical / Remote Characters
│
├─ Stardew Valley Expanded
├─ Ridgeside Village
└─ Other Expansion Packs

Companion Units
├─ Player Main Pet
├─ Vanilla / Team Up Creature Profiles
├─ Pelipper Town
└─ Other Companion Providers

Systems
├─ Roles
├─ Engagement Styles
├─ Party Strategies
├─ Threat / Aggro
├─ Party Vault
└─ Formations
```

## 3. Party Member page template

Each Party Member page should eventually contain:

```text
Name
Source Mod
Combat Eligibility

Primary Role
Secondary Role
Recommended Engagement Style

Role Affinity
Tank       ★★★
DPS        ★★★★
Support    ★★
Healer     ★
Control    ★★★

Passive
Signature Ability

Suggested Combat Range
Suggested Party Strategy
Retreat Guidance

Linked Companion
- none / available / provider-specific

Suggested Teammates
Strengths
Weaknesses
Combat Tips
Compatibility Notes
```

Ratings are recommendations and should be generated from tuned combat data once available.

## 4. Companion Unit page template

```text
Companion Name
Provider / Source Mod
Owner
Unit Type
Deployment State

Recommended Role
Recommended Engagement Style
Preferred Range
Combat Tags

Passive / Trait
Skills

Owner Synergy
Party Synergy
Threat Behavior
Strengths
Weaknesses
Compatibility Notes
```

## 5. Linked Companion display

A Linked Companion should appear nested under its owner wherever practical.

Example:

```text
Abigail
DPS / Control
Aggressive

Linked Companion
└─ Pikachu
   Control / DPS
   Active
```

This communicates that the creature is attached to Abigail's party relationship rather than consuming another Main Party slot.

## 6. Party-building pages

Later Wiki pages may provide composition suggestions such as:

### Balanced Party

- Tank;
- DPS;
- Support;
- Healer;
- optional Control/Flex;
- optional Linked Companion deployment.

### Boss Party

Recommendations based on encounter traits rather than fixed character requirements.

### Creature-heavy Party

Guidance for players using multiple Linked Companions.

The Wiki should suggest compositions without declaring one mandatory meta.

## 7. Vanilla-first documentation rule

The Wiki development order should be:

1. document Team Up! systems;
2. complete vanilla NPC entries;
3. tune vanilla combat data;
4. add SVE and Ridgeside roster pages;
5. add Pelipper Town / creature-provider pages after integration details are technically verified;
6. add third-party packs through the same templates.

## 8. Expansion page requirements

Every optional compatibility entry should display:

- source mod;
- required compatibility module or adapter;
- whether the character is a Party Member, Companion Unit, or both through ownership linkage;
- balance version;
- known limitations.

This prevents players from confusing Team Up! base content with another mod's content.

## 9. Balance language

Avoid Wiki wording like:

- "best NPC in the game";
- "strictly superior";
- "mandatory";

Prefer:

- Recommended Role;
- Strong Match;
- Alternative Build;
- High Synergy;
- Easy / Advanced to use.

Team Up! should encourage party experimentation rather than one solved roster.
