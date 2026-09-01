# Team Up! - CONTINUE HERE

> **Handoff checkpoint: 2026-09-01**
>
> This file is the shortest reliable way to resume development in a new chat/session. Read this first, then follow the linked design/status docs as needed.

## 1. Current baseline

- Repository: `ronvotri/Team-Up`
- Default branch: `main`
- Current project version: **`0.1.0-alpha.5`**
- UniqueID: **`Ronvotri.TeamUp`**
- Standalone SMAPI mod. **The Stardew Squad is not a dependency.**
- Latest known main-line checkpoint before this handoff: commit `e5d0cd63db2d7fbc940d2ddc5d3f261fe2407652` (`Document alpha five Party Identity and Codex architecture`).
- Alpha.5 has been written and packaged as a one-click source build, but **it has not been compile-tested in the assistant environment** because that environment does not have the .NET/Stardew build references. Do not claim alpha.5 is build-clean until the user's PC successfully builds it.

### Build entry point

Run:

```text
BUILD_ALPHA5.bat
```

Expected output:

```text
release/TeamUp_v0.1.0-alpha.5_SMOKE_TEST.zip
```

If compilation fails, use `BUILD_LOG.txt` as the source of truth. Do not infer the real compiler error from the PowerShell `throw 'dotnet build failed.'` line.

## 2. Product identity

Tagline direction:

> **Team Up! - Party & Combat Companions for Stardew Valley**

Core definition:

> Team Up! turns Stardew Valley companions into an RPG-style party with roles, tactical AI, pets/creatures, shared storage, combat strategy, and later party progression.

Primary product pillars:

1. **Party Combat**
2. **Tactical Roles & AI**
3. **Party Vault**

Design compass:

> **What role does this companion play in the party?**

Preferred terms:

- Party
- Party Member
- Companion / Companion Unit
- Party Vault
- Role
- Engagement Style
- Threat / Aggro
- Strategy
- Formation
- Skill
- Trait
- Codex

Avoid reusing The Stardew Squad terminology/class naming as an implementation template.

## 3. Clean-room boundary

This project is independently written.

Rules that must remain true:

- no code copied from The Stardew Squad;
- no assets copied from The Stardew Squad;
- do not redistribute `TheStardewSquad.dll`;
- do not copy its i18n/config/icons/sprites;
- keep independent architecture and names;
- original mod source may only have been referenced for compatibility work from an earlier addon project, not as implementation source for Team Up!;
- safe public wording: **“Team Up! codebase is independently written and contains no code or assets from The Stardew Squad.”**

See `docs/CLEAN_ROOM.md`.

## 4. Party and companion slot model

### Main Party

- Main Party contains NPC people.
- Default Main Party size: **4**.
- Configurable maximum: **6**.
- Player does not consume a slot.

### Companion Units

Companions are a separate subsystem, not hidden inside `PartyMemberData`.

- player main pet: free Main Party slot;
- NPC-linked pet/Pokemon/creature: free Main Party slot;
- default active linked-companion limit: **2**;
- configurable active linked-companion maximum: **6**;
- player main pet does not count toward the linked-companion active cap;
- extra linked companions may remain `Standby`;
- linked creature follows its **owner NPC**, not the player directly.

Do not hard-code Pelipper Town/Pokemon assumptions into core. External creature mods should eventually use a provider/adapter/profile system.

## 5. Recruitment UX - LOCKED DECISION

This section is important because an earlier alpha temporarily implemented the wrong interaction model.

### NPC is NOT in Team Up

While a normal NPC dialogue is open:

- **Keyboard:** `E`
- **Controller:** Right Shoulder, shown to the user as **`R (Controller)`**

The hint appears immediately above the NPC dialogue box:

```text
R (Controller) / E  Join Party
```

For profiled NPCs alpha.5 can also show their recommended Primary / Secondary role hint beside/near that recruitment information.

If the NPC has exhausted their normal dialogue for the day, talking/interacting with them again should become the Team Up recruitment conversation automatically:

```text
Invite Abigail to Team Up?

> Invite to Party
  Cancel
```

### Important correction

