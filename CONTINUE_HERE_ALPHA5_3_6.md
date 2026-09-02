# Team Up! alpha.5.3.6 checkpoint

Branch: `alpha5.3.6-vault-member-hint-hotfix`
Version: `0.1.0-alpha.5.3.6`
Build entry: `BUILD_ALPHA5_3_6.bat`
Expected release: `release/TeamUp_v0.1.0-alpha.5.3.6_SMOKE_TEST.zip`

## User-tested blockers addressed

### 1. Party Vault transfer failure / stuck menu
alpha.5.3.5 still failed in real testing: items could not reliably move in/out and the player could become stuck in the menu.

alpha.5.3.6 removes both `ItemGrabMenu` and `StorageContainer` transfer behavior from Team Up's Vault UI.

The new Vault menu owns exactly two vanilla `InventoryMenu` instances:
- upper: Team Up Party Vault, 36 slots / 3 rows / 12 columns;
- lower: player inventory.

Team Up routes left-click/right-click/controller actions directly to the selected inventory. There is no reward/recipe/Stardrop handling layer between them.

Controller:
- D-pad/left stick: move selection;
- crossing the top/bottom inventory boundary moves between Vault and player inventory;
- A: normal left-click behavior;
- X: right-click behavior;
- B: close safely;
- mouse OK and Escape also close.

A held item tracks its origin inventory. Closing attempts to return the held stack safely instead of trapping the player in the menu.

Persistence remains:
`FarmerTeam.GetOrCreateGlobalInventory("Ronvotri.TeamUp/PartyVault")`

### 2. Member root hints not appearing immediately
User observed that after selecting a Party NPC, the root Team Up menu opened without the contextual hints; pressing R first could make the right-side action appear.

alpha.5.3.6 pins the Party NPC name as soon as `ShowMemberMenu` opens, and dialogue-speaker resolution prefers that pinned NPC over vanilla `Game1.currentSpeaker`.

Expected immediately on member root menu:
- left: `L (Controller) / Q  Hồ sơ`
- right: `Rời đội  R (Controller) / E`

Role and Engagement submenus clear the pinned root-menu hint context.

The vanilla question-mark icon is suppressed on the Team Up member command context.

### 3. Leave Team but NPC keeps following
User reported that an NPC could be removed from the party but continue following the Farmer.

alpha.5.3.6 changes leave ordering:
1. capture any linked companion;
2. remove the Party Member from roster first;
3. release NPC movement control back to vanilla;
4. release linked companion movement control if present;
5. save.

`FollowService` also gains a released-character guard:
- `ReleaseToVanilla` marks the character released;
- Team Up update loops refuse to reacquire released characters;
- a future explicit `TakePartyControl` (recruit/redeploy) clears that guard.

This prevents stale data or a timing edge from recreating Team Up pathing after Leave Team.

### 4. Follow polish carried forward
Build-time integration also carries forward:
- update every 2 ticks;
- stop distance 1.45 tiles;
- repath threshold 0.90 tile;
- warp threshold 11 tiles;
- vanilla `PathFindController` remains the movement engine.

### 5. Codex/Profile controller fixes carried forward
The direct alpha.5.3.5 source remains in this branch:
- Character Profile footer controller navigation;
- physical-mouse-only cursor restore;
- native Codex controller routing and dropdown filters.

## Build implementation

`BuildAlpha5_3_6.ps1` runs only `ApplyAlpha5_3_6Patches.ps1` before restore/build.

The patcher is intended to be idempotent and only integrates the large `ModEntry.cs` / `FollowService.cs` changes. Vault, Codex, and Profile changes are already written directly into branch source.

## Important

- Combat AI is still NOT implemented.
- Five complete authored combat profiles remain: Abigail, Alex, Emily, Harvey, Maru.
- Other NPCs can use the profile-shell behavior.
- ChaCha and Farmer/Special Companions remain outside Main Party recruitment.
- Any Vault item loss or post-Leave ghost following is a release blocker.

See `SMOKE_TEST_ALPHA5_3_6_VI.txt`.
