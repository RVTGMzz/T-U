# Team Up! v0.2.0-alpha.1 checkpoint

Branch: `v0.2-alpha1-full-codex-combat`
Version: `0.2.0-alpha.1`
Build entry: `BUILD_V0_2_ALPHA1.bat`
Expected release: `release/TeamUp_v0.2.0-alpha.1_COMBAT_TEST.zip`

## Milestone
This is the first checkpoint where Main Party NPCs perform real combat against Stardew Valley `Monster` instances instead of only following the Farmer.

## Full vanilla Codex pass
`NpcProfileCatalog` now contains authored Team Up profiles for 29 supported vanilla adult human NPCs:
Abigail, Alex, Caroline, Clint, Demetrius, Elliott, Emily, Evelyn, George, Gus,
Haley, Harvey, Jodi, Kent, Leah, Lewis, Linus, Marnie, Maru, Pam, Penny, Pierre,
Robin, Sam, Sandy, Sebastian, Shane, Willy, Wizard.

Every authored profile contains:
- Primary role;
- Secondary role;
- recommended Engagement Style;
- five role affinities;
- Passive identity text;
- Signature Ability identity text;
- Stardew Valley source metadata for Codex filtering.

Unknown/mod NPCs retain the provider/profile-shell path. If they are recruitable and enter Main Party, v0.2 combat has a conservative fallback role/affinity so they can still fight before a provider profile exists.

## Main Party classification
Main Party remains adult-human oriented.
Built-in non-Main-Party vanilla names: Jas, Vincent, Leo, Dwarf, Krobus.
ChaCha remains a built-in Farmer/Special Companion and can never consume a Main Party slot.
The `Ronvotri.TeamUp/CompanionKind` contract remains the preferred path for Farmer Summons, Special Companions and Linked Companions.

## Real combat loop
New file: `src/TeamUp/Combat/CombatService.cs`.

Combat only exists when real `Monster` instances are present in the Farmer's current location.
Active Party Members in `Following` state:
1. acquire a valid monster inside their Engagement radius;
2. temporarily leave formation control;
3. path toward an attack position when needed;
4. face and attack the target;
5. apply real monster damage through Stardew's location combat API;
6. return to formation following when no valid target remains.

### Engagement radii
- Passive: ~2.75 tiles, defensive only near Farmer.
- Cautious: ~4.5.
- Balanced: ~6.5.
- Aggressive: ~8.5.
- Reckless: ~10.5.

Hard leash: 12 tiles from Farmer.

### Role behavior in alpha one
- DPS: fastest/highest basic damage, close range.
- Tank: prioritizes monsters closest to Farmer and uses high knockback.
- Control: mid-range attack with strongest knockback.
- Support: mid-range lower damage; small recovery when Farmer is sufficiently hurt.
- Healer: prioritizes real Farmer HP recovery; emergency heal scales with Healer affinity, plus light combat attack.

Role affinity affects basic combat efficiency without turning lore-powerful characters into god-tier units.

### Not implemented yet
- dedicated NPC HP/damage-taking model;
- true monster threat table/aggro redirect to Tank;
- unique per-NPC Passive execution;
- unique per-NPC Signature Ability execution;
- companion/summon combat adapters;
- advanced ranged projectile visuals.

Those are v0.2 follow-up layers, not claims of this checkpoint.

## Follow/combat ownership
`ApplyV0_2Alpha1Patches.ps1` adds explicit combat-control ownership to `FollowService`.
Formation following skips NPCs while CombatService owns their movement, preventing both systems from fighting over `npc.controller`.
Disengaging clears combat pathing so normal formation following can resume on the next follow update.

## Build chain
A fresh ZIP source still carries the alpha.5.3.6 and alpha.5.3.7 integration patchers.
`BuildV0_2Alpha1.ps1` applies only the missing stages in order:
1. alpha.5.3.6 integration if source is still at alpha.5.3.3;
2. alpha.5.3.7 lifecycle integration;
3. v0.2-alpha.1 combat integration;
4. restore/build/package.

The script is state-aware so a second build doesn't deliberately reapply an already-integrated stage.

## Release blockers
Do not call this checkpoint stable until real-game smoke testing confirms:
- no compile errors;
- monsters lose HP and die normally from party attacks;
- drops still occur;
- combat does not fight formation pathing;
- leave-team stops combat/follow;
- Healer doesn't heal above max HP;
- Vault remains lossless;
- ChaCha is never Main Party recruitable.

See `SMOKE_TEST_V0_2_ALPHA1_VI.txt`.
