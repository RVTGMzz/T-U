# CURRENT CHAT HANDOFF — Team Up v0.2 Alpha 6.1

**Prepared:** 2026-09-03 00:39 +07:00  
**Repository:** `ronvotri/Team-Up`  
**Read this file first in the next chat before changing code.**

## 1. Current source-of-truth checkpoint

- Current branch: `v0.2-alpha6-1-cardcha-test-bridge-debug-presets`
- Current HEAD at handoff creation: `03689606e781b26eb5b9570548a1480ed1f80eed`
- Project version: `0.2.0-alpha.6.1`
- One-click build: `BUILD_V0_2_ALPHA6.bat`
- Build script: `BuildV0_2Alpha6.ps1`
- Expected release ZIP: `release/TeamUp_v0.2.0-alpha.6.1_CARDCHA_TEST_BRIDGE_DEBUG_PRESETS_TEST.zip`
- Smoke test: `SMOKE_TEST_V0_2_ALPHA6_1_VI.txt`
- Fresh source ZIP:
  `https://github.com/ronvotri/Team-Up/archive/refs/heads/v0.2-alpha6-1-cardcha-test-bridge-debug-presets.zip`

**IMPORTANT:** Alpha 6.1 Cardcha bridge/debug checkpoint is NOT yet compile-verified by the user at the time of this handoff. Do not call it compile-clean until the user's BAT succeeds or CI proves it.

Build helpers currently used:

- `_build_support/FinalizeV0_2Alpha6.ps1`
- `_build_support/FixCompileV0_2Alpha6.ps1`
- `_build_support/FixAlpha6UxRegressions.ps1`
- `_build_support/IntegrateAlpha61DebugHarness.ps1`

Build order:

1. finalize stable Alpha 5 foundation
2. compile compatibility fixes
3. integrate Alpha 6 signature/rescue layer
4. apply Follow/Codex/dialogue-hint/Vietnamese i18n hotfixes
5. integrate Alpha 6.1 Cardcha arena bridge + Team Up debug harness
6. `dotnet restore`
7. `dotnet build`
8. package release ZIP

The build helpers modify source in the extracted folder. If a build fails midway, a **fresh source ZIP is safest before rerun**.

---

## 2. Non-negotiable Team Up project rules

Team Up is standalone SMAPI mod with UniqueID:

`Ronvotri.TeamUp`

**The Stardew Squad is NOT a dependency.** No Squad code/assets may be copied. Safe wording:

> Team Up! codebase is independently written and contains no code or assets from The Stardew Squad.

Core pillars:

1. Party Combat
2. Tactical Roles & AI
3. Party Vault

Design compass:

> What role does this companion play in the party?

Main Party:

- human NPCs only
- default max 4, configurable max 6
- Farmer does not consume a slot

Companion architecture:

1. Main Party Member: human NPC, invite required, slot, role/engagement/profile
2. NPC-linked Companion Unit: pet/Pokémon/creature linked to NPC owner, no Main Party slot
3. Special/Farmer Companion Unit: ChaCha/Farmer summons, no recruit hint/slot

ChaCha must remain special and never be recruitable as Main Party.

Cardcha must NOT become a required dependency. The new Alpha 6.1 bridge is optional test integration only.

---

## 3. Locked dialogue/Codex UX

Before recruit:

- left hint: `L (Controller) / Q  Hồ sơ`
- right hint: `Thu nạp R (Controller) / E`
- R/E opens confirmation, never direct recruit

After recruit:

- left hint remains Profile
- right hint becomes `Rời đội R (Controller) / E`
- R/E opens leave confirmation, never direct removal

Gifting remains vanilla when holding an item.

Member tactical menu currently includes:

- Talk
- Follow Me / Stand Here
- Role
- Engagement Style
- Equipment
- Party Vault
- Close

Global Codex key: `P`.

Codex controller design:

