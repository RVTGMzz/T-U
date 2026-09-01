# Team Up! v0.1 implementation status

This file tracks what is actually present in the current source tree, separate from the broader design roadmap.

> Current source version: **`0.1.0-alpha.5`**
>
> Alpha.5 source is implemented and packaged for one-click build, but it is **awaiting successful user-side compilation/runtime smoke testing**. Do not treat the newest slice as validated until that gate passes.

For session handoff and locked UX decisions, read root `CONTINUE_HERE.md` first.

## Foundation inherited from alpha.2 / alpha.3

Implemented in source:

- standalone SMAPI project;
- UniqueID `Ronvotri.TeamUp`;
- no runtime dependency on The Stardew Squad;
- independent Party Member registry and save/load;
- Main Party default size 4, configurable up to 6;
- separate Companion Unit registry;
- player main pet and NPC-linked creatures do not consume Main Party slots;
- linked companion active cap default 2, configurable 0-6;
- party states: Following, Waiting, ReturningHome, Inactive;
- companion deployment states: Standby, Active, Waiting, ReturningHome, Inactive;
- cross-location follower catch-up;
- independent follow-slot logic;
- linked-companion ownership model designed to follow the owner NPC;
- alpha save migration for the older pet-as-party-member model;
- roster/link repair on load;
- Stardew 1.6-compatible clear-tile helper in Team Up's own compatibility layer.

Important lifecycle behavior:

- roster membership persists across days;
- active following does not persist automatically into the next morning;
- NPCs are released back to vanilla behavior at day end/new-day load.

## Recruitment and management UX - alpha.3.4 baseline carried forward

Implemented in current source direction:

- recruitment shortcut while normal NPC dialogue is already open;
- **Keyboard `E`** for recruitment;
- **Controller Right Shoulder**, shown to user as `R (Controller)`;
- recruitment hint rendered immediately above the NPC dialogue box;
- keyboard `R` is not the Team Up all-purpose interaction key;
- after normal daily dialogue is exhausted, interacting again can open the Team Up Invite / Cancel recruitment conversation;
- once recruited, NPC interaction opens Team Member management rather than directly toggling movement;
- gifting preservation is considered so holding a gift does not get swallowed by Team Up management;
- member management includes Talk, Follow/Stand, Engagement, Leave Team, and newer Role/Vault/Codex entries.

Historical warning:

- alpha.3.3's direct Join / Stand / Follow one-button toggle design was a rejected UX experiment and should not be restored.

## Follow responsiveness fixes

Implemented in source after testing:

- Team Up can take movement control from a recruited NPC so a stale vanilla schedule/path controller does not leave the NPC apparently stationary;
- follower catch-up behavior was made more responsive after early tests showed NPCs trailing so slowly that following was unclear;
- release-to-vanilla handling exists for leave/day transitions;
- the removed Stardew helper `isTileLocationTotallyClearAndPlaceable` is no longer relied on directly.

Movement remains host/single-player oriented and still needs continued testing across modded maps and dense locations.

## Alpha.4 - Party Vault foundation

Implemented in source:

- real shared **Party Vault**;
- 36-slot alpha storage UI;
- openable from Team Member management;
- storage backed by Stardew Valley 1.6 native `FarmerTeam.GetOrCreateGlobalInventory(...)`;
- global inventory key: `Ronvotri.TeamUp/PartyVault`;
- item persistence is delegated to Stardew's native save/global-inventory system instead of custom lossy item snapshots.

Not implemented yet:

- auto-loot routing;
- AI consumable use;
- Party Supply rules;
- combat inventory logic.

## Alpha.5 - Party Identity & Codex

Implemented in current source:

### Role model

Five roles:

- Tank
- DPS (`PartyRole.Damage` internally)
- Support
- Healer
- Control

NPC class is not hard-locked. Current Role is mutable and saved separately from Recommended Role data.

### Vanilla-first profile model

`NpcCombatProfile` supports:

- Primary Role;
- Secondary Role;
- affinity 1-5 for all five roles;
- Recommended Engagement Style;
- Passive concept;
- Signature Ability concept.

Initial test profiles:

