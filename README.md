# Team Up!

**Party & Combat Companions for Stardew Valley**

> Build an RPG-style party from Stardew Valley NPCs, bring pets and creature companions, assign combat roles, share loot, and take the team into monster-heavy content.

## Current development checkpoint

Current source line: **`v0.1.0-alpha.5` - Party Identity & Codex**.

If you are resuming development in a new chat/session, **read [`CONTINUE_HERE.md`](CONTINUE_HERE.md) first**. It records the locked recruitment/management UX, Party Vault architecture, role/Codex work, historical regressions to avoid, validation state, and the next combat milestone.

For what is actually present in source vs future design, see [`docs/V0_1_IMPLEMENTATION_STATUS.md`](docs/V0_1_IMPLEMENTATION_STATUS.md).

## Project direction

Team Up! is an independently developed companion-combat mod focused on **party building, tactical roles, pets/creatures, shared storage, and MMORPG-inspired combat systems**.

The design goal is not simply to make NPCs follow the player. Each Party Member should have a meaningful place in the team, while pets and Pokemon-like creatures can join as separate Companion Units.

## Core pillars

- **Party-based combat** with NPC Party Members and creature companions.
- **4 Main Party slots by default**, configurable up to 6.
- **Companion Units do not consume Main Party slots.**
- **Player main pet is a free special companion.**
- **NPC Linked Companions** can travel and fight with their owner.
- **2 active Linked Companions by default**, configurable from 0 to 6.
- **Combat roles**: Tank, DPS, Support, Healer, and Control.
- **Engagement Styles**: Passive, Cautious, Balanced, Aggressive, and Reckless.
- **Role affinity** instead of hard class locking.
- **Party Vault** for shared loot and expedition supplies.
- **Threat / aggro gameplay** so Tank roles have real purpose.
- **Combat strategies** such as Balanced, Defensive, Aggressive, Hold Position, and Boss Focus.
- **Controller-first interaction**, with keyboard/mouse support and future mobile-friendly UI considerations.
- **Party Codex / Wiki** showing recommended roles, traits, affinities, linked companions, skills, and suggested compositions.
- **Integration-friendly architecture** so other mods can register bosses, dungeons, companions, pets, creature profiles, skills, loot, or encounter metadata later.

## Party slot model

Team Up! separates the Main Party from Companion Units.

### Main Party

- Player does not count toward the limit.
- Default: 4 NPC Party Members.
- Maximum: 6 NPC Party Members.

### Companion Units

- Player main pet: free special companion.
- Linked Companion: optional pet/Pokemon-like creature attached to a Party Member.
- Linked Companions do not consume one of the 6 Main Party slots.
- Default active Linked Companion limit: 2.
- Configurable active Linked Companion limit: 0 to 6.
- Extra registered companions can remain in Standby.

Recommended default combat footprint:

```text
Player
├─ Alex        Tank
├─ Abigail     DPS / Control
│  └─ Pikachu  Linked Companion
├─ Emily       Support
├─ Harvey      Healer
├─ Dog         Player Main Pet
└─ One optional additional active Linked Companion
```

An advanced configuration may eventually allow 6 Party Members + 6 Linked Companions + the player's main pet, but high-entity setups will be considered experimental due to pathfinding, screen readability, balance, and performance.

## Proposed party roles

| Role | Purpose |
| --- | --- |
| Tank | Hold threat, protect allies, intercept enemies, survive pressure. |
| DPS | Primary damage dealer. Can later branch into melee/ranged styles. |
| Support | Buff allies, debuff enemies, improve party performance. |
| Healer | Restore HP, shield allies, emergency recovery. |
| Control | Stun, slow, root, knock back, interrupt, or manipulate enemy positioning. |

NPCs should not be permanently class-locked. Instead, each Party Member can have an affinity profile, for example:

- DPS ★★★★
- Control ★★★
- Tank ★★
- Support ★
- Healer ★

The player can still build unusual teams, while the future Codex can recommend stronger combinations.

## Engagement Style

Role and aggression are separate concepts.

Planned Engagement Styles:

