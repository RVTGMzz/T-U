# Team Up handoff: 0.2.0-alpha.6.7.44.6

## Source of truth

- Branch: `v0.2-alpha6-7-44-6-pelipper-runtime-performance`
- Version: `0.2.0-alpha.6.7.44.6`
- CI-verified source commit: `ff7457e9840a272e721f2faef50390cad2060755`
- `main` is not merged.
- 6.7.45 story work has NOT started.
- This is a live-test candidate, not stable.

## Live findings that triggered 6.7.44.6

The user's 6.7.44.5 SMAPI log proved source-aware Shiny handling is working in game:

- Team Up resolved real Pelipper source names such as `Sentret` and `Exeggcute` while the combat proxy remained `Green Slime`.
- Shiny reaction logged `hold=True` and the user confirmed the full party actually stopped attacking.
- Therefore source-aware Shiny detection + Shiny Emergency Hold are LIVE PROVEN at least in this test environment.

The same log exposed three remaining regressions:

1. Capture safety interpreted `EnableCaptureTechniqueBonuses` as Catch Mode and `WildEncounterLowHealthCatchBonus=25%` as an HP floor. Those are capture-bonus settings, not the live combat mode / mercy floor.
2. Many Pelipper wild defeats produced no Mutation/SurgeStory telemetry. The existing Mutation system only intercepted `Monster.deathAnimation`, while Pelipper's proxy lifecycle can bypass that path.
3. Performance degraded heavily with many wild Pokemon. 6.7.44.4 source-aware encounter discovery performed repeated source/proxy scanning and Shiny reflection on a full 60Hz update path.
4. At least one ordinary Pelipper wild proxy still produced a generic `EliteBoss` reaction.

No Team Up crash/exception was found in that log.

## Implemented in 6.7.44.6

### 1. Real Pelipper combat-mode detection

`PelipperCaptureSafetyService` no longer treats capture-bonus configuration as mode truth.

Rejected semantics include:

- bonus
- chance
- multiplier
- rate
- accuracy
- pity
- odds
- weight
- roll

Config/settings/menu containers are not accepted as live combat-mode authority.

Team Up now looks for high-confidence runtime/player state such as `CombatMode`, `BattleMode`, `CurrentMode`, `ActiveMode`, `BehaviorMode`, equivalent enum/string state, or an explicit `IsCaptureMode`-style boolean.

Policy remains fail-closed:

- mode unresolved -> capture safety OFF
- Defensive/Aggressive/Peaceful/other non-capture mode -> OFF
- Capture/Catch mode positively resolved -> ON
- 10% fallback floor is allowed only after Capture mode is confirmed ON
- a numeric Pelipper field is accepted as an HP floor only when it has explicit mercy/non-lethal/stop-attack semantics
- capture bonus/chance/multiplier values are never an HP floor

Diagnostics now include `modeValue`, `modeSource`, threshold and threshold source.

### 2. Source/proxy identity cache

`PelipperWildEncounterIdentityService` now uses a weak per-proxy cache:

- positive source/proxy pairing: 240 ticks
- negative lookup retry: 15 ticks

This keeps source-aware identity while avoiding repeated full NPC scans every combat tick.

### 3. Encounter discovery throttled from 60Hz to 20Hz

The old `OnAlpha67442UpdateTicking` full-rate discovery handler is replaced by a 3-tick pulse.

- expensive discovery/classification runs about 20 times/sec instead of 60
- confirmed Shiny HOLD state remains stored on the combat proxy, so CombatService still sees HOLD FIRE continuously between discovery pulses
- expected worst added Shiny discovery latency is roughly 50ms at 60 TPS

### 4. Shiny reflection cache

`Alpha67446PelipperRuntimeService` caches expensive `HasExplicitShinyEvidence` reflection:

- positive Shiny result: 3600 ticks
- negative result: 120 ticks

This preserves source-aware Shiny detection while sharply reducing repeated reflection under dense Pelipper wild populations.

### 5. Pelipper pre-lethal Mutation bridge