| NPC | Primary | Secondary | Engagement |
| --- | --- | --- | --- |
| Abigail | DPS | Control | Aggressive |
| Alex | Tank | DPS | Balanced |
| Emily | Support | Healer | Cautious |
| Harvey | Healer | Support | Cautious |
| Maru | Control | Support | Balanced |

On first recruitment of a profiled NPC, current source initializes their selected Role and Engagement from the profile recommendation, while allowing the player to change both afterwards.

### Role identity UI

Implemented in source:

- original Team Up pixel role glyphs generated in code;
- no external icon asset dependency for the alpha;
- role selector in member management;
- recommended role information can appear alongside pre-recruitment dialogue hints for profiled NPCs;
- role labels/icons are designed to work without relying on color alone.

### In-game Team Up Codex

Implemented as a Stardew-native question-dialogue skeleton for early keyboard/controller compatibility.

Current sections:

- Characters;
- Party Roles;
- Party Vault shortcut.

Character profiles can show:

- Primary / Secondary roles;
- recommended Engagement Style;
- five role affinities;
- Passive concept;
- Signature Ability concept;
- role glyphs.

Current entry paths include the Team Member menu and the configured party/Codex key direction (`P` on keyboard in the current alpha setup).

The alpha Codex is data-first. A custom book/panel UI may replace the presentation later without replacing the profile model.

## Localization

Current source includes English/default and Vietnamese strings for:

- recruitment;
- member management;
- Engagement Style;
- Party Vault;
- Role selection;
- initial Codex content/profile concepts.

The broader old addon locale list is not automatically inherited by standalone Team Up. Additional locales should be added deliberately after the English/Vietnamese interaction model stabilizes.

## Current validation status

What has been observed during earlier alpha testing:

- recruitment/follow prototypes can add NPCs and make them follow;
- early follow responsiveness was too slow and was adjusted;
- stale vanilla NPC control could prevent obvious follow behavior and was addressed;
- an old Stardew tile helper caused a real compiler error and was replaced;
- keeping active follow state across days caused NPCs to continue into the next morning and was changed to roster + inactive deployment behavior.

What still needs explicit validation on the current alpha.5 source:

1. successful `dotnet build` on the user's Stardew/SMAPI PC;
2. clean SMAPI load;
3. recruitment hint placement above dialogue;
4. E keyboard recruitment;
5. controller Right Shoulder recruitment;
6. exhausted-dialogue recruitment fallback;
7. follower catch-up and map transitions;
8. Party Member management flow;
9. Role save/change behavior;
10. Engagement save/change behavior;
11. Party Vault deposit/withdraw/reopen/save/load;
12. Codex navigation and five test profiles;
13. day transition returns members to inactive roster without auto-follow;
14. no duplicate Party Members/Companion Units.

Use `SMOKE_TEST_ALPHA5_VI.txt` as the current checklist.

## Explicitly not implemented yet

- actual combat attacks by Party Members;
- target acquisition;
- threat/aggro runtime;
- Tank taunt/intercept behavior;
- Healer recovery logic;
- Support buffs/debuffs;
- Control stun/slow/root behavior;
- DPS combat rotation;
- Engagement Style affecting combat radius/behavior;
- retreat thresholds;
- Party Strategy runtime;
- Passive/Signature Ability runtime effects;
- full vanilla profile roster;
- SVE/Ridgeside runtime roster integration;
- Pelipper Town/Pokemon provider integration;
- multiplayer-complete follower/combat networking;
- polished custom Codex book UI;
- combat Party HUD.

## Next implementation slice

Do **not** start the combat slice until alpha.5 builds and passes the core smoke test.

After alpha.5 validation, begin **v0.2 Combat Foundation** using the five vanilla role representatives only:

1. combat eligibility / safe locations;
2. enemy target acquisition;
3. Engagement Style controls engagement distance;
4. combat leash and return behavior;
5. retreat threshold;
6. minimal per-Role behavior;
7. threat/aggro prototype;
8. then Passive/Signature Ability prototypes.

Vanilla remains the canonical balance baseline. Expansion rosters come later and must fit the same party power budget.
