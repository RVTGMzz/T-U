# Team Up Alpha 6.7.19 Mutation Encounters Handoff

## Status
- Version: `0.2.0-alpha.6.7.19`
- Branch: `v0.2-alpha6-7-19-mutation-encounters`
- CI run: `34322764794`
- CI result: SUCCESS
- Materialized source commit: `da45f4e0f8a6d889c09f8f9804254a3e1c55591c`
- Artifact ID: `10092524340`
- Artifact wrapper SHA256: `3410a9a4c65e24f10baf1301347dedd00ec57ea0177aaf61dc6af8308f00f6a3`
- Inner mod ZIP: `TeamUp_v0.2.0-alpha.6.7.19_MUTATION_ENCOUNTERS_TEST.zip`
- Inner ZIP SHA256: `da9962cef6acd3223170333c64a6782af718dac6ea4582aad5826f94460eeee5`
- Main branch is intentionally NOT merged. Live Stardew verification is still required.

## Requested gameplay
A normal hostile monster gets a default 5% chance at its death moment to become a Mutant instead of dying.

Defaults:
- Mutation chance: 5%
- HP: x3
- Damage / exposed combat stats: x2
- Speed: x2 with a safety cap
- Visual scale: x3 best-effort
- Reinforcements: 2-4 normal minions
- Persistent Team Up aura while mutant is alive
- Mutation minion loot suppressed by default

## Implementation strategy
The system Harmony-patches concrete `Monster.deathAnimation` implementations. A successful roll transforms the SAME live Monster instance, restores/scales its stats, adds mutation modData, then suppresses that lethal death animation. This avoids blind reflection-cloning of third-party Monster subclasses and preserves their custom AI, sprite, NetFields and constructor-only runtime state as much as possible.

Minion spawning is deferred one tick so Team Up does not modify `location.characters` inside the death-animation call. Safe spawn placement follows the existing Surge collision discipline.

## Compatibility policy
Eligible by default:
- vanilla normal hostile monsters
- normal third-party/custom Monster subclasses
- normal Cardcha monsters on ordinary Cardcha maps

Excluded:
- already-mutated monsters
- mutation minions
- Team Up Surge monsters
- Cardcha test arena/dummy/kill-target instrumentation
- Pelipper wild/capture actors
- Pelipper-owned companion/proxy actors excluded from Team Up combat
- boss-like type/name actors
- actors with truthy modData tags containing Boss, Scripted, QuestProtected or MutationExcluded
- Team Up explicit markers `Ronvotri.TeamUp/MutationBoss` and `Ronvotri.TeamUp/MutationExcluded`

Important: Cardcha is NOT source-wide blacklisted. Normal Cardcha monsters are deliberately mutation-eligible. Cardcha boss/script encounters should eventually mark their protected actors explicitly.

## New command
- `teamup_mutation status`
- `teamup_mutation list`
- `teamup_mutation force`

`force` transforms the nearest eligible living normal monster and is intended for quick visual/stat/minion testing. It does NOT prove the real death-interception path.

## Required live tests
1. Force test: `teamup_mutation force` on a vanilla normal monster. Verify HP refill, visual enlargement where supported, aura and 2-4 minions.
2. Kill that mutant. It must die normally and never mutate recursively.
3. Deterministic death test: temporarily set `MutationChancePercent` to 100, kill a fresh normal monster, and verify it transforms at death instead of being removed or dropping loot immediately. Restore to 5 afterward.
4. Repeat deterministic death test with a normal Cardcha monster on a real gameplay map, not the test arena.
5. Verify Cardcha boss/script/test actors do not mutate.
6. Verify Pelipper wild/capture Pokemon do not mutate and capture mercy behavior is unchanged.
7. Re-run `teamup_diag_all` and existing Gunther/Aerodactyl/capture smoke tests because those remain separately unverified live issues.

## Verification boundary
CI passed source acceptance, mutation core/minion/policy audits, locked invariant carry-forward, .NET build with 0 warnings and 0 errors, and binary acceptance. This does NOT prove that Stardew or every custom monster calls `deathAnimation` at a point where suppressing it is sufficient to cancel removal/drop/death side effects. Do not mark Mutation Encounters live-verified until the 100% death test succeeds in game.

## Future polish only after live proof
- Scale mutant combat hitbox to match visual x3, if safe across monster classes.
- Same-species/custom minion adapters instead of GreenSlime fallback.
- Optional affixes such as Berserk, Armored, Vampiric, Explosive, Swarm, Arcane.
- Explicit Cardcha boss/script markers in Cardcha source.
