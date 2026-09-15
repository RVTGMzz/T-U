# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.22**

Development branch:

`v0.2-alpha6-7-44-22-pelipper-pack-steering`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_22_PELIPPER_PACK_STEERING_HANDOFF.md`

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.22`
- CI source SHA: `1921deff08d397cca6c867fcce35f5b184c68c1c`
- CI run: `34991407821`
- CI job: `104456710100`
- Artifact ID: `10405981878`
- Wrapper SHA256: `60b24fdb084fc687da548569e390546fed627330219ab2cc0b55a13f6f90529b`
- Inner ZIP SHA256: `51c5422cb6c456e9946290c9c65090f4b4438aae9dfbd9fa76cb2d2a260e6432`
- Build: PASS, 0 warnings, 0 errors

Docs-only commits after the CI source SHA are expected.

## Live truth

6.7.44.19 live-proved genuine same-species Pelipper followers with Spawn Commands OFF and no Slime fallback.

6.7.44.20 proved generic Monster pursuit flags are insufficient for Pelipper wild Pokemon.

6.7.44.21 proved a Team Up-owned movement layer can make Pelipper Mutation Pokemon chase Farmer, but the tile PathFindController approach is not acceptable: small followers can block the x2 leader, and passive/gentle species move in an unnatural grid-like way.

## 6.7.44.22 fix

`Alpha674422PelipperMutationSteeringService` supersedes the 6.7.44.21 runtime chase implementation.

It keeps the real visible Pokemon, real hidden proxy and native capture identity, but uses low-level NPC movement plus pack steering:

- followers approach a small ring around Farmer;
- pack separation reduces stacking;
- minions strongly yield to the large Mutant leader;
- leader gets right-of-way;
- hidden proxy cannot physically block its source Pokemon;
- blocked actors use short alternating sidesteps;
- contact damage still uses the real proxy as damager.

6.7.44.21 remains in source history but is NOT instantiated at runtime in 6.7.44.22.

## Immediate runtime test

Keep Pelipper Spawn Commands OFF and run:

```text
teamup_mutation force
```

Prefer one small species and one large/gentle species when practical. Do not attack first. Verify:

- leader reaches Farmer instead of getting trapped behind followers;
- followers spread rather than stack;
- movement is less grid-like/erratic;
- contact damages Farmer;
- native same-species spawn and capture remain intact.

Then:

```text
teamup_mutation status
```

Expected steering telemetry: `leaderMoves>0`, `minionMoves>0`, `separation>0` when the pack closes, `leaderClearance>0` when followers approach the leader, usually `proxySyncs>0`, and ideally `identityMisses=0`. `sidesteps` may stay 0 on open ground, but should rise after a genuine block.

## Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**. No unrelated Slime fallback.

Leader keeps HP x3, stat x2, visible Pelipper x2 cap, aura and global native loot x3. Followers match the original creature, remain ordinary and Mutation-excluded, receive no x3 leader reward, and remain genuine Pelipper wild encounters when Pelipper is the source.

## Remaining gates before 6.7.45

- 6.7.44.22 pack steering live-pass;
- follower native capture recheck;
- all three Pelipper HP phases;
- final global x3 leader loot;
- vanilla/non-Pelipper Mutation regression;
- compatible custom-source regression when practical;
- Lower Workings runtime gate.

Do not start 6.7.45 unless the user explicitly waives remaining gates.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-22-pelipper-pack-steering. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_22_PELIPPER_PACK_STEERING_HANDOFF.md. Current verified code SHA là 1921deff08d397cca6c867fcce35f5b184c68c1c, run 34991407821. 6.7.44.21 live-proved Team Up chase works nhưng PathFindController làm leader x2 dễ bị đệ chặn và Pokemon hiền di chuyển grid-like kỳ lạ. 6.7.44.22 bỏ runtime PathFindController, dùng low-level NPC movement + pack separation + leader right-of-way + blocked sidestep, vẫn giữ real source/proxy/native capture. Ưu tiên live-test locomotion nhỏ/lớn, contact damage + capture, sau đó 3 HP phases, x3 loot, vanilla/custom regression và Lower Workings. Không bắt đầu 6.7.45 trừ khi tôi chủ động waive.`