6.7.44.6 patches loaded `Monster.takeDamage` implementations whose first argument is damage.

For a genuine Pelipper wild combat proxy, only when the incoming hit can be lethal:

- owned/companion actors remain excluded by the existing eligibility policy
- confirmed Shiny is excluded
- existing Mutant/minion is excluded
- Team Up invokes the existing Mutation/Surge story roll BEFORE Pelipper can remove the proxy
- if the Mutation succeeds, the lethal damage argument is changed to zero so the same hit does not instantly kill the newly transformed Mutant
- if compatibility interception fails, Pelipper's native damage path continues normally

The original `Monster.deathAnimation` Mutation hook remains for vanilla/other mods.

Runtime testing must verify Pelipper does not subsequently produce a duplicate death-path roll after a failed pre-lethal roll.

### 6. Ordinary Pelipper proxy Elite/Boss guard

A generic Pelipper wild combat proxy is no longer treated as Elite/Boss solely because generic proxy metadata contains boss/elite-like semantics.

Trade-off: a Pelipper-specific real wild boss represented only through generic proxy metadata could be suppressed by this conservative hotfix. This requires later explicit Pelipper boss identity support if such encounters need Team Up boss reactions.

### 7. New runtime diagnostic

Command:

`teamup_pelipper_runtime`

Reports:

- 6.7.44.6 runtime hooks/cache state
- live Pelipper capture-mode policy
- Mutation counters/status
- latest Mutation telemetry

Existing commands remain:

- `teamup_encounter status`
- `teamup_mutation status`
- `teamup_mutation list`
- `teamup_mutation force`

## CI / package verification

