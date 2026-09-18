# Alpha 6.7.44.34 - Combat Presence Handoff

Repository: **`ronvotri/T-U`**

Branch: `v0.2-alpha6-7-44-34-combat-presence-fix`

Version: `0.2.0-alpha.6.7.44.34`

## Build identity

- CI source SHA: `9c75bd4790be3eb747f1fd869621540efc86dae5`
- CI run: `35289471845`
- CI job: `105428979340`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.34_COMBAT_PRESENCE_FIX_TEST.zip`
- ZIP SHA256: `177036a76f6164bedf5fd15dd78ba8281eba1df95f2b0322df9efac5ee284d2a`
- Build: PASS, 0 warnings / 0 errors
- Package audit: PASS

## Why this build exists

Ron live-confirmed that 6.7.44.33 restored Pelipper Mutation spawning. The next live issues were:

1. the enlarged Mutant leader visibly flickered;
2. leader and followers only pursued when Farmer was too close;
3. contact damage felt too weak.

6.7.44.34 is intentionally narrow and does not restore the 6.7.44.24-30 crash stack.

## Changes

### Presentation stability

`PelipperVisibleMutationAlpha674413.Update()` is asserted from `RenderingWorld`, immediately before world draw. The old 20Hz presentation write is no longer the primary assertion point. This is intended to stop Pelipper from alternating the visible source between normal and x2 scale frames.

### Aggro arena

Baseline is treated as 6 tiles. Mutation arena is now:

`6 * 3 = 18 tiles`

Inside the arena, Mutation actors are armed for pursuit.

For Pelipper source/proxy pairs Team Up also writes:

```text
Griff.PelipperTown/WildCombatEngaged=true
Griff.PelipperTown/PassiveUntilAttacked=false
```

This counters the native passive-until-hit state seen in live telemetry.

### Damage presence

Pelipper technical combat proxies may expose placeholder damage 1.

6.7.44.34 therefore uses:

- ordinary Mutation follower minimum raw damage: 4;
- Mutant leader minimum raw damage: 8;
- if intended x2 Mutation damage is higher than 8, the higher value wins.

The intended damage is persisted via:

`Ronvotri.TeamUp/MutantIntendedDamage`

### Pairing carry-forward

6.7.44.33 source/proxy restoration remains active:

- source-side `PokemonNpcEncounter/v1` is recognized;
- proxy-side `WildEncounterId` can pair to that source;
- Nidoran♂ and Nidoran♀ remain distinct during normalization.

## Explicit exclusions

The following post-6.7.44.23 services are NOT present in this build:

- `Alpha674424EliteCombatFinalizationService`
- `Alpha674424EliteReachOverlayService`
- `Alpha674425PelipperLeaderContinuousChaseService`
- `Alpha674426PelipperMutantPhaseLifecycleService`
- `Alpha674427MutationRegressionGuardService`
- `Alpha674428LowerWorkingsRuntimeGateService`

This is deliberate because the combined 6.7.44.24-30 path caused live load crashes.

## Live test

Run:

```text
teamup_mutation force
```

Only judge these first:

- visual flicker;
- pursuit distance;
- damage feel.

Do not call 6.7.44.34 Runtime PASS until Ron confirms.

## After live pass

Continue from this runtime shape. Reintroduce leader movement/reach, capture block, three HP phases, x3 reward and Lower Workings validation as isolated patches, one change at a time. Do not wholesale restore the old crash stack.
