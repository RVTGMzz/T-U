# Team Up 6.7.44.45 Runtime Profile Fallback - Session Handoff

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Development branch:

`v0.2-alpha6-7-44-45-runtime-profile-fallback`

`main` is NOT merged.

Important naming note: **6.7.44.45 is a patch inside the 6.7.44 line. Alpha 6.7.45 Containment Chamber Escalation Encounter has NOT started.**

## Dual runtime authority

There are two separate live-test gates and they must not be conflated.

### A. Lower Workings route authority

The player was already testing:

- Version: `0.2.0-alpha.6.7.44.44`
- Branch: `v0.2-alpha6-7-44-44-lower-route-test-harness`
- Package/source commit: `19e2cf60369e37debff8b5be5eb61d7e2202e463`
- CI run: `36575202368`
- CI job: `109428916981`
- ZIP SHA256: `f6a9dcfda9269bf887376dda99b47f852e0e7bd86537ccef177e8cca40b2b430`
- State: **RUNTIME RETEST REQUIRED**
- Do not make the player restart Mutation/Ponyta/Nidoran tests.

The save is already debug-prepared at `UndergroundMine1`:
- Controlled Breach 5/5, persisted breach location = UndergroundMine1;
- Surge HIGH 4/4;
- roster slots 4/4;
- Entry Protocol 4/4;
- Lower Descent 5/5;
- Interior Survey reset to 0/6;
- Lower Workings map valid 32x24.

Route-test command:
`teamup_lower_route_test status|arm|off`

Normal live route:
1. `teamup_lower_route_test arm`
2. Visit Adventure Guild so Interior Survey can advance to stage 1.
3. Return to the persisted breach in UndergroundMine1.
4. Press Action to enter Lower Workings through the normal story handler.
5. At the Lower Workings arrival tile, press Action again for the normal safe return.
6. Run `teamup_lower_runtime status`.
7. Run `teamup_preflight`.

Pass target:
- entries=1
- entryPass=1
- entryMismatch=0
- returns=1
- returnPass=1
- returnMismatch=0
- mapFail=0
- errors=0
- `lowerRoute=PASS`

The player may still be in this test when the next chat starts. If they provide the result, process that result first.

### B. Uncatalogued NPC profile authority

While the player was testing .44.44, 6.7.44.45 was prepared to fix modded NPC dossiers such as Brianna showing **Profile provider pending / no provider**.

- Version: `0.2.0-alpha.6.7.44.45`
- Branch: `v0.2-alpha6-7-44-45-runtime-profile-fallback`
- Package/source commit: `b5777ee8f5d9e1766f15c4e44bd018ac988144b1`
- CI run: `36619149851`
- CI job: `109579771734`
- CI conclusion: **SUCCESS**
- Release id: `399458243`
- Release tag: `team-up-6.7.44.45-runtime-profile-b5777ee8`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.45_RUNTIME_PROFILE_FALLBACK_TEST.zip`
- ZIP size: `621449` bytes
- ZIP SHA256: `d822023ed955ce29d438aced5bd18f934ba93f9f5653e0adc892bf858183ca44`

Direct ZIP:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.45-runtime-profile-b5777ee8/TeamUp_v0.2.0-alpha.6.7.44.45_RUNTIME_PROFILE_FALLBACK_TEST.zip`

Expected startup includes:

`[TeamUpBuild] version=0.2.0-alpha.6.7.44.45 branch=v0.2-alpha6-7-44-45-runtime-profile-fallback`

and:

`Team Up 6.7.44.45 load-safe capture guard: Pelipper capture-method Harmony patching disabled; hooks=0.`

## What .44.45 changes

A new `RuntimeNpcProfileFallbackCatalog` handles adult human NPCs that are valid Main Party candidates but have no curated Team Up profile.

The fallback:
- supplies Primary and Secondary role;
- supplies affinities for Tank / DPS / Support / Healer / Control;
- supplies recommended engagement;
- supplies passive text;
- supplies a real runtime signature identity so the dossier is not cosmetic-only;
- chooses one of five balanced generic templates deterministically from the NPC internal name;
- keeps fallback affinities low enough that these NPCs remain **Rank D** by default;
- labels the source as **Team Up Runtime Profile**;
- explicitly describes the kit as a Team Up fallback rather than source-mod canon.

Existing curated profiles always win. This does not replace Vanilla, SVE, RSV, MiMi, Sudoku, or other existing catalog entries.

## Safety/exclusion rules

Fallback only applies when the runtime NPC passes existing Main Party classification.

It excludes:
- Pelipper/Pokémon combat actors;
- pets;
- Farmer summons;
- special companions;
- linked companions;
- Stardew Child actors;
- built-in non-Main-Party characters already excluded by Team Up;
- non-villagers and actors that cannot talk.

The fallback does not write config, does not write story/save modData, and does not create combat actors.

## CI history for .44.45

Run 1 failed compile because the new file was missing the `StardewModdingAPI` import required for `Context`.

Run 2:
- build/audit passed;
- artifact upload passed;
- prerelease failed only because the build script still emitted the old `LOWER_ROUTE_TEST_HARNESS` package filename.

Run 3 fixed the package name and completed **SUCCESS**:
- build and audit PASS;
- artifact PASS;
- prerelease PASS.

This history is useful if a future regression appears. Do not treat the first two failed workflow runs as a runtime failure.

## Runtime test for .44.45

After the Lower Workings .44.44 gate is closed, clean-replace Team Up with .44.45 and open a modded NPC that previously showed a pending dossier, especially Brianna.

Expected:
- source is populated instead of "Profile provider pending";
- Primary/Secondary role visible;
- affinity values visible;
- passive text visible;
- signature ability visible;
- fallback NPC remains Rank D;
- existing curated NPC dossiers remain unchanged;
- Pelipper/Pokémon and companion exclusions remain intact.

Do not call this Runtime PASS until the player confirms in-game behavior.

## Carry-forward locks

Preserve all existing 6.7.44 runtime stability work:
- capture-method broad Harmony scan remains disabled, `hooks=0`;
- primary command `teamup_preflight` remains owned by the modern unified layer;
- legacy diagnostic remains `teamup_preflight_legacy`;
- Lower Workings .44 route harness is preserved in .45;
- .34 combat presence/anti-flicker/aggro/damage;
- .35 pursuit/hold/reach;
- .36 no-capture/3 HP phases/final-only x3 loot;
- .37 fail-closed native minion behavior;
- .38 Lower Workings runtime gate v2;
- .39 exact Nidoran gender token mapping;
- .40 unified read-only preflight;
- .42+ capture guard quarantine.

Do not restore the broad Pelipper capture-method scan.

## Next-chat decision tree

If the player says the .44.44 Lower Workings route passed:
1. mark Lower Route runtime PASS;
2. update handoff authority with the runtime evidence;
3. move to .44.45 for the NPC profile live check;
4. do not start Alpha 6.7.45 yet.

If .44.44 Lower Workings route fails:
1. inspect the exact command output / SMAPI log;
2. fix the route on the 6.7.44 line;
3. do not blame the unrelated NPC fallback work;
4. do not start Alpha 6.7.45.

If the player already installed .44.45 and reports a dossier result:
1. verify the loaded build line is .44.45;
2. inspect Brianna or the named uncatalogued NPC;
3. confirm fallback source/roles/affinities/passive/signature/Rank D;
4. verify no companion/Pelipper misclassification;
5. mark only the NPC fallback gate PASS if runtime evidence supports it.

## Future milestone

Exact future milestone label:

**6.7.45 Containment Chamber Escalation Encounter**

It remains unopened until the active 6.7.44 runtime gates are closed.
