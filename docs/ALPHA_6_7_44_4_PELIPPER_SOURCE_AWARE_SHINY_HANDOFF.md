# Team Up Alpha 6.7.44.4 - Pelipper Source-Aware Shiny Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.44.4`
- Branch: `v0.2-alpha6-7-44-4-pelipper-source-aware-shiny`
- Base: `v0.2-alpha6-7-44-3-pelipper-runtime-hotfix`
- Successful CI run: `34657983661`
- Successful CI job: `103454392595`
- CI input SHA: `1c6652458562174e1d1809d356c24d57e9e694ab`
- CI-materialized source SHA: `73a6179f1f74aaaf3a45b3ff02cb8b8a406b01c4`
- Artifact ID: `10285714352`
- Artifact wrapper SHA256: `87e07717ce2022950fdcb2b545fbac6e2d63dffa94da37495d6fe6106dfa2200`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.4_PELIPPER_SOURCE_AWARE_SHINY_TEST.zip`
- Inner ZIP SHA256: `ef3b31f1cc7d81935beb53c20faf2fd9293c6c7789d4cac51e4414c89e37e2c6`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared
- 6.7.45: NOT started

## Why 6.7.44.4 exists
Live feedback clarified that the 6.7.44.2 `SHINY: Green Slime` prompt happened while a real Shiny Pokémon was visibly present. The core problem was therefore source/proxy identity, not necessarily a false Shiny event.

Pelipper may represent one encounter with two runtime actors:
- visible/source Pokémon actor: owns Pokémon display identity and current Shiny state;
- Monster combat proxy: owns HP, damage, Team Up targetability and capture-floor combat state.

Team Up 6.7.44.4 models those roles explicitly.

## Source-aware encounter identity
New service:
- `src/TeamUp/Core/PelipperWildEncounterIdentityService.cs`

It pairs a Pelipper wild combat proxy to the nearby visible Pelipper wild source actor using source identity + spatial overlap/proximity.

The proxy receives:
- `Ronvotri.TeamUp/PelipperEncounterId`
- `Ronvotri.TeamUp/PelipperDisplayName`

The source actor receives:
- `Ronvotri.TeamUp/PelipperSourceEncounterId`

Display name prefers source Pokémon/species fields and then source display/name, stripping `Wild ` / `Shiny ` prefixes for UI.

Encounter ID prefers stable encounter/spawn/instance/guid identifiers when exposed; otherwise Team Up generates a per-encounter ID and stores it on the source actor.

## Shiny behavior
- Shiny state is read preferentially from the visible Pelipper source actor.
- Combat proxy Shiny evidence remains a compatibility fallback for Pelipper versions that expose state there.
- Natural Shiny still receives Team Up `MutationExcluded`.
- HOLD FIRE / Ignore / Engage state is applied to the Monster combat proxy.
- Prompt now displays the source Pokémon name, not proxy names such as `Green Slime`.
- Diagnostics log both source label and proxy name.

## Sticky tactical order by encounter
A new `_shinyOrdersByEncounterId` dictionary remembers Farmer tactical choice:
- Hold
- Engage
- Ignore

If Pelipper recreates the Monster proxy, the same source-derived encounter ID reapplies the previous order instead of asking again.

Prompt token is now based on `location + encounterId`, not tile position or proxy name/type.

Multiplayer Shiny order messages now include the encounter ID and source display name; host resolution prioritizes encounter ID while tile remains only a nearest-target fallback.

## 6.7.44.3 rules carried forward
- genuine Pelipper wild targets retain low-HP mercy/capture protection with source threshold when available, otherwise 10% fallback;
- Team Up Mutants do not inherit the capture mercy floor;
- raw HP is not an Elite/Boss classifier;
- normal non-Shiny Pelipper wild combat proxies are Mutation-eligible;
- confirmed natural Shiny is Mutation-exempt;
- owned/source-controlled companions remain excluded;
- Pelipper wild Mutants restore Team Up combat targeting.

Still NOT fully implemented/proven:
- explicit interception of Pelipper's real Poké Ball capture path for Team Up Mutants. Do not claim Mutants are fully non-catchable yet.

## CI acceptance
Successful run `34657983661` passed:
- `PELIPPER SOURCE/PROXY PAIRING AUDIT: PASS`
- `SOURCE-AWARE SHINY HOLD AUDIT: PASS`
- `SHINY PROMPT IDENTITY + STICKY ORDER AUDIT: PASS`
- `6.7.44.3 PELIPPER RUNTIME HOTFIX CARRY-FORWARD: PASS`
- `LOWER WORKINGS + EN/VI CARRY-FORWARD: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT + LOWER WORKINGS TMX RE-PARSE: PASS`
- build `0 Warning(s)`, `0 Error(s)`.

Run #1 `34657913470` failed only because the static audit scoped from an earlier call site and accidentally included multiplayer tile fields. Runtime source materialization itself passed. Audit scope was corrected before successful run #2.

## Mandatory live test
Install ONLY 6.7.44.4.

### Real Shiny
Expected:
1. Team Up detects the real Shiny.
2. Prompt shows the Pokémon/source name instead of `Green Slime` or another proxy name.
3. Team Up holds fire immediately.
4. Choose Hold / Ignore / Engage once.
5. No repeated prompt while the proxy moves.
6. If Pelipper recreates the proxy, the same Farmer order remains in force.
7. `teamup_encounter status` should show source display name, proxy name and encounter ID.
8. Natural Shiny remains Mutation-exempt.

### Normal Pelipper wild
- capture mercy floor still works;
- Team Up stops offensive friendly damage at the protected floor;
- `teamup_capture`, `teamup_capture_proxy`, `teamup_preflight` remain useful diagnostics.

### Mutation
On normal non-Shiny Pelipper wild:
- `teamup_mutation force` should transform an eligible target;
- mutant remains attackable;
- mutant does not stop at 10% mercy floor.

### Encounter reactions
- normal Green Slime should not become Elite/Boss purely from high HP;
- reaction spam should remain suppressed;
- Elite/Boss/Mutation/Special reactions continue without forced HOLD except Shiny.

### Lower Workings
6.7.44.1 TMX fix and 6.7.44 story route remain unchanged. Continue custom map, collision, exact ingress/egress, save/reload, clue holds, withdrawal, Guild report, multiplayer and reactions 36..41 live gate.

## Story locks
Unchanged:
- SURGE HIGH;
- Entry Protocol READY;
- story NPC slots 4/4;
- hard formation cap 5 PEOPLE including Farmers;
- George stays observed Rank D / Non-Combatant / unrecruitable before 6.7.46 reveal;
- no Rank S / Last Blaster / historical-miner reveal yet;
- Evelyn secret remains postgame;
- no exact `SECTOR 17` unless later explicitly designed;
- no final boss;
- Pelipper source ownership/render/controller authority preserved;
- no legacy fake-hide writer.

## Next target
`6.7.45 - Containment Chamber Escalation Encounter`

Do NOT start until 6.7.44.4 Pelipper source/proxy Shiny behavior and Lower Workings live gate pass, unless the user explicitly waives the gate.
