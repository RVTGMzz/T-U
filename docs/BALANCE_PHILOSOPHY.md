# Team Up! - Balance Philosophy

## Purpose

This document defines the balance hierarchy for Team Up! and records the current design decisions for Party Members, Companion Units, expansion NPCs, and future compatibility packs.

## 1. Vanilla-first balance

**Stardew Valley base-game NPCs are the canonical balance baseline for Team Up!.**

Team Up! must be fully playable and mechanically complete without Stardew Valley Expanded, Ridgeside Village, Pelipper Town, or any other content expansion.

The base-game roster defines the normal Team Up! power budget for:

- Party Member durability;
- damage output;
- healing output;
- support value;
- control value;
- passive strength;
- Signature Ability strength;
- cooldown expectations;
- role affinity;
- Engagement Style behavior.

Expansion NPCs should be balanced **against the vanilla roster**, not above it.

A character being more powerful in lore does not automatically justify a stronger Team Up! kit.

## 2. Content tiers

Team Up! should present roster sources in clear tiers.

### Base Game

The primary roster and balance reference.

This roster should always receive the most careful balancing and documentation.

### Expansion Rosters

Examples:

- Stardew Valley Expanded;
- Ridgeside Village;
- future NPC expansions.

Expansion characters may add new party-building options, but should use the same Team Up! balance budget as vanilla characters.

They are additions to the roster, not a second power tier.

### Companion Providers

Creature-focused mods can primarily integrate by providing **Companion Units** and owner/companion relationships.

Pelipper Town is currently planned to be treated primarily as a creature / Linked Companion integration target where technically appropriate.

Exact NPC ownership, Pokemon mapping, and live-entity handling must be verified through that mod's actual data/API/behavior before implementation. Team Up! should not hard-code assumptions that have not been verified.

## 3. Main Party and Companion Units

The current slot rules remain:

### Main Party

- default: 4 Party Members;
- configurable maximum: 6 Party Members;
- player does not count;
- Main Party slots are intended primarily for NPC Party Members.

### Companion Units

- do not consume Main Party slots;
- player main pet is a free special exception;
- NPCs may have one optional Linked Companion in the initial design;
- default active Linked Companion limit: 2;
- configurable active Linked Companion limit: 0 to 6;
- extra Linked Companions remain in Standby.

An experimental high-entity setup may eventually allow 6 Party Members + 6 Linked Companions + the player's main pet, but it must not be the recommended default due to pathfinding, readability, combat balance, and performance concerns.

## 4. Party Vault remains core

**Party Vault is a permanent core feature.**

It is the shared expedition inventory for the whole Team Up! party and should not be replaced by separate per-NPC inventories.

Initial priorities:

- safe item persistence;
- controller-friendly access;
- support for modded items where possible;
- no automatic consumption of valuable items by followers.

Later systems may add opt-in auto-loot and party consumable rules.

## 5. Role model

The standard Team Up! Party Member roles are:

- Tank;
- DPS;
- Support;
- Healer;
- Control.

NPCs are not hard-locked to one class.

Each character can have:

- one recommended Primary Role;
- one recommended Secondary Role;
- role affinity ratings;
- one Passive;
- one Signature Ability;
- a recommended Engagement Style.

This gives each NPC identity without requiring a large skill tree for the first balance pass.

## 6. Ability budget

For the first balanced roster pass, each vanilla Party Member should be designed around a simple comparable package:

**1 Passive + 1 Signature Ability + role affinities.**

A Signature Ability can be strong, but its total party value should remain comparable to other vanilla Party Members.

Examples of equivalent value:

- a Tank may prevent damage rather than deal it;
- a Healer may restore HP rather than attack;
- a Support may increase the total party's output;
- a Control character may reduce enemy pressure;
- a DPS may contribute direct burst or sustained damage.

Balance should compare **party contribution**, not damage numbers alone.

## 7. Suggested Wiki ratings

The Codex may eventually display readable ratings such as:

- Damage;
- Defense;
- Support;
- Control;
- Mobility;
- Difficulty.

These ratings are player-facing guidance, not necessarily literal internal stat multipliers.

## 8. Engagement Style

Engagement Style remains independent from Role:

- Passive;
- Cautious;
- Balanced;
- Aggressive;
- Reckless.

It controls how proactively a unit performs its role.

An Aggressive Healer should heal and reposition more proactively, not suddenly behave like a melee DPS.

Future combat AI can combine Engagement Style with:

- combat leash;
- retreat HP threshold;
- target priority;
- preferred range;
- threat modifier;
- emergency behavior.

## 9. Linked Companion power budget

A Linked Companion should add meaningful utility without making an NPC + companion pair equivalent to two full Party Members by default.

Exact values will be determined through testing, but the design intent is that Companion Units use a **smaller combat contribution budget** than a full Party Member unless a future encounter mode is explicitly balanced around larger teams.

This is especially important when multiple Party Members can bring creature companions.

## 10. Expansion balance rule

When adding SVE, Ridgeside Village, or other NPC expansion support:

1. identify the character's personality, story, and obvious combat fantasy;
2. map them into the existing five-role system;
3. compare their full kit against vanilla benchmarks;
4. do not increase total kit strength merely because the source mod is late-game or dramatic;
5. allow uniqueness through mechanics rather than raw power creep.

## 11. Design checkpoint

Before an expansion compatibility pack is considered balanced, Team Up! should already have a stable vanilla reference roster.

The preferred development order is:

**Vanilla roster -> combat system tuning -> vanilla balance pass -> expansion rosters -> creature-provider integrations.**
