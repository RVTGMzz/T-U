# Team Up! — Design Bible

## 1. Vision

Team Up! is a Stardew Valley party-combat mod inspired by MMORPG party structure rather than task automation.

The player builds an active team of NPCs and pets, assigns combat responsibilities, manages shared resources, and brings that party into combat-heavy content from Stardew Valley and compatible mods.

## 2. Core fantasy

The player should feel like a party leader, not an employer.

The central design question is:

> What role does this companion play in the party?

rather than:

> What chore can this NPC perform for the player?

## 3. Party size

- Default active companion slots: **4**.
- Target configurable maximum: **6** where performance and map readability permit it.
- The player is not counted as one of the companion slots.
- The system should be designed so encounters and UI remain understandable with both small and large parties.

## 4. Combat roles

### Tank

Responsibilities:

- generate and hold threat;
- intercept enemies;
- protect vulnerable allies;
- survive sustained pressure;
- use taunts, guards, blocks, or defensive skills.

### DPS

Responsibilities:

- deal primary damage;
- exploit openings;
- prioritize dangerous or weakened targets;
- later support melee/ranged specializations.

### Support

Responsibilities:

- buff allies;
- debuff enemies;
- improve movement, attack, defense, crit, or utility;
- improve party efficiency without necessarily healing.

### Healer

Responsibilities:

- restore HP;
- shield allies;
- rescue low-health party members;
- use emergency recovery logic.

### Control

Responsibilities:

- stun;
- slow;
- root;
- knockback;
- interrupt;
- manipulate enemy positioning or attack timing.

## 5. Role affinity

NPCs are not permanently class-locked.

Each companion can have role affinity values. Example:

- DPS ★★★★
- Control ★★★
- Tank ★★
- Support ★
- Healer ★

Affinity can affect stat scaling, skill access, AI efficiency, cooldown behavior, or passive bonuses.

The player may still create unconventional builds.

## 6. Party composition

Example five-companion setup:

- Alex — Tank
- Abigail — DPS / Control
- Emily — Support
- Harvey — Healer
- Pet — Guard / Flex

The system should support flexible compositions rather than requiring one exact MMO template.

## 7. Threat and aggro

Threat is a core mechanic because Tank roles need mechanical purpose.

Potential threat sources:

- damage dealt;
- healing done;
- taunt skills;
- tank stance bonuses;
- support actions;
- proximity;
- scripted boss mechanics.

Potential modifiers:

- Tank: increased threat generation.
- DPS: reduced passive threat.
- Support/Healer: reduced threat, but spikes can occur during strong skills.

Bosses or special enemies may override normal targeting rules.

## 8. Combat strategies

Party-level strategy presets should eventually include:

### Balanced

General-purpose behavior.

### Defensive

Stay closer to the player, reduce chase distance, prioritize survival.

### Aggressive

Higher chase radius and target pressure.

### Hold Position

Do not leave a defined area unless forced.

### Boss Focus

Prioritize one marked or highest-priority target.

## 9. Individual companion behavior

Each party member may later expose:

- assigned role;
- combat stance;
- preferred range;
- chase radius;
- skill priority;
- retreat HP threshold;
- target priority;
- threat modifier;
- emergency behavior;
- follow distance.

## 10. Pets

Pets are first-class party members.

They should not be treated as NPCs with dialogue removed.

Potential pet combat identities:

- Guard / Tank
- Fast DPS
- Support
- Control

Possible examples:

### Dog

- guard owner;
- bark taunt;
- knockback;
- defensive utility.

### Cat

- fast attacks;
- dodge;
- crit-oriented DPS;
- weak-target prioritization.

External mods should eventually be able to register custom pet profiles through an API.

## 11. Party Vault

Shared storage is a core system.

Working names:

- Party Vault
- Team Chest

Primary use cases:

- monster drops;
- healing items;
- bombs;
- consumables;
- companion equipment;
- boss/dungeon loot;
- integration items from supported mods.

Potential future auto-loot rules:

- Monster Drops -> Party Vault
- Forage -> Player
- Weapons -> Party Vault
- Consumables -> Player

## 12. Formation

Travel and combat formations may differ.

Travel should favor readability and path safety.

Combat formation may position:

- Tank forward;
- melee DPS near flanks;
- ranged/support farther back;
- healer protected;
- pet according to role.

Formation should react dynamically rather than force rigid tile-perfect positioning in cramped Stardew maps.

## 13. Companion identity

Party members should eventually feel mechanically distinct.

Potential identity layers:

- role affinity;
- passive traits;
- active skills;
- combat range;
- behavior personality;
- contextual combat dialogue;
- compatibility with locations or encounter types.

Examples for concept only:

### Harvey

**Emergency Care** — prioritizes healing when an ally drops below a health threshold.

### Abigail

**Thrill of Battle** — stronger offensive behavior in combat zones.

### Alex

**Protector** — increased threat when nearby allies are attacked.

### Emily

**Positive Energy** — periodic party support buff.

Final values and effects should be implemented independently and balanced through Team Up!'s own systems.

## 14. Party Codex / Wiki

The future in-game Codex should support party-building decisions.

For each companion it may show:

- recommended role;
- alternate roles;
- role affinity ratings;
- traits/passives;
- available skills;
- suggested teammates;
- combat tips;
- pet compatibility;
- mod compatibility notes.

Example presentation:

### Abigail

- DPS ★★★★
- Control ★★★
- Tank ★★
- Support ★
- Healer ★

Recommended: Melee DPS / Control

## 15. UI direction

Team Up! should have a visual identity clearly separate from The Stardew Squad.

Goals:

- avoid copying its menu layout;
- avoid copying its terminology;
- avoid its exact color language;
- prioritize controller navigation;
- keep party status readable during combat;
- use compact cards, role icons, health/status information, and clear focus states.

Potential visual palette should be designed specifically for Team Up! later.

## 16. Integration direction

Team Up! is intended to support other Ronvotri mods and eventually third-party integrations.

Future API registration targets may include:

- bosses;
- dungeons;
- companions;
- pets;
- combat skills;
- roles/affinities;
- loot profiles;
- encounter metadata;
- combat dialogue profiles.

## 17. Version roadmap

### v0.1 — Party Core

- invite/recruit;
- NPC party members;
- pet party members;
- follow;
- wait;
- resume;
- leave party;
- party UI;
- Party Vault;
- 4 active slots by default;
- controller/keyboard/mouse support.

### v0.2 — Combat Roles

- Tank;
- DPS;
- Support;
- Healer;
- Control;
- threat/aggro;
- combat targeting;
- role behavior profiles.

### v0.3 — Skills

- active skills;
- cooldowns;
- buffs;
- debuffs;
- healing;
- shielding;
- control effects.

### v0.4 — Party Builds

- role affinities;
- traits/passives;
- equipment;
- formations;
- strategy presets.

### v0.5 — Party Codex

- NPC recommendations;
- party suggestions;
- skill encyclopedia;
- pet recommendations;
- compatibility information.

### v0.6 — Integration API

- external companion registration;
- pet profile registration;
- encounter integration;
- boss/dungeon metadata;
- loot integration;
- skill registration where safe and stable.

## 18. Scope discipline

Do not rebuild every idea at once.

The first milestone must prove:

1. independent companion state;
2. stable follow/wait/resume;
3. stable map transitions;
4. stable party membership;
5. reliable controller interaction;
6. shared Party Vault state;
7. NPC and pet support without dependency on The Stardew Squad.

Combat depth should be layered on top only after Party Core is stable.
