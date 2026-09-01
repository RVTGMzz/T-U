# Team Up! - Design Bible

## 1. Vision

Team Up! is a Stardew Valley party-combat mod inspired by MMORPG party structure rather than task automation.

The player builds an active team of NPCs, brings pets and creature companions into adventures, assigns combat responsibilities, manages shared resources, and takes that party into combat-heavy content from Stardew Valley and compatible mods.

## 2. Core fantasy

The player should feel like a party leader, not an employer.

The central design question is:

> What role does this companion play in the party?

rather than:

> What chore can this NPC perform for the player?

## 3. Party size and slot model

Team Up! separates **Party Members** from **Companion Units**.

### Main Party

- Default active Party Member slots: **4**.
- Configurable maximum Party Member slots: **6**.
- The player is not counted as a Party Member slot.
- Main Party slots are intended primarily for recruited NPCs.

### Companion Units

Companion Units are pets, Pokemon-like creatures, familiars, summons, or other non-humanoid companions associated with the player or a Party Member.

- Companion Units do **not** consume one of the 6 Main Party slots.
- The player's main pet is a special free companion and does not consume the linked-companion active limit.
- A Party Member may have an optional **Linked Companion**.
- Default maximum active Linked Companions: **2**.
- Configurable maximum active Linked Companions: **0 to 6**.
- Linked Companions beyond the active limit remain in **Standby** rather than disappearing from the roster.
- An advanced configuration may allow 6 Party Members plus 6 Linked Companions plus the player's main pet, but this should be treated as an experimental high-entity mode due to pathfinding, readability, balance, and performance concerns.

### Recommended default combat footprint

A normal recommended setup is:

- Player;
- 4 Party Members;
- up to 2 active Linked Companions;
- optional player main pet.

This keeps the default experience readable while preserving room for creature-focused integrations.

## 4. Roster vs Active Party

Team Up! should distinguish between the player's full **Roster** and the current **Active Party**.

- Roster: all unlocked or available Party Members and Companion Units.
- Active Party: Party Members currently traveling with the player.
- Active Linked Companions: selected linked creatures currently deployed with their owners.
- Standby Companion Units: linked creatures that remain registered but are not currently spawned or controlled by Team Up! combat logic.

Future dungeon or boss preparation can encourage the player to choose which Party Members and Linked Companions to deploy.

## 5. Combat roles

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

## 6. Role affinity

NPCs are not permanently class-locked.

Each Party Member can have role affinity values. Example:

- DPS ★★★★
- Control ★★★
- Tank ★★
- Support ★
- Healer ★

Affinity can affect stat scaling, skill access, AI efficiency, cooldown behavior, or passive bonuses.

The player may still create unconventional builds.

Companion Units may also have recommended combat roles or archetypes, but their role does not need to match their owner's role.

## 7. Party composition

Example party:

- Alex - Tank
- Abigail - DPS / Control
- Emily - Support
- Harvey - Healer
- Abigail's linked creature - Control / DPS
- Player main pet - Guard

The linked creature and player pet do not consume Main Party slots.

The system should support flexible compositions rather than requiring one exact MMO template.

## 8. Linked Companion ownership

A Linked Companion belongs to one Party Member for the current Team Up! relationship model.

Conceptual structure:

```text
PartyMember
  CharacterId
  Role
  State
  LinkedCompanionUnitId

CompanionUnit
  UnitId
  OwnerType
  OwnerId
  ProviderId
  UnitType
  Role
  DeploymentState
```

### Ownership rules

- A Linked Companion follows its **owner**, not the player directly.
- If the owner waits, the Linked Companion waits with or near the owner.
- If the owner resumes following, the Linked Companion resumes with the owner.
- If the owner leaves the party, the Linked Companion leaves Team Up!'s active deployment with them.
- If the owner retreats or is knocked out later, the Linked Companion should inherit an appropriate retreat/standby response.
- One physical companion instance must never be linked to two Party Members at the same time.
- Save/load must preserve ownership without spawning duplicate creature instances.

The initial implementation should support one Linked Companion per Party Member. The data model may remain extensible for more complex integrations later.

## 9. Invite flow with linked companions

When an NPC has a companion recognized by Team Up! or an integration adapter, invitation should explicitly ask whether to bring it.

Example:

```text
Invite Abigail to Team Up!?

Pikachu can come too.

> Abigail + Pikachu
  Abigail only
  Cancel
```

If the active Linked Companion limit is full:

```text
Companion deployment is full.

> Invite Abigail only
  Manage Companions
  Cancel
```

The NPC still occupies exactly one Main Party slot whether or not their linked creature is invited.

## 10. Threat and aggro

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

Companion Units can participate in threat using their own role and behavior profile.

## 11. Combat strategies

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

## 12. Engagement Style

Individual combat behavior should use **Engagement Style** rather than a raw aggression percentage.

Suggested levels:

- Passive: do not initiate combat; evade or defend when necessary.
- Cautious: engage only near the party and use a short leash.
- Balanced: standard engagement and chase behavior.
- Aggressive: acquire targets proactively and chase farther.
- Reckless: maximum pressure with a long leash and reduced formation discipline.

Engagement Style modifies how a role is performed. It does not replace the role.

Examples:

- Aggressive Tank: intercepts enemies early and actively generates threat.
- Aggressive DPS: chases priority targets and finishes weakened enemies.
- Aggressive Healer: stays closer to danger and reacts to healing opportunities earlier, rather than becoming a melee attacker.

Each combatant may later expose:

- assigned role;
- engagement style;
- preferred range;
- combat leash radius;
- skill priority;
- retreat HP threshold;
- target priority;
- threat modifier;
- emergency behavior;
- follow distance.

