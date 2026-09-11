# Team Up - Alpha 6.7.44.2 Pelipper Encounter Reactions Handoff

## Checkpoint
- Version: `0.2.0-alpha.6.7.44.2`
- Branch: `v0.2-alpha6-7-44-2-pelipper-encounter-reactions`
- Base: `v0.2-alpha6-7-44-1-tmx-csv-hotfix`
- Successful CI run: `34632929013`
- Successful CI job: `103373890011`
- CI-verified source SHA: `03deba20fae0bf6f763f8d7547c7eb566aea8acd`
- Artifact ID: `10276996823`
- Artifact: `team-up-alpha6-7-44-2-pelipper-encounter-reactions`
- Artifact wrapper SHA256: `91b4e45c8f4f8fa1ef85825451c70bb1faa8d34adbd2308276c254a551594446`
- Inner ZIP: `TeamUp_v0.2.0-alpha.6.7.44.2_PELIPPER_ENCOUNTER_REACTIONS_TEST.zip`
- Inner ZIP SHA256: `e05cdd1f762244cdd5a81067b9cf4636c9e10ff5fd579c42c7bdbfacf466d3be`
- Compiler: `0 Warning(s)`, `0 Error(s)`
- Main: NOT merged
- Stable: NOT declared; live runtime verification required
- 6.7.45: NOT started

## Why 6.7.44.2 exists
This compatibility checkpoint addresses two Pelipper-facing design requirements without consuming the 6.7.45 story slot:

1. The low-HP capture floor must exist only when Pelipper Town is actually present and its Catch/Capture/Mercy mode is positively confirmed enabled.
2. Team Up party members should react to notable encounters, with a special tactical stop for a naturally occurring Shiny Pokemon.

## Capture floor rule - locked
`PelipperCaptureSafetyService` is now fail-closed.

Expected behavior:
- Pelipper Town absent -> capture floor OFF -> normal lethal combat.
- Pelipper Town present + Catch Mode OFF -> capture floor OFF -> normal lethal combat.
- Pelipper Town present + Catch Mode ON -> capture floor ON -> use Pelipper threshold when discoverable; use 10% fallback only after Catch Mode is positively confirmed ON.
- Pelipper present but Catch Mode cannot be resolved safely -> capture floor OFF.

The old broad default-on fallback is removed.

Diagnostics expose:
- Pelipper detected state;
- Catch mode confirmed state;
- final safety enabled state;
- active threshold.

## Shiny Emergency Hold
A confirmed natural Pelipper Shiny is a separate tactical state, independent from Catch Mode.

Detection is deliberately conservative and fails closed:
- target must be a Pelipper wild combat actor;
- explicit Shiny evidence must exist on the HP proxy or a nearby paired visible Pelipper wild actor;
- evidence may come from explicit Shiny name/display identity, modData, reflected Shiny members, or one-level appearance/Pokemon/variant/form/rarity/spawn descriptors.

On confirmation Team Up immediately:
- marks the target `Ronvotri.TeamUp/PelipperShinyConfirmed=true`;
- marks `Ronvotri.TeamUp/MutationExcluded=true` so a natural Shiny can never become a Team Up Mutation seed;
- sets `Ronvotri.TeamUp/ShinyEmergencyHold=true`;
- removes Team Up `CombatTarget` offensive opt-in;
- routes the target through the existing friendly-damage protection layer with damage budget `0`;
- does NOT create or repair an artificial 10% HP floor unless Pelipper Catch Mode is independently ON.

The Shiny scan runs on `GameLoop.UpdateTicking`, before normal Team Up `UpdateTicked` combat selection, so Team Up can enter HOLD FIRE before selecting a new attack on that tick.

## Farmer Shiny orders
When a local Farmer with at least one active Team Up NPC encounters a held Shiny, Team Up opens a tactical prompt:
- `Engage` - release Shiny Hold and allow Team Up attacks. If Pelipper Catch Mode is ON, the normal capture floor still applies later.
- `Keep holding` - continue zero friendly damage and wait.
- `Ignore` - leave the Shiny alone and keep Team Up from targeting it.

Console fallback:
`teamup_encounter status|engage|hold|ignore`

Multiplayer is host-authoritative:
- farmhands send `Alpha67442/ShinyOrderRequest`;
- host validates online Farmer, location and target identity/tile;
- host mutates the target state;
- result returns through `Alpha67442/ShinyOrderResult`.

## Encounter Reaction System
Reaction priority:
1. Shiny
2. Mutation
3. Elite/Boss
4. Special

Behavior:
- Shiny -> personality reaction + tactical HOLD FIRE.
- Mutation -> personality/story reaction only; combat continues.
- Elite/Boss -> personality reaction only; combat continues.
- Special/Surge/story-tagged monster -> attention reaction only; combat continues unless a separate scripted system says otherwise.

Spam control:
- one primary party reaction;
- at most one delayed teammate reply.

Authored personality examples exist for Abigail, Marlon, Harvey, Sebastian, Haley, Wizard, Maru, Demetrius, George, Alex and others. Unknown/custom NPCs fall back to their Team Up `EngagementStyle` and role behavior.

George remains generic/gruff only. This checkpoint contains no reveal, Rank S identity, Last Blaster reference, or historical-miner confirmation.

