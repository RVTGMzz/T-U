# Team Up! alpha.5.3.7 checkpoint

Branch: `alpha5.3.7-native-vault-special-lifecycle`
Version: `0.1.0-alpha.5.3.7`
Build entry: `BUILD_ALPHA5_3_7.bat`
Expected release: `release/TeamUp_v0.1.0-alpha.5.3.7_SMOKE_TEST.zip`

## User-tested blockers addressed

### 1. Party Vault native-feel controls
The custom two-inventory Vault from alpha.5.3.6 is retained, but alpha.5.3.7 adds normal chest ergonomics without reintroducing ItemGrabMenu reward behavior.

Mouse/keyboard:
- normal left/right click transfer behavior remains;
- Shift + click quick-transfers a whole selected stack between player inventory and Party Vault;
- Fill Stacks button uses Stardew's vanilla chest icon and only tops up stacks which already exist in Party Vault;
- Organize button uses Stardew's vanilla organize icon and compacts/sorts Party Vault;
- OK / Escape close safely.

Controller:
- D-pad / left stick navigates both inventories;
- Y quick-transfers the selected stack to the opposite inventory;
- Right from the last inventory column enters the side-button rail;
- A activates Fill Stacks / Organize / OK;
- X keeps right-click/split-stack behavior;
- B closes safely.

Persistence stays on:
`FarmerTeam.GetOrCreateGlobalInventory("Ronvotri.TeamUp/PartyVault")`

### 2. Member root hints appear together
The Party Member root menu pins the selected NPC and clears stale confirmation state before creating the question dialogue.

Expected in the first rendered frame of the Team Up member menu:
- left: `L (Controller) / Q  Hồ sơ`
- right: `Rời đội  R (Controller) / E`

No preliminary R/E press should be needed.

### 3. Leave Team resumes vanilla life
alpha.5.3.6 prevented ghost-follow reacquisition, but a released NPC could remain standing because Team Up had disabled their schedule and then simply removed its own path controller.

alpha.5.3.7 adds `ReleaseToVanillaAndResumeSchedule` for explicit Leave Team:
- clear Team Up path ownership;
- restore base movement speed;
- re-enable vanilla schedule flags;
- find the latest schedule destination at or before the current in-game time;
- if the destination is in the same map, path toward it;
- if it is on another map or path creation fails, place the NPC at the current schedule destination;
- set `lastAttemptedSchedule` to current time so later vanilla schedule entries can continue normally.

Linked companions are released separately without forcing a villager schedule onto them.

### 4. ChaCha and Farmer summons are Special Companions
ChaCha is now a built-in product rule inside `CompanionClassificationService`, not merely a config default.

This means:
- ChaCha can never be a Main Party recruit candidate;
- ChaCha never consumes a Main Party slot;
- existing user config cannot accidentally make ChaCha recruitable;
- old save data containing ChaCha in Main Party is migrated out on SaveLoaded.

Generic future compatibility remains data-driven through:
`Ronvotri.TeamUp/CompanionKind`

Recognized special values:
- `FarmerCompanion`
- `FarmerSummon`
- `SpecialCompanion`

NPC-linked creatures continue using:
- `LinkedCompanion`

### 5. Build flow
The downloaded branch contains source which already has the alpha.5.3.7 Vault and special-classification changes.

`BuildAlpha5_3_7.ps1`:
1. applies alpha.5.3.6 integration only when ModEntry is still at the pre-integrated alpha.5.3.3 source state;
2. applies alpha.5.3.7 lifecycle integration only when needed;
3. can be run again without requiring a fresh source extraction after a successful first build;
4. restores/builds/packages the mod.

## Important

- Combat AI is still NOT implemented.
- Five complete authored combat profiles remain Abigail, Alex, Emily, Harvey and Maru.
- Other NPCs use profile-shell behavior until their provider/profile is authored.
- Any Vault item loss, ChaCha appearing as recruitable, ghost following after Leave Team, or NPC freezing after Leave Team is a release blocker.

See `SMOKE_TEST_ALPHA5_3_7_VI.txt`.
