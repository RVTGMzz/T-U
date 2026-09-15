# Continue Team Up Here

Current checkpoint: **Team Up v0.2.0-alpha.6.7.44.21**

Development branch:

`v0.2-alpha6-7-44-21-pelipper-mutation-hostility`

`main` is NOT merged. Alpha 6.7.45 has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_21_PELIPPER_MUTATION_HOSTILITY_HANDOFF.md`

## Verified build checkpoint

- Version: `0.2.0-alpha.6.7.44.21`
- CI source SHA: `9bb3a88a98c51a3695ffa9e982f3da482c08632c`
- CI run: `34976619122`
- CI job: `104405839414`
- Artifact ID: `10398993101`
- Wrapper SHA256: `c27601d8160b76edaad383ed0be7a1dcd47f0b5e4b672101e71b8b38ae41d72e`
- Inner ZIP SHA256: `7e68d423593acf32efcfa1d618bb5d5f90cfa9a0f9679e976b309ee6aaa602e9`
- Build: PASS, 0 warnings, 0 errors

Docs-only commits after the CI source SHA are expected.

## Live truth

6.7.44.19 proved native same-species Pelipper followers: a 4/4 Rockruff wave spawned with no Slime, no pending/failure/safe reject, while Pelipper Spawn Commands remained OFF.

6.7.44.20 live Growlithe test proved the generic Stardew pursuit flags were armed correctly but ineffective for Pelipper hostility. Telemetry showed `leaderArms=8`, `minionArms=67`, `pelipperProxyArms=75`, `pelipperSourceArms=75`, `damageFloors=4`, `identityMisses=0`, yet neither leader nor followers attacked Farmer.

Therefore do NOT return to repeatedly setting `focusedOnFarmers` / `moveTowardPlayer` as the main Pelipper fix.

## 6.7.44.21 fix

`Alpha674421PelipperMutationHostilityService` supplies Team Up-owned hostility only for Pelipper Mutation leaders/minions:

- path the genuine visible `PokemonNpc` toward a passable tile beside Farmer using `PathFindController`;
- repath every 12 ticks or when Farmer target tile changes;
- sync the genuine hidden Monster combat proxy to the visible source;
- on contact, call `Farmer.takeDamage(..., proxy)` with a 45-tick per-proxy cooldown;
- preserve the real source/proxy pair and native capture identity;
- leave natural non-Mutation Pelipper wilds untouched.

## Immediate runtime test

Keep Pelipper Spawn Commands OFF. On a normal non-Shiny wild Pokemon:

```text
teamup_mutation force
```

Do not attack first. Stand or move nearby and verify the Mutant leader plus 2-4 followers visibly chase Farmer and cause damage on contact. Then:

```text
teamup_mutation status
```

Expected new hostility telemetry: `pairs>0`, `leaders>0`, `minions>0`, `pathsBuilt>0`, `pathSteps>0`, usually `proxySyncs>0`, and after contact `contactDamageCalls>0`, ideally `identityMisses=0`.

Source-native spawn should remain `pelipperNative=N`, `pending=0`, `sourceFailures=0`.

Then throw a Poke Ball at one follower to explicitly recheck capture after the movement layer.

## Mutation contract

One encounter is **1 Mutant leader + 2-4 ordinary hostile source-equivalent followers**. No unrelated Slime fallback.

Leader keeps HP x3, stat x2, visible Pelipper x2 cap, aura and global native loot x3. Followers match the original creature, remain ordinary and Mutation-excluded, receive no x3 leader reward, and remain genuine Pelipper wild encounters when Pelipper is the source.

## Remaining gates before 6.7.45

- 6.7.44.21 hostility live-pass;
- follower capture live-pass after hostility layer;
- all three Pelipper HP phases;
- final global x3 leader loot;
- vanilla/non-Pelipper Mutation regression;
- compatible custom-source regression when practical;
- Lower Workings runtime gate.

Do not start 6.7.45 unless the user explicitly waives remaining gates.

## Fresh-chat resume prompt

`Tiếp tục Team Up từ CONTINUE_HERE.md trên branch v0.2-alpha6-7-44-21-pelipper-mutation-hostility. Đọc LATEST_TEAM_UP_HANDOFF.md, docs/LATEST_HANDOFF.md và docs/ALPHA_6_7_44_21_PELIPPER_MUTATION_HOSTILITY_HANDOFF.md. Current verified code SHA là 9bb3a88a98c51a3695ffa9e982f3da482c08632c, run 34976619122. 6.7.44.19 live-pass native same-species followers; 6.7.44.20 live-proved native Monster pursuit flags are armed but Pelipper wilds remain passive. 6.7.44.21 adds Team Up-owned PathFindController chase on the real PokemonNpc, syncs the real combat proxy and routes contact damage through that proxy while preserving capture identity. Ưu tiên live-test chase/contact damage + capture, sau đó 3 HP phases, x3 loot, vanilla/custom regression và Lower Workings. Không bắt đầu 6.7.45 trừ khi tôi chủ động waive.`
