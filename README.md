# Team Up!

**Party & Combat Companions for Stardew Valley**

> Build a real RPG-style party from Stardew Valley NPCs and pets, assign combat roles, share loot, and take your team into monster-heavy content.

## Project direction

Team Up! is an independently developed companion-combat mod focused on **party building, tactical roles, pets, shared storage, and MMORPG-inspired combat systems**.

The design goal is not simply to make NPCs follow the player. Each companion should have a meaningful place in the party, with strengths, preferred roles, combat behavior, skills, and later a codex/wiki that helps players build effective teams.

## Core pillars

- **Party-based combat** with NPCs and pets.
- **4 active companions by default**, configurable up to 6 where practical.
- **Combat roles** such as Tank, DPS, Support, Healer, and Control.
- **Role affinity** instead of hard class locking: NPCs can fill different jobs, but some roles suit them better.
- **Party Vault / Team Chest** for shared loot and expedition supplies.
- **Threat / aggro gameplay** so Tank roles have real purpose.
- **Combat strategies** for the whole party, such as Balanced, Defensive, Aggressive, Hold Position, and Boss Focus.
- **Pet combat roles and abilities**, not cosmetic followers only.
- **Controller-first interaction**, with keyboard/mouse support and future mobile-friendly UI considerations.
- **Party Codex / Wiki** showing recommended NPC roles, traits, affinities, skills, and suggested compositions.
- **Integration-friendly architecture** so other Ronvotri mods can add bosses, dungeons, companions, pets, skills, loot, or encounter profiles later.

## Proposed party roles

| Role | Purpose |
| --- | --- |
| Tank | Hold threat, protect allies, intercept enemies, survive pressure. |
| DPS | Primary damage dealer. Can later branch into melee/ranged styles. |
| Support | Buff allies, debuff enemies, improve party performance. |
| Healer | Restore HP, shield allies, emergency recovery. |
| Control | Stun, slow, root, knock back, interrupt, or manipulate enemy positioning. |

NPCs should not be permanently class-locked. Instead, each companion can have an affinity profile, for example:

- DPS ★★★★
- Control ★★★
- Tank ★★
- Support ★
- Healer ★

The player can still build unusual teams, while the future Codex can recommend stronger combinations.

## Example party

- **Alex** — Tank
- **Abigail** — DPS / Control
- **Emily** — Support
- **Harvey** — Healer
- **Pet** — Guard / Flex

## Party Vault

The shared storage system is part of the Team Up! identity and should support combat/exploration gameplay rather than simple follower inventory.

Planned uses include:

- monster drops;
- healing items;
- bombs and consumables;
- companion equipment;
- loot from supported dungeons or bosses;
- configurable auto-loot rules later.

## Combat systems planned

### Threat / Aggro

Tank behavior should be more than "high HP". Team Up! will aim for a threat model where taunts, role stance, damage, healing, and skills influence enemy targeting.

### Strategy presets

Party-level tactical presets may include:

- Balanced
- Defensive
- Aggressive
- Hold Position
- Boss Focus

### Formations

Travel and combat formations may differ. During combat, Tanks can advance, melee DPS can flank, ranged/support companions can maintain distance, and pets can follow their assigned behavior profile.

### Companion identity

Companions should eventually feel different through:

- role affinities;
- traits/passives;
- skill priorities;
- combat range;
- retreat thresholds;
- contextual behavior;
- adventure/combat dialogue.

## Pet direction

Pets are a first-class system in Team Up!, not an afterthought.

Potential pet archetypes:

- Guard / Tank
- Fast DPS
- Support
- Control

Future integrations may allow external mods to register pet profiles and abilities through a Team Up! API.

## Party Codex / Wiki

A future in-game Codex will help players understand party building instead of forcing trial-and-error.

Planned information:

- recommended role(s) for each NPC;
- role affinity ratings;
- traits/passives;
- skills;
- suggested teammates;
- combat tips;
- compatibility notes;
- pet role guidance.

## Roadmap

### v0.1 — Party Core

- Invite/recruit companions.
- NPC and pet party members.
- Follow, Wait, Resume, Leave Party.
- Party UI.
- Party Vault.
- 4 active slots by default, configurable toward 6.
- Keyboard, mouse, and controller support.

### v0.2 — Combat Roles

- Tank / DPS / Support / Healer / Control.
- Threat and aggro.
- Combat targeting.
- Basic combat behavior profiles.

### v0.3 — Skills

- Active abilities.
- Cooldowns.
- Buffs and debuffs.
- Healing and shielding.
- Status/control effects.

### v0.4 — Party Builds

- NPC affinities.
- Traits/passives.
- Equipment.
- Formations.
- Strategy presets.

### v0.5 — Party Codex

- NPC recommendations.
- Party suggestions.
- Skill encyclopedia.
- Pet recommendations.
- Compatibility information.

### v0.6 — Integration API

Allow other mods to register or integrate:

- bosses;
- dungeons;
- companions;
- pets;
- skills;
- roles;
- loot;
- encounter profiles.

## Independent development / clean-room rule

Team Up! is a **new independent codebase**. It is not intended to be a fork, modification, or redistribution of The Stardew Squad.

Project rules:

- Do not copy or redistribute The Stardew Squad code, DLLs, assets, translations, content packs, UI assets, or dialogue.
- Do not port its internal classes or implementation into Team Up!.
- Build Team Up! systems from a fresh architecture using Stardew Valley, SMAPI, and permitted dependencies/APIs.
- Shared genre concepts such as companions, pets, following, waiting, combat, parties, or shared storage should be implemented independently.
- Keep Git history clear so the design and implementation process remains traceable.
- Compatibility research with other mods is allowed, but compatibility research must not become code/asset copying.

## Naming

- **Display name:** Team Up!
- **Repository:** `ronvotri/Team-Up`
- **Suggested SMAPI UniqueID:** `Ronvotri.TeamUp`
- **Working subtitle:** *Party & Combat Companions for Stardew Valley*

## Design compass

> Don't ask only, "What work can this NPC do for the player?" Ask, **"What role does this companion play in the party?"**

That question should guide Team Up! toward its own identity: an RPG-style party framework built for Stardew Valley combat, exploration, pets, and future mod integrations.