## 13. Companion Units and pets

Pets and creature companions are first-class combat systems, but they are not Main Party Members.

They should not be treated as NPCs with dialogue removed.

Potential combat identities:

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

### External creature / Pokemon-style integration

External integrations should provide a stable identity and resolver rather than forcing Team Up! to hard-code every creature mod.

Potential registration metadata:

- provider mod UniqueID;
- provider unit ID;
- display name;
- owner relationship;
- preferred role;
- combat tags;
- range profile;
- resolver for the live game entity;
- optional skill profile.

Team Up! should eventually expose an API for this registration.

## 14. Party Vault

**Party Vault is a permanent core feature and must remain part of Team Up!**

Working names:

- Party Vault
- Team Chest

It is shared by the whole team, including gameplay involving Linked Companions, rather than being a separate inventory for each NPC or pet.

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

Future companion consumable use must be opt-in and rule-driven so followers do not consume valuable items unexpectedly.

Initial Party Vault implementation should prioritize safe item persistence and controller-friendly access before auto-consumption or auto-loot logic.

## 15. Formation

Travel and combat formations may differ.

Travel should favor readability and path safety.

Combat formation may position:

- Tank forward;
- melee DPS near flanks;
- ranged/support farther back;
- healer protected;
- Linked Companion relative to its owner and role.

Linked Companions should normally anchor around their owner instead of competing for the same follow point behind the player.

Formation should react dynamically rather than force rigid tile-perfect positioning in cramped Stardew maps.

## 16. Companion identity

Party Members and Companion Units should eventually feel mechanically distinct.

Potential identity layers:

- role affinity;
- passive traits;
- active skills;
- combat range;
- engagement style;
- contextual combat dialogue for NPCs;
- compatibility with locations or encounter types.

Examples for concept only:

### Harvey

**Emergency Care** - prioritizes healing when an ally drops below a health threshold.

### Abigail

**Thrill of Battle** - stronger offensive behavior in combat zones.

### Alex

**Protector** - increased threat when nearby allies are attacked.

### Emily

**Positive Energy** - periodic party support buff.

Final values and effects should be implemented independently and balanced through Team Up!'s own systems.

## 17. Party Codex / Wiki

The future in-game Codex should support party-building decisions.

For each Party Member it may show:

- recommended role;
- alternate roles;
- role affinity ratings;
- traits/passives;
- available skills;
- suggested teammates;
- combat tips;
- linked companion information;
- mod compatibility notes.

For Companion Units it may show:

- owner;
- provider/source mod;
- recommended role;
- combat tags;
- skills;
- preferred engagement style;
- deployment status.

Example Party Member presentation:

### Abigail

- DPS ★★★★
- Control ★★★
- Tank ★★
- Support ★
- Healer ★

Recommended: Melee DPS / Control

Linked Companion: Pikachu

## 18. UI direction

Team Up! should have a visual identity clearly separate from The Stardew Squad.

Goals:

- avoid copying its menu layout;
- avoid copying its terminology;
- avoid its exact color language;
- prioritize controller navigation;
- keep party status readable during combat;
- use compact cards, role icons, health/status information, and clear focus states.

The Party Panel should visually distinguish:

- Main Party slots;
- Linked Companion attached under/next to its owner;
- player main pet as a separate special companion;
- Standby vs Active deployment;
- Party Vault access.

Potential visual palette should be designed specifically for Team Up! later.

## 19. Integration direction

Team Up! is intended to support other Ronvotri mods and eventually third-party integrations.

Future API registration targets may include:

- bosses;
- dungeons;
- Party Members;
- Companion Units;
- pets;
- Pokemon-like creatures;
- combat skills;
- roles/affinities;
- loot profiles;
- encounter metadata;
- combat dialogue profiles.

## 20. Version roadmap

### v0.1 - Party Core

- invite/recruit NPC Party Members;
- player main pet as a free Companion Unit;
- Linked Companion data model;
- follow;
- wait;
- resume;
- leave party;
- party UI;
- Party Vault;
- 4 Main Party slots by default, configurable to 6;
- default 2 active Linked Companions, configurable to 6;
- controller/keyboard/mouse support.

### v0.2 - Combat Roles

- Tank;
- DPS;
- Support;
- Healer;
- Control;
- Engagement Style;
- combat leash;
- retreat thresholds;
- target priorities;
- threat/aggro;
- combat targeting;
- role behavior profiles.

### v0.3 - Skills

- active skills;
- cooldowns;
- buffs;
- debuffs;
- healing;
- shielding;
- control effects;
- creature skills through integration profiles where supported.

### v0.4 - Party Builds

- role affinities;
- traits/passives;
- equipment;
- formations;
- strategy presets;
- linked companion deployment management.

### v0.5 - Party Codex

- NPC recommendations;
- party suggestions;
- skill encyclopedia;
- Companion Unit recommendations;
- owner/companion pairing information;
- compatibility information.

### v0.6 - Integration API

- external Party Member registration;
- Companion Unit registration;
- pet/creature profile registration;
- live-entity resolver adapters;
- encounter integration;
- boss/dungeon metadata;
- loot integration;
- skill registration where safe and stable.

## 21. Scope discipline

Do not rebuild every idea at once.

The first milestone must prove:

1. independent Party Member state;
2. stable follow/wait/resume;
3. stable map transitions;
4. stable party membership;
5. player main pet that does not consume a Main Party slot;
6. Linked Companion ownership and save/load without duplicate entities;
7. reliable controller interaction;
8. shared Party Vault state;
9. no dependency on The Stardew Squad.

Combat depth should be layered on top only after Party Core is stable.
