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

## Invite flow for Linked Companions

If an integration reports that an invited NPC has a Linked Companion, Team Up! should ask whether to bring that companion too.

The NPC still consumes one Main Party slot whether or not the Linked Companion is deployed.

## Recruitment UX - locked after alpha.3.3 correction

For an NPC who is **not yet in Team Up**:

- while normal NPC dialogue is open, **Keyboard E** is the recruitment shortcut;
- controller recruitment uses **Right Shoulder**, displayed to the player as `R (Controller)`;
- the recruitment hint appears immediately above the NPC dialogue box;
- keyboard `R` is not the Team Up all-purpose interaction key;
- if today's normal dialogue has been exhausted, interacting with the NPC again should automatically become the Team Up Invite / Cancel recruitment conversation;
- vanilla gifting should not be swallowed by Team Up when the player is holding a gift/item.

The alpha.3.3 experiment where one key directly toggled Join / Stand / Follow is rejected and must not be restored.

## Party Member interaction UX - locked direction

Once an NPC is already a Party Member, interaction switches from recruitment to Team Up management.

The member-management surface should contain or lead to:

- Talk;
- Follow / Stand Here;
- Current Role;
- Engagement Style;
- Party Vault;
- Team Up Codex;
- Leave Team;
- Close.

Follow/Stand is managed through Team Member UI, not as a direct E/R state toggle.

## Day lifecycle

Roster membership is distinct from active deployment.

- roster membership persists across days;
- active Following should end at day transition;
- NPCs return to vanilla schedule/behavior;
- next morning they should not automatically continue following;
- Party Members can remain in the roster as inactive until deployed again.

## Party Vault

- Party Vault is retained as a permanent core feature.
- It is a shared team inventory, not one chest per follower.
- Alpha.4 uses Stardew Valley 1.6 native `FarmerTeam.GetOrCreateGlobalInventory(...)` for persistence.
- The current global inventory ID is `Ronvotri.TeamUp/PartyVault`.
- The alpha storage UI uses 36 slots.
- Do not replace native item persistence with simplistic QualifiedItemId/Stack snapshots.
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

## Recommended Role vs Current Role

NPCs should not be permanently class-locked.

A Team Up combat profile can define:

- Primary Role;
- Secondary Role;
- affinities for all five roles;
- Recommended Engagement Style;
- Passive concept;
- Signature Ability concept.

The player's **Current Role** is mutable and saved separately. Recommended Role communicates natural fit, not a class restriction.

## Role icons / Party Identity

- Team Up should use a recognizable role-icon language so players can identify party function quickly.
- Role identification must use icon silhouette + text; color alone is not sufficient.
- Alpha.5 starts with original code-generated pixel glyphs instead of external art assets.
- Polished original pixel art may replace the glyphs later without changing role/profile data.

## Alpha.5 vanilla test profiles

The initial five-role test set is:

| NPC | Primary | Secondary | Recommended Engagement |
| --- | --- | --- | --- |
| Abigail | DPS | Control | Aggressive |
| Alex | Tank | DPS | Balanced |
| Emily | Support | Healer | Cautious |
| Harvey | Healer | Support | Cautious |
| Maru | Control | Support | Balanced |

These are Team Up gameplay concepts, not existing vanilla mechanics.

## Balance hierarchy

- Stardew Valley vanilla NPCs are the canonical balance reference.
- Vanilla should be complete and balanced without expansion mods.
- Stardew Valley Expanded and Ridgeside Village are optional expansion rosters.
- Expansion NPCs use the same power budget as vanilla NPCs.
- Lore strength does not automatically grant a stronger Team Up! kit.
- Pelipper Town is planned primarily as a Linked Companion / creature-provider integration target where technically appropriate.
- Exact Pelipper Town ownership and creature mappings must be verified before implementation.
- Linked Companion combat contribution should be meaningfully below a full Party Member; current design target is roughly 40-60% before testing/tuning.

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

Alpha.5 adds the first in-game Codex skeleton using Stardew-native dialogue UI so controller support is available before a bespoke book interface is built.

Initial alpha.5 Codex sections:

- Characters;
- Party Roles;
- Party Vault shortcut.

The long-term custom book/panel may replace presentation without changing the underlying profile model.

## Current implementation checkpoint

Current source line at the end of this session is **v0.1.0-alpha.5 Party Identity & Codex**.

Alpha.5 source includes:

- corrected recruitment/member-management UX from alpha.3.4;
- roster-vs-active day lifecycle;
- Party Vault foundation from alpha.4;
- mutable Role selection;
- five vanilla profile records;
- code-drawn role glyphs;
- in-game Codex skeleton;
- English/Vietnamese alpha strings;
- one-click build script and smoke checklist.

The newest alpha still requires successful user-side build/runtime validation before combat development begins.

## Revised development order

Current preferred order after this session:

1. build and smoke-test alpha.5 on the user's Stardew/SMAPI PC;
2. fix any compiler/runtime regressions before adding systems;
3. validate recruitment hint, E keyboard, controller Right Shoulder, exhausted-dialogue invite, follower movement, day transition, member menu, Role, Engagement, Party Vault, and Codex;
4. only after alpha.5 passes, begin **v0.2 Combat Foundation**;
5. use only the five vanilla role representatives at first;
6. implement combat eligibility and target acquisition;
7. make Engagement Style affect combat engagement distance;
8. implement leash/return and retreat behavior;
9. implement minimal Tank/DPS/Support/Healer/Control behaviors;
10. prototype real threat/aggro;
11. then add Passive/Signature Ability runtime effects;
12. tune the five-role loop before expanding to the full vanilla roster;
13. add SVE/Ridgeside compatibility only after vanilla is stable;
14. add Pelipper Town / creature-provider integration after technical/API/license verification.

For the complete current-session handoff, read root `CONTINUE_HERE.md`.
