# NEXT SESSION - START HERE

Continue **Team Up!** from the current runtime checkpoint:

- Version: `0.2.0-alpha.6.7.44.17`
- Branch: `v0.2-alpha6-7-44-17-lightweight-minions`
- CI-verified artifact source SHA: `c1df68ef8f6f8d0e7bd73d1876b32c28c914c00e`
- CI run: `34904471245`
- Verified ZIP: `TeamUp_v0.2.0-alpha.6.7.44.17_LIGHTWEIGHT_MUTATION_MINIONS_TEST.zip`
- ZIP SHA256: `dc19dd525401583892f2c056b4448dcb1aeff01531b410f073ec73469f4c680d`
- `main`: not merged
- 6.7.45: not started

Read in this order:

1. `../CONTINUE_HERE.md`
2. `../LATEST_TEAM_UP_HANDOFF.md`
3. `../docs/LATEST_HANDOFF.md`
4. `../docs/ALPHA_6_7_44_17_LIGHTWEIGHT_MINIONS_HANDOFF.md`
5. `CURRENT_CHAT_HANDOFF_V0_2_ALPHA6_7_44_17_2026-09-15.md`

## First task

Live-test 6.7.44.17 on a normal non-Shiny Pelipper wild Pokemon:

```text
teamup_mutation force
teamup_mutation status
```

Confirm 2-4 lightweight followers appear, attack normally, cause no noticeable hitch, and status shows `pelipperLightweight>0` plus `nativePelipperSpawnsAvoided>0`.

Then test all three Mutant HP phases and final x3 leader loot. After that test one non-Pelipper Mutant.

Captureability of temporary followers is not a gate. Performance is the priority. Do not switch back to full native Pelipper follower encounters unless the user explicitly asks for that tradeoff.

Do not start 6.7.45 until current Mutation runtime gates and Lower Workings pass unless the user explicitly waives the gate.
