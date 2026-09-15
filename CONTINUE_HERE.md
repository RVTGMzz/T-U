# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.19**

Development branch:

`v0.2-alpha6-7-44-19-pelipper-command-gate`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_19_PELIPPER_COMMAND_GATE_HANDOFF.md`

6.7.44.19 supersedes 6.7.44.18 for live testing.

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.19`
- CI source SHA: `bdc566349eedd1e111226e4da049c6e00c5d58f7`
- CI run: `34970728030`
- CI job: `104386087201`
- Artifact ID: `10396698147`
- Artifact: `team-up-alpha6-7-44-19-pelipper-native-minion-gate-fix`
- Wrapper SHA256: `40f1c3738b2709e1a1a5d7ef247c1deb747b932383dbd606dae3623bdabad71a`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.19_PELIPPER_NATIVE_MINION_GATE_FIX_TEST.zip`
- Inner ZIP SHA256: `116835726f684870792cfe357a73fe8722bd40263562779cd36119d20e13d003`
- Build: PASS, 0 warnings, 0 errors

Docs-only commits after the CI source SHA are expected.

## Live finding that caused 6.7.44.19

6.7.44.18 successfully mutated Minccino with baseHP 87 -> 261, x2 visible scale and 4 requested followers, but Pelipper rejected every native follower request because the player's `Spawn Commands` option was OFF. Team Up then timed out all four requests.

Capsakid showed the same failure earlier.

Therefore Mutation core, count and safe placement were not the problem. The blocker was only Pelipper's player-facing debug/cheat command gate.

## 6.7.44.19 fix

Team Up still uses Pelipper's own native `pokemon_spawn` callback so source/proxy identity and capture remain native. For Team Up's internal Mutation request only, it temporarily opens the Pelipper in-memory boolean whose semantic name contains both `spawn` and `command`, calls the native pipeline, then restores the exact original value in a Harmony finalizer.

It does not write Pelipper config and does not bypass wild enablement, host authority, species validation or capture rules.

## Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**.

Followers must match the creature before Mutation. No unrelated Slime fallback is allowed.

Pelipper followers must be genuine same-species Pelipper wild encounters. Vanilla/custom followers must use the same source/runtime type when safe. Unsupported custom sources fail closed.

Leader keeps HP x3, stat x2, Pelipper visible x2 cap, aura and global x3 native loot. Followers have no Mutation bonuses/aura/x3 leader reward and cannot recursively mutate.

## Immediate runtime test

Keep Pelipper's `Spawn Commands` option OFF. On a normal non-Shiny wild Pokémon:

```text
teamup_mutation force
```

Wait about one second, then:

```text
teamup_mutation status
```

Expected for an N-follower wave where N is 2-4:

- N same-species followers visible and hostile;
- no Slimes;
- native minions `pelipperNative=N`;
- `pending=0`;
- `sourceFailures=0`;
- spawn-command gate `attempts=N`;
- `gateFound=N`;
- `bypasses=N`;
- `restores=N`;
- `probeFailures=0`;
- `writeFailures=0`.

Then throw a Poké Ball at one follower. Native capture is still a live gate until proven.

After that test all three leader HP phases, final global x3 loot, one vanilla/non-Pelipper Mutation, a compatible custom monster when practical, and Lower Workings.

## Frozen locks

- Confirmed Shiny remains Mutation-excluded.
- No unrelated Slime fallback.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Preserve 20Hz Pelipper discovery and pair cache.
- Real Pelipper HP remains `WildCurrentHealth` / `WildMaxHealth`, never the proxy's 1,000,000 sentinel.
- Lower Workings still gates 6.7.45 unless explicitly waived.

## 6.7.45 lock

Planned 6.7.45 remains **Containment Chamber Escalation Encounter**. Do not start it until current runtime gates and Lower Workings pass unless the user explicitly waives them.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-19-pelipper-command-gate. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_19_PELIPPER_COMMAND_GATE_HANDOFF.md. Current verified code SHA là bdc566349eedd1e111226e4da049c6e00c5d58f7, run 34970728030. 6.7.44.18 live-fail vì Pelipper Spawn Commands OFF chặn 2-4 native followers. 6.7.44.19 tạm mở đúng in-memory spawn-command gate cho internal Mutation request rồi restore ngay. Ưu tiên live-test same-species followers với setting vẫn OFF, gate bypass/restore telemetry, capture, 3 HP phases, x3 loot, vanilla/custom regression và Lower Workings. Không bắt đầu 6.7.45 trừ khi tôi chủ động waive.`
