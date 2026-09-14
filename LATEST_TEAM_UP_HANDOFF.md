# Team Up handoff: 0.2.0-alpha.6.7.44.14

Canonical latest: `docs/LATEST_HANDOFF.md`
Detailed notes: `docs/ALPHA_6_7_44_14_PELIPPER_X2_LOOT_X3_HANDOFF.md`

- Branch: `v0.2-alpha6-7-44-14-pelipper-x2-loot-x3`
- CI SHA: `96811b01800b2eeaf1a7a3247899818d74a791b1`
- Run: `34862316701`
- Job: `104037413329`
- Artifact ID: `10355521041`
- Artifact wrapper SHA256: `9c0985f806bce5fc87ab98104c44b785064650a01530b73c92e09827332c9c96`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.14_PELIPPER_X2_LOOT_X3_TEST.zip`
- ZIP SHA256: `8f81c20f34305dc3ceaa5f2823b7fd046ae04a05004a51170b25a7d06bf08b8b`
- Build: PASS, 0 warnings / 0 errors
- main not merged
- 6.7.45 not started

## Live truth from 6.7.44.13
Pelipper Mutation core and visible source scaling both work. Fidough visibly scaled from 1->3, proving source presentation is writable, but x3 is too large. Pelipper minion spawning still failed 0/4 with all fallback candidates rejected.

## 6.7.44.14
- Visible Pelipper Mutant scale is capped at x2, including old configs that still request x3. New configs default to x2.
- Pelipper Mutants temporarily suppress the unreliable 2-4 minion wave. Generic/non-Pelipper Mutation minions are unchanged.
- Successful Pelipper Mutants are marked for loot x3.
- At final death, Team Up currently attempts x3 reward by repeating the native Stardew `monsterDrop` pass two extra times with recursion protection. This still requires live validation on Pelipper final death.
- `teamup_mutation status` adds `Pelipper Mutant reward: lootX3 | minions=off | marked=... | minionWavesSuppressed=... | dropHooks=... | dropCalls=... | extraDropPasses=... | errors=... | last=...`.

## Next live test
1. `teamup_mutation force` on a normal non-Shiny Pelipper Pokemon.
2. Confirm visible size is about x2.
3. Confirm no Pelipper minions appear; `minionWavesSuppressed` should rise.
4. Fight through all HPx3 phases. First two depleted bars should increment `phaseGuards`; final bar should end encounter.
5. After final death, run status. Expected for accepted x3 reward: `dropCalls>=1`, `extraDropPasses=2`, `errors=0`, plus visibly increased loot. If `dropCalls=0`, Pelipper uses a different drop path and the reward hook must be moved there.

Carry-forward locks:
- current Shiny behavior remains frozen as accepted unless a concrete regression appears;
- Shiny remains Mutation-excluded;
- active Following/Waiting teammates cannot receive held-item vanilla gifts;
- pair cache/performance safeguards remain enabled;
- never use hidden Green Slime 1,000,000 sentinel HP as Pokemon HP;
- Pelipper retains controller/render/ownership authority;
- Lower Workings remains unchanged and still gates 6.7.45;
- do not claim Mutants fully non-catchable until the actual Pelipper ball-capture path is intercepted and live-tested.