- Workflow: `Team Up v0.2.0-alpha.6.7.44.6 Pelipper Runtime Performance`
- Successful run: `34686942790`
- Job: `103535463049`
- CI head: `ff7457e9840a272e721f2faef50390cad2060755`
- Result: PASS
- C# build: 0 warnings / 0 errors
- Artifact ID: `10296097130`
- Artifact: `team-up-alpha6-7-44-6-pelipper-runtime-performance`
- Artifact-wrapper SHA256: `6cd74223c46d0f6bb8942663c459efb24744c426d51350b09a1fdaf1898107fe`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.6_PELIPPER_RUNTIME_PERFORMANCE_TEST.zip`
- Test ZIP SHA256: `d4e003715b5a03886d20beaa3c8fcb3d2e91dc1eb5b7911fe36efbb722757f27`

Audits PASS:

1. version + build environment
2. real Pelipper combat-mode / bonus rejection
3. encounter performance guards
4. Pelipper pre-lethal Mutation + Elite guard
5. source-aware Shiny carry-forward
6. Lower Workings + EN/VI carry-forward
7. C# build
8. ZIP content

## Runtime acceptance required

Do NOT call 6.7.44.6 stable until the following live tests pass.

### Performance

Go to a Pelipper-dense Farm/Forest with an active Team Up party.

Expected: the severe lag seen on 6.7.44.5 should be materially reduced while Shiny Hold still triggers promptly.

### Pelipper combat mode

Run `teamup_pelipper_runtime` or `teamup_encounter status` outside Capture mode.

Expected:

- `modeSource` must NOT be `EnableCaptureTechniqueBonuses`
- threshold source must NOT be `WildEncounterLowHealthCatchBonus`
- if non-capture mode is resolved, `catchModeEnabled=False`, `captureSafetyEnabled=False`
- if mode cannot be resolved, safety remains OFF by design

Switch Pelipper to `Bắt giữ` / Capture mode, wait >1.5 seconds, run the diagnostic again.

Expected:

- mode value resolves to Capture/Catch or equivalent
- `catchModeEnabled=True`
- `captureSafetyEnabled=True`
- threshold should normally be fallback 10% unless Pelipper exposes a real mercy-floor field

### Normal wild capture floor

Only while actual Capture mode is ON, Team Up should stop friendly damage around the mercy floor. Outside Capture mode, normal wild Pokemon can be defeated normally.

### Mutation

Use `teamup_mutation status` before/after several Pelipper wild defeats.

Important story lock: before first Surge activation, the first 9 eligible lethal defeats are intended to suppress random Mutation while counting toward the forced first Mutation; the 10th eligible defeat forces the first Mutation.

With the 6.7.44.6 pre-lethal bridge, Pelipper wild kills should now begin advancing Mutation/Surge telemetry. `teamup_mutation force` should still work for a nearby eligible non-Shiny wild target.

### Elite regression

Ordinary Pelipper wild Pokemon must not emit `kind=EliteBoss target=Green Slime`.

### Shiny regression

Real Shiny must still:

- resolve the Pokemon's real display name, not `Green Slime`
- enter HOLD FIRE
- block Team Up friendly damage
- preserve Farmer Engage/Hold/Ignore order by encounter ID
- remain Mutation-excluded
- work even when Capture mode is OFF

## Combat balance design lock

Future Team Up combat balance must preserve room for character leveling, equipment and build progression.

- Base NPC damage should be modest: useful, but not capable of replacing the Farmer or deleting same-tier encounters immediately.
- Raw damage growth by character level should be slow, roughly 3-5% per level as an initial design target rather than large 10-20% jumps.
- A meaningful part of late-game power should come from gear, skill ranks, passives, traits and party synergy.
- Strong signature skills should rely on cooldowns/conditions instead of constant burst spam.
- Additional active party members should eventually use mild diminishing party-DPS scaling so a full party does not scale linearly into room deletion.
- Ordinary NPCs should not routinely one-shot enemies of comparable tier unless a deep build or conditional signature action explicitly earns it.
- Do not prematurely inflate base damage before the equipment/build system is mature.

A later dedicated balance pass should audit base damage, skill multipliers and party-size scaling before expanding the gear system.

## Roster note

The previous user-facing roster breakdown counted about 110 combat-profile characters across vanilla, SVE, RSV, MiMi and Sudoku. Current runtime startup reports `Alpha 6.7.4 roster integrity PASS: 111 profile row(s)`.

Do not hard-lock the public roster count at 110 until a later roster audit reconciles the extra profile row. Pelipper Pokemon are still compatibility/companion actors, not counted as human NPC combat-profile rows.

## Mutant capture limitation

Do NOT claim a Pelipper Mutant is fully non-catchable yet.

6.7.44.6 ensures Mutants bypass Team Up's mercy floor, but the exact Pelipper Poke Ball capture path has not yet been intercepted/proven. Full Mutant non-catchability remains separate compatibility work.

## Lower Workings gate

The Lower Workings 6.7.44.1 runtime gate remains required. Do not start 6.7.45 until Lower Workings runtime acceptance and the 6.7.44.6 Pelipper regression gate pass, unless the user explicitly waives a gate.

## Story locks carried forward

- George remains observed Rank D / Non-Combatant before the planned 6.7.46 reveal. No Rank S, no `The Last Blaster`, no explicit historical miner identity.
- Evelyn's postgame secret remains untouched; main-story presentation stays ordinary low Rank D healer/support.
- No exact `SECTOR 17` unless explicitly designed later.
- Story NPC slots remain 4/4.
- Hard formation cap remains 5 PEOPLE including Farmers.
- Entry Protocol READY + SURGE HIGH prerequisites remain unchanged.
- No final boss.
- Pelipper source ownership/render/controller authority must remain intact.
- 6.7.45 remains reserved for the Containment Chamber Escalation Encounter after runtime gates pass.

## Next-chat instruction

Continue Team Up from `LATEST_TEAM_UP_HANDOFF.md` on branch `v0.2-alpha6-7-44-6-pelipper-runtime-performance`. Live-test 6.7.44.6 performance, real Pelipper Capture mode detection/10% mercy, Pelipper pre-lethal Mutation telemetry, ordinary wild Elite regression, Shiny regression and Lower Workings before starting 6.7.45.
