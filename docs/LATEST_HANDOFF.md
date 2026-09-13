# Team Up - Canonical Latest Handoff

Current detailed handoff: `docs/ALPHA_6_7_44_12_PELIPPER_MODDATA_HP_BINDING_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.12`
- Branch: `v0.2-alpha6-7-44-12-pelipper-moddata-hp-binding`
- CI SHA: `c79916d0718c567975fd028f9b0f17f674cda015`
- Run: `34753394198`
- Job: `103713602860`
- Artifact ID: `10316283846`
- ZIP SHA256: `83f80afa3acd28c33aa50287b0ba454ad6be5ba77715e6e69be26b9484927912`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

Live 6.7.44.11 proved Pelipper wild HP is authoritative in proxy modData keys `Griff.PelipperTown/WildCurrentHealth` and `Griff.PelipperTown/WildMaxHealth`. The proxy's 1000000 Monster HP is technical controller state only.

6.7.44.12 binds those exact HP keys into source-aware Mutation while preserving Pelipper authority. Pair cache, current Shiny handling, active-teammate gift guard, 20Hz encounter discovery, NPC progression lock and Lower Workings are carried forward.

Next live test: run `teamup_mutation force`, then `teamup_mutation status`, and return `Pelipper modData HP binding`, `Pelipper SOURCE mutation`, and `Mutation` lines. If force succeeds, test the Mutation HP phases/aura/minions and then verify natural encounters increase Mutation rolls.