- D-pad left/right filters
- A opens dropdown
- up/down select
- A confirm
- B closes dropdown only
- D-pad down enters list
- no LB/RB filter cycling

---

## 4. Current gameplay stack through Alpha 6

Alpha 3+4 foundation:

- NPC HP / defense
- retreat thresholds
- Downed / Revive / Wounded / Withdraw
- Level / EXP
- Role Mastery
- Weapon / Armor / Trinket equipment
- save schema 4

Alpha 5:

- runtime threat tables per Monster
- Farmer baseline threat
- Tank threat aura + taunt
- damage/heal/control threat
- Tank Guard/intercept layer
- anti-dogpile target assignment
- role-aware target scoring
- healer/support pressure awareness

Alpha 6:

- signature skill tiers derived from Level/Mastery, no new save fields
- Tier 2: Lv10 OR active-role Mastery 4
- Tier 3: Lv20 OR active-role Mastery 8
- Farmer pre-faint Rescue
- combat polish

Original five signature progression direction:

- Abigail: Spirit Slash → Spirit Wave → Haunted Blade
- Alex: Bodyguard → Challenge → Iron Wall
- Harvey: Emergency Care → Triage → Field Hospital
- Maru: Shock Device → Chain Shock → Overload
- Emily: Prismatic Aura → Resonance → Prismatic Sanctuary

Farmer Rescue is intentionally **pre-faint**, not a Harmony override of vanilla faint/death flow. One-shot damage may still trigger vanilla faint first.

---

## 5. Bugs reported in this chat and current fixes

### A. Alex stood still / sprite animation changed after recruit

User screenshot/test showed Alex entering party then standing still with an abnormal animation/pose.

Likely cause identified: vanilla schedule/end-of-route animation remained active when Team Up took movement ownership. `npc.Halt()` alone was insufficient.

Current hotfix in `_build_support/FixAlpha6UxRegressions.ps1` changes `TakePartyControl` to reset vanilla route animation state:

- `doingEndOfRouteAnimation.Value = false`
- `nextEndOfRouteMessage = null`
- `endOfRouteMessage.Value = null`
- `Halt()`
- `Sprite.StopAnimation()`
- face current direction again

**Status:** coded, but not confirmed in-game after this final checkpoint. Retest Alex, preferably recruit while he is visibly doing a schedule animation.

### B. Codex selection reset after opening a profile

User reported: choose a character lower in Codex, open profile, go back, cursor returns to the start of the list.

Cause: profile callback recreated a new `CodexBrowserMenu`, losing selected index, scroll, filters and focus.

Current fix:

- `CodexBrowserMenu` passes its live instance into the profile callback
- profile Back / All Characters restores that exact browser instance

Expected preserved state:

- selected NPC
- scroll offset
- filters
- controller focus

**Status:** coded, needs regression test on final Alpha 6.1 build.

### C. Dialogue hints disappear after viewing an in-party NPC profile

Latest user report before handoff:

- NPC is already in party
- open Profile from NPC dialogue/member context
- go back
- restored dialogue/member menu has no Profile/Leave hints

Cause: restoring the old `DialogueBox` did not restore `RecruitHintNpcName`, which Team Up uses to identify the pinned NPC when rendering hints.

Current fix in `FixAlpha6UxRegressions.ps1`:

`OpenProfileFromDialogue` now restores `RecruitHintNpcName = npc.Name` before restoring the dialogue menu, including the callback path through All Characters.

Expected after returning:

- `L (Controller) / Q  Hồ sơ`
- `Rời đội R (Controller) / E`

**Status:** coded in current branch, NOT user-verified yet.

### D. Vietnamese equipment text mojibake

Screenshots showed examples like:

- `TRANG Bá»Š`
- `VÅ khÃ...`
- `ChÆa trang bá»‹`
- latest screenshot still visibly showed `Trang bá»‹` in the member menu

