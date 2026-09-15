# Team Up latest handoff: 0.2.0-alpha.6.7.44.22

Start here: `CONTINUE_HERE.md`

Detailed handoff: `docs/ALPHA_6_7_44_22_PELIPPER_PACK_STEERING_HANDOFF.md`

## Current checkpoint

- Branch: `v0.2-alpha6-7-44-22-pelipper-pack-steering`
- Version: `0.2.0-alpha.6.7.44.22`
- CI source SHA: `1921deff08d397cca6c867fcce35f5b184c68c1c`
- Run: `34991407821`
- Job: `104456710100`
- Artifact ID: `10405981878`
- Wrapper SHA256: `60b24fdb084fc687da548569e390546fed627330219ab2cc0b55a13f6f90529b`
- Inner ZIP SHA256: `51c5422cb6c456e9946290c9c65090f4b4438aae9dfbd9fa76cb2d2a260e6432`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

## Current live truth

6.7.44.19 proved native same-species Pelipper followers can spawn with Spawn Commands OFF and no Slime fallback. 6.7.44.20 proved generic Monster aggro flags do not make Pelipper wild Pokemon hostile.

6.7.44.21 proved Team Up-owned chase can move real Pelipper Pokemon toward Farmer, but its tile PathFindController implementation has bad pack locomotion: followers can jam the x2 leader and passive/gentle species look grid-like or erratic.

## 6.7.44.22

`Alpha674422PelipperMutationSteeringService` replaces the 6.7.44.21 runtime chase with low-level Stardew NPC movement. It preserves the real visible Pokemon, real hidden proxy and native capture identity while adding follower attack-ring targets, pack separation, leader right-of-way, hidden-proxy non-collision, blocked sidestep recovery and proxy-based contact damage.

The 6.7.44.21 service remains in source history but is not instantiated in 6.7.44.22.

Immediate live gate: test one small species and one large/gentle species. Expect `leaderMoves>0`, `minionMoves>0`, separation/leader-clearance counters when the pack closes, usually `proxySyncs>0`, ideally `identityMisses=0`, and `contactDamageCalls>0` after actual touch. Recheck native follower capture.

After locomotion passes, continue 3 HP phases, x3 final loot, vanilla/custom regression and Lower Workings. Do not begin 6.7.45 unless remaining runtime gates are passed or explicitly waived.
