# Team Up! v0.1 implementation status

This file tracks what is actually implemented, separate from the broader design roadmap.

## Alpha 0.1.0-alpha.1

Implemented:

- clean standalone SMAPI project;
- `Ronvotri.TeamUp` UniqueID;
- no dependency on The Stardew Squad;
- independent party registry;
- save/load of party membership;
- default party size 4, clamped up to 6;
- NPC and pet detection in the party registry;
- fresh English and Vietnamese strings;
- keyboard/controller-safe SMAPI keybind config surface;
- Windows one-click build script.

Prototype input:

- Face an NPC or pet and press `R` to register them as a Team Up! party member.

Not implemented yet:

- physical follow movement;
- cross-map following;
- Wait / Resume;
- Leave Party flow;
- Party UI;
- Party Vault UI/storage;
- combat;
- roles in gameplay;
- threat/aggro;
- skills;
- Codex.

## Next implementation slice

The next code milestone is the **Follow State Machine**:

1. resolve saved party member names back to live game characters;
2. define Following / Waiting state transitions;
3. move companions without stealing player input or freezing game state;
4. handle player map transitions safely;
5. restore vanilla NPC behavior when a member leaves the party;
6. test one NPC first, then pets, then multiple party members.

The follow system should be implemented against Stardew Valley / SMAPI behavior directly and remain independent from other companion mods.
