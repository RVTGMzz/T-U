# Team Up handoff: 0.2.0-alpha.6.7.44.12

Canonical latest: `docs/LATEST_HANDOFF.md`
Detailed notes: `docs/ALPHA_6_7_44_12_PELIPPER_MODDATA_HP_BINDING_HANDOFF.md`

- Branch: `v0.2-alpha6-7-44-12-pelipper-moddata-hp-binding`
- CI SHA: `c79916d0718c567975fd028f9b0f17f674cda015`
- Run: `34753394198`
- Job: `103713602860`
- Artifact ID: `10316283846`
- ZIP SHA256: `83f80afa3acd28c33aa50287b0ba454ad6be5ba77715e6e69be26b9484927912`
- Build: PASS, 0 warnings / 0 errors
- main not merged
- 6.7.45 not started

Live 6.7.44.11 proved Pelipper authoritative wild HP is `Griff.PelipperTown/WildCurrentHealth` and `Griff.PelipperTown/WildMaxHealth` on the combat proxy modData. `Monster.Health/MaxHealth=1000000` is technical proxy state only.

6.7.44.12 binds those exact HP keys into source-aware Mutation while preserving Pelipper controller authority. Pair cache, current Shiny behavior, active-teammate gift guard, performance safeguards, NPC progression lock and Lower Workings remain carried forward.

Next: run `teamup_mutation force`, then `teamup_mutation status`, and return the `Pelipper modData HP binding`, `Pelipper SOURCE mutation`, and `Mutation` lines.