This is not considered merely a typo. Diagnosis: Windows PowerShell 5.1 may interpret UTF-8-without-BOM `.ps1` literals as ANSI, corrupting Vietnamese text inserted by build helpers.

Current mitigation in `FixAlpha6UxRegressions.ps1`:

- read/write files explicitly as UTF-8
- rewrite equipment translation values using ASCII-only JSON `\uXXXX` escapes

Expected correct strings include:

- `Trang bị`
- `TRANG BỊ`
- `Vũ khí`
- `Giáp / Giày`
- `Nhẫn / Trinket`
- `Chưa trang bị`

**Status:** coded, but do NOT mark fixed until the final Alpha 6.1 package is built and user confirms the UI no longer shows mojibake. The latest screenshot still contained mojibake, so this needs explicit retest.

---

## 6. Alpha 6.1 Cardcha Test Arena Bridge

User asked to reuse Cardcha's existing test/combat area instead of building a second Team Up Combat Lab.

Architecture decision:

- Team Up remains standalone
- Cardcha remains optional
- no Cardcha gameplay API dependency
- no copying Cardcha maps/assets into Team Up
- Team Up treats Cardcha as an optional test arena host

Cardcha UniqueID:

`Ronvotri.Cardcha`

Relevant Cardcha repo/context:

- repo: `ronvotri/Cardcha-Shardbound`
- current Cardcha branch known from this chat: `cardcha-alpha28-053-airship-upgrade-foundation`
- Cardcha broader Team Up card integration remains DEFERRED

New Team Up service:

`src/TeamUp/Debugging/TeamUpDebugService.cs`

Command:

`teamup_test arena`

Behavior:

1. checks whether `Ronvotri.Cardcha` is loaded
2. searches loaded `Game1.locations`
3. looks for map property `CardchaRegionRole` containing `region1-hunting`
4. finds an open tile near preferred arena coordinates
5. clears Team Up combat runtime
6. warps Farmer into that Cardcha arena host

The bridge deliberately does **not** hard-code Cardcha's internal location name.

If Cardcha is absent, Team Up should simply report that the arena bridge is unavailable and continue normally.

**Status:** source added, not compile/user verified yet.

---

## 7. Team Up debug commands / presets added in Alpha 6.1

Console root:

`teamup_test`

Help:

`teamup_test help`

Commands:

```text
teamup_test arena
teamup_test add <NPC>
teamup_test level <NPC> <1-30>
teamup_test mastery <NPC> <tank|dps|support|healer|control> <0-10>
teamup_test hp <NPC> <value|percent%>
teamup_test farmerhp <value|percent%>
teamup_test preset <tier2|tier3|rescue|downed|fullparty>
teamup_test cooldowns clear
teamup_test reset
teamup_test status
```

Preset intent:

### `preset tier2`

- existing party → Lv10
- active-role Mastery M4
- full HP
- clear Downed/Withdrawn/Wounded
- Following
- clear combat/signature runtime

### `preset tier3`

- existing party → Lv20
- active-role Mastery M8
- full HP
- clear lifecycle negatives
- clear cooldown/runtime

### `preset rescue`

- party Tier 3
- Alex → Tank if present
- Harvey → Healer if present
- Emily → Support if present
- Farmer HP ≈ 12%
- rescue/combat runtime cleared

Then approach/spawn monsters to test rescue priority.

### `preset downed`

- party Tier 2
- chooses a non-Healer when possible
- sets target to ~8% HP, not yet Downed
- then let monster pressure trigger Downed/Revive naturally

### `preset fullparty`

Attempts to add:

- Alex
- Abigail
- Harvey
- Maru

Then applies Tier 3. Existing party cap must still be respected.

`teamup_test add Emily` can be used separately.

---

## 8. Recommended immediate test sequence in next chat

Do not add new gameplay features until this checkpoint builds and basic regressions pass.

### Step 1 — Build

Use a fresh source ZIP from current branch and run only:

`BUILD_V0_2_ALPHA6.bat`

If build fails:

- user should upload/send the full `BUILD_LOG.txt`
- inspect the FIRST real PowerShell or `error CSxxxx`
- fix directly on this branch or a clearly named successor branch
- do not claim compile-clean before proof

### Step 2 — UI regression

1. Recruit/open member dialogue for an NPC already in party.
2. Open Profile with Q/L.
3. Go Back.
4. Verify Profile + Leave hints return immediately.
5. Open global Codex, select a character far down the list.
6. Open profile and Back.
7. Verify cursor/scroll/filter are preserved.
8. Open Equipment and verify all Vietnamese strings are correctly encoded.

### Step 3 — Alex follow regression

Recruit Alex while he is doing a vanilla schedule animation if possible.

Verify:

- he leaves the schedule animation
- returns to a normal standing/walking pose
- Follow actually moves him behind Farmer
- he does not remain frozen

If still frozen, get SMAPI log + short video and inspect Follow controller ownership instead of adding more animation guesses.

### Step 4 — Debug harness

Run:

```text
teamup_test help
teamup_test status
teamup_test preset fullparty
teamup_test preset tier3
```

Verify no exceptions and state output makes sense.

### Step 5 — Cardcha arena

With Cardcha loaded:

`teamup_test arena`

Verify it discovers the Cardcha Region I hunting map by metadata and warps successfully.

If it says the arena is not loaded, first enter/unlock/load the Cardcha Region I hunting location and retry.

### Step 6 — Combat presets

Use:

```text
teamup_test preset rescue
teamup_test preset downed
teamup_test cooldowns clear
```

Test Alpha 6 signatures/rescue without grinding levels/mastery manually.

---

## 9. Important implementation cautions

1. **Do not call this checkpoint compile-clean yet.**
2. User strongly dislikes repeated fragmented test downloads. When fixing, batch related compile/runtime issues when practical.
3. Build-time source mutation remains fragile. Longer-term, materialize the final integrated C# source and simplify the build chain when this checkpoint is stable.
4. Previous exact-block PowerShell patch failures consumed many test cycles. Avoid introducing new exact whole-block patch dependencies unless necessary.
5. When a user reruns on a folder from a failed build, warn that source may be partially transformed; fresh ZIP is safest.
6. Cardcha card-system integration into NPC combat is still postponed. Do not start per-NPC Cardcha loadouts unless user explicitly reopens that scope.
7. Cardcha bridge in this checkpoint is for **testing environment only**, not the future gameplay Cardcha API integration.

---

## 10. Historical build notes that may matter if regression returns

Earlier Alpha 3+4 build issues were caused by brittle historical patch chains and PowerShell newline/exact-block matching. Those were progressively hardened, but current branch still has consolidation helpers.

Important known compile fixes carried forward:

- `Trinket` namespace: `StardewValley.Objects.Trinkets`
- C# callback issue where `delegate(Farmer _, ...)` caused `out _` to bind to the Farmer variable instead of discard; use named `out string` variables where needed
- avoid `DialogueBox.x/y` if compile compatibility breaks; the historically compile-safe fallback is:

```csharp
int dialogueLeft = Math.Max(8, (Game1.uiViewport.Width - dialogueBox.width) / 2);
int dialogueTop = Math.Max(8, Game1.uiViewport.Height - dialogueBox.height - 64);
```

Current Follow values targeted by the consolidated foundation are approximately:

- stop: 1.45 tiles
- warp: 11 tiles
- repath: 0.90 tiles
- Follow update: every 2 ticks in the integrated build

---

## 11. Suggested first message for the next chat

User can simply send:

> Đọc `handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_1_2026-09-03.md` trên branch `v0.2-alpha6-1-cardcha-test-bridge-debug-presets` rồi tiếp tục từ checkpoint Alpha 6.1.

Next assistant should read this handoff from GitHub first, verify current branch HEAD because it may have advanced after this file was written, then continue from the latest actual repo state.
