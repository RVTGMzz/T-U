# CURRENT CHAT HANDOFF — Team Up v0.2 Alpha 6.1.1

**Prepared:** 2026-09-03 +07:00  
**Repository:** `ronvotri/Team-Up`  
**Branch:** `v0.2-alpha6-1-cardcha-test-bridge-debug-presets`

Read this file FIRST in the next chat. For older project history, also see `handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_1_2026-09-03.md`.

## Current checkpoint

- Version: `0.2.0-alpha.6.1.1`
- One-click build: `BUILD_V0_2_ALPHA6.bat`
- Build script: `BuildV0_2Alpha6.ps1`
- Expected ZIP: `release/TeamUp_v0.2.0-alpha.6.1.1_CARDCHA_TEST_BRIDGE_DEBUG_PRESETS_TEST.zip`
- Smoke test: `SMOKE_TEST_V0_2_ALPHA6_1_VI.txt`
- Fresh source ZIP: `https://github.com/ronvotri/Team-Up/archive/refs/heads/v0.2-alpha6-1-cardcha-test-bridge-debug-presets.zip`

**Do NOT call Alpha 6.1.1 compile-clean yet.** User has not built this exact 6.1.1 checkpoint after the fixes below.

## Why Alpha 6.1.1 exists

User tested the earlier Alpha 6.1 package and SMAPI returned:

```text
[SMAPI] Unknown command 'teamup_test'; type 'help' for a list of available commands.
```

for `teamup_test arena`, `teamup_test help`, and `teamup_test add Emily`.

Inspection confirmed `TeamUpDebugService.RegisterCommands()` and `DebugTools.RegisterCommands()` exist in the intended integration code. The most likely packaging/runtime confusion was that the earlier follow/codex/i18n hotfix and the Cardcha bridge build both used version `0.2.0-alpha.6.1`, making it too easy to install an old DLL while SMAPI still reported the same version.

Alpha 6.1.1 therefore adds hard identification and verification:

- csproj version bumped to `0.2.0-alpha.6.1.1`
- ZIP name bumped to `...alpha.6.1.1...`
- package contains `DEBUG_BUILD_INFO.txt`
- startup must log:

```text
Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.1
```

- build verifies `ModEntry.cs` contains both `DebugTools.RegisterCommands();` and the startup marker before compile
- build log must include `Debug harness source verification: OK`

If startup marker is absent after installing 6.1.1, do not troubleshoot command syntax. The game is not loading the intended DLL or startup failed earlier. Request full SMAPI log.

## New runtime bug reported immediately after command issue

User reported severe lag when Team Up has followers and the Farmer uses Aerodactyl from Pelipper Town to fly across the lake near the Wizard area.

Diagnosis from `FollowService`:

- Follow runs every 2 ticks.
- `FindOpenNear` can fail to find a legal walkable target around a Farmer flying over water/cliffs and then returns the preferred tile anyway.
- `FollowTarget` could repeatedly clear/recreate `PathFindController` as the fast-moving target changed by >= 0.90 tile.
- With multiple party NPCs this can create a pathfinding storm against unreachable water/terrain targets and cause severe CPU/FPS loss.

Do NOT hard-code Aerodactyl or Pelipper Town behavior. The fix is generic for mounts, modded traversal, water, cliffs and other unreachable target tiles.

## Alpha 6.1.1 generic Follow anti-thrash fix

New helper:

`_build_support/FixAlpha611FollowPerformance.ps1`

It runs after UX fixes and before Debug Harness integration.

Behavior added to final `FollowService`:

1. `FollowTarget` checks the final target tile with `IsOpen` BEFORE asking `PathFindController` to solve it.
2. Invalid/unwalkable target:
   - clear owned path once
   - restore base speed
   - halt follower at safe current position
   - keep follower in an unsafe-target suspended set
   - do not create pathfinding work toward water/cliff/invalid tiles
3. When a valid target returns:
   - remove unsafe suspension
   - clear retry cooldown
   - immediately resume normal follow/catch-up
4. Repath throttle:
   - `RepathCooldownUpdates = 6`
   - target churn cannot rebuild a path every Follow update
   - constructor failures use `UnsafeTargetCooldownUpdates = 15`
5. Existing warp/catch-up logic remains available for genuinely large distances once a legal target exists.
6. `IsOpen` now fails safely for NaN/negative/exceptional target tiles.

Build verifies final `FollowService.cs` contains `RepathCooldownUpdates` and `SuspendForUnsafeTarget` before compile. Build log must include:

```text
Follow anti-thrash source verification: OK
```

