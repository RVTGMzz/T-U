# Team Up! alpha.5.3 - Codex + Social UX

## Why this checkpoint exists

Alpha.5.3 turns the Codex from a debug-like dialogue list into a real Team Up UI system and removes profile/leave actions from the party member management list.

This checkpoint is still **pre-combat**. NPC combat AI is not implemented here.

---

## Locked dialogue layout

While normal NPC dialogue is visible, Team Up adds two contextual actions attached to the top edge of the chat frame.

### Left side

`L (Controller) / Q  Profile`

- opens the profile of the NPC currently speaking;
- works before recruitment;
- remains available after recruitment;
- special/Farmer companions may expose a profile shell even when they are not recruitable.

### Right side before recruitment

`Recruit  R (Controller) / E`

Pressing it must **not** recruit immediately. It opens a confirmation question first.

### Right side after recruitment

`Leave Team  R (Controller) / E`

Pressing it must **not** remove immediately. It opens a confirmation question first.

### Special/Farmer companions

ChaCha and future Farmer-owned summons are not Main Party recruits, so they do not show the right-side Recruit action.

---

## Party member management menu

Profile and Leave Team are intentionally removed from the selectable member-menu list to reduce clutter.

The management list is now focused on tactical/party controls:

- Talk
- Follow Me / Stand Here
- Role
- Engagement
- Party Vault
- Close

When Talk opens normal NPC dialogue, the fixed Profile and Leave Team shortcuts appear around the dialogue frame.

---

## Codex is UI, not an inventory item

There is no physical Codex/book item. It should never occupy a chest or inventory slot.

Primary access paths:

1. **Social / Relationships menu**
   - Team Up adds a visible `TEAM UP CODEX` entry inside the vanilla Social tab.
   - PC: P or click.
   - Controller in alpha.5.3: X while the Social tab is open.

2. **NPC dialogue**
   - Q / Left Shoulder opens the current NPC profile directly.

3. **Global PC shortcut**
   - P while the player is free opens the character Codex.

---

## Profile -> all characters flow

Opening a profile from NPC dialogue shows that NPC first.

The profile includes an `All Characters` button that opens the full character browser.

This means the player does not need to find another NPC just to browse the Codex.

---

## Full roster status

The combat-profile catalog is **not complete yet**.

Alpha.5.3 still has five fully-authored sample profiles:

- Abigail
- Alex
- Emily
- Harvey
- Maru

Other NPCs can open a **profile shell** from dialogue. The shell shows identity/status and clearly says the combat profile is still in progress. It must not invent fake Role/Affinity/Passive/Signature data.

Future work should fill the vanilla roster, then expansion/provider rosters.

---

## Codex filters

Alpha.5.3 adds the first filterable character browser.

### Role filter

- All
- Tank
- DPS
- Support
- Healer
- Control

A role match includes Primary, Secondary, or affinity >= 3 so the browser can help players find candidates for a missing party function.

### Status filter

- All
- In Party
- Recruitable
- Not Recruited

### Source filter

Profile data now has source metadata (`SourceId`, `SourceLabel`).

Current built-in profiles use:

- `stardew-valley`
- `Stardew Valley`

Future SVE/Ridgeside/East Scarp/other providers should supply their own source metadata rather than hard-coding expansion names into Team Up core logic.

### Search

Name-text search is part of the intended final Codex UX but is **not implemented in this checkpoint**. Role/status/source filters are the alpha.5.3 foundation.

---

## Controller behavior in Codex

- D-pad Up/Down: select NPC
- A: open selected profile
- B/Back: close/back
- LB/RB: cycle Role filter
- Y in profile: All Characters

Dialogue shoulder controls only apply while the DialogueBox is active. Once the dedicated profile/Codex menu is open, shoulder buttons can safely be reused by that menu.

---

## Combat gate

Alpha.5.3 does not add:

- target acquisition;
- damage AI;
- healer AI;
- tank threat;
- support buffs;
- control CC;
- passive/signature runtime effects.

Combat Foundation starts only after the alpha.5.3 UX smoke test is build-clean and runtime-clean.
