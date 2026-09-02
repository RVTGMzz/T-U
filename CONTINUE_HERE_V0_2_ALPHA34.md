# Team Up! v0.2.0-alpha.3.4 checkpoint

Branch: `v0.2-alpha3-4-survival-progression-equipment`
Version: `0.2.0-alpha.3.4`
Build entry: `BUILD_V0_2_ALPHA34.bat`
Expected release: `release/TeamUp_v0.2.0-alpha.3.4_SURVIVAL_PROGRESSION_EQUIPMENT_TEST.zip`

## Scope
This checkpoint deliberately combines the planned Alpha 3 and Alpha 4 layers into one user-facing test build:

1. NPC survival model;
2. Level + EXP;
3. Role Mastery;
4. 3-slot equipment;
5. equipment combat modifiers;
6. persistence and migration from older saves.

## NPC survival
Party Members now have persistent/current HP while deployed.
Role influences baseline HP/DEF and Engagement influences retreat threshold.

Retreat thresholds:
- Passive: ~50% HP;
- Cautious: ~40%;
- Balanced: ~30%;
- Aggressive: ~20%;
- Reckless: ~10%.

Wounded NPCs retreat about 10 percentage points earlier during the grace period.

NPC incoming damage is currently modeled when a live Monster is within about 1.35 tiles of the NPC. Damage uses a bounded fraction of the monster's vanilla DamageToFarmer, then subtracts Team Up DEF. This is a survival foundation while a true threat/aggro system remains a later combat layer.

## Downed lifecycle
- HP 0 does NOT kill/remove a Stardew NPC.
- Down 1/2: `DOWNED`, combat stops.
- Healer/Support can revive after a short minimum down time.
- Without a revive, NPC auto-recovers after ~12 seconds at 25% HP.
- Revive applies Wounded grace state.
- Third down in the same day: `WITHDRAWN` from combat for the rest of the day.
- Withdrawn NPC can still follow the Farmer.
- Overnight resets HP/Downed/Down count/Wounded/Withdrawn.

No permanent NPC death is introduced.

## Level + EXP
Character Level cap: 30.
EXP is participation-based, not last-hit-only:
- successful attacks grant small EXP;
- monster kills grant a larger bonus;
- healing and reviving grant EXP.

Level growth is intentionally bounded. It increases HP and combat efficiency without multiplying NPC power by MMO-scale values.

## Role Mastery
Each Party Member tracks independent mastery EXP for:
- Tank;
- DPS;
- Support;
- Healer;
- Control.

Mastery cap: 10 per role.
Using a role grants that role's mastery EXP. Changing role does not erase previous mastery.
Mastery improves the matching role's damage/healing/control/cooldown behavior in small increments.

## Equipment
Three slots:
- Weapon: melee weapons;
- Armor: Boots;
- Trinket: Ring or Trinket.

The member menu gets a native Stardew question-dialogue equipment flow, avoiding another custom item-transfer menu during this stability phase.

### Item safety architecture
Equipped items are NOT recreated from IDs during normal use.
Each NPC + slot owns a `FarmerTeam.GetOrCreateGlobalInventory(...)` inventory with one slot.
The exact Stardew `Item` object is moved there while equipped and moved back on unequip.
This preserves forge/enchant/modData/custom item state far better than serializing only an item ID.

PartyMemberData stores a combat-stat snapshot and display label; the real equipped Item object lives in the FarmerTeam global inventory.

If inventory is full, swap/unequip is blocked.
Leaving Team Up first attempts to return all equipped gear; if that cannot be done safely, Leave is blocked.

## Equipment effects
Current bounded foundation:
- Weapon: Attack bonus;
- Armor: Defense bonus;
- Ring: mixed Attack/Defense + cooldown value;
- Trinket: Heal/Control + cooldown value.

Small identity synergies:
- Abigail + Weapon: extra Control;
- Alex + Armor: extra Defense;
- Harvey + Trinket: extra Heal power;
- Maru + Trinket: extra Control;
- Emily + Trinket: extra Heal power.

These are early signature-equipment synergies, not final unique named gear.

## Combat integration
CombatService now receives ProgressionService.
Level, Mastery and Equipment influence:
- base damage;
- healing;
- Control stun duration;
- cooldowns;
- defense / NPC incoming damage.

Existing Alpha 2 feedback and signatures remain:
- heal VFX/floating HP;
- Control STUN;
- Abigail Spirit Slash;
- Alex Bodyguard;
- Harvey Emergency Care;
- Emily Prismatic Aura;
- Maru Shock Device.

## Save schema
PartySaveData schema is bumped to 4.
PartyManager build patch persists:
- Level / EXP;
- five Mastery EXP values;
- CurrentHealth;
- equipment metadata;
- Downed / Withdrawn / DownCount / Wounded state.

Older saves normalize missing Level to 1 and missing CurrentHealth to role-appropriate full HP.

## Profile / Codex
For an NPC currently in the Party, the Character Profile source/identity area is augmented with a compact progression line:
- Level;
- current/max HP;
- current Role Mastery;
- compact Weapon/Armor/Trinket summary.

The existing profile layout, Codex filters and controller routing are retained rather than replaced with a new experimental UI in this combined checkpoint.

## Farmer faint
This checkpoint does NOT override Stardew's final Farmer faint/death flow. Healer/Support can still rescue low Farmer HP before fainting, but a full Party Rescue/penalty override remains a later layer after NPC survival is stable.

## Build chain
Fresh source ZIP build order:
1. alpha.5.3.6 carry-forward;
2. alpha.5.3.7 carry-forward;
3. v0.2-alpha.1 combat;
4. v0.2-alpha.2 VFX;
5. v0.2-alpha.3+4 survival/progression/equipment;
6. i18n patch;
7. restore/build/package.

All patch stages are intended to be idempotent/state-aware.

## Do not call stable until tested
Main blockers:
- compile on the user's Stardew/SMAPI environment;
- exact equipped items survive equip/unequip;
- no item loss on full inventory;
- Level/Mastery persist across save/load;
- NPC down/revive/withdraw behavior does not fight FollowService;
- role change and gear do not create invalid CurrentHealth;
- older Alpha 2 save migration works.

See `SMOKE_TEST_V0_2_ALPHA34_VI.txt`.