- **Passive**: never initiates combat unless forced to defend.
- **Cautious**: short leash, stays near the party.
- **Balanced**: normal targeting and chase behavior.
- **Aggressive**: proactively acquires targets and chases farther.
- **Reckless**: maximum pressure with reduced formation discipline.

A Healer using Aggressive does not become a melee attacker. It means the Healer reacts earlier, moves closer to active combat, and uses their support role more proactively.

Future AI layers include combat leash, retreat HP threshold, target priority, and threat modifiers.

## Linked Companions

A Party Member may have one linked creature in the initial design.

When an integration identifies that an invited NPC owns a companion, Team Up! can later show a prompt such as:

```text
Invite Abigail to Team Up!?

Pikachu can come too.

> Abigail + Pikachu
  Abigail only
  Cancel
```

A Linked Companion follows its owner rather than competing for the same follow point behind the player. If the owner waits, resumes, retreats, or leaves the party, the linked creature should inherit the appropriate response.

External creature mods should eventually integrate through a registration/resolver API rather than Team Up! hard-coding every creature implementation.

## Party Vault

**Party Vault is a permanent core Team Up! feature.**

It is shared by the whole party rather than giving every NPC or pet a separate inventory.

Planned uses include:

- monster drops;
- healing items;
- bombs and consumables;
- companion equipment;
- loot from supported dungeons or bosses;
- integration items from supported mods;
- configurable auto-loot rules later.

Future companion consumable use will be opt-in so followers cannot unexpectedly consume valuable player items.

## Combat systems planned

### Threat / Aggro

Tank behavior should be more than "high HP". Team Up! will aim for a threat model where taunts, role stance, damage, healing, support actions, and skills influence enemy targeting.

### Strategy presets

Party-level tactical presets may include:

- Balanced
- Defensive
- Aggressive
- Hold Position
- Boss Focus

### Formations

Travel and combat formations may differ. Tanks can advance, melee DPS can flank, ranged/support companions can maintain distance, and Linked Companions can anchor around their owner.

## Party Codex / Wiki

A future in-game Codex will help players build teams instead of relying on trial-and-error.

Planned information:

- recommended role(s) for each NPC;
- role affinity ratings;
- Engagement Style recommendation;
- traits/passives;
- skills;
- suggested teammates;
- linked companion information;
- combat tips;
- compatibility notes;
- pet/creature role guidance.

## Roadmap

### v0.1 - Party Core

- Invite/recruit NPC Party Members.
- Player main pet as a free Companion Unit.
- Linked Companion data model.
- Follow, Wait, Resume, Leave Party.
- Party UI.
- Party Vault.
- 4 Main Party slots by default, configurable to 6.
- 2 active Linked Companions by default, configurable to 6.
- Keyboard, mouse, and controller support.

### v0.2 - Combat Roles

- Tank / DPS / Support / Healer / Control.
- Engagement Style.
- Combat leash and retreat thresholds.
- Target priorities.
- Threat and aggro.
- Combat targeting.
- Basic combat behavior profiles.

### v0.3 - Skills

- Active abilities.
- Cooldowns.
- Buffs and debuffs.
- Healing and shielding.
- Status/control effects.
- Creature skills through supported integration profiles.

### v0.4 - Party Builds

- NPC affinities.
- Traits/passives.
- Equipment.
- Formations.
- Strategy presets.
- Linked Companion deployment management.

### v0.5 - Party Codex

- NPC recommendations.
- Party suggestions.
- Skill encyclopedia.
- Companion Unit recommendations.
- Owner/companion pairing information.
- Compatibility information.

### v0.6 - Integration API

Allow other mods to register or integrate:

- bosses;
- dungeons;
- Party Members;
- Companion Units;
- pets/Pokemon-like creatures;
- skills;
- roles;
- loot;
- encounter profiles;
- live-entity resolvers.

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
- **SMAPI UniqueID:** `Ronvotri.TeamUp`
- **Working subtitle:** *Party & Combat Companions for Stardew Valley*

## Design compass

> Don't ask only, "What work can this NPC do for the player?" Ask, **"What role does this companion play in the party?"**

That question should guide Team Up! toward its own identity: an RPG-style party framework built for Stardew Valley combat, exploration, pets, creature companions, shared loot, and future mod integrations.
