# Team Up! alpha.5 - Party Identity & Codex

## Purpose

Alpha.5 gives party members a readable RPG identity before combat mechanics are activated.

The player should be able to answer three questions quickly:

1. What is this NPC naturally good at?
2. What role have I currently assigned them?
3. Where can I learn what that role means?

## Role language

Team Up uses five core combat roles:

- Tank
- DPS
- Support
- Healer
- Control

Role icons are original Team Up pixel glyphs generated in code for the alpha. This avoids external art dependencies and lets the visual language stabilize before polished assets are produced.

Color is supplemental only. The icon silhouette and role text must remain readable independently.

## Recommended Role vs Current Role

NPCs do not have permanently locked classes.

Each combat profile has:

- Primary Role
- Secondary Role
- five role affinities from 1 to 5
- Recommended Engagement Style
- one Passive concept
- one Signature Ability concept

The player's Current Role is saved separately and can be changed freely.

For profiled NPCs, first recruitment initializes Current Role to the Primary Role and Engagement Style to the recommended value. The player may then override both.

## Vanilla-first alpha profiles

Alpha.5 starts with five vanilla NPCs, one representative for each core role:

| NPC | Primary | Secondary | Engagement |
| --- | --- | --- | --- |
| Abigail | DPS | Control | Aggressive |
| Alex | Tank | DPS | Balanced |
| Emily | Support | Healer | Cautious |
| Harvey | Healer | Support | Cautious |
| Maru | Control | Support | Balanced |

These are Team Up gameplay concepts, not vanilla Stardew Valley mechanics.

## In-game discovery

Before recruitment, a profiled NPC dialogue can show:

- the normal `R (Controller) / E Join Party` hint;
- Recommended Primary / Secondary roles;
- the Primary Role pixel icon.

After recruitment, the Team Member menu exposes:

- Talk
- Follow / Stand
- Current Role
- Engagement Style
- Party Vault
- Team Up Codex
- Leave Team

## Team Up Codex

Alpha.5 uses Stardew-native question-dialogue UI for the Codex so keyboard/controller navigation works before a bespoke book UI is attempted.

Initial sections:

- Characters
- Party Roles
- Party Vault shortcut

Character entries show:

- Primary / Secondary Role
- Recommended Engagement Style
- affinity values for all five roles
- Passive concept
- Signature Ability concept
- Primary and Secondary role icons

Role entries explain the tactical purpose of Tank, DPS, Support, Healer, and Control.

## Future upgrade path

The alpha Codex is intentionally data-first. A later custom book/panel UI may replace the native dialogue presentation without changing the profile model.

Future additions may include:

- all vanilla recruitable NPCs;
- compatibility rosters for SVE and Ridgeside Village;
- linked companion/provider information;
- party synergy suggestions;
- preferred range and retreat threshold;
- unlocked combat skills and traits;
- Party HUD role badges.

Vanilla remains the canonical balance baseline. Expansion NPCs must fit the same party power budget rather than becoming stronger simply because they come from external content mods.
