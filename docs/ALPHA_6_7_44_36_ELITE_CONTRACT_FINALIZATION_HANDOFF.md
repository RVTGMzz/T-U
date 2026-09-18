# Alpha 6.7.44.36 - Elite Contract Finalization Handoff

Repository: `ronvotri/T-U`

Branch: `v0.2-alpha6-7-44-36-elite-contract-finalization`

Version: `0.2.0-alpha.6.7.44.36`

## Build

- CI source SHA: `a20fc738743eabb88bd8f9c080ce0cb12af40ac6`
- Run: `35362286478`
- Job: `105656210575`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.36_ELITE_CONTRACT_FINALIZATION_TEST.zip`
- SHA256: `f05ea922ea88de8a5e4f65204208da9f4bcd34951d1d519d9fef2614a3805ea9`
- Build: PASS, 0 warnings / 0 errors
- ZIP audit: PASS

## Contract

6.7.44.36 bundles three previously separate gates into one runtime test.

### Capture

Mutant leader gets `Ronvotri.TeamUp/MutantLeaderNoCapture=true` on both hidden proxy and visible Pelipper source. A compact capture guard patches Pelipper capture/catch/Pokeball paths and blocks only a target carrying the leader marker or actual Mutant identity.

Mutation followers remain native and are not given the no-capture marker.

### Three HP phases

The existing source-aware Pelipper HP engine remains authoritative.

Default Mutation HP multiplier is x3, so the leader uses three native-size source HP bars.

Markers:
- `MutationPhaseTotal=3`
- `MutationPhaseCurrent=1/2/3`
- `PelipperMutantExtraLives=2 -> 1 -> 0`

First and second lethal events restore source HP to full and cancel death. Third lethal event is final.

### Final-only x3 loot

`monsterDrop` is blocked while extra lives remain or current phase is below total phases.

Only final phase authorizes the native drop call. The existing reward service then repeats the native drop pass two additional times, for three native passes total.

## Explicit exclusions

The old 6.7.44.24-30 services remain absent. No Lower Workings runtime code is reintroduced.

## Live test

Run `teamup_mutation force`.

Verify:
1. leader cannot be captured;
2. follower can still be captured;
3. lethal #1 restores full HP with no loot;
4. lethal #2 restores full HP with no loot;
5. lethal #3 kills for real and produces x3 native reward;
6. 6.7.44.35 chase/reach and 6.7.44.34 aggro/damage remain good.

Do not call Runtime PASS until Ron confirms.
