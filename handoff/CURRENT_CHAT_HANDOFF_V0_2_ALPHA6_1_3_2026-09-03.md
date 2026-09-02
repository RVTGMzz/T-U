# Team Up! current chat handoff — v0.2 Alpha 6.1.3

Date: 2026-09-03
Branch: `v0.2-alpha6-1-cardcha-test-bridge-debug-presets`

## Current checkpoint

Target build: `0.2.0-alpha.6.1.3`
Expected ZIP:
`release/TeamUp_v0.2.0-alpha.6.1.3_PARTY_UX_TANK_CHACHA_TEST.zip`

Expected SMAPI marker:
`Team Up DEBUG HARNESS READY | command: teamup_test | build: v0.2.0-alpha.6.1.3`

This checkpoint is NOT compile-verified yet. User must run `BUILD_V0_2_ALPHA6.bat`. Never claim compile-clean until the user shows a successful build/log.

## User observations immediately before this checkpoint

1. ChaCha showed Team Up recruit confirmation even though ChaCha is a special Farmer/Cardcha companion and must never be recruited into Main Party.
2. User saw Abigail appear after warp, then clarified they had run `teamup_test preset fullparty`. This is expected because the preset intentionally adds Alex + Abigail + Harvey + Maru and saves the roster. Do NOT treat Abigail persistence as a warp bug.
3. Alex Tank displayed `TAUNT` but appeared to stand still instead of moving toward monsters first.
4. Party Vault Vietnamese text still showed mojibake/encoding corruption.
5. Equipment was still presented through Stardew question-dialogue rows and user said it was difficult to read.

## Alpha 6.1.3 fixes authored

### ChaCha hard exclusion
Cardcha actual native runtime ID confirmed from `ronvotri/Cardcha-Shardbound`:
`Ronvotri.Cardcha_ChaCha`
Display name: `ChaCha`

`CompanionClassificationService` now recognizes both internal ID and display name as Special/Farmer Companion.
The Alpha 6.1.3 build fixer also adds a PartyManager guard:
`CompanionClassificationService.IsSpecialName(characterName, null)`
so direct PartyManager calls cannot add ChaCha either.

Expected behavior:
- no Recruit hint for ChaCha
- R/E cannot open recruit confirmation
- old save entries with ChaCha Main Party are migrated out by existing special-member migration
- ChaCha remains outside Main Party slots

### Alex Tank approach before TAUNT
Previous `TryTankTaunt` considered monsters around Alex OR around Farmer, so Tank could taunt pressure near Farmer while still standing away from the fight.
Alpha 6.1.3 changes the behavior:
- target acquisition occurs before taunt
- TAUNT candidate radius is local to Tank only, ~5 tiles around Alex
- if target is farther away, normal combat `MoveTowardTarget` runs
- once Alex reaches local pressure radius, TAUNT can fire
- after taunt, normal movement/attack continues
- hard leash remains ~12 tiles

Also Alpha 6 Alex upgraded signature pressure radius is based on Alex position instead of remote Farmer-only radius.

### Dedicated Equipment UI
New source file:
`src/TeamUp/UI/EquipmentMenu.cs`

The Alpha 6.1.3 fixer replaces `ShowEquipmentMenu` so Equipment opens this menu instead of question-dialogue chat rows.

Design:
- left pane: Weapon / Armor-Boots / Ring-Trinket slots
- right pane: currently equipped item + Team Up stats, then eligible backpack gear
- controller: left/right changes Slot vs Bag focus, up/down selects, A equip, X unequip, B return
- mouse: click slot/item/unequip/back
- returns to Team Up member menu
- EquipmentService remains source of truth for exact Item-object storage and save behavior

### Vault + Equipment Vietnamese encoding
Alpha 6.1.3 consolidated fixer rewrites Vault and Equipment Vietnamese strings through ASCII-only JSON Unicode escapes to avoid Windows PowerShell 5.1 mojibake.

Vault expected strings:
- `KHO PARTY`
- `Vật tư dùng chung cho toàn đội`
- `ô đã dùng`
- `Thức ăn · Hồi phục · Tiện ích · Chiến lợi phẩm`

No `Æ`, `Ã`, `»`, `trá»¯`, `lÆ°u`, etc.

### Existing Alpha 6.1.2 fixes retained
- dialogue Profile -> return hint restore
- Codex browser selection/scroll/filter memory
- Alex schedule/end-of-route animation reset on party takeover
- Follow anti-thrash for flying/mount/unwalkable Farmer target tiles
- Farmer can pass through Team Up party members while they are controlled by Team Up
- original NPC `farmerPassesThrough` restored on release
- Cardcha optional test arena bridge
- `teamup_test` debug harness/presets

## Debug commands

`teamup_test help`
`teamup_test status`
`teamup_test arena`
`teamup_test add <NPC>`
`teamup_test level <NPC> <1-30>`
`teamup_test mastery <NPC> <tank|dps|support|healer|control> <0-10>`
`teamup_test hp <NPC> <value|percent%>`
`teamup_test farmerhp <value|percent%>`
`teamup_test preset tier2`
`teamup_test preset tier3`
`teamup_test preset rescue`
`teamup_test preset downed`
`teamup_test preset fullparty`
`teamup_test cooldowns clear`
`teamup_test reset`

Important: `preset fullparty` intentionally attempts Alex + Abigail + Harvey + Maru and SAVES them. A member added by this preset remaining after warp is expected.

## Current build chain

`BUILD_V0_2_ALPHA6.bat`
 -> `BuildV0_2Alpha6.ps1`
 -> `FinalizeV0_2Alpha6.ps1`
 -> `FixCompileV0_2Alpha6.ps1`
 -> Alpha 6 signature/rescue integration
 -> `FixAlpha612UxRegressions.ps1`
 -> `FixAlpha611FollowPerformance.ps1`
 -> `FixAlpha612PartyGhosting.ps1`
 -> `IntegrateAlpha61DebugHarness.ps1`
 -> `FixAlpha613PartyUxCombat.ps1`
 -> source verification gates
 -> dotnet restore/build
 -> package ZIP + SHA256

Alpha 6.1.3 build has verification gates for:
- Debug harness registration + 6.1.3 marker
- Follow anti-thrash
- Farmer-through-party collision
- ChaCha internal/display identity + PartyManager hard gate
- EquipmentMenu integration
- Vault Unicode repair
- Tank-local taunt radius

## Highest-priority next action

Ask user to download a fresh source ZIP from the branch and run `BUILD_V0_2_ALPHA6.bat`.
If build fails, request the complete `BUILD_LOG.txt` and fix all compiler/helper errors in one pass.

If build succeeds, test in this order:
1. ChaCha has no Recruit option.
2. Party Vault Vietnamese text is clean.
3. Equipment opens dedicated panel and controller navigation works.
4. Alex Tank moves toward enemy before TAUNT and continues combat after taunting.
5. Profile return hint / Codex focus regression.
6. Flying Aerodactyl over lake performance.
7. Farmer passes through following/waiting party members.
8. `teamup_test help` and Cardcha arena bridge.

## Architecture locks

- Team Up remains standalone UniqueID `Ronvotri.TeamUp`.
- Cardcha is optional test host only. No Cardcha gameplay dependency yet.
- ChaCha is Special/Farmer Companion, no invite, no Main Party slot.
- Main Party remains human NPC recruitment only.
- Do not hard-code broad Pelipper Town/Pokemon behavior into Team Up core.
- Do not claim monster visual AI is universally redirected by threat; Team Up threat/guard pressure is real gameplay logic but vanilla monster subclasses are not globally Harmony-retargeted.