**Keyboard `R` must NOT be the Team Up all-purpose interaction key.**

An earlier alpha.3.3 experiment incorrectly made one button toggle Join / Stand / Follow. That UX is abandoned and must not be restored.

### Preserve vanilla gifting

If the player is holding a gift/item during normal interaction, Team Up should not steal the vanilla gifting behavior.

## 6. Party-member interaction UX - LOCKED DECISION

Once an NPC is already a Party Member, interaction changes from recruitment to management.

Talking/interacting with the Party Member opens the Team Up member menu. Current alpha direction includes:

```text
Team Up: Abigail

Talk
Follow Me / Stand Here
Role
Engagement Style
Party Vault
Team Up Codex
Leave Team
Close
```

Movement management lives inside the Party Member menu. It is **not** an E/R direct toggle.

Current Engagement Styles:

- Passive
- Cautious
- Balanced
- Aggressive
- Reckless

Role and Engagement are saved data. Engagement does not have combat effect yet because combat AI has not been implemented.

## 7. Day lifecycle

Roster membership and active deployment are different concepts.

At day end / new save day:

- Party roster membership is remembered;
- NPCs are released back to vanilla behavior/schedules;
- they do **not** automatically keep following the player into the next morning;
- Party Members become inactive until deployed/followed again through Team Up interaction.

This fixes the earlier bug where an NPC could continue following into the next day.

## 8. Follow system status

Implemented foundation:

- independent follow states;
- cross-location catch-up;
- vanilla pathfinding controller use;
- responsive target refresh/catch-up changes were added after testing showed followers could lag far behind;
- `TakePartyControl`/release concepts exist so vanilla schedule controllers do not block Team Up movement;
- linked companions are architected to follow their owner NPC.

Past compile issue already fixed:

- `GameLocation.isTileLocationTotallyClearAndPlaceable(...)` was unavailable in the Stardew 1.6 build references;
- Team Up now uses its own compatibility helper based on public Stardew 1.6 tile checks.

Do not regress to the removed direct helper call.

## 9. Party Vault - implemented foundation

**Party Vault is a permanent core Team Up! feature.**

Alpha.4 introduced a real shared storage UI.

Architecture:

- storage ID: `Ronvotri.TeamUp/PartyVault`;
- backed by Stardew Valley 1.6 native `FarmerTeam.GetOrCreateGlobalInventory(...)`;
- 36-slot alpha storage UI;
- opened from the Party Member menu and Codex shortcut;
- persistence/item serialization is delegated to Stardew's native global inventory/save system instead of lossy custom JSON item snapshots.

Do not replace this with simplistic `QualifiedItemId + Stack` custom serialization.

Not implemented yet:

- auto-loot;
- AI auto-consumption;
- combat supply rules.

## 10. Role system - current alpha.5

Five core roles:

- **Tank**
- **DPS** (`PartyRole.Damage` internally at present)
- **Support**
- **Healer**
- **Control**

NPCs do **not** have hard-locked classes.

Each combat profile can define:

- Primary Role;
- Secondary Role;
- affinity 1-5 for all five roles;
- Recommended Engagement Style;
- Passive concept;
- Signature Ability concept.

The player's **Current Role** is mutable and saved separately.

On first recruitment of a profiled NPC, alpha.5 initializes Current Role and Engagement from recommendations. The player can change them later.

### Alpha.5 vanilla test profiles

| NPC | Primary | Secondary | Recommended Engagement |
| --- | --- | --- | --- |
| Abigail | DPS | Control | Aggressive |
| Alex | Tank | DPS | Balanced |
| Emily | Support | Healer | Cautious |
| Harvey | Healer | Support | Cautious |
| Maru | Control | Support | Balanced |

These are Team Up original gameplay concepts, not existing vanilla mechanics.

## 11. Role icons and UI identity

Alpha.5 adds original **code-drawn pixel role glyphs**, so there is no external icon asset dependency yet.

Visual rules:

- icon silhouette + text must identify the role;
- color is supplemental only;
- role icon system should eventually become part of Team Up's visual identity;
- do not require color alone to distinguish roles.

Future polish can replace the generated glyphs with refined original pixel-art assets without changing role data.

