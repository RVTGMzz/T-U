# Team Up - Canonical Latest Handoff

Read this file first when continuing Team Up. Detailed current notes are in `docs/ALPHA_6_7_44_10_PELIPPER_SPECIES_PAIRING_PROBE_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.10`
- Branch: `v0.2-alpha6-7-44-10-pelipper-species-pairing-probe`
- Base: `v0.2-alpha6-7-44-9-pelipper-source-probe-i18n`
- CI-verified input SHA: `671384cae203fb9486b8b10b6b81841aad9aa5de`
- Successful CI run: `34746045289`
- Successful CI job: `103694070503`
- Artifact ID: `10314790062`
- Artifact name: `team-up-alpha6-7-44-10-pelipper-species-pairing-probe`
- Artifact wrapper SHA256: `4a65685412dbcba9a55ee8797a02bce83a41a5f05e2200b952ae3f36deb6a486`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.10_PELIPPER_SPECIES_PAIRING_PROBE_TEST.zip`
- Inner ZIP SHA256: `7ba49566fa9350ac594f37d339ad57dfa330dc15c21404abdc3f8f5f39b48642`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared
- 6.7.45: NOT started

## Live truth from 6.7.44.9
The user tested a normal wild Clauncher:

`Không thể cưỡng chế đột biến cho Wild Clauncher.`

Status then showed:

`Pelipper SOURCE mutation: ... transformBlocked=1 ... sourceHP-unresolved proxy=Green Slime force=True`

`Pelipper SOURCE HP probe: runs=0 | cached=0 | last=identity-unresolved proxy=Green Slime`

The key finding is that source HP probing never started because source/proxy identity failed first. Historical Pelipper 1.2.0 logs confirm wild encounters are still built as a visible NPC plus a separate combat proxy. The proxy can retain `displayName=Wild <Pokemon>` while its technical Monster name is `Green Slime`.

## 6.7.44.10: late unique-species source pairing
New service:
`src/TeamUp/Core/Alpha674410PelipperSpeciesPairingService.cs`

It runs only after the canonical `PelipperWildEncounterIdentityService.TryResolve` fails.

Policy:
- read the proxy Pokemon-facing display name;
- strip `Wild ` / `Shiny `;
- find visible Pelipper wild actors with the exact same normalized species;
- one unique same-species source => pair it;
- multiple same-species sources => pair only if exactly one intersects the proxy;
- otherwise fail closed;
- stamp/reuse encounter ID markers after successful pairing.

New status line:

`Pelipper species pairing: attempts=... | resolved=... | ambiguous=... | noMatch=... | last=...`

This fallback is intentionally conservative. It must never guess between duplicate same-species encounters.

## Source HP gate remains fail-closed
6.7.44.8 source-aware Mutation remains authoritative. Hidden proxy/sentinel HP is never treated as Pokemon HP.

6.7.44.9 source HP probe remains active. If species pairing succeeds but HP layout remains unknown, the next status should finally show `Pelipper SOURCE HP probe: runs>0` with real `type=... candidates=[...] members=[...]` data. Use that live payload for any 6.7.44.11 HP binding. Do not guess another HP field name.

## Force UI
`teamup_mutation force` remains EN/VI locale-aware and now strips encounter prefixes from the user-facing target name.

Expected Vietnamese rejection:

`Không thể cưỡng chế đột biến cho Clauncher.`

Do not expose `Wild Clauncher` or `Green Slime` in this HUD/log result.

## Safety / carry-forward locks
- Test only normal non-Shiny wild Pokemon for this gate.
- Shiny remains source-aware, Emergency Hold-capable, and Mutation-excluded.
- Owned/companion Pokemon remain excluded from normal wild Mutation.
- Pelipper source/render/controller authority remains preserved.
- Mutation remains fail-closed until real source HP is proven.
- Active teammate gifting guard remains unchanged.
- Encounter discovery stays at 20Hz; performance remains a live gate.
- Lower Workings is unchanged and remains a required runtime gate.
- Story NPC slots remain 4/4; hard formation cap remains 5 PEOPLE including Farmers.
- George pre-6.7.46 remains observed Rank D / Non-Combatant / unrecruitable.
- Evelyn main-story identity remains ordinary low Rank D healer/support.
- No exact `SECTOR 17`; no final boss.
- 6.7.45 remains reserved for Containment Chamber Escalation Encounter and is NOT started.

## CI
Successful run `34746045289` passed:
- `VERSION + BUILD ENVIRONMENT AUDIT: PASS`
- `PELIPPER UNIQUE-SPECIES SOURCE PAIRING AUDIT: PASS`
- `PELIPPER SOURCE HP PROBE CARRY-FORWARD: PASS`
- `PELIPPER SOURCE MUTATION FAIL-CLOSED AUDIT: PASS`
- `FORCE TARGET EN/VI + DISPLAY CLEANUP AUDIT: PASS`
- `GIFT GUARD + EN/VI PARITY CARRY-FORWARD: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT AUDIT: PASS`
- `0 Warning(s)`, `0 Error(s)`

Run #1 `34745980939` failed only from an over-broad static assertion that matched `Green Slime` inside a developer comment. The runtime implementation was not the cause. The audit was corrected before successful run #2.

## Next live test
On a fresh session, stand near a normal non-Shiny Pelipper wild Pokemon, preferably a species that appears only once in the current location.

Run:

`teamup_mutation force`

then:

`teamup_mutation status`

Return these complete lines:
- `Pelipper species pairing: ...`
- `Pelipper SOURCE mutation: ...`
- `Pelipper SOURCE HP probe: ...`

Expected progression:
1. user-facing force text shows the actual species without `Wild ` and never `Green Slime`;
2. `resolved` increases if the late species fallback finds a unique source;
3. then either source HP resolves and force progresses, or HP probe `runs>0` exposes the actual Pelipper source runtime layout for the next compatibility fix.

New-chat instruction:
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-10-pelipper-species-pairing-probe. Live-test teamup_mutation force + status trên Pokémon hoang non-Shiny; đọc Pelipper species pairing, Pelipper SOURCE mutation và Pelipper SOURCE HP probe trước khi làm tiếp.`
