# Team Up handoff: 0.2.0-alpha.6.7.44.4

## Source of truth

- Branch: `v0.2-alpha6-7-44-4-pelipper-source-aware-shiny`
- Version: `0.2.0-alpha.6.7.44.4`
- Source head before this handoff: `e48e4299d85b5c981e0e0382c01b95eeb591147b`
- `main` is not merged and this build is not declared stable.

## Implemented in 6.7.44.4

### Pelipper source-aware wild identity

`PelipperWildEncounterIdentityService` resolves the visible Pelipper wild Pokemon source actor separately from its Monster combat proxy. Team Up stores encounter identity/display metadata on the proxy while leaving Pelipper source ownership/render/controller authority intact.

### Source-aware Shiny detection

`EncounterReactionService` checks explicit Shiny evidence on the resolved Pelipper source actor first, with proxy evidence as a compatibility fallback. Detection is fail-closed and old false-positive Team Up Shiny markers are cleared when explicit evidence is no longer present.

### Shiny Emergency Hold

A confirmed Pelipper Shiny has highest encounter priority. Team Up places the proxy into HOLD FIRE before reaction dialogue, removes Team Up combat targeting, blocks Team Up friendly damage through the existing damage-budget safety path, and waits for Farmer tactical input. ENGAGE/IGNORE/HOLD state is remembered by encounter ID.

Shiny Hold is intentionally independent from Pelipper Catch Mode. A Shiny may trigger HOLD even when Catch Mode is disabled.

### Encounter reactions

Shiny, Mutation, Elite/Boss and Special encounters can trigger personality-aware teammate reactions. Shiny additionally gets the tactical HOLD behavior. Reactions are throttled so the party does not produce a wall of dialogue.

## IMPORTANT blocker discovered before 6.7.44.5

`PelipperCaptureSafetyService` is not yet strict enough for the user's Catch Mode rule.

The intended rule is:

- Pelipper absent -> capture floor OFF -> normal combat.
- Pelipper present + Catch Mode OFF -> capture floor OFF -> normal combat to 0 HP.
- Pelipper present + Catch Mode ON -> capture floor ON -> Team Up respects Pelipper's capture/mercy threshold.
- Catch Mode unknown/unresolved -> OFF, never fail-open.
- A 10% fallback threshold is allowed only after Catch Mode is positively confirmed ON.

Current 6.7.44.4 source still ends policy refresh with `_enabled = pelipperDetected;`. Therefore Pelipper presence alone can enable the capture floor even when a reflected Catch Mode is false or unresolved. `ModeConfirmed` is diagnostic only in this branch.

This is a compatibility blocker, not a completed feature. It must be fixed in the next hotfix branch before treating capture-floor behavior as accepted.

## Runtime acceptance status

Source structure is verified in-repo. Live Stardew/Pelipper runtime acceptance is still required for:

1. normal wild Pokemon source/proxy identity,
2. natural Shiny detection without false positives,
3. immediate Shiny HOLD before Team Up damage,
4. ENGAGE/HOLD/IGNORE behavior,
5. no duplicate source/proxy ownership or render state,
6. Mutation/Shiny priority,
7. multiplayer authority behavior.

Do not describe those as live-proven until an actual game test passes.

## Next branch

Create from this handoff commit:

`v0.2-alpha6-7-44-5-pelipper-catchmode-gate`

Target version:

`0.2.0-alpha.6.7.44.5`

Scope is compatibility/safety only:

1. make Pelipper capture-floor policy fail-closed,
2. positively resolve actual Catch Mode state from Pelipper runtime/config surfaces already present in source,
3. reset stale enabled state when mode becomes false/unknown,
4. expose diagnostics for `PelipperPresent`, `CatchModeDetected`, `CaptureSafetyEnabled`, threshold and threshold source,
5. keep Shiny Emergency Hold independent from Catch Mode,
6. preserve Pelipper source ownership/render/controller authority.

Do not start 6.7.45 story work in this branch.

## Story locks carried forward

- George remains observed Rank D / Non-Combatant before the planned 6.7.46 reveal. No Rank S, no `The Last Blaster`, no explicit historical miner identity.
- Evelyn's postgame secret remains untouched; main-story presentation stays ordinary low Rank D healer/support.
- No exact `SECTOR 17` unless explicitly designed later.
- Story NPC slots remain 4/4. Hard formation cap remains 5 people including Farmers.
- Entry Protocol READY + SURGE HIGH prerequisites remain unchanged.
- No final boss.
- 6.7.45 remains reserved for the Containment Chamber Escalation Encounter after the Lower Workings runtime gate is accepted or explicitly waived.
