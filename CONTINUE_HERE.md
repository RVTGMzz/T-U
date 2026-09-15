# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.18**

Development branch:

`v0.2-alpha6-7-44-18-native-minions`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_18_NATIVE_MINIONS_HANDOFF.md`

6.7.44.18 supersedes the 6.7.44.17 lightweight-Slime follower design.

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.18`
- CI-verified source SHA: `8c73eb93a3a7529e3d8232773e7c61c73ca9567d`
- CI run: `34914882066`
- CI job: `104210252539`
- Artifact ID: `10375867225`
- Artifact: `team-up-alpha6-7-44-18-native-source-minions`
- Artifact wrapper SHA256: `9aaf524f071610f64c5e93dc05dc9ad4225f586e21ca832ef41b4059e7afeebd`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.18_NATIVE_SOURCE_MINIONS_TEST.zip`
- Inner ZIP SHA256: `81a0a16ec0890f2434fa32961dcf08242933377197348918a17f340eefa63957`
- Build: PASS, 0 warnings, 0 errors

The branch HEAD can be newer because handoff docs are synchronized after the verified build. The ZIP above was produced from `8c73eb9...`.

## Mutation design lock

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**.

Leader:

- HP x3;
- stat x2;
- Pelipper visible scale capped at x2;
- Mutation aura;
- global x3 native loot on final defeat.

Followers:

- must correspond to the creature before Mutation;
- ordinary hostile;
- no Mutation bonus/aura/x3 leader reward;
- `MutationExcluded`;
- valid Team Up combat targets;
- no unrelated Slime fallback.

Pelipper followers now use genuine native same-species wild encounters so Pelipper keeps source/proxy identity and native capture authority. Vanilla/custom followers require the same runtime/source type when Team Up can create it safely. Unsupported custom sources fail closed and report telemetry.

## Live truth already proven

The 6.7.44.17 Nidoran♂ BusStop test proved:

- forced Pelipper Mutation;
- visible x2 leader scale;
- 3 requested followers can spawn 3/3;
- `safeRejected=0`;
- ordinary followers attack normally.

The rejected part was visual/source identity: those followers were lightweight Slimes. 6.7.44.18 replaces that architecture with native/source-equivalent followers.

Earlier Seadra/Fidough tests already proved Pelipper real HP binding, source/proxy pairing cache, natural Mutation roll reachability and visible source scaling.

Real Pokemon HP remains:

- `Griff.PelipperTown/WildCurrentHealth`
- `Griff.PelipperTown/WildMaxHealth`

Never use the hidden combat proxy's technical `1,000,000` Health sentinel as Pokemon HP.

## Immediate runtime test

On a normal non-Shiny Pelipper wild Pokemon:

```text
teamup_mutation force
```

Wait about one second, then:

```text
teamup_mutation status
```

Expected:

- 2-4 followers are the same Pokemon species as the leader before Mutation;
- no Slimes;
- followers attack normally;
- `pelipperCommands > 0`;
- `pelipperNative > 0`;
- `sourceFailures = 0`;
- `pending` returns to 0;
- no unacceptable hitch.

Then throw a Poké Ball at one follower. Native Pelipper capture is a runtime gate and is not considered passed until tested live.

After that test all three leader HP phases, global x3 leader loot, one vanilla/non-Pelipper Mutation and at least one compatible custom monster.

## Frozen locks

- Confirmed Shiny remains Mutation-excluded.
- Shiny behavior stays frozen unless a concrete regression appears.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Preserve 20Hz Pelipper discovery and pair cache.
- Pelipper keeps render/controller/ownership/capture authority for genuine Pelipper actors.
- Lower Workings remains unchanged and still gates 6.7.45 unless explicitly waived.

## 6.7.45 lock

Planned 6.7.45 is **Containment Chamber Escalation Encounter** in the real Lower Workings. Do not start it until current runtime gates and Lower Workings pass unless the user explicitly waives them. George stays ordinary/anonymous until 6.7.46. No exact `SECTOR 17`. No final boss.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-18-native-minions. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_18_NATIVE_MINIONS_HANDOFF.md. Current verified code SHA là 8c73eb93a3a7529e3d8232773e7c61c73ca9567d, run 34914882066. 6.7.44.18 supersedes lightweight Slime minions: follower phải là quái tương ứng trước Mutation; Pelipper dùng native same-species wild encounter và cần live-test capture. Ưu tiên same-species spawn, capture, 3 HP phases, x3 leader loot, vanilla/custom regression và Lower Workings. Không bắt đầu 6.7.45 trừ khi tôi chủ động waive.`
