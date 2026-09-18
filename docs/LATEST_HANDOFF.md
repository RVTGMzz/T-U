# Team Up - Canonical Latest Handoff

Repository: **`ronvotri/T-U`**

Current restart file: `../CONTINUE_HERE.md`

Current detailed handoff: `ALPHA_6_7_44_34_COMBAT_PRESENCE_HANDOFF.md`

## Current checkpoint

- Version: `0.2.0-alpha.6.7.44.34`
- Branch: `v0.2-alpha6-7-44-34-combat-presence-fix`
- CI source SHA: `9c75bd4790be3eb747f1fd869621540efc86dae5`
- Run: `35289471845`
- Job: `105428979340`
- ZIP SHA256: `177036a76f6164bedf5fd15dd78ba8281eba1df95f2b0322df9efac5ee284d2a`
- Build: PASS, 0 warnings / 0 errors
- Main: NOT merged
- 6.7.45: NOT started

## Runtime authority

Ron live-confirmed 6.7.44.33 can spawn Pelipper Mutation encounters again. Current authoritative feedback is:

- Mutant leader visibly flickers;
- leader and same-species followers require too-close proximity before pursuing;
- contact damage feels too weak.

6.7.44.34 addresses exactly those points and is awaiting live confirmation.

## Active architecture

- visible Pelipper source: `PelipperTown.PokemonNpc`;
- hidden combat proxy: `StardewValley.Monsters.Monster`;
- real Pokemon HP: `WildCurrentHealth` / `WildMaxHealth`;
- 6.7.44.33 source/proxy pairing recognizes `PokemonNpcEncounter/v1` and preserves Nidoran gender identity;
- 6.7.44.34 pre-render reasserts the Mutant presentation scale;
- Mutation combat arena is 18 tiles;
- Pelipper Mutation source/proxy are explicitly engaged and no longer passive inside that arena;
- follower raw contact-damage floor is 4;
- Mutant leader raw floor is 8 with higher intended x2 damage preserved.

## Safety boundary

Do not reintroduce the 6.7.44.24-30 service stack wholesale. It caused game-load crashes during live testing. Re-add any desired contract feature only as an isolated change on the current loadable runtime shape.

## Next runtime sequence

1. Install 6.7.44.34.
2. Run `teamup_mutation force`.
3. Verify no visible leader flicker.
4. Verify leader/followers pursue from substantially farther away.
5. Verify damage is materially stronger.
6. Only after Ron confirms, address remaining leader movement/reach.
7. Reintroduce capture block, three HP phases and x3 loot one isolated patch at a time.
8. Run vanilla/non-Pelipper regression.
9. Rebuild Lower Workings runtime validation without the previous crash path.
10. Only then prepare 6.7.45 unless explicitly waived.

## Carry-forward locks

- Confirmed Shiny remains Mutation-excluded.
- No unrelated GreenSlime visible fallback.
- Spawn Commands may remain OFF outside Team Up's temporary internal gate.
- Native follower capture remains preferred where Pelipper supports it.
- No Runtime PASS without Ron's live confirmation.
