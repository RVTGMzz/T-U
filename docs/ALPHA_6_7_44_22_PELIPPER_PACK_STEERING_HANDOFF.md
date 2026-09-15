# Team Up 0.2.0-alpha.6.7.44.22 - Pelipper Pack Steering Handoff

## Verified checkpoint

- Branch: `v0.2-alpha6-7-44-22-pelipper-pack-steering`
- Version: `0.2.0-alpha.6.7.44.22`
- CI source SHA: `1921deff08d397cca6c867fcce35f5b184c68c1c`
- CI run: `34991407821`
- CI job: `104456710100`
- Artifact ID: `10405981878`
- Artifact: `team-up-alpha6-7-44-22-pelipper-pack-steering`
- Wrapper SHA256: `60b24fdb084fc687da548569e390546fed627330219ab2cc0b55a13f6f90529b`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.22_PELIPPER_PACK_STEERING_TEST.zip`
- Inner ZIP SHA256: `51c5422cb6c456e9946290c9c65090f4b4438aae9dfbd9fa76cb2d2a260e6432`
- Build: PASS, 0 warnings, 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

Docs-only commits after the verified CI source SHA are expected.

## Live finding from 6.7.44.21

The Team Up-owned Pelipper hostility concept is valid: Ekans, Meowth and Ponyta Mutation waves visibly chased Farmer after the 6.7.44.21 PathFindController bridge was added.

However the tile-oriented controller is not acceptable as final locomotion:

- small followers can reach Farmer while the x2 Mutant leader becomes jammed behind them;
- all actors converge on nearly the same adjacent target space;
- passive/gentle Pokemon can move in an unnatural grid-NPC style;
- this is a locomotion/pack-spacing problem, not a source/proxy identity or native-spawn problem.

Therefore 6.7.44.22 supersedes the 6.7.44.21 runtime movement implementation. Do not return to tile PathFindController as the primary Pelipper Mutation chase solution.

## 6.7.44.22 design

`Alpha674422PelipperMutationSteeringService` handles only Pelipper Mutation leaders and Mutation minions.

It preserves the genuine Pelipper visible `PokemonNpc`, genuine hidden combat proxy, native encounter identity and capture path. No replacement actor is spawned.

Movement changes:

- uses Stardew low-level NPC movement flags plus `NPC.MovePosition(...)`, not a tile PathFindController;
- followers approach a small ring around Farmer instead of every actor targeting the exact same point;
- flock-style separation keeps pack members from stacking;
- followers strongly yield to the Mutant leader within a larger leader-clearance radius;
- leader only weakly yields to followers, giving the x2 leader right-of-way;
- hidden proxy has character collision disabled so it cannot physically block its own visible Pokemon;
- blocked actors detect lack of movement and temporarily sidestep perpendicular to Farmer before resuming pursuit;
- source speed is temporarily raised only during the Team Up movement step, then restored;
- contact damage still routes through `Farmer.takeDamage(..., proxy)` with the real proxy as damager and a 45-tick cooldown.

The old 6.7.44.21 hostility service remains in source history but is not instantiated by 6.7.44.22.

## New telemetry

`teamup_mutation status` now includes:

`Pelipper Mutation steering: pack-steering`

Important counters:

- `leaderMoves`
- `minionMoves`
- `separation`
- `leaderClearance`
- `sidesteps`
- `blockedFrames`
- `proxySyncs`
- `contactDamageCalls`
- `identityMisses`

`sidesteps=0` is fine in open terrain. It should increase only when an actor is actually blocked long enough.

## Immediate live test

Keep Pelipper Spawn Commands OFF.

1. Force a normal non-Shiny Pelipper Mutation with `teamup_mutation force`.
2. Prefer testing both a small species and a large/gentle-looking species when available.
3. Do not attack first. Move around and watch the pack.
4. Confirm the x2 leader can reach Farmer instead of remaining trapped behind followers.
5. Confirm followers spread around the approach instead of stacking on one line/tile.
6. Confirm movement looks less grid-like/erratic than 6.7.44.21.
7. Confirm contact still damages Farmer.
8. Run `teamup_mutation status`.

Expected:

- `leaderMoves > 0`
- `minionMoves > 0`
- `separation > 0` once the pack closes together
- `leaderClearance > 0` when followers enter the leader's clearance radius
- `proxySyncs > 0` in normal movement
- `identityMisses = 0`
- `contactDamageCalls > 0` after actual contact

If an actor visibly gets stuck, `blockedFrames` should increase and eventually `sidesteps` should increase.

Recheck one follower with a Poke Ball after movement is acceptable. Native capture must remain intact.

## Frozen Mutation contract

One encounter is 1 Mutant leader + 2-4 ordinary hostile source-equivalent followers.

Pelipper followers are genuine same-species native wild encounters. No unrelated Slime fallback is allowed. Unsupported custom sources fail closed.

Leader remains HP x3, stat x2, Pelipper visible x2 cap, aura and global native loot x3. Followers remain ordinary, Mutation-excluded, receive no Mutant bonuses/aura/x3 leader reward, and remain valid Team Up combat targets.

Pelipper Spawn Commands may remain OFF. Team Up's 6.7.44.19 internal in-memory gate opens only for its native follower request and restores the exact original setting immediately.

Real Pelipper HP remains `WildCurrentHealth` / `WildMaxHealth`, never the hidden proxy's technical sentinel HP.

## Remaining gates before 6.7.45

After 6.7.44.22 pack locomotion passes live testing:

- follower capture recheck;
- all three Pelipper Mutant HP phases;
- final global x3 leader loot;
- vanilla/non-Pelipper Mutation regression;
- compatible custom-source regression when practical;
- Lower Workings runtime gate.

Do not start 6.7.45 unless these gates pass or the user explicitly waives them.