## Mutation compatibility truth
Important: 6.7.44.2 does NOT yet implement the broader proposed Pelipper Mutation redesign.

Current historical `MonsterMutationService` policy still excludes Pelipper wild/capture actors from Mutation eligibility. Therefore normal wild Pokemon still do not yet advance the first-Mutation 10-defeat counter.

The accepted future design direction remains:
- normal non-Shiny wild Pelipper Pokemon may become eligible natural Mutation candidates;
- natural Shiny remains Mutation-exempt;
- Mutated Pokemon becomes combat-only, non-catchable and non-Shiny;
- owned/companion/source/protected Pokemon remain excluded.

Implement that as a separate compatibility checkpoint only after safely resolving Pelipper runtime Shiny/capture ownership semantics. Do not claim 6.7.44.2 already does it.

## CI acceptance
Run `34632929013` passed:
- `CAPTURE MODE FAIL-CLOSED AUDIT: PASS`
- `SHINY EMERGENCY HOLD AUDIT: PASS`
- `ENCOUNTER PERSONALITY REACTION AUDIT: PASS`
- `MULTIPLAYER SHINY ORDER AUTHORITY AUDIT: PASS`
- `6.7.44.1 LOWER WORKINGS TMX HOTFIX CARRY-FORWARD: PASS`
- `EN/VI PARITY CARRY-FORWARD: PASS`
- `C# BUILD: PASS`
- `ZIP CONTENT + LOWER WORKINGS TMX RE-PARSE: PASS`
- Build: `0 Warning(s)`, `0 Error(s)`

Two earlier CI runs failed only during development and were corrected before this checkpoint:
- run `34632664326`: invalid Stardew `modData` enumeration + C# definite assignment error;
- run `34632832568`: intermediate partial fix;
- run `34632929013`: final successful source.

## Mandatory live acceptance
Do not call this stable from CI alone.

### A. Catch Mode OFF
1. Install Pelipper Town and disable its Catch mode.
2. Use Farmer + at least one Team Up NPC.
3. Fight an ordinary wild Pokemon.
4. It must be able to reach `0 HP`; Team Up must not impose a 10% floor.
5. Run `teamup_encounter status`; expected capture safety `enabled=False`.

### B. Catch Mode ON
1. Enable Pelipper Catch mode.
2. Fight ordinary wild Pokemon with Farmer + Team Up NPC + owned Pokemon if available.
3. Friendly damage must stop at Pelipper's active capture floor.
4. Team Up must cease targeting the protected wild proxy.
5. Existing diagnostics remain useful: `teamup_capture`, `teamup_capture_proxy`, `teamup_preflight`, `teamup_encounter status`.

### C. Real Shiny
1. Encounter a real Pelipper Shiny with at least one following Team Up NPC.
2. Expect immediate reaction/HUD attention and HOLD FIRE.
3. Team Up damage budget must become zero before further Team Up attacks.
4. Shiny must carry Team Up Mutation exclusion.
5. `Hold` keeps protection.
6. `Ignore` leaves it alone.
7. `Engage` releases the Shiny hold.
8. With Catch Mode ON, Engage must still preserve the later Pelipper capture floor.
9. With Catch Mode OFF, Engage permits normal lethal combat.

Because Shiny detection intentionally fails closed, an actual Pelipper Shiny live test is mandatory. If Pelipper stores Shiny identity somewhere Team Up cannot observe, the expected failure is a missed hold, not a false positive. Capture the SMAPI log and `teamup_encounter status` if that occurs.

### D. Encounter reactions
- Mutation should trigger reaction but combat continues.
- Elite/Boss should trigger reaction but combat continues.
- Surge/special/story-tagged monster should trigger attention reaction but combat continues.
- Never more than one primary + one delayed reply per tracked encounter/farmer.

### E. Multiplayer
If possible, let a farmhand receive the Shiny prompt and issue one order. Host must apply the actual monster-state change.

## Lower Workings gate still applies
6.7.44.2 carries forward the 6.7.44.1 TMX hotfix unchanged. Still live-test:
- custom location loads without the old XML/UInt32 exception;
- map renders/collides correctly;
- exact persisted breach return works;
- save/quit/load while inside works;
- three 120-tick survey holds work;
- early withdrawal preserves stage;
- stage 5 safe withdrawal + Guild report reaches stage 6;
- host/farmhand shared route works;
- story reactions 36..41 remain correct;
- George/Evelyn/Pelipper/Mutation regressions remain absent.

## Next target
### 6.7.45 - Containment Chamber Escalation Encounter
Do not start until the Lower Workings 6.7.44.2 live map/warp/save gate passes or the user explicitly waives it.

6.7.45 remains story-only scope:
- first major Lower Workings chamber escalation encounter;
- stronger causal evidence linking current Mutations to the old containment event;
- secured retreat + host authority;
- immediate NPC reactions;
- George still unrevealed until 6.7.46;
- no final boss.

## New-chat instruction
`Tiếp tục Team Up từ docs/LATEST_HANDOFF.md trên branch v0.2-alpha6-7-44-2-pelipper-encounter-reactions. Live-test 6.7.44.2 Pelipper Catch/Shiny + Lower Workings trước khi bắt đầu 6.7.45.`
