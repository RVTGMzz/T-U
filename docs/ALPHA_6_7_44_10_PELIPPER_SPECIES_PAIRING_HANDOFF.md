# Team Up 0.2.0-alpha.6.7.44.10 — Pelipper Species Pairing live finding

Live test confirmed that the late species fallback can pair Pelipper 1.2.0 combat proxies to visible `PelipperTown.PokemonNpc` actors. Examples included Voltorb, Furfrou, Pidgey, Shinx, Mr. Mime, Quaxly, Gossifleur, Skitty, Tinkatink, Abra, Meowth, Taillow, Magnemite, Delcatty, Lillipup, Flabébé, Happiny, Spewpa and Sunflora.

However the live console also exposed a performance/logging regression: the same species pairings were recalculated and logged repeatedly during a single `teamup_mutation force` flow. This can contribute to the previously reported combat lag and floods diagnostics.

`teamup_mutation force` still rejected the selected Voltorb. The pasted live output did not include the expected `teamup_mutation status` telemetry lines after the force result, so the exact source-HP probe payload was not captured in this test.

Next hotfix must:

1. cache successful species pairings by combat proxy instance and validate source presence/species before reuse;
2. stop per-resolution pairing logs; expose aggregate counters only through status;
3. make unresolved source HP invoke the diagnostic probe directly from the fail-closed Mutation path;
4. probe both the paired `PelipperTown.PokemonNpc` source and the Green Slime combat proxy/nested runtime state, cached by source/proxy runtime type pair;
5. keep Shiny behavior frozen, active-teammate gift guard intact, and 6.7.45 unopened.
