# Team Up! — Independent Development / Clean-Room Rules

This document records the development boundary for Team Up! so the project remains clearly independent from other Stardew Valley companion mods.

## Project identity

Team Up! is a new party-combat mod focused on MMORPG-style team composition, combat roles, pets, shared storage, tactics, and integration with combat-heavy content.

It is not intended to be a fork, modification, repackage, or redistribution of The Stardew Squad.

## Do not copy

Do not copy or redistribute from The Stardew Squad:

- source code;
- DLLs;
- assets;
- sprites;
- UI graphics;
- translations;
- dialogue;
- content packs;
- configuration files;
- class implementations;
- internal algorithms;
- menu layout;
- distinctive wording or naming.

## Do not port architecture

Team Up! should not port internal classes or systems from another mod and simply rename them.

Use Team Up!'s own architecture and terminology.

Preferred conceptual vocabulary includes:

- Party
- Companion
- Party Member
- Party Vault / Team Chest
- Role
- Affinity
- Threat
- Strategy
- Formation
- Skill
- Trait
- Codex

## Shared gameplay concepts

General gameplay concepts are allowed to exist independently, including:

- NPCs following the player;
- pets accompanying the player;
- wait/resume commands;
- multiple companions;
- combat followers;
- party roles;
- shared inventory/storage;
- healing;
- aggro/threat;
- formations;
- buffs/debuffs;
- controller support.

These concepts must be implemented independently for Team Up!.

## Implementation rule

When implementing a feature:

1. Write down the Team Up! behavior specification first.
2. Implement against Stardew Valley / SMAPI APIs and permitted dependencies.
3. Prefer public game/API behavior over reverse-porting another mod's internal code.
4. If compatibility research is needed, document what behavior must interoperate, not how another mod internally implements it.
5. Keep implementation commits small and descriptive.

## Git history

Use commit history as a development record.

Recommended commit style:

- `Initialize Team Up SMAPI project`
- `Add independent party member registry`
- `Add follow state machine`
- `Add wait and resume states`
- `Add pet party member prototype`
- `Add Party Vault prototype`
- `Add threat model prototype`
- `Add Tank role behavior`
- `Add controller party menu`

Avoid importing a large pre-existing codebase as the first commit.

## Dependency boundary

The initial Team Up! core should not require The Stardew Squad.

Do not package:

- `TheStardewSquad.dll`;
- its content packs;
- its assets;
- modified copies of its files.

If compatibility support is added later, it should be optional and implemented through safe detection/API/interop patterns rather than redistribution.

## UI boundary

Team Up! should have its own UI identity.

Avoid recreating another mod's exact:

- panel structure;
- button arrangement;
- color hierarchy;
- labels;
- iconography;
- recruitment/management flow.

Team Up! UI should reflect party combat concepts: roles, HP/status, strategy, skills, threat, party slots, and shared storage.

## Product differentiation

The clearest distinction is product focus.

Team Up! asks:

> What role does this companion play in the party?

Its identity should come from:

- Tank / DPS / Support / Healer / Control roles;
- role affinity;
- threat and aggro;
- pet combat roles;
- party strategies;
- Party Vault;
- formations;
- companion traits and skills;
- Party Codex;
- boss/dungeon integrations.

## Documentation statement

When Team Up! becomes public, the README may state:

> Team Up! is an independently developed Stardew Valley party-combat mod. It is not a fork, modification, or redistribution of The Stardew Squad and does not include its code or assets.

Do not claim that the developers have never viewed or researched other companion mods if that would be inaccurate. The meaningful claim is that Team Up!'s distributed code and assets are independently created.
