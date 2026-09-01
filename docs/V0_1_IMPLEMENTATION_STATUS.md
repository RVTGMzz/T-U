# Team Up! v0.1 implementation status

This file tracks what is actually implemented, separate from the broader design roadmap.

## Alpha 0.1.0-alpha.2

Implemented:

- clean standalone SMAPI project;
- `Ronvotri.TeamUp` UniqueID;
- no dependency on The Stardew Squad;
- independent party registry and save/load;
- default party size 4, clamped up to 6;
- NPC and pet detection in the party registry;
- first independent follow-state prototype;
- cross-location catch-up using Stardew Valley's own NPC warp API;
- vanilla `PathFindController` movement toward party slots;
- six simple follow-slot offsets around the player;
- R on a non-party NPC/pet: join party;
- R on a following member: Wait;
- R on a waiting member: Resume;
- party NPC schedules are released back to vanilla at day end;
- fresh English and Vietnamese strings;
- Windows one-click build script.

Current prototype limitations:

- host/single-player only for movement logic;
- no Leave Party command yet;
- no custom Party UI yet;
- follow formation is intentionally simple;
- pet movement still needs dedicated testing;
- no combat yet;
- Party Vault not implemented yet.

## Next implementation slice

After this prototype builds and moves one NPC safely, the next milestone is **Party Control + Leave + Vault foundation**:

1. add explicit Leave Party and restore NPC schedule immediately;
2. add a small Team Up party panel instead of overloading R forever;
3. add controller navigation from day one;
4. add Party Vault save model before the visual inventory screen;
5. then test pet behavior separately;
6. only after Party Core is stable, begin Tank / DPS / Support / Healer / Control combat logic.

The follow system is implemented against Stardew Valley / SMAPI APIs directly and remains independent from other companion mods.
