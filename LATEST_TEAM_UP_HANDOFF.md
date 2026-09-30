# Team Up latest handoff

Repository: **`RVTGMzz/T-U`**

GitHub write account: **`lengochung28191@gmail.com`**

Current development checkpoint: **Team Up v0.2.0-alpha.6.7.44.45**

Development branch:

`v0.2-alpha6-7-44-45-runtime-profile-fallback`

`main` is NOT merged.

**Important:** `6.7.44.45` is still inside the 6.7.44 stabilization line. The future milestone **6.7.45 Containment Chamber Escalation Encounter** has NOT started.

## Read first in a new chat

1. `CONTINUE_HERE.md`
2. `LATEST_TEAM_UP_HANDOFF.md`
3. `docs/LATEST_HANDOFF.md`
4. `docs/ALPHA_6_7_44_45_RUNTIME_PROFILE_FALLBACK_HANDOFF.md`
5. `docs/ALPHA_6_7_44_44_LOWER_ROUTE_TEST_HARNESS.md`
6. `NEXT_CHAT_PROMPT.md`

## Two active runtime gates

### 1. Lower Workings route: .44.44 live test still authoritative

The player was already testing `0.2.0-alpha.6.7.44.44`.

- Branch: `v0.2-alpha6-7-44-44-lower-route-test-harness`
- Package commit: `19e2cf60369e37debff8b5be5eb61d7e2202e463`
- CI run/job: `36575202368` / `109428916981`
- ZIP SHA256: `f6a9dcfda9269bf887376dda99b47f852e0e7bd86537ccef177e8cca40b2b430`
- State: **Lower Route RUNTIME RETEST REQUIRED**
- Load stability PASS
- Mutation PASS
- Elite PASS
- Nidoran live PASS
- Lower map PASS

The save is already debug-prepared in `UndergroundMine1`. Do NOT redo Mutation/Ponyta/Nidoran work.

Command:
`teamup_lower_route_test status|arm|off`

Pass target:
- entries=1, entryPass=1, entryMismatch=0
- returns=1, returnPass=1, returnMismatch=0
- mapFail=0, errors=0
- `lowerRoute=PASS`

If the next user message is the .44.44 test result, process that first.

### 2. Uncatalogued NPC profiles: .44.45 package ready, runtime not yet confirmed

While .44.44 was being tested, .44.45 was built to eliminate pending/no-provider dossiers for eligible modded NPCs such as Brianna.

Verified package:
- Version: `0.2.0-alpha.6.7.44.45`
- Branch: `v0.2-alpha6-7-44-45-runtime-profile-fallback`
- Package/source commit: `b5777ee8f5d9e1766f15c4e44bd018ac988144b1`
- CI run: `36619149851`
- CI job: `109579771734`
- CI: **SUCCESS**
- Release id: `399458243`
- Release tag: `team-up-6.7.44.45-runtime-profile-b5777ee8`
- ZIP: `TeamUp_v0.2.0-alpha.6.7.44.45_RUNTIME_PROFILE_FALLBACK_TEST.zip`
- ZIP SHA256: `d822023ed955ce29d438aced5bd18f934ba93f9f5653e0adc892bf858183ca44`
- ZIP size: `621449` bytes

Direct package:

`https://github.com/RVTGMzz/T-U/releases/download/team-up-6.7.44.45-runtime-profile-b5777ee8/TeamUp_v0.2.0-alpha.6.7.44.45_RUNTIME_PROFILE_FALLBACK_TEST.zip`

What .44.45 adds:
- runtime fallback combat profiles for uncatalogued adult human Main Party candidates;
- deterministic one-of-five balanced generic kits;
- Primary/Secondary role, affinities, engagement, passive, and real runtime signature skill;
- fallback stays Rank D by design;
- source labeled `Team Up Runtime Profile`;
- clearly described as Team Up fallback, not source-mod canon;
- existing curated profiles always win;
- Pelipper/Pokémon, pets, summons, special/linked companions, children, and other non-Main-Party actors stay excluded.

Expected startup:
`[TeamUpBuild] version=0.2.0-alpha.6.7.44.45 branch=v0.2-alpha6-7-44-45-runtime-profile-fallback`

and:
`Team Up 6.7.44.45 load-safe capture guard: Pelipper capture-method Harmony patching disabled; hooks=0.`

Do not call .44.45 Runtime PASS until the player tests an NPC dossier in-game.

## Next sequence

1. Finish the current .44.44 Lower Workings route test if it is still in progress.
2. If the route passes, update the handoff with runtime evidence.
3. Then clean-replace with .44.45 and open Brianna or another previously pending modded NPC dossier.
4. Confirm source/roles/affinities/passive/signature are populated and fallback Rank D remains.
5. Only after these 6.7.44 gates are closed should **6.7.45 Containment Chamber Escalation Encounter** begin.

## Locks

- Capture broad Harmony scan stays disabled, `hooks=0`.
- Do not restore declaring-type capture matching.
- `teamup_preflight` remains the unified primary command.
- Legacy diagnostic remains `teamup_preflight_legacy`.
- Preserve .44 Lower Workings route harness in .45.
- Do not merge `main` yet.