## 12. In-game Codex - current alpha.5

Alpha.5 adds a data-first **Team Up Codex** using Stardew-native dialogue/question UI so keyboard/controller support comes early.

Entry points:

- from Party Member menu;
- `P` on keyboard when the player is free, per current config/direction.

Initial sections:

- Characters
- Party Roles
- Party Vault shortcut

Current Character profile can show:

- Primary / Secondary roles;
- Recommended Engagement Style;
- affinity for Tank / DPS / Support / Healer / Control;
- Passive concept;
- Signature Ability concept;
- role glyphs.

The native-dialogue Codex is intentionally a skeleton. A custom book/panel can replace its presentation later without replacing the profile model.

See `docs/ALPHA5_PARTY_IDENTITY.md` and `docs/WIKI_CODEX_STRUCTURE.md`.

## 13. Balance philosophy - VANILLA FIRST

This is a locked project principle.

- **Vanilla Stardew Valley is the canonical 100% balance baseline.**
- SVE, Ridgeside Village, and other expansion rosters are optional additions.
- Expansion NPCs must fit the same Team Up power budget.
- A lore-powerful NPC (for example Wizard) should not automatically become mechanically god-tier.
- balance by **party contribution**, not equal DPS.
- linked companions should contribute substantially less than a full Party Member; current design target is roughly **40-60% of a Party Member**, subject to testing.

Do Vanilla roster first, then external roster compatibility.

See `docs/BALANCE_PHILOSOPHY.md` and `docs/NPC_BALANCE.md`.

## 14. Future combat architecture already decided

Combat has **not** been implemented yet. The following are design commitments, not current runtime features.

### Threat / Aggro

Tank needs a real threat system so the role matters.

Potential threat sources:

- damage;
- healing;
- taunt;
- tank stance;
- support actions;
- proximity;
- encounter rules.

### Engagement Style

Engagement Style modifies how a selected Role executes. It does not replace Role.

Example:

- Tank + Aggressive = proactive interception/taunt;
- DPS + Aggressive = wider hunt/chase;
- Healer + Aggressive = moves into danger sooner to support, not mindless melee.

### Safety controls

Future AI should include:

- combat leash;
- retreat HP threshold;
- target priority;
- party strategy.

Conceptual leash baseline discussed:

- Cautious: ~4 tiles
- Balanced: ~7 tiles
- Aggressive: ~11 tiles
- Reckless: ~16 tiles

These numbers are design starting points only, not implemented balance constants.

## 15. External mod / Pelipper Town direction

Pelipper Town should be treated primarily as a future **Linked Companion Provider / Pokemon integration**, not as a core dependency or primary human NPC roster.

Do not hard-code Pelipper into core.

Before technical integration:

1. inspect whether it exposes a usable public API/entity model;
2. respect its permissions/license;
3. build a generic Team Up provider/adapter first.

Potential future adapter data:

```text
Species
Preferred Role
Range
Combat Style
Skill Tags
Owner
ProviderId
ProviderUnitId
```

## 16. Current important source areas

Core party data:

```text
src/TeamUp/Core/PartyManager.cs
src/TeamUp/Core/PartyMemberData.cs
src/TeamUp/Core/PartySaveData.cs
src/TeamUp/Core/CompanionUnitData.cs
src/TeamUp/Core/CompanionUnitTypes.cs
src/TeamUp/Core/PartyRole.cs
src/TeamUp/Core/EngagementStyle.cs
```

Alpha.5 profile/identity data:

```text
src/TeamUp/Core/NpcCombatProfile.cs
```

Follow system:

```text
src/TeamUp/Following/FollowService.cs
src/TeamUp/Following/GameLocationCompatibilityExtensions.cs
```

Party Vault:

```text
src/TeamUp/Storage/PartyVaultService.cs
```

Runtime/UI wiring:

```text
src/TeamUp/ModEntry.cs
src/TeamUp/ModConfig.cs
```

Localization:

```text
src/TeamUp/i18n/default.json
src/TeamUp/i18n/vi.json
```

Build/test:

```text
BUILD_TEAM_UP.bat
BuildAlpha5.ps1
SMOKE_TEST_ALPHA5_VI.txt
```

## 17. Historical alpha notes / do not regress

### alpha.3.x

Important lessons:

- a build failed because an old tile helper method was not available in current Stardew 1.6 references; fixed with compatibility helper;
- Team Up membership could succeed while NPC appeared stationary because vanilla schedule/path controller still owned the NPC; Party control/release logic was added;
- follow responsiveness needed catch-up tuning;
- NPC following persisted into next morning; fixed by separating roster from active deployment;
- alpha.3.3 used the wrong UX: keyboard/controller input became an all-purpose Join/Stand/Follow toggle. **Abandoned.**

### alpha.3.4

Corrected UX baseline:

- E keyboard / controller Right Shoulder recruits while dialogue is open;
- recruitment hint is drawn immediately above the dialogue box;
- exhausted daily dialogue can fall back to recruitment conversation;
- Party Members open management UI instead of direct movement toggles;
- gifting preservation is considered.

### alpha.4

Added real Party Vault foundation using native FarmerTeam global inventory.

### alpha.5

Added Party Identity layer:

- role selector;
- five test NPC profiles;
- recommended vs current role split;
- code-drawn role icons;
- Codex skeleton;
- EN/VI strings;
- one-click build script and smoke checklist.

## 18. Immediate next action in the next chat

**Do not jump directly into combat code before validating alpha.5.**

First gate:

```text
Build alpha.5
-> launch SMAPI cleanly
-> test normal dialogue recruitment hint
-> test E keyboard
-> test controller Right Shoulder
-> test exhausted-dialogue recruitment
-> verify follower movement/map transitions
-> verify Party Member menu
-> change Role and Engagement
-> open Party Vault and verify stored items survive reopen/save/load
-> open Codex from member menu and P
-> verify all five test NPC profiles/icons
-> sleep to next day and confirm roster remains but auto-follow does not
-> verify no duplicate NPC/party records
```

If anything fails, fix alpha.5 before adding combat.

## 19. Next development milestone after alpha.5 passes

Recommended next milestone: **v0.2 Combat Foundation**, using a small vanilla test squad first.

Suggested representatives:

- Alex -> Tank
- Abigail -> DPS
- Emily -> Support
- Harvey -> Healer
- Maru -> Control

Implementation order:

1. combat eligibility and safe location checks;
2. target acquisition;
3. Engagement Style actually changes engagement radius/behavior;
4. leash + return-to-owner/formation;
5. retreat threshold;
6. simple per-Role behaviors;
7. threat/aggro prototype;
8. only then Passive/Signature Ability prototypes.

Do not implement the full vanilla roster before the five-role combat loop is fun and stable.

## 20. Key documentation map

Read in this order when more detail is needed:

1. `CONTINUE_HERE.md` - current checkpoint and resume instructions
2. `docs/V0_1_IMPLEMENTATION_STATUS.md` - what is actually implemented
3. `docs/ALPHA5_PARTY_IDENTITY.md` - current role/icon/Codex alpha
4. `docs/DESIGN_BIBLE.md` - broad product architecture
5. `docs/BALANCE_PHILOSOPHY.md` - vanilla-first balance rules
6. `docs/NPC_BALANCE.md` - working vanilla NPC role concepts
7. `docs/WIKI_CODEX_STRUCTURE.md` - future Codex hierarchy
8. `docs/CLEAN_ROOM.md` - clean-room requirements
9. `docs/DECISION_LOG_2026-09-01.md` - earlier design decision snapshot

## 21. Communication / workflow preference for future sessions

Development workflow that has worked best:

- make concrete code changes rather than only describing them;
- update GitHub as decisions become locked;
- provide a one-click source build ZIP when local compilation is unavailable;
- if user build fails, inspect `BUILD_LOG.txt` and fix the first real compiler error;
- if runtime fails, inspect `SMAPI-latest.txt` plus the exact failed smoke-test step;
- preserve stable working behavior while adding one subsystem at a time.

**Current checkpoint ends at Team Up! v0.1.0-alpha.5 Party Identity & Codex, awaiting build/runtime validation.**
