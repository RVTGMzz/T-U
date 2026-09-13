# Team Up 0.2.0-alpha.6.7.44.12 - Pelipper modData HP Binding

## Source of truth
- Branch: `v0.2-alpha6-7-44-12-pelipper-moddata-hp-binding`
- Version: `0.2.0-alpha.6.7.44.12`
- CI input SHA: `c79916d0718c567975fd028f9b0f17f674cda015`
- Successful run: `34753394198`
- Successful job: `103713602860`
- Artifact ID: `10316283846`
- Artifact wrapper SHA256: `5af6dc98d56474889ac2c4e72387895d1dd50930e93eb53d0795d37fdbf47da5`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.12_PELIPPER_MODDATA_HP_BINDING_TEST.zip`
- Inner ZIP SHA256: `83f80afa3acd28c33aa50287b0ba454ad6be5ba77715e6e69be26b9484927912`
- Build: PASS, 0 warnings / 0 errors
- `main` not merged
- 6.7.45 not started

## Live truth from 6.7.44.11
The dual probe resolved a normal wild Taillow to a visible `PelipperTown.PokemonNpc` and exposed the authoritative combat HP on the hidden Pelipper proxy modData:

- `Griff.PelipperTown/WildMaxHealth=66`
- `Griff.PelipperTown/WildCurrentHealth=66`

The same proxy still had `Monster.Health=1000000` and `Monster.MaxHealth=1000000`, proving those Monster values are technical sentinel/controller HP and must never be treated as Pokemon HP.

The pair cache also behaved correctly during this test: species pairs resolved, cache hits increased, and there were no cache invalidations or repeated per-pair log floods.

## 6.7.44.12 implementation
New service:
`src/TeamUp/Core/Alpha674412PelipperModDataHpBindingService.cs`

It patches the existing 6.7.44.8 source-aware Mutation HP resolver at highest priority and binds the exact Pelipper modData keys above into that engine.

Rules:
- visible `PokemonNpc` remains the Pokemon identity/visual/Shiny actor;
- hidden Monster proxy remains Pelipper combat/controller authority;
- `WildCurrentHealth` and `WildMaxHealth` are authoritative Pokemon HP;
- technical `Monster.Health/MaxHealth` sentinel values are preserved;
- successful Mutation still uses the existing generic Mutation engine for rolls, stat scaling, markers and minions;
- HPx3 remains represented as multiple logical Pokemon HP bars/phases;
- phase recovery writes back to `WildCurrentHealth` only;
- Shiny remains Mutation-excluded;
- owned/companion Pokemon remain excluded.

New status line:
`Pelipper modData HP binding: resolved=... | writes=... | invalid=... | fallbacks=... | last=...`

## Expected live behavior
For a normal non-Shiny wild Pokemon:

1. `teamup_mutation force` should succeed instead of `transform-blocked sourceHP-unresolved`.
2. `teamup_mutation status` should show `Pelipper modData HP binding: resolved>0`.
3. `Pelipper SOURCE mutation` should show `forceTransforms>0` for forced test.
4. The visible Pokemon should receive Mutation feedback/aura while the hidden proxy sentinel stays intact.
5. On natural lethal encounters, `sourceDamageCalls`, `hpResolved`, `sourceLethalCandidates`, and core `rolls` should begin increasing.

## Carry-forward locks
- Current Shiny handling is frozen as acceptable unless a concrete regression appears.
- Active Following/Waiting Team Up NPCs cannot receive held-item vanilla gifts.
- Pairing cache remains enabled; do not restore per-pair debug spam.
- Encounter discovery stays at 20Hz; performance remains a live gate until the user explicitly confirms combat lag is gone.
- Capture bonus/chance/rate/multiplier values are not Catch Mode or HP floors.
- NPC base damage remains moderate with slow level scaling so future gear, skills, traits and builds retain meaningful power budget.
- Lower Workings remains unchanged and still gates 6.7.45.
- Story slots 4/4, formation cap 5 PEOPLE including Farmers, George/Evelyn lore locks unchanged, no exact `SECTOR 17`, no final boss.

## Next live test
Fresh session, normal non-Shiny Pelipper wild Pokemon:

`teamup_mutation force`

then:

`teamup_mutation status`

Return the full lines beginning:
- `Pelipper modData HP binding:`
- `Pelipper SOURCE mutation:`
- `Mutation:`

If force succeeds, fight that Mutant through its HP phases and report aura, minions and final defeat behavior. Then kill several normal non-Shiny wild Pokemon naturally and check whether `rolls` rises above zero.
