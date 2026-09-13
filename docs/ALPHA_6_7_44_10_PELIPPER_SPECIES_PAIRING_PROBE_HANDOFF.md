# Team Up 0.2.0-alpha.6.7.44.10 - Pelipper Species Pairing Probe Handoff

## Source of truth
- Branch: `v0.2-alpha6-7-44-10-pelipper-species-pairing-probe`
- Version: `0.2.0-alpha.6.7.44.10`
- CI-verified input SHA: `671384cae203fb9486b8b10b6b81841aad9aa5de`
- Successful workflow run: `34746045289`
- Successful job: `103694070503`
- Artifact ID: `10314790062`
- Artifact name: `team-up-alpha6-7-44-10-pelipper-species-pairing-probe`
- Artifact wrapper SHA256: `4a65685412dbcba9a55ee8797a02bce83a41a5f05e2200b952ae3f36deb6a486`
- Test ZIP: `TeamUp_v0.2.0-alpha.6.7.44.10_PELIPPER_SPECIES_PAIRING_PROBE_TEST.zip`
- Test ZIP SHA256: `7ba49566fa9350ac594f37d339ad57dfa330dc15c21404abdc3f8f5f39b48642`
- Build: PASS, `0 Warning(s)`, `0 Error(s)`
- `main`: NOT merged
- Stable: NOT declared
- 6.7.45: NOT started

## Live finding from 6.7.44.9
User ran `teamup_mutation force` beside a normal Pelipper wild Clauncher and received the correctly localized line:

`Không thể cưỡng chế đột biến cho Wild Clauncher.`

Immediate status showed:

`Pelipper SOURCE mutation: ... transformBlocked=1 ... last=transform-blocked sourceHP-unresolved proxy=Green Slime force=True`

`Pelipper SOURCE HP probe: runs=0 | cached=0 | last=identity-unresolved proxy=Green Slime`

This changes the active diagnosis. The failure is before HP member discovery: the source/proxy identity resolver is losing the visible Pokemon source during live combat. The proxy still exposes a Pokemon-facing `displayName` (`Wild Clauncher`) even though its technical Monster `Name` is `Green Slime`.

Historical Pelipper 1.2.0 logs confirm each wild spawn is created as a visible NPC plus a separate combat proxy. Previous Team Up logs also successfully paired Shiny source actors to `Green Slime` proxies. Therefore source/proxy architecture remains correct; late pairing is the missing runtime bridge.

## New 6.7.44.10 service
`src/TeamUp/Core/Alpha674410PelipperSpeciesPairingService.cs`

It Harmony-postfixes `PelipperWildEncounterIdentityService.TryResolve` at `Priority.Last` and runs only when the canonical resolver fails.

Fallback policy:
1. Require a genuine Team Up/Pelipper wild combat proxy.
2. Read the proxy Pokemon-facing display name and strip `Wild ` / `Shiny `.
3. Find visible Pelipper wild source actors with the exact same normalized species name.
4. If exactly one same-species source exists, pair it.
5. If several same-species sources exist, accept only if exactly one intersects the proxy.
6. Otherwise fail closed. Never guess across duplicate same-species encounters.
7. Reuse Team Up/Pelipper encounter IDs when available and stamp source/proxy markers after a successful late pair.

Telemetry:

`Pelipper species pairing: attempts=... | resolved=... | ambiguous=... | noMatch=... | last=...`

Expected successful form:

`last=paired species=Clauncher sourceType=<runtime type> proxy=Green Slime encounter=<id>`

## Source HP probe remains the next gate
6.7.44.9 `Alpha67449PelipperSourceProbeService` remains active.

If 6.7.44.10 repairs identity but the current HP resolver still cannot bind Pelipper's real Pokemon HP, the probe should finally move from `runs=0 identity-unresolved` to `runs>0` and print:

`source=<Pokemon> type=<runtime type> ... candidates=[...] members=[...]`

That payload is the source of truth for a possible 6.7.44.11 HP-layout fix. Do not guess another HP field before receiving this live output.

## Force UI cleanup
The force command remains locale-aware. Vietnamese output stays Vietnamese.

The user-facing target name now strips technical encounter prefixes, so expected text is:

`Không thể cưỡng chế đột biến cho Clauncher.`

not `Wild Clauncher`, and never `Green Slime`.

## Safety preserved
- Pelipper Mutation remains fail-closed until real source identity + real source HP are proven.
- Never scale or treat the hidden Green Slime sentinel HP as Pokemon HP.
- Shiny still has priority over Mutation and remains Mutation-excluded.
- Do not live-test forced Mutation on a Shiny.
- Owned/companion Pokemon remain excluded from normal wild Mutation behavior.
- Pelipper render/controller authority remains source-owned.
- Active teammate gift guard remains unchanged.
- 20Hz encounter discovery/performance changes remain unchanged.
- Lower Workings story/runtime source remains unchanged.

## CI
Successful run #2 `34746045289` passed:
- `VERSION + BUILD ENVIRONMENT AUDIT: PASS`
- `PELIPPER UNIQUE-SPECIES SOURCE PAIRING AUDIT: PASS`
- `PELIPPER SOURCE HP PROBE CARRY-FORWARD: PASS`
- `PELIPPER SOURCE MUTATION FAIL-CLOSED AUDIT: PASS`
- `FORCE TARGET EN/VI + DISPLAY CLEANUP AUDIT: PASS`
- `GIFT GUARD + EN/VI PARITY CARRY-FORWARD: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT AUDIT: PASS`
- `0 Warning(s)`, `0 Error(s)`

Run #1 `34745980939` failed only because a static audit rejected the words `Green Slime` inside a developer comment. Runtime code was not proven bad by that failure. The audit was narrowed to runtime literals before successful run #2.

## Next live test
Use a fresh session and a normal non-Shiny Pelipper wild Pokemon, preferably a species that appears only once in the current location.

Run:

`teamup_mutation force`

then immediately:

`teamup_mutation status`

Return these complete lines:
- `Pelipper species pairing: ...`
- `Pelipper SOURCE mutation: ...`
- `Pelipper SOURCE HP probe: ...`

Pass criteria for this checkpoint:
- force UI names `Clauncher`/actual Pokemon without `Wild ` and without `Green Slime`;
- `Pelipper species pairing: resolved` increases, OR telemetry explains why the species fallback stayed ambiguous/no-match;
- after a successful pair, either source HP resolves and force can proceed, or HP probe `runs>0` produces the real runtime member candidates needed for the next fix.

Do not start 6.7.45 until this Pelipper runtime gate and Lower Workings runtime gates pass unless explicitly waived by the user.
