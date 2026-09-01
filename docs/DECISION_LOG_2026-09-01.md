# Team Up! - Decision Log - 2026-09-01

This file records the current agreed design decisions so later implementation changes can be compared against a clear baseline.

## Party identity

- Team Up! is primarily a party-combat / MMORPG-style companion framework.
- Main Party uses NPC Party Members.
- Default Main Party size is 4.
- Maximum Main Party size is 6.
- The player does not consume a Main Party slot.

## Companion Units

- Pets and Pokemon-like creatures are Companion Units, not Main Party Members.
- Player main pet is a free special exception.
- An NPC may have one Linked Companion in the initial model.
- Linked Companions do not consume Main Party slots.
- Default active Linked Companion limit is 2.
- Configurable active Linked Companion limit is 0 to 6.
- Additional linked creatures can remain in Standby.
- Linked Companions follow their owner rather than following the player directly.
- High-entity setups such as 6 NPCs + 6 Linked Companions + player main pet are experimental, not the recommended default.

## Invite flow

If an integration reports that an invited NPC has a Linked Companion, Team Up! should ask whether to bring that companion too.

The NPC still consumes one Main Party slot whether or not the Linked Companion is deployed.

## Party Vault

- Party Vault is retained as a permanent core feature.
- It is a shared team inventory, not one chest per follower.
- Auto-consumption and auto-loot are future opt-in systems, not initial behavior.

## Combat identity

Standard roles:

- Tank;
- DPS;
- Support;
- Healer;
- Control.

Engagement Style is separate from Role:

- Passive;
- Cautious;
- Balanced;
- Aggressive;
- Reckless.

Future AI also considers leash, retreat threshold, target priority, range, and threat.

## Balance hierarchy

- Stardew Valley vanilla NPCs are the canonical balance reference.
- Vanilla should be complete and balanced without expansion mods.
- Stardew Valley Expanded and Ridgeside Village are optional expansion rosters.
- Expansion NPCs use the same power budget as vanilla NPCs.
- Lore strength does not automatically grant a stronger Team Up! kit.
- Pelipper Town is planned primarily as a Linked Companion / creature-provider integration target where technically appropriate.
- Exact Pelipper Town ownership and creature mappings must be verified before implementation.

## Wiki / Codex

The Codex should separate:

- Base Game Party Members;
- Expansion Party Members;
- Companion Units;
- provider/source mod information;
- roles and affinities;
- Engagement Style;
- Passive;
- Signature Ability;
- strengths/weaknesses;
- linked companion information;
- compatibility notes.

The initial vanilla design template is:

**Primary Role + Secondary Role + Engagement Style + Passive + Signature Ability.**

## Development order

Current preferred order:

1. validate alpha.3 Party Core;
2. implement Leave Party;
3. implement Party Vault Core;
4. implement minimal controller-friendly Party Panel;
5. validate save/load and linked companion UI state;
6. only then begin combat foundation;
7. tune a small vanilla test roster;
8. expand to the full vanilla roster;
9. add SVE/Ridgeside compatibility;
10. add Pelipper Town / creature-provider integration after technical verification.
