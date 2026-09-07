# CURRENT CHAT HANDOFF — Team Up v0.2 Alpha 6.1.2

Prepared: 2026-09-03 +07:00
Repository: `ronvotri/Team-Up`
Branch: `v0.2-alpha6-1-cardcha-test-bridge-debug-presets`
Code HEAD immediately before this handoff doc: `633d21002c750e63c1bd5d6b728aad33a241e85a`

## Current checkpoint

Version: `0.2.0-alpha.6.1.2`
One-click build: `BUILD_V0_2_ALPHA6.bat`
Build script: `BuildV0_2Alpha6.ps1`
Expected ZIP:
`release/TeamUp_v0.2.0-alpha.6.1.2_CARDCHA_DEBUG_FOLLOW_GHOST_TEST.zip`

Fresh source ZIP:
`https://github.com/ronvotri/Team-Up/archive/refs/heads/v0.2-alpha6-1-cardcha-test-bridge-debug-presets.zip`

DO NOT call this compile-clean until the user's BAT succeeds or CI proves it.
Build helpers mutate extracted source. After a failed build, fresh source ZIP is safest.

## Why Alpha 6.1.2 exists

The user's Alpha 6.1.1 build failed in `_build_support/FixAlpha6UxRegressions.ps1` with:
`Alpha 6 UX hotfix could not locate dialogue profile hint restoration.`

Cause: brittle exact-block matching in the old UX helper. The obsolete helper has been removed. Build now uses:
`_build_support/FixAlpha612UxRegressions.ps1`

The new helper normalizes line endings and replaces `OpenProfileFromDialogue` by method boundary rather than exact whole block. It preserves the existing fixes for:
- Alex schedule/end-route animation reset on recruit;
- Codex selected NPC/scroll/filter preservation;
- dialogue hint restoration after returning from profile;
- Vietnamese equipment mojibake mitigation with ASCII-only JSON unicode escapes.

## New Party pass-through behavior

User also requested that party members never block the Farmer when followers catch up/warp in front.

New helper:
`_build_support/FixAlpha612PartyGhosting.ps1`

Behavior:
- while NPC is under Team Up control, `npc.farmerPassesThrough = true`;
- original `farmerPassesThrough` value is remembered per NPC;
- when NPC is released back to vanilla, the original value is restored;
- this also applies while member is Waiting/Stand Here because Team Up still owns the member;
- `collidesWithOtherCharacters` is intentionally NOT disabled, so party members do not become ghosts to monsters/vanilla NPCs.

## Follow anti-lag from Alpha 6.1.1 remains

Helper:
`_build_support/FixAlpha611FollowPerformance.ps1`

Generic behavior:
- invalid/unwalkable target tiles suspend pathfinding rather than repeatedly rebuilding PathFindController;
- repath is throttled when Farmer moves rapidly;
- intended to prevent severe lag when using a flying/fast mount over water or unreachable terrain, e.g. Aerodactyl/Pelipper Town near Wizard lake;
- not hard-coded to Aerodactyl or Pelipper Town.

Build verifies markers:
- `RepathCooldownUpdates`
- `SuspendForUnsafeTarget`

## Debug Harness / Cardcha bridge

`teamup_test` commands remain:
- `teamup_test help`
- `teamup_test arena`
- `teamup_test add <NPC>`
- `teamup_test level <NPC> <1-30>`
- `teamup_test mastery <NPC> <tank|dps|support|healer|control> <0-10>`
- `teamup_test hp <NPC> <value|percent%>`
- `teamup_test farmerhp <value|percent%>`
- `teamup_test preset <tier2|tier3|rescue|downed|fullparty>`
- `teamup_test cooldowns clear`
- `teamup_test reset`
- `teamup_test status`

Cardcha is optional. `teamup_test arena` discovers the Cardcha Region I hunting arena by map metadata `CardchaRegionRole` containing `region1-hunting`; it does not make Cardcha a required dependency.

Alpha 6.1.2 startup marker must be:
`Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.2`

If SMAPI does not show that marker, the wrong DLL/package is loaded.

## Build order now

1. `FinalizeV0_2Alpha6.ps1`
2. `FixCompileV0_2Alpha6.ps1`
3. integrate Alpha 6 signature/rescue in `BuildV0_2Alpha6.ps1`
4. `FixAlpha612UxRegressions.ps1`
5. `FixAlpha611FollowPerformance.ps1`
6. `FixAlpha612PartyGhosting.ps1`
7. `IntegrateAlpha61DebugHarness.ps1`
8. verify debug/follow/pass-through source markers
9. dotnet restore/build
10. package ZIP + `DEBUG_BUILD_INFO.txt`

## Immediate next test

Use a FRESH source ZIP and run only `BUILD_V0_2_ALPHA6.bat`.

If build fails, send full `BUILD_LOG.txt` and fix the first real error on this branch.

If build succeeds:
1. delete old `Mods/Team Up` entirely, do not overwrite;
2. install the 6.1.2 ZIP;
3. confirm SMAPI startup marker contains 6.1.2;
4. run `teamup_test help`;
5. verify profile -> Back restores hints;
6. verify Codex preserves selection/scroll/filter;
7. verify Equipment Vietnamese text is not mojibake;
8. verify Farmer can walk through Following and Waiting Team Up NPCs;
9. remove an NPC from party and verify vanilla collision is restored;
10. with 2-4 followers, use flying/fast mount over unreachable water near Wizard area and verify no severe FPS collapse, then return to land and verify catch-up resumes.

## Historical gameplay stack

Alpha 3+4: HP/defense, retreat, Downed/Revive/Wounded/Withdraw, Level/EXP, Role Mastery, Weapon/Armor/Trinket, save schema 4.

Alpha 5: runtime threat, Tank taunt/guard, anti-dogpile, role target scoring, healer/support pressure awareness.

Alpha 6: Signature skill Tier 2/3 from Level/Mastery and Farmer pre-faint Rescue.

Alpha 6.1.x: UX fixes + Cardcha test bridge/debug presets + follow performance safeguards + party pass-through.

Cardcha gameplay/card integration remains deferred.