Expected gameplay while flying over a broad lake:

- followers may wait at the shore while Farmer is over an unreachable area
- FPS should not collapse from pathfinding spam
- when Farmer returns to walkable terrain, party should resume/catch up automatically

This behavior is intentional and preferable to warping NPCs onto water.

## Current build helper order

1. `_build_support/FinalizeV0_2Alpha6.ps1`
2. `_build_support/FixCompileV0_2Alpha6.ps1`
3. Alpha 6 signature/rescue integration in `BuildV0_2Alpha6.ps1`
4. `_build_support/FixAlpha6UxRegressions.ps1`
5. `_build_support/FixAlpha611FollowPerformance.ps1`
6. `_build_support/IntegrateAlpha61DebugHarness.ps1`
7. source verification for Debug Harness + Follow anti-thrash
8. `dotnet restore`
9. `dotnet build`
10. package ZIP

Build helpers mutate the extracted source. After any failed/partial build, use a fresh source ZIP before rerun.

## Existing Alpha 6.1 UX fixes still included and need retest

### Alex schedule animation takeover

`FixAlpha6UxRegressions.ps1` clears vanilla route animation state when Team Up takes control:

- `doingEndOfRouteAnimation.Value = false`
- `nextEndOfRouteMessage = null`
- `endOfRouteMessage.Value = null`
- halt / stop animation / face direction

Needs user retest.

### Codex focus memory

Opening a profile from Codex now preserves/restores the live Codex browser instance, so selected NPC, scroll, filters and controller focus should remain.

Needs user retest.

### Dialogue hint restoration

When opening a profile from an in-party NPC dialogue/member context and going Back, the callback restores `RecruitHintNpcName = npc.Name` before restoring the old DialogueBox.

Expected hints after return:

- `L (Controller) / Q Hồ sơ`
- `Rời đội R (Controller) / E`

Needs user retest.

### Vietnamese equipment mojibake

Equipment i18n hotfix rewrites Vietnamese values through ASCII-only JSON Unicode escapes to avoid Windows PowerShell 5.1 UTF-8 literal corruption.

Expected correct strings include `Trang bị`, `TRANG BỊ`, `Vũ khí`, `Chưa trang bị`, `Giáp / Giày`, `Nhẫn / Trinket`.

Latest user screenshot before this build still showed mojibake, so do not mark fixed until retested on the new package.

## Team Up Debug Harness commands

Root command: `teamup_test`

```text
teamup_test help
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

Cardcha remains OPTIONAL. `teamup_test arena` checks `Ronvotri.Cardcha` and discovers the loaded arena by map metadata `CardchaRegionRole` containing `region1-hunting`. It does not call Cardcha gameplay/card APIs and does not hard-code the internal Cardcha location name.

## Immediate next test sequence

1. Delete the old `Mods/Team Up` folder completely. Do not copy over an old DLL.
2. Download a fresh ZIP of the branch and run only `BUILD_V0_2_ALPHA6.bat`.
3. Build log must show:
   - `Debug harness source verification: OK`
   - `Follow anti-thrash source verification: OK`
4. Install release ZIP `TeamUp_v0.2.0-alpha.6.1.1_CARDCHA_TEST_BRIDGE_DEBUG_PRESETS_TEST.zip`.
5. Confirm `DEBUG_BUILD_INFO.txt` exists in the installed Team Up folder.
6. Start SMAPI and confirm startup marker `Team Up DEBUG HARNESS READY ... 6.1.1`.
7. Run `teamup_test help` first. If unknown command remains despite startup marker, collect full SMAPI log because that would be a different registration/runtime failure.
8. With Cardcha loaded, run `teamup_test arena`.
9. Reproduce flying performance bug with 2–4 followers over the lake/unwalkable traversal area. Verify FPS no longer collapses and party catches up after returning to walkable land.
10. Retest Alex follow, Codex focus memory, dialogue hints and Vietnamese equipment text.

## Project invariants

- Team Up UniqueID: `Ronvotri.TeamUp`
- standalone mod
- The Stardew Squad is NOT a dependency and no code/assets may be copied
- Cardcha card-system gameplay integration is still deferred
- Cardcha bridge here is test-environment-only
- ChaCha remains special and cannot be recruited as a Main Party member
- Main Party is human NPCs, default max 4 configurable max 6, Farmer does not consume a slot

## Suggested first message in next chat

> Đọc `handoff/CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_1_1_2026-09-03.md` trên branch `v0.2-alpha6-1-cardcha-test-bridge-debug-presets` rồi tiếp tục từ Alpha 6.1.1.
