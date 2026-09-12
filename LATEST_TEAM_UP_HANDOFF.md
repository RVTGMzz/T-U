# Team Up handoff: 0.2.0-alpha.6.7.44.7

## Source of truth

- Branch: `v0.2-alpha6-7-44-7-active-teammate-gift-guard`
- Version: `0.2.0-alpha.6.7.44.7`
- CI-verified build commit: `f9472bd9e49781dc2fb5ac37e403294e89843327`
- Workflow run: `34696610701`
- Job: `103561070750`
- Artifact ID: `10299335336`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.7_ACTIVE_TEAMMATE_GIFT_GUARD_TEST.zip`
- Test ZIP SHA256: `016d49854fd322325429270be7ab067ae6116acf160613922fe41598c1a59816`
- Build result: PASS, 0 warnings / 0 errors.
- `main` is not merged.
- 6.7.45 story work has NOT started.
- This remains a live-test candidate, not stable.

## Live findings carried into 6.7.44.7

### Shiny

The user reports Shiny handling is acceptable for now. Keep the current source-aware Shiny Emergency Hold behavior intact. Do not redesign it while the Pelipper runtime gates below are still being tested.

6.7.44.6 pairing rule remains:

- Prefer Pelipper `WildEncounterId` shared by visible source Pokemon and combat proxy.
- If stable IDs are present but mismatch, fail closed. Do not guess by proximity.
- Spatial fallback is allowed only for legacy/no-ID cases and must fail closed when ambiguous.

### Mutation

The user defeated roughly 10 Pelipper wild Pokemon without seeing a natural Mutation.

This alone does NOT prove a bug at the current default 5% chance: the probability of zero successes in ten independent 5% rolls is about 59.9%.

However, previous builds did have a real Pelipper lifecycle problem, so 6.7.44.7 adds explicit bridge telemetry rather than relying on visual luck.

`teamup_mutation status` now prints both the core Mutation counters and:

`Pelipper mutation bridge: damageCalls=... | wildDamageCalls=... | lethalCandidates=... | mutationAttempts=... | mutationIntercepts=... | duplicateSuppressed=... | shinyLethalExcluded=... | last=...`

Interpretation for live tests:

- `wildDamageCalls=0` while fighting Pelipper wild Pokemon: the bridge is not recognizing the combat proxy.
- `wildDamageCalls>0` but `lethalCandidates=0` after actual defeats: the pre-lethal detector is missing Pelipper's lethal path.
- `lethalCandidates>0` and `mutationAttempts>0` but core `rolls=0`: the target reached the bridge but failed Mutation eligibility inside `MonsterMutationService`.
- `mutationAttempts` and core `rolls` both rise while `mutations=0`: the system is rolling and the result can legitimately be RNG/story suppression.
- `mutationIntercepts>0`: a lethal Pelipper hit successfully transformed the live proxy into a Mutant and the lethal hit was cancelled.
- `teamup_mutation force` should immediately transform the nearest eligible normal non-Shiny hostile and is the fastest eligibility sanity test. It does not by itself prove the natural pre-lethal hook.

The 6.7.44.6 pre-lethal hook remains active and duplicate same-tick lethal calls are suppressed.

### Performance / lag

6.7.44.6 introduced:

- weak per-proxy source/identity cache;
- positive/negative cache windows;
- cached Shiny reflection evidence;
- encounter discovery throttled from 60Hz to 20Hz;
- encounter-ID pairing to reduce repeated/ambiguous scans.

The user has not yet explicitly confirmed that combat lag is gone. Treat performance as a pending runtime gate, not a completed fix.

## New in 6.7.44.7: active teammate gift guard

User design lock: do not allow accidental vanilla gifting to an NPC while that NPC is actively deployed in Team Up.

Implemented policy:

- If Farmer is holding an object and presses the Action button while facing their own Team Up member in `Following` or `Waiting`, Team Up suppresses the vanilla action.
- The held item is NOT removed or modified.
- Friendship is NOT changed.
- A short HUD message explains that gifts cannot be given while the NPC is active in Team Up.
- Inactive roster members retain normal vanilla gifting.
- Empty-hand interaction remains available for Team Up member menus/dialogue.
- Keyboard/controller Action inputs use the same guard.

Live acceptance matrix:

1. Active `Following` member + held gift + Action -> gift blocked, item count unchanged.
2. Active `Waiting` member + held gift + Action -> gift blocked, item count unchanged.
3. Inactive roster member + held gift + Action -> vanilla gifting still works.
4. Active member + empty hand -> normal Team Up interaction/menu still works.

## Pelipper capture / Shiny policy carried forward

- Do not treat `EnableCaptureTechniqueBonuses` as Catch Mode.
- Do not treat `WildEncounterLowHealthCatchBonus` or any bonus/chance/rate/multiplier/technique setting as an HP floor.
- Capture safety is fail-closed unless an actual Catch/Capture/Mercy combat mode is positively identified as enabled.
- 10% fallback may be used only after a real capture mode is confirmed.
- Shiny Emergency Hold is independent from capture mode.
- Mutants never inherit the Pelipper mercy/capture floor.
- Owned/companion Pokemon remain excluded from normal wild Mutation behavior.

## NPC damage/progression design lock

Do not let base NPC damage erase the later RPG progression systems.

- Base NPC damage should be useful but moderate.
- Level damage growth should remain modest; current design target is roughly 3-5% per level rather than large jumps.
- A meaningful share of late power should come from equipment, skill ranks, traits and party synergy.
- Ordinary NPCs should not routinely one-shot equal-tier enemies.
- Avoid linear party-DPS snowball as roster size grows; consider diminishing returns / encounter scaling rather than huge per-NPC base numbers.
- Preserve room for future gear/build/specialization systems before tuning late-game numbers upward.

## Current roster source truth

Current hand-crafted combat-profile coverage is approximately 110 characters:

- Stardew Valley vanilla: 30
- Stardew Valley Expanded: 24
- Ridgeside Village: 54
- Cardcha: MiMi
- Hey! You're Cursed!: Sudoku

Pelipper Pokemon are compatibility/companion/wild actors, not counted as NPC roster profiles. East Scarp does not yet have a completed hand-crafted Team Up roster.

## Lower Workings gate

6.7.44.1 Lower Workings map/runtime acceptance remains separate. Do not start 6.7.45 until the Pelipper runtime regressions and Lower Workings gate pass, unless the user explicitly waives the gate.

Lower Workings source locks remain:

- dedicated `Ronvotri.TeamUp_LowerWorkings` location;
- 32x24 TMX with Back / Buildings / Front;
- persisted breach return;
- three 120-tick survey clues;
- host-authoritative story writes;
- safe withdrawal behavior preserved.

## Story / gameplay locks carried forward

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

Use the 6.7.44.7 test ZIP. On a fresh game session:

1. Verify active-teammate gift blocking and inactive-member normal gifting.
2. Run `teamup_mutation status` once before fighting.
3. Defeat at least several normal non-Shiny Pelipper wild Pokemon.
4. Run `teamup_mutation status` again and preserve the full lines for core `rolls/mutations` plus the Pelipper bridge counters.
5. Run `teamup_mutation force` once on a normal non-Shiny wild Pokemon as an eligibility sanity test.
6. Observe whether the previous combat lag is materially reduced.
7. Leave Shiny behavior unchanged unless a new concrete regression is observed.
