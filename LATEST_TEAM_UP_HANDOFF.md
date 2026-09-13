# Team Up handoff: 0.2.0-alpha.6.7.44.9

## Source of truth

- Branch: `v0.2-alpha6-7-44-9-pelipper-source-probe-i18n`
- Version: `0.2.0-alpha.6.7.44.9`
- CI-verified build commit: `4c975a31c0fb06c7c23b74f6c96c4fd5c1262ed4`
- Workflow run: `34737578822`
- Job: `103671569428`
- Artifact ID: `10311014522`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.9_PELIPPER_SOURCE_PROBE_I18N_TEST.zip`
- Test ZIP SHA256: `d2b0d3e27591c2d4e70cfc359d74167d49412af74d32ba7dc90e5f5f3eda67e6`
- Build result: PASS, 0 warnings / 0 errors.
- `main` is NOT merged.
- 6.7.45 story work has NOT started.
- This remains a live-test/diagnostic candidate, not stable.

## Live finding that triggered 6.7.44.9

User tested 6.7.44.8 with `teamup_mutation force` and got:

`Force mutation was rejected for Green Slime.`

Status then showed:

`Pelipper SOURCE mutation: ... transformBlocked=1 ... last=transform-blocked sourceHP-unresolved proxy=Green Slime force=True`

This proves 6.7.44.8 correctly FAILS CLOSED instead of mutating Pelipper's hidden Green Slime sentinel HP, but its source HP resolver still does not understand Pelipper 1.2.0's actual visible-Pokemon HP layout.

The user also correctly flagged two UX bugs:

1. force result was hard-coded English while the game locale is Vietnamese;
2. rejected-force text leaked the hidden combat proxy name `Green Slime` instead of the visible Pokemon identity.

## New in 6.7.44.9

### Source HP failure probe

New service:

`src/TeamUp/Core/Alpha67449PelipperSourceProbeService.cs`

It patches `Alpha67448PelipperSourceMutationService.TryResolveSourceHealth` with a failure-only postfix.

Rules:

- probe runs ONLY when the existing source HP resolver returns false;
- expensive member inspection is cached by source runtime Type;
- normal resolved combat does not continuously scan reflection members;
- probe keeps 6.7.44.8 fail-closed behavior intact;
- it never invents an HP value and never falls back to Green Slime sentinel HP.

Probe output is available through `teamup_mutation status` and `teamup_pelipper_runtime`:

`Pelipper SOURCE HP probe: runs=... | cached=... | last=source=<Pokemon> type=<runtime type> encounter=<id> candidates=[...] members=[...]`

The important live-test payload is `candidates=[...]` plus `members=[...]`. It should reveal Pelipper 1.2.0's actual source actor member shape so the next fix can bind to real HP without guessing.

### Force result localization and source name

`teamup_mutation force` now captures the exact nearest eligible target using the same private `MonsterMutationService.IsEligible` decision used by core force logic.

For Pelipper wild targets it resolves the paired source Pokemon display name before force runs.

User-facing result is locale-aware:

Vietnamese examples:

- `Đã cưỡng chế đột biến: <Pokemon>.`
- `Không thể cưỡng chế đột biến cho <Pokemon>.`
- `Không có quái thường hợp lệ gần đây để cưỡng chế đột biến.`

English equivalents remain available under non-Vietnamese locales.

Do not expose `Green Slime` in force HUD/log text when a paired source Pokemon identity is available.

This hotfix deliberately does NOT edit the large i18n JSON catalogs for this single diagnostic command; it follows the active SMAPI locale through `Helper.Translation.Locale` and preserves existing EN/VI catalog parity.

## Mutation truth carried forward

6.7.44.7 live telemetry proved old proxy-lethal logic was wrong:

- `wildDamageCalls=542`
- `lethalCandidates=0`
- core `rolls=0`

Pelipper's hidden combat proxy can use sentinel/technical HP and must NOT be used as real Pokemon lethal truth.

6.7.44.8 source-aware Mutation remains authoritative for Pelipper:

- pair visible source Pokemon and combat proxy through stable encounter identity;
- source Pokemon identity/HP/visual actor is the intended Mutation truth;
- hidden Green Slime remains Pelipper's combat/controller authority;
- proxy sentinel HP is restored/preserved and must not be permanently scaled;
- HP multiplier is represented as source-Pokemon phases after a successful source-aware Mutation;
- source aura is drawn around the visible Pokemon;
- Shiny wins over Mutation;
- owned/companion Pokemon remain excluded.

Until the source HP member is positively resolved in live Pelipper 1.2.0, transforms remain fail-closed.

## Shiny, gifting, performance and capture locks

Shiny:

- user currently considers Shiny handling acceptable;
- preserve source-aware Pokemon name, Emergency Hold and Mutation exclusion;
- prefer exact Pelipper `WildEncounterId`; ambiguous fallback fails closed.

Active teammate gifting:

- own Team Up member in `Following` or `Waiting` + held item + Action -> vanilla gift blocked;
- item is not consumed and friendship is unchanged;
- inactive roster members remain normally giftable;
- empty-hand interaction remains available.

Performance carry-forward:

- source/proxy identity cache;
- Shiny reflection cache;
- encounter discovery 20Hz instead of 60Hz;
- stable encounter-ID pairing.

User has not yet explicitly confirmed the old combat lag is fully gone. Keep performance as a pending live gate.

Capture:

- never interpret capture bonus/chance/rate/multiplier settings as Catch Mode or HP floor;
- capture safety fails closed unless real Catch/Capture/Mercy combat mode is positively identified;
- Shiny Hold is independent from Catch Mode;
- Mutants do not inherit the mercy/capture floor.

## NPC progression lock

- base NPC damage remains moderate;
- level damage growth target roughly 3-5% per level;
- preserve large power budget for future equipment, skill ranks, traits and party synergy;
- ordinary NPCs should not routinely one-shot equal-tier enemies.

## Current roster source truth

Approximate hand-crafted combat profile coverage: 110 characters.

- Stardew Valley vanilla: 30
- Stardew Valley Expanded: 24
- Ridgeside Village: 54
- Cardcha: MiMi
- Hey! You're Cursed!: Sudoku

Pelipper Pokemon are compatibility actors, not counted as NPC profiles. East Scarp does not yet have a completed hand-crafted roster.

## Lower Workings / story locks

Do not start 6.7.45 until Pelipper runtime regressions and Lower Workings gates pass unless the user explicitly waives the gate.

Carry forward:

- `Ronvotri.TeamUp_LowerWorkings`, 32x24 TMX, Back/Buildings/Front;
- persisted breach return;
- three 120-tick survey clues;
- host-authoritative writes and safe withdrawal;
- Story NPC slots 4/4;
- hard formation cap 5 PEOPLE including Farmers;
- Entry Protocol READY + SURGE HIGH prerequisites;
- George pre-6.7.46 = observed Rank D / Non-Combatant / unrecruitable;
- Evelyn main story remains ordinary low Rank D healer/support;
- no exact `SECTOR 17` and no final boss yet;
- 6.7.45 remains reserved for Containment Chamber Escalation Encounter.

## Next live-test instruction

Use 6.7.44.9 on a fresh game session.

1. Stand near a normal non-Shiny Pelipper wild Pokemon.
2. Run `teamup_mutation force`.
3. Confirm the HUD/log is Vietnamese and names the actual Pokemon instead of `Green Slime`.
4. Run `teamup_mutation status` immediately afterward.
5. Copy the complete line beginning `Pelipper SOURCE HP probe:` plus the `Pelipper SOURCE mutation:` line.
6. If force unexpectedly succeeds, continue fighting that Mutation and also report aura/phases/minions behavior.
7. Preserve current Shiny behavior, gift guard and note whether combat lag is materially reduced.

The next compatibility change should be based on the real probe member paths from this live test, not another guessed HP field name.
