# Team Up - Canonical Latest Handoff

Read this file first when continuing Team Up. Detailed implementation notes are in `docs/ALPHA_6_7_44_4_PELIPPER_SOURCE_AWARE_SHINY_HANDOFF.md`.

## Current checkpoint
- Version: `0.2.0-alpha.6.7.44.4`
- Branch: `v0.2-alpha6-7-44-4-pelipper-source-aware-shiny`
- Base: `v0.2-alpha6-7-44-3-pelipper-runtime-hotfix`
- Successful CI input SHA: `1c6652458562174e1d1809d356c24d57e9e694ab`
- CI-materialized source SHA: `73a6179f1f74aaaf3a45b3ff02cb8b8a406b01c4`
- Successful CI run: `34657983661`
- Successful CI job: `103454392595`
- Artifact ID: `10285714352`
- Artifact name: `team-up-alpha6-7-44-4-pelipper-source-aware-shiny`
- Artifact wrapper SHA256: `87e07717ce2022950fdcb2b545fbac6e2d63dffa94da37495d6fe6106dfa2200`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.4_PELIPPER_SOURCE_AWARE_SHINY_TEST.zip`
- Inner ZIP SHA256: `ef3b31f1cc7d81935beb53c20faf2fd9293c6c7789d4cac51e4414c89e37e2c6`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared
- 6.7.45: NOT started

## Corrected Pelipper runtime truth
Live feedback clarified that the `SHINY: Green Slime` prompt occurred while a real Shiny Pokémon was visibly present. The problem was source/proxy identity, not simply a false Shiny.

Pelipper can represent one encounter with:
- a visible/source Pokémon actor that owns Pokémon identity and Shiny state;
- a separate Monster combat proxy that owns HP, damage and Team Up combat targeting.

6.7.44.4 explicitly pairs those actors.

## Source-aware Shiny identity
New service:
`src/TeamUp/Core/PelipperWildEncounterIdentityService.cs`

For a genuine Pelipper wild combat proxy it resolves the nearby visible Pelipper wild source actor, then stamps the proxy with:
- `Ronvotri.TeamUp/PelipperEncounterId`
- `Ronvotri.TeamUp/PelipperDisplayName`

The source actor receives:
- `Ronvotri.TeamUp/PelipperSourceEncounterId`

UI and diagnostics use the source Pokémon name. HOLD FIRE / targeting / damage protection remain applied to the Monster proxy.

Expected example:
- visible/source actor = Shiny Pokémon such as Pikachu;
- combat proxy = `Green Slime`;
- Team Up prompt = `SHINY: Pikachu`, not `SHINY: Green Slime`;
- combat hold still blocks the Green Slime proxy underneath.

## Sticky Shiny tactical orders
Farmer choices remain:
- Engage
- Hold
- Ignore

The choice is now remembered by source-derived `EncounterId` through `_shinyOrdersByEncounterId`.

Prompt identity is `location + encounterId`, not proxy tile or proxy name/type. If Pelipper moves or recreates the combat proxy, Team Up reapplies the remembered order for that encounter instead of opening the prompt again.

Multiplayer order messages include `EncounterId`; host remains authoritative.

## Shiny detection priority
Team Up now prefers Shiny evidence on the visible Pelipper source actor. Combat-proxy Shiny evidence remains a compatibility fallback.

Confirmed natural Shiny remains `MutationExcluded`.

Do not reinterpret a real Shiny as false merely because its underlying proxy is named `Green Slime` or another vanilla monster.

## 6.7.44.3 rules carried forward
### Pelipper capture mercy
For genuine Pelipper wild combat proxies:
- Pelipper wild capture safety is prioritized;
- source threshold is used when resolvable, otherwise fallback is 10%;
- capture clamps / floor repair / ceasefire remain active;
- Team Up Mutants do NOT inherit this mercy floor.

### Elite/Boss
Raw HP is not an Elite/Boss criterion. Explicit boss/elite/champion identity/tag is required.

### Pelipper wild Mutation
- ordinary non-Shiny Pelipper wild proxies are Mutation-eligible;
- confirmed natural Shiny is Mutation-exempt;
- owned/source-controlled companions remain excluded;
- Pelipper wild Mutant restores Team Up combat targeting and does not stop at capture mercy floor.

Still NOT fully implemented/proven:
- explicit interception of Pelipper's actual Poké Ball capture path for a Team Up Mutant. Do not claim Mutants are fully non-catchable yet.

## CI acceptance
Successful run `34657983661` passed:
- `PELIPPER SOURCE/PROXY PAIRING AUDIT: PASS`
- `SOURCE-AWARE SHINY HOLD AUDIT: PASS`
- `SHINY PROMPT IDENTITY + STICKY ORDER AUDIT: PASS`
- `6.7.44.3 PELIPPER RUNTIME HOTFIX CARRY-FORWARD: PASS`
- `LOWER WORKINGS + EN/VI CARRY-FORWARD: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT + LOWER WORKINGS TMX RE-PARSE: PASS`
- `0 Warning(s)`, `0 Error(s)`.

Run #1 `34657913470` failed only because the static audit scoped from an earlier call site and accidentally included multiplayer tile fields. The audit was corrected before successful run #2.

## Lower Workings carry-forward
6.7.44.1 / 6.7.44 Lower Workings remains unchanged:
- `Ronvotri.TeamUp_LowerWorkings`;
- `assets/LowerWorkings.tmx`, 32x24;
- Back / Buildings / Front;
- dynamic return to persisted breach;
- three 120-tick clue holds at `(8,9)`, `(22,8)`, `(23,16)`;
- early withdrawal preserves stages 2..4;
- stage 5 secured withdrawal, Guild report -> stage 6;
- host-authoritative story progression and shared farmhand route;
- story reaction windows 36..41 with EN/VI parity.

## Lore/gameplay locks
- SURGE HIGH.
- Entry Protocol READY.
- Story NPC slots 4/4.
- Hard formation cap = 5 PEOPLE including Farmers.
- George pre-reveal stays observed Rank D, Non-Combatant, unrecruitable, no Rank S, no `The Last Blaster`, no historical-miner confirmation. Reveal remains 6.7.46.
- Evelyn stays ordinary low Rank D healer/support in main story; secret remains postgame.
- Historical worker and source/entity beyond seal remain unidentified.
- No exact `SECTOR 17` unless later explicitly designed.
- No final boss.
- Pelipper source ownership/render/controller authority preserved.
- No legacy fake-hide writer.
- Do not merge `main` until explicitly requested.

## Mandatory live test now
Install ONLY **6.7.44.4**, replacing the previous Team Up folder.

### 1. Real Shiny source/proxy test
Expected:
- Team Up detects the real Shiny;
- prompt shows actual Pokémon/source name, NOT `Green Slime` proxy name;
- Team Up immediately holds fire against combat proxy;
- Hold / Ignore / Engage only needs one answer;
- movement does not reopen prompt;
- proxy recreation should preserve the same tactical choice;
- `teamup_encounter status` should expose source display name + proxy name + encounter ID;
- natural Shiny remains Mutation-exempt.

### 2. Normal Pelipper wild capture floor
Use Farmer + Team Up NPC + player Pokémon.
- reduce normal wild target to capture threshold;
- Team Up must stop friendly attacks there;
- useful: `teamup_capture`, `teamup_capture_proxy`, `teamup_preflight`, `teamup_encounter status`.

Expected protected state remains approximately:
`budget=0`, `protected=True`, `target=False`, `proxy=True`.

### 3. Pelipper wild Mutation
On normal non-Shiny wild target:
- `teamup_mutation force`
- `teamup_mutation list`
- `teamup_mutation status`

Expected:
- force finds/transforms target;
- Mutant stays attackable;
- Mutant does not stop at 10% mercy floor.

### 4. Encounter reaction regression
- ordinary Green Slime must not become Elite/Boss purely due to HP;
- no repeated reaction log/bubble flood;
- Mutation / Elite-Boss / Special reactions continue combat normally;
- only Shiny triggers tactical HOLD.

### 5. Lower Workings gate
Still verify map load, collision, exact ingress/egress, save/reload inside, clue holds, early withdrawal, stage 5 safe return, stage 6 Guild report, farmhand access and reactions 36..41.

## Next target
### 6.7.45 - Containment Chamber Escalation Encounter
Do NOT begin until 6.7.44.4 Pelipper source/proxy Shiny behavior and Lower Workings live gate pass, unless user explicitly waives the gate.

Goals remain:
- first major Lower Workings chamber escalation encounter;
- stronger causal evidence linking current Mutations to old containment event;
- secure retreat and host authority;
- immediate NPC reactions;
- George remains anonymous/unrevealed until 6.7.46;
- no final boss.

## New-chat instruction
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-4-pelipper-source-aware-shiny. Live-test source-aware Shiny name/Hold, Pelipper capture mercy, Pelipper wild Mutation và Lower Workings trước khi bắt đầu 6.7.45.`
