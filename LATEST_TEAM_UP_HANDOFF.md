# Team Up handoff: 0.2.0-alpha.6.7.44.18

Start here: `CONTINUE_HERE.md`

Canonical latest: `docs/LATEST_HANDOFF.md`

Detailed technical handoff: `docs/ALPHA_6_7_44_18_NATIVE_MINIONS_HANDOFF.md`

## Checkpoint

- Branch: `v0.2-alpha6-7-44-18-native-minions`
- Version: `0.2.0-alpha.6.7.44.18`
- CI-verified source SHA: `8c73eb93a3a7529e3d8232773e7c61c73ca9567d`
- Run: `34914882066`
- Job: `104210252539`
- Artifact ID: `10375867225`
- Artifact: `team-up-alpha6-7-44-18-native-source-minions`
- Artifact wrapper SHA256: `9aaf524f071610f64c5e93dc05dc9ad4225f586e21ca832ef41b4059e7afeebd`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.18_NATIVE_SOURCE_MINIONS_TEST.zip`
- ZIP SHA256: `81a0a16ec0890f2434fa32961dcf08242933377197348918a17f340eefa63957`
- Build: PASS, 0 warnings / 0 errors
- `main`: NOT merged
- 6.7.45: NOT started

Docs-only commits may make branch HEAD newer than the CI source SHA. The artifact above is tied to `8c73eb9...`.

## Superseded architecture

6.7.44.17 proved that lightweight followers could spawn 3/3 with `safeRejected=0` and attack normally, but the user rejected Slime followers as too generic.

6.7.44.18 supersedes that design. Do not resume the lightweight-Slime path.

## Current Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**.

Leader keeps HPx3, stat x2, aura, Pelipper visible x2 cap and global x3 native loot on final defeat.

Followers:

- correspond to the creature before Mutation;
- ordinary hostile;
- no Mutation bonuses/aura;
- no x3 leader reward;
- `MutationExcluded`;
- no unrelated fallback creature.

Pelipper followers use Pelipper's native wild spawn pipeline for the same species so they retain normal encounter identity and should be catchable through Pelipper's own Poké Ball system. This capture behavior still requires live proof.

Vanilla/custom followers require the same runtime/source type when Team Up can construct it safely. Unsupported custom types fail closed and increment `sourceFailures` instead of becoming Slimes.

## CI gates passed

- Pelipper modData HP + pair-cache carry-forward
- Pelipper visible Mutation x2 cap + restore
- Mutation 2-4 wide-spawn carry-forward
- Mutant leader + source-equivalent ordinary hostile minion policy
- Source-native minions + Pelipper native capture pipeline audit
- Global leader-only Mutant loot x3
- C# build, 0 warnings / 0 errors
- ZIP content audit

## Already live-proven

- forced Pelipper Mutation works;
- Pelipper real HP resolves from `Griff.PelipperTown/WildCurrentHealth` / `WildMaxHealth`;
- source HP writes work;
- natural non-force Mutation rolls reach the engine;
- Pelipper source/proxy pair cache works;
- visible Pelipper source scaling works;
- x2 visible scale is accepted;
- 2-4 wave placement can spawn all requested followers with `safeRejected=0`;
- ordinary follower hostility works.

## Still needs live proof in 6.7.44.18

- same-species Pelipper followers actually spawn through `pokemon_spawn`;
- no Slimes appear;
- `pelipperCommands>0`, `pelipperNative>0`, `sourceFailures=0`;
- native Pelipper follower capture works;
- no unacceptable native-wave hitch;
- three Pelipper HP phases complete correctly;
- final global x3 leader loot works live;
- vanilla same-runtime follower regression;
- one compatible custom-mod regression;
- Lower Workings runtime gate.

## Immediate test

```text
teamup_mutation force
```

Wait about one second, then:

```text
teamup_mutation status
```

For a Nidoran♂ leader, expect 2-4 ordinary Nidoran♂ followers. Then throw a Poké Ball at one follower to test native capture.

If `pokemon_spawn` is unavailable, first inspect Pelipper Town's `Pokémon spawn commands` setting or move the adapter to a direct native API/internal spawn entry point. Never restore Slime fallback.

## Carry-forward locks

- Confirmed Shiny remains Mutation-excluded.
- Shiny behavior stays frozen unless a concrete regression appears.
- Active Following/Waiting teammates cannot receive held-item vanilla gifts.
- Preserve 20Hz encounter discovery and Pelipper pair cache.
- Hidden proxy `Health/MaxHealth = 1,000,000` remains a technical sentinel, not Pokemon HP.
- Pelipper retains render/controller/ownership/capture authority for real Pelipper actors.
- Performance remains a subjective live gate.
- Lower Workings still gates 6.7.45 unless explicitly waived.

## 6.7.45 lock

Planned 6.7.45 remains **Containment Chamber Escalation Encounter**. George stays ordinary/anonymous until 6.7.46. No exact `SECTOR 17`. No final boss.
