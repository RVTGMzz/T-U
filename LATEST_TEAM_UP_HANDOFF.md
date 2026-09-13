# Team Up handoff: 0.2.0-alpha.6.7.44.8

## Source of truth

- Branch: `v0.2-alpha6-7-44-8-pelipper-source-mutation`
- Version: `0.2.0-alpha.6.7.44.8`
- CI-verified build commit: `d34b957462b91ab1d74ebf43ac0c30e6fe9d9a61`
- Workflow run: `34736568380`
- Job: `103668940210`
- Artifact ID: `10310853662`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.8_PELIPPER_SOURCE_MUTATION_TEST.zip`
- Test ZIP SHA256: `bc253c80c1022fd8d3c31e3e8a8b52c8ae9e3628f027a1108942b7284facdf01`
- Build result: PASS, 0 warnings / 0 errors.
- `main` is not merged.
- 6.7.45 story work has NOT started.
- This is a live-test candidate, not stable.

## Live finding that triggered 6.7.44.8

6.7.44.7 telemetry ended the RNG ambiguity.

The user fought Pelipper wild Pokemon and then reported:

`Mutation: ... rolls=0 | mutations=0 ...`

`Pelipper mutation bridge: damageCalls=542 | wildDamageCalls=542 | lethalCandidates=0 | mutationAttempts=0 | mutationIntercepts=0 ...`

This proves the old pre-lethal bridge recognized Pelipper wild combat proxies and saw their damage calls, but never considered any hit lethal. Therefore the natural 5% Mutation roll never ran at all.

The user also ran `teamup_mutation force`. It produced:

- `MUTATION DETECTED`
- target name `Green Slime`
- `Forced mutation: Green Slime -> HP 2000000/2000000.`

The visible Pokemon did not meaningfully look mutated afterward.

Diagnosis:

- Pelipper wild encounters have a visible Pokemon source actor plus a hidden `Monster` combat proxy.
- The proxy can be named `Green Slime` and uses a large technical/sentinel HP pool.
- 6.7.44.6/7 incorrectly used `incoming >= proxy.Health` for lethal detection.
- Generic Mutation also scaled the sentinel proxy HP and proxy visuals, causing the misleading 2,000,000 HP Green Slime result.
- The visible source Pokemon is the correct identity/HP/visual actor for Pelipper Mutation presentation.

## New in 6.7.44.8: source-aware Pelipper Mutation

New service:

`src/TeamUp/Core/Alpha67448PelipperSourceMutationService.cs`

### Source HP is now the lethal truth

The new bridge:

1. identifies the Pelipper wild combat proxy;
2. pairs it to the visible source Pokemon through `PelipperWildEncounterIdentityService` / stable `WildEncounterId` when available;
3. conservatively resolves a writable source HP + max HP member using cached runtime reflection;
4. compares incoming damage against the SOURCE Pokemon current HP;
5. enters the existing `MonsterMutationService.TryMutate` roll only for a real source-lethal candidate.

The new Harmony damage prefix uses `Priority.First`, so it runs before the legacy proxy-sentinel hook.

If source HP cannot be resolved, the Pelipper Mutation path fails closed and reports telemetry. It must NOT fall back to mutating sentinel Green Slime HP.

### Existing Mutation engine remains authoritative

The source bridge still calls the existing private `MonsterMutationService.TryMutate` path, so these systems remain centralized:

- Enabled flag / Mutation chance;
- Surge story directives;
- core `rolls` / `mutations` telemetry;
- stat scaling;
- Mutation markers;
- minion-wave spawning;
- normal non-Pelipper Mutation behavior.

### Preserve Pelipper controller authority

For Pelipper targets only, the generic Mutation engine temporarily receives the real source Pokemon HP so it calculates sensible logical Mutation HP.

Immediately afterward Team Up restores:

- original technical proxy `Health`;
- original technical proxy `MaxHealth`;
- original proxy Scale;
- original proxy name/display name.

The hidden proxy Mutation footprint is neutralized to scale 1.

Do NOT permanently replace Pelipper's sentinel proxy HP with Pokemon HP. Pelipper remains authoritative over its source/proxy runtime controller.

### HPx3 becomes source-Pokemon phases

For Pelipper Mutation, the configured health multiplier is represented as source-Pokemon HP phases instead of inflating the hidden proxy sentinel.

Default HPx3 behavior:

- Mutation begins: source Pokemon restored to a full real HP bar;
- first lethal after Mutation: damage is cancelled, source HP restored to full, one extra phase consumed;
- second lethal: same, final extra phase consumed;
- third lethal: allowed through to Pelipper normally.

This approximates three real Pokemon HP bars while preserving Pelipper's own max-HP/controller data.

Markers:

- `Ronvotri.TeamUp/PelipperSourceMutant`
- `Ronvotri.TeamUp/PelipperMutantExtraLives`
- `Ronvotri.TeamUp/PelipperMutantLogicalMaxHp`
- `Ronvotri.TeamUp/PelipperSourceHpAccessor`

### Visible Mutation presentation

Mutation aura now draws around `identity.SourceActor.GetBoundingBox()` for Pelipper mutants rather than around the hidden Green Slime proxy.

Force output is source-aware. Expected form:

`Forced mutation: <Pokemon> -> source HP X/X, HPx3 (3 phase(s)).`

The generic Mutation message temporarily receives the source Pokemon name so `MUTATION DETECTED` should identify the actual Pokemon instead of `Green Slime`.

The visible Pokemon sprite itself is NOT permanently scale-mutated in 6.7.44.8. This is intentional: blindly writing Pelipper renderer scale could leak into captured/owned Pokemon state. For this live gate, source aura + correct Pokemon identity + real HP phases + minions are the safe visible Mutation proof.

## New source Mutation telemetry

`teamup_mutation status` now prints the core Mutation line, the new source bridge line, the legacy proxy bridge line, then the core last-Mutation line.

New line:

`Pelipper SOURCE mutation: sourceDamageCalls=... | hpResolved=... | hpUnresolved=... | sourceLethalCandidates=... | mutationAttempts=... | mutationIntercepts=... | shinyExcluded=... | duplicateSuppressed=... | phaseGuards=... | finalLethalPasses=... | transformBlocked=... | forceTransforms=... | auraDraws=... | damageHooks=... | last=...`

Interpretation:

- `sourceDamageCalls > 0`, `hpResolved > 0`: source Pokemon HP is being found correctly.
- `hpUnresolved > 0` with no `hpResolved`: reflection still does not know Pelipper's source HP member. Preserve the full status line for the next compatibility fix.
- `sourceLethalCandidates > 0`: source-HP lethal detection is alive.
- `mutationAttempts > 0` and core `rolls > 0`: the natural Mutation RNG is truly running.
- `mutationIntercepts > 0`: a natural source-lethal hit successfully became a Mutation.
- `phaseGuards > 0`: a mutated Pokemon consumed an extra HP phase and survived a lethal hit.
- `finalLethalPasses > 0`: all extra Mutation phases were spent and the final lethal hit was allowed through.
- `transformBlocked > 0`: source HP could not be safely resolved for a transform.
- `auraDraws > 0`: the visible Pokemon source is receiving the Mutation aura.

## Shiny policy remains frozen for this hotfix

The user explicitly reports current Shiny handling is acceptable for now.

Keep intact:

- source-aware Shiny identification;
- real Pokemon display name rather than Green Slime;
- Shiny Emergency Hold;
- Shiny exclusion from Mutation;
- stable `WildEncounterId` pairing when available;
- fail-closed ambiguous fallback.

Do not redesign Shiny unless a new concrete regression is observed.

## Active teammate gift guard carried forward

User design lock from 6.7.44.7:

- own active Team Up member in `Following` or `Waiting` + held object + Action => suppress vanilla gift;
- item remains in inventory/hand;
- friendship is unchanged;
- short HUD warning is shown;
- inactive roster members remain normally giftable;
- empty-hand Team Up interaction remains available.

## Pelipper performance / capture policy carried forward

Performance carry-forward:

- weak per-proxy source/identity cache;
- positive/negative cache windows;
- cached Shiny reflection evidence;
- encounter discovery at 20Hz instead of 60Hz;
- encounter-ID pairing to avoid repeated ambiguous scans.

The user has not explicitly confirmed the old combat lag is fully gone. Performance remains a live gate.

Capture carry-forward:

- do not treat capture bonus/chance/rate/multiplier settings as Catch Mode;
- capture safety remains fail-closed unless a real Catch/Capture/Mercy combat mode is positively identified;
- Shiny Emergency Hold is independent of capture mode;
- Mutants do not inherit Team Up's Pelipper mercy/capture floor;
- owned/companion Pokemon remain excluded from normal wild Mutation behavior.

## Lower Workings / story locks

Do not start 6.7.45 until Pelipper Mutation runtime and Lower Workings gates pass unless the user explicitly waives the gate.

Lower Workings source locks remain:

- dedicated `Ronvotri.TeamUp_LowerWorkings` location;
- 32x24 TMX, Back / Buildings / Front;
- persisted breach return;
- three 120-tick survey clues;
- host-authoritative story writes;
- safe withdrawal behavior preserved.

Story locks:

- George before 6.7.46 remains observed Rank D / Non-Combatant / unrecruitable. No Rank S, no `The Last Blaster`, no explicit historical miner identity.
- Evelyn main story remains ordinary low Rank D healer/support; secret reveal remains postgame only.
- Do not introduce exact `SECTOR 17` unless explicitly designed later.
- Story NPC slots remain 4/4.
- Hard formation cap remains 5 PEOPLE including Farmers.
- Entry Protocol READY + SURGE HIGH prerequisites remain unchanged.
- No final boss yet.
- Pelipper source ownership/render/controller authority is preserved.
- 6.7.45 remains reserved for the Containment Chamber Escalation Encounter.

## Next live-test instruction

Use the 6.7.44.8 test ZIP on a fresh game session.

1. Find a normal non-Shiny Pelipper wild Pokemon.
2. Run `teamup_mutation force` once.
   - It should name the actual Pokemon, not Green Slime.
   - It should not show 2,000,000/2,000,000 proxy HP.
   - A visible pulsing Mutation aura should surround the Pokemon.
   - Minions should appear from the existing Mutation system.
3. Run `teamup_mutation status` and preserve the full new `Pelipper SOURCE mutation:` line.
4. For the natural path, defeat several normal non-Shiny Pelipper wild Pokemon.
5. Run `teamup_mutation status` again.
   - `sourceLethalCandidates` should rise.
   - core `rolls` should rise with eligible defeats.
   - A natural success should increase `mutationIntercepts`.
6. If a Mutation appears, continue fighting it long enough to verify `phaseGuards` rises before `finalLethalPasses`.
7. Reconfirm Shiny still holds correctly and the active-teammate gift guard still works.
8. Observe combat performance/lag.
